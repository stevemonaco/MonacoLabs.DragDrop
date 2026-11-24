---
id: DD-EFFECTS
title: Drop effects and modifier keys
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop.Abstractions
sources:
  - MonacoLabs.DragDrop/Abstractions/DropOperationBase.cs
  - MonacoLabs.DragDrop/Internals/DropEffectResolver.cs
types: []
tests:
  - DropEffectResolverTests
depends:
  - DD-TRANSFER
  - DD-PLATFORM
---

# Drop effects and modifier keys

## Purpose

How a drop target turns the effects a drag source allows and the modifier keys held into the single effect it reports (`GetDropEffect` on `DropOperationBase`). The reported effect decides the cursor, the preview's pseudo-classes (DD-SESSION) and, on drop, whether the drag side removes its original: every drag operation removes only on an exact Move (DD-ITEM, DD-COLLECTION).

## Requirements

### In-process payloads

- **DD-EFFECTS-001** — When the drag carries an in-process payload for one of the target's ids and the copy modifier and Shift are both held, the target shall report Link if the source allows it.
  - Tests: `DropEffectResolverTests.CopyModifierWithShiftLinks`
- **DD-EFFECTS-002** — When only the copy modifier is held, the target shall report Copy if the source allows it.
  - Tests: `DropEffectResolverTests.CopyModifierCopies`
- **DD-EFFECTS-003** — When only Shift is held, the target shall report Move if the source allows it.
  - Tests: `DropEffectResolverTests.ShiftMoves`
- **DD-EFFECTS-004** — When no modifier is held, or the requested effect is not allowed, the target shall report Move if allowed, else Copy if allowed, else Link if allowed, else None.
  - Tests: `DropEffectResolverTests.WithoutModifiersMoveIsPreferredThenCopyThenLink`, `DropEffectResolverTests.AnUnavailableRequestFallsBackToTheDefaultOrder`
- **DD-EFFECTS-005** — The copy modifier shall be Option (Alt) on macOS and Ctrl on every other platform.
  - Tests: `DropEffectResolverTests.CopyModifierCopies`, `DropEffectResolverTests.CopyModifierWithShiftLinks`, `DropEffectResolverTests.TheOtherPlatformsCopyModifierDoesNotCopy`
- **DD-EFFECTS-006** — Modifier keys other than the copy modifier and Shift shall not change the effect.
  - Tests: `DropEffectResolverTests.OtherModifiersDoNotChangeTheEffect`

### Platform data

- **DD-EFFECTS-007** — When the drag carries no in-process payload for the target's ids, the target shall report Copy if the source allows it, else Link if allowed, else None, whatever modifiers are held.
  - Tests: `DropEffectResolverTests.PlatformDataIsCopiedElseLinkedWhateverTheModifiers`, `DropEffectResolverTests.TheEffectIsAlwaysOneAllowedEffectOrNone`

### When the effect is computed

- **DD-EFFECTS-008** — The target shall compute the effect afresh on every DragEnter, DragOver and Drop, from that event's allowed effects and modifiers.
  - Tests: untested
- **DD-EFFECTS-009** — The effect passed to `DropCompleted` shall be the effect computed on Drop.
  - Tests: untested

## Invariants

- The reported effect is always one of the allowed effects, or None.
- A platform drop never reports Move.

## Edge cases

- A source allowing only Copy (the single-item page's menu) reports Copy with no modifiers held.
- Holding Shift on a source that allows only Copy reports Copy, not None: an unavailable request falls back to the default order.
- A drag carrying both an in-process payload and platform text, dropped on a target that lists only `platform:text`, is a platform drop and reports Copy.

## Threading and lifetime

- A pure function of the event. No state is kept between events.

## Platform behavior

- macOS uses Option for copy (DD-EFFECTS-005), matching Finder. Windows and Linux use Ctrl.
- On Win32, returning Move to a platform source such as Explorer makes it delete the original file (DD-EFFECTS-007 prevents it).

## Decisions

- **Platform data is always copied or linked.** Reason: a Move tells an external source such as a file manager to delete its original, and the library has no way to know a consumer really took ownership of the file. Rejected: honoring Shift for platform data, which deletes user files on a mis-drop.
- **Move is the default for in-process payloads.** Reason: rearranging is the most common in-app drag, and Windows Explorer uses the same default within one volume. The modifiers follow the platform conventions for copy, move and link.

## Non-goals

- Per-target effect preferences (a target that prefers Copy when no modifier is held). An override of `GetDropEffect` can do it.
- `DragDropEffects.Scroll`.

## Open items

None.
