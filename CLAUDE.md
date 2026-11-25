# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

MonacoLabs.DragDrop is a WIP/prototype MVVM-friendly drag-and-drop library for Avalonia 12, plus a demo app (`DragDropSample`) that exercises it, and a headless xUnit v3 test project (`MonacoLabs.DragDrop.Tests`). There is no CI or lint/format config yet ([docs/changes/test-suite.md](docs/changes/test-suite.md) plans CI).

## Docs

| Path | Content |
|---|---|
| `docs/ARCHITECTURE.md` | Goals, architecture map, testing strategy, project-wide decisions, sample |
| `docs/specs/dragdrop/` | Feature specs, the source of truth for behavior; format in `docs/specs/README.md`, index in `index.md` |
| `docs/changes/` | Change proposals for work in progress |
| `docs/BACKLOG.md` | Open work: bugs and gaps, docs and hygiene, manual checks, candidates |

**Specs:** before changing a feature's behavior, read its spec (and the specs in its `depends`), and update its requirements and `Tests:` lines in the same change. A change that diverges from its spec without updating it is incomplete. Design decisions and their reasons live in the spec's Decisions section, or ARCHITECTURE §4 when project-wide. New work that needs design starts as a proposal in `docs/changes/`; small fixes come straight from the backlog.

## Commands

```powershell
dotnet build MonacoLabs.DragDrop.slnx
dotnet run --project DragDropSample
dotnet test --project MonacoLabs.DragDrop.Tests
```

- Targets `net10.0`; the solution is a `.slnx` file. `global.json` pins SDK 10.0 with `rollForward: latestMajor` and selects the Microsoft Testing Platform runner for `dotnet test`.
- `xunit.v3` is pinned to 3.2.2: `Avalonia.Headless.XUnit` 12.2 fails discovery of every `[AvaloniaFact]` on 4.x.
- Package versions are centrally managed in `Directory.Packages.props`. Add `<PackageVersion>` there and a version-less `<PackageReference>` in the csproj.
- Debug builds of the sample reference `AvaloniaUI.DiagnosticsSupport` and call `WithDeveloperTools()`, so the Avalonia DevTools MCP (`attach-to-app`) can inspect the running demo's visual tree, adorners, and pseudo-classes.

## Tests (`MonacoLabs.DragDrop.Tests`)

- Pure helpers in the library's `Internals/` (effect resolution, removal index, auto-scroll step) get plain `[Fact]`/`[Theory]` tests through `InternalsVisibleTo`. Anything with controls uses `[AvaloniaFact]`, which runs on the headless UI thread with Skia and the Fluent theme (`TestApp`).
- Helpers in `Infrastructure/`: `TestWindow.Show(content)` and `window.PointIn(visual)`; `DragData.For(operation, payload)` builds the `DataTransfer` an operation would start with; `window.SimulateDrag(data)` raises drag events as if from another app and returns the effect the app reports; `window.MouseDrag(from, to)` runs a real drag, because headless `DoDragDropAsync` is Avalonia's in-process drag driven by mouse input; `TestDispatcher.PumpUntil(condition)`; `RecordingDropOperation`; `RelayCommand`.
- Headless `DispatcherTimer`s fire only while the test awaits (`PumpUntil`, `await Task.Delay`), never from `Dispatcher.UIThread.RunJobs()`.
- A control that handles its own presses (`TextBox`, `Button`, `Slider`) hides them from a bubbling drag source, so press-filter tests use `Routing = Tunnel`. A `TextBlock` without a `Background` is hit-testable only on its glyphs. Item templates use fixed sizes so hover zones don't depend on fonts.
- Test methods read as behaviors and are cited from the specs' `Tests:` lines in the same change.

## Library architecture (`MonacoLabs.DragDrop`)

### Wiring
`DragDrop` exposes two attached properties, `DragDrop.DragOperation` and `DragDrop.DropOperation` (`BindingMode.OneTime`). Setting one calls `Attach(control)` on the operation and `Detach` on the old one. An operation instance can attach to only one control and throws if reused.

Operations are `AvaloniaObject`s, not `StyledElement`s, so they have no `DataContext` of their own. A plain `{Binding Items}` still works: Avalonia anchors bindings on non-control targets to the nearest parent `Control` in the XAML and uses that control's `DataContext`. Don't use `#root.DataContext...` paths. Named-element bindings on non-`ILogical` targets log "Value is null" while the view's `DataContext` is still unset, and `ViewLocator` only sets it after construction.

Avalonia's Win32 COM layer swallows exceptions thrown from drop handlers (DragEnter/DragOver/Drop) and returns `E_FAIL`. That surfaces as a `COMException` from `DoDragDrop`, so the real exception is only visible as a first-chance exception in the debugger.

