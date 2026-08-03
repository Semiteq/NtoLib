# OpcTreeManager iconnect — snapshot-capture fix + scaffolding cleanup

## Overview

`OpcTreeManager` snapshots an OPC-UA subtree to `tree.json` and, on Execute, deletes a channel
subtree and rebuilds it from the snapshot, then re-establishes every external link. The reported
defect: after rebuild, `directPin`/`directPout` links reconnect but `iconnect` links on
TemperatureControllers channels (Kp, Ti, Td, MaxOutput, MinOutput, Setpoint, SpeedSP, TempOffset,
PowerOffset) never show connected.

Root cause (confirmed): the snapshot dropped the **input half** of every settings-class iconnect
pin. Each such pin exposes two distinct wires — the base pin carries an `iconnect` (feedback) and
its `$` sibling carries a `directPin` (input) to the same external element. `LinkCollector.DedupByWire`
folded the pair into one row, keeping the iconnect and discarding the `directPin`. The `directPin`
input is the same reliable link class as StatusWord (routes by name), so its loss is exactly why the
pin never showed connected. Fixed already on this branch (HEAD `077bbb2`): dedup now removes only
exact `(local, external, linkType)` triples and keeps both halves.

Two things follow from that fix and must be resolved here:

1. **A residual, genuine iconnect case exists.** Some iconnect wires have no `$` twin at all — every
   `Setpoint` pin carries two feedback-only iconnects (`→…TemperatureSP`, `→…PowerSP`) plus a `$`
   directPin to a *third* external (`→…Setpoints.Setpoint`). Restoring the directPin input does not
   cover the twinless feedback wires. Whether those reconnect on a **well-formed** snapshot (both
   halves present, direct wired first) is untested — that is the host experiment this plan gates on.
2. **The branch carries 8-attempt diagnostic scaffolding that must be removed.** A production build
   from this branch currently ships a modal dialog (`SelectedIConnectMechanism = PasteConnection`,
   `PlanExecutor.cs:90`). The fresh-id remap, the 10-mode mechanism selector, the DeferredExecutor
   second tick, and the blind read-back verdict were all built for the failed reconnect theory and
   are dead weight or actively misleading.

## Context (from discovery)

- Project: NtoLib, MasterSCADA 3.x FB library, .NET Framework 4.8, C# 10. Host validation precedes
  unit tests for FB/XML/COM changes (project convention, `CLAUDE.md`).
- `NtoLib/OpcTreeManager/TreeOperations/LinkCollector.cs` — capture. `DedupByWire` now exact-triple
  only, logging "Exact-duplicate dedup: {DroppedCount} rows removed" at Debug (`LinkCollector.cs:129`);
  `AppendLinks` maps mask→linkType 1:1 (`LinkCollector.cs:87-92`); `CollectLink` per-link DBG line at
  `LinkCollector.cs:151-153`.
- `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs` — rebuild + reconnect. Diagnostic block
  `PlanExecutor.cs:26-99`; live default `:90-91`; `DiagnosticReconnectPreserved :98`; fresh-id remap
  seed `:162-172` + `NextEvenIdOffset :421`; `Execute` returns `Result<DeferredConnectWork>` `:126`;
  `BuildProbes :643`; `BuildConnectAction`/`BuildIConnectAction :765,:779`; `ConnectAndVerifyDeferred
  :260`; `LogDeferredAbort :366`; `ReadBackPeers :856`; `CombinedMaskReadBack :878`; `ConnectVerifier`
  (`ConnectAll :974`, `VerifyAll :1003`) `:927-1074`.
- `NtoLib/OpcTreeManager/Entities/OpcScadaItemDto.cs` — DTO→live tree. `idOffset` threaded through
  `ToScadaItem :85`, `ToScadaItemPruned :104`, `BuildWithoutChildren :124`, applied `Id = Id + idOffset
  :152`.
- `NtoLib/OpcTreeManager/TreeOperations/DeferredExecutor.cs` — two-tick lifecycle. `Post :35`,
  once-only `Release :49`; second-tick arming described `:21-32`.
- `NtoLib/OpcTreeManager/Logging/OpcLoggerFactory.cs` — Serilog `MinimumLevel.Debug()` to file
  (`OpcLoggerFactory.cs:22-23`). DBG IS emitted in the deployed config; the diagnosis was slow because
  the pre-fix dedup logged only a collapsed-row count with no identity, not because the level hid it.
  The current dedup line still lacks per-row identity (Task 3 adds it).
