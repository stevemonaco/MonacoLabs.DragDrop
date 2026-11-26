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
  - MonacoLabs.DragDrop/Info/CollectionDragItem.cs
  - MonacoLabs.DragDrop/Operations/DraggedItems.cs
  - MonacoLabs.DragDrop/Operations/ItemOrder.cs
  - MonacoLabs.DragDrop/Info/CollectionDropInfo.cs
  - MonacoLabs.DragDrop/Info/CollectionDropLocation.cs
  - MonacoLabs.DragDrop/Info/DropTargetOffset.cs
  - MonacoLabs.DragDrop/Internals/CollectionRemoval.cs
  - MonacoLabs.DragDrop/Internals/AutoScroll.cs
  - MonacoLabs.DragDrop/Internals/DisplayIndexMap.cs
types:
  - CollectionDragOperation
  - CollectionDropOperation
  - CollectionDragInfo
  - CollectionDragItem
  - DraggedItems
  - ItemOrder
  - CollectionDropInfo
  - CollectionDropLocation
  - DropTargetOffset
tests:
  - CollectionDragTests
  - CollectionDropTests
  - CollectionRemovalTests
  - AutoScrollStepTests
  - DisplayIndexMapTests
  - EndToEndTests
  - AutoScrollTests
  - TreeDropTests
depends:
  - DD-DRAG
  - DD-DROP
  - DD-EFFECTS
  - DD-ADORNERS
---

# Collection operations

## Purpose

`CollectionDragOperation` and `CollectionDropOperation` move items between and within flat item controls (`ListBox`, `ItemsControl`). The drag side drags the pressed item container's item, or the whole selection when the pressed item is part of it, and remembers each item's collection and index. The drop side runs the DD-DROP flow and overrides its hover, feedback, location and transfer steps: it tracks the hovered container, computes where the items would land (`CollectionDropLocation`), draws an insertion line (DD-ADORNERS), auto-scrolls, and inserts on drop. The base then tells the drag side, which removes the originals. Tree operations (DD-TREE) derive from both and replace the hover zones and locations.

## Requirements

### Drag side

- **DD-COLLECTION-001** — The collection drag operation shall handle pointer events with tunneling routing, so presses reach it before `ListBoxItem` handles them for selection.
  - Tests: `CollectionDragTests.PressesOnAListBoxItemReachTheOperation`, `CollectionDragTests.ByDefaultTheSourceTunnelsAndIgnoresHandledPresses`
- **DD-COLLECTION-002** — When attached to an `ItemsControl`, the drag operation shall track only presses inside an item container of that control.
  - Tests: `CollectionDragTests.PressesOutsideAnItemContainerAreNotTracked`
- **DD-COLLECTION-003** — When a press is inside an item container, the dragged payloads shall be items of the attached control (DD-COLLECTION-041, DD-COLLECTION-042), whatever `Payload` says.
  - Tests: `CollectionDragTests.ThePayloadIsThePressedItemWhateverPayloadAndSelectionSay`
- **DD-COLLECTION-041** — When the pressed item is selected, `DraggedItems` is `SelectedItems` and the attached control is a `ListBox` with more than one selected item, the drag shall carry every selected item, in index order.
  - Tests: `CollectionDragTests.DraggingASelectedItemCarriesTheWholeSelectionInIndexOrder`, `EndToEndTests.MovingASelectionWithinOneListKeepsItsOrder`
- **DD-COLLECTION-042** — Otherwise the drag shall carry only the pressed container's item.
  - Tests: `CollectionDragTests.DraggingAnUnselectedItemCarriesOnlyIt`, `CollectionDragTests.DraggedItemsPressedItemCarriesOnlyThePressedItem`, `CollectionDragTests.ThePayloadIsThePressedItemWhateverPayloadAndSelectionSay`
- **DD-COLLECTION-043** — Selected items without a realized container shall be carried, with their index from the selection mapped into their collection (DD-COLLECTION-049) and no container.
  - Tests: `CollectionDragTests.SelectedItemsScrolledOutOfViewAreCarried`, `EndToEndTests.MovingASelectionOutOfAFilteredListRemovesEveryOriginal`
- **DD-COLLECTION-004** — When a press is not inside an item container (the operation is attached to a non-items control), the drag operation shall resolve the payload as DD-DRAG-016 does.
  - Tests: `CollectionDragTests.OnAControlThatIsNotAnItemsControlThePayloadResolvesAsUsual`
