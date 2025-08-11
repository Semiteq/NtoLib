# Why we inherit the column and the cell for ActionTarget (and ship a custom editing control)

This document explains why the ActionTarget column is implemented with custom types:
- ActionTargetComboBoxColumn (inherits DataGridViewComboBoxColumn)
- ActionTargetComboBoxCell (inherits DataGridViewComboBoxCell)
- ActionTargetEditingControl (inherits DataGridViewComboBoxEditingControl)

and how this design solves several WinForms/DataGridView integration problems cleanly and predictably.

## Background

- The recipe table has two related columns: Action and ActionTarget.
- The available values for ActionTarget depend on the Action selected in the same row.
- Some actions (e.g., Close All) do not support a target at all; for them, ActionTarget must be visually disabled and not editable.
- We want a robust UX with minimum event spaghetti in the main TableControl.

## Problems with the vanilla DataGridViewComboBoxColumn

Using a stock DataGridViewComboBoxColumn for ActionTarget has a few hard-to-avoid pitfalls:

1) Per-row, dynamic DataSource
- A standard column expects one DataSource for all rows.
- Our rows have different per-row lists (StepViewModel.AvailableActionTargets).
- Wiring per-row DataSource via grid events (CellFormatting, EditingControlShowing, etc.) is fragile:
    - You fight the control’s lifecycle and timing.
    - Flicker/race conditions appear when rows/cells refresh.
    - Code in TableControl becomes tightly coupled and hard to maintain.

2) Commit/parse errors on selection
- Classic exception during CommitEdit: “Input string was not in a correct format.”
- Root cause: the grid tries to convert the formatted display text (string) back to the ValueType (int) without the cell having a matching DataSource/ValueMember at commit time.
- Fix requires BOTH:
    - ValueType = int? (nullable, matching the VM property type).
    - Assigning DataSource/ValueMember/DisplayMember to the CELL (not only the editing control), so the grid can correctly resolve the selected Key on commit.

3) Owner-draw “black dropdown” rendering bug
- WinForms can render the dropdown list as a “black rectangle” for combo boxes inside a DataGridView due to owner-draw/default styles on some environments.
- Fix: a custom editing control that forces DrawMode = Normal, FlatStyle = Standard, and applies the cell’s colors/fonts to the dropdown.

4) Proper ReadOnly/Disabled visuals per cell
- Some actions set ActionTarget = null (not applicable). The cell must look disabled (grey), not focusable, and without a dropdown arrow.
- With stock column/cell, hiding the arrow per-cell and keeping visuals “grey even when selected” is fiddly and inconsistent.
- Our custom cell paints a disabled look consistently and cooperates with the TableBehaviorManager to hide the arrow.

5) Encapsulation and maintainability
- We want a “thin” TableControl. Pushing behavior into the custom column/cell reduces event spaghetti and lifecycle bugs.
- The column/cell/editor trio become the single place of truth for ActionTarget behavior.

## What each custom type does

### ActionTargetComboBoxColumn
- Mostly a thin wrapper to set up a correct CellTemplate.
- Sets ValueType = typeof(int?) to match StepViewModel.ActionTarget.
- Leaves DataSource blank at column-level (we use per-row DataSource instead).

### ActionTargetComboBoxCell
- Sets DisplayMember = "Value" and ValueMember = "Key" (we bind to List<KeyValuePair<int, string>>).
- ValueType = int? to match the VM and allow null (for “no target” actions).
- InitializeEditingControl:
    - Assigns DataSource/ValueMember/DisplayMember to BOTH the cell and the editing control.
    - Reads the row’s StepViewModel.AvailableActionTargets and sets ctl.SelectedValue to the current key (if any).
    - Applies editor styles immediately.
- GetFormattedValue:
    - Converts the underlying key to a display text using AvailableActionTargets of the current row.
    - Returns a special placeholder "—" when the property is disabled (not applicable).
- Paint override:
    - When the property is ReadOnly/Disabled, paints the cell with its “blocked” look (grey) and draws the text/placeholders itself.
    - This keeps the cell grey even when the row is selected, avoiding the default selection inversion that would confuse the user.

