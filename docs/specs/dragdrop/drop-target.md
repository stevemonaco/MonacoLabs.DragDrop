---
id: DD-DROP
title: Drop target
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop.Abstractions
sources:
  - MonacoLabs.DragDrop/Abstractions/DropOperationBase.cs
  - MonacoLabs.DragDrop/Abstractions/DropOperationBase.props.cs
types:
  - DropOperationBase
tests:
  - DropTargetTests
  - DropNotificationTests
  - PlatformDropTests
depends:
  - DD-WIRING
  - DD-TRANSFER
  - DD-EFFECTS
  - DD-PLATFORM
  - DD-ADORNERS
---

# Drop target

## Purpose

`DropOperationBase` makes its attached control accept drags, and runs one drop flow for every kind of target. On each DragEnter and DragOver it decides whether the drag is meant for it, resolves what is under the pointer, validates the drag through the overridable `CanDrop`, updates its drop adorner (DD-ADORNERS) and `:dropover`, and picks the effect (DD-EFFECTS). On drop it runs the same steps, then its transfer step, reports back to the drag source and runs `PayloadCommand`. The base's transfer step does nothing, so a direct subclass is a command-only target. `ItemDropOperation` transfers by assigning `PayloadTarget` (DD-ITEM); collection and tree targets resolve a hovered container and location, add their own feedback and insert into a collection (DD-COLLECTION, DD-TREE). Every target raises the same notifications, `OnDragEnter`, `OnDragOver`, `OnDragLeave` and `OnDrop`, after the library has done its work for the event.

## Requirements

### Attaching

- **DD-DROP-001** — When attached, the drop target shall set `AllowDrop` on the control and handle its DragEnter, DragOver, DragLeave and Drop events with bubbling routing, including handled events when `HandledEventsToo` is true.
  - Tests: `DropTargetTests.AttachingAllowsDropsOnTheControl`, `DropTargetTests.HandledEventsTooDecidesWhetherADropAChildHandlesReachesTheTarget`
- **DD-DROP-002** — When attached with no `DropAdorner`, the drop target shall create a `DropHighlightAdorner` with its default `Kind`. In either case it shall point the adorner's `TargetControl` at the attached control.
  - Tests: `DropTargetTests.AttachingWithoutAnAdornerCreatesASolidHighlight`
- **DD-DROP-003** — When detached, the drop target shall clear `AllowDrop` on the control and remove its handlers.
  - Tests: `DropTargetTests.DetachingDisallowsDropsAndStopsHandlingDragEvents`
- **DD-DROP-027** — When detached, the drop target shall cancel a pending deferred leave, clear `:dropover` from the control and clear `ErrorMessage`, and hide its adorner.
  - Tests: `DropTargetTests.DetachingClearsDropoverAndHidesTheAdorner`
- **DD-DROP-028** — A deferred leave that runs after the drop target was detached shall do nothing.
  - Tests: `DropTargetTests.DetachingCancelsAPendingLeave`
- **DD-DROP-023** — When `HandledEventsToo` is set while the operation is attached, the drop target shall throw `InvalidOperationException`.
  - Tests: `DropTargetTests.SettingHandledEventsTooWhileAttachedThrows`

### Targeting

- **DD-DROP-004** — The drop target shall treat a drag as meant for it when the drag carries data for one of its `InteractionIds` (an in-process payload, or platform data for a platform id) and, unless the target allows self-drops, the drag did not start from the same control.
  - Tests: `DropTargetTests.ADragCarryingOneOfTheTargetsIdsIsMeantForIt`, `DropTargetTests.ADragWithoutAnyOfTheTargetsIdsIsNotMeantForIt`, `DropTargetTests.ADragFromTheTargetsOwnControlIsNotMeantForIt`
- **DD-DROP-005** — When a drag is not meant for the target, the drop target shall report effect None on DragEnter, DragOver and Drop and shall not show its adorner.
  - Tests: `DropTargetTests.ADragWithoutAnyOfTheTargetsIdsIsNotMeantForIt`, `DropTargetTests.ADragFromTheTargetsOwnControlIsNotMeantForIt`
