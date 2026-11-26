using Avalonia.Platform.Storage;

namespace DragDropSample.ViewModels;

public record DroppedFileViewModel(IStorageItem StorageItem, string? LocalPath, ulong? Size)
{
    public string Name => StorageItem.Name;

    public string Icon => StorageItem is IStorageFolder ? "📁" : "📄";

    public string Details => StorageItem is IStorageFolder
        ? LocalPath ?? "Folder"
        : $"{FormatSize(Size)} · {LocalPath ?? StorageItem.Path.ToString()}";

    private static string FormatSize(ulong? size) => size switch
    {
        null => "Unknown size",
        < 1024 => $"{size} B",
        < 1024 * 1024 => $"{size / 1024d:0.#} KB",
        _ => $"{size / (1024d * 1024):0.#} MB"
    };
}
