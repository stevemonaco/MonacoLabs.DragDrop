using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MonacoLabs.DragDrop.Tests;

public static class TestFiles
{
    /// <summary>
    /// Creates an empty temporary file named <paramref name="name"/> and returns it as the storage item a platform drop would carry.
    /// </summary>
    /// <remarks>Storage items are not implementable by user code, so tests use real files from <paramref name="topLevel"/>'s storage provider.</remarks>
    public static async Task<IStorageFile> CreateAsync(TopLevel topLevel, string name)
    {
        var directory = Directory.CreateTempSubdirectory("MonacoLabs.DragDrop.Tests");
        var path = Path.Combine(directory.FullName, name);
        await File.WriteAllTextAsync(path, "");
        return await topLevel.StorageProvider.TryGetFileFromPathAsync(new Uri(path))
            ?? throw new InvalidOperationException($"{topLevel.StorageProvider.GetType().Name} cannot open {path}.");
    }
}
