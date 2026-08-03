# OpcTreeManager — wire iconnect links via a second deferred pass

## Overview

`OpcTreeManager` reconnects a freshly-constructed OPC channel's external links from the
`tree.json` snapshot. Direct links (`directPin`/`directPout`) wire correctly; **iconnect**
links (Kp, Ti, Td, MaxOutput, MinOutput, Setpoint, SpeedSP, TempOffset, PowerOffset) do
not — confirmed by live-tree inspection after the run, no exception thrown.

**Root cause.** The host finalizes an OPC pin-space change **asynchronously**, on a
message-pump tick *after* `PlanExecutor.Execute` returns. During that window the freshly
constructed pins are mid-change:
- Direct links wire and survive; their connection is recorded in a form the host replays when
  it finalizes, and it only becomes enumerable afterwards (which is why in-`Execute` read-back
  is blind even for direct links that do wire).
- iconnect goes through `IConnect.Connect(obj, 1, 1)`, which mutates **object state** on a
  pin whose item the host then rebuilds during finalize → the edit is discarded. Silent, no
  exception.

The discriminating variable is **pin commit-state, not the API or direction**:
`NtoLib/LinkSwitcher/TreeOperations/LinkExecutor.cs:95-98` issues the identical
`targetPin.Connect(externalPin, ctIConnect)` (preceded by a `Disconnect`) with the same
OPC-side subject, but against **already-committed** pins, and it wires in production. Same
call, fresh pin fails, committed pin succeeds.

ASSUMPTION (decompile-inferred, not observed in NtoLib code): the survive-vs-discard split is
name-record replay (direct) vs object-state edit lost on rebuild (iconnect). The NtoLib direct
call is the no-arg `localPin.Connect(externalPin)` (`PlanExecutor.cs:486`); the vendor routing
underneath it is inferred from the `MasterScada3Wiki` decompile. The **empirical** facts the
plan rests on — direct wires / iconnect does not on fresh pins, iconnect wires on committed
pins (LinkSwitcher), in-pass read-back is blind — are observed, not inferred.

**Primary fix.** Run the iconnect connect in a **second deferred pass**: after the structural
rebuild + direct-link connects complete on one timer tick, run the iconnect connect on a
second one-shot timer tick after the host has finalized the pins, then `Connect(ctIConnect)`
as LinkSwitcher does. Because the pins are now committed, the connect sticks — and **all**
read-back verification (direct + iconnect) plus the summary move to that second pass, so the
log becomes honest.

**Fallback fix (contingency, not a peer).** `IProject.AddPasteConnection(...)` +
`ApplyPasteConnections()` — the vendor's paste-restore machinery, the only public connection
queue with a public **synchronous** flush. Carries two unverified arguments (`bPathFromPout`,
`hostFB` nullability), so it is a fallback if the deferred pass does not wire, not a co-equal
candidate.

Both are built behind the existing diagnostic selector (one host build tests both via a const
flip), validated by tree inspection, then the winner is shipped and the scaffolding removed.

## Context (from discovery)

### Execute flow — `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- `Execute` ordering (body `:87-158`): `ApplyDesiredSpec` (build/swap Items, `:128`) →
  `ResetScadaItemsMap` (`:137`) → `protocol.SynchWihSysTree()` (`:138`) →
  `CommitStructuralChange` = `opcFbItem.ApplyChange()` (`:140`) → `ExecuteExpand` (the
  connects, `:146`) → summary log (`:152-155`) → `return Result.Ok()` (`:157`). The whole
  method runs on **one** timer tick; the host's async finalize is the only thing that runs
  after it returns.
- `ExecuteExpand(context.Constructions, plan.OpcFbPath)` (`:146`) builds a `ConnectProbe` per
  link and runs connect+verify. Per-link-type action is chosen by
  `BuildIConnectAction`/`BuildConnectAction` via the temporary selector (`IConnectMechanism`
  enum `:38-54`, `SelectedIConnectMechanism`/`DiagnosticApplyChange` consts `:58-59`, end
  marker `:60`). The direct-link calls are `localPin.Connect(externalPin)` (directPin) and
  `externalPin.Connect(localPin)` (directPout) at `:486` — the no-arg overload.
