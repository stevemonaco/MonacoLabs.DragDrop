using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using MonacoLabs.DragDrop.Adorners;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Abstractions;

public abstract partial class DropOperationBase : AvaloniaObject, IDropOperation
{
    public IList<string> InteractionIds { get; set; } = [DragDropIds.DefaultOperation];
    public object? Context { get; set; }
    public RoutingStrategies Routing { get; set; } = RoutingStrategies.Bubble;
    protected bool _handledEventsToo = false;

    private bool _isDragLeavePending;

    protected DropAdornerKind DropAdornerType { get; set; } = DropAdornerKind.Solid;

    /// <summary>
    /// Whether a drag started from <see cref="AttachedControl"/> can drop back onto it. False for single-value targets,
    /// where such a drop would replace the value with itself and then let the drag side clear it.
    /// </summary>
    protected virtual bool AllowsSelfDrop => false;

    public virtual void Attach(Control control)
    {
        ThrowIf.NotNull(AttachedControl);

        AttachedControl = control;
        AvaDragDrop.SetAllowDrop(control, true);
        SubscribeDropEvents(control);

        DropAdorner ??= new DropHighlightAdorner(DropAdornerType);
        DropAdorner.TargetControl = AttachedControl;
    }

    public virtual void Detach(Control control)
    {
        if (AttachedControl is null)
            return;

        AvaDragDrop.SetAllowDrop(control, false);
        UnsubscribeDropEvents(control);
        AttachedControl = null;
    }

    protected virtual void SubscribeDropEvents(Control control)
    {
        control.AddHandler(AvaDragDrop.DragEnterEvent, DragEnter, Routing, _handledEventsToo);
        control.AddHandler(AvaDragDrop.DragLeaveEvent, DragLeave, Routing, _handledEventsToo);
        control.AddHandler(AvaDragDrop.DragOverEvent, DragOver, Routing, _handledEventsToo);
        control.AddHandler(AvaDragDrop.DropEvent, Drop, Routing, _handledEventsToo);
    }

    protected virtual void UnsubscribeDropEvents(Control control)
    {
        control.RemoveHandler(AvaDragDrop.DragEnterEvent, DragEnter);
        control.RemoveHandler(AvaDragDrop.DragLeaveEvent, DragLeave);
        control.RemoveHandler(AvaDragDrop.DragOverEvent, DragOver);
        control.RemoveHandler(AvaDragDrop.DropEvent, Drop);
    }

    protected virtual void DragEnter(object? sender, DragEventArgs e)
    {
        CancelPendingDragLeave();

        if (!IsDropTargetFor(e))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = OnDragEnter(e, EvaluateCanDrop(e));
    }

