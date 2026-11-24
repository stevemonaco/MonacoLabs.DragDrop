---
id: DD-DROP
title: Drop target
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop.Abstractions
sources:
  - MonacoLabs.DragDrop/Abstractions/DropOperationBase.cs
  - MonacoLabs.DragDrop/Abstractions/DropOperationBase.props.cs
types:
  - DropOperationBase
tests: []
depends:
  - DD-WIRING
  - DD-TRANSFER
  - DD-EFFECTS
  - DD-PLATFORM
  - DD-ADORNERS
---

# Drop target

## Purpose

`DropOperationBase` makes its attached control accept drags: it decides whether a drag is meant for it, validates it through the overridable `CanDrop`, shows and updates its drop adorner (DD-ADORNERS), sets `:dropover`, picks the effect (DD-EFFECTS) and, on drop, assigns `PayloadTarget`, reports back to the drag source and runs `PayloadCommand`. The drop flow specified here is the single-value flow `ItemDropOperation` uses unchanged (DD-ITEM). Collection and tree targets replace the event handling but keep the targeting, validation and helper methods (DD-COLLECTION, DD-TREE).

## Requirements

### Attaching

- **DD-DROP-001** — When attached, the drop target shall set `AllowDrop` on the control and handle its DragEnter, DragOver, DragLeave and Drop events with the strategy in `Routing`.
  - Tests: untested
- **DD-DROP-002** — When attached with no `DropAdorner`, the drop target shall create a solid `DropHighlightAdorner`. In either case it shall point the adorner's `TargetControl` at the attached control.
  - Tests: untested
- **DD-DROP-003** — When detached, the drop target shall clear `AllowDrop` on the control and remove its handlers.
  - Tests: untested

### Targeting

- **DD-DROP-004** — The drop target shall treat a drag as meant for it when the drag carries data for one of its `InteractionIds` (an in-process payload, or platform data for a platform id) and, unless the target allows self-drops, the drag did not start from the same control.
  - Tests: untested
- **DD-DROP-005** — When a drag is not meant for the target, the drop target shall report effect None on DragEnter, DragOver and Drop and shall not show its adorner.
  - Tests: untested
- **DD-DROP-006** — A single-value drop target shall not allow self-drops.
  - Tests: untested

### Validation

- **DD-DROP-007** — Before each `CanDrop` evaluation, the drop target shall clear `DropAdorner.ErrorMessage`, so a message set by an override reflects only the latest evaluation.
  - Tests: untested
- **DD-DROP-008** — The drop target shall evaluate `CanDrop` on every DragEnter, DragOver and Drop of a drag meant for it.
  - Tests: untested
- **DD-DROP-009** — When `CanDrop` is not overridden, the drop target shall accept every drag meant for it.
  - Tests: untested

### Hover feedback

- **DD-DROP-010** — When a drag meant for the target enters, the drop target shall show its adorner with `IsDropValid` equal to the `CanDrop` result, so a rejected drag shows the adorner in its error state.
  - Tests: untested
- **DD-DROP-011** — While a drag meant for the target is over it, the drop target shall set `:dropover` on the attached control exactly when `CanDrop` is true, and update the adorner's `IsDropValid` on each DragOver.
  - Tests: untested
- **DD-DROP-012** — While `CanDrop` is false, the drop target shall report effect None; otherwise it shall report the effect DD-EFFECTS picks.
  - Tests: untested
- **DD-DROP-013** — When DragLeave is raised, the drop target shall defer the leave and, if no DragEnter or Drop arrives on the target before the deferred work runs, clear `:dropover` and hide its adorner.
  - Tests: untested

### Dropping

- **DD-DROP-014** — When a drag meant for the target is dropped and `CanDrop` is false, the drop target shall report effect None and clear its hover feedback without changing anything else.
  - Tests: untested
- **DD-DROP-015** — When an in-process payload is dropped and accepted, the drop target shall recompute the effect, set `PayloadTarget` to the payload (whatever the effect), call the drag operation's `DropCompleted` with that effect and a `DropInfo` whose `ReplacedPayload` is the previous `PayloadTarget`, and then run `PayloadCommand`.
  - Tests: untested
- **DD-DROP-016** — When platform data is dropped and accepted, the drop target shall leave `PayloadTarget` unchanged, not call `DropCompleted`, and run `PayloadCommand` with the files or text and `IsPlatformDrop` set (DD-PLATFORM).
  - Tests: untested
