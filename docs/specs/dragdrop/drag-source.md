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
tests:
  - DragGestureTests
depends:
  - DD-WIRING
  - DD-TRANSFER
  - DD-SESSION
---

# Drag source

## Purpose

`DragOperationBase` turns pointer input on its attached control into an Avalonia drag-and-drop operation: it decides which presses may start a drag, waits for the pointer to pass a threshold, resolves the payload and builds the data transfer (DD-TRANSFER), runs the drag inside a drag session (DD-SESSION) and, when the drop side reports back, removes the payload from its source through `DropCompleted`. The single-value, collection and tree drag operations (DD-ITEM, DD-COLLECTION, DD-TREE) specialize it. Also specifies how `DragDrop.IsDragHandle` and `DragDrop.IgnoresPress` (DD-WIRING) restrict presses.

## Requirements

### Subscribing

- **DD-DRAG-001** — When attached, the drag source shall handle the control's pointer pressed, moved and released events with the strategy in `Routing`, including handled events when `HandledEventsToo` is true.
  - Tests: untested
- **DD-DRAG-002** — When detached, the drag source shall remove those handlers.
  - Tests: untested
- **DD-DRAG-003** (inherited) — When `Routing` and `HandledEventsToo` are not set, the drag source shall use bubbling routing and ignore handled events.
  - Tests: untested
- **DD-DRAG-025** — When `Routing` or `HandledEventsToo` is set while the operation is attached, the drag source shall throw `InvalidOperationException`.
  - Tests: `DragGestureTests.SettingRoutingWhileAttachedThrows`, `DragGestureTests.RoutingCanBeSetWhileDetached`

### Tracking a press

- **DD-DRAG-004** — When the left button is pressed on the attached control, the drag source shall track the press as a possible drag, unless one of DD-DRAG-005 to DD-DRAG-008 applies.
  - Tests: untested
- **DD-DRAG-005** — While `CanDrag` is false, the drag source shall not track presses.
  - Tests: untested
- **DD-DRAG-006** — Walking from the pressed element up to the attached control, the drag source shall let the first element that decides choose whether the press is ignored, and shall not track an ignored press. An element decides when `DragDrop.IgnoresPress` is set on it (true ignores, false allows), or else when `IsIgnoredPressSource` is true for it (ignores). By default `IsIgnoredPressSource` is true for a `ScrollBar`, `Button`, `TextBox` or `Thumb`.
  - Tests: `DragGestureTests.AStyleCanIgnorePressesOnAControlType`, `DragGestureTests.IgnoresPressFalseLetsAnIgnoredControlStartDrags`, `DragGestureTests.TheElementNearestThePressDecides`, `DragGestureTests.PressesInsideATextBoxDoNotStartDrags`, `DragGestureTests.PressesOnASliderThumbDoNotStartDrags`, `DragGestureTests.ATextBoxInASelectedRowReceivesItsPress`, `DragGestureTests.OverridingIsIgnoredPressSourceAddsControls`, `DragGestureTests.ADragHandleInsideAButtonDoesNotStartADrag`
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
  - Tests: `DragGestureTests.ADragDoesNotStartTwiceFromOnePress`
- **DD-DRAG-014** — The drag source shall start the platform drag with the stored press event, not the move that crossed the threshold.
  - Tests: untested
- **DD-DRAG-015** — The drag source shall offer `AllowedEffects` to the platform drag.
  - Tests: untested
- **DD-DRAG-026** — The drag source's moved handler shall await `OnPointerMoved`, so an override can await before calling the base and the drag still starts.
  - Tests: `DragGestureTests.AnOnPointerMovedOverrideCanAwaitBeforeCallingTheBase`

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
- **DD-DRAG-027** (inherited) — When `DragDrop.IgnoresPress` is not set, it shall be null.
  - Tests: `DragGestureTests.IgnoresPressDefaultsToNull`

## Invariants

- At most one press is tracked at a time, and none while a drag the operation started is running.
- `DropCompleted` runs synchronously inside the drop target's Drop handler, before `DoDragDropAsync` returns to the drag source.

## Edge cases

- A press with a null payload stays tracked: every later move past the threshold resolves the payload again and returns, until the pointer is released.
- The press position and the threshold are measured relative to the attached control, so a control that moves under the pointer (for example during scrolling) changes the measured distance.
- An explicit `DragDrop.IgnoresPress` of false does not reach past a nearer element that decides: a `Button` inside an element set to false still keeps its presses.
- The ignore check runs before the drag-handle check, so a drag handle inside an ignored control (a handle placed in a `Button`) never starts a drag.
- Ignored presses (DD-DRAG-005 to 008) are left unhandled, so the pressed element and its ancestors handle them as usual.

