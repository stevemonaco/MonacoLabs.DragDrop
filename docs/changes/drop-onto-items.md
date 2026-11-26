# Dropping onto items in flat lists: act on, replace or swap

## Why

A flat collection target can only insert between items: `GetDropOffset` never returns `OnTarget` (DD-COLLECTION-014), and `ResolveDropLocation` would treat `OnTarget` as an insertion index anyway. Common gestures need the item itself as the target:

- **Act on an item:** drop a tag onto a card, a file onto a folder row, a person onto a project, messages onto a folder in a sidebar list.
- **Replace an item:** fixed slots shown as a list (a hotbar, equipment slots, a "top 5" ranking) where a drop puts the payload in that slot.
- **Swap two items:** the same slots, where the slot's old item goes to where the dragged one came from, within one list or between two.

Today each needs a subclass that reimplements hover zones, the location and removal. Trees already have "inside" zones (DD-TREE-007), and single-value targets already replace and swap (DD-ITEM), so flat lists are the gap. Prompted by the review of 2026-11-26.

## What

**A drop mode on `CollectionDropOperation`** (name open), a styled property:

| Mode | Zones over an item | On an `OnTarget` drop |
|---|---|---|
| `Insert` (default) | Halves: before / after (today) | — |
| `InsertOrOnto` | First quarter before, last quarter after, middle onto (like trees) | See "onto behavior" |
| `Onto` | The whole item is onto; gaps and the empty area still insert or append | See "onto behavior" |

**Onto behavior** (a second styled property, name open):
- `Command` (default): nothing is transferred. `PayloadCommand` receives `Offset = OnTarget` and a location whose `Index` is the hovered item's index (mapped into the collection, DD-COLLECTION-049) and whose new `Item` is the hovered item. The source completes a Move as usual (DD-DROP-029), so moving messages onto a folder removes them from the inbox.
- `Replace`: the payload replaces the hovered item in place (`collection[index] = payload`). The drop info reports the replaced item (`DropInfo.ReplacedPayload`, as single-value targets do).
- `Swap` is a drag-side choice, as on single-value targets: a new `SwapOnDrop` on `CollectionDragOperation` puts the replaced item where the dragged item was instead of removing the original. Within one list that exchanges the two items; between lists it trades them.

**Validation.** `CanDropAtLocation` and `CanDrop` see the `OnTarget` offset and `DropLocation.Item`, so "only folders accept messages" is a `CanDrop` override that checks the item. Dropping an item onto itself is rejected structurally. `Replace` accepts only single-payload drags: a multi-payload drag over an item in `Replace` mode is invalid there (it can still insert in the gaps).

**Feedback** reuses what exists: the insertion adorner's `OnTarget` outline (DD-ADORNERS-014) and `:dropinside` on the hovered container.

**Removal.** A `Replace` inserts nothing, so removal (DD-COLLECTION-008, 009) must not shift indexes or skip an "inserted range" for it, and must never remove the payload now sitting in the replaced slot. The drop info says the drop replaced rather than inserted (`TargetCount = 0` plus `ReplacedPayload`, or an explicit flag, see open questions).

**Sample.** A new page, `DropOntoPage` ("Drop onto items"):
- **Mail triage:** an inbox list and a folder list in `Onto` + `Command` mode; dropping messages onto a folder moves them into it, and a `CanDrop` override rejects the "Sent" folder with a message.
- **Ranking:** a "Top 5" list in `InsertOrOnto` + `Replace` mode with `SwapOnDrop` on its drag side, fed from a longer candidates list: drop onto a rank to replace it (the replaced entry goes back to the candidates), drop between ranks to insert, drag rank onto rank to swap them.

