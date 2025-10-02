# Presentation Layer — Recipe Table UI

## Purpose

Interactive DataGridView-based table for editing and displaying MBE recipe steps. Supports dynamic column composition (Action determines available parameters), real-time execution state visualization (Upcoming/Current/Passed), and robust handling of user input with domain validation.

**Key features:**
- VirtualMode (no data duplication, stable under Action changes)
- Dynamic ComboBox data sources (row-dependent item lists)
- State-driven rendering (execution state + data availability → visual style)
- Custom cells (units parsing, read-only fields, disabled combo rendering)
- Reactive color scheme (design-time properties → instant UI update)

---

## Architecture

### Components

| Component               | Responsibility |
|-------------------------|----------------|
| `TableControl`          | Root WinForms UserControl; orchestrates DI, DataGridView lifecycle, mode switching (design/runtime) |
| `TableInitializer`      | One-time setup: headers, columns (via factories), rows, styles, VirtualMode activation |
| `Column Factories`      | `BaseColumnFactory` + specific implementations create typed columns and custom cells |
| `VirtualModeDataManager` | Routes `CellValueNeeded/CellValuePushed` events to `RecipeViewModel` |
| `TableRenderCoordinator` | Subscribes to `CellFormatting`, current line changes, color scheme updates; applies `CellVisualState` |
| `CellStateResolver`     | Composes `RowExecutionState` + `CellDataState` → `CellVisualState` (font/colors/readonly/display style) |
| `RecipeComboBoxCell`    | ComboBox cell supporting dynamic/static data source strategies; custom Paint for Disabled state |
| `PropertyGridCell`      | Text cell with domain-aware parsing (units, validation via `StepProperty`) |
| `IColorSchemeProvider`  | Reactive access to current `ColorScheme`; notifies subscribers on design-time changes |

### Data Flow (User Edit Cycle)

1. Table initialized → columns built from YAML-based config (via factories).
2. Cell drawn → VirtualMode fires `CellValueNeeded` → `RecipeViewModel.GetCellValue(rowIndex, columnIndex)`.
3. `CellFormatting` → Coordinator resolves visual state → applies Font/Colors/ReadOnly/DisplayStyle.
4. User edits → `CellValuePushed` → `RecipeViewModel.SetCellValue` → domain validation → VM update → row invalidation (if Action changed).
5. PLC state changes → `RowExecutionStateProvider` updates current line → invalidates affected rows.

### Entry Points

- Initialization: `TableControl.put_DesignMode` / `OnFBLinkChanged`.
- External commands (buttons): Add/Delete/Save/Send via `AppStateMachine`.
- Configuration: external YAML (structure defined in main README).

---

## Core Concepts

### State-Driven Rendering

Visual appearance determined by **priority composition** of two states:

**RowExecutionState** (highest priority):
- Upcoming → normal editing (white bg, editable)
- Current → executing now (blue bg, ReadOnly)
- Passed → already executed (gray bg, ReadOnly)

**CellDataState** (lower priority):
- Normal → property exists, editable
- ReadOnly → property exists but non-editable (calculated field, e.g., step start time)
- Disabled → property not applicable for current Action (gray bg, DisplayStyle=Nothing)

**Rule:** Current/Passed override CellDataState entirely. Only Upcoming respects Normal/ReadOnly/Disabled.

### ComboBox Data Source Strategies

**Problem:** "Action" ComboBox shows all available commands (static). "ActionTarget" ComboBox (channel/valve/sensor) must show list depending on Action in the same row.

**Solution:** `IComboBoxDataSourceStrategy`
- `ColumnStaticDataSource` → returns empty list → uses column.DataSource (for Action)
- `RowDynamicDataSource` → queries `StepViewModel.GetComboItems(columnKey)` → calls provider with `CurrentActionId` → returns dynamic list

**Critical detail:** Strategy must read `CurrentActionId` from live `_stepRecord` (not closure-captured), otherwise shows stale list after Action change.

### Disabled ComboBox Rendering

**Problem:** WinForms `DataGridViewComboBoxCell` ignores `BackColor` when `DisplayStyle=Nothing`. Early columns may skip `CellFormatting` if ViewModels empty at initial paint.

**Solution:**
- Custom `Paint` override determines disabled state from domain (no StepProperty or null from strategy).
- Disabled → manual background fill, skip `base.Paint` to avoid white cell.
- Execution state (Current/Passed) takes priority over Disabled (execution color shown, text hidden if disabled).
- One-time forced `Invalidate` after ViewModels build ensures initial formatting pass.