    // Moving between children raises DragLeave then DragEnter in one pass, so leave only if no DragEnter follows
    protected virtual void DragLeave(object? sender, DragEventArgs e)
    {
        _isDragLeavePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            if (!_isDragLeavePending)
                return;

            _isDragLeavePending = false;
            OnDragLeave(e);
        });
    }

    protected void CancelPendingDragLeave() => _isDragLeavePending = false;

    /// <summary>
    /// Checks whether the drag is meant for this target at all: it carries one of <see cref="InteractionIds"/> and,
    /// unless <see cref="AllowsSelfDrop"/>, didn't start from <see cref="AttachedControl"/>. Drags that fail this get no adorner.
    /// </summary>
    protected virtual bool IsDropTargetFor(DragEventArgs e)
    {
        if (!CanGetPayload(e))
            return false;

        return AllowsSelfDrop
            || !TryGetDragInfo<DragInfo>(e, out var dragInfo)
            || !ReferenceEquals(dragInfo.DragOperation.AttachedControl, AttachedControl);
    }

    /// <summary>
    /// Clears the previous validation message and runs <see cref="CanDrop"/>.
    /// </summary>
    protected bool EvaluateCanDrop(DragEventArgs e)
    {
        if (DropAdorner is not null)
            DropAdorner.ErrorMessage = null;

        return CanDrop(e);
    }

    /// <summary>
    /// Picks the effect for a drop from the effects the drag source allows. In-process payloads honor the platform's modifier keys
    /// (Ctrl or Option to copy, Shift to move, both to link) and otherwise prefer Move, then Copy, then Link.
    /// Platform data is always copied or linked.
    /// </summary>
    protected virtual DragDropEffects GetDropEffect(DragEventArgs e)
    {
        var allowed = e.DragEffects;

        // A Move tells an external source such as a file manager to delete its original
        if (!HasInProcessPayload(e))
            return allowed.HasFlag(DragDropEffects.Copy) ? DragDropEffects.Copy : allowed & DragDropEffects.Link;

        var copyModifier = OperatingSystem.IsMacOS() ? KeyModifiers.Alt : KeyModifiers.Control;
        var requested = (e.KeyModifiers & (copyModifier | KeyModifiers.Shift)) switch
        {
            var m when m == (copyModifier | KeyModifiers.Shift) => DragDropEffects.Link,
            var m when m == copyModifier => DragDropEffects.Copy,
            KeyModifiers.Shift => DragDropEffects.Move,
            _ => DragDropEffects.None
        };

        if (requested != DragDropEffects.None && allowed.HasFlag(requested))
            return requested;

        if (allowed.HasFlag(DragDropEffects.Move))
            return DragDropEffects.Move;

        if (allowed.HasFlag(DragDropEffects.Copy))
            return DragDropEffects.Copy;

        return allowed & DragDropEffects.Link;
    }

    protected void InvokePayloadCommand(DragEventArgs e, IReadOnlyList<object> items, DropTargetOffset offset, CollectionDropLocation? location = null, bool isPlatformDrop = false)
    {
        if (PayloadCommand is null)
            return;

        var targetContext = (e.Source as StyledElement)?.DataContext;
        DropEventArgs args = new(e.Source, targetContext, items, offset, location, isPlatformDrop);

        if (PayloadCommand.CanExecute(args))
            PayloadCommand.Execute(args);
    }

    /// <summary>
    /// Invoked when a drag meant for this target enters the AttachedControl
    /// </summary>
    protected virtual DragDropEffects OnDragEnter(DragEventArgs dragEventArgs, bool canDrop)
    {
        if (DropAdorner is not null)
        {
            DropAdorner.IsDropValid = canDrop;
            DropAdorner.Attach();
        }

        if (!canDrop)
            return DragDropEffects.None;

        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", true);
        return GetDropEffect(dragEventArgs);
    }

    /// <summary>
    /// Invoked repeatedly while a valid drag is over the AttachedControl. Returns the effect to apply.
    /// </summary>
    protected virtual DragDropEffects OnDragOver(DragEventArgs dragEventArgs) => GetDropEffect(dragEventArgs);

    /// <summary>
    /// Invoked when a drag leaves the AttachedControl
    /// </summary>
    protected virtual void OnDragLeave(DragEventArgs dragEventArgs)
    {
        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", false);
        DropAdorner?.Detach();
    }

    /// <summary>
    /// Invoked when a drag is over the AttachedControl
    /// This event fires repeatedly while the drag is over the control, so it should
    /// be reasonably efficient with validation
    /// </summary>
    protected virtual void DragOver(object? sender, DragEventArgs e)
    {
        if (!IsDropTargetFor(e))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        bool canDrop = EvaluateCanDrop(e);

        if (DropAdorner is not null)
        {
            DropAdorner.IsDropValid = canDrop;
            DropAdorner.InvalidateVisual();
        }

        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", canDrop);
        e.DragEffects = canDrop ? OnDragOver(e) : DragDropEffects.None;
    }

    /// <summary>
    /// Completes the DragDrop operation by assigning payload and cleaning up visuals
    /// </summary>
    protected virtual void Drop(object? sender, DragEventArgs e)
    {
        CancelPendingDragLeave();

        if (!IsDropTargetFor(e))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        if (!EvaluateCanDrop(e))
        {
            e.DragEffects = DragDropEffects.None;
            OnDragLeave(e);
            return;
        }

        e.DragEffects = GetDropEffect(e);

        if (TryGetDragInfo<DragInfo>(e, out var dragInfo) && TryGetPayload<object>(e, out var payload))
        {
            var replaced = PayloadTarget;
            SetCurrentValue(PayloadTargetProperty, payload);

            var dropMetadata = new DropInfo()
            {
                DragEventArgs = e,
                HoveredControl = AttachedControl!,
                HoverLocation = e.GetPosition(AttachedControl!),
                ReplacedPayload = replaced
            };

            dragInfo.DragOperation.DropCompleted(e.DragEffects, dragInfo, dropMetadata);
            InvokePayloadCommand(e, [payload], DropTargetOffset.OnTarget);
        }
        else if (TryGetPlatformItems(e, out var items))
        {
            InvokePayloadCommand(e, items, DropTargetOffset.OnTarget, isPlatformDrop: true);
        }

        OnDrop(e);

        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", false);

        if (DropAdorner is not null)
        {
            DropAdorner.IsDropValid = false;
            DropAdorner.Detach();
        }
    }

    protected virtual void OnDrop(DragEventArgs dragEventArgs)
    {
    }

    /// <summary>
    /// Checks if the drop is expected to be valid. Override for custom validation and set <see cref="DropAdornerBase.ErrorMessage"/> to explain a rejection.
    /// </summary>
    protected virtual bool CanDrop(DragEventArgs e) => CanGetPayload(e);

    /// <summary>
    /// Checks if the drag carries data for any of <see cref="InteractionIds"/>, either as an in-process payload or as platform data.
    /// </summary>
    protected virtual bool CanGetPayload(DragEventArgs e)
    {
        return InteractionIds.Any(x => e.DataTransfer.Contains(DragDropIds.FormatFor(x)));
    }

    protected bool HasInProcessPayload(DragEventArgs e) =>
        InteractionIds.Any(x => !DragDropIds.IsPlatform(x) && e.DataTransfer.Contains(DragDropIds.PayloadFormat(x)));

    /// <summary>
    /// Tries to get the in-process Payload to be assigned to the DropTarget
    /// Overriding this allows a data transform from the Payload set by the Drag operation into
    /// what the DropTarget expects, if it's not 1:1 compatible
    /// </summary>
    /// <typeparam name="TPayload">Compatible type of payload expected</typeparam>
    /// <param name="e">Drag event that started the operation</param>
    /// <param name="payload">Payload to be transferred by the operation</param>
    protected virtual bool TryGetPayload<TPayload>(DragEventArgs e, [MaybeNullWhen(false)] out TPayload payload)
        where TPayload : class
    {
        var value = InteractionIds
            .Where(x => !DragDropIds.IsPlatform(x))
            .Select(x => e.DataTransfer.TryGetValue(DragDropIds.PayloadFormat(x)))
            .FirstOrDefault(x => x is not null);

        if (value is TPayload p)
        {
            payload = p;
            return true;
        }

        payload = null;
        return false;
    }

    /// <summary>
    /// Tries to get dropped files and folders when <see cref="PlatformInteractionIds.Files"/> is in <see cref="InteractionIds"/>.
    /// </summary>
    protected bool TryGetFiles(DragEventArgs e, [MaybeNullWhen(false)] out IReadOnlyList<IStorageItem> files)
    {
        files = InteractionIds.Contains(PlatformInteractionIds.Files) ? e.DataTransfer.TryGetFiles() : null;
        return files is { Count: > 0 };
    }

    /// <summary>
    /// Tries to get dropped text when <see cref="PlatformInteractionIds.Text"/> is in <see cref="InteractionIds"/>.
    /// </summary>
    protected bool TryGetText(DragEventArgs e, [MaybeNullWhen(false)] out string text)
    {
        text = InteractionIds.Contains(PlatformInteractionIds.Text) ? e.DataTransfer.TryGetText() : null;
        return text is not null;
    }

    /// <summary>
    /// Tries to get the files, or else the text, of a platform drop.
    /// </summary>
    protected bool TryGetPlatformItems(DragEventArgs e, [MaybeNullWhen(false)] out IReadOnlyList<object> items)
    {
        if (TryGetFiles(e, out var files))
            items = files.ToList<object>();
        else if (TryGetText(e, out var text))
            items = [text];
        else
            items = null;

        return items is not null;
    }

    /// <summary>
    /// Tries to get metadata related to the initiation of the drag operation
    /// </summary>
    /// <typeparam name="TDragMetadata">Compatible type of drag metadata</typeparam>
    /// <param name="e">Drag event that started the operation</param>
    /// <param name="dragMetadata">Metadata from the drag initiation</param>
    protected bool TryGetDragInfo<TDragMetadata>(DragEventArgs e, [MaybeNullWhen(false)] out TDragMetadata dragMetadata)
        where TDragMetadata : DragInfo
    {
        if (e.DataTransfer.TryGetValue(DragDropIds.DragMetadataFormat) is TDragMetadata metadata)
        {
            dragMetadata = metadata;
            return true;
        }

        dragMetadata = null;
        return false;
    }
}
