using Avalonia.Platform.Storage;
using DragDropSample.ViewModels;
using MonacoLabs.DragDrop;

namespace DragDropSample.CustomDragDrop;

/// <summary>
/// Offers the storage item wrapped by a <see cref="DroppedFileViewModel"/> to other applications.
/// </summary>
public class FileDragOperation : CollectionDragOperation
{
    protected override IStorageItem? ResolvePlatformFile(object payload) => (payload as DroppedFileViewModel)?.StorageItem;
}
