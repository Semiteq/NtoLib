# OpcTreeManager: resolve OpcFbPath exactly

Queued behind `20260921-opctreemanager-log-as-technical-record.md`. Branch from that branch's tip,
or from `master` after both merge; the two edit the same files.

## Overview

`OpcProtocolAccessor.GetProtocol` accepts a wrong `OpcFbPath`. It walks up the tree from the
configured path, trimming one segment at a time until it finds an `OpcUaClientHostObject`, then
returns the protocol and discards the path that actually matched. Everything downstream keeps using
the configured path, so a path one segment too deep resolves a protocol and then builds every
absolute path wrong.

The walk forgives exactly one operator mistake, pointing below the OPC FB node, and that mistake is
the one it converts into a wrong-prefix run. Pointing above the node it cannot help with at all,
because it only goes up. The forgiveness is undocumented, unused and costly.

The logging work that precedes this plan made the consequences loud, but it cannot stop the run: the
operator learns from the file, afterwards, that the destructive half already happened.

Resolving the path exactly closes this entrance: the scan fails, nothing mutates, no snapshot is
written. It does not make every wrong path impossible — a path naming a different OPC UA FB node in
the same project still resolves, and nothing here can tell that apart from the intended one.

## Context (from discovery)

Measured against the current tip of `opctreemanager-log-as-technical-record`.

- `OpcProtocolAccessor.cs:20-37`: `GetProtocol` loops on `searchPath`, calling
  `project.SafeItem<ITreeItemHlp>` at `:26`, trimming after the last dot at `:33-34`, and failing at
  `:37` only when the walk exhausts every ancestor. `ResolveProtocol` returns the protocol alone;
  the matched `searchPath` is dropped.
- Consumers of the unvalidated path, all of which receive the property value rather than anything
  the walk resolved:
  - `PlanExecutor.cs:76` builds `groupPath` as `plan.OpcFbPath + "." + groupRelativePath`
  - `PlanExecutor.cs:112` looks the FB item up by `plan.OpcFbPath` for `ApplyChange`, failing at
    `:115` after the reshape has already run
  - `OpcTreeManagerService.JoinPath` (`:304-306`) prefixes every node path in `BuildSnapshot`
    (`:153`)
- What a path one segment too deep does today, after the logging work:
  - `ScanAndValidate` passes, because it resolves through the same walk
  - on rebuild, every `DisconnectSubtree` reports the node missing from the project while `Items` is
    still swapped, and the `ApplyChange` lookup then fails after the mutation
  - on `ExecuteSnapshot`, the group is walked correctly but each node's links are looked up under
    the wrong prefix, so every node logs the Error at `OpcTreeManagerService.cs:158-161` and the
    snapshot is written with every node present and no links
- `TreeSnapshotWriter.Write` refuses a snapshot with zero **nodes**. A wrong prefix produces the
  full node set with zero **links**, so the guard does not fire and the reference file is
  overwritten.
- The property is documented as the node itself: `OpcTreeManagerFB.cs:45` reads "Полный путь к узлу
  OPC UA FB в дереве проекта (например, Система.АРМ.OPC UA Siemens)", and the shipped default
  `:46` is that node.
- The walk has been in the module since its first commit, `f616463`. No document explains it, and no
  caller depends on the forgiveness.
- `GetProtocol` has never been tested, because it needs `IProjectHlp`. Three `Func<string, T?>`
  resolution seams now exist on this branch and are house style: `PlanExecutor.cs:213`,
  `ProjectSubtreeDisconnector.cs:42`, `TreeReshaper.cs:35`.

ASSUMPTION: no deployed project points `OpcFbPath` below the OPC FB node. The shipped default is the
node, and the property is typed in by hand against the project tree. A project that did point deeper
has been running on a wrong prefix all along and will now refuse loudly, which is the intent.

## Development Approach

- **testing approach**: regular, code first, tests in the same task
- run `dotnet format NtoLib.sln` after each code change
- keep this plan in sync when scope changes

## Testing Strategy

`GetProtocol` becomes testable the same way the rest of the module did: its `IProjectHlp` parameter
is **replaced** by a `Func<string, ITreeItemHlp?>`, not joined by one, so the method keeps exactly
one way to resolve a path. The two call sites (`OpcTreeManagerService.cs:297`, `PlanExecutor.cs:60`)
pass `path => _project.SafeItem<ITreeItemHlp>(path)`.
`Tests/OpcTreeManager/Unit/OpcProtocolAccessorTests.cs` already exists and builds vendor objects
directly, so the new cases join it.

The seam earns its place only as the regression pin for the deletion, so the pin has to discriminate.
A test where nothing resolves, or where the item is not an OPC FB node, passes on today's code too:
the walk trims to the root, finds no host object and fails. Those two cases pin nothing.

