using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    public void TheDragInfoCarriesTheItemItsCollectionIndexAndContainer()
    {
        var items = new List<string> { "a", "b", "c" };
        var (window, list, target, drop) = Show(items);
        var container = list.ContainerFromIndex(2)!;

        window.MouseDrag(window.PointIn(container), window.PointIn(target));

        var info = Assert.IsType<CollectionDragInfo>(drop.LastDragInfo);
        Assert.Equal(new CollectionDragItem("c", items, 2, container), Assert.Single(info.Items));
        Assert.Same(container, info.PressedContainer);
        Assert.Equal(["c"], info.Payloads);
    }

    [AvaloniaFact]
    public void OnlyAMoveWithAPayloadCollectionRemovesTheOriginal()
    {
        var items = new List<string> { "a", "b" };
        var operation = new CollectionDragOperation();
        var dropInfo = new DropInfo { HoveredControl = new Border(), DragEventArgs = null! };

        foreach (var effect in new[] { DragDropEffects.Copy, DragDropEffects.Link, DragDropEffects.None })
            operation.DropCompleted(effect, Info(items), dropInfo);
        operation.DropCompleted(DragDropEffects.Move, Info(null), dropInfo);
        Assert.Equal(["a", "b"], items);

        operation.DropCompleted(DragDropEffects.Move, Info(items), dropInfo);
        Assert.Equal(["a"], items);

        CollectionDragInfo Info(List<string>? collection) =>
            new() { DragOperation = operation, DragIds = ["app:default"], Items = [new CollectionDragItem("b", collection, 1, null)] };
    }

    [AvaloniaFact]
    public void ThePayloadIsThePressedItemWhateverPayloadAndSelectionSay()
    {
        var (window, list, target, drop) = Show(["a", "b", "c"]);
        DragDrop.GetDragOperation(list)!.Payload = "fallback";
        list.SelectedIndex = 0;

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(2)!), window.PointIn(target));

        Assert.Equal("c", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void OnAControlThatIsNotAnItemsControlThePayloadResolvesAsUsual()
    {
        var (window, source, target, operation, drop) = ShowBorderSource();
        source.DataContext = "context";

        window.MouseDrag(window.PointIn(source), window.PointIn(target));
        Assert.Equal("context", drop.PayloadTarget);

        operation.Payload = "A";
        window.MouseDrag(window.PointIn(source), window.PointIn(target));
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void TheDragVisualIsThePressedContainer()
    {
        var (window, list, target, drop) = Show(["a", "b", "c"]);
        var container = list.ContainerFromIndex(1)!;
        var dragging = new List<bool>();
        drop.Probe = _ => dragging.AddRange([container.Classes.Contains(":dragging"), list.Classes.Contains(":dragging")]);

        window.MouseDrag(window.PointIn(container), window.PointIn(target));

        Assert.Equal([true, false], dragging.Take(2));
        Assert.DoesNotContain(":dragging", container.Classes);
    }

    [AvaloniaFact]
    public void OnAControlThatIsNotAnItemsControlTheDragVisualIsTheControl()
    {
        var (window, source, target, operation, drop) = ShowBorderSource();
        operation.Payload = "A";
        bool? dragging = null;
        drop.Probe = _ => dragging ??= source.Classes.Contains(":dragging");

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.True(dragging);
    }

    [AvaloniaFact]
    public void PressingAnUnselectedItemSelectsItInTheSameGesture()
    {
        var (window, list, target, drop) = Show(["a", "b", "c"], SelectionMode.Multiple);
        list.SelectedIndex = 0;

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(1)!), window.PointIn(target));

        Assert.Equal(["b"], list.SelectedItems!.Cast<string>());
        Assert.Equal("b", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void PressingASelectedItemKeepsTheMultipleSelection()
    {
        var (window, list, target, drop) = Show(["a", "b", "c"], SelectionMode.Multiple);
        ((CollectionDragOperation)DragDrop.GetDragOperation(list)!).DraggedItems = DraggedItems.PressedItem;
        list.Selection.Select(0);
        list.Selection.Select(2);

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(2)!), window.PointIn(target));

        Assert.Equal(["a", "c"], list.SelectedItems!.Cast<string>().Order());
        Assert.Equal("c", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void DraggingASelectedItemCarriesTheWholeSelectionInIndexOrder()
    {
        var items = new List<string> { "a", "b", "c", "d" };
        var (window, list, target, drop) = ShowSelection(items, 3, 1);

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(3)!), window.PointIn(target));

        var info = Assert.IsType<CollectionDragInfo>(drop.LastDragInfo);
        Assert.Equal(["b", "d"], info.Payloads);
        Assert.Equal(["b", "d"], drop.LastPayloads);
        Assert.Equal([new CollectionDragItem("b", items, 1, list.ContainerFromIndex(1)), new CollectionDragItem("d", items, 3, list.ContainerFromIndex(3))], info.Items);
        Assert.Same(list.ContainerFromIndex(3), info.PressedContainer);
    }

    [AvaloniaFact]
    public void DraggingAnUnselectedItemCarriesOnlyIt()
    {
        var (window, list, target, drop) = ShowSelection(["a", "b", "c"], 0, 2);

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(1)!), window.PointIn(target));

        Assert.Equal(["b"], drop.LastPayloads);
    }

    [AvaloniaFact]
    public void DraggedItemsPressedItemCarriesOnlyThePressedItem()
    {
        var (window, list, target, drop) = ShowSelection(["a", "b", "c"], 0, 2);
        ((CollectionDragOperation)DragDrop.GetDragOperation(list)!).DraggedItems = DraggedItems.PressedItem;

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(2)!), window.PointIn(target));

        Assert.Equal(["c"], drop.LastPayloads);
    }

    [AvaloniaFact]
    public void SelectedItemsScrolledOutOfViewAreCarried()
    {
        var items = Enumerable.Range(0, 40).Select(x => $"item {x}").ToList();
        var (window, list, target, drop) = ShowSelection(items);
        list.AutoScrollToSelectedItem = false;
        list.Selection.Select(0);
        list.Selection.Select(30);
        Assert.Null(list.ContainerFromIndex(30));

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(0)!), window.PointIn(target));

        var info = Assert.IsType<CollectionDragInfo>(drop.LastDragInfo);
        Assert.Equal(["item 0", "item 30"], info.Payloads);
        Assert.Equal(new CollectionDragItem("item 30", items, 30, null), info.Items[1]);
    }

    [AvaloniaFact]
    public void EveryRealizedDraggedContainerIsDragging()
    {
        var (window, list, target, drop) = ShowSelection(["a", "b", "c"], 0, 2);
        string[]? dragging = null;
        drop.Probe = _ => dragging ??= [.. list.GetRealizedContainers().Where(x => x.Classes.Contains(":dragging")).Select(x => (string)x.DataContext!)];

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(0)!), window.PointIn(target));

        Assert.NotNull(dragging);
        Assert.Equal(["a", "c"], dragging);
        Assert.DoesNotContain(list.GetRealizedContainers(), x => x.Classes.Contains(":dragging"));
    }

    [AvaloniaFact]
    public void DraggedItemsDefaultsToSelectedItems() => Assert.Equal(DraggedItems.SelectedItems, new CollectionDragOperation().DraggedItems);

    [AvaloniaFact]
    public void ADragFromAFilteredViewRecordsCollectionIndexes()
    {
        var items = new List<string> { "a", "b", "c", "d", "e" };
        var list = new ListBox
        {
            ItemsSource = new List<string> { "b", "d" },
            Width = 150,
            Height = 250,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 30 })
        };
        DragDrop.SetDragOperation(list, new CollectionDragOperation { PayloadCollection = items });
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var drop = new RecordingDropOperation();
        DragDrop.SetDropOperation(target, drop);
        var window = TestWindow.Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { list, target } });

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(1)!), window.PointIn(target));

        var item = Assert.Single(Assert.IsType<CollectionDragInfo>(drop.LastDragInfo).Items);
        Assert.Equal("d", item.Item);
        Assert.Same(items, item.Collection);
        Assert.Equal(3, item.Index);
    }

    [AvaloniaFact]
    public void ByDefaultTheSourceTunnelsAndIgnoresHandledPresses()
    {
        var operation = new CollectionDragOperation();

        Assert.Equal(RoutingStrategies.Tunnel, operation.Routing);
        Assert.False(operation.HandledEventsToo);
    }

    [AvaloniaTheory]
    [InlineData(null, null)]
    [InlineData(true, "b")]
    public void AnAncestorThatHandlesTunnelingPressesKeepsTheGesture(bool? handledEventsToo, string? expected)
    {
        var operation = new CollectionDragOperation();
        if (handledEventsToo is { } value)
            operation.HandledEventsToo = value;
        var items = new List<string> { "a", "b", "c" };
        var list = new ListBox
        {
            ItemsSource = items,
            Width = 150,
            Height = 250,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 30 })
        };
        operation.PayloadCollection = items;
        DragDrop.SetDragOperation(list, operation);
        var gestureHost = new Border { Child = list };
        gestureHost.AddHandler(InputElement.PointerPressedEvent, (_, e) => e.Handled = true, RoutingStrategies.Tunnel);

        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var drop = new RecordingDropOperation();
        DragDrop.SetDropOperation(target, drop);
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { gestureHost, target } };
        var window = TestWindow.Show(panel);

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(1)!), window.PointIn(target));

        Assert.Equal(expected, drop.PayloadTarget);
    }

    private static (Window, ListBox, Border, PayloadsRecordingDropOperation) ShowSelection(List<string> items, params int[] selected)
    {
        var (window, list, target, _) = Show(items, SelectionMode.Multiple);
        var drop = new PayloadsRecordingDropOperation();
        DragDrop.SetDropOperation(target, drop);
        foreach (var index in selected)
            list.Selection.Select(index);

        return (window, list, target, drop);
    }

    private static (Window, Border, Border, CollectionDragOperation, RecordingDropOperation) ShowBorderSource()
    {
        var source = new Border { Background = Brushes.Red, Width = 100, Height = 100 };
        var operation = new CollectionDragOperation();
        DragDrop.SetDragOperation(source, operation);
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var drop = new RecordingDropOperation();
        DragDrop.SetDropOperation(target, drop);
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { source, target } };
        return (TestWindow.Show(panel), source, target, operation, drop);
    }

    private static (Window, ListBox, Border, RecordingDropOperation) Show(List<string> items, SelectionMode selectionMode = SelectionMode.Single)
    {
        var list = new ListBox
        {
            SelectionMode = selectionMode,
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
