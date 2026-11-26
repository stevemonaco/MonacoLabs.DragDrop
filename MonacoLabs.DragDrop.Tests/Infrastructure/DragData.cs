using Avalonia.Input;
using MonacoLabs.DragDrop.Abstractions;

namespace MonacoLabs.DragDrop.Tests;

public static class DragData
{
    /// <summary>
    /// Builds the <see cref="DataTransfer"/> <paramref name="operation"/> would start a drag of one payload with, through its own data transfer items.
    /// </summary>
    /// <param name="metadata">The drag's metadata. When omitted, a <see cref="CollectionDragInfo"/> whose items come from the operation's
    /// <c>SourceCollection</c> for collection operations, and a plain <see cref="DragInfo"/> otherwise, both without pressed indexes.</param>
    public static DataTransfer For(DragOperationBase operation, object payload, DragInfo? metadata = null) =>
        For(operation, [payload], metadata);

    /// <summary>
    /// Builds the <see cref="DataTransfer"/> <paramref name="operation"/> would start a drag of <paramref name="payloads"/> with.
    /// </summary>
    public static DataTransfer For(DragOperationBase operation, IReadOnlyList<object> payloads, DragInfo? metadata = null)
    {
        metadata ??= CreateMetadata(operation, payloads);
        metadata.Payloads = payloads;
        return operation.CreateDataTransfer(metadata);
    }

    private static DragInfo CreateMetadata(DragOperationBase operation, IReadOnlyList<object> payloads) => operation switch
    {
        CollectionDragOperation collection => new CollectionDragInfo
        {
            DragOperation = operation,
            DragIds = [.. operation.InteractionIds],
            Items = [.. payloads.Select(x => new CollectionDragItem(x, collection.SourceCollection, null, null))]
        },
        _ => new DragInfo { DragOperation = operation, DragIds = [.. operation.InteractionIds] }
    };
}
