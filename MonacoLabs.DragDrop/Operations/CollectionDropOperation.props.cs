using Avalonia;
using Avalonia.Data;

namespace MonacoLabs.DragDrop;
public partial class CollectionDropOperation
{
    public static readonly StyledProperty<DropInsertionAdorner?> ItemDropAdornerProperty =
        AvaloniaProperty.Register<CollectionDropOperation, DropInsertionAdorner?>(nameof(ItemDropAdorner), defaultBindingMode: BindingMode.OneTime);

    /// <summary>
    /// Adorner that marks where the payload will be inserted relative to the hovered item
    /// </summary>
    public DropInsertionAdorner? ItemDropAdorner
    {
        get => GetValue(ItemDropAdornerProperty);
        set => SetValue(ItemDropAdornerProperty, value);
    }

    public static readonly StyledProperty<double> AutoScrollMarginProperty =
        AvaloniaProperty.Register<CollectionDropOperation, double>(nameof(AutoScrollMargin), defaultValue: 28);

    /// <summary>
    /// Distance from the edge of the control's scroll viewer within which a drag scrolls it. Zero disables auto-scrolling.
    /// </summary>
    public double AutoScrollMargin
    {
        get => GetValue(AutoScrollMarginProperty);
        set => SetValue(AutoScrollMarginProperty, value);
    }
}
