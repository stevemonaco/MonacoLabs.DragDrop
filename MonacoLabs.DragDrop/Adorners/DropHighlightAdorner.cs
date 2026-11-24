using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using MonacoLabs.DragDrop.Adorners;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Adorner that tints the whole drop target and shows <see cref="DropAdornerBase.ErrorMessage"/> in a badge when the drop is invalid.
/// Defaults come from a built-in <see cref="ControlTheme"/>, so app styles targeting <c>DropHighlightAdorner</c> and <c>:error</c> override them.
/// </summary>
public class DropHighlightAdorner : DropAdornerBase
{
    public static readonly StyledProperty<IBrush?> ErrorBadgeBackgroundProperty =
        AvaloniaProperty.Register<DropHighlightAdorner, IBrush?>(nameof(ErrorBadgeBackground), AdornerBrushes.Error);

    /// <summary>
    /// Fill of the badge that displays <see cref="DropAdornerBase.ErrorMessage"/>.
    /// </summary>
    public IBrush? ErrorBadgeBackground
    {
        get => GetValue(ErrorBadgeBackgroundProperty);
        set => SetValue(ErrorBadgeBackgroundProperty, value);
    }

    private static readonly ControlTheme DefaultTheme = new(typeof(DropHighlightAdorner))
    {
        Setters =
        {
            new Setter(BackgroundProperty, AdornerBrushes.AccentFill),
            new Setter(BorderBrushProperty, AdornerBrushes.Accent),
            new Setter(BorderThicknessProperty, new Thickness(2)),
        },
        Children =
        {
            new Style(x => x.Nesting().Class(":error"))
            {
                Setters =
                {
                    new Setter(BackgroundProperty, AdornerBrushes.ErrorFill),
                    new Setter(BorderBrushProperty, AdornerBrushes.Error),
                }
            },
            new Style(x => x.Nesting().Class(":outline"))
            {
                Setters = { new Setter(BackgroundProperty, Brushes.Transparent) }
            },
        }
    };

    private readonly Border _errorBadge;
    private readonly TextBlock _errorText;
    private bool _isCornerRadiusInferred;

    static DropHighlightAdorner()
    {
        ThemeProperty.OverrideDefaultValue<DropHighlightAdorner>(DefaultTheme);
    }

    public DropHighlightAdorner() : this(DropAdornerKind.Solid)
    {
    }

    public DropHighlightAdorner(DropAdornerKind dropAdornerType)
    {
        PseudoClasses.Set(":outline", dropAdornerType == DropAdornerKind.Border);
        IsVisible = dropAdornerType != DropAdornerKind.None;

        _errorText = new TextBlock
        {
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _errorBadge = new Border
        {
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10, 5, 12, 5),
            Margin = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            BoxShadow = BoxShadows.Parse("0 2 8 0 #33000000"),
            IsVisible = false,
            [!Border.BackgroundProperty] = this[!ErrorBadgeBackgroundProperty],
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children = { CreateErrorIcon(), _errorText }
            }
        };

        Child = _errorBadge;
    }

    public override void Attach()
    {
        base.Attach();
        InferCornerRadius();
    }

    // Matches the target's rounding unless CornerRadius was set or styled on the adorner
    private void InferCornerRadius()
    {
        if (TargetControl is null || (IsSet(CornerRadiusProperty) && !_isCornerRadiusInferred))
            return;

        var cornerRadius = TargetControl switch
        {
            Border border => border.CornerRadius,
            TemplatedControl templated => templated.CornerRadius,
            _ => default
        };

        SetCurrentValue(CornerRadiusProperty, cornerRadius);
        _isCornerRadiusInferred = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ErrorMessageProperty || change.Property == IsDropValidProperty)
        {
            _errorText.Text = ErrorMessage;
            _errorBadge.IsVisible = !IsDropValid && !string.IsNullOrEmpty(ErrorMessage);
        }
    }

    private static Control CreateErrorIcon() => new Path
    {
        Width = 12,
        Height = 12,
        Stroke = Brushes.White,
        StrokeThickness = 1.5,
        VerticalAlignment = VerticalAlignment.Center,
        Data = new GeometryGroup
        {
            Children =
            {
                new EllipseGeometry(new Rect(0.75, 0.75, 10.5, 10.5)),
                new LineGeometry(new Point(2.8, 2.8), new Point(9.2, 9.2)),
            }
        }
    };
}