- Read-back seam at the bottom of the file: `ConnectProbe`, `ConnectVerifier.ConnectAndVerify`,
  `CombinedMaskReadBack`, `ReadBackPeers`. This is the piece that is **blind in-pass**;
  moving its invocation to the second (post-finalize) pass makes it honest.
- Probe construction (`TryBuildProbe` → `_project.SafeItem<ITreePinHlp>`) is COM-bound; the
  partition decision "which probes connect in-pass vs deferred" must be split out COM-free to
  be testable (`TestApplyDesiredSpec` stops before `ExecuteExpand`, so no seam exists today).

### Deferred flow — `NtoLib/OpcTreeManager/TreeOperations/DeferredExecutor.cs`
- `DeferredExecutor.Post` (`:28-87`) creates a WinForms `Timer` (`RetryIntervalMs = 200`,
  `:16`; `MaxRetries = 100`, `:15`), waits for `IProjectHlp.InRuntime == false`, then on that
  tick calls `FinishTimer` (stop+dispose the timer, `:58`) and `executor.Execute(plan)` inside
  a `try/finally` that **disposes the logger and calls `onFinished()` exactly once** (`:79-83`);
  the timeout path does the same (`:100-104`). `onFinished` is
  `() => PendingPlan = null` (`Facade/OpcTreeManagerService.cs:108`) — if it never runs,
  `PendingPlan` stays non-null and blocks every future deferred run.
- Consequence for the second pass: the current timer is already disposed before `Execute`
  returns, so the second pass needs a **fresh** timer and a **distinct** tick handler (not a
  re-`Start()` of the existing lambda, which would re-run `Execute`), and its own guaranteed
  once-only release of logger/`onFinished`.

### Vendor evidence (decompile `C:\Users\admin\projects\MasterScada3Wiki\srcs\`)
- Property pages create every connection through the project-level deferred queue, never
  synchronously: `DelayConnection(<own pin>, <peer>, TRUE, ctIConnect)` then `ApplyPage()`
  (`vavobj/vavobj.c:171105-171117`). The queue flush lives in `MasterSCADA.exe` (absent from
  the decompile), so no code-reachable flush for `DelayConnection` is confirmed — this is why
  the earlier `DelayConnection + ApplyChange` host run did not wire.
- `IProject.AddPasteConnection(bstrPout, bstrPin, bPathFromPout, EConnectionType, hostFB)` +
  `IProject.ApplyPasteConnections()` (`MasterSCADALib/IProject.cs:262-271`). The managed
  wrapper hardcodes `ctGenericPin` (`MasterSCADA.Common/…/Hlp/IProjectHlp.cs:779`), so call
  `_project.Project.AddPasteConnection(...)` directly with `ctIConnect`. Wrapper arg shape:
  `AddPasteConnection(pout.FullName, pin.FullName, pathFromPout ? 1 : 0, <connType>, funkBlock)`
  and `ApplyPasteConnections()` (`IProjectHlp.cs:779,782`).
  ASSUMPTION: `bPathFromPout` value and whether `hostFB` may be null are unknown — permute in-host.
- `EParentType.ptFB` (`MasterSCADALib/EParentType.cs:16`) resolves a pin's host FB for the
  paste `hostFB` argument.

### Confirmed API reachability (RE of shipped `Resources/*.dll`, 2026-07-30)
- `_project` is `IProjectHlp` (`PlanExecutor.cs:22`); `_project.Project` returns `IProject`
  carrying `DelayConnection`, `AddPasteConnection`, `ApplyPasteConnections`.
- `EConnectionType.ctIConnect = 2` (plain, for Connect/paste); `EConnectionTypeMask.ctIConnect
  = 4` (mask, for GetConnections). Do not conflate.

