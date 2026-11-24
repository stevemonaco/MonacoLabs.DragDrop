using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class TestAppTests
{
    [Fact]
    public void TheTestAssemblySeesLibraryInternals() =>
        Assert.True(DragDropIds.IsPlatform(PlatformInteractionIds.Text));

    [AvaloniaFact]
    public void AListBoxInAShownWindowHasItemContainers()
    {
        var list = new ListBox { ItemsSource = new[] { "a", "b", "c" } };
        TestWindow.Show(list);

        Assert.All(Enumerable.Range(0, 3), i => Assert.IsType<ListBoxItem>(list.ContainerFromIndex(i)));
    }
}
