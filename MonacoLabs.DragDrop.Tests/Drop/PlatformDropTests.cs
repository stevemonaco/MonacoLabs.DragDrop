using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class PlatformDropTests
{
    [AvaloniaFact]
    public void ATargetListingThePlatformTextIdAcceptsTextFromAnotherApp()
    {
        var operation = new ItemDropOperation { InteractionIds = [PlatformInteractionIds.Text] };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(PlatformData(text: "hello"));

        Assert.Equal(DragDropEffects.Copy, drag.Enter(window.PointIn(target)));
        Assert.Equal(DragDropEffects.Copy, drag.Drop(window.PointIn(target)));
    }

    [AvaloniaFact]
    public void ADragFromThisAppAdvertisingOnlyAPlatformIdTheTargetListsIsAPlatformDrop()
    {
        DropEventArgs? args = null;
        var operation = new ItemDropOperation { InteractionIds = [PlatformInteractionIds.Text], PayloadCommand = Record(a => args = a) };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation { InteractionIds = ["notes", PlatformInteractionIds.Text] }, "hello"));

        Assert.Equal(DragDropEffects.Copy, drag.Enter(window.PointIn(target)));
        Assert.Equal(DragDropEffects.Copy, drag.Drop(window.PointIn(target)));

        Assert.True(args?.IsPlatformDrop);
        Assert.Equal(["hello"], args?.Items);
    }

    [AvaloniaFact]
    public void AMatchingInProcessIdMakesTheDropInProcess()
    {
        DropEventArgs? args = null;
        var operation = new ItemDropOperation { InteractionIds = ["notes", PlatformInteractionIds.Text], PayloadCommand = Record(a => args = a) };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(new ItemDragOperation { InteractionIds = ["notes", PlatformInteractionIds.Text] }, "hello"));

        drag.Enter(window.PointIn(target));
        Assert.Equal(DragDropEffects.Move, drag.Drop(window.PointIn(target)));

        Assert.False(args?.IsPlatformDrop);
        Assert.Equal("hello", operation.PayloadTarget);
    }

    [AvaloniaFact]
    public async Task APlatformDropOfFilesPassesTheStorageItems()
    {
        DropEventArgs? args = null;
        var operation = new ItemDropOperation { InteractionIds = [PlatformInteractionIds.Files], PayloadCommand = Record(a => args = a) };
        var (window, target) = Show(operation);
        var first = await TestFiles.CreateAsync(window, "a.txt");
        var second = await TestFiles.CreateAsync(window, "b.txt");
        using var drag = window.SimulateDrag(PlatformData([first, second]));

        drag.Enter(window.PointIn(target));
        drag.Drop(window.PointIn(target));

        Assert.Equal([first, second], args?.Items);
    }

    [AvaloniaFact]
    public void APlatformDropOfTextPassesTheTextAsTheOnlyItem()
    {
        DropEventArgs? args = null;
        var operation = new ItemDropOperation { InteractionIds = [PlatformInteractionIds.Files, PlatformInteractionIds.Text], PayloadCommand = Record(a => args = a) };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(PlatformData(text: "hello"));

        drag.Enter(window.PointIn(target));
        drag.Drop(window.PointIn(target));

        Assert.Equal(["hello"], args?.Items);
    }

    [AvaloniaFact]
    public async Task FilesWinOverTextWhenTheTargetListsBoth()
    {
        DropEventArgs? args = null;
        var operation = new ItemDropOperation { InteractionIds = [PlatformInteractionIds.Text, PlatformInteractionIds.Files], PayloadCommand = Record(a => args = a) };
        var (window, target) = Show(operation);
        var file = await TestFiles.CreateAsync(window, "a.txt");
        using var drag = window.SimulateDrag(PlatformData([file], "hello"));

        drag.Enter(window.PointIn(target));
        drag.Drop(window.PointIn(target));

        Assert.Equal([file], args?.Items);
    }

    [AvaloniaFact]
    public void APlatformDropLeavesPayloadTargetAndNeverCompletesTheDrag()
    {
        DropEventArgs? args = null;
        var source = new RecordingDragOperation { InteractionIds = ["notes", PlatformInteractionIds.Text] };
        var operation = new ItemDropOperation { InteractionIds = [PlatformInteractionIds.Text], PayloadTarget = "old", PayloadCommand = Record(a => args = a) };
        var (window, target) = Show(operation);
        using var drag = window.SimulateDrag(DragData.For(source, "hello"));

        drag.Enter(window.PointIn(target));
        drag.Drop(window.PointIn(target));

        Assert.Equal("old", operation.PayloadTarget);
        Assert.Empty(source.Completions);
        Assert.True(args?.IsPlatformDrop);
        Assert.Equal(["hello"], args?.Items);
    }

    [AvaloniaFact]
    public void APlatformIdWhoseResolverReturnsNullCarriesNoData()
    {
        var ids = new List<string> { PlatformInteractionIds.Text, PlatformInteractionIds.Files };

        IDataTransfer withText = DragData.For(new ItemDragOperation { InteractionIds = ids }, "hello");
        IDataTransfer withoutText = DragData.For(new NoTextDragOperation { InteractionIds = ids }, "hello");

        Assert.True(withText.Contains(DataFormat.Text));
        Assert.False(withText.Contains(DataFormat.File));
        Assert.False(withoutText.Contains(DataFormat.Text));
    }

    private static (Window, Border) Show(ItemDropOperation operation)
    {
        var target = new Border { Background = Brushes.Blue, Width = 100, Height = 100 };
        DragDrop.SetDropOperation(target, operation);
        return (TestWindow.Show(target), target);
    }

    private static DataTransfer PlatformData(IReadOnlyList<IStorageItem>? files = null, string? text = null)
    {
        var data = new DataTransfer();
        foreach (var file in files ?? [])
        {
            var item = new DataTransferItem();
            item.SetFile(file);
            data.Add(item);
        }

        if (text is not null)
        {
            var item = new DataTransferItem();
            item.SetText(text);
            data.Add(item);
        }

        return data;
    }

    private static RelayCommand Record(Action<DropEventArgs?> record) => new(a => record(a as DropEventArgs));

    private sealed class NoTextDragOperation : ItemDragOperation
    {
        protected override string? ResolvePlatformText(object payload) => null;
    }
}
