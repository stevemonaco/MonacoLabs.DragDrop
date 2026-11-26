using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class TreeDropTests
{
    [AvaloniaFact]
    public void TheHoveredContainerIsTheNearestTreeViewItem()
    {
        var tree = new TestTree(new TreeDropOperation());
        tree.Expand(tree.A);
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.A1, 0.5));

        Assert.Contains(":dropover", tree.Item(tree.A1).Classes);
        Assert.DoesNotContain(":dropover", tree.Item(tree.A).Classes);
    }

    [AvaloniaFact]
    public void ZonesAndTheInsertionLineUseTheHeaderNotTheWholeItem()
    {
        var operation = new TreeDropOperation();
        var tree = new TestTree(operation);
        tree.Expand(tree.A);
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.A, 0.9));

        Assert.True(tree.Item(tree.A).Bounds.Height > 2 * tree.Header(tree.A).Bounds.Height);
        Assert.Contains(":dropbottom", tree.Item(tree.A).Classes);
        Assert.Same(tree.Header(tree.A), operation.ItemDropAdorner!.TargetControl);
    }

    [AvaloniaTheory]
    [InlineData(0.1, ":droptop")]
    [InlineData(0.5, ":dropinside")]
    [InlineData(0.9, ":dropbottom")]
    public void ANodeWithAListOfChildrenHasBeforeInsideAndAfterZones(double height, string pseudoClass)
    {
        var tree = new TestTree(new TreeDropOperation { HoverExpandDelay = TimeSpan.Zero });
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.B, height));

        Assert.Equal(new[] { ":dropover", pseudoClass }.Order(), tree.Item(tree.B).Classes.Where(c => c.StartsWith(":drop")).Order());
    }

    [AvaloniaTheory]
    [InlineData(0.4, ":droptop")]
    [InlineData(0.6, ":dropbottom")]
    public void ANodeWithoutAListOfChildrenHasBeforeAndAfterHalves(double height, string pseudoClass)
    {
        var tree = new TestTree(new TreeDropOperation());
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        drag.Enter(tree.At(tree.L, height));

        Assert.Equal(new[] { ":dropover", pseudoClass }.Order(), tree.Item(tree.L).Classes.Where(c => c.StartsWith(":drop")).Order());
    }

    [AvaloniaFact]
    public void DroppingInsideANodeAppendsToItsChildren()
    {
        var tree = new TestTree(new TreeDropOperation());
        var x = new TreeNode("X");

        var info = DropOn(tree, tree.At(tree.B, 0.5), x);

        Assert.Equal([tree.B1, x], tree.B.Children);
        Assert.Same(tree.B, info.TargetParent);
        Assert.Equal(1, info.TargetIndex);
    }

    [AvaloniaFact]
    public void DroppingAfterAnExpandedNodeInsertsAsItsFirstChild()
    {
        var tree = new TestTree(new TreeDropOperation());
        tree.Expand(tree.A);
        var x = new TreeNode("X");

        var info = DropOn(tree, tree.At(tree.A, 0.9), x);

        Assert.Equal([x, tree.A1, tree.A2], tree.A.Children);
        Assert.Same(tree.A, info.TargetParent);
    }

    [AvaloniaFact]
    public void DroppingBeforeAChildInsertsAmongItsSiblings()
    {
        var tree = new TestTree(new TreeDropOperation());
        tree.Expand(tree.A);
        var x = new TreeNode("X");

        var info = DropOn(tree, tree.At(tree.A2, 0.1), x);

        Assert.Equal([tree.A1, x, tree.A2], tree.A.Children);
        Assert.Same(tree.A, info.TargetParent);
        Assert.Equal(1, info.TargetIndex);
    }

    [AvaloniaFact]
    public void DroppingAfterACollapsedRootInsertsAmongTheRoots()
    {
        var tree = new TestTree(new TreeDropOperation());
        var x = new TreeNode("X");

        var info = DropOn(tree, tree.At(tree.B, 0.9), x);

        Assert.Equal([tree.A, tree.B, x, tree.L], tree.RootList);
        Assert.Null(info.TargetParent);
        Assert.Equal(2, info.TargetIndex);
    }

    [AvaloniaFact]
    public void DroppingBelowTheNodesAppendsToTheRoots()
    {
        var tree = new TestTree(new TreeDropOperation());
        var x = new TreeNode("X");

        var info = DropOn(tree, tree.BelowTheNodes, x);

        Assert.Equal([tree.A, tree.B, tree.L, x], tree.RootList);
        Assert.Null(info.TargetParent);
    }

    [AvaloniaFact]
    public void TheTreeAlwaysTransfersAndIgnoresPayloadTarget()
    {
        var unused = new List<TreeNode>();
        var tree = new TestTree(new TreeDropOperation { PayloadTarget = unused });
        var x = new TreeNode("X");

        DropOn(tree, tree.BelowTheNodes, x);

        Assert.Equal([tree.A, tree.B, tree.L, x], tree.RootList);
        Assert.Empty(unused);
    }

    [AvaloniaFact]
    public void WithoutALocationTheDropIsRejected()
    {
        var tree = new TestTree(new TreeDropOperation(), rootsAreAList: false);
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), new TreeNode("X")));

        Assert.Equal(DragDropEffects.None, drag.Enter(tree.BelowTheNodes));
        Assert.Equal(DragDropEffects.None, drag.Over(tree.At(tree.B, 0.1)));
        Assert.Equal(DragDropEffects.Move, drag.Over(tree.At(tree.B, 0.5)));
    }

    [AvaloniaFact]
    public void DroppingANodeIntoItselfShowsTheSelfDropMessage()
    {
        var parent = new TreeNode("parent", new ObservableCollection<TreeNode> { new("child") });
        var tree = new TreeView
        {
            ItemsSource = new ObservableCollection<TreeNode> { parent },
            Width = 200,
            Height = 250,
            ItemTemplate = new FuncTreeDataTemplate<TreeNode>((_, _) => new TextBlock { Height = 24 }, n => n.Children)
        };
        var drop = new TreeDropOperation();
        DragDrop.SetDropOperation(tree, drop);
        var window = TestWindow.Show(tree);
        var parentItem = (TreeViewItem)tree.ContainerFromIndex(0)!;
        parentItem.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        var childHeader = TreeDragOperation.FindHeader((TreeViewItem)parentItem.ContainerFromIndex(0)!);
        using var drag = window.SimulateDrag(DragData.For(new TreeDragOperation(), parent));

        Assert.Equal(DragDropEffects.None, drag.Enter(window.PointIn(childHeader)));

        Assert.Equal(drop.SelfDropMessage, drop.ErrorMessage);
        Assert.Equal(drop.SelfDropMessage, drop.DropAdorner!.ErrorMessage);
    }

    [AvaloniaFact]
    public void ALocationInsideAnyDraggedNodeIsRefused()
    {
        var operation = new TreeDropOperation();
        var tree = new TestTree(operation);
        tree.Expand(tree.B);
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), [tree.A, tree.B]));

        Assert.Equal(DragDropEffects.None, drag.Enter(tree.At(tree.A, 0.5)));
        Assert.Equal(operation.SelfDropMessage, operation.ErrorMessage);
        Assert.Equal(DragDropEffects.None, drag.Over(tree.At(tree.B1, 0.1)));
        Assert.Equal(operation.SelfDropMessage, operation.ErrorMessage);
        Assert.Equal(DragDropEffects.Move, drag.Over(tree.At(tree.L, 0.75)));
    }

    [AvaloniaFact]
    public void ACanDropOverrideCannotAllowANodeIntoItself()
    {
        var operation = new AcceptingTreeDropOperation();
        var tree = new TestTree(operation);
        tree.Expand(tree.A);
        using var drag = tree.Window.SimulateDrag(DragData.For(new TreeDragOperation(), tree.A));

        Assert.Equal(DragDropEffects.None, drag.Enter(tree.At(tree.A1, 0.5)));
        Assert.Equal(operation.SelfDropMessage, operation.ErrorMessage);
        Assert.Equal(DragDropEffects.Move, drag.Over(tree.At(tree.B, 0.5)));
    }

    [AvaloniaFact]
    public void SelfDropMessageHasADefault() =>
        Assert.Equal("A node can't be moved into itself", new TreeDropOperation().SelfDropMessage);

    private static CollectionDropInfo DropOn(TestTree tree, Point point, TreeNode payload)
    {
        var source = new RecordingTreeDragOperation();
        using var drag = tree.Window.SimulateDrag(DragData.For(source, payload));
        drag.Enter(point);
        drag.Drop(point);
        return Assert.IsType<CollectionDropInfo>(Assert.Single(source.Completions));
    }

    private sealed class RecordingTreeDragOperation : TreeDragOperation
    {
        public List<DropInfo> Completions { get; } = [];

        public override void DropCompleted(DragDropEffects effect, DragInfo dragInfo, DropInfo dropInfo)
        {
            Completions.Add(dropInfo);
            base.DropCompleted(effect, dragInfo, dropInfo);
        }
    }

    private sealed class AcceptingTreeDropOperation : TreeDropOperation
    {
        protected override bool CanDrop(DragEventArgs e) => true;
    }
}
