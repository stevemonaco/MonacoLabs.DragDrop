# Collection fixes: reorder selection, `:dragging` on recycled containers, read-only collections

## Why

Three bugs, found by probing the library with headless end-to-end drags on 2026-11-26:

- **A reorder clears the selection.** Dragging `a` below `c` in a `ListBox` of `a,b,c,d` gives `b,c,a,d` with nothing selected. Pressing an unselected item selects it (DD-DRAG), so every single-item reorder ends with the moved item deselected, and a multi-item reorder loses the whole selection. The collection also sees `Add` then `Remove`, because the target inserts (DD-COLLECTION-025) and the source removes (DD-COLLECTION-007). A probe showed `ObservableCollection.Move` keeps the container but **also** deselects the moved item, because Avalonia's selection model handles a move as a removal plus an insertion. So switching to `Move` alone does not fix it.
- **`:dragging` lands on the wrong items in virtualized lists.** With items 0–2 of a 200-item `ListBox` selected and dragged, scrolling during the drag (auto-scroll does this) left `:dragging` on the recycled containers now showing items 58 and 59, and the dragged items lost it. The session sets the pseudo-class on container instances when the drag starts (DD-COLLECTION-005, DD-SESSION-001), and virtualization reuses those instances for other items.
- **Read-only and fixed-size collections are accepted while hovering, then throw on drop.** A `ReadOnlyObservableCollection` target throws "Collection is read-only" and an array target throws "Collection was of a fixed size", both `NotSupportedException` from `Insert`. `CanTransfer` (DD-COLLECTION-048) never checks `IList.IsReadOnly` or `IsFixedSize`. On Win32 the exception becomes a logged `COMException` and `DragFailed`, so the user sees a valid insertion line and then nothing happens. Trees are the most exposed: `TreeDropOperation.TransfersPayload` is always true and its collections come from the tree, which DynamicData-style view models expose as read-only. A read-only *source* has the same problem on the drag side: `RemoveAt` throws in `DropCompleted` after the target already inserted, leaving the item in both places.

## What

**Reorders keep their selection.** When a Move drop lands in the control the drag started from, on a `ListBox` or `TreeView`, the items that were selected and dragged are selected again in their new positions once the drag source has removed the originals. A single-item reorder therefore leaves the moved item selected, as it was after the press. Drops into another control still don't select anything (the decision "The library does not select the dropped items" is narrowed to cross-control drops, see Decisions).

**`:dragging` follows the dragged items, not containers.** While a drag from a collection or tree runs, every container that shows a dragged item has `:dragging`, including containers realized during the drag, and a container recycled to show another item loses it. The drag source tracks the dragged items' display indexes and listens to its items control's container lifecycle (`ContainerPrepared`, `ContainerIndexChanged`, `ContainerClearing`). The pressed container stays as it is: Avalonia keeps the focused container realized.

**Collections that can't change are never changed.**
- Drop side: `CanTransfer` is false when the location's collection is read-only or fixed-size, for flat and tree targets alike. Per DD-DROP-038, the drag is then valid only where `PayloadCommand` can execute, so an app that owns its collections (a DynamicData `SourceList` behind a read-only view) handles the drop in its command, and a target with no command shows the drag as invalid instead of failing on drop.
- Drag side: see the open question on read-only sources.

**What does not change:** removal rules for writable collections (DD-COLLECTION-008, 009, 044), the order of transfer, report and command, and what cross-control drops do with selection.

## Decisions

