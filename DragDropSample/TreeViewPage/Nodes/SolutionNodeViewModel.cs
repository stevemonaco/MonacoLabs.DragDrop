using System.Collections.ObjectModel;

namespace DragDropSample.ViewModels.Nodes;
public partial class SolutionNodeViewModel : NodeViewModel, IParentNode
{
    public ObservableCollection<NodeViewModel> Children { get; init; } = [];

    public override string Icon => "🧩";

    public SolutionNodeViewModel(string name) : base(name)
    {
    }

    public bool CanAddNode(NodeViewModel node)
    {
        return node is ProjectNodeViewModel;
    }
}
