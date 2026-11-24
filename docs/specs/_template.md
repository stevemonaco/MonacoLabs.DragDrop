---
id: DD-EXAMPLE
title: Example feature
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop
sources:
  - MonacoLabs.DragDrop/Operations/ExampleOperation.cs
types:
  - ExampleOperation
tests:
  - ExampleOperationTests
depends:
  - DD-DROP
---

# Example feature

## Purpose

Two or three sentences: what the feature is for and where it sits (which spec it builds on, which builds on it). When the spec specifies members of a type another spec owns, one line names them.

## Requirements

### Group name

- **DD-EXAMPLE-001** — When a valid drag enters the attached control, the example shall show its adorner.
  - Tests: `ExampleOperationTests.AValidDragShowsTheAdorner`
- **DD-EXAMPLE-002** — The example shall never call `DropCompleted` twice for one drop.
  - Tests: untested
- **DD-EXAMPLE-003** — When a file is dragged in from Explorer, the example shall resolve the effect to Copy.
  - Tests: manual — needs a real OLE drag from Explorer onto the Desktop page.
- ~~**DD-EXAMPLE-004**~~ — Removed by <change>: superseded by DD-EXAMPLE-005.

### Defaults

- **DD-EXAMPLE-005** (inherited) — When `Delay` is not set, the example shall wait 700 ms.
  - Tests: untested

## Invariants

- What holds after every event and every drop.

## Edge cases

- Null payloads, empty collections, detaching mid-drag, reentrant calls from commands…

## Threading and lifetime

- Which thread, what keeps the object alive, what releases it.

## Platform behavior

- Win32/macOS/X11 differences, or "No platform-specific behavior."

## Decisions

- **A short title.** The decision. Reason: why. Rejected: the alternatives and why, when known.

## Non-goals

- What the feature deliberately does not do.

## Open items

- Disagreements between code, docs and intent found while writing the spec; behavior with no test that should have one; pending manual checks. "None." when empty.
