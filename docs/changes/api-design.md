# API design before 1.0

## Why

The public surface grew while the library was a prototype, and parts of it will be hard to change once consumers depend on it or once `PublicApiAnalyzers` baselines it (backlog: Docs and hygiene). The pre-1.0 review found the problems below: protected mutable fields exposed to every subclass, properties that bypass the attach contract, a validation API that needs `!` in every override, a structural check that overrides can lose, dead types and members, and duplicated code that subclasses must copy. All of them are breaking, so they should land together, before the API is frozen and before most headless tests are written against today's surface ([test-suite.md](test-suite.md) tasks 5–8).

## What

**Subclass surface.**
- The protected fields `_trackedControl`, `_dragOrigin` and `_pressedEventArgs` on `DragOperationBase` become private. `_isDragPending` is already removed by [correctness-fixes.md](correctness-fixes.md).
- `_handledEventsToo` on both bases is replaced by a public `HandledEventsToo` property next to `Routing`. Both may only be set while the operation is detached, and setting either while attached throws `InvalidOperationException`. `CollectionDragOperation` sets its tunneling, handled-events defaults in its constructor through these properties.
- `CreateMetadata` exists once per info type. The base builds `DragInfo` and receives the press origin as a parameter, `CollectionDragOperation` builds `CollectionDragInfo`, and `TreeDragOperation` only overrides a new `protected virtual IList? ResolvePayloadCollection(Control? container)`. No subclass needs the press origin field any more.
- The press filter becomes per element. The base still walks from the pressed element up to the attached control, and the first element that decides wins: one with the new `bool?` attached property `DragDrop.IgnoresPress` set (true ignores, false allows), or else one for which `protected virtual bool IsIgnoredPressSource(Visual element)` is true. By default the predicate is true for a `ScrollBar`, `Button`, `TextBox` or `Thumb`. Apps add a type with a style setting `IgnoresPress` and opt an element out with `IgnoresPress="False"`; subclasses override the predicate for rules that need code, and never repeat the walk.
- The pointer handlers stop being extension points. Private handlers own the subscription and call conventional hooks without a `sender`: `protected virtual void OnPointerPressed(PointerPressedEventArgs e)`, `protected virtual void OnPointerReleased(PointerReleasedEventArgs e)` and `protected virtual Task OnPointerMoved(PointerEventArgs e)`. They replace `Control_PointerPressed`, `Control_PointerReleased` and the `async void Control_PointerMoved`. The private moved handler awaits `OnPointerMoved`, so an override can await normally and anything it throws reaches that handler. That handler is where correctness-fixes puts its try/catch. `CollectionDragOperation`'s press filter moves to an `OnPointerPressed` override.

**Attach contract.**
- `AttachedControl` becomes a read-only direct property on both bases, set only by `Attach` and `Detach`, and still observable.
- `DragDrop` becomes a `static class`. Its attached properties register through the `Type ownerType` overloads, because a static class cannot be a generic type argument. The registrations move into `DragDrop.props.cs`, replacing its commented-out code.
- Operations remain one-per-control. Declaring one in a style is documented as unsupported, and the double-`Attach` exception from correctness-fixes names the operation and its control.

**Validation.**
- `DropOperationBase` gains a public read-only `ErrorMessage` direct property and `protected bool Reject(string? message)`, which sets it and returns false.
- `EvaluateCanDrop` clears `ErrorMessage` before `CanDrop` and copies it to `DropAdorner.ErrorMessage` afterwards, and the message is cleared when the drag leaves or drops. App UI can bind the message (a status bar, a tooltip) without reaching into the adorner. A collection's `CanDropAtLocation` runs inside the same evaluation, before `CanDrop`, so its `Reject` message is mirrored too.
- `TreeDropOperation`'s subtree rejection and the sample's custom operations use `Reject`.
- The logical-descendant check in `CollectionDropOperation.CanDrop` (DD-COLLECTION-024) is removed. Task 5 found no drag it guards against, and one it breaks: a collection target whose own surface is hit (a `Border` with a background) rejected drags over that surface.

