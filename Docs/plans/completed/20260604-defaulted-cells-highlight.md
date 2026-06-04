# Defaulted Cells Highlight (Issue #90)

## Overview

When an operator changes a row's action in the MbeTable recipe grid, `RecipeMutator.ReplaceStepAction` rebuilds the entire `Step` with default values. The operator may not notice that cells silently changed. This plan adds an orange background to every cell that was initialized to a default by an action change, cleared per-cell on operator acknowledgment and in bulk on Send/Save/Load.

Locked behavioral decisions (agreed with the user):

1. Highlight all `Enabled` cells of the affected row, excluding the Action column and disabled/readonly cells.
2. Per-cell clear: (a) a new value committed into the cell; (b) "visited and left" — the cell became `CurrentCell` and focus then moved to another cell.
3. Newly added rows (Add button, paste) are NOT highlighted — only action change marks.
4. Bulk clear on successful Send, Save (CSV), and Load. In `MbeTableEditor` (no Send button) Save is the bulk trigger — automatic, no variant branch.
5. Marks are transient UI state — never serialized, never sent to the PLC.
6. Repeated action change on the same row replaces its marks with a fresh set.

Architecture selected via multi-agent evaluation (3 competing placements, 3 judge lenses): **presentation-layer `DefaultedCellTracker` fed by factual application-layer events**. The application layer announces what happened (`ActionReplaced`, `CellValueCommitted`, `RecipeSent`, `RecipeSaved`, enriched `RecipeStructureChanged`); only one presentation class knows the word "orange".

## Context (from discovery)

- Action change path: `TablePresenter.OnCellValuePushed` → `RecipeOperationService.SetCellValueAsync` (action column discriminated at ~line 103) → `RecipeFacade.ReplaceAction` → `RecipeMutator.ReplaceStepAction` (full row rebuild).
- Rendering: `CellFormattingEngine.ResolveCellVisualState` composes tints (availability → loop → execution → contrast); colors centralized in `ColorScheme`, blend helpers in `ColorStyleHelpers`.
- `RecipeViewModel.OnRecipeStructureChanged` rebuilds all `StepViewModel`s — per-VM state does not survive; **after an action change `SetCellValueAsync` does NOT raise `RecipeStructureChanged`, so ViewModels are stale** — the tracker must read `RecipeFacade.CurrentSnapshot.Recipe.Steps[row]` via `PropertyStateProvider`, never the ViewModels.
- Structural mutations converge on `RecipeOperationService.NotifyStructureChanged()` call sites — **7 in total**: `AddStep` (~119), `RemoveStep` (~134), `LoadRecipeAsync` (~149), `ReceiveRecipe` (~247), `PerformCut` (~317), `PerformPaste` (~375), `PerformDelete` (~409). `TableInputActions` bypasses the presenter — index shifts must ride these existing emission sites. `Load` and `Receive` are the two `Reset` sites.
- **No pre-existing branches to hook**: `SetCellValueAsync` has a single `if (result.IsSuccess)` block (the action/non-action discriminator lives inside `PerformEditCellAsync` and is not surfaced); `SaveRecipeAsync` and `SendRecipeAsync` return the pipeline result directly with no `IsSuccess` block. All event raises below require small in-method gating to be added.
- `RecipeStructureChanged` has exactly one production subscriber: `TablePresenter.cs:54` — signature break is contained.
- `RecipeOperationService.cs` is already 459 lines (cap 300) — only event raises may be added there, no feature logic.
- `TableRenderCoordinator` holds the `DataGridView` directly, owns grid-event attach/detach and `InvokeOnUiThread` — visited-and-left wiring belongs there; no `ITableView` growth.
- Both shells create `TableRenderCoordinator` via `ActivatorUtilities.CreateInstance` — new ctor dependencies resolve from the container without touching either control shell.
- `NtoLib.csproj` has `EnableDefaultCompileItems=false` — every new `.cs` file needs an explicit `<Compile Include>`. `Tests.csproj` uses default globs — no edit needed.
- DI: register in `RegisterPresentationServices` (reached from `RegisterShared` by both `ConfigureServices` and `ConfigureEditorServices`) — editor variant gets the feature automatically.

## Development Approach

- **Testing approach: Regular** (code first, then tests within the same task).
- Complete each task fully before moving to the next.
- Make small, focused changes.
- **CRITICAL: every task MUST include new/updated tests** for code changes in that task; success and error scenarios both covered.
- **CRITICAL: all tests must pass before starting the next task** (`dotnet test NtoLib.sln`).
- **CRITICAL: update this plan file when scope changes during implementation.**
- Run `dotnet format NtoLib.sln` before presenting changes.
- Runtime validation in the MasterSCADA host follows unit tests for FB/UI integration behavior (project convention).

