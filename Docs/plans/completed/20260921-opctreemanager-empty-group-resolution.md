# OpcTreeManager — reach an empty target group

## Overview

`Execute` fails with `OPC group 'MBE' not found in ScadaRootNode.` whenever the target group has
no children, which is exactly the state a clean migration from a reference snapshot starts from.
The group is present in the project tree; the search rejects it because the match predicate asks
whether the node currently holds children instead of whether it is a container.

This plan makes the group reachable when empty, stops the rebuild over an empty group from
reporting success while restoring nothing, stops `ExecuteSnapshot` from destroying the reference
snapshot in that same state, and leaves a diagnostic that names the real tree when a lookup fails.

The guard covers a strictly empty container only. A container that holds names outside the desired
set is shrunk to empty, constructs nothing and still reports success; that state is out of scope
because it has a different cause — a `config.yaml` pointed at the wrong project, not a migration.

Scope is `NtoLib/OpcTreeManager` plus its tests and docs. The FB's designer properties keep their
current shape: no path-form `GroupName`, no ambiguity guard over duplicate names.

## Context (from discovery)

Files involved:

- `NtoLib/OpcTreeManager/TreeOperations/OpcProtocolAccessor.cs` — group lookup, line 52 is the defect
- `NtoLib/OpcTreeManager/Config/TreeSnapshotWriter.cs` — the only writer of `tree.json`
- `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs` — scan, snapshot capture, logging
- `NtoLib/OpcTreeManager/Facade/PlanBuilder.cs` — plan construction and its resolvability guard
- `NtoLib/OpcTreeManager/TreeOperations/TreeReshaper.cs` — the skip-with-warning branch, line 105-111
- `Tests/OpcTreeManager/Unit/` and `Tests/OpcTreeManager/Integration/ApplyDesiredSpecTests.cs` — the
  existing tiers; `Tests.csproj` uses default compile globs, so new test files need no csproj edit
  (unlike `NtoLib.csproj`, which lists every `.cs` explicitly at `NtoLib.csproj:250-274`)

Vendor facts, from decompiling `Resources/OpcUaClient.dll` (`ilspycmd Resources/OpcUaClient.dll -r
Resources/ -o <dir> -p --nested-directories`):

- `OpcUaScadaItem.IsGroup => Items != null && Items.Count > 0` — current contents, not node type
- `OpcUaScadaItem.IsNode` is a settable bool the vendor fills through constructors only: the 3-argument
  `(name, id, nodeId)` ctor leaves it `false` and builds folders; the 9-argument ctor takes
  `bool isChannel = true` and builds leaf channels
- `OpcUaProtocol.SyncNewElementWithSerializableObjectRec` picks the 3-argument ctor under
  `!isLeaf || (valueType == null && AllChilds.Count != 0)`. The `!isLeaf` arm covers every node reached
  as a parent of a checked node; the second arm covers a directly checked node that has a browse
  subtree. A folder whose children are cut off at `MaxBrowseLevel`, or listed in
  `BrowseIgnoreChildsInNodeNames`, satisfies neither and is born with `IsNode == true`, later gaining
  children — the "variable as group" case
- `OpcUaProtocol.FromSerializedToScadaTree` branches on `IsNode`: a `!IsNode` item becomes
  `AddGroup(item.Id, item.Name)` even with zero children; an `IsNode` item with children becomes a
  group at id `10000000 + item.Id` plus a pin of its own through `AddVarToScadaTree`; an `IsNode` leaf
  becomes a pin, and a folder-shaped one becomes the pin pair `MBE` / `MBE$`
- `OpcUaProtocol.DeleteFromProjectRec` drops an emptied folder whenever `!IsNode`, regardless of the
  folder's own checkbox; the checkbox branch applies only to a folder that was already empty before the
  pass. The surviving empty folder on the operator's machine was therefore created fresh by a later
  Apply, not left behind by the one that emptied it
