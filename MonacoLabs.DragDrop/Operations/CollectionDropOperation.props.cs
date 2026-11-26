using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using MonacoLabs.DragDrop.Abstractions;
using System.Collections;

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

    public static readonly StyledProperty<IList?> TargetCollectionProperty =
        AvaloniaProperty.Register<CollectionDropOperation, IList?>(nameof(TargetCollection));

    /// <summary>
    /// The collection an accepted in-process drop inserts the payloads into. Without it, drops only run <see cref="DropOperationBase.PayloadCommand"/>,
    /// with locations in the control's <see cref="ItemsControl.ItemsSource"/>.
    /// </summary>
    public IList? TargetCollection
    {
        get => GetValue(TargetCollectionProperty);
        set => SetValue(TargetCollectionProperty, value);
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

    public static readonly StyledProperty<ItemOrder> ItemOrderProperty =
        AvaloniaProperty.Register<CollectionDropOperation, ItemOrder>(nameof(ItemOrder), defaultValue: ItemOrder.Positional);

    /// <summary>
    /// Whether drops insert where the user points (<c>Positional</c>) or append to a collection that sorts itself (<c>Sorted</c>),
    /// with no insertion line and no reordering within the control.
    /// </summary>
    public ItemOrder ItemOrder
    {
        get => GetValue(ItemOrderProperty);
        set => SetValue(ItemOrderProperty, value);
    }
}