**What does not change:** `Insert` mode is the default and behaves exactly as today; trees keep their own `OnTarget` meaning (into the node's children, DD-TREE-009).

## Decisions

- **Mode and behavior are separate settings.** Reason: where the onto zone is (whole item or middle) and what an onto drop does (command or replace) vary independently; one enum would need every combination. Rejected: a single enum with combined values.
- **Swap lives on the drag side.** Reason: it matches `ItemDragOperation.SwapOnDrop`, and only the source knows where the dragged item was. The target only reports what it replaced.
- **`Command` is the default onto behavior.** Reason: "act on an item" is domain logic (add a member, file a message) that the library can't guess; the library supplies the zone, feedback, item and removal.
- **The location carries the hovered item.** Reason: `DropEventArgs.TargetContext` is the data context of the element hit, which in a nested template may not be the item. Rejected: relying on `TargetContext`.
- **Flat onto differs from tree onto.** A flat `OnTarget` location points at the item; a tree's points into the node's children. Reason: flat items have no children, and changing the tree's meaning would break every tree consumer.
- **Replace is single-payload.** Reason: replacing a run of items with a run of payloads has no obvious extent when the counts differ.

## Spec changes

**DD-COLLECTION**
- Added: the zone rules for `InsertOrOnto` and `Onto`, and (inherited) the defaults `Insert` and `Command`.
- Changed **DD-COLLECTION-022**: for `OnTarget`, the location's index is the mapping of the hovered index and its `Item` is the hovered item.
- Added: `Command` transfers nothing; `Replace` sets the item at the location's index and reports `ReplacedPayload`; self-onto is rejected structurally; `Replace` rejects multi-payload drags over items.
- Added: "When `SwapOnDrop` is true and a Move drop reports a replaced item, the drag operation shall put the replaced item at the original's index instead of removing the original."
- Changed **DD-COLLECTION-008/009**: removal after a replace neither shifts nor skips an inserted range, and never removes the replacing payload.

**DD-TRANSFER**: `CollectionDropLocation` gains `Item`; DD-TRANSFER-010's `ReplacedPayload` applies to collection replaces too.

ARCHITECTURE §5 and CLAUDE.md: the new page, and onto drops in the `CollectionDropOperation` paragraph.

## Tasks

Each task first writes the listed tests and confirms they fail, then changes the code, updates the spec and cites the tests.

1. **Zones and location.**
   - `CollectionDropTests.InsertOrOntoSplitsItemsIntoQuarters`
   - `CollectionDropTests.OntoModeMakesTheWholeItemOnto`
   - `CollectionDropTests.AnOntoLocationCarriesTheHoveredItemAndItsIndex`
   - `CollectionDropTests.DroppingAnItemOntoItselfIsRejected`
2. **Command behavior.**
   - `CollectionDropTests.AnOntoCommandDropTransfersNothingAndRunsTheCommand`
   - `EndToEndTests.MovingOntoAnItemThroughACommandRemovesTheOriginal`
3. **Replace and swap.**
   - `CollectionDropTests.AReplaceDropReplacesTheItemAndReportsIt`
   - `CollectionDropTests.ReplaceRejectsSeveralPayloadsOverAnItem`
   - `EndToEndTests.ReplacingWithinOneListRemovesTheOriginalNotTheReplacement`
   - `EndToEndTests.SwappingWithinOneListExchangesTheItems`
   - `EndToEndTests.SwappingBetweenListsTradesTheItems`
4. **Sample page** `DropOntoPage` as described.
5. **Manual check on the Win32 sample.** Triage mail onto folders (including the rejected one), replace, insert and swap in the ranking.
6. **Close.** Property names settled, specs cite every test, this proposal is deleted.

## Open questions

- **Property names.** Candidates: `ItemDropMode` (`Insert`, `InsertOrOnto`, `Onto`) and `OntoBehavior` (`Command`, `Replace`).
- **How a replace is reported to removal.** `TargetCount = 0` with `ReplacedPayload` set, or an explicit `CollectionDropInfo.IsReplace`. Recommended: the explicit flag; overloading a count is easy to misread.
- **Copy onto with `Replace`.** A Copy replace leaves the original in place and discards the replaced item. Confirm that is wanted, or restrict `Replace` to Move.
