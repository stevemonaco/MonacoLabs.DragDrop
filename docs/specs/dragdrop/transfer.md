---
id: DD-TRANSFER
title: Interaction ids and the data transfer
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop
sources:
  - MonacoLabs.DragDrop/DragDropIds.cs
  - MonacoLabs.DragDrop/Info/DragInfo.cs
  - MonacoLabs.DragDrop/Info/DropInfo.cs
  - MonacoLabs.DragDrop/Info/DropEventArgs.cs
types:
  - DragInfo
  - DropInfo
  - DropEventArgs
tests:
  - InteractionIdTests
  - DropTargetTests
  - CollectionDropTests
  - EndToEndTests
depends:
  - DD-WIRING
---

# Interaction ids and the data transfer

## Purpose

Defines what a drag carries and how a drop target recognizes it. Interaction ids are named channels: a drag source advertises one or more, a drop target accepts a subset, and each payload travels once per id in an in-process `DataTransfer`, one item per payload, together with a `DragInfo`. A drag carries one payload, or several when it drags a selection (DD-COLLECTION, DD-TREE). `DropInfo` goes back to the drag source on drop, and `DropEventArgs` goes to the target's `PayloadCommand`. The drag side builds the transfer (DD-DRAG), and the drop side reads it (DD-DROP). The platform ids are in DD-PLATFORM.

## Requirements

### Interaction ids

- **DD-TRANSFER-001** (inherited) — When `InteractionIds` is not set on a drag or drop operation, it shall be the single id `app:default`.
  - Tests: `InteractionIdTests.DragOperationsDefaultToTheAppDefaultId`, `InteractionIdTests.DropOperationsDefaultToTheAppDefaultId`
- **DD-TRANSFER-002** — A drop target shall accept a drag when any one of its `InteractionIds` is present in the drag's data, regardless of the other ids the drag carries.
  - Tests: `EndToEndTests.AnyOneSharedIdIsEnough`, `DropTargetTests.ADragCarryingOneOfTheTargetsIdsIsMeantForIt`
- **DD-TRANSFER-003** — When `InteractionIds` is set from XAML as a comma-separated string, each id shall be one entry.
  - Tests: `EndToEndTests.CommaSeparatedIdsFromXamlAreSeparateEntries`

### Data transfer layout

- **DD-TRANSFER-004** — For each payload and each non-platform id in the drag's `InteractionIds`, the data transfer shall hold the payload under an in-process format named by that id, in that payload's item.
  - Tests: `EndToEndTests.TheDragsDataHoldsThePayloadUnderEachIdAndTheDragInfoInOneItem`, `EndToEndTests.SeveralPayloadsTravelOneItemEachInOrderWithTheDragInfoOnTheFirst`
- **DD-TRANSFER-005** — The data transfer shall hold the drag's `DragInfo` under the in-process format `app:dragmetadata`.
  - Tests: `EndToEndTests.TheDragsDataHoldsThePayloadUnderEachIdAndTheDragInfoInOneItem`
- **DD-TRANSFER-006** — The data transfer shall hold one item per payload, in payload order, with the drag's `DragInfo` on the first.
  - Tests: `EndToEndTests.TheDragsDataHoldsThePayloadUnderEachIdAndTheDragInfoInOneItem`, `EndToEndTests.SeveralPayloadsTravelOneItemEachInOrderWithTheDragInfoOnTheFirst`
- **DD-TRANSFER-007** — When a drop target reads the payloads, it shall take them, in item order, from the first of its own non-platform ids that has an in-process value, in the order of its `InteractionIds`.
  - Tests: `EndToEndTests.TheTargetReadsThePayloadOfItsFirstIdThatHasOne`

### DragInfo

- **DD-TRANSFER-008** — `DragInfo` shall carry the drag operation that created it, the press position relative to the drag source's attached control, a copy of the drag's interaction ids, the id of the pointer that pressed, and the payloads resolved when the drag started, in order (`Payloads`), with `Payload` the first of them.
  - Tests: `EndToEndTests.TheDragInfoDescribesTheDragWithACopyOfItsIds`, `EndToEndTests.SeveralPayloadsTravelOneItemEachInOrderWithTheDragInfoOnTheFirst`
- **DD-TRANSFER-009** — `DragInfo.DragIds` shall be a copy, so later changes to the operation's `InteractionIds` do not change it.
  - Tests: `EndToEndTests.TheDragInfoDescribesTheDragWithACopyOfItsIds`

### DropInfo

- **DD-TRANSFER-010** — `DropInfo` shall carry the drop's `DragEventArgs`, the hovered control, the pointer position relative to the drop target's attached control and, for single-value targets, the value the target held before the drop (`ReplacedPayload`).
  - Tests: `EndToEndTests.TheDropInfoDescribesTheDrop`

