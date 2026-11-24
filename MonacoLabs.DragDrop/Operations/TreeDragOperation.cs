using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System.Collections;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Drags nodes out of a <see cref="TreeView"/>. Unless <see cref="CollectionDragOperation.PayloadCollection"/> is set,
/// the collection to remove from is the dragged node's parent <see cref="ItemsControl.ItemsSource"/>.
/// </summary>
public class TreeDragOperation : CollectionDragOperation
{
    protected override DragInfo CreateMetadata(PointerEventArgs e)
    {
        var container = LocatePayloadContainer(e);

        var payloadCollection = PayloadCollection;
        if (payloadCollection is null && container is not null)
            payloadCollection = ItemsControl.ItemsControlFromItemContainer(container)?.ItemsSource as IList;

        return new CollectionDragInfo()
        {
            DragOperation = this,
            DragOrigin = _dragOrigin!.Value,
            DragIds = InteractionIds.ToList(),
            PointerId = e.Pointer.Id,
            PayloadCollection = payloadCollection,
            PayloadContainer = container,
            PayloadContainerIndex = LocatePayloadContainerIndex(container)
        };
    }

    // A TreeViewItem's bounds include its expanded children, so only the header row is snapshotted
    protected override Control? ResolveDragVisual(PointerPressedEventArgs pressedEventArgs)
    {
        var container = base.ResolveDragVisual(pressedEventArgs);
        if (container is not TreeViewItem item)
            return container;

        return FindHeader(item);
    }

    protected override Control? LocatePayloadContainer(RoutedEventArgs triggerEvent)
    {
        if (AttachedControl is TreeView && triggerEvent.Source is Control sourceItem)
            return sourceItem.GetSelfAndLogicalAncestors().OfType<TreeViewItem>().FirstOrDefault();

        return null;
    }

    protected override int? LocatePayloadContainerIndex(Control? container)
    {
        if (AttachedControl is TreeView && container is TreeViewItem item)
            return ItemsControl.ItemsControlFromItemContainer(item)?.IndexFromContainer(item);

        return null;
    }

    internal static Control FindHeader(TreeViewItem item) =>
        item.GetVisualDescendants().OfType<Control>().FirstOrDefault(x => x.Name == "PART_Header" && x.TemplatedParent == item) ?? item;
}
