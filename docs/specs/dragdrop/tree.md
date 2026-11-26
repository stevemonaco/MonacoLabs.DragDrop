---
id: DD-TREE
title: Tree operations
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop
sources:
  - MonacoLabs.DragDrop/Operations/TreeDragOperation.cs
  - MonacoLabs.DragDrop/Operations/TreeDropOperation.cs
  - MonacoLabs.DragDrop/Operations/TreeDropOperation.props.cs
types:
  - TreeDragOperation
  - TreeDropOperation
tests:
  - TreeDragTests
  - TreeDropTests
  - HoverExpandTests
  - EndToEndTests
depends:
  - DD-COLLECTION
---

# Tree operations

## Purpose

`TreeDragOperation` and `TreeDropOperation` move nodes within and between `TreeView`s, and between trees and flat lists. They derive from the collection operations (DD-COLLECTION) and keep their completion, hover and adorner behavior. What changes: the drag side finds each node's own collection and drags the selected nodes shown in the tree, and the drop side uses three hover zones per node, reads every collection from the tree instead of `TargetCollection`, refuses drops into the subtree of any dragged node and expands collapsed nodes on hover.

## Requirements

### Drag side

- **DD-TREE-001** — When attached to a `TreeView`, the drag operation's pressed container shall be the nearest `TreeViewItem` at or above the pressed element, at any depth.
  - Tests: `TreeDragTests.ThePressedContainerIsTheNearestTreeViewItemAtAnyDepth`
- **DD-TREE-002** — The pressed index shall be the container's index within its parent items control (the parent `TreeViewItem` or the `TreeView`).
  - Tests: `TreeDragTests.TheSourceCollectionIsTheParentsItemsSource`
- **DD-TREE-003** — When `SourceCollection` is not set, the drag's payload collection shall be the parent items control's `ItemsSource` when that is an `IList`.
  - Tests: `TreeDragTests.TheSourceCollectionIsTheParentsItemsSource`
- **DD-TREE-004** — The drag visual shall be the node's `PART_Header` when its template has one, and the whole `TreeViewItem` otherwise. The same part of every other dragged node shall have `:dragging` (DD-COLLECTION-005).
  - Tests: `TreeDragTests.TheDragVisualIsTheNodesHeaderRow`, `TreeDragTests.WithoutAHeaderPartTheDragVisualIsTheWholeItem`
- **DD-TREE-022** — When the pressed node is selected, `DraggedItems` is `SelectedItems` and the `TreeView` has more than one selected item, the drag shall carry the selected nodes that are shown (every ancestor expanded), in top-to-bottom order, leaving out any node whose ancestor is also selected, each with its own parent's collection and index.
  - Tests: `TreeDragTests.DraggingSelectedNodesCarriesThemTopToBottomWithTheirOwnCollections`, `TreeDragTests.ASelectedNodeUnderASelectedAncestorIsNotCarried`, `TreeDragTests.ASelectedNodeUnderACollapsedNodeIsNotCarried`, `EndToEndTests.MovingNodesFromSeveralParentsRemovesEachFromItsParent`

### Drop side: zones

- **DD-TREE-005** — The hovered container shall be the nearest `TreeViewItem` at or above the event source, below the attached control.
  - Tests: `TreeDropTests.TheHoveredContainerIsTheNearestTreeViewItem`
- **DD-TREE-006** — Zones, the insertion line and the hover position shall be measured against the node's `PART_Header`, not the whole `TreeViewItem`.
  - Tests: `TreeDropTests.ZonesAndTheInsertionLineUseTheHeaderNotTheWholeItem`
- **DD-TREE-007** — When the hovered node's container has an `IList` `ItemsSource`, the offset shall be `BeforeTarget` in the header's top quarter, `AfterTarget` in its bottom quarter and `OnTarget` in between.
  - Tests: `TreeDropTests.ANodeWithAListOfChildrenHasBeforeInsideAndAfterZones`
- **DD-TREE-008** — When the hovered node's container has no `IList` `ItemsSource`, the offset shall follow the halves rule of DD-COLLECTION-014.
  - Tests: `TreeDropTests.ANodeWithoutAListOfChildrenHasBeforeAndAfterHalves`

### Drop side: locations

- **DD-TREE-009** — For `OnTarget`, the location shall be the end of the hovered node's children, with that node as parent.
  - Tests: `TreeDropTests.DroppingInsideANodeAppendsToItsChildren`
- **DD-TREE-010** — For `AfterTarget` on an expanded node with at least one child, the location shall be index 0 of its children, with that node as parent, because that spot is drawn directly above its first child.
  - Tests: `TreeDropTests.DroppingAfterAnExpandedNodeInsertsAsItsFirstChild`
- **DD-TREE-011** — Otherwise the location shall be in the hovered node's sibling collection (its parent's `ItemsSource`), at the node's index for `BeforeTarget` or the index plus one for `AfterTarget`, with the parent node as parent, or null at the root.
  - Tests: `TreeDropTests.DroppingBeforeAChildInsertsAmongItsSiblings`, `TreeDropTests.DroppingAfterACollapsedRootInsertsAmongTheRoots`
- **DD-TREE-012** — While no node is hovered, the location shall append to the `TreeView`'s `ItemsSource` with no parent, or there shall be no location when that is not an `IList`.
  - Tests: `TreeDropTests.DroppingBelowTheNodesAppendsToTheRoots`, `TreeDropTests.WithoutALocationTheDropIsRejected`
