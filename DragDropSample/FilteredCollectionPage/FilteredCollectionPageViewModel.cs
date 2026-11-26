using CommunityToolkit.Mvvm.ComponentModel;
using MonacoLabs.DragDrop;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DragDropSample.ViewModels;

public partial class FilteredCollectionPageViewModel : PageViewModel
{
    private readonly FilteredView<TaskItemViewModel> _backlogView;

    public ObservableCollection<TaskItemViewModel> Backlog { get; } =
    [
        new("Fix login timeout", TaskPriority.High),
        new("Write release notes", TaskPriority.Low),
        new("Add dark mode toggle", TaskPriority.Medium),
        new("Profile startup time", TaskPriority.Medium, isDone: true),
        new("Migrate settings file", TaskPriority.High),
        new("Update onboarding copy", TaskPriority.Low, isDone: true),
        new("Cache avatar images", TaskPriority.Medium),
        new("Fix crash on resume", TaskPriority.High, isDone: true),
        new("Localize error messages", TaskPriority.Low),
        new("Add export to CSV", TaskPriority.Medium),
        new("Review open pull requests", TaskPriority.High),
        new("Tidy logging output", TaskPriority.Low),
    ];

    public ObservableCollection<TaskItemViewModel> ShownBacklog => _backlogView.Items;

    public ObservableCollection<TaskItemViewModel> Sprint { get; } = [];

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    public partial bool HideDone { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BacklogOrder))]
    public partial bool SortByPriority { get; set; }

    public ItemOrder BacklogOrder => SortByPriority ? ItemOrder.Sorted : ItemOrder.Positional;

    public FilteredCollectionPageViewModel()
    {
        Title = "Filtered & Sorted";
        Description = "The backlog shows a filtered, optionally sorted view of its list. Drops land next to the item you point at in the underlying list, "
            + "and moves remove the right original. With Sort by priority on, drops append and the list sorts them into place.";
        _backlogView = new FilteredView<TaskItemViewModel>(Backlog);
    }

    partial void OnSearchTextChanged(string? value) => UpdateBacklogView();

    partial void OnHideDoneChanged(bool value) => UpdateBacklogView();

    partial void OnSortByPriorityChanged(bool value) => UpdateBacklogView();

    private void UpdateBacklogView()
    {
        var search = SearchText?.Trim();
        _backlogView.Update(
            task => (!HideDone || !task.IsDone) && (string.IsNullOrEmpty(search) || task.Title.Contains(search, StringComparison.OrdinalIgnoreCase)),
            SortByPriority ? tasks => tasks.OrderBy(x => x.Priority) : null);
    }
}
