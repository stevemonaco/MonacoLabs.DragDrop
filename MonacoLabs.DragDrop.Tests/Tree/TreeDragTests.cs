using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class TreeDragTests
{
    [AvaloniaFact]
    public void ThePayloadCollectionIsTheParentsItemsSource()
    {
        var child = new Node("child");
        var parent = new Node("parent", new Node("first"), child);
        var (window, tree, target, drop) = Show(new Node[] { parent });
        Expand(tree, parent);

        window.MouseDrag(window.PointIn(TreeDragOperation.FindHeader(Item(tree, child))), window.PointIn(target));

        var info = Assert.IsType<CollectionDragInfo>(drop.LastDragInfo);
        Assert.Same(parent.Children, info.PayloadCollection);
        Assert.Equal(1, info.PayloadContainerIndex);
        Assert.Same(child, drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void ThePressedContainerIsTheNearestTreeViewItemAtAnyDepth()
    {
        var grandchild = new Node("grandchild");
        var child = new Node("child", grandchild);
        var root = new Node("root", child);
        var (window, tree, target, drop) = Show(new Node[] { root });
        Expand(tree, root);
        Expand(tree, child);
        var item = Item(tree, grandchild);

        window.MouseDrag(window.PointIn(TreeDragOperation.FindHeader(item)), window.PointIn(target));

        var info = Assert.IsType<CollectionDragInfo>(drop.LastDragInfo);
        Assert.Same(item, info.PayloadContainer);
        Assert.Same(grandchild, drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void TheDragVisualIsTheNodesHeaderRow()
    {
        var child = new Node("child");
        var parent = new Node("parent", child);
        var (window, tree, target, drop) = Show(new Node[] { parent });
        Expand(tree, parent);
        var item = Item(tree, parent);
        var header = TreeDragOperation.FindHeader(item);
        var dragging = new List<bool>();
        drop.Probe = _ => dragging.AddRange([header.Classes.Contains(":dragging"), item.Classes.Contains(":dragging")]);

        window.MouseDrag(window.PointIn(header), window.PointIn(target));

        Assert.NotSame(item, header);
        Assert.Equal([true, false], dragging.Take(2));
    }

    [AvaloniaFact]
    public void WithoutAHeaderPartTheDragVisualIsTheWholeItem()
    {
        var node = new Node("node");
        var (window, tree, target, drop) = Show(new Node[] { node });
        tree.ItemContainerTheme = HeaderlessItemTheme();
        Dispatcher.UIThread.RunJobs();
        var item = Item(tree, node);
        bool? dragging = null;
        drop.Probe = _ => dragging ??= item.Classes.Contains(":dragging");

        window.MouseDrag(window.PointIn(item), window.PointIn(target));

        Assert.Same(item, TreeDragOperation.FindHeader(item));
        Assert.True(dragging);
    }

    private static (Window, TreeView, Border, RecordingDropOperation) Show(Node[] roots)
    {
        var tree = new TreeView
        {
            ItemsSource = new ObservableCollection<Node>(roots),
            Width = 200,
            Height = 250,
            ItemTemplate = new FuncTreeDataTemplate<Node>((_, _) => new TextBlock { Height = 24 }, n => n.Children)
        };
        DragDrop.SetDragOperation(tree, new TreeDragOperation());
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var drop = new RecordingDropOperation();
        DragDrop.SetDropOperation(target, drop);
        var window = TestWindow.Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { tree, target } });
        return (window, tree, target, drop);
    }

    private static TreeViewItem Item(TreeView tree, Node node) =>
        tree.GetVisualDescendants().OfType<TreeViewItem>().Single(x => ReferenceEquals(x.DataContext, node));

    private static void Expand(TreeView tree, Node node)
    {
        Item(tree, node).IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
    }

    private static ControlTheme HeaderlessItemTheme() => new(typeof(TreeViewItem))
    {
        Setters =
        {
            new Setter(Panel.BackgroundProperty, Brushes.Transparent),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TreeViewItem>((item, _) => new StackPanel
            {
                Background = Brushes.Transparent,
                Children =
                {
                    new ContentPresenter { Name = "PART_HeaderPresenter", Height = 24, [!ContentPresenter.ContentProperty] = item[!HeaderedItemsControl.HeaderProperty] },
                    new ItemsPresenter { Name = "PART_ItemsPresenter" }
                }
            }))
        }
    };

    private sealed class Node(string name, params Node[] children)
    {
        public string Name { get; } = name;
        public ObservableCollection<Node> Children { get; } = [.. children];
    }
}