- **DD-COLLECTION-005** — The drag visual (DD-SESSION) shall be the pressed item container when there is one, and the attached control otherwise. The realized container of every other dragged item shall also have `:dragging` while the drag runs.
  - Tests: `CollectionDragTests.TheDragVisualIsThePressedContainer`, `CollectionDragTests.OnAControlThatIsNotAnItemsControlTheDragVisualIsTheControl`, `CollectionDragTests.EveryRealizedDraggedContainerIsDragging`
- **DD-COLLECTION-006** — The drag's `CollectionDragInfo` shall carry the pressed container and, per payload and in payload order, a `CollectionDragItem` with the item, its collection (`SourceCollection`), its index in that collection (DD-COLLECTION-049) and its container when realized.
  - Tests: `CollectionDragTests.TheDragInfoCarriesTheItemItsCollectionIndexAndContainer`, `CollectionDragTests.DraggingASelectedItemCarriesTheWholeSelectionInIndexOrder`, `CollectionDragTests.ADragFromAFilteredViewRecordsCollectionIndexes`

### Drag-side completion

- **DD-COLLECTION-007** — When a drop completes with effect Move, the drag operation shall remove every original that has a collection from that collection, whatever kind of target received the drop.
  - Tests: `CollectionRemovalTests.AMoveToAnotherListRemovesThePressedIndex`, `CollectionRemovalTests.AMoveOntoATargetWithoutACollectionRemovesThePressedIndex`, `CollectionRemovalTests.SeveralOriginalsMovedToAnotherListAreAllRemoved`, `CollectionRemovalTests.OriginalsFromSeveralCollectionsAreEachRemovedFromTheirOwn`, `EndToEndTests.MovingAListItemOntoASingleValueTargetRemovesItFromTheList`, `EndToEndTests.MovingASelectionToAnotherListRemovesEveryOriginal`
- **DD-COLLECTION-008** — The drag operation shall remove each original at its recorded index, shifted by the number of items the drop inserted when it inserted them into the same collection at or before that index, when the index holds an item equal to the original that no other original has claimed.
  - Tests: `CollectionRemovalTests.AMoveWithinOneListRemovesTheOriginalAfterTheInsertionShiftsIt`, `CollectionRemovalTests.AnAppendWithinOneListRemovesThePressedIndex`, `CollectionRemovalTests.MovingOneOfTwoEqualItemsRemovesThatOne`, `CollectionRemovalTests.SeveralOriginalsMovedWithinOneListAreRemovedAfterTheInsertionShiftsThem`, `CollectionDropTests.ItemsCanBeReorderedWithinOneList`, `EndToEndTests.MovingASelectionWithinOneListKeepsItsOrder`
- **DD-COLLECTION-009** — When an original's index is unknown or no longer holds an equal unclaimed item, the drag operation shall remove the first unclaimed item equal to it outside the range the drop inserted (the inserted count from the target index, or the last items when the drop appended).
  - Tests: `CollectionRemovalTests.WithoutAPressedIndexTheMoveRemovesThePayloadByEquality`, `CollectionRemovalTests.AStalePressedIndexFallsBackToTheFirstEqualItem`, `CollectionRemovalTests.TheEqualitySearchSkipsTheItemTheDropInserted`, `CollectionRemovalTests.WithoutAPressedIndexAnAppendRemovesTheOriginalNotTheAppendedItem`, `CollectionRemovalTests.StaleIndexesOfSeveralOriginalsFallBackToTheirFirstEqualItems`, `CollectionRemovalTests.EqualOriginalsRemoveOneItemEach`, `CollectionRemovalTests.WithoutIndexesTheEqualitySearchSkipsEveryInsertedItem`
- **DD-COLLECTION-044** — The drag operation shall choose the item to remove for every original before removing any, and remove them from the highest index down within each collection.
  - Tests: `CollectionRemovalTests.SeveralOriginalsMovedWithinOneListAreRemovedAfterTheInsertionShiftsThem`, `CollectionRemovalTests.EqualOriginalsRemoveOneItemEach`
- **DD-COLLECTION-010** — When the effect is not Move, the drag operation shall remove nothing, and it shall never remove an original without a collection.
  - Tests: `CollectionDragTests.OnlyAMoveWithASourceCollectionRemovesTheOriginal`
