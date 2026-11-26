# MonacoLabs.DragDrop

MVVM-friendly drag and drop for [Avalonia](https://avaloniaui.net) 12. Attach a drag operation and a drop operation to your controls in XAML, bind them to your view model, and the library handles the rest. It starts the drag, shows a preview and drop feedback, moves or copies the payloads, and removes moved items from their source.

## Features

- **Single values, lists and trees:** `ItemDragOperation` / `ItemDropOperation`, `CollectionDragOperation` / `CollectionDropOperation` and `TreeDragOperation` / `TreeDropOperation`.
- **Multi-selection drags** from a `ListBox` or `TreeView`, kept in the control's order.
- **Feedback:** a snapshot preview that follows the pointer, a highlight over the target, an insertion line between items, auto-scroll near the edges, and hover-to-expand in trees.
- **Typed channels:** interaction ids decide which targets accept which drags, so one source can feed several targets and each target takes only what it understands.
- **Move, copy and link:** effects follow the modifier keys (Ctrl or Option copies, Shift moves), with a `CreateCopy` hook for real copies.
- **Filtered and sorted lists:** drops land next to the item you point at in the underlying collection, and `ItemOrder="Sorted"` makes a target append to a list that sorts itself.
- **Other applications:** drag files and text in from your file manager or browser, and out to other apps (`platform:files`, `platform:text`).
- **Validation:** override `CanDrop` and return `Reject("why")` to show a message on the target, or let `PayloadCommand.CanExecute` refuse a drop as the pointer reaches it.
- **Styleable:** pseudo-classes on the controls involved, and adorners whose looks come from control themes.

## AI Disclaimer

This library, API structure, and usability ideas were originally hand-written. After painful experiences with every drag-and-drop API I've ever tried, I wanted an easy-to-use system for Avalonia. The interaction IDs are especially important here.

Some tree <-> tree transfer work, multiple item transfers, drag visuals, bug cleanup, and refactorings were AI-assisted to help bring this project to the finish line.

## Install

```
dotnet add package MonacoLabs.DragDrop
```

Requires .NET 10 and Avalonia 12.

## Quick start

Reorder a list, or move items between two lists that share an interaction id:

```xml
<UserControl xmlns:dd="https://github.com/stevemonaco/MonacoLabs.DragDrop" ...>
    <ListBox ItemsSource="{Binding Inbox}" SelectionMode="Multiple">
        <dd:DragDrop.DragOperation>
            <dd:CollectionDragOperation AllowedEffects="Move,Copy" InteractionIds="message" SourceCollection="{Binding Inbox}" />
        </dd:DragDrop.DragOperation>
        <dd:DragDrop.DropOperation>
            <dd:CollectionDropOperation InteractionIds="message" TargetCollection="{Binding Inbox}" />
        </dd:DragDrop.DropOperation>
    </ListBox>
</UserControl>
```

- `SourceCollection` is where the drag side removes a moved item from. `TargetCollection` is where the drop side inserts it.
- To handle drops in your view model instead, leave `TargetCollection` unset and bind `PayloadCommand`. It receives a `DropEventArgs` with the payloads, the insertion location and the effect.
- For a single slot, use `ItemDragOperation` with a TwoWay `Payload` binding and `ItemDropOperation` with `PayloadTarget`. A move clears the source slot, or swaps the two values with `SwapOnDrop`.

![A single-value slot](Assets/singleitem.gif)

## Validation

Subclass a drop operation and override `CanDrop`. A rejected drag shows the target in its error state with your message:

```csharp
public class TeamDropOperation : CollectionDropOperation
{
    protected override bool CanDrop(DragEventArgs e)
    {
        if (!TryGetPayloads<WorkerViewModel>(e, out var workers))
            return false;

        if (workers.Any(x => x.IsOnLeave))
            return Reject("Someone in the selection is on leave");

        return true;
    }
}
```

On collection and tree targets, `DropLocation` says where the payloads would land (collection, index and parent), and `CanDropAtLocation` runs before `CanDrop` for structural checks.

## Styling

| Pseudo-class | Set on |
|---|---|
| `:dropover` | The drop target and the hovered item container |
| `:droptop`, `:dropbottom`, `:dropinside` | The hovered item container, by where the drop lands |
| `:dragging` | Every dragged item container, or the drag source control |
| `:error` | Drop adorners while the drag is rejected |
| `:nodrop`, `:copy`, `:remove` | The drag preview, by what releasing there would do |

Replace an adorner by setting `DropAdorner` or `ItemDropAdorner` on the drop operation, or restyle `DropHighlightAdorner` and `DropInsertionAdorner` from your own styles.

## Sample app

`DragDropSample` has a page for each scenario: single slots, simple and validated lists, filtered and sorted lists, tree to list, tree to tree, and files and text with other applications.

```powershell
dotnet run --project DragDropSample
```

## Building and contributing

```powershell
dotnet build MonacoLabs.DragDrop.slnx
dotnet test --project MonacoLabs.DragDrop.Tests
dotnet format MonacoLabs.DragDrop.slnx
```

The tests run headless (xUnit v3 with `Avalonia.Headless`), and CI runs them on Windows and Linux. Behavior is specified in [`docs/specs/dragdrop/`](docs/specs/dragdrop/index.md), one spec per feature with each requirement citing its tests; read the relevant spec before changing behavior. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) covers the overall design, [`docs/BACKLOG.md`](docs/BACKLOG.md) lists open work, and new designs start as proposals in [`docs/changes/`](docs/changes/README.md).

## License

[MIT](LICENSE.txt)