The one that discriminates: a resolver that returns `null` for the configured too-deep path and an
item whose `FBObject` is an `OpcUaClientHostObject` for its parent. Any walk climbs, finds the host
object and enters `ResolveProtocol`; exact resolution never gets there and fails with
`OpcFbPath must name the OPC UA FB node itself`. Asserting **that sentence** is the pin, because
only the exact-resolution branch can emit it. Asserting which path the message names is not: a walk
that passes `opcFbPath` rather than the matched ancestor into `ResolveProtocol` — the shape the
method has after this change, so the natural thing for a later editor to write — fails on the null
`Instance` with a message naming the configured path, satisfying a path assertion while the
forgiveness is fully back.

Measured against `Resources/`, and confirmed by a passing test run: `OpcUaClientHostObject` has a
public parameterless constructor; `MasterSCADA.Hlp.ITreeItemHlp` is a non-sealed public class whose
public constructor accepts a null wrapped item (the base constructor only casts it to
`INumProperties`), and `FBObject` is `public override object` on it, not sealed, so it can be
overridden without the base getter running. A full success cannot be built: `ResolveProtocol` needs
`Instance` to be an `OpcUaClientInstance` whose non-virtual `OpcUaProtocol` getter returns an
`OpcUaProtocol`, and constructing that instance against `Resources/` fails on a missing assembly.

If the discriminating construction turns out not to work either, drop the seam and ship the deletion
with no unit test. A seam whose only tests pass on the code it was meant to pin is dead surface.

## Acceptance Evidence

```powershell
dotnet test NtoLib.sln --filter "FullyQualifiedName~OpcProtocolAccessorTests"
```

- `GetProtocol_PathBelowTheFbNode_Fails`: a path one segment deeper than the OPC FB node returns a
  failed `Result` carrying the exact-resolution refusal and naming the configured path. On today's
  code it succeeds and every downstream path is built wrong.
- `GetProtocol_ItemAtThePathIsNotAnOpcFbNode_Fails`: the other arm of the node test — an item that
  exists at the exact path but carries no `OpcUaClientHostObject`, which is the production shape of
  the fault (`...OPC UA Siemens.ServerInterfaces` is a real item with a null `FBObject`).
- `GetProtocol_PathAtTheFbNode_PassesTheNodeTest`: the documented path reaches `ResolveProtocol` and
  fails there on the null `Instance`. It shipped under this name rather than `..._Succeeds` because a
  successful `ResolveProtocol` needs a live `OpcUaClientInstance` and its non-virtual protocol
  getter; the full success is covered by the host run below. Without it, a `GetProtocol` that failed
  unconditionally would pass both negative cases.

In the host, on a project copy: set `OpcFbPath` one segment too deep and raise `Execute`. The scan
must fail immediately, naming the configured path, with no `Reshaped` line, no `Link not issued`
line and no terminal rebuild verdict in the log. Then raise `ExecuteSnapshot` with the same wrong
path and confirm `tree.json` is unchanged.

Before the change, the same two presses produce a full rebuild against a wrong prefix and a
`tree.json` holding every node with no links.

## Progress Tracking

- mark completed items `[x]` when done
- new tasks get a ➕ prefix, blockers a ⚠️ prefix

## Solution Overview

Resolve the configured path exactly. If the item at `OpcFbPath` is not an OPC FB node, fail.

**The walk goes entirely, including as a diagnostic.** Naming the nearest OPC FB ancestor in the
failure message was considered and rejected: the operator types this path into a string field in the
FB's settings, against the project tree in front of them, so there is nothing for the message to
help them find. Keeping the loop only to build that sentence would also leave the deleted mechanism
sitting in the failure path, one line away from being restored by whoever reads "we already found
the node here".

**Nothing downstream needs the resolved path threaded through it.** Once resolution is exact, the
configured path *is* the resolved path, so `groupPath`, `JoinPath` and the `ApplyChange` lookup are
already correct by construction. That is the whole reason this is a small change rather than a
refactor of four call sites.

**Rejected: hardening `TreeSnapshotWriter` against a zero-link snapshot.** It looks like the
matching guard, and it is not. A node with no consumers legitimately has no links, so the writer
would refuse healthy captures. With exact resolution the wrong-prefix cause cannot reach the writer
at all, and the per-node Error at `OpcTreeManagerService.cs:157-161` remains the signal for every
other cause.

**Rejected: returning the matched path from `GetProtocol` and threading it downstream.** That keeps
the forgiveness and pays for it in four places, to support a usage nobody documented or asked for.

## Technical Details

The loop disappears: one lookup of `opcFbPath`, one type test, return the protocol or fail. Ten
lines become three, and there is no ancestor state left in the method to be tempted by.

`ResolveProtocol`'s three existing failure messages (`Instance is null`, wrong instance type, wrong
protocol type) stay as they are; they describe a node that is an OPC FB but is not usable, which is
a different fault from the one this plan addresses. Its `resolvedPath` parameter becomes a leftover
of the walk, since it can now only ever be the configured path: pass the configured path and name
the parameter for what it is.

