using Avalonia.Input;

namespace MonacoLabs.DragDrop.Tests;

/// <summary>
/// A single-value drop target that records the metadata of the last drag it evaluated.
/// </summary>
public class RecordingDropOperation : ItemDropOperation
{
    public DragInfo? LastDragInfo { get; private set; }

    protected override bool CanDrop(DragEventArgs e)
    {
        LastDragInfo = TryGetDragInfo<DragInfo>(e, out var info) ? info : null;
        return base.CanDrop(e);
    }
}
