namespace MonacoLabs.DragDrop;

/// <summary>
/// Interaction ids that map to platform data formats, so drags can exchange files and text with other applications.
/// </summary>
/// <remarks>
/// On a drag operation, these ids add the payload as platform data (see <c>ResolvePlatformFile</c> and <c>ResolvePlatformText</c>).
/// On a drop operation, they accept that data from any source. Platform drops never assign <c>PayloadTarget</c>:
/// <c>PayloadCommand</c> receives the dropped <see cref="Avalonia.Platform.Storage.IStorageItem"/>s or text in <see cref="DropEventArgs.Items"/>.
/// </remarks>
public static class PlatformInteractionIds
{
    public const string Files = "platform:files";
    public const string Text = "platform:text";
}
