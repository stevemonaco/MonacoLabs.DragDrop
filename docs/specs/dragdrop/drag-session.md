---
id: DD-SESSION
title: Drag session, preview and drops outside targets
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop
sources:
  - MonacoLabs.DragDrop/Internals/DragSession.cs
  - MonacoLabs.DragDrop/Adorners/DragPreviewAdorner.cs
types:
  - DragPreviewAdorner
tests:
  - DragSessionTests
  - CollectionDragTests
depends:
  - DD-DRAG
---

# Drag session, preview and drops outside targets

## Purpose

One internal `DragSession` exists per drag, created by the drag source (DD-DRAG) just before the platform drag starts and disposed after it returns. It owns the source-side visuals: the `:dragging` pseudo-class on the drag visual and the other dragged containers, and the `DragPreviewAdorner` snapshot that follows the pointer within the source's window. It also listens to the window's own drag events so that, with `RemoveOnDropOutside`, a drop on part of the window that is not a drop target can remove the payload.

## Requirements

### Dragging pseudo-class

- **DD-SESSION-001** — While a drag runs, the drag visual and every other control the drag source names (DD-COLLECTION-005) shall have the `:dragging` pseudo-class, and it shall be removed when the drag ends, whatever its outcome.
  - Tests: `DragSessionTests.TheDragVisualIsDraggingUntilTheDragEndsWhateverTheOutcome`, `CollectionDragTests.EveryRealizedDraggedContainerIsDragging`

### Preview

- **DD-SESSION-002** — When `ShowDragPreview` is true and the drag visual has an adorner layer and a size of at least 1×1 DIP, the session shall add a snapshot of the drag visual to that adorner layer.
  - Tests: `DragSessionTests.ThePreviewIsAddedToTheSourcesAdornerLayerWhenShown`, `DragSessionTests.AVisualWithoutSizeHasNoPreview`
- **DD-SESSION-003** — The snapshot shall be taken before `:dragging` is applied, so it shows the visual's normal look.
  - Tests: `DragSessionTests.TheSnapshotShowsTheVisualsNormalLook`
- **DD-SESSION-004** — The snapshot shall be rendered at the window's render scaling, and keep the visual's corner radius when the visual is a `Border` or a templated control.
  - Tests: `DragSessionTests.TheSnapshotUsesTheRenderScalingAndKeepsABordersCornerRadius`, `DragSessionTests.TheSnapshotKeepsATemplatedControlsCornerRadius`
- **DD-SESSION-005** — The preview shall paint the nearest ancestor's opaque background behind the snapshot (a solid brush with full alpha and opacity, or any non-solid brush), so transparent drag visuals stay legible.
  - Tests: `DragSessionTests.ATransparentVisualShowsTheNearestOpaqueBackgroundBehindIt`, `DragSessionTests.ANonSolidBackgroundCountsAsOpaque`
- **DD-SESSION-006** — The preview shall be hidden until the drag first enters or moves over the source's window, then follow the pointer with the press point under it.
  - Tests: `DragSessionTests.ThePreviewIsHiddenUntilTheDragReachesTheWindowThenFollowsThePointer`
- **DD-SESSION-007** — When the drag leaves the window or is dropped, the preview shall be hidden; when the drag ends, it shall be removed from the adorner layer.
  - Tests: `DragSessionTests.ThePreviewHidesWhenTheDragLeavesTheWindow`, `DragSessionTests.ThePreviewHidesOnDropAndLeavesTheLayer`, `DragSessionTests.EndingTheDragDisposesThePreview`
- **DD-SESSION-015** — When the drag ends, the session shall dispose the preview, releasing its snapshot bitmap.
  - Tests: `DragSessionTests.EndingTheDragDisposesThePreview`
- **DD-SESSION-008** — While the pointer is over the window, the preview shall have `:nodrop` when the current effect is None, `:copy` when it is Copy and the drop would not remove the payload, and `:remove` when dropping there would remove the payload.
  - Tests: `DragSessionTests.ThePreviewShowsWhetherTheDropWouldCopyOrRemove`, `DragSessionTests.ThePreviewShowsNoDropWhereNothingAcceptsTheDrag`

- **DD-SESSION-016** — When a drag carries more than one payload, the preview shall have the `:multiple` pseudo-class, report the count in `PayloadCount`, and show it in a badge over its top-right corner.
  - Tests: `DragSessionTests.APreviewOfSeveralPayloadsShowsTheirCount`

### Watching the window

- **DD-SESSION-009** — When a preview is shown or `RemoveOnDropOutside` is true, and the window does not already set `AllowDrop`, the session shall set it for the drag's duration and clear it afterwards.
  - Tests: `DragSessionTests.TheSessionAllowsDropsOnTheWindowOnlyForTheDrag`, `DragSessionTests.AWindowThatAlreadyAllowsDropsKeepsIt`
