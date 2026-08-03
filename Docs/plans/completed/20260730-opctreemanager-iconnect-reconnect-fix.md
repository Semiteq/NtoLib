# OpcTreeManager — fix silent iconnect reconnection on freshly-constructed pins

## Overview

When `OpcTreeManager` Execute constructs a new OPC node (e.g.
`TemperatureControllers.CH17..CH23`), it reconnects the node's external links from the
`tree.json` snapshot. Direct links (`directPin` / `directPout`) reconnect correctly.
**iconnect** links (Kp, Ti, Td, MaxOutput, MinOutput, Setpoint, SpeedSP, TempOffset,
PowerOffset) are logged as `Connected` but never materialize in the project tree.

Two independent defects:

1. **Verification gap (cause known, fix certain).** `PlanExecutor.TryConnectLink` treats a
   non-throwing COM `Connect` call as success (`PlanExecutor.cs:409`). The iconnect COM call
   returns `void` and does nothing on a freshly-constructed pin, so the run reports
   `Execution complete: ... ok=133 fail=0` over links that were never wired
   (host run 13:56, `C:\DISTR\Logs\OpcTreeManager\opc-tree-manager.log`). The log is not
   evidence of connection.
2. **iconnect reconnect failure (cause UNCONFIRMED).** The replay uses a **live object
   reference** — `((IConnect)localPin.TreeObject).Connect(externalPin.TreeObject, 1, 1)`
   (`PlanExecutor.cs:389`). Direct links route through the vendor's name-based
   `ConnectByName` and survive the rebuild; iconnect is the only replay path passing an
   object reference, and it no-ops on a fresh pin. The correct mechanism is undetermined
   from code and is resolved by an isolated host matrix (one mechanism per run, below) rather
   than by shipping a guess.

The approach: land the verification fix (deferred-tolerant), run the isolated single-mechanism
matrix in the host to identify which connect mechanism actually wires the pin, then ship that
mechanism and remove the diagnostic selector.

## Context (from discovery)

### Connect path
`NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`:
- `Execute` (`PlanExecutor.cs:49-122`) ordering: `ApplyDesiredSpec` (builds/swaps `Items`) →
  `ResetScadaItemsMap` → `SynchWihSysTree` → `CommitStructuralChange`/`ApplyChange`
  (`PlanExecutor.cs:101-108`) → `ExecuteExpand` (the connects). **`ApplyChange` runs before
  the connects and is never called again after them.**
- `TryConnectLink` (`PlanExecutor.cs:367-424`): re-resolves `localPin`/`externalPin` via
  `_project.SafeItem<ITreePinHlp>(path)` **after** `ApplyChange` (so there is no stale-helper
  capture), branches on `link.LinkType` (`PlanExecutor.cs:387-397`), logs `Connected`
  unconditionally on no-exception (`PlanExecutor.cs:409`).
- `ConnectLinks` (`PlanExecutor.cs:347-365`): counts success/fail purely from the bool
  return, so a `false` already flows into the `fail` tally and the summary log format
  (`PlanExecutor.cs:110-119`) is unchanged. But today the verdict is produced **per-link,
  inline, before** `ExecuteExpand` returns (`PlanExecutor.cs:110,325-364`). Deferred-tolerant
  verification needs the verdict taken after an optional trailing commit, so the
  count-production path must be restructured two-phase (connect-all → verify-all → count) —
  see Task 2. The log line stays; the counting flow changes.

### Why direct works and iconnect does not (route asymmetry)
Per the vendor decompile of `ITreePinHlp.Connect` (quoted in
`Docs/known_issues/05-opc-command-pin-connect-overload.md`;
`MasterScada3Wiki/srcs/MasterSCADA.Common/MasterSCADA/Hlp/ITreePinHlp.cs:842-866`):
- `directPin` → `external.TreePin.ConnectByName(local.FullName,1,0)` (name-based).
- `directPout` → `local.TreePin.ConnectByName(external.FullName,1,0)` (name-based).
- `iconnect` → `((IConnect)local.TreeObject).Connect(external.TreeObject,1,1)` (object ref).

