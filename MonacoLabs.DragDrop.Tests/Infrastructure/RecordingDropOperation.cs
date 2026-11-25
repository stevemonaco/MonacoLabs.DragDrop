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

/// <summary>
/// A command-only drop target that accepts drags of several payloads, records the metadata and payloads of the last drag it evaluated,
/// and runs <see cref="Probe"/> on each evaluation.
/// </summary>
public class PayloadsRecordingDropOperation : Abstractions.DropOperationBase
{
    public DragInfo? LastDragInfo { get; private set; }

    public IReadOnlyList<object>? LastPayloads { get; private set; }

    public Action<DragEventArgs>? Probe { get; set; }

    protected override bool CanDrop(DragEventArgs e)
    {
        LastDragInfo = TryGetDragInfo<DragInfo>(e, out var info) ? info : null;
        LastPayloads = TryGetPayloads<object>(e, out var payloads) ? payloads : null;
        Probe?.Invoke(e);
        return base.CanDrop(e);
    }
}
