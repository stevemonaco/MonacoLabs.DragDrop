using Avalonia;
using Avalonia.Data;

namespace MonacoLabs.DragDrop;

public partial class ItemDropOperation
{
    public static readonly StyledProperty<object?> PayloadTargetProperty =
        AvaloniaProperty.Register<ItemDropOperation, object?>(nameof(PayloadTarget), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// The value an accepted in-process drop replaces with the payload. Binds TwoWay by default.
    /// </summary>
    public object? PayloadTarget
    {
        get => GetValue(PayloadTargetProperty);
        set => SetValue(PayloadTargetProperty, value);
    }
}