### Superseded plan
`Docs/plans/completed/20260730-opctreemanager-iconnect-reconnect-fix.md` — its inline read-back
verification and same-pass mechanism matrix cannot work: read-back is blind in-pass, so no
same-pass mechanism can be verified in-code. Superseded by this plan.

## Development Approach

- **testing approach**: Regular (code first, then tests).
- Phased around one host build carrying both candidate mechanisms. The winner is chosen by
  host tree inspection, then shipped and the scaffolding removed.
- Keep `directPin`/`directPout` connect calls byte-for-byte identical throughout.
- Unit tests apply only to COM-free logic: the partition decision (which probes connect
  in-pass vs deferred, which get verified), the mode→action selection, and the existing
  combined-mask read-back predicate. The second-pass timer wiring and the vendor connect/flush
  are COM/pump-bound and are **not** unit-testable; their gate is the host smoke, stated per
  task rather than faked.

## Testing Strategy

- **unit tests**: the COM-free partition function (probes + mode → in-pass-connect /
  deferred-connect / verify sets); the mode→action mapping (direct types untouched by mode);
  the combined-mask read-back predicate (existing `ConnectVerification` tests). Mocking
  `ITreePinHlp.Connect` or the WinForms timer would stub away the behavior under test, so there
  is **no** CI regression test for "iconnect wires on a fresh pin" — host-only.
- **e2e tests**: none — no UI e2e harness for OPC tree operations.

## Acceptance Evidence

**Reproduction (host, manual — confirmed 2026-07-30):**
1. Start from `C:\Users\admin\MBE_Avangard2.zip` (clean), deploy the branch build (Rider
   "Deploy Debug" rebuilds from source), turn off CH17..23 in the OPC parameter list.
2. Run → Execute (`0→1`) → stop the project (leave runtime → deferred apply).
3. Observed: under `TemperatureControllers.CHx` the direct pins (StatusWord, ControlWord,
   ActualTemperature, ActualSP, ActualPower, TimeSP, TimeLeft) are linked; the iconnect pins
   (Kp, Ti, Td, MaxOutput, MinOutput, Setpoint, SpeedSP, TempOffset, PowerOffset) are **not**.

**The fix is validated by the tree, not the log,** until the second-pass mechanism lands
(read-back is blind in-pass). Host check after a run: expand `CH17` and one more (e.g. `CH22`)
and confirm all nine iconnect pins are now linked to `…Канал N.Настройки.*` / `…Setpoints.*`
/ `…Offset.*`, and direct pins remain linked.

**Automatable (unit — COM-free surface only):**
```powershell
dotnet test NtoLib.sln
```
Must stay green (303 today) and cover: the partition function routes iconnect probes to the
deferred set under a deferred mode and to the in-pass set under a non-deferred mode, always
routing direct probes in-pass; direct types unaffected by mode; the combined-mask read-back
predicate.

**Post-fix honest logging (once the deferred winner ships):** because verification + the
summary move to the second pass (post-finalize) for **all** link types, the log becomes
honest end to end. Host check: point one snapshot link (direct or iconnect) at a non-existent
peer → the second-pass summary reports `fail≥1` for it while the rest report `ok`.

## Progress Tracking
- mark items `[x]` when done; ➕ new tasks, ⚠️ blockers.
- Phase-2 "ship the winner" tasks are gated on the host experiment — record the winning mode
  inline when known.

## Solution Overview

- **Two-tick reconnect.** Tick N (`InRuntime==false`): structural rebuild + connect direct
  links + build (but do not connect) the iconnect probes; `Execute` **returns** a
  `DeferredConnectWork` value carrying the iconnect probes to connect and the full probe set to
  verify. Tick N+1 (fresh one-shot timer, after the host finalizes): connect the iconnect
  probes (`Connect(ctIConnect)`), then read-back-verify **all** probes and emit the summary.
