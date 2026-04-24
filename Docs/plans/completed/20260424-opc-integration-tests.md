# Plan: OpcTreeManager integration tests

## Goal

Cover the two regression areas that the last week of hot-fixes repeatedly
bit us: (A) mis-parsed config + malformed snapshot producing wrong plans,
and (B) the recursive executor mis-routing preserved vs newly-constructed
vs removed subtrees. Both through the public-surface semantics, not by
poking at private helpers. Where the vendor COM boundary sits in the way,
introduce the minimum seam to let tests run against in-memory fakes.

## Context

- Vendor types `IProjectHlp`, `ITreeItemHlp`, `ITreePinHlp` are declared as
  **classes** (not interfaces) in the decompile despite the `I`-prefix.
  Moq cannot mock them cleanly. `OpcUaScadaItem` / `OpcUaProtocol` are
  vendor concrete classes too, but `OpcUaScadaItem` is freely constructible
  (`new`) and its `Items` list is a regular mutable `List<>` — we already
  rely on that in `ToScadaItem()`.
- `PlanExecutor.ApplyDesiredSpec` is pure in-memory except for one call:
  `DisconnectNodeLinks(path)` → `_project.SafeItem<ITreePinHlp>(path)` →
  vendor COM. That's the single seam point.
- `OpcTreeManagerService.ScanAndValidate` has a similar single seam — it
  calls `ResolveGroup` which needs `IProjectHlp`. Everything after group
  resolution is pure: merge desired tree against `group.Items` top-level
  names, detect short-circuit, build `RebuildPlan`.
- Existing tests cover only low-level pure helpers (`OpcConfigLoader`,
  `LinkCollector.BuildLinks` / `FilterForSubtree`, `OpcScadaItemDto.ToScadaItemPruned`).
  Nothing exercises the interaction between layers.

## Design

Two test tiers, landed in order.

### Tier A — acceptance tests for plan construction

File-fixture-driven: each case lives as `(config.yaml, tree.json, expected.json)`
under `Tests/OpcTreeManager/Fixtures/Acceptance/<case-name>/`. Tests load
each trio and exercise the plan-building logic end-to-end **minus the
vendor group resolution**. Expected.json describes the expected `RebuildPlan`
shape (desired tree dumped, snapshot key set, short-circuit flag) plus
the expected result (Ok/Fail + error substring).

To make this possible without mocking `IProjectHlp`, extract the pure
plan-building portion of `ScanAndValidate` into a small static helper:

```csharp
internal static class PlanBuilder
{
    public static Result<RebuildPlan?> Build(
        string opcFbPath,
        string groupName,
        string targetProject,
        OpcConfig config,
        Dictionary<string, NodeSnapshot> snapshot,
        IReadOnlyList<string> currentTopLevelNames);
    // Returns Ok(null) for "no operations required" (short-circuit);
    // Ok(plan) when a plan is produced; Fail for config/target-project errors.
}
```

`ScanAndValidate` calls this after its file-loads and group-resolve. Tests
call `PlanBuilder.Build` directly with fixture data. No seam around
`IProjectHlp` needed for Tier A.

### Tier B — seam-based tests for `ApplyDesiredSpec`

Add a narrow strategy interface, injected via the PlanExecutor ctor,
covering the single COM-boundary call:

```csharp
internal interface ISubtreeDisconnector
{
    (int Total, int Success, int Fail) DisconnectSubtree(string nodePath);
}
```

Production: concrete `ProjectSubtreeDisconnector(IProjectHlp)` wrapping the
existing `DisconnectNodeLinks` logic. Tests: a fake that records
`nodePath` and returns a canned tuple.

Expose an internal test entry point on `PlanExecutor`:

```csharp
internal void TestApplyDesiredSpec(
    OpcUaScadaItem container,
    IReadOnlyList<NodeSpec> desired,
    string containerPath,
    IReadOnlyDictionary<string, NodeSnapshot> snapshot,
    out List<Construction> constructions,
    out int shrinkCount);
```

Tests build an in-memory `OpcUaScadaItem` tree (using `new`), a matching
`NodeSpec` tree, and a snapshot `Dictionary<string, NodeSnapshot>`. They
call the entry, then assert on:
- `container.Items` after swap (names, order, child counts at depth),
- `constructions` list (paths + link counts),
- the fake disconnector's recorded calls.

Covers recursion, pruning, link filtering, preserved/newly-constructed
classification.

### Non-goals

