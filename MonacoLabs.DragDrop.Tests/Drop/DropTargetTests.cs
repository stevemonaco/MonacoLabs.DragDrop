using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MonacoLabs.DragDrop.Abstractions;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class DropTargetTests
{
    [AvaloniaFact]
    public void SettingHandledEventsTooWhileAttachedThrows()
    {
        var operation = new ItemDropOperation();
        operation.Attach(new Border());

        Assert.Throws<InvalidOperationException>(() => operation.HandledEventsToo = true);
        Assert.Throws<InvalidOperationException>(() => operation.Routing = RoutingStrategies.Tunnel);
        Assert.False(operation.HandledEventsToo);
        Assert.Equal(RoutingStrategies.Bubble, operation.Routing);
    }

    [AvaloniaFact]
    public void AttachingWithoutAnAdornerCreatesASolidHighlight()
    {
        var control = new Border();
        var operation = new ItemDropOperation();

        operation.Attach(control);

        var adorner = Assert.IsType<DropHighlightAdorner>(operation.DropAdorner);
        Assert.Equal(DropAdornerKind.Solid, adorner.Kind);
        Assert.Same(control, adorner.TargetControl);
    }

    [AvaloniaFact]
    public void RejectSetsTheMessageAndTheAdornerMirrorsIt()
    {
        var (window, target, operation) = ShowRejectingTarget("Not here");
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        Assert.Equal(DragDropEffects.None, drag.Enter(window.PointIn(target)));

        Assert.True(DropOperationBase.ErrorMessageProperty.IsReadOnly);
        Assert.Equal("Not here", operation.ErrorMessage);
        Assert.Equal("Not here", operation.DropAdorner!.ErrorMessage);
    }

    [AvaloniaFact]
    public void TheMessageIsClearedBeforeEachEvaluation()
    {
        var (window, target, operation) = ShowRejectingTarget("Not here");
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));

        operation.Message = null;

        Assert.Equal(DragDropEffects.Move, drag.Over(window.PointIn(target)));
        Assert.Null(operation.ErrorMessage);
        Assert.Null(operation.DropAdorner!.ErrorMessage);
    }

    [AvaloniaFact]
    public void TheMessageIsClearedWhenTheDragLeaves()
    {
        var (window, target, operation) = ShowRejectingTarget("Not here");
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));

        drag.Leave(window.PointIn(target));

        Assert.Null(operation.ErrorMessage);
        Assert.Null(operation.DropAdorner!.ErrorMessage);
    }

    [AvaloniaFact]
    public void ACopyDropAssignsTheCreatedCopy()
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        DropEventArgs? args = null;
        var operation = new CopyingDropOperation { PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) };
        DragDrop.SetDropOperation(target, operation);
        var window = TestWindow.Show(target);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"), DragDropEffects.Copy | DragDropEffects.Move);

        drag.Enter(window.PointIn(target), RawInputModifiers.Control);
        Assert.Equal(DragDropEffects.Copy, drag.Drop(window.PointIn(target), RawInputModifiers.Control));

        Assert.Equal("A copy", operation.PayloadTarget);
        Assert.Equal(["A copy"], args?.Items);
    }

    private static (Window, Border, RejectingDropOperation) ShowRejectingTarget(string message)
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var operation = new RejectingDropOperation { Message = message };
        DragDrop.SetDropOperation(target, operation);
        return (TestWindow.Show(target), target, operation);
    }

    private sealed class CopyingDropOperation : ItemDropOperation
    {
        protected override object CreateCopy(object payload) => $"{payload} copy";
    }

    private sealed class RejectingDropOperation : ItemDropOperation
    {
        public string? Message { get; set; }

        protected override bool CanDrop(DragEventArgs e) => Message is null ? base.CanDrop(e) : Reject(Message);
    }
}
