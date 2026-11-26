using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using System.Windows.Input;

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

    public static readonly StyledProperty<bool> AcceptsMultiplePayloadsProperty =
        AvaloniaProperty.Register<DropOperationBase, bool>(nameof(AcceptsMultiplePayloads), defaultValue: true);

    /// <summary>
    /// Whether a drag carrying more than one payload can be meant for this target. When false, such a drag shows no adorner and reports None.
    /// True by default, and false for <see cref="ItemDropOperation"/>, which holds one value.
    /// </summary>
    public bool AcceptsMultiplePayloads
    {
        get => GetValue(AcceptsMultiplePayloadsProperty);
        set => SetValue(AcceptsMultiplePayloadsProperty, value);
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
