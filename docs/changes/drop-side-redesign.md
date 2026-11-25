# Redesign the drop side's overridable surface

## Why

`DropOperationBase` has two overridable layers that only half fit together. The subscribed handlers (`DragEnter`, `DragOver`, `DragLeave` and `Drop`, each `(object? sender, DragEventArgs e)`) do the targeting, validation, adorners and transfer. The hooks (`OnDragEnter`, `OnDragOver`, `OnDragLeave` and `OnDrop`) are called from those handlers. `CollectionDropOperation` replaces the `DragEnter`, `DragOver` and `Drop` handlers with its own `UpdateDrag` flow and overrides only `OnDragLeave`. As a result:

- On collection and tree targets, an `OnDragEnter` or `OnDragOver` override is silently never called, `OnDrop` is, and `OnDragLeave` means "end the drag" (DD-DROP and DD-COLLECTION Open items).
- Which layer to override depends on the base class, and nothing tells the consumer.
- The single-value and collection flows have drifted: only collection targets mark valid drag events handled, re-attach the adorner on DragOver and clear their hover when a drag stops being meant for them.
- `Routing` accepts `Tunnel`, but Avalonia raises drag events as bubbling only, so a tunneling target never sees a drag (DD-DROP Open items).

The drag side already has the target shape: private subscribed handlers and protected pointer hooks (DD-DRAG, "Pointer hooks, not event handlers"). The drop side should match before the 1.0 API freeze, because every change here breaks protected API. The only overrides in the sample are of `CanDrop`, which does not change.

## What

**One flow in the base.** `DropOperationBase` subscribes private handlers and runs one template flow for every target:

1. **Targeting.** `IsDropTargetFor` decides whether the drag is meant for this target. When it is not, the target reports None and ends any hover it had.
2. **Hover.** The target resolves what is under the pointer. Single-value targets have nothing to resolve; collection and tree targets find the hovered container, its offset and the drop location through the protected steps they already have (`LocateTargetContainer`, `GetInsertionTarget`, `GetDropOffset`, `ResolveDropLocation`).
3. **Validation.** `CanDropAtLocation` (collection and tree targets) and then `CanDrop`, with `ErrorMessage` cleared before and mirrored after, as today.
4. **Feedback.** Adorners, `:dropover`, the hovered container's pseudo-classes and auto-scroll, updated the same way on DragEnter and DragOver.
5. **Effect.** None while invalid, otherwise `GetDropEffect`. A valid event is marked handled, on every kind of target.

On Drop the same steps run, and then the target transfers the payload, reports back to the drag source, runs `PayloadCommand` and ends the hover. The "every accepted in-process drop that did something reports back" rule (DD-COLLECTION) becomes the base rule for every target.

**Transfer is a step, not a handler.** A new protected `Transfer` step does what each kind of target does with an accepted in-process payload and returns the `DropInfo` to report, or null when it transferred nothing. `ItemDropOperation` assigns `PayloadTarget` (moving that behavior out of the base). `CollectionDropOperation` inserts at the location when `TransfersPayload`. The base then calls `DropCompleted` with that info, or, when nothing was transferred but `PayloadCommand` ran, with the location as target; when neither happened it reports None. A custom subclass of the base that does not override `Transfer` is a command-only target.

**One set of notifications.** `OnDragEnter`, `OnDragOver`, `OnDragLeave` and `OnDrop` become notifications that every target raises, in the same order, after the library has done its work for the event:

- `OnDragEnter` when a drag meant for the target first arrives, `OnDragOver` on each later DragOver.
- `OnDragLeave` when the hover ends without an accepted drop: the deferred leave, a drag that stops being meant for the target, a rejected drop, or detaching mid-drag.
- `OnDrop` after an accepted drop, before the hover is cleared.

They take the `DragEventArgs` and return nothing. Overrides read `IsDropValid`, `HoveredContainer`, `HoveredOffset` and `DropLocation` from protected properties, and change the effect only through `GetDropEffect`.

