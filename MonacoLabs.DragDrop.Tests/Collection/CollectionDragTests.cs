using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
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
            new() { DragOperation = operation, DragIds = ["app:default"], PayloadCollection = collection, PayloadContainerIndex = 1, Payload = "b" };
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
        list.Selection.Select(0);
        list.Selection.Select(2);

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(2)!), window.PointIn(target));

        Assert.Equal(["a", "c"], list.SelectedItems!.Cast<string>().Order());
        Assert.Equal("c", drop.PayloadTarget);
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