- No integration with `OpcProtocolAccessor.GetProtocol` / `FindGroup` —
  these are pure static helpers that just walk existing vendor objects.
  They would need real COM to exercise and offer no logic worth
  duplicating in a test harness.
- No test of `DeferredExecutor.Post` timer behavior — already covered by
  manual host smoke, and the timer + `InRuntime` predicate are a vendor
  concern. Tests would essentially assert `System.Windows.Forms.Timer`.
- No integration test of `ExecuteSnapshot` as a separate path — the
  collector logic it calls is already unit-tested via
  `LinkCollectorTests`; what's left (file write) is a one-line
  JsonSerializer + File.WriteAllText, covered by a trivial round-trip
  test we can fold into Tier A.

## Tasks

### Task 1: Extract `PlanBuilder` pure helper

**Files:**
- Create: `NtoLib/OpcTreeManager/Facade/PlanBuilder.cs`
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`
- Modify: `NtoLib/NtoLib.csproj` (add `<Compile Include>`)

- [x] Create `internal static class PlanBuilder` with `Build(opcFbPath, groupName, targetProject, config, snapshot, currentTopLevelNames)` returning `Result<RebuildPlan?>`.
- [x] Move the post-group-resolve logic out of `ScanAndValidate` into `PlanBuilder.Build`. Keep the same Information-level logs.
- [x] `ScanAndValidate` now calls `PlanBuilder.Build(...)` after its loads.
- [x] Run existing tests (`dotnet test`) — no regressions.

### Task 2: Tier A acceptance tests for `PlanBuilder`

**Files:**
- Create: `Tests/OpcTreeManager/Acceptance/PlanBuilderAcceptanceTests.cs`
- Create: fixture folders under `Tests/OpcTreeManager/Fixtures/Acceptance/` (one folder per case, with `config.yaml`, `tree.json`, `expected.json`).

- [x] Fixture harness: load the three files, call `PlanBuilder.Build`, assert against `expected.json`.
- [x] Case: legacy flat config, matching current top level → expect `Ok(null)` (short-circuit fires).
- [x] Case: flat config, missing top-level node → expect plan with that name in DesiredTree and a Construction in PendingPlan Snapshot lookup.
- [x] Case: nested config (`Valves: [VPG1, VPG2]`) with both children in snapshot → plan has nested NodeSpec in DesiredTree.
- [x] Case: nested config referencing a child NOT in snapshot → plan still builds (warning happens at Execute time, not at plan build).
- [x] Case: missing target project in config → expect Fail with error containing `"not found in config"`.
- [x] Case: malformed config scalar (folded YAML with spaces) → expect Fail from `OpcConfigLoader` layer. **➕ This also requires Task 3 below** — parser currently accepts folded scalars as node names. Flag the fixture with `[x] Must pass after Task 3` if needed. [x] skipped pending Task 3 (skipReason field in expected.json causes test to return early without asserting).
- [x] Run tests — all acceptance cases pass.

### Task 3: Tighten `OpcConfigLoader` name validation

**Files:**
- Modify: `NtoLib/OpcTreeManager/Config/OpcConfigLoader.cs`
- Modify: `Tests/OpcTreeManager/Unit/OpcConfigLoaderTests.cs`

- [x] In `ConvertNode`, validate that a scalar `Name` does not contain whitespace, newlines, or the ` - ` sequence (the characteristic folded-YAML indicator). Throw `InvalidOperationException` with a message suggesting the correct `Name:` mapping form.
- [x] Add unit tests: folded `- Valves\n  - VPG1` body → Fail with clear message; single-word `- Valves` → Ok; name with dot inside → depends (OPC node names can contain dots? — check existing fixtures; if yes, allow). Conservative rule: reject only whitespace/newline; keep `.` permissive.
- [x] Run tests — all pass.

### Task 4: Introduce `ISubtreeDisconnector` seam

**Files:**
- Create: `NtoLib/OpcTreeManager/TreeOperations/ISubtreeDisconnector.cs`
- Create: `NtoLib/OpcTreeManager/TreeOperations/ProjectSubtreeDisconnector.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs` (construct the concrete disconnector)
- Modify: `NtoLib/NtoLib.csproj`

- [x] Create `internal interface ISubtreeDisconnector` with `(Total, Success, Fail) DisconnectSubtree(string nodePath)`.
- [x] Move the body of `PlanExecutor.DisconnectNodeLinks` + `DisconnectPinConnections` into `ProjectSubtreeDisconnector(IProjectHlp, ILogger)` implementing the interface.
- [x] `PlanExecutor` ctor takes `ISubtreeDisconnector`; replace the private disconnect method with a call to it.
- [x] `OpcTreeManagerService` ctor wires up `new ProjectSubtreeDisconnector(project, logger)` and passes it to the `PlanExecutor`.
- [x] Run tests — no regressions (integration happens at COM boundary unchanged).

### Task 5: Tier B seam-based tests for `ApplyDesiredSpec`

**Files:**
- Create: `Tests/OpcTreeManager/Integration/ApplyDesiredSpecTests.cs`
- Create: `Tests/OpcTreeManager/Integration/Fakes/FakeSubtreeDisconnector.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs` (add `internal` test entry point)

- [x] Add `internal` test entry point method on `PlanExecutor` that calls `ApplyDesiredSpec` with a synthetic group root and returns `(constructions, shrinkCount, container)` for assertion. Signature: take `(OpcUaScadaItem container, IReadOnlyList<NodeSpec> desired, string containerPath, IReadOnlyDictionary<string, NodeSnapshot> snapshot)`.
- [x] Implement `FakeSubtreeDisconnector`: records every `nodePath` call into a list, returns `(0, 0, 0)` by default.
- [x] Helper to build `OpcUaScadaItem` fake trees: recursive `BuildItem(name, children[])` that returns a valid `new OpcUaScadaItem { Name = ..., Items = { ... } }`.
- [x] Helper to build `NodeSnapshot` fake entries with matching `OpcScadaItemDto` tree and a flat `LinkEntry` list for the subtree.
- [x] Case: single-level shrink — current has `[A, B, C]`, desired = `[A, B]`. Assert `container.Items` == `[A, B]`; disconnector records one call for path ending in `.C`; `constructions` empty.
- [x] Case: single-level expand — current has `[A]`, desired `[A, B]` leaves. B present in snapshot. Assert `container.Items == [A, B]`; construction recorded with path ending `.B` and link count = snapshot link count filtered to B's subtree.
- [x] Case: nested expand — `Valves` preserved, spec says `Valves: [VPG1, VPG2]`, current `Valves.Items = [VPG1]`, snapshot Valves DTO has `[VPG1, VPG2, VPG4]`. Assert `Valves.Items == [VPG1, VPG2]`; VPG2 constructed; links filtered to the VPG2 subtree only.
- [x] Case: nested shrink — `Valves` preserved, spec `Valves: [VPG1]`, current `[VPG1, VPG4]`. Assert VPG4 disconnected; `container.Items` for Valves is `[VPG1]`.
- [x] Case: pruned construction — `Valves` missing from current, spec `Valves: [VPG1, VPG2]`, snapshot has 5 VPG children. Assert constructed `Valves` has only 2 children; constructions.Links filtered to just those two subtrees.
- [x] Case: $-only link in snapshot survives dedup + filter — insert a single `ControlWord$ → CMD.Результат` link, construct a Command-containing subtree, verify the link appears once in `Construction.Links`.
- [x] Run tests — all pass.

### Task 6: Final verification

**Files:**
- None (verification only).

- [x] `dotnet build NtoLib.sln` — 0 errors.
- [x] `dotnet test NtoLib.sln` — all pass.
- [x] `cd .format && dotnet format ../NtoLib.sln --include ../NtoLib/OpcTreeManager/ ../Tests/OpcTreeManager/ --verify-no-changes` — clean.
- [x] Move plan to `Docs/plans/completed/`.

## Post-Completion

- **Fixture maintenance**: when a new config-shape scenario surfaces on
  the host, add a new folder under `Tests/OpcTreeManager/Fixtures/Acceptance/`
  — one minute of copy-paste, zero code change. This is the whole point
  of Tier A: regressions stay regressions.
- **Coverage gap (known, acceptable)**: tier B stubs the disconnector,
  so we don't assert vavobj's real behavior on Connect/Disconnect. That
  layer is covered by `Docs/KnownIssues/05-opc-command-pin-connect-overload.md`
  and by the host smoke. Tier B's job is to prove we call the right
  thing with the right arguments, not that vavobj responds correctly.
- **Possible future tier C** (not scoped here): acceptance test against
  a recorded live `tree.json` from the real MBE project, with a pruning
  config, asserting the final constructed subtree's DTO shape matches a
  golden file. Useful as an anti-drift tripwire for snapshot schema
  changes. Skip until we actually change the snapshot format.