- **DD-COLLECTION-011** — When the drag is dropped outside any target with `RemoveOnDropOutside` (DD-SESSION), the drag operation shall remove every original: at its recorded index if that index still holds an equal unclaimed item, and otherwise the first equal unclaimed item.
  - Tests: `CollectionRemovalTests.ADropOutsideRemovesThePressedIndexWhenItHoldsThePayload`, `CollectionRemovalTests.ADropOutsideRemovesByEqualityWhenThePressedIndexDoesNotHoldThePayload`, `CollectionRemovalTests.ADropOutsideRemovesEveryOriginal`, `EndToEndTests.DroppingAListItemWhereNothingAcceptsItRemovesItWithRemoveOnDropOutside`

### Drop side: hover

- **DD-COLLECTION-012** — The collection drop operation shall allow self-drops, so items can be reordered within one control.
  - Tests: `CollectionDropTests.ItemsCanBeReorderedWithinOneList`
- **DD-COLLECTION-013** — While a drag meant for the target is over it, the drop operation shall find the hovered item container: the topmost logical ancestor of the event source below the attached control, or none when that is part of the attached control's own template.
  - Tests: `CollectionDropTests.TheHoveredContainerIsTheItemContainerUnderThePointer`
- **DD-COLLECTION-014** — While an item container is hovered, the drop operation shall set the offset to `BeforeTarget` when the pointer is in the top half of the container and `AfterTarget` otherwise.
  - Tests: `CollectionDropTests.TheTopHalfOfAContainerIsBeforeItAndTheBottomHalfAfterIt`
- **DD-COLLECTION-015** — While no item container is hovered, the offset shall be `AfterTarget`.
  - Tests: `CollectionDropTests.WithoutAHoveredContainerTheDropAppendsAfterTheItems`
- **DD-COLLECTION-016** — While an item container is hovered, the drop operation shall set `:dropover` on it, plus `:droptop`, `:dropbottom` or `:dropinside` for the `BeforeTarget`, `AfterTarget` or `OnTarget` offset, only while the drop is valid.
  - Tests: `CollectionDropTests.TheHoveredContainerHasTheDropPseudoClassesOnlyWhileTheDropIsValid`, `TreeDropTests.ANodeWithAListOfChildrenHasBeforeInsideAndAfterZones`
- **DD-COLLECTION-017** — When the hovered container changes or the drag ends, the drop operation shall clear those pseudo-classes from the previous container.
  - Tests: `CollectionDropTests.MovingToAnotherContainerClearsThePreviousOne`, `CollectionDropTests.ADropClearsTheHoverAndTheAdorners`
- **DD-COLLECTION-018** — While an item container is hovered, the drop operation shall retarget its single `ItemDropAdorner` to the container's insertion target and update it with the offset and validity.
  - Tests: `CollectionDropTests.TheInsertionAdornerFollowsTheHoveredContainer`
- **DD-COLLECTION-019** — When attached with no `ItemDropAdorner`, the drop operation shall create a `DropInsertionAdorner`.
  - Tests: `CollectionDropTests.AttachingWithoutAnItemAdornerCreatesAnInsertionAdorner`
- **DD-COLLECTION-020** — The drop operation shall resolve its hover, location and auto-scroll on both DragEnter and DragOver, as steps of the DD-DROP flow.
  - Tests: `CollectionDropTests.EnterAndOverBothRunTheHoverAndAreHandledOnlyWhenValid`, `AutoScrollTests.HoveringNearAnEdgeScrollsTheListTowardsIt`

### Drop side: location and validation

- **DD-COLLECTION-021** — The drop location's collection shall be `TargetCollection` when it is set, and otherwise the attached control's `ItemsSource` when that is an `IList`; with neither, there shall be no location.
  - Tests: `CollectionDropTests.TheLocationIsInTheTargetCollection`, `CollectionDropTests.WithoutATargetCollectionNothingIsInsertedAndTheLocationIsInTheItemsSource`, `CollectionDropTests.WithoutAnyListThereIsNoLocation`
