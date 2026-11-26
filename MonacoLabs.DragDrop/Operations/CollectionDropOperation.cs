using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MonacoLabs.DragDrop.Abstractions;
using System.Collections;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Handles drop operations into flat collection controls
/// </summary>
public partial class CollectionDropOperation : DropOperationBase
{
    private DispatcherTimer? _autoScrollTimer;
    private ScrollViewer? _autoScrollViewer;
    private Vector _autoScrollStep;

    /// <summary>
    /// True, so items can be reordered within the attached control, unless <see cref="ItemOrder"/> is <c>Sorted</c>.
    /// </summary>
    protected override bool AllowsSelfDrop => ItemOrder == ItemOrder.Positional;

    /// <summary>
    /// Item container under the pointer, or null when the pointer isn't over an item.
    /// </summary>
    protected Control? HoveredContainer { get; private set; }

    /// <summary>
    /// Where the payload lands relative to <see cref="HoveredContainer"/>.
    /// </summary>
    protected DropTargetOffset HoveredOffset { get; private set; } = DropTargetOffset.AfterTarget;

    /// <summary>
    /// Where the payload will be inserted, updated as the drag moves. Null when there is no target collection.
    /// </summary>
    protected CollectionDropLocation? DropLocation { get; private set; }

    /// <summary>
    /// Whether in-process payloads are inserted into <see cref="DropLocation"/> on drop. Requires <see cref="DropOperationBase.PayloadTarget"/> by default.
    /// </summary>
    protected virtual bool TransfersPayload => PayloadTarget is IList;

    private protected override void OnAttached() => ItemDropAdorner ??= new DropInsertionAdorner();

    private protected override void ResolveHover(DragEventArgs e)
    {
        UpdateHoveredContainer(e);
        DropLocation = ResolveDropLocation();
    }

    private protected override void UpdateFeedback(DragEventArgs e)
    {
        base.UpdateFeedback(e);

        if (ItemDropAdorner is not null)
            ItemDropAdorner.IsDropValid = IsDropValid;

        if (HoveredContainer is not null)
            SetContainerPseudoClasses(HoveredContainer, IsDropValid ? HoveredOffset : null);

        UpdateAutoScroll(e);
    }

    private protected override void ClearFeedback()
    {
        ClearHoveredContainer();
        StopAutoScroll();
        _autoScrollViewer = null;
        DropLocation = null;
        base.ClearFeedback();
    }

    // Avalonia 12.2 (#22002) scrolls to a shifted selection anchor before the panel sees the insert, leaving a stale container
    private protected override IDisposable? BeginCompleteDrop() =>
        AttachedControl is SelectingItemsControl { AutoScrollToSelectedItem: true } control
            ? control.SetValue(SelectingItemsControl.AutoScrollToSelectedItemProperty, false, BindingPriority.Animation)
            : null;

    private void UpdateHoveredContainer(DragEventArgs e)
    {
        // A sorted collection places the drop itself, so no container is a meaningful position
        var container = ItemOrder == ItemOrder.Sorted ? null : LocateTargetContainer(e);
        if (!ReferenceEquals(container, HoveredContainer))
        {
            ClearHoveredContainer();
            HoveredContainer = container;

            if (container is not null && ItemDropAdorner is not null)
            {
                ItemDropAdorner.TargetControl = GetInsertionTarget(container);
                ItemDropAdorner.Attach();
            }
        }

        if (container is null)
        {
            HoveredOffset = DropTargetOffset.AfterTarget;
            return;
        }

        var insertionTarget = GetInsertionTarget(container);
        HoveredOffset = GetDropOffset(container, e.GetPosition(insertionTarget), insertionTarget.Bounds.Size);
        ItemDropAdorner?.Update(HoveredOffset);
    }

    private void ClearHoveredContainer()
    {
        if (HoveredContainer is not null)
            SetContainerPseudoClasses(HoveredContainer, null);

        ItemDropAdorner?.Detach();
        HoveredContainer = null;
    }