## Testing Strategy

- **Unit tests** (xUnit + FluentAssertions + Moq, no vendor SDK): tracker state machine, event contracts of `RecipeOperationService`, pure tint helper.
- **No e2e harness exists**; grid behaviors that need a live `DataGridView` (visited-and-left guard, repaint marshaling) are validated manually in MasterSCADA (see Post-Completion).

## Progress Tracking

- Mark completed items with `[x]` immediately when done.
- Add newly discovered tasks with ➕ prefix.
- Document issues/blockers with ⚠️ prefix.
- Keep plan in sync with actual work done.

## Solution Overview

- **`StructureChange` DTO** (`ModuleApplication`): mutation-shape vocabulary — `StructureChangeKind { Insert, Remove, Reset }` + indices (no `ActionReplaced` kind — that is a separate event, not a structure change). `RecipeStructureChanged` is enriched from `Action` to `Action<StructureChange>`.
- **Factual events on `RecipeOperationService`** (signals only, no state): in `SetCellValueAsync`'s existing single success block, re-derive the discriminator (`var isActionEdit = columnKey == MandatoryColumns.Action && value is short;` — same predicate as `PerformEditCellAsync`) and raise `ActionReplaced(int row)` or `CellValueCommitted(int row, ColumnIdentifier column)` accordingly (the commit event fires only on the non-action path, so the tracker never depends on "MarkRow excludes Action" for correctness). `CellValueCommitted` carries the **`ColumnIdentifier` key, not an index** — the service has no column list and must not gain one; key→index mapping is the tracker's job. `RecipeSent` / `RecipeSaved`: capture the pipeline result into a local, gate on `IsSuccess`, raise, then return (`SendRecipeAsync`/`SaveRecipeAsync` currently return the pipeline call directly).
- **`DefaultedCellTracker`** (`ModulePresentation/State`): the single owner of mark state (`Dictionary<int, HashSet<int>>` row → column indices). Subscribes to the service events; computes the markable column set via `PropertyStateProvider` against `RecipeFacade.CurrentSnapshot`; shifts indices on `Insert`/`Remove`; clears all on `Reset`/`RecipeSent`/`RecipeSaved`. Raises `MarksChanged(MarksChange)` (`Row` set ⇒ row repaint, `null` ⇒ bulk repaint). Exposes the narrow `IDefaultedCellsReader` (`IsMarked`) to the renderer (ISP).
- **Rendering**: one new tint step `ColorStyleHelpers.ApplyDefaultedTint` inserted into `CellFormattingEngine.ResolveCellVisualState` between the execution tint and contrast; color pair `DefaultedCellBgColor` / `DefaultedCellTintWeight` added to `ColorScheme`.
- **`TableRenderCoordinator`**: subscribes `MarksChanged` → marshaled repaint via existing `InvokeOnUiThread`; owns visited-and-left (`_table.CurrentCellChanged`, previous-cell cache, guard against programmatic moves during structural rebuilds).
- Sprawl budget: 8 production files know the feature exists; exactly 1 (`DefaultedCellTracker`) owns the logic; zero `ModuleCore` changes; zero control-shell changes.

## Technical Details

