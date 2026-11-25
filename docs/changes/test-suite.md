# Introduce the xUnit v3 test suite

## Why

The library is approaching a stable API, and nothing checks it automatically: there is no test project and no CI, and every requirement in `docs/specs/` is `untested`. The pre-1.0 review found eight bugs (since fixed by the correctness-fixes change), all of them reachable only through specific gestures that nobody re-checks by hand after each change. Before the API settles and the bugs are fixed, the library needs a suite that pins the specified behavior, lets each fix start from a failing test, and keeps the specs honest through their `Tests:` lines.

## What

A new test project, `MonacoLabs.DragDrop.Tests/`, built on xUnit v3 and Avalonia's headless platform, added to `MonacoLabs.DragDrop.slnx`, and run with `dotnet test` locally and in a new GitHub Actions workflow.

**Layers.**

1. **Pure unit tests** (`[Fact]`/`[Theory]`, no Avalonia platform) for logic extracted into internal static helpers: effect resolution (DD-EFFECTS), the collection removal index (DD-COLLECTION-007 to 011), the auto-scroll step (DD-COLLECTION-033, 034), `ThrowIf` and the interaction-id format mapping (DD-PLATFORM-001). The extraction does not change behavior.
2. **Headless component tests** (`[AvaloniaFact]`) for each operation inside a real `Window` with the Fluent theme. Drop targets get synthetic drag events through the headless `TopLevel.DragDrop(...)` raw-event extension. Drag sources get synthetic mouse input (`MouseDown`/`MouseMove`/`MouseUp`).
3. **End-to-end tests**, where synthetic mouse input drags from a real drag source into a real drop target. Headless `DoDragDropAsync` runs Avalonia's in-process drag loop, so the real `DataTransfer`, the drag session (DD-SESSION) and `DropCompleted` all run, with nothing in the library replaced.
4. **`SpecTests`**, which checks the specs themselves: front matter, unique ids, existing sources, every public type owned by exactly one spec, and every cited test existing.

**Test setup.** A `TestAppBuilder` (`AppBuilder.Configure<TestApp>().UseSkia().UseHeadless(new() { UseHeadlessDrawing = false })`) registered with `[assembly: AvaloniaTestApplication(...)]`. `TestApp` adds `FluentTheme`. Without a theme, `ListBox` and `TreeView` have no templates, so they have no containers, no `ScrollViewer` and no `PART_Header`. Shared helpers live in `MonacoLabs.DragDrop.Tests/Infrastructure/`:

