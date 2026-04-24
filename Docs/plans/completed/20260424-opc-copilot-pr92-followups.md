# Plan: Address Copilot PR #92 review findings

## Goal

Fix four actionable findings from Copilot's review of PR #92 plus one small
tech-debt cleanup that the user opted into. Findings 4 and 6 are skipped —
4 is an intentional change by the user (documented out of band), 6 is a
pre-existing fragile pattern unrelated to this PR and not worth the churn
right now.

## Findings to address

| # | Finding | Fix |
|---|---|---|
| 1 | `ScanAndValidate` always creates `PendingPlan` even when current group already matches desired | Short-circuit: compute `toRemove`/`toAdd` from `groupResult.Value.Group.Items` vs `desiredNames`; if both empty, log "no changes required", set `PendingPlan = null`, return `Ok()`. |
| 2 | Missing/unparseable `tree.json` currently logs Warning and proceeds with empty snapshot — silent shrink-only | Replace the Warning branch with `LogAndFail("Snapshot load failed: …")`. Treat as configuration error. |
| 3 | `OpcScadaItemDto.ToScadaItem` silently assigns `null` DataType when `Type.GetType(DataType)` cannot resolve | Throw `InvalidOperationException` when `DataType != null && Type.GetType(DataType) == null`. Consistent with the existing throw on unparseable `PinValueType`/`DeadbandType`. `DeferredExecutor.cs:75` catches and converts to logged Result. |
| 5 | `Tests/Tests.csproj` has a dangling `<Reference Include="System.Text.Json">` to `..\Resources\System.Text.Json.dll`, a file that was deleted in the squash | Delete the three-line `<Reference>` block. `PackageReference` at line 23 is the only source going forward. |
| 7 | `NtoLib.csproj` declares both framework `<Reference Include="System.ValueTuple" />` and imports `..\packages\System.ValueTuple.4.6.2\build\net471\System.ValueTuple.targets` via NuGet package; package is redundant on net48 | Remove the package from `packages.config`, remove the paired `<Import Project="...System.ValueTuple.4.6.2...">` and `<Error Condition>` lines from `NtoLib.csproj`. `<Reference Include="System.ValueTuple" />` resolves to the in-box BCL 4.8 assembly. |

## Style decisions

- Fix 3 uses `throw`, not `Result<OpcUaScadaItem>` promotion, because
  `ToScadaItem` already throws on sibling validation (`PinValueType`,
  `DeadbandType`) at lines 86-88. `DeferredExecutor.cs:75` wraps the whole
  `PlanExecutor.Execute` call in `catch (Exception)` and logs as a failed
  `Result`, so propagation to the caller is identical to a Result-based
  signature without polluting five files with `Result<T>` threading.
- Fix 2 does not split shrink-only vs shrink+expand cases. User explicitly
  chose strict behaviour — missing snapshot is a config error, full stop.

## Non-goals

- Finding 4 (Copy.ps1 config dir cleanup): intentional user change. Not
  documented but kept as-is.
- Finding 6 (Tests.csproj Serilog hard-coded HintPath): pre-existing
  fragility inherited from `packages.config`-driven NtoLib restore. Works
  in practice because NtoLib builds first. Not this PR's problem.

## Tasks

### Task 1: Fix 5 — drop dangling System.Text.Json `<Reference>`

**Files:**
- Modify: `Tests/Tests.csproj`

- [x] Remove the 3-line `<Reference Include="System.Text.Json"><HintPath>..\Resources\System.Text.Json.dll</HintPath></Reference>` block (lines 47-49).
- [x] `dotnet build NtoLib.sln` — zero new errors or warnings.
- [x] `dotnet test NtoLib.sln` — all pass.

### Task 2: Fix 7 — unify System.ValueTuple to in-box BCL

**Files:**
- Modify: `NtoLib/packages.config`
- Modify: `NtoLib/NtoLib.csproj`

- [x] Remove `<package id="System.ValueTuple" version="4.6.2" targetFramework="net48" />` from `packages.config`.
- [x] Remove the paired `<Error Condition>` (line ~900) and `<Import Project>` (line ~903) lines for `System.ValueTuple.4.6.2\build\net471\System.ValueTuple.targets` from `NtoLib.csproj`.
- [x] Keep `<Reference Include="System.ValueTuple" />` (line ~225) — it binds to the in-box BCL type on net48.
- [x] `dotnet build NtoLib.sln` — zero new errors.
- [x] `dotnet test NtoLib.sln` — all pass.

### Task 3: Fix 3 — throw on unresolved DataType in OpcScadaItemDto

**Files:**
- Modify: `NtoLib/OpcTreeManager/Entities/OpcScadaItemDto.cs`

- [x] Before the `new OpcUaScadaItem { ... DataType = … }` initializer, resolve the type into a local: `var dataType = DataType != null ? Type.GetType(DataType) : null;`.
- [x] If `DataType != null && dataType == null`, throw `InvalidOperationException($"Cannot resolve snapshot DataType '{DataType}' for item '{Name}'.")`. Match existing error message format (lines 86-88).
- [x] Assign `dataType` in the initializer.
- [x] `dotnet build` + `dotnet test` — all pass.

### Task 4: Fix 2 — Fail when snapshot load fails

**Files:**
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`

- [x] In `ScanAndValidate`, replace the `snapshotResult.IsFailed` Warning+continue branch (lines 84-101) with `return LogAndFail(new[] { new Error($"Snapshot load failed at '{treeJsonPath}': {string.Join("; ", snapshotResult.Errors)}") });` (or the existing helper shape — match the sibling calls at lines 56, 77).
- [x] `dotnet build` + `dotnet test` — all pass.

### Task 5: Fix 1 — Short-circuit when no changes required

**Files:**
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`

- [x] After building `desiredNames` and `expandSpecs`, compute `currentNames = groupResult.Value.Group.Items.Select(i => i.Name).ToHashSet(StringComparer.Ordinal)`.
- [x] If `currentNames.SetEquals(desiredNames)` — log `Information("OpcTreeManager: no operations required for group '{GroupName}' — nothing to do.", groupName)`, leave `PendingPlan = null`, return `Ok()`.
- [x] Otherwise build and assign `PendingPlan` as today.
- [x] `dotnet build` + `dotnet test` — all pass.

### Task 6: Final verification and format

**Files:**
- None (verification only).

- [x] `dotnet build NtoLib.sln` — 0 errors.
- [x] `dotnet test NtoLib.sln` — all pass.
- [x] `cd .format && dotnet format ../NtoLib.sln --include ../NtoLib/OpcTreeManager/ ../Tests/OpcTreeManager/ --verify-no-changes` — clean.
- [x] Manual smoke: re-read the 5 modified files to confirm no extraneous edits.

## Post-Completion

- **Tests note**: existing tests don't exercise the Fail-on-missing-snapshot or no-op short-circuit paths. Adding integration-style tests would require mocking `IProjectHlp` / the vendor COM, which is not currently stood up. Skipping unit coverage for Fix 1/2 — they will be verified in the next host smoke run. Fix 3 unit-testable via `OpcScadaItemDto` round-trip but adds a new test file; defer unless user asks.
- **Commit strategy**: one follow-up commit `fix(opc): address Copilot PR #92 review` summarising all five fixes. No separate commits per finding — they are small and share the rationale.
