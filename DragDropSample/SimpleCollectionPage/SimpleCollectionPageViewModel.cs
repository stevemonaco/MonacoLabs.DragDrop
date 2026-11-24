using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DragDropSample.ViewModels;
public class SimpleCollectionPageViewModel : PageViewModel
{
    public ObservableCollection<Guid> LeftGuids { get; }
    public ObservableCollection<Guid> RightGuids { get; } = [];

    public SimpleCollectionPageViewModel()
    {
        Title = "Simple Collection";
        Description = "Reorder within a list or move items between lists. Ctrl+click to multi-select, then drag any selected item.";
        LeftGuids = new(Enumerable.Range(0, 20).Select(x => Guid.NewGuid()));
    }
}
