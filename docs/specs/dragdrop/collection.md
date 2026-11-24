---
id: DD-COLLECTION
title: Collection operations
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop
sources:
  - MonacoLabs.DragDrop/Operations/CollectionDragOperation.cs
  - MonacoLabs.DragDrop/Operations/CollectionDropOperation.cs
  - MonacoLabs.DragDrop/Operations/CollectionDropOperation.props.cs
  - MonacoLabs.DragDrop/Info/CollectionDragInfo.cs
  - MonacoLabs.DragDrop/Info/CollectionDropInfo.cs
  - MonacoLabs.DragDrop/Info/CollectionDropLocation.cs
  - MonacoLabs.DragDrop/Info/DropTargetOffset.cs
  - MonacoLabs.DragDrop/Internals/CollectionRemoval.cs
  - MonacoLabs.DragDrop/Internals/AutoScroll.cs
types:
  - CollectionDragOperation
  - CollectionDropOperation
  - CollectionDragInfo
  - CollectionDropInfo
  - CollectionDropLocation
  - DropTargetOffset
tests:
  - CollectionDragTests
  - CollectionRemovalTests
  - AutoScrollStepTests
depends:
  - DD-DRAG
  - DD-DROP
  - DD-EFFECTS
  - DD-ADORNERS
---

# Collection operations

## Purpose

`CollectionDragOperation` and `CollectionDropOperation` move items between and within flat item controls (`ListBox`, `ItemsControl`). The drag side drags the pressed item container's item and remembers its collection and index. The drop side tracks the hovered container, computes where the item would land (`CollectionDropLocation`), draws an insertion line (DD-ADORNERS), auto-scrolls, inserts on drop, and tells the drag side, which removes the original. Tree operations (DD-TREE) derive from both and replace the hover zones and locations.

## Requirements

### Drag side

- **DD-COLLECTION-001** — The collection drag operation shall handle pointer events with tunneling routing, including handled ones, so presses reach it before `ListBoxItem` handles them for selection.
  - Tests: `CollectionDragTests.PressesOnAListBoxItemReachTheOperation`
- **DD-COLLECTION-002** — When attached to an `ItemsControl`, the drag operation shall track only presses inside an item container of that control.
  - Tests: `CollectionDragTests.PressesOutsideAnItemContainerAreNotTracked`
- **DD-COLLECTION-003** — When a press is inside an item container, the dragged payload shall be that container's item, whatever `Payload` and the selection say.
  - Tests: untested
- **DD-COLLECTION-004** — When a press is not inside an item container (the operation is attached to a non-items control), the drag operation shall resolve the payload as DD-DRAG-016 does.
  - Tests: untested
- **DD-COLLECTION-005** — The drag visual (DD-SESSION) shall be the pressed item container when there is one, and the attached control otherwise.
  - Tests: untested
- **DD-COLLECTION-006** — The drag's `CollectionDragInfo` shall carry `PayloadCollection`, the pressed container, and the container's index in the attached control.
  - Tests: `CollectionDragTests.TheDragInfoCarriesTheCollectionTheContainerAndItsIndex`

### Drag-side completion

- **DD-COLLECTION-007** — When a drop completes with effect Move, `PayloadCollection` is set and the drop info is a `CollectionDropInfo` with a target collection, the drag operation shall remove the original from `PayloadCollection` at the pressed container's index.
  - Tests: `CollectionRemovalTests.AMoveToAnotherListRemovesThePressedIndex`
- **DD-COLLECTION-008** — When the target collection is the payload collection and the pressed index is at or after the target index, the drag operation shall remove at the pressed index plus one, because the insertion shifted the original down.
  - Tests: `CollectionRemovalTests.AMoveWithinOneListRemovesTheOriginalAfterTheInsertionShiftsIt`, `CollectionRemovalTests.AnAppendWithinOneListRemovesThePressedIndex`
- **DD-COLLECTION-009** — When the drag has no pressed index, the drag operation shall remove the payload from `PayloadCollection` by equality.
  - Tests: `CollectionRemovalTests.WithoutAPressedIndexTheMoveRemovesThePayloadByEquality`
- **DD-COLLECTION-010** — When the effect is not Move, `PayloadCollection` is null, or the drop info is not a `CollectionDropInfo`, the drag operation shall remove nothing.
  - Tests: untested
- **DD-COLLECTION-011** — When the drag is dropped outside any target with `RemoveOnDropOutside` (DD-SESSION), the drag operation shall remove the item at the pressed index if that index still holds an equal item, and otherwise remove the payload by equality.
  - Tests: `CollectionRemovalTests.ADropOutsideRemovesThePressedIndexWhenItHoldsThePayload`, `CollectionRemovalTests.ADropOutsideRemovesByEqualityWhenThePressedIndexDoesNotHoldThePayload`

