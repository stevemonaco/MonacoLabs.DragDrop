using System.Windows.Input;
using Avalonia.Data;
using Avalonia;
using Avalonia.Controls;

namespace MonacoLabs.DragDrop.Abstractions;
public abstract partial class DropOperationBase
{
    private Control? _attachedControl;

    public static readonly DirectProperty<DropOperationBase, Control?> AttachedControlProperty =
        AvaloniaProperty.RegisterDirect<DropOperationBase, Control?>(nameof(AttachedControl), o => o.AttachedControl);

    /// <summary>
    /// Control that the DropOperation is attached to. Changes only through <see cref="Attach"/> and <see cref="Detach"/>.
    /// </summary>
    public Control? AttachedControl
    {
        get => _attachedControl;
        private set => SetAndRaise(AttachedControlProperty, ref _attachedControl, value);
    }

    private string? _errorMessage;

    public static readonly DirectProperty<DropOperationBase, string?> ErrorMessageProperty =
        AvaloniaProperty.RegisterDirect<DropOperationBase, string?>(nameof(ErrorMessage), o => o.ErrorMessage);

    /// <summary>
    /// Why the drag over the attached control is rejected, as passed to <see cref="Reject"/>. Mirrored to the drop adorner,
    /// and cleared before each validation and when the drag leaves or drops.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetAndRaise(ErrorMessageProperty, ref _errorMessage, value);
    }

    public static readonly StyledProperty<object?> PayloadTargetProperty =
        AvaloniaProperty.Register<DropOperationBase, object?>(nameof(PayloadTarget), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// PayloadTarget will be assigned the Payload that was registered by the DragOperation when the Drop is applied
    /// </summary>
    public object? PayloadTarget
    {
        get => GetValue(PayloadTargetProperty);
        set => SetValue(PayloadTargetProperty, value);
    }

    private ICommand? _payloadCommand;

    public static readonly DirectProperty<DropOperationBase, ICommand?> PayloadCommandProperty =
        AvaloniaProperty.RegisterDirect<DropOperationBase, ICommand?>(nameof(PayloadCommand), o => o.PayloadCommand, (o, v) => o.PayloadCommand = v);

    /// <summary>
    /// PayloadCommand will be executed when the Drop is applied.
    /// </summary>
    public ICommand? PayloadCommand
    {
        get => _payloadCommand;
        set => SetAndRaise(PayloadCommandProperty, ref _payloadCommand, value);
    }

    public static readonly StyledProperty<DropAdornerBase?> DropAdornerProperty =
        AvaloniaProperty.Register<DropOperationBase, DropAdornerBase?>(nameof(DropAdorner), defaultBindingMode: BindingMode.OneTime);

    /// <summary>
    /// Adorner responsible for rendering overlay visual when dragging over and in a valid state
    /// </summary>
    public DropAdornerBase? DropAdorner
    {
        get => GetValue(DropAdornerProperty);
        set => SetValue(DropAdornerProperty, value);
    }
}
