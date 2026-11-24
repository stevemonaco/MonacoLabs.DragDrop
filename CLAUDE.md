# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

MonacoLabs.DragDrop is a WIP/prototype MVVM-friendly drag-and-drop library for Avalonia 12, plus a demo app (`DragDropSample`) that exercises it. TreeView support is unfinished. There are no tests, CI, or lint/format configs.

## Commands

```powershell
dotnet build MonacoLabs.DragDrop.slnx
dotnet run --project DragDropSample
```

- Targets `net10.0`; the solution is a `.slnx` file.
- Package versions are centrally managed in `Directory.Packages.props`. Add `<PackageVersion>` there and a version-less `<PackageReference>` in the csproj.
- Debug builds of the sample reference `AvaloniaUI.DiagnosticsSupport` and call `WithDeveloperTools()`, so the Avalonia DevTools MCP (`attach-to-app`) can inspect the running demo's visual tree, adorners, and pseudo-classes.

## Library architecture (`MonacoLabs.DragDrop`)

### Wiring
`DragDrop` exposes two attached properties, `DragDrop.DragOperation` and `DragDrop.DropOperation` (`BindingMode.OneTime`). Setting one calls `Attach(control)` on the operation and `Detach` on the old one. An operation instance can attach to only one control and throws if reused.

Operations are `AvaloniaObject`s, not `StyledElement`s, so they have no `DataContext` of their own. A plain `{Binding Items}` still works: Avalonia anchors bindings on non-control targets to the nearest parent `Control` in the XAML and uses that control's `DataContext`. Don't use `#root.DataContext...` paths. Named-element bindings on non-`ILogical` targets log "Value is null" while the view's `DataContext` is still unset, and `ViewLocator` only sets it after construction.

Avalonia's Win32 COM layer swallows exceptions thrown from drop handlers (DragEnter/DragOver/Drop) and returns `E_FAIL`. That surfaces as a `COMException` from `DoDragDrop`, so the real exception is only visible as a first-chance exception in the debugger.

### Drag → drop data flow
1. `DragOperationBase` hooks pointer events on the attached control. After a press and a move past `DragThreshold`, it calls Avalonia's `DoDragDropAsync`. It passes the stored *pressed* event args because Avalonia requires them.
2. The `DataTransfer` holds the payload once per entry in `InteractionIds`, each under an in-process `DataFormat` keyed by the id (`DragDropIds.PayloadFormat`). It also holds a `DragInfo` under `app:dragmetadata`. The payload comes from `ResolvePayload`: `Payload`, falling back to `AttachedControl.DataContext`. `CollectionDragOperation` uses the pressed item container's item instead, so the payload always matches `PayloadContainerIndex`, even with multiple selection.
3. `DropOperationBase.CanDrop` accepts when any of its own `InteractionIds` is present. Interaction ids act as typed channels: one drag can advertise several ids, and each drop target accepts a subset.
4. On drop, the drop side sets `PayloadTarget` (TwoWay) and calls `dragInfo.DragOperation.DropCompleted(...)`. **The drag side is responsible for removing the item from its source.** The drop side then invokes `PayloadCommand` with a `DropEventArgs` and calls the `OnDrop` hook.

### Operation hierarchy (`Operations/`)
- `ItemDragOperation` / `ItemDropOperation`: single-value transfer (`PayloadTarget` is replaced).
- `CollectionDragOperation` / `CollectionDropOperation`: `PayloadTarget` and `PayloadCollection` are `IList`s. The drop side inserts at the hovered container's index (`TransferPayloadIndexed`) or appends (`TransferPayload`). The drag side's `DropCompleted` then removes the original, shifting the index by +1 for same-collection moves where the source index is at or after the target. `CollectionDragOperation` uses `Tunnel` routing with `handledEventsToo` because `ListBoxItem` handles pointer presses for selection.
- `TreeDragOperation` / `TreeDropOperation`: the drag side infers `PayloadCollection` from the parent `TreeViewItem.ItemsSource`. `TreeDropOperation.Drop` does **not** transfer anything; it only invokes `PayloadCommand`.
- Every left press is tracked for a drag. If the pressed element's nearest `ISelectable` ancestor (up to `AttachedControl`) is unselected, the press stays unhandled so the item selects itself in the same gesture. `SelectingItemsControl.UpdateSelectionFromEvent` ignores handled events. Presses on a selected item are handled so an existing multi-selection survives. On an `ItemsControl`, `CollectionDragOperation` only starts a drag from a press inside an item container.
- Custom validation is done by subclassing and overriding `CanDrop`. Use `TryGetDragInfo<T>` and `TryGetPayload<T>`, and set `DropAdorner.ErrorMessage` (see `DragDropSample/ListBoxPage/WorkerDropOperation.cs`).

### Adorners and styling hooks (`Adorners/`)
`DropAdornerBase` is a `Border`. It is placed in the `AdornerLayer` over `TargetControl`, is not hit-testable, and gets the `:error` pseudo-class when `!IsDropValid`. `DropHighlightAdorner` covers the whole drop target. `DropInsertionAdorner` draws an insertion line above or below the hovered item container, or stretches over it when `SupportsChildInsertion` (trees) is set. Its `Target` (`DropTargetOffset`) is passed to `PayloadCommand`.

The library sets pseudo-classes for consumers to style:
- `:dropover` on the drop control and the hovered item container.
- `:droptop` / `:dropbottom` on the hovered item container.

`CollectionDropOperation.TransferPayloadIndexed` reads `:dropbottom` to compute the insert index, so adorner state and transfer logic are coupled.

### Conventions
- Avalonia property registrations live in `*.props.cs` partial files next to the logic file (`DragDrop.props.cs`, `DragOperationBase.props.cs`, ...).
- Most public types use the root `MonacoLabs.DragDrop` namespace regardless of folder. The exceptions are `MonacoLabs.DragDrop.Abstractions` and `DropAdornerKind` in `MonacoLabs.DragDrop.Adorners`. The `XmlnsDefinition` maps only the root namespace. The samples use `xmlns:dd="using:MonacoLabs.DragDrop"`.
- Use `ThrowIf` (in `Internals/`) for guard clauses.
- The library has `ImplicitUsings` enabled. The sample does not, so it uses explicit `using System;` etc.

## Demo app architecture (`DragDropSample`)

- Feature folders (`SingleItemPage/`, `SimpleCollectionPage/`, `ListBoxPage/`, `TreeViewPage/`, `Shell/`). Namespaces are **not** folder-based: views use `DragDropSample.Views` and view models use `DragDropSample.ViewModels`. `ViewLocator` resolves a view by replacing "ViewModel" with "View" in the full type name, so a new page must follow this naming and namespace scheme.
- To add a page, create a `PageViewModel` subclass (set `Title`) and a matching `UserControl` view, then add the view model to `MainWindowViewModel.Pages`.
- MVVM uses CommunityToolkit.Mvvm with partial-property source generation (`[ObservableProperty] public partial T Name { get; set; }`) and `[RelayCommand]`.
- `AvaloniaUseCompiledBindingsByDefault` is on, so XAML needs `x:DataType`. Use `ReflectionBinding` only where compiled bindings can't resolve the path.
- Demo data is generated with Bogus (`ListBoxPageViewModel`).
