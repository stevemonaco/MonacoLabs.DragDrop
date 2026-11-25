using Avalonia.Input;
using DragDropSample.ViewModels.Nodes;
using MonacoLabs.DragDrop;

namespace DragDropSample.CustomDragDrop;

/// <summary>
/// Validates tree drops against <see cref="IParentNode.CanAddNode"/>, and only allows projects at the top level.
/// </summary>
public class NodeDropOperation : TreeDropOperation
{
    protected override bool CanDrop(DragEventArgs e)
    {
        if (!TryGetPayload<NodeViewModel>(e, out var node) || DropLocation is null)
            return false;

        if (DropLocation.Parent is null)
        {
            if (node is ProjectNodeViewModel)
                return true;

            return Reject("Only projects can be at the top level");
        }

        if (DropLocation.Parent is IParentNode parent && parent.CanAddNode(node))
            return true;

        return Reject($"{(DropLocation.Parent as NodeViewModel)?.Name} can't contain {Describe(node)}");
    }

    private static string Describe(NodeViewModel node) => node switch
    {
        ProjectNodeViewModel => "projects",
        FolderNodeViewModel => "folders",
        SolutionNodeViewModel => "solutions",
        _ => "files"
    };
}
