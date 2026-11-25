using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Tests;

/// <summary>
/// Records the effect a window reports to the platform for each DragEnter and DragOver, read after every handler has run, the drag
/// session's window handlers included.
/// </summary>
public sealed class ReportedEffects : IDisposable, IObserver<RoutedEventArgs>
{
    private readonly TopLevel _topLevel;
    private readonly IDisposable[] _subscriptions;

    public ReportedEffects(TopLevel topLevel)
    {
        _topLevel = topLevel;
        _subscriptions = [AvaDragDrop.DragEnterEvent.RouteFinished.Subscribe(this), AvaDragDrop.DragOverEvent.RouteFinished.Subscribe(this)];
    }

    public DragDropEffects? Last { get; private set; }

    public void OnNext(RoutedEventArgs value)
    {
        if (value is DragEventArgs e && e.Source is Visual source && TopLevel.GetTopLevel(source) == _topLevel)
            Last = e.DragEffects;
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
            subscription.Dispose();
    }
}