- `Docs/plans/iconnect-investigation-log.md` — the attempt matrix; its RESOLVED id-path-collision
  section (`:74-105`) is falsified by attempt 7 (`:127-131`) and must be retracted.
- Tests: `Tests/OpcTreeManager/Unit/LinkCollectorTests.cs`, `DeferredIconnectPartitionTests.cs`,
  `ConnectVerificationTests.cs`.

## Development Approach

- **Testing approach**: Regular (code first), host-validation-first per project convention. The
  COM-free logic (dedup, probe ordering, capture invariant, partition) is unit-tested; the connect
  itself and the WinForms tick are host-only and judged by tree + project save/reload.
- Complete each task fully, tests green, before the next.
- Small focused changes; every task lists new/updated tests as separate checklist items.
- **Do NOT stage or commit `DefaultConfig/OpcTreeManager/{config.yaml,tree.json}`** — those carry
  intentional uncommitted user edits.
- Keep this plan in sync as scope shifts.

## Testing Strategy

- **Unit tests** (xUnit + FluentAssertions): required per task for COM-free code. No live `IProjectHlp`.
- **No e2e framework** in this repo. The iconnect connect + tick lifecycle are host-only; they are
  covered by the numbered host smoke in Acceptance Evidence, not by unit tests (mocking them would
  stub away the exact behavior under test).

## Acceptance Evidence

**Automatable (the capture fix — the confirmed defect):**

- Regression already in place: `dotnet test NtoLib.sln --filter FullyQualifiedName~LinkCollectorTests`
  → 8 pass, including `BuildLinks_IConnectAndDollarDirectPinToSameExternal_KeepsBoth` (both halves
  survive) and `BuildLinks_ExactDuplicateRows_CollapsedToOne`.
- New per-task assertions: direct-first probe ordering (Task 2), capture invariant warning (Task 3).
- Full suite green after every task: `dotnet test NtoLib.sln` (baseline 325 pass at HEAD `077bbb2`).

**Manual host smoke — the gate between Phase A and Phase B (genuinely COM/SCADA-bound):**

1. Rider "Deploy Debug" the Phase-A DLL; confirm the deployed `NtoLib.dll` timestamp advances.
2. Open the fully-connected **donor** project from disk (not "load previously loaded"); `ExecuteSnapshot`.
3. Inspect the regenerated `tree.json`: `CH1.Kp` carries BOTH a `Kp` iconnect row AND a `Kp$`
   directPin row (the 456-row broken snapshot had only the iconnect). The new per-drop identity lines
   show zero settings-class directPin drops; the capture invariant warns only for twinless feedback pins.
4. On a project whose external root name matches the snapshot, purge + rebuild the OPC subtree, Execute
   **once under each candidate mechanism** — `ObjectForward` and `DelayConnection(...,ctIConnect)` +
   trailing `ApplyChange` — recording each run separately. The mechanism selector still exists at the
   gate precisely to allow this comparison.
5. Judge **by tree + project save/reload only** (in-code read-back is blind — proven): every Kp-class
   pin shows connected via its restored directPin input after reload (this is mechanism-independent —
   it rides the capture fix, not any iconnect call).
6. Record which of three outcomes holds for the twinless `Setpoint` feedback iconnects
   (`TemperatureSP`/`PowerSP`): (a) they land under `ObjectForward`; (b) only under
   `DelayConnection+ApplyChange`; (c) neither wires them. This decides which single mechanism Task 7
   keeps (outcome c → keep `ObjectForward` as the inert default and document the residual loss).

A defect that reproduces (snapshot dropped 192 rows; the fixed dedup keeps them) and is measured
(row counts + host reload) — not a hard stop.

## Progress Tracking

- Mark `[x]` immediately when done. New tasks get ➕; blockers get ⚠️.
- **Phase A (Tasks 1-4)** is safe and independent of the host outcome — including removing the
  fresh-id remap, which attempts 7-8 already falsified (the native id-path is a derived handle, so the
  remap is inert regardless of snapshot quality). Removing it in Phase A also makes the host gate
  exercise the **shipping** reconstruction path, not a remap path Phase B would delete.
- **Task 5 is the host gate.** **Phase B (Tasks 6-9)** is gated only where it truly depends on the
  host result: Task 7 (which single iconnect mechanism survives). Task 6 (deferred-plumbing removal)
  precedes Task 7 for compile order. Do not start Phase B until the host smoke is recorded.

## Solution Overview

Two phases with a host gate between them.

