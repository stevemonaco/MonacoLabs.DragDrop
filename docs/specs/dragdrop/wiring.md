---
id: DD-WIRING
title: Wiring and attached properties
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop
  - MonacoLabs.DragDrop.Abstractions
sources:
  - MonacoLabs.DragDrop/DragDrop.cs
  - MonacoLabs.DragDrop/DragDrop.props.cs
  - MonacoLabs.DragDrop/Abstractions/IDragOperation.cs
  - MonacoLabs.DragDrop/Abstractions/IDropOperation.cs
  - MonacoLabs.DragDrop/AssemblyInfo.cs
types:
  - DragDrop
  - IDragOperation
  - IDropOperation
tests: []
depends: []
---

# Wiring and attached properties

## Purpose

`DragDrop` is the XAML entry point: the `DragOperation` and `DropOperation` attached properties turn a control into a drag source or a drop target by attaching an operation object to it, and `IsDragHandle` marks where drags may start. Every other spec builds on the attach/detach contract defined here. The drag-handle rule itself is in DD-DRAG.

## Requirements

### Attaching operations

- **DD-WIRING-001** — When `DragDrop.DragOperation` is set on a control, the library shall call `Attach(control)` on the new operation, after calling `Detach(control)` on the previous one if there was one.
  - Tests: untested
- **DD-WIRING-002** — When `DragDrop.DropOperation` is set on a control, the library shall call `Attach(control)` on the new operation, after calling `Detach(control)` on the previous one if there was one.
  - Tests: untested
- **DD-WIRING-003** — When either attached property is cleared, the library shall call `Detach(control)` on the operation it held.
  - Tests: untested
- **DD-WIRING-004** — The `DragOperation` and `DropOperation` attached properties shall default to `BindingMode.OneTime`.
  - Tests: untested
- **DD-WIRING-005** — A control may hold one drag operation and one drop operation at the same time, independently.
  - Tests: untested

### One control per operation

- **DD-WIRING-006** — When `Attach` is called on an operation that is already attached, the operation shall throw `ArgumentException`.
  - Tests: untested
- **DD-WIRING-007** — When `Detach` is called on an operation that is not attached, the operation shall do nothing.
  - Tests: untested
- **DD-WIRING-008** — After `Detach`, `AttachedControl` shall be null and the operation may be attached to another control.
  - Tests: untested

### Drag handles

- **DD-WIRING-009** (inherited) — When `DragDrop.IsDragHandle` is not set, it shall be false.
  - Tests: untested

### XAML namespace

- **DD-WIRING-010** — The assembly shall map the `MonacoLabs.DragDrop` namespace, and only that namespace, to the XML namespace `https://github.com/stevemonaco/MonacoLabs.DragDrop`.
  - Tests: untested

## Invariants

- An operation is attached to at most one control at a time, and `AttachedControl` names it.
- A control's drag operation and drop operation never share state through `DragDrop`; each operation subscribes to the control on its own.

## Edge cases

- `Detach(control)` does not check that `control` is the attached control: it unsubscribes from the control passed and clears `AttachedControl` anyway.
- Defining an operation inside a `Style` setter shares one instance between every styled control, so the second control's `Attach` throws (DD-WIRING-006). Operations must be declared per control, for example inside an item template.
- `AttachedControl` has a public setter, so code can change it without going through `Attach`, leaving the event subscriptions on the old control.

## Threading and lifetime

- UI thread only, like every Avalonia property change.
- The attached property keeps the operation alive as long as the control is. A control removed from the tree keeps its operation attached and subscribed; the two are collected together.

## Platform behavior

- No platform-specific behavior.

## Decisions

- **Operations are attached through attached properties.** Reason: it keeps view models free of drag-and-drop code (the library is MVVM-first) and lets XAML configure operations with bindings. Rejected: behaviors (an extra package dependency) and code-behind registration.
- **Operations are `AvaloniaObject`s, not `StyledElement`s.** Reason: they only need styled and direct properties for binding. Bindings on them resolve against the nearest parent control's `DataContext` (see ARCHITECTURE §4, "Bindings on operations use the host control's DataContext").
- **One operation instance per control.** `Attach` throws when reused. Reason: an operation keeps per-control drag state (tracked press, hovered container, adorners), which two controls would corrupt.

## Non-goals

- Attaching operations from styles or control themes.
- Reporting which control an operation was detached from.

## Open items

- `Attach` throws `ArgumentException` for a state error; `InvalidOperationException` fits better. Addressed by [changes/correctness-fixes.md](../../changes/correctness-fixes.md).
- `DragDrop` is an instantiable, non-static class, its `DragDrop.props.cs` holds only commented-out code, and the attached-property registrations live in `DragDrop.cs`, against the `*.props.cs` convention. Addressed by [changes/api-design.md](../../changes/api-design.md).
- `AttachedControl` is publicly settable (Edge cases). Addressed by [changes/api-design.md](../../changes/api-design.md).
- `SetIsDragHandle` and `GetIsDragHandle` have no doc comments. Logged in the backlog.
