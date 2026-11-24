# Introduce the xUnit v3 test suite

## Why

The library is approaching a stable API, and nothing checks it automatically: there is no test project and no CI, and every requirement in `docs/specs/` is `untested`. The pre-1.0 review found eight bugs (see [correctness-fixes.md](correctness-fixes.md)), all of them reachable only through specific gestures that nobody re-checks by hand after each change. Before the API settles and the bugs are fixed, the library needs a suite that pins the specified behavior, lets each fix start from a failing test, and keeps the specs honest through their `Tests:` lines.

## What

A new test project, `MonacoLabs.DragDrop.Tests/`, built on xUnit v3 and Avalonia's headless platform, added to `MonacoLabs.DragDrop.slnx`, and run with `dotnet test` locally and in a new GitHub Actions workflow.

**Layers.**

1. **Pure unit tests** (`[Fact]`/`[Theory]`, no Avalonia platform) for logic extracted into internal static helpers: effect resolution (DD-EFFECTS), the collection removal index (DD-COLLECTION-007 to 011), the auto-scroll step (DD-COLLECTION-033, 034), `ThrowIf` and the interaction-id format mapping (DD-PLATFORM-001). The extraction does not change behavior.
2. **Headless component tests** (`[AvaloniaFact]`) for each operation inside a real `Window` with the Fluent theme. Drop targets get synthetic drag events through the headless `TopLevel.DragDrop(...)` raw-event extension. Drag sources get synthetic mouse input (`MouseDown`/`MouseMove`/`MouseUp`).
3. **End-to-end tests**, where a drag source's platform drag call is replaced by a test hook that routes the real `DataTransfer` into a drop target through synthetic drag events and returns the effect. The drag session (DD-SESSION) and `DropCompleted` both run for real.
4. **`SpecTests`**, which checks the specs themselves: front matter, unique ids, existing sources, every public type owned by exactly one spec, and every cited test existing.

**Test setup.** A `TestAppBuilder` (`AppBuilder.Configure<TestApp>().UseSkia().UseHeadless(new() { UseHeadlessDrawing = false })`) registered with `[assembly: AvaloniaTestApplication(...)]`. `TestApp` adds `FluentTheme`. Without a theme, `ListBox` and `TreeView` have no templates, so they have no containers, no `ScrollViewer` and no `PART_Header`. Shared helpers live in `MonacoLabs.DragDrop.Tests/Infrastructure/`:

- `TestWindow`: shows a window with given content at a fixed size and runs layout (`Dispatcher.UIThread.RunJobs()`).
- `DragData`: builds the `DataTransfer` a given drag operation would produce, through the operation's own `CreateDataTransferItem`, so tests never copy the internal format ids.
- `SimulateDrag`: raises enter, over, leave and drop at a point, with allowed effects and modifiers, and returns the reported effect.
- `PumpUntil(condition, timeout)`: runs dispatcher jobs until a condition holds, for the deferred DragLeave and timers.
- `RecordingDragOperation` variants of the three drag operations that record what the platform drag hook received.

**What does not change.** No public API, and no behavior: every requirement keeps its wording. The library gains an `InternalsVisibleTo` for the test assembly and an internal per-instance hook for the platform drag call. Requirements that [correctness-fixes.md](correctness-fixes.md) or [api-design.md](api-design.md) changes are not pinned here; those proposals write their tests. Tasks 5–8 are best done after api-design lands, so they test the settled API (see the order in [README.md](README.md)).

## Decisions

- **xUnit v3 with `Avalonia.Headless.XUnit`.** `Avalonia.Headless.XUnit` 12.2.0 is built on `xunit.v3.extensibility.core` 3.2.2 or later, so `[AvaloniaFact]` runs each test on the headless UI thread with no custom fixture. Rejected: NUnit with `Avalonia.Headless.NUnit` (no advantage, and xUnit v3 was the request); a hand-rolled dispatcher fixture.
- **Microsoft Testing Platform runner.** xUnit v3 test projects are executables and support MTP natively, and the .NET 10 SDK's `dotnet test` runs MTP when `global.json` sets `"test": { "runner": "Microsoft.Testing.Platform" }`. Rejected: VSTest through `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`, which is kept only as a fallback if an IDE cannot discover MTP tests (Open questions).
- **Real Skia rendering in tests.** `UseHeadlessDrawing = false`, so `RenderTargetBitmap` snapshots (DD-SESSION-002 to 005) and render scaling (DD-ADORNERS-016) work. Rejected: headless drawing stubs, which make the preview tests impossible.
- **Fixed-size item templates.** Test item templates set explicit heights and widths, so hover zones and insertion indexes do not depend on font metrics. Reason: headless text measurement uses the machine's default font, which differs between Windows and Linux runners.
- **Tests replace the platform drag call, not `DoDragDrop`.** `DragOperationBase` gains an internal, per-instance hook used in place of `Avalonia.Input.DragDrop.DoDragDropAsync`. Reason: the headless platform has no drag source, and overriding `DoDragDrop` would skip the drag session, the part most worth testing end to end. Rejected: a static hook, which parallel test collections would share.
- **Pure logic is extracted, not mocked.** Effect resolution, the removal index and the auto-scroll step become internal static functions that the operations call. Reason: they are the densest logic and need many cases, which run in microseconds without a window. Rejected: a mocking library over `DragEventArgs` (Avalonia types are not designed for mocking).
- **Plain xUnit `Assert`.** Rejected: FluentAssertions (commercial license since v8) and Shouldly (an extra dependency for little gain).
- **Tests are named as behaviors and cited from the specs.** Each test method reads as a sentence (`DroppingInTheTopHalfInsertsBeforeTheItem`), and each requirement's `Tests:` line lists the tests that pin it, in the same commit as the test.
- **CI runs on Windows and Linux.** Fixed-size templates remove the font dependence that keeps other Avalonia suites Windows-only. Rejected: Windows only. If the Linux leg fails for environment reasons (native Skia libraries), it is dropped and a backlog item is added.

