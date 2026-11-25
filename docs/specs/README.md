# Feature specs

One spec per library feature, describing how it behaves **now**. The specs are the source of truth for behavior. Where a change proposal and a spec disagree, the spec wins until the proposal lands. Specs are written for AI agents first. They are terse, numbered and checkable, so an agent can read the spec instead of reconstructing a feature from its code, tests and history.

## Folders

One folder per shipped package, so a spec never crosses a package boundary:

| Folder | Package | Id prefix |
|---|---|---|
| [`dragdrop/`](dragdrop/index.md) | `MonacoLabs.DragDrop` | `DD-` |

The sample app (`DragDropSample`) has no specs. Its decisions are in [ARCHITECTURE.md](../ARCHITECTURE.md) §5.

## Using the specs

- **Before changing behavior**, read the feature's spec (find it in the folder's `index.md`, or by the type in its `types` list) and the specs in its `depends`.
- **With the change**, update the spec in the same commit: change or add requirements, update their `Tests:` lines, and strike requirements that no longer hold. A change whose behavior diverges from its spec without updating it is incomplete.
- **New work starts as a change proposal** in [`docs/changes/`](../changes/README.md). A new feature also gets a draft spec (`status: draft` in the front matter), requirements before code. The draft becomes current, and the proposal is deleted, when the change lands.
- **When code and spec disagree** and the spec was not changed on purpose, one of them is a bug. Decide which, and log it in the spec's Open items until it is fixed.

## Format

Copy [`_template.md`](_template.md).

**Front matter** (between `---` lines; a scalar is `key: value`, a list is `key:` followed by `  - item` lines, or `key: []` when empty):

| Key | Content |
|---|---|
| `id` | `DD-DRAG`, `DD-COLLECTION`: the folder's prefix, then an upper-case name. Unique. |
| `title` | The feature's name. |
| `status` | Optional. `draft` for a spec written ahead of its feature; omitted when current. |
| `package` | The package the folder is for. |
| `namespaces` | Namespaces the feature's public types live in. |
| `sources` | Files or folders, relative to the repo root. Each must exist. |
| `types` | The public top-level types this spec owns (generic types without type parameters). Every public type in the package is owned by exactly one spec. A spec that specifies members of a type another spec owns names them in its Purpose. |
| `tests` | Test classes that cover the feature, or `[]`. |
| `depends` | Ids of specs this one builds on. |

**Sections**, as `##` headings in this order: Purpose, Requirements, Invariants, Edge cases, Threading and lifetime, Platform behavior, Decisions, Non-goals, Open items. A section that does not apply says why in one line ("No platform-specific behavior."). Specs carry no API listings or code samples: signatures belong to the code.

**Requirements** may be grouped under `###` headings. Each is one bullet, one behavior:

```markdown
- **DD-DRAG-011** — While a press is tracked, when the pointer moves at least `DragThreshold` DIPs from the press position, the drag source shall start a drag.
  - Tests: `DragGestureTests.MovingPastTheThresholdStartsADrag`
```

- Ids are `<spec id>-NNN`, never reused or renumbered. A requirement that no longer holds is struck through and kept: `- ~~**DD-DRAG-011**~~ — Removed by <change>: reason.`
- Wording is EARS-style: "When …, the X shall …", "While …, the X shall …", or "The X shall …" for unconditional behavior. State observable behavior, not implementation.
- `Tests:` lists `Class.Method` for each test that pins the requirement (theory methods by name only). Every cited test must exist. Otherwise it says `untested`, or `manual — <what has to be checked by hand, and where>`.

**Decisions** record why the feature behaves as it does: one bullet per decision, led by a bold title, then the decision, its reason and, when known, the alternatives rejected. Requirements refer to a decision by its title, never by a number. A decision governing several features lives in the spec it most constrains; the others name that spec. Project-wide decisions are in [ARCHITECTURE.md](../ARCHITECTURE.md) §4.

```markdown
- **The drag side removes the item.** The drop side inserts and then calls `DropCompleted`, and the drag side removes its original. Reason: only the source knows where the payload lives. Rejected: having the drop side remove from `PayloadCollection`, which couples targets to every source type.
```

**Inherited requirements** are marked `(inherited)` after the id:

```markdown
- **DD-DRAG-022** (inherited) — When `DragThreshold` is not set, the threshold shall be 6 DIPs.
  - Tests: untested
```

An inherited requirement records behavior the code has that was not necessarily a deliberate choice: a default value, a threshold, a timing constant. It is specified so changes to it are visible, but it carries no design intent. Before relying on it as a contract, or when changing it, a human confirms it; once confirmed, the marker is removed.

## Where the specs came from

The specs were extracted from the code as of commit `0a4f44b`, before any tests existed, so every requirement starts `untested`. Specs describe; they do not fix. A behavior that looks wrong is specified as it is and logged in Open items, with the change proposal or backlog line that addresses it.