Direct links succeed on the fresh pin both as subject and as receiver, proving the fresh pin
is committed and name-resolvable. Only the object-reference route no-ops. Reverse-direction
is a weak fix hypothesis: LinkSwitcher (`LinkSwitcher/TreeOperations/LinkExecutor.cs:98`)
uses the **same** iconnect subject direction, and both `Connect` overloads collapse to the
identical `IConnect.Connect` for iconnect — there is no vendor direction handling to mirror.

### Name-based iconnect APIs (leading fix candidate — CONFIRMED in shipped DLLs)
The vendor creates iconnect wires programmatically by **name**, not by object reference. Both
APIs are verified present in `Resources/MasterSCADALib.dll` with the signatures below (RE via
`ilspycmd`, 2026-07-30); the vendor's own property page does
`DelayConnection(..., TRUE, ctIConnect)` to create an iconnect wire
(`MasterScada3Wiki/srcs/vavobj/vavobj.c:171110`):
- `MasterSCADALib.IProject.DelayConnection(string bstrPout, string bstrPin, int bConnect, EConnectionType connType)`
  — **fully statically typed reachability, no cast**: `PlanExecutor._project`
  (`IProjectHlp`, `PlanExecutor.cs:22`) → `_project.Project` (`IProject`) →
  `.DelayConnection(local.FullName, external.FullName, 1, EConnectionType.ctIConnect)`.
  (`IProjectHlp.Project` returns `IProject` — RE-confirmed; note the codebase's common
  `TreeItemHlp.Project` is `ITreeItemHlp.Project`, a different type, so it is not a precedent
  for this exact call.)
- `MasterSCADALib.IConnect.ConnectByString(string bstrItem, int bDual, int bCreateUndo)`
  — reached via a **COM QI cast**: `((IConnect)localPin.TreeObject).ConnectByString(external.FullName, 1, 1)`.
  `localPin.TreeObject` is typed `ITreeObject` which does not inherit `IConnect` in metadata;
  the cast is a runtime QueryInterface (the same cast the current object-ref path already
  relies on), so it compiles but is not compile-time guaranteed. The `DelayConnection` route
  is therefore the safer of the two.

**Two-enum footgun (confirmed, must not be conflated):** `GetConnections` takes
`EConnectionTypeMask` (`[Flags] uint`: `ctIConnect = 4`); `Connect`/`DelayConnection` take the
plain `EConnectionType` (`ctGenericPin = 0`, `ctGenericPout = 1`, `ctIConnect = 2`). Read-back
uses `EConnectionTypeMask.ctIConnect`; the connect/DelayConnection calls use
`EConnectionType.ctIConnect`. The current code already uses each correctly
(`PlanExecutor.cs:389` plain enum; read-back would use the mask) — the new code must keep
them distinct.

### Read-back
- `ProjectSubtreeDisconnector.cs:73` already reads
  `localPin.GetConnections(mask).Cast<ITreePinHlp>().ToList()` — the same call verifies a
  link exists after connect.
- **Read back under a combined mask, not the label's mask.** `EConnectionTypeMask` is
  `[Flags]`, so verify with `ctGenericPin | ctGenericPout | ctIConnect` and check the peer
  appears under **any** type. Rationale: KI-05 (`05-...md:46`) establishes that POUT-POUT
  Command wires (`ControlWord$`/`SelectPosWord$` ↔ `CMD.Результат`) are labeled `directPout`
  in the snapshot yet route through `IConnect` internally — so a reconnected Command wire may
  surface under `ctIConnect` rather than `ctGenericPout`. Verifying under the label's mask
  would **false-fail the ~24 KI-05 links that currently report green**. "Peer present under
  any connection type" is the correct predicate for "did the wire materialize" and sidesteps
  the label-vs-surface routing mismatch entirely.

### Dedup (audited — not the cause)
- `DedupByWire` keys on `(stripTrailingDollar(local), external)` (`LinkCollector.cs:115`).
  Multi-peer `Setpoint` (TemperatureSP + PowerSP) yields distinct keys → both survive.
- Kp/Ti/Td have **no `$` twin** (verified against `tree.json`; only ControlWord, Setpoint,
  TimeSP are PinPout pairs), so the `$`-half-collapse concern does not apply to the failing
  pins. It remains a latent smell for PinPout pins but is out of scope here.

### Test seam / references
- `LinkCollector` is tested via the `PinView` record-with-delegates seam
  (`LinkCollector.cs:172-184`, `Tests/OpcTreeManager/Unit/LinkCollectorTests.cs`).
