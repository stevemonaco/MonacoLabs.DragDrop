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

    public CollectionDragOperation()
    {
        Routing = RoutingStrategies.Tunnel;
        _handledEventsToo = true;
    }

    protected override void Control_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (AttachedControl is ItemsControl && LocatePayloadContainerIndex(LocatePayloadContainer(e)) is null or < 0)
            return;

        base.Control_PointerPressed(sender, e);
    }

    /// <summary>
    /// Uses the pressed item container's item so the transferred item always matches <see cref="CollectionDragInfo.PayloadContainerIndex"/>.
    /// Falls back to <see cref="DragOperationBase.Payload"/> when the press isn't on an item container.
    /// </summary>
    protected override object? ResolvePayload(PointerPressedEventArgs pressedEventArgs)
    {
        if (LocatePayloadContainer(pressedEventArgs) is { } container
            && ItemsControl.ItemsControlFromItemContainer(container) is { } owner)
            return owner.ItemFromContainer(container);

        return base.ResolvePayload(pressedEventArgs);
    }

    protected override Control? ResolveDragVisual(PointerPressedEventArgs pressedEventArgs) =>
        LocatePayloadContainer(pressedEventArgs) ?? base.ResolveDragVisual(pressedEventArgs);

    protected override DragInfo CreateMetadata(PointerEventArgs e)
    {
        var container = LocatePayloadContainer(e);

        return new CollectionDragInfo()
        {
            DragOperation = this,
            DragOrigin = _dragOrigin!.Value,
            DragIds = InteractionIds.ToList(),
            PointerId = e.Pointer.Id,
            PayloadCollection = PayloadCollection,
            PayloadContainer = container,
            PayloadContainerIndex = LocatePayloadContainerIndex(container)
        };
    }

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

        if (effect != DragDropEffects.Move
            || dragInfo is not CollectionDragInfo { PayloadCollection: IList payloadCollection } collectionDragInfo
            || dropInfo is not CollectionDropInfo { TargetCollection: IList targetCollection } collectionDropInfo)
            return;

        CollectionRemoval.RemoveAfterDrop(payloadCollection, dragInfo.Payload, collectionDragInfo.PayloadContainerIndex, targetCollection, collectionDropInfo.TargetIndex);
    }

    protected override void OnDroppedOutside(DragInfo dragInfo, object payload)
    {
        if (dragInfo is not CollectionDragInfo { PayloadCollection: IList payloadCollection } collectionDragInfo)
            return;

        CollectionRemoval.RemoveDroppedOutside(payloadCollection, payload, collectionDragInfo.PayloadContainerIndex);
    }
}
