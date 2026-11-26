# DataGrid and ItemsRepeater support

## Why

The collection operations only understand `ItemsControl` (and `TreeView`): they find the pressed or hovered container, its index and item, the selection and the scroll viewer through `ItemsControl` APIs. Two common list controls are not `ItemsControl`s:

- **`DataGrid`** (`Avalonia.Controls.DataGrid`) is a `TemplatedControl` whose rows are `DataGridRow`s, laid out by its own `DataGridRowsPresenter` with scroll bars rather than a `ScrollViewer`. Row reordering and dragging rows between grids is one of the most requested drag-and-drop features in Avalonia, and the DataGrid has none built in (12.1 has column reordering only).
- **`ItemsRepeater`** (`Avalonia.Controls.ItemsRepeater`) is a `Panel` whose elements are the item template roots, with no containers and no selection, laid out by `StackLayout` or `UniformGridLayout`.

On both, `CollectionDragOperation` today falls back to `Payload` or the control's `DataContext` (DD-COLLECTION), so dragging a DataGrid row drags the grid's view model: silently wrong rather than unsupported. Prompted by the review of 2026-11-26.

## What

**An items-host abstraction inside the library.** Everything the collection operations ask of their control goes through one internal adapter, the *items host*:

- the container for an event source, its display index and its item; the container for an index, if realized;
- container lifecycle notifications (realized, re-indexed, recycled), which collection-fixes.md's `:dragging` tracking needs;
- the selected items with their display indexes, and whether an index is selected (the press logic in DD-DRAG uses `ISelectable` today, which `DataGridRow` is not);
- the items view, for display-index mapping (DD-COLLECTION-049);
- the flow (flow-layouts.md) and the insertion target of a container;
- how to auto-scroll.

The existing behavior becomes the built-in hosts for `ItemsControl`, `ListBox` (adds selection) and `TreeView`, with no change to what they do.

**Satellite packages.** The core package keeps depending only on `Avalonia`. Two new packages add the hosts and operation types:

- `MonacoLabs.DragDrop.DataGrid`: `DataGridDragOperation` and `DataGridDropOperation`.
  - Rows are the containers. Presses on column headers, the row header's resize grip and cells in edit mode don't start drags.
  - A drag carries the selected rows in display order when the pressed row is one of several selected (`DraggedItems`), like `ListBox`.
  - The insertion line is drawn between rows; the empty area below the last row appends.
  - Row recycling (`LoadingRow` / `UnloadingRow`) keeps `:dragging` on the right rows.
  - Auto-scroll scrolls the grid's rows (through its scroll bar or `ScrollIntoView`), since there is no `ScrollViewer`.
  - A `DataGridCollectionView` source works through the existing display-index mapping; see the open question on sorted grids.
- `MonacoLabs.DragDrop.ItemsRepeater`: `RepeaterDragOperation` and `RepeaterDropOperation`.
  - Elements are the containers, found with `GetElementIndex`, tracked through `ElementPrepared` / `ElementClearing`.
  - No selection: a drag carries the pressed item.
  - The flow comes from the layout: `StackLayout` by `Orientation`, `UniformGridLayout` as a wrapping flow (flow-layouts.md).
  - Auto-scroll uses the nearest ancestor `ScrollViewer`, since a repeater is placed inside one rather than containing one.

**Samples.**
- A `DataGridPage` ("Data grid"): an inventory table with sortable columns. Reorder rows, select several and drag them to an "Archive" `ListBox` and back, Ctrl-copy between two grids, and sort a column to see drops switch to appending.
- A `RepeaterPage` ("Items repeater"): a palette editor with color swatches in a `UniformGridLayout` (reorder in the grid) and a vertical `StackLayout` "Favorites" strip, with moves between them.

**What does not change:** the behavior of every existing operation, which become thin users of the built-in hosts.

## Decisions

