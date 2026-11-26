# Horizontal, wrap and grid layouts for collection targets

## Why

`CollectionDropOperation` assumes items stack vertically:

- **Zones are vertical only.** `GetDropOffset` splits a container at its vertical middle (DD-COLLECTION-014). In a horizontal list, a pointer on the right half of an item inserts before it whenever it is in the top half.
- **The insertion line is horizontal only** (DD-ADORNERS-013), so a horizontal or wrapping list shows a line above or below an item rather than between two items side by side.
- **Gaps append to the end.** When the pointer is in the spacing between items (a `WrapPanel` with `ItemSpacing`/`LineSpacing`, a `StackPanel` with `Spacing`, or item margins in any panel), no container is hovered, so the location has no index and the drop appends (DD-COLLECTION-015, 022). In a grid of thumbnails the gap is most of where people aim.
- **Right-to-left flow is ignored**, so "before" is on the wrong side when `FlowDirection` is `RightToLeft`.

Tab strips, filmstrips, kanban columns, photo and icon grids all hit this. Prompted by the backlog candidate "Horizontal and wrap-panel insertion zones for `CollectionDropOperation`" and the review of 2026-11-26, which ranked it near the top. DD-COLLECTION lists it as a non-goal today.

## What

**The flow is inferred from the items panel.** The drop operation reads the realized items panel of its `ItemsControl`:

