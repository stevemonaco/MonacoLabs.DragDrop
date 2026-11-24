using CommunityToolkit.Mvvm.ComponentModel;

namespace DragDropSample.ViewModels.Nodes;
public abstract partial class NodeViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public abstract string Icon { get; }

    public NodeViewModel(string name)
    {
        Name = name;
    }
}
