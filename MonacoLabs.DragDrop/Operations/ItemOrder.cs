namespace MonacoLabs.DragDrop;

/// <summary>
/// Who decides the order of a collection drop target's items.
/// </summary>
public enum ItemOrder
{
    /// <summary>
    /// The user: a drop inserts where the insertion line shows.
    /// </summary>
    Positional,

    /// <summary>
    /// The collection, which sorts itself: a drop appends, shows no insertion line, and the list can't be reordered by dragging within it.
    /// </summary>
    Sorted
}
