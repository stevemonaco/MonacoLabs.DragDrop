using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using MonacoLabs.DragDrop.Abstractions;

namespace MonacoLabs.DragDrop;
public static partial class DragDrop
{
    /// <summary>
    /// Identifies the DragOperation attached property, which makes a control a drag source via an <see cref="IDragOperation"/>.
    /// </summary>
    public static readonly AttachedProperty<IDragOperation?> DragOperationProperty = AvaloniaProperty.RegisterAttached<Control, IDragOperation?>(
        "DragOperation", typeof(DragDrop), null, false, BindingMode.OneTime);

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

    /// <summary>
    /// Identifies the DropOperation attached property, which makes a control a drop target via an <see cref="IDropOperation"/>.
    /// </summary>
    public static readonly AttachedProperty<IDropOperation?> DropOperationProperty = AvaloniaProperty.RegisterAttached<Control, IDropOperation?>(
        "DropOperation", typeof(DragDrop), null, false, BindingMode.OneTime);

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

    /// <summary>
    /// Identifies the IsDragHandle attached property, which marks an element inside a drag source as a place where drags can start.
    /// Only drag operations with <see cref="DragOperationBase.RequiresDragHandle"/> set restrict drags to handles.
    /// </summary>
    public static readonly AttachedProperty<bool> IsDragHandleProperty = AvaloniaProperty.RegisterAttached<Control, bool>("IsDragHandle", typeof(DragDrop));

    public static void SetIsDragHandle(AvaloniaObject element, bool value)
    {
        element.SetValue(IsDragHandleProperty, value);
    }

    public static bool GetIsDragHandle(AvaloniaObject element)
    {
        return element.GetValue(IsDragHandleProperty);
    }
}
