using Avalonia.Controls;
using Avalonia.Input;

namespace MonacoLabs.DragDrop;
public class TreeDropOperation : CollectionDropOperation
{
    protected override void Drop(object? sender, DragEventArgs e)
    {
        if (!TryGetDragInfo<CollectionDragInfo>(e, out _) || !TryGetPayload<object>(e, out var payload))
            return;

        InvokePayloadCommand(e, payload, ItemDropAdorner?.Target ?? DropTargetOffset.AfterTarget);
        
        e.Handled = true;
        ((IPseudoClasses)AttachedControl!.Classes).Set(":dropover", false);
        ItemDropAdorner?.Detach();
        DropAdorner?.Detach();
    }
}
