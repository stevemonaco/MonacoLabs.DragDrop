namespace MonacoLabs.DragDrop;

/// <summary>
/// Arguments passed to <c>PayloadCommand</c> when a drop is applied.
/// </summary>
/// <param name="Target">Element under the pointer when the drop occurred.</param>
/// <param name="TargetContext">DataContext of <paramref name="Target"/>.</param>
/// <param name="Items">Dropped payload, or the files or text of a platform drop.</param>
/// <param name="Offset">Position of the drop relative to the hovered item.</param>
/// <param name="Location">Insertion point for drops into collections.</param>
/// <param name="IsPlatformDrop">True when <paramref name="Items"/> came from platform data rather than an in-process payload.</param>
public record class DropEventArgs(
    object? Target,
    object? TargetContext,
    IReadOnlyList<object> Items,
    DropTargetOffset Offset,
    CollectionDropLocation? Location = null,
    bool IsPlatformDrop = false);