- **DD-DROP-017** — The drop target shall run `PayloadCommand` only when `CanExecute` returns true for the `DropEventArgs` it would receive.
  - Tests: untested
- **DD-DROP-018** — After an accepted drop, the drop target shall call `OnDrop`, clear `:dropover`, set the adorner invalid and hide it.
  - Tests: untested
- **DD-DROP-019** — `PayloadTarget` shall bind TwoWay by default, so a drop writes back to the bound view-model property.
  - Tests: untested

### Helpers for overrides

- **DD-DROP-020** — `TryGetPayload<T>` shall succeed when the in-process payload (DD-TRANSFER-007) is a `T`.
  - Tests: untested
- **DD-DROP-021** — `TryGetDragInfo<T>` shall succeed when the drag's `DragInfo` is a `T`.
  - Tests: untested
- **DD-DROP-022** — `TryGetFiles` shall succeed only when `PlatformInteractionIds.Files` is among the target's ids and the drag holds at least one storage item; `TryGetText` only when `PlatformInteractionIds.Text` is among them and the drag holds text.
  - Tests: untested

## Invariants

- `PayloadTarget` changes only on an accepted in-process drop.
- `DropCompleted` is called at most once per drop, and only for an accepted in-process drop.
- Once a drop or an undisturbed leave has been processed, the attached control has no `:dropover` and the adorner is hidden.

## Edge cases

- A drag rejected by DD-DROP-004 never shows an adorner, even an error one. Only `CanDrop` rejections show the error state.
- The adorner's `ErrorMessage` is set by `CanDrop` overrides through `DropAdorner!`. A target whose `DropAdorner` was set to null cannot show messages, and overrides that use `!` throw.
- `PayloadTarget` is set on Copy and Link drops too; the effect only changes what the drag side does.
- The deferred leave captures the leaving event's args and runs `OnDragLeave` with them after the event has finished.

## Threading and lifetime

- UI thread only. The deferred leave (DD-DROP-013) is posted to the UI dispatcher.
- Detach does not cancel a pending deferred leave. If one runs after Detach, `OnDragLeave` dereferences the now-null `AttachedControl` and throws.

## Platform behavior

- Avalonia's Win32 layer swallows exceptions thrown from these handlers and returns `E_FAIL` (see DD-DRAG Platform behavior).
- Moving between child elements raises DragLeave then DragEnter in the same pass on every platform Avalonia supports, which is why the leave is deferred.

## Decisions

- **Targeting and validation are separate steps.** `IsDropTargetFor` decides whether the target is involved at all, and `CanDrop` decides whether this particular drop is valid. Reason: a drag meant for another target should leave this one visually untouched, while a meant-for-me drag that fails validation should explain itself with the error adorner.
- **DragLeave is deferred.** Reason: Avalonia raises DragLeave then DragEnter when the pointer crosses into a child, and leaving immediately makes the adorner flicker. Rejected: hit testing on each leave to see whether the pointer is still inside.
- **The effect is recomputed on Drop.** Reason: on Drop, `e.DragEffects` arrives as the allowed set, not the effect returned by the last DragOver. Rejected: caching the last DragOver effect, which misses a modifier pressed right before release.

## Non-goals

- Accepting a drop without an interaction id match.
- Transforming the payload on the way in. `TryGetPayload` is virtual for that, but no built-in target overrides it.

## Open items

- Detach leaves a pending deferred leave able to throw, and leaves `:dropover`, the adorner and `DropAdorner.TargetControl` in place. Addressed by [changes/correctness-fixes.md](../../changes/correctness-fixes.md).
- `Context` is a public property that nothing reads. Addressed by [changes/api-design.md](../../changes/api-design.md).
- `DropAdornerType` is protected with no way to set it from XAML, so `DropAdornerKind.Border` and `None` are reachable only by constructing the adorner. Addressed by [changes/api-design.md](../../changes/api-design.md).
- Rejecting with a message needs `DropAdorner!.ErrorMessage` in every override. Addressed by [changes/api-design.md](../../changes/api-design.md).
- The protected `_handledEventsToo` field is part of the public API surface. Addressed by [changes/api-design.md](../../changes/api-design.md).
- Subclasses can override either the subscribed handlers (`DragEnter`, `DragOver`, `DragLeave`, `Drop`) or the hooks they call (`OnDragEnter`, `OnDragOver`, `OnDragLeave`, `OnDrop`). Collection targets replace the handlers, so which hooks run depends on the base class (see DD-COLLECTION Open items). Logged in the backlog (drop-side redesign).
