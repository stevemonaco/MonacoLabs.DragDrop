# MonacoLabs.DragDrop specs

One spec per feature; format and usage in [../README.md](../README.md). Find a type's spec by its `types` list, or search this folder for the type name.

## Foundation

| Spec | Id | Owns |
|---|---|---|
| [Wiring and attached properties](wiring.md) | DD-WIRING | `DragDrop`, `IDragOperation`, `IDropOperation` |
| [Interaction ids and the data transfer](transfer.md) | DD-TRANSFER | `DragInfo`, `DropInfo`, `DropEventArgs` |
| [Drag source](drag-source.md) | DD-DRAG | `DragOperationBase` |
| [Drop target](drop-target.md) | DD-DROP | `DropOperationBase` |
| [Drop effects and modifier keys](effects.md) | DD-EFFECTS | — |
| [Platform files and text](platform-data.md) | DD-PLATFORM | `PlatformInteractionIds` |

## Operations

| Spec | Id | Owns |
|---|---|---|
| [Single-value operations](item.md) | DD-ITEM | `ItemDragOperation`, `ItemDropOperation` |
| [Collection operations](collection.md) | DD-COLLECTION | `CollectionDragOperation`, `CollectionDropOperation`, `CollectionDragInfo`, `CollectionDragItem`, `DraggedItems`, `CollectionDropInfo`, `CollectionDropLocation`, `DropTargetOffset` |
| [Tree operations](tree.md) | DD-TREE | `TreeDragOperation`, `TreeDropOperation` |

## Visuals

| Spec | Id | Owns |
|---|---|---|
| [Drag session, preview and drops outside targets](drag-session.md) | DD-SESSION | `DragPreviewAdorner` |
| [Drop adorners](adorners.md) | DD-ADORNERS | `DropAdornerBase`, `DropHighlightAdorner`, `DropInsertionAdorner`, `DropAdornerKind` |