- `OpcUaScadaItem` is `[Serializable]` and lives inside the saved MasterSCADA project, so an `IsNode`
  value can outlive the vendor build that wrote it

ASSUMPTION: the live `MBE` is not an empty item carrying `IsNode == true`. The predicate in Task 1
accepts both shapes a container can take — a folder, and a variable-as-group that holds children — so
the empty `IsNode == true` shape is the only one left out of reach. For a folder-shaped browse node
`AccessLevel & PinType.PinPout` is zero, `AddPinToTree` takes its `default:` arm, and
`FromSerializedToScadaTree` renders the item as the pin pair `MBE` / `MBE$` rather than as a node. The
project tree shows a single `MBE` node under `ServerInterfaces` with no `MBE$` sibling, and a `$`
sibling is a separate tree object (`Docs/architecture/masterscada-fb-primer.md:150-162`). What
falsifies it: the lookup keeps failing and the Task 3 dump prints `IsNode` for every node. A successful
lookup narrows nothing further, because the predicate does not record which arm matched.

Measured 2026-09-21: `DefaultConfig/OpcTreeManager/config.yaml` lists 23 node names under project
`MBE`; `DefaultConfig/OpcTreeManager/tree.json` holds 24 keys and covers all 23. The extra key is
`MBE_Type`, which no task removes — the rebuild simply never constructs it, because the desired tree
comes from `config.yaml`.

## Development Approach

- **testing approach**: regular — code first, tests in the same task
- complete each task fully before moving to the next
- every task carries its own tests; all tests pass before the next task starts
- run `dotnet format NtoLib.sln` after each code change
- keep this plan in sync when scope changes

## Testing Strategy

- **unit tests**: required per task, in `Tests/OpcTreeManager/Unit/`
- **integration tests**: the COM-free rebuild core through
  `Tests/OpcTreeManager/Integration/ApplyDesiredSpecTests.cs` and its `FakeSubtreeDisconnector`
- **e2e tests**: none in this project
- `OpcProtocolAccessor.FindGroup` is `internal` and `NtoLib` carries `InternalsVisibleTo("Tests")`, so
  tests call it directly. The vendor protocol is constructed as `new OpcUaProtocol(null!)` with
  `ScadaRootNode` assigned: the constructor only stores the instance and `FindGroup` reads
  `ScadaRootNode` alone, so no production surface is widened for the tests
- `OpcTreeManagerService` is not unit-testable and no test constructs it: reaching the protocol runs
  `SafeItem<ITreeItemHlp>` → `ITreeObjectHlp.FBObject` → `OpcUaClientHostObject.Instance`, which only
  the FB host supplies. Guards that must be tested therefore live in the COM-free helpers the service
  calls, not in the service
- `Tests.csproj` already references `OpcUaClient.dll` and `Opc.Ua.Core.dll` (`Tests.csproj:45-50`), so
  an `OpcUaScadaItem` tree is built in-process

## Acceptance Evidence

Automated, the defect itself:

```powershell
dotnet test NtoLib.sln --filter "FullyQualifiedName~OpcProtocolAccessorTests"
```

`FindGroup_GroupWithoutChildren_IsFound` builds `root → ServerInterfaces → MBE` where `MBE.Items` is
empty and `MBE.IsNode` is false, and asserts `result.IsSuccess` with
`result.Value.RelativePath == "ServerInterfaces.MBE"`. On today's code it fails with
`OPC group 'MBE' not found in ScadaRootNode.`

Automated, the outcome the operator needs — an empty container rebuilt into the full node set:

```powershell
dotnet test NtoLib.sln --filter "FullyQualifiedName~ApplyDesiredSpecTests"
```

`ApplyDesiredSpec_EmptyContainer_ConstructsEveryDesiredNodeWithItsLinks` starts from a container with
zero `Items` and a snapshot holding every desired name plus one extra, and asserts that the
constructions count equals the desired count, that the extra name is absent from the rebuilt
container, and that the constructions carry the desired node's link and not the dropped node's.
`ShrinkCount` is not asserted: over a zero-item container it is trivially zero.

