# MonacoLabs.DragDrop — Architecture

An MVVM-friendly drag-and-drop library for Avalonia 12, plus a sample app that exercises it. This document is the map: goals, the architecture, how it is tested, the project-wide decisions and the sample. What each feature does, and why, is in the [feature specs](specs/README.md). Work not done yet is in the [backlog](BACKLOG.md), and new work starts as a [change proposal](changes/README.md).

## 1. Goals and non-goals

**Goals**

- Drag and drop declared entirely in XAML, with view models that only expose properties, collections and commands. No drag-and-drop code in view models or code-behind.
- Correct move semantics between single values, flat lists and trees, within one control and between controls, including reordering and dragging a selection of several items.
- Validation that explains itself: a drop target can reject a drag and say why, in place.
- Exchange of files and text with other applications through the same mechanism as in-app drags.
- Styling hooks (pseudo-classes, themeable adorners) rather than hard-coded visuals.

**Non-goals**

- Cross-process in-app payloads. Only files and text leave the process.
- Touch-specific gestures, keyboard drags, and native OS drag images.
- Hierarchical controls other than `TreeView` (no `TreeDataGrid`).

## 2. Architecture

One package, `MonacoLabs.DragDrop` (`net10.0`, depends only on `Avalonia`). The spec index is [specs/dragdrop/index.md](specs/dragdrop/index.md).

### 2.1 Wiring

`DragDrop.DragOperation` and `DragDrop.DropOperation` attach an operation object to a control. An operation subscribes to the control's pointer or drag events and keeps per-control state, so each instance attaches to one control (DD-WIRING).

### 2.2 Drag → drop data flow

1. The drag source (DD-DRAG) tracks a press, waits for the threshold, resolves the payloads (the selection, for collection and tree sources) and starts Avalonia's `DoDragDropAsync` inside a drag session (DD-SESSION).
2. The `DataTransfer` holds one item per payload, each holding its payload once per interaction id, plus a `DragInfo` on the first (DD-TRANSFER). Platform ids add files or text (DD-PLATFORM).
3. The drop target (DD-DROP) runs one flow on every DragEnter and DragOver, for every kind of target: it decides whether the drag is meant for it, resolves what is under the pointer, validates it with `CanDrop`, shows its adorners (DD-ADORNERS) and reports an effect (DD-EFFECTS).
4. On drop, the same steps run, then the target's `Transfer` step assigns or inserts the payloads, the target calls the drag operation's `DropCompleted`, and runs `PayloadCommand`. The drag side removes its originals when the effect is Move.
5. Every target raises the same notifications, `OnDragEnter`, `OnDragOver`, `OnDragLeave` and `OnDrop`, after the library has done its work for the event.

### 2.3 Operations

| Drag | Drop | Spec |
|---|---|---|
| `ItemDragOperation` | `ItemDropOperation` | DD-ITEM: one value replaces another |
| `CollectionDragOperation` | `CollectionDropOperation` | DD-COLLECTION: insert into and remove from `IList`s |
| `TreeDragOperation` | `TreeDropOperation` | DD-TREE: per-node collections, three zones, subtree protection |

All derive from `DragOperationBase` / `DropOperationBase` in `MonacoLabs.DragDrop.Abstractions`. Both bases subscribe private event handlers and expose overridable steps and hooks: pointer hooks on the drag side, and on the drop side `IsDropTargetFor`, `CanDrop`, `GetDropEffect`, `CreateCopy`, `Transfer` and the four notifications. Consumers customize validation by subclassing a drop operation and overriding `CanDrop` (and, for trees and collections, reading `DropLocation`).

### 2.4 Source layout

| Folder | Content |
|---|---|
| `MonacoLabs.DragDrop/` | `DragDrop` attached properties, interaction ids, assembly attributes |
| `Abstractions/` | Base operations and their interfaces |
| `Operations/` | Item, collection and tree operations |
| `Info/` | `DragInfo`, `DropInfo`, collection variants, `DropEventArgs`, `CollectionDropLocation`, `DropTargetOffset` |
| `Adorners/` | Drop adorners and the drag preview |
| `Internals/` | `DragSession`, `ThrowIf` |
| `DragDropSample/` | The sample app (§5) |
| `docs/` | This map, the specs, the backlog and change proposals |

## 3. Testing strategy

`MonacoLabs.DragDrop.Tests` is an xUnit v3 project on Avalonia's headless platform (`Avalonia.Headless.XUnit`, which pins `xunit.v3` to 3.2.2). CI runs it on Windows and Linux. Every spec requirement cites the tests that pin it in its `Tests:` line, or says why it has none.

**Layers.**

