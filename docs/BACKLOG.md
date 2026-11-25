# Backlog

The single list of work not done in MonacoLabs.DragDrop: known bugs, gaps, checks, candidates and undecided questions. Each line is tagged with the spec it belongs to.
Small items are taken straight from here and fixed with their spec updated. Larger ones become a change proposal in `docs/changes/` first (see [docs/changes/README.md](changes/README.md)).
When an item is closed, its line is deleted.

## Bugs and gaps

- [DD-DRAG] A press whose payload resolves to null stays tracked and re-resolves the payload on every move until release. Reset the press instead.
- [DD-COLLECTION] Insertion and removal indexes come from item containers, so a `PayloadTarget` that differs from the displayed order (a filtered or sorted view over it) inserts at the wrong position.
- [DD-COLLECTION] The auto-scroll `ScrollViewer` is cached for the operation's lifetime and is not refreshed when the control is re-templated or detached.
- [DD-DROP] Consider consulting `PayloadCommand.CanExecute` during DragOver, so a drop the command will refuse shows None instead of failing on release (see DD-DROP, "A drop that does nothing reports None").
- [DD-DRAG] Consider a public `DragFailed` event alongside the drag-failure logging (DD-DRAG-030).
- [DD-ADORNERS] Add pixel tests for the insertion line's position in the gap between containers (DD-ADORNERS-013), the `OnTarget` outline (014) and device-pixel alignment at render scaling above 1 (016), and for the preview snapshot at scaling above 1 (DD-SESSION-004). They need exact pixel comparisons and a way to render headless windows at other scalings.
- [DD-COLLECTION] Check whether `CollectionDragOperation` still needs `HandledEventsToo`. With tunneling it sees a press before `ListBoxItem` handles it, and `CollectionDragTests.PressesOnAListBoxItemReachTheOperation` passes with it off; moves and releases may still need it.

## Docs and hygiene

- [DD-ADORNERS] Fix `DropAdornerBase.IsDropValid`'s doc comment, copied from `TargetControl`.
- [DD-WIRING] Add doc comments to `DragDrop.SetIsDragHandle`/`GetIsDragHandle`, `SetIgnoresPress`/`GetIgnoresPress` and to the public types that have none (`DragDrop`, `DropInfo`, `CollectionDropInfo`, `DropTargetOffset`, `ItemDropOperation`, `IDragOperation`, `IDropOperation`).
- [DD-TRANSFER] Cache `DataFormat` instances per interaction id instead of creating one per id on every DragOver.
- [DD-WIRING] Remove the `Guid` and `ComVisible` assembly attributes.
- Enable `GenerateDocumentationFile` and fix the resulting missing-doc warnings.
- Add `Microsoft.CodeAnalysis.PublicApiAnalyzers` with `PublicAPI.Shipped.txt`/`Unshipped.txt`.
- Add package metadata (description, license expression, repository URL, readme, tags), SourceLink, and `ContinuousIntegrationBuild` under CI.
- Add an `.editorconfig` with the repo's formatting rules.
- Report code coverage from CI with `Microsoft.Testing.Extensions.CodeCoverage`.
- [DD-EFFECTS] Add a macOS leg to CI, so the Option-copy modifier (DD-EFFECTS-005) runs through `OperatingSystem.IsMacOS()` rather than only through the resolver's macOS parameter.

## Manual and platform checks

- [DD-EFFECTS] Check the Option-copy modifier and the Explorer-style effects on macOS. Nothing has been run on macOS, and Linux only runs the headless tests in CI.

## Candidates

- Horizontal and wrap-panel insertion zones for `CollectionDropOperation`.
- Animating the preview back to its source on cancel.
- [DD-WIRING] A template or factory property so a drag or drop operation can be declared once in a style and created per styled control (see DD-WIRING, "Operations are not shareable through styles").
