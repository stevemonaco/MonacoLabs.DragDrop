using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Abstractions;
public abstract partial class DragOperationBase : AvaloniaObject, IDragOperation
{
    public IList<string> InteractionIds { get; set; } = [DragDropIds.DefaultOperation];
    public RoutingStrategies Routing { get; set; } = RoutingStrategies.Bubble;

    protected Control? _trackedControl; // Control that has been clicked and may be initiating a drag
    protected Point? _dragOrigin;
    protected PointerPressedEventArgs? _pressedEventArgs; // DoDragDropAsync requires the press event, not the move event that crossed the threshold
    protected bool _isDragPending; // True if Control has been clicked, mouse is being held, but drag operation has not started yet
    protected bool _handledEventsToo = false;

    public void Attach(Control control)
    {
        ThrowIf.NotNull(AttachedControl, "DragOperation is already attached to a control");

        AttachedControl = control;
        SubscribeDragEvents(control);
    }

    public void Detach(Control control)
    {
        if (AttachedControl is null)
            return;

        UnsubscribeDragEvents(control);
        AttachedControl = null;
    }

    protected virtual void SubscribeDragEvents(Control control)
    {
        control.AddHandler(InputElement.PointerPressedEvent, Control_PointerPressed, Routing, _handledEventsToo);
        control.AddHandler(InputElement.PointerMovedEvent, Control_PointerMoved, Routing, _handledEventsToo);
        control.AddHandler(InputElement.PointerReleasedEvent, Control_PointerReleased, Routing, _handledEventsToo);
    }

    protected virtual void UnsubscribeDragEvents(Control control)
    {
        control.RemoveHandler(InputElement.PointerPressedEvent, Control_PointerPressed);
        control.RemoveHandler(InputElement.PointerMovedEvent, Control_PointerMoved);
        control.RemoveHandler(InputElement.PointerReleasedEvent, Control_PointerReleased);
    }

