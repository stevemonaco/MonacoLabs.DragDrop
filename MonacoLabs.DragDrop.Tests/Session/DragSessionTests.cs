using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using System.Collections.Specialized;
using Xunit;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop.Tests;

public class DragSessionTests
{
    [AvaloniaFact]
    public void EndingTheDragDisposesThePreview()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" });
        DragPreviewAdorner? preview = null;
        var layer = AdornerLayer.GetAdornerLayer(scene.Source)!;
        NotifyCollectionChangedEventHandler capture = (_, e) => preview ??= e.NewItems?.OfType<DragPreviewAdorner>().FirstOrDefault();
        layer.Children.CollectionChanged += capture;

        scene.Window.MouseDrag(scene.InSource, scene.InTarget);
        layer.Children.CollectionChanged -= capture;

        Assert.NotNull(preview);
        Assert.DoesNotContain(preview, layer.Children);
        Assert.True(preview.IsDisposed);
    }

    [AvaloniaTheory]
    [InlineData(Outcome.DropOnTarget)]
    [InlineData(Outcome.DropOnBackground)]
    [InlineData(Outcome.Cancel)]
    public void TheDragVisualIsDraggingUntilTheDragEndsWhateverTheOutcome(Outcome outcome)
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A", RemoveOnDropOutside = true });

        using (var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget))
        {
            drag.MoveTo(outcome == Outcome.DropOnBackground ? scene.OnBackground : scene.InTarget);
            Assert.Contains(":dragging", scene.Source.Classes);

            if (outcome == Outcome.Cancel)
                drag.Cancel();
        }

        Assert.DoesNotContain(":dragging", scene.Source.Classes);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThePreviewIsAddedToTheSourcesAdornerLayerWhenShown(bool showPreview)
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A", ShowDragPreview = showPreview });

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);

        Assert.Equal(showPreview, scene.Preview is not null);
    }

    [AvaloniaFact]
    public void AVisualWithoutSizeHasNoPreview() => Assert.Null(DragPreviewAdorner.Create(new Border(), default));

    [AvaloniaFact]
    public void TheSnapshotShowsTheVisualsNormalLook()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" }, new Border { Name = "source", Width = 100, Height = 100 });
        scene.Window.Styles.Add(new Style(x => x.OfType<Border>().Name("source")) { Setters = { new Setter(Border.BackgroundProperty, Brushes.Red) } });
        scene.Window.Styles.Add(new Style(x => x.OfType<Border>().Name("source").Class(":dragging")) { Setters = { new Setter(Border.BackgroundProperty, Brushes.Lime) } });

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);

        Assert.Equal(Brushes.Lime, ((Border)scene.Source).Background);
        Assert.Equal(Colors.Red, WindowSnapshot.Of(SnapshotOf(scene.Preview!))[50, 50]);
    }

    [AvaloniaFact]
    public void TheSnapshotUsesTheRenderScalingAndKeepsABordersCornerRadius()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" }, new Border { Background = Brushes.Red, Width = 100, Height = 80, CornerRadius = new CornerRadius(8) });

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);

        Assert.Equal(PixelSize.FromSize(new Size(100, 80), scene.Window.RenderScaling), SnapshotOf(scene.Preview!).PixelSize);
        Assert.Equal(new CornerRadius(8), scene.Preview!.CornerRadius);
    }

    [AvaloniaFact]
    public void TheSnapshotKeepsATemplatedControlsCornerRadius()
    {
        var source = new ContentControl { Background = Brushes.Red, Width = 100, Height = 100, CornerRadius = new CornerRadius(5) };
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" }, source);

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);

        Assert.Equal(new CornerRadius(5), scene.Preview!.CornerRadius);
    }

    [AvaloniaFact]
    public void ATransparentVisualShowsTheNearestOpaqueBackgroundBehindIt()
    {
        var source = new Border { Background = Brushes.Transparent, Width = 100, Height = 100 };
        var translucent = new Border { Background = new SolidColorBrush(Color.Parse("#80FFFF00")), Child = source };
        var opaque = new Border { Background = Brushes.Yellow, Child = translucent };
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" }, source, opaque);

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);

        Assert.Equal(Brushes.Yellow, BackdropOf(scene.Preview!));
    }

    [AvaloniaFact]
    public void ANonSolidBackgroundCountsAsOpaque()
    {
        var gradient = new LinearGradientBrush { GradientStops = { new GradientStop(Colors.Yellow, 0), new GradientStop(Colors.Transparent, 1) } };
        var source = new Border { Background = Brushes.Transparent, Width = 100, Height = 100 };
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" }, source, new Border { Background = gradient, Child = source });

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);

        Assert.Same(gradient, BackdropOf(scene.Preview!));
    }

    [AvaloniaFact]
    public void ThePreviewIsHiddenUntilTheDragReachesTheWindowThenFollowsThePointer()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" });
        var press = scene.Window.PointIn(scene.Source, new Point(30, 40));

        using var drag = HeldMouseDrag.Start(scene.Window, press, scene.InTarget);
        var preview = scene.Preview!;
        Assert.False(preview.IsVisible);

        var pointer = scene.Window.PointIn(scene.Target, new Point(20, 25));
        drag.MoveTo(pointer);

        var layer = AdornerLayer.GetAdornerLayer(scene.Source)!;
        var inLayer = scene.Window.TranslatePoint(pointer, layer)!.Value;
        Assert.True(preview.IsVisible);
        Assert.Equal(inLayer.X - 30, Canvas.GetLeft(preview));
        Assert.Equal(inLayer.Y - 40, Canvas.GetTop(preview));
    }

    [AvaloniaFact]
    public void ThePreviewHidesWhenTheDragLeavesTheWindow()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" });

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);
        drag.MoveTo(scene.InTarget);
        Assert.True(scene.Preview!.IsVisible);

        drag.MoveTo(new Point(600, 600));
        Assert.False(scene.Preview!.IsVisible);

        drag.MoveTo(scene.InTarget);
        Assert.True(scene.Preview!.IsVisible);
    }

    [AvaloniaFact]
    public void ThePreviewHidesOnDropAndLeavesTheLayer()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" });
        var layer = AdornerLayer.GetAdornerLayer(scene.Source)!;
        var visibility = new List<bool>();

        using (var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget))
        {
            var preview = scene.Preview!;
            preview.PropertyChanged += (_, e) => { if (e.Property == Visual.IsVisibleProperty && preview.Parent is not null) visibility.Add(preview.IsVisible); };
            drag.MoveTo(scene.InTarget);
            drag.Release();
        }

        Assert.Equal([true, false], visibility);
        Assert.Empty(layer.Children.OfType<DragPreviewAdorner>());
    }

    [AvaloniaFact]
    public void ThePreviewShowsWhetherTheDropWouldCopyOrRemove()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A", AllowedEffects = DragDropEffects.Copy | DragDropEffects.Move, RemoveOnDropOutside = true });

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);
        drag.MoveTo(scene.InTarget);
        Assert.Empty(EffectClasses(scene.Preview!));

        drag.MoveTo(scene.InTarget, RawInputModifiers.Control);
        Assert.Equal([":copy"], EffectClasses(scene.Preview!));

        drag.MoveTo(scene.OnBackground, RawInputModifiers.Control);
        Assert.Equal([":remove"], EffectClasses(scene.Preview!));
    }

    [AvaloniaFact]
    public void ThePreviewShowsNoDropWhereNothingAcceptsTheDrag()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" });

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);
        drag.MoveTo(scene.OnBackground);

        Assert.Equal([":nodrop"], EffectClasses(scene.Preview!));
    }

    [AvaloniaTheory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void TheSessionAllowsDropsOnTheWindowOnlyForTheDrag(bool showPreview, bool removeOnDropOutside, bool allowsDrops)
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A", ShowDragPreview = showPreview, RemoveOnDropOutside = removeOnDropOutside });

        using (HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget))
            Assert.Equal(allowsDrops, AvaDragDrop.GetAllowDrop(scene.Window));

        Assert.False(scene.Window.IsSet(AvaDragDrop.AllowDropProperty));
    }

    [AvaloniaFact]
    public void AWindowThatAlreadyAllowsDropsKeepsIt()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" });
        AvaDragDrop.SetAllowDrop(scene.Window, true);

        scene.Window.MouseDrag(scene.InSource, scene.InTarget);

        Assert.True(AvaDragDrop.GetAllowDrop(scene.Window));
    }

    [AvaloniaTheory]
    [InlineData(false, DragDropEffects.Move, DragDropEffects.None)]
    [InlineData(true, DragDropEffects.Copy | DragDropEffects.Move, DragDropEffects.Move)]
    [InlineData(true, DragDropEffects.Copy | DragDropEffects.Link, DragDropEffects.Copy)]
    [InlineData(true, DragDropEffects.Link, DragDropEffects.Link)]
    public void WhereNothingAllowsDropsTheSessionReportsNoneOrTheRemoveEffect(bool removeOnDropOutside, DragDropEffects allowed, DragDropEffects reported)
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A", AllowedEffects = allowed, RemoveOnDropOutside = removeOnDropOutside });
        using var effects = new ReportedEffects(scene.Window);

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);
        drag.MoveTo(scene.OnBackground);

        Assert.Equal(reported, effects.Last);
    }

    [AvaloniaFact]
    public void OverAnElementThatAllowsDropsTheSessionLeavesTheEffectAlone()
    {
        var scene = Scene.Show(new ItemDragOperation { Payload = "A" });
        using var effects = new ReportedEffects(scene.Window);

        using var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget);
        drag.MoveTo(scene.InTarget);

        Assert.Equal(DragDropEffects.Move, effects.Last);
    }

    [AvaloniaTheory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void ADropWhereNothingAllowsDropsIsOutsideAnyTargetWithRemoveOnDropOutside(bool removeOnDropOutside, int calls)
    {
        var operation = new OutsideRecordingDragOperation { Payload = "A", RemoveOnDropOutside = removeOnDropOutside };
        var scene = Scene.Show(operation);

        scene.Window.MouseDrag(scene.InSource, scene.OnBackground);

        Assert.Equal(calls, operation.Outside.Count);
    }

    [AvaloniaFact]
    public void ADropOnATargetIsNotOutside()
    {
        var operation = new OutsideRecordingDragOperation { Payload = "A", RemoveOnDropOutside = true };
        var scene = Scene.Show(operation);

        scene.Window.MouseDrag(scene.InSource, scene.InTarget);

        Assert.Empty(operation.Outside);
    }

    [AvaloniaFact]
    public void AWindowThatAllowsDropsItselfStillHasAreasOutsideAnyTarget()
    {
        var operation = new OutsideRecordingDragOperation { Payload = "A", RemoveOnDropOutside = true };
        var scene = Scene.Show(operation);
        AvaDragDrop.SetAllowDrop(scene.Window, true);
        using var effects = new ReportedEffects(scene.Window);

        using (var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.OnBackground))
        {
            drag.MoveTo(scene.OnBackground);
            Assert.Equal(DragDropEffects.Move, effects.Last);
            Assert.Equal([":remove"], EffectClasses(scene.Preview!));
            drag.Release();
        }

        Assert.Single(operation.Outside);
        Assert.Null(operation.Payload);
    }

    [AvaloniaFact]
    public void AnAreaWhoseDragEventsTheAppHandlesIsNotOutsideAnyTarget()
    {
        var operation = new OutsideRecordingDragOperation { Payload = "A", AllowedEffects = DragDropEffects.Copy | DragDropEffects.Move, RemoveOnDropOutside = true };
        var scene = Scene.Show(operation);
        AvaDragDrop.SetAllowDrop(scene.Window, true);
        var appDrops = 0;
        scene.Window.AddHandler(AvaDragDrop.DragEnterEvent, AcceptAsCopy);
        scene.Window.AddHandler(AvaDragDrop.DragOverEvent, AcceptAsCopy);
        scene.Window.AddHandler(AvaDragDrop.DropEvent, (s, e) => { appDrops++; AcceptAsCopy(s, e); });
        using var effects = new ReportedEffects(scene.Window);

        using (var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.OnBackground))
        {
            drag.MoveTo(scene.OnBackground);
            Assert.Equal(DragDropEffects.Copy, effects.Last);
            Assert.Equal([":copy"], EffectClasses(scene.Preview!));
            drag.Release();
        }

        Assert.Equal(1, appDrops);
        Assert.Empty(operation.Outside);
        Assert.Equal("A", operation.Payload);

        static void AcceptAsCopy(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    [AvaloniaTheory]
    [InlineData(Outcome.Cancel)]
    [InlineData(Outcome.DropOutsideTheWindow)]
    public void ACanceledDragOrADropOutsideTheWindowIsNeverOutsideAnyTarget(Outcome outcome)
    {
        var operation = new OutsideRecordingDragOperation { Payload = "A", RemoveOnDropOutside = true };
        var scene = Scene.Show(operation);

        using (var drag = HeldMouseDrag.Start(scene.Window, scene.InSource, scene.InTarget))
        {
            drag.MoveTo(scene.OnBackground);
            if (outcome == Outcome.Cancel)
                drag.Cancel();
            else
                drag.MoveTo(new Point(600, 600));
        }

        Assert.Empty(operation.Outside);
        Assert.Equal("A", operation.Payload);
    }

    [AvaloniaFact]
    public void ThePreviewIsPassiveTranslucentShadowedAndOnTop()
    {
        var source = new Border { Background = Brushes.Red, Width = 50, Height = 50 };
        TestWindow.Show(source);

        using var preview = DragPreviewAdorner.Create(source, default)!;

        Assert.False(preview.IsHitTestVisible);
        Assert.Equal(0.9, preview.Opacity);
        Assert.Equal(BoxShadows.Parse("0 8 20 0 #40000000"), preview.BoxShadow);
        Assert.Equal(int.MaxValue, preview.ZIndex);
    }

    [AvaloniaFact]
    public void APreviewOfSeveralPayloadsShowsTheirCount()
    {
        var items = new List<string> { "a", "b", "c" };
        var list = new ListBox
        {
            SelectionMode = SelectionMode.Multiple,
            ItemsSource = items,
            Width = 150,
            Height = 200,
            HorizontalAlignment = HorizontalAlignment.Left,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Height = 30 })
        };
        DragDrop.SetDragOperation(list, new CollectionDragOperation { SourceCollection = items });
        var window = TestWindow.Show(list);
        list.Selection.Select(0);
        list.Selection.Select(2);
        using var single = DragPreviewAdorner.Create(list, default)!;

        using (var drag = HeldMouseDrag.Start(window, window.PointIn(list.ContainerFromIndex(0)!), new Point(300, 100)))
        {
            drag.MoveTo(new Point(310, 110));

            var preview = AdornerLayer.GetAdornerLayer(list)!.Children.OfType<DragPreviewAdorner>().Single();
            Assert.Equal(2, preview.PayloadCount);
            Assert.Contains(":multiple", preview.Classes);
            var count = preview.GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal("2", count.Text);
            Assert.Equal((12, FontWeight.SemiBold, Colors.White), (count.FontSize, count.FontWeight, ((ISolidColorBrush)count.Foreground!).Color));
            var badge = (Border)count.Parent!;
            Assert.Equal(Color.Parse("#0078D4"), ((ISolidColorBrush)badge.Background!).Color);
            Assert.Equal((20, new CornerRadius(10)), (badge.Height, badge.CornerRadius));
            Assert.Equal((HorizontalAlignment.Right, VerticalAlignment.Top), (badge.HorizontalAlignment, badge.VerticalAlignment));
            Assert.Equal(new Thickness(0, -8, -8, 0), badge.Margin);
            drag.Cancel();
        }

        Assert.Equal(1, single.PayloadCount);
        Assert.DoesNotContain(":multiple", single.Classes);
        Assert.Empty(single.GetVisualDescendants().OfType<TextBlock>());
    }

    public enum Outcome { DropOnTarget, DropOnBackground, Cancel, DropOutsideTheWindow }

    private static Bitmap SnapshotOf(DragPreviewAdorner preview) => (Bitmap)preview.GetVisualDescendants().OfType<Image>().Single().Source!;

    private static IBrush? BackdropOf(DragPreviewAdorner preview) => ((Border)preview.Child!).Background;

    private static string[] EffectClasses(DragPreviewAdorner preview) => [.. preview.Classes.Where(c => c is ":nodrop" or ":copy" or ":remove").Order()];

    private sealed record Scene(Window Window, Control Source, Border Target, RecordingDropOperation Drop)
    {
        public Point InSource => Window.PointIn(Source);
        public Point InTarget => Window.PointIn(Target);
        public Point OnBackground => new(350, 250);
        public DragPreviewAdorner? Preview => AdornerLayer.GetAdornerLayer(Source)!.Children.OfType<DragPreviewAdorner>().SingleOrDefault();

        public static Scene Show(ItemDragOperation operation, Control? source = null, Control? sourceHost = null)
        {
            source ??= new Border { Background = Brushes.Red, Width = 100, Height = 100 };
            DragDrop.SetDragOperation(source, operation);
            var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
            var drop = new RecordingDropOperation();
            DragDrop.SetDropOperation(target, drop);
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, VerticalAlignment = VerticalAlignment.Top, Children = { sourceHost ?? source, target } };
            return new Scene(TestWindow.Show(panel), source, target, drop);
        }
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
