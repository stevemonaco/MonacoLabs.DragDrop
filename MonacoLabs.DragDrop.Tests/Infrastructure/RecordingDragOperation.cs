using Avalonia.Input;

namespace MonacoLabs.DragDrop.Tests;

/// <summary>
/// A single-value drag source that records each <see cref="DropCompleted"/> call before handling it as usual.
/// </summary>
public class RecordingDragOperation : ItemDragOperation
{
    public List<(DragDropEffects Effect, DropInfo DropInfo)> Completions { get; } = [];

    public override void DropCompleted(DragDropEffects effect, DragInfo dragInfo, DropInfo dropInfo)
    {
        Completions.Add((effect, dropInfo));
        base.DropCompleted(effect, dragInfo, dropInfo);
    }
}
