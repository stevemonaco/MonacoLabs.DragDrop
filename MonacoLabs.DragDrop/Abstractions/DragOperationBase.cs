using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using System.Runtime.CompilerServices;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Abstractions;
public abstract partial class DragOperationBase : AvaloniaObject, IDragOperation
{
    private const string LogArea = "DragDrop";

    private RoutingStrategies _routing = RoutingStrategies.Bubble;
    private bool _handledEventsToo;
    private Control? _trackedControl;
    private Point? _dragOrigin;
    private PointerPressedEventArgs? _pressedEventArgs; // DoDragDropAsync requires the press event, not the move event that crossed the threshold

    public IList<string> InteractionIds { get; set; } = [DragDropIds.DefaultOperation];

    /// <summary>
    /// Routing strategy of the pointer handlers. Can only be set while the operation is detached.
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
    /// Whether the pointer handlers also receive events that are already handled. Can only be set while the operation is detached.
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

    public void Attach(Control control)
    {
        ThrowIf.InvalidOperation(AttachedControl is not null, $"{GetType().Name} is already attached to {AttachedControl}; an operation can attach to only one control");

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
        control.AddHandler(InputElement.PointerPressedEvent, HandlePointerPressed, Routing, HandledEventsToo);
        control.AddHandler(InputElement.PointerMovedEvent, HandlePointerMoved, Routing, HandledEventsToo);
        control.AddHandler(InputElement.PointerReleasedEvent, HandlePointerReleased, Routing, HandledEventsToo);
        // Direct, but raised on every element the capture leaves, so this fires when capture leaves the attached control's subtree
        control.AddHandler(InputElement.PointerCaptureLostEvent, HandlePointerCaptureLost, RoutingStrategies.Direct);
    }

    protected virtual void UnsubscribeDragEvents(Control control)
    {
        control.RemoveHandler(InputElement.PointerPressedEvent, HandlePointerPressed);
        control.RemoveHandler(InputElement.PointerMovedEvent, HandlePointerMoved);
        control.RemoveHandler(InputElement.PointerReleasedEvent, HandlePointerReleased);
        control.RemoveHandler(InputElement.PointerCaptureLostEvent, HandlePointerCaptureLost);
    }

    private void HandlePointerPressed(object? sender, PointerPressedEventArgs e) => OnPointerPressed(e);

    private void HandlePointerReleased(object? sender, PointerReleasedEventArgs e) => OnPointerReleased(e);

    private void HandlePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => OnPointerCaptureLost(e);

    // An exception escaping this async void would terminate the app, and on Win32 the platform has already reduced it to E_FAIL
    private async void HandlePointerMoved(object? sender, PointerEventArgs e)
    {
        try
        {
            await OnPointerMoved(e);
        }
        catch (Exception ex)
        {
            StopTrackingPress();
            Logger.TryGet(LogEventLevel.Error, LogArea)?.Log(this, "Drag failed: {Exception}", ex);
        }
    }

    /// <summary>
    /// Called when a pointer is pressed on the attached control. Tracks the press as a possible drag unless it is filtered out.
    /// </summary>
    protected virtual void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!CanDrag)
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

    /// <summary>
    /// Whether a press on <paramref name="element"/>, or inside it, is left to the element by default instead of being tracked as a drag.
    /// True for scroll bars, buttons (including a tree item's expander), text boxes and thumbs.
    /// </summary>
    /// <remarks>Called for the pressed element and each ancestor up to the attached control that has no <see cref="DragDrop.IgnoresPressProperty"/>
    /// value. Prefer that property for per-element or styled choices; override for rules that need code.</remarks>
    protected virtual bool IsIgnoredPressSource(Visual element) => element is ScrollBar or Button or TextBox or Thumb;

    private bool IsInsideIgnoredControl(Control source)
    {
        foreach (var element in SelfAndAncestorsWithinAttachedControl(source))
        {
            if (DragDrop.GetIgnoresPress(element) is { } ignores)
                return ignores;

            if (IsIgnoredPressSource(element))
                return true;
        }

        return false;
    }

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

    /// <summary>
    /// Called when a pointer is released on the attached control. Stops tracking the press.
    /// </summary>
    protected virtual void OnPointerReleased(PointerReleasedEventArgs e) => StopTrackingPress();

    /// <summary>
    /// Called when the pointer capture leaves the attached control and its descendants. Stops tracking the press, whose release may never arrive.
    /// </summary>
    protected virtual void OnPointerCaptureLost(PointerCaptureLostEventArgs e) => StopTrackingPress();

    private void StopTrackingPress()
    {
        _trackedControl = null;
        _dragOrigin = null;
        _pressedEventArgs = null;
    }

    /// <summary>
    /// Called when a pointer moves over the attached control. Starts a drag once a tracked press has moved past <see cref="DragThreshold"/>,
    /// and completes when that drag ends. A move without the left button pressed stops tracking the press.
    /// </summary>
    protected virtual Task OnPointerMoved(PointerEventArgs e)
    {
        if (AttachedControl is null || _trackedControl is null || _dragOrigin is null)
            return Task.CompletedTask;

        var local = e.GetCurrentPoint(AttachedControl);
        if (!local.Properties.IsLeftButtonPressed)
        {
            StopTrackingPress();
            return Task.CompletedTask;
        }

        var delta = local.Position - _dragOrigin.Value;
        if (delta.X * delta.X + delta.Y * delta.Y < DragThreshold * DragThreshold)
            return Task.CompletedTask;

        return StartDrag(e);
    }

    protected virtual async Task StartDrag(PointerEventArgs e)
    {
        var pressedEventArgs = _pressedEventArgs;
        if (_dragOrigin is not { } origin || pressedEventArgs is null)
            return;

        var payload = ResolvePayload(pressedEventArgs);
        if (payload is null)
            return;

        var metadata = CreateMetadata(pressedEventArgs, origin);
        metadata.Payload = payload;

        StopTrackingPress();

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

    /// <summary>
    /// Creates the metadata for a drag started by the press <paramref name="e"/> at <paramref name="origin"/>, relative to the attached control.
    /// </summary>
    protected virtual DragInfo CreateMetadata(PointerEventArgs e, Point origin)
    {
        return new DragInfo()
        {
            DragOperation = this,
            DragOrigin = origin,
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
