using Avalonia;
using Avalonia.Controls;
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

    protected override bool AllowsSelfDrop => true;

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

    public override void Attach(Control control)
    {
        base.Attach(control);
        ItemDropAdorner ??= new DropInsertionAdorner();
    }

    public override void Detach(Control control)
    {
        EndDrag();
        base.Detach(control);
    }

    protected override void DragEnter(object? sender, DragEventArgs e)
    {
        CancelPendingDragLeave();
        UpdateDrag(e);
    }

    protected override void DragOver(object? sender, DragEventArgs e) => UpdateDrag(e);

    protected override void OnDragLeave(DragEventArgs dragEventArgs) => EndDrag();

    protected virtual void UpdateDrag(DragEventArgs e)
    {
        if (!IsDropTargetFor(e))
        {
            e.DragEffects = DragDropEffects.None;
            EndDrag();
            return;
        }

        UpdateHover(e);
        DropLocation = ResolveDropLocation();

        var canDrop = CanDropAtLocation(e) && EvaluateCanDrop(e);

        if (DropAdorner is not null)
        {
            DropAdorner.IsDropValid = canDrop;
            DropAdorner.Attach();
        }

        if (ItemDropAdorner is not null)
            ItemDropAdorner.IsDropValid = canDrop;

        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", canDrop);
        if (HoveredContainer is not null)
            SetContainerPseudoClasses(HoveredContainer, canDrop ? HoveredOffset : null);

        UpdateAutoScroll(e);

        if (!canDrop)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = GetDropEffect(e);
        e.Handled = true;
    }

    private void UpdateHover(DragEventArgs e)
    {
        var container = LocateTargetContainer(e);
        if (!ReferenceEquals(container, HoveredContainer))
        {
            ClearHover();
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

    private void ClearHover()
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
    /// Clears hover state, adorners and auto-scrolling once the drag leaves or drops.
    /// </summary>
    protected virtual void EndDrag()
    {
        ClearHover();
        StopAutoScroll();
        DropLocation = null;

        if (AttachedControl is not null)
            ((IPseudoClasses)AttachedControl.Classes).Set(":dropover", false);

        DropAdorner?.Detach();
    }

    protected override void Drop(object? sender, DragEventArgs e)
    {
        CancelPendingDragLeave();

        if (!IsDropTargetFor(e))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        UpdateHover(e);
        DropLocation = ResolveDropLocation();
        var location = DropLocation;

        if (!CanDropAtLocation(e) || !EvaluateCanDrop(e))
        {
            e.DragEffects = DragDropEffects.None;
            EndDrag();
            return;
        }

        e.DragEffects = GetDropEffect(e);

        if (TryGetDragInfo<DragInfo>(e, out var dragInfo) && TryGetPayload<object>(e, out var payload))
        {
            if (TransfersPayload && location is not null)
            {
                var payloadCollection = (dragInfo as CollectionDragInfo)?.PayloadCollection;
                var payloadIndex = (dragInfo as CollectionDragInfo)?.PayloadContainerIndex;

                var actualIndex = location.Index is int index
                    ? TransferPayloadIndexed(payload, location.Collection, index, payloadCollection, payloadIndex)
                    : TransferPayload(payload, location.Collection, payloadCollection, payloadIndex);

                var dropInfo = new CollectionDropInfo()
                {
                    DragEventArgs = e,
                    HoveredControl = HoveredContainer ?? AttachedControl!,
                    HoverLocation = e.GetPosition(AttachedControl!),
                    TargetCollection = location.Collection,
                    TargetIndex = actualIndex,
                    TargetContainer = HoveredContainer,
                    TargetParent = location.Parent
                };

                dragInfo.DragOperation.DropCompleted(e.DragEffects, dragInfo, dropInfo);
            }

            InvokePayloadCommand(e, [payload], HoveredOffset, location);
        }
        else if (TryGetPlatformItems(e, out var items))
        {
            InvokePayloadCommand(e, items, HoveredOffset, location, isPlatformDrop: true);
        }

        OnDrop(e);
        e.Handled = true;
        EndDrag();
    }

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

    protected override bool CanDrop(DragEventArgs e)
    {
        if (e.Source is Control sourceItem && !AttachedControl.IsLogicalAncestorOf(sourceItem))
            return false;

        return CanGetPayload(e);
    }

    /// <summary>
    /// Structural check of <see cref="DropLocation"/> that runs before <see cref="DropOperationBase.CanDrop"/>, so overrides of CanDrop can't skip it.
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

        return new CollectionDropLocation(collection, HoveredOffset == DropTargetOffset.AfterTarget ? index + 1 : index, null, AttachedControl);
    }

    /// <summary>
    /// Decides where the payload lands relative to the hovered container from the pointer position over its insertion target.
    /// </summary>
    protected virtual DropTargetOffset GetDropOffset(Control container, Point position, Size size) =>
        position.Y < size.Height / 2 ? DropTargetOffset.BeforeTarget : DropTargetOffset.AfterTarget;

    /// <summary>
    /// Gets the control that <see cref="ItemDropAdorner"/> is drawn over for a hovered container.
    /// </summary>
    protected virtual Control GetInsertionTarget(Control container) => container;

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