### Drop side: hover

- **DD-COLLECTION-012** — The collection drop operation shall allow self-drops, so items can be reordered within one control.
  - Tests: untested
- **DD-COLLECTION-013** — While a drag meant for the target is over it, the drop operation shall find the hovered item container: the topmost logical ancestor of the event source below the attached control, or none when that is part of the attached control's own template.
  - Tests: untested
- **DD-COLLECTION-014** — While an item container is hovered, the drop operation shall set the offset to `BeforeTarget` when the pointer is in the top half of the container and `AfterTarget` otherwise.
  - Tests: untested
- **DD-COLLECTION-015** — While no item container is hovered, the offset shall be `AfterTarget`.
  - Tests: untested
- **DD-COLLECTION-016** — While an item container is hovered, the drop operation shall set `:dropover` on it, plus `:droptop`, `:dropbottom` or `:dropinside` for the `BeforeTarget`, `AfterTarget` or `OnTarget` offset, only while the drop is valid.
  - Tests: untested
- **DD-COLLECTION-017** — When the hovered container changes or the drag ends, the drop operation shall clear those pseudo-classes from the previous container.
  - Tests: untested
- **DD-COLLECTION-018** — While an item container is hovered, the drop operation shall retarget its single `ItemDropAdorner` to the container's insertion target and update it with the offset and validity.
  - Tests: untested
- **DD-COLLECTION-019** — When attached with no `ItemDropAdorner`, the drop operation shall create a `DropInsertionAdorner`.
  - Tests: untested
- **DD-COLLECTION-020** — The drop operation shall run hover, location, validation, adorners, pseudo-classes and auto-scroll on both DragEnter and DragOver, and mark those events handled when the drop is valid.
  - Tests: untested

### Drop side: location and validation

- **DD-COLLECTION-021** — The drop location's collection shall be `PayloadTarget` when it is an `IList`, and otherwise the attached control's `ItemsSource` when that is an `IList`; with neither, there shall be no location.
  - Tests: untested
- **DD-COLLECTION-022** — While an item container at index *i* is hovered, the location's index shall be *i* for `BeforeTarget` and *i* + 1 for `AfterTarget`; while none is hovered, the index shall be null, meaning append.
  - Tests: untested
- **DD-COLLECTION-023** — The drop operation shall evaluate `CanDropAtLocation` before `CanDrop`, and reject the drop when either is false, so overrides of `CanDrop` cannot skip the structural check.
  - Tests: untested
- **DD-COLLECTION-024** — When `CanDrop` is not overridden, the drop operation shall reject drags whose event source is not a logical descendant of the attached control, and otherwise accept them as DD-DROP-009 does.
  - Tests: untested

### Drop side: dropping

- **DD-COLLECTION-025** — When an in-process payload is dropped and accepted, `TransfersPayload` is true and there is a location, the drop operation shall insert the payload at the location's index, or append it when the index is null, and then call `DropCompleted` with a `CollectionDropInfo` carrying the target collection, the index actually used, the hovered container and the location's parent.
  - Tests: untested
- **DD-COLLECTION-026** — `TransfersPayload` shall be true exactly when `PayloadTarget` is an `IList`, unless a subclass overrides it.
  - Tests: untested
- **DD-COLLECTION-027** — When an in-process payload is dropped and accepted but nothing is transferred, the drop operation shall not call `DropCompleted`.
  - Tests: untested
- **DD-COLLECTION-028** — After an accepted in-process drop, the drop operation shall run `PayloadCommand` with the payload, the hovered offset and the location, whether or not it transferred.
  - Tests: untested
- **DD-COLLECTION-029** — When platform data is dropped and accepted, the drop operation shall run `PayloadCommand` with the platform items, the hovered offset and the location (from `ItemsSource` when there is no `PayloadTarget`), and shall insert nothing.
  - Tests: untested
- **DD-COLLECTION-030** — A Copy drop shall insert the same object reference the source holds, not a copy of it.
  - Tests: untested
- **DD-COLLECTION-031** — After a drop, or when a drag leaves or stops being meant for the target, the drop operation shall clear the hovered container, its pseudo-classes, the location, auto-scrolling, `:dropover` and both adorners.
  - Tests: untested

### Auto-scroll

- **DD-COLLECTION-032** — While a drag meant for the target is within `AutoScrollMargin` of an edge of the scroll viewer (the attached control itself, or its first descendant `ScrollViewer`), the drop operation shall scroll that viewer towards the edge on a 16 ms timer.
  - Tests: untested
