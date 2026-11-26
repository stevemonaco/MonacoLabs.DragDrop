using Avalonia.Controls;
using Avalonia.Input;

namespace MonacoLabs.DragDrop.Abstractions;

/// <summary>
/// A drag source, attached to a control through <see cref="DragDrop.DragOperationProperty"/>.
/// </summary>
public interface IDragOperation
{
    /// <summary>
    /// Channels the drag advertises. A drop target accepts the drag only when it shares one of them.
    /// </summary>
    IList<string> InteractionIds { get; set; }

    /// <summary>
    /// The control the operation is attached to, or null while detached.
    /// </summary>
    Control? AttachedControl { get; }

    /// <summary>
    /// The value to drag. See <see cref="DragOperationBase.Payload"/> for what is dragged when it is not set.
    /// </summary>
    object? Payload { get; set; }

    /// <summary>
    /// Hooks the operation to <paramref name="control"/>. Throws <see cref="InvalidOperationException"/> when it is already attached.
    /// </summary>
    void Attach(Control control);

    /// <summary>
    /// Unhooks the operation from <paramref name="control"/>, so it can attach again.
    /// </summary>
    void Detach(Control control);

    /// <summary>
    /// Called by the drop target after it accepted an in-process drop. The source removes what it moved when <paramref name="effect"/> is Move.
    /// </summary>
    void DropCompleted(DragDropEffects effect, DragInfo dragInfo, DropInfo dropInfo);
}