- `Tests.csproj:38-55` references `MasterSCADA.Common`/`MasterSCADALib`, so `ITreePinHlp`,
  `EConnectionType`, `EConnectionTypeMask` are available to tests.

### Shared gap
`LinkSwitcher/TreeOperations/LinkExecutor.cs:93-116` counts non-throw as success too — it
carries the identical verification gap and must receive the same verify treatment.

## Development Approach

- **testing approach**: Regular (code first, then tests).
- Two phases separated by a host matrix run. Phase 1 lands verification + a temporary
  single-mechanism diagnostic selector and produces host data; Phase 2 ships the winning
  mechanism and removes the selector.
- Keep `directPin`/`directPout` forward connect calls byte-for-byte identical (no-arg
  overload) to avoid regressing the known-issue-05 fixes.
- Every code task adds/updates unit tests before the next. Tasks that are pure investigation
  (RE-confirm) or documentation carry no unit tests and say so.

## Testing Strategy

- **unit tests** cover the connect **orchestration** only — verify-then-count semantics,
  per-type mechanism selection, deferred-tolerant re-check — driven by fake pins.
  They do **not** and cannot test the real platform behavior: mocking `ITreePinHlp.Connect`
  would stub away the exact no-op under investigation. There is no CI regression test for
  "iconnect wires on a fresh node".
- **the only true gate** for the platform behavior is the manual host smoke run in
  Acceptance Evidence.
- **e2e tests**: none — no UI e2e harness exists for OPC tree operations.

## Acceptance Evidence

**Reproduction today (host, manual — confirmed 2026-07-30):**
1. `config.yaml` project `MBE` lists `TemperatureControllers` with `CH1..CH23`.
2. Delete `CH17..CH23` under
   `Система.АРМ.OPC UA Siemens.ServerInterfaces.MBE.TemperatureControllers`.
3. OpcTreeManager → Execute, leave runtime.
4. Observed: `Execution complete: shrink=0 expand=7; links total=133 ok=133 fail=0`, zero
   ERR/WRN, while `CHx.Kp/Ti/Td` (and the other iconnect pins) are unlinked in the tree.

**The discriminating experiment (host — one mechanism per run, NOT a single-pass ladder):**
A single pass that tries mechanisms in sequence per link **cannot** attribute the winner:
`DelayConnection` is deferred by design (materializes only on a later commit), so its
immediate read-back shows empty and the pass falls through; the trailing `ApplyChange` then
materializes whatever earlier rungs queued, and the win is unattributable — plus queued wires
from multiple attempts leave the pin double-wired or throw. Instead, each host run tests
**exactly one** mechanism (selected by a config constant, Task 3), isolated so nothing is
queued from another mechanism:

Per run: connect-all iconnect links with the selected mechanism → optional trailing
`opcFbItem.ApplyChange()` (a run dimension, on/off) → re-resolve pins → verify-all under the
combined mask → log per-link `iconnect probe mechanism={name} applyChange={bool} verified={bool}`.
Between runs the operator resets (delete `CH17..23` again) so each run starts clean.

Mechanism matrix, run in likelihood order; stop at the first that verifies:
1. `_project.Project.DelayConnection(local.FullName, external.FullName, 1, EConnectionType.ctIConnect)`
   — with trailing `ApplyChange` ON (deferred APIs need the commit). Try both argument orders
   across separate runs: the `(bstrPout, bstrPin)` role of local vs external is unknown; the
   vendor passes the command/pin side first at `vavobj.c:171110`.
2. `((IConnect)local.TreeObject).ConnectByString(external.FullName, 1, 1)` — ApplyChange ON.
3. forward object `local.Connect(external, ctIConnect)` (current behavior) — ApplyChange ON,
   to isolate whether a trailing commit alone fixes the existing mechanism.
4. reverse object `external.Connect(local, ctIConnect)` — ApplyChange ON.
5. forward object, ApplyChange OFF — the control run that reproduces the production no-op.

The first mechanism whose verify-all shows the peers is the fix. If none verify even with
ApplyChange, the cause is deeper than any connect API (native transaction/undo context) and
the honest `fail>0` from the verification is the starting signal for further investigation.

