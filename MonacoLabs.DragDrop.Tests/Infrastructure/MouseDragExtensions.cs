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
    /// <remarks>Runs dispatcher jobs after each move, as a message loop would, and returns after the drag source's code following the
    /// platform drag has run. The button is always released, because an in-process drag that never sees the release never ends.</remarks>
    public static void MouseDrag(this Window window, Point from, IReadOnlyList<Point> path, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Count);
        var held = modifiers | RawInputModifiers.LeftMouseButton;

        window.MouseDown(from, MouseButton.Left, modifiers);
        try
        {
            // The first move starts the drag; the in-process drag loop only sees the moves after it
            Move(path[0]);
            foreach (var point in path)
                Move(point);
            Move(path[^1]);
        }
        finally
        {
            window.MouseUp(path[^1], MouseButton.Left, modifiers);
            Dispatcher.UIThread.RunJobs();
        }

        void Move(Point point)
        {
            window.MouseMove(point, held);
            Dispatcher.UIThread.RunJobs();
        }
    }

    public static void MouseDrag(this Window window, Point from, Point to, RawInputModifiers modifiers = RawInputModifiers.None) =>
        window.MouseDrag(from, [to], modifiers);
}
