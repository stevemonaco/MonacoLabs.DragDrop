using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Abstractions;

/// <summary>
/// Makes its attached control accept drags. Every target runs one flow: targeting (<see cref="IsDropTargetFor"/>), validation
/// (<see cref="CanDrop"/>), feedback, the effect (<see cref="GetDropEffect"/>) and, on drop, <see cref="Transfer"/>, the report back to the
/// drag source and <see cref="PayloadCommand"/>. Subclasses override those steps and observe the drag through
/// <see cref="OnDragEnter"/>, <see cref="OnDragOver"/>, <see cref="OnDragLeave"/> and <see cref="OnDrop"/>.
/// </summary>
public abstract partial class DropOperationBase : AvaloniaObject, IDropOperation
{
    private bool _handledEventsToo;
    private bool _isDragLeavePending;
    private DragEventArgs? _hoverEventArgs; // The latest event of the drag hovering this target; null when none is

    /// <inheritdoc/>
    public IList<string> InteractionIds { get; set; } = [DragDropIds.DefaultOperation];

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

    /// <summary>
    /// Whether the drag over the attached control passed validation in its latest event. False when no drag is over it.
    /// </summary>
    protected bool IsDropValid { get; private set; }

    /// <inheritdoc/>
    public void Attach(Control control)
    {
        ThrowIf.InvalidOperation(AttachedControl is not null, $"{GetType().Name} is already attached to {AttachedControl}; an operation can attach to only one control");

        AttachedControl = control;
        AvaDragDrop.SetAllowDrop(control, true);
        control.AddHandler(AvaDragDrop.DragEnterEvent, HandleDragEnter, RoutingStrategies.Bubble, HandledEventsToo);
        control.AddHandler(AvaDragDrop.DragLeaveEvent, HandleDragLeave, RoutingStrategies.Bubble, HandledEventsToo);
        control.AddHandler(AvaDragDrop.DragOverEvent, HandleDragOver, RoutingStrategies.Bubble, HandledEventsToo);
        control.AddHandler(AvaDragDrop.DropEvent, HandleDrop, RoutingStrategies.Bubble, HandledEventsToo);

        DropAdorner ??= new DropHighlightAdorner();
        DropAdorner.TargetControl = control;
        OnAttached();
    }

    /// <inheritdoc/>
    public void Detach(Control control)
    {
        if (AttachedControl is null)
            return;

        _isDragLeavePending = false;
        EndHover(_hoverEventArgs);

        AvaDragDrop.SetAllowDrop(control, false);
        control.RemoveHandler(AvaDragDrop.DragEnterEvent, HandleDragEnter);
        control.RemoveHandler(AvaDragDrop.DragLeaveEvent, HandleDragLeave);
        control.RemoveHandler(AvaDragDrop.DragOverEvent, HandleDragOver);
        control.RemoveHandler(AvaDragDrop.DropEvent, HandleDrop);
        AttachedControl = null;
    }

    private protected virtual void OnAttached()
    {
    }

    private void HandleDragEnter(object? sender, DragEventArgs e)
    {
        _isDragLeavePending = false;
        Hover(e);
    }

    private void HandleDragOver(object? sender, DragEventArgs e) => Hover(e);

