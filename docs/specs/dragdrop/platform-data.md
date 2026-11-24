---
id: DD-PLATFORM
title: Platform files and text
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop
sources:
  - MonacoLabs.DragDrop/PlatformInteractionIds.cs
  - MonacoLabs.DragDrop/DragDropIds.cs
types:
  - PlatformInteractionIds
tests:
  - InteractionIdTests
depends:
  - DD-TRANSFER
---

# Platform files and text

## Purpose

`PlatformInteractionIds` names the two interaction ids that map to platform data formats, so drags can exchange files and text with other applications. On a drag operation they add platform data next to the in-process payload (DD-DRAG-018, DD-DRAG-019); on a drop operation they accept that data from any source, this app included. The drop-side handling is in DD-DROP-016 and the collection variant in DD-COLLECTION; the effect rule is DD-EFFECTS-007.

## Requirements

- **DD-PLATFORM-001** — `PlatformInteractionIds.Files` (`platform:files`) shall map to the platform file format, and `PlatformInteractionIds.Text` (`platform:text`) to the platform text format; every other id shall map to an in-process format.
  - Tests: `InteractionIdTests.TheFilesIdMapsToThePlatformFileFormat`, `InteractionIdTests.TheTextIdMapsToThePlatformTextFormat`, `InteractionIdTests.OtherIdsMapToAnInProcessFormat`
- **DD-PLATFORM-002** — A drop target listing a platform id shall accept that platform data from any source, including other applications and drags from this app that advertise the same platform id.
  - Tests: untested
- **DD-PLATFORM-003** — A drop shall be a platform drop when the drag carries no in-process payload for the target's non-platform ids.
  - Tests: untested
- **DD-PLATFORM-004** — When a platform drop carries files and the target lists `platform:files`, `DropEventArgs.Items` shall hold the dropped `IStorageItem`s; otherwise, when it carries text and the target lists `platform:text`, `Items` shall hold that one string.
  - Tests: untested
- **DD-PLATFORM-005** — A platform drop shall never change `PayloadTarget`, never call `DropCompleted` and always set `DropEventArgs.IsPlatformDrop`.
  - Tests: untested
- **DD-PLATFORM-006** — When a drag carries both files and text and the target lists both ids, `Items` shall hold the files.
  - Tests: untested
- **DD-PLATFORM-007** — When a drag lists a platform id but `ResolvePlatformText` or `ResolvePlatformFile` returns null, the drag shall carry no data for that id.
  - Tests: untested
- **DD-PLATFORM-008** — Dragging a file from Explorer onto the Desktop page's file list shall add it, and Explorer shall keep the original.
  - Tests: manual — real OLE drag from Explorer onto `DragDropSample/DesktopPage`.
- **DD-PLATFORM-009** — Dragging a snippet from the Desktop page into another application's text field (Notepad, a browser) shall insert its text.
  - Tests: manual — real OLE drag from `DragDropSample/DesktopPage` into another app.

## Invariants

- No platform drop removes anything from a source, in this app or another.

## Edge cases

- A drag from this app that advertises both an in-process id and `platform:text` is an in-process drop for a target listing the in-process id, and a platform drop for a target listing only `platform:text`.
- With the default `ResolvePlatformText`, a payload without a `ToString()` override offers its type name as text.
- A target can list a platform id together with in-process ids; the in-process payload wins whenever one matches.

## Threading and lifetime

- Platform data is read on the UI thread during the drop. `IStorageItem`s keep whatever handles the platform gives them; the library does not dispose them.

## Platform behavior

- Format support follows Avalonia's platform backends. The sample is only checked on Win32 (DD-PLATFORM-008, 009).

## Decisions

- **Platform data goes to `PayloadCommand`, not `PayloadTarget`.** Reason: files and text rarely have the payload's type, so they need a view-model decision (copy the file, create an item from the text). Assigning them to a typed `PayloadTarget` would fail or silently store the wrong type.
- **Platform ids are plain interaction ids.** Reason: one mechanism selects both in-process and platform data, so a source opts into exporting text the same way it opts into a channel. Rejected: separate `ExportsText`/`AcceptsFiles` flags.

## Non-goals

- Formats beyond files and text (images, HTML, custom clipboard formats).
- Producing virtual files for in-memory payloads.

## Open items

- DD-PLATFORM-008 and DD-PLATFORM-009 have not been recorded as checked by hand. Logged in the backlog.
