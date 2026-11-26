using System.Collections.ObjectModel;

namespace DragDropSample.ViewModels.Nodes;

public partial class FolderNodeViewModel : NodeViewModel, IParentNode
{
    public ObservableCollection<NodeViewModel> Children { get; init; } = [];

    public override string Icon => "📁";

    public FolderNodeViewModel(string name) : base(name)
    {
    }

    public bool CanAddNode(NodeViewModel node)
    {
        return node is FolderNodeViewModel or FileNodeViewModel;
    }
}
