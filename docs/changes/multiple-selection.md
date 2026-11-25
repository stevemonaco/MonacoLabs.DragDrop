# Drag and drop multiple selected items

## Why

A drag carries exactly one payload. With multiple selection, only the pressed item is dragged and moved and the rest of the selection stays (DD-COLLECTION Edge cases, DD-TRANSFER Invariants); dragging several items is a non-goal of both specs and a backlog candidate. Users expect to select several workers, files or tree nodes and move them in one gesture, as in Explorer and most list UIs. The payload model (`DragInfo.Payload`, `CollectionDragInfo.PayloadContainerIndex`, one `DataTransfer` item) and the protected drag hooks (`ResolvePayload`, `DoDragDrop`, `CreateDataTransferItem`, `OnDroppedOutside`) all assume one item, so changing them is an API break that has to land before the 1.0 freeze. It builds on the drop side's transfer step (DD-DROP, "Transfer is a step, not a handler"), which is where several payloads get inserted.

## What

**What a drag carries.** When the pressed item is selected and its control has more than one selected item, a collection or tree drag carries every selected item; otherwise it carries the pressed item, as today. A press on an unselected item still selects it in the same gesture (DD-DRAG-009), so with a plain click it drags alone. `CollectionDragOperation.DragsSelection` (default true) turns this off. Single-value sources (`ItemDragOperation`) are unchanged.

- **Order.** The payloads keep the control's order: index order in a flat list, top-to-bottom order in a tree. Not selection order, so a moved block keeps its arrangement.
- **Flat lists** read the selection from the control's selection model (indexes and items), so selected items scrolled out of view are dragged too.
- **Trees** drag the selected nodes whose containers are realized, and drop any selected node whose ancestor is also selected, since it moves with that ancestor.

**The payload model.**

- `DragInfo.Payloads` holds every payload in order; `DragInfo.Payload` is the first. A single-item drag has one entry.
- `CollectionDragInfo.Items` replaces `PayloadCollection`, `PayloadContainer` and `PayloadContainerIndex` with one `CollectionDragItem` per payload: the item, its source collection, its index in it, and its container when realized. Tree items each carry their own parent's collection. `CollectionDragInfo.PressedContainer` keeps the container under the pointer.
- The `DataTransfer` holds one item per payload, each carrying that payload under every in-process id; the `DragInfo` rides on the first. Platform files go one per item, so Explorer receives every file. Platform text is carried once: `ResolvePlatformText` receives the payload list and by default joins their texts with newlines.
- `TryGetPayload<T>` keeps returning the first payload. New `TryGetPayloads<T>` returns all of them, and succeeds only when every payload is a `T`.

**Drop targets.**

- **Collection and tree targets** insert every payload at the location, consecutively and in order, creating a copy of each on a Copy drop. `CollectionDropInfo` reports the first index and the count. `DropEventArgs.Items` holds every payload (or copy).
- **Tree targets** refuse a location inside the subtree of any dragged node.
- **Single-value targets** are not meant for a drag with more than one payload: no adorner, effect None. A slot cannot hold several values. Subclasses can opt in through a protected `AcceptsMultiplePayloads`.
- `CanDrop` overrides that check one payload with `TryGetPayload<T>` keep working, but see only the first; overrides that must check every payload use `TryGetPayloads<T>`.

**Removal.** On a Move, the drag side removes every original. For each, it starts from its recorded index, shifted by the inserted count when the drop inserted into the same collection at or before it, checks that the index still holds an equal item, and otherwise falls back to the first equal item outside the inserted range (the DD-COLLECTION removal rule, generalized). It removes in descending index order within each collection, so earlier removals do not shift later ones. A drop outside any target with `RemoveOnDropOutside` removes them all.

**Visuals.** `:dragging` goes on every dragged container that is realized. The preview stays a snapshot of the pressed container and, for several payloads, shows their count in a badge and gets a `:multiple` pseudo-class.

