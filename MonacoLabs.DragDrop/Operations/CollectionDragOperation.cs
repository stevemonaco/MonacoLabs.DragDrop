using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using MonacoLabs.DragDrop.Abstractions;
using System.Collections;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Handles drag operations from flat collection controls
/// </summary>
public class CollectionDragOperation : DragOperationBase
{
    public static readonly StyledProperty<IList?> SourceCollectionProperty =
        AvaloniaProperty.Register<CollectionDragOperation, IList?>(nameof(SourceCollection));

    /// <summary>
    /// Specifies the collection the Payload is inside of so it can be removed when
    /// the Drop transfer occurs
    /// </summary>
    public IList? SourceCollection
    {
        get => GetValue(SourceCollectionProperty);
        set => SetValue(SourceCollectionProperty, value);
    }

    public static readonly StyledProperty<DraggedItems> DraggedItemsProperty =
        AvaloniaProperty.Register<CollectionDragOperation, DraggedItems>(nameof(DraggedItems), defaultValue: DraggedItems.SelectedItems);

    /// <summary>
    /// Which items a drag carries: the selection when the pressed item is one of several selected (the default), or only the pressed item.
    /// </summary>
    public DraggedItems DraggedItems
    {
        get => GetValue(DraggedItemsProperty);
        set => SetValue(DraggedItemsProperty, value);
    }

    public CollectionDragOperation()
    {
        Routing = RoutingStrategies.Tunnel;
    }

    /// <summary>
    /// On an <see cref="ItemsControl"/>, tracks only presses inside an item container; otherwise behaves as the base.
    /// </summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (AttachedControl is ItemsControl && LocatePayloadContainerIndex(LocatePayloadContainer(e)) is null or < 0)
            return;

