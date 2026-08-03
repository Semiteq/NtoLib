# OpcTreeManager — cleanup and architecture refactor

## Overview

Consolidate the OpcTreeManager module after the iconnect fix landed: close one latent runtime
hazard, separate the COM-free core from the COM shell, remove scaffolding-era scar tissue, cut the
comment overgrowth, and rebalance the domain docs. Scope is **`NtoLib/OpcTreeManager` only** — no
LinkSwitcher, no cross-module shared helpers (the shared deferred-lifecycle idea is deliberately out
of scope; it is a both-stacks decision).

Grounded findings this addresses:

- **Mid-mutation abort (real bug).** A desired spec child absent from the snapshot makes
  `OpcScadaItemDto.ToScadaItemPruned` **throw** (`OpcScadaItemDto.cs:110-112`) *during* rebuild —
  after `ApplyDesiredSpec` already live-disconnected removed subtrees (`PlanExecutor.cs:220-227`).
  The tree is left half-rebuilt, the error only in the log. The same condition is handled two ways:
  a top-level missing node is Warning+skip (`PlanExecutor.cs:260-266`), a nested one aborts the whole
  plan. It is fully detectable at plan time — `PlanBuilder.Build` already receives the snapshot
  (`PlanBuilder.cs:41`) but validates only "project key non-empty" (`PlanBuilder.cs:45-48`).
- **Pure core trapped in the COM orchestrator.** `PlanExecutor` holds a ~120-line COM-free
  tree-reshaping algorithm (`ApplyDesiredSpec` `:210-289` + `RebuildContext` `:192-199` +
  `Construction` `:190` + `ItemsReferenceEqual`/`EnumerateSubtreeNodePaths`/`SwapContainerItems`)
  (`ApplyDesiredSpec` `:210-291`) reachable from tests only via the `TestApplyDesiredSpec` back door
  (`:169-188`) and a nullability lie (`_project = project!` `:33`, misuse guard `:63-68`). The
  recursion needs only `ISubtreeDisconnector` + a logger, never `IProjectHlp`.
- **Logger-ownership prose overgrowth.** The deferred-dispose handoff (the concrete `Logger` flows to
  `DeferredExecutor` so it can dispose it after runtime ends) is correct but documented in three
  synchronized comment blocks plus a dual `_logger`/`_log` field split. The concrete-`Logger`-in-the-
  interface aspect resolves when the interface is removed (Task 3); the handoff itself stays — Task 6
  trims only the prose.
- **Silent link drop at load.** `TreeSnapshotLoader.FilterInvalidLinks` discards blank-path links
  with a bare `continue` (`TreeSnapshotLoader.cs:59-62`) — no count, no log. Same wound class as the
  DedupByWire bug that started this whole investigation.
- **Scaffolding scars.** `ConnectProbe` no longer probes (`PlanExecutor.cs:463`); the three aux types
  (`ConnectProbe`/`ConnectRunner`/`ProbeOrdering`, `:456-551`) squat in `PlanExecutor.cs`;
  `ProbeOrdering`'s `<see cref="BuildProbes"/>` (`:532`) crefs a private method of another class; the
  "read-back is blind" fact is repeated ~6× in `PlanExecutor.cs`; `DeferredExecutor.MaxRetries` is a
  misnomer (it polls, never retries); test classes `ConnectVerificationTests`/`DeferredIconnectPartitionTests`
  name behavior that no longer exists; `OpcTreeManagerFB.ToRuntime` throws raw (`OpcTreeManagerFB.cs:76`)
  while every other init failure is pin-signalled; `IOpcTreeManagerService` is unneeded indirection
  (single impl, never mocked, blackbox tests impossible).
- **Doc imbalance.** `known_issues/11` mixes durable platform mechanics (PinPout `$`-sibling model,
  connect-API routing table) into a bug entry; `architecture.md:420-434` describes a nonexistent
  `PlanExecutor(ISubtreeDisconnector, ILogger)` constructor and omits the `Unit/` test tier.

## Context (from discovery)

- Module: 22 `.cs` files, ~2270 lines. Facade (`OpcTreeManagerService`, `PlanBuilder`,
  `IOpcTreeManagerService`), TreeOperations (`PlanExecutor` 551 lines, `DeferredExecutor`,
  `LinkCollector`, `ProjectSubtreeDisconnector`, `OpcProtocolAccessor`), Entities, Config, Logging,
  and `OpcTreeManagerFB`.
