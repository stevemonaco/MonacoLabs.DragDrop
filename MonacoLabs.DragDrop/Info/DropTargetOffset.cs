namespace MonacoLabs.DragDrop;

/// <summary>
/// Where a drop lands relative to the hovered item.
/// </summary>
public enum DropTargetOffset
{
    /// <summary>
    /// Into the item, such as among a tree node's children, or onto the target as a whole.
    /// </summary>
    OnTarget,
    BeforeTarget,
    AfterTarget,
}