**Copies.**
- `DropOperationBase` gains `protected virtual object CreateCopy(object payload)`, returning the payload by default. When the drop effect is Copy, single-value and collection targets assign or insert the value it returns, and `DropEventArgs.Items` carries that value.

**Adorners.**
- `DropHighlightAdorner` gains a styled `Kind` property (`DropAdornerKind`: `Solid`, `Border`, `None`), which drives `:outline` and visibility. It replaces the constructor parameter and the protected `DropOperationBase.DropAdornerType`. A border-only highlight is then one XAML element or one app style.
- `DropAdornerKind` moves to the root `MonacoLabs.DragDrop` namespace, so the XAML namespace reaches it.
- The unused `IDropAdorner` interface is deleted.
- `DropAdornerBase.TargetControlProperty` drops its `OneWayToSource` default binding mode.

**Removed.**
- `DropOperationBase.Context`, which nothing reads.

**Types.**
- `DragThreshold` changes from `int` to `double`, keeping the 6 DIP default.

**Not in this change.** The drop side's two overridable layers (the `DragEnter`/`DragOver`/`DragLeave`/`Drop` handlers and the `OnDragEnter`/`OnDragOver`/`OnDragLeave`/`OnDrop` hooks) stay as they are. Collection and tree targets replace the handlers, so fixing the layering is a redesign of the drop flow, not a rename. It is logged in the backlog.

What does not change: interaction ids, targeting rules, effect resolution, the completion contract (as amended by correctness-fixes), the visuals, and the XAML attached-property names (`dd:DragDrop.DragOperation`, `dd:DragDrop.DropOperation`, `dd:DragDrop.IsDragHandle`).

## Decisions

- **Routing options are settable only while detached.** `Routing` and `HandledEventsToo` are read when `Attach` subscribes, so a later change silently does nothing. Throwing makes the mistake visible, and XAML sets both before the attached property attaches the operation. Rejected: a protected constructor parameter (XAML users could not change the routing of a built-in operation); re-subscribing on change (state for a case nobody needs).
- **Subclasses get hooks, not fields.** Every protected member is a virtual method or a read-only property with a documented meaning. Reason: protected fields freeze the implementation, and the current ones let a subclass leave the press-tracking state inconsistent. The only current reader, the press origin in `CreateMetadata`, becomes a parameter.
- **The operation owns the validation message.** `ErrorMessage` lives on the operation and is mirrored to the adorner. Reason: validation belongs to the operation, overrides should not need `DropAdorner!`, and MVVM apps want to show the message outside the adorner. A message set directly on `DropAdorner.ErrorMessage` inside `CanDrop` is overwritten by the mirror, so overrides use `Reject`. Rejected: a `Reject` helper that writes only to the adorner (no bindable message); a `CanDrop` that returns a result object (a larger break for every override).
- **Structural checks live in `CanDropAtLocation`.** See DD-COLLECTION, "Structural checks run before CanDrop". The logical-descendant check was to move there, but task 5 found that every drag event reaching the target comes from a logical descendant (items, template parts, the empty area) except the attached control itself, which the check wrongly rejected, so the requirement is struck instead (DD-COLLECTION, "No logical-descendant check").
- **Copies come from a hook, not `ICloneable`.** Reason: what a copy of a view model means (deep or shallow, a new id or the same) is a domain decision, and `ICloneable` does not say. Rejected: calling `ICloneable.Clone` automatically; a bindable factory property (XAML cannot easily supply a delegate).
- **More controls keep their presses.** `TextBox` (caret placement and selection) and `Thumb` (sliders, splitters, scroll thumbs) join `ScrollBar` and `Button` by default. Reason: a selected row's press is handled in the tunneling phase (DD-DRAG-009), so a `TextBox` in it can't focus, and a selection drag turns into an item drag; a slider's thumb starts an item drag the same way. An app that drags rows by a read-only `TextBox` sets `DragDrop.IgnoresPress` to false on it. Rejected: opt-in only, which leaves editable rows broken by default.
- **Ignored presses are configured by a non-inherited attached property.** `DragDrop.IgnoresPress` is a `bool?` read during the walk, like `DragDrop.IsDragHandle`. Reason: styles and XAML cover the common cases without a subclass, and the walk keeps the boundary at the attached control. Rejected: an inherited property checked only on the pressed element. A probe showed Avalonia does not inherit per-type default values, so the defaults would need app-wide styles or the library setting values on unrelated controls, and values inherited from above the attached control (a drag source inside a `Button`) would ignore every press.
- **The press filter is a per-element predicate.** The base owns the walk up to the attached control; the attached property, then the virtual method, answers for one element. Reason: overrides add a type in one expression and cannot get the walk's stopping point wrong. Rejected: a virtual that receives the pressed element and walks itself, which every override would have to repeat.
- **Pointer hooks, not event handlers.** The subscribed handlers are private, and subclasses override `OnPointerPressed`, `OnPointerReleased` and a `Task`-returning `OnPointerMoved`. Reason: `Control_*` is designer-handler naming for what is a public extension point, `sender` is always the attached control, and a virtual `async void` forces overrides to be `async void` too, whose exceptions bypass the library's handling and crash the app. Rejected: keeping the handlers virtual and only renaming them (an override still decides whether the exception handling runs).
- **`DragDrop` keeps its name.** Reason: the name is in every consumer's XAML, and the XML namespace prefix already separates it from `Avalonia.Input.DragDrop`. C# code that imports both namespaces needs an alias, as the library itself does. Rejected: renaming to `DragDropOps` or similar (breaks all XAML for a C#-only annoyance).
- **Operations are not shareable through styles.** One instance per control stays the rule (DD-WIRING, "One operation instance per control"). Rejected: a template or factory property that creates an operation per styled control (useful, but a new feature; backlog candidate).
- **The threshold stays 6 DIPs, as a `double`.** Reason: Avalonia measures in DIPs as doubles. The platform's tap size depends on the pointer type and the OS, which would make the default untestable. Rejected: deriving the default from `IPlatformSettings`.