- **PlanExecutor stays re-entrant.** It holds no cross-tick state; the deferred work is a
  return value that `DeferredExecutor` owns between ticks. Non-deferred (diagnostic) modes
  return empty deferred work and verify in-pass (blind) as today.
- **DeferredExecutor owns the two-tick lifecycle** and guarantees logger-dispose + `onFinished`
  fire exactly once on every terminal path: first-tick timeout/abort, completion with empty
  deferred work, second-tick completion, second-tick timeout/abort/exception.
- **Diagnostic-first.** Both mechanisms ship behind the `IConnectMechanism` selector as new
  modes (`DeferredObjectForward`, `PasteConnection`). One host build; flip the const; judge by
  tree. Then the winner becomes permanent and the selector + the blind in-pass verification are
  deleted.
- **Reuse** the existing `ConnectProbe`/`CombinedMaskReadBack` seam for the second-pass
  read-back. No standing abstraction for the scaffolding.

## Technical Details

- **DeferredConnectWork** (returned from `Execute`): `IReadOnlyList<ConnectProbe> IconnectToConnect`
  (empty for non-deferred modes) and `IReadOnlyList<ConnectProbe> AllToVerify` (the direct +
  iconnect probes to read-back in the second pass), plus the running shrink counts for the
  summary. For non-deferred modes both connect and verify happen in-pass and the returned work
  is empty (single-tick path unchanged).
- **Two-tick lifecycle** (`DeferredExecutor`):
  - First tick (existing InRuntime-wait loop, `:43-56`): on `InRuntime==false`, `FinishTimer`
    the first timer, run `Execute`. If the returned work is empty → dispose logger + call
    `onFinished` once (as today) and stop.
  - If the work is non-empty → arm a **new** `Timer` (fresh instance, `RetryIntervalMs`) with a
    **distinct** handler that (a) re-checks `InRuntime` with its own retry budget mirroring
    `MaxRetries`/timeout and aborts (dispose+`onFinished` once) on timeout, exactly like
    `AbortWithTimeout` (`:89-105`); (b) on `InRuntime==false`, `FinishTimer`s the second timer
    and runs `executor.ConnectAndVerifyDeferred(work)` inside a `try/finally` that disposes the
    logger + calls `onFinished` **exactly once**; a `catch` logs and the `finally` still
    releases. The second handler never calls `Execute`.
  - Invariant to enforce and test-by-inspection: on every path out of `Post` — first-tick
    abort, empty-work completion, second-tick completion, second-tick abort, any exception —
    `logger.Dispose()` and `onFinished()` run exactly once. A helper that both disposes and
    calls `onFinished`, invoked from each terminal branch and guarded against re-entry, is the
    minimal shape.
- **Second-pass connect**: re-resolve `localPin`/`externalPin` via `_project.SafeItem<ITreePinHlp>`
  on tick N+1 (fresh helpers post-finalize), then mirror LinkSwitcher (`LinkExecutor.cs:95-98`):
  `Disconnect` then `localPin.Connect(externalPin, EConnectionType.ctIConnect)` — the
  `Disconnect` is a no-op on a fresh pin but matches the proven path. Then read-back-verify.
- **Paste mode** (contingency): per iconnect link
  `_project.Project.AddPasteConnection(localPin.FullName, externalPin.FullName, bPathFromPout,
  EConnectionType.ctIConnect, hostFB)` with `hostFB = externalPin.GetParent(EParentType.ptFB)`,
  then one `_project.Project.ApplyPasteConnections()`. Runs in-pass (synchronous flush), guard
  each call, emit the probe log line. ASSUMPTION on `bPathFromPout`/null-`hostFB` — permute in-host.
- **Enum discipline**: `EConnectionType.ctIConnect` for connect/paste; combined
  `EConnectionTypeMask` for read-back.
- **Direct links**: unchanged in `ExecuteExpand`; connect in-pass on tick N.

## What Goes Where
- **Implementation Steps**: partition extraction, the two candidate modes, the DeferredExecutor
  two-tick change, unit tests, (Phase 2) ship-winner cleanup, docs — all in this repo.
