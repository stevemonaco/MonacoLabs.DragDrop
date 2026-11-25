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
  - CollectionDropTests
  - CollectionRemovalTests
  - AutoScrollStepTests
  - EndToEndTests
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

- **DD-COLLECTION-007** — When a drop completes with effect Move and `PayloadCollection` is set, the drag operation shall remove the original from `PayloadCollection`, whatever kind of target received the drop.
  - Tests: `CollectionRemovalTests.AMoveToAnotherListRemovesThePressedIndex`, `CollectionRemovalTests.AMoveOntoATargetWithoutACollectionRemovesThePressedIndex`, `EndToEndTests.MovingAListItemOntoASingleValueTargetRemovesItFromTheList`
- **DD-COLLECTION-008** — The drag operation shall remove at the pressed index, shifted by one when the drop inserted into the same collection at or before it, when that index holds an item equal to the payload.
  - Tests: `CollectionRemovalTests.AMoveWithinOneListRemovesTheOriginalAfterTheInsertionShiftsIt`, `CollectionRemovalTests.AnAppendWithinOneListRemovesThePressedIndex`, `CollectionRemovalTests.MovingOneOfTwoEqualItemsRemovesThatOne`
- **DD-COLLECTION-009** — When the pressed index is unknown or no longer holds an equal item, the drag operation shall remove the first item equal to the payload that is not the item the drop inserted (the one at the target index, or the last one when the drop appended).
  - Tests: `CollectionRemovalTests.WithoutAPressedIndexTheMoveRemovesThePayloadByEquality`, `CollectionRemovalTests.AStalePressedIndexFallsBackToTheFirstEqualItem`, `CollectionRemovalTests.TheEqualitySearchSkipsTheItemTheDropInserted`, `CollectionRemovalTests.WithoutAPressedIndexAnAppendRemovesTheOriginalNotTheAppendedItem`
- **DD-COLLECTION-010** — When the effect is not Move or `PayloadCollection` is null, the drag operation shall remove nothing.
  - Tests: `CollectionDragTests.OnlyAMoveWithAPayloadCollectionRemovesTheOriginal`
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
- **DD-COLLECTION-023** — The drop operation shall evaluate `CanDropAtLocation` before `CanDrop`, inside the same validation (DD-DROP-007), and reject the drop when either is false, so overrides of `CanDrop` cannot skip the structural check and a message passed to `Reject` by either reaches `ErrorMessage`.
  - Tests: `TreeDropTests.DroppingANodeIntoItselfShowsTheSelfDropMessage`
- ~~**DD-COLLECTION-024**~~ — Removed by api-design: see the decision "No logical-descendant check".
- **DD-COLLECTION-037** — The drop operation shall validate drags over the attached control's own surface like drags over its content.
  - Tests: `CollectionDropTests.ADragOverTheTargetsOwnSurfaceIsAccepted`

### Drop side: dropping

- **DD-COLLECTION-025** — When an in-process payload is dropped and accepted, `TransfersPayload` is true and there is a location, the drop operation shall insert the payload at the location's index, or append it when the index is null, and then call `DropCompleted` with a `CollectionDropInfo` carrying the target collection, the index actually used, the hovered container and the location's parent.
  - Tests: untested
- **DD-COLLECTION-026** — `TransfersPayload` shall be true exactly when `PayloadTarget` is an `IList`, unless a subclass overrides it.
  - Tests: untested
- **DD-COLLECTION-027** — When an in-process payload is dropped and accepted and nothing is transferred but `PayloadCommand` ran, the drop operation shall call `DropCompleted` after the command with a `CollectionDropInfo` whose target collection and index are the location's.
  - Tests: `CollectionDropTests.ACommandOnlyTargetCompletesAMove`, `EndToEndTests.ReorderingThroughACommandRemovesTheOriginal`
- **DD-COLLECTION-038** — When an in-process drop neither transfers nor runs `PayloadCommand` (there is no command, or `CanExecute` is false), the drop operation shall report effect None and not call `DropCompleted`.
  - Tests: `CollectionDropTests.ADropThatNeitherTransfersNorRunsACommandReportsNone`, `CollectionDropTests.ACommandWhoseCanExecuteIsFalseReportsNone`
- **DD-COLLECTION-028** — After an accepted in-process drop, the drop operation shall run `PayloadCommand` with the payload, the hovered offset and the location, whether or not it transferred.
  - Tests: untested
