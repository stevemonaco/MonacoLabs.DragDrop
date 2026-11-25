using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MonacoLabs.DragDrop.Abstractions;
using Xunit;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Tests;

public class DropTargetTests
{
    [AvaloniaFact]
    public void SettingHandledEventsTooWhileAttachedThrows()
    {
        var operation = new ItemDropOperation();
        operation.Attach(new Border());

        Assert.Throws<InvalidOperationException>(() => operation.HandledEventsToo = true);
        Assert.False(operation.HandledEventsToo);
    }

    [AvaloniaFact]
    public void AttachingWithoutAnAdornerCreatesASolidHighlight()
    {
        var control = new Border();
        var operation = new ItemDropOperation();

        operation.Attach(control);

        var adorner = Assert.IsType<DropHighlightAdorner>(operation.DropAdorner);
        Assert.Equal(DropAdornerKind.Solid, adorner.Kind);
        Assert.Same(control, adorner.TargetControl);
    }

    [AvaloniaFact]
    public void AttachingAllowsDropsOnTheControl()
    {
        var control = new Border();

        DragDrop.SetDropOperation(control, new ItemDropOperation());

        Assert.True(AvaDragDrop.GetAllowDrop(control));
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void HandledEventsTooDecidesWhetherADropAChildHandlesReachesTheTarget(bool handledEventsToo, bool reaches)
    {
        var child = new Border { Background = Brushes.Green, Width = 40, Height = 40 };
        AvaDragDrop.SetAllowDrop(child, true);
        foreach (var routedEvent in new[] { AvaDragDrop.DragEnterEvent, AvaDragDrop.DragOverEvent, AvaDragDrop.DropEvent })
            child.AddHandler(routedEvent, (_, e) => e.Handled = true);
        var operation = new ItemDropOperation { HandledEventsToo = handledEventsToo };
        var (window, _) = Show(operation, child);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        drag.Enter(window.PointIn(child));
        drag.Drop(window.PointIn(child));

        Assert.Equal(reaches ? "A" : null, operation.PayloadTarget);
    }

    [AvaloniaFact]
    public void DetachingDisallowsDropsAndStopsHandlingDragEvents()
    {
        var operation = new ItemDropOperation();
        var (window, target) = Show(operation);

        DragDrop.SetDropOperation(target, null);
        Assert.False(AvaDragDrop.GetAllowDrop(target));

        AvaDragDrop.SetAllowDrop(target, true);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));
        drag.Drop(window.PointIn(target));

