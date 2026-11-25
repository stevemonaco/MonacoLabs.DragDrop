using Avalonia.Input;

namespace MonacoLabs.DragDrop.Tests;

/// <summary>
/// A single-value drop target that records the metadata of the last drag it evaluated, and runs <see cref="Probe"/> on each evaluation
/// so tests can look at the drag while it is in flight.
/// </summary>
public class RecordingDropOperation : ItemDropOperation
{
    public DragInfo? LastDragInfo { get; private set; }

    public Action<DragEventArgs>? Probe { get; set; }

    protected override bool CanDrop(DragEventArgs e)
    {
        LastDragInfo = TryGetDragInfo<DragInfo>(e, out var info) ? info : null;
        Probe?.Invoke(e);
        return base.CanDrop(e);
    }
}