**Protected API.** `ResolvePayload` becomes `ResolvePayloads`, returning the list; `ResolvePlatformText` takes the payload list; `DoDragDrop`, `CreateDataTransferItem` and `OnDroppedOutside` take the `DragInfo` and read `Payloads` from it, with `CreateDataTransferItem` called once per payload.

**What does not change:** single-value drags and drops, interaction ids and targeting, effect resolution, hover zones and locations, and selection itself. The library does not select the dropped items in the target or clear the source's selection.

## Decisions

- **Drag the selection only when the pressed item is in it.** Reason: pressing an unselected item already makes it the selection (DD-DRAG-009), and dragging a selection the pointer is not on would surprise. Matches Explorer.
- **Keep the control's order.** Reason: a moved block should look the same in its new place; selection order depends on click history. Rejected: selection order; the pressed item first.
- **One `DataTransfer` item per payload.** Reason: it is how Avalonia and the platforms represent multi-item drags, so several files reach other applications as several files. Rejected: one item holding a list, which other applications cannot read and which breaks `TryGetPayload<T>` for every existing override.
- **`Payload` stays, as the first payload.** Reason: single-item code and `CanDrop` overrides keep working unchanged. Rejected: removing it, which breaks every override for the common single-item case.
- **Single-value targets refuse several payloads.** Reason: assigning one of them would lose the rest, and on a Move the source would remove them all. Rejected: assigning the first; a validation message (the drag is not meant for a slot at all).
- **Selected descendants of selected nodes are not dragged separately.** Reason: they move with their ancestor; dragging them too would insert them twice and detach them from the moved subtree.
- **Flat lists drag unrealized selected items; trees drag only realized ones.** Reason: a flat list's selection model gives indexes without containers, but a tree's selection gives only items, and a node's collection is known only through its container. A selected node under a collapsed ancestor is not visible to the user either.
- **The library does not select the dropped items.** Reason: what the target's selection should be after a drop (the dropped items, unchanged, or cleared) is an app decision, and apps can do it from `PayloadCommand` or `DropCompleted`. Rejected: selecting the dropped items in the target.
- **`ResolvePlatformText` receives the payload list.** It joins their texts with newlines by default. Reason: the separator, and whether to offer text for several payloads at all, depends on what the receiving application expects. Rejected: a fixed newline separator.

## Spec changes

**DD-TRANSFER**
- Changed **DD-TRANSFER-004**: "For each payload and each non-platform id in the drag's `InteractionIds`, the data transfer shall hold the payload under an in-process format named by that id, in that payload's item."
- Changed **DD-TRANSFER-006**: "The data transfer shall hold one item per payload, in payload order, with the drag's `DragInfo` on the first."
- Changed **DD-TRANSFER-008**: `DragInfo` carries `Payloads` in order, and `Payload` is the first.
- Added: "`TryGetPayloads<T>` shall succeed when every payload is a `T`, returning them in order." / "`DropEventArgs.Items` shall hold every payload of an in-process drop, or its copy on a Copy drop, in order."
- Invariants: "A drag carries one or more payloads." Non-goals: remove "Dragging several items at once". Decisions: add "One `DataTransfer` item per payload" and "`Payload` stays, as the first payload".

**DD-DRAG**
- Changed **DD-DRAG-016**: the drag source resolves its payloads through `ResolvePayloads`; for the base, `Payload` or the `DataContext`, as one payload.
- Changed **DD-DRAG-018** and **019**: platform text is what `ResolvePlatformText` returns for the payload list, by default their texts joined by newlines; platform files are one per payload.
- Changed **DD-DRAG-021**: `OnDroppedOutside` receives the `DragInfo`.

**DD-COLLECTION**
- Added: "When the pressed item is selected, `DragsSelection` is true and the attached control has more than one selected item, the drag shall carry every selected item, in index order." / "Otherwise the drag shall carry the pressed item." / "Selected items without a realized container shall be dragged, with their index from the selection."
- Changed **DD-COLLECTION-003**, **006**: `CollectionDragInfo.Items` and `PressedContainer` replace the single collection, container and index.
- Changed **DD-COLLECTION-005**: `:dragging` on every realized dragged container; the preview snapshots the pressed one.
- Changed **DD-COLLECTION-007** to **009** and **011**: removal per original, as described above.
- Changed **DD-COLLECTION-025**: insert every payload consecutively at the location; `CollectionDropInfo` carries the first index and the count.
- Edge cases: replace "only the pressed item is dragged". Non-goals: remove "Dragging several selected items at once". Decisions: add "Drag the selection only when the pressed item is in it", "Keep the control's order", "Flat lists drag unrealized selected items; trees drag only realized ones".