**Smaller surface.** These become private (or `private protected`, for the library's own subclasses): the four subscribed handlers, `SubscribeDropEvents`/`UnsubscribeDropEvents`, `CancelPendingDragLeave`, `EvaluateCanDrop`, `InvokePayloadCommand`, and the collection's `UpdateDrag` and `EndDrag`. `Attach` and `Detach` stop being virtual. `Routing` is removed from the drop side; `HandledEventsToo` stays.

**What does not change:** targeting (DD-DROP-004 to 006), validation and messages, effect resolution, hover zones and locations, adorners and their looks, auto-scroll, hover expand, removal, platform drops, and the public properties other than `Routing`.

## Decisions

- **The base owns the flow; subclasses override steps.** Reason: one flow means every target raises the same notifications and gets the same fixes, and the steps collection and tree targets need already exist. Rejected: keeping overridable handlers (the current two-layer problem); separate flows per base class with documentation (nothing enforces it).
- **Notifications cannot change the effect.** Reason: two ways to set the effect (hook return values and `GetDropEffect`) let them disagree. `GetDropEffect` stays the one place. Rejected: hooks that return an effect, as `OnDragEnter` and `OnDragOver` do today.
- **Notifications take the event args; state comes from properties.** Reason: it keeps the signatures stable when the state grows (the multiple-selection proposal adds to it). Rejected: a new event-args type per notification, which adds four public types for data the properties already expose.
- **`ItemDropOperation` assigns `PayloadTarget`.** Reason: assignment is the single-value behavior, and the base holding it is why collection targets had to replace the base's Drop. Rejected: keeping assignment in the base with a flag for collections.
- **Every target marks valid drag events handled.** Reason: with nested targets (an item target inside a list target), the inner target that accepted the drag should win; today the outer collection target recomputes the effect after the inner one. An outer target that wants the events anyway sets `HandledEventsToo`.
- **`Routing` is removed from the drop side.** Reason: Avalonia's drag events bubble only, so `Bubble` is the only routing that works. Rejected: rejecting `Tunnel` in the setter (keeps an option with one valid value); keeping it for symmetry with the drag side, where tunneling does work.

## Spec changes

**DD-DROP**
- Purpose: the flow is shared by every target; transfer is a step that `ItemDropOperation` and the collection targets implement.
- Changed **DD-DROP-001**: "When attached, the drop target shall set `AllowDrop` on the control and handle its DragEnter, DragOver, DragLeave and Drop events with bubbling routing, including handled events when `HandledEventsToo` is true."
- Struck **DD-DROP-023** (the `Routing` setter): removed with `Routing`.
- Changed **DD-DROP-015**: "When an in-process payload is dropped and accepted, the drop target shall recompute the effect, run its transfer step and, when the step transferred the payload, call the drag operation's `DropCompleted` with that effect and the step's `DropInfo`, and then run `PayloadCommand`."
- Added: "When the transfer step transferred nothing but `PayloadCommand` ran, the drop target shall call `DropCompleted` after the command; when neither happened, it shall report effect None and not call `DropCompleted`." (Moves DD-COLLECTION-027 and 038 to every target.)
- Added: "When a drag meant for the target is valid, the drop target shall mark DragEnter, DragOver and Drop handled." (Moves DD-COLLECTION-020's last clause to every target.)
- Added: "When a drag stops being meant for the target while it is over it, the drop target shall clear its hover feedback."
- Added, the notifications: "The drop target shall call `OnDragEnter` when a drag meant for it first arrives and `OnDragOver` on each later DragOver, after updating its feedback." / "The drop target shall call `OnDragLeave` when its hover ends without an accepted drop: after the deferred leave, when a drag stops being meant for it, after a rejected drop, and when detached mid-drag." / "The drop target shall call `OnDrop` after an accepted drop, before clearing its hover."
- Changed **DD-DROP-018**: "After an accepted drop, the drop target shall call `OnDrop`, then clear `:dropover`, set the adorner invalid and hide it."
- Decisions: add "The base owns the flow; subclasses override steps", "Notifications cannot change the effect", "Notifications take the event args; state comes from properties", "Every target marks valid drag events handled", "`Routing` is removed from the drop side". Edge cases: remove the `Tunnel` bullet. Open items: remove both items.

**DD-ITEM**
- Changed **DD-ITEM-008**: "`ItemDropOperation` shall follow the DD-DROP flow, refuse drags that started from its own attached control, and transfer by setting `PayloadTarget` to the payload, or its copy on a Copy drop, reporting the previous value as `ReplacedPayload`." (DD-DROP-015's assignment moves here.)

**DD-COLLECTION**
- Purpose: the collection target runs the base flow and overrides its hover, location and transfer steps.
- Changed **DD-COLLECTION-020**: "The drop operation shall resolve its hover, location and auto-scroll on both DragEnter and DragOver, as steps of the DD-DROP flow."
- Struck **DD-COLLECTION-027** and **038**: moved to DD-DROP.
- Open items: remove the hooks item.

**DD-TREE**
- No requirement changes. Hover expand runs as part of the collection steps.

**ARCHITECTURE**, CLAUDE.md: describe the single drop flow, the transfer step and the notifications in place of the handler layer.

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **Notifications on every target.** One test class runs the same drag over an `ItemDropOperation`, a `CollectionDropOperation` and a `TreeDropOperation` subclass that records its notifications.
   - `DropNotificationTests.EveryTargetRaisesEnterThenOverThenDrop` (theory over the three kinds)
   - `DropNotificationTests.EveryTargetRaisesLeaveWhenTheDragLeaves`
   - `DropNotificationTests.ARejectedDropRaisesLeaveNotDrop`
   - `DropNotificationTests.DetachingMidDragRaisesLeave`
2. **One flow in the base.** Move the collection's `UpdateDrag` and `EndDrag` into the base flow with `private protected` steps; make the handlers private.
   - `DropTargetTests.AValidDragIsMarkedHandled`
   - `DropTargetTests.ADragThatStopsBeingMeantForTheTargetClearsTheHover`
   - `DropTargetTests.AnInnerTargetThatAcceptsWinsOverItsOuterTarget`
3. **Transfer step.** Add `Transfer`; move `PayloadTarget` assignment to `ItemDropOperation`; move the completion rule to the base.
   - `DropTargetTests.ACommandOnlyBaseSubclassCompletesAMove`
   - `DropTargetTests.ABaseSubclassWithoutACommandReportsNone`
   - Existing `ItemDropTests`, `CollectionDropTests` and `EndToEndTests` keep passing unchanged.
4. **Remove `Routing`.** Delete it from the drop side; update the routing theory to `HandledEventsToo` only.
5. **Docs.** ARCHITECTURE, CLAUDE.md, and the specs listed above.
6. **Manual check on the Win32 sample.** Every page's drags still work.
7. **Close.** Every changed and added requirement cites its tests, the Open items named above are removed, this proposal is deleted, and the backlog lines for the redesign and for `Routing = Tunnel` are removed.

## Open questions

- Should `OnDragOver` fire on every DragOver, or only when the hovered container, offset or validity changes? Every DragOver matches the event and is simpler; change-only is cheaper for overrides that do work. Recommended: every DragOver, as today.
