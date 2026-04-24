# Plan: Nested node specifications in OpcTreeManager config.yaml

## Goal

Let `config.yaml` specify not only top-level group children but also their
descendants to arbitrary depth, so a target project can keep a parent node
with only a *subset* of its children. Example: two projects both keep
`Valves`, but one keeps `[VPG1, VPG2]` and the other keeps only `[VPG2]`.

Today `config.yaml` is a flat `project -> list of direct-child names` and
`PlanExecutor` only mutates `group.Items` at the top level. After this
change the spec is a tree, and shrink/expand decisions happen at every
nesting level.

## Context

Per issue #88 comment from 2026-04-23. Current surface (from code audit):

- `NtoLib/OpcTreeManager/Config/OpcConfig.cs` — `Dictionary<string, List<string>>`
- `NtoLib/OpcTreeManager/Config/OpcConfigLoader.cs` — direct `Deserialize<OpcConfig>`
- `NtoLib/OpcTreeManager/Entities/RebuildPlan.cs` — `DesiredNodeNames: IReadOnlyList<string>`
- `NtoLib/OpcTreeManager/Entities/ExpandSpec.cs` — `{Name, ScadaItem, Links}`
- `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs` — `ScanAndValidate`, `BuildExpandSpecs`
- `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs` — `Execute`, `BuildNewItems`, `DisconnectAll`
- `NtoLib/OpcTreeManager/Config/TreeSnapshotLoader.cs` / `TreeSnapshotWriter.cs` — dict keyed by top-level name, each `NodeSnapshot` has full-subtree `scadaItem` (which already nests via `Items`) and a flat `links` list for the whole subtree
- `NtoLib/OpcTreeManager/TreeOperations/LinkCollector.cs` — already works on any `ITreeItemHlp`, no change needed

## Design decisions (confirmed with user)

1. **YAML syntax**: block-mapping form.
   ```yaml
   MBE:
     - Cameras           # scalar -> leaf
     - Valves:           # mapping -> non-leaf with listed children
         - VPG1
         - VPG2
   ```
   A leaf means "keep this node with its entire current subtree untouched".
   A non-leaf means "keep this node, but within it only keep the listed
   children, recursively".

2. **Backward compatibility**: existing flat `config.yaml` continues to parse
   without modification. All current projects (`MBE`, `PLASMA`, `MAGN`) are
   leaf-only and still work.

3. **`tree.json` schema unchanged**: still `Dictionary<string, NodeSnapshot>`
   keyed by top-level child name. `NodeSnapshot.scadaItem.items` already
   nests to arbitrary depth and `NodeSnapshot.links` is a flat list covering
   the whole subtree. Deep-sub-node construction pulls the DTO and filters
   links by path prefix. **User must regenerate `tree.json` from a
   master-project state that contains the union of all possible
   sub-nodes across all target projects** — noted in the user doc.

4. **Depth**: arbitrary, via recursion.

5. **Leaf semantics on construction**: if a leaf node is missing from the
   current group, it is constructed with its full subtree from the snapshot.
   If a non-leaf is missing, it is constructed *pruned* to only the listed
   descendants.

## Solution overview

**Config layer.** Introduce a `NodeSpec` record:

```csharp
public sealed record NodeSpec(string Name, IReadOnlyList<NodeSpec>? Children);
// Children == null -> leaf ("keep whole subtree as-is")
```

YAML is deserialized into an intermediate `Dictionary<string, List<object>>`
(each element is either a string scalar or a one-entry map); a small
post-processing pass converts each list into `List<NodeSpec>` recursively.
Keeps `YamlDotNet` usage off custom converters.

**Plan layer.** Replace `RebuildPlan.DesiredNodeNames: IReadOnlyList<string>`
with `DesiredTree: IReadOnlyList<NodeSpec>`. The plan carries the full
snapshot dictionary so the executor can resolve deep constructions on
demand. `ExpandSpec` is retired — executor builds new items inline from the
snapshot + spec tree.

