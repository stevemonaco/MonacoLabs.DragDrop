# Custom drag-and-drop surfaces: slots and free placement

## Why

Some of the most drag-heavy UIs aren't lists or trees, and aren't built from one control per item:

- a **tilemap or level editor**: a grid of cells drawn by one control, where tiles are painted from a palette, moved or swapped between cells, and erased by dragging them off;
- an **image editor**: a pasted or dropped image that is dragged around a canvas to align where it lands before it is applied;
- seat maps, calendar time slots, board games, diagram canvases.

The library has a subclassing story for validation (`CanDrop`) and transfer (`Transfer`), but such a surface hits walls:

- **No hover step to override.** Resolving what is under the pointer and drawing feedback are the `private protected` steps `ResolveHover`, `UpdateFeedback` and `ClearFeedback`, used only by the collection and tree targets. A custom target can't compute "the cell under the pointer" once per event before validation, and can only draw feedback from `OnDragOver`, after the effect is decided.
- **Feedback needs a control.** `DropInsertionAdorner` and the container pseudo-classes target controls. A cell drawn by `Render` has no control to outline.
- **The drag side thinks in controls too.** `ResolvePayloads` can pick a payload from the press position, but the preview snapshots a whole control (DD-SESSION-002), `:dragging` marks a control, and `DropCompleted` has only `DragOrigin` (a point) to find what to remove.
- **Placement needs the grab point.** `DropEventArgs.Position` (DD-TRANSFER-016, added 2026-11-26) gives the pointer, but placing an object so it doesn't jump needs where in the object it was grabbed, and a snapped placement preview needs a hook.

Prompted by the review of 2026-11-26: a tilemap grid sample is required, and an image editor is the second case.

## What

Three layers, each usable without the next.