- **Phase A — make the branch safe, correct capture, and validate the shipping reconstruction.** Reset
  the live diagnostic default so no build ships the dialog; order replay so directs wire before
  iconnects (the state manual editing always operates in, currently reversed); make the capture logging
  honest (per-drop identity, a capture invariant); remove the falsified fresh-id remap so the gate
  exercises the reconstruction path that actually ships. None of this depends on the host result.
- **Host gate.** Regenerate the snapshot, rebuild a channel under each candidate iconnect mechanism,
  observe by tree + reload. This answers the one open question: do the Kp-class pins reconnect via their
  restored directPin (expected, mechanism-independent), and do the twinless feedback iconnects reconnect
  on a well-formed snapshot under any mechanism.
- **Phase B — cleanup, gated on the gate.** Remove the deferred second tick (keep the first-tick
  InRuntime wait, which serves the deferred-execution known issue, not the saga), then collapse the
  mechanism selector to the one the host validated, then remove the blind read-back verdict. Salvage the
  durable MasterSCADA facts into `Docs/known_issues/` and retract the falsified id-path theory. The
  deferred-plumbing removal comes before the mechanism collapse because `ConnectPartitioner` references
  the `DeferredObjectForward` enum value; collapsing the enum first would not compile.

## Technical Details

- **Probe ordering**: `BuildProbes` (`PlanExecutor.cs:643`) emits probes in pin-enumeration order, so
  a pin's iconnect is issued before its `$` directPin twin. Extract a named COM-free static
  (`OrderProbesDirectFirst(IReadOnlyList<ConnectProbe>)`, keyed on `LinkType` alone, like
  `ConnectPartitioner`) that returns directPin/directPout first, iconnect last, stable within class;
  `Execute` calls it on the built probe list. The static seam is what makes the ordering unit-testable
  without a live `IProjectHlp`.
- **Fresh-id remap removal**: revert `OpcScadaItemDto` to snapshot-id reuse (drop the `idOffset`
  parameter chain and `Id = Id + idOffset`), drop `RebuildContext.IdOffset` and `NextEvenIdOffset`.
  Attempt 7 proved the remap changes nothing (the native id-path is a derived handle), and shifting
  ids on every Execute is a monotonic ratchet that diverges from the snapshot.
- **Mechanism collapse**: reduce to exactly one iconnect connect call, chosen by the gate's three
  outcomes — (a) twinless iconnect lands under `ObjectForward` → keep `localPin.Connect(externalPin,
  ctIConnect)`; (b) only under `DelayConnection(local, external, 1, ctIConnect)` + trailing `ApplyChange`
  → keep that; (c) neither wires twinless feedback → keep `ObjectForward` as the inert default and
  document the residual loss. In every outcome the Kp-class pins already reconnect via their restored
  directPin (the capture fix), so the mechanism choice only governs the twinless feedback wires. Delete
  the `IConnectMechanism` enum and `SelectedIConnectMechanism` const entirely (no one-value enum) and
  inline the surviving call; delete `PasteConnection`, `DiagnosticApplyChange`,
  `DiagnosticReconnectPreserved`, and the per-link attribution logging.
- **Deferred second tick removal**: `Execute` connects iconnect in-pass again (single tick after
  `InRuntime==false`). Remove `ConnectAndVerifyDeferred`, `DeferredConnectWork`, `ConnectPartitioner`,
  `LogDeferredAbort`, and `DeferredExecutor.ArmSecondPass`. Keep `Post`'s first timer + `RunTickGuarded`
  + `Release` (the InRuntime-wait that the deferred-execution known issue requires).
- **Read-back verdict removal**: `GetConnections` read-back is blind post-commit, so `VerifyAll`'s
  ok/fail `[ERR]` is meaningless. Keep `ConnectAll` with its per-probe try/catch and emit an honest
  "iconnect: N connects issued, M threw" summary. Remove `VerifyAll` verdict, `CombinedMaskReadBack`,
  `ReadBackPeers`, and `ConnectProbe.ReadBackPeers`.
- **Logging honesty**: `DedupByWire` logs each removed row's `(local, external, linkType)` and what it
  merged into, at Warning. Add a capture invariant: after `BuildLinks`, warn for any settings-class
  iconnect row lacking a same-external `$` directPin twin (surfaces the exact class this bug hid).

## What Goes Where

- **Implementation Steps** (`[ ]`): all code, tests, and docs changes below.
- **Post-Completion** (no checkboxes): the host smoke (Task 5 gate body), Rider deploy, snapshot
  regeneration, and the customer decision on any residual twinless-iconnect loss.

