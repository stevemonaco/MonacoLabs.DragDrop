using Avalonia.Input;
using DragDropSample.ViewModels.Nodes;
using MonacoLabs.DragDrop;
using System.Linq;

namespace DragDropSample.CustomDragDrop;

/// <summary>
/// Validates tree drops against <see cref="IParentNode.CanAddNode"/>, and only allows projects at the top level. Every dragged node must fit.
/// </summary>
public class NodeDropOperation : TreeDropOperation
{
    protected override bool CanDrop(DragEventArgs e)
    {
        if (!TryGetPayloads<NodeViewModel>(e, out var nodes) || DropLocation is null)
            return false;

        if (DropLocation.Parent is null)
        {
            if (nodes.All(x => x is ProjectNodeViewModel))
                return true;

            return Reject("Only projects can be at the top level");
        }

        var misfit = DropLocation.Parent is IParentNode parent ? nodes.FirstOrDefault(x => !parent.CanAddNode(x)) : nodes[0];
        if (misfit is null)
            return true;

        return Reject($"{(DropLocation.Parent as NodeViewModel)?.Name} can't contain {Describe(misfit)}");
    }

    private static string Describe(NodeViewModel node) => node switch
    {
        ProjectNodeViewModel => "projects",
        FolderNodeViewModel => "folders",
        SolutionNodeViewModel => "solutions",
        _ => "files"
    };
}