- `IOpcTreeManagerService`: one impl (`OpcTreeManagerService`), one consumer (`OpcTreeManagerFB.cs:114`),
  never mocked. Its only rationale was blackbox-test substitutability, impossible for a live-COM wrapper —
  so it is pure indirection and is removed (Task 3). `LinkSwitcher`'s parallel `ILinkSwitcherService` is
  out of scope and untouched.
- Comment density is concentrated: `PlanExecutor.cs` 114 comment lines (20%), `DeferredExecutor.cs`
  33 (19%). The volume is a symptom of `PlanExecutor` doing too much — the A2 split shrinks it.
- Project pattern precedent: `CLAUDE.md` "Shared-core / thin-shell" — COM-neutral logic in a static
  helper / pure type, platform bits passed as delegates. The module is ~70% there already
  (`LinkCollector` via the `PinView` seam, `ConnectRunner`/`ProbeOrdering` via the connect delegate,
  `PlanBuilder` pure). `PlanExecutor` is the remaining lump.
- Error handling is otherwise uniform FluentResults at boundaries; the two deviations are the
  `ToScadaItemPruned` throw (A1) and the raw `ToRuntime` throw (task on FB init).

## Development Approach

- **Testing approach**: Regular (code first), host-validation-first per project convention. COM-free
  logic (plan validation, tree reshaping, dedup, ordering, config/snapshot IO) is unit-tested with
  xUnit + FluentAssertions; the COM connect, the WinForms tick, and protocol resolution are host-only
  and not unit-tested (mocking them falsifies the behavior under test).
- **Behavior-preserving refactor.** Every task must leave the full suite green; the module's runtime
  behavior (rebuild + reconnect) must not change. Because A2/A3 and the FB task touch the execution
  and lifecycle paths, a host re-smoke is a required gate before shipping (Post-Completion).
- Small focused changes; each task ends by writing/updating tests before the next.
- **Do NOT stage or commit `DefaultConfig/OpcTreeManager/{config.yaml,tree.json}`** — intentional
  uncommitted user edits.
- Keep this plan in sync as scope shifts.

## Testing Strategy

- **Unit tests** per task for COM-free code: `PlanBuilder` validation (fixture Tier A),
  `TreeReshaper` (extracted, driven directly — replacing the `TestApplyDesiredSpec` path),
  `TreeSnapshotLoader` dropped-link count, `ConnectRunner`/`ProbeOrdering` (renamed test classes),
  `LinkCollector`, `OpcScadaItemDto` pruning.
- **No e2e framework.** The connect + tick + FB lifecycle are host-only; covered by the host re-smoke
  in Post-Completion, not by unit tests.

## Acceptance Evidence

**Automatable (run from `C:\Users\admin\projects\NtoLib`):**

- `dotnet build NtoLib.sln` → 0 warn / 0 err; `dotnet test NtoLib.sln` → all pass (baseline 309 at
  HEAD `8e5b694`, count rises with new tests).
- **A1 (the bug):** a new `PlanBuilder` test — a desired spec whose (nested) child is absent from the
  snapshot returns `Result.Fail` and produces no plan. Pre-fix the same input reaches
  `ToScadaItemPruned` and throws mid-execute; the test asserts the failure is caught at plan time,
  before any mutation. Command: `dotnet test NtoLib.sln --filter FullyQualifiedName~PlanBuilder`.
- **A2:** `TreeReshaper` is unit-tested directly; `grep -rn "TestApplyDesiredSpec\|project!" NtoLib/OpcTreeManager`
  returns nothing; `PlanExecutor`'s constructor `_project` is assigned non-null without `!`.
- **A4:** a `TreeSnapshotLoader` test — a snapshot with a blank-path link surfaces a dropped-link
  count (asserted on the returned value / logged), not a silent drop.
- **Scars:** `grep -rn "MaxRetries\|ConnectProbe\|IOpcTreeManagerService" NtoLib/OpcTreeManager`
  returns nothing (renamed / interface deleted); `BuildSnapshot` survives only as a `private` method on
  `OpcTreeManagerService`; `ConnectCommand`/`ConnectRunner`/`ProbeOrdering` live in their own files, not
  `PlanExecutor.cs`; the "read-back is blind" phrase appears at most once in `PlanExecutor.cs`.
