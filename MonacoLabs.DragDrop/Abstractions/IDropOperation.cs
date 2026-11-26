using Avalonia.Controls;

namespace MonacoLabs.DragDrop.Abstractions;

/// <summary>
/// A drop target, attached to a control through <see cref="DragDrop.DropOperationProperty"/>.
/// </summary>
public interface IDropOperation
{
    /// <summary>
    /// Channels the target accepts. A drag is meant for it only when the drag advertises one of them.
    /// </summary>
    IList<string> InteractionIds { get; set; }

    /// <summary>
    /// The control the operation is attached to, or null while detached.
    /// </summary>
    Control? AttachedControl { get; }

    /// <summary>
    /// What receives dropped payloads: the value itself on single-value targets, or the list on collection targets.
    /// </summary>
    object? PayloadTarget { get; set; }

    /// <summary>
    /// Hooks the operation to <paramref name="control"/>. Throws <see cref="InvalidOperationException"/> when it is already attached.
    /// </summary>
    void Attach(Control control);

    /// <summary>
    /// Unhooks the operation from <paramref name="control"/>, ending any hover it shows, so it can attach again.
    /// </summary>
    void Detach(Control control);
}
