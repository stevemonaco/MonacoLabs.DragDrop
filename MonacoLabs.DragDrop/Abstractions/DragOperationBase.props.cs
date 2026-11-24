using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace MonacoLabs.DragDrop.Abstractions;
public abstract partial class DragOperationBase
{
    private Control? _attachedControl;

    public static readonly DirectProperty<DragOperationBase, Control?> AttachedControlProperty =
        AvaloniaProperty.RegisterDirect<DragOperationBase, Control?>(nameof(AttachedControl), o => o.AttachedControl);

    /// <summary>
    /// Control that the DragOperation is attached to. Changes only through <see cref="Attach"/> and <see cref="Detach"/>.
    /// </summary>
    public Control? AttachedControl
    {
        get => _attachedControl;
        private set => SetAndRaise(AttachedControlProperty, ref _attachedControl, value);
    }

    public static readonly StyledProperty<object?> PayloadProperty =
        AvaloniaProperty.Register<DragOperationBase, object?>(nameof(Payload));

    /// <summary>
    /// Object that is transferred via DragDrop. If not set, this falls back to AttachedControl.DataContext.
    /// Once set or bound, a null value means there is nothing to drag.
    /// </summary>
    public object? Payload
    {
        get => GetValue(PayloadProperty);
        set => SetValue(PayloadProperty, value);
    }

    public static readonly StyledProperty<int> DragThresholdProperty =
        AvaloniaProperty.Register<DragOperationBase, int>(nameof(DragThreshold), defaultValue: 6);

    /// <summary>
    /// Threshold in pixels that the user must move a pressed pointer to initiate a drag operation
    /// </summary>
    public int DragThreshold
    {
        get => GetValue(DragThresholdProperty);
        set => SetValue(DragThresholdProperty, value);
    }

    public static readonly StyledProperty<DragDropEffects> AllowedEffectsProperty =
        AvaloniaProperty.Register<DragOperationBase, DragDropEffects>(nameof(AllowedEffects), defaultValue: DragDropEffects.Move);

    /// <summary>
    /// Effects the drag source permits. Use <see cref="DragDropEffects.Copy"/> when the source keeps its item after a drop.
    /// </summary>
    public DragDropEffects AllowedEffects
    {
        get => GetValue(AllowedEffectsProperty);
        set => SetValue(AllowedEffectsProperty, value);
    }

    public static readonly StyledProperty<bool> CanDragProperty =
        AvaloniaProperty.Register<DragOperationBase, bool>(nameof(CanDrag), defaultValue: true);

    /// <summary>
    /// When false, presses on the attached control don't start drags.
    /// </summary>
    public bool CanDrag
    {
        get => GetValue(CanDragProperty);
        set => SetValue(CanDragProperty, value);
    }

    public static readonly StyledProperty<bool> RequiresDragHandleProperty =
        AvaloniaProperty.Register<DragOperationBase, bool>(nameof(RequiresDragHandle));

    /// <summary>
    /// Only starts drags from presses inside an element marked with <see cref="DragDrop.IsDragHandleProperty"/>.
    /// </summary>
    public bool RequiresDragHandle
    {
        get => GetValue(RequiresDragHandleProperty);
        set => SetValue(RequiresDragHandleProperty, value);
    }

    public static readonly StyledProperty<bool> RemoveOnDropOutsideProperty =
        AvaloniaProperty.Register<DragOperationBase, bool>(nameof(RemoveOnDropOutside));

    /// <summary>
    /// Removes the payload from its source when it is dropped on a part of the source's window that isn't a drop target.
    /// Drops outside the window and cancelled drags never remove.
    /// </summary>
    public bool RemoveOnDropOutside
    {
        get => GetValue(RemoveOnDropOutsideProperty);
        set => SetValue(RemoveOnDropOutsideProperty, value);
    }

    public static readonly StyledProperty<bool> ShowDragPreviewProperty =
        AvaloniaProperty.Register<DragOperationBase, bool>(nameof(ShowDragPreview), defaultValue: true);

    /// <summary>
    /// Shows a <see cref="DragPreviewAdorner"/> snapshot of the dragged control under the pointer while dragging within its window.
    /// </summary>
    public bool ShowDragPreview
    {
        get => GetValue(ShowDragPreviewProperty);
        set => SetValue(ShowDragPreviewProperty, value);
    }
}