- **DD-DROP-006** — A single-value drop target shall not allow self-drops.
  - Tests: `DropTargetTests.ADragFromTheTargetsOwnControlIsNotMeantForIt`
- **DD-DROP-031** — When a drag stops being meant for the target while it is over it, the drop target shall clear its hover feedback.
  - Tests: `DropTargetTests.ADragThatStopsBeingMeantForTheTargetClearsTheHover`, `CollectionDropTests.ADragThatStopsBeingMeantForTheTargetClearsTheHover`

### Validation

- **DD-DROP-007** — Before each validation, the drop target shall clear `ErrorMessage`; after it, it shall copy `ErrorMessage` to `DropAdorner.ErrorMessage`, so the message reflects only the latest evaluation.
  - Tests: `DropTargetTests.TheMessageIsClearedBeforeEachEvaluation`, `DropTargetTests.RejectSetsTheMessageAndTheAdornerMirrorsIt`
- **DD-DROP-024** — `ErrorMessage` shall be read-only outside the operation, and `Reject(message)` shall set it to the message and return false.
  - Tests: `DropTargetTests.RejectSetsTheMessageAndTheAdornerMirrorsIt`
- **DD-DROP-025** — When a drag leaves the target or drops on it, the drop target shall clear `ErrorMessage` and the adorner's message.
  - Tests: `DropTargetTests.TheMessageIsClearedWhenTheDragLeaves`
- **DD-DROP-026** — When an in-process drop's effect is Copy, the drop target shall transfer the value `CreateCopy` returns, which by default is the payload itself, and call `CreateCopy` once per drop; for any other effect it shall transfer the payload.
  - Tests: `DropTargetTests.ACopyDropAssignsTheCreatedCopy`
- **DD-DROP-008** — The drop target shall evaluate `CanDrop` on every DragEnter, DragOver and Drop of a drag meant for it.
  - Tests: `DropTargetTests.CanDropIsEvaluatedOnEveryEnterOverAndDrop`
- **DD-DROP-009** — When `CanDrop` is not overridden, the drop target shall accept every drag meant for it.
  - Tests: `DropTargetTests.WithoutACanDropOverrideEveryDragMeantForTheTargetIsAccepted`

### Hover feedback

- **DD-DROP-010** — When a drag meant for the target enters, the drop target shall show its adorner with `IsDropValid` equal to the `CanDrop` result, so a rejected drag shows the adorner in its error state.
  - Tests: `DropTargetTests.EnteringShowsTheAdornerWithTheValidationResult`
- **DD-DROP-011** — While a drag meant for the target is over it, the drop target shall set `:dropover` on the attached control exactly when `CanDrop` is true, and update the adorner's `IsDropValid` on each DragOver.
  - Tests: `DropTargetTests.DropoverAndTheAdornersValidityFollowEachDragOver`
- **DD-DROP-012** — While `CanDrop` is false, the drop target shall report effect None; otherwise it shall report the effect DD-EFFECTS picks.
  - Tests: `DropTargetTests.TheEffectIsNoneWhileRejectedAndOtherwiseTheResolvedEffect`
- **DD-DROP-030** — When a drag meant for the target is valid, the drop target shall mark DragEnter, DragOver and Drop handled.
  - Tests: `DropTargetTests.AValidDragIsMarkedHandled`, `DropTargetTests.AnInnerTargetThatAcceptsWinsOverItsOuterTarget`, `CollectionDropTests.EnterAndOverBothRunTheHoverAndAreHandledOnlyWhenValid`
- **DD-DROP-013** — When DragLeave is raised, the drop target shall defer the leave and, if no DragEnter or Drop arrives on the target before the deferred work runs, clear `:dropover` and hide its adorner.
  - Tests: `DropTargetTests.ALeaveClearsTheHoverOnceTheDeferredWorkRuns`, `DropTargetTests.AnEnterBeforeTheDeferredLeaveRunsKeepsTheHover`