- **DD-SESSION-010** — While the pointer is over a part of the window where no element up to the window sets `AllowDrop` explicitly, and the session set the window's `AllowDrop`, the session shall report effect None, or, when `RemoveOnDropOutside` is true, Move if allowed, else Copy, else Link.
  - Tests: `DragSessionTests.WhereNothingAllowsDropsTheSessionReportsNoneOrTheRemoveEffect`, `DragSessionTests.OverAnElementThatAllowsDropsTheSessionLeavesTheEffectAlone`
- **DD-SESSION-011** — When the drag is dropped on such a part of the window and `RemoveOnDropOutside` is true, the session shall report the drop as outside any target (DD-DRAG-021).
  - Tests: `DragSessionTests.ADropWhereNothingAllowsDropsIsOutsideAnyTargetWithRemoveOnDropOutside`, `DragSessionTests.ADropOnATargetIsNotOutside`
- **DD-SESSION-012** — Drops outside the window and canceled drags shall never count as outside any target.
  - Tests: `DragSessionTests.ACanceledDragOrADropOutsideTheWindowIsNeverOutsideAnyTarget`
- **DD-SESSION-013** — Dropping an order slot's item on the window background shall clear the slot, and dropping it on the desktop or pressing Esc shall not.
  - Tests: manual — Win32, `DragDropSample/SingleItemPage` (slots use `RemoveOnDropOutside`); repeat for the Desktop page's file list. Checked 2026-11-25.

### Preview defaults

- **DD-SESSION-014** (inherited) — The preview shall not be hit-testable, shall have opacity 0.9, a `0 8 20 0 #40000000` box shadow and the highest z-index in its layer.
  - Tests: `DragSessionTests.ThePreviewIsPassiveTranslucentShadowedAndOnTop`
- **DD-SESSION-017** (inherited) — The count badge shall be 20 DIPs tall with fully rounded ends, the accent color (DD-ADORNERS-019) as its fill and white 12 DIP semibold text, and overhang the preview's corner by 8 DIPs.
  - Tests: `DragSessionTests.APreviewOfSeveralPayloadsShowsTheirCount`

## Invariants

- After the drag ends, the drag visual has no `:dragging`, the preview is out of the adorner layer, the window handlers are removed and the window's `AllowDrop` is as it was.

## Edge cases

- When the app already sets `AllowDrop` on the window, the session does not inject it and treats nothing as outside a target (DD-SESSION-010), so `RemoveOnDropOutside` never removes and the preview never shows `:remove`.
- A drag visual with no adorner layer (not in a visual tree with one) gets no preview, but still gets `:dragging`.
- The preview lives only in the source window. Over other windows and other applications, only the platform cursor shows.
- The preview's snapshot is static: changes to the drag visual during the drag do not appear.
- A drag of several payloads previews only the pressed container, with the count badge; the other dragged containers show only `:dragging`.

## Threading and lifetime

- UI thread only. The session lives exactly as long as `DoDragDropAsync`: created before it, disposed by `using` after it returns or throws.
- The session disposes the preview when the drag ends (DD-SESSION-015), which releases its `RenderTargetBitmap`. A disposed preview shows nothing; a caller of the public `DragPreviewAdorner.Create` owns the preview and disposes it.

## Platform behavior

- On Win32, the platform raises DragLeave instead of Drop when the last DragOver returned None. DD-SESSION-010 returns a non-None effect over non-target areas when tracking drops, so the Drop arrives.
- On Win32, a drop outside the window and an Esc cancel both make `DoDragDropAsync` return None, which is why neither removes (DD-SESSION-012).

## Decisions

- **The preview is an adorner, not the OS drag image.** Reason: Avalonia exposes no cross-platform drag image API. An adorner can be styled with pseudo-classes and matches the app's look. Rejected: native drag images per platform.
- **Removing on drop outside is limited to the source window.** Reason: outside the window the library cannot tell a drop on the desktop from a cancel (Platform behavior), and removing on cancel would lose data.
- **The preview owns its snapshot.** `DragPreviewAdorner` implements `IDisposable` and disposes its bitmap. Reason: the adorner creates the bitmap in `Create`, so it is the natural owner, and `IDisposable` tells other callers of `Create` to release it. Rejected: an internal release method called only by the session, which leaks for any other caller of the public `Create`.
- **The session only injects `AllowDrop` when the window lacks it.** Reason: changing an app's own `AllowDrop` value would change which drops the app accepts.

## Non-goals

- A preview that follows the pointer outside the source window.
- Animating the preview back to the source on cancel.

## Open items

- An app that sets `AllowDrop` on its window silently disables `RemoveOnDropOutside` (Edge cases). Logged in the backlog.
