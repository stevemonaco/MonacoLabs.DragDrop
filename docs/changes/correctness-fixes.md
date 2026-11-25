# Correctness fixes from the pre-1.0 review

## Why

A review of the library before stabilizing its API found eight defects: lost moves, a crash path, a leak, dead state and wrong diagnostics. Each one is logged in its spec's Open items. They are fixed together because they share the test infrastructure from [test-suite.md](test-suite.md) (tasks 1–4 must land first), and because the two move fixes change the same completion contract between drop targets and drag sources. Every fix starts from a failing test.

## What

1. **A Move from a list onto a single-value target removes the item from the list.** Today `CollectionDragOperation.DropCompleted` removes only when the drop info is a `CollectionDropInfo` (DD-COLLECTION-010), so dragging a list item onto an `ItemDropOperation` copies it whatever the effect says.
2. **A Move onto a collection target without `PayloadTarget` completes the move.** Today a `CollectionDropOperation` that only runs `PayloadCommand` never calls `DropCompleted` (DD-COLLECTION-027), so the source keeps the item. After the change, the drop target calls `DropCompleted` after the command runs. A drop that neither transfers nor runs a command reports effect None and changes nothing. `DropEventArgs` gains `Effect`, so a command can tell a Move from a Copy.
3. **Detaching a drop operation is clean.** Detach cancels a pending deferred DragLeave, clears `:dropover` and hides the adorners. A deferred leave that runs after Detach does nothing, where today it throws a `NullReferenceException` from the dispatcher (DD-DROP Threading and lifetime).
4. **The drag preview releases its bitmap.** `DragPreviewAdorner` implements `IDisposable` and disposes its `RenderTargetBitmap`, and the drag session disposes the preview when the drag ends.
5. ~~**Dead drag state is removed.**~~ Landed with the pre-1.0 API changes, which deleted `_isDragPending` with the other protected drag fields.
6. **Stale presses cannot start drags.** A pointer move without the left button pressed, or a lost pointer capture, ends the tracked press.
7. **A failing drag does not crash the app.** An exception from the platform drag (on Win32, the `COMException` that wraps an exception from a drop handler) is caught in the private pointer-moved handler, logged through Avalonia's logger, and the drag state is reset. Today it escapes that `async void` handler. The handler awaits the `Task`-returning `OnPointerMoved`, so the same catch also covers exceptions thrown by subclass overrides.
8. **Guard messages and exception types are right.** `ThrowIf.True` and `ThrowIf.False` no longer report the opposite condition. Attaching an operation that is already attached throws `InvalidOperationException` naming the operation type and the control it is attached to.

What does not change: interaction ids, targeting and validation, effect resolution, and every visual. The public API changes in three places: `DragPreviewAdorner` becomes `IDisposable`, `DropEventArgs.Effect` added, and the exception type of a double `Attach`.

## Decisions

