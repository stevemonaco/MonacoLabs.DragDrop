using Avalonia.Input;
using Avalonia.Platform.Storage;
using MonacoLabs.DragDrop;
using System;
using System.IO;
using System.Linq;

namespace DragDropSample.CustomDragDrop;

/// <summary>
/// Accepts a single dropped image file and explains why anything else is rejected.
/// </summary>
public class ImageDropOperation : ItemDropOperation
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp"];

    protected override bool CanDrop(DragEventArgs e)
    {
        if (!TryGetFiles(e, out var files))
            return false;

        if (files.Count > 1)
        {
            DropAdorner!.ErrorMessage = "Drop one image at a time";
            return false;
        }

        if (files[0] is not IStorageFile file || !ImageExtensions.Contains(Path.GetExtension(file.Name), StringComparer.OrdinalIgnoreCase))
        {
            DropAdorner!.ErrorMessage = "Only PNG, JPG, GIF, BMP or WebP images";
            return false;
        }

        return true;
    }
}
