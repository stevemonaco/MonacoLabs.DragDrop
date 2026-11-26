using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MonacoLabs.DragDrop.Abstractions;
using System.Collections.ObjectModel;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class EndToEndTests
{
    private static readonly Point OnBackground = new(350, 250);

    [AvaloniaFact]
    public void CopyingWithAnOverriddenCreateCopyLeavesTheSourceUnchanged()
    {
        var original = new Card("first");
        var sourceItems = new ObservableCollection<Card> { original, new("second") };
        var targetItems = new ObservableCollection<Card>();
        var source = CardList(sourceItems);
        var target = CardList(targetItems);
        DragDrop.SetDragOperation(source, new CollectionDragOperation { SourceCollection = sourceItems, AllowedEffects = DragDropEffects.Copy | DragDropEffects.Move });
        DragDrop.SetDropOperation(target, new CardCopyingDropOperation { TargetCollection = targetItems });
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
        DragDrop.SetDragOperation(source, new CollectionDragOperation { SourceCollection = items });
        var target = new Border { Width = 100, Height = 100, Background = Brushes.Blue };
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
        DragDrop.SetDragOperation(list, new CollectionDragOperation { SourceCollection = items });
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

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(0)!), window.PointIn(last, new Point(10, last.Bounds.Height - 3)));

        Assert.Equal(["second", "third", "first"], items.Select(x => x.Name));
    }

    [AvaloniaTheory]
    [InlineData(0, new[] { "b", "d", "a", "c", "e" })]
    [InlineData(4, new[] { "a", "c", "e", "b", "d" })]
    public void MovingASelectionWithinOneListKeepsItsOrder(int targetIndex, string[] expected)
    {
        var items = new ObservableCollection<Card>(new[] { "a", "b", "c", "d", "e" }.Select(x => new Card(x)));
        var list = CardList(items);
        list.SelectionMode = SelectionMode.Multiple;
        list.Height = 300;
        DragDrop.SetDragOperation(list, new CollectionDragOperation { SourceCollection = items });
        DragDrop.SetDropOperation(list, new CollectionDropOperation { TargetCollection = items });
        var window = TestWindow.Show(list);
        list.Selection.Select(3);
        list.Selection.Select(1);
        var target = list.ContainerFromIndex(targetIndex)!;
        var point = targetIndex == 0 ? new Point(10, 3) : new Point(10, target.Bounds.Height - 3);

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(3)!), window.PointIn(target, point));

        Assert.Equal(expected, items.Select(x => x.Name));
    }

    [AvaloniaFact]
    public void MovingASelectionToAnotherListRemovesEveryOriginal()
    {
        var sourceItems = new ObservableCollection<Card> { new("a"), new("b"), new("c") };
        var targetItems = new ObservableCollection<Card> { new("x") };
        var source = CardList(sourceItems);
        source.SelectionMode = SelectionMode.Multiple;
        var target = CardList(targetItems);
        DragDrop.SetDragOperation(source, new CollectionDragOperation { SourceCollection = sourceItems });
        DragDrop.SetDropOperation(target, new CollectionDropOperation { TargetCollection = targetItems });
        var window = TestWindow.Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, Children = { source, target } });
        source.Selection.Select(0);
        source.Selection.Select(2);

        window.MouseDrag(window.PointIn(source.ContainerFromIndex(2)!), window.PointIn(target, new Point(10, 150)));

        Assert.Equal(["b"], sourceItems.Select(x => x.Name));
        Assert.Equal(["x", "a", "c"], targetItems.Select(x => x.Name));
    }

    [AvaloniaTheory]
    [InlineData(1, 0, false, "a,d,b,c,e")]
    [InlineData(0, 1, true, "a,c,d,b,e")]
    public void ReorderingWithinAFilteredListRemovesTheOriginalNotTheCopy(int from, int onto, bool after, string expected)
    {
        var items = new ObservableCollection<Card> { new("a"), new("b"), new("c"), new("d"), new("e") };
        var list = CardList(new ObservableCollection<Card> { items[1], items[3] });
        DragDrop.SetDragOperation(list, new CollectionDragOperation { SourceCollection = items });
        DragDrop.SetDropOperation(list, new CollectionDropOperation { TargetCollection = items });
        var window = TestWindow.Show(list);
        var target = list.ContainerFromIndex(onto)!;

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(from)!), window.PointIn(target, new Point(10, target.Bounds.Height * (after ? 0.75 : 0.25))));

        Assert.Equal(expected, string.Join(",", items.Select(x => x.Name)));
    }

    [AvaloniaFact]
    public void MovingASelectionOutOfAFilteredListRemovesEveryOriginal()
    {
        var items = new ObservableCollection<Card> { new("x"), new("a"), new("x"), new("b"), new("x") };
        var targetItems = new ObservableCollection<Card>();
        var source = CardList(new ObservableCollection<Card> { items[0], items[2], items[4] });
        source.SelectionMode = SelectionMode.Multiple;
        var target = CardList(targetItems);
        DragDrop.SetDragOperation(source, new CollectionDragOperation { SourceCollection = items });
        DragDrop.SetDropOperation(target, new CollectionDropOperation { TargetCollection = targetItems });
        var window = TestWindow.Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, Children = { source, target } });
        source.Selection.Select(1);
        source.Selection.Select(2);

        window.MouseDrag(window.PointIn(source.ContainerFromIndex(2)!), window.PointIn(target, new Point(10, 150)));

        Assert.Equal(["x", "a", "b"], items.Select(x => x.Name));
        Assert.Equal(["x", "x"], targetItems.Select(x => x.Name));
    }

    [AvaloniaFact]
    public void MovingNodesFromSeveralParentsRemovesEachFromItsParent()
    {
        var tree = new TestTree(new TreeDropOperation());
        tree.View.SelectionMode = SelectionMode.Multiple;
        DragDrop.SetDragOperation(tree.View, new TreeDragOperation());
        tree.Expand(tree.A);
        tree.Expand(tree.B);
        tree.View.SelectedItems.Add(tree.B1);
        tree.View.SelectedItems.Add(tree.A1);

        tree.Window.MouseDrag(tree.At(tree.B1, 0.5), tree.BelowTheNodes);

        Assert.Equal([tree.A, tree.B, tree.L, tree.A1, tree.B1], tree.RootList);
        Assert.Equal([tree.A2], tree.A.ChildList);
        Assert.Empty(tree.B.ChildList);
    }

    [AvaloniaFact]
    public void DroppingAListItemWhereNothingAcceptsItRemovesItWithRemoveOnDropOutside()
    {
        var dropped = new Card("second");
        var items = new ObservableCollection<Card> { new("first"), dropped, new("third") };
        var list = CardList(items);
        DragDrop.SetDragOperation(list, new CollectionDragOperation { SourceCollection = items, RemoveOnDropOutside = true });
        var window = TestWindow.Show(new StackPanel { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, Children = { list } });

        window.MouseDrag(window.PointIn(list.ContainerFromIndex(1)!), OnBackground);

        Assert.Equal(["first", "third"], items.Select(x => x.Name));
    }

    [AvaloniaFact]
    public void TheBaseDragSourceKeepsItsPayloadAfterAMove()
    {
        var drag = new PlainDragOperation { Payload = "A" };
        var drop = new ItemDropOperation();
        var (window, source, target) = Slots(drag, drop);

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Equal("A", drop.PayloadTarget);
        Assert.Equal("A", drag.Payload);
    }

    [AvaloniaFact]
    public void ADropOutsideAnyTargetPassesTheDragToOnDroppedOutside()
    {
        var drag = new OutsideRecordingDragOperation { Payload = "A", RemoveOnDropOutside = true };
        var (window, source, _) = Slots(drag, new ItemDropOperation());

        window.MouseDrag(window.PointIn(source), OnBackground);

        var info = Assert.Single(drag.Outside);
        Assert.Equal(["A"], info.Payloads);
        Assert.Same(drag, info.DragOperation);
    }

    [Fact]
    public void ItemPayloadBindsTwoWayByDefault() =>
        Assert.Equal(BindingMode.TwoWay, DragOperationBase.PayloadProperty.GetMetadata(typeof(ItemDragOperation)).DefaultBindingMode);

    [AvaloniaFact]
    public void AMoveClearsASetPayload()
    {
        var drag = new ItemDragOperation { Payload = "A" };
        var drop = new ItemDropOperation { PayloadTarget = "B" };
        var (window, source, target) = Slots(drag, drop);

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Null(drag.Payload);
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void WithSwapOnDropAMoveTakesTheTargetsPreviousValue()
    {
        var drag = new ItemDragOperation { Payload = "A", SwapOnDrop = true };
        var drop = new ItemDropOperation { PayloadTarget = "B" };
        var (window, source, target) = Slots(drag, drop);

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Equal("B", drag.Payload);
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaTheory]
    [InlineData(RawInputModifiers.Control)]
    [InlineData(RawInputModifiers.Control | RawInputModifiers.Shift)]
    public void AnyEffectOtherThanMoveLeavesThePayload(RawInputModifiers modifiers)
    {
        var drag = new ItemDragOperation { Payload = "A", AllowedEffects = SimulatedDrag.AllEffects, SwapOnDrop = true };
        var drop = new ItemDropOperation { PayloadTarget = "B" };
        var (window, source, target) = Slots(drag, drop);

        window.MouseDrag(window.PointIn(source), window.PointIn(target), modifiers);

        Assert.Equal("A", drag.Payload);
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADataContextPayloadIsLeftAlone(bool dropOutside)
    {
        var drag = new ItemDragOperation { RemoveOnDropOutside = true };
        var drop = new ItemDropOperation();
        var (window, source, target) = Slots(drag, drop);
        source.DataContext = "context";

        window.MouseDrag(window.PointIn(source), dropOutside ? OnBackground : window.PointIn(target));

        Assert.Equal("context", source.DataContext);
        Assert.False(drag.IsSet(DragOperationBase.PayloadProperty));
    }

    [AvaloniaFact]
    public void ADropOutsideAnyTargetClearsASetPayload()
    {
        var drag = new ItemDragOperation { Payload = "A", RemoveOnDropOutside = true };
        var (window, source, _) = Slots(drag, new ItemDropOperation());

        window.MouseDrag(window.PointIn(source), OnBackground);

        Assert.Null(drag.Payload);
    }

    [AvaloniaTheory]
    [InlineData(BindingMode.Default)]
    [InlineData(BindingMode.OneWay)]
    public void AMoveKeepsTheBindingOnPayload(BindingMode mode)
    {
        var holder = new Border { Tag = "A" };
        var drag = new ItemDragOperation();
        drag.Bind(DragOperationBase.PayloadProperty, new ReflectionBinding(nameof(Control.Tag)) { Source = holder, Mode = mode });
        var (window, source, target) = Slots(drag, new ItemDropOperation());

        window.MouseDrag(window.PointIn(source), window.PointIn(target));
        Assert.Null(drag.Payload);
        Assert.Equal(mode == BindingMode.OneWay ? "A" : null, holder.Tag);

        holder.Tag = "B";
        Assert.Equal("B", drag.Payload);
    }

    [Fact]
    public void SwapOnDropDefaultsToFalse() => Assert.False(new ItemDragOperation().SwapOnDrop);

    [AvaloniaFact]
    public void AnyOneSharedIdIsEnough()
    {
        var drop = new ItemDropOperation { InteractionIds = ["app:drinks", "app:desserts"] };
        var (window, source, target) = Slots(new ItemDragOperation { Payload = "A", InteractionIds = ["app:meals", "app:drinks"] }, drop);

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void CommaSeparatedIdsFromXamlAreSeparateEntries()
    {
        var view = new InteractionIdsView();

        Assert.Equal(["app:meal", "app:drink"], DragDrop.GetDragOperation(view.SlotBorder)!.InteractionIds);
    }

    [AvaloniaFact]
    public void TheDragsDataHoldsThePayloadUnderEachIdAndTheDragInfoInOneItem()
    {
        var drag = new ItemDragOperation { Payload = "A", InteractionIds = ["app:meals", "app:drinks", PlatformInteractionIds.Text] };
        var drop = new RecordingDropOperation { InteractionIds = ["app:meals"] };
        IDataTransfer? data = null;
        drop.Probe = e => data ??= e.DataTransfer;
        var (window, source, target) = Slots(drag, drop);

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.NotNull(data);
        Assert.Single(data.Items);
        Assert.Equal("A", data.TryGetValue(DragDropIds.PayloadFormat("app:meals")));
        Assert.Equal("A", data.TryGetValue(DragDropIds.PayloadFormat("app:drinks")));
        Assert.Same(drop.LastDragInfo, data.TryGetValue(DragDropIds.DragMetadataFormat));
        Assert.Equal("app:dragmetadata", DragDropIds.DragMetadataFormat.Identifier);
    }

    [AvaloniaFact]
    public void SeveralPayloadsTravelOneItemEachInOrderWithTheDragInfoOnTheFirst()
    {
        var drag = new CollectionDragOperation { InteractionIds = ["app:meals", "app:drinks"] };

        IDataTransfer data = DragData.For(drag, ["A", "B", "C"]);

        Assert.Equal(3, data.Items.Count);
        foreach (var id in drag.InteractionIds)
            Assert.Equal(["A", "B", "C"], data.Items.Select(x => x.TryGetValue(DragDropIds.PayloadFormat(id))));
        Assert.Equal(["A", "B", "C"], data.Items[0].TryGetValue(DragDropIds.DragMetadataFormat)?.Payloads);
        Assert.All(data.Items.Skip(1), x => Assert.Null(x.TryGetValue(DragDropIds.DragMetadataFormat)));
    }

    [AvaloniaFact]
    public void TheTargetReadsThePayloadOfItsFirstIdThatHasOne()
    {
        var item = new DataTransferItem();
        item.Set(DragDropIds.PayloadFormat("app:a"), "A");
        item.Set(DragDropIds.PayloadFormat("app:b"), "B");
        item.Set(DragDropIds.DragMetadataFormat, new DragInfo { DragOperation = new ItemDragOperation(), DragIds = ["app:a", "app:b"] });
        var data = new DataTransfer();
        data.Add(item);
        var drop = new ItemDropOperation { InteractionIds = ["app:missing", "app:b", "app:a"] };
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        DragDrop.SetDropOperation(target, drop);
        var window = TestWindow.Show(target);
        using var drag = window.SimulateDrag(data);

        drag.Enter(window.PointIn(target));
        drag.Drop(window.PointIn(target));

        Assert.Equal("B", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void TheDragInfoDescribesTheDragWithACopyOfItsIds()
    {
        var drag = new ItemDragOperation { Payload = "A", InteractionIds = ["app:meals"] };
        var drop = new RecordingDropOperation { InteractionIds = ["app:meals"] };
        var (window, source, target) = Slots(drag, drop);

        window.MouseDrag(window.PointIn(source, new Point(30, 40)), window.PointIn(target));
        drag.InteractionIds.Add("app:drinks");

        var info = drop.LastDragInfo!;
        Assert.Same(drag, info.DragOperation);
        Assert.Equal(new Point(30, 40), info.DragOrigin);
        Assert.Equal(["app:meals"], info.DragIds);
        Assert.NotNull(info.PointerId);
        Assert.Equal("A", info.Payload);
    }

    [AvaloniaFact]
    public void TheDropInfoDescribesTheDrop()
    {
        var drag = new RecordingDragOperation { Payload = "A" };
        var (window, source, target) = Slots(drag, new ItemDropOperation { PayloadTarget = "old" });

        window.MouseDrag(window.PointIn(source), window.PointIn(target, new Point(20, 25)));

        var dropInfo = Assert.Single(drag.Completions).DropInfo;
        Assert.NotNull(dropInfo.DragEventArgs);
        Assert.Same(target, dropInfo.HoveredControl);
        Assert.Equal(new Point(20, 25), dropInfo.HoverLocation);
        Assert.Equal("old", dropInfo.ReplacedPayload);
    }

    [AvaloniaFact]
    public void TheCommandSeesTheElementDroppedOnAndItsContext()
    {
        DropEventArgs? args = null;
        var badge = new Border { Background = Brushes.Green, Width = 40, Height = 40, DataContext = "badge context" };
        var drop = new ItemDropOperation { PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) };
        var (window, source, target) = Slots(new ItemDragOperation { Payload = "A" }, drop, badge);

        window.MouseDrag(window.PointIn(source), window.PointIn(badge));

        Assert.Same(badge, args?.Target);
        Assert.Equal("badge context", args?.TargetContext);
    }

    [AvaloniaFact]
    public void ASingleValueTargetDropsOnTargetWithNoLocation()
    {
        DropEventArgs? args = null;
        var drop = new ItemDropOperation { PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) };
        var (window, source, target) = Slots(new ItemDragOperation { Payload = "A" }, drop);

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Equal(DropTargetOffset.OnTarget, args?.Offset);
        Assert.NotNull(args);
        Assert.Null(args.Location);
    }

    [AvaloniaFact]
    public void TheEffectFollowsTheModifiersOfEachEvent()
    {
        var drag = new RecordingDragOperation { Payload = "A", AllowedEffects = DragDropEffects.Copy | DragDropEffects.Move };
        var (window, source, target) = Slots(drag, new ItemDropOperation());
        using var effects = new ReportedEffects(window);

        using (var held = HeldMouseDrag.Start(window, window.PointIn(source), window.PointIn(target)))
        {
            held.MoveTo(window.PointIn(target));
            Assert.Equal(DragDropEffects.Move, effects.Last);

            held.MoveTo(window.PointIn(target), RawInputModifiers.Control);
            Assert.Equal(DragDropEffects.Copy, effects.Last);

            held.MoveTo(window.PointIn(target));
            Assert.Equal(DragDropEffects.Move, effects.Last);

            held.Release(RawInputModifiers.Control);
        }

        Assert.Equal(DragDropEffects.Copy, Assert.Single(drag.Completions).Effect);
        Assert.Equal("A", drag.Payload);
    }

    [AvaloniaTheory]
    [InlineData(RawInputModifiers.None, DragDropEffects.Move)]
    [InlineData(RawInputModifiers.Control, DragDropEffects.Copy)]
    public void DropCompletedGetsTheEffectComputedOnDrop(RawInputModifiers modifiers, DragDropEffects effect)
    {
        var drag = new RecordingDragOperation { Payload = "A", AllowedEffects = DragDropEffects.Copy | DragDropEffects.Move };
        var (window, source, target) = Slots(drag, new ItemDropOperation());

        window.MouseDrag(window.PointIn(source), window.PointIn(target), modifiers);

        Assert.Equal(effect, Assert.Single(drag.Completions).Effect);
    }

    private static (Window, Border, Border) Slots(DragOperationBase drag, DropOperationBase drop, Control? targetContent = null)
    {
        var source = new Border { Background = Brushes.Red, Width = 100, Height = 100 };
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100, Child = targetContent };
        DragDrop.SetDragOperation(source, drag);
        DragDrop.SetDropOperation(target, drop);
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { source, target } };
        return (TestWindow.Show(panel), source, target);
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

    private sealed class PlainDragOperation : DragOperationBase
    {
    }

    private sealed class OutsideRecordingDragOperation : ItemDragOperation
    {
        public List<DragInfo> Outside { get; } = [];

        protected override void OnDroppedOutside(DragInfo dragInfo)
        {
            Outside.Add(dragInfo);
            base.OnDroppedOutside(dragInfo);
        }
    }
}
