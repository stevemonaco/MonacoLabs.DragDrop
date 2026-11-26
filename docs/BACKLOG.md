# Backlog

The single list of work not done in MonacoLabs.DragDrop: known bugs, gaps, checks, candidates and undecided questions. Each line is tagged with the spec it belongs to.
Small items are taken straight from here and fixed with their spec updated. Larger ones become a change proposal in `docs/changes/` first (see [docs/changes/README.md](changes/README.md)).
When an item is closed, its line is deleted.

## Bugs and gaps

- [DD-COLLECTION] A drag source nested in another drag source's item (a `ListBox` inside an outer `ListBox`'s item) never drags its own items: the outer operation tunnels first and starts the drag. The innermost source that tracks the press should win. Found while probing `HandledEventsToo` on collection drags.
- [DD-ADORNERS] Add pixel tests for the insertion line's position in the gap between containers (DD-ADORNERS-013), the `OnTarget` outline (014) and device-pixel alignment at render scaling above 1 (016), and for the preview snapshot at scaling above 1 (DD-SESSION-004). They need exact pixel comparisons and a way to render headless windows at other scalings.

## Docs and hygiene

- [DD-COLLECTION] Report Avalonia's stale container upstream (repro: select item 2 of `["a", "b", "c"]` in a `ListBox`, then `Insert(0, "c")`; it shows `c, c, a, b, c`; introduced by Avalonia #22002, commit `4142052f9`, first released in 12.2.0), and remove DD-COLLECTION-047's `AutoScrollToSelectedItem` workaround once a fixed Avalonia ships. Its two tests should keep passing without it.
- [DD-EFFECTS] Add a macOS leg to CI, so the Option-copy modifier (DD-EFFECTS-005) runs through `OperatingSystem.IsMacOS()` rather than only through the resolver's macOS parameter.

## Manual and platform checks

- [DD-EFFECTS] Check the Option-copy modifier and the Explorer-style effects on macOS. Nothing has been run on macOS, and Linux only runs the headless tests in CI.

## Candidates

- Horizontal and wrap-panel insertion zones for `CollectionDropOperation`.
- Animating the preview back to its source on cancel.
- [DD-WIRING] A template or factory property so a drag or drop operation can be declared once in a style and created per styled control (see DD-WIRING, "Operations are not shareable through styles").