    // Moving between children raises DragLeave then DragEnter in one pass, so leave only if no DragEnter follows
    private void HandleDragLeave(object? sender, DragEventArgs e)
    {
        _isDragLeavePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            if (!_isDragLeavePending)
                return;

            _isDragLeavePending = false;
            EndHover(e);
        });
    }

    private void HandleDrop(object? sender, DragEventArgs e)
    {
        _isDragLeavePending = false;

        if (!IsDropTargetFor(e))
        {
            e.DragEffects = DragDropEffects.None;
            if (_hoverEventArgs is not null)
                EndHover(e);
            return;
        }

        if (!Evaluate(e))
        {
            e.DragEffects = DragDropEffects.None;
            EndHover(e);
            return;
        }

        e.DragEffects = GetDropEffect(e);
        e.Handled = true;
        using (BeginCompleteDrop())
            CompleteDrop(e);

        OnDrop(e);
        EndHover(null);
    }

    private void Hover(DragEventArgs e)
    {
        if (!IsDropTargetFor(e))
        {
            e.DragEffects = DragDropEffects.None;
            if (_hoverEventArgs is not null)
                EndHover(e);
            return;
        }

        var isEntering = _hoverEventArgs is null;
        _hoverEventArgs = e;

        Evaluate(e);
        UpdateFeedback(e);

        e.DragEffects = IsDropValid ? GetDropEffect(e) : DragDropEffects.None;
        if (IsDropValid)
            e.Handled = true;

        if (isEntering)
            OnDragEnter(e);
        else
            OnDragOver(e);
    }

    // A null leaveArgs ends the hover without raising OnDragLeave
    private void EndHover(DragEventArgs? leaveArgs)
    {
        var wasHovering = _hoverEventArgs is not null;
        _hoverEventArgs = null;
        IsDropValid = false;
        ClearFeedback();

        if (wasHovering && leaveArgs is not null)
            OnDragLeave(leaveArgs);
    }

    private bool Evaluate(DragEventArgs e)
    {
        ResolveHover(e);

        ErrorMessage = null;
        IsDropValid = CanDropStructurally(e) && CanDrop(e);
        MirrorErrorMessage();
        return IsDropValid;
    }

    private protected virtual void ResolveHover(DragEventArgs e)
    {
    }

    // Collection targets run their location check here, before CanDrop, so overrides of CanDrop can't skip it
    private protected virtual bool CanDropStructurally(DragEventArgs e) => true;

    private protected virtual void UpdateFeedback(DragEventArgs e)
    {
        if (DropAdorner is not null)
        {
            DropAdorner.IsDropValid = IsDropValid;
            DropAdorner.Attach();
        }

        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", IsDropValid);
    }

    private protected virtual void ClearFeedback()
    {
        if (AttachedControl is not null)
            ((IPseudoClasses)AttachedControl.Classes).Set(":dropover", false);

        if (DropAdorner is not null)
        {
            DropAdorner.IsDropValid = false;
            DropAdorner.Detach();
        }

        ErrorMessage = null;
        MirrorErrorMessage();
    }

    // Scopes the transfer, the source's DropCompleted and PayloadCommand
    private protected virtual IDisposable? BeginCompleteDrop() => null;

    private void CompleteDrop(DragEventArgs e)
    {
        if (TryGetDragInfo<DragInfo>(e, out var dragInfo) && TryGetPayloads<object>(e, out var payloads))
        {
            IReadOnlyList<object> values = e.DragEffects == DragDropEffects.Copy ? [.. payloads.Select(CreateCopy)] : payloads;

            if (Transfer(e, dragInfo, values) is { } dropInfo)
            {
                dragInfo.DragOperation.DropCompleted(e.DragEffects, dragInfo, dropInfo);
                InvokePayloadCommand(e, values, isPlatformDrop: false);
            }
            else if (InvokePayloadCommand(e, values, isPlatformDrop: false))
            {
                dragInfo.DragOperation.DropCompleted(e.DragEffects, dragInfo, CreateUntransferredDropInfo(e, values));
            }
            else
            {
                // Reporting Move for a drop that went nowhere would let the source remove the item
                e.DragEffects = DragDropEffects.None;
            }
        }
        else if (TryGetPlatformItems(e, out var items))
        {
            InvokePayloadCommand(e, items, isPlatformDrop: true);
        }
    }

    /// <summary>
    /// Does what this target does with the accepted in-process payloads, and returns the <see cref="DropInfo"/> to report to the drag source,
    /// or null when it transferred nothing. Does nothing by default, so a target that doesn't override it only runs <see cref="PayloadCommand"/>.
    /// </summary>
    /// <param name="e">The drop event.</param>
    /// <param name="dragInfo">The drag's metadata.</param>
    /// <param name="values">The payloads in order, or their <see cref="CreateCopy"/> on a Copy drop.</param>
    protected virtual DropInfo? Transfer(DragEventArgs e, DragInfo dragInfo, IReadOnlyList<object> values) => null;

    private protected virtual DropInfo CreateUntransferredDropInfo(DragEventArgs e, IReadOnlyList<object> values) => new()
    {
        DragEventArgs = e,
        HoveredControl = AttachedControl!,
        HoverLocation = e.GetPosition(AttachedControl!)
    };

    private protected virtual DropTargetOffset CommandOffset => DropTargetOffset.OnTarget;

    private protected virtual CollectionDropLocation? CommandLocation => null;

    // Returns false when there is no command or it cannot execute
    private bool InvokePayloadCommand(DragEventArgs e, IReadOnlyList<object> items, bool isPlatformDrop)
    {
        if (PayloadCommand is null)
            return false;

        var targetContext = (e.Source as StyledElement)?.DataContext;
        DropEventArgs args = new(e.Source, targetContext, items, CommandOffset, CommandLocation, isPlatformDrop, e.DragEffects);

        if (!PayloadCommand.CanExecute(args))
            return false;

        PayloadCommand.Execute(args);
        return true;
    }

    /// <summary>
    /// Called when a drag meant for this target first arrives, after its feedback and effect are updated.
    /// </summary>
    protected virtual void OnDragEnter(DragEventArgs e)
    {
    }

    /// <summary>
    /// Called on each DragOver after the first event of a drag meant for this target, after its feedback and effect are updated.
    /// </summary>
    protected virtual void OnDragOver(DragEventArgs e)
    {
    }

    /// <summary>
    /// Called when the hover ends without an accepted drop, after the feedback is cleared: the drag left, stopped being meant for this target,
    /// was dropped and rejected, or the operation was detached mid-drag, in which case <paramref name="e"/> is the drag's latest event.
    /// </summary>
    protected virtual void OnDragLeave(DragEventArgs e)
    {
    }

    /// <summary>
    /// Called after an accepted drop has transferred, reported and run <see cref="PayloadCommand"/>, before the feedback is cleared.
    /// </summary>
    protected virtual void OnDrop(DragEventArgs e)
    {
    }

    /// <summary>
    /// Checks whether the drag is meant for this target at all: it carries one of <see cref="InteractionIds"/>, carries one payload unless
    /// <see cref="AcceptsMultiplePayloads"/>, and, unless <see cref="AllowsSelfDrop"/>, didn't start from <see cref="AttachedControl"/>.
    /// Drags that fail this get no adorner.
    /// </summary>
    protected virtual bool IsDropTargetFor(DragEventArgs e)
    {
        if (!CanGetPayload(e))
            return false;

        if (!AcceptsMultiplePayloads && TryGetPayloads<object>(e, out var payloads) && payloads.Count > 1)
            return false;

        return AllowsSelfDrop
            || !TryGetDragInfo<DragInfo>(e, out var dragInfo)
            || !ReferenceEquals(dragInfo.DragOperation.AttachedControl, AttachedControl);
    }

    /// <summary>
    /// Rejects the drag being validated, with <paramref name="message"/> as <see cref="ErrorMessage"/>. Returns false, so validation can
    /// <c>return Reject("...")</c>.
    /// </summary>
    protected bool Reject(string? message)
    {
        ErrorMessage = message;
        return false;
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
    /// Creates the value a Copy drop transfers in place of <paramref name="payload"/>, and passes to <see cref="PayloadCommand"/>.
    /// Returns the payload itself by default, so a copy shares the source's reference. Not called for Move or Link drops.
    /// </summary>
    protected virtual object CreateCopy(object payload) => payload;

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

    /// <summary>
    /// Checks if the drag carries an in-process payload for any of <see cref="InteractionIds"/>. False means a platform drop.
    /// </summary>
    protected bool HasInProcessPayload(DragEventArgs e) =>
        InteractionIds.Any(x => !DragDropIds.IsPlatform(x) && e.DataTransfer.Contains(DragDropIds.PayloadFormat(x)));

    /// <summary>
    /// Tries to get the first in-process payload of the drag (see <see cref="TryGetPayloads"/>). Succeeds when it is a <typeparamref name="TPayload"/>.
    /// </summary>
    protected bool TryGetPayload<TPayload>(DragEventArgs e, [MaybeNullWhen(false)] out TPayload payload)
        where TPayload : class
    {
        if (TryGetPayloads<object>(e, out var payloads) && payloads[0] is TPayload first)
        {
            payload = first;
            return true;
        }

        payload = null;
        return false;
    }

    /// <summary>
    /// Tries to get every in-process payload of the drag, in order, from the first of <see cref="InteractionIds"/> that carries any.
    /// Succeeds only when each is a <typeparamref name="TPayload"/>. Overriding this transforms the payloads a drop transfers.
    /// </summary>
    protected virtual bool TryGetPayloads<TPayload>(DragEventArgs e, [MaybeNullWhen(false)] out IReadOnlyList<TPayload> payloads)
        where TPayload : class
    {
        var values = InteractionIds
            .Where(x => !DragDropIds.IsPlatform(x))
            .Select(x => e.DataTransfer.TryGetValues(DragDropIds.PayloadFormat(x)))
            .FirstOrDefault(x => x is not null && x.Any(v => v is not null));

        if (values is not null && values.All(x => x is TPayload))
        {
            payloads = [.. values.Cast<TPayload>()];
            return true;
        }

        payloads = null;
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