    private static void SetContainerPseudoClasses(Control container, DropTargetOffset? offset)
    {
        var classes = (IPseudoClasses)container.Classes;
        classes.Set(":dropover", offset is not null);
        classes.Set(":droptop", offset == DropTargetOffset.BeforeTarget);
        classes.Set(":dropbottom", offset == DropTargetOffset.AfterTarget);
        classes.Set(":dropinside", offset == DropTargetOffset.OnTarget);
    }

    /// <summary>
    /// Inserts <paramref name="values"/> consecutively, in order, at <see cref="DropLocation"/>, or appends them when the location has no index,
    /// and reports where the first landed. Transfers nothing when <see cref="TransfersPayload"/> is false or there is no location.
    /// </summary>
    protected override DropInfo? Transfer(DragEventArgs e, DragInfo dragInfo, IReadOnlyList<object> values)
    {
        if (!TransfersPayload || DropLocation is not { } location)
            return null;

        var origins = (dragInfo as CollectionDragInfo)?.Items;
        int? firstIndex = null;
        var nextIndex = location.Index;

        for (var i = 0; i < values.Count; i++)
        {
            var origin = origins?.ElementAtOrDefault(i);
            var actualIndex = nextIndex is int index
                ? TransferPayloadIndexed(values[i], location.Collection, index, origin?.Collection, origin?.Index)
                : TransferPayload(values[i], location.Collection, origin?.Collection, origin?.Index);

            firstIndex ??= actualIndex;
            if (nextIndex is not null)
                nextIndex = actualIndex + 1;
        }

        return CreateDropInfo(e, firstIndex, values.Count);
    }

    /// <summary>
    /// True when <see cref="TransfersPayload"/> is true and there is a <see cref="DropLocation"/>.
    /// </summary>
    protected override bool CanTransfer(DragEventArgs e) => TransfersPayload && DropLocation is not null;

    private protected override DropInfo CreateUntransferredDropInfo(DragEventArgs e, IReadOnlyList<object> values) =>
        CreateDropInfo(e, DropLocation?.Index, values.Count);

    private protected override DropTargetOffset CommandOffset => HoveredOffset;

    private protected override CollectionDropLocation? CommandLocation => DropLocation;

    private CollectionDropInfo CreateDropInfo(DragEventArgs e, int? targetIndex, int targetCount) => new()
    {
        DragEventArgs = e,
        HoveredControl = HoveredContainer ?? AttachedControl!,
        HoverLocation = e.GetPosition(AttachedControl!),
        TargetCollection = DropLocation?.Collection,
        TargetIndex = targetIndex,
        TargetCount = targetCount,
        TargetContainer = HoveredContainer,
        TargetParent = DropLocation?.Parent
    };

    /// <summary>
    /// Appends the payload to the target collection
    /// </summary>
    /// <returns>Actual index where payload was stored</returns>
    protected virtual int TransferPayload(object payload, IList targetCollection, IList? payloadCollection, int? payloadIndex)
    {
        targetCollection.Add(payload);
        return targetCollection.Count - 1;
    }

    /// <summary>
    /// Inserts the payload at <paramref name="targetIndex"/> in the target collection
    /// </summary>
    /// <returns>Actual index where payload was stored</returns>
    protected virtual int TransferPayloadIndexed(object payload, IList targetCollection, int targetIndex, IList? payloadCollection, int? payloadIndex)
    {
        targetCollection.Insert(targetIndex, payload);
        return targetIndex;
    }

    private protected override bool CanDropStructurally(DragEventArgs e) => CanDropAtLocation(e);

    /// <summary>
    /// Structural check of <see cref="DropLocation"/> that runs before <see cref="DropOperationBase.CanDrop"/>, so overrides of CanDrop can't skip it.
    /// Return <see cref="DropOperationBase.Reject"/> with a message to explain a rejection.
    /// </summary>
    protected virtual bool CanDropAtLocation(DragEventArgs e) => true;

