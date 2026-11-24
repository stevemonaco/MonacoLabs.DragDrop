using Avalonia.Data;
using Avalonia;
using Avalonia.Controls;
using MonacoLabs.DragDrop.Abstractions;

namespace MonacoLabs.DragDrop;
public partial class DragDrop
{
    static DragDrop()
    {
        DragOperationProperty.Changed.AddClassHandler<Control>(HandleDragOperationChanged);
        DropOperationProperty.Changed.AddClassHandler<Control>(HandleDropOperationChanged);
    }

    /// <summary>
    /// Identifies the DragOperation attached property, which makes a control a drag source via an <see cref="IDragOperation"/>.
    /// </summary>
    public static readonly AttachedProperty<IDragOperation?> DragOperationProperty = AvaloniaProperty.RegisterAttached<DragDrop, Control, IDragOperation?>(
        "DragOperation", null, false, BindingMode.OneTime);

    /// <summary>
    /// Sets the <see cref="DragOperationProperty"/> attached property on <paramref name="element"/>.
    /// </summary>
    public static void SetDragOperation(AvaloniaObject element, IDragOperation value)
    {
        element.SetValue(DragOperationProperty, value);
    }

    /// <summary>
    /// Gets the <see cref="DragOperationProperty"/> attached property from <paramref name="element"/>.
    /// </summary>
    public static IDragOperation? GetDragOperation(AvaloniaObject element)
    {
        return element.GetValue(DragOperationProperty);
    }

    private static void HandleDragOperationChanged(Control control, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.OldValue is IDragOperation oldDrag)
        {
            oldDrag.Detach(control);
        }

        if (change.NewValue is IDragOperation newDrag)
        {
            newDrag.Attach(control);
        }
    }

    /// <summary>
    /// Identifies the DropOperation attached property, which makes a control a drop target via an <see cref="IDropOperation"/>.
    /// </summary>
    public static readonly AttachedProperty<IDropOperation?> DropOperationProperty = AvaloniaProperty.RegisterAttached<DragDrop, Control, IDropOperation?>(
        "DropOperation", null, false, BindingMode.OneTime);

    /// <summary>
    /// Sets the <see cref="DropOperationProperty"/> attached property on <paramref name="element"/>.
    /// </summary>
    public static void SetDropOperation(AvaloniaObject element, IDropOperation value)
    {
        element.SetValue(DropOperationProperty, value);
    }

    /// <summary>
    /// Gets the <see cref="DropOperationProperty"/> attached property from <paramref name="element"/>.
    /// </summary>
    public static IDropOperation? GetDropOperation(AvaloniaObject element)
    {
        return element.GetValue(DropOperationProperty);
    }

    private static void HandleDropOperationChanged(Control control, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.OldValue is IDropOperation oldDrop)
        {
            oldDrop.Detach(control);
        }

        if (change.NewValue is IDropOperation newDrop)
        {
            newDrop.Attach(control);
        }
    }
}
