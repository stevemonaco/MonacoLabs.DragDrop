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
