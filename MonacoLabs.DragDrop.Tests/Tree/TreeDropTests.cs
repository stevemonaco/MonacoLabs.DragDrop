using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class TreeDropTests
{
    [AvaloniaFact]
    public void DroppingANodeIntoItselfShowsTheSelfDropMessage()
    {
        var parent = new TreeNode("parent", new TreeNode("child"));
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

    private sealed class TreeNode(string name, params TreeNode[] children)
    {
        public string Name { get; } = name;
        public ObservableCollection<TreeNode> Children { get; } = [.. children];
    }
}
