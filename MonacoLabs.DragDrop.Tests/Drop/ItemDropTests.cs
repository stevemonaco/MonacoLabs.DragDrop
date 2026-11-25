using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class ItemDropTests
{
    [AvaloniaFact]
    public void ASlotDroppedOnItselfKeepsItsValue()
    {
        var slot = new Border { Background = Brushes.Blue, Width = 100, Height = 100, HorizontalAlignment = HorizontalAlignment.Left };
        var drag = new ItemDragOperation { Payload = "A" };
        var drop = new ItemDropOperation { PayloadTarget = "A" };
        DragDrop.SetDragOperation(slot, drag);
        DragDrop.SetDropOperation(slot, drop);
        var window = TestWindow.Show(slot);

        window.MouseDrag(window.PointIn(slot, new(20, 20)), window.PointIn(slot, new(80, 80)));

        Assert.Equal("A", drag.Payload);
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void ASingleValueTargetRefusesSeveralPayloads()
    {
        var source = new RecordingDragOperation();
        var drop = new ItemDropOperation { PayloadTarget = "old" };
        var (window, target) = Show(drop);
        using var drag = window.SimulateDrag(DragData.For(source, ["A", "B"]));

        Assert.Equal(DragDropEffects.None, drag.Enter(window.PointIn(target)));
        Assert.DoesNotContain(":dropover", target.Classes);
        Assert.False(drop.DropAdorner!.IsAttached);
        Assert.Equal(DragDropEffects.None, drag.Drop(window.PointIn(target)));

        Assert.Equal("old", drop.PayloadTarget);
        Assert.Empty(source.Completions);
    }

    [AvaloniaFact]
    public void AnItemTargetThatAcceptsSeveralPayloadsTakesTheFirst()
    {
        var drop = new ItemDropOperation { AcceptsMultiplePayloads = true };
        var (window, target) = Show(drop);
        using var drag = window.SimulateDrag(DragData.For(new RecordingDragOperation(), ["A", "B"]));

        Assert.Equal(DragDropEffects.Move, drag.Enter(window.PointIn(target)));
        drag.Drop(window.PointIn(target));

        Assert.Equal("A", drop.PayloadTarget);
    }

    private static (Window, Border) Show(ItemDropOperation operation)
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        DragDrop.SetDropOperation(target, operation);
        return (TestWindow.Show(target), target);
    }
}
