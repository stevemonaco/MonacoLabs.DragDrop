using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace MonacoLabs.DragDrop;

internal static class AdornerBrushes
{
    public static IImmutableSolidColorBrush Accent { get; } = new ImmutableSolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
    public static IImmutableSolidColorBrush AccentFill { get; } = new ImmutableSolidColorBrush(Color.FromArgb(0x24, 0x00, 0x78, 0xD4));
    public static IImmutableSolidColorBrush Error { get; } = new ImmutableSolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x1C));
    public static IImmutableSolidColorBrush ErrorFill { get; } = new ImmutableSolidColorBrush(Color.FromArgb(0x24, 0xC4, 0x2B, 0x1C));
}
