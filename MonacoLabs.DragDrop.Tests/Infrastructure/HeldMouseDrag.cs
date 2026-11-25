using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace MonacoLabs.DragDrop.Tests;

/// <summary>
/// A real drag held between moves, so tests can look at it in flight. Disposing releases the button at the last point if the drag
/// was not released or canceled.
/// </summary>
public sealed class HeldMouseDrag : IDisposable
{
    private readonly Window _window;
    private Point _last;
    private bool _ended;

    private HeldMouseDrag(Window window, Point start)
    {
        _window = window;
        _last = start;
    }

    /// <summary>
    /// Presses at <paramref name="from"/> and moves to <paramref name="start"/>, which must be past the source's drag threshold.
    /// The drag has started, but has not yet raised drag events: the in-process drag loop only sees the moves after it.
    /// </summary>
    public static HeldMouseDrag Start(Window window, Point from, Point start)
    {
        window.MouseDown(from, MouseButton.Left);
        window.MouseMove(start, RawInputModifiers.LeftMouseButton);
        Dispatcher.UIThread.RunJobs();
        return new HeldMouseDrag(window, start);
    }

    public void MoveTo(Point point, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _window.MouseMove(point, modifiers | RawInputModifiers.LeftMouseButton);
        Dispatcher.UIThread.RunJobs();
        _last = point;
    }

    public void Release(RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _ended = true;
        _window.MouseUp(_last, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    public void Cancel()
    {
        _window.KeyPress(Key.Escape, RawInputModifiers.LeftMouseButton, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();
        Release();
    }

    public void Dispose()
    {
        if (!_ended)
            Release();
    }
}