## Threading and lifetime

- UI thread only. The private moved handler awaits `OnPointerMoved` on the UI thread, and returns to the input system while the platform drag is in progress.
- An exception thrown while the drag runs (including the `COMException` Win32 raises when a drop handler throws) escapes the private `async void` moved handler and goes to the app's unhandled-exception handling.

## Platform behavior

- On Win32, Avalonia runs the OLE drag loop, which reports exceptions from drop handlers as `E_FAIL`. That surfaces as a `COMException` from `DoDragDropAsync`, and the original exception is visible only as a first-chance exception in the debugger.

## Decisions

- **Presses on unselected items stay unhandled.** Reason: `SelectingItemsControl.UpdateSelectionFromEvent` ignores handled events, so handling the press would make the user click twice (once to select, once to drag). Presses on selected items are handled so a multiple selection is not collapsed to one item. Rejected: always leaving presses unhandled, which loses multiple selection on every drag start.
- **More controls keep their presses.** Scroll bars, buttons, text boxes and thumbs keep their presses by default. Reason: a drag source on an items control would otherwise swallow scroll bar drags and the tree expander's toggle; a selected row's press is handled in the tunneling phase (DD-DRAG-009), so a `TextBox` in it can't focus and a selection drag turns into an item drag; a slider's thumb starts an item drag the same way. An app that drags rows by a read-only `TextBox` sets `DragDrop.IgnoresPress` to false on it. Rejected: opt-in only, which leaves editable rows broken by default.
- **The press filter is a per-element predicate.** The base owns the walk up to the attached control; `DragDrop.IgnoresPress`, then `IsIgnoredPressSource`, answers for one element. Reason: overrides and styles add a type in one expression and cannot get the walk's stopping point wrong. Rejected: a virtual that receives the pressed element and walks itself, which every override would have to repeat.
- **Ignored presses are configured by a non-inherited attached property.** `DragDrop.IgnoresPress` is a `bool?` read during the walk, like `DragDrop.IsDragHandle`, and the nearest element with a value decides. Reason: apps add control types with a style and opt single elements out in XAML, without subclassing an operation, and the walk keeps the boundary at the attached control. Rejected: an inherited property, checked only on the pressed element. Avalonia does not inherit per-type default values, so the defaults would need app-wide styles that every consumer adds, or the library setting values on unrelated controls. Values inherited from above the attached control (a drag source inside a `Button` or an `Expander` header) would also ignore every press, unless the operation overwrote a local value on its control.
- **An explicit null `Payload` means nothing to drag.** Reason: a bound slot that is empty (an empty order slot on the single-item page) must not fall back to its `DataContext` and drag the whole view model.
- **Subclasses get hooks, not fields.** Every protected member is a virtual method or a read-only property with a documented meaning. Reason: protected fields freeze the implementation, and the former ones let a subclass leave the press-tracking state inconsistent. The press origin, their only subclass reader, is a parameter of `CreateMetadata`.
- **Pointer hooks, not event handlers.** The subscribed handlers are private, and subclasses override `OnPointerPressed`, `OnPointerReleased` and a `Task`-returning `OnPointerMoved`. Reason: `Control_*` was designer-handler naming for a public extension point, `sender` is always the attached control, and a virtual `async void` forces overrides to be `async void` too, whose exceptions bypass the library's handling and crash the app. Rejected: keeping the handlers virtual and only renaming them (an override still decides whether the exception handling runs).
- **Routing options are settable only while detached.** `Routing` and `HandledEventsToo` are read when `Attach` subscribes, so a later change would silently do nothing. Throwing makes the mistake visible, and XAML sets both before the attached property attaches the operation. Rejected: a protected constructor parameter (XAML users could not change the routing of a built-in operation); re-subscribing on change (state for a case nobody needs).

## Non-goals

- Touch- or pen-specific thresholds.
- Dragging with a button other than the left one.
- Keyboard-initiated drags.

## Open items

- A pointer move with no button pressed can start a drag from a stale press, because moves don't check the button and `PointerCaptureLost` is not handled. Not yet reproduced. Addressed by [changes/correctness-fixes.md](../../changes/correctness-fixes.md).
- Exceptions during a drag crash the app through the `async void` handler (Threading and lifetime). Addressed by [changes/correctness-fixes.md](../../changes/correctness-fixes.md).
