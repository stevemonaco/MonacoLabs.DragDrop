using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
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

    private static (Window, Border, Border, T, ItemDropOperation) Show<T>(T operation, Control? content = null) where T : ItemDragOperation
    {
        if (content is not null)
        {
            content.HorizontalAlignment = HorizontalAlignment.Center;
            content.VerticalAlignment = VerticalAlignment.Center;
        }

        var source = new Border { Background = Brushes.Red, Width = 100, Height = 100, Child = content };
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var drop = new ItemDropOperation();
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