- **Restore the selection explicitly.** The drag source reselects the dragged items after removing the originals. Reason: Avalonia's selection model deselects moved items on both `Add`+`Remove` and `Move`, so no change to how the collection is mutated keeps it. Rejected: switching to `Move` and relying on Avalonia (the probe disproved it).
- **Only reorders within one control keep their selection.** Reason: the user's selection in that control is the thing they dragged, so keeping it is not a choice about the target's selection; what a different target should select remains an app decision. The collection spec's decision "The library does not select the dropped items" is reworded to "into another control".
- **Track dragged items by display index.** Containers are matched by the index they show, recorded at drag start. Reason: indexes are exact for value payloads that repeat (the sample's Guids), and collections don't change during a drag. Rejected: matching containers by item equality, which marks every equal item.
- **Unchangeable collections fail validation, not the drop.** Reason: a drop that would throw is a drop that would do nothing, which DD-DROP-038 already rejects while hovering. Rejected: catching the exception at drop time (the user already saw a valid insertion line).
- **No message for an unchangeable target.** Reason: the other "would do nothing" rejections have none, and the condition is a developer error (wiring a read-only `TargetCollection` without a command), which the open question's log line covers better than a user-facing message.

## Spec changes

**DD-COLLECTION**
- Added: "When a Move drop lands in the control the drag started from, and that control is a `ListBox` or `TreeView`, the drag operation shall, after removing the originals, select every dragged item that was selected when the drag started."
- Changed **DD-COLLECTION-005**: "…While the drag runs, every container that shows a dragged item shall have `:dragging`, including containers realized during the drag, and a container that stops showing a dragged item shall lose it."
- Changed **DD-COLLECTION-048**: "`CanTransfer` shall be true exactly when `TransfersPayload` is true, there is a location, and the location's collection is neither read-only nor fixed-size…"
- Decisions: reword "The library does not select the dropped items" to cover only drops into another control; add the decisions above.

**DD-TREE**: the same reselection and `:dragging` rules for nodes (DD-TREE-004 references DD-COLLECTION-005); note that a tree location whose collection is read-only doesn't transfer.

**DD-SESSION**
- Changed **DD-SESSION-001**: the session owns `:dragging` on the drag visual; the containers a collection drag names are kept up to date by the drag operation (DD-COLLECTION-005).

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **Read-only and fixed-size targets.**
   - `CollectionDropTests.AReadOnlyTargetCollectionIsInvalidWithoutACommand`
   - `CollectionDropTests.AFixedSizeTargetCollectionIsInvalidWithoutACommand`
   - `CollectionDropTests.AReadOnlyTargetCollectionRunsTheCommandInstead`
   - `TreeDropTests.ANodeWithReadOnlyChildrenIsInvalidWithoutACommand`
2. **Read-only sources**, once the open question is settled.
   - `EndToEndTests.MovingOutOfAReadOnlySourceNeverDuplicates` (or the tests the chosen option implies)
3. **`:dragging` follows items.**
   - `CollectionDragTests.ScrollingDuringADragMovesDraggingWithTheItems`
   - `CollectionDragTests.ADraggedItemRealizedDuringTheDragIsMarkedDragging`
   - `TreeDragTests.ExpandingANodeDuringADragMarksDraggedChildren`
4. **Reorders keep their selection.**
   - `EndToEndTests.ReorderingAnItemKeepsItSelected`
   - `EndToEndTests.ReorderingASelectionKeepsItSelected`
   - `EndToEndTests.MovingToAnotherListSelectsNothingThere`
   - `EndToEndTests.ReorderingANodeKeepsItSelected`
5. **Manual check on the Win32 sample.** Reorder in Simple Collections and keep the selection; drag a multiple selection in a long list while auto-scrolling and watch only the dragged rows dim.
6. **Close.** Specs cite every test, this proposal is deleted.

## Open questions

- **Read-only sources.** A Move out of a read-only or fixed-size source can't remove the original. Options:
  - *Recommended:* the drag source skips removal from collections it can't change and logs a Warning under `DragDrop`, **and** `CanTransfer` is false for a Move whose originals sit in such collections. A library transfer then never duplicates an item; commands still run with `Effect = Move`, so an app that owns its collections completes the move itself.
  - Strip Move from the drag's allowed effects at drag start. Simple and honest, but a source that only allows Move then can't drop anywhere, and tree reordering in DynamicData trees becomes impossible without a subclass.
  - Only skip removal and log. Leaves a duplicate whenever the target transfers.
- **Use `Move` for same-collection reorders?** It doesn't fix the selection, but it keeps containers (focus, animations, no re-templating) and gives observers one notification. It crosses the split where the target inserts and the source removes, so it needs a new report (the drop info would say the move already happened). Recommended: leave it out of this change and add a backlog candidate.
