using Avalonia;

namespace MonacoLabs.DragDrop;

public partial class TreeDropOperation
{
    public static readonly StyledProperty<TimeSpan> HoverExpandDelayProperty =
        AvaloniaProperty.Register<TreeDropOperation, TimeSpan>(nameof(HoverExpandDelay), TimeSpan.FromMilliseconds(700));

    /// <summary>
    /// How long a drag must hover over the middle of a collapsed node before it expands. <see cref="TimeSpan.Zero"/> disables expanding.
    /// </summary>
    public TimeSpan HoverExpandDelay
    {
        get => GetValue(HoverExpandDelayProperty);
        set => SetValue(HoverExpandDelayProperty, value);
    }

    public static readonly StyledProperty<string?> SelfDropMessageProperty =
        AvaloniaProperty.Register<TreeDropOperation, string?>(nameof(SelfDropMessage), "A node can't be moved into itself");

    /// <summary>
    /// Error shown when a node is dragged into its own subtree.
    /// </summary>
    public string? SelfDropMessage
    {
        get => GetValue(SelfDropMessageProperty);
        set => SetValue(SelfDropMessageProperty, value);
    }
}
