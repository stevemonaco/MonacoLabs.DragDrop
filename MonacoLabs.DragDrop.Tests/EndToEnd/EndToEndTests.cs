using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class EndToEndTests
{
    [AvaloniaFact]
    public void CopyingWithAnOverriddenCreateCopyLeavesTheSourceUnchanged()
    {
        var original = new Card("first");
        var sourceItems = new ObservableCollection<Card> { original, new("second") };
        var targetItems = new ObservableCollection<Card>();
        var source = CardList(sourceItems);
        var target = CardList(targetItems);
        DragDrop.SetDragOperation(source, new CollectionDragOperation { PayloadCollection = sourceItems, AllowedEffects = DragDropEffects.Copy | DragDropEffects.Move });
        DragDrop.SetDropOperation(target, new CardCopyingDropOperation { PayloadTarget = targetItems });
        var window = TestWindow.Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, Children = { source, target } });

        window.MouseDrag(window.PointIn(source.ContainerFromIndex(0)!), window.PointIn(target), RawInputModifiers.Control);

        Assert.Equal([original, sourceItems[1]], sourceItems);
        var copy = Assert.Single(targetItems);
        Assert.NotSame(original, copy);
        Assert.Equal("first copy", copy.Name);
    }

    [AvaloniaFact]
    public void MovingAListItemOntoASingleValueTargetRemovesItFromTheList()
    {
        var moved = new Card("second");
        var items = new ObservableCollection<Card> { new("first"), moved, new("third") };
        var source = CardList(items);
        DragDrop.SetDragOperation(source, new CollectionDragOperation { PayloadCollection = items });
        var target = new Border { Width = 100, Height = 100, Background = Avalonia.Media.Brushes.Blue };
        var drop = new ItemDropOperation();
        DragDrop.SetDropOperation(target, drop);
        var window = TestWindow.Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, Children = { source, target } });

        window.MouseDrag(window.PointIn(source.ContainerFromIndex(1)!), window.PointIn(target));

        Assert.Same(moved, drop.PayloadTarget);
        Assert.DoesNotContain(moved, items);
        Assert.Equal(2, items.Count);
    }

    [AvaloniaFact]
    public void ReorderingThroughACommandRemovesTheOriginal()
    {
        var first = new Card("first");
        var items = new ObservableCollection<Card> { first, new("second"), new("third") };
        var list = CardList(items);
        DragDrop.SetDragOperation(list, new CollectionDragOperation { PayloadCollection = items });
        DragDrop.SetDropOperation(list, new CollectionDropOperation
        {
            PayloadCommand = new RelayCommand(a =>
            {
                var args = (DropEventArgs)a!;
                args.Location!.Collection.Insert(args.Location.Index ?? args.Location.Collection.Count, args.Items[0]);
            })
        });
        var window = TestWindow.Show(list);
        var last = list.ContainerFromIndex(2)!;

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(0)!), window.PointIn(last, new Avalonia.Point(10, last.Bounds.Height - 3)));

        Assert.Equal(["second", "third", "first"], items.Select(x => x.Name));
    }

    private static ListBox CardList(ObservableCollection<Card> items) => new()
    {
        ItemsSource = items,
        Width = 150,
        Height = 200,
        ItemTemplate = new FuncDataTemplate<Card>((_, _) => new TextBlock { Height = 30 })
    };

    private sealed record Card(string Name);

    private sealed class CardCopyingDropOperation : CollectionDropOperation
    {
        protected override object CreateCopy(object payload) => payload is Card card ? new Card($"{card.Name} copy") : payload;
    }
}
