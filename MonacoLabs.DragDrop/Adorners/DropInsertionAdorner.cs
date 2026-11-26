using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Adorner that draws an insertion line above or below the hovered item, or an outline around it for drops into the item.
/// The owning drop operation decides the <see cref="Offset"/>.
/// </summary>
public class DropInsertionAdorner : DropAdornerBase
{
    private DropTargetOffset _offset = DropTargetOffset.BeforeTarget;
    public static readonly DirectProperty<DropInsertionAdorner, DropTargetOffset> OffsetProperty =
        AvaloniaProperty.RegisterDirect<DropInsertionAdorner, DropTargetOffset>(nameof(Offset), o => o.Offset);

    /// <summary>
    /// Where the payload lands relative to <see cref="DropAdornerBase.TargetControl"/>.
    /// </summary>
    public DropTargetOffset Offset
    {
        get => _offset;
        private set => SetAndRaise(OffsetProperty, ref _offset, value);
    }

    public static readonly StyledProperty<IBrush?> LineBrushProperty =
        AvaloniaProperty.Register<DropInsertionAdorner, IBrush?>(nameof(LineBrush), AdornerBrushes.Accent);

    /// <summary>
    /// Brush of the insertion line and its end marker.
    /// </summary>
    public IBrush? LineBrush
    {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    private double _gapOffset;

    static DropInsertionAdorner()
    {
        BorderBrushProperty.OverrideDefaultValue<DropInsertionAdorner>(Brushes.Transparent);
        IsHitTestVisibleProperty.OverrideDefaultValue<DropInsertionAdorner>(false);
        ClipToBoundsProperty.OverrideDefaultValue<DropInsertionAdorner>(false);
        FadeInDurationProperty.OverrideDefaultValue<DropInsertionAdorner>(TimeSpan.Zero);
    }

    public DropInsertionAdorner()
    {
        AdornerLayer.SetIsClipEnabled(this, false);
        Child = new InsertionMarker(this);
    }

    /// <summary>
    /// Moves the indicator to <paramref name="offset"/> relative to <see cref="DropAdornerBase.TargetControl"/>.
    /// </summary>
    public virtual void Update(DropTargetOffset offset)
    {
        if (TargetControl is null)
            return;

        Offset = offset;

        // Covering the target exactly keeps the adorner on its pixel-aligned bounds; the marker draws the line at the edge
        _gapOffset = offset == DropTargetOffset.OnTarget ? 0 : GetGapCenterOffset(offset == DropTargetOffset.BeforeTarget);
        Width = TargetControl.Bounds.Width;
        Height = TargetControl.Bounds.Height;
        Child?.InvalidateVisual();
    }

    /// <summary>
    /// Gets the distance from the target's edge to the middle of the gap shared with its neighbor, so "below item N"
    /// and "above item N+1" draw at the same place in flat lists.
    /// </summary>
    private double GetGapCenterOffset(bool isTop)
    {
        if (TargetControl is null || ItemsControl.ItemsControlFromItemContainer(TargetControl) is not { } owner)
            return 0;

        var index = owner.IndexFromContainer(TargetControl);
        if (index < 0 || owner.ContainerFromIndex(isTop ? index - 1 : index + 1) is not { } neighbor)
            return 0;

        var neighborEdge = neighbor.TranslatePoint(new Point(0, isTop ? neighbor.Bounds.Height : 0), TargetControl);
        if (neighborEdge is null)
            return 0;

        var targetEdge = isTop ? 0 : TargetControl.Bounds.Height;
        return (neighborEdge.Value.Y - targetEdge) / 2;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsDropValidProperty || change.Property == LineBrushProperty)
            Child?.InvalidateVisual();
    }

    private sealed class InsertionMarker(DropInsertionAdorner owner) : Control
    {
        private const double MarkerRadius = 3.5;
        private const double Thickness = 2;

        public override void Render(DrawingContext context)
        {
            if (!owner.IsDropValid || owner.LineBrush is not { } brush)
                return;

            var size = Bounds.Size;
            var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1d;
            var thicknessPixels = Math.Max(1, Math.Round(Thickness * scaling));
            var thickness = thicknessPixels / scaling;
            var pen = new Pen(brush, thickness, lineCap: PenLineCap.Round);

            if (owner.Offset == DropTargetOffset.OnTarget)
            {
                context.DrawRectangle(null, pen, new Rect(size).Deflate(thickness / 2), 4, 4);
                return;
            }

            var edge = owner.Offset == DropTargetOffset.BeforeTarget ? 0 : size.Height;
            var y = SnapLineCenter(edge + owner._gapOffset, thicknessPixels, scaling);
            context.DrawEllipse(null, pen, new Point(MarkerRadius + thickness / 2, y), MarkerRadius, MarkerRadius);
            context.DrawLine(pen, new Point(MarkerRadius * 2 + thickness / 2, y), new Point(size.Width - thickness / 2, y));
        }

        // Aligns the stroke to whole device pixels so both sides of a shared edge rasterize identically
        private static double SnapLineCenter(double y, double thicknessPixels, double scaling)
        {
            var topPixel = Math.Round(y * scaling - thicknessPixels / 2);
            return (topPixel + thicknessPixels / 2) / scaling;
        }
    }
}