**Automatable (unit — gates the orchestration code only):**
```powershell
dotnet test NtoLib.sln --filter FullyQualifiedName~ConnectVerification
```
Asserts: empty read-back → counted `fail` + one ERR; non-empty read-back → success; the
re-check tolerates a link that only appears after the trailing `ApplyChange` (does not
report a false `fail`); a peer that surfaces under a **different** connection type than the
link's label (KI-05 Command case) still verifies (combined-mask predicate).

**Post-fix host smoke (Phase 2):** re-run the reproduction; the tree shows `CHx.Kp/Ti/Td`
linked and the log reports `fail=0`; save/reopen the project and confirm the links persist.

## Progress Tracking

- mark completed items `[x]` when done; ➕ for new tasks, ⚠️ for blockers.
- Phase 2 tasks are gated on the host matrix result — do not start them before that data
  exists; record the winning mechanism (+ ApplyChange on/off) inline when known.

## Solution Overview

- **Verification** (Task 2): a two-phase, combined-mask read-back gate. Connect-all →
  verify-all → count. A link is `ok` only if the peer appears under any connection type,
  otherwise `fail` with an ERR. Verification does **not** itself force a permanent trailing
  `ApplyChange` — the commit is a diagnostic dimension (Task 3) and ships in Phase 2 only if
  the winning mechanism needs it. This makes the log honest independent of the reconnect fix.
- **Diagnostic runs** (Task 3, temporary, Phase 1): behind a config constant selecting one
  mechanism + ApplyChange on/off, run the isolated matrix above; one mechanism per host run.
- **Permanent fix** (Phase 2): the single winning mechanism (with a trailing `ApplyChange`
  only if the matrix showed it necessary) replaces the current iconnect branch; the diagnostic
  selector is deleted.
- **Structure**: keep the permanent connect logic inline in `TryConnectLink` (the testable
  surface is thin; a `PinView`-style seam is used only where a unit test needs to drive the
  verify/count decision without a live project). Do not build a standing abstraction for the
  temporary diagnostic selector.

## Technical Details

- Combined-mask read-back (all link types, single predicate):
  `localPin.GetConnections(EConnectionTypeMask.ctGenericPin | EConnectionTypeMask.ctGenericPout | EConnectionTypeMask.ctIConnect).Cast<ITreePinHlp>().Any(p => string.Equals(p.FullName, externalPin.FullName, StringComparison.Ordinal))`.
  "Peer present under any type" — avoids the KI-05 label-vs-surface mismatch (see Read-back).
- Enum discipline: `GetConnections` takes `EConnectionTypeMask` (mask `ctIConnect = 4`);
  `Connect`/`DelayConnection` take plain `EConnectionType` (`ctIConnect = 2`). Do not conflate.
- Two-phase verdict: connect-all → (optional trailing `ApplyChange`, diagnostic-only in
  Phase 1) → re-resolve pins → verify-all → count. The final per-link verdict is the
  post-commit read-back, so a commit-based fix does not produce false failures.
- Counting: `ConnectLinks` turns a `false` verdict into a `fail`; log format unchanged.
- Logging: DBG per diagnostic run `iconnect probe mechanism={name} applyChange={bool} verified={bool}`;
  DBG on permanent-fix success `Connected {Local} ↔ {External} via {mechanism}`; ERR on
  verify failure `Connect {Local} ↔ {External} — reported connected but read-back shows no link`.

## What Goes Where

- **Implementation Steps**: RE-confirm, verification, diagnostic selector, permanent fix,
  LinkSwitcher port, docs — all in this repo.
- **Post-Completion**: the host matrix runs and the Phase-2 host smoke — require MasterSCADA,
  cannot run in CI.

## Implementation Steps

### Task 1: Confirm name-based iconnect APIs against the shipped assemblies — DONE

**Files:** none (investigation).

- [x] inspected `Resources/MasterSCADALib.dll` / `MasterSCADA.Common.dll` (ilspycmd, 2026-07-30)
- [x] `IProject.DelayConnection(string, string, int, EConnectionType)` present; reachable
      statically via `_project.Project.DelayConnection(...)` — see Context
- [x] `IConnect.ConnectByString(string, int, int)` present; reached via `(IConnect)pin.TreeObject`
      QI cast — see Context