`TreeReshaper` itself is untouched by this change, so that test alone passes identically on master.
`PlannedRebuild_EmptyContainer_ReshapesFromTheBuiltPlan` covers the seam that does change: it feeds
the plan `PlanBuilder.Build` produced into `TreeReshaper.Reshape` and asserts the reshape constructs,
which is what makes a passing guard mean a real rebuild.

Automated, the guards:

```powershell
dotnet test NtoLib.sln --filter "FullyQualifiedName~OpcTreeManager"
```

- `Build_EmptyContainerAndNoDesiredNameInSnapshot_Fails` asserts `Result.IsFailed`
- `Write_EmptySnapshot_LeavesExistingFileUntouched` asserts the target file is byte-identical and the
  returned `Result` is failed

Manual smoke in the MasterSCADA host, because the connect pass has no headless path:

1. In the OPC UA FB settings, uncheck every child of `MBE` and press OK — `DeleteFromProjectRec`
   drops the emptied folder regardless of its own checkbox, so `MBE` disappears. Reopen the settings,
   check `MBE` alone and press OK again; the second Apply recreates it empty. Confirm `MBE` is
   present and empty in the project tree
2. Enter runtime, raise `Execute`, confirm `IsPending` goes high and `Failed` stays low
3. Read the log: the scan line names the resolved group path `ServerInterfaces.MBE`
4. Leave runtime, wait for the deferred pass, confirm the log reports
   `expand=23 connects issued=<n> threw=0`
5. Confirm the 23 nodes are back under `MBE` in the project tree with their links intact
6. With `MBE` still empty, raise `ExecuteSnapshot` and confirm `tree.json` is unchanged and `Failed`
   goes high

## Progress Tracking

- mark completed items `[x]` when done
- new tasks get a ➕ prefix, blockers a ⚠️ prefix
- if the Task 3 dump contradicts the assumption in Context, stop and revise the plan rather than
  widening the predicate further

## Solution Overview

Identity comes from the node's type and its position, never from its current contents.

The predicate becomes `!child.IsNode || child.IsGroup` — everything that is not an empty leaf pin.
This is a strict superset of today's behaviour: every node the old predicate accepted is still
accepted, including the variable-as-group case where `IsNode` is true and children are present, and
the empty folder is added. The one shape it still rejects is an empty `IsNode == true` item, which
`FromSerializedToScadaTree` renders as a pin pair and which can therefore never be a container.

Reaching the empty group opens a hole of its own and exposes two more, so three guards follow it:

- `BuildSnapshot` over an empty group produces an empty dictionary, and `TreeSnapshotWriter.Write`
  would serialise `{}` over the reference file. Today the lookup failure is the only thing preventing
  that, so the writer refuses an empty snapshot — and it does so immediately after the predicate
  change, not later
- a desired node missing from the snapshot is a permanent hole rather than a preserved node, so the
  plan refuses when nothing at all resolves and reports every miss as an error
- a failed lookup prints the actual `ScadaRootNode` shape, which is what falsifies the `IsNode`
  assumption on the operator's machine

Rejected, with reasons:

- **path-form `GroupName`** (`ServerInterfaces.MBE`): solves a problem that does not exist here.
  Siblings cannot share a name — `SyncNewElementWithSerializableObjectRec` merges by
  `Items.Find(x => x.Name == ...)` — and every link in `tree.json` sits under
  `OPC UA Siemens.ServerInterfaces.MBE`. Cost: every deployed project must have its FB property
  edited
- **ambiguity guard** (collect all matches, fail on more than one): a hard stop the operator cannot
  resolve, because there is no property to disambiguate with. Logging the resolved path gives the same
  information at a fraction of the cost. Known gap this leaves open: the widened predicate accepts an
  empty folder, so an empty folder named `MBE` met earlier in the pre-order walk now shadows the
  populated one, which the old predicate could not do. The "siblings cannot share a name" argument
  does not cover it — the two nodes sit under different parents. The resolved-path log line is what
  exposes it, and the fix if it ever happens is a path-form `GroupName`, not a guard
