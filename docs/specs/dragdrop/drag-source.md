---
id: DD-DRAG
title: Drag source
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop.Abstractions
sources:
  - MonacoLabs.DragDrop/Abstractions/DragOperationBase.cs
  - MonacoLabs.DragDrop/Abstractions/DragOperationBase.props.cs
types:
  - DragOperationBase
tests: []
depends:
  - DD-WIRING
  - DD-TRANSFER
  - DD-SESSION
---

# Drag source

## Purpose

`DragOperationBase` turns pointer input on its attached control into an Avalonia drag-and-drop operation: it decides which presses may start a drag, waits for the pointer to pass a threshold, resolves the payload and builds the data transfer (DD-TRANSFER), runs the drag inside a drag session (DD-SESSION) and, when the drop side reports back, removes the payload from its source through `DropCompleted`. The single-value, collection and tree drag operations (DD-ITEM, DD-COLLECTION, DD-TREE) specialize it. Also specifies how `DragDrop.IsDragHandle` (DD-WIRING) restricts presses.

## Requirements

### Subscribing

- **DD-DRAG-001** — When attached, the drag source shall handle the control's pointer pressed, moved and released events with the strategy in `Routing`, including handled events only when the subclass opts in.
  - Tests: untested
- **DD-DRAG-002** — When detached, the drag source shall remove those handlers.
  - Tests: untested
- **DD-DRAG-003** (inherited) — When `Routing` is not set, the drag source shall use bubbling routing and ignore handled events.
  - Tests: untested

### Tracking a press

- **DD-DRAG-004** — When the left button is pressed on the attached control, the drag source shall track the press as a possible drag, unless one of DD-DRAG-005 to DD-DRAG-008 applies.
  - Tests: untested
- **DD-DRAG-005** — While `CanDrag` is false, the drag source shall not track presses.
  - Tests: untested
- **DD-DRAG-006** — When the pressed element or any ancestor up to the attached control is a `ScrollBar` or a `Button`, the drag source shall not track the press, so the scroll bar or button (such as a tree expander) keeps working.
  - Tests: untested
- **DD-DRAG-007** — When `RequiresDragHandle` is true and neither the pressed element nor any ancestor up to the attached control has `DragDrop.IsDragHandle` set to true, the drag source shall not track the press.
  - Tests: untested
- **DD-DRAG-008** — When a press other than the left button arrives, the drag source shall ignore it.
  - Tests: untested
- **DD-DRAG-009** — When the drag source tracks a press, it shall mark the press handled unless the nearest `ISelectable` at or above the pressed element (up to the attached control) is unselected, so an unselected item selects itself in the same gesture and an existing multiple selection survives a press on a selected item.
  - Tests: untested
- **DD-DRAG-010** — When the pointer is released, the drag source shall stop tracking the press.
  - Tests: untested

### Starting a drag

- **DD-DRAG-011** — While a press is tracked, when the pointer moves at least `DragThreshold` DIPs (Euclidean distance) from the press position, the drag source shall start a drag.
  - Tests: untested
- **DD-DRAG-012** — While the pointer is within `DragThreshold` of the press position, the drag source shall not start a drag.
  - Tests: untested
- **DD-DRAG-013** — When a drag starts, the drag source shall stop tracking the press before the drag runs, so moves during the drag do not start another.
  - Tests: untested
- **DD-DRAG-014** — The drag source shall start the platform drag with the stored press event, not the move that crossed the threshold.
  - Tests: untested
- **DD-DRAG-015** — The drag source shall offer `AllowedEffects` to the platform drag.
  - Tests: untested

### Payload

- **DD-DRAG-016** — When `Payload` is set or bound, the drag source shall drag its value, even when that value is null; otherwise it shall drag the attached control's `DataContext`.
  - Tests: untested
- **DD-DRAG-017** — When the resolved payload is null, the drag source shall not start a drag.
  - Tests: untested
- **DD-DRAG-018** — When `PlatformInteractionIds.Text` is among `InteractionIds`, the drag source shall add the text `ResolvePlatformText` returns, which by default is the payload itself when it is a string and otherwise its `ToString()`.
  - Tests: untested
