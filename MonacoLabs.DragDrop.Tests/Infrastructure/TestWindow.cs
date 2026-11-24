using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace MonacoLabs.DragDrop.Tests;

public static class TestWindow
{
    /// <summary>
    /// Shows <paramref name="content"/> in a window of a fixed size and runs layout.
    /// </summary>
    public static Window Show(Control content, double width = 400, double height = 300)
    {
        var window = new Window { Width = width, Height = height, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>
    /// Translates <paramref name="local"/> in <paramref name="visual"/>, or its center when omitted, to window coordinates.
    /// </summary>
    public static Point PointIn(this Window window, Visual visual, Point? local = null) =>
        visual.TranslatePoint(local ?? new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), window)
        ?? throw new InvalidOperationException($"{visual} is not in the window.");
}
