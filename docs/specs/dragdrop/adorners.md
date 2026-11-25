---
id: DD-ADORNERS
title: Drop adorners
package: MonacoLabs.DragDrop
namespaces:
  - MonacoLabs.DragDrop
sources:
  - MonacoLabs.DragDrop/Adorners/DropAdornerBase.cs
  - MonacoLabs.DragDrop/Adorners/DropHighlightAdorner.cs
  - MonacoLabs.DragDrop/Adorners/DropInsertionAdorner.cs
  - MonacoLabs.DragDrop/Adorners/DropAdornerKind.cs
  - MonacoLabs.DragDrop/Adorners/AdornerBrushes.cs
types:
  - DropAdornerBase
  - DropHighlightAdorner
  - DropInsertionAdorner
  - DropAdornerKind
tests:
  - DropAdornerTests
depends:
  - DD-WIRING
---

# Drop adorners

## Purpose

Drop adorners are the drop target's visual feedback, placed in the `AdornerLayer` over a target control. `DropHighlightAdorner` covers the whole drop target and shows validation messages (DD-DROP). `DropInsertionAdorner` marks where a collection or tree drop will land (DD-COLLECTION, DD-TREE). Both are passive: the drop operations attach, detach, retarget and update them. They expose pseudo-classes and brushes for app styling.

## Requirements

### Common behavior

- **DD-ADORNERS-001** — A drop adorner shall not be hit-testable and shall not accept drops, so it never intercepts the drag events of the control it covers.
  - Tests: `DropAdornerTests.DropAdornersAreNotHitTestableAndRefuseDrops`, `DropAdornerTests.DragsOverAShownAdornerStillReachTheTarget`
- **DD-ADORNERS-002** — While `IsDropValid` is false, a drop adorner shall have the `:error` pseudo-class.
  - Tests: `DropAdornerTests.AnInvalidAdornerHasTheErrorPseudoClass`
- **DD-ADORNERS-003** — When `Attach` is called with a `TargetControl` and the adorner is not already attached, it shall become the target control's adorner in its `AdornerLayer` and fade its opacity in from 0 over `FadeInDuration` with a cubic ease-out.
  - Tests: `DropAdornerTests.AttachingPlacesTheAdornerOverTheTargetAndEasesItIn`
- **DD-ADORNERS-004** — When `Attach` is called with no `TargetControl`, or while already attached, it shall do nothing.
  - Tests: `DropAdornerTests.AttachingWithoutATargetOrWhileAttachedDoesNothing`
- **DD-ADORNERS-005** — When `Detach` is called, the adorner shall cancel a running fade and stop being the target control's adorner.
  - Tests: `DropAdornerTests.DetachingMidFadeRestoresTheOpacityAndRemovesTheAdorner`
- **DD-ADORNERS-006** — When `FadeInDuration` is zero or less, attaching shall show the adorner at once.
  - Tests: `DropAdornerTests.AZeroFadeShowsTheAdornerAtOnce`
- **DD-ADORNERS-007** — `IsAttached` shall be true exactly while the adorner is its `TargetControl`'s adorner.
  - Tests: `DropAdornerTests.IsAttachedFollowsTheTargetsAdorner`

### Highlight adorner

- **DD-ADORNERS-008** — The highlight adorner's default look shall come from a built-in control theme: an accent fill, a 2 DIP accent border and, under `:error`, an error fill and border, so app styles targeting `DropHighlightAdorner` override it.
  - Tests: `DropAdornerTests.TheHighlightThemeDrawsAnAccentLookAndAnErrorLook`, `DropAdornerTests.AppStylesOverrideTheHighlightTheme`
- **DD-ADORNERS-009** — While `Kind` is `Border`, the highlight adorner shall have the `:outline` pseudo-class and a transparent background; while it is `None`, it shall have the `:hidden` pseudo-class and be invisible.
  - Tests: `DropAdornerTests.BorderKindHasTheOutlinePseudoClass`, `DropAdornerTests.NoneKindIsInvisible`
- **DD-ADORNERS-010** — While `IsDropValid` is false and `ErrorMessage` is not empty, the highlight adorner shall show a centered badge with an icon and the message on `ErrorBadgeBackground`; otherwise it shall hide the badge.
  - Tests: `DropAdornerTests.TheErrorBadgeShowsTheMessageOnlyWhileInvalid`
- **DD-ADORNERS-011** — When attached, the highlight adorner shall copy the target control's corner radius (from a `Border` or templated control), unless `CornerRadius` was set or styled on the adorner itself.
  - Tests: `DropAdornerTests.TheHighlightCopiesTheTargetsCornerRadius`, `DropAdornerTests.ACornerRadiusSetOrStyledOnTheHighlightWins`

### Insertion adorner

- **DD-ADORNERS-012** — When `Update` is called, the insertion adorner shall take the given offset and size itself to the target control's bounds.
  - Tests: `DropAdornerTests.UpdateTakesTheOffsetAndTheTargetsSize`
- **DD-ADORNERS-013** — For `BeforeTarget` and `AfterTarget`, the insertion adorner shall draw a horizontal line with a circular marker at its start, at the target's top or bottom edge moved to the middle of the gap shared with the neighboring container, so "after item N" and "before item N+1" draw in the same place.
  - Tests: untested