- **DD-COLLECTION-022** — While an item container at index *i* is hovered, the location's index shall be the mapping (DD-COLLECTION-049) of *i* for `BeforeTarget` and of *i* + 1 for `AfterTarget`; while none is hovered, the index shall be null, meaning append.
  - Tests: `CollectionDropTests.TheLocationIndexIsTheContainersIndexOrTheNextOne`, `CollectionDropTests.WithoutAHoveredContainerTheDropAppendsAfterTheItems`, `CollectionDropTests.ADropOnAFilteredViewInsertsBeforeTheShownItemInTheCollection`, `CollectionDropTests.ADropAfterTheLastShownItemInsertsAfterItInTheCollection`
- **DD-COLLECTION-023** — The drop operation shall evaluate `CanDropAtLocation` before `CanDrop`, inside the same validation (DD-DROP-007), and reject the drop when either is false, so overrides of `CanDrop` cannot skip the structural check and a message passed to `Reject` by either reaches `ErrorMessage`.
  - Tests: `TreeDropTests.DroppingANodeIntoItselfShowsTheSelfDropMessage`, `CollectionDropTests.TheStructuralCheckRunsFirstAndItsMessageWins`
- ~~**DD-COLLECTION-024**~~ — Removed by api-design: see the decision "No logical-descendant check".
- **DD-COLLECTION-037** — The drop operation shall validate drags over the attached control's own surface like drags over its content.
  - Tests: `CollectionDropTests.ADragOverTheTargetsOwnSurfaceIsAccepted`

### Drop side: dropping

- **DD-COLLECTION-025** — When an in-process drag is dropped and accepted, `TransfersPayload` is true and there is a location, the drop operation's transfer step shall insert every payload, consecutively and in order, at the location's index, or append them when the index is null, and report (DD-DROP-015) a `CollectionDropInfo` carrying the target collection, the index the first payload actually landed at, the number of payloads (`TargetCount`), the hovered container and the location's parent.
  - Tests: `CollectionDropTests.AnAcceptedDropInsertsThenCompletesWithWhereItLanded`, `CollectionDropTests.SeveralPayloadsAreInsertedTogetherInOrder`
- **DD-COLLECTION-026** — `TransfersPayload` shall be true exactly when `TargetCollection` is set, unless a subclass overrides it.
  - Tests: `CollectionDropTests.AnAcceptedDropInsertsThenCompletesWithWhereItLanded`, `CollectionDropTests.WithoutATargetCollectionNothingIsInsertedAndTheLocationIsInTheItemsSource`, `CollectionDropTests.ASubclassCanTransferWithoutATargetCollection`
- **DD-COLLECTION-048** — `CanTransfer` (DD-DROP-038) shall be true exactly when `TransfersPayload` is true and there is a location, so a target that doesn't transfer accepts a drag only where its `PayloadCommand` can execute the hovered offset and location.
  - Tests: `CollectionDropTests.ACommandOnlyListShowsNoneWhereItsCommandCannotExecuteTheLocation`, `CollectionDropTests.ADropThatNeitherTransfersNorRunsACommandShowsAndReportsNone`
- ~~**DD-COLLECTION-027**~~ — Removed by drop-side-redesign: completing after a command-only drop is DD-DROP-029, for every target.
- **DD-COLLECTION-040** — When an accepted in-process drop transfers nothing but `PayloadCommand` ran, the `DropInfo` passed to `DropCompleted` (DD-DROP-029) shall be a `CollectionDropInfo` whose target collection and index are the location's, and whose count is the number of payloads.
  - Tests: `CollectionDropTests.ACommandOnlyTargetCompletesAMove`, `EndToEndTests.ReorderingThroughACommandRemovesTheOriginal`
- ~~**DD-COLLECTION-038**~~ — Removed by drop-side-redesign: reporting None for a drop that does nothing is DD-DROP-029, for every target.
- **DD-COLLECTION-028** — After an accepted in-process drop, the drop operation shall run `PayloadCommand` with the payloads, the hovered offset and the location, whether or not it transferred.
  - Tests: `CollectionDropTests.ATransferringDropAlsoRunsTheCommandWithThePayloadOffsetAndLocation`, `CollectionDropTests.ACommandOnlyTargetCompletesAMove`
- **DD-COLLECTION-029** — When platform data is dropped and accepted, the drop operation shall run `PayloadCommand` with the platform items, the hovered offset and the location (from `ItemsSource` when there is no `TargetCollection`), and shall insert nothing.
  - Tests: `CollectionDropTests.APlatformDropRunsTheCommandAtTheItemsSourceLocationAndInsertsNothing`