- **DD-COLLECTION-033** — The scroll step shall grow linearly from 0 at the margin's inner edge to 14 DIPs per tick at the viewer's edge, per axis.
  - Tests: `AutoScrollStepTests.TheStepGrowsLinearlyFromTheMarginsInnerEdgeToTheViewersEdge`
- **DD-COLLECTION-034** — The effective margin shall be at most a third of the viewer's extent on that axis, so small viewers keep a middle zone that does not scroll.
  - Tests: `AutoScrollStepTests.TheMarginIsAtMostAThirdOfTheExtent`
- **DD-COLLECTION-035** — When `AutoScrollMargin` is zero or less, the drop operation shall not auto-scroll.
  - Tests: `AutoScrollStepTests.AMarginOfZeroOrLessNeverScrolls`

### Defaults

- **DD-COLLECTION-036** (inherited) — When `AutoScrollMargin` is not set, the margin shall be 28 DIPs.
  - Tests: untested

## Invariants

- At most one item container carries the drop pseudo-classes from a given drop operation at a time.
- The auto-scroll timer runs only while a drag meant for the target is over it.
- `DropCompleted` is called only after the payload has been inserted.

## Edge cases

- Dropping an item before or after itself inserts a duplicate next to it and removes the original: the list is unchanged (DD-COLLECTION-008).
- Indexes come from the item control's containers. When `PayloadTarget` is not the collection shown (for example a filtered or sorted view over it), insertion and removal indexes refer to different orders.
- With multiple selection, only the pressed item is dragged and moved; the rest of the selection stays.
- The scroll viewer is looked up on the first drag and cached for the operation's lifetime, so a re-templated control keeps scrolling the old viewer.
- A `CanDrop` override that does not call the base loses DD-COLLECTION-024 (the sample's `WorkerDropOperation` does).

## Threading and lifetime

- UI thread only. The auto-scroll `DispatcherTimer` is created on first use, stopped when the drag ends and never disposed; it holds the operation through its callback while running.

## Platform behavior

- No platform-specific behavior beyond DD-DROP.

## Decisions

- **The drag side removes the payload.** The drop side inserts and then calls `DropCompleted`, and the drag operation removes its original. Reason: only the source knows where the payload lives and whether it should go (a Copy source keeps it). Rejected: the drop side removing from `PayloadCollection`, which would couple every target to every source type.
- **The pressed container's item is the payload.** Reason: with multiple selection, `Payload` or `SelectedItem` can differ from the item under the pointer, and the payload must match `PayloadContainerIndex` or the wrong item is removed.
- **One insertion adorner is retargeted.** Reason: styles set on a custom `ItemDropAdorner` instance survive hover changes, and no adorner is created per container.
- **Locations fall back to `ItemsSource`.** Reason: platform drops and command-only targets still need an insertion index to hand to `PayloadCommand`.
- **Structural checks run before `CanDrop`.** See DD-COLLECTION-023. Reason: consumers override `CanDrop` for domain validation and should not have to remember to call the base for structural correctness.

## Non-goals

- Dragging several selected items at once.
- Cloning payloads on Copy (DD-COLLECTION-030).
- Horizontal insertion zones for wrap or horizontal panels. `GetDropOffset` is virtual for that.

## Open items

- A Move onto a single-value target (`ItemDropOperation`) removes nothing from the source list (DD-COLLECTION-010). Addressed by [changes/correctness-fixes.md](../../changes/correctness-fixes.md).
- A Move onto a collection target that does not transfer (no `PayloadTarget`, only `PayloadCommand`) never calls `DropCompleted` (DD-COLLECTION-027), so the source keeps the item. Addressed by [changes/correctness-fixes.md](../../changes/correctness-fixes.md).
- DD-COLLECTION-024 is a structural check in `CanDrop`, contradicting the "Structural checks run before CanDrop" decision. Addressed by [changes/api-design.md](../../changes/api-design.md).
- Copy drops insert the source's reference with no way to clone it (DD-COLLECTION-030). Addressed by [changes/api-design.md](../../changes/api-design.md).
- `CollectionDragInfo.PayloadContainerIndex` and `CollectionDropInfo.TargetIndex` have copy-paste doc comments that describe the wrong collection. Logged in the backlog.
- The collection drop operation replaces the base DragEnter, DragOver and Drop handlers, so `OnDragEnter` and `OnDragOver` (DD-DROP) are never called on collection and tree targets, while `OnDrop` is and `OnDragLeave` ends the drag. Logged in the backlog (drop-side redesign).
