using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MonacoLabs.DragDrop.Abstractions;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class DragGestureTests
{
    [AvaloniaFact]
    public void SettingRoutingWhileAttachedThrows()
    {
        var operation = new ItemDragOperation();
        operation.Attach(new Border());

        Assert.Throws<InvalidOperationException>(() => operation.Routing = RoutingStrategies.Tunnel);
        Assert.Throws<InvalidOperationException>(() => operation.HandledEventsToo = true);
        Assert.Equal(RoutingStrategies.Bubble, operation.Routing);
        Assert.False(operation.HandledEventsToo);
    }

    [AvaloniaFact]
    public void RoutingCanBeSetWhileDetached()
    {
        var operation = new ItemDragOperation { Routing = RoutingStrategies.Tunnel, HandledEventsToo = true };
        var control = new Border();
        operation.Attach(control);
        operation.Detach(control);

        operation.Routing = RoutingStrategies.Bubble;
        operation.HandledEventsToo = false;

        Assert.Equal(RoutingStrategies.Bubble, operation.Routing);
        Assert.False(operation.HandledEventsToo);
    }

    [AvaloniaTheory]
    [InlineData(RoutingStrategies.Bubble, false, false)]
    [InlineData(RoutingStrategies.Bubble, true, true)]
    [InlineData(RoutingStrategies.Tunnel, false, true)]
    public void RoutingDecidesWhetherAPressAChildHandlesStartsADrag(RoutingStrategies routing, bool handledEventsToo, bool starts)
    {
        var child = new Border { Background = Brushes.Green, Width = 40, Height = 40 };
        child.PointerPressed += (_, e) => e.Handled = true;
        var (window, _, target, _, drop) = Show(new ItemDragOperation { Payload = "A", Routing = routing, HandledEventsToo = handledEventsToo }, child);

        window.MouseDrag(window.PointIn(child), window.PointIn(target));

        Assert.Equal(starts ? "A" : null, drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void AnOnPointerMovedOverrideCanAwaitBeforeCallingTheBase()
    {
        var (window, source, target, operation, drop) = Show(new YieldingDragOperation { Payload = "A" });

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.True(operation.Resumed);
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void ADragDoesNotStartTwiceFromOnePress()
    {
        var (window, source, target, operation, drop) = Show(new CountingDragOperation { Payload = "A" });

        window.MouseDrag(window.PointIn(source), [window.PointIn(target), window.PointIn(target) + new Point(5, 5), window.PointIn(target)]);

        Assert.Equal(1, operation.Starts);
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void MovingPastTheThresholdStartsADrag()
    {
        var (window, source, _, operation, _) = Show(new CountingDragOperation { Payload = "A", DragThreshold = 10.5 });
        var press = window.PointIn(source);

        window.MouseDrag(press, press + new Point(10.5, 0));

        Assert.Equal(1, operation.Starts);
    }

    [AvaloniaFact]
    public void MovingWithinTheThresholdDoesNotStartADrag()
    {
        var (window, source, _, operation, _) = Show(new CountingDragOperation { Payload = "A", DragThreshold = 10.5 });
        var press = window.PointIn(source);

        window.MouseDrag(press, press + new Point(7.4, 7.4));

        Assert.Equal(0, operation.Starts);
    }

    [AvaloniaFact]
    public void AMoveWithoutTheButtonPressedDoesNotStartADrag()
    {
        var (window, source, target, operation, _) = Show(new CountingDragOperation { Payload = "A" });
        var press = window.PointIn(source);

        window.MouseDown(press, MouseButton.Left);
        window.MouseMove(window.PointIn(target));
        window.MouseMove(window.PointIn(target) + new Point(5, 5), RawInputModifiers.LeftMouseButton);
        window.MouseUp(window.PointIn(target), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, operation.Starts);
    }

    [AvaloniaFact]
    public void LosingCaptureEndsTheTrackedPress()
    {
        var (window, source, _, operation, _) = Show(new CountingDragOperation { Payload = "A" });
        IPointer? pointer = null;
        source.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel, handledEventsToo: true);
        var withinSource = window.PointIn(source, new Point(5, 5));

        window.MouseDown(window.PointIn(source), MouseButton.Left);
        Assert.NotNull(pointer?.Captured);
        pointer.Capture(null);
        window.MouseMove(withinSource, RawInputModifiers.LeftMouseButton);
        window.MouseUp(withinSource, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, operation.Starts);
    }

    [AvaloniaFact]
    public void AThrowingPlatformDragIsLoggedAndResetsState()
    {
        var (window, source, target, operation, drop) = Show(new ThrowingDragOperation { Payload = "A" });
        using var log = RecordingLogSink.Install();

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        var entry = Assert.Single(log.Entries, x => x.Area == "DragDrop");
        Assert.Equal(LogEventLevel.Error, entry.Level);
        Assert.Same(operation, entry.Source);
        Assert.Contains(entry.Values, x => x is InvalidOperationException { Message: "platform drag failed" });
        Assert.Null(drop.PayloadTarget);

        operation.Throws = false;
        window.MouseDrag(window.PointIn(source), window.PointIn(target));
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void AThrowingOnPointerMovedOverrideIsLogged()
    {
        var (window, source, target, operation, _) = Show(new ThrowingMoveDragOperation { Payload = "A" });
        using var log = RecordingLogSink.Install();

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.NotEmpty(log.Entries);
        Assert.All(log.Entries, x => Assert.Equal(("DragDrop", LogEventLevel.Error), (x.Area, x.Level)));
        Assert.Contains(log.Entries, x => x.Values.Any(v => v is InvalidOperationException { Message: "override failed" }));
    }

    [AvaloniaFact]
    public void TheThresholdDefaultsToSixDips()
    {
        object threshold = new ItemDragOperation().DragThreshold;

        Assert.Equal(6d, Assert.IsType<double>(threshold));
    }

    [AvaloniaFact]
    public void PressesInsideATextBoxDoNotStartDrags()
    {
        var textBox = new TextBox { Width = 80, Height = 32 };
        var (window, source, target, _, drop) = Show(new ItemDragOperation { Payload = "A", Routing = RoutingStrategies.Tunnel }, textBox);

        window.MouseDrag(window.PointIn(textBox), window.PointIn(target));
        Assert.Null(drop.PayloadTarget);

        window.MouseDrag(window.PointIn(source, new Point(5, 5)), window.PointIn(target));
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void ATextBoxInASelectedRowReceivesItsPress()
    {
        var list = new ListBox
        {
            ItemsSource = new List<string> { "a", "b" },
            Width = 200,
            Height = 200,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBox { Width = 120, Height = 32 }),
            SelectedIndex = 0
        };
        DragDrop.SetDragOperation(list, new CollectionDragOperation());
        var window = TestWindow.Show(list);
        var textBox = list.ContainerFromIndex(0)!.GetVisualDescendants().OfType<TextBox>().Single();

        window.MouseDown(window.PointIn(textBox), MouseButton.Left);
        window.MouseUp(window.PointIn(textBox), MouseButton.Left);

        Assert.True(textBox.IsFocused);
    }

    [AvaloniaFact]
    public void PressesOnASliderThumbDoNotStartDrags()
    {
        var slider = new Slider { Width = 90, Minimum = 0, Maximum = 100, Value = 50 };
        var (window, _, target, _, drop) = Show(new ItemDragOperation { Payload = "A", Routing = RoutingStrategies.Tunnel }, slider);
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();

        window.MouseDrag(window.PointIn(thumb), window.PointIn(target));

        Assert.Null(drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void OverridingIsIgnoredPressSourceAddsControls()
    {
        var label = new TextBlock { Text = "label", Width = 60, Height = 20, Background = Brushes.Transparent };
        var (window, source, target, _, drop) = Show(new IgnoreTextBlockDragOperation { Payload = "A" }, label);

        window.MouseDrag(window.PointIn(label), window.PointIn(target));
        Assert.Null(drop.PayloadTarget);

        window.MouseDrag(window.PointIn(source, new Point(5, 5)), window.PointIn(target));
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void AStyleCanIgnorePressesOnAControlType()
    {
        var label = new TextBlock { Text = "label", Width = 60, Height = 20, Background = Brushes.Transparent };
        var (window, source, target, _, drop) = Show(new ItemDragOperation { Payload = "A" }, label);
        window.Styles.Add(new Style(x => x.OfType<TextBlock>()) { Setters = { new Setter(DragDrop.IgnoresPressProperty, (bool?)true) } });

        window.MouseDrag(window.PointIn(label), window.PointIn(target));
        Assert.Null(drop.PayloadTarget);

        window.MouseDrag(window.PointIn(source, new Point(5, 5)), window.PointIn(target));
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void IgnoresPressFalseLetsAnIgnoredControlStartDrags()
    {
        var textBox = new TextBox { Width = 80, Height = 32, IsReadOnly = true };
        DragDrop.SetIgnoresPress(textBox, false);
        var (window, _, target, _, drop) = Show(new ItemDragOperation { Payload = "A", Routing = RoutingStrategies.Tunnel }, textBox);

        window.MouseDrag(window.PointIn(textBox), window.PointIn(target));

        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void TheElementNearestThePressDecides()
    {
        var optedIn = new Border { Background = Brushes.Green, Width = 20, Height = 20 };
        DragDrop.SetIgnoresPress(optedIn, false);
        var button = new Button { Width = 20, Height = 20 };
        var panel = new StackPanel { Width = 80, Height = 80, Background = Brushes.Yellow, Spacing = 10, Children = { optedIn, button } };
        DragDrop.SetIgnoresPress(panel, true);
        var (window, _, target, _, drop) = Show(new ItemDragOperation { Payload = "A", Routing = RoutingStrategies.Tunnel }, panel);

        window.MouseDrag(window.PointIn(panel, new Point(70, 70)), window.PointIn(target));
        Assert.Null(drop.PayloadTarget);

        window.MouseDrag(window.PointIn(optedIn), window.PointIn(target));
        Assert.Equal("A", drop.PayloadTarget);

        drop.PayloadTarget = null;
        DragDrop.SetIgnoresPress(panel, false);
        window.MouseDrag(window.PointIn(button), window.PointIn(target));
        Assert.Null(drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void IgnoresPressDefaultsToNull() => Assert.Null(DragDrop.GetIgnoresPress(new Border()));

    [AvaloniaFact]
    public void ADragHandleInsideAButtonDoesNotStartADrag()
    {
        var handle = new Border { Background = Brushes.Green, Width = 20, Height = 20 };
        DragDrop.SetIsDragHandle(handle, true);
        var button = new Button { Content = handle, Padding = new Thickness(10) };
        var operation = new ItemDragOperation { Payload = "A", Routing = RoutingStrategies.Tunnel, RequiresDragHandle = true };
        var (window, _, target, _, drop) = Show(operation, button);

        window.MouseDrag(window.PointIn(handle), window.PointIn(target));

        Assert.Null(drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void DetachingStopsTheSourceFromStartingDrags()
    {
        var (window, source, target, operation, drop) = Show(new CountingDragOperation { Payload = "A" });

        DragDrop.SetDragOperation(source, null);
        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Equal(0, operation.Starts);
        Assert.Null(drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void ByDefaultTheSourceBubblesAndIgnoresHandledPresses()
    {
        var operation = new ItemDragOperation();

        Assert.Equal(RoutingStrategies.Bubble, operation.Routing);
        Assert.False(operation.HandledEventsToo);
    }

    [AvaloniaFact]
    public void ALeftPressIsTrackedAndMarkedHandled()
    {
        var (window, source, target, _, drop) = Show(new ItemDragOperation { Payload = "A" });
        bool? handled = null;
        window.AddHandler(InputElement.PointerPressedEvent, (_, e) => handled = e.Handled, handledEventsToo: true);

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.True(handled);
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void WhileCanDragIsFalsePressesAreNotTracked()
    {
        var (window, source, target, operation, drop) = Show(new ItemDragOperation { Payload = "A", CanDrag = false });

        window.MouseDrag(window.PointIn(source), window.PointIn(target));
        Assert.Null(drop.PayloadTarget);

        operation.CanDrag = true;
        window.MouseDrag(window.PointIn(source), window.PointIn(target));
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void WithRequiresDragHandleOnlyPressesInsideAHandleAreTracked()
    {
        var grip = new Border { Background = Brushes.Yellow, Width = 10, Height = 10 };
        var handle = new Border { Background = Brushes.Green, Width = 30, Height = 30, Child = grip, HorizontalAlignment = HorizontalAlignment.Left };
        DragDrop.SetIsDragHandle(handle, true);
        var (window, source, target, _, drop) = Show(new ItemDragOperation { Payload = "A", RequiresDragHandle = true }, handle);

        window.MouseDrag(window.PointIn(source, new Point(90, 90)), window.PointIn(target));
        Assert.Null(drop.PayloadTarget);

        window.MouseDrag(window.PointIn(grip), window.PointIn(target));
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void ARightPressIsIgnored()
    {
        var (window, source, target, operation, _) = Show(new CountingDragOperation { Payload = "A" });

        window.MouseDown(window.PointIn(source), MouseButton.Right);
        window.MouseMove(window.PointIn(target), RawInputModifiers.RightMouseButton);
        window.MouseUp(window.PointIn(target), MouseButton.Right);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, operation.Starts);
    }

    [AvaloniaFact]
    public void ReleasingStopsTrackingThePress()
    {
        // Releasing also drops the pointer capture, so capture loss is ignored to isolate the release
        var (window, source, _, operation, _) = Show(new CaptureIgnoringDragOperation { Payload = "A" });
        var press = window.PointIn(source);

        window.MouseDown(press, MouseButton.Left);
        window.MouseUp(press, MouseButton.Left);
        window.MouseMove(press + new Point(20, 20), RawInputModifiers.LeftMouseButton);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, operation.Starts);
    }

    [AvaloniaFact]
    public void ThePlatformDragStartsWithThePressNotTheMove()
    {
        var (window, source, target, operation, drop) = Show(new TriggerRecordingDragOperation { Payload = "A" });
        var press = window.PointIn(source, new Point(30, 40));

        window.MouseDrag(press, window.PointIn(target));

        Assert.Equal(new Point(30, 40), operation.TriggerPosition);
        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void TheSourceOffersItsAllowedEffects()
    {
        var (window, source, target, _, drop) = Show(new ItemDragOperation { Payload = "A", AllowedEffects = DragDropEffects.Copy | DragDropEffects.Link });
        DragDropEffects? offered = null;
        drop.Probe = e => offered ??= e.DragEffects;

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Equal(DragDropEffects.Copy | DragDropEffects.Link, offered);
    }

    [AvaloniaFact]
    public void WithoutAPayloadTheDataContextIsDragged()
    {
        var (window, source, target, _, drop) = Show(new ItemDragOperation());
        source.DataContext = "context";

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Equal("context", drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void ASetPayloadIsDraggedInsteadOfTheDataContext()
    {
        var (window, source, target, _, drop) = Show(new ItemDragOperation { Payload = "A" });
        source.DataContext = "context";

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Equal("A", drop.PayloadTarget);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANullPayloadStartsNoDrag(bool payloadIsSet)
    {
        var operation = new ItemDragOperation();
        if (payloadIsSet)
            operation.Payload = null;
        var (window, source, target, _, drop) = Show(operation);
        source.DataContext = payloadIsSet ? "context" : null;

        window.MouseDrag(window.PointIn(source), window.PointIn(target));

        Assert.Null(drop.LastDragInfo);
        Assert.Null(drop.PayloadTarget);
    }

    [AvaloniaFact]
    public void PlatformTextIsAStringPayloadOrThePayloadsText()
    {
        var operation = new ItemDragOperation { InteractionIds = [PlatformInteractionIds.Text] };

        Assert.Equal("hello", ((IDataTransfer)DragData.For(operation, "hello")).TryGetText());
        Assert.Equal("42", ((IDataTransfer)DragData.For(operation, 42)).TryGetText());
        Assert.Equal("custom", ((IDataTransfer)DragData.For(new CustomTextDragOperation { InteractionIds = [PlatformInteractionIds.Text] }, 42)).TryGetText());
    }

    [AvaloniaFact]
    public async Task APlatformFileIsAStorageItemPayload()
    {
        var window = TestWindow.Show(new Border());
        var file = await TestFiles.CreateAsync(window, "a.txt");
        var operation = new ItemDragOperation { InteractionIds = [PlatformInteractionIds.Files] };

        Assert.Equal([file], ((IDataTransfer)DragData.For(operation, file)).TryGetFiles());
        Assert.Null(((IDataTransfer)DragData.For(operation, "a.txt")).TryGetFiles());
    }

    [AvaloniaFact]
    public void AllowedEffectsDefaultsToMove() => Assert.Equal(DragDropEffects.Move, new ItemDragOperation().AllowedEffects);

    [AvaloniaFact]
    public void TheSwitchesDefaultToDraggableWithAPreview()
    {
        var operation = new ItemDragOperation();

        Assert.True(operation.CanDrag);
        Assert.False(operation.RequiresDragHandle);
        Assert.False(operation.RemoveOnDropOutside);
        Assert.True(operation.ShowDragPreview);
    }

    private static (Window, Border, Border, T, RecordingDropOperation) Show<T>(T operation, Control? content = null) where T : ItemDragOperation
    {
        if (content is not null)
        {
            content.HorizontalAlignment = HorizontalAlignment.Center;
            content.VerticalAlignment = VerticalAlignment.Center;
        }

        var source = new Border { Background = Brushes.Red, Width = 100, Height = 100, Child = content };
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var drop = new RecordingDropOperation();
        DragDrop.SetDragOperation(source, operation);
        DragDrop.SetDropOperation(target, drop);
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { source, target } };
        return (TestWindow.Show(panel), source, target, operation, drop);
    }

    private sealed class YieldingDragOperation : ItemDragOperation
    {
        public bool Resumed { get; private set; }

        protected override async Task OnPointerMoved(PointerEventArgs e)
        {
            await Task.Yield();
            Resumed = true;
            await base.OnPointerMoved(e);
        }
    }

    private sealed class IgnoreTextBlockDragOperation : ItemDragOperation
    {
        protected override bool IsIgnoredPressSource(Visual element) => base.IsIgnoredPressSource(element) || element is TextBlock;
    }

    private sealed class ThrowingDragOperation : ItemDragOperation
    {
        public bool Throws { get; set; } = true;

        protected override Task DoDragDrop(PointerPressedEventArgs triggerEvent, DragInfo metadata, object payload) =>
            Throws ? throw new InvalidOperationException("platform drag failed") : base.DoDragDrop(triggerEvent, metadata, payload);
    }

    private sealed class ThrowingMoveDragOperation : ItemDragOperation
    {
        protected override async Task OnPointerMoved(PointerEventArgs e)
        {
            await Task.Yield();
            throw new InvalidOperationException("override failed");
        }
    }

    private sealed class TriggerRecordingDragOperation : ItemDragOperation
    {
        public Point? TriggerPosition { get; private set; }

        protected override Task DoDragDrop(PointerPressedEventArgs triggerEvent, DragInfo metadata, object payload)
        {
            TriggerPosition = triggerEvent.GetPosition(AttachedControl);
            return base.DoDragDrop(triggerEvent, metadata, payload);
        }
    }

    private sealed class CaptureIgnoringDragOperation : ItemDragOperation
    {
        public int Starts { get; private set; }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
        }

        protected override Task StartDrag(PointerEventArgs e)
        {
            Starts++;
            return base.StartDrag(e);
        }
    }

    private sealed class CustomTextDragOperation : ItemDragOperation
    {
        protected override string? ResolvePlatformText(object payload) => "custom";
    }

    private sealed class CountingDragOperation : ItemDragOperation
    {
        public int Starts { get; private set; }

        protected override Task StartDrag(PointerEventArgs e)
        {
            Starts++;
            return base.StartDrag(e);
        }
    }
}