- **Post-Completion**: the host experiment(s) and the ship-winner host smoke — require
  MasterSCADA, cannot run in CI.

## Implementation Steps

### Task 1: Extract a COM-free connect/verify partition and add the deferred-pass plumbing

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/DeferredExecutor.cs`
- Create: `Tests/OpcTreeManager/Unit/DeferredIconnectPartitionTests.cs`

- [x] add `DeferredObjectForward` to `IConnectMechanism` (so there is a deferred mode to branch
      on) and a `DeferredConnectWork` return type
- [x] extract a **COM-free** partition function: given the already-built `ConnectProbe`s + the
      selected mode → `(inPassConnect, deferredConnect, allToVerify)`. Under a deferred mode,
      iconnect probes go to `deferredConnect`, direct to `inPassConnect`, both to `allToVerify`;
      under a non-deferred mode, all go to `inPassConnect`/`allToVerify` and `deferredConnect`
      is empty
- [x] make `ExecuteExpand`/`Execute` connect the `inPassConnect` set, then: if
      `deferredConnect` is empty, verify `allToVerify` in-pass (unchanged) and return empty
      work; else return `DeferredConnectWork` (deferredConnect + allToVerify + shrink counts)
      without verifying in-pass; add `ConnectAndVerifyDeferred(work)` that connects the deferred
      set and verifies `allToVerify` and logs the summary
- [x] change `DeferredExecutor.Post` to the two-tick lifecycle in Technical Details: fresh
      second timer, distinct handler, second-tick `InRuntime` guard + timeout/abort, and a
      once-only logger-dispose+`onFinished` release covering every terminal path
- [x] write tests for the partition function (deferred vs non-deferred routing; direct always
      in-pass; verify set is direct+iconnect) — COM-free. Do **not** unit-test the COM connect
      or the timer (host-only; note it in a comment)
- [x] run tests — must pass before Task 2

### Task 2: Add the `PasteConnection` contingency mode

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/DeferredIconnectPartitionTests.cs`

- [x] add `PasteConnection` to `IConnectMechanism`; in the iconnect action path, per link
      `AddPasteConnection(local.FullName, external.FullName, bPathFromPout, ctIConnect, hostFB)`
      (`hostFB = externalPin.GetParent(EParentType.ptFB)`), then one `ApplyPasteConnections()`
      after connect-all; in-pass (synchronous), guard each call, emit the probe log line
- [x] keep the selector marked TEMPORARY DIAGNOSTIC; default the const to current production
      behavior so a normal build is unchanged
- [x] write tests: `PasteConnection` selects the paste action for iconnect and leaves direct
      types untouched (COM-free selection assertion). No unit test of the real paste COM call
- [x] run tests — must pass before the host experiment

### Task 3: Ship the winning mechanism; remove the selector and the blind in-pass verification

