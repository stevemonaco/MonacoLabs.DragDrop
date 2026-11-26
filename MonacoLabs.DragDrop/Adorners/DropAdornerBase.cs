using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Styling;
using AvaDragDrop = Avalonia.Input.DragDrop;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Base class for drop feedback placed in the <see cref="AdornerLayer"/> over <see cref="TargetControl"/>. It is not hit-testable and has
/// the <c>:error</c> pseudo-class while <see cref="IsDropValid"/> is false.
/// </summary>
public abstract class DropAdornerBase : Border
{
    public static readonly StyledProperty<Control?> TargetControlProperty =
        AvaloniaProperty.Register<DropAdornerBase, Control?>(nameof(TargetControl));

    /// <summary>
    /// Control which the adorner should be positioned above
    /// </summary>
    public Control? TargetControl
    {
        get => GetValue(TargetControlProperty);
        set => SetValue(TargetControlProperty, value);
    }

    private bool _isDropValid;
    public static readonly DirectProperty<DropAdornerBase, bool> IsDropValidProperty =
        AvaloniaProperty.RegisterDirect<DropAdornerBase, bool>(nameof(IsDropValid), o => o.IsDropValid);

    /// <summary>
    /// Whether the drag over <see cref="TargetControl"/> passed validation. False sets the <c>:error</c> pseudo-class.
    /// </summary>
    public bool IsDropValid
    {
        get => _isDropValid;
        set { SetAndRaise(IsDropValidProperty, ref _isDropValid, value); }
    }

    private string? _errorMessage;
    public static readonly DirectProperty<DropAdornerBase, string?> ErrorMessageProperty =
        AvaloniaProperty.RegisterDirect<DropAdornerBase, string?>(nameof(ErrorMessage), o => o.ErrorMessage);

    /// <summary>
    /// Error message indicating that a validation has failed
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { SetAndRaise(ErrorMessageProperty, ref _errorMessage, value); }
    }

    public static readonly StyledProperty<TimeSpan> FadeInDurationProperty =
        AvaloniaProperty.Register<DropAdornerBase, TimeSpan>(nameof(FadeInDuration), TimeSpan.FromMilliseconds(120));

    /// <summary>
    /// Duration of the opacity fade when the adorner is attached. <see cref="TimeSpan.Zero"/> disables the fade.
    /// </summary>
    public TimeSpan FadeInDuration
    {
        get => GetValue(FadeInDurationProperty);
        set => SetValue(FadeInDurationProperty, value);
    }

    /// <summary>
    /// True while this adorner is placed over <see cref="TargetControl"/>.
    /// </summary>
    public bool IsAttached => TargetControl is not null && ReferenceEquals(TargetControl.GetValue(AdornerLayer.AdornerProperty), this);

    private CancellationTokenSource? _fadeCts;

    static DropAdornerBase()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<DropAdornerBase>(false);
    }

    public DropAdornerBase()
    {
        AvaDragDrop.SetAllowDrop(this, false);
        UpdatePseudoclasses(IsDropValid);
    }

    /// <summary>
    /// Places the adorner over <see cref="TargetControl"/> and fades it in. Does nothing without a target or when already attached.
    /// </summary>
    public virtual void Attach()
    {
        if (TargetControl is null || IsAttached)
            return;

        AdornerLayer.SetAdorner(TargetControl, this);
        FadeIn();
    }

    /// <summary>
    /// Removes the adorner from over <see cref="TargetControl"/>.
    /// </summary>
    public virtual void Detach()
    {
        if (TargetControl is null)
            return;

        _fadeCts?.Cancel();
        AdornerLayer.SetAdorner(TargetControl, null);
    }

    private void FadeIn()
    {
        _fadeCts?.Cancel();
        if (FadeInDuration <= TimeSpan.Zero)
            return;

        _fadeCts = new CancellationTokenSource();
        var animation = new Animation
        {
            Duration = FadeInDuration,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(OpacityProperty, 0d) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(OpacityProperty, Opacity) } },
            }
        };

        _ = animation.RunAsync(this, _fadeCts.Token);
    }

    /// <summary>
    /// Gets the rect overlaying the TargetControl in AdornerLayer coordinates
    /// </summary>
    /// <returns></returns>
    public Rect GetAdornerRect()
    {
        var root = TopLevel.GetTopLevel(TargetControl)!;
        var bounds = TargetControl!.Bounds;
        var point = TargetControl.TranslatePoint(bounds.TopLeft, root)!.Value;

        return new Rect(point.X - bounds.X, point.Y - bounds.Y, bounds.Width, bounds.Height);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsDropValidProperty && change.NewValue is bool isDropValid)
        {
            UpdatePseudoclasses(isDropValid);
        }
    }

    private void UpdatePseudoclasses(bool isDropValid)
    {
        PseudoClasses.Set(":error", !isDropValid);
    }
}