**Executor.** `PlanExecutor.Execute` calls a new recursive method
`ApplyDesiredSpec(container, desired, containerPath, snapshot)` that:
1. Computes `toRemove = current.Items.Name \ desired.Name`, disconnects
   each removed subtree via live re-enumeration (existing
   `DisconnectNodeLinks`), then removes from `container.Items`.
2. For each `spec in desired`:
   - **Preserved** (name already in current): keep the existing item; if
     `spec.Children != null`, recurse with the existing item as new
     container and `spec.Children` as new desired.
   - **Newly constructed** (name missing from current): resolve the top-level
     snapshot entry by walking the snapshot tree from the group root down to
     this spec's path, then *prune* the constructed `OpcUaScadaItem` to
     match `spec.Children` (or keep full subtree if `spec.Children == null`).
     Queue the filtered links for reconnection after the top-level swap.
3. Replace `container.Items` with the freshly-ordered list (same swap
   pattern as today).

`SynchWihSysTree` and `ApplyChange` run once at the group level after the
full recursion, matching today's behavior.

**Pruning helper.** Add `OpcScadaItemDto.ToScadaItemPruned(NodeSpec spec)`:
- If `spec.Children == null` → delegate to existing `ToScadaItem()`.
- Otherwise build the `OpcUaScadaItem` with only those children whose names
  appear in `spec.Children`, calling themselves recursively. Missing child
  in snapshot → `InvalidOperationException` consistent with existing
  validation style.

**Link filter.** Add `LinkEntry` filter by path prefix:
`links.Where(l => l.LocalPinPath.StartsWith(keptSubtreePath + ".", Ordinal) || l.LocalPinPath == keptSubtreePath)`.
When expanding a pruned Valves containing only VPG1, keep only the links
whose `localPin` starts with `...Valves.VPG1.` (or equals `...Valves` for
pins at the Valves node itself). Used for newly-constructed nodes only.

## Non-goals

- No change to `tree.json` file format.
- No change to `LinkCollector`.
- No automatic migration tooling for old `config.yaml` — old format works as-is.
- No UI for editing the config — still manual YAML.

## Tasks

### Task 1: `NodeSpec` record and recursive YAML parser

**Files:**
- Modify: `NtoLib/OpcTreeManager/Config/OpcConfig.cs`
- Modify: `NtoLib/OpcTreeManager/Config/OpcConfigLoader.cs`
- Create: `NtoLib/OpcTreeManager/Entities/NodeSpec.cs`
- Modify: `NtoLib/NtoLib.csproj` (add the new Compile Include)

- [ ] Create `NodeSpec(string Name, IReadOnlyList<NodeSpec>? Children)` record in `Entities/NodeSpec.cs`.
- [ ] Change `OpcConfig.Projects` to `Dictionary<string, List<NodeSpec>>`.
- [ ] In `OpcConfigLoader.Load`, deserialize YAML into an intermediate form (`Dictionary<string, List<object>>`), then convert each `object` element to a `NodeSpec`: `string` → leaf, `Dictionary<object, object>` with exactly one key → non-leaf, recurse on the value's list. Fail with `Result.Fail` on malformed shapes (multiple keys in one mapping, non-string names, non-list children, etc.).
- [ ] Write unit tests for the loader covering: legacy flat list, mixed flat+nested, fully nested, empty children list (non-leaf with no children — valid, means "empty but present"), malformed (list inside list without wrapping map), multi-key mapping (rejected).
- [ ] `dotnet build` + `dotnet test` green.

### Task 2: `RebuildPlan` carries `DesiredTree` and full snapshot; retire `ExpandSpec`

**Files:**
- Modify: `NtoLib/OpcTreeManager/Entities/RebuildPlan.cs`
- Delete: `NtoLib/OpcTreeManager/Entities/ExpandSpec.cs`
- Modify: `NtoLib/NtoLib.csproj` (remove deleted file from Compile Include)

