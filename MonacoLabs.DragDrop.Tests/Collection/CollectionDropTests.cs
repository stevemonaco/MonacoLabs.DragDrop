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

    [AvaloniaFact]
    public void ACopyDropInsertsTheCreatedCopy()
    {
        var (window, target, items, commandItems) = ShowCopyingTarget();
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "A"), DragDropEffects.Copy | DragDropEffects.Move);

        drag.Enter(window.PointIn(target), RawInputModifiers.Control);
        Assert.Equal(DragDropEffects.Copy, drag.Drop(window.PointIn(target), RawInputModifiers.Control));

        Assert.Equal(["A copy"], items);
        Assert.Equal(["A copy"], commandItems());
    }

    [AvaloniaFact]
    public void AMoveDropIgnoresCreateCopy()
    {
        var (window, target, items, commandItems) = ShowCopyingTarget();
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "A"), DragDropEffects.Copy | DragDropEffects.Move);

        drag.Enter(window.PointIn(target));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));

        Assert.Equal(["A"], items);
        Assert.Equal(["A"], commandItems());
    }

    private static (Window, Border, List<string>, Func<IReadOnlyList<object>?>) ShowCopyingTarget()
    {
        var items = new List<string>();
        DropEventArgs? args = null;
        var target = new Border { Width = 200, Height = 200, Background = Brushes.Blue };
        var operation = new CopyingCollectionDropOperation { PayloadTarget = items, PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) };
        DragDrop.SetDropOperation(target, operation);
        return (TestWindow.Show(target), target, items, () => args?.Items);
    }

    private sealed class CopyingCollectionDropOperation : CollectionDropOperation
    {
        protected override object CreateCopy(object payload) => $"{payload} copy";
    }
}