**DD-TREE**
- Added: "A tree drag of several nodes shall carry the selected nodes with realized containers, in top-to-bottom order, leaving out any node whose ancestor is also selected, each with its own parent's collection and index."
- Changed **DD-TREE-015**: reject a location inside the subtree of any dragged node.
- Decisions: add "Selected descendants of selected nodes are not dragged separately".

**DD-ITEM**
- Added: "`ItemDropOperation` shall not be meant for a drag with more than one payload."
- Decisions: add "Single-value targets refuse several payloads".

**DD-SESSION**
- Added: "When a drag carries more than one payload, the preview shall have the `:multiple` pseudo-class and show the payload count."

**DD-ADORNERS** (preview badge look), ARCHITECTURE and CLAUDE.md: describe multi-item drags.

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **Payload model.** `DragInfo.Payloads`, `CollectionDragItem`, `CollectionDragInfo.Items`, one `DataTransfer` item per payload, `TryGetPayloads<T>`, and the protected hook signatures. Single-item behavior unchanged.
   - `InteractionIdTests` / `EndToEndTests`: the data layout for one and several payloads.
   - `DropTargetTests.TryGetPayloadsSucceedsOnlyWhenEveryPayloadIsOfTheType`
2. **Removal of several originals.** Generalize `Internals/CollectionRemoval`.
   - `CollectionRemovalTests`: several originals in another list; in the same list before, around and after the insertion; stale indexes; duplicates of equal values; drop outside.
3. **Flat list drags.**
   - `CollectionDragTests.DraggingASelectedItemCarriesTheWholeSelectionInIndexOrder`
   - `CollectionDragTests.DraggingAnUnselectedItemCarriesOnlyIt`
   - `CollectionDragTests.DragsSelectionFalseCarriesOnlyThePressedItem`
   - `CollectionDragTests.SelectedItemsScrolledOutOfViewAreCarried`
   - `CollectionDragTests.EveryRealizedDraggedContainerIsDragging`
4. **Flat list drops.**
   - `CollectionDropTests.SeveralPayloadsAreInsertedTogetherInOrder`
   - `CollectionDropTests.ACopyDropCopiesEachPayload`
   - `EndToEndTests.MovingASelectionWithinOneListKeepsItsOrder`
   - `EndToEndTests.MovingASelectionToAnotherListRemovesEveryOriginal`
   - `ItemDropTests.ASingleValueTargetRefusesSeveralPayloads`
5. **Trees.**
   - `TreeDragTests.DraggingSelectedNodesCarriesThemTopToBottomWithTheirOwnCollections`
   - `TreeDragTests.ASelectedNodeUnderASelectedAncestorIsNotCarried`
   - `TreeDropTests.ALocationInsideAnyDraggedNodeIsRefused`
   - `EndToEndTests.MovingNodesFromSeveralParentsRemovesEachFromItsParent`
6. **Platform data and preview.**
   - `PlatformDropTests` / `DragGestureTests`: several files as several items; joined text.
   - `DragSessionTests.APreviewOfSeveralPayloadsShowsTheirCount`
7. **Sample.** Turn on extended selection on the List, Tree and Desktop pages; the Desktop page's file list drags several files.
8. **Manual checks on the Win32 sample.** Move a selection within and between lists and trees; Ctrl-copy a selection; drag several files from the Desktop page into Explorer (copied, not moved) and several snippets into Notepad.
9. **Close.** Every changed and added requirement cites its tests, this proposal is deleted, and the backlog candidate "Dragging several selected items as one payload" is removed.

## Open questions

- None.