### ActionTargetEditingControl
- A custom editor to avoid the black dropdown and unify visuals:
    - Forces:
        - DrawMode = Normal (disables owner draw),
        - FlatStyle = Standard,
        - DropDownStyle = DropDownList.
    - On create/visibility changes, re-applies style from DataGridView.CurrentCell.InheritedStyle (colors + font).
- By centralizing this here, we avoid relying on grid-level event hacks.

## How it integrates with the rest

- TableBehaviorManager (central behavior):
    - OnCellFormatting picks the correct visual state (Default / ReadOnly / Disabled) from TableCellStateManager and applies colors.
    - Ensures ReadOnly cells cannot start editing (CellBeginEdit is cancelled).
    - Hides the dropdown arrow for ReadOnly/Disabled by setting DataGridViewComboBoxCell.DisplayStyle = Nothing.
    - Normalizes any combo editors it sees (as a backup safety net).

- TableCellStateManager + ColorScheme:
    - Calculates the visual state per cell based on the StepViewModel (e.g., Disabled when the property is null/not applicable).
    - Uses unified Blocked* colors from ColorScheme for ReadOnly/Disabled.

- StepViewModel:
    - Exposes ActionTarget as int? and AvailableActionTargets as List<KeyValuePair<int, string>> for the row.
    - IsPropertyDisabled(ColumnKey.ActionTarget) returns true when the property is absent/null (e.g., Close All).
    - IsPropertyReadonly(ColumnKey.ActionTarget) can be used if at some point we decide to make targets readable but not editable (today it's mainly Disabled for “not applicable”).

- Data flow:
    - On selection, the editing control commits the Key (int?) back to the cell.
    - The cell’s DataSource/ValueMember/DisplayMember ensure the grid can parse the selection properly.
    - The VM setter relays the change to the domain engine (RecipeViewModel -> Engine), which may rebuild the row if Action changed.
    - If the target is not found in the new per-row DataSource, this is a domain logic error. UI degrades gracefully (empty text), while logging can report the inconsistency.

## Why this approach is better

- Correctness:
    - No commit/parse exceptions.
    - Correct per-row drop-down items without flicker or event timing races.
    - No “black dropdown” rendering glitches.
- UX:
    - Disabled cells look and behave disabled (grey, no dropdown arrow, no editing).
    - Selected disabled cells remain grey, avoiding misleading highlights.
- Maintainability:
    - TableControl remains “thin”.
    - Behavior is encapsulated in the column/cell/editor and the central TableBehaviorManager.
    - Extensible to other per-row dependent columns.

## Extending this pattern

- If you need another per-row dependent column:
    - Create a <Feature>ComboBoxColumn and <Feature>ComboBoxCell.
    - Override InitializeEditingControl to bind per-row DataSource.
    - Keep ValueType aligned with the VM property type (nullable if needed).
    - Provide a custom editing control if owner-draw issues arise.
    - For ReadOnly/Disabled visuals, either:
        - Let TableBehaviorManager toggle DisplayStyle + colors, and/or
        - Override Paint/GetFormattedValue for special placeholders.

## Files to look at

- Presentation/Table/Columns/ActionTargetComboBoxColumn.cs
- Presentation/Table/TableBehaviorManager.cs
- Presentation/Table/TableCellStateManager.cs
- Presentation/Table/ColorScheme.cs
- Core/Application/ViewModels/StepViewModel.cs

## Known edge cases and our stance

- “Target not found in list” after an Action change:
    - Treated as a domain inconsistency.
    - UI shows empty text (or "—" if disabled).
    - Logging can be added in RecipeViewModel to help diagnose.
- If you later want “visible but readonly” semantics for ActionTarget:
    - IsPropertyReadonly can be used to keep the value visible but non-editable (the current paint logic already handles this).

---
In short: inheriting the column, the cell, and the editing control gives us precise control over per-row data binding, type-safe commit behavior, and reliable rendering/UX that the stock DataGridView combo column cannot offer by itself without fragile grid-level event juggling.