## Implementation Steps

### Task 1: Reset the live diagnostic default (stop shipping the dialog)

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`

- [x] set `SelectedIConnectMechanism` (`PlanExecutor.cs:90`) to `IConnectMechanism.ObjectForward`
- [x] confirm `DiagnosticApplyChange` (`:91`) and `DiagnosticReconnectPreserved` (`:98`) are `false`
- [x] run `dotnet build NtoLib.sln` — 0 warn / 0 err (guard against the const CS0162 noted in history)
- [x] run `dotnet test NtoLib.sln` — 325 pass unchanged
- [x] `dotnet format NtoLib.sln`

### Task 2: Order replay direct-first, iconnect-last

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/DeferredIconnectPartitionTests.cs` (or a new ordering test file)

- [x] extract a COM-free static `OrderProbesDirectFirst(IReadOnlyList<ConnectProbe>)` keyed on
      `LinkType`, returning `directPin`/`directPout` before `iconnect`, stable within class
- [x] call it in `Execute` on the list from `BuildProbes` (`PlanExecutor.cs:643`) before partition/connect
- [x] verify directPin/directPout connect calls stay byte-for-byte identical (only order changes)
- [x] write a unit test: a mixed probe set returns directs before iconnects, stable within class
- [x] write an edge test: all-iconnect and all-direct sets are unchanged by the sort
- [x] run tests — must pass before Task 3