1. **Pure unit tests** (`[Fact]`/`[Theory]`) for the logic extracted into internal static helpers in `Internals/`: effect resolution, the collection removal index, the auto-scroll step, `ThrowIf` and the interaction-id format mapping. They need no window.
2. **Headless component tests** (`[AvaloniaFact]`) for each operation, adorner and the drag session, inside a real `Window`. Drop targets get drag events raised on the window as if from another app (`SimulateDrag`); drag sources get synthetic mouse input.
3. **End-to-end tests**, where synthetic mouse input drags from a real source into a real target. Headless `DoDragDropAsync` is Avalonia's in-process drag driven by that input, so the real `DataTransfer`, drag session and `DropCompleted` run with nothing in the library replaced. A drag can be held between moves to look at it in flight.

**Test app.** `TestApp` adds the Fluent theme, without which `ListBox` and `TreeView` have no containers, and renders with Skia (`UseHeadlessDrawing = false`), so preview snapshots and the few pixel checks work.

**Conventions.**

- Test methods read as behaviors (`DroppingInTheTopHalfInsertsBeforeTheItem`) and are cited from the specs in the same change.
- Item templates use fixed sizes, so hover zones and indexes do not depend on the machine's fonts. This is what lets CI run on Linux.
- Headless `DispatcherTimer`s run on the real clock and fire only while the test awaits, so timer tests (hover expand, auto-scroll, the adorner fade) are async and use short intervals.
- Adorners are tested through pseudo-classes, `IsAttached` and visibility. Pixels are read only where nothing else shows the behavior: whether the insertion line draws at all, its thickness and marker, and that it is not clipped.
- The helpers live in `Infrastructure/`; CLAUDE.md lists them.

**Manual checks.** Behavior that needs the real OS stays manual: OLE drags with Explorer and other apps, drops outside the window on Win32, and render scaling above 100%. Each is a `manual —` requirement in its spec and a line in the backlog's manual checks. The headless drag does not reproduce Win32's DragLeave-instead-of-Drop after a None DragOver, so the tests assert the non-None effect that prevents it (DD-SESSION-010).

**Decisions.**

- **xUnit v3 with `Avalonia.Headless.XUnit`, pinned to `xunit.v3` 3.2.2.** `Avalonia.Headless.XUnit` 12.2 is built on xUnit v3 3.2.2, so `[AvaloniaFact]` runs each test on the headless UI thread with no custom fixture. On `xunit.v3` 4.x every `[AvaloniaFact]` fails discovery with a `MissingMethodException`. Revisit when `Avalonia.Headless.XUnit` ships against 4.x. Rejected: NUnit with `Avalonia.Headless.NUnit` (no advantage); a hand-rolled dispatcher fixture.
- **Microsoft Testing Platform runner.** xUnit v3 test projects are executables that support it natively, and `global.json` selects it for `dotnet test`. `global.json` pins a 10.0 SDK with `rollForward: latestMajor`, so CI uses 10.0 and newer local SDKs still work. Rejected: VSTest (`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`), kept only as a fallback if an IDE cannot discover the tests.
- **Tests drive the real platform drag.** The headless platform registers Avalonia's in-process drag source, so `DoDragDropAsync` runs as in production, driven by synthetic mouse input, and delivers the real `DataTransfer` and allowed effects. It completes only on release or Escape, so the helpers always release the mouse. It does not reproduce two Win32 behaviors: DragLeave instead of Drop after a None DragOver, and a drop handler's exception surfacing as a `COMException`; a failing drag is simulated by overriding `DoDragDrop`. Rejected: an internal hook replacing `DoDragDropAsync`, which would leave the in-process path untested.
- **`SimulateDrag` reads the reported effect from `RouteFinished`.** The headless `TopLevel.DragDrop` extension hit-tests and routes like the platform but returns nothing, so the effect is read after every handler has run, the drag session's window handlers included.
- **Timers run on the real clock, only while the dispatcher waits.** `Dispatcher.UIThread.RunJobs()` never runs `DispatcherTimer`s; they fire when a test awaits. `PumpUntil` is async, and timer tests use short intervals.
- **Real Skia rendering.** `UseHeadlessDrawing = false`, so preview snapshots and pixel checks work. Rejected: headless drawing stubs, which make the preview tests impossible.
- **Pure logic is extracted, not mocked.** Effect resolution, the removal index and the auto-scroll step are internal static helpers the operations call, so their many cases run without a window. Rejected: mocking `DragEventArgs` (Avalonia types are not designed for it).
- **Plain xUnit `Assert`.** Rejected: FluentAssertions (commercial license since v8) and Shouldly (an extra dependency for little gain).
- **No spec-format tests.** AI agents maintain the specs and read past small format drift, so tests enforcing the spec schema cost more in friction than they return. Cited tests are still checked when specs change. Rejected: a `SpecTests` class, built and removed.
- **CI runs on Windows and Linux.** Fixed-size item templates remove the font dependence that keeps many Avalonia suites Windows-only. CI runs on pushes to `main`, pull requests and manual runs, skips Markdown-only changes, and packs nothing yet. The Windows leg also checks formatting (`dotnet format --verify-no-changes`) and uploads a Cobertura coverage report, with no threshold until a few reports have set a baseline.