- [x] exact signatures, reachability paths, and the `EConnectionType` vs `EConnectionTypeMask`
      distinction recorded inline in Context (above)
- [x] no tests (investigation task)

### Task 2: Add deferred-tolerant read-back verification to the connect path

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Create: `Tests/OpcTreeManager/Unit/ConnectVerificationTests.cs`
- Modify: `Tests/Tests.csproj` (add `<Compile Include>` for the new test file)

- [x] extract the verify+count decision into a testable helper (delegate-seam,
      `PinView`-style) taking `linkType`, a `connect` action, and a `readBackPeers` func
      (`ConnectProbe` record + `ConnectVerifier.ConnectAndVerify`, bottom of `PlanExecutor.cs`)
- [x] restructure `ExecuteExpand`/`ConnectLinks` (`PlanExecutor.cs:325-365`) two-phase:
      connect-all → re-resolve → verify-all → count, instead of the per-link inline verdict
      (`ExecuteExpand` builds probes, `ReadBackPeers` re-resolves the pin inside each probe)
- [x] verify every link type via the **combined-mask** predicate (peer present under any of
      `ctGenericPin|ctGenericPout|ctIConnect`); `ok` only if the peer appears, `fail` + ERR
      otherwise; keep the not-found guards (`PlanExecutor.cs:369-381` → `TryBuildProbe`)
- [x] do **not** add a permanent trailing `ApplyChange` here — that is a Task-3 diagnostic
      dimension and ships in Phase 2 only if the winning mechanism needs it (none added)
- [x] keep `directPin`/`directPout` forward calls byte-for-byte identical
      (`localPin.Connect(externalPin)` / `externalPin.Connect(localPin)` in `BuildConnectAction`)
- [x] write tests: empty read-back → fail + ERR; non-empty → ok; **peer under a different
      type than the label (KI-05 Command case) → ok** (combined-mask); verdict taken from the
      post-verify-all pass (`ConnectVerificationTests.cs`, 5 cases)
- [x] run tests — must pass before Task 3 (298 pass, 5 new ConnectVerification)

### Task 3: Add isolated single-mechanism iconnect diagnostic selector

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`

- [x] add a config constant selecting **one** iconnect mechanism per run
      (`DelayConnection` fwd/rev-arg-order, `ConnectByString`, object-forward, object-reverse)
      plus an ApplyChange on/off flag — **no cross-mechanism fall-through**, so nothing is
      queued from another mechanism (avoids double-wiring and unattributable deferred wins)
      (`IConnectMechanism` enum + `SelectedIConnectMechanism`/`DiagnosticApplyChange` consts at
      the top of `PlanExecutor`; iconnect routes through `BuildIConnectAction` — one vendor call
      per link, direct links untouched)
- [x] connect-all iconnect links with the selected mechanism → optional trailing
      `opcFbItem.ApplyChange()` → re-resolve → verify-all (combined mask) → log per link
      `iconnect probe mechanism={name} applyChange={bool} verified={bool}` (ApplyChange runs
      between connect-all and verify-all inside `ConnectVerifier.ConnectAndVerify`, resolved the
      same way as `CommitStructuralChange`; the DBG probe line is emitted in the verify pass)
- [x] mark the selector clearly as temporary diagnostic to be removed in Task 4 (bracketed
      "TEMPORARY DIAGNOSTIC (Task 3) — REMOVE … IN Task 4" banners on the enum/consts,
      `BuildIConnectAction`, `BuildDiagnosticApplyChange`, and the `ConnectAndVerify` params)
- [x] no unit tests (host-only diagnostic; the mechanisms are untestable without a live
      project) — noted in the selector banner comment
- [x] run existing tests — must still pass before the host matrix runs (298 pass, unchanged)

### Task 4: Implement the winning mechanism as the permanent iconnect fix

*(gated on the host matrix result — record winning mechanism + ApplyChange on/off here: __________)*

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/ConnectVerificationTests.cs`

- [ ] replace the iconnect branch (`PlanExecutor.cs:387-397`) with the winning mechanism;
      delete the Task-3 diagnostic selector entirely
- [ ] ship the trailing `ApplyChange` permanently only if the winning run required it
- [ ] keep the two-phase verification from Task 2 as the permanent gate
- [ ] write/adjust tests for the chosen mechanism's orchestration (mechanism invoked for
      iconnect, verified, counted)
