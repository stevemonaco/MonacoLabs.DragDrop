using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MonacoLabs.DragDrop.Abstractions;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class DragGestureTests
{
    [AvaloniaFact]
    public void SettingRoutingWhileAttachedThrows()
    {
        var operation = new ItemDragOperation();
        operation.Attach(new Border());

        Assert.Throws<InvalidOperationException>(() => operation.Routing = RoutingStrategies.Tunnel);
        Assert.Throws<InvalidOperationException>(() => operation.HandledEventsToo = true);
        Assert.Equal(RoutingStrategies.Bubble, operation.Routing);
        Assert.False(operation.HandledEventsToo);
    }

    [AvaloniaFact]
    public void RoutingCanBeSetWhileDetached()
    {
        var operation = new ItemDragOperation { Routing = RoutingStrategies.Tunnel, HandledEventsToo = true };
        var control = new Border();
        operation.Attach(control);
        operation.Detach(control);

        operation.Routing = RoutingStrategies.Bubble;
        operation.HandledEventsToo = false;

        Assert.Equal(RoutingStrategies.Bubble, operation.Routing);
        Assert.False(operation.HandledEventsToo);
    }

    [AvaloniaFact]
    public void AnOnPointerMovedOverrideCanAwaitBeforeCallingTheBase()
    {
        var (window, source, target, operation, drop) = Show(new YieldingDragOperation { Payload = "A" });

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.True(operation.Resumed);
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void ADragDoesNotStartTwiceFromOnePress()
    {
        var (window, source, target, operation, drop) = Show(new CountingDragOperation { Payload = "A" });

        window.MouseDrag(window.PointIn(source), [window.PointIn(target), window.PointIn(target) + new Point(5, 5), window.PointIn(target)]);

        Assert.Equal(1, operation.Starts);
        Assert.Equal("A", drop.PayloadTarget);
    }

    private static (Window, Border, Border, T, ItemDropOperation) Show<T>(T operation) where T : ItemDragOperation
    {
        var source = new Border { Background = Brushes.Red, Width = 100, Height = 100 };
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var drop = new ItemDropOperation();
        DragDrop.SetDragOperation(source, operation);
        DragDrop.SetDropOperation(target, drop);
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { source, target } };
        return (TestWindow.Show(panel), source, target, operation, drop);
    }

    private sealed class YieldingDragOperation : ItemDragOperation
    {
        public bool Resumed { get; private set; }

        protected override async Task OnPointerMoved(PointerEventArgs e)
        {
            await Task.Yield();
            Resumed = true;
            await base.OnPointerMoved(e);
        }
    }

    private sealed class CountingDragOperation : ItemDragOperation
    {
        public int Starts { get; private set; }

        protected override Task StartDrag(PointerEventArgs e)
        {
            Starts++;
            return base.StartDrag(e);
        }
    }
}