- **Removal checks the index before trusting it.** When a drop completes with Move, the collection drag side removes the item at the pressed index, shifted by one when the drop inserted into the same collection at or before it, if that index still holds an equal item. Otherwise it removes the first equal item that is not the one the drop just inserted. The just-inserted item is the one at the drop's target index, or the last item when the drop appended. Reason: the source no longer needs to know what kind of target received the drop, so single-value and command-only targets work. A wrong index (a command that inserted somewhere else, a collection changed during the drag) degrades to an equality search instead of removing the wrong item. `OnDroppedOutside` already works this way. Rejected: special-casing each drop-info type in `DropCompleted`, which repeats the bug for the next target kind.
- **Every accepted in-process drop that did something reports back.** A collection or tree target calls `DropCompleted` after it transferred the payload or ran `PayloadCommand`. With no transfer, the `CollectionDropInfo` carries the requested location (collection and index) as the target. Reason: the drag side owns removal (DD-COLLECTION, "The drag side removes the payload"), and a target that silently skips the report turns every Move into a Copy. Rejected: leaving the move to the command, which cannot reach the source collection through `DropEventArgs`.
- **A drop that does nothing reports None.** When a collection target neither transfers nor runs its command (no `PayloadTarget` list and no command, or `CanExecute` false), it reports effect None and does not call `DropCompleted`. Reason: reporting Move for a drop that went nowhere would let the source remove the item into nothing. Rejected: consulting `CanExecute` during DragOver to show None early. That is a separate behavior change, logged in the backlog.
- **`DropEventArgs` carries the effect.** Reason: a command that inserts the payload itself must know whether the source keeps its item (Copy) or loses it (Move), for example to clone on Copy.
- **Drag failures are logged, not rethrown.** The pointer-moved handler catches exceptions from the drag, logs them at Error under the log area `DragDrop`, and resets the drag state. The `using` around the drag session already removes the preview and `:dragging`. Reason: an exception escaping an `async void` handler terminates the app, and on Win32 the original exception has already been reduced to `E_FAIL`. Rejected: rethrowing (crashes the app); a public `DragFailed` event (no consumer has asked; backlog).
- **The preview owns its snapshot.** `DragPreviewAdorner` implements `IDisposable` and disposes its bitmap. Reason: the adorner creates the bitmap in `Create`, so it is the natural owner, and `IDisposable` tells subclasses and other callers of `Create` to release it. Rejected: an internal release method called only by the session, which leaks for any other caller of the public `Create`.
- **A double `Attach` is an `InvalidOperationException`.** Reason: the argument is fine; the operation's state is wrong. Both operation bases use `ThrowIf.InvalidOperation`, which the pre-1.0 API changes added for the routing options, so they share one message format.

## Spec changes

**DD-COLLECTION**
- Changed **DD-COLLECTION-007**: "When a drop completes with effect Move and `PayloadCollection` is set, the drag operation shall remove the original from `PayloadCollection`, whatever kind of target received the drop."
- Changed **DD-COLLECTION-008**: "The drag operation shall remove at the pressed index, shifted by one when the drop inserted into the same collection at or before it, when that index holds an item equal to the payload."
- Changed **DD-COLLECTION-009**: "When the pressed index is unknown or no longer holds an equal item, the drag operation shall remove the first item equal to the payload that is not the item the drop inserted (the one at the target index, or the last one when the drop appended)."
- Changed **DD-COLLECTION-010**: "When the effect is not Move or `PayloadCollection` is null, the drag operation shall remove nothing."
- Changed **DD-COLLECTION-027**: "When an in-process payload is dropped and accepted and nothing is transferred but `PayloadCommand` ran, the drop operation shall call `DropCompleted` after the command with a `CollectionDropInfo` whose target collection and index are the location's."
- Added: "When an in-process drop neither transfers nor runs `PayloadCommand`, the drop operation shall report effect None and not call `DropCompleted`."
- Decisions: add "Removal checks the index before trusting it", "Every accepted in-process drop that did something reports back" and "A drop that does nothing reports None". Open items: remove the two move items.

**DD-TRANSFER**
- Added: "`DropEventArgs.Effect` shall be the effect computed on Drop (DD-EFFECTS-009), or Copy for platform drops."
- Decisions: add "`DropEventArgs` carries the effect".

**DD-DROP**
- Added: "When detached, the drop target shall cancel a pending deferred leave, clear `:dropover` from the control and hide its adorner."
- Added: "A deferred leave that runs after the drop target was detached shall do nothing."
- Threading and lifetime: replace the Detach paragraph with the new behavior. Open items: remove the Detach item.

**DD-COLLECTION** (detach)
- Added: "When detached, the collection drop operation shall clear its hovered container's pseudo-classes and hide its insertion adorner."

**DD-SESSION**
- Added: "When the drag ends, the session shall dispose the preview, releasing its snapshot bitmap."
- Threading and lifetime: replace the bitmap sentence. Open items: remove the bitmap item. Decisions: add "The preview owns its snapshot".