## 4. Project-wide decisions

- **MVVM first.** View models expose collections, properties and `ICommand`s, and never reference controls or drag events. Validation that needs domain knowledge lives in a `CanDrop` override in the view layer (the sample's `CustomDragDrop` operations), not in the view model.
- **Bindings on operations use the host control's DataContext.** Operations are `AvaloniaObject`s with no `DataContext`. Avalonia anchors bindings on non-control targets to the nearest parent control in XAML, so a plain `{Binding Items}` works. Reason: making operations `StyledElement`s would put them in the logical tree for no benefit. Avoid `#root.DataContext…` paths: named-element bindings on non-`ILogical` targets log "Value is null" until the view's `DataContext` is set, which `ViewLocator` only does after construction.
- **Interaction ids, not types, select drop targets.** See DD-TRANSFER, "Interaction ids are typed channels".
- **The drag side removes.** See DD-COLLECTION, "The drag side removes the payload".
- **Never report Move for platform data.** See DD-EFFECTS, "Platform data is always copied or linked".
- **Visual feedback is styleable.** The library sets pseudo-classes on the controls involved (`:dropover`, `:droptop`, `:dropbottom`, `:dropinside`, `:dragging`, and `:error`, `:nodrop`, `:copy`, `:remove` on adorners) and takes its default adorner looks from control themes, so app styles override them.
- **Avalonia property registrations live in `*.props.cs` partial files** next to the logic file.
- **Most public types use the root `MonacoLabs.DragDrop` namespace regardless of folder.** The exception is `MonacoLabs.DragDrop.Abstractions`. The XML namespace maps only the root namespace.
- **Guard clauses use `ThrowIf`** (`Internals/ThrowIf.cs`).
- **Package versions are central** in `Directory.Packages.props`.
- **Packaging.** Only `MonacoLabs.DragDrop` packs; the sample and tests set `IsPackable` to false. The package carries the XML documentation file, `docs/PACKAGE.md` as its README (absolute image URLs, since nuget.org can't resolve repo-relative ones), an MIT license expression, and a `.snupkg` with Source Link. `ContinuousIntegrationBuild` is on under GitHub Actions, so CI builds are deterministic. CS1591 stays suppressed (agreed 2026-11-26, revisit before 1.0): public types and members get a summary, but self-evident constructors and `*Property` fields don't, per the comment rules. The public API is recorded by `Microsoft.CodeAnalysis.PublicApiAnalyzers` in `PublicAPI.Unshipped.txt`, so every API change shows up in review as a diff to that file; CI fails on a change the file doesn't record (RS0016/RS0017 as errors under `ContinuousIntegrationBuild`). To cut a pre-release, set `Version` in the csproj and run `dotnet pack -c Release`; the entries move to `PublicAPI.Shipped.txt` at 1.0.0. The first version is 0.7.0; 1.0.0 follows once the open proposals have landed.

## 5. Sample

`DragDropSample` is a page-per-feature demo, and the only consumer of the library so far.

| Page | Exercises |
|---|---|
| `SingleItemPage` | DD-ITEM: menu (Copy only) → order slots, slot ↔ slot with `SwapOnDrop`, drag handles, `RemoveOnDropOutside`, validation messages |
| `SimpleCollectionPage` | DD-COLLECTION: two lists with Move and Copy, selection drags |
| `ListBoxPage` | DD-COLLECTION: one source feeding three targets through interaction ids, domain validation of every dragged worker (`WorkerDropOperation`) |
| `FilteredCollectionPage` | DD-COLLECTION: a filtered, optionally sorted view over a backing list (display-index mapping, `ItemOrder="Sorted"`), a plain list on the other side, `CreateCopy` (`TaskDropOperation`) |
| `TreeViewPage` | DD-TREE: tree ↔ flat list, node rules (`NodeDropOperation`), selection drags |
| `TreeToTreePage` | DD-TREE: two trees, selection drags |
| `DesktopPage` | DD-PLATFORM: files and text with other applications (several at once from a selection), image validation (`ImageDropOperation`) |

Decisions:

- **Namespaces are not folder-based.** Views use `DragDropSample.Views` and view models `DragDropSample.ViewModels`, because `ViewLocator` maps a view model to its view by replacing "ViewModel" with "View" in the full type name. Custom operations use `DragDropSample.CustomDragDrop`.
- **Compiled bindings by default.** XAML needs `x:DataType`; `ReflectionBinding` only where compiled bindings cannot resolve the path.
- **CommunityToolkit.Mvvm** with partial-property `[ObservableProperty]` and `[RelayCommand]`; demo data from Bogus.
- **Debug builds include Avalonia DevTools** (`WithDeveloperTools()`), so the DevTools MCP can inspect adorners and pseudo-classes in the running sample.
