using Avalonia.Input;
using DragDropSample.ViewModels;
using MonacoLabs.DragDrop;

namespace DragDropSample.CustomDragDrop;

/// <summary>
/// Rejects sold-out menu items and drops into locked orders with a message shown by the drop adorner.
/// </summary>
public class MenuItemDropOperation : ItemDropOperation
{
    protected override bool CanDrop(DragEventArgs e)
    {
        if (!TryGetPayload<MenuItemViewModel>(e, out var item))
            return false;

        if (AttachedControl?.DataContext is GuestOrderViewModel { IsLocked: true } guest)
        {
            DropAdorner!.ErrorMessage = $"{guest.Name}'s order is locked";
            return false;
        }

        if (item.IsSoldOut)
        {
            DropAdorner!.ErrorMessage = $"{item.Name} is sold out today";
            return false;
        }

        return true;
    }
}
