using Avalonia.Controls;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Contains additional drag metadata for collection controls
/// </summary>
public class CollectionDragInfo : DragInfo
{
    /// <summary>
    /// One entry per payload, in the order of <see cref="DragInfo.Payloads"/>.
    /// </summary>
    public IReadOnlyList<CollectionDragItem> Items { get; init; } = [];

    /// <summary>
    /// Item container the drag started from, or null when the press wasn't on one.
    /// </summary>
    public Control? PressedContainer { get; init; }
}
