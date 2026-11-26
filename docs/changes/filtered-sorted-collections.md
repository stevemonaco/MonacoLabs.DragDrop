# Drag and drop with filtered and sorted lists

## Why

Collection operations take every index from the item control's containers (DD-COLLECTION Edge cases). That is correct only when the control shows `PayloadTarget` itself. A real app often shows a filtered or sorted collection built from its backing list: it binds `ItemsSource` to the derived collection and points `PayloadTarget` and `PayloadCollection` at the backing list. Avalonia 12 has no built-in filtered or sorted view for `ItemsControl` (only `ItemsSourceView`, a wrapper that does neither), so the derived collection is always the app's own: an `ObservableCollection` it re-filters, a DynamicData pipeline, or `DataGridCollectionView`.

With a filtered list today:

- **Drops land in the wrong place.** Dropping before the 3rd shown item inserts at index 2 of the backing list, which may be far from that item, or not next to any shown item at all.
- **Moves can remove the wrong item.** The drag records display indexes, and removal checks them against the backing list. Usually that check fails and the equality fallback finds the original. But on a reorder within one filtered list, the "inserted range" that the fallback skips is computed from the display index, so it can skip the original and remove the copy just inserted, leaving the list as it was plus one duplicate.

With a sorted list, the insertion line itself is misleading: the item lands wherever the sort puts it.

Prompted by the backlog item [DD-COLLECTION] "Insertion and removal indexes come from item containers".

## What

**Display indexes are mapped to the collection.** Whenever a collection operation reads an index from the control (a hovered container, a pressed container, a selected index), it maps that *display index* into the collection it is about to use: `PayloadTarget` on the drop side, the item's `PayloadCollection` on the drag side. When that collection is the control's `ItemsSource`, the index is unchanged, so every app that binds the list directly behaves exactly as today. Otherwise:

- **Insert before the shown item at display index *i*:** insert at that item's index in the collection.
- **Insert after the last shown item:** insert just after that item's index in the collection, so the new item stays at the end of the filtered view if it passes the filter.
- **An empty view, or a shown item the collection doesn't contain:** append.
- **Equal items** (the same value shown twice) are matched by occurrence: the *k*-th equal item in the view maps to the *k*-th equal item in the collection. That is exact for filters, which keep the backing order. For sorted views it picks one of the equal items, which are interchangeable by `Equals`.

Both sides use the same mapping, as two protected virtuals that apps override when they know better (a `DataGridCollectionView` can answer directly): `CollectionDropOperation.ResolveTargetIndex(IList collection, int displayIndex)` and `CollectionDragOperation.ResolvePayloadIndex(IList collection, int displayIndex, object item)`.

As a result, `CollectionDragItem.Index`, `CollectionDropLocation.Index` and `CollectionDropInfo.TargetIndex` are always indexes into their own collection. Removal (DD-COLLECTION-008, 009) is then correct for filtered lists with no change to `CollectionRemoval`.

**Sorted lists.** A new styled property `CollectionDropOperation.ItemOrder` says who orders the list: `Positional` (default; the user picks the position) or `Sorted` (the collection orders itself). With `Sorted`, the drop operation:

- shows the highlight adorner over the whole control and no insertion line, and sets no `:droptop`/`:dropbottom` on containers;
- appends (the location index is null), and the view re-sorts;
- is not meant for drags that started from the same control, because reordering a sorted list does nothing.

**Sample.** A new page, `FilteredCollectionPage` ("Filtered & Sorted"):

- **Backlog** (left): tasks with a title and a priority, shown through a view the page builds from a backing `ObservableCollection`. A search box filters by title, a "Hide done" check box filters by state, and a "Sort by priority" toggle switches the view to a priority sort and the drop operation to `ItemOrder="Sorted"`.
- **Sprint** (right): a plain list bound directly, as the control on the other side of moves and Ctrl-copies.
- **Underlying order** (below the backlog): a read-only list of the backing collection, so the user sees exactly where a drop landed and which item a move removed.

Reorder within the filtered backlog, move tasks to the sprint and back, and select several shown tasks and drag them together.

**What does not change:** lists bound directly, interaction ids, validation, effects, and removal by equality. Trees read every collection from the tree (DD-TREE), so a filtered tree inserts into whatever its `ItemsSource` is; filtered trees stay out of scope.

## Decisions