- **Docs:** the PinPout `$`-sibling model + connect-API routing table appear in
  `masterscada-fb-primer.md`; `architecture.md` names no nonexistent constructor and lists the `Unit/`
  tier.

**Manual (host re-smoke — required gate before shipping, Post-Completion):** redeploy, delete + rebuild
a TemperatureControllers channel, confirm by tree + reload that a settings pin (`CHx.Kp`) still shows
both Обратная связь + Входные. This is a refactor, so the expected result is "identical to the
2026-08-03 gate outcome (a)". A behavior-preserving refactor whose defect is not automatable at the
COM boundary is gated on this smoke, not asserted by unit tests alone.

## Progress Tracking

- Mark `[x]` immediately when done. New tasks get ➕; blockers get ⚠️.
- Tasks are ordered so each compiles and tests green independently. A2 is the backbone; the aux-type
  move (Task 5) follows it because both touch `PlanExecutor.cs`. The logger and FB tasks touch the
  facade/lifecycle and come after the core is stable.

## Solution Overview

Draw the COM seam where the project's own thin-shell pattern says it goes: a **COM-free core** (plan
validation, tree-reshaping, dedup, ordering, snapshot/config IO) that touches no vendor COM types, and
a **thin COM shell** (`PlanExecutor` resolving protocol, committing, and invoking `Connect`). Note the
reshaping core is COM-free by *delegation*, not side-effect-free — it drives live disconnects through
the `ISubtreeDisconnector` seam and mutates the container during the walk; it is not "pure" (the truly
pure form would reorder disconnect-vs-swap, a behavior change this plan forbids). Move the reshaping algorithm into `TreeReshaper`; that alone deletes the two
`null!`s, the test back door, and most of `PlanExecutor`'s bulk and comment load. Then remove the
scar tissue, make the FB init failure pin-signalled like its peers, trim the logger-ownership prose
(the handoff is correct as-is — do not restructure it), cut the comment overgrowth on what remains,
and rebalance the docs so durable mechanics live in the primer and `known_issues/11` keeps the bug
narrative.

Key decisions:
- **Drop `IOpcTreeManagerService`** — its only rationale was blackbox-test substitutability, which is
  impossible for a live-COM wrapper (single impl, one consumer, never mocked). Pure indirection; the FB
  holds the concrete `OpcTreeManagerService`. LinkSwitcher's parallel `ILinkSwitcherService` is out of
  scope and untouched.
- **`TreeReshaper` is a static helper**, matching the module's other pure helpers (`PlanBuilder`,
  `LinkCollector`, `ConnectRunner`, `ProbeOrdering` are all `static class`). One static
  `Reshape(container, desired, containerPath, resolveChild, ISubtreeDisconnector, ILogger)` returns a
  `ReshapeResult` DTO (constructions list + shrink tally). `RebuildContext` stays the internal
  recursion accumulator threaded as a parameter — no instance state, so no class is needed.
- **Behavior-preserving throughout** — no runtime behavior changes; the reconnect path stays the
  single-tick ObjectForward flow the host already validated.

## Technical Details

- **A1 validation** lives in `PlanBuilder.Build` (or a private recursive helper it calls), walking
  each `desiredTree` `NodeSpec` against the snapshot `NodeSnapshot.ScadaItem` DTOs: for every spec
  node that is neither currently present nor snapshot-constructible at its depth, apply ONE policy.
  Chosen policy: **fail the plan** (Result.Fail, nothing mutates) — the safer default, and it makes
  `ScanAndValidate`'s name honest. The top-level skip-with-warning at `PlanExecutor.cs:260-266`
  changes to match (a node that passed plan-time validation cannot be missing at execute time; the
  branch becomes an invariant guard, not a silent skip).
- **A2 `TreeReshaper`**: a new `internal static class TreeReshaper` in `TreeOperations/TreeReshaper.cs`
  holding `ApplyDesiredSpec`, `Construction`, `ItemsReferenceEqual`, `EnumerateSubtreeNodePaths`,
  `SwapContainerItems`. One static `Reshape(OpcUaScadaItem container, IReadOnlyList<NodeSpec> desired,
  string containerPath, Func<…> resolveChild, ISubtreeDisconnector disconnector, ILogger logger)`
  returns a `ReshapeResult` DTO — the constructions list + the four shrink numbers
  (`ShrinkCount/Total/Success/Fail`). `RebuildContext` stays the internal recursion accumulator threaded
  as a parameter (no instance state). `PlanExecutor` calls `TreeReshaper.Reshape(…)`; its own `_project`
  becomes non-null (`?? throw`), the misuse guard at `:63-68` is deleted, and `TestApplyDesiredSpec` is
  deleted (tests call `TreeReshaper.Reshape` directly).
