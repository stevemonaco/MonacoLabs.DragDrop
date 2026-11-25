using Avalonia.Controls;
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
    protected override IList? ResolvePayloadCollection(Control? container) =>
        PayloadCollection ?? (container is null ? null : ItemsControl.ItemsControlFromItemContainer(container)?.ItemsSource as IList);

    // A TreeViewItem's bounds include its expanded children, so only the header row is snapshotted and marked dragging
    private protected override Control DragVisualOf(Control container) => container is TreeViewItem item ? FindHeader(item) : container;

    // Walks the shown nodes top to bottom and stops at a selected one, whose selected descendants move with it
    private protected override IReadOnlyList<CollectionDragItem>? ResolveSelectedItems(Control pressedContainer)
    {
        if (AttachedControl is not TreeView { SelectedItems.Count: > 1 } tree || pressedContainer is not TreeViewItem { IsSelected: true })
            return null;

        var items = new List<CollectionDragItem>();
        Collect(tree);
        return items;

        void Collect(ItemsControl parent)
        {
            foreach (var node in parent.GetRealizedContainers().OfType<TreeViewItem>().OrderBy(parent.IndexFromContainer))
            {
                if (node.IsSelected)
                {
                    if (ToDragItem(node) is { } item)
                        items.Add(item);
                }
                else if (node.IsExpanded)
                {
                    Collect(node);
                }
            }
        }
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