- **DD-COLLECTION-030** — A Copy drop shall insert the value `CreateCopy` returns for each payload (DD-DROP-026), which by default is the reference the source holds; any other effect shall insert the payloads.
  - Tests: `CollectionDropTests.ACopyDropInsertsTheCreatedCopy`, `CollectionDropTests.ACopyDropCopiesEachPayload`, `CollectionDropTests.AMoveDropIgnoresCreateCopy`, `EndToEndTests.CopyingWithAnOverriddenCreateCopyLeavesTheSourceUnchanged`
- **DD-COLLECTION-031** — After a drop, or when a drag leaves or stops being meant for the target, the drop operation shall clear the hovered container, its pseudo-classes, the location, auto-scrolling, `:dropover` and both adorners.
  - Tests: `CollectionDropTests.ADropClearsTheHoverAndTheAdorners`, `CollectionDropTests.ALeaveClearsTheHoverAndTheAdorners`, `CollectionDropTests.ADragThatStopsBeingMeantForTheTargetClearsTheHover`, `AutoScrollTests.LeavingStopsTheScrolling`
- **DD-COLLECTION-039** — When detached, the collection drop operation shall clear its hovered container's pseudo-classes and hide its insertion adorner.
  - Tests: `CollectionDropTests.DetachingMidDragClearsTheHoveredContainer`
- **DD-COLLECTION-047** — While an accepted drop transfers, reports to the drag source and runs `PayloadCommand`, a drop operation attached to a `SelectingItemsControl` shall hold its `AutoScrollToSelectedItem` false, then restore the value it had before.
  - Tests: `CollectionDropTests.InsertingBeforeTheSelectedItemKeepsTheContainersInStep`, `CollectionDropTests.ADropLeavesAutoScrollToSelectedItemAsItWas`

### Filtered and sorted lists

- **DD-COLLECTION-049** — Whenever the drag or drop operation reads an index from the attached control (a hovered, pressed or selected item), it shall map that display index into the collection it applies to: unchanged when that collection is the `ItemsSource` of the items control showing the item, and otherwise through `ResolveSourceIndex` (drag) or `ResolveTargetIndex` (drop).
  - Tests: `CollectionDropTests.AListBoundToItsTargetKeepsDisplayIndexes`, `CollectionDropTests.ADropOnAFilteredViewInsertsBeforeTheShownItemInTheCollection`, `CollectionDragTests.ADragFromAFilteredViewRecordsCollectionIndexes`, `EndToEndTests.ReorderingWithinAFilteredListRemovesTheOriginalNotTheCopy`, `EndToEndTests.MovingASelectionOutOfAFilteredListRemovesEveryOriginal`
- **DD-COLLECTION-050** — By default, display index *i* below the view's count shall map to the collection index of the item shown at *i*, matched by occurrence among equal items (the *k*-th equal item shown maps to the *k*-th equal item in the collection); *i* equal to the view's count shall map to one past the last shown item's index; an empty view, an index outside the view, or an item the collection does not contain shall map to null (append on the drop side, removal by equality on the drag side).
  - Tests: `DisplayIndexMapTests.AViewOfTheWholeCollectionMapsEachIndexToItself`, `DisplayIndexMapTests.AShownItemMapsToItsIndexInTheCollection`, `DisplayIndexMapTests.ThePositionAfterTheLastShownItemMapsJustAfterItInTheCollection`, `DisplayIndexMapTests.AnEmptyViewMapsToNull`, `DisplayIndexMapTests.AShownItemTheCollectionDoesNotContainMapsToNull`, `DisplayIndexMapTests.EqualItemsAreMatchedByOccurrence`, `DisplayIndexMapTests.AnIndexOutsideTheViewMapsToNull`, `CollectionDropTests.ADropAfterTheLastShownItemInsertsAfterItInTheCollection`
- **DD-COLLECTION-051** — When `ItemOrder` is `Sorted`, the drop operation shall track no hovered container, so it shows its drop adorner but no insertion adorner, sets no drop pseudo-classes on containers and uses a null location index; and it shall not be meant for drags from its own control.
  - Tests: `CollectionDropTests.ASortedTargetAppendsAndShowsNoInsertionLine`, `CollectionDropTests.ASortedTargetIsNotMeantForItsOwnDrags`