- [ ] Replace `RebuildPlan.DesiredNodeNames` with `IReadOnlyList<NodeSpec> DesiredTree`.
- [ ] Replace `RebuildPlan.ExpandSpecs` with `IReadOnlyDictionary<string, NodeSnapshot> Snapshot` (the full deserialized tree.json dict).
- [ ] Delete `ExpandSpec.cs`.
- [ ] Temporarily make `OpcTreeManagerService` and `PlanExecutor` compile with TODO stubs as needed — Tasks 3 and 4 replace the bodies. Don't break build.
- [ ] `dotnet build` green (runtime will fail until Task 4, that is expected).

### Task 3: Recursive `ScanAndValidate` producing a `NodeSpec`-tree plan

**Files:**
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`

- [ ] In `ScanAndValidate`, replace `desiredNames` / `desiredSet` with the `List<NodeSpec>` from `config.Projects[targetProject]` (plus the distinct-non-null filter, applied at every level).
- [ ] Update the no-op short-circuit (Fix 1 from the previous plan) to compare current `group.Items.Name` against top-level `spec.Name` set only. If the top level matches and every matching pair is either leaf or has empty `Children`, log "no operations required" and return `Ok()`. Any non-leaf with children means we can't know without scanning, so fall through to plan creation.
- [ ] Remove `BuildExpandSpecs` (no longer needed — executor pulls from snapshot directly). Drop all `ExpandSpec` references.
- [ ] Construct `RebuildPlan` with the desired tree + the full loaded snapshot.
- [ ] Write/update unit tests covering the shallow-match short-circuit vs. the "top matches but a child spec differs" non-short-circuit case.
- [ ] `dotnet build` + `dotnet test` green.

### Task 4: Recursive `PlanExecutor.Execute` with pruning and link filter

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `NtoLib/OpcTreeManager/Entities/OpcScadaItemDto.cs` (add pruning overload)

- [ ] In `OpcScadaItemDto`, add `ToScadaItemPruned(NodeSpec? spec)`. When `spec == null || spec.Children == null` delegate to existing `ToScadaItem()`. Otherwise construct the `OpcUaScadaItem` with a filtered child list; throw if any child named by `spec` is absent from the DTO's `Items`.
- [ ] Add a helper `LinkCollector.FilterByPrefix(IReadOnlyList<LinkEntry> links, string keptSubtreePath)` (or place it in a new `LinkFilter` static class if that reads cleaner — check by line count) returning only links whose `LocalPinPath` is equal to the subtree path or starts with `keptSubtreePath + "."`.
- [ ] Replace `Execute`'s current shrink + swap + expand block with a call to a new private `ApplyDesiredSpec(OpcUaScadaItem container, IReadOnlyList<NodeSpec> desired, string containerPath, IReadOnlyDictionary<string, NodeSnapshot> snapshot)` that:
  - Collects names to remove; for each, calls the existing `DisconnectNodeLinks(containerPath + "." + name)` then drops from `container.Items`.
  - Builds a fresh ordered `newItems` list following `desired` order; for each spec it either (a) keeps the existing item and recurses into it with `spec.Children`, or (b) constructs a new item from the snapshot subtree walked down the path from the group root, pruned by `spec`.
  - Collects `LinkEntry` reconnects for any newly constructed sub-tree, filtered by path prefix.
  - Swaps `container.Items` once at the end of its level (via the existing `SwapGroupItems` helper generalized to any container).
- [ ] After full recursion at the top level, `ResetScadaItemsMap`, `SynchWihSysTree`, and `ApplyChange` run once as today, then the collected cross-level `linksToReconnect` are wired via the existing `TryConnectLink` loop.
- [ ] Keep the existing `iconnect` vs no-arg-Connect distinction in `TryConnectLink`. No change to connection logic.
- [ ] Write unit tests for `ToScadaItemPruned`: leaf (delegates), non-leaf full-match, non-leaf partial (keeps only some children), missing child name (throws).
- [ ] Write unit tests for the link filter: simple subtree, mixed paths, leaf path with no children.
- [ ] `dotnet build` + `dotnet test` green.

### Task 5: Integration test for recursive planning end-to-end (if feasible)

**Files:**
- Create: `Tests/OpcTreeManager/Unit/NestedPlanTests.cs` (only if we can stand up a fake-container harness without vendor COM)

- [ ] Assess whether we can construct a minimal fake `OpcUaScadaItem` tree for testing `ApplyDesiredSpec` in isolation. If yes, write at least two scenarios: (a) `Valves: [VPG1, VPG2]` against a current Valves with `[VPG1, VPG4]` — expect VPG4 removed, VPG2 constructed pruned; (b) legacy flat spec against flat current — expect identical behavior to pre-refactor code.
- [ ] If not feasible (needs real vendor COM), skip this task and note "verified only via SCADA-host smoke run" in Post-Completion.
- [ ] `dotnet test` green.

### Task 6: Documentation — feature doc and readme

**Files:**
- Modify: `Docs/opc-tree-manager.md`
- Modify: `Docs/readme.md`

- [ ] Rewrite section 3.2 of `Docs/opc-tree-manager.md` with the new YAML syntax (leaf vs mapping with children), semantic rules (leaf = keep whole subtree, non-leaf = keep this node with only the listed children, recursively), and an example mirroring the issue #88 comment (MBE vs MBE2 vs PLASMA, with a nested `Valves` entry).
- [ ] Add a new short subsection (e.g., `3.3. Snapshot coverage requirement`) explaining that `tree.json` must be generated from a master-project state that contains the union of every node used across every target project, because nested specs can select sub-nodes that are only present in some projects.
- [ ] Update section 4 "Порядок работы Execute" — note that shrink/expand now recurses through nested specs, not only at the group top level. Mention that `BuildNewItems` preserved/newly-constructed log lines fire at each level where a change happens.
- [ ] Update the TL;DR at the top of `Docs/opc-tree-manager.md` to mention nested sub-selection (one sentence).
- [ ] In `Docs/readme.md`, refine the `OpcTreeManager` entry description to hint at the nested feature ("с произвольной глубиной вложенности" or similar). Don't expand into a full feature paragraph — readme is a TOC.

### Task 7: Final verification

**Files:**
- None (verification only).

- [ ] `dotnet build NtoLib.sln` — 0 errors.
- [ ] `dotnet test NtoLib.sln` — all pass.
- [ ] `cd .format && dotnet format ../NtoLib.sln --include ../NtoLib/OpcTreeManager/ ../Tests/OpcTreeManager/ --verify-no-changes` — clean.
- [ ] Re-read all modified files for extraneous edits.
- [ ] Move this plan to `Docs/plans/completed/`.

## Post-Completion

- **SCADA-host smoke test** required before merging. Scenarios:
  - Legacy flat `config.yaml` — verify identical behavior to pre-change.
  - `Valves: [VPG1, VPG2]` under current Valves with `[VPG1, VPG4, VPG5]` — verify VPG4 and VPG5 links are disconnected and items removed; VPG1 preserved; VPG2 constructed with links from snapshot.
  - Three-level nesting (e.g., `TemperatureControllers: [CH1: [Setpoint, Actual], CH2]`) — verify deep recursion works.
  - Confirm `ApplyChange` registers deep sub-pin changes with vavobj — watch for runtime Connect failures that would indicate unregistered new slots.
- **Snapshot regeneration** note: bundle a refreshed `DefaultConfig/OpcTreeManager/tree.json` only if the master-project covers every potential sub-node across every target project. If new sub-nodes are introduced later, snapshot must be refreshed and re-committed.
- **Risk**: deep `ApplyChange` behavior is unverified from code alone. If vavobj refuses to register pins added inside a preserved parent, we may need to force-construct the parent with the new child set rather than mutating its `Items` in place. Fall back noted here; investigate only if the smoke test hits it.
