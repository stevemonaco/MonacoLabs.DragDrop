using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class CollectionDragTests
{
    [AvaloniaFact]
    public void PressesOnAListBoxItemReachTheOperation()
    {
        var (window, list, target, drop) = Show(["a", "b", "c"]);

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(1)!), window.PointIn(target));

        Assert.Equal("b", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void PressesOutsideAnItemContainerAreNotTracked()
    {
        var (window, list, target, drop) = Show(["a", "b", "c"]);
        DragDrop.GetDragOperation(list)!.Payload = "fallback";

        window.MouseDrag(window.PointIn(list, new Point(75, 240)), window.PointIn(target));

        Assert.Null(drop.PayloadTarget);
        Assert.Null(drop.LastDragInfo);
    }

    [AvaloniaFact]
    public void TheDragInfoCarriesTheCollectionTheContainerAndItsIndex()
    {
        var items = new List<string> { "a", "b", "c" };
        var (window, list, target, drop) = Show(items);
        var container = list.ContainerFromIndex(2)!;

        window.MouseDrag(window.PointIn(container), window.PointIn(target));

        var info = Assert.IsType<CollectionDragInfo>(drop.LastDragInfo);
        Assert.Same(items, info.PayloadCollection);
        Assert.Same(container, info.PayloadContainer);
        Assert.Equal(2, info.PayloadContainerIndex);
    }

    private static (Window, ListBox, Border, RecordingDropOperation) Show(List<string> items)
    {
        var list = new ListBox
        {
            ItemsSource = items,
            Width = 150,
            Height = 250,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 30 })
        };
        DragDrop.SetDragOperation(list, new CollectionDragOperation { PayloadCollection = items });

        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var drop = new RecordingDropOperation();
        DragDrop.SetDropOperation(target, drop);

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { list, target } };
        return (TestWindow.Show(panel), list, target, drop);
    }
}