### Drag → drop data flow
1. `DragOperationBase` hooks pointer events on the attached control. After a press and a move past `DragThreshold`, it calls Avalonia's `DoDragDropAsync`. It passes the stored *pressed* event args because Avalonia requires them.
2. The `DataTransfer` holds the payload once per entry in `InteractionIds`, each under an in-process `DataFormat` keyed by the id (`DragDropIds.PayloadFormat`). It also holds a `DragInfo` under `app:dragmetadata`. The payload comes from `ResolvePayload`: `Payload` if it is set or bound (even when null), otherwise `AttachedControl.DataContext`. `CollectionDragOperation` uses the pressed item container's item instead, so the payload always matches `PayloadContainerIndex`, even with multiple selection.
3. `DropOperationBase.IsDropTargetFor` decides whether a drag is meant for the target at all: one of its `InteractionIds` is present and, unless `AllowsSelfDrop` (true for collections), the drag didn't start from the same control. Drags that fail get no adorner. `CanDrop` (via `EvaluateCanDrop`, which clears `ErrorMessage` first, runs a collection's `CanDropAtLocation` before `CanDrop`, and mirrors the message to the adorner) then validates. Interaction ids act as typed channels: one drag can advertise several ids, and each drop target accepts a subset.
4. On drop, the drop side sets `PayloadTarget` (TwoWay) and calls `dragInfo.DragOperation.DropCompleted(...)`. **The drag side is responsible for removing the item from its source.** The drop side then invokes `PayloadCommand` with a `DropEventArgs` and calls the `OnDrop` hook.

`GetDropEffect` recomputes the effect on every DragOver and Drop from the source's allowed effects and the modifier keys: Ctrl (Option on macOS) copies, Shift moves, both link, otherwise Move > Copy > Link. On Drop, `e.DragEffects` arrives as the *allowed* set, so it must be recomputed before calling `DropCompleted`, which only removes on an exact `Move`.

### Platform data (`PlatformInteractionIds`)
`platform:files` and `platform:text` map to `DataFormat.File` / `DataFormat.Text` (`DragDropIds.FormatFor`). On the drag side they add `ResolvePlatformFile` / `ResolvePlatformText` output to the `DataTransfer`. On the drop side they accept that data from any source. A drop is a "platform drop" when no in-process payload matches the target's ids. Platform drops never touch `PayloadTarget` or call `DropCompleted`; `PayloadCommand` gets the files or text in `Items` with `IsPlatformDrop = true`. They always resolve to Copy (or Link), because returning Move to Explorer makes it delete the original file. Use `TryGetFiles` / `TryGetText` in `CanDrop` overrides.