### Task 3: Honest capture logging — per-drop identity + capture invariant

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/LinkCollector.cs`
- Modify: `Tests/OpcTreeManager/Unit/LinkCollectorTests.cs`

- [x] `DedupByWire` (`LinkCollector.cs:111-134`): log each removed row's `(local, external, linkType)`
      at Warning (not a bare count); the survivor it duplicates is implicit (exact triple)
- [x] add a capture invariant in `BuildLinks`: after dedup, for each `iconnect` row whose local path
      has no same-external `$` directPin sibling in the set, log a Warning naming the pin (the exact
      class this bug silently dropped) — feedback-only pins like Setpoint are expected and named as such
- [x] write a unit test: exact-duplicate input emits a Warning naming the dropped triple
- [x] write a unit test: an iconnect row with no `$` twin triggers the invariant Warning; a paired
      iconnect+`$`directPin does not
- [x] run tests — must pass before Task 4
- [x] (the misleading connect-side read-back verdict is NOT here — it is COM-side and removed in Task 8)

### Task 4: Remove the fresh-id remap (validate the shipping reconstruction before the gate)

**Files:**
- Modify: `NtoLib/OpcTreeManager/Entities/OpcScadaItemDto.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/*` (remove `NextEvenIdOffset` theory)

- [x] drop the `idOffset` parameter from `ToScadaItem`/`ToScadaItemPruned`/`BuildWithoutChildren` and
      restore `Id = Id` verbatim (`OpcScadaItemDto.cs:85,104,124,152`)
- [x] remove `RebuildContext.IdOffset` seeding (`PlanExecutor.cs:162-172`), the `ToScadaItemPruned(spec,
      context.IdOffset)` call site (`:536`), and `NextEvenIdOffset` (`:421`)
- [x] delete the `NextEvenIdOffset` unit tests
- [x] run tests — snapshot-id reuse path green
- [x] `dotnet format`

### Task 5: Host smoke — regenerate snapshot, confirm capture, compare mechanisms (GATE)

**Files:**
- Modify: `Docs/plans/completed/20260731-opctreemanager-iconnect-snapshot-fix-and-cleanup.md` (record result)

- [x] deploy the Phase-A DLL; regenerate the donor `tree.json`; confirm `CH1.Kp` has both halves and
      the invariant Warning fired only for the twinless feedback pins — verified 2026-08-03 12:05 log:
      `CH19.Kp` captured as both `iconnect CH19.Kp` and `directPin CH19.Kp$`; zero dedup drops; capture
      invariant fired only for `*.Setpoint` pins
- [x] rebuild a name-matched channel and judge by tree + reload — verified 2026-08-03 12:07 rebuild of
      `CH17..CH23` under the default `ObjectForward`: `CH19.Kp` shows both Обратная связь + Входные, and
      `CH19.Setpoint` shows its feedback links, in the live project. `DelayConnection+ApplyChange` was
      not needed — `ObjectForward` reconnected everything, including the twinless Setpoint feedbacks.
      (The log's `ok=24 fail=190` is the blind read-back verdict, not real failures — Task 8 removes it.)
- [x] **Gate outcome: (a)** — twinless iconnect lands under `ObjectForward`. Task 7 keeps `ObjectForward`
      outright; no residual limitation to document. Both-half capture (dedup fix) + direct-first ordering
      put the iconnect on an already-good pin, which is the state manual editing always used.

### Task 6: Remove the DeferredExecutor second tick (keep the first-tick InRuntime wait)

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/DeferredExecutor.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/DeferredIconnectPartitionTests.cs`

- [x] `Execute` (`PlanExecutor.cs:126`) connects iconnect in-pass on the single post-`InRuntime` tick;
      change its return from `Result<DeferredConnectWork>` to an in-pass result
- [x] remove `ConnectAndVerifyDeferred` (`PlanExecutor.cs:260`), `DeferredConnectWork`,
      `ConnectPartitioner`, `LogDeferredAbort` (`:366`)
- [x] remove `DeferredExecutor.ArmSecondPass` and the second-tick handler; collapse `Post`'s tick body
      to "InRuntime==false → `Execute` → `Release`" with no `IsEmpty`/`ArmSecondPass` branch
      (`DeferredExecutor.cs:96-123`); keep the first timer, `RunTickGuarded`, and once-only `Release`
      (`DeferredExecutor.cs:35,49`); update the doc comments (`DeferredExecutor.cs:19-33`,
      `PlanExecutor.cs:119-125`)
- [x] update/trim `DeferredIconnectPartitionTests` to the single-tick reality
- [x] run tests — must pass before Task 7

### Task 7: Collapse the mechanism selector to the one validated mechanism

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/DeferredIconnectPartitionTests.cs`

- [x] delete the `IConnectMechanism` enum and `SelectedIConnectMechanism` const entirely (no one-value
      enum), the `PasteConnection`/`DiagnosticApplyChange`/`DiagnosticReconnectPreserved` consts, and the
      diagnostic comment block (`PlanExecutor.cs:26-99`)
- [x] reduce `BuildIConnectAction` (`:779`) to the single mechanism the gate validated (outcome a/b/c;
      default `ObjectForward` — inline `localPin.Connect(externalPin, ctIConnect)`); remove the per-link
      "iconnect probe mechanism=…" attribution logging
- [x] remove `AddPasteConnection`/`ApplyPasteConnections` wiring and its between-phase flush
- [x] update partition tests to the reduced surface (or delete cases that only tested dead modes)
- [x] run tests — must pass before Task 8

### Task 8: Remove the blind read-back verdict

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/ConnectVerificationTests.cs`

- [x] remove `VerifyAll` verdict logic (`PlanExecutor.cs:1003`), `CombinedMaskReadBack` (`:878`),
      `ReadBackPeers` (`:856`), and the `ConnectProbe.ReadBackPeers` field (`:751`)
- [x] keep `ConnectAll` (`:974`) with per-probe try/catch; replace the execution-complete summary with an
      honest "iconnect: N connects issued, M threw"
- [x] rewrite `ConnectVerificationTests` to assert connect-all ordering + throw-tolerance, not read-back
- [x] run tests — must pass before Task 9

### Task 9: Salvage domain facts and retract the falsified theory

**Files:**
- Create: `Docs/known_issues/<nn>-opc-pinpout-sibling-and-iconnect-connect.md`
- Modify: `Docs/plans/iconnect-investigation-log.md`

- [x] write the known-issue entry with the facts **inlined** (no scratchpad pointers — that dir is
      session-ephemeral): the PinPout `$`-sibling model (base = Pout/feedback, `$` = Pin/input),
      iconnect-vs-directPin semantics, the connect-API routing table (name-route vs live-RCW-ptr,
      duplicate-skip, direct-only `AddConnectionRecord`, dialog-bound paste), and the "in-code read-back
      is blind post-commit" fact — each a crisp reusable fact
      → `Docs/known_issues/11-opc-pinpout-sibling-and-iconnect-connect.md`; added to `Docs/readme.md` index
- [x] mark clearly which facts are durable vs which held only under the broken snapshot (do not
      memorialize the latter as platform facts) — separate "Опровергнутая теория — id-path collision" section
- [x] retract the RESOLVED id-path-collision section in the investigation log (`:74-105`): note it was
      falsified by attempt 7 and superseded by the snapshot-capture root cause
- [x] no unit tests (docs only)

### Task 10: Verify acceptance criteria

- [x] both PinPout halves survive capture (LinkCollectorTests green); direct-first ordering holds —
      `DedupByWire` is exact-triple only, `ProbeOrdering.OrderProbesDirectFirst` called in `Execute`
- [x] no diagnostic default, no fresh-id remap, no second tick, no blind verdict remain (grep clean) —
      zero hits across `NtoLib/` and `Tests/` for `IConnectMechanism`, `SelectedIConnectMechanism`,
      `PasteConnection`, `AddPasteConnection`, `DiagnosticApplyChange`, `DiagnosticReconnectPreserved`,
      `DeferredConnectWork`, `ConnectPartitioner`, `ConnectAndVerifyDeferred`, `ArmSecondPass`,
      `VerifyAll`, `CombinedMaskReadBack`, `ReadBackPeers`, `NextEvenIdOffset`, `idOffset`. `Execute`
      connects iconnect via a single inlined `localPin.Connect(externalPin, EConnectionType.ctIConnect)`
- [x] run full suite: `dotnet test NtoLib.sln` — 309 passed, 0 failed, 0 skipped
- [x] `dotnet format NtoLib.sln` clean — only the known pre-existing IDE1006 in
      `Tests/MbeTable/Infrastructure/EditorRuntimeOptionsProviderTests.cs` (unrelated)

### Task 11: Update documentation and close the plan

- [x] update `CLAUDE.md` / `Docs/architecture/` only if a convention actually changed —
      (no convention change; the `ProbeOrdering.OrderProbesDirectFirst` seam and capture-invariant
      logging are internal OpcTreeManager details, not project-wide conventions)
- [x] move this plan to `Docs/plans/completed/` — (left in place — archiving is delivery work done
      after host testing)

## Post-Completion

*Manual / external — no checkboxes.*

**Host validation (Task 5 gate body, and re-run after Phase B):**
- Rider "Deploy Debug"; open the donor project **from disk** and fully close/reopen MasterSCADA between
  runs (cached "load previously loaded" state has produced false results before).
- Regenerate `tree.json`; rebuild a channel; judge by tree + save/reload, never by in-code read-back.

**Customer decision:**
- If twinless `Setpoint` feedback iconnects do not reconnect even on a well-formed snapshot, that is a
  cleanly isolated platform limitation. Document the concrete residual loss and decide with the
  customer whether device→FB feedback initialization justifies further effort.

**Executed by exec:**
- branch: iconnect-reconnect-fix
- All tasks complete. Phase A (Tasks 1-4) → host gate (Task 5, outcome (a), verified 2026-08-03) →
  Phase B (Tasks 6-9 cleanup) → Tasks 10-11 (verify + close). The iconnect reconnect works on the host
  under `ObjectForward`; the 8-attempt scaffolding is removed; the domain facts are salvaged to
  `Docs/known_issues/11`. Final: 309 tests pass, build 0/0. Branch left unrebased for delivery.

## Verify it yourself

Phase A is unit-verified in the repo; the reconnect behavior it sets up is only observable in the
MasterSCADA host (Task 5 gate). Commit range: `0a4fc32~1..HEAD` (plan `0a4fc32`, code `6faf50e..a097845`,
review fixes `c045f44`).

Automatable checks (run from `C:\Users\admin\projects\NtoLib`):

1. `dotnet build NtoLib.sln` → 0 warn / 0 err.
2. `dotnet test NtoLib.sln` → all pass (325).
3. Capture fix regression: `dotnet test NtoLib.sln --filter FullyQualifiedName~LinkCollectorTests` →
   both PinPout halves survive dedup; the twinless-iconnect invariant fires only when no same-external
   `$` twin exists. Compare pre-fix commit `9d83388` (folds the halves) against HEAD.
4. Id-preservation: `dotnet test NtoLib.sln --filter FullyQualifiedName~OpcScadaItemDtoPruneTests` →
   `ToScadaItem`/`ToScadaItemPruned` copy every source `Id` verbatim (no remap).
5. No scaffolding regressions land in Phase A: `git grep -nE "IdOffset|NextEvenIdOffset|idOffset" -- NtoLib Tests`
   returns nothing; `PlanExecutor.cs` line 90 reads `ObjectForward`, not `PasteConnection`.

Host gate (Task 5) — not automatable, judged by tree + project save/reload only (in-code read-back is
blind): deploy the Phase-A DLL, regenerate the donor `tree.json`, confirm `CH1.Kp` carries both a `Kp`
iconnect and a `Kp$` directPin row, then rebuild a name-matched channel under each candidate mechanism
and record whether Kp-class pins reconnect (expected, via directPin) and whether twinless `Setpoint`
feedbacks land.
