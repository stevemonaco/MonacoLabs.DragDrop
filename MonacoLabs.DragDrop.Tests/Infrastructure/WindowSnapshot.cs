using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace MonacoLabs.DragDrop.Tests;

/// <summary>
/// The pixels of a window rendered at scaling 1, adorner layer included, for the few checks only pixels can answer.
/// </summary>
public sealed class WindowSnapshot
{
    private readonly byte[] _pixels;
    private readonly int _width;
    private readonly bool _isBgra;

    private WindowSnapshot(byte[] pixels, int width, bool isBgra)
    {
        _pixels = pixels;
        _width = width;
        _isBgra = isBgra;
    }

    public static WindowSnapshot Take(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        var size = PixelSize.FromSize(window.ClientSize, 1);
        using var bitmap = new RenderTargetBitmap(size);
        bitmap.Render(window);

        var pixels = new byte[size.Width * size.Height * 4];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), pixels.Length, size.Width * 4);
        }
        finally
        {
            handle.Free();
        }

        return new WindowSnapshot(pixels, size.Width, bitmap.Format != PixelFormat.Rgba8888);
    }

    public Color this[int x, int y]
    {
        get
        {
            var i = (y * _width + x) * 4;
            return _isBgra
                ? Color.FromArgb(_pixels[i + 3], _pixels[i + 2], _pixels[i + 1], _pixels[i])
                : Color.FromArgb(_pixels[i + 3], _pixels[i], _pixels[i + 1], _pixels[i + 2]);
        }
    }

    /// <summary>
    /// Whether every pixel in <paramref name="area"/>, in window coordinates, is <paramref name="color"/>.
    /// </summary>
    public bool IsUniform(PixelRect area, Color color)
    {
        for (var y = area.Y; y < area.Bottom; y++)
            for (var x = area.X; x < area.Right; x++)
                if (this[x, y] != color)
                    return false;

        return true;
    }
}
