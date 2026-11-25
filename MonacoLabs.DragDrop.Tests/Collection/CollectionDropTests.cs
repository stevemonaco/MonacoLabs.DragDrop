using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
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

    [AvaloniaFact]
    public void ACommandOnlyTargetCompletesAMove()
    {
        var source = new List<string> { "A", "B" };
        DropEventArgs? args = null;
        var (window, target) = ShowTarget(new CollectionDropOperation { PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) });
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation { PayloadCollection = source }, "A"));

        drag.Enter(window.PointIn(target));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));

        Assert.Equal(["A"], args?.Items);
        Assert.Equal(["B"], source);
    }

    [AvaloniaFact]
    public void ADropThatNeitherTransfersNorRunsACommandReportsNone()
    {
        var source = new List<string> { "A", "B" };
        var (window, target) = ShowTarget(new CollectionDropOperation());
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation { PayloadCollection = source }, "A"));

        Assert.Equal(DragDropEffects.Move, drag.Enter(window.PointIn(target)));
        Assert.Equal(DragDropEffects.None, drag.Drop(window.PointIn(target)));

        Assert.Equal(["A", "B"], source);
    }

    [AvaloniaFact]
    public void ACommandWhoseCanExecuteIsFalseReportsNone()
    {
        var source = new List<string> { "A", "B" };
        var executed = false;
        var (window, target) = ShowTarget(new CollectionDropOperation { PayloadCommand = new RelayCommand(_ => executed = true, _ => false) });
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation { PayloadCollection = source }, "A"));

        drag.Enter(window.PointIn(target));
        Assert.Equal(DragDropEffects.None, drag.Drop(window.PointIn(target)));

        Assert.False(executed);
        Assert.Equal(["A", "B"], source);
    }

    [AvaloniaFact]
    public void DetachingMidDragClearsTheHoveredContainer()
    {
        var items = new List<string> { "a", "b", "c" };
        var list = new ListBox
        {
            ItemsSource = items,
            Width = 150,
            Height = 200,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 30 })
        };
        var operation = new CollectionDropOperation { PayloadTarget = items };
        DragDrop.SetDropOperation(list, operation);
        var window = TestWindow.Show(list);
        var container = list.ContainerFromIndex(1)!;
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));
        drag.Enter(window.PointIn(container));
        Assert.Contains(":dropover", container.Classes);
        Assert.True(operation.ItemDropAdorner!.IsAttached);

        DragDrop.SetDropOperation(list, null);

        Assert.DoesNotContain(container.Classes, c => c is ":dropover" or ":droptop" or ":dropbottom" or ":dropinside");
        Assert.DoesNotContain(":dropover", list.Classes);
        Assert.False(operation.ItemDropAdorner.IsAttached);
        Assert.False(operation.DropAdorner!.IsAttached);
    }

    private static (Window, Border) ShowTarget(CollectionDropOperation operation)
    {
        var target = new Border { Width = 200, Height = 200, Background = Brushes.Blue };
        DragDrop.SetDropOperation(target, operation);
        return (TestWindow.Show(target), target);
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