- [ ] run tests — must pass before Task 5

### Task 5: Port verification to LinkSwitcher (mechanism only if it fails there)

**Files:**
- Modify: `NtoLib/LinkSwitcher/TreeOperations/LinkExecutor.cs`
- Create/Modify: LinkSwitcher unit test for the connect-verify path

- [ ] apply the same combined-mask read-back verification to `LinkExecutor`
      (`LinkExecutor.cs:93-116`) so its iconnect success is no longer non-throw-equals-success
- [ ] do **not** change LinkSwitcher's connect mechanism preemptively — it runs against
      already-materialized pins where object `Connect` works. Change the mechanism only if the
      new verification actually observes a failure there
- [ ] write tests for the verify/count path
- [ ] run tests — must pass before Task 6

### Task 6: Verify acceptance criteria
- [ ] verify the honest-logging defect: a simulated silent iconnect returns `false` and is
      counted (unit tests, Tasks 2/4)
- [ ] run full test suite: `dotnet test NtoLib.sln`
- [ ] run `dotnet format NtoLib.sln`
- [ ] diff-review that `directPin`/`directPout` connect calls are unchanged

### Task 7: Update documentation
- [ ] `Docs/opc-tree-manager.md` §8 (Логгирование): read-back verification, trailing
      `ApplyChange`, the winning iconnect mechanism, the new ERR line meaning
- [ ] `Docs/known_issues/05-opc-command-pin-connect-overload.md`: add the
      freshly-constructed-pin iconnect no-op, the route (name-based vs object-ref) root cause,
      and the shipped fix
- [ ] `CLAUDE.md`: only if a reusable pattern emerges (deferred-tolerant COM-edge verify) —
      otherwise skip
- [ ] move this plan to `Docs/plans/completed/`

## Post-Completion
*Manual / external — no checkboxes*

**Host matrix runs (between Task 3 and Task 4, requires MasterSCADA):**
- For each mechanism in the matrix order: delete `CH17..CH23`, set the Task-3 selector, Execute,
  read the `iconnect probe mechanism=…` log lines. Stop at the first mechanism whose verify-all
  shows the peers; record it (mechanism + ApplyChange on/off) in Task 4. Reset between runs so
  each starts clean. If **no** mechanism verifies even with ApplyChange, the cause is deeper
  than any connect API (native transaction/undo context); the honest `fail>0` from Task 2 is
  then the starting signal for further investigation.

**Phase-2 host smoke (after Task 4, requires MasterSCADA):**
- Re-run the reproduction; confirm `CHx.Kp/Ti/Td` linked, `fail=0`, and that links persist
  across save/reopen.
- Confirm `directPin`/`directPout` still report `ok` (guards against a wrong read-back side).

---

**Executed by exec:** (Phase 1 only — Tasks 2, 3; Tasks 4–7 gated on the host matrix)
- branch: `iconnect-reconnect-fix`

## Verify it yourself

**Phase 1 (shipped, repo-verifiable) — the honest-logging defect:**
- The verification code is proven by unit tests. Run:
  `dotnet test NtoLib.sln --filter FullyQualifiedName~ConnectVerification`
  10 tests pass. They prove: an empty read-back is counted `fail` with exactly one ERR
  (`EmptyReadBack_CountsFailAndLogsOneError`); a non-empty read-back with the wrong peer is
  `fail`, not a false `ok` (`WrongPeerInReadBack_...`); a peer wired only under `ctIConnect`
  still surfaces through the combined mask (`CombinedMaskReadBack_SurfacesPeerWiredOnlyUnderIConnect`);
  a read-back throw is one `fail`, not an aborted run
  (`ConnectThrowsAndReadBackEmpty_OneErrorOneDebug`); and a mixed batch tallies correctly
  (`MixedBatch_...`).
- Full suite green: `dotnet test NtoLib.sln` → 303 passed, 0 failed.
- The platform behavior (does iconnect actually wire) has NO repo-side repro — it manifests
  only in the MasterSCADA host. That is the host matrix below, not a CI check.

**Phase 2 (not yet shipped) — the reconnect wiring:**
- Requires the host matrix runs (see Post-Completion). No automated proof exists until the
  winning mechanism is identified in the host and shipped in Task 4.
