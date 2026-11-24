namespace DragDropSample.ViewModels.Nodes;
public partial class FileNodeViewModel : NodeViewModel
{
    public override string Icon => "📄";

    public FileNodeViewModel(string name) : base(name)
    {
    }
}
