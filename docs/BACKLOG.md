# Backlog

The single list of work not done in MonacoLabs.DragDrop: known bugs, gaps, checks, candidates and undecided questions. Each line is tagged with the spec it belongs to.
Small items are taken straight from here and fixed with their spec updated. Larger ones become a change proposal in `docs/changes/` first (see [docs/changes/README.md](changes/README.md)).
When an item is closed, its line is deleted.

In progress elsewhere: the review bugs in [changes/correctness-fixes.md](changes/correctness-fixes.md), and the test suite in [changes/test-suite.md](changes/test-suite.md).

## Bugs and gaps

- [DD-DROP] Redesign the drop side's overridable surface; needs a change proposal. `DropOperationBase` has two layers: the subscribed handlers `DragEnter`/`DragOver`/`DragLeave`/`Drop(object? sender, DragEventArgs e)`, which do targeting, validation and adorners, and the hooks `OnDragEnter`/`OnDragOver`/`OnDragLeave`/`OnDrop`, which they call. `CollectionDropOperation` replaces the `DragEnter`, `DragOver` and `Drop` handlers with its `UpdateDrag` flow and overrides only the `OnDragLeave` hook. As a result, on collection and tree targets an `OnDragEnter` or `OnDragOver` override is silently never called, `OnDrop` is, and `OnDragLeave` means "end the drag". Which layer to override depends on the base class, and nothing tells the consumer.
  - Direction: one template flow in the base (target check → hover resolution → location → validation → adorners → effect), with protected steps that collection and tree targets override (`LocateTargetContainer`, `ResolveDropLocation`, `GetDropOffset`, `CanDropAtLocation` already exist), private subscribed handlers, and one set of `On*` notifications that every target raises.
  - The drag side already has this shape (DD-DRAG, "Pointer hooks, not event handlers"). Rewrites much of DD-DROP and DD-COLLECTION, so it is best done before `PublicApiAnalyzers` baselines the API.

- [DD-SESSION] An app that sets `AllowDrop` on its window silently disables `RemoveOnDropOutside` and the preview's `:remove`, because the session only tracks outside-target areas when it injected `AllowDrop` itself.
- [DD-DRAG] A press whose payload resolves to null stays tracked and re-resolves the payload on every move until release. Reset the press instead.
- [DD-COLLECTION] Insertion and removal indexes come from item containers, so a `PayloadTarget` that differs from the displayed order (a filtered or sorted view over it) inserts at the wrong position.
- [DD-COLLECTION] The auto-scroll `ScrollViewer` is cached for the operation's lifetime and is not refreshed when the control is re-templated or detached.
- [DD-DROP] Consider consulting `PayloadCommand.CanExecute` during DragOver, so a drop the command will refuse shows None instead of failing on release (see correctness-fixes, "A drop that does nothing reports None").
- [DD-DRAG] Consider a public `DragFailed` event alongside the logging that correctness-fixes adds.
- [DD-COLLECTION] Check whether `CollectionDragOperation` still needs `HandledEventsToo`. With tunneling it sees a press before `ListBoxItem` handles it, and `CollectionDragTests.PressesOnAListBoxItemReachTheOperation` passes with it off; moves and releases may still need it.

## Docs and hygiene

- [DD-COLLECTION] Fix the doc comments of `CollectionDragInfo.PayloadContainerIndex` ("where the payload was dropped") and `CollectionDropInfo.TargetIndex` ("should match the index in the PayloadCollection").
- [DD-ADORNERS] Fix `DropAdornerBase.IsDropValid`'s doc comment, copied from `TargetControl`.
- [DD-WIRING] Add doc comments to `DragDrop.SetIsDragHandle`/`GetIsDragHandle`, `SetIgnoresPress`/`GetIgnoresPress` and to the public types that have none (`DragDrop`, `DropInfo`, `CollectionDropInfo`, `DropTargetOffset`, `ItemDropOperation`, `IDragOperation`, `IDropOperation`).
- [DD-TRANSFER] Cache `DataFormat` instances per interaction id instead of creating one per id on every DragOver.
- [DD-WIRING] Remove the `Guid` and `ComVisible` assembly attributes.
- Enable `GenerateDocumentationFile` and fix the resulting missing-doc warnings.
- Add `Microsoft.CodeAnalysis.PublicApiAnalyzers` with `PublicAPI.Shipped.txt`/`Unshipped.txt`. The pre-1.0 API changes have landed; the drop-side redesign (Bugs and gaps) and correctness-fixes still change the surface, so their entries start in `Unshipped.txt`.
- Add package metadata (description, license expression, repository URL, readme, tags), SourceLink, and `ContinuousIntegrationBuild` under CI.
- Add an `.editorconfig` with the repo's formatting rules.

## Manual and platform checks

- [DD-PLATFORM] Drag a file from Explorer onto the Desktop page's file list and confirm Explorer keeps it (DD-PLATFORM-008).
- [DD-PLATFORM] Drag a snippet from the Desktop page into Notepad and a browser text field (DD-PLATFORM-009).
- [DD-SESSION] On the Single-item page, drop a slot's item on the window background (clears), on the desktop and with Esc (both keep) (DD-SESSION-013).
- [DD-ADORNERS] Check the insertion line at 100% and 150% Windows scaling (DD-ADORNERS-021).
- [DD-EFFECTS] Check the Option-copy modifier and the Explorer-style effects on macOS. Nothing has been run on macOS or Linux.

## Candidates

- Dragging several selected items as one payload.
- Horizontal and wrap-panel insertion zones for `CollectionDropOperation`.
- Animating the preview back to its source on cancel.
- [DD-WIRING] A template or factory property so a drag or drop operation can be declared once in a style and created per styled control (see DD-WIRING, "Operations are not shareable through styles").