- **A3 logger prose (NOT ownership)**: the deferred-dispose handoff is inherent and correct — the
  timer fires after `ToDesign` (post-`InRuntime`), so the logger outlives FB runtime; the FB keeps the
  concrete root `Logger` for sink teardown while service/executor hold `ForContext` wrappers (disposing
  a wrapper does not tear down the root's sinks), and the init-failure window at
  `OpcTreeManagerFB.cs:112-130` needs the concrete reference. Do NOT restructure ownership, make the
  service `IDisposable`, or collapse the dual fields. Only trim the three synchronized comment blocks
  to one statement + pointers, and rename `MaxRetries`→`MaxPolls`.
- **A4**: `FilterInvalidLinks` counts dropped links; `Load`/`ScanAndValidate` surfaces the count (a
  `Result` success reason or a logged line — the loader is static/loggerless, so return the count).
- **Aux types** move to `TreeOperations/`: `ConnectCommand.cs` (renamed from `ConnectProbe`),
  `ConnectRunner.cs`, `ProbeOrdering.cs`. Fix the `BuildProbes` cref (`:532`) when moving.
- **Docs**: move the "PinPout `$`-sibling" and "connect-API routing table" sections from
  `known_issues/11` into `masterscada-fb-primer.md` (pin-system material); `known_issues/11` keeps the
  symptom, the DedupByWire root cause (labelled NtoLib's own bug, not the platform's), the read-back
  blindness pitfall, the direct-first rule, and the id-path refutation. Cross-link both ways.

## What Goes Where

- **Implementation Steps** (`[ ]`): all code, test, and doc changes below.
- **Post-Completion** (no checkboxes): the host re-smoke gate, and the deliberately-deferred
  low-value items (folder rename, LinkType enum, visibility normalization) with rationale.

## Implementation Steps

### Task 1: Validate the desired tree against the snapshot at plan time

**Files:**
- Modify: `NtoLib/OpcTreeManager/Facade/PlanBuilder.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs` (the skip branch; travels into `TreeReshaper` in Task 4 — not a conflict)
- Modify: `Tests/OpcTreeManager/**` (PlanBuilder fixture tests)

- [x] add a recursive spec-vs-snapshot resolvability walk to `PlanBuilder.Build` (`PlanBuilder.cs:36`):
      for each `desiredTree` node and its `Children`, confirm it resolves against the snapshot DTO tree
      at that depth; on the first unresolvable node return `Result.Fail` with the offending path — no
      plan produced, nothing mutated. The walk MUST mirror the runtime resolver exactly: top level via
      `plan.Snapshot.TryGetValue(name)` (`PlanExecutor.cs:101`), nested by descending
      `snapshot[topName].ScadaItem.Items` by ordinal `Name` (mirroring
      `childDto.Items.FirstOrDefault(i => i.Name == inner)` at `:252`). Validate the WHOLE desired
      spec — an over-approximation, since PlanBuilder cannot know which nodes preserve vs construct
- [x] keep `PlanExecutor.cs:260-266` a NON-throwing defensive log-and-skip: with plan-time validation
      it is dead-by-construction, but it must stay safe if ever violated — a throw there fires AFTER
      the disconnect loop at `:220-227`, re-creating the exact half-rebuilt state A1 removes. Do NOT
      convert it to a throwing guard mid-recursion
- [x] write a test: a nested desired child absent from the snapshot → `Result.Fail`, no plan
- [x] write a test: a fully-resolvable desired tree still builds `Ok(plan)` unchanged (no regression)
- [x] run tests — must pass before Task 2

### Task 2: Surface the dropped-link count at snapshot load

**Files:**
- Modify: `NtoLib/OpcTreeManager/Config/TreeSnapshotLoader.cs`
- Modify: `Tests/OpcTreeManager/**` (loader tests)

- [x] `FilterInvalidLinks` (`TreeSnapshotLoader.cs:48-70`) counts blank-path links it drops; surface
      the total via a `Result` success `Reason` on `Load` — lower churn than changing the
      `Result<Dictionary<...>>` return type consumed at `OpcTreeManagerService.cs:73-83`
- [x] confirm `ScanAndValidate` actually logs the dropped-link count — otherwise it is computed but
      still not surfaced
- [x] write a test: a snapshot with N blank-path links reports N dropped
- [x] write a test: a clean snapshot reports zero dropped and loads every link
- [x] run tests — must pass before Task 3

### Task 3: Remove the `IOpcTreeManagerService` interface

The interface's only justification was blackbox-test substitutability, impossible from both ends here:
the service wraps a live `IProjectHlp`/COM surface, and its sole consumer `OpcTreeManagerFB` is
`StaticFBBase`/COM-bound and cannot be instantiated in tests either — so a mock would serve a test that
cannot exist. One impl, one consumer, never mocked. Pure indirection; delete it and have the FB hold
the concrete service. LinkSwitcher's parallel `ILinkSwitcherService` is out of scope and untouched.

**Files:**
- Delete: `NtoLib/OpcTreeManager/Facade/IOpcTreeManagerService.cs`
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`
- Modify: `NtoLib/OpcTreeManager/OpcTreeManagerFB.cs`
- Modify: `NtoLib/NtoLib.csproj` (drop the interface `<Compile Include>`)

- [x] delete `IOpcTreeManagerService`; drop `: IOpcTreeManagerService` from `OpcTreeManagerService`
- [x] retype the FB's `_service` field so it holds the concrete `OpcTreeManagerService`
      (`OpcTreeManagerFB.cs:114` construction)
- [x] demote `BuildSnapshot` to `private` — with no interface it is internal-only, STILL called by
      `CaptureAndWriteSnapshot` (`OpcTreeManagerService.cs:160`); do NOT delete it
- [x] keep the concrete `ExecuteDeferred(Logger?)` signature and the disposal handoff (trimmed in Task 6)
- [x] run tests — must pass before Task 4

### Task 4: Extract `TreeReshaper` (COM-free core out of PlanExecutor)

**Files:**
- Create: `NtoLib/OpcTreeManager/TreeOperations/TreeReshaper.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `NtoLib/NtoLib.csproj` (add the `<Compile Include>`)
- Modify: `Tests/OpcTreeManager/**` (retarget the reshaping tests to `TreeReshaper`)

- [x] create `internal static class TreeReshaper` with one static `Reshape(container, desired,
      containerPath, resolveChild, ISubtreeDisconnector, ILogger)` returning a `ReshapeResult` DTO —
      the constructions list plus the four shrink numbers `LogExecutionComplete` needs (`ShrinkCount`,
      `ShrinkTotal`, `ShrinkSuccess`, `ShrinkFail`, `PlanExecutor.cs:126-134`; do not reduce to a single
      count). Move `ApplyDesiredSpec`, `Construction`, `ItemsReferenceEqual`,
      `EnumerateSubtreeNodePaths`, `SwapContainerItems` in; `RebuildContext` becomes the internal
      recursion accumulator threaded as a param (no instance state). If `RebuildContext` and
      `ReshapeResult` fall out as the same shape (a mutable accumulator you return directly), collapse
      them to one type rather than mapping between two — do not force two types where one serves
- [x] `PlanExecutor` calls `TreeReshaper.Reshape(…)`; assign `_project` non-null (`?? throw`), delete
      the misuse guard (`PlanExecutor.cs:63-68`) and `TestApplyDesiredSpec` (`:169-188`)
- [x] retarget the tests that drove `TestApplyDesiredSpec` to call `TreeReshaper.Reshape` directly
- [x] write/keep tests for the reshaping behavior (preserve/construct/shrink, deep recursion)
- [x] run tests — must pass before Task 5

### Task 5: Move the connect aux types into their own files

**Files:**
- Create: `NtoLib/OpcTreeManager/TreeOperations/ConnectCommand.cs`
- Create: `NtoLib/OpcTreeManager/TreeOperations/ConnectRunner.cs`
- Create: `NtoLib/OpcTreeManager/TreeOperations/ProbeOrdering.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `NtoLib/NtoLib.csproj`
- Modify: `Tests/OpcTreeManager/Unit/*`

- [x] rename `ConnectProbe` → `ConnectCommand` (keep the `Action Connect` delegate — it is the COM
      seam), move it, `ConnectRunner`, and `ProbeOrdering` out of `PlanExecutor.cs:456-551` into their
      own files; add the three `<Compile Include>` entries
- [x] fix `ProbeOrdering`'s broken `<see cref="BuildProbes"/>` (`:532`) — cref the real location or
      drop the cref
- [x] update all references (`PlanExecutor`, tests) to the new names/locations
- [x] run tests — must pass before Task 6

### Task 6: Trim the logger-ownership prose; fix the `MaxRetries` misnomer

The deferred-dispose handoff is inherent, not scar tissue: the WinForms timer fires after `ToDesign`
returns (once `InRuntime` drops to false), so the logger must outlive FB runtime. The current design
is correct — the FB keeps the concrete root `Logger` for sink teardown while the service/executor
hold `ForContext` wrappers (disposing a wrapper does NOT tear down the root's sinks), and the
init-failure window at `OpcTreeManagerFB.cs:112-130` needs the concrete reference. So DO NOT
restructure disposal ownership, do NOT make the service `IDisposable`, do NOT collapse the dual
`_logger`/`_log` fields. Reduce this to the two safe wins.

**Files:**
- Modify: `NtoLib/OpcTreeManager/OpcTreeManagerFB.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/DeferredExecutor.cs`

- [x] trim the ownership comment blocks (`OpcTreeManagerFB.cs:226-233`, `DeferredExecutor.cs:26-30`;
      the third block lived on `IOpcTreeManagerService`, which Task 3 deletes) to ONE concise statement
      at the owning site (the FB) plus a one-line pointer from `DeferredExecutor` — the design stays,
      only the prose shrinks
- [x] rename `DeferredExecutor.MaxRetries` → `MaxPolls` (it polls for `InRuntime==false`, never retries)
- [x] leave the `Logger`-typed `ExecuteDeferred` signature and the disposal handoff AS-IS — verified
      correct; restructuring it risks a sink-teardown leak for a cosmetic gain
- [x] no behavior change; build 0/0, run tests — must pass before Task 7

### Task 7: Remove the raw-throw FB init guard (let the existing catch pin-signal)

The minimal fix is a deletion, not a new signaling path: `InitializeRuntime`'s existing catch
(`OpcTreeManagerFB.cs:126-135`) already pin-signals `Failed` and disposes the logger when `Project` is
null — the service ctor throws `ArgumentNullException` (`OpcTreeManagerService.cs:29`), which the catch
handles. The raw `throw` at `ToRuntime` (`OpcTreeManagerFB.cs:74-77`) short-circuits that path into the
host lifecycle call instead.

**Files:**
- Modify: `NtoLib/OpcTreeManager/OpcTreeManagerFB.cs`

- [x] delete the four-line `TreeItemHlp?.Project == null` raw-throw guard (`OpcTreeManagerFB.cs:74-77`)
      so a null `Project` flows into `InitializeRuntime` and is pin-signalled by the existing catch — do
      NOT add a new signaling path
- [x] confirm the existing catch (`:126-135`) actually reports the null-`Project` case as a `Failed`
      pin, not a swallowed error; if it does not, add the pin-signal there instead
- [x] no unit test (host/COM init path); note the manual verification in Post-Completion
- [x] build 0/0, run tests — must pass before Task 8

### Task 8: Rename the scaffolding-era test classes

**Files:**
- Modify (rename): `Tests/OpcTreeManager/Unit/ConnectVerificationTests.cs` → `ConnectRunnerTests.cs`
- Modify (rename): `Tests/OpcTreeManager/Unit/DeferredIconnectPartitionTests.cs` → `ProbeOrderingTests.cs`

- [x] rename `ConnectVerificationTests` → `ConnectRunnerTests` (it tests `ConnectRunner`; nothing
      verifies anymore)
- [x] rename `DeferredIconnectPartitionTests` → `ProbeOrderingTests` (it tests `ProbeOrdering`; no
      deferred partition exists); drop the dangling "host smoke described in the plan" pointer
- [x] update `<Compile Include>` if the test csproj lists them explicitly (it uses default globs — no edit)
- [x] run tests — must pass before Task 9

### Task 9: Cut the comment overgrowth

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/DeferredExecutor.cs`

- [x] collapse the "read-back is blind" litany in `PlanExecutor.cs` to a single mention (the `Execute`
      docstring) + a pointer to `known_issues/11`; delete the other copies
- [x] harsher comment pass on `PlanExecutor.cs` (now smaller after Tasks 4-5) and `DeferredExecutor.cs`:
      delete restatement/process/banner comments; keep only short-local decode keys, warnings, and
      the earned COM hazards (STA no-lock, live-view materialization, ctIConnect-vs-mask, double-dispose)
- [x] no test change (comments only); build 0/0, run tests — must pass before Task 10

### Task 10: Rebalance the domain docs

**Files:**
- Modify: `Docs/architecture/masterscada-fb-primer.md`
- Modify: `Docs/known_issues/11-opc-pinpout-sibling-and-iconnect-connect.md`
- Modify: `Docs/architecture/architecture.md`
- Modify: `Docs/readme.md` (if the index line needs adjusting)

- [x] move the durable platform-mechanics sections (PinPout `$`-sibling model, connect-API routing
      table) from `known_issues/11` into `masterscada-fb-primer.md`; leave `known_issues/11` the bug
      narrative — symptom, DedupByWire root cause (labelled NtoLib's own bug), read-back blindness,
      direct-first rule, id-path refutation; cross-link both ways
- [x] accuracy-sweep the OPC/pin known-issues (05, 06, 09, 11) — TIMEBOXED to fixing lines that state a
      now-refuted theory (e.g. the id-path-collision framing) or contradict the final understanding.
      This is NOT a rewrite pass: do not restructure or re-word docs that are merely old-but-correct
- [x] de-dup: each fact has one canonical home (primer or known_issues), pointers elsewhere
- [x] fix `architecture.md:420-434` — name no nonexistent `PlanExecutor(ISubtreeDisconnector, ILogger)`
      constructor (name `TreeReshaper` if a type is cited); add the `Unit/` test tier (three tiers)
- [x] no unit test (docs only)

### Task 11: Verify acceptance criteria

- [x] all Overview findings addressed; behavior unchanged (no runtime-behavior edits) — plan-time
      validation (`PlanBuilder.FindUnresolvableNode`), `TreeReshaper` extracted, interface gone,
      dropped-link count (`DroppedLinksSuccess`), scars fixed, docs rebalanced
- [x] `grep -rn "TestApplyDesiredSpec\|project!\|MaxRetries\|ConnectProbe\|IOpcTreeManagerService" NtoLib/OpcTreeManager`
      returns nothing (interface deleted); `BuildSnapshot` survives only as a `private` method; the aux
      types live in their own files (`ConnectCommand.cs`/`ConnectRunner.cs`/`ProbeOrdering.cs`);
      "read-back is blind" 1× in PlanExecutor (Execute docstring + 1 pointer restatement)
- [x] run full suite: `dotnet test NtoLib.sln` → 314 passed / 0 failed
- [x] `dotnet build NtoLib.sln` → 0 warn / 0 err; `dotnet format NtoLib.sln --verify-no-changes` clean
      except the known pre-existing IDE1006 in `EditorRuntimeOptionsProviderTests.cs`

### Task 12: Update documentation, tidy the plans directory, and close

**Files:**
- Move: `Docs/plans/20260731-opctreemanager-iconnect-snapshot-fix-and-cleanup.md` → `Docs/plans/completed/`
- Move: `Docs/plans/20260730-opctreemanager-iconnect-reconnect-fix.md` → `Docs/plans/completed/`
- Move: `Docs/plans/20260730-opctreemanager-iconnect-deferred-connect.md` → `Docs/plans/completed/`
- Move: `Docs/plans/20260731-opctreemanager-iconnect-fresh-id-remap.md` → `Docs/plans/completed/`
- Modify: `Docs/plans/iconnect-investigation-log.md`

- [x] update `CLAUDE.md` only if a genuinely new convention emerged (no convention change — this
      follows the existing thin-shell pattern; nothing to add)
- [x] `git mv` the four done/superseded iconnect plans (snapshot-fix-and-cleanup [executed + host-verified],
      reconnect-fix, deferred-connect, fresh-id-remap [all superseded]) into `Docs/plans/completed/`
- [x] keep `iconnect-investigation-log.md` in `Docs/plans/` (living doc, referenced from
      `known_issues/11`); updated its line-5 companion pointer to
      `completed/20260730-opctreemanager-iconnect-deferred-connect.md`
- [x] verify no other doc/code path references a moved plan (grep `20260730`/`20260731` under
      `NtoLib/` and `Docs/`); fixed the three now-stale full-path cross-refs inside the moved plans
      (bare-filename sibling refs resolve as-is); `known_issues/11` → investigation-log still resolves
- [x] leave THIS plan (`20260803-…-cleanup-and-refactor.md`) in `Docs/plans/` — it archives on its own
      completion/delivery, not now (left in place — delivery work)

## Post-Completion

*Manual / external — no checkboxes.*

**Host re-smoke (required gate before shipping — Tasks 4, 6, 7 touch the execution/lifecycle path):**
- Rider "Deploy Debug"; delete + rebuild a TemperatureControllers channel from the snapshot; confirm
  by tree + project save/reload that a settings pin (`CHx.Kp`) still shows both Обратная связь +
  Входные, and `CHx.Setpoint` still reconnects. Expected: identical to the 2026-08-03 gate outcome (a).
  A behavior change here is a refactor regression, not a new finding.

**Deliberately deferred (considered, low value / high churn — pull in only if you disagree):**
- `Config/` folder holds non-config IO (`TreeSnapshotLoader`/`Writer`). Rename to `Persistence/` or
  `Io/` — a namespace change across the module for cohesion; deferred as churn-heavy, low-value.
- `LinkType` string constants → enum. Defensible as-is (the string is the JSON wire format and every
  compare site handles the unknown case → logged `resolveFail`, not a crash). Worth it only alongside
  a snapshot-schema change; deferred.
- Visibility normalization (`LinkCollector` is `public`, peers `internal`). Tests use
  `InternalsVisibleTo`, so nothing is forced public; internal-by-default is tidier but near-zero value.

**Executed by exec:**
- branch: iconnect-reconnect-fix
- All 12 tasks complete (commits `0ea5902`..`7b55196`), reviewed by a 5-lens comprehensive pass
  (implementation/testing/simplification clean; quality + documentation recovered after a session-limit
  cut — both ACHIEVED) plus a fixer that finished the probe→command rename and fixed a stale pointer.
  Final: 314 tests pass, build 0/0, format clean, working tree clean except the intentional
  `DefaultConfig` edits. Branch left unrebased for delivery. The host re-smoke below is still pending.

## Verify it yourself

This is a behavior-preserving refactor — most of it is proven by the suite staying green (314) across
every commit, not by a manual repro. Commit range `e8a36e6..HEAD`.

Automatable (run from `C:\Users\admin\projects\NtoLib`):
1. `dotnet build NtoLib.sln` → 0 warn / 0 err; `dotnet test NtoLib.sln` → 314 pass.
2. **A1 (the one real behavior change — fail-fast instead of mid-mutation throw):**
   `dotnet test NtoLib.sln --filter FullyQualifiedName~PlanBuilder` — a nested desired child absent
   from the snapshot now returns `Result.Fail` (no plan, no mutation). The committed fixture
   `Tests/OpcTreeManager/Fixtures/Acceptance/nested-child-not-in-snapshot/expected.json` is that
   scenario (flipped `isOk` true→false: the `true` had encoded the pre-fix buggy behavior). Compare
   pre-fix commit `e8a36e6` (fixture `isOk:true`, throw reachable) vs `0ea5902` (fail at plan time).
3. `grep -rn "TestApplyDesiredSpec\|project!\|MaxRetries\|ConnectProbe\|IOpcTreeManagerService\|ProbeOrdering" NtoLib/OpcTreeManager`
   → nothing (all removed/renamed); `TreeReshaper.Reshape` exists; `ConnectCommand`/`ConnectRunner`/
   `CommandOrdering` in their own files.

Host re-smoke (required gate — Tasks 4/6/7 touched the execution + lifecycle path; unit tests cannot
cover the COM half): redeploy, delete + rebuild a TemperatureControllers channel, confirm by tree +
reload that `CHx.Kp` still shows both Обратная связь + Входные and `CHx.Setpoint` still reconnects.
Expected: identical to the 2026-08-03 iconnect gate outcome. Any difference is a refactor regression.
