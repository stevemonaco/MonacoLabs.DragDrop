using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace MonacoLabs.DragDrop.Abstractions;
public abstract partial class DragOperationBase
{
    public static readonly StyledProperty<Control?> AttachedControlProperty =
        AvaloniaProperty.Register<DragOperationBase, Control?>(nameof(AttachedControl));

    /// <summary>
    /// Control that the DragOperation is attached to
    /// </summary>
    public Control? AttachedControl
    {
        get => GetValue(AttachedControlProperty);
        set => SetValue(AttachedControlProperty, value);
    }

    public static readonly StyledProperty<object?> PayloadProperty =
        AvaloniaProperty.Register<DragOperationBase, object?>(nameof(Payload));

    /// <summary>
    /// Object that is transferred via DragDrop. If null, this falls back to AttachedControl.DataContext.
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
