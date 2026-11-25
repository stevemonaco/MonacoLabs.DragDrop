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
    public static readonly StyledProperty<IList?> PayloadCollectionProperty =
        AvaloniaProperty.Register<CollectionDragOperation, IList?>(nameof(PayloadCollection));

    /// <summary>
    /// Specifies the collection the Payload is inside of so it can be removed when
    /// the Drop transfer occurs
    /// </summary>
    public IList? PayloadCollection
    {
        get => GetValue(PayloadCollectionProperty);
        set => SetValue(PayloadCollectionProperty, value);
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
        HandledEventsToo = true;
    }

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

    protected override Control? ResolveDragVisual(PointerPressedEventArgs pressedEventArgs) =>
        LocatePayloadContainer(pressedEventArgs) is { } container ? DragVisualOf(container) : base.ResolveDragVisual(pressedEventArgs);

    private protected virtual Control DragVisualOf(Control container) => container;

    private protected override IEnumerable<Control> ResolveDraggingControls(DragInfo dragInfo, Control? dragVisual)
    {
        var containers = (dragInfo as CollectionDragInfo)?.Items.Select(x => x.Container).OfType<Control>().Select(DragVisualOf) ?? [];
        return base.ResolveDraggingControls(dragInfo, dragVisual).Concat(containers).Distinct();
    }

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
            return ResolveDefaultPayload() is { } payload ? [new CollectionDragItem(payload, ResolvePayloadCollection(null), null, null)] : [];

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
                return new CollectionDragItem(x.Second!, ResolvePayloadCollection(container), x.First, container);
            })];
    }

    private protected CollectionDragItem? ToDragItem(Control container) =>
        ItemsControl.ItemsControlFromItemContainer(container)?.ItemFromContainer(container) is { } item
            ? new CollectionDragItem(item, ResolvePayloadCollection(container), LocatePayloadContainerIndex(container), container)
            : null;

    /// <summary>
    /// Resolves the collection a dragged item is removed from after a Move, given its item container, or null when it isn't realized.
    /// </summary>
    protected virtual IList? ResolvePayloadCollection(Control? container) => PayloadCollection;

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

    public override void DropCompleted(DragDropEffects effect, DragInfo dragInfo, DropInfo dropInfo)
    {
        base.DropCompleted(effect, dragInfo, dropInfo);

        if (effect != DragDropEffects.Move || dragInfo is not CollectionDragInfo collectionDragInfo)
            return;

        var collectionDropInfo = dropInfo as CollectionDropInfo;
        CollectionRemoval.RemoveAfterDrop(collectionDragInfo.Items, collectionDropInfo?.TargetCollection, collectionDropInfo?.TargetIndex,
            collectionDropInfo?.TargetCount ?? 0);
    }

    protected override void OnDroppedOutside(DragInfo dragInfo)
    {
        if (dragInfo is CollectionDragInfo collectionDragInfo)
            CollectionRemoval.RemoveDroppedOutside(collectionDragInfo.Items);
    }
}