- **DD-ADORNERS-014** — For `OnTarget`, the insertion adorner shall draw a rounded outline around the target.
  - Tests: untested
- **DD-ADORNERS-015** — While `IsDropValid` is false or `LineBrush` is null, the insertion adorner shall draw nothing.
  - Tests: `DropAdornerTests.TheInsertionAdornerDrawsOnlyWhileValidWithABrush`
- **DD-ADORNERS-016** — The insertion line shall be a whole number of device pixels thick (at least 1) and aligned to device pixels at the window's render scaling.
  - Tests: untested
- **DD-ADORNERS-017** — The insertion adorner shall not be clipped by the adorner layer or its own bounds, so a line in the gap between containers is fully visible.
  - Tests: `DropAdornerTests.TheInsertionLineIsNotClippedToTheTarget`

### Defaults

- **DD-ADORNERS-018** (inherited) — When `FadeInDuration` is not set, it shall be 120 ms for the highlight adorner and zero for the insertion adorner.
  - Tests: `DropAdornerTests.FadeInDefaultsToAShortFadeForTheHighlightAndNoneForTheInsertionLine`
- **DD-ADORNERS-019** (inherited) — The accent color shall be `#0078D4` (fill at alpha `0x24`) and the error color `#C42B1C` (fill at alpha `0x24`); `LineBrush` and `ErrorBadgeBackground` shall default to the accent and error colors.
  - Tests: `DropAdornerTests.TheHighlightThemeDrawsAnAccentLookAndAnErrorLook`, `DropAdornerTests.BrushesDefaultToTheAccentAndErrorColors`
- **DD-ADORNERS-020** (inherited) — The insertion line shall be 2 DIPs thick with a 3.5 DIP marker radius.
  - Tests: `DropAdornerTests.TheInsertionLineIsTwoDipsThickWithAThreeAndAHalfDipMarker`
- **DD-ADORNERS-021** — Hovering a list in the sample shall draw the insertion line crisply between items at 100% and 150% Windows scaling, with no doubled or blurred line.
  - Tests: manual — Win32 Desktop sample at both scalings. Checked 2026-11-25.
- **DD-ADORNERS-022** (inherited) — When `Kind` is not set, it shall be `Solid`.
  - Tests: `DropAdornerTests.KindDefaultsToSolid`
- **DD-ADORNERS-023** — The highlight adorner shall apply `Kind` only through its pseudo-classes, styled by its built-in control theme, so app styles can set `Kind` and override the look of each kind.
  - Tests: `DropAdornerTests.KindCanBeSetFromAStyle`, `DropAdornerTests.AppStylesOverrideTheLookOfAKind`
- **DD-ADORNERS-024** — `TargetControl` shall bind one way by default.
  - Tests: `DropAdornerTests.TargetControlBindsOneWayByDefault`

## Invariants

- An adorner instance is the adorner of at most one control at a time (`AdornerLayer.SetAdorner` replaces any previous one).
- A drop adorner never receives pointer or drag input.

## Edge cases

- A newly created adorner has `IsDropValid` false, so it starts with `:error` until a drop operation sets it.
- Retargeting an attached insertion adorner leaves it the adorner of the old control until the drop operation detaches it (DD-COLLECTION does so before retargeting).
- `GetAdornerRect` throws when the adorner has no target control or the target is not in a `TopLevel`.
- The fade animates to the opacity at the moment `Attach` is called. A fade started during another fade targets the partly faded value only if that value is the base value, which it is after `Detach` cancels the previous fade.

## Threading and lifetime

- UI thread only. The fade runs as an Avalonia animation canceled through a `CancellationTokenSource`. The adorner holds its target control only through `TargetControl`.

## Platform behavior

- No platform-specific behavior. Device-pixel alignment uses the window's render scaling on every platform.

## Decisions

- **Adorners are passive.** The operation computes the offset and validity and calls `Update` and `Attach`. Reason: one place (the operation) owns hover state, and a custom adorner only has to draw.
- **Defaults come from a control theme, not local values.** Reason: local values outrank styles, so app styles could not restyle the adorner. `Kind` follows the same rule: it only sets pseudo-classes, and the theme maps them to the background and visibility. Rejected: hard-coded brushes set in the constructor; `Kind` setting `Background` or `IsVisible` directly, as the former constructor argument did, which blocked an app style hiding the highlight.
- **The highlight's kind is a styled property.** Reason: an app picks `Border` or `None` per target in XAML, or for every target with one style, without subclassing an operation. Rejected: a constructor argument chosen through a protected property on the operation (unreachable from XAML and styles); an extra shortcut property on the operation (two ways to set one thing, and unclear when `DropAdorner` is also set).
- **The insertion line sits in the middle of the gap.** Reason: lists with item spacing or margins would otherwise draw "below N" and "above N+1" as two different lines, and the line would jump while crossing the gap.

## Non-goals

- Adorners that animate between positions.
- Error messages on the insertion adorner. Messages show on the highlight adorner only.

## Open items

- DD-ADORNERS-013, 014 and 016 are untested. They describe where and how crisply the insertion line and outline are drawn, which only exact pixel comparisons can check, and the headless test windows render at scaling 1. Pixel tests are logged in the backlog.
- `IsDropValid`'s doc comment is copied from `TargetControl`. Logged in the backlog.