    protected virtual void Control_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control c || _isDragPending || !CanDrag)
            return;

        if (e.Source is not Control control || IsInsideIgnoredControl(control))
            return;

        var point = e.GetCurrentPoint(AttachedControl);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        if (RequiresDragHandle && !IsInsideDragHandle(control))
            return;

        _trackedControl = control;
        _dragOrigin = point.Position;
        _pressedEventArgs = e;

        // Selecting items ignore handled presses, so an unselected item must receive this one to become selected
        if (FindSelectable(control) is not { IsSelected: false })
            e.Handled = true;
    }

    // Scrollbars and buttons (e.g. a tree item's expander) must keep receiving their presses
    private bool IsInsideIgnoredControl(Control source) =>
        SelfAndAncestorsWithinAttachedControl(source).Any(x => x is ScrollBar or Button);

    private bool IsInsideDragHandle(Control source) =>
        SelfAndAncestorsWithinAttachedControl(source).Any(DragDrop.GetIsDragHandle);

    private ISelectable? FindSelectable(Control source) =>
        SelfAndAncestorsWithinAttachedControl(source).OfType<ISelectable>().FirstOrDefault();

    private IEnumerable<Visual> SelfAndAncestorsWithinAttachedControl(Control source)
    {
        foreach (var visual in source.GetSelfAndVisualAncestors())
        {
            yield return visual;

            if (visual == AttachedControl)
                yield break;
        }
    }

    protected virtual void Control_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _trackedControl = null;
        _dragOrigin = null;
        _pressedEventArgs = null;
        _isDragPending = false;
    }

    protected virtual async void Control_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (AttachedControl is null)
            return;

        if (_trackedControl is null || _dragOrigin is null)
            return;

        var local = e.GetCurrentPoint(AttachedControl);
        var delta = local.Position - _dragOrigin.Value;
        if (delta.X * delta.X + delta.Y * delta.Y < DragThreshold * DragThreshold)
            return;

        if (!_isDragPending)
            await StartDrag(e);
    }

    protected virtual async Task StartDrag(PointerEventArgs e)
    {
        var pressedEventArgs = _pressedEventArgs;
        if (_dragOrigin is null || pressedEventArgs is null)
            return;

        var payload = ResolvePayload(pressedEventArgs);
        if (payload is null)
            return;

        var metadata = CreateMetadata(pressedEventArgs);
        metadata.Payload = payload;

        _trackedControl = null;
        _dragOrigin = null;
        _pressedEventArgs = null;
        _isDragPending = false;

        await DoDragDrop(pressedEventArgs, metadata, payload);
    }

    /// <summary>
    /// Resolves the object to transfer for a drag started by <paramref name="pressedEventArgs"/>.
    /// </summary>
    protected virtual object? ResolvePayload(PointerPressedEventArgs pressedEventArgs) =>
        IsSet(PayloadProperty) ? Payload : AttachedControl?.DataContext;

    /// <summary>
    /// Resolves the control that represents the dragged item. It receives the <c>:dragging</c> pseudo-class and is snapshotted for the drag preview.
    /// </summary>
    protected virtual Control? ResolveDragVisual(PointerPressedEventArgs pressedEventArgs) => AttachedControl;

    /// <summary>
    /// Resolves the text offered to other applications when <see cref="PlatformInteractionIds.Text"/> is in <see cref="InteractionIds"/>.
    /// </summary>
    protected virtual string? ResolvePlatformText(object payload) => payload as string ?? payload.ToString();

    /// <summary>
    /// Resolves the file or folder offered to other applications when <see cref="PlatformInteractionIds.Files"/> is in <see cref="InteractionIds"/>.
    /// </summary>
    protected virtual IStorageItem? ResolvePlatformFile(object payload) => payload as IStorageItem;

    protected virtual async Task DoDragDrop(PointerPressedEventArgs triggerEvent, DragInfo metadata, object payload)
    {
        var data = CreateDataTransfer(metadata, payload);
        var dragVisual = ResolveDragVisual(triggerEvent);
        var grabOffset = dragVisual is null ? default : triggerEvent.GetPosition(dragVisual);
        using var session = new DragSession(TopLevel.GetTopLevel(AttachedControl), dragVisual, grabOffset, ShowDragPreview, RemoveOnDropOutside);

        await AvaDragDrop.DoDragDropAsync(triggerEvent, data, AllowedEffects);

        if (RemoveOnDropOutside && session.DroppedOutsideTarget)
            OnDroppedOutside(metadata, payload);
    }

    internal DataTransfer CreateDataTransfer(DragInfo metadata, object payload)
    {
        var data = new DataTransfer();
        data.Add(CreateDataTransferItem(metadata, payload));
        return data;
    }

    protected virtual DataTransferItem CreateDataTransferItem(DragInfo metadata, object payload)
    {
        var item = new DataTransferItem();
        foreach (var id in InteractionIds)
        {
            if (id == PlatformInteractionIds.Text)
            {
                if (ResolvePlatformText(payload) is { } text)
                    item.SetText(text);
            }
            else if (id == PlatformInteractionIds.Files)
            {
                if (ResolvePlatformFile(payload) is { } file)
                    item.SetFile(file);
            }
            else
            {
                item.Set(DragDropIds.PayloadFormat(id), payload);
            }
        }

        item.Set(DragDropIds.DragMetadataFormat, metadata);
        return item;
    }

    protected virtual DragInfo CreateMetadata(PointerEventArgs e)
    {
        return new DragInfo()
        {
            DragOperation = this,
            DragOrigin = _dragOrigin!.Value,
            DragIds = InteractionIds.ToList(),
            PointerId = e.Pointer.Id
        };
    }

    /// <summary>
    /// Occurs when the Payload has been transferred
    /// Responsible for removing Payload from attached control, if necessary
    /// </summary>
    /// <param name="effect">Effect when the drop occurred</param>
    /// <param name="dragInfo">DragInfo associated with the dragdrop operation</param>
    /// <param name="dropInfo">DropInfo associated with the dragdrop operation</param>
    public virtual void DropCompleted(DragDropEffects effect, DragInfo dragInfo, DropInfo dropInfo)
    {
    }

    /// <summary>
    /// Occurs when <see cref="RemoveOnDropOutside"/> is set and the payload was dropped where no drop target lives.
    /// Responsible for removing the payload from its source.
    /// </summary>
    protected virtual void OnDroppedOutside(DragInfo dragInfo, object payload)
    {
    }
}
