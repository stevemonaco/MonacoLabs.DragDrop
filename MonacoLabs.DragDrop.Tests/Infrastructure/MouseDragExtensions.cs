using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace MonacoLabs.DragDrop.Tests;

public static class MouseDragExtensions
{
    /// <summary>
    /// Presses the left button at <paramref name="from"/>, moves through <paramref name="path"/> and releases on its last point, holding
    /// <paramref name="modifiers"/> throughout. The first point must be at least the source's drag threshold from <paramref name="from"/>.
    /// </summary>
    /// <remarks>Returns after the drag source's code following the platform drag has run. The button is always released, because an
    /// in-process drag that never sees the release never ends.</remarks>
    public static void MouseDrag(this Window window, Point from, IReadOnlyList<Point> path, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Count);
        var held = modifiers | RawInputModifiers.LeftMouseButton;

        window.MouseDown(from, MouseButton.Left, modifiers);
        try
        {
            // The first move starts the drag; the in-process drag loop only sees the moves after it
            window.MouseMove(path[0], held);
            foreach (var point in path)
                window.MouseMove(point, held);
            window.MouseMove(path[^1], held);
        }
        finally
        {
            window.MouseUp(path[^1], MouseButton.Left, modifiers);
            Dispatcher.UIThread.RunJobs();
        }
    }

    public static void MouseDrag(this Window window, Point from, Point to, RawInputModifiers modifiers = RawInputModifiers.None) =>
        window.MouseDrag(from, [to], modifiers);
}
