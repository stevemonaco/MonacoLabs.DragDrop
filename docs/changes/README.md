# Change proposals

New work starts here. A change proposal is a short file, `docs/changes/<name>.md` (kebab-case), describing one change to the library: a new feature, a behavior change or a fix that needs design. It exists while the change is being agreed and built. When the change lands, its content lives in the specs (`docs/specs/`) and the file is deleted; git history keeps it.

Small fixes that need no design skip the proposal: take the item from [`docs/BACKLOG.md`](../BACKLOG.md) and update the affected spec with the fix.

## Current proposals

| Proposal | Depends on |
|---|---|
| [collection-fixes.md](collection-fixes.md): reorders keep their selection, `:dragging` follows items in virtualized lists, read-only and fixed-size collections are never changed | — |
| [flow-layouts.md](flow-layouts.md): horizontal, wrap and grid items panels (zones, gaps, insertion line, right-to-left), a Layouts sample page | — |
| [drop-onto-items.md](drop-onto-items.md): drop onto items in flat lists to act on, replace or swap them, a sample page | — |
| [drag-in-progress.md](drag-in-progress.md): `IsDragging`, drag start and end events, `:dragactive` on the window, `:dropcandidate` on targets | — |
| [items-hosts.md](items-hosts.md): an internal items-host abstraction, `DataGrid` and `ItemsRepeater` satellite packages, two sample pages | collection-fixes (lifecycle tracking), flow-layouts (repeater grids) |
| [custom-surfaces.md](custom-surfaces.md): protected hover steps, slot operations, grab offset and placement preview, Tilemap and Image editor sample pages | drop-onto-items (swap reporting) |

Suggested order: collection-fixes (small), flow-layouts, drop-onto-items, drag-in-progress, items-hosts, custom-surfaces.

## Format

```markdown
# <Title>

## Why

What problem it solves, for whom, and what prompted it (a backlog item, a bug, a request).

## What

The change, in a few paragraphs: the behavior after it lands, and what does not change.

## Decisions

Each choice the change makes: the decision, the reason, and the alternatives rejected. These move into the governing spec's Decisions section when the change lands.

## Spec changes

Per spec id: requirements added (draft wording, no ids yet), requirements changed (by id, new wording), requirements struck (by id, with the reason). A new feature names its new spec file.

## Tasks

The ordered work items, each with its tests. Manual checks on the sample are listed as tasks.

## Open questions

What must be settled before or during the work. Empty before implementation starts.
```

## Lifecycle

1. **Draft** the proposal and settle its open questions.
2. **Implement** it. Each task updates its spec as it goes: requirements, their `Tests:` lines, and the Decisions section.
3. **Close**: when the tests are green and the specs say what the code does, delete the proposal and remove the backlog lines it resolved. Anything left undone goes back into the backlog.
