using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class DropTargetTests
{
    [AvaloniaFact]
    public void SettingHandledEventsTooWhileAttachedThrows()
    {
        var operation = new ItemDropOperation();
        operation.Attach(new Border());

        Assert.Throws<InvalidOperationException>(() => operation.HandledEventsToo = true);
        Assert.Throws<InvalidOperationException>(() => operation.Routing = RoutingStrategies.Tunnel);
        Assert.False(operation.HandledEventsToo);
        Assert.Equal(RoutingStrategies.Bubble, operation.Routing);
    }
}