**1. The hover steps become protected.** `DropOperationBase` exposes the steps every target runs (names open): resolving the hover, the structural check, updating and clearing feedback. They keep their order (DD-DROP's flow); the collection and tree targets become ordinary users of them.

**2. Slot operations.** A *slot* is an app-defined object naming a place on the surface: a cell coordinate, a seat, a time slot. A pair of operations (names open: `SlotDragOperation` / `SlotDropOperation`) is overridden with:

| Drag side | Drop side |
|---|---|
| `ResolveSlot(point)`: the slot under a press, or null to not drag | `ResolveSlot(point)`: the slot under the pointer, or null |
| `GetSlotBounds(slot)`: the slot's rectangle, snapshotted as the preview and marked as dragging | `GetSlotBounds(slot)`: the rectangle to outline |
| `ResolvePayloads(slot)` | `CanDropAt(slot, payloads)`, with `Reject(message)` as usual |
| `RemoveFrom(slot, payloads)` on a Move, or `Swap` with what the target replaced | `TransferTo(slot, values)`: returns the replaced value, if any |

The drop target outlines the slot's bounds with a new `DropRegionAdorner` (a styled rectangle with `:error`, like the other adorners), and `DropEventArgs` and the drop info carry the slot, so a command-only target can act on it. Slots are compared with `Equals`, and a drop onto the slot it came from is rejected structurally. Swap uses the same reporting as drop-onto-items.md: the target reports what it replaced, and a drag side with `SwapOnDrop` puts it in the slot the drag came from.

**3. Free placement.**
- `DragInfo` and `DropEventArgs` carry the **grab offset**: the press point relative to the drag visual (or to the slot's bounds), so the dropped object's top-left is `Position - GrabOffset`.
- A drop target can show a **placement preview** instead of the floating preview while the drag is over it: an override (name open, `ResolvePlacement(position, grabOffset)`) returns the rectangle where the object would land, after snapping or clamping, and the session draws the snapshot there. The floating preview reappears when the pointer leaves the target.

**Samples** (both required by this change):
- **`TilemapPage` ("Tilemap"):** a level editor. A `TileMapView` control renders a 16×12 map itself, with no child per cell, from a view model holding the tiles. A palette strip drags tiles onto cells (Copy, paints). A cell drags onto another cell (Move, swaps when the target is occupied), and dragging a tile off the map erases it (`RemoveOnDropOutside`). `CanDropAt` rejects a building on water with a message. The hovered cell is outlined by `DropRegionAdorner`.
- **`ImageEditorPage` ("Image editor"):** a canvas with a base image. Paste an image (Ctrl+V; reading the clipboard is sample code, not library code) or drop an image file from Explorer at the pointer (`platform:files` plus `Position`) to create a floating layer. Drag the layer around with a placement preview snapped to an 8-pixel grid and clamped to the canvas; "Apply" flattens it.

**What does not change:** lists, trees and single-value operations; the interaction-id, validation and effect rules.

## Decisions

- **Slots are opaque, app-defined objects.** Reason: cells, seats and time slots have nothing in common but "a place under the pointer with bounds". Rejected: integer cell coordinates (fits tilemaps only).
- **Open the existing steps rather than add parallel hooks.** Reason: the collection and tree targets already prove the steps are the right seams; a second set would drift. Rejected: keeping them `private protected` and offering only slot operations.
- **Feedback by rectangle, not by control.** Reason: a surface that renders its own content has no per-slot controls, and adding one per cell just for feedback defeats the point. A control-based surface can return its control's bounds.
- **Placement preview replaces the floating preview over its target.** Reason: two previews (one at the pointer, one at the snapped spot) is confusing; the snapped one is where the object will actually land.
- **Paste stays in the sample.** Reason: clipboard handling isn't drag and drop; the library's part is placing what arrives, whether pasted, dropped from another app or dragged within the canvas.
- **Both samples ship.** Reason: they exercise different halves, discrete slots and continuous placement. Either alone would leave one half unproven.

## Spec changes

**DD-DROP**: the hover steps are protected and documented as the flow's extension points (requirements on their order stay as they are; DD-COLLECTION and DD-TREE become users of them).

**DD-TRANSFER**: `DragInfo` and `DropEventArgs` gain the grab offset; `DropEventArgs` and the drop info gain the slot.

**DD-SESSION**: the preview can be a region of the drag visual, and can be replaced by a target's placement preview while over that target.

**DD-ADORNERS**: `DropRegionAdorner`.

**New spec** `dragdrop/surface.md` (DD-SURFACE): slot operations, slot resolution, structural rejection of self-drops, swap, removal on Move and outside drops.

ARCHITECTURE §5 and CLAUDE.md: the two pages and the slot operations.

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **Protected hover steps.** Expose and document them; the existing suite stays green.
   - `DropTargetTests.ACustomHoverStepRunsBeforeValidationOnEveryEvent`
2. **Grab offset.**
   - `EndToEndTests.TheDropSeesWhereTheDraggedVisualWasGrabbed`
3. **Region preview and region adorner.**
   - `DragSessionTests.APreviewCanShowARegionOfTheDragVisual`
   - `DropAdornerTests.TheRegionAdornerOutlinesItsRectangle` (pixel test)
4. **Slot operations.**
   - `SurfaceTests.ThePressedSlotsPayloadIsDragged`
   - `SurfaceTests.TheHoveredSlotIsOutlinedAndValidated`
   - `SurfaceTests.DroppingOnTheSourceSlotIsRejected`
   - `SurfaceTests.AMoveRemovesFromTheSourceSlot`
   - `SurfaceTests.SwapOnDropTradesTheTwoSlots`
   - `SurfaceTests.ACommandOnlySurfaceReceivesTheSlot`
5. **Placement preview.**
   - `DragSessionTests.ATargetsPlacementPreviewReplacesTheFloatingPreview`
   - `DragSessionTests.LeavingTheTargetRestoresTheFloatingPreview`
6. **`TilemapPage`** as described.
7. **`ImageEditorPage`** as described.
8. **Manual check on the Win32 sample.** Paint, move, swap and erase tiles, and see a rejection; paste an image and drop one from Explorer, drag it with snapping, and apply it.
9. **Close.** Names settled, specs cite every test, this proposal is deleted.

## Open questions

- **Names** for the protected steps, the slot operations, `GetSlotBounds`, `ResolvePlacement` and the grab offset property.
- **Dragging within one control.** Moving the image layer inside the canvas goes through the platform drag loop (cursor effects, Esc cancels), as every drag does today. Confirm that is acceptable for the editor, or whether a capture-based move inside one control belongs in this change.
- **Clipboard images.** Confirm Avalonia 12 reads bitmaps from the clipboard on Win32; if not, the editor's paste takes image files and paths only.
- **Order with drop-onto-items.md.** Both report replaced values for swaps. Recommended: land drop-onto-items first and reuse its reporting.