### Dropping

- **DD-DROP-014** — When a drag meant for the target is dropped and `CanDrop` is false, the drop target shall report effect None and clear its hover feedback without changing anything else.
  - Tests: `DropTargetTests.ARejectedDropReportsNoneAndOnlyClearsTheHover`
- **DD-DROP-015** — When an in-process payload is dropped and accepted, the drop target shall recompute the effect, run its transfer step and, when the step transferred the payload, call the drag operation's `DropCompleted` with that effect and the step's `DropInfo`, and then run `PayloadCommand`.
  - Tests: `DropTargetTests.AnAcceptedDropAssignsThePayloadCompletesTheDragThenRunsTheCommand`, `CollectionDropTests.AnAcceptedDropInsertsThenCompletesWithWhereItLanded`
- **DD-DROP-029** — When the transfer step transferred nothing but `PayloadCommand` ran, the drop target shall call `DropCompleted` after the command; when neither happened (there is no command, or `CanExecute` is false), it shall report effect None and not call `DropCompleted`.
  - Tests: `DropTargetTests.ACommandOnlyBaseSubclassCompletesAMove`, `DropTargetTests.ABaseSubclassWithoutACommandReportsNone`, `CollectionDropTests.ACommandOnlyTargetCompletesAMove`, `CollectionDropTests.ADropThatNeitherTransfersNorRunsACommandReportsNone`, `CollectionDropTests.ACommandWhoseCanExecuteIsFalseReportsNone`, `EndToEndTests.ReorderingThroughACommandRemovesTheOriginal`
- **DD-DROP-016** — When platform data is dropped and accepted, the drop target shall leave `PayloadTarget` unchanged, not call `DropCompleted`, and run `PayloadCommand` with the files or text and `IsPlatformDrop` set (DD-PLATFORM).
  - Tests: `PlatformDropTests.APlatformDropLeavesPayloadTargetAndNeverCompletesTheDrag`
- **DD-DROP-017** — The drop target shall run `PayloadCommand` only when `CanExecute` returns true for the `DropEventArgs` it would receive.
  - Tests: `DropTargetTests.TheCommandRunsOnlyWhenItCanExecuteTheArgsItWouldReceive`
- **DD-DROP-018** — After an accepted drop, the drop target shall call `OnDrop`, then clear `:dropover`, set the adorner invalid and hide it.
  - Tests: `DropTargetTests.AnAcceptedDropCallsOnDropAndHidesTheFeedback`
- **DD-DROP-019** — `PayloadTarget` shall bind TwoWay by default, so a drop writes back to the bound view-model property.
  - Tests: `DropTargetTests.ADropWritesPayloadTargetBackThroughADefaultBinding`

### Notifications

- **DD-DROP-032** — The drop target shall call `OnDragEnter` when a drag meant for it first arrives and `OnDragOver` on each later DragOver, after updating its feedback and effect.
  - Tests: `DropNotificationTests.EveryTargetRaisesEnterThenOverThenDrop`, `DropNotificationTests.NotificationsSeeTheFeedbackOfTheirEvent`, `DropNotificationTests.ADragThatIsNeverMeantForTheTargetRaisesNothing`
- **DD-DROP-033** — The drop target shall call `OnDragLeave` when its hover ends without an accepted drop: after the deferred leave, when a drag stops being meant for it, after a rejected drop, and when detached mid-drag. It shall call it after clearing its feedback.
  - Tests: `DropNotificationTests.EveryTargetRaisesLeaveWhenTheDragLeaves`, `DropNotificationTests.ADragThatStopsBeingMeantForTheTargetRaisesLeave`, `DropNotificationTests.ARejectedDropRaisesLeaveNotDrop`, `DropNotificationTests.DetachingMidDragRaisesLeave`, `DropTargetTests.DetachingCancelsAPendingLeave`
