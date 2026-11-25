using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Snapshot of the dragged control that follows the pointer while a drag is over the source's window.
/// Has the <c>:nodrop</c> pseudo-class while the hovered location rejects the drag, <c>:copy</c> while it would copy,
/// and <c>:remove</c> while dropping would remove the payload (see <see cref="Abstractions.DragOperationBase.RemoveOnDropOutside"/>).
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

    private DragPreviewAdorner(Point grabOffset, RenderTargetBitmap snapshot, Size size)
    {
        _grabOffset = grabOffset;
        _snapshot = snapshot;
        _image = new Image { Source = snapshot, Width = size.Width, Height = size.Height };
    }

    /// <summary>
    /// Creates a preview from a snapshot of <paramref name="source"/>, or null if it has no visible size.
    /// </summary>
    /// <param name="grabOffset">Pointer position relative to <paramref name="source"/> when the drag started.</param>
    public static DragPreviewAdorner? Create(Control source, Point grabOffset)
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

        var preview = new DragPreviewAdorner(grabOffset, snapshot, size) { CornerRadius = cornerRadius };
        preview.Child = new Border
        {
            CornerRadius = cornerRadius,
            ClipToBounds = true,
            Background = FindBackdrop(source),
            Child = preview._image
        };
        return preview;
    }

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
