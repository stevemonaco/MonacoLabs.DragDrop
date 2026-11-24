using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class TreeDragTests
{
    [AvaloniaFact]
    public void ThePayloadCollectionIsTheParentsItemsSource()
    {
        var child = new Node("child");
        var parent = new Node("parent", new Node("first"), child);
        var tree = new TreeView
        {
            ItemsSource = new ObservableCollection<Node> { parent },
            Width = 200,
            Height = 250,
            ItemTemplate = new FuncTreeDataTemplate<Node>((_, _) => new TextBlock { Height = 24 }, n => n.Children)
        };
        DragDrop.SetDragOperation(tree, new TreeDragOperation());
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var drop = new RecordingDropOperation();
        DragDrop.SetDropOperation(target, drop);
        var window = TestWindow.Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { tree, target } });

        var parentItem = (TreeViewItem)tree.ContainerFromIndex(0)!;
        parentItem.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        var childItem = (TreeViewItem)parentItem.ContainerFromIndex(1)!;

        window.MouseDrag(window.PointIn(TreeDragOperation.FindHeader(childItem)), window.PointIn(target));

        var info = Assert.IsType<CollectionDragInfo>(drop.LastDragInfo);
        Assert.Same(parent.Children, info.PayloadCollection);
        Assert.Equal(1, info.PayloadContainerIndex);
        Assert.Same(child, drop.PayloadTarget);
    }

    private sealed class Node(string name, params Node[] children)
    {
        public string Name { get; } = name;
        public ObservableCollection<Node> Children { get; } = [.. children];
    }
}
