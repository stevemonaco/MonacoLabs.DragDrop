# Telling the rest of the UI that a drag is in progress

## Why

Nothing outside the dragged element can tell that a drag is running. The drag source sets `:dragging` on its own visual (DD-SESSION-001) and raises `DragFailed` when a drag fails, and that is all. Apps commonly want to:

- show a trash zone or a "drop here" area only while something is being dragged;
- highlight every target that would accept the current drag before the pointer reaches it, and dim the rest;
- know in the view model that a drag started or ended (pause live updates, show a hint, log what happened).

Today that needs a subclass of every drag operation, plus app-wide plumbing. Prompted by the review of 2026-11-26.

## What

**Drag side.** `DragOperationBase` gains:
- `IsDragging`, a read-only bindable property, true from just before the platform drag starts until it ends. A view model binds it with `Mode=OneWayToSource`.
- `DragStarted` and `DragEnded` events. `DragEnded` carries the drag's `DragInfo`, the final effect, and whether it was dropped outside a target (DD-SESSION-011). `DragFailed` keeps firing for failures, and `DragEnded` follows it.

**Window.** While a drag started by this library runs, the source's window has a `:dragactive` pseudo-class, so any style can react: `Window:dragactive Border.trash { IsVisible: True }`.

**Drop side: candidate targets.** While an in-process drag runs, every attached drop operation for which the drag is meant (DD-DROP's targeting: an interaction id matches, the payload count is acceptable, the self-drop rule) sets `:dropcandidate` on its control, in any window of the app. A target the pointer is over still gets `:dropover` when the drop is valid, as today. Candidacy does not run `CanDrop`, which needs a hover position.

**What does not change:** hover feedback, validation and effects. Drags from other applications only become known when they enter a window (see open questions).

## Decisions

- **A bindable property and events, not commands.** Reason: `IsDragging` covers the common view-model need (show or hide something) with an ordinary binding, and events cover code that needs the details; neither forces an `ICommand` on view models. Rejected: `DragStartedCommand` / `DragEndedCommand` (revisit if a sample needs them, see open questions).
- **Candidacy is targeting only.** Reason: targeting depends only on the drag, so it is right for every target at once; validation can depend on the pointer position (collection locations, slots) and would light targets that then reject. Rejected: running `CanDrop` with no hover.
- **A registry of attached drop operations.** Candidates are found through an app-wide registry that `Attach` adds to and `Detach` removes from, holding operations weakly. Reason: drag events only reach the element under the pointer, so a target elsewhere can't learn about the drag from Avalonia. Rejected: walking the visual tree of every window at drag start (slow, and misses windows opened mid-drag).
- **Pseudo-classes for styling, properties for view models.** Reason: the library's styling hooks are pseudo-classes (ARCHITECTURE §1); view models can't observe pseudo-classes.

## Spec changes

**DD-DRAG**
- Added: `IsDragging` is true from just before the platform drag starts until it returns, whatever the outcome.
- Added: `DragStarted` is raised after the payloads resolve and before the platform drag starts; `DragEnded` after it returns, with the effect and the dropped-outside flag, and after `DragFailed` when the drag failed.

**DD-SESSION**
- Added: while a drag runs, the source's top level has `:dragactive`, removed when the drag ends.

**DD-DROP**
- Added: "While an in-process drag started by this library runs, a drop target for which the drag is meant (DD-DROP-004, 035) shall have `:dropcandidate`, removed when the drag ends or the operation is detached."
- Targeting must be callable from the drag's data alone, without a `DragEventArgs`: an internal refactor of `IsDropTargetFor`'s logic, behavior unchanged.

**DD-WIRING**: attaching registers the drop operation for candidacy and detaching unregisters it; the registry does not keep operations alive.

ARCHITECTURE §5 and CLAUDE.md: the new pseudo-classes in the styling hooks list.

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **Drag-side state and events.**
   - `DragGestureTests.IsDraggingIsTrueOnlyWhileTheDragRuns`
   - `DragGestureTests.DragStartedAndDragEndedBracketTheDrag`
   - `DragGestureTests.DragEndedReportsTheEffectAndDropsOutside`
   - `DragGestureTests.AFailedDragRaisesDragFailedThenDragEnded`
2. **Window pseudo-class.**
   - `DragSessionTests.TheWindowIsDragActiveWhileADragRuns`
3. **Candidates.**
   - `DropTargetTests.TargetsTheDragIsMeantForAreCandidates`
   - `DropTargetTests.TargetsWithoutAMatchingIdAreNotCandidates`
   - `DropTargetTests.TheSourceIsNotACandidateForItsOwnSingleValueDrag`
   - `DropTargetTests.CandidacyEndsWithTheDragAndOnDetach`
   - `DropTargetTests.TargetsInAnotherWindowAreCandidates`
4. **Sample.**
   - Simple Collections: a trash zone (a command-only target that accepts the GUIDs and completes the Move) hidden until `Window:dragactive`, and a status line bound to the view model through `IsDragging`.
   - Workers & Teams: the three teams style `:dropcandidate`, so they light up when a worker is picked up.
5. **Manual check on the Win32 sample.** Pick items up on both pages; the trash appears and disappears, teams light up, and both reset after a drop, a cancel and a drop outside.
6. **Close.** Names settled, specs cite every test, this proposal is deleted.

## Open questions

- **Names.** `:dragactive` and `:dropcandidate` versus alternatives such as `:dragging` on the window (clashes with the source's `:dragging` in descendant selectors) or `:dropavailable`.
- **Drags from other applications.** They can't be candidates until they enter a window, and leave without a reliable signal per window. Recommended: leave them out; a later change could set `:dragactive` while an external drag is over a window.
- **Commands.** Add `DragStartedCommand` / `DragEndedCommand` if the sample or a consumer needs view-model reactions beyond `IsDragging`.