- `StructureChange` record: `(StructureChangeKind Kind, int Index, int Count, IReadOnlyList<int>? RemovedIndices)`. Emission mapping: `AddStep` → `Insert(index, 1)`; `RemoveStep` → `Remove([index])`; `PerformPaste` → `Insert(targetIndex, steps.Count)`; `PerformCut`/`PerformDelete` → `Remove(valid)` (the validated index list — set-based shift tolerates sorted vs unsorted); `Load`/`ReceiveRecipe` → `Reset`.
- Shift semantics: `Insert(i, n)` — marked rows `>= i` shift by `+n` (inserted rows unmarked); `Remove(set)` — marks of removed rows dropped, survivors decremented by the count of removed indices below them.
- `MarkRow(row)`: fresh `HashSet` per call (decision 6); columns where `PropertyStateProvider.GetPropertyState(step, key) == PropertyState.Enabled`, excluding `MandatoryColumns.Action`, with `step = RecipeFacade.CurrentSnapshot.Recipe.Steps[row]`.
- **Column index basis (pinned invariant)**: mark-store column indices are positions in the injected `IReadOnlyList<ColumnDefinition>`. This order is assumed identical to the `DataGridView` column order (grid columns are built from the same list; `DataGridViewAdapter.GetColumnKey` maps grid index → key by `column.Name`). `MarkRow` and `ClearCell` convert `ColumnIdentifier` → index via that list; the tracker tests must assert the key→index mapping.
- Threading: marks mutate synchronously inside the service's success branches (pool thread), then `MarksChanged` fires; the coordinator marshals every repaint through `InvokeOnUiThread`. Mark mutation always precedes the marshaled read.
- Visited-and-left guard: ignore `CurrentCellChanged` while `_table.IsCurrentCellInEditMode` transitions and while a structure-driven `RowCount` reset is in flight (`_suppressVisitedClear` flag around structural repaints). `DataGridView` fires `CurrentCellChanged` *after* the move — the coordinator caches the previous `(row, col)`.
- Editor variant: `SendRecipeAsync` returns early on the `_modbus is null` guard, so `RecipeSent` never fires there; `RecipeSaved` covers the editor's bulk clear. No variant-specific DI.

## Implementation Steps

### Task 1: Factual events and StructureChange in the application layer

**Files:**
- Create: `NtoLib/Recipes/MbeTable/ModuleApplication/StructureChange.cs`
- Modify: `NtoLib/Recipes/MbeTable/ModuleApplication/RecipeOperationService.cs`
- Modify: `NtoLib/Recipes/MbeTable/ModulePresentation/TablePresenter.cs`
- Modify: `NtoLib/NtoLib.csproj`
- Create: `Tests/MbeTable/Application/RecipeOperationServiceEventContractTests.cs`

- [x] create `StructureChange.cs`: `StructureChangeKind { Insert, Remove, Reset }` enum + immutable `StructureChange` record with static factories `Insert(int index, int count)`, `Remove(IReadOnlyList<int> indices)`, `Reset()` (enum and record may share the file as one cohesive contract, or split per one-class-per-file — follow repo convention)
- [x] change `RecipeOperationService.RecipeStructureChanged` to `event Action<StructureChange>?`; `NotifyStructureChanged(StructureChange change)`; update **all 7 emission sites** with the mapping from Technical Details (including `LoadRecipeAsync` ~149 → `Reset`)
- [x] in `SetCellValueAsync`'s single existing success block, re-derive `var isActionEdit = columnKey == MandatoryColumns.Action && value is short;` and raise either `event Action<int>? ActionReplaced` (action edit, next to existing `RaiseStepDataChanged`) or `event Action<(int Row, ColumnIdentifier Column)>? CellValueCommitted` (non-action edit; payload carries the key — the service has no column list and must not gain one)
- [x] add `event Action? RecipeSent` and `event Action? RecipeSaved`: in `SendRecipeAsync`/`SaveRecipeAsync` capture the pipeline result into a local, gate on `IsSuccess`, raise, then return (both currently return the pipeline call directly — this gating is new code) — no state, no column enumeration, signals only
- [x] update `TablePresenter.OnRecipeStructureChanged` signature to accept `StructureChange` (body unchanged: `RowCount` reset + `Invalidate`); fix subscribe/unsubscribe; update any test doubles
- [x] add `<Compile Include="Recipes\MbeTable\ModuleApplication\StructureChange.cs" />` to `NtoLib.csproj`
- [x] write contract tests: action edit raises `ActionReplaced(row)` and not `CellValueCommitted`; non-action edit raises `CellValueCommitted(row, key)` and not `ActionReplaced`; `AddStep`→`Insert`, `RemoveStep`→`Remove`, paste→`Insert(target,count)`, delete/cut→`Remove(valid)`, Load/Receive→`Reset`; successful Save raises `RecipeSaved`; Send with null modbus (editor) raises neither `RecipeSent` nor errors
- [x] run `dotnet test NtoLib.sln` — must pass before task 2

### Task 2: DefaultedCellTracker — state owner

**Files:**
- Create: `NtoLib/Recipes/MbeTable/ModulePresentation/State/DefaultedCellTracker.cs`
- Create: `NtoLib/Recipes/MbeTable/ModulePresentation/State/IDefaultedCellsReader.cs`
- Create: `NtoLib/Recipes/MbeTable/ModulePresentation/State/MarksChange.cs`
- Modify: `NtoLib/NtoLib.csproj`
- Modify: `NtoLib/Recipes/MbeTable/ModuleInfrastructure/DiContainer.cs`
- Create: `Tests/MbeTable/Presentation/DefaultedCellTrackerTests.cs`

