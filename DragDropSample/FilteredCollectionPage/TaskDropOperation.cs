using DragDropSample.ViewModels;
using MonacoLabs.DragDrop;

namespace DragDropSample.CustomDragDrop;

/// <summary>
/// Copies a task on a Copy drop, so the copy and the original are separate items in the backing list.
/// </summary>
public class TaskDropOperation : CollectionDropOperation
{
    protected override object CreateCopy(object payload) => payload is TaskItemViewModel task ? task.Clone() : payload;
}