*(gated on the host experiment — record winning mode here: __________)*

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/DeferredExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/ConnectVerificationTests.cs`

- [ ] make the winning mechanism the permanent iconnect path; delete `IConnectMechanism` and
      the losing branches
- [ ] if the winner is the deferred pass: keep the two-tick `DeferredExecutor` path and the
      second-pass verify-all + summary as the permanent honest logging (direct + iconnect both
      verified post-finalize); remove the in-pass blind verification entirely
- [ ] if the winner is `PasteConnection`: keep the in-pass path; keep the read-back only if it
      verifies on committed pins after `ApplyPasteConnections`, otherwise log without a verdict
      rather than a blind false one
- [ ] adjust/trim `ConnectVerificationTests.cs` to the surviving seam; keep the combined-mask
      predicate tests
- [ ] run tests — must pass before Task 4

### Task 4: Port honest read-back to LinkSwitcher

**Files:**
- Modify: `NtoLib/LinkSwitcher/TreeOperations/LinkExecutor.cs`
- Create/Modify: a LinkSwitcher unit test for the surviving verify seam

- [ ] LinkSwitcher runs against already-committed pins, so its `Disconnect`-then-
      `Connect(ctIConnect)` (`LinkExecutor.cs:95-98`) already works — do NOT change its
      mechanism. Apply the same combined-mask read-back verification (`:93-116`) so success is
      no longer non-throw-equals-success; change the mechanism only if a verification failure is
      actually observed there
- [ ] write tests for the verify/count path
- [ ] run tests — must pass before Task 5

### Task 5: Verify acceptance criteria
- [ ] run full suite: `dotnet test NtoLib.sln` (all green)
- [ ] run `dotnet format NtoLib.sln`
- [ ] diff-review that `directPin`/`directPout` connect calls are byte-for-byte unchanged

### Task 6: Update documentation
- [ ] `Docs/opc-tree-manager.md`: the async-finalize behavior, the second-pass iconnect
      mechanism, and that logging is now honest because verification runs post-finalize for all
      link types
- [ ] `Docs/known_issues/`: add an entry — "connections made in the same deferred pass as a
      structural change are not committed or enumerable until the host finalizes on a later
      tick" — cross-referencing `05-opc-command-pin-connect-overload.md`
- [ ] add a one-line "superseded by 20260730-opctreemanager-iconnect-deferred-connect.md" note
      at the top of `Docs/plans/completed/20260730-opctreemanager-iconnect-reconnect-fix.md`; do not
      delete it
- [ ] move this plan to `Docs/plans/completed/`

## Post-Completion
*Manual / external — no checkboxes*

**Host experiment (between Task 2 and Task 3, requires MasterSCADA):**
- Deploy the Task-2 build, selector = `DeferredObjectForward`. Run the reproduction; check the
  tree for the iconnect pins.
  - Wired and persisting → ship the deferred pass (Task 3).
  - Not wired → flip the const to `PasteConnection`, redeploy, re-run; check the tree. If not
    wired, permute the paste `bPathFromPout`/`hostFB` and then try `ConnectByString` before
    concluding.
  - **Wired but lost after save / project reopen** → the connection did not survive the
    passport-apply stage. Do not ship; escalate: prefer `PasteConnection` (its
    `ApplyPasteConnections` is the vendor's own persist-through-apply path) and, if that also
    fails to persist, investigate hooking the OPC group's own post-apply reconnect rather than
    connecting from our timer at all.

**Ship-winner host smoke (after Task 3, requires MasterSCADA):**
- Re-run the reproduction; confirm all nine iconnect pins wire, **persist across save/reopen**,
  direct pins remain wired, and the honest second-pass log reports `fail=0`.

---

**Executed by exec:** (Phase 1 only — Tasks 1, 2; Tasks 3–6 gated on the host experiment)
- branch: `iconnect-reconnect-fix`

## Verify it yourself

**Phase 1 (shipped, repo-verifiable):**
- Build + unit suite: `dotnet build NtoLib.sln` (0/0) and `dotnet test NtoLib.sln`
  (312 passed). The COM-free partition is proven by
  `Tests/OpcTreeManager/Unit/DeferredIconnectPartitionTests.cs`: iconnect routes to the
  deferred set only under `DeferredObjectForward`; `PasteConnection` and the other modes stay
  in-pass; direct links always in-pass.
- The two-tick `DeferredExecutor` lifecycle (once-only `Release` on every terminal path,
  independent tick-2 retry budget, no `Execute` re-run) is verified by code review, not CI —
  the WinForms timer + vendor COM connect cannot be unit-tested without stubbing away the
  behavior under test.

**Phase 2 (not yet shipped) — does iconnect actually wire:**
- No repo-side proof exists; it manifests only in MasterSCADA. Run the host experiment
  (Post-Completion above): deploy with the selector at `DeferredObjectForward`, delete
  CH17..23, Execute, and inspect the tree for the nine iconnect pins. The winning mechanism is
  shipped in Task 3, after which its second-pass read-back makes the log honest.
