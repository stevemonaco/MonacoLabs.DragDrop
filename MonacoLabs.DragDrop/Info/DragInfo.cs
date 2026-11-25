using Avalonia;
using MonacoLabs.DragDrop.Abstractions;

namespace MonacoLabs.DragDrop;

/// <summary>
/// Contains metadata about the app state at the time of drag initiation
/// </summary>
public class DragInfo
{
    /// <summary>
    /// Drag Operation that created this metadata
    /// </summary>
    public required IDragOperation DragOperation { get; init; }

    /// <summary>
    /// Pointer location when first clicked
    /// </summary>
    public Point DragOrigin { get; init; }

    /// <summary>
    /// Ids specified by the DragOperation
    /// </summary>
    public required IList<string> DragIds { get; init; }

    /// <summary>
    /// Id associated with the pointer that started the drag operation
    /// </summary>
    public int? PointerId { get; init; }

    /// <summary>
    /// Payloads resolved when the drag started, in order. A single-item drag has one.
    /// </summary>
    public IReadOnlyList<object> Payloads { get; internal set; } = [];

    /// <summary>
    /// First of <see cref="Payloads"/>.
    /// </summary>
    public object? Payload => Payloads.Count > 0 ? Payloads[0] : null;
}
