using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System.Collections;

namespace MonacoLabs.DragDrop;
public class TreeDragOperation : CollectionDragOperation
{
    protected override DragInfo CreateMetadata(PointerEventArgs e)
    {
        var container = LocatePayloadContainer(e);
        var index = LocatePayloadContainerIndex(container);

        var payloadCollection = PayloadCollection;
        if (payloadCollection is null)
        {
            if (container is TreeViewItem { Parent: TreeViewItem { ItemsSource: IList itemList } })
                payloadCollection = itemList;
            else if (AttachedControl is TreeView { ItemsSource: IList treeList })
                payloadCollection = treeList;
        }

        return new CollectionDragInfo()
        {
            DragOperation = this,
            DragOrigin = _dragOrigin!.Value,
            DragIds = InteractionIds.ToList(),
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

        return item.GetVisualDescendants().OfType<Control>().FirstOrDefault(x => x.Name == "PART_Header" && x.TemplatedParent == item) ?? item;
    }

    protected override Control? LocatePayloadContainer(RoutedEventArgs triggerEvent)
    {
        if (AttachedControl is TreeView tree && triggerEvent.Source is Control sourceItem)
        {
            var ancestors = sourceItem.GetSelfAndLogicalAncestors();
            return ancestors.OfType<TreeViewItem>().FirstOrDefault();
        }

        return null;
    }

    protected override int? LocatePayloadContainerIndex(Control? container)
    {
        if (AttachedControl is TreeView items && container is TreeViewItem item)
        {
            if (item.Parent is TreeViewItem parent)
                return parent.IndexFromContainer(container);
            
            return items.IndexFromContainer(container);
        }
        
        return null;
    }
}
