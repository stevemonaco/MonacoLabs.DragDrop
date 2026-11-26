using DragDropSample.ViewModels.Nodes;
using System.Collections.ObjectModel;

namespace DragDropSample.ViewModels;

public partial class TreeViewPageViewModel : PageViewModel
{
    public ObservableCollection<NodeViewModel> Solutions { get; }
    public ObservableCollection<NodeViewModel> FlatItems { get; } = [];

    public TreeViewPageViewModel()
    {
        Title = "Tree ↔ List";
        Description = "Drag nodes between the tree and the flat list, or rearrange the tree. Drop on a node's top or bottom edge to insert beside it, "
            + "or on its middle to move it inside. Folders hold folders and files, and only projects can be at the top level.";

        Solutions = new()
        {
            new ProjectNodeViewModel("DragDropSample")
            {
                IsExpanded = true,
                Children = new()
                {
                    new FolderNodeViewModel("Assets")
                    {
                        Children =
                        [
                            new FileNodeViewModel("avalonia-logo.ico")
                        ]
                    },
                    new FolderNodeViewModel("Shell")
                    {
                        IsExpanded = true,
                        Children =
                        [
                            new FileNodeViewModel("MainWindow.axaml"),
                            new FileNodeViewModel("MainWindow.axaml.cs"),
                            new FileNodeViewModel("MainWindowViewModel.cs"),
                            new FileNodeViewModel("PageViewModel.cs")
                        ]
                    },
                    new FileNodeViewModel("App.axaml"),
                    new FileNodeViewModel("App.axaml.cs"),
                    new FileNodeViewModel("Program.cs"),
                    new FileNodeViewModel("ViewLocator.cs")
                }
            },
            new ProjectNodeViewModel("MonacoLabs.DragDrop")
            {
                IsExpanded = true,
                Children = new()
                {
                    new FileNodeViewModel("DragDrop.cs"),
                    new FileNodeViewModel("DragDrop.props.cs"),
                    new FileNodeViewModel("DragDropIds.cs")
                }
            }
        };
    }
}
