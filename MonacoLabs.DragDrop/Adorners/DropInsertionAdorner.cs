using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaDragDrop = Avalonia.Input.DragDrop;
using System.Diagnostics;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Adorner that displays an insertion separator between child items in a collection control
/// </summary>
public class DropInsertionAdorner : DropAdornerBase
{
    public static readonly StyledProperty<bool> SupportsChildInsertionProperty =
        AvaloniaProperty.Register<DropInsertionAdorner, bool>("SupportsChildInsertion");

    public bool SupportsChildInsertion
    {
        get => GetValue(SupportsChildInsertionProperty);
        set => SetValue(SupportsChildInsertionProperty, value);
    }

    public DropTargetOffset Target { get; set; } = DropTargetOffset.BeforeTarget;
    
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

    public override void Attach()
    {
        if (TargetControl is null)
            return;

        base.Attach();
        TargetControl.AddHandler(AvaDragDrop.DragOverEvent, TargetControl_DragOver, RoutingStrategies.Bubble, true);
        TargetControl.AddHandler(AvaDragDrop.DragEnterEvent, TargetControl_DragEnter, RoutingStrategies.Bubble, true);

        var rect = GetAdornerRect();

        Width = rect.Width;
        Height = 4;

        Debug.WriteLine($"Attach: {TargetControl.GetType()}");

        Target = DropTargetOffset.BeforeTarget;
        //IsVisible = false;

        //TargetControl.RenderTransform = new TranslateTransform(0, 40);
        //RenderTransform = new TranslateTransform(40, 40);

        //if (VerticalAlignment == VerticalAlignment.Top)
        //{
        //    RenderTransform = new TranslateTransform(rect.X, rect.Y);
        //}
        //else if (VerticalAlignment == VerticalAlignment.Bottom)
        //{
        //    RenderTransform = new TranslateTransform(rect.X, 0); // TargetControl.Height + 4);
        //}

        //Child = new TextBlock()
        //{
        //    //[!TextBlock.TextProperty] = new Binding()
        //    //{
        //    //    Source = this,
        //    //    Path = nameof(ErrorMessage)
        //    //},
        //    Text = ErrorMessage,
        //    FontSize = 24,
        //    Foreground = Brushes.White,
        //    HorizontalAlignment = HorizontalAlignment.Center,
        //    VerticalAlignment = VerticalAlignment.Center,
        //    TextWrapping = TextWrapping.Wrap
        //};
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Debug.WriteLine("Detached from Visual Tree");
    }

    public override void Detach()
    {
        if (TargetControl is null)
            return;

        RemovePseudoclasses();
        Debug.WriteLine("Detach");
        base.Detach();
        TargetControl.RemoveHandler(AvaDragDrop.DragOverEvent, TargetControl_DragOver);
        TargetControl.RemoveHandler(AvaDragDrop.DragEnterEvent, TargetControl_DragEnter);
    }

    protected virtual void TargetControl_DragEnter(object? sender, DragEventArgs e)
    {
        if (TargetControl is null)
            return;

        var dragLocation = e.GetPosition(TargetControl);
        Update(dragLocation);
    }

    protected virtual void TargetControl_DragOver(object? sender, DragEventArgs e)
    {
        if (TargetControl is null)
            return;

        var dragLocation = e.GetPosition(TargetControl);
        Update(dragLocation);
    }

    public virtual void Update(Point dragLocation)
    {
        if (TargetControl is null)
            return;

        IsVisible = true;

        if (SupportsChildInsertion)
        {
            if (dragLocation.Y < (Math.Floor(TargetControl.Bounds.Height / 4)))
            {
                Target = DropTargetOffset.AfterTarget;
                VerticalAlignment = VerticalAlignment.Top;
            }
            else if (dragLocation.Y > TargetControl.Bounds.Height - (Math.Floor(TargetControl.Bounds.Height / 4)))
            {
                Target = DropTargetOffset.BeforeTarget;
                VerticalAlignment = VerticalAlignment.Bottom;
            }
            else
            {
                Target = DropTargetOffset.OnTarget;
                VerticalAlignment = VerticalAlignment.Stretch;
            }
        }
        else
        {
            if (dragLocation.Y < Math.Floor(TargetControl.Bounds.Height / 2))
            {
                Target = DropTargetOffset.AfterTarget;
                VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                Target = DropTargetOffset.BeforeTarget;
                VerticalAlignment = VerticalAlignment.Bottom;
            }
        }

        // Half adorner thickness on each side

        var parent = TargetControl.Parent;
        if (VerticalAlignment == VerticalAlignment.Stretch)
        {
            Margin = new Thickness(0);
            Height = GetAdornerRect().Height;
        }
        else
        {
            Margin = new Thickness(0, -2, 0, -2);
            Height = 4;
        }

        UpdatePseudoclasses(VerticalAlignment == VerticalAlignment.Top);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ErrorMessageProperty
            && change.NewValue is string errorMessage
            && Child is TextBlock errorText)
        {
            errorText.Text = errorMessage;
        }
        else if (change.Property == IsDropValidProperty
            || change.Property == VerticalAlignmentProperty
            || change.Property == LineBrushProperty)
        {
            Child?.InvalidateVisual();
        }
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
            var pen = new Pen(brush, Thickness, lineCap: PenLineCap.Round);

            if (owner.VerticalAlignment == VerticalAlignment.Stretch)
            {
                context.DrawRectangle(null, pen, new Rect(size).Deflate(Thickness / 2), 4, 4);
                return;
            }

            var y = size.Height / 2;
            context.DrawEllipse(null, pen, new Point(MarkerRadius + Thickness / 2, y), MarkerRadius, MarkerRadius);
            context.DrawLine(pen, new Point(MarkerRadius * 2 + Thickness / 2, y), new Point(size.Width - Thickness / 2, y));
        }
    }

    private void UpdatePseudoclasses(bool isTop)
    {
        ((IPseudoClasses)TargetControl!.Classes).Set(":droptop", isTop);
        ((IPseudoClasses)TargetControl!.Classes).Set(":dropbottom", !isTop);
    }

    private void RemovePseudoclasses()
    {
        ((IPseudoClasses)TargetControl!.Classes).Set(":droptop", false);
        ((IPseudoClasses)TargetControl!.Classes).Set(":dropbottom", false);
    }
}
