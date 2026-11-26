# Collection drags stop receiving handled pointer events

## Why

`CollectionDragOperation` sets `Routing` to `Tunnel` and `HandledEventsToo` to true (DD-COLLECTION-001), "so presses reach it before `ListBoxItem` handles them for selection". Tunneling alone already does that: the tunnel phase runs from the window down to the pressed element before any element's bubbling handler, so the operation sees the press before `ListBoxItem` selects. The backlog asked whether `HandledEventsToo` is still needed.

Probing (throwaway headless tests, 2026-11-26) found:

| Scenario | `HandledEventsToo` true | false |
|---|---|---|
| Press and drag on a `ListBoxItem` | drags | drags |
| The whole suite (420 tests) | passes | passes |
| An ancestor marks `PointerPressed` handled in its tunnel phase | drags | no drag |
| An ancestor marks `PointerMoved` handled in its tunnel phase | drags | no drag |
| A light-dismiss `Popup` is open and the press dismisses it | no drag | no drag |
| A list nested in an outer list's item, both drag sources | the outer list drags | the outer list drags |

So the setting's only effect today is to ignore an ancestor that claims the gesture in the tunnel phase. Avalonia's convention is that a handled event belongs to whoever handled it, and every other drag source (DD-DRAG-003) follows it. An ancestor that handles tunneling presses or moves (a pan-and-zoom canvas, a design surface, a custom gesture host) means "this gesture is mine", and a list inside it should not start a drag anyway.

## What

`CollectionDragOperation` (and so `TreeDragOperation`) keeps `Routing = Tunnel` and stops setting `HandledEventsToo`, which falls back to the base default, false. Drags from `ListBox` and `TreeView` items behave as before. An app whose list sits under an ancestor that handles tunneling pointer events, and wants the list to drag anyway, sets `HandledEventsToo="True"` on the operation in XAML.

What does not change: tunneling, the press filter (DD-DRAG-005 to 008), marking presses handled (DD-DRAG-009), and the drop side's `HandledEventsToo`, which is a separate property with its own default.

## Decisions

- **Respect handled events on collection drags.** Reason: the setting no longer serves `ListBoxItem`, and its remaining effect overrides ancestors that explicitly claimed the gesture. One default for every drag source is easier to explain. Rejected: keeping it on and documenting why (it defends against no known control); removing the property (apps under a gesture-claiming ancestor still need the opt-in).

## Spec changes

**DD-COLLECTION**
- Changed **DD-COLLECTION-001**: "The collection drag operation shall handle pointer events with tunneling routing, so presses reach it before `ListBoxItem` handles them for selection."
- Added (inherited): "When `HandledEventsToo` is not set on a collection drag operation, it shall be false."
- Decisions: add "Respect handled events on collection drags".

**DD-TREE**: no change; it inherits DD-COLLECTION-001.

CLAUDE.md: drop "and `HandledEventsToo` to true" from the `CollectionDragOperation` line.

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **Stop setting `HandledEventsToo`** in the `CollectionDragOperation` constructor.
   - `CollectionDragTests.ByDefaultTheSourceTunnelsAndIgnoresHandledPresses` (defaults: `Tunnel`, false)
   - `CollectionDragTests.AnAncestorThatHandlesTunnelingPressesKeepsTheGesture`: no drag by default, a drag with `HandledEventsToo` true
   - `CollectionDragTests.PressesOnAListBoxItemReachTheOperation` and the tree drag tests keep passing
2. **Manual check on the Win32 sample.** Drag and select on the List, Simple Collection, Tree and Tree to Tree pages: single press-drag, Ctrl-select then drag a selection, drag from a row's text, use the tree expander.
3. **Close.** Update DD-COLLECTION, its Open items and CLAUDE.md, and delete this proposal and the backlog pointer to it.

## Open questions

- None.

## Related finding

A drag source nested inside another drag source's item (a `ListBox` inside an outer `ListBox`'s item) never drags its own items: the outer operation tunnels first and starts its drag on the first move past the threshold, with either setting. This is logged in the backlog as its own gap, because it needs its own rule (the innermost source that tracks the press wins).
