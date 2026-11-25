namespace MonacoLabs.DragDrop;

/// <summary>
/// How a <see cref="DropHighlightAdorner"/> marks its drop target.
/// </summary>
public enum DropAdornerKind
{
    /// <summary>No highlight.</summary>
    None,

    /// <summary>A tinted fill with a border.</summary>
    Solid,

    /// <summary>A border with a transparent fill.</summary>
    Border,
}
