using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace MonacoLabs.DragDrop;
public class DropInfo
{
    /// <summary>
    /// Pointer location relative to HoveredControl
    /// </summary>
    public Point HoverLocation { get; init; }

    public required Control HoveredControl { get; init; }

    public required DragEventArgs DragEventArgs { get; init; }

    /// <summary>
    /// Value the drop target held before the payload replaced it, for single-value targets.
    /// </summary>
    public object? ReplacedPayload { get; init; }
}
