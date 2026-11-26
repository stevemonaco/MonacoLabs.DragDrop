# Package hygiene before the first release

## Why

The library builds and tests cleanly, but it is not ready to publish as a package: no package metadata, no XML documentation file (consumers get no IntelliSense), no record of the public API, no formatting rules, and no coverage report. The items were separate lines under "Docs and hygiene" in the backlog. They share one goal and touch the same few files (`MonacoLabs.DragDrop.csproj`, a new `Directory.Build.props`, `ci.yml`), so they land together. Some of them also have an order: the API baseline should come after the doc comments and after the API-changing proposals.

Measured on 2026-11-26 (`dotnet build -p:GenerateDocumentationFile=true`):

- **100 CS1591** (missing XML comment) warnings: 29 on `*Property` fields; the rest on public types (`DragDrop`, `DropInfo`, `CollectionDropInfo`, `DropTargetOffset` and its members, `IDragOperation`, `IDropOperation`, `DropAdornerBase`, `DragOperationBase`), overrides (`OnPropertyChanged`, `DropCompleted`, `OnDroppedOutside`, `CreateMetadata`, ...), constructors, and a few protected virtuals (`StartDrag`, `SubscribeDragEvents`, `LocatePayloadContainer`, `LocateTargetContainer`, ...).
- **2 CS1573** (a parameter missing its `<param>` while others have one): `DragPreviewAdorner.Create` and `SetEffect`.
- `LICENSE.txt` is MIT but still has the template's `[year] [fullname]` placeholder.
- `README.md` describes the library as a prototype with unfinished TreeView support, and its images use root-relative paths (`/Assets/...`) that nuget.org won't resolve.

## What

After this lands:

- **Assembly.** `ComVisible` and `Guid` are gone from `AssemblyInfo.cs`; only `XmlnsDefinition` stays.
- **Docs.** The package ships an XML documentation file. Every public type and every member whose meaning isn't obvious from its name has a 1-2 sentence `<summary>`, following the repo's comment rules. Overrides use `<inheritdoc/>`. `DropAdornerBase.IsDropValid` gets its own comment instead of the one copied from `TargetControl`, and the two CS1573 warnings are fixed.
- **Public API record.** `Microsoft.CodeAnalysis.PublicApiAnalyzers` tracks the surface in `PublicAPI.Shipped.txt` (empty until the first release) and `PublicAPI.Unshipped.txt`. Any API change shows up in review as a diff to those files.
- **Package.** `PackageId` `MonacoLabs.DragDrop`, `Version` 0.7.0, `Authors` and `Copyright` (Stephen Monaco, 2026), `Description`, `PackageLicenseExpression` (MIT), `PackageProjectUrl` and `RepositoryUrl`, `PackageTags` (avalonia, drag-drop, dragdrop, mvvm), `PackageReadmeFile` pointing at a package README, and a symbols package (`snupkg`). Source Link comes with the .NET SDK for GitHub repositories, and `ContinuousIntegrationBuild` is set when `GITHUB_ACTIONS` is true, so CI builds are deterministic and step-into-source works. The sample and test projects set `IsPackable` to false.
- **Shared settings.** A root `Directory.Build.props` holds what all three projects share (`Nullable`, the CI flag), so each csproj holds only what is its own.
- **Formatting.** A root `.editorconfig` encodes the style the code already uses (file-scoped namespaces, `var`, braces, using placement, naming), checked by `dotnet format --verify-no-changes` in CI. The first commit is format-only, so later diffs stay readable.
- **Coverage.** CI collects coverage with `Microsoft.Testing.Extensions.CodeCoverage` on the Windows leg and uploads the Cobertura report as an artifact. No threshold yet.
- **Interaction id formats.** `DataFormat` instances are cached per interaction id instead of created per id on every DragOver (the DD-TRANSFER backlog item). It is a perf fix, not packaging, but it is small and touches no API.

What does not change: behavior, public API (except where a doc comment says more), target framework, and CI's OS matrix. The macOS leg stays in the backlog until there is a macOS check plan.

## Decisions

