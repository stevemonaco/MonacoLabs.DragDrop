using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class DropAdornerTests
{
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
}