### Operation hierarchy (`Operations/`)
- `ItemDragOperation` / `ItemDropOperation`: single-value transfer (`PayloadTarget` is replaced). `ItemDragOperation.Payload` binds TwoWay by default. When it is set and the effect is Move, `DropCompleted` clears it, or with `SwapOnDrop` assigns `DropInfo.ReplacedPayload` (the target's previous value). Self-drops are blocked by `IsDropTargetFor`, otherwise a slot dropped on itself would be cleared.
- `CollectionDragOperation` (derives from `DragOperationBase`, not `ItemDragOperation`) / `CollectionDropOperation`: `PayloadTarget` and `PayloadCollection` are `IList`s. The drop side computes `DropLocation` (`CollectionDropLocation`: collection, index, parent) from the hovered container and `HoveredOffset`, then inserts (`TransferPayloadIndexed`) or appends (`TransferPayload`). It only transfers when `TransfersPayload` (default: `PayloadTarget` is an `IList`). `DropLocation` falls back to the control's `ItemsSource`, so platform drops get an insertion index without a `PayloadTarget`. The drag side's `DropCompleted` then removes the original, shifting the index by +1 for same-collection moves where the source index is at or after the target. `CollectionDragOperation` sets `Routing` to `Tunnel` and `HandledEventsToo` to true because `ListBoxItem` handles pointer presses for selection.
- `CollectionDropOperation` drives everything from one `UpdateDrag` path (DragEnter and DragOver): hover container, offset, location, validation, adorners, pseudo-classes, and auto-scroll (`AutoScrollMargin`, a `DispatcherTimer` scrolling the first descendant `ScrollViewer`). `EndDrag` resets it on leave or drop.
- `TreeDragOperation` / `TreeDropOperation`: the drag side infers `PayloadCollection` from the parent's `ItemsSource`. The drop side reads collections from the tree, so `PayloadTarget` is unused and it always transfers. Zones use the item's `PART_Header` (a `TreeViewItem`'s bounds include its children): top/bottom quarter is before/after, the middle is inside, but only if the container has an `IList` `ItemsSource`. "After" an expanded node with children inserts as its first child. `CanDropAtLocation` rejects drops into the dragged node's own subtree (it runs before `CanDrop`, so overrides can't skip it). Collapsed nodes expand after `HoverExpandDelay` over their middle.
- A left press is tracked for a drag unless `CanDrag` is false, the press is ignored, or `RequiresDragHandle` is set and no ancestor up to `AttachedControl` has `DragDrop.IsDragHandle`. A press is ignored when, walking from the pressed element up to `AttachedControl`, the first element that decides ignores it: one with the `bool?` `DragDrop.IgnoresPress` set (true ignores, false allows), or else one `IsIgnoredPressSource` matches (by default `ScrollBar`, `Button` (e.g. the tree expander), `TextBox`, `Thumb`). If the pressed element's nearest `ISelectable` ancestor (up to `AttachedControl`) is unselected, the press stays unhandled so the item selects itself in the same gesture. `SelectingItemsControl.UpdateSelectionFromEvent` ignores handled events. Presses on a selected item are handled so an existing multi-selection survives. On an `ItemsControl`, `CollectionDragOperation` only starts a drag from a press inside an item container.
- `RemoveOnDropOutside`: `Internals/DragSession` (one per drag; also owns `:dragging` and the preview) sets `AllowDrop` on the window and watches its drag events. Over areas without an explicit drop target it returns a non-None effect when tracking, because Win32 raises `DragLeave` instead of `Drop` when the last DragOver returned None. A Drop there sets `DroppedOutsideTarget`, and the drag op calls `OnDroppedOutside` after `DoDragDropAsync` returns. Drops outside the window and Esc are indistinguishable, so they never remove.
- Custom validation is done by subclassing and overriding `CanDrop`. Use `TryGetDragInfo<T>`, `TryGetPayload<T>`, and `DropLocation` for collections and trees, and reject with `return Reject(message)`, which sets the bindable `ErrorMessage` (see `DragDropSample/ListBoxPage/WorkerDropOperation.cs` and `TreeViewPage/NodeDropOperation.cs`).

### Adorners and styling hooks (`Adorners/`)
`DropAdornerBase` is a `Border`. It is placed in the `AdornerLayer` over `TargetControl`, is not hit-testable, and gets the `:error` pseudo-class when `!IsDropValid`. `DropHighlightAdorner` covers the whole drop target; its styled `Kind` (`Solid`, `Border`, `None`) only sets pseudo-classes that its built-in `ControlTheme` styles, so app styles still win. `DropInsertionAdorner` is passive: the operation calls `Update(DropTargetOffset)`, and it draws a line above (`BeforeTarget`) or below (`AfterTarget`) its target, or an outline (`OnTarget`). One instance is retargeted as the hover moves, so styles set on a custom `ItemDropAdorner` survive.

The library sets pseudo-classes for consumers to style:
- `:dropover` on the drop control and the hovered item container.
- `:droptop` / `:dropbottom` / `:dropinside` on the hovered item container (set by the operation, not the adorner).
- `:outline` / `:hidden` on `DropHighlightAdorner` for `Kind` `Border` / `None`.
- `:dragging` on the drag visual, and `:nodrop` / `:copy` / `:remove` on `DragPreviewAdorner`.

### Conventions
- Avalonia property registrations live in `*.props.cs` partial files next to the logic file (`DragDrop.props.cs`, `DragOperationBase.props.cs`, ...).
- Most public types use the root `MonacoLabs.DragDrop` namespace regardless of folder. The exception is `MonacoLabs.DragDrop.Abstractions`. The `XmlnsDefinition` maps only the root namespace. The samples use `xmlns:dd="using:MonacoLabs.DragDrop"`.
- Use `ThrowIf` (in `Internals/`) for guard clauses.
- The library has `ImplicitUsings` enabled. The sample does not, so it uses explicit `using System;` etc.

## Demo app architecture (`DragDropSample`)

- Feature folders (`SingleItemPage/`, `SimpleCollectionPage/`, `ListBoxPage/`, `TreeViewPage/` (tree ↔ list, plus the shared `Nodes/` and `NodeDropOperation`), `TreeToTreePage/`, `DesktopPage/` (platform files/text), `Shell/`). Custom operations use the `DragDropSample.CustomDragDrop` namespace. Namespaces are **not** folder-based: views use `DragDropSample.Views` and view models use `DragDropSample.ViewModels`. `ViewLocator` resolves a view by replacing "ViewModel" with "View" in the full type name, so a new page must follow this naming and namespace scheme.
- To add a page, create a `PageViewModel` subclass (set `Title`) and a matching `UserControl` view, then add the view model to `MainWindowViewModel.Pages`.
- MVVM uses CommunityToolkit.Mvvm with partial-property source generation (`[ObservableProperty] public partial T Name { get; set; }`) and `[RelayCommand]`.
- `AvaloniaUseCompiledBindingsByDefault` is on, so XAML needs `x:DataType`. Use `ReflectionBinding` only where compiled bindings can't resolve the path.
- Demo data is generated with Bogus (`ListBoxPageViewModel`).
