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
tests:
  - ItemDropTests
  - DropTargetTests
  - EndToEndTests
depends:
  - DD-DRAG
  - DD-DROP
  - DD-EFFECTS
---

# Single-value operations

## Purpose

`ItemDragOperation` and `ItemDropOperation` move one value between slots, such as a meal between guest orders on the single-item page. The drop side runs the DD-DROP flow, and its transfer step replaces `PayloadTarget` with the payload; a drag carrying several payloads is not meant for it. The drag side then clears its bound `Payload` on a Move, or swaps in the target's previous value.

## Requirements

### Drag side

- **DD-ITEM-001** — `ItemDragOperation.Payload` shall bind TwoWay by default.
  - Tests: `EndToEndTests.ItemPayloadBindsTwoWayByDefault`
- **DD-ITEM-002** — When a drop completes with effect Move and `Payload` is set or bound, the drag operation shall set `Payload` to null.
  - Tests: `EndToEndTests.AMoveClearsASetPayload`
- **DD-ITEM-003** — When a drop completes with effect Move, `Payload` is set or bound and `SwapOnDrop` is true, the drag operation shall set `Payload` to the drop's `ReplacedPayload` instead of null.
  - Tests: `EndToEndTests.WithSwapOnDropAMoveTakesTheTargetsPreviousValue`
- **DD-ITEM-004** — When a drop completes with any effect other than Move, the drag operation shall leave `Payload` unchanged.
  - Tests: `EndToEndTests.AnyEffectOtherThanMoveLeavesThePayload`
- **DD-ITEM-005** — When `Payload` is neither set nor bound (the payload came from `DataContext`), the drag operation shall change nothing on completion.
  - Tests: `EndToEndTests.ADataContextPayloadIsLeftAlone`
- **DD-ITEM-006** — When `RemoveOnDropOutside` is true, `Payload` is set or bound and the drag is dropped outside any target (DD-SESSION), the drag operation shall set `Payload` to null.
  - Tests: `EndToEndTests.ADropOutsideAnyTargetClearsASetPayload`
- **DD-ITEM-007** — The drag operation shall write `Payload` with `SetCurrentValue`, so a TwoWay binding pushes the new value to the view model and stays in place.
  - Tests: `EndToEndTests.AMoveKeepsTheBindingOnPayload`

### Drop side

- **DD-ITEM-008** — `ItemDropOperation` shall follow the DD-DROP flow, refuse drags that started from its own attached control, and transfer by setting `PayloadTarget` to the payload, or its copy on a Copy drop, reporting the previous value as `ReplacedPayload`.
  - Tests: `DropTargetTests.AnAcceptedDropAssignsThePayloadCompletesTheDragThenRunsTheCommand`, `DropTargetTests.ACopyDropAssignsTheCreatedCopy`, `DropTargetTests.ADropWritesPayloadTargetBackThroughADefaultBinding`, `ItemDropTests.ASlotDroppedOnItselfKeepsItsValue`
- **DD-ITEM-010** — Unless `AcceptsMultiplePayloads` is set to true, `ItemDropOperation` shall not be meant for a drag with more than one payload (DD-DROP-035): it shows no adorner and reports effect None. When it is true, the first payload shall be assigned to `PayloadTarget`.
  - Tests: `ItemDropTests.ASingleValueTargetRefusesSeveralPayloads`, `ItemDropTests.AnItemTargetThatAcceptsSeveralPayloadsTakesTheFirst`, `DropTargetTests.AcceptsMultiplePayloadsDefaultsToTrueExceptOnItemTargets`

### Defaults

- **DD-ITEM-009** (inherited) — When `SwapOnDrop` is not set, it shall be false.
  - Tests: `EndToEndTests.SwapOnDropDefaultsToFalse`

## Invariants

- After a Move between two `Payload`-bound slots, the value is in exactly one of them, or, with `SwapOnDrop`, the two values have traded places.

## Edge cases

- `SwapOnDrop` assigns the replaced value without validating it against the source. A source restricted to meals can receive a drink by swap.
- A slot dropped on itself would replace the value with itself and then clear it. DD-DROP-006 blocks this.
- With a source allowing only Copy (the menu), the source slot never changes.
- `PayloadTarget` is set on Copy and Link drops too; the effect only changes what the drag side does.

## Threading and lifetime

- UI thread only, inside the drop target's Drop handler.

## Platform behavior

- No platform-specific behavior.

## Decisions

- **The drag side clears or swaps; the drop side only assigns.** Reason: the source knows whether its value is a bound slot (clear it) or a template's `DataContext` (leave it alone). See DD-COLLECTION, "The drag side removes the payload".
- **`ItemDropOperation` assigns `PayloadTarget`.** Assignment is its transfer step (DD-DROP, "Transfer is a step, not a handler"), not part of `DropOperationBase`, whose direct subclasses are command-only targets.
- **Single-value targets refuse several payloads.** Reason: assigning one of them would lose the rest, and on a Move the source would remove them all. The refusal is targeting, not validation: a slot is not a place for several values at all. Rejected: assigning the first; a validation message.
- **Swapping is opt-in.** Reason: in most slot-to-slot UIs the target's previous value is discarded, and swapping silently moves a value the user did not drag.

## Non-goals

- Validating swapped values.
- Single-value targets that merge rather than replace.

## Open items

- None.
