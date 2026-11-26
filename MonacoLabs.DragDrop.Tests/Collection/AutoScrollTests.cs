using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class AutoScrollTests
{
    [AvaloniaFact]
    public async Task HoveringNearAnEdgeScrollsTheListTowardsIt()
    {
        var (window, list, viewer) = ShowLongList(new CollectionDropOperation());
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(window.PointIn(list, new Point(10, list.Bounds.Height - 5)));
        await TestDispatcher.PumpUntil(() => viewer.Offset.Y > 60);

        drag.Over(window.PointIn(list, new Point(10, 5)));
        await TestDispatcher.PumpUntil(() => viewer.Offset.Y == 0);
    }

    [AvaloniaFact]
    public async Task AnAttachedScrollViewerScrollsItself()
    {
        var viewer = new ScrollViewer
        {
            Width = 150,
            Height = 100,
            Content = new Border { Height = 1000, Background = Brushes.Blue },
            VerticalAlignment = VerticalAlignment.Top
        };
        DragDrop.SetDropOperation(viewer, new CollectionDropOperation { PayloadTarget = new List<string>() });
        var window = TestWindow.Show(viewer);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(window.PointIn(viewer, new Point(10, 95)));

        await TestDispatcher.PumpUntil(() => viewer.Offset.Y > 0);
    }

    [AvaloniaFact]
    public async Task OutsideTheMarginsTheListDoesNotScroll()
    {
        var (window, list, viewer) = ShowLongList(new CollectionDropOperation());
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(window.PointIn(list, new Point(10, list.Bounds.Height / 2)));
        await Task.Delay(100);

        Assert.Equal(0, viewer.Offset.Y);
    }

    [AvaloniaFact]
    public async Task LeavingStopsTheScrolling()
    {
        var (window, list, viewer) = ShowLongList(new CollectionDropOperation());
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));
        drag.Enter(window.PointIn(list, new Point(10, list.Bounds.Height - 5)));
        await TestDispatcher.PumpUntil(() => viewer.Offset.Y > 0);

        drag.Leave(window.PointIn(list, new Point(10, list.Bounds.Height - 5)));
        var offset = viewer.Offset.Y;
        await Task.Delay(100);

        Assert.Equal(offset, viewer.Offset.Y);
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task AMarginOfZeroOrLessNeverScrolls(double margin)
    {
        var (window, list, viewer) = ShowLongList(new CollectionDropOperation { AutoScrollMargin = margin });
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(window.PointIn(list, new Point(10, list.Bounds.Height - 2)));
        await Task.Delay(100);

        Assert.Equal(0, viewer.Offset.Y);
    }

    [AvaloniaFact]
    public async Task AScrollViewerReplacedBetweenDragsIsTheOneScrolled()
    {
        var host = new Decorator { Width = 150, Height = 100, VerticalAlignment = VerticalAlignment.Top, Child = TallViewer() };
        DragDrop.SetDropOperation(host, new CollectionDropOperation { PayloadTarget = new List<string>() });
        var window = TestWindow.Show(host);
        var nearBottom = window.PointIn(host, new Point(10, 95));
        await ScrollWithOneDrag(window, nearBottom, (ScrollViewer)host.Child);

        var replacement = TallViewer();
        host.Child = replacement;
        window.UpdateLayout();

        await ScrollWithOneDrag(window, nearBottom, replacement);
    }

    [AvaloniaFact]
    public async Task AnOperationMovedToAnotherControlScrollsThatControlsViewer()
    {
        var first = TallViewer();
        var second = TallViewer();
        var operation = new CollectionDropOperation { PayloadTarget = new List<string>() };
        DragDrop.SetDropOperation(first, operation);
        var window = TestWindow.Show(new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, Children = { first, second } });
        await ScrollWithOneDrag(window, window.PointIn(first, new Point(10, 95)), first);

        DragDrop.SetDropOperation(first, null);
        DragDrop.SetDropOperation(second, operation);

        await ScrollWithOneDrag(window, window.PointIn(second, new Point(10, 95)), second);
    }

    [AvaloniaFact]
    public void TheMarginDefaultsTo28Dips() => Assert.Equal(28, new CollectionDropOperation().AutoScrollMargin);

    private static ScrollViewer TallViewer() => new()
    {
        Width = 150,
        Height = 100,
        Content = new Border { Height = 1000, Background = Brushes.Blue },
        VerticalAlignment = VerticalAlignment.Top
    };

    private static async Task ScrollWithOneDrag(Window window, Point nearEdge, ScrollViewer expected)
    {
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));
        drag.Enter(nearEdge);
        await TestDispatcher.PumpUntil(() => expected.Offset.Y > 0);
        drag.Leave(nearEdge);
    }

    private static (Window, ListBox, ScrollViewer) ShowLongList(CollectionDropOperation operation)
    {
        var items = Enumerable.Range(0, 40).Select(i => $"item {i}").ToList();
        operation.PayloadTarget = items;
        var list = new ListBox
        {
            ItemsSource = items,
            Width = 150,
            Height = 150,
            VerticalAlignment = VerticalAlignment.Top,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 30 })
        };
        DragDrop.SetDropOperation(list, operation);
        var window = TestWindow.Show(list);
        return (window, list, list.GetVisualDescendants().OfType<ScrollViewer>().First());
    }
}