    /// <summary>
    /// Resolves where a drop at the current hover position inserts the payload.
    /// </summary>
    protected virtual CollectionDropLocation? ResolveDropLocation()
    {
        var collection = PayloadTarget as IList ?? (AttachedControl as ItemsControl)?.ItemsSource as IList;
        if (collection is null)
            return null;

        if (HoveredContainer is null || LocateTargetContainerIndex(HoveredContainer) is not int index || index < 0)
            return new CollectionDropLocation(collection, null, null, AttachedControl);

        var displayIndex = HoveredOffset == DropTargetOffset.AfterTarget ? index + 1 : index;
        var targetIndex = AttachedControl is ItemsControl items && !ReferenceEquals(items.ItemsSource, collection)
            ? ResolveTargetIndex(collection, displayIndex)
            : displayIndex;
        return new CollectionDropLocation(collection, targetIndex, null, AttachedControl);
    }

    /// <summary>
    /// Maps a position in the attached control's items to an index in <paramref name="collection"/>, when the control shows a filtered or sorted
    /// view of it rather than the collection itself. By default, the index of the item shown at <paramref name="displayIndex"/> (equal items
    /// matched by occurrence), or just after the last shown item at the end of the view. Return null to append.
    /// </summary>
    protected virtual int? ResolveTargetIndex(IList collection, int displayIndex) =>
        AttachedControl is ItemsControl items ? DisplayIndexMap.ToCollectionIndex(items.ItemsView, collection, displayIndex) : displayIndex;

    /// <summary>
    /// Decides where the payload lands relative to the hovered container from the pointer position over its insertion target.
    /// </summary>
    protected virtual DropTargetOffset GetDropOffset(Control container, Point position, Size size) =>
        position.Y < size.Height / 2 ? DropTargetOffset.BeforeTarget : DropTargetOffset.AfterTarget;

    /// <summary>
    /// Gets the control that <see cref="ItemDropAdorner"/> is drawn over for a hovered container.
    /// </summary>
    protected virtual Control GetInsertionTarget(Control container) => container;

    /// <summary>
    /// Finds the item container under the drag event's source: its topmost logical ancestor below the attached control,
    /// or null when that is part of the attached control's own template.
    /// </summary>
    protected virtual Control? LocateTargetContainer(RoutedEventArgs triggerEvent)
    {
        if (AttachedControl is ItemsControl
            && triggerEvent.Source is Control sourceItem
            && sourceItem.TemplatedParent != AttachedControl)
        {
            var ancestors = sourceItem.GetSelfAndLogicalAncestors();
            var target = ancestors.TakeWhile(x => x != AttachedControl).OfType<Control>().LastOrDefault();

            if (target?.TemplatedParent == AttachedControl)
                return null;

            return target;
        }

        return null;
    }

    /// <summary>
    /// Gets the index of <paramref name="container"/> in its items control, or null when there is none.
    /// </summary>
    protected virtual int? LocateTargetContainerIndex(Control? container)
    {
        if (AttachedControl is ItemsControl items && container is not null)
            return items.IndexFromContainer(container);

        return null;
    }

    private void UpdateAutoScroll(DragEventArgs e)
    {
        _autoScrollViewer ??= AttachedControl as ScrollViewer ?? AttachedControl?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (_autoScrollViewer is null || AutoScrollMargin <= 0)
            return;

        var position = e.GetPosition(_autoScrollViewer);
        var size = _autoScrollViewer.Bounds.Size;
        _autoScrollStep = new Vector(
            AutoScroll.Step(position.X, size.Width, AutoScrollMargin),
            AutoScroll.Step(position.Y, size.Height, AutoScrollMargin));

        if (_autoScrollStep == default)
        {
            StopAutoScroll();
            return;
        }

        _autoScrollTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Input, (_, _) => AutoScrollTick());
        _autoScrollTimer.Start();
    }

    private void AutoScrollTick()
    {
        if (_autoScrollViewer is null)
        {
            StopAutoScroll();
            return;
        }

        _autoScrollViewer.Offset += _autoScrollStep;
    }

    private void StopAutoScroll()
    {
        _autoScrollTimer?.Stop();
        _autoScrollStep = default;
    }
}
