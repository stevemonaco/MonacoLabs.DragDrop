using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using System.Collections;
using System.Collections.ObjectModel;
using Xunit;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Tests;

public class CollectionDropTests
{
    private static readonly string[] DropClasses = [":dropover", ":droptop", ":dropbottom", ":dropinside"];

    [AvaloniaFact]
    public void ItemsCanBeReorderedWithinOneList()
    {
        var items = new ObservableCollection<string> { "a", "b", "c" };
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = items }, items);
        DragDrop.SetDragOperation(list, new CollectionDragOperation { PayloadCollection = items });

        window.MouseDrag(window.PointIn(Container(list, 2)), Top(window, Container(list, 0)));

        Assert.Equal(["c", "a", "b"], items);
    }

    [AvaloniaFact]
    public void TheHoveredContainerIsTheItemContainerUnderThePointer()
    {
        var items = new List<string> { "a", "b", "c" };
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = items }, items);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(window.PointIn(Container(list, 1)));
        Assert.Contains(":dropover", Container(list, 1).Classes);

        drag.Over(Below(window, list));
        Assert.All(Containers(list), c => Assert.DoesNotContain(":dropover", c.Classes));
        Assert.Contains(":dropover", list.Classes);
    }

    [AvaloniaFact]
    public void TheTopHalfOfAContainerIsBeforeItAndTheBottomHalfAfterIt()
    {
        var items = new List<string> { "a", "b", "c" };
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = items }, items);
        var container = Container(list, 1);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(window.PointIn(container, new Point(10, container.Bounds.Height / 2 - 1)));
        Assert.Contains(":droptop", container.Classes);

        drag.Over(window.PointIn(container, new Point(10, container.Bounds.Height / 2 + 1)));
        Assert.Contains(":dropbottom", container.Classes);
        Assert.DoesNotContain(":droptop", container.Classes);
    }

    [AvaloniaFact]
    public void WithoutAHoveredContainerTheDropAppendsAfterTheItems()
    {
        var items = new List<string> { "a", "b" };
        DropEventArgs? args = null;
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = items, PayloadCommand = Record(a => args = a) }, items);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(Below(window, list));
        drag.Drop(Below(window, list));

        Assert.Equal(DropTargetOffset.AfterTarget, args?.Offset);
        Assert.Null(args?.Location?.Index);
        Assert.Equal(["a", "b", "x"], items);
    }

    [AvaloniaFact]
    public void TheHoveredContainerHasTheDropPseudoClassesOnlyWhileTheDropIsValid()
    {
        var items = new List<string> { "a", "b", "c" };
        var operation = new ProbingCollectionDropOperation { PayloadTarget = items, Rejection = "No" };
        var (window, list) = ShowList(operation, items);
        var container = Container(list, 1);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(Top(window, container));
        Assert.DoesNotContain(container.Classes, DropClasses.Contains);

        operation.Rejection = null;
        drag.Over(Top(window, container));
        Assert.Equal([":dropover", ":droptop"], container.Classes.Where(DropClasses.Contains).Order());
    }

    [AvaloniaFact]
    public void MovingToAnotherContainerClearsThePreviousOne()
    {
        var items = new List<string> { "a", "b", "c" };
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = items }, items);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(Top(window, Container(list, 0)));
        drag.Over(Bottom(window, Container(list, 2)));

        Assert.DoesNotContain(Container(list, 0).Classes, DropClasses.Contains);
        Assert.Contains(":dropbottom", Container(list, 2).Classes);
    }

    [AvaloniaFact]
    public void TheInsertionAdornerFollowsTheHoveredContainer()
    {
        var items = new List<string> { "a", "b", "c" };
        var operation = new ProbingCollectionDropOperation { PayloadTarget = items };
        var (window, list) = ShowList(operation, items);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));
        var adorner = operation.ItemDropAdorner!;

        drag.Enter(Top(window, Container(list, 0)));
        Assert.Same(Container(list, 0), adorner.TargetControl);
        Assert.Equal(DropTargetOffset.BeforeTarget, adorner.Offset);
        Assert.True(adorner.IsAttached);
        Assert.True(adorner.IsDropValid);

        operation.Rejection = "No";
        drag.Over(Bottom(window, Container(list, 2)));
        Assert.Same(Container(list, 2), adorner.TargetControl);
        Assert.Equal(DropTargetOffset.AfterTarget, adorner.Offset);
        Assert.True(adorner.IsAttached);
        Assert.False(adorner.IsDropValid);
    }

    [AvaloniaFact]
    public void AttachingWithoutAnItemAdornerCreatesAnInsertionAdorner()
    {
        var created = new CollectionDropOperation();
        var custom = new DropInsertionAdorner();
        var given = new CollectionDropOperation { ItemDropAdorner = custom };

        created.Attach(new ListBox());
        given.Attach(new ListBox());

        Assert.IsType<DropInsertionAdorner>(created.ItemDropAdorner);
        Assert.Same(custom, given.ItemDropAdorner);
    }

    [AvaloniaFact]
    public void EnterAndOverBothRunTheHoverAndAreHandledOnlyWhenValid()
    {
        var items = new List<string> { "a", "b", "c" };
        var operation = new ProbingCollectionDropOperation { PayloadTarget = items };
        var (window, list) = ShowList(operation, items);
        var handled = new List<bool>();
        foreach (var routedEvent in new[] { AvaDragDrop.DragEnterEvent, AvaDragDrop.DragOverEvent })
            window.AddHandler(routedEvent, (_, e) => handled.Add(e.Handled), handledEventsToo: true);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        drag.Enter(Top(window, Container(list, 0)));
        Assert.Contains(":droptop", Container(list, 0).Classes);

        drag.Over(Top(window, Container(list, 1)));
        Assert.Contains(":droptop", Container(list, 1).Classes);

        operation.Rejection = "No";
        drag.Over(Top(window, Container(list, 1)));

        Assert.Equal([true, true, false], handled);
    }

    [AvaloniaFact]
    public void TheLocationIsInPayloadTargetWhenItIsAList()
    {
        var target = new List<string>();
        DropEventArgs? args = null;
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = target, PayloadCommand = Record(a => args = a) }, new List<string> { "a" });

        DropOn(window, Top(window, Container(list, 0)));

        Assert.Same(target, args?.Location?.Collection);
    }

    [AvaloniaFact]
    public void WithoutAListPayloadTargetTheLocationIsInTheItemsSource()
    {
        var items = new List<string> { "a" };
        DropEventArgs? args = null;
        var (window, list) = ShowList(new CollectionDropOperation { PayloadCommand = Record(a => args = a) }, items);

        DropOn(window, Top(window, Container(list, 0)));

        Assert.Same(items, args?.Location?.Collection);
    }

    [AvaloniaFact]
    public void WithoutAnyListThereIsNoLocation()
    {
        DropEventArgs? args = null;
        var (window, list) = ShowList(new CollectionDropOperation { PayloadCommand = Record(a => args = a) }, Enumerate("a"));

        DropOn(window, Top(window, Container(list, 0)));

        Assert.NotNull(args);
        Assert.Null(args.Location);
    }

    [AvaloniaTheory]
    [InlineData(0.25, 1)]
    [InlineData(0.75, 2)]
    public void TheLocationIndexIsTheContainersIndexOrTheNextOne(double height, int index)
    {
        var items = new List<string> { "a", "b", "c" };
        DropEventArgs? args = null;
        var (window, list) = ShowList(new CollectionDropOperation { PayloadCommand = Record(a => args = a) }, items);
        var container = Container(list, 1);

        DropOn(window, window.PointIn(container, new Point(10, container.Bounds.Height * height)));

        Assert.Equal(index, args?.Location?.Index);
    }

    [AvaloniaFact]
    public void TheStructuralCheckRunsFirstAndItsMessageWins()
    {
        var items = new List<string> { "a" };
        var operation = new ProbingCollectionDropOperation { PayloadTarget = items, StructuralRejection = "Not at this spot", AcceptsAll = true };
        var (window, list) = ShowList(operation, items);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));

        Assert.Equal(DragDropEffects.None, drag.Enter(Top(window, Container(list, 0))));
        Assert.Equal("Not at this spot", operation.ErrorMessage);
        Assert.Equal(0, operation.CanDropCalls);

        operation.StructuralRejection = null;
        operation.AcceptsAll = false;
        operation.Rejection = "Not this item";
        Assert.Equal(DragDropEffects.None, drag.Over(Top(window, Container(list, 0))));
        Assert.Equal("Not this item", operation.ErrorMessage);
    }

    [AvaloniaTheory]
    [InlineData(0.25, new[] { "a", "x", "b" }, 1)]
    [InlineData(-1, new[] { "a", "b", "x" }, 2)]
    public void AnAcceptedDropInsertsThenCompletesWithWhereItLanded(double height, string[] expected, int index)
    {
        var items = new ObservableCollection<string> { "a", "b" };
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = items }, items);
        var container = Container(list, 1);
        var source = new InsertionCheckingDragOperation(items);
        var point = height < 0 ? Below(window, list) : window.PointIn(container, new Point(10, container.Bounds.Height * height));
        using var drag = window.SimulateDrag(DragData.For(source, "x"));

        drag.Enter(point);
        drag.Drop(point);

        Assert.Equal(expected, items);
        var info = Assert.IsType<CollectionDropInfo>(Assert.Single(source.Completions).DropInfo);
        Assert.True(source.InsertedBeforeCompletion);
        Assert.Same(items, info.TargetCollection);
        Assert.Equal(index, info.TargetIndex);
        Assert.Same(height < 0 ? null : container, info.TargetContainer);
        Assert.Null(info.TargetParent);
    }

    [AvaloniaFact]
    public void ANonListPayloadTargetDoesNotTransfer()
    {
        var items = new List<string> { "a" };
        DropEventArgs? args = null;
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = "not a list", PayloadCommand = Record(a => args = a) }, items);

        DropOn(window, Top(window, Container(list, 0)));

        Assert.Equal(["a"], items);
        Assert.Same(items, args?.Location?.Collection);
    }

    [AvaloniaFact]
    public void ASubclassCanTransferWithoutAPayloadTarget()
    {
        var items = new List<string> { "a" };
        var (window, list) = ShowList(new TransferringCollectionDropOperation(), items);

        DropOn(window, Top(window, Container(list, 0)));

        Assert.Equal(["x", "a"], items);
    }

    [AvaloniaFact]
    public void ATransferringDropAlsoRunsTheCommandWithThePayloadOffsetAndLocation()
    {
        var items = new List<string> { "a", "b" };
        DropEventArgs? args = null;
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = items, PayloadCommand = Record(a => args = a) }, items);

        DropOn(window, Top(window, Container(list, 1)));

        Assert.Equal(["a", "x", "b"], items);
        Assert.Equal(["x"], args?.Items);
        Assert.Equal(DropTargetOffset.BeforeTarget, args?.Offset);
        Assert.Equal(1, args?.Location?.Index);
        Assert.False(args?.IsPlatformDrop);
    }

    [AvaloniaFact]
    public void APlatformDropRunsTheCommandAtTheItemsSourceLocationAndInsertsNothing()
    {
        var items = new List<string> { "a", "b" };
        DropEventArgs? args = null;
        var operation = new CollectionDropOperation { InteractionIds = [PlatformInteractionIds.Text], PayloadCommand = Record(a => args = a) };
        var (window, list) = ShowList(operation, items);
        var item = new DataTransferItem();
        item.SetText("hello");
        var data = new DataTransfer();
        data.Add(item);
        using var drag = window.SimulateDrag(data);

        drag.Enter(Top(window, Container(list, 1)));
        drag.Drop(Top(window, Container(list, 1)));

        Assert.True(args?.IsPlatformDrop);
        Assert.Equal(["hello"], args?.Items);
        Assert.Equal(DropTargetOffset.BeforeTarget, args?.Offset);
        Assert.Same(items, args?.Location?.Collection);
        Assert.Equal(1, args?.Location?.Index);
        Assert.Equal(["a", "b"], items);
    }

    [AvaloniaFact]
    public void ADropClearsTheHoverAndTheAdorners()
    {
        var items = new List<string> { "a", "b" };
        var operation = new ProbingCollectionDropOperation { PayloadTarget = items };
        var (window, list) = ShowList(operation, items);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));
        drag.Enter(Top(window, Container(list, 1)));

        drag.Drop(Top(window, Container(list, 1)));

        AssertCleared(operation, list);
    }

    [AvaloniaFact]
    public void ALeaveClearsTheHoverAndTheAdorners()
    {
        var items = new List<string> { "a", "b" };
        var operation = new ProbingCollectionDropOperation { PayloadTarget = items };
        var (window, list) = ShowList(operation, items);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));
        drag.Enter(Top(window, Container(list, 1)));
        Assert.NotNull(operation.Location);

        drag.Leave(Top(window, Container(list, 1)));

        AssertCleared(operation, list);
    }

    [AvaloniaFact]
    public void ADragThatStopsBeingMeantForTheTargetClearsTheHover()
    {
        var items = new List<string> { "a", "b" };
        var operation = new ProbingCollectionDropOperation { PayloadTarget = items };
        var (window, list) = ShowList(operation, items);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));
        drag.Enter(Top(window, Container(list, 1)));

        using var other = window.SimulateDrag(DragData.For(new CollectionDragOperation { InteractionIds = ["other"] }, "x"));
        Assert.Equal(DragDropEffects.None, other.Over(Top(window, Container(list, 1))));

        AssertCleared(operation, list);
    }

    [AvaloniaFact]
    public void ADragOverTheTargetsOwnSurfaceIsAccepted()
    {
        var items = new List<string>();
        var target = new Border { Width = 200, Height = 200, Background = Brushes.Blue, Child = new Border { Width = 40, Height = 40 } };
        DragDrop.SetDropOperation(target, new CollectionDropOperation { PayloadTarget = items });
        var window = TestWindow.Show(target);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "A"));
        var background = window.PointIn(target, new Point(10, 10));

        Assert.Equal(DragDropEffects.Move, drag.Enter(background));
        Assert.Equal(DragDropEffects.Move, drag.Drop(background));
        Assert.Equal(["A"], items);
    }

    [AvaloniaFact]
    public void ACopyDropInsertsTheCreatedCopy()
    {
        var (window, target, items, commandItems) = ShowCopyingTarget();
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "A"), DragDropEffects.Copy | DragDropEffects.Move);

        drag.Enter(window.PointIn(target), RawInputModifiers.Control);
        Assert.Equal(DragDropEffects.Copy, drag.Drop(window.PointIn(target), RawInputModifiers.Control));

        Assert.Equal(["A copy"], items);
        Assert.Equal(["A copy"], commandItems());
    }

    [AvaloniaFact]
    public void ACopyDropCopiesEachPayload()
    {
        var (window, target, items, commandItems) = ShowCopyingTarget();
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), ["A", "B"]), DragDropEffects.Copy | DragDropEffects.Move);

        drag.Enter(window.PointIn(target), RawInputModifiers.Control);
        Assert.Equal(DragDropEffects.Copy, drag.Drop(window.PointIn(target), RawInputModifiers.Control));

        Assert.Equal(["A copy", "B copy"], items);
        Assert.Equal(["A copy", "B copy"], commandItems());
    }

    [AvaloniaTheory]
    [InlineData(0.25, new[] { "a", "x", "y", "b" }, 1)]
    [InlineData(-1, new[] { "a", "b", "x", "y" }, 2)]
    public void SeveralPayloadsAreInsertedTogetherInOrder(double height, string[] expected, int index)
    {
        var items = new ObservableCollection<string> { "a", "b" };
        DropEventArgs? args = null;
        var (window, list) = ShowList(new CollectionDropOperation { PayloadTarget = items, PayloadCommand = Record(a => args = a) }, items);
        var container = Container(list, 1);
        var source = new RecordingDragOperation();
        var point = height < 0 ? Below(window, list) : window.PointIn(container, new Point(10, container.Bounds.Height * height));
        using var drag = window.SimulateDrag(DragData.For(source, ["x", "y"]));

        drag.Enter(point);
        drag.Drop(point);

        Assert.Equal(expected, items);
        Assert.Equal(["x", "y"], args?.Items);
        var info = Assert.IsType<CollectionDropInfo>(Assert.Single(source.Completions).DropInfo);
        Assert.Equal(index, info.TargetIndex);
        Assert.Equal(2, info.TargetCount);
    }

    [AvaloniaFact]
    public void ATargetThatRefusesSeveralPayloadsIsNotMeantForTheirDrag()
    {
        var items = new List<string> { "a" };
        var operation = new CollectionDropOperation { PayloadTarget = items, AcceptsMultiplePayloads = false };
        var (window, target) = ShowTarget(operation);

        using (var several = window.SimulateDrag(DragData.For(new CollectionDragOperation(), ["x", "y"])))
        {
            Assert.Equal(DragDropEffects.None, several.Enter(window.PointIn(target)));
            Assert.False(operation.DropAdorner!.IsAttached);
            Assert.Equal(DragDropEffects.None, several.Drop(window.PointIn(target)));
        }

        using (var one = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x")))
        {
            Assert.Equal(DragDropEffects.Move, one.Enter(window.PointIn(target)));
            one.Drop(window.PointIn(target));
        }

        Assert.Equal(["a", "x"], items);
    }

    [AvaloniaFact]
    public void AMoveDropIgnoresCreateCopy()
    {
        var (window, target, items, commandItems) = ShowCopyingTarget();
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "A"), DragDropEffects.Copy | DragDropEffects.Move);

        drag.Enter(window.PointIn(target));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));

        Assert.Equal(["A"], items);
        Assert.Equal(["A"], commandItems());
    }

    [AvaloniaFact]
    public void ACommandOnlyTargetCompletesAMove()
    {
        var source = new List<string> { "A", "B" };
        DropEventArgs? args = null;
        var (window, target) = ShowTarget(new CollectionDropOperation { PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) });
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation { PayloadCollection = source }, "A"));

        drag.Enter(window.PointIn(target));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));

        Assert.Equal(["A"], args?.Items);
        Assert.Equal(["B"], source);
    }

    [AvaloniaFact]
    public void ADropThatNeitherTransfersNorRunsACommandReportsNone()
    {
        var source = new List<string> { "A", "B" };
        var (window, target) = ShowTarget(new CollectionDropOperation());
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation { PayloadCollection = source }, "A"));

        Assert.Equal(DragDropEffects.Move, drag.Enter(window.PointIn(target)));
        Assert.Equal(DragDropEffects.None, drag.Drop(window.PointIn(target)));

        Assert.Equal(["A", "B"], source);
    }

    [AvaloniaFact]
    public void ACommandWhoseCanExecuteIsFalseReportsNone()
    {
        var source = new List<string> { "A", "B" };
        var executed = false;
        var (window, target) = ShowTarget(new CollectionDropOperation { PayloadCommand = new RelayCommand(_ => executed = true, _ => false) });
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation { PayloadCollection = source }, "A"));

        drag.Enter(window.PointIn(target));
        Assert.Equal(DragDropEffects.None, drag.Drop(window.PointIn(target)));

        Assert.False(executed);
        Assert.Equal(["A", "B"], source);
    }

    [AvaloniaFact]
    public void DetachingMidDragClearsTheHoveredContainer()
    {
        var items = new List<string> { "a", "b", "c" };
        var operation = new CollectionDropOperation { PayloadTarget = items };
        var (window, list) = ShowList(operation, items);
        var container = list.ContainerFromIndex(1)!;
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));
        drag.Enter(window.PointIn(container));
        Assert.Contains(":dropover", container.Classes);
        Assert.True(operation.ItemDropAdorner!.IsAttached);

        DragDrop.SetDropOperation(list, null);

        Assert.DoesNotContain(container.Classes, DropClasses.Contains);
        Assert.DoesNotContain(":dropover", list.Classes);
        Assert.False(operation.ItemDropAdorner.IsAttached);
        Assert.False(operation.DropAdorner!.IsAttached);
    }

    private static (Window, ListBox) ShowList(CollectionDropOperation operation, IEnumerable items)
    {
        var list = new ListBox
        {
            ItemsSource = items,
            Width = 150,
            Height = 250,
            ItemTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock { Height = 30 })
        };
        DragDrop.SetDropOperation(list, operation);
        return (TestWindow.Show(list), list);
    }

    private static Control Container(ListBox list, int index) => list.ContainerFromIndex(index)!;

    private static IEnumerable<Control> Containers(ListBox list) => list.GetRealizedContainers();

    private static Point Top(Window window, Control container) => window.PointIn(container, new Point(10, container.Bounds.Height / 4));

    private static Point Bottom(Window window, Control container) => window.PointIn(container, new Point(10, container.Bounds.Height * 3 / 4));

    // Below the last item and outside the auto-scroll margin
    private static Point Below(Window window, ListBox list) => window.PointIn(list, new Point(10, list.Bounds.Height - 50));

    private static void DropOn(Window window, Point point)
    {
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), "x"));
        drag.Enter(point);
        drag.Drop(point);
    }

    private static void AssertCleared(ProbingCollectionDropOperation operation, ListBox list)
    {
        Assert.All(Containers(list), c => Assert.DoesNotContain(c.Classes, DropClasses.Contains));
        Assert.DoesNotContain(":dropover", list.Classes);
        Assert.Null(operation.Location);
        Assert.False(operation.ItemDropAdorner!.IsAttached);
        Assert.False(operation.DropAdorner!.IsAttached);
    }

    private static IEnumerable<string> Enumerate(params string[] items)
    {
        foreach (var item in items)
            yield return item;
    }

    private static RelayCommand Record(Action<DropEventArgs?> record) => new(a => record(a as DropEventArgs));

    private static (Window, Border) ShowTarget(CollectionDropOperation operation)
    {
        var target = new Border { Width = 200, Height = 200, Background = Brushes.Blue };
        DragDrop.SetDropOperation(target, operation);
        return (TestWindow.Show(target), target);
    }

    private static (Window, Border, List<string>, Func<IReadOnlyList<object>?>) ShowCopyingTarget()
    {
        var items = new List<string>();
        DropEventArgs? args = null;
        var target = new Border { Width = 200, Height = 200, Background = Brushes.Blue };
        var operation = new CopyingCollectionDropOperation { PayloadTarget = items, PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) };
        DragDrop.SetDropOperation(target, operation);
        return (TestWindow.Show(target), target, items, () => args?.Items);
    }

    private sealed class CopyingCollectionDropOperation : CollectionDropOperation
    {
        protected override object CreateCopy(object payload) => $"{payload} copy";
    }

    private sealed class TransferringCollectionDropOperation : CollectionDropOperation
    {
        protected override bool TransfersPayload => true;
    }

    private sealed class ProbingCollectionDropOperation : CollectionDropOperation
    {
        public string? Rejection { get; set; }
        public string? StructuralRejection { get; set; }
        public bool AcceptsAll { get; set; }
        public int CanDropCalls { get; private set; }
        public CollectionDropLocation? Location => DropLocation;

        protected override bool CanDropAtLocation(DragEventArgs e) => StructuralRejection is null || Reject(StructuralRejection);

        protected override bool CanDrop(DragEventArgs e)
        {
            CanDropCalls++;
            if (AcceptsAll)
                return true;

            return Rejection is null ? base.CanDrop(e) : Reject(Rejection);
        }
    }

    private sealed class InsertionCheckingDragOperation(IList target) : RecordingDragOperation
    {
        public bool InsertedBeforeCompletion { get; private set; }

        public override void DropCompleted(DragDropEffects effect, DragInfo dragInfo, DropInfo dropInfo)
        {
            InsertedBeforeCompletion = target.Contains("x");
            base.DropCompleted(effect, dragInfo, dropInfo);
        }
    }
}