- **DD-COLLECTION-053** — On the sample's Filtered & Sorted page, with a filter on, reordering within the backlog, moving and Ctrl-copying tasks to the sprint and back, and dragging a multiple selection out of the filtered view shall land and remove the items the underlying order shows; with Sort by priority on, drops shall append and re-sort with no insertion line, and reordering within the backlog shall be refused.
  - Tests: manual — real OLE drags on `DragDropSample/FilteredCollectionPage`. Checked on Win32 2026-11-26.

### Auto-scroll

- **DD-COLLECTION-032** — While a drag meant for the target is within `AutoScrollMargin` of an edge of the scroll viewer (the attached control itself, or its first descendant `ScrollViewer`, looked up once per hover), the drop operation shall scroll that viewer towards the edge on a 16 ms timer.
  - Tests: `AutoScrollTests.HoveringNearAnEdgeScrollsTheListTowardsIt`, `AutoScrollTests.AnAttachedScrollViewerScrollsItself`, `AutoScrollTests.OutsideTheMarginsTheListDoesNotScroll`, `AutoScrollTests.AScrollViewerReplacedBetweenDragsIsTheOneScrolled`, `AutoScrollTests.AnOperationMovedToAnotherControlScrollsThatControlsViewer`
- **DD-COLLECTION-033** — The scroll step shall grow linearly from 0 at the margin's inner edge to 14 DIPs per tick at the viewer's edge, per axis.
  - Tests: `AutoScrollStepTests.TheStepGrowsLinearlyFromTheMarginsInnerEdgeToTheViewersEdge`
- **DD-COLLECTION-034** — The effective margin shall be at most a third of the viewer's extent on that axis, so small viewers keep a middle zone that does not scroll.
  - Tests: `AutoScrollStepTests.TheMarginIsAtMostAThirdOfTheExtent`
- **DD-COLLECTION-035** — When `AutoScrollMargin` is zero or less, the drop operation shall not auto-scroll.
  - Tests: `AutoScrollStepTests.AMarginOfZeroOrLessNeverScrolls`, `AutoScrollTests.AMarginOfZeroOrLessNeverScrolls`

### Defaults

- **DD-COLLECTION-036** (inherited) — When `AutoScrollMargin` is not set, the margin shall be 28 DIPs.
  - Tests: `AutoScrollTests.TheMarginDefaultsTo28Dips`
- **DD-COLLECTION-045** (inherited) — When `DraggedItems` is not set, it shall be `SelectedItems`.
  - Tests: `CollectionDragTests.DraggedItemsDefaultsToSelectedItems`
- **DD-COLLECTION-046** (inherited) — When `HandledEventsToo` is not set on a collection drag operation, it shall be false, so an ancestor that marks a tunneling press or move handled keeps the gesture.
  - Tests: `CollectionDragTests.ByDefaultTheSourceTunnelsAndIgnoresHandledPresses`, `CollectionDragTests.AnAncestorThatHandlesTunnelingPressesKeepsTheGesture`
- **DD-COLLECTION-052** (inherited) — When `ItemOrder` is not set, it shall be `Positional`.
  - Tests: `CollectionDropTests.ItemOrderDefaultsToPositional`

## Invariants

- At most one item container carries the drop pseudo-classes from a given drop operation at a time.
- The auto-scroll timer runs only while a drag meant for the target is over it.
- `DropCompleted` is called only after the payloads have been inserted.
- The payloads of a collection drag and its `CollectionDragInfo.Items` are in the same order, one entry each.

## Edge cases

- Dropping an item before or after itself inserts a duplicate next to it and removes the original: the list is unchanged (DD-COLLECTION-008). Dropping a selection inside itself gathers it at the location, in index order.
- In a sorted view with equal items, the occurrence rule (DD-COLLECTION-050) picks one of the equal items in the collection, not necessarily the one shown; equal items are interchangeable by `Equals`, so removal and insertion still land next to an equal item.
- A view whose items are not the collection's instances (a projection to view models) maps every index to null: drops append, and moves find nothing to remove unless the payloads are equal to the collection's items.
- Only a `ListBox` exposes its selection model, so other selecting controls drag the pressed item even with several items selected.
- The selection is read when the drag starts, after the press has updated it: a plain press on an unselected item makes it the only selection (DD-DRAG-009), so it drags alone, and a Ctrl-press adds it to the selection, which then drags as a whole.
- The library does not change selection on drop: the target does not select the dropped items, and the source keeps whatever selection survives the removal.
- The scroll viewer is looked up when a hover starts and kept until it ends, so a control re-templated in the middle of a hover keeps scrolling the old viewer until the drag leaves.
- DD-COLLECTION-047 covers only what runs inside the drop handler. A `PayloadCommand` that inserts after an `await`, or app code that inserts before the selected item at any other time, still hits the Avalonia bug it works around.