- [x] create `MarksChange.cs` (`record MarksChange(int? Row)`) and `IDefaultedCellsReader.cs` (`bool IsMarked(int row, int col)` — read-only, event-free)
- [x] create `DefaultedCellTracker` implementing `IDefaultedCellsReader`, `IDisposable`: ctor takes `RecipeOperationService`, `RecipeFacade`, `PropertyStateProvider`, `IReadOnlyList<ColumnDefinition>`; subscribes to `ActionReplaced` → `MarkRow`, `CellValueCommitted` → `ClearCell`, `RecipeStructureChanged` → `ApplyStructureChange`, `RecipeSent`/`RecipeSaved` → `ClearAll`; unsubscribes in `Dispose`
- [x] implement `MarkRow` (fresh set, Enabled-only via `PropertyStateProvider` against `RecipeFacade.CurrentSnapshot`, Action excluded), `ClearCell`, `ClearAll`, `ApplyStructureChange` (Insert shift / Remove drop-and-decrement / Reset clear), `event Action<MarksChange>? MarksChanged` raised after every state mutation that changed anything; key→index conversion against the injected `IReadOnlyList<ColumnDefinition>` lives here (pinned invariant from Technical Details)
- [x] register in `DiContainer.RegisterPresentationServices`: `AddSingleton<DefaultedCellTracker>()` + `AddSingleton<IDefaultedCellsReader>(sp => sp.GetRequiredService<DefaultedCellTracker>())`
- [x] add the three `<Compile Include>` entries to `NtoLib.csproj` in a single edit
- [x] write `DefaultedCellTrackerTests`: MarkRow seeds exactly Enabled non-Action columns (readonly `step_start_time` and `action` excluded); key→index mapping matches positions in the `ColumnDefinition` list; repeated MarkRow replaces the set; ClearCell clears one cell, siblings survive; `Insert(i,n)` shifts rows `>= i`, leaves rows `< i`; `Remove(set)` drops removed rows and decrements survivors (cases: removing a marked row; removing between two marked rows; **unsorted, deduplicated index list mirroring the `PerformDelete` shape** — it builds `valid` with `Distinct()` but no `OrderBy`); `Reset`/`ClearAll` empties and raises `MarksChanged(null)`; `MarksChanged` payload scope is `Row` for single-row ops; no event when nothing changed
- [x] run `dotnet test NtoLib.sln` — must pass before task 3

### Task 3: Orange tint in the rendering chain

**Files:**
- Modify: `NtoLib/Recipes/MbeTable/ModulePresentation/Style/ColorScheme.cs`
- Modify: `NtoLib/Recipes/MbeTable/ModulePresentation/Style/ColorStyleHelpers.cs`
- Modify: `NtoLib/Recipes/MbeTable/ModulePresentation/Rendering/CellFormattingEngine.cs`
- Create or extend: `Tests/MbeTable/Presentation/ColorStyleHelpersDefaultedTintTests.cs` (or the existing helper-test file if one exists)

- [x] add `DefaultedCellBgColor` (orange) and `DefaultedCellTintWeight` to `ColorScheme` with defaults, following the existing color/weight pattern
- [x] add `ColorStyleHelpers.ApplyDefaultedTint(Color baseColor, bool isMarked, bool isRestricted, ColorScheme scheme)` mirroring `ApplyExecutionTint` (early-return when not marked; `Blend` toward `DefaultedCellBgColor`)
- [x] add `IDefaultedCellsReader` ctor param to `CellFormattingEngine`; in `ResolveCellVisualState` insert the defaulted tint after `ApplyExecutionTint` and feed the result into `EnsureContrast`, `CellVisualState.BackColor`, and the user-selection blend below — no column guard (Action/disabled never in the mark set) (also threaded an `IDefaultedCellsReader` ctor param through `TableRenderCoordinator` so the manually-constructed engine compiles; full `MarksChanged`/visited-and-left wiring is Task 4)
- [x] write tint tests: returns base color when not marked; blends toward orange at configured weight when marked; restricted attenuation mirrors execution tint behavior
- [x] run `dotnet test NtoLib.sln` — must pass before task 4

### Task 4: TableRenderCoordinator wiring — repaint and visited-and-left

**Files:**
- Modify: `NtoLib/Recipes/MbeTable/ModulePresentation/Rendering/TableRenderCoordinator.cs`

