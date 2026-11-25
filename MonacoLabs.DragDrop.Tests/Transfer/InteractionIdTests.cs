using Avalonia.Headless.XUnit;
using Avalonia.Input;
using MonacoLabs.DragDrop.Abstractions;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class InteractionIdTests
{
    [Fact]
    public void TheFilesIdMapsToThePlatformFileFormat() =>
        Assert.Equal(DataFormat.File, DragDropIds.FormatFor(PlatformInteractionIds.Files));

    [Fact]
    public void TheTextIdMapsToThePlatformTextFormat() =>
        Assert.Equal(DataFormat.Text, DragDropIds.FormatFor(PlatformInteractionIds.Text));

    [Theory]
    [InlineData("app:default")]
    [InlineData("workers")]
    [InlineData("platform:other")]
    public void OtherIdsMapToAnInProcessFormat(string id)
    {
        var format = DragDropIds.FormatFor(id);

        Assert.Equal(DataFormatKind.InProcess, format.Kind);
        Assert.Equal(DataFormat.CreateInProcessFormat<object>(id), format);
    }

    [AvaloniaFact]
    public void DragOperationsDefaultToTheAppDefaultId() =>
        Assert.All(
            new IDragOperation[] { new ItemDragOperation(), new CollectionDragOperation(), new TreeDragOperation() },
            op => Assert.Equal(["app:default"], op.InteractionIds));

    [AvaloniaFact]
    public void DropOperationsDefaultToTheAppDefaultId() =>
        Assert.All(
            new IDropOperation[] { new ItemDropOperation(), new CollectionDropOperation(), new TreeDropOperation() },
            op => Assert.Equal(["app:default"], op.InteractionIds));
}
