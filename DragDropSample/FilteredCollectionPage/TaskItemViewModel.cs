namespace DragDropSample.ViewModels;

public enum TaskPriority
{
    High,
    Medium,
    Low
}

public sealed class TaskItemViewModel(string title, TaskPriority priority, bool isDone = false)
{
    public string Title { get; } = title;

    public TaskPriority Priority { get; } = priority;

    public bool IsDone { get; } = isDone;

    public TaskItemViewModel Clone() => new(Title, Priority, IsDone);
}