- [x] add `DefaultedCellTracker` ctor param (auto-resolved via `ActivatorUtilities.CreateInstance` in both shells); pass it as `IDefaultedCellsReader` into the manually-constructed `CellFormattingEngine`
- [x] subscribe `tracker.MarksChanged` in `AttachEventHandlers`: `Row` set → `InvokeOnUiThread(FormatRowCells(r) + InvalidateRow(r))`; `null` → `InvokeOnUiThread(FormatAllCells + Invalidate)`; add unsubscribe to `DetachEventHandlers` / `SafeDisposal.RunAll`
- [x] wire visited-and-left: `_table.CurrentCellChanged` handler caching previous `(row, col)`; on a genuine user move call `tracker.ClearCell(prevRow, prevCol)`
- [x] implement the programmatic-move guard: skip clears while `IsCurrentCellInEditMode` transitions and while a structural rebuild is in flight (`_suppressVisitedClear` around structure-driven repaints)
- [x] build both variants compile: `dotnet build NtoLib.sln` (coordinator wiring is grid-bound; behavior is covered by the manual runtime pass — extracted the index-addressed `DefaultedCellTracker.ClearCell(int row, int columnIndex)` helper and covered it with unit tests)
- [x] run `dotnet test NtoLib.sln` — must pass before task 5

### Task 5: Verify acceptance criteria

- [x] verify all six locked decisions are implemented (trace each to code): (1) `MarkRow` marks Enabled non-Action cells only; (2) per-cell clear via `CellValueCommitted`→`ClearCell` and `OnCurrentCellChanged`→`ClearCell`; (3) `ApplyInsert` shifts but never marks inserted rows, Add emits `Insert` not `ActionReplaced`; (4) `RecipeSent`/`RecipeSaved`→`ClearAll`, Load/Receive→`Reset`→`ClearAll`; (5) marks live only in `_marks` dictionary, never serialized; (6) `MarkRow` builds a fresh `HashSet` each call
- [x] verify edge cases: repeated action change replaces marks (fresh set); paste does not mark (`Insert` only shifts); multi-delete with unsorted indices shifts correctly (set-based `ApplyRemove` with per-survivor decrement count); editor variant resolves DI graph — `RegisterPresentationServices` reached via `RegisterShared` from `ConfigureEditorServices`; tracker deps all singletons; editor `SendRecipeAsync` returns early on null modbus so `RecipeSent` never fires, `RecipeSaved` covers bulk clear
- [x] run full test suite: `dotnet test NtoLib.sln` — 287 passed, 0 failed
- [x] run `dotnet format NtoLib.sln` and resolve any diagnostics — fixed the only feature diagnostic (`ColorStyleHelpersDefaultedTintTests` static field renamed to `_scheme`); the one remaining `IDE1006` is pre-existing in `EditorRuntimeOptionsProviderTests.cs` (commit `50321b8`), out of scope
- [x] confirm sprawl budget held: 8 production files know the feature (`DefaultedCellTracker`, `IDefaultedCellsReader`, `MarksChange`, `CellFormattingEngine`, `TableRenderCoordinator`, `ColorScheme`, `ColorStyleHelpers`, `DiContainer`); feature logic only in `DefaultedCellTracker`; zero `ModuleCore` changes; zero control-shell changes

### Task 6: [Final] Update documentation

- [x] update `Docs/mbe-table.md` (per-FB user documentation) with the orange-highlight behavior and clearing rules, in Russian per established style (added section 2.8 "Подсветка ячеек по умолчанию")
- [x] update `CLAUDE.md` / `Docs/architecture/architecture.md` only if a genuinely new reusable pattern emerged (factual-event tracker), otherwise skip (skipped — feature-specific, not a reusable template; architecture.md already covers the shared-DI/event patterns it builds on)
- [x] move this plan to `docs/plans/completed/`

## Post-Completion

**Manual verification in MasterSCADA host (required before release, per FB/UI-first convention):**
- Action change paints the row orange (except Action and disabled cells); repeated change repaints fresh.
- Visited-and-left clears exactly the left cell; no spurious clears during add/remove/paste/load (programmatic `CurrentCell` moves) — this guard is the one piece that cannot be unit-tested.
- Bulk clear repaints the whole grid on successful Send, Save, and Load; failed Send/Save leaves marks intact.
- `MbeTableEditor`: highlight works, Save clears, no errors from the absent Modbus path.
- Repaint correctness while a recipe is executing (orange composes with loop/execution tints and the current-line highlight).

**External:**
- Close GitHub issue #90 with a reference to the merged PR.
