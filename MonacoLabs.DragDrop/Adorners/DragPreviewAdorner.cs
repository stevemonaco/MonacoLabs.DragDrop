using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using System.Globalization;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Snapshot of the dragged control that follows the pointer while a drag is over the source's window.
/// Has the <c>:nodrop</c> pseudo-class while the hovered location rejects the drag, <c>:copy</c> while it would copy,
/// and <c>:remove</c> while dropping would remove the payload (see <see cref="Abstractions.DragOperationBase.RemoveOnDropOutside"/>).
/// A drag of several payloads shows their count in a badge and has the <c>:multiple</c> pseudo-class.
/// </summary>
public class DragPreviewAdorner : Border, IDisposable
{
    private readonly Point _grabOffset;
    private readonly RenderTargetBitmap _snapshot;
    private readonly Image _image;

    static DragPreviewAdorner()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<DragPreviewAdorner>(false);
        OpacityProperty.OverrideDefaultValue<DragPreviewAdorner>(0.9);
        BoxShadowProperty.OverrideDefaultValue<DragPreviewAdorner>(BoxShadows.Parse("0 8 20 0 #40000000"));
        ZIndexProperty.OverrideDefaultValue<DragPreviewAdorner>(int.MaxValue);
    }

    private DragPreviewAdorner(Point grabOffset, RenderTargetBitmap snapshot, Size size, int payloadCount)
    {
        _grabOffset = grabOffset;
        _snapshot = snapshot;
        _image = new Image { Source = snapshot, Width = size.Width, Height = size.Height };
        PayloadCount = payloadCount;
        PseudoClasses.Set(":multiple", payloadCount > 1);
    }

    /// <summary>
    /// Number of payloads the drag carries.
    /// </summary>
    public int PayloadCount { get; }

    /// <summary>
    /// Creates a preview from a snapshot of <paramref name="source"/>, or null if it has no visible size.
    /// </summary>
    /// <param name="grabOffset">Pointer position relative to <paramref name="source"/> when the drag started.</param>
    /// <param name="payloadCount">Number of payloads the drag carries; above 1, the preview shows it in a badge.</param>
    public static DragPreviewAdorner? Create(Control source, Point grabOffset, int payloadCount = 1)
    {
        var size = source.Bounds.Size;
        if (size.Width < 1 || size.Height < 1)
            return null;

        var scaling = TopLevel.GetTopLevel(source)?.RenderScaling ?? 1d;
        var snapshot = new RenderTargetBitmap(PixelSize.FromSize(size, scaling), new Vector(96 * scaling, 96 * scaling));
        snapshot.Render(source);

        var cornerRadius = source switch
        {
            Border border => border.CornerRadius,
            TemplatedControl templated => templated.CornerRadius,
            _ => default
        };

        var preview = new DragPreviewAdorner(grabOffset, snapshot, size, payloadCount) { CornerRadius = cornerRadius };
        var backdrop = new Border
        {
            CornerRadius = cornerRadius,
            ClipToBounds = true,
            Background = FindBackdrop(source),
            Child = preview._image
        };
        preview.Child = payloadCount > 1 ? new Panel { Children = { backdrop, CreateCountBadge(payloadCount) } } : backdrop;
        return preview;
    }

    private static Border CreateCountBadge(int count) => new()
    {
        Name = "PART_CountBadge",
        Background = AdornerBrushes.Accent,
        CornerRadius = new CornerRadius(10),
        MinWidth = 20,
        Height = 20,
        Padding = new Thickness(6, 0),
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Top,
        Margin = new Thickness(0, -8, -8, 0),
        Child = new TextBlock
        {
            Text = count.ToString(CultureInfo.CurrentCulture),
            Foreground = Brushes.White,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    /// <summary>
    /// Positions the preview so its grab point sits under <paramref name="pointer"/>, in AdornerLayer coordinates.
    /// </summary>
    public void MoveTo(Point pointer)
    {
        Canvas.SetLeft(this, pointer.X - _grabOffset.X);
        Canvas.SetTop(this, pointer.Y - _grabOffset.Y);
    }

    /// <summary>
    /// Reflects the hovered location through the <c>:nodrop</c>, <c>:copy</c> and <c>:remove</c> pseudo-classes.
    /// </summary>
    /// <param name="isRemoving">Dropping here removes the payload from its source.</param>
    public void SetEffect(DragDropEffects effect, bool isRemoving)
    {
        PseudoClasses.Set(":nodrop", effect == DragDropEffects.None);
        PseudoClasses.Set(":copy", effect == DragDropEffects.Copy && !isRemoving);
        PseudoClasses.Set(":remove", isRemoving);
    }

    internal bool IsDisposed { get; private set; }

    /// <summary>
    /// Releases the snapshot bitmap. The preview shows nothing afterwards.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        _image.Source = null;
        _snapshot.Dispose();
    }

    // The snapshot omits whatever is painted behind the source, so transparent sources need the nearest opaque backdrop
    private static IBrush? FindBackdrop(Visual source)
    {
        foreach (var ancestor in source.GetVisualAncestors())
        {
            var brush = ancestor switch
            {
                Border border => border.Background,
                Panel panel => panel.Background,
                TemplatedControl templated => templated.Background,
                _ => null
            };

            if (brush is ISolidColorBrush { Color.A: 255, Opacity: >= 1 } or (not null and not ISolidColorBrush))
                return brush;
        }

        return null;
    }
}