- **DD-COLLECTION-029** — When platform data is dropped and accepted, the drop operation shall run `PayloadCommand` with the platform items, the hovered offset and the location (from `ItemsSource` when there is no `PayloadTarget`), and shall insert nothing.
  - Tests: untested
- **DD-COLLECTION-030** — A Copy drop shall insert the value `CreateCopy` returns (DD-DROP-026), which by default is the reference the source holds; any other effect shall insert the payload.
  - Tests: `CollectionDropTests.ACopyDropInsertsTheCreatedCopy`, `CollectionDropTests.AMoveDropIgnoresCreateCopy`, `EndToEndTests.CopyingWithAnOverriddenCreateCopyLeavesTheSourceUnchanged`
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

## Threading and lifetime

- UI thread only. The auto-scroll `DispatcherTimer` is created on first use, stopped when the drag ends and never disposed; it holds the operation through its callback while running.

## Platform behavior

- No platform-specific behavior beyond DD-DROP.

## Decisions

- **The drag side removes the payload.** The drop side inserts and then calls `DropCompleted`, and the drag operation removes its original. Reason: only the source knows where the payload lives and whether it should go (a Copy source keeps it). Rejected: the drop side removing from `PayloadCollection`, which would couple every target to every source type.
- **Removal checks the index before trusting it.** When a drop completes with Move, the drag side removes the item at the pressed index, shifted by one when the drop inserted into the same collection at or before it, if that index still holds an equal item. Otherwise it removes the first equal item that is not the one the drop just inserted: the one at the drop's target index, or the last item when the drop appended. Reason: the source no longer needs to know what kind of target received the drop, so single-value and command-only targets work, and a wrong index (a command that inserted somewhere else, a collection changed during the drag) degrades to an equality search instead of removing the wrong item. Equality is `Equals`, so boxed value payloads (the sample's Guids) match. Rejected: special-casing each drop-info type in `DropCompleted`, which repeats the bug for the next target kind; reference equality, which can never match a boxed value.
- **Every accepted in-process drop that did something reports back.** A collection or tree target calls `DropCompleted` after it transferred the payload or ran `PayloadCommand`. With no transfer, the `CollectionDropInfo` carries the requested location (collection and index) as the target. Reason: the drag side owns removal, and a target that silently skips the report turns every Move into a Copy. Rejected: leaving the move to the command, which cannot reach the source collection through `DropEventArgs`.
- **A drop that does nothing reports None.** When a collection target neither transfers nor runs its command (DD-COLLECTION-038), it reports effect None and does not call `DropCompleted`. Reason: reporting Move for a drop that went nowhere would let the source remove the item into nothing. Rejected: consulting `CanExecute` during DragOver to show None early, which is a separate behavior change (backlog).
- **The pressed container's item is the payload.** Reason: with multiple selection, `Payload` or `SelectedItem` can differ from the item under the pointer, and the payload must match `PayloadContainerIndex` or the wrong item is removed.
- **One insertion adorner is retargeted.** Reason: styles set on a custom `ItemDropAdorner` instance survive hover changes, and no adorner is created per container.
- **Locations fall back to `ItemsSource`.** Reason: platform drops and command-only targets still need an insertion index to hand to `PayloadCommand`.
- **Structural checks run before `CanDrop`.** See DD-COLLECTION-023. Reason: consumers override `CanDrop` for domain validation and should not have to remember to call the base for structural correctness.
- **No logical-descendant check.** The former DD-COLLECTION-024 rejected drags whose event source was not a logical descendant of the attached control. Probing found that every source that reaches the target's handlers is one (item containers, template parts such as the empty area's `ScrollContentPresenter`), except the attached control itself: Avalonia's `IsLogicalAncestorOf` is false for the control, so a target whose own surface is hit (a `Border` with a background) rejected drags over it and accepted them over its child. Rejected: moving the check into `CanDropAtLocation`, which keeps that bug for a case nobody found.

## Non-goals

- Dragging several selected items at once.
- Cloning payloads automatically on Copy. `CreateCopy` is the hook (DD-COLLECTION-030).
- Horizontal insertion zones for wrap or horizontal panels. `GetDropOffset` is virtual for that.

## Open items

- `CollectionDragInfo.PayloadContainerIndex` and `CollectionDropInfo.TargetIndex` have copy-paste doc comments that describe the wrong collection. Logged in the backlog.
- The collection drop operation replaces the base DragEnter, DragOver and Drop handlers, so `OnDragEnter` and `OnDragOver` (DD-DROP) are never called on collection and tree targets, while `OnDrop` is and `OnDragLeave` ends the drag. Logged in the backlog (drop-side redesign).