- **DD-TREE-013** — The tree drop operation shall always transfer the payload, ignoring `TargetCollection`.
  - Tests: `TreeDropTests.TheTreeAlwaysTransfersAndIgnoresTargetCollection`

### Drop side: validation

- **DD-TREE-014** — When there is no location, the drop operation shall reject the drop.
  - Tests: `TreeDropTests.WithoutALocationTheDropIsRejected`
- **DD-TREE-015** — When the location's parent container is any dragged node or one of its descendants, the drop operation shall reject the drop with `SelfDropMessage` as its error message.
  - Tests: `TreeDropTests.DroppingANodeIntoItselfShowsTheSelfDropMessage`, `TreeDropTests.ALocationInsideAnyDraggedNodeIsRefused`
- **DD-TREE-016** — The subtree check shall run in `CanDropAtLocation`, before `CanDrop` (DD-COLLECTION-023), so a `CanDrop` override cannot allow a node into itself.
  - Tests: `TreeDropTests.ACanDropOverrideCannotAllowANodeIntoItself`

### Hover expand

- **DD-TREE-017** — While the pointer stays over the middle zone of a collapsed node that has children, the drop operation shall expand the node after `HoverExpandDelay`.
  - Tests: `HoverExpandTests.HoveringTheMiddleOfACollapsedNodeExpandsItAfterTheDelay`, `HoverExpandTests.ANodeWithoutChildrenIsNotExpanded`
- **DD-TREE-018** — When the hovered node or zone changes, or the drag ends, before the delay elapses, the drop operation shall cancel the pending expand.
  - Tests: `HoverExpandTests.MovingToAnotherZoneCancelsThePendingExpand`, `HoverExpandTests.MovingToAnotherNodeCancelsThePendingExpand`, `HoverExpandTests.LeavingCancelsThePendingExpand`
- **DD-TREE-019** — When `HoverExpandDelay` is zero or less, the drop operation shall never expand nodes on hover.
  - Tests: `HoverExpandTests.AZeroDelayNeverExpands`

### Defaults

- **DD-TREE-020** (inherited) — When `HoverExpandDelay` is not set, the delay shall be 700 ms.
  - Tests: `HoverExpandTests.TheDelayDefaultsTo700Milliseconds`
- **DD-TREE-021** (inherited) — When `SelfDropMessage` is not set, the message shall be "A node can't be moved into itself".
  - Tests: `TreeDropTests.SelfDropMessageHasADefault`

## Invariants

- No accepted drop places a node inside its own subtree.
- At most one hover expand is pending per drop operation.

## Edge cases

- Moving a node within its own sibling collection relies on the same-collection index shift (DD-COLLECTION-008), since the source and target collections are the same reference.
- A node dropped `AfterTarget` on its own expanded parent's header moves to index 0 of the parent's children.
- Expanding a node during a drag changes the layout under the pointer. The next DragOver re-evaluates the hovered node and zone.
- A tree whose nodes' `ItemsSource` are not `IList` (for example `IEnumerable` projections) accepts no "inside" drops and no sibling drops for those nodes.
- Dragging a node from a tree to a flat `CollectionDropOperation` works through DD-COLLECTION completion: the tree drag knows its own collection and index.
- A selection drag can carry nodes from several parents; each is removed from its own parent's collection (DD-COLLECTION-007).
- A selected node under a collapsed node stays where it is when the rest of the selection is dragged.
- When the pressed node is under a selected ancestor, the ancestor is dragged and the pressed node moves with it; the preview still shows the pressed node.

## Threading and lifetime

- UI thread only. The hover expand uses `DispatcherTimer.RunOnce` and is canceled with its disposable.

## Platform behavior

- `PART_Header` is the name used by Avalonia's built-in `TreeViewItem` themes (Fluent and Simple). A custom theme without it falls back to the whole item (DD-TREE-004, DD-TREE-006).

## Decisions

- **Zones use the header row.** Reason: a `TreeViewItem`'s bounds include its expanded children, so halves and quarters of the whole item would put "before" and "after" far from the row under the pointer.
- **"After" an expanded node means its first child.** Reason: the gap below an expanded header is visually the gap above its first child. Inserting after the node in the sibling list would land the item below the whole subtree, far from where the line was drawn.
- **Collections come from the tree.** Reason: every node has its own collection, so one `TargetCollection` cannot describe a tree. The tree's `ItemsSource` bindings already name each collection.
- **Selected descendants of selected nodes are not dragged separately.** Reason: they move with their ancestor; dragging them too would insert them twice and detach them from the moved subtree.
- **Trees drag only shown nodes.** A selected node is dragged only when every ancestor is expanded. Reason: a tree's selection gives only items, and a node's collection and index are known only through its container; a node under a collapsed ancestor is also not visible to the user, who would not expect it to move. Rejected: dragging every realized selected container, which includes hidden ones whose ancestors were expanded once.
- **Nodes can't be moved into themselves, structurally.** Reason: inserting a node into its own subtree detaches the subtree from the tree and creates a cycle in the view model. No domain rule should be able to allow it.

## Non-goals

- Dropping onto `TreeDataGrid` or other hierarchical controls.
- Auto-collapsing nodes that were expanded on hover once the drag moves on.

## Open items

- None.
