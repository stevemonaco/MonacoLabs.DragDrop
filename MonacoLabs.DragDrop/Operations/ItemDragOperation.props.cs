using Avalonia;

namespace MonacoLabs.DragDrop;

public partial class ItemDragOperation
{
    public static readonly StyledProperty<bool> SwapOnDropProperty =
        AvaloniaProperty.Register<ItemDragOperation, bool>(nameof(SwapOnDrop));

    /// <summary>
    /// When a Move replaces a value at the drop target, the replaced value moves into <see cref="Abstractions.DragOperationBase.Payload"/> instead of clearing it.
    /// The replaced value isn't validated against this source.
    /// </summary>
    public bool SwapOnDrop
    {
        get => GetValue(SwapOnDropProperty);
        set => SetValue(SwapOnDropProperty, value);
    }
}