## What Goes Where

- **Implementation Steps**: code, tests and documentation in this repository
- **Post-Completion**: the host check

## Implementation Steps

### Task 1: Resolve the configured path exactly

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/OpcProtocolAccessor.cs`
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/OpcProtocolAccessorTests.cs`

- [x] add a `Func<string, ITreeItemHlp?>` resolution parameter to `GetProtocol`, mirroring
      `ProjectSubtreeDisconnector.cs:42`; the two production call sites
      (`OpcTreeManagerService.cs:297`, `PlanExecutor.cs:60`) pass
      `path => _project.SafeItem<ITreeItemHlp>(path)`
- [x] replace the ancestor loop with a single lookup: accept the configured path only when the item
      at that exact path has an `OpcUaClientHostObject` as its `FBObject`. The `is ITreeObjectHlp`
      test the old loop carried is vacuous once the seam is typed `ITreeItemHlp?`, which derives from
      it; `FBObject` is the only discriminator left
- [x] fail naming the configured path, with no ancestor search and no suggestion; the operator typed
      this value into a string field and does not need it looked up for them
- [x] write `GetProtocol_PathBelowTheFbNode_Fails`, `GetProtocol_PathAtTheFbNode_Succeeds` and
      `GetProtocol_PathResolvesToNothing_Fails`; if the vendor host object cannot be constructed in
      the Tests project, say so in the plan and ship the two negative cases — the host object
      constructs fine, so all three shipped. The second shipped as
      `GetProtocol_PathAtTheFbNode_PassesTheNodeTest`, since it stops at the null `Instance`; the
      third as `GetProtocol_ItemAtThePathIsNotAnOpcFbNode_Fails`, because a resolver returning
      `null` for every path enters the same branch as the first test and covers nothing extra
- [x] run `dotnet format NtoLib.sln` and `dotnet test NtoLib.sln --filter "FullyQualifiedName~OpcTreeManager"`

### Task 2: Say so in the operator documentation

**Files:**
- Modify: `NtoLib/OpcTreeManager/OpcTreeManagerFB.cs`
- Modify: `Docs/opc-tree-manager.md`

- [x] extend the `OpcFbPath` property description at `OpcTreeManagerFB.cs:45` to say the path must
      name the OPC UA FB node itself and that a deeper path is refused
- [x] add the refusal to the validation-errors section, with the message shape so an operator can
      recognise it; the section now also separates it from the three `ResolveProtocol` failures and
      says a different OPC UA FB node in the same project still resolves
- [x] state in the `ExecuteSnapshot` section that a refused scan writes nothing, so a wrong path can
      no longer overwrite `tree.json`

### Task 3: Verify acceptance criteria

- [x] run the full suite: `dotnet test NtoLib.sln` — 373 passed, 0 failed
- [x] run `dotnet format NtoLib.sln` — exit 0, no file rewritten. `--verify-no-changes` exits 2, on
      an IDE1006 in `Tests/MbeTable/Infrastructure/EditorRuntimeOptionsProviderTests.cs` that
      predates this branch and that `NamingStyleCodeFixProvider` cannot fix solution-wide
- [x] confirm every assertion in Acceptance Evidence exists under its shipped name and passes — all
      three run green one at a time. What the pin discriminates: `GetProtocol_PathBelowTheFbNode_Fails`
      asserts the refusal sentence `must name the OPC UA FB node itself`, which only the
      exact-resolution branch emits, so it rejects every reconstruction of the ancestor walk rather
      than one of them. The earlier record claimed the pin on the path the message names; that
      assertion alone survives a walk that passes `opcFbPath` into `ResolveProtocol`, which is the
      shape the method now has
- [x] reread the comments added by Tasks 1-2: English, ASCII, one paragraph, one line on internal
      and private members — all clean. `terse` over the five touched files exits 1, but every
      finding predates this branch (`PlanExecutor` doc-long and em-dashes) or is the Russian
      `[Description]` text MasterSCADA shows the operator; on the three files whose English
      comments changed it exits 0
- [x] update the class summary of `Tests/OpcTreeManager/Unit/OpcProtocolAccessorTests.cs:19-22`,
      which scopes the file to `FindGroup` and no longer covers what it holds

## Post-Completion

**Manual verification:**

- the two-press host check in Acceptance Evidence, on a project copy

**Deployed projects:**

- a project whose `OpcFbPath` points below the OPC FB node now refuses instead of running on a wrong
  prefix. None is known to; the default is the node

**Executed by exec:**

- branch: `opctreemanager-opcfbpath-resolution`, the third of a stack on
  `opctreemanager-log-as-technical-record`, itself on `opctreemanager-empty-group-resolution`.
  Deliver the two below it first; every review in this run diffed against the parent, not `master`
- `3ea107d chore(repo): normalise path casing` sits at the base of this branch and belongs to no
  plan. It is the operator's own edit, picked up from the index before the run
