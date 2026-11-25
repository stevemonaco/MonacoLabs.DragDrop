using Avalonia.Input;
using MonacoLabs.DragDrop.Abstractions;

namespace MonacoLabs.DragDrop.Tests;

public static class DragData
{
    /// <summary>
    /// Builds the <see cref="DataTransfer"/> <paramref name="operation"/> would start a drag with, through its own data transfer item.
    /// </summary>
    /// <param name="metadata">The drag's metadata. When omitted, a <see cref="CollectionDragInfo"/> carrying the operation's <c>PayloadCollection</c> for
    /// collection operations, and a plain <see cref="DragInfo"/> otherwise, both without a pressed index.</param>
    public static DataTransfer For(DragOperationBase operation, object payload, DragInfo? metadata = null) =>
        operation.CreateDataTransfer(metadata ?? CreateMetadata(operation, payload), payload);

    private static DragInfo CreateMetadata(DragOperationBase operation, object payload) => operation switch
    {
        CollectionDragOperation collection => new CollectionDragInfo
        {
            DragOperation = operation,
            DragIds = [.. operation.InteractionIds],
            PayloadCollection = collection.PayloadCollection,
            Payload = payload
        },
        _ => new DragInfo { DragOperation = operation, DragIds = [.. operation.InteractionIds], Payload = payload }
    };
}