        base.OnPointerPressed(e);
    }

    /// <summary>
    /// Uses the pressed item container's item, or every selected item when the press is on one of several (see <see cref="DraggedItems"/>),
    /// so the payloads always match <see cref="CollectionDragInfo.Items"/>. Falls back to <see cref="DragOperationBase.Payload"/> when the press
    /// isn't on an item container.
    /// </summary>
    protected override IReadOnlyList<object> ResolvePayloads(PointerPressedEventArgs pressedEventArgs) =>
        [.. ResolveDragItems(pressedEventArgs).Select(x => x.Item)];

    /// <summary>
    /// The pressed item container, or the attached control when the press isn't on one.
    /// </summary>
    protected override Control? ResolveDragVisual(PointerPressedEventArgs pressedEventArgs) =>
        LocatePayloadContainer(pressedEventArgs) is { } container ? DragVisualOf(container) : base.ResolveDragVisual(pressedEventArgs);

    private protected virtual Control DragVisualOf(Control container) => container;

    private protected override IEnumerable<Control> ResolveDraggingControls(DragInfo dragInfo, Control? dragVisual)
    {
        var containers = (dragInfo as CollectionDragInfo)?.Items.Select(x => x.Container).OfType<Control>().Select(DragVisualOf) ?? [];
        return base.ResolveDraggingControls(dragInfo, dragVisual).Concat(containers).Distinct();
    }

    /// <summary>
    /// Creates a <see cref="CollectionDragInfo"/> with the pressed container and, per payload, its collection, index and container.
    /// </summary>
    protected override DragInfo CreateMetadata(PointerEventArgs e, Point origin)
    {
        return new CollectionDragInfo()
        {
            DragOperation = this,
            DragOrigin = origin,
            DragIds = InteractionIds.ToList(),
            PointerId = e.Pointer.Id,
            Items = ResolveDragItems(e),
            PressedContainer = LocatePayloadContainer(e)
        };
    }

    private IReadOnlyList<CollectionDragItem> ResolveDragItems(RoutedEventArgs e)
    {
        if (LocatePayloadContainer(e) is not { } pressed || ItemsControl.ItemsControlFromItemContainer(pressed) is null)
            return ResolveDefaultPayload() is { } payload ? [new CollectionDragItem(payload, ResolveSourceCollection(null), null, null)] : [];

        if (DraggedItems == DraggedItems.SelectedItems && ResolveSelectedItems(pressed) is { Count: > 0 } selected)
            return selected;

        return ToDragItem(pressed) is { } item ? [item] : [];
    }

    // Null when the drag carries only the pressed item
    private protected virtual IReadOnlyList<CollectionDragItem>? ResolveSelectedItems(Control pressedContainer)
    {
        if (AttachedControl is not ListBox { Selection: { Count: > 1 } selection } list
            || LocatePayloadContainerIndex(pressedContainer) is not int pressedIndex
            || !selection.IsSelected(pressedIndex))
            return null;

        // The selection model knows the index and item of every selected item, realized or not
        return [.. selection.SelectedIndexes
            .Zip(selection.SelectedItems)
            .Where(x => x.Second is not null)
            .OrderBy(x => x.First)
            .Select(x =>
            {
                var container = list.ContainerFromIndex(x.First);
                var collection = ResolveSourceCollection(container);
                return new CollectionDragItem(x.Second!, collection, MapSourceIndex(list, collection, x.First, x.Second!), container);
            })];
    }

    private protected CollectionDragItem? ToDragItem(Control container)
    {
        if (ItemsControl.ItemsControlFromItemContainer(container) is not { } owner || owner.ItemFromContainer(container) is not { } item)
            return null;

        var collection = ResolveSourceCollection(container);
        return new CollectionDragItem(item, collection, MapSourceIndex(owner, collection, LocatePayloadContainerIndex(container), item), container);
    }

    private int? MapSourceIndex(ItemsControl owner, IList? collection, int? displayIndex, object item) =>
        collection is null || displayIndex is not int index || index < 0 || ReferenceEquals(owner.ItemsSource, collection)
            ? displayIndex
            : ResolveSourceIndex(collection, index, item);

    /// <summary>
    /// Maps the position of a dragged <paramref name="item"/> in the attached control's items to its index in <paramref name="collection"/>, when
    /// the control shows a filtered or sorted view of the collection rather than the collection itself. By default, the index of the item shown at
    /// <paramref name="displayIndex"/>, equal items matched by occurrence. Null leaves the item to be found by equality when it is removed.
    /// </summary>
    protected virtual int? ResolveSourceIndex(IList collection, int displayIndex, object item) =>
        AttachedControl is ItemsControl items ? DisplayIndexMap.ToCollectionIndex(items.ItemsView, collection, displayIndex) : displayIndex;

    /// <summary>
    /// Resolves the collection a dragged item is removed from after a Move, given its item container, or null when it isn't realized.
    /// </summary>
    protected virtual IList? ResolveSourceCollection(Control? container) => SourceCollection;

    /// <summary>
    /// Finds the item container under the event's source: its topmost logical ancestor below the attached control, or null when the
    /// attached control isn't an <see cref="ItemsControl"/>.
    /// </summary>
    protected virtual Control? LocatePayloadContainer(RoutedEventArgs triggerEvent)
    {
        if (AttachedControl is ItemsControl items && triggerEvent.Source is Control sourceItem)
        {
            var ancestors = sourceItem.GetSelfAndLogicalAncestors();
            return ancestors.TakeWhile(x => x != AttachedControl).OfType<Control>().LastOrDefault();
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the index of <paramref name="container"/> in the attached control, or null when there is none. Negative when it isn't an item container.
    /// </summary>
    protected virtual int? LocatePayloadContainerIndex(Control? container)
    {
        if (AttachedControl is ItemsControl items && container is not null)
        {
            return items.IndexFromContainer(container);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// On a Move, removes every original from its collection, checking each recorded index before trusting it.
    /// </summary>
    public override void DropCompleted(DragDropEffects effect, DragInfo dragInfo, DropInfo dropInfo)
    {
        base.DropCompleted(effect, dragInfo, dropInfo);

        if (effect != DragDropEffects.Move || dragInfo is not CollectionDragInfo collectionDragInfo)
            return;

        var collectionDropInfo = dropInfo as CollectionDropInfo;
        CollectionRemoval.RemoveAfterDrop(collectionDragInfo.Items, collectionDropInfo?.TargetCollection, collectionDropInfo?.TargetIndex,
            collectionDropInfo?.TargetCount ?? 0);
    }

    /// <summary>
    /// Removes every original from its collection.
    /// </summary>
    protected override void OnDroppedOutside(DragInfo dragInfo)
    {
        if (dragInfo is CollectionDragInfo collectionDragInfo)
            CollectionRemoval.RemoveDroppedOutside(collectionDragInfo.Items);
    }
}