**Result:** Predictable rendering independent of event timing. No cell type substitution needed.

### RecipeComboBoxCell Architecture

**Separation of concerns:**
- **CellFormatting** (coordinator): Expensive computation once → `RecipeCellRenderInfo` stored in `cell.Tag`.
- **Paint** (cell): Read precalculated Tag, direct color fill for execution/disabled. Fast, no VM access.
- **Editing** (cell): Early exit for disabled prevents editor activation.

**Priority flow:**
1. Execution state (Current/Passed) → override all, show execution colors
2. Not executing + Disabled → blocked color, no text
3. Otherwise → standard `base.Paint`

Disabled cells in executing rows show execution background without text (state priority over applicability).

### VirtualMode (Why Not BindingList)

**What:** DataGridView does not store data; requests it via `CellValueNeeded` on paint.

**Consequences:**
- `row.DataBoundItem` always `null`
- Data lives in `RecipeViewModel.ViewModels[rowIndex]`
- Changes do not auto-refresh → manual `InvalidateRow` required
- `CellFormatting` called **every repaint** → avoid heavy logic

**Reason:** BindingList caused `ArgumentException: rowIndex invalid` during `InitializeEditingControl` when Action changed (ResetBindings mid-edit).

---

## Usage

### Add New Column Type

1. Entry in external YAML config (structure in main README).
2. Pick factory in `TableInitializer._factoryCreators` or create new one inheriting `BaseColumnFactory`.
3. Override `CreateColumnInstance` / `ConfigureColumn`.
4. Optionally create custom cell inheriting `DataGridViewTextBoxCell` or `DataGridViewComboBoxCell`.
5. Register in `_factoryCreators` dictionary.

### Change Colors/Fonts

**Design-time:** Modify `TableControl` public properties (e.g., `BlockedBgColor`) in SCADA editor → `UpdateColorScheme()` triggers provider update → Coordinator invalidates table.

**Runtime:** Assign property or call `DesignTimeColorSchemeProvider.Update(newScheme)`.

### Customize Data Read/Write

**Read:** Override `GetFormattedValue` in custom cell (e.g., `PropertyGridCell` formats with units).

**Write:** Override `ParseFormattedValue` in custom cell → delegate to domain `StepProperty.WithValue`.

---

## Troubleshooting

### Common Issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| Disabled ComboBox shows white bg | Cell not initialized or no forced Invalidate after VM build | Check `InitializeComboBoxCellStrategies`, ensure initial `Invalidate` |
| Wrong value after Action change | Missing delayed invalidation | Verify timer in `OnStepPropertyChangedInternal` fires |
| FormatException on input | Invalid text for `PropertyGridCell` | Check external property config (min/max/type) |
| ComboBox empty after Action change | `CurrentActionId` closure-captured instead of live read | Ensure `StepViewModel.CurrentActionId` reads from `_stepRecord` |

### Diagnostics

Internal logger writes to `%APPDATA%\NtoLibLogs\debug_log.txt` (not covered here; check main README).

Key events visible in log (if enabled):
- Table initialization stages
- Row count changes
- Validation failures (routed to status manager)

---

## Extension Points

| What | How |
|------|-----|
| New visual column type | New factory + custom cell |
| Dynamic enum data source | Modify external provider (injected via DI) |
| Style calculation logic | Extend `ColorScheme.GetStyleForState` |
| Row error highlighting | Add field to `CellVisualState` + resolver logic |

---

## Maintenance Rules

- Domain model changes (new Action) → verify YAML consistency and Disabled state handling.
- Do not put heavy logic in `CellFormatting` (called frequently).
- All visual state changes must flow through `CellStateResolver`, not direct paint.
- When adding functionality: YAML valid → Factory registered → ViewModel returns value → `GetDataState` handles column (if needed) → no excess Invalidate → color scheme intact.

---

## Minimal Checklist (Adding Feature)

- [ ] External YAML updated and valid
- [ ] Factory registered (if new type)
- [ ] ViewModel returns value in `GetCellValue`
- [ ] `GetDataState` considers new column (if applicable)
- [ ] No redundant Invalidate calls
- [ ] Color scheme intact (Disabled/Current/Passed distinguishable)