using Avalonia;
using Avalonia.Controls;
using MonacoLabs.DragDrop.Abstractions;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Attached properties that make controls drag sources and drop targets, and mark drag handles and ignored presses inside them.
/// </summary>
public static partial class DragDrop
{
    static DragDrop()
    {
        DragOperationProperty.Changed.AddClassHandler<Control>(HandleDragOperationChanged);
        DropOperationProperty.Changed.AddClassHandler<Control>(HandleDropOperationChanged);
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