- **Map through the shown neighbor.** Reason: it needs nothing from the app, works with any derived collection that holds the same item instances, and keeps relative order for filters. Rejected: requiring an app-supplied mapping (every app would write the same code); inserting at the display index (today's bug).
- **The identity case is detected by reference.** When the collection is the control's `ItemsSource`, indexes pass through unchanged. Reason: zero cost and zero behavior change for the common case.
- **Occurrence matching for equal items.** Reason: value payloads (the sample's Guids, strings) can repeat. Matching the *k*-th occurrence keeps the mapping exact for filters, which preserve order. Rejected: first equal item, which collapses duplicates onto one index.
- **Indexes are always in their own collection.** `CollectionDragItem.Index`, `CollectionDropLocation.Index` and `CollectionDropInfo.TargetIndex` refer to the collection next to them. Reason: removal and commands then need no knowledge of views. Rejected: carrying both a display index and a collection index (twice the surface, and commands would pick the wrong one).
- **Sorted lists are declared, not detected.** Reason: nothing in an `IList` says it is sorted. A declared `ItemOrder` lets the insertion feedback stop lying. Rejected: inferring from the view's type (works only for types the library knows).
- **Sorted targets are part of this change.** Agreed 2026-11-26. Reason: the sample's sort toggle needs them, and mapping indexes alone leaves sorted lists with a misleading insertion line. `ItemOrder` does not depend on the mapping, so it could land separately, but it would not be demonstrable without it. Rejected: a later proposal.
- **Sorted targets refuse self-drops.** Reason: a self-drop in a sorted list inserts a copy and removes the original, a no-op that would still flash the item.

## Spec changes

**DD-COLLECTION**
- Added: "Whenever the drag or drop operation reads an index from the attached control, it shall map that display index into the collection it applies to: unchanged when the collection is the control's `ItemsSource`, and otherwise through `ResolvePayloadIndex` (drag) or `ResolveTargetIndex` (drop)."
- Added: "By default, display index *i* below the view's count shall map to the collection index of the item shown at *i*, matched by occurrence among equal items; *i* equal to the view's count shall map to one past the last shown item's index; an empty view or an item the collection does not contain shall map to null (append)."
- Changed **DD-COLLECTION-006**: each `CollectionDragItem` carries "its index in its collection".
- Changed **DD-COLLECTION-022**: "the location's index shall be the mapping of *i* for `BeforeTarget` and of *i* + 1 for `AfterTarget`".
- Changed **DD-COLLECTION-043**: unrealized selected items carry "their mapped index".
- Added: "When `ItemOrder` is `Sorted`, the drop operation shall show its drop adorner and no insertion adorner, set no offset pseudo-classes on containers, use a null location index, and not be meant for drags from the same control."
- Added (inherited): "When `ItemOrder` is not set, it shall be `Positional`."
- Edge cases: replace "Indexes come from the item control's containers..." with the occurrence rule for sorted views with equal items, and "a view whose items are not the collection's instances (a projection to view models) appends".
- Non-goals: add "Filtered or sorted trees".
- Decisions: add the decisions above.

**DD-ADORNERS**: note that a `Sorted` collection target uses only the drop adorner.

ARCHITECTURE §5 and CLAUDE.md: add the new page and describe display-index mapping in the `CollectionDropOperation` paragraph.

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **The mapping.** A pure helper in `Internals/` with plain `[Theory]` tests.
   - `DisplayIndexMapTests`: identity; before a shown item; after the last shown item; empty view; item missing from the collection; duplicates by occurrence.
2. **Drop side.** `ResolveTargetIndex`, used by `ResolveDropLocation`.
   - `CollectionDropTests.ADropOnAFilteredViewInsertsBeforeTheShownItemInTheCollection`
   - `CollectionDropTests.ADropAfterTheLastShownItemInsertsAfterItInTheCollection`
   - `CollectionDropTests.AListBoundToItsTargetKeepsDisplayIndexes`
3. **Drag side.** `ResolvePayloadIndex`, used for pressed and selected items.
   - `CollectionDragTests.ADragFromAFilteredViewRecordsCollectionIndexes`
   - `EndToEndTests.ReorderingWithinAFilteredListRemovesTheOriginalNotTheCopy`
   - `EndToEndTests.MovingASelectionOutOfAFilteredListRemovesEveryOriginal`
4. **Sorted targets.** `ItemOrder` and its feedback.
   - `CollectionDropTests.ASortedTargetAppendsAndShowsNoInsertionLine`
   - `CollectionDropTests.ASortedTargetIsNotMeantForItsOwnDrags`
   - `CollectionDropTests.ItemOrderDefaultsToPositional`
5. **Sample page.** `FilteredCollectionPage` as described, added to `MainWindowViewModel.Pages` and the ARCHITECTURE table.
6. **Manual check on the Win32 sample.** On the new page: with a filter on, reorder within the backlog and confirm the underlying order; move tasks to the sprint and back; drag a multiple selection out of a filtered view; switch to priority sort and confirm there is no insertion line, drops append and the view re-sorts.
7. **Close.** The property's name is settled, every changed and added requirement cites its tests, DD-COLLECTION's Open items line is removed, and this proposal and the backlog pointer to it are deleted.

## Open questions

- **The property's final name.** Deferred 2026-11-26: build with `ItemOrder` (`Positional`/`Sorted`), which reads as what the list is, and settle the name once the API and the sample are together, before task 7. The alternative is `InsertionMode` (`AtPosition`/`Append`), which reads as what the drop does.
- **How the sample keeps its view in sync.** Deferred 2026-11-26, to be settled while building the sample (task 5). What each option implies:
  - *Rebuild* (clear and refill the shown collection on every source or filter change). Little sample code. Every change is a `Reset`, so the `ListBox` drops its selection and re-realizes its containers after each drop and on each keystroke in the search box: items selected but not dragged lose their selection. A drop's insertion also triggers a `Reset` while the drop handler is still running, so the page exercises the library with its hovered container recycled mid-drop, which no page does today. That is useful coverage, but any bug it finds lands in this change.
  - *Incremental sync* (mirror each source insert and removal into the view when the item passes the filter, at its mapped position, and recompute only on filter or sort changes). Selection survives drops, and the page behaves like an app built on DynamicData or a collection view. It costs more sample code, roughly a small helper class, and that code has to be right for the page to be trustworthy.