## Spec changes

**All specs in `docs/specs/dragdrop/`**: each `tests:` front-matter list gains the test classes that cover it, and each requirement's `Tests:` line names its tests as they are written. No requirement wording changes.

**DD-EFFECTS** Open items: remove the item about extracting the effect logic once `DropEffectResolverTests` exists.

**`docs/specs/README.md`**: "Once the test suite exists, `SpecTests` enforces the format" becomes "`SpecTests` enforces the format".

**`docs/ARCHITECTURE.md` §3** is rewritten to describe the four layers, the test app, the helpers, the fixed-size rule and the manual checks that remain.

**`CLAUDE.md`**: "There are no tests, CI, or lint/format configs" is replaced; Commands gains `dotnet test --project MonacoLabs.DragDrop.Tests`; a short testing paragraph names the helpers and the platform drag hook.

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
   - `CollectionRemovalTests`: DD-COLLECTION-007 to 009 and 011, excluding the behavior correctness-fixes changes.
   - `AutoScrollStepTests`: DD-COLLECTION-033 to 035.
   - `ThrowIfTests`.
   - `InteractionIdTests`: DD-PLATFORM-001, DD-TRANSFER-001.

   Verify: the sample behaves as before on its List and Tree pages (manual).
4. **Infrastructure.** Add `TestWindow`, `DragData`, `SimulateDrag`, `PumpUntil`, the internal platform drag hook and the `RecordingDragOperation` variants. Verify with one smoke test per helper.
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
   - `DragGestureTests`: DD-DRAG-001 to 019, 022 to 024, via synthetic mouse input and the platform drag hook.
   - `CollectionDragTests`: DD-COLLECTION-001 to 006.
   - `TreeDragTests`: DD-TREE-001 to 004.
   - Cover the selection rule (DD-DRAG-009) on a `ListBox` in `Multiple` selection mode.
8. **Session and end-to-end tests.**
   - `DragSessionTests`: DD-SESSION-001 to 012 and 014. Window `AllowDrop` injection and restoration, preview visibility and pseudo-classes, and drops outside targets through synthetic window drag events.
   - `EndToEndTests`: DD-DRAG-020 and 021, DD-ITEM-001 to 007 and 009, DD-COLLECTION-007 to 009 and 011, DD-TRANSFER-002 to 014, DD-EFFECTS-008 and 009. Each test drags from a real source into a real target through the hook.
9. **`SpecTests`.** Parse every spec's front matter and requirements. Check:
   - ids are unique and well formed
   - `sources` exist
   - every exported type of `MonacoLabs.DragDrop` is in exactly one spec's `types`
   - every `Class.Method` in a `Tests:` line exists in the test assembly

   Verify: it passes, and fails on a deliberately broken citation (then reverted).
10. **CI.** Add `.github/workflows/ci.yml` on push and pull request, with `windows-latest` and `ubuntu-latest`. It runs `actions/setup-dotnet` from `global.json`, builds the solution, runs `dotnet test` with results uploaded on failure, and caches NuGet keyed on `Directory.Packages.props`. Verify: a branch push goes green on both legs.
11. **Docs.** Make the ARCHITECTURE §3, specs README and CLAUDE.md edits listed above. Verify: `SpecTests` passes, and every non-manual requirement outside those correctness-fixes changes cites a test or says why it is `untested` in its spec's Open items.
12. **Backlog.** Add what this change leaves out: code coverage reporting (`Microsoft.Testing.Extensions.CodeCoverage`), pixel tests for the adorners and preview, and a macOS CI leg for DD-EFFECTS-005.

## Open questions

All five are answered by task 1:

- The headless drag-event API shape and routing in Avalonia 12.2. If it cannot route to the element under the point, `SimulateDrag` raises the routed events directly on the hit-tested element.
- Headless `DoDragDropAsync` behavior. It only affects whether a forgotten hook fails loudly.
- Headless timer behavior, which `PumpUntil` and `HoverExpandTests` depend on.
- `xunit.v3` 4.0.x compatibility. Fallback: pin `xunit.v3` 3.2.2.
- MTP discovery in Rider and Visual Studio. Fallback: VSTest packages.
