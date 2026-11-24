using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using MonacoLabs.DragDrop.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace MonacoLabs.DragDrop.Tests;

public class TestApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
}

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