- `TestWindow.Show(content)`: shows a window with given content at a fixed size and runs layout. `window.PointIn(visual)` translates a point in a visual (its center by default) to window coordinates.
- `DragData.For(operation, payload)`: builds the `DataTransfer` a given drag operation would produce, through the same internal `CreateDataTransfer` its drags use (which calls the operation's own `CreateDataTransferItem`), so tests never copy the internal format ids.
- `window.SimulateDrag(data, allowed)`: a `SimulatedDrag` that raises enter, over, leave and drop at a point, with modifiers, and returns the effect the app reports, read from `RoutedEvent.RouteFinished` after every handler has run. It runs dispatcher jobs after each event.
- `window.MouseDrag(from, to or path, modifiers)`: presses at a source point, moves through the given points with the modifiers held, and releases on the last point. It returns once the drag has ended and the drag source's code after the platform drag has run (the in-process loop completes inside `MouseUp`). It always releases the mouse, because an in-process drag that never sees `MouseUp` never ends.
- `TestDispatcher.PumpUntil(condition, timeout)`: an async loop of `RunJobs()` and short `Task.Delay`s until a condition holds, for timers. It throws `TimeoutException` naming the condition.

**What does not change.** No public API, and no behavior: every requirement keeps its wording. The library gains an `InternalsVisibleTo` for the test assembly and an internal `DragOperationBase.CreateDataTransfer`, which its own drags use. Requirements that the correctness fixes changed are not pinned here; that change wrote their tests. The pre-1.0 API changes have landed, so tasks 5–8 test the settled API. They already added several of the classes below (`WiringTests`, `DragGestureTests`, `DropTargetTests`, `CollectionDragTests`, `CollectionDropTests`, `TreeDragTests`, `TreeDropTests`, `DropAdornerTests`, `EndToEndTests`); tasks 5–8 extend them and skip the requirements those already cite.

## Decisions

- **xUnit v3 with `Avalonia.Headless.XUnit`, pinned to `xunit.v3` 3.2.2.** `Avalonia.Headless.XUnit` 12.2.0 is built on `xunit.v3.extensibility.core` 3.2.2, so `[AvaloniaFact]` runs each test on the headless UI thread with no custom fixture. The spike found `xunit.v3` 4.0.3 incompatible: every `[AvaloniaFact]` fails discovery with a `MissingMethodException` on `TestIntrospectionHelper.GetTestCaseDetails`, while plain `[Fact]`s run. Revisit when `Avalonia.Headless.XUnit` ships against 4.x. Rejected: NUnit with `Avalonia.Headless.NUnit` (no advantage, and xUnit v3 was the request); a hand-rolled dispatcher fixture.
- **`SimulateDrag` uses the headless `TopLevel.DragDrop` extension.** In 12.2 its signature is `DragDrop(TopLevel, Point, RawDragEventType, IDataTransfer, DragDropEffects, RawInputModifiers)`. It hit-tests the point and routes to the element under it, raising DragLeave on the old element and DragEnter on the new one when a DragOver crosses between targets. Modifiers and the `DataTransfer` instance arrive as given. It returns `void`, and the raw event and `ITopLevelImpl.Input` are internal, so `SimulateDrag` reads the resulting effect from `RoutedEvent.RouteFinished` for DragEnter, DragOver and Drop, filtered to the window. That runs after every handler, including the drag session's window handlers, which a window-level handler registered before the session would not. A raw event that reaches no drop target reports None.
- **Headless `DoDragDropAsync` runs a real in-process drag.** The headless platform registers Avalonia's `InProcessDragSource`. The returned task completes only after synthetic mouse input: `MouseMove` raises DragEnter/DragOver on the element under the pointer, `MouseUp` drops and returns the target's effect, and Escape returns `None`. Without input it never completes, so a test that forgets to release the mouse hangs instead of failing. The spike drove a real `ItemDragOperation` to `ItemDropOperation` drag this way: `:dragging`, `:dropover`, `PayloadTarget` and the Move removal all ran. It does not reproduce Win32 behavior: there is no DragLeave instead of Drop after a None DragOver (DD-SESSION), and an exception from a drop handler escapes from `MouseUp` and leaves the drag task incomplete, instead of surfacing as a `COMException` from `DoDragDropAsync`.
- **Timers run on the real clock, only while the dispatcher waits.** The headless dispatcher is `ManagedDispatcherImpl`. `Dispatcher.UIThread.RunJobs()` never runs `DispatcherTimer` or `DispatcherTimer.RunOnce`, not even after 3 s of spinning. They fire on time when the test awaits (`await Task.Delay` in an async `[AvaloniaFact]`) or pushes a `DispatcherFrame`. So `PumpUntil` is async and polls with short `Task.Delay`s, and timer tests set short intervals (`HoverExpandDelay`, the auto-scroll interval) to stay fast.
- **The .NET 11 SDK builds and runs the suite.** The spike ran on a machine with only the 9 and 11 SDKs: the 11 SDK builds the `net10.0` project, and `dotnet test` honours the `global.json` MTP runner setting. `global.json` pins a 10.0 SDK with `"rollForward": "latestMajor"`, so CI uses 10.0 and newer local SDKs still work.
- **Microsoft Testing Platform runner.** xUnit v3 test projects are executables and support MTP natively, and the .NET 10 SDK's `dotnet test` runs MTP when `global.json` sets `"test": { "runner": "Microsoft.Testing.Platform" }`. The spike's tests, `[AvaloniaFact]`s included, were discovered and run by `dotnet test` and by the Passenger IDE; Rider and Visual Studio were not checked. Rejected: VSTest through `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`, kept only as a fallback if an IDE cannot discover MTP tests.
- **Real Skia rendering in tests.** `UseHeadlessDrawing = false`, so `RenderTargetBitmap` snapshots (DD-SESSION-002 to 005) and render scaling (DD-ADORNERS-016) work. Rejected: headless drawing stubs, which make the preview tests impossible.
- **Fixed-size item templates.** Test item templates set explicit heights and widths, so hover zones and insertion indexes do not depend on font metrics. Reason: headless text measurement uses the machine's default font, which differs between Windows and Linux runners.
- **Tests drive the real platform drag.** Drag-side and end-to-end tests start drags with `MouseDrag`, and the library calls `DoDragDropAsync` as it does in production. Reason: the headless in-process drag source delivers the real `DataTransfer` and allowed effects to the target and returns its effect, so nothing has to be replaced and the drag session runs as shipped. What the source offered (DD-DRAG-015, DD-TRANSFER) is asserted at the target's DragEnter. The Win32-only DD-SESSION sequences (a None DragOver followed by DragLeave) are raised on the window with `SimulateDrag`. A failing platform drag is simulated by a test subclass overriding `DoDragDrop`. Rejected: an internal per-instance hook in place of `DoDragDropAsync` with `RecordingDragOperation` subclasses, which the spike made unnecessary and which would leave the in-process path untested.
- **Pure logic is extracted, not mocked.** Effect resolution, the removal index and the auto-scroll step become internal static functions that the operations call. Reason: they are the densest logic and need many cases, which run in microseconds without a window. Rejected: a mocking library over `DragEventArgs` (Avalonia types are not designed for mocking).
- **Plain xUnit `Assert`.** Rejected: FluentAssertions (commercial license since v8) and Shouldly (an extra dependency for little gain).
- **Tests are named as behaviors and cited from the specs.** Each test method reads as a sentence (`DroppingInTheTopHalfInsertsBeforeTheItem`), and each requirement's `Tests:` line lists the tests that pin it, in the same commit as the test.
- **CI runs on Windows and Linux.** Fixed-size templates remove the font dependence that keeps other Avalonia suites Windows-only. Rejected: Windows only. If the Linux leg fails for environment reasons (native Skia libraries), it is dropped and a backlog item is added.

## Spec changes

**All specs in `docs/specs/dragdrop/`**: each `tests:` front-matter list gains the test classes that cover it, and each requirement's `Tests:` line names its tests as they are written. No requirement wording changes.

**DD-EFFECTS** Open items: remove the item about extracting the effect logic once `DropEffectResolverTests` exists.

**`docs/specs/README.md`**: "Once the test suite exists, `SpecTests` enforces the format" becomes "`SpecTests` enforces the format".

**`docs/ARCHITECTURE.md` §3** is rewritten to describe the four layers, the test app, the helpers, the fixed-size rule and the manual checks that remain.

**`CLAUDE.md`**: "There are no tests, CI, or lint/format configs" is replaced; Commands gains `dotnet test --project MonacoLabs.DragDrop.Tests`; a short testing paragraph names the helpers, says drags run for real through `MouseDrag`, and warns that headless timers need an async wait. Done early, after the pre-1.0 API changes, except a CI mention, which task 10 adds; task 11 still makes the ARCHITECTURE §3 and specs README edits.

## Tasks

1. **Spike** (throwaway branch). Answer the open questions:
   - Does the headless `TopLevel.DragDrop(...)` extension take a `DataTransfer` in 12.2, and does it hit-test and route DragEnter/DragOver/DragLeave/Drop to the element under the point?
   - What does `DoDragDropAsync` do headless: throw, or return None?
   - Do `DispatcherTimer` and `DispatcherTimer.RunOnce` fire under `[AvaloniaFact]` when the dispatcher is pumped, and on what clock?
   - Does `xunit.v3` 4.0.x work with `Avalonia.Headless.XUnit` 12.2.0, which was built against 3.2.2?
   - Do Rider and Visual Studio discover the MTP tests?

   Record the answers in this proposal's Decisions and remove them from Open questions.
2. **Scaffold.**
   - Create `MonacoLabs.DragDrop.Tests/` (`OutputType` Exe, `net10.0`, `Nullable` and `ImplicitUsings` enabled) and add it to the `.slnx`.
   - Add `xunit.v3`, `Avalonia.Headless.XUnit`, `Avalonia.Skia` and `Avalonia.Themes.Fluent` to `Directory.Packages.props`, versioned there.
   - Add `global.json` with the SDK band and the MTP runner.
   - Add the library's `InternalsVisibleTo` for `MonacoLabs.DragDrop.Tests`.
   - Add `TestApp` and `TestAppBuilder`.

   Verify: `dotnet test --project MonacoLabs.DragDrop.Tests` runs one `[Fact]` and one `[AvaloniaFact]` that shows a window with a `ListBox` and finds its containers.
3. **Extract pure logic and unit-test it.** Move effect resolution, the collection removal index and the auto-scroll step into internal static helpers called by the operations. Tests:
   - `DropEffectResolverTests`: DD-EFFECTS-001 to 007, as theories over allowed sets × modifiers × in-process × macOS.
   - `CollectionRemovalTests`: DD-COLLECTION-007 to 009 and 011, excluding the behavior the correctness fixes changed.
   - `AutoScrollStepTests`: DD-COLLECTION-033 to 035.
   - `ThrowIfTests`.
   - `InteractionIdTests`: DD-PLATFORM-001, DD-TRANSFER-001.

   Verify: the sample behaves as before on its List and Tree pages (manual).
4. **Infrastructure.** Add `TestWindow`, `DragData`, `SimulateDrag`, `MouseDrag` and `PumpUntil`. Verify with one smoke test per helper.
5. **Drop-side tests.**
   - `WiringTests`: DD-WIRING-001 to 005, 007 to 010.
   - `DropTargetTests`: DD-DROP-001 to 022 on `ItemDropOperation`.
   - `PlatformDropTests`: DD-PLATFORM-002 to 007, DD-DROP-016.
   - `ItemDropTests`: DD-ITEM-008.
   - `DropAdornerTests`: DD-ADORNERS-001 to 012, 015, 017 to 020, through pseudo-classes, `IsAttached` and visibility, not pixels.
6. **Collection and tree drop tests.**
   - `CollectionDropTests`: DD-COLLECTION-012 to 026, 028 to 031, using fixed-height items.
   - `AutoScrollTests`: DD-COLLECTION-032, 035, with a short list in a small viewer.
   - `TreeDropTests`: DD-TREE-005 to 016.
   - `HoverExpandTests`: DD-TREE-017 to 020.
7. **Drag-side tests.**
   - `DragGestureTests`: DD-DRAG-001 to 019, 022 to 024, via `MouseDrag` into a recording drop target.
   - `CollectionDragTests`: DD-COLLECTION-001 to 006.
   - `TreeDragTests`: DD-TREE-001 to 004.
   - Cover the selection rule (DD-DRAG-009) on a `ListBox` in `Multiple` selection mode.
8. **Session and end-to-end tests.**
   - `DragSessionTests`: DD-SESSION-001 to 012 and 014. Window `AllowDrop` injection and restoration, preview visibility and pseudo-classes, drops outside targets through `MouseDrag`, and the Win32 None-then-DragLeave sequence through `SimulateDrag` on the window.
   - `EndToEndTests`: DD-DRAG-020 and 021, DD-ITEM-001 to 007 and 009, DD-COLLECTION-007 to 009 and 011, DD-TRANSFER-002 to 014, DD-EFFECTS-008 and 009. Each test drags from a real source into a real target with `MouseDrag`.
9. **`SpecTests`.** Parse every spec's front matter and requirements. Check:
   - ids are unique and well formed
   - `sources` exist
   - every exported type of `MonacoLabs.DragDrop` is in exactly one spec's `types`
   - every `Class.Method` in a `Tests:` line exists in the test assembly

   Verify: it passes, and fails on a deliberately broken citation (then reverted).
10. **CI.** Add `.github/workflows/ci.yml` on push and pull request, with `windows-latest` and `ubuntu-latest`. It runs `actions/setup-dotnet` from `global.json`, builds the solution, runs `dotnet test` with results uploaded on failure, and caches NuGet keyed on `Directory.Packages.props`. Verify: a branch push goes green on both legs.
11. **Docs.** Make the ARCHITECTURE §3, specs README and CLAUDE.md edits listed above. Verify: `SpecTests` passes, and every non-manual requirement outside those the correctness fixes changed cites a test or says why it is `untested` in its spec's Open items.
12. **Backlog.** Add what this change leaves out: code coverage reporting (`Microsoft.Testing.Extensions.CodeCoverage`), pixel tests for the adorners and preview, and a macOS CI leg for DD-EFFECTS-005.

## Open questions

None. The spike (task 1) answered them; see Decisions.