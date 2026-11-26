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
/// so <see cref="CollectionDropOperation.TargetCollection"/> isn't used. Use <see cref="CollectionDropOperation.DropLocation"/> in
/// <see cref="Abstractions.DropOperationBase.CanDrop"/> overrides to validate the destination.
/// </summary>
public partial class TreeDropOperation : CollectionDropOperation
{
    private IDisposable? _expandTimer;
    private TreeViewItem? _expandCandidate;

    /// <summary>
    /// True: the target collection comes from the tree, not from <see cref="CollectionDropOperation.TargetCollection"/>.
    /// </summary>
    protected override bool TransfersPayload => true;

    private protected override void UpdateFeedback(DragEventArgs e)
    {
        base.UpdateFeedback(e);
        UpdateHoverExpand();
    }

    private protected override void ClearFeedback()
    {
        base.ClearFeedback();
        CancelHoverExpand();
    }

    /// <summary>
    /// Finds the nearest <see cref="TreeViewItem"/> at or above the drag event's source, within the attached control.
    /// </summary>
    protected override Control? LocateTargetContainer(RoutedEventArgs triggerEvent)
    {
        if (triggerEvent.Source is not Control source)
            return null;

        return source.GetSelfAndLogicalAncestors()
            .TakeWhile(x => x != AttachedControl)
            .OfType<TreeViewItem>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the node's index among its siblings.
    /// </summary>
    protected override int? LocateTargetContainerIndex(Control? container) =>
        container is null ? null : ItemsControl.ItemsControlFromItemContainer(container)?.IndexFromContainer(container);

    /// <summary>
    /// The node's header row, because a <see cref="TreeViewItem"/>'s bounds include its expanded children.
    /// </summary>
    protected override Control GetInsertionTarget(Control container) =>
        container is TreeViewItem item ? TreeDragOperation.FindHeader(item) : container;

    /// <summary>
    /// For a node whose container has an <see cref="IList"/> <see cref="ItemsControl.ItemsSource"/>: the top quarter is before it,
    /// the bottom quarter after it and the middle inside it. Other nodes split at the middle.
    /// </summary>
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

    /// <summary>
    /// Resolves the location from the tree: the hovered node's children for a drop inside it or just after an expanded node's header,
    /// its siblings otherwise, and the roots when no node is hovered.
    /// </summary>
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
    /// Rejects drops into any dragged node itself or any of its descendants.
    /// </summary>
    protected override bool CanDropAtLocation(DragEventArgs e)
    {
        if (DropLocation is null)
            return false;

        if (!TryGetPayloads<object>(e, out var payloads) || !payloads.Any(x => IsWithinSubtreeOf(DropLocation.ParentContainer, x)))
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
