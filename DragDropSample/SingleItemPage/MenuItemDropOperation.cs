using Avalonia.Input;
using DragDropSample.ViewModels;
using MonacoLabs.DragDrop;

namespace DragDropSample.CustomDragDrop;

/// <summary>
/// Rejects sold-out menu items with a message shown by the drop adorner.
/// </summary>
public class MenuItemDropOperation : ItemDropOperation
{
    protected override bool CanDrop(DragEventArgs e)
    {
        if (!TryGetPayload<MenuItemViewModel>(e, out var item))
            return false;

        DropAdorner!.ErrorMessage = item.IsSoldOut ? $"{item.Name} is sold out today" : null;
        return !item.IsSoldOut;
    }
}
