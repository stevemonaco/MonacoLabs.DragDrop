using Avalonia.Controls;
using System.Collections;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Where a drop into a list or tree landed: the collection, the index of the first payload and how many were inserted.
/// </summary>
public class CollectionDropInfo : DropInfo
{
    /// <summary>
    /// Collection that the payload has been dropped into
    /// </summary>
    public IList? TargetCollection { get; init; }

    /// <summary>
    /// UI container in the collection control associated with the drop
    /// ie. ListBoxItem, TreeViewItem, or topmost for ItemsControl
    /// </summary>
    public Control? TargetContainer { get; init; }

    /// <summary>
    /// Index in <see cref="TargetCollection"/> of the first dropped payload, or null when unknown.
    /// </summary>
    public int? TargetIndex { get; init; }

    /// <summary>
    /// Number of payloads dropped at <see cref="TargetIndex"/>, consecutively.
    /// </summary>
    public int TargetCount { get; init; } = 1;

    /// <summary>
    /// Data item that owns <see cref="TargetCollection"/>, such as the parent tree node. Null for a flat list or a tree's root.
    /// </summary>
    public object? TargetParent { get; init; }
}