| Panel | Flow |
|---|---|
| `StackPanel`, `VirtualizingStackPanel` | Vertical or horizontal, by `Orientation` |
| `WrapPanel` | Lines of items: horizontal lines that wrap downward for `Orientation="Horizontal"`, vertical lines that wrap rightward otherwise |
| `UniformGrid` | Horizontal lines (row-major), like a horizontal `WrapPanel` |
| Anything else | Vertical (today's behavior) |

A new styled property overrides the inference for custom panels (name open, see below): `Auto` (default), `Vertical`, `Horizontal`, `HorizontalWrap`, `VerticalWrap`.

**Zones follow the flow.** In a horizontal flow, the left half of a container is `BeforeTarget` and the right half `AfterTarget`, mirrored under `FlowDirection="RightToLeft"`. In a wrapping flow, the split is along the line (X for horizontal lines, Y for vertical lines). Vertical flow is unchanged.

**Gaps resolve to the nearest item.** When the pointer is over the items panel but no container is hovered, the operation picks the nearest realized container: first within the line the pointer is on, then the nearest line. The zone is decided against that container as if it were hovered, so dropping in the gap between two thumbnails inserts between them. Past the last item of the last line, the location appends, as today.

**The insertion line follows the flow.** For a horizontal or wrapping flow, the insertion adorner draws a vertical line with its marker at the top, centered in the gap shared with the neighboring container in the same line (DD-ADORNERS-013's rule, turned 90°). At the end of a line, where "after the last item of this line" and "before the first item of the next line" are the same index, the line is drawn on the side the pointer is on.

**Pseudo-classes.** Containers also get `:dropbefore` / `:dropafter` in every flow; `:droptop` / `:dropbottom` stay for vertical flows, and horizontal flows get `:dropleft` / `:dropright` (physical sides, after `FlowDirection`).

**Sample.** A new page, `LayoutsPage` ("Layouts"), with one interaction id shared by every list so items move between all of them:
- **Filmstrip:** a `ListBox` with a horizontal `VirtualizingStackPanel`, long enough to auto-scroll sideways.
- **Photo grid:** a `ListBox` with a `WrapPanel` items panel, `ItemSpacing` and `LineSpacing` set, multiple selection.
- **Home screen:** an `ItemsControl` with a 4-column `UniformGrid`.
- A right-to-left toggle that flips the page's `FlowDirection`.

**What does not change:** vertical lists, trees (always vertical), display-index mapping, `ItemOrder="Sorted"` (no zones at all), auto-scroll (already per axis).

## Decisions

- **Infer the flow from the panel, with an override.** Reason: every built-in panel states its flow, so most apps need no setting, and a custom panel can still declare one. Rejected: a required property on every horizontal list (easy to forget, and a missed setting is today's bug).
- **Gaps resolve to the nearest container.** Reason: in a spaced layout the gap is where people aim, and appending at the end is never what they meant. Rejected: today's append; widening each container's hit area (changes the app's layout and hit testing).
- **`UniformGrid` is row-major.** Reason: that is how it arranges its children.
- **`Grid` and `Canvas` are not flows.** Items placed by row/column or by coordinates have no insertion order to show; they belong to custom surfaces (see custom-surfaces.md).
- **Neutral pseudo-classes alongside physical ones.** Reason: a style written for `:dropbefore` works in every flow and direction; `:droptop` / `:dropbottom` keep existing styles working.

## Spec changes

**DD-COLLECTION**
- Added: the flow inference table as requirements, one per panel kind, and "When the flow property is set to anything but `Auto`, the drop operation shall use it instead of inferring one."
- Added (inherited): "When the flow property is not set, it shall be `Auto`."
- Changed **DD-COLLECTION-014**: "While an item container is hovered, the offset shall be `BeforeTarget` when the pointer is in the first half of the container along the flow's line direction (left half for a horizontal left-to-right flow, right half for right-to-left, top half for a vertical flow), and `AfterTarget` otherwise."
- Changed **DD-COLLECTION-015**: "While no item container is hovered and the pointer is over the items panel, the drop operation shall treat the nearest realized container (in the pointer's line first) as hovered; otherwise the offset shall be `AfterTarget` with no hovered container."
- Changed **DD-COLLECTION-016**: add `:dropbefore`/`:dropafter`, and `:dropleft`/`:dropright` for horizontal flows.
- Non-goals: strike "Horizontal insertion zones for wrap or horizontal panels"; add "`Grid` and `Canvas` items panels (see custom surfaces)".

**DD-ADORNERS**
- Changed **DD-ADORNERS-013**: the line is horizontal for vertical flows and vertical for horizontal and wrapping flows, centered in the gap with the neighbor in the same line, and drawn on the pointer's side at a line end.

ARCHITECTURE §5 and CLAUDE.md: the new page, and the flow inference in the `CollectionDropOperation` paragraph.

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **Flow inference.** A pure helper in `Internals/` mapping a panel to a flow, with plain tests, plus the override property.
   - `ItemFlowTests` (theory over each panel and orientation)
   - `CollectionDropTests.TheFlowPropertyOverridesTheInferredFlow`
2. **Zones.**
   - `CollectionDropTests.AHorizontalListSplitsItemsLeftAndRight`
   - `CollectionDropTests.RightToLeftMirrorsTheZones`
   - `CollectionDropTests.AWrappingListSplitsItemsAlongTheLine`
3. **Gaps.**
   - `CollectionDropTests.TheGapBetweenTwoItemsInsertsBetweenThem`
   - `CollectionDropTests.TheGapBetweenLinesUsesTheNearestLine`
   - `CollectionDropTests.PastTheLastItemAppends`
4. **Adorner and pseudo-classes.**
   - `DropAdornerTests.AHorizontalFlowDrawsAVerticalLine` (pixel test, like the existing insertion-line tests)
   - `CollectionDropTests.HorizontalFlowsSetBeforeAfterAndPhysicalClasses`
5. **Sample page** `LayoutsPage` as described, added to `MainWindowViewModel.Pages` and the ARCHITECTURE table.
6. **Manual check on the Win32 sample.** Reorder in each layout, aim at gaps, auto-scroll the filmstrip sideways, move items between layouts, flip right-to-left; confirm the line sits between the items you aimed at, crisply at 100% and 150% scaling.
7. **Close.** The property name is settled, specs cite every test, this proposal is deleted.

## Open questions

- **The override property's name and values.** Candidates: `ItemFlow` (`Auto`, `Vertical`, `Horizontal`, `HorizontalWrap`, `VerticalWrap`), or `InsertionOrientation` (`Auto`, `Vertical`, `Horizontal`) with wrapping inferred from the panel regardless. Recommended: `ItemFlow`, because a custom wrapping panel needs to say it wraps.
- **Where the flow helper lives**, given items-hosts.md will need the same inference for `ItemsRepeater` layouts (`StackLayout`, `UniformGridLayout`). Recommended: keep the panel-to-flow mapping in one internal helper that the items-hosts change extends with layouts.