- **DD-DROP-034** — The drop target shall call `OnDrop` after an accepted drop, before clearing its hover.
  - Tests: `DropNotificationTests.EveryTargetRaisesEnterThenOverThenDrop`, `DropNotificationTests.NotificationsSeeTheFeedbackOfTheirEvent`, `DropTargetTests.AnAcceptedDropCallsOnDropAndHidesTheFeedback`

### Helpers for overrides

- **DD-DROP-020** — `TryGetPayload<T>` shall succeed when the in-process payload (DD-TRANSFER-007) is a `T`.
  - Tests: `DropTargetTests.TryGetPayloadSucceedsForThePayloadsType`
- **DD-DROP-021** — `TryGetDragInfo<T>` shall succeed when the drag's `DragInfo` is a `T`.
  - Tests: `DropTargetTests.TryGetDragInfoSucceedsForTheDragInfosType`
- **DD-DROP-022** — `TryGetFiles` shall succeed only when `PlatformInteractionIds.Files` is among the target's ids and the drag holds at least one storage item; `TryGetText` only when `PlatformInteractionIds.Text` is among them and the drag holds text.
  - Tests: `DropTargetTests.TryGetFilesAndTryGetTextNeedTheirIdAndTheirData`, `DropTargetTests.TryGetFilesAndTryGetTextSucceedWithTheirIdAndTheirData`

## Invariants

- The base never changes `PayloadTarget`; only a transfer step does (DD-ITEM).
- `DropCompleted` is called at most once per drop, and only for an accepted in-process drop that transferred or ran `PayloadCommand`.
- Once a drop or an undisturbed leave has been processed, the attached control has no `:dropover` and the adorner is hidden.
- A hover raises `OnDragEnter` once, then `OnDragOver` any number of times, and ends with exactly one `OnDrop` or `OnDragLeave`.

## Edge cases

- A drag rejected by DD-DROP-004 never shows an adorner, even an error one, and raises no notifications. Only `CanDrop` rejections show the error state.
- A message set directly on `DropAdorner.ErrorMessage` inside `CanDrop` is overwritten when the operation mirrors `ErrorMessage` after validation; overrides use `Reject`. A target whose `DropAdorner` is null still has `ErrorMessage`.
- Crossing into a child raises DragLeave then DragEnter; the DragEnter cancels the deferred leave and, because the drag is still hovering, raises `OnDragOver`, not a second `OnDragEnter`.
- The deferred leave captures the leaving event's args and runs `OnDragLeave` with them after the event has finished. A detach mid-drag passes the drag's latest event.
- With nested targets, the innermost target that accepts the drag marks its events handled, so an outer target sees them only with `HandledEventsToo`. An inner target that rejects or is not meant for the drag leaves them unhandled, and the outer target decides.

## Threading and lifetime

- UI thread only. The deferred leave (DD-DROP-013) is posted to the UI dispatcher.
- Detach ends the hover, raising `OnDragLeave` while `AttachedControl` is still set, and cancels a pending deferred leave (DD-DROP-028), so a leave posted just before Detach never runs. `DropAdorner.TargetControl` keeps pointing at the old control until the next Attach.

## Platform behavior

- Avalonia's Win32 layer swallows exceptions thrown from these handlers and returns `E_FAIL` (see DD-DRAG Platform behavior).
- Moving between child elements raises DragLeave then DragEnter in the same pass on every platform Avalonia supports, which is why the leave is deferred.
- Avalonia raises drag events as bubbling only, which is why the drop target has no routing option.

## Decisions

