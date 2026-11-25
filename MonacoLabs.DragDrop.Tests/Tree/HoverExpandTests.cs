using Avalonia.Headless.XUnit;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class HoverExpandTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(150);

    [AvaloniaFact]
    public async Task HoveringTheMiddleOfACollapsedNodeExpandsItAfterTheDelay()
    {
        var tree = new TestTree(new TreeDropOperation { HoverExpandDelay = Delay });
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.A, 0.5));
        await Task.Delay(50);
        Assert.False(tree.Item(tree.A).IsExpanded);

        await TestDispatcher.PumpUntil(() => tree.Item(tree.A).IsExpanded);
    }

    [AvaloniaFact]
    public async Task ANodeWithoutChildrenIsNotExpanded()
    {
        var tree = new TestTree(new TreeDropOperation { HoverExpandDelay = Delay });
        tree.B.ChildList.Clear();
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.B, 0.5));
        await Task.Delay(Delay * 3);

        Assert.False(tree.Item(tree.B).IsExpanded);
    }

    [AvaloniaFact]
    public async Task MovingToAnotherZoneCancelsThePendingExpand()
    {
        var tree = new TestTree(new TreeDropOperation { HoverExpandDelay = Delay });
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.A, 0.5));
        drag.Over(tree.At(tree.A, 0.1));
        await Task.Delay(Delay * 3);

        Assert.False(tree.Item(tree.A).IsExpanded);
    }

    [AvaloniaFact]
    public async Task MovingToAnotherNodeCancelsThePendingExpand()
    {
        var tree = new TestTree(new TreeDropOperation { HoverExpandDelay = Delay });
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.A, 0.5));
        drag.Over(tree.At(tree.L, 0.5));
        await Task.Delay(Delay * 3);

        Assert.False(tree.Item(tree.A).IsExpanded);
    }

    [AvaloniaFact]
    public async Task LeavingCancelsThePendingExpand()
    {
        var tree = new TestTree(new TreeDropOperation { HoverExpandDelay = Delay });
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.A, 0.5));
        drag.Leave(tree.At(tree.A, 0.5));
        await Task.Delay(Delay * 3);

        Assert.False(tree.Item(tree.A).IsExpanded);
    }

    [AvaloniaFact]
    public async Task AZeroDelayNeverExpands()
    {
        var tree = new TestTree(new TreeDropOperation { HoverExpandDelay = TimeSpan.Zero });
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.A, 0.5));
        await Task.Delay(Delay * 2);

        Assert.False(tree.Item(tree.A).IsExpanded);
    }

    [AvaloniaFact]
    public void TheDelayDefaultsTo700Milliseconds() =>
        Assert.Equal(TimeSpan.FromMilliseconds(700), new TreeDropOperation().HoverExpandDelay);
}
