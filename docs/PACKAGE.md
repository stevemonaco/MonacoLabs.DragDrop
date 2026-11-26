# MonacoLabs.DragDrop

MVVM-friendly drag and drop for Avalonia 12. You attach drag and drop operations to controls in XAML and bind them to your view model. The library starts the drag, shows a preview and drop feedback, moves or copies the payloads, and removes moved items from their source.

- Single values (`ItemDragOperation` / `ItemDropOperation`), flat lists (`CollectionDragOperation` / `CollectionDropOperation`) and trees (`TreeDragOperation` / `TreeDropOperation`)
- Multi-selection drags, insertion lines, drag previews, auto-scroll and hover-expand in trees
- Interaction ids act as typed channels, so each target accepts only the drags meant for it
- Files and text from and to other applications (`platform:files`, `platform:text`)
- Custom validation by overriding `CanDrop`, with an error message shown on the target

![Dragging between lists](https://raw.githubusercontent.com/stevemonaco/MonacoLabs.DragDrop/main/Assets/listbox.gif)

## Install

```
dotnet add package MonacoLabs.DragDrop
```

Requires .NET 10 and Avalonia 12.

## Use

Move items between two lists, or reorder them within one:

```xml
<UserControl xmlns:dd="https://github.com/stevemonaco/MonacoLabs.DragDrop" ...>
    <ListBox ItemsSource="{Binding Inbox}" SelectionMode="Multiple">
        <dd:DragDrop.DragOperation>
            <dd:CollectionDragOperation InteractionIds="message" SourceCollection="{Binding Inbox}" />
        </dd:DragDrop.DragOperation>
        <dd:DragDrop.DropOperation>
            <dd:CollectionDropOperation InteractionIds="message" TargetCollection="{Binding Inbox}" />
        </dd:DragDrop.DropOperation>
    </ListBox>
</UserControl>
```

Give a second `ListBox` the same operations, bound to another collection, and items move between the two. Hold Ctrl (Option on macOS) to copy instead, when the drag operation's `AllowedEffects` includes `Copy`. To handle drops in your view model instead, leave `TargetCollection` unset and bind `PayloadCommand`: it receives the payloads and the insertion index.

## Learn more

The [repository](https://github.com/stevemonaco/MonacoLabs.DragDrop) has a sample app that covers every operation, and the behavior specs.