- **The base owns the flow; subclasses override steps.** The subscribed handlers are private. Subclasses override `IsDropTargetFor`, `CanDrop`, `GetDropEffect`, `CreateCopy` and `Transfer`, and the library's collection targets override internal steps for hover, feedback and location. Reason: one flow means every target raises the same notifications and gets the same fixes. Rejected: overridable handlers next to hooks, where collection targets replaced the handlers and silently skipped `OnDragEnter` and `OnDragOver`; separate flows per base class with documentation (nothing enforces it).
- **Transfer is a step, not a handler.** `Transfer` does what the target does with an accepted in-process payload and returns the `DropInfo` to report, or null. Reason: assignment is the single-value behavior, and the base holding it is why collection targets had to replace the base's Drop. Rejected: keeping assignment in the base with a flag for collections.
- **Every accepted in-process drop that did something reports back.** A target calls `DropCompleted` after it transferred the payload or ran `PayloadCommand`. Reason: the drag side owns removal, and a target that silently skips the report turns every Move into a Copy. Rejected: leaving the move to the command, which cannot reach the source collection through `DropEventArgs`.
- **A drop that does nothing reports None.** When a target neither transfers nor runs its command (DD-DROP-029), it reports effect None and does not call `DropCompleted`. Reason: reporting Move for a drop that went nowhere would let the source remove the item into nothing. Rejected: consulting `CanExecute` during DragOver to show None early, which is a separate behavior change (backlog).
- **Notifications cannot change the effect.** `OnDragEnter`, `OnDragOver`, `OnDragLeave` and `OnDrop` return nothing. Reason: two ways to set the effect (hook return values and `GetDropEffect`) let them disagree, so `GetDropEffect` stays the one place. Rejected: hooks that return an effect.
- **Notifications take the event args; state comes from properties.** Overrides read `IsDropValid`, and on collection targets `HoveredContainer`, `HoveredOffset` and `DropLocation`. Reason: it keeps the signatures stable when the state grows. Rejected: a new event-args type per notification, which adds public types for data the properties already expose.
- **Notifications fire on every DragOver.** Reason: it matches the event and is simpler; overrides that do work can compare against the state they saw last. Rejected: raising `OnDragOver` only when the hovered container, offset or validity changes.
- **Every target marks valid drag events handled.** Reason: with nested targets (an item target inside a list target), the inner target that accepted the drag should win, instead of the outer target recomputing the effect after it. An outer target that wants the events anyway sets `HandledEventsToo`.
- **`Routing` is removed from the drop side.** Reason: Avalonia's drag events bubble only, so `Bubble` is the only routing that works, and `Tunnel` left the target deaf to every drag. Rejected: rejecting `Tunnel` in the setter (keeps an option with one valid value); keeping it for symmetry with the drag side, where tunneling does work.
- **Copies come from a hook, not `ICloneable`.** `CreateCopy` returns the value a Copy drop transfers. Reason: what a copy of a view model means (deep or shallow, a new id or the same) is a domain decision, and `ICloneable` does not say. Rejected: calling `ICloneable.Clone` automatically; a bindable factory property (XAML cannot easily supply a delegate).
- **The operation owns the validation message.** `ErrorMessage` lives on the operation and is mirrored to the adorner. Reason: validation belongs to the operation, overrides should not need `DropAdorner!`, and MVVM apps want to show the message outside the adorner, which is why it is also cleared when the drag ends. Rejected: a `Reject` helper that writes only to the adorner (no bindable message); a `CanDrop` that returns a result object (a larger break for every override).
- **Targeting and validation are separate steps.** `IsDropTargetFor` decides whether the target is involved at all, and `CanDrop` decides whether this particular drop is valid. Reason: a drag meant for another target should leave this one visually untouched, while a meant-for-me drag that fails validation should explain itself with the error adorner.
- **DragLeave is deferred.** Reason: Avalonia raises DragLeave then DragEnter when the pointer crosses into a child, and leaving immediately makes the adorner flicker. Rejected: hit testing on each leave to see whether the pointer is still inside.
- **The effect is recomputed on Drop.** Reason: on Drop, `e.DragEffects` arrives as the allowed set, not the effect returned by the last DragOver. Rejected: caching the last DragOver effect, which misses a modifier pressed right before release.

## Non-goals

- Accepting a drop without an interaction id match.
- Transforming the payload on the way in. `TryGetPayload` is virtual for that, but no built-in target overrides it.

## Open items

- None.
