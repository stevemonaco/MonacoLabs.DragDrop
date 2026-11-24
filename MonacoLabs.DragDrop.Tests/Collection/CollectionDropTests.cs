using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class CollectionDropTests
{
    [AvaloniaFact]
    public void ADragOverTheTargetsOwnSurfaceIsAccepted()
    {
        var items = new List<string>();
        var target = new Border { Width = 200, Height = 200, Background = Brushes.Blue, Child = new Border { Width = 40, Height = 40 } };
        DragDrop.SetDropOperation(target, new CollectionDropOperation { PayloadTarget = items });
        var window = TestWindow.Show(target);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "A"));
        var background = window.PointIn(target, new Point(10, 10));

        Assert.Equal(DragDropEffects.Move, drag.Enter(background));
        Assert.Equal(DragDropEffects.Move, drag.Drop(background));
        Assert.Equal(["A"], items);
    }
}
