using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Tests;

/// <summary>
/// A drag from outside the window: raises platform drag events on a top level and reports the effect the app returns for each.
/// </summary>
public sealed class SimulatedDrag : IDisposable
{
    public const DragDropEffects AllEffects = DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;

    private readonly TopLevel _topLevel;
    private readonly IDataTransfer _data;
    private readonly DragDropEffects _allowed;
    private readonly IDisposable[] _subscriptions;
    private DragDropEffects? _reportedEffect;

    public SimulatedDrag(TopLevel topLevel, IDataTransfer data, DragDropEffects allowed = AllEffects)
    {
        _topLevel = topLevel;
        _data = data;
        _allowed = allowed;

        // RouteFinished runs after every handler, so it sees the effect the platform would receive
        _subscriptions = [.. new[] { AvaDragDrop.DragEnterEvent, AvaDragDrop.DragOverEvent, AvaDragDrop.DropEvent }
            .Select(e => e.RouteFinished.Subscribe(new Observer(Record)))];
    }

    public DragDropEffects Enter(Point point, RawInputModifiers modifiers = RawInputModifiers.None) =>
        Raise(point, RawDragEventType.DragEnter, modifiers);

    public DragDropEffects Over(Point point, RawInputModifiers modifiers = RawInputModifiers.None) =>
        Raise(point, RawDragEventType.DragOver, modifiers);

    public void Leave(Point point) => Raise(point, RawDragEventType.DragLeave, RawInputModifiers.None);

    public DragDropEffects Drop(Point point, RawInputModifiers modifiers = RawInputModifiers.None) =>
        Raise(point, RawDragEventType.Drop, modifiers);

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
            subscription.Dispose();
    }

    private DragDropEffects Raise(Point point, RawDragEventType type, RawInputModifiers modifiers)
    {
        _reportedEffect = null;
        _topLevel.DragDrop(point, type, _data, _allowed, modifiers);
        var effect = _reportedEffect ?? DragDropEffects.None;
        Dispatcher.UIThread.RunJobs();
        return effect;
    }

    private void Record(RoutedEventArgs e)
    {
        if (e is DragEventArgs drag && e.Source is Visual source && TopLevel.GetTopLevel(source) == _topLevel)
            _reportedEffect = drag.DragEffects;
    }

    private sealed class Observer(Action<RoutedEventArgs> onNext) : IObserver<RoutedEventArgs>
    {
        public void OnNext(RoutedEventArgs value) => onNext(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}

public static class SimulatedDragExtensions
{
    public static SimulatedDrag SimulateDrag(this TopLevel topLevel, IDataTransfer data, DragDropEffects allowed = SimulatedDrag.AllEffects) =>
        new(topLevel, data, allowed);
}