- **creating the group when absent**: the reported state has the group present. Recreating it needs an
  `Id` the snapshot does not carry and a parent path the FB does not know

## Technical Details

`FindGroupRecursive` keeps its pre-order walk: each child is tested, then descended into, then the
next sibling. Relative paths and the returned tuple shape do not change.

`TreeSnapshotWriter.Write` refuses a snapshot with zero entries before it touches the file. The rule
belongs to the writer rather than to the service: it is the only writer of `tree.json`, it is
COM-free, and a zero-node snapshot is never a legitimate reference state. The service adds the group
name when it reports the failure.

The tree dump is a pure string builder over `OpcUaScadaItem`, rendering per node the name, `IsNode`,
and child count, indented by depth, cut off at a `const` depth of 3 and a `const` breadth of 20, with
a marker wherever a cutoff hides nodes. A browse tree holds thousands of nodes within three levels,
so the breadth cap is what keeps the entry loggable. It lives in `OpcProtocolAccessor.cs` next to the
search it explains, so `NtoLib.csproj` needs no new `<Compile Include>` entry.

`FindGroup` folds the dump into its own failure message rather than leaving each caller to log it.
`PlanExecutor.Execute` calls `FindGroup` directly on the deferred pass and returns the bare message
to `DeferredExecutor`, so a dump logged only in the service would never print on that path.

`PlanBuilder.Build` already receives both the snapshot and `currentTopLevelNames`
(`PlanBuilder.cs:36-43`). The new guard runs only when `currentTopLevelNames` is empty: it counts
desired top-level names present in the snapshot, fails when that count is zero, and logs each missing
name at error level when the count is partial. A non-empty container keeps today's behaviour, where a
missing name means "preserve whatever is there" and stays a warning in `TreeReshaper.cs:107-109`.

Zero fails and partial proceeds because the two states have different causes and different remedies.
Zero means the snapshot does not describe this group at all — a wrong path, a truncated file, a
snapshot from another installation — and nothing useful can come of the pass. Partial means
`config.yaml` moved ahead of `tree.json`, which is an ordinary state during commissioning; refusing
the whole rebuild over one name leaves the machine empty, while restoring 22 of 23 leaves an error in
the log that a second `Execute` closes once the files are reconciled. This differs from the existing
nested guard (`PlanBuilder.cs:92-98`), which refuses because `ToScadaItemPruned` throws mid-rebuild
after subtrees are already disconnected; a missing top-level name throws nothing and skips cleanly.

## What Goes Where

- **Implementation Steps**: code, tests, docs inside this repository
- **Post-Completion**: the MasterSCADA host run, which no test can stand in for

## Implementation Steps

### Task 1: Match the target group by type, not by contents

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/OpcProtocolAccessor.cs`
- Create: `Tests/OpcTreeManager/Unit/OpcProtocolAccessorTests.cs`

- [x] replace the predicate at `OpcProtocolAccessor.cs:52` with `!child.IsNode || child.IsGroup`
- [x] state in the method's summary why contents cannot carry identity, pointing at
      `Docs/known_issues/15-derived-properties-as-identity.md` rather than restating it
- [x] add a test helper that builds an `OpcUaScadaItem` tree and an `OpcUaProtocol` over it
- [x] write `FindGroup_GroupWithoutChildren_IsFound` — empty folder at `ServerInterfaces.MBE`, asserts
      success and the relative path
- [x] write `FindGroup_PopulatedGroup_IsFound` and `FindGroup_VariableAsGroup_IsFound` (`IsNode` true
      with children), covering what the old predicate accepted
- [x] write `FindGroup_LeafPinWithMatchingName_IsNotMatched` and `FindGroup_NoMatch_Fails`
- [x] run `dotnet format NtoLib.sln` and `dotnet test NtoLib.sln --filter "FullyQualifiedName~OpcTreeManager"`

### Task 2: Refuse to write an empty snapshot over the reference file

Task 1 makes `ExecuteSnapshot` reach an empty group, so from here until this guard lands one press
serialises `{}` over a 24-key `tree.json`. This task closes a hole Task 1 opens, which is why it comes
directly after it.

**Files:**
- Modify: `NtoLib/OpcTreeManager/Config/TreeSnapshotWriter.cs`
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`
- Create: `Tests/OpcTreeManager/Unit/TreeSnapshotWriterTests.cs`

