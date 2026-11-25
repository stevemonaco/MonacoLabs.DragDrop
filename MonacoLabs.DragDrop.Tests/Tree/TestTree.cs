using System.Collections;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace MonacoLabs.DragDrop.Tests;

public sealed class TreeNode(string name, IEnumerable<TreeNode>? children = null)
{
    public string Name => name;

    public IEnumerable<TreeNode> Children { get; } = children ?? new ObservableCollection<TreeNode>();

    public IList<TreeNode> ChildList => (IList<TreeNode>)Children;

    public override string ToString() => name;
}

/// <summary>
/// A <see cref="TreeView"/> with fixed-height rows in a window: root A (children A1, A2), root B (child B1) and root L, whose children are not
/// an <see cref="IList"/>. Every node starts collapsed.
/// </summary>
public sealed class TestTree
{
    public TreeNode A1 { get; } = new("A1");
    public TreeNode A2 { get; } = new("A2");
    public TreeNode A { get; }
    public TreeNode B1 { get; } = new("B1");
    public TreeNode B { get; }
    public TreeNode L { get; } = new("L", Enumerate());
    public IEnumerable Roots { get; }
    public TreeView View { get; }
    public Window Window { get; }

    public TestTree(CollectionDropOperation operation, bool rootsAreAList = true)
    {
        A = new("A", new ObservableCollection<TreeNode> { A1, A2 });
        B = new("B", new ObservableCollection<TreeNode> { B1 });
        Roots = rootsAreAList ? new ObservableCollection<TreeNode> { A, B, L } : Enumerate(A, B, L);
        View = new TreeView
        {
            ItemsSource = Roots,
            Width = 200,
            Height = 300,
            ItemTemplate = new FuncTreeDataTemplate<TreeNode>((node, _) => new TextBlock { Height = 24, Text = node.Name }, node => node.Children)
        };
        DragDrop.SetDropOperation(View, operation);
        Window = TestWindow.Show(View);
    }

    public IList<TreeNode> RootList => (IList<TreeNode>)Roots;

    public TreeViewItem Item(TreeNode node) => View.GetVisualDescendants().OfType<TreeViewItem>().Single(x => ReferenceEquals(x.DataContext, node));

    public Control Header(TreeNode node) => TreeDragOperation.FindHeader(Item(node));

    public void Expand(TreeNode node)
    {
        Item(node).IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// A point in the middle of <paramref name="node"/>'s header row, at <paramref name="height"/> (0 to 1) of its height.
    /// </summary>
    public Point At(TreeNode node, double height)
    {
        var header = Header(node);
        return Window.PointIn(header, new Point(header.Bounds.Width / 2, header.Bounds.Height * height));
    }

    public Point BelowTheNodes => Window.PointIn(View, new Point(View.Bounds.Width / 2, View.Bounds.Height - 50));

    private static IEnumerable<TreeNode> Enumerate(params TreeNode[] nodes)
    {
        foreach (var node in nodes)
            yield return node;
    }
}
