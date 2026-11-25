using Avalonia.Controls;

namespace MonacoLabs.DragDrop.Tests;

public partial class InteractionIdsView : UserControl
{
    public InteractionIdsView()
    {
        InitializeComponent();
    }

    public Border SlotBorder => Slot;
}
