using Avalonia.Input;
using System.Collections.Concurrent;

namespace MonacoLabs.DragDrop;

internal static class DragDropIds
{
    public static string DefaultOperation { get; } = "app:default";
    public static string DragMetadata { get; } = "app:dragmetadata";

    public static DataFormat<DragInfo> DragMetadataFormat { get; } = DataFormat.CreateInProcessFormat<DragInfo>(DragMetadata);

    private static readonly ConcurrentDictionary<string, DataFormat<object>> PayloadFormats = new();

    public static DataFormat<object> PayloadFormat(string interactionId) =>
        PayloadFormats.GetOrAdd(interactionId, DataFormat.CreateInProcessFormat<object>);

    public static bool IsPlatform(string interactionId) =>
        interactionId is PlatformInteractionIds.Files or PlatformInteractionIds.Text;

    public static DataFormat FormatFor(string interactionId) => interactionId switch
    {
        PlatformInteractionIds.Files => DataFormat.File,
        PlatformInteractionIds.Text => DataFormat.Text,
        _ => PayloadFormat(interactionId)
    };
}
