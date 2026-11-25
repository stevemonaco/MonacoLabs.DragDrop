namespace MonacoLabs.DragDrop;

/// <summary>
/// Which items a collection or tree drag carries.
/// </summary>
public enum DraggedItems
{
    /// <summary>
    /// Every selected item when the pressed item is one of several selected, otherwise the pressed item.
    /// </summary>
    SelectedItems,

    /// <summary>
    /// Only the pressed item, whatever else is selected.
    /// </summary>
    PressedItem
}