## Threading and lifetime

- UI thread only. The auto-scroll `DispatcherTimer` is created on first use, stopped when the drag ends and never disposed; it holds the operation through its callback while running.

## Platform behavior

- No platform-specific behavior beyond DD-DROP.

## Decisions

- **The drag side removes the payload.** The drop side inserts and then calls `DropCompleted`, and the drag operation removes its original. Reason: only the source knows where the payload lives and whether it should go (a Copy source keeps it). Rejected: the drop side removing from `SourceCollection`, which would couple every target to every source type.
- **Removal checks the index before trusting it.** When a drop completes with Move, the drag side removes the item at the pressed index, shifted by one when the drop inserted into the same collection at or before it, if that index still holds an equal item. Otherwise it removes the first equal item that is not the one the drop just inserted: the one at the drop's target index, or the last item when the drop appended. Reason: the source no longer needs to know what kind of target received the drop, so single-value and command-only targets work, and a wrong index (a command that inserted somewhere else, a collection changed during the drag) degrades to an equality search instead of removing the wrong item. Equality is `Equals`, so boxed value payloads (the sample's Guids) match. Rejected: special-casing each drop-info type in `DropCompleted`, which repeats the bug for the next target kind; reference equality, which can never match a boxed value.
- **A command-only drop reports its location.** Every accepted in-process drop that did something reports back (DD-DROP, "Every accepted in-process drop that did something reports back"). With no transfer, the `CollectionDropInfo` carries the requested location (collection and index) as the target. Reason: the drag side's removal shifts the original's index by the insertion when both are in the same collection, and a command that inserts at the location needs that shift too.
- **Payloads come from the containers and the selection, not `Payload`.** Reason: `Payload` or `SelectedItem` can differ from the items being dragged, and each payload must match its `CollectionDragItem` index or the wrong item is removed.
- **Drag the selection only when the pressed item is in it.** Reason: pressing an unselected item already makes it the selection (DD-DRAG-009), and dragging a selection the pointer is not on would surprise. Matches Explorer.
- **What a drag carries is named by an enum.** `DraggedItems` is `SelectedItems` or `PressedItem`. Reason: the setting reads as the choice it makes, in code and XAML. Rejected: a `DragsSelection` bool, whose false value does not say that only the pressed item is dragged.
- **Keep the control's order.** Reason: a moved block should look the same in its new place; selection order depends on click history. Rejected: selection order; the pressed item first.
- **Flat lists drag unrealized selected items.** Reason: the selection model gives every selected index and item, so items scrolled out of view are part of the gesture the user made. Trees cannot do the same (DD-TREE, "Trees drag only shown nodes").
- **The library does not select the dropped items.** Reason: what the target's selection should be after a drop (the dropped items, unchanged, or cleared) is an app decision, and apps can do it from `PayloadCommand` or `DropCompleted`. Rejected: selecting the dropped items in the target.
- **One insertion adorner is retargeted.** Reason: styles set on a custom `ItemDropAdorner` instance survive hover changes, and no adorner is created per container.
- **Locations fall back to `ItemsSource`.** Reason: platform drops and command-only targets still need an insertion index to hand to `PayloadCommand`.
- **Structural checks run before `CanDrop`.** See DD-COLLECTION-023. Reason: consumers override `CanDrop` for domain validation and should not have to remember to call the base for structural correctness.
- **Respect handled events on collection drags.** Tunneling alone already reaches the operation before `ListBoxItem` selects, so `HandledEventsToo` (formerly true) only overrode an ancestor that marked a tunneling press or move handled: a pan-and-zoom canvas, a design surface or a custom gesture host that claimed the gesture. Reason: Avalonia's convention is that a handled event belongs to whoever handled it, every other drag source (DD-DRAG-003) follows it, and one default is easier to explain. An app that wants the list to drag anyway sets `HandledEventsToo` on the operation. Rejected: keeping it on and documenting why (it defends against no known control); removing the property (apps under a gesture-claiming ancestor still need the opt-in).
- **Work around Avalonia's stale container on drop.** In Avalonia 12.2, inserting an item before the selected item of a `ListBox`, while `AutoScrollToSelectedItem` is on and the panel is a `VirtualizingStackPanel` (the defaults), leaves a stale container: the list shows an extra row and wrong contents until it re-realizes. The insert shifts the selection anchor, and since Avalonia #22002 the auto-scroll that follows runs synchronously, before the panel has handled the insert (regressed between 12.1.6 and 12.2.0, still present on master nightlies on 2026-11-26). Pressing an item selects it, so dropping a copy before it hit this every time. The drop operation holds `AutoScrollToSelectedItem` false at animation priority for the drop's duration (DD-COLLECTION-047), which outranks local values and styles and leaves no value behind. Reason: every consumer with a `ListBox` target is affected, and turning off either setting avoids it. Rejected: fixing only the sample (consumers would still hit it); documenting it without a workaround. Remove once Avalonia fixes the bug (backlog).
- **Map display indexes through the shown neighbor.** A filtered or sorted control shows a collection derived from `TargetCollection` or `SourceCollection`, so its container indexes are positions in the view. The operations map them to the collection through the item shown at that position (DD-COLLECTION-049, 050). Reason: it needs nothing from the app, works with any derived collection that holds the same item instances (an `ObservableCollection` the app re-filters, DynamicData, `DataGridCollectionView`), and keeps relative order for filters. Avalonia 12 has no built-in filtered or sorted view to ask. Rejected: requiring an app-supplied mapping (every app would write the same code); inserting at the display index (the bug this fixed).
- **The identity case is detected by reference.** When the collection is the showing control's `ItemsSource`, indexes pass through unchanged. Reason: zero cost and zero behavior change for lists bound directly.
- **Equal items are matched by occurrence.** Reason: value payloads (the sample's Guids, strings) can repeat, and matching the *k*-th occurrence keeps the mapping exact for filters, which preserve order. Rejected: the first equal item, which collapses duplicates onto one index.
- **Indexes are always in their own collection.** `CollectionDragItem.Index`, `CollectionDropLocation.Index` and `CollectionDropInfo.TargetIndex` refer to the collection next to them. Reason: removal and commands then need no knowledge of views, and `CollectionRemoval` is unchanged. Rejected: carrying both a display index and a collection index (twice the surface, and commands would pick the wrong one).
- **Overridable mappings.** `ResolveTargetIndex` and `ResolveSourceIndex` are protected virtuals. Reason: a view that knows its source indexes (a `DataGridCollectionView`) can answer directly and handle projections. Rejected: an interface the view must implement.
- **Sorted lists are declared, not detected.** `ItemOrder` says who orders the list (DD-COLLECTION-051). Reason: nothing in an `IList` says it is sorted, and an insertion line in a sorted list lies about where the item lands. Rejected: inferring from the view's type (works only for types the library knows). The name reads as what the list is; `InsertionMode` (`AtPosition`/`Append`), which reads as what the drop does, was the alternative.
- **Sorted targets refuse self-drops.** Reason: a self-drop in a sorted list inserts a copy and removes the original, a no-op that would still flash the item.
- **No logical-descendant check.** The former DD-COLLECTION-024 rejected drags whose event source was not a logical descendant of the attached control. Probing found that every source that reaches the target's handlers is one (item containers, template parts such as the empty area's `ScrollContentPresenter`), except the attached control itself: Avalonia's `IsLogicalAncestorOf` is false for the control, so a target whose own surface is hit (a `Border` with a background) rejected drags over it and accepted them over its child. Rejected: moving the check into `CanDropAtLocation`, which keeps that bug for a case nobody found.

## Non-goals

- Cloning payloads automatically on Copy. `CreateCopy` is the hook (DD-COLLECTION-030).
- Horizontal insertion zones for wrap or horizontal panels. `GetDropOffset` is virtual for that.
- Filtered or sorted trees. Trees read every collection from the tree (DD-TREE), so `TreeDragOperation` keeps display indexes.

## Open items

- None.