- **One internal abstraction, built-in hosts for today's controls.** Reason: the collection operations' control-specific code is already spread over many protected virtuals (`LocatePayloadContainer`, `LocateTargetContainerIndex`, ...); gathering it lets two more controls plug in without duplicating the operations. Rejected: separate operation hierarchies per control (copies removal, mapping and selection logic).
- **The abstraction stays internal for now.** Reason: its shape will settle with two real implementations; publishing it later is easy, changing a published one isn't. Custom surfaces have their own proposal (custom-surfaces.md).
- **Satellite packages per control package.** Reason: an app without a DataGrid shouldn't pull one in, and the core stays dependent on `Avalonia` alone (ARCHITECTURE §2). Rejected: referencing both packages from the core; reflection over types by name (fragile, invisible to trimming).
- **Dedicated operation types.** Reason: they are discoverable in XAML and pick the host statically. Rejected: one `CollectionDragOperation` that looks up a registered host by control type at attach time.
- **Each satellite gets its own spec folder**, per the specs README ("a spec never crosses a package boundary"): `docs/specs/dragdrop-datagrid/` (`DDG-`) and `docs/specs/dragdrop-repeater/` (`DDR-`).

## Spec changes

**DD-COLLECTION**
- Added: requirements stating that container lookup, indexes, selection, mapping, lifecycle and auto-scroll go through the control's items host, with the built-in hosts for `ItemsControl`, `ListBox` and `TreeView` behaving as today's requirements say.
- Changed: DD-DRAG's selectable-press rule reads "selected in its items host" rather than "`ISelectable`".

**New specs:** `dragdrop-datagrid/datagrid.md` (DDG-DATAGRID) and `dragdrop-repeater/repeater.md` (DDR-REPEATER), each with the behaviors above, its tests and its decisions. `docs/specs/README.md` lists the two folders.

ARCHITECTURE (§2 packages, §5 pages), CLAUDE.md, `docs/PACKAGE.md` (or a README per satellite), CI (pack and test the satellites).

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **Items host refactor.** Move today's control logic behind the internal host, with no behavior change: the whole existing suite stays green, and no test changes.
2. **DataGrid package**, its project, test project (or test folder with the package reference) and spec.
   - `DataGridDragTests.DraggingARowCarriesItsItem`
   - `DataGridDragTests.DraggingASelectedRowCarriesTheSelectionInDisplayOrder`
   - `DataGridDragTests.PressesOnHeadersAndEditingCellsDoNotDrag`
   - `DataGridDropTests.HoveringBetweenRowsShowsTheInsertionLine`
   - `DataGridDropTests.BelowTheLastRowAppends`
   - `DataGridDropTests.HoveringNearAnEdgeScrollsTheRows`
   - `DataGridEndToEndTests.ReorderingRowsMovesTheItem`
   - `DataGridEndToEndTests.MovingRowsToAListRemovesThem`
3. **ItemsRepeater package**, likewise.
   - `RepeaterDragTests.DraggingAnElementCarriesItsItem`
   - `RepeaterDropTests.AUniformGridLayoutWrapsItsZones`
   - `RepeaterDropTests.AutoScrollUsesTheAncestorScrollViewer`
   - `RepeaterEndToEndTests.ReorderingInTheGridMovesTheItem`
4. **Sample pages** `DataGridPage` and `RepeaterPage`, with the package references and the DataGrid theme include.
5. **Manual check on the Win32 sample.** Reorder and move rows, sort a column, edit a cell and confirm editing still works, reorder swatches and move them to favorites.
6. **Close.** Packages build, pack and test in CI, specs cite every test, this proposal is deleted.

## Open questions

- **Sorted grids.** When a `DataGridCollectionView` has sort descriptions, a positional drop lies about where the row lands. Either detect it (the library knows this type) and behave as `ItemOrder="Sorted"`, or leave it to the app to bind `ItemOrder`. Detecting departs from the collection spec's decision "Sorted lists are declared, not detected". Recommended: detect for `DataGridCollectionView` only, and record the exception in the decision.
- **Grouped grids.** Group header rows have no item. Recommended: out of scope; reject drops while the view is grouped, as a non-goal of the DataGrid spec.
- **Selection for repeaters.** `ItemsRepeater` has none; apps build their own. Recommended: pressed item only for now; add a bindable selected-items property on the repeater operations if the sample shows the need.
- **Package versions.** Confirm the current `Avalonia.Controls.DataGrid` (12.1.0 here) and `Avalonia.Controls.ItemsRepeater` (12.0.0) versions and licenses when starting, and whether they need to match the core's `Avalonia` 12.2.
- **Order with flow-layouts.md.** The repeater's `UniformGridLayout` needs wrapping flows. Recommended: land flow-layouts first; otherwise ship the repeater with `StackLayout` only.