## Spec changes

**DD-WIRING**
- Added: "`AttachedControl` shall be read-only outside the operation and change only through `Attach` and `Detach`, raising a property change when it does."
- Added: "`DragDrop` shall be a static class."
- Edge cases: remove the `AttachedControl` setter edge case. Decisions: add "`DragDrop` keeps its name" and "Operations are not shareable through styles". Open items: remove the static-class and `AttachedControl` items.

**DD-DRAG**
- Changed **DD-DRAG-001**: "…with the strategy in `Routing`, including handled events when `HandledEventsToo` is true."
- Changed **DD-DRAG-003**: "When `Routing` and `HandledEventsToo` are not set, the drag source shall use bubbling routing and ignore handled events."
- Added: "When `Routing` or `HandledEventsToo` is set while the operation is attached, it shall throw `InvalidOperationException`."
- Changed **DD-DRAG-006**: the first element from the pressed element up to the attached control that decides (`DragDrop.IgnoresPress` set, else `IsIgnoredPressSource` true) chooses whether the press is ignored. By default `IsIgnoredPressSource` is true for a `ScrollBar`, `Button`, `TextBox` or `Thumb`.
- Added (inherited): "When `DragDrop.IgnoresPress` is not set, it shall be null."
- Edge cases:
  - Remove the `CreateMetadata` edge case.
  - Add: "The ignore check runs before the drag-handle check, so a drag handle inside an ignored control (a handle placed in a `Button`) never starts a drag."
- Threading and lifetime: replace "`Control_PointerMoved` is `async void`" with: the private moved handler awaits `OnPointerMoved` on the UI thread, and the handler returns while the platform drag is in progress.
- Decisions: add "Subclasses get hooks, not fields", "Pointer hooks, not event handlers", "Routing options are settable only while detached", "More controls keep their presses", "The press filter is a per-element predicate" and "The threshold stays 6 DIPs, as a `double`".
- Open items: remove the protected-fields and ignored-control items.

