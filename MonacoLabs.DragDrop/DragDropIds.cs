using Avalonia.Input;

namespace MonacoLabs.DragDrop;
internal static class DragDropIds
{
    public static string DefaultOperation { get; } = "app:default";
    public static string DragMetadata { get; } = "app:dragmetadata";

    public static DataFormat<DragInfo> DragMetadataFormat { get; } = DataFormat.CreateInProcessFormat<DragInfo>(DragMetadata);

    public static DataFormat<object> PayloadFormat(string interactionId) => DataFormat.CreateInProcessFormat<object>(interactionId);

    public static bool IsPlatform(string interactionId) =>
        interactionId is PlatformInteractionIds.Files or PlatformInteractionIds.Text;

    public static DataFormat FormatFor(string interactionId) => interactionId switch
    {
        PlatformInteractionIds.Files => DataFormat.File,
        PlatformInteractionIds.Text => DataFormat.Text,
        _ => PayloadFormat(interactionId)
    };
}
