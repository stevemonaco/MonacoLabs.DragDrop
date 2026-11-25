---
id: DD-ITEM
title: Single-value operations
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop
sources:
  - MonacoLabs.DragDrop/Operations/ItemDragOperation.cs
  - MonacoLabs.DragDrop/Operations/ItemDragOperation.props.cs
  - MonacoLabs.DragDrop/Operations/ItemDropOperation.cs
types:
  - ItemDragOperation
  - ItemDropOperation
tests: []
depends:
  - DD-DRAG
  - DD-DROP
  - DD-EFFECTS
---

# Single-value operations

## Purpose

`ItemDragOperation` and `ItemDropOperation` move one value between slots, such as a meal between guest orders on the single-item page. The drop side replaces its `PayloadTarget` with the payload (DD-DROP-015). The drag side then clears its bound `Payload` on a Move, or swaps in the target's previous value. `ItemDropOperation` adds nothing to DD-DROP. It exists so XAML has a concrete single-value target to instantiate and subclass.

## Requirements

### Drag side

- **DD-ITEM-001** — `ItemDragOperation.Payload` shall bind TwoWay by default.
  - Tests: untested
- **DD-ITEM-002** — When a drop completes with effect Move and `Payload` is set or bound, the drag operation shall set `Payload` to null.
  - Tests: untested
- **DD-ITEM-003** — When a drop completes with effect Move, `Payload` is set or bound and `SwapOnDrop` is true, the drag operation shall set `Payload` to the drop's `ReplacedPayload` instead of null.
  - Tests: untested
- **DD-ITEM-004** — When a drop completes with any effect other than Move, the drag operation shall leave `Payload` unchanged.
  - Tests: untested
- **DD-ITEM-005** — When `Payload` is neither set nor bound (the payload came from `DataContext`), the drag operation shall change nothing on completion.
  - Tests: untested
- **DD-ITEM-006** — When `RemoveOnDropOutside` is true, `Payload` is set or bound and the drag is dropped outside any target (DD-SESSION), the drag operation shall set `Payload` to null.
  - Tests: untested
- **DD-ITEM-007** — The drag operation shall write `Payload` with `SetCurrentValue`, so a TwoWay binding pushes the new value to the view model and stays in place.
  - Tests: untested

### Drop side

- **DD-ITEM-008** — `ItemDropOperation` shall behave exactly as DD-DROP specifies, including refusing drags that started from its own attached control.
  - Tests: untested

### Defaults

- **DD-ITEM-009** (inherited) — When `SwapOnDrop` is not set, it shall be false.
  - Tests: untested

## Invariants

- After a Move between two `Payload`-bound slots, the value is in exactly one of them, or, with `SwapOnDrop`, the two values have traded places.

## Edge cases

- `SwapOnDrop` assigns the replaced value without validating it against the source. A source restricted to meals can receive a drink by swap.
- A slot dropped on itself would replace the value with itself and then clear it. DD-DROP-006 blocks this.
- With a source allowing only Copy (the menu), the source slot never changes.

## Threading and lifetime

- UI thread only, inside the drop target's Drop handler.

## Platform behavior

- No platform-specific behavior.

## Decisions

- **The drag side clears or swaps; the drop side only assigns.** Reason: the source knows whether its value is a bound slot (clear it) or a template's `DataContext` (leave it alone). See DD-COLLECTION, "The drag side removes the payload".
- **Swapping is opt-in.** Reason: in most slot-to-slot UIs the target's previous value is discarded, and swapping silently moves a value the user did not drag.

## Non-goals

- Validating swapped values.
- Single-value targets that merge rather than replace.

## Open items

- None.
