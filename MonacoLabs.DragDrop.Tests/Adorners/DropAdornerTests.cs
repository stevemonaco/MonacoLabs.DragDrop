using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;
using AvaDragDrop = Avalonia.Input.DragDrop;
using Path = Avalonia.Controls.Shapes.Path;

namespace MonacoLabs.DragDrop.Tests;

public class DropAdornerTests
{
    private static readonly Color Accent = Color.Parse("#0078D4");
    private static readonly Color AccentFill = Color.Parse("#240078D4");
    private static readonly Color Error = Color.Parse("#C42B1C");
    private static readonly Color ErrorFill = Color.Parse("#24C42B1C");

    [AvaloniaTheory]
    [InlineData(typeof(DropHighlightAdorner))]
    [InlineData(typeof(DropInsertionAdorner))]
    public void DropAdornersAreNotHitTestableAndRefuseDrops(Type type)
    {
        var adorner = (DropAdornerBase)Activator.CreateInstance(type)!;

        Assert.False(adorner.IsHitTestVisible);
        Assert.False(AvaDragDrop.GetAllowDrop(adorner));
    }

    [AvaloniaFact]
    public void DragsOverAShownAdornerStillReachTheTarget()
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        var operation = new ItemDropOperation();
        DragDrop.SetDropOperation(target, operation);
        var window = TestWindow.Show(target);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation(), "A"));
        drag.Enter(window.PointIn(target));
        Assert.True(operation.DropAdorner!.IsAttached);

        Assert.Equal(DragDropEffects.Move, drag.Over(window.PointIn(target)));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));

        Assert.Equal("A", operation.PayloadTarget);
    }

    [AvaloniaFact]
    public void AnInvalidAdornerHasTheErrorPseudoClass()
    {
        var adorner = new DropHighlightAdorner();
        Assert.Contains(":error", adorner.Classes);

        adorner.IsDropValid = true;
        Assert.DoesNotContain(":error", adorner.Classes);

        adorner.IsDropValid = false;
        Assert.Contains(":error", adorner.Classes);
    }

    [AvaloniaFact]
    public async Task AttachingPlacesTheAdornerOverTheTargetAndEasesItIn()
    {
        var (target, adorner) = ShowTarget(new DropHighlightAdorner { FadeInDuration = TimeSpan.FromSeconds(1) });

        adorner.Attach();

        Assert.Same(adorner, AdornerLayer.GetAdorner(target));
        var samples = new List<double>();
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(i == 0 ? 100 : 200);
            samples.Add(adorner.Opacity);
        }

        Assert.True(samples[0] < samples[1] && samples[1] < samples[2] && samples[2] < 1, string.Join(", ", samples));
        Assert.True(samples[1] - samples[0] > 1.3 * (samples[2] - samples[1]), $"not easing out: {string.Join(", ", samples)}");
        await TestDispatcher.PumpUntil(() => adorner.Opacity == 1);
    }

    [AvaloniaFact]
    public async Task AttachingWithoutATargetOrWhileAttachedDoesNothing()
    {
        var untargeted = new DropHighlightAdorner();
        untargeted.Attach();
        Assert.False(untargeted.IsAttached);

        var (_, adorner) = ShowTarget(new DropHighlightAdorner { FadeInDuration = TimeSpan.FromMilliseconds(200) });
        adorner.Attach();
        await TestDispatcher.PumpUntil(() => adorner.Opacity == 1);

        adorner.Attach();
        await Task.Delay(50);

        Assert.Equal(1, adorner.Opacity);
        Assert.True(adorner.IsAttached);
    }

    [AvaloniaFact]
    public async Task DetachingMidFadeRestoresTheOpacityAndRemovesTheAdorner()
    {
        var (target, adorner) = ShowTarget(new DropHighlightAdorner { FadeInDuration = TimeSpan.FromSeconds(1) });
        adorner.Attach();
        await Task.Delay(100);
        Assert.True(adorner.Opacity < 1);

        adorner.Detach();

        Assert.Equal(1, adorner.Opacity);
        Assert.False(adorner.IsAttached);
        Assert.Null(AdornerLayer.GetAdorner(target));
    }

    [AvaloniaFact]
    public void AZeroFadeShowsTheAdornerAtOnce()
    {
        var (_, adorner) = ShowTarget(new DropHighlightAdorner { FadeInDuration = TimeSpan.Zero });

        adorner.Attach();
        Dispatcher.UIThread.RunJobs();

        Assert.True(adorner.IsAttached);
        Assert.Equal(1, adorner.Opacity);
    }

    [AvaloniaFact]
    public void IsAttachedFollowsTheTargetsAdorner()
    {
        var (target, adorner) = ShowTarget(new DropHighlightAdorner());
        adorner.Attach();
        Assert.True(adorner.IsAttached);

        AdornerLayer.SetAdorner(target, null);
        Assert.False(adorner.IsAttached);

        AdornerLayer.SetAdorner(target, adorner);
        Assert.True(adorner.IsAttached);

        adorner.TargetControl = new Border();
        Assert.False(adorner.IsAttached);
    }

    [AvaloniaFact]
    public void TheHighlightThemeDrawsAnAccentLookAndAnErrorLook()
    {
        var adorner = new DropHighlightAdorner { IsDropValid = true };
        TestWindow.Show(adorner);

        Assert.Equal(AccentFill, ColorOf(adorner.Background));
        Assert.Equal(Accent, ColorOf(adorner.BorderBrush));
        Assert.Equal(new Thickness(2), adorner.BorderThickness);

        adorner.IsDropValid = false;

        Assert.Equal(ErrorFill, ColorOf(adorner.Background));
        Assert.Equal(Error, ColorOf(adorner.BorderBrush));
    }

    [AvaloniaFact]
    public void AppStylesOverrideTheHighlightTheme()
    {
        var adorner = new DropHighlightAdorner { IsDropValid = true };
        var window = new Window { Content = adorner };
        window.Styles.Add(new Style(x => x.OfType<DropHighlightAdorner>()) { Setters = { new Setter(Border.BackgroundProperty, Brushes.Red) } });
        window.Styles.Add(new Style(x => x.OfType<DropHighlightAdorner>().Class(":error")) { Setters = { new Setter(Border.BorderBrushProperty, Brushes.Orange) } });
        window.Show();

        Assert.Equal(Brushes.Red, adorner.Background);

        adorner.IsDropValid = false;

        Assert.Equal(Brushes.Orange, adorner.BorderBrush);
    }

    [AvaloniaTheory]
    [InlineData(false, "Not here", true)]
    [InlineData(true, "Not here", false)]
    [InlineData(false, "", false)]
    [InlineData(false, null, false)]
    public void TheErrorBadgeShowsTheMessageOnlyWhileInvalid(bool isDropValid, string? message, bool shown)
    {
        var adorner = new DropHighlightAdorner { ErrorBadgeBackground = Brushes.Purple };
        TestWindow.Show(adorner);

        adorner.ErrorMessage = message;
        adorner.IsDropValid = isDropValid;

        var badge = Assert.IsType<Border>(adorner.Child);
        Assert.Equal(shown, badge.IsVisible);
        Assert.Equal(Brushes.Purple, badge.Background);
        Assert.Equal(HorizontalAlignment.Center, badge.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, badge.VerticalAlignment);
        Assert.Single(badge.GetVisualDescendants().OfType<Path>());
        if (shown)
            Assert.Equal(message, badge.GetVisualDescendants().OfType<TextBlock>().Single().Text);
    }

    [AvaloniaFact]
    public void TheHighlightCopiesTheTargetsCornerRadius()
    {
        var rounded = new Border { CornerRadius = new CornerRadius(6) };
        var button = new Button { CornerRadius = new CornerRadius(3) };
        var adorner = new DropHighlightAdorner();
        TestWindow.Show(new StackPanel { Children = { rounded, button } });

        adorner.TargetControl = rounded;
        adorner.Attach();
        Assert.Equal(new CornerRadius(6), adorner.CornerRadius);

        adorner.Detach();
        adorner.TargetControl = button;
        adorner.Attach();
        Assert.Equal(new CornerRadius(3), adorner.CornerRadius);
    }

    [AvaloniaFact]
    public void ACornerRadiusSetOrStyledOnTheHighlightWins()
    {
        var rounded = new Border { CornerRadius = new CornerRadius(6) };
        var local = new DropHighlightAdorner { CornerRadius = new CornerRadius(1), TargetControl = rounded };
        var styled = new DropHighlightAdorner { TargetControl = rounded };
        var window = TestWindow.Show(rounded);
        window.Styles.Add(new Style(x => x.OfType<DropHighlightAdorner>()) { Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(2)) } });

        local.Attach();
        local.Detach();
        styled.Attach();

        Assert.Equal(new CornerRadius(1), local.CornerRadius);
        Assert.Equal(new CornerRadius(2), styled.CornerRadius);
    }

    [AvaloniaFact]
    public void UpdateTakesTheOffsetAndTheTargetsSize()
    {
        var (_, adorner) = ShowTarget(new DropInsertionAdorner());
        adorner.Attach();

        adorner.Update(DropTargetOffset.AfterTarget);

        Assert.Equal(DropTargetOffset.AfterTarget, adorner.Offset);
        Assert.Equal(100, adorner.Width);
        Assert.Equal(40, adorner.Height);
    }

    [AvaloniaTheory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void TheInsertionAdornerDrawsOnlyWhileValidWithABrush(bool isDropValid, bool hasBrush, bool draws)
    {
        var (target, adorner) = ShowTarget(new DropInsertionAdorner { IsDropValid = isDropValid });
        if (!hasBrush)
            adorner.LineBrush = null;
        adorner.Attach();

        adorner.Update(DropTargetOffset.OnTarget);

        var window = (Window)TopLevel.GetTopLevel(target)!;
        var snapshot = WindowSnapshot.Take(window);
        Assert.Equal(!draws, snapshot.IsUniform(PixelRectOf(window, target, margin: 3), snapshot[0, 0]));
    }

    [AvaloniaFact]
    public void TheInsertionLineIsNotClippedToTheTarget()
    {
        var (target, adorner) = ShowTarget(new DropInsertionAdorner { IsDropValid = true });
        adorner.Attach();

        adorner.Update(DropTargetOffset.AfterTarget);

        var window = (Window)TopLevel.GetTopLevel(target)!;
        var bounds = PixelRectOf(window, target);
        Assert.False(adorner.ClipToBounds);
        Assert.False(AdornerLayer.GetIsClipEnabled(adorner));
        Assert.Equal(Accent, WindowSnapshot.Take(window)[bounds.Center.X, bounds.Bottom]);
    }

    [AvaloniaFact]
    public void TheInsertionLineIsTwoDipsThickWithAThreeAndAHalfDipMarker()
    {
        var (target, adorner) = ShowTarget(new DropInsertionAdorner { IsDropValid = true });
        adorner.Attach();

        adorner.Update(DropTargetOffset.AfterTarget);

        var window = (Window)TopLevel.GetTopLevel(target)!;
        var snapshot = WindowSnapshot.Take(window);
        var bounds = PixelRectOf(window, target);
        var background = snapshot[0, 0];
        var (x, y) = (bounds.Center.X, bounds.Bottom);
        Assert.Equal([background, Accent, Accent, background], Enumerable.Range(y - 2, 4).Select(row => snapshot[x, row]));

        // The marker's center sits 3.5 + 1 DIPs from the start, so a 3.5 DIP radius puts its outer edge half way through this row
        var markerX = bounds.X + 4;
        Assert.Equal(background, snapshot[markerX, y - 6]);
        Assert.InRange(snapshot[markerX, y - 5].R, 90, 165);
        Assert.Equal(Accent, snapshot[markerX, y - 4]);
        Assert.Equal(background, snapshot[markerX, y]);
    }

    [AvaloniaFact]
    public void FadeInDefaultsToAShortFadeForTheHighlightAndNoneForTheInsertionLine()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(120), new DropHighlightAdorner().FadeInDuration);
        Assert.Equal(TimeSpan.Zero, new DropInsertionAdorner().FadeInDuration);
    }

    [AvaloniaFact]
    public void BrushesDefaultToTheAccentAndErrorColors()
    {
        Assert.Equal(Accent, ColorOf(new DropInsertionAdorner().LineBrush));
        Assert.Equal(Error, ColorOf(new DropHighlightAdorner().ErrorBadgeBackground));
    }

    [AvaloniaFact]
    public void BorderKindHasTheOutlinePseudoClass()
    {
        var adorner = new DropHighlightAdorner { Kind = DropAdornerKind.Border };
        TestWindow.Show(adorner);

        Assert.Contains(":outline", adorner.Classes);
        Assert.Equal(Brushes.Transparent, adorner.Background);
        Assert.True(adorner.IsVisible);
    }

    [AvaloniaFact]
    public void NoneKindIsInvisible()
    {
        var adorner = new DropHighlightAdorner();
        TestWindow.Show(adorner);

        adorner.Kind = DropAdornerKind.None;

        Assert.Contains(":hidden", adorner.Classes);
        Assert.False(adorner.IsVisible);
    }

    [AvaloniaFact]
    public void KindCanBeSetFromAStyle()
    {
        var adorner = new DropHighlightAdorner();
        var window = new Window { Content = adorner };
        window.Styles.Add(new Style(x => x.OfType<DropHighlightAdorner>()) { Setters = { new Setter(DropHighlightAdorner.KindProperty, DropAdornerKind.Border) } });
        window.Show();

        Assert.Equal(DropAdornerKind.Border, adorner.Kind);
        Assert.Contains(":outline", adorner.Classes);
    }

    [AvaloniaFact]
    public void AppStylesOverrideTheLookOfAKind()
    {
        var adorner = new DropHighlightAdorner();
        var window = new Window { Content = adorner };
        window.Styles.Add(new Style(x => x.OfType<DropHighlightAdorner>()) { Setters = { new Setter(Visual.IsVisibleProperty, false) } });
        window.Show();

        Assert.False(adorner.IsVisible);
    }

    [AvaloniaFact]
    public void KindDefaultsToSolid()
    {
        var adorner = new DropHighlightAdorner();
        TestWindow.Show(adorner);

        Assert.Equal(DropAdornerKind.Solid, adorner.Kind);
        Assert.DoesNotContain(":outline", adorner.Classes);
        Assert.DoesNotContain(":hidden", adorner.Classes);
        Assert.True(adorner.IsVisible);
    }

    [Fact]
    public void TargetControlBindsOneWayByDefault() =>
        Assert.Equal(BindingMode.OneWay, DropAdornerBase.TargetControlProperty.GetMetadata(typeof(DropHighlightAdorner)).DefaultBindingMode);

    private static (Border, T) ShowTarget<T>(T adorner) where T : DropAdornerBase
    {
        var target = new Border { Background = Brushes.White, Width = 100, Height = 40 };
        TestWindow.Show(target);
        adorner.TargetControl = target;
        return (target, adorner);
    }

    private static PixelRect PixelRectOf(Window window, Visual visual, int margin = 0)
    {
        var topLeft = visual.TranslatePoint(default, window)!.Value;
        return new PixelRect((int)topLeft.X - margin, (int)topLeft.Y - margin, (int)visual.Bounds.Width + 2 * margin, (int)visual.Bounds.Height + 2 * margin);
    }

    private static Color? ColorOf(IBrush? brush) => (brush as ISolidColorBrush)?.Color;
}
