using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class DragSessionTests
{
    [AvaloniaFact]
    public void EndingTheDragDisposesThePreview()
    {
        var source = new Border { Background = Brushes.Red, Width = 100, Height = 100 };
        DragDrop.SetDragOperation(source, new ItemDragOperation { Payload = "A" });
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        DragDrop.SetDropOperation(target, new ItemDropOperation());
        var window = TestWindow.Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 50, Children = { source, target } });

        DragPreviewAdorner? preview = null;
        var layer = AdornerLayer.GetAdornerLayer(source)!;
        NotifyCollectionChangedEventHandler capture = (_, e) => preview ??= e.NewItems?.OfType<DragPreviewAdorner>().FirstOrDefault();
        layer.Children.CollectionChanged += capture;

        window.MouseDrag(window.PointIn(source), window.PointIn(target));
        layer.Children.CollectionChanged -= capture;

        Assert.NotNull(preview);
        Assert.DoesNotContain(preview, layer.Children);
        Assert.True(preview.IsDisposed);
    }
}