- [x] in `TreeSnapshotWriter.Write`, fail on a snapshot with zero entries before the file is opened,
      with a message naming the target path
- [x] in `CaptureAndWriteSnapshot` (`OpcTreeManagerService.cs:167-183`), add the group name to the
      reported failure so the log says which group was empty
- [x] write `Write_EmptySnapshot_LeavesExistingFileUntouched` — existing file byte-identical, result
      failed
- [x] write `Write_EmptySnapshot_CreatesNoFile` for the path where no file exists yet
- [x] write `Write_NonEmptySnapshot_RoundTripsThroughTheLoader` pinning the unchanged path
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 3: Print the real tree when the lookup fails

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/OpcProtocolAccessor.cs`
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/OpcProtocolAccessorTests.cs`

- [x] add `internal static string DescribeTree(OpcUaScadaItem root)` to `OpcProtocolAccessor.cs` —
      name, `IsNode`, child count per node, indented, cut off at a `const` depth of 3 and a `const`
      breadth of 20, with a marker wherever a cutoff hides nodes
- [x] fold the description into `FindGroup`'s failure message so every caller reports it, the
      deferred `PlanExecutor.Execute` path included
- [x] add the resolved relative path to the scan log in `ScanAndValidate`
      (`OpcTreeManagerService.cs:62-64`, after the `ResolveGroup` call at `:66`) so it is visible while
      `Cancel` still works
- [x] add the resolved relative path to the execution log line in `PlanExecutor.Execute`
- [x] write tests for `DescribeTree`: nesting and indentation, the depth cutoff and its marker, an
      empty root
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 4: Refuse a plan that would restore nothing

**Files:**
- Modify: `NtoLib/OpcTreeManager/Facade/PlanBuilder.cs`
- Modify: `Tests/OpcTreeManager/Unit/PlanBuilderValidationTests.cs`
- Modify: `Tests/OpcTreeManager/Integration/ApplyDesiredSpecTests.cs`

- [x] in `PlanBuilder.Build`, after the existing resolvability guard (`PlanBuilder.cs:92-98`), add the
      empty-container branch: count desired top-level names whose snapshot entry carries a non-null
      `ScadaItem`, which is what the executor resolves through — a key alone restores nothing
- [x] fail with a message naming the group and the target project when that count is zero
- [x] log each desired name absent from the snapshot at error level when the count is partial, naming
      the node, and keep building the plan
- [x] leave the non-empty-container path untouched
- [x] write `Build_EmptyContainerAndNoDesiredNameInSnapshot_Fails`
- [x] write `Build_EmptyContainerAndSnapshotEntriesWithoutScadaItem_Fails` — keyed entries whose
      `ScadaItem` is null
- [x] write `Build_EmptyContainerAndPartialSnapshot_BuildsPlanAndLogsEveryMiss` asserting the plan
      still carries every desired node and one Error event per unrestorable name
- [x] write `Build_NonEmptyContainerAndMissingSnapshotNode_BuildsPlan` pinning the unchanged path
- [x] write `ApplyDesiredSpec_EmptyContainer_ConstructsEveryDesiredNodeWithItsLinks` in
      `ApplyDesiredSpecTests` — zero-item container, snapshot with every desired name plus one extra;
      asserts the constructions count, that the extra name is not constructed, and that the
      constructions carry the desired links only
- [x] write `PlannedRebuild_EmptyContainer_ReshapesFromTheBuiltPlan` — the builder-to-reshaper seam
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 5: Verify acceptance criteria

