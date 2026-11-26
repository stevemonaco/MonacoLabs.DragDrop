using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Where an accepted in-process drop landed, passed to the drag source's <see cref="Abstractions.IDragOperation.DropCompleted"/>.
/// </summary>
public class DropInfo
{
    /// <summary>
    /// Pointer location relative to the drop target's attached control.
    /// </summary>
    public Point HoverLocation { get; init; }

    /// <summary>
    /// The hovered item container on collection and tree targets, otherwise the drop target's attached control.
    /// </summary>
    public required Control HoveredControl { get; init; }

    /// <summary>
    /// The Drop event the drop target handled.
    /// </summary>
    public required DragEventArgs DragEventArgs { get; init; }

    /// <summary>
    /// Value the drop target held before the payload replaced it, for single-value targets.
    /// </summary>
    public object? ReplacedPayload { get; init; }
}