- **One proposal for the hygiene batch.** Reason: the items share files and an order (docs → API baseline → package). As separate backlog fixes, each would re-touch the csproj, and the baseline could land before the API settles. Rejected: a fix per backlog line.
- **CS1591 stays suppressed, for now.** Agreed 2026-11-26; revisit before 1.0. The package ships the documentation file, but `NoWarn` keeps CS1591. Reason: the repo's comment rules skip docs on self-evident constructors and properties, and Avalonia's `*Property` fields are self-describing. Enforcing CS1591 would force boilerplate those rules forbid. The doc pass is checked once, by reviewing the 100 warnings above, and PublicApiAnalyzers then flags every new public member for review. Rejected: CS1591 as a warning or error (boilerplate on 29 property fields and every constructor).
- **The first version is 0.7.0.** Agreed 2026-11-26. Reason: the API and behavior are close to 1.0, and a 0.1 or preview version would undersell that. 1.0.0 follows once the open proposals have landed and the API baseline moves to `PublicAPI.Shipped.txt`. Rejected: `0.1.0-preview.1`.
- **The API baseline comes last.** `PublicAPI.Unshipped.txt` is generated after the filtered/sorted, `DragFailed` and handled-events changes have landed, so their entries don't churn. Reason: a baseline taken earlier fills every review with known churn.
- **A separate package README.** `docs/PACKAGE.md`, with a short description, an install line, a minimal XAML example and absolute image URLs. Reason: nuget.org can't resolve root-relative paths, and the repo README is for contributors too. Rejected: packing the repo README as is.
- **No coverage threshold yet.** Reason: a threshold set before the number is known either fails CI for no reason or is meaningless. Revisit after a few reports.

## Spec changes

- **DD-WIRING**: Open items lose the `SetIsDragHandle`/`GetIsDragHandle` doc comment line.
- **DD-ADORNERS**: Open items lose the `IsDropValid` doc comment line.
- **DD-TRANSFER**: add to Threading and lifetime: "Formats for interaction ids are created once per id and reused."
- ARCHITECTURE: a short "Packaging" paragraph (what ships, API tracking, how to cut a pre-release). CLAUDE.md: mention `PublicAPI.*.txt` updates in Conventions, and `dotnet format` in Commands.

## Tasks

Tasks 1-6 landed on 2026-11-26. Coverage uses `Microsoft.Testing.Extensions.CodeCoverage` 18.0.6, the last line on Microsoft.Testing.Platform 1.x, which `xunit.v3` 3.2.2 needs. Overrides that change behavior got their own summary instead of `<inheritdoc/>`. What remains is task 7, which waits for filtered-sorted-collections and the backlog's `DragFailed` event, and closing.

1. **Assembly attributes.** Remove `ComVisible` and `Guid`. Build.
2. **Doc comments.** Turn on `GenerateDocumentationFile` with CS1591 in `NoWarn`. Write the summaries listed in Why, use `<inheritdoc/>` on overrides, fix `IsDropValid` and the two CS1573. Done when a build with CS1591 enabled lists only self-evident constructors, `*Property` fields and enum members whose names say it all.
3. **`DataFormat` cache.**
   - `InteractionIdTests.TheFormatForAnIdIsCreatedOnce` (the same instance for the same id)
4. **Shared props and `.editorconfig`.** Add `Directory.Build.props`, `.editorconfig`, then one format-only commit from `dotnet format`. Add `dotnet format --verify-no-changes` to CI.
5. **Package metadata.** Metadata, package README, `LICENSE.txt`'s placeholder filled in as `Copyright (c) 2026 Stephen Monaco`, `IsPackable` false on the sample and tests, `ContinuousIntegrationBuild` under CI. Check `dotnet pack -c Release` output: the README renders, the license is recognized, the `.snupkg` is produced and Source Link resolves (`dotnet-sourcelink test` or NuGet Package Explorer).
6. **Coverage in CI.** Add the extension, collect on the Windows leg, upload the report.
7. **Public API baseline.** Add PublicApiAnalyzers and generate `PublicAPI.Unshipped.txt` after the API-changing proposals have landed. Build with no RS0016/RS0017. Mention `PublicAPI.*.txt` updates in CLAUDE.md's Conventions.
8. **Close.** Delete this proposal and the backlog and changes README pointers to it. (The DD-WIRING and DD-ADORNERS Open items lines are already gone.)

## Open questions

- None. The package README is `docs/PACKAGE.md`, packed as `README.md` (decided 2026-11-26).
