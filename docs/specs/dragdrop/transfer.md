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
depends:
  - DD-WIRING
---

# Interaction ids and the data transfer

## Purpose

Defines what a drag carries and how a drop target recognizes it. Interaction ids are named channels: a drag source advertises one or more, a drop target accepts a subset, and the payload travels once per id in an in-process `DataTransfer` together with a `DragInfo`. `DropInfo` goes back to the drag source on drop, and `DropEventArgs` goes to the target's `PayloadCommand`. The drag side builds the transfer (DD-DRAG), and the drop side reads it (DD-DROP). The platform ids are in DD-PLATFORM.

## Requirements

### Interaction ids

- **DD-TRANSFER-001** (inherited) — When `InteractionIds` is not set on a drag or drop operation, it shall be the single id `app:default`.
  - Tests: `InteractionIdTests.DragOperationsDefaultToTheAppDefaultId`, `InteractionIdTests.DropOperationsDefaultToTheAppDefaultId`
- **DD-TRANSFER-002** — A drop target shall accept a drag when any one of its `InteractionIds` is present in the drag's data, regardless of the other ids the drag carries.
  - Tests: untested
- **DD-TRANSFER-003** — When `InteractionIds` is set from XAML as a comma-separated string, each id shall be one entry.
  - Tests: untested

### Data transfer layout

- **DD-TRANSFER-004** — For each non-platform id in the drag's `InteractionIds`, the data transfer shall hold the payload under an in-process format named by that id.
  - Tests: untested
- **DD-TRANSFER-005** — The data transfer shall hold the drag's `DragInfo` under the in-process format `app:dragmetadata`.
  - Tests: untested
- **DD-TRANSFER-006** — The data transfer shall hold exactly one item, carrying every format of the drag.
  - Tests: untested
- **DD-TRANSFER-007** — When a drop target reads the payload, it shall take the first non-null in-process value across its own non-platform ids, in the order of its `InteractionIds`.
  - Tests: untested

### DragInfo

- **DD-TRANSFER-008** — `DragInfo` shall carry the drag operation that created it, the press position relative to the drag source's attached control, a copy of the drag's interaction ids, the id of the pointer that pressed, and the payload resolved when the drag started.
  - Tests: untested
- **DD-TRANSFER-009** — `DragInfo.DragIds` shall be a copy, so later changes to the operation's `InteractionIds` do not change it.
  - Tests: untested

### DropInfo

- **DD-TRANSFER-010** — `DropInfo` shall carry the drop's `DragEventArgs`, the hovered control, the pointer position relative to the drop target's attached control and, for single-value targets, the value the target held before the drop (`ReplacedPayload`).
  - Tests: untested

### DropEventArgs

- **DD-TRANSFER-011** — `DropEventArgs.Target` shall be the drop event's source element, and `TargetContext` shall be that element's `DataContext`, or null when the source is not a `StyledElement`.
  - Tests: untested
- **DD-TRANSFER-012** — `DropEventArgs.Items` shall hold the single in-process payload for an in-process drop, or the dropped files or text for a platform drop (DD-PLATFORM).
  - Tests: untested
- **DD-TRANSFER-013** — `DropEventArgs.Offset` shall be `OnTarget` for single-value targets, and the hovered offset for collection and tree targets (DD-COLLECTION).
  - Tests: untested
- **DD-TRANSFER-014** — `DropEventArgs.Location` shall be the insertion point for collection and tree targets, and null for single-value targets.
  - Tests: untested

## Invariants

- Every drag started by this library carries `app:dragmetadata`, so `TryGetDragInfo` succeeds for any in-process drag.
- A drag carries exactly one payload. Multiple selection still drags only the pressed item (DD-COLLECTION).

## Edge cases

- Two ids carrying the same payload is the normal case. A target that lists several of them reads the payload once (DD-TRANSFER-007).
- Ids are case-sensitive strings. The `app:` and `platform:` prefixes are conventions, except the two exact platform ids, which map to platform formats.
- A drag from another instance of the same app is not in-process: Avalonia's in-process formats do not cross process boundaries, so only its platform ids are visible.

## Threading and lifetime

- `DragInfo` and `DropInfo` hold references to controls and event args. They live as long as whoever keeps them. The library keeps neither after the drop returns.

## Platform behavior

- In-process formats never reach the OS. Only the platform ids (DD-PLATFORM) produce data other applications can read.

## Decisions

- **Interaction ids are typed channels.** One drag can advertise several ids, and each target accepts a subset. Reason: a source shared by several targets (the ListBox page's worker pool feeding three teams) needs to be told apart by the targets without a type check on the payload. Rejected: matching on payload type, which cannot distinguish two targets that both take `WorkerViewModel`.
- **The payload is stored once per id.** Reason: `DataTransfer` looks up by format, and a target only knows its own ids. Rejected: one shared format plus an id list, which would make every target parse the list.

## Non-goals

- Dragging several items at once.
- Serializing payloads across processes.

## Open items

- None.
