using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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
}
