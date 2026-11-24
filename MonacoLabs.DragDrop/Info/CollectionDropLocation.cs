using Avalonia.Controls;
using System.Collections;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Where a collection drop inserts its payload.
/// </summary>
/// <param name="Collection">Collection that receives the payload.</param>
/// <param name="Index">Insertion index, or null to append.</param>
/// <param name="Parent">Data item that owns <paramref name="Collection"/>, such as a tree node. Null for a flat list or a tree's root.</param>
/// <param name="ParentContainer">Control that displays <paramref name="Collection"/>, such as the parent <see cref="TreeViewItem"/>.</param>
public sealed record CollectionDropLocation(IList Collection, int? Index, object? Parent, Control? ParentContainer);
