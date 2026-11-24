using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using System.Collections;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Drops into a <see cref="TreeView"/>. Payloads are inserted before or after the hovered node, or into it when the pointer is over
/// the middle of a node whose container has an <see cref="ItemsControl.ItemsSource"/>. The target collection is read from the tree,
/// so <see cref="Abstractions.DropOperationBase.PayloadTarget"/> isn't used. Use <see cref="CollectionDropOperation.DropLocation"/> in
/// <see cref="Abstractions.DropOperationBase.CanDrop"/> overrides to validate the destination.
/// </summary>
public partial class TreeDropOperation : CollectionDropOperation
{
    private IDisposable? _expandTimer;
    private TreeViewItem? _expandCandidate;

    protected override bool TransfersPayload => true;

    protected override void UpdateDrag(DragEventArgs e)
    {
        base.UpdateDrag(e);
        UpdateHoverExpand();
    }

    protected override void EndDrag()
    {
        base.EndDrag();
        CancelHoverExpand();
    }

    protected override Control? LocateTargetContainer(RoutedEventArgs triggerEvent)
    {
        if (triggerEvent.Source is not Control source)
            return null;

        return source.GetSelfAndLogicalAncestors()
            .TakeWhile(x => x != AttachedControl)
            .OfType<TreeViewItem>()
            .FirstOrDefault();
    }

    protected override int? LocateTargetContainerIndex(Control? container) =>
        container is null ? null : ItemsControl.ItemsControlFromItemContainer(container)?.IndexFromContainer(container);

    // A TreeViewItem's bounds include its expanded children, so zones and indicators use the header row
    protected override Control GetInsertionTarget(Control container) =>
        container is TreeViewItem item ? TreeDragOperation.FindHeader(item) : container;

    protected override DropTargetOffset GetDropOffset(Control container, Point position, Size size)
    {
        if (container is not TreeViewItem { ItemsSource: IList })
            return base.GetDropOffset(container, position, size);

        var edge = size.Height / 4;
        if (position.Y < edge)
            return DropTargetOffset.BeforeTarget;

        if (position.Y > size.Height - edge)
            return DropTargetOffset.AfterTarget;

        return DropTargetOffset.OnTarget;
    }

    protected override CollectionDropLocation? ResolveDropLocation()
    {
        if (HoveredContainer is not TreeViewItem item)
            return AttachedControl is TreeView { ItemsSource: IList roots } tree
                ? new CollectionDropLocation(roots, null, null, tree)
                : null;

        if (item.ItemsSource is IList children)
        {
            if (HoveredOffset == DropTargetOffset.OnTarget)
                return new CollectionDropLocation(children, null, item.DataContext, item);

            // Below an expanded node's header is visually above its first child
            if (HoveredOffset == DropTargetOffset.AfterTarget && item.IsExpanded && children.Count > 0)
                return new CollectionDropLocation(children, 0, item.DataContext, item);
        }

        if (ItemsControl.ItemsControlFromItemContainer(item) is not { ItemsSource: IList siblings } owner)
            return null;

        var index = owner.IndexFromContainer(item);
        var parent = owner is TreeViewItem ? owner.DataContext : null;
        return new CollectionDropLocation(siblings, HoveredOffset == DropTargetOffset.AfterTarget ? index + 1 : index, parent, owner);
    }

    /// <summary>
    /// Rejects drops into the dragged node itself or any of its descendants.
    /// </summary>
    protected override bool CanDropAtLocation(DragEventArgs e)
    {
        if (DropLocation is null)
            return false;

        if (!TryGetPayload<object>(e, out var payload) || !IsWithinSubtreeOf(DropLocation.ParentContainer, payload))
            return true;

        return Reject(SelfDropMessage);
    }

    private static bool IsWithinSubtreeOf(Control? container, object node) =>
        container?.GetSelfAndLogicalAncestors().OfType<TreeViewItem>().Any(x => ReferenceEquals(x.DataContext, node)) ?? false;

    private void UpdateHoverExpand()
    {
        var candidate = HoveredOffset == DropTargetOffset.OnTarget
            && HoveredContainer is TreeViewItem { IsExpanded: false, ItemsSource: IList { Count: > 0 } } item
            ? item
            : null;

        if (ReferenceEquals(candidate, _expandCandidate))
            return;

        CancelHoverExpand();
        if (candidate is null || HoverExpandDelay <= TimeSpan.Zero)
            return;

        _expandCandidate = candidate;
        _expandTimer = DispatcherTimer.RunOnce(() =>
        {
            candidate.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
            _expandCandidate = null;
        }, HoverExpandDelay);
    }

    private void CancelHoverExpand()
    {
        _expandTimer?.Dispose();
        _expandTimer = null;
        _expandCandidate = null;
    }
}