**DD-DRAG**
- Changed **DD-DRAG-001**: add pointer capture lost to the events handled.
- Added: "When the pointer moves while a press is tracked but the left button is no longer pressed, the drag source shall stop tracking the press and not start a drag."
- Added: "When the pointer capture is lost while a press is tracked, the drag source shall stop tracking the press."
- Added: "When the platform drag, or an `OnPointerMoved` override, throws, the drag source shall log the exception at Error level under the `DragDrop` log area, clear its drag state, and not let the exception reach the application."
- Threading and lifetime: replace the paragraph on exceptions escaping `async void`. Decisions: add "Drag failures are logged, not rethrown". Open items: remove the two items this change resolves.

**DD-WIRING**
- Changed **DD-WIRING-006**: "When `Attach` is called on an operation that is already attached, the operation shall throw `InvalidOperationException` naming the operation's type and the control it is attached to."
- Decisions: add "A double `Attach` is an `InvalidOperationException`". Open items: remove the exception-type item.

## Tasks

Each task first writes the listed tests and confirms they fail, then fixes the code, updates the spec and cites the tests.

1. **Removal by checked index** (What 1).
   - `CollectionRemovalTests`: the changed DD-COLLECTION-008 and 009 cases. Pressed index valid; pressed index stale; duplicates of equal values (the Guid lists on the Simple collection page); append into the same collection; no pressed index.
   - `EndToEndTests.MovingAListItemOntoASingleValueTargetRemovesItFromTheList`, for the changed DD-COLLECTION-007 and 010.
2. **Command-only targets complete the move** (What 2). Depends on task 1.
   - `CollectionDropTests.ACommandOnlyTargetCompletesAMove`
   - `CollectionDropTests.ADropThatNeitherTransfersNorRunsACommandReportsNone`
   - `CollectionDropTests.ACommandWhoseCanExecuteIsFalseReportsNone`
   - `EndToEndTests.ReorderingThroughACommandRemovesTheOriginal`
   - `DropTargetTests.DropEventArgsCarriesTheDropEffect`
3. **Clean detach** (What 3).
   - `DropTargetTests.DetachingCancelsAPendingLeave`
   - `DropTargetTests.DetachingClearsDropoverAndHidesTheAdorner`
   - `CollectionDropTests.DetachingMidDragClearsTheHoveredContainer`
4. **Preview disposal** (What 4).
   - `DragSessionTests.EndingTheDragDisposesThePreview`. Assert through the preview's disposed state, not the bitmap's.
5. ~~**Remove `_isDragPending`**~~ Landed with the pre-1.0 API changes, with `DragGestureTests.ADragDoesNotStartTwiceFromOnePress`.
6. **Stale presses** (What 6).
   - `DragGestureTests.AMoveWithoutTheButtonPressedDoesNotStartADrag`
   - `DragGestureTests.LosingCaptureEndsTheTrackedPress`
7. **Logged drag failures** (What 7).
   - `DragGestureTests.AThrowingPlatformDragIsLoggedAndResetsState`: a test subclass's `DoDragDrop` override throws (headless drags cannot produce the Win32 `COMException`); assert a log entry through a test `ILogSink`, no unhandled exception, and a fresh drag possible afterwards.
   - `DragGestureTests.AThrowingOnPointerMovedOverrideIsLogged`
8. **Guards** (What 8).
   - `ThrowIfTests`: the message cases.
   - `WiringTests.AttachingAnAttachedOperationThrowsInvalidOperation` (changed DD-WIRING-006).
9. **Manual checks on the Win32 sample.**
   - Every page's drags still work.
   - Simple collection page: Ctrl-copy a Guid within one list, then Move one of the duplicates. The right item leaves and the copy stays.
   - Desktop page: drag a snippet into Notepad, then make a drop handler throw on purpose (temporary code). The app stays up and the log shows the exception.
10. **Close.** Every changed and added requirement cites its tests. The spec Open items named above are removed, this proposal is deleted, and the resolved backlog lines are removed.

## Open questions

None. Command-only targets do trigger source removal (Decisions, "Every accepted in-process drop that did something reports back"), and removal keeps `Equals` with the index checked first ("Removal checks the index before trusting it"), as recommended.