        Assert.Null(operation.PayloadTarget);
        Assert.False(operation.DropAdorner!.IsAttached);
    }

    [AvaloniaFact]
    public void ADragCarryingOneOfTheTargetsIdsIsMeantForIt()
    {
        var operation = new ItemDropOperation { InteractionIds = ["meals", "drinks"] };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation { InteractionIds = ["snacks", "drinks"] }, "A"));

        Assert.Equal(DragDropEffects.Move, drag.Enter(window.PointIn(target)));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));

        Assert.Equal("A", operation.PayloadTarget);
    }

    [AvaloniaFact]
    public void ADragWithoutAnyOfTheTargetsIdsIsNotMeantForIt()
    {
        var operation = new RejectingDropOperation { InteractionIds = ["meals"] };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation { InteractionIds = ["drinks"] }, "A"));

        Assert.Equal(DragDropEffects.None, drag.Enter(window.PointIn(target)));
        Assert.False(operation.DropAdorner!.IsAttached);
        Assert.Equal(DragDropEffects.None, drag.Over(window.PointIn(target)));
        Assert.Equal(DragDropEffects.None, drag.Drop(window.PointIn(target)));

        Assert.Equal(0, operation.Evaluations);
        Assert.False(operation.DropAdorner.IsAttached);
        Assert.DoesNotContain(":dropover", target.Classes);
        Assert.Null(operation.PayloadTarget);
    }

    [AvaloniaFact]
    public void ADragFromTheTargetsOwnControlIsNotMeantForIt()
    {
        var operation = new ItemDropOperation();
        var (window, target) = Show(operation);
        var source = new ItemDragOperation();
        DragDrop.SetDragOperation(target, source);
        using var drag = window.SimulateDrag(DragData.For(source, "A"));

        Assert.Equal(DragDropEffects.None, drag.Enter(window.PointIn(target)));
        Assert.False(operation.DropAdorner!.IsAttached);
        Assert.Equal(DragDropEffects.None, drag.Drop(window.PointIn(target)));

        Assert.Null(operation.PayloadTarget);
    }

    [AvaloniaFact]
    public void CanDropIsEvaluatedOnEveryEnterOverAndDrop()
    {
        var operation = new RejectingDropOperation();
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        drag.Enter(window.PointIn(target));
        drag.Over(window.PointIn(target));
        drag.Over(window.PointIn(target));
        drag.Drop(window.PointIn(target));

        Assert.Equal(4, operation.Evaluations);
    }

    [AvaloniaFact]
    public void WithoutACanDropOverrideEveryDragMeantForTheTargetIsAccepted()
    {
        var operation = new ItemDropOperation();
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        Assert.Equal(DragDropEffects.Move, drag.Enter(window.PointIn(target)));
        Assert.Equal(DragDropEffects.Move, drag.Over(window.PointIn(target)));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("Not here")]
    public void EnteringShowsTheAdornerWithTheValidationResult(string? rejection)
    {
        var operation = new RejectingDropOperation { Message = rejection };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        drag.Enter(window.PointIn(target));

        var adorner = operation.DropAdorner!;
        Assert.True(adorner.IsAttached);
        Assert.Equal(rejection is null, adorner.IsDropValid);
        Assert.Equal(rejection is not null, adorner.Classes.Contains(":error"));
    }

    [AvaloniaFact]
    public void DropoverAndTheAdornersValidityFollowEachDragOver()
    {
        var operation = new RejectingDropOperation();
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));
        Assert.Contains(":dropover", target.Classes);

        operation.Message = "Not here";
        drag.Over(window.PointIn(target));
        Assert.DoesNotContain(":dropover", target.Classes);
        Assert.False(operation.DropAdorner!.IsDropValid);
        Assert.True(operation.DropAdorner.IsAttached);

        operation.Message = null;
        drag.Over(window.PointIn(target));
        Assert.Contains(":dropover", target.Classes);
        Assert.True(operation.DropAdorner.IsDropValid);
    }

    [AvaloniaFact]
    public void TheEffectIsNoneWhileRejectedAndOtherwiseTheResolvedEffect()
    {
        var operation = new RejectingDropOperation { Message = "Not here" };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"), DragDropEffects.Copy | DragDropEffects.Move);

        Assert.Equal(DragDropEffects.None, drag.Enter(window.PointIn(target), RawInputModifiers.Control));
        Assert.Equal(DragDropEffects.None, drag.Over(window.PointIn(target), RawInputModifiers.Control));

        operation.Message = null;
        Assert.Equal(DragDropEffects.Copy, drag.Over(window.PointIn(target), RawInputModifiers.Control));
        Assert.Equal(DragDropEffects.Move, drag.Over(window.PointIn(target)));
    }

    [AvaloniaFact]
    public void ALeaveClearsTheHoverOnceTheDeferredWorkRuns()
    {
        var operation = new ItemDropOperation();
        var (window, target) = Show(operation);
        var data = DragData.For(new ItemDragOperation(), "A");
        using var drag = window.SimulateDrag(data);
        drag.Enter(window.PointIn(target));

        Raise(target, AvaDragDrop.DragLeaveEvent, data);
        Assert.Contains(":dropover", target.Classes);

        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(":dropover", target.Classes);
        Assert.False(operation.DropAdorner!.IsAttached);
    }

    [AvaloniaFact]
    public void AnEnterBeforeTheDeferredLeaveRunsKeepsTheHover()
    {
        var operation = new ItemDropOperation();
        var (window, target) = Show(operation);
        var data = DragData.For(new ItemDragOperation(), "A");
        using var drag = window.SimulateDrag(data);
        drag.Enter(window.PointIn(target));

        Raise(target, AvaDragDrop.DragLeaveEvent, data);
        Raise(target, AvaDragDrop.DragEnterEvent, data);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(":dropover", target.Classes);
        Assert.True(operation.DropAdorner!.IsAttached);
    }

    [AvaloniaFact]
    public void ARejectedDropReportsNoneAndOnlyClearsTheHover()
    {
        var source = new RecordingDragOperation();
        var executed = false;
        var operation = new RejectingDropOperation { Message = "Not here", PayloadTarget = "old", PayloadCommand = new RelayCommand(_ => executed = true) };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(source, "A"));
        drag.Enter(window.PointIn(target));

        Assert.Equal(DragDropEffects.None, drag.Drop(window.PointIn(target)));

        Assert.Equal("old", operation.PayloadTarget);
        Assert.Empty(source.Completions);
        Assert.False(executed);
        Assert.Equal(0, operation.Drops);
        Assert.DoesNotContain(":dropover", target.Classes);
        Assert.False(operation.DropAdorner!.IsAttached);
    }

    [AvaloniaFact]
    public void AnAcceptedDropAssignsThePayloadCompletesTheDragThenRunsTheCommand()
    {
        var source = new RecordingDragOperation();
        var completedBeforeCommand = false;
        var operation = new ItemDropOperation { PayloadTarget = "old" };
        operation.PayloadCommand = new RelayCommand(_ => completedBeforeCommand = source.Completions.Count == 1 && Equals(operation.PayloadTarget, "A"));
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(source, "A"), DragDropEffects.Copy | DragDropEffects.Move);
        drag.Enter(window.PointIn(target));

        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));

        Assert.Equal("A", operation.PayloadTarget);
        var (effect, dropInfo) = Assert.Single(source.Completions);
        Assert.Equal(DragDropEffects.Move, effect);
        Assert.Equal("old", dropInfo.ReplacedPayload);
        Assert.Same(target, dropInfo.HoveredControl);
        Assert.True(completedBeforeCommand);
    }

    [AvaloniaFact]
    public void TheCommandRunsOnlyWhenItCanExecuteTheArgsItWouldReceive()
    {
        object? checkedArgs = null;
        object? executedArgs = null;
        var allow = false;
        var operation = new ItemDropOperation { PayloadCommand = new RelayCommand(a => executedArgs = a, a => { checkedArgs = a; return allow; }) };
        var (window, target) = Show(operation);

        DropOnce();
        Assert.Equal(["A"], Assert.IsType<DropEventArgs>(checkedArgs).Items);
        Assert.Null(executedArgs);

        allow = true;
        DropOnce();
        Assert.NotNull(executedArgs);
        Assert.Same(checkedArgs, executedArgs);

        void DropOnce()
        {
            using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
            drag.Enter(window.PointIn(target));
            drag.Drop(window.PointIn(target));
        }
    }

    [AvaloniaFact]
    public void AnAcceptedDropCallsOnDropAndHidesTheFeedback()
    {
        var operation = new RejectingDropOperation();
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));
        Assert.True(operation.DropAdorner!.IsDropValid);

        drag.Drop(window.PointIn(target));

        Assert.Equal(1, operation.Drops);
        Assert.DoesNotContain(":dropover", target.Classes);
        Assert.False(operation.DropAdorner.IsDropValid);
        Assert.False(operation.DropAdorner.IsAttached);
    }

    [AvaloniaFact]
    public void ADropWritesPayloadTargetBackThroughADefaultBinding()
    {
        var holder = new Border { Tag = "old" };
        var operation = new ItemDropOperation();
        operation.Bind(DropOperationBase.PayloadTargetProperty, new ReflectionBinding(nameof(Control.Tag)) { Source = holder });
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        drag.Enter(window.PointIn(target));
        drag.Drop(window.PointIn(target));

        Assert.Equal(BindingMode.TwoWay, DropOperationBase.PayloadTargetProperty.GetMetadata(typeof(ItemDropOperation)).DefaultBindingMode);
        Assert.Equal("A", holder.Tag);
    }

    [AvaloniaFact]
    public void TryGetPayloadSucceedsForThePayloadsType()
    {
        var operation = new ProbingDropOperation();
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        drag.Enter(window.PointIn(target));

        Assert.True(operation.GotString);
        Assert.False(operation.GotUri);
    }

    [AvaloniaFact]
    public void TryGetDragInfoSucceedsForTheDragInfosType()
    {
        var operation = new ProbingDropOperation();
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        drag.Enter(window.PointIn(target));

        Assert.True(operation.GotDragInfo);
        Assert.False(operation.GotCollectionDragInfo);
    }

    [AvaloniaFact]
    public void TryGetFilesAndTryGetTextNeedTheirIdAndTheirData()
    {
        var operation = new ProbingDropOperation { InteractionIds = [DragDropIds.DefaultOperation, PlatformInteractionIds.Files] };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation { InteractionIds = [DragDropIds.DefaultOperation, PlatformInteractionIds.Text] }, "A"));

        drag.Enter(window.PointIn(target));

        Assert.False(operation.GotFiles);
        Assert.False(operation.GotText);
    }

    [AvaloniaFact]
    public async Task TryGetFilesAndTryGetTextSucceedWithTheirIdAndTheirData()
    {
        var operation = new ProbingDropOperation { InteractionIds = [PlatformInteractionIds.Files, PlatformInteractionIds.Text] };
        var (window, target) = Show(operation);
        var item = new DataTransferItem();
        item.SetFile(await TestFiles.CreateAsync(window, "a.txt"));
        item.SetText("hello");
        var data = new DataTransfer();
        data.Add(item);
        using var drag = window.SimulateDrag(data);

        drag.Enter(window.PointIn(target));

        Assert.True(operation.GotFiles);
        Assert.True(operation.GotText);
    }

    [AvaloniaFact]
    public void RejectSetsTheMessageAndTheAdornerMirrorsIt()
    {
        var (window, target, operation) = ShowRejectingTarget("Not here");
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        Assert.Equal(DragDropEffects.None, drag.Enter(window.PointIn(target)));

        Assert.True(DropOperationBase.ErrorMessageProperty.IsReadOnly);
        Assert.Equal("Not here", operation.ErrorMessage);
        Assert.Equal("Not here", operation.DropAdorner!.ErrorMessage);
    }

    [AvaloniaFact]
    public void TheMessageIsClearedBeforeEachEvaluation()
    {
        var (window, target, operation) = ShowRejectingTarget("Not here");
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));

        operation.Message = null;

        Assert.Equal(DragDropEffects.Move, drag.Over(window.PointIn(target)));
        Assert.Null(operation.ErrorMessage);
        Assert.Null(operation.DropAdorner!.ErrorMessage);
    }

    [AvaloniaFact]
    public void TheMessageIsClearedWhenTheDragLeaves()
    {
        var (window, target, operation) = ShowRejectingTarget("Not here");
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));

        drag.Leave(window.PointIn(target));

        Assert.Null(operation.ErrorMessage);
        Assert.Null(operation.DropAdorner!.ErrorMessage);
    }

    [AvaloniaFact]
    public void ACopyDropAssignsTheCreatedCopy()
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        DropEventArgs? args = null;
        var operation = new CopyingDropOperation { PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) };
        DragDrop.SetDropOperation(target, operation);
        var window = TestWindow.Show(target);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"), DragDropEffects.Copy | DragDropEffects.Move);

        drag.Enter(window.PointIn(target), RawInputModifiers.Control);
        Assert.Equal(DragDropEffects.Copy, drag.Drop(window.PointIn(target), RawInputModifiers.Control));

        Assert.Equal("A copy", operation.PayloadTarget);
        Assert.Equal(["A copy"], args?.Items);
    }

    [AvaloniaTheory]
    [InlineData(RawInputModifiers.None, DragDropEffects.Move)]
    [InlineData(RawInputModifiers.Control, DragDropEffects.Copy)]
    public void DropEventArgsCarriesTheDropEffect(RawInputModifiers modifiers, DragDropEffects expected)
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        DropEventArgs? args = null;
        DragDrop.SetDropOperation(target, new ItemDropOperation { PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) });
        var window = TestWindow.Show(target);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"), DragDropEffects.Copy | DragDropEffects.Move);

        drag.Enter(window.PointIn(target), modifiers);
        drag.Drop(window.PointIn(target), modifiers);

        Assert.Equal(expected, args?.Effect);
    }

    [AvaloniaFact]
    public void DetachingCancelsAPendingLeave()
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var operation = new LeaveCountingDropOperation();
        DragDrop.SetDropOperation(target, operation);
        var window = TestWindow.Show(target);
        var data = DragData.For(new ItemDragOperation(), "A");
        using var drag = window.SimulateDrag(data);
        drag.Enter(window.PointIn(target));
        Raise(target, AvaDragDrop.DragLeaveEvent, data);

        DragDrop.SetDropOperation(target, null);
        Dispatcher.UIThread.RunJobs();

        // Once, from the detach
        Assert.Equal(1, operation.Leaves);
    }

    [AvaloniaFact]
    public void DetachingClearsDropoverAndHidesTheAdorner()
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var operation = new ItemDropOperation();
        DragDrop.SetDropOperation(target, operation);
        var window = TestWindow.Show(target);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));
        Assert.True(operation.DropAdorner!.IsAttached);

        DragDrop.SetDropOperation(target, null);

        Assert.DoesNotContain(":dropover", target.Classes);
        Assert.False(operation.DropAdorner.IsAttached);
    }

    [AvaloniaFact]
    public void AValidDragIsMarkedHandled()
    {
        var operation = new RejectingDropOperation();
        var (window, target) = Show(operation);
        var handled = new List<bool>();
        foreach (var routedEvent in new[] { AvaDragDrop.DragEnterEvent, AvaDragDrop.DragOverEvent, AvaDragDrop.DropEvent })
            window.AddHandler(routedEvent, (_, e) => handled.Add(e.Handled), handledEventsToo: true);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        drag.Enter(window.PointIn(target));
        drag.Over(window.PointIn(target));
        operation.Message = "Not here";
        drag.Over(window.PointIn(target));
        operation.Message = null;
        drag.Drop(window.PointIn(target));

        Assert.Equal([true, true, false, true], handled);
    }

    [AvaloniaFact]
    public void ADragThatStopsBeingMeantForTheTargetClearsTheHover()
    {
        var operation = new RejectingDropOperation { Message = "Not here" };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));
        Assert.True(operation.DropAdorner!.IsAttached);

        using var other = window.SimulateDrag(DragData.For(new ItemDragOperation { InteractionIds = ["other"] }, "A"));
        Assert.Equal(DragDropEffects.None, other.Over(window.PointIn(target)));

        Assert.DoesNotContain(":dropover", target.Classes);
        Assert.False(operation.DropAdorner.IsAttached);
        Assert.Null(operation.ErrorMessage);
    }

    [AvaloniaFact]
    public void AnInnerTargetThatAcceptsWinsOverItsOuterTarget()
    {
        var child = new Border { Background = Brushes.Green, Width = 40, Height = 40 };
        var inner = new ItemDropOperation();
        DragDrop.SetDropOperation(child, inner);
        var outer = new RejectingDropOperation { Message = "Not here" };
        var (window, _) = Show(outer, child);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));

        Assert.Equal(DragDropEffects.Move, drag.Enter(window.PointIn(child)));
        Assert.Equal(DragDropEffects.Move, drag.Over(window.PointIn(child)));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(child)));

        Assert.Equal("A", inner.PayloadTarget);
        Assert.Null(outer.PayloadTarget);
        Assert.Equal(0, outer.Evaluations);
    }

    [AvaloniaFact]
    public void ACommandOnlyBaseSubclassCompletesAMove()
    {
        var source = new RecordingDragOperation();
        DropEventArgs? args = null;
        var operation = new CommandOnlyDropOperation { PayloadCommand = new RelayCommand(a => args = (DropEventArgs?)a) };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(source, "A"));

        drag.Enter(window.PointIn(target));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));

        Assert.Equal(["A"], args?.Items);
        Assert.Null(operation.PayloadTarget);
        var (effect, dropInfo) = Assert.Single(source.Completions);
        Assert.Equal(DragDropEffects.Move, effect);
        Assert.Same(target, dropInfo.HoveredControl);
    }

    [AvaloniaFact]
    public void ABaseSubclassWithoutACommandReportsNone()
    {
        var source = new RecordingDragOperation();
        var operation = new CommandOnlyDropOperation();
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(source, "A"));

        Assert.Equal(DragDropEffects.Move, drag.Enter(window.PointIn(target)));
        Assert.Equal(DragDropEffects.None, drag.Drop(window.PointIn(target)));

        Assert.Empty(source.Completions);
        Assert.Null(operation.PayloadTarget);
    }

    [AvaloniaTheory]
    [InlineData(new object[] { "a", "b" }, true, true)]
    [InlineData(new object[] { "a", 42 }, false, true)]
    [InlineData(new object[] { 42, "a" }, false, false)]
    public void TryGetPayloadsSucceedsOnlyWhenEveryPayloadIsOfTheType(object[] payloads, bool allStrings, bool firstIsString)
    {
        var operation = new PayloadsProbingDropOperation();
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new CollectionDragOperation(), payloads));

        drag.Enter(window.PointIn(target));

        Assert.Equal(allStrings, operation.Strings is not null);
        Assert.Equal(payloads, operation.Objects);
        Assert.Equal(firstIsString, operation.FirstString is not null);
    }

    [AvaloniaFact]
    public void AcceptsMultiplePayloadsDefaultsToTrueExceptOnItemTargets()
    {
        Assert.True(new CommandOnlyDropOperation().AcceptsMultiplePayloads);
        Assert.True(new CollectionDropOperation().AcceptsMultiplePayloads);
        Assert.True(new TreeDropOperation().AcceptsMultiplePayloads);
        Assert.False(new ItemDropOperation().AcceptsMultiplePayloads);
    }

    private static (Window, Border) Show(DropOperationBase operation, Control? child = null)
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100, Child = child };
        DragDrop.SetDropOperation(target, operation);
        return (TestWindow.Show(target), target);
    }

    // Raised directly, because the headless platform's drag events run pending jobs before returning
    private static void Raise(Border target, RoutedEvent<DragEventArgs> routedEvent, IDataTransfer data) =>
        target.RaiseEvent(new DragEventArgs(routedEvent, data, target, default, KeyModifiers.None));

    private static (Window, Border, RejectingDropOperation) ShowRejectingTarget(string message)
    {
        var operation = new RejectingDropOperation { Message = message };
        var (window, target) = Show(operation);
        return (window, target, operation);
    }

    private sealed class CommandOnlyDropOperation : DropOperationBase;

    private sealed class PayloadsProbingDropOperation : DropOperationBase
    {
        public IReadOnlyList<string>? Strings { get; private set; }
        public IReadOnlyList<object>? Objects { get; private set; }
        public string? FirstString { get; private set; }

        protected override bool CanDrop(DragEventArgs e)
        {
            Strings = TryGetPayloads<string>(e, out var strings) ? strings : null;
            Objects = TryGetPayloads<object>(e, out var objects) ? objects : null;
            FirstString = TryGetPayload<string>(e, out var first) ? first : null;
            return base.CanDrop(e);
        }
    }

    private sealed class CopyingDropOperation : ItemDropOperation
    {
        protected override object CreateCopy(object payload) => $"{payload} copy";
    }

    private sealed class LeaveCountingDropOperation : ItemDropOperation
    {
        public int Leaves { get; private set; }

        protected override void OnDragLeave(DragEventArgs e) => Leaves++;
    }

    private sealed class RejectingDropOperation : ItemDropOperation
    {
        public string? Message { get; set; }

        public int Evaluations { get; private set; }

        public int Drops { get; private set; }

        protected override bool CanDrop(DragEventArgs e)
        {
            Evaluations++;
            return Message is null ? base.CanDrop(e) : Reject(Message);
        }

        protected override void OnDrop(DragEventArgs dragEventArgs) => Drops++;
    }

    private sealed class ProbingDropOperation : ItemDropOperation
    {
        public bool GotString { get; private set; }
        public bool GotUri { get; private set; }
        public bool GotDragInfo { get; private set; }
        public bool GotCollectionDragInfo { get; private set; }
        public bool GotFiles { get; private set; }
        public bool GotText { get; private set; }

        protected override bool CanDrop(DragEventArgs e)
        {
            GotString = TryGetPayload<string>(e, out _);
            GotUri = TryGetPayload<Uri>(e, out _);
            GotDragInfo = TryGetDragInfo<DragInfo>(e, out _);
            GotCollectionDragInfo = TryGetDragInfo<CollectionDragInfo>(e, out _);
            GotFiles = TryGetFiles(e, out _);
            GotText = TryGetText(e, out _);
            return base.CanDrop(e);
        }
    }
}