**DD-DROP**
- Added: the `Routing`/`HandledEventsToo` requirement, as for DD-DRAG.
- Changed **DD-DROP-002**: "When attached with no `DropAdorner`, the drop target shall create a `DropHighlightAdorner` with its default `Kind`…"
- Changed **DD-DROP-007**: "Before each `CanDrop` evaluation, the drop target shall clear its `ErrorMessage`; after it, it shall copy `ErrorMessage` to `DropAdorner.ErrorMessage`."
- Added: "`Reject(message)` shall set `ErrorMessage` to the message and return false."
- Added: "When a drag leaves the target or drops on it, the drop target shall clear `ErrorMessage` and the adorner's message."
- Added: "When an in-process drop's effect is Copy, the drop target shall assign the value `CreateCopy` returns, which by default is the payload itself."
- Changed **DD-DROP-015**: "set `PayloadTarget` to the payload, or to its copy (DD-DROP new requirement) when the effect is Copy…"
- Edge cases: replace the `DropAdorner!` edge case with: a message set directly on the adorner inside `CanDrop` is overwritten. Decisions: add "The operation owns the validation message" and "Copies come from a hook, not `ICloneable`". Open items: remove the `Context`, `DropAdornerType` and `DropAdorner!` items.

**DD-TRANSFER**
- Changed **DD-TRANSFER-012**: "…the single in-process payload (or its copy on a Copy drop) for an in-process drop…"

**DD-COLLECTION**
- Struck **DD-COLLECTION-024**, with the decision "No logical-descendant check".
- Added: "The drop operation shall validate drags over the attached control's own surface like drags over its content."
- Changed **DD-COLLECTION-023**: `CanDropAtLocation` runs inside the same validation as `CanDrop`, so its `Reject` message reaches `ErrorMessage`.
- Changed **DD-COLLECTION-030**: "A Copy drop shall insert the value `CreateCopy` returns, which by default is the reference the source holds."
- Edge cases: remove the `WorkerDropOperation` edge case. Open items: remove the DD-COLLECTION-024 and `CreateMetadata` items.

**DD-TREE**
- Changed **DD-TREE-016**: "The subtree check shall run in `CanDropAtLocation` before `CanDrop`…"
- Changed **DD-TREE-015**: "…reject the drop with `SelfDropMessage` as its error message."

**DD-ADORNERS**
- Front matter: remove `IDropAdorner` from `types`, and the `MonacoLabs.DragDrop.Adorners` namespace.
- Changed **DD-ADORNERS-009**: "While `Kind` is `Border`, the highlight adorner shall have the `:outline` pseudo-class and a transparent background; while it is `None` it shall be invisible."
- Added (inherited): "When `Kind` is not set, it shall be `Solid`."
- Open items: remove the `IDropAdorner`, namespace and `OneWayToSource` items.

**ARCHITECTURE.md §4**: the namespace decision names only `MonacoLabs.DragDrop.Abstractions` as an exception.

**CLAUDE.md**:
- The Conventions namespace line drops `DropAdornerKind`.
- The custom-validation line says to call `Reject(message)` instead of setting `DropAdorner.ErrorMessage`.
- The operation-hierarchy paragraph drops the `_handledEventsToo` wording in favor of `HandledEventsToo`.

## Tasks

Each task writes or adjusts its tests first, then changes the code, updates the sample, updates the spec and cites the tests. It depends on [test-suite.md](test-suite.md) tasks 1–4. It overlaps [correctness-fixes.md](correctness-fixes.md) at two points. `_isDragPending` is removed by whichever lands first. The pointer-moved handler is where correctness-fixes adds its try/catch: in the private handler around `OnPointerMoved` if this change landed first, or in `Control_PointerMoved` otherwise, moved here by task 2.

1. **Attach contract.** Read-only `AttachedControl`, static `DragDrop` with registrations in `DragDrop.props.cs`. Tests:
   - `WiringTests.AttachedControlChangesOnlyThroughAttachAndDetach`
   - `WiringTests.AttachedControlRaisesAPropertyChange`
   - the existing `WiringTests` stay green
2. **Routing options and pointer hooks.**
   - Add `HandledEventsToo` on both bases with the detached-only rule, and make the fields private. `CollectionDragOperation` uses the properties in its constructor.
   - Replace the three `Control_*` handlers with private handlers calling `OnPointerPressed`, `OnPointerReleased` and `Task OnPointerMoved`, and move `CollectionDragOperation`'s press filter to `OnPointerPressed`.

   Tests:
   - `DragGestureTests.SettingRoutingWhileAttachedThrows`
   - `DropTargetTests.SettingHandledEventsTooWhileAttachedThrows`
   - `CollectionDragTests.PressesOnAListBoxItemReachTheOperation` (DD-COLLECTION-001)
   - `CollectionDragTests.PressesOutsideAnItemContainerAreNotTracked` (DD-COLLECTION-002, through the new override)
   - `DragGestureTests.AnOnPointerMovedOverrideCanAwaitBeforeCallingTheBase`
