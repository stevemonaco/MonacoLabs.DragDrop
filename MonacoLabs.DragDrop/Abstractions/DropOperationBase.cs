using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Abstractions;

public abstract partial class DropOperationBase : AvaloniaObject, IDropOperation
{
    private RoutingStrategies _routing = RoutingStrategies.Bubble;
    private bool _handledEventsToo;
    private bool _isDragLeavePending;

    public IList<string> InteractionIds { get; set; } = [DragDropIds.DefaultOperation];

    /// <summary>
    /// Routing strategy of the drag event handlers. Can only be set while the operation is detached.
    /// </summary>
    public RoutingStrategies Routing
    {
        get => _routing;
        set
        {
            ThrowIfAttached();
            _routing = value;
        }
    }

    /// <summary>
    /// Whether the drag event handlers also receive events that are already handled. Can only be set while the operation is detached.
    /// </summary>
    public bool HandledEventsToo
    {
        get => _handledEventsToo;
        set
        {
            ThrowIfAttached();
            _handledEventsToo = value;
        }
    }

    private void ThrowIfAttached([CallerMemberName] string property = "") =>
        ThrowIf.InvalidOperation(AttachedControl is not null, $"{property} can only be set while {GetType().Name} is detached");

    /// <summary>
    /// Whether a drag started from <see cref="AttachedControl"/> can drop back onto it. False for single-value targets,
    /// where such a drop would replace the value with itself and then let the drag side clear it.
    /// </summary>
    protected virtual bool AllowsSelfDrop => false;

    public virtual void Attach(Control control)
    {
        ThrowIf.InvalidOperation(AttachedControl is not null, $"{GetType().Name} is already attached to {AttachedControl}; an operation can attach to only one control");

        AttachedControl = control;
        AvaDragDrop.SetAllowDrop(control, true);
        SubscribeDropEvents(control);

        DropAdorner ??= new DropHighlightAdorner();
        DropAdorner.TargetControl = AttachedControl;
    }

    public virtual void Detach(Control control)
    {
        if (AttachedControl is null)
            return;

        CancelPendingDragLeave();
        ((IPseudoClasses)AttachedControl.Classes).Set(":dropover", false);
        DropAdorner?.Detach();
        ClearErrorMessage();

        AvaDragDrop.SetAllowDrop(control, false);
        UnsubscribeDropEvents(control);
        AttachedControl = null;
    }

    protected virtual void SubscribeDropEvents(Control control)
    {
        control.AddHandler(AvaDragDrop.DragEnterEvent, DragEnter, Routing, HandledEventsToo);
        control.AddHandler(AvaDragDrop.DragLeaveEvent, DragLeave, Routing, HandledEventsToo);
        control.AddHandler(AvaDragDrop.DragOverEvent, DragOver, Routing, HandledEventsToo);
        control.AddHandler(AvaDragDrop.DropEvent, Drop, Routing, HandledEventsToo);
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
    /// Clears <see cref="ErrorMessage"/>, runs the target's validation including <see cref="CanDrop"/>, and mirrors the resulting message to the drop adorner.
    /// </summary>
    protected bool EvaluateCanDrop(DragEventArgs e)
    {
        ErrorMessage = null;
        var canDrop = CanDropStructurally(e) && CanDrop(e);
        MirrorErrorMessage();
        return canDrop;
    }

    // Collection targets run their location check here, before CanDrop, so overrides of CanDrop can't skip it
    private protected virtual bool CanDropStructurally(DragEventArgs e) => true;

    /// <summary>
    /// Rejects the drag being validated, with <paramref name="message"/> as <see cref="ErrorMessage"/>. Returns false, so validation can
    /// <c>return Reject("...")</c>.
    /// </summary>
    protected bool Reject(string? message)
    {
        ErrorMessage = message;
        return false;
    }

    private protected void ClearErrorMessage()
    {
        ErrorMessage = null;
        MirrorErrorMessage();
    }

    private void MirrorErrorMessage()
    {
        if (DropAdorner is not null)
            DropAdorner.ErrorMessage = ErrorMessage;
    }

    /// <summary>
    /// Picks the effect for a drop from the effects the drag source allows. In-process payloads honor the platform's modifier keys
    /// (Ctrl or Option to copy, Shift to move, both to link) and otherwise prefer Move, then Copy, then Link.
    /// Platform data is always copied or linked.
    /// </summary>
    protected virtual DragDropEffects GetDropEffect(DragEventArgs e) =>
        DropEffectResolver.Resolve(e.DragEffects, e.KeyModifiers, HasInProcessPayload(e), OperatingSystem.IsMacOS());

    /// <summary>
    /// Runs <see cref="PayloadCommand"/> with the drop's <see cref="DropEventArgs"/>. Returns false when there is no command or it cannot execute.
    /// </summary>
    protected bool InvokePayloadCommand(DragEventArgs e, IReadOnlyList<object> items, DropTargetOffset offset, CollectionDropLocation? location = null, bool isPlatformDrop = false)
    {
        if (PayloadCommand is null)
            return false;

        var targetContext = (e.Source as StyledElement)?.DataContext;
        DropEventArgs args = new(e.Source, targetContext, items, offset, location, isPlatformDrop, e.DragEffects);

        if (!PayloadCommand.CanExecute(args))
            return false;

        PayloadCommand.Execute(args);
        return true;
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
        ClearErrorMessage();
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
            var value = ResolveDroppedValue(e.DragEffects, payload);
            SetCurrentValue(PayloadTargetProperty, value);

            var dropMetadata = new DropInfo()
            {
                DragEventArgs = e,
                HoveredControl = AttachedControl!,
                HoverLocation = e.GetPosition(AttachedControl!),
                ReplacedPayload = replaced
            };

            dragInfo.DragOperation.DropCompleted(e.DragEffects, dragInfo, dropMetadata);
            InvokePayloadCommand(e, [value], DropTargetOffset.OnTarget);
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

        ClearErrorMessage();
    }

    protected virtual void OnDrop(DragEventArgs dragEventArgs)
    {
    }

    /// <summary>
    /// Creates the value a Copy drop assigns or inserts in place of <paramref name="payload"/>, and passes to <see cref="PayloadCommand"/>.
    /// Returns the payload itself by default, so a copy shares the source's reference. Not called for Move or Link drops.
    /// </summary>
    protected virtual object CreateCopy(object payload) => payload;

    private protected object ResolveDroppedValue(DragDropEffects effect, object payload) =>
        effect == DragDropEffects.Copy ? CreateCopy(payload) : payload;

    /// <summary>
    /// Checks if the drop is expected to be valid. Override for custom validation, and return <see cref="Reject"/> with a message to explain a rejection.
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