- [x] confirm every requirement in the Overview is implemented
- [x] confirm `FindGroup_GroupWithoutChildren_IsFound` fails on `git stash` of Task 1 and passes with it
      (proved by reverting only the predicate expression in the working copy, because the pre-fix file
      lacks `DescribeTree` and would not build: `Expected result.IsSuccess to be True because an empty
      folder is still a container, but found False.`)
- [x] run the full suite: `dotnet test NtoLib.sln`
- [x] run `dotnet format NtoLib.sln` and confirm a clean tree
- [x] reread the comments added in Tasks 1-4: English, one paragraph, no restating of what the code
      already says

### Task 6: Update documentation

**Files:**
- Modify: `Docs/known_issues/15-derived-properties-as-identity.md`
- Modify: `Docs/opc-tree-manager.md`
- Modify: `Docs/plans/20260921-opctreemanager-empty-group-resolution.md`

- [x] in `known_issues/15`, replace the `IsGroup` status line with the shipped predicate and the reason
      it is a superset; keep the `IsHistorySupport` half unchanged
- [x] reconcile rule 1 of that file with the shipped code: the `IsGroup` arm survives only to keep the
      variable-as-group shape reachable, and identity still comes from name and position
- [x] record in `known_issues/15` that `DeleteFromProjectRec` drops an emptied folder regardless of its
      own checkbox, so the surviving empty folder comes from a later Apply
- [x] in `Docs/opc-tree-manager.md` section 3.4, delete the "target group must hold at least one node"
      limitation and its workaround, correct the opening count from four limitations to three, and
      state the two new refusals: no plan when nothing resolves, no snapshot write over an empty group
- [x] confirm the remaining three limitations in 3.4 still hold
- [x] leave this plan in `Docs/plans/`; the move to `Docs/plans/completed/` happens at delivery,
      after the review phases that still read it from here

## Post-Completion

**Manual verification:**

- the six-step host smoke in Acceptance Evidence, on a project copy, not the production one
- if step 2 still fails and the dump prints `MBE` with `IsNode == true` and no children, the
  assumption in Context is wrong: the node is a pin pair, the rebuild cannot hang children under it,
  and the group has to be recreated as a folder — a different plan, not a wider predicate

**External system updates:**

- none: the FB's designer properties, `config.yaml` and the `tree.json` format are unchanged, so
  deployed projects need no edit

**Executed by exec:**

- branch: `opctreemanager-empty-group-resolution`

## Verify it yourself

Automated, and it fails before the fix:

```powershell
dotnet test NtoLib.sln --filter "FullyQualifiedName~FindGroup_GroupWithoutChildren_IsFound"
```

Passes at `f175d3a` and later. To watch it fail, change the predicate in
`NtoLib/OpcTreeManager/TreeOperations/OpcProtocolAccessor.cs` back to `var isContainer =
child.IsGroup;` and run the same command: it reports
`Expected result.IsSuccess to be True because an empty folder is still a container, but found False`.
A whole-file checkout of the pre-fix version does not work, because that version has no
`DescribeTree` and the build fails before the assertion is reached.

The rest of the suite:

```powershell
dotnet test NtoLib.sln
```

334 tests, 0 failures.

In the MasterSCADA host, where the connect pass has no headless path, run the six-step smoke in
Acceptance Evidence on a copy of the project. The two observations that decide it: step 2 must leave
`Failed` low where today it goes high with `OPC group 'MBE' not found in ScadaRootNode.`, and step 4
must report `expand=23` where today nothing runs at all.

Read the log afterwards. Two lines are new: `Group 'MBE' resolved at 'ServerInterfaces.MBE'` on both
the scan and the deferred pass, and, only when a lookup fails, the `ScadaRootNode` shape with
`IsNode` and the child count per node. That dump is what settles the assumption in Context — if it
ever prints `MBE` with `IsNode == true` and no children, the node is a pin pair and this fix does not
apply to that project.