3. **One `CreateMetadata` per info type.** Base with the origin parameter, the collection override, and the tree `ResolvePayloadCollection`. Tests: `TreeDragTests.ThePayloadCollectionIsTheParentsItemsSource` (DD-TREE-003) and `CollectionDragTests` DD-COLLECTION-006 stay green.
4. **Press filter.** `DragDrop.IgnoresPress` and the per-element `IsIgnoredPressSource`, with `TextBox` and `Thumb` added. Tests:
   - `DragGestureTests.PressesInsideATextBoxDoNotStartDrags`
   - `DragGestureTests.ATextBoxInASelectedRowReceivesItsPress`
   - `DragGestureTests.PressesOnASliderThumbDoNotStartDrags`
   - `DragGestureTests.OverridingIsIgnoredPressSourceAddsControls`
   - `DragGestureTests.ADragHandleInsideAButtonDoesNotStartADrag`
   - `DragGestureTests.AStyleCanIgnorePressesOnAControlType`
   - `DragGestureTests.IgnoresPressFalseLetsAnIgnoredControlStartDrags`
   - `DragGestureTests.TheElementNearestThePressDecides`
   - `DragGestureTests.IgnoresPressDefaultsToNull`
5. **Validation message and structural check.**
   - Add `ErrorMessage` and `Reject`, mirror the message to the adorner, and run `CanDropAtLocation` inside the same evaluation.
   - Before moving the check, look for a drag whose source is not a logical descendant (adorner layer, popup, overlay) with a test. If none exists, strike the requirement instead.
   - Migrate `WorkerDropOperation`, `NodeDropOperation`, `ImageDropOperation`, `MenuItemDropOperation` and `TreeDropOperation` to `Reject`.

   Tests:
   - `DropTargetTests.RejectSetsTheMessageAndTheAdornerMirrorsIt`
   - `DropTargetTests.TheMessageIsClearedBeforeEachEvaluation`
   - `DropTargetTests.TheMessageIsClearedWhenTheDragLeaves`
   - `CollectionDropTests.ADragOverTheTargetsOwnSurfaceIsAccepted`
   - `TreeDropTests.DroppingANodeIntoItselfShowsTheSelfDropMessage`
6. **`CreateCopy`.** Tests:
   - `DropTargetTests.ACopyDropAssignsTheCreatedCopy`
   - `CollectionDropTests.ACopyDropInsertsTheCreatedCopy`
   - `CollectionDropTests.AMoveDropIgnoresCreateCopy`
   - `EndToEndTests.CopyingWithAnOverriddenCreateCopyLeavesTheSourceUnchanged`
7. **Adorners.** `Kind` on `DropHighlightAdorner`, removal of `DropAdornerType`, the namespace move of `DropAdornerKind`, deletion of `IDropAdorner`, and `TargetControl`'s binding mode. Tests:
   - `DropAdornerTests.BorderKindHasTheOutlinePseudoClass`
   - `DropAdornerTests.NoneKindIsInvisible`
   - `DropAdornerTests.KindCanBeSetFromAStyle`
8. **Removals and types.** Delete `Context` and make `DragThreshold` a `double`. Tests: `DragGestureTests` DD-DRAG-011, 012 and 022 stay green with fractional thresholds.
9. **Sample.** Show `Kind="Border"` on one drop target and a bound `ErrorMessage` on the List page. Manual, on the Win32 Desktop head:
   - every page's drags and validation messages behave as before
   - a `TextBox` in a draggable row stays editable
   - the border highlight shows
10. **Docs and close.** Make the ARCHITECTURE and CLAUDE.md edits, and confirm `SpecTests` passes. Delete this proposal and the backlog lines it resolved; the `PublicApiAnalyzers` backlog item becomes actionable.

## Open questions

None.