### DropEventArgs

- **DD-TRANSFER-011** — `DropEventArgs.Target` shall be the drop event's source element, and `TargetContext` shall be that element's `DataContext`, or null when the source is not a `StyledElement`.
  - Tests: `EndToEndTests.TheCommandSeesTheElementDroppedOnAndItsContext`
- **DD-TRANSFER-012** — `DropEventArgs.Items` shall hold every in-process payload of an in-process drop, in order (or their copies on a Copy drop, DD-DROP-026), or the dropped files or text for a platform drop (DD-PLATFORM).
  - Tests: `DropTargetTests.ACopyDropAssignsTheCreatedCopy`, `CollectionDropTests.ACopyDropInsertsTheCreatedCopy`, `CollectionDropTests.AMoveDropIgnoresCreateCopy`, `CollectionDropTests.SeveralPayloadsAreInsertedTogetherInOrder`, `CollectionDropTests.ACopyDropCopiesEachPayload`
- **DD-TRANSFER-013** — `DropEventArgs.Offset` shall be `OnTarget` for single-value targets, and the hovered offset for collection and tree targets (DD-COLLECTION).
  - Tests: `EndToEndTests.ASingleValueTargetDropsOnTargetWithNoLocation`, `CollectionDropTests.ATransferringDropAlsoRunsTheCommandWithThePayloadOffsetAndLocation`
- **DD-TRANSFER-014** — `DropEventArgs.Location` shall be the insertion point for collection and tree targets, and null for single-value targets.
  - Tests: `EndToEndTests.ASingleValueTargetDropsOnTargetWithNoLocation`, `CollectionDropTests.ATransferringDropAlsoRunsTheCommandWithThePayloadOffsetAndLocation`
- **DD-TRANSFER-015** — `DropEventArgs.Effect` shall be the effect computed on Drop (DD-EFFECTS-009), which for a platform drop is Copy or Link (DD-PLATFORM).
  - Tests: `DropTargetTests.DropEventArgsCarriesTheDropEffect`

## Invariants

- Every drag started by this library carries `app:dragmetadata`, so `TryGetDragInfo` succeeds for any in-process drag.
- A drag carries one or more payloads, and its data transfer holds one item per payload.

## Edge cases

- Two ids carrying the same payload is the normal case. A target that lists several of them reads the payloads once (DD-TRANSFER-007).- Ids are case-sensitive strings. The `app:` and `platform:` prefixes are conventions, except the two exact platform ids, which map to platform formats.
- A drag from another instance of the same app is not in-process: Avalonia's in-process formats do not cross process boundaries, so only its platform ids are visible.

## Threading and lifetime

- `DragInfo` and `DropInfo` hold references to controls and event args. They live as long as whoever keeps them. The library keeps neither after the drop returns.

## Platform behavior

- In-process formats never reach the OS. Only the platform ids (DD-PLATFORM) produce data other applications can read.
- Avalonia's Windows and X11 backends offer other applications a single item for every format except files, which is why platform text is carried once, on the first item (DD-DRAG-018), while files go one per item. In-process drops on Win32 receive the original `DataTransfer`, so every item reaches the target.

## Decisions

- **Interaction ids are typed channels.** One drag can advertise several ids, and each target accepts a subset. Reason: a source shared by several targets (the ListBox page's worker pool feeding three teams) needs to be told apart by the targets without a type check on the payload. Rejected: matching on payload type, which cannot distinguish two targets that both take `WorkerViewModel`.
- **`DropEventArgs` carries the effect.** Reason: a command that inserts the payload itself must know whether the source keeps its item (Copy) or loses it (Move), for example to clone on Copy. Rejected: leaving commands to recompute it from the modifier keys, which duplicates DD-EFFECTS and cannot see the source's allowed effects.
- **The payload is stored once per id.** Reason: `DataTransfer` looks up by format, and a target only knows its own ids. Rejected: one shared format plus an id list, which would make every target parse the list.
- **One `DataTransfer` item per payload.** Reason: it is how Avalonia and the platforms represent multi-item drags, so several files reach other applications as several files. Rejected: one item holding a list, which other applications cannot read and which breaks `TryGetPayload<T>` for every existing override.
- **`Payload` stays, as the first payload.** `DragInfo.Payload` and `TryGetPayload<T>` read the first of `Payloads`. Reason: single-item code and `CanDrop` overrides keep working unchanged. Rejected: removing them, which breaks every override for the common single-item case.

## Non-goals

- Serializing payloads across processes.

## Open items

- None.
