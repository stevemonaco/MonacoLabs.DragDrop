using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonacoLabs.DragDrop;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DragDropSample.ViewModels;
public partial class DesktopPageViewModel : PageViewModel
{
    public ObservableCollection<DroppedFileViewModel> Files { get; } = [];

    public ObservableCollection<string> Snippets { get; } =
    [
        "Drag me into a text editor",
        "Drop text from a browser or editor onto this list",
        "Hold Ctrl while dropping inside the list to copy",
    ];

    [ObservableProperty]
    public partial Bitmap? Image { get; set; }

    [ObservableProperty]
    public partial string? ImageName { get; set; }

    public DesktopPageViewModel()
    {
        Title = "Desktop";
        Description = "Exchange files and text with other applications through the platform:files and platform:text interaction ids. "
            + "Drops from other applications always copy, so the source never deletes its original.";
    }

    [RelayCommand]
    private async Task AddFiles(DropEventArgs args)
    {
        if (!args.IsPlatformDrop)
            return;

        var index = args.Location?.Index ?? Files.Count;
        foreach (var item in args.Items.OfType<IStorageItem>())
        {
            var path = item.TryGetLocalPath();
            if (Files.Any(x => x.LocalPath is not null && x.LocalPath == path))
                continue;

            var properties = await item.GetBasicPropertiesAsync();
            Files.Insert(index++, new DroppedFileViewModel(item, path, properties.Size));
        }
    }

    [RelayCommand]
    private void ClearFiles() => Files.Clear();

    [RelayCommand]
    private async Task SetImage(DropEventArgs args)
    {
        if (args.Items.FirstOrDefault() is not IStorageFile file)
            return;

        await using var stream = await file.OpenReadAsync();
        var previous = Image;
        Image = new Bitmap(stream);
        ImageName = file.Name;
        previous?.Dispose();
    }

    [RelayCommand]
    private void ClearImage()
    {
        var previous = Image;
        Image = null;
        ImageName = null;
        previous?.Dispose();
    }

    [RelayCommand]
    private void AddSnippets(DropEventArgs args)
    {
        // In-process drops were already moved by the drop operation
        if (!args.IsPlatformDrop)
            return;

        var index = args.Location?.Index ?? Snippets.Count;
        foreach (var text in args.Items.OfType<string>())
            Snippets.Insert(index++, text.Trim());
    }
}