- **DD-DRAG-019** — When `PlatformInteractionIds.Files` is among `InteractionIds`, the drag source shall add the storage item `ResolvePlatformFile` returns, which by default is the payload when it is an `IStorageItem` and nothing otherwise.
  - Tests: untested

### Completion

- **DD-DRAG-020** — When the drop side calls `DropCompleted`, the base drag source shall do nothing; removing the payload from its source is each subclass's job (DD-ITEM, DD-COLLECTION).
  - Tests: untested
- **DD-DRAG-021** — When `RemoveOnDropOutside` is true and the drag session reports a drop outside any target (DD-SESSION), the drag source shall call `OnDroppedOutside` after the platform drag returns.
  - Tests: untested

### Defaults

- **DD-DRAG-022** (inherited) — When `DragThreshold` is not set, the threshold shall be 6 DIPs.
  - Tests: untested
- **DD-DRAG-023** (inherited) — When `AllowedEffects` is not set, the drag source shall allow Move only.
  - Tests: untested
- **DD-DRAG-024** (inherited) — When `CanDrag` is not set, it shall be true; `RequiresDragHandle` and `RemoveOnDropOutside` shall be false; `ShowDragPreview` shall be true.
  - Tests: untested

## Invariants

- At most one press is tracked at a time, and none while a drag the operation started is running.
- `DropCompleted` runs synchronously inside the drop target's Drop handler, before `DoDragDropAsync` returns to the drag source.

## Edge cases

- A press with a null payload stays tracked: every later move past the threshold resolves the payload again and returns, until the pointer is released.
- `CreateMetadata` reads the press origin, which is set only while a press is tracked; a subclass calling it outside `StartDrag` fails.
- The press position and the threshold are measured relative to the attached control, so a control that moves under the pointer (for example during scrolling) changes the measured distance.
- Ignored presses (DD-DRAG-005 to 008) are left unhandled, so the pressed element and its ancestors handle them as usual.

## Threading and lifetime

- UI thread only. `Control_PointerMoved` is `async void`: the drag runs as a continuation on the UI thread, and the handler returns while the platform drag is in progress.
- An exception thrown while the drag runs (including the `COMException` Win32 raises when a drop handler throws) escapes the `async void` handler and goes to the app's unhandled-exception handling.

## Platform behavior

- On Win32, Avalonia runs the OLE drag loop, which reports exceptions from drop handlers as `E_FAIL`. That surfaces as a `COMException` from `DoDragDropAsync`, and the original exception is visible only as a first-chance exception in the debugger.

## Decisions

- **Presses on unselected items stay unhandled.** Reason: `SelectingItemsControl.UpdateSelectionFromEvent` ignores handled events, so handling the press would make the user click twice (once to select, once to drag). Presses on selected items are handled so a multiple selection is not collapsed to one item. Rejected: always leaving presses unhandled, which loses multiple selection on every drag start.
- **Scroll bars and buttons keep their presses.** Reason: a drag source on an items control would otherwise swallow scroll bar drags and the tree expander's toggle.
- **An explicit null `Payload` means nothing to drag.** Reason: a bound slot that is empty (an empty order slot on the single-item page) must not fall back to its `DataContext` and drag the whole view model.

## Non-goals

- Touch- or pen-specific thresholds.
- Dragging with a button other than the left one.
- Keyboard-initiated drags.

## Open items

- `_isDragPending` is never set to true, so its checks are dead code. Addressed by [changes/correctness-fixes.md](../../changes/correctness-fixes.md).
- A pointer move with no button pressed can start a drag from a stale press, because moves don't check the button and `PointerCaptureLost` is not handled. Not yet reproduced. Addressed by [changes/correctness-fixes.md](../../changes/correctness-fixes.md).
- Exceptions during a drag crash the app through the `async void` handler (Threading and lifetime). Addressed by [changes/correctness-fixes.md](../../changes/correctness-fixes.md).
- The protected fields `_trackedControl`, `_dragOrigin`, `_pressedEventArgs`, `_isDragPending` and `_handledEventsToo` are part of the public API surface. Addressed by [changes/api-design.md](../../changes/api-design.md).
- The ignored-control list (`ScrollBar`, `Button`) is fixed; a `TextBox` inside a drag source loses its press. Addressed by [changes/api-design.md](../../changes/api-design.md).
