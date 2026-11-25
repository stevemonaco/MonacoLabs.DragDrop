using Avalonia.Controls;
using System.Collections;

namespace MonacoLabs.DragDrop;

/// <summary>
/// One dragged item of a collection drag, and where it came from.
/// </summary>
/// <param name="Item">The dragged item.</param>
/// <param name="Collection">Collection the item is removed from after a Move, or null when the drag source has none.</param>
/// <param name="Index">Index of the item in <paramref name="Collection"/> when the drag started, or null when unknown.</param>
/// <param name="Container">The item's container, or null when it wasn't realized.</param>
public sealed record CollectionDragItem(object Item, IList? Collection, int? Index, Control? Container);
