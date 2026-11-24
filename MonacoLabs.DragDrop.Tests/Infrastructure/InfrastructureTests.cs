using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class InfrastructureTests
{
    [AvaloniaFact]
    public void TestWindowShowsTheContentLaidOut()
    {
        var border = new Border { Width = 100, Height = 50 };
        var window = TestWindow.Show(new StackPanel { Margin = new Thickness(10), Children = { border } });

        Assert.Equal(100, border.Bounds.Width);
        Assert.Equal(new Point(200, 35), window.PointIn(border));
    }

    [AvaloniaFact]
    public void DragDataCarriesThePayloadUnderEachIdAndTheMetadata()
    {
        var operation = new ItemDragOperation { InteractionIds = ["a", "b", PlatformInteractionIds.Text] };

        var data = DragData.For(operation, "hello");

        Assert.Equal("hello", data.TryGetValue(DragDropIds.PayloadFormat("a")));
        Assert.Equal("hello", data.TryGetValue(DragDropIds.PayloadFormat("b")));
        Assert.Equal("hello", data.TryGetText());
        var metadata = data.TryGetValue(DragDropIds.DragMetadataFormat);
        Assert.Same(operation, metadata?.DragOperation);
        Assert.Equal("hello", metadata?.Payload);
    }

    [AvaloniaFact]
    public void SimulatedDragReportsTheTargetsEffectsAndDrops()
    {
        var (window, target, dropOperation) = ShowItemTarget();
        var source = new ItemDragOperation { Payload = "A", AllowedEffects = DragDropEffects.Copy | DragDropEffects.Move };
        using var drag = window.SimulateDrag(DragData.For(source, "A"), source.AllowedEffects);
        var center = window.PointIn(target);

        Assert.Equal(DragDropEffects.None, drag.Enter(new Point(350, 150)));
        Assert.Equal(DragDropEffects.Move, drag.Over(center));
        Assert.Equal(DragDropEffects.Copy, drag.Over(center, RawInputModifiers.Control));
        Assert.Equal(DragDropEffects.Move, drag.Drop(center));
        Assert.Equal("A", dropOperation.PayloadTarget);
        Assert.Null(source.Payload);
    }

    [AvaloniaFact]
    public void SimulatedDragLeaveRunsTheDeferredLeave()
    {
        var (window, target, _) = ShowItemTarget();
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        drag.Enter(window.PointIn(target));
        Assert.Contains(":dropover", target.Classes);

        drag.Leave(window.PointIn(target));
        Assert.DoesNotContain(":dropover", target.Classes);
    }

    [AvaloniaFact]
    public void MouseDragMovesThePayloadFromSourceToTarget()
    {
        var (window, source, target, dragOperation, dropOperation) = ShowSourceAndTarget();

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Equal("A", dropOperation.PayloadTarget);
        Assert.Null(dragOperation.Payload);
        Assert.DoesNotContain(":dragging", source.Classes);
    }

    [AvaloniaFact]
    public void MouseDragHoldsItsModifiersThroughTheDrop()
    {
        var (window, source, target, dragOperation, dropOperation) = ShowSourceAndTarget();
        dragOperation.AllowedEffects = DragDropEffects.Copy | DragDropEffects.Move;

        window.MouseDrag(window.PointIn(source), window.PointIn(target), RawInputModifiers.Control);

        Assert.Equal("A", dropOperation.PayloadTarget);
        Assert.Equal("A", dragOperation.Payload);
    }

    [AvaloniaFact]
    public void MouseDragReturnsAfterTheSourceHandledTheDragsEnd()
    {
        var (window, source, _, dragOperation, _) = ShowSourceAndTarget();
        dragOperation.RemoveOnDropOutside = true;

        window.MouseDrag(window.PointIn(source), new Point(350, 250));

        Assert.Null(dragOperation.Payload);
    }

    [AvaloniaFact]
    public async Task PumpUntilRunsTimers()
    {
        var fired = false;
        DispatcherTimer.RunOnce(() => fired = true, TimeSpan.FromMilliseconds(50));

        await TestDispatcher.PumpUntil(() => fired);

        Assert.True(fired);
    }

    [AvaloniaFact]
    public async Task PumpUntilTimesOutWhenTheConditionNeverHolds()
    {
        var ex = await Assert.ThrowsAsync<TimeoutException>(() => TestDispatcher.PumpUntil(() => false, TimeSpan.FromMilliseconds(50)));

        Assert.Contains("() => false", ex.Message);
    }

    private static (Window, Border, ItemDropOperation) ShowItemTarget()
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100, HorizontalAlignment = HorizontalAlignment.Left };
        var dropOperation = new ItemDropOperation();
        DragDrop.SetDropOperation(target, dropOperation);
        return (TestWindow.Show(target), target, dropOperation);
    }

    private static (Window, Border, Border, ItemDragOperation, ItemDropOperation) ShowSourceAndTarget()
    {
        var source = new Border { Background = Brushes.Red, Width = 100, Height = 100 };
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var dragOperation = new ItemDragOperation { Payload = "A" };
        var dropOperation = new ItemDropOperation();
        DragDrop.SetDragOperation(source, dragOperation);
        DragDrop.SetDropOperation(target, dropOperation);
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { source, target } };
        return (TestWindow.Show(panel), source, target, dragOperation, dropOperation);
    }
}
