# OpcTreeManager — wire iconnect by reconstructing with fresh ids

## Overview

`OpcTreeManager` reconstructs a deleted OPC channel (`TemperatureControllers.CH17..CH23`) from
the `tree.json` snapshot and reconnects its links. Direct links wire; **iconnect** links
(Kp/Ti/Td/MaxOutput/MinOutput/Setpoint/SpeedSP/TempOffset/PowerOffset) do not.

**Root cause — id-path collision (disasm-proven).** MasterSCADA's "Duplicate connection" skip in
`IConnectImpl::Connect` keys on a **numeric id-path** (each pin's numeric id formatted `%d`, the
by-name flag off), not the pin name. Our reconstruction copies the snapshot pin `Id`
**verbatim** (`OpcScadaItemDto.cs:60` / `:143`). The vendor's own OPC import **never reuses
ids** — `GetNextId() = ScadaRootNode.CalcMaxId() + 2` (`OpcUaProtocol.cs:248`). When CH17..23 are
turned off in the OPC parameter list they are removed **outside** NtoLib's `DisconnectSubtree`
(they reach only the construct branch of `ApplyDesiredSpec`, never shrink/disconnect), so the
**FB-side pin keeps a dangling connection descriptor baked with the old id-path**. Reconstructing
with the same id makes the fresh connection's id-path identical to that stale descriptor → the
connect matches it as a duplicate and **silently skips the append** (returns S_OK — which is why
the vendor's own restore dialog no-op'd with an empty "cannot-restore" list). Direct links are
immune: `ConnectByName` is single-source (vtable slot 0x84), builds no two-sided id-path, runs no
duplicate check.

**Primary fix.** Reconstruct with **fresh ids**: shift every reconstructed item's id by a uniform
**even** delta seeded from `protocol.ScadaRootNode.CalcMaxId() + 2` — matching what the vendor's
own import does. Fresh ids give the new connection a new id-path that no longer collides with the
stale descriptor, so the name-based connect appends. The connect stays name-based
(`DelayConnection(..., ctIConnect)` + `ApplyChange`), because the reconstructed pin's COM wrapper
is still poisoned (object `Connect`/`Disconnect` throw `ArgumentOutOfRange`) — name-based calls
never touch it.

**Fallback.** One residual unknown: whether the id-path field is exactly the id we set via the
DTO, or a **derived internal handle**. If derived, shifting our id would not change the id-path and
the collision would persist. The fallback clears the stale descriptor directly, from the
**healthy FB side**: `((IConnect)externalPin.TreeObject).DisconnectByString(localPin.FullName, 1, 1)`
before the name-based connect. Built as a second selectable connect mode so one host build settles
which is needed.

Both ship behind the existing diagnostic selector; validated by **tree inspection** (in-code
`GetConnections` read-back is proven blind — connections are not queryable until the host finalizes
after `Execute` returns; the log stays `ok=0` regardless). Then the winner is made permanent and
the scaffolding removed.

## Context (from discovery)

### Reconstruction / id path — `NtoLib/OpcTreeManager`
- `OpcScadaItemDto` (`Entities/OpcScadaItemDto.cs`): `FromScadaItem` copies `Id = item.Id`
  (`:60`); `ToScadaItem` (`:76`) and `ToScadaItemPruned(spec)` (`:95`) recurse building
  `OpcUaScadaItem`s via `BuildWithoutChildren` (`:115`), which sets `Id = Id` (`:143`). **This is
  where the remap lands** — the only place the snapshot id becomes the live item id. Both recurse
  into children (`ToScadaItem` at `:82`, `ToScadaItemPruned` at `:109`), so the offset must be
  forwarded on those recursive calls too.
- `PlanExecutor.ApplyDesiredSpec` construct branch: the `constructed = childDto.ToScadaItemPruned(spec)`
  call is at `TreeOperations/PlanExecutor.cs:460` (inside the `foreach (var spec in desired)` loop),
  adds it to `newItems`, records the reconnect `Construction` (links filtered by path). CH17..23
  land here (construct), never in the shrink/disconnect branch (`DisconnectSubtree` call `:415`) —
  this is why the FB-side descriptor is never cleared.
- `Execute` binds the live `protocol` (`:132`) and already reads `protocol.ScadaRootNode` (in
  `ResetScadaItemsMap`). `ApplyDesiredSpec` receives a `RebuildContext` (threaded through the
  recursion) — the natural carrier for the computed id delta.
- The vendor derives the `$Pin` as `id-1` at `AddPinToTree`, so the fix must **preserve each id's
  parity** (a Pout at an even id has its `$Pin` at the odd `id-1`). A **uniform even delta**
  preserves parity for every item, keeps the `(id, id-1)` gap, and lands every new id above the
  existing range. Invariant the remap relies on (assert in the test): snapshot ids are
  **non-negative** and **mutually unique** (true for ids captured from a live tree).
- ASSUMPTION: `OpcUaScadaItem.CalcMaxId()` is accessible from NtoLib (the vendor calls
  `this.ScadaRootNode.CalcMaxId()` at `OpcUaProtocol.cs:248`). Task 1 confirms; if it is not
  public, compute the max id by walking `protocol.ScadaRootNode` in NtoLib.

### Connect path
- `PlanExecutor.BuildIConnectAction` (`TreeOperations/PlanExecutor.cs:703`, the `IConnectMechanism`
  switch): the diagnostic selector. `DelayConnectionForward` does
  `_project.Project.DelayConnection(local.FullName, external.FullName, 1, ctIConnect)`. Consts
  `SelectedIConnectMechanism` (`:78`) / `DiagnosticApplyChange` (`:79`). `_project` is `IProjectHlp`;
  `_project.Project` is `IProject`. `EConnectionType`/`IConnect` in `MasterSCADALib`. NOTE: a dead
  mode `DelayConnectionDisconnectFirst` (`:49`) already exists — it is failed attempt #6 (name-based
  `DelayConnection(0)`/`(1)`); the fallback below is a DIFFERENT mechanism (object `DisconnectByString`
  on the FB side) and Task 3 deletes #6.
- The reconstructed OPC pin's RCW is poisoned → object connect/disconnect on it throw. The FB-side
  (external) pin's RCW is **healthy** — proven in-repo: `directPout` links already do the object
  call `externalPin.Connect(localPin)` (`:691`) and those links wire. So
  `((IConnect)externalPin.TreeObject).DisconnectByString(...)` on the external pin is safe (the
  fallback).

### Established facts (see `Docs/plans/iconnect-investigation-log.md`)
- In-code read-back is blind; validate by tree only.
- All 6 prior connect mechanisms failed for the id-collision reason; full disasm in
  `scratchpad/fable-solve.md` / `fable-conclusion.md`.

## Development Approach

- **testing approach**: Regular (code first, then tests).
- The **id-remap is COM-free and unit-testable** (given a DTO tree + a delta, assert the produced
  item ids). The connect mechanisms and the real wiring are host-only.
- Keep direct-link connects byte-for-byte unchanged. The remap must not change `Name`, `NodeId`,
  or structure — only the numeric `Id`, by a uniform even delta.
- Phased: land the remap + both connect modes (one host build), run the tree experiment, ship the
  winner, remove scaffolding.

## Testing Strategy

- **unit tests**: the id-remap — even delta computed from a seed; every item (root + descendants)
  shifted by the same delta; delta even; all new ids > seed; structure/names/NodeIds unchanged;
  pruning still honored. And the selector routing for the new fallback mode. COM-free.
- No CI test for the actual wiring (host-only, read-back blind).
- **e2e**: none.

## Acceptance Evidence

**Reproduction (host, manual — confirmed):** fresh project from `C:\Users\admin\MBE_Avangard2.zip`,
turn off CH17..23 in the OPC parameter list, run → Execute → stop. Today: iconnect pins
(Kp/Ti/Td/…) are unlinked in the tree; direct pins are linked.

**Fix validated by the tree** (read-back blind): after a run with the fresh-id remap +
`DelayConnectionForward`, expand `CH17` and `CH22` and confirm all nine iconnect pins are now
linked to `…Канал N.Настройки.*` / `…Setpoints.*` / `…Offset.*`, direct pins still linked. Also
confirm the **FB-side** pin (`MBE_Locomotive…Kp`) carries a single valid link and **no stale/
duplicate descriptor** — the fresh-ids-only path leaves the old dangling descriptor in place
(orphaned at the now-unused old id-path); an orphan showing as a phantom/duplicate link on
save-reopen is a signal to prefer the FB-side-disconnect mode even if the plain path wired.

**Automatable (unit — the remap only):**
```powershell
dotnet test NtoLib.sln
```
Must stay green (312 today) plus new remap tests: a DTO tree with ids `{e, e-1, e+2, …}` remapped
with seed `S` → every id shifted by one even delta `≥ S+? `, all `> S`, names/NodeIds/structure
identical, delta even.

## Progress Tracking
- mark items `[x]`; ➕ new, ⚠️ blockers. Record the winning connect mode inline after the host run.

## Solution Overview

- **Fresh-id remap (reconstruction).** In `Execute`, once, compute
  `delta = round-up-to-even(protocol.ScadaRootNode.CalcMaxId() + 2)`; carry it on `RebuildContext`.
  In the construct branch, call `childDto.ToScadaItemPruned(spec, delta)`; `ToScadaItem`/
  `BuildWithoutChildren` set `Id = Id + delta`. Uniform even delta → structure + `(id,id-1)`
  pairing preserved, all ids above the existing range. Snapshot links are by name, so unaffected.
- **Connect stays name-based.** `DelayConnectionForward` + `DiagnosticApplyChange=true`.
- **Fallback mode.** `DelayConnectionFbDisconnectFirst`: `((IConnect)externalPin.TreeObject)
  .DisconnectByString(localPin.FullName, 1, 1)` then the name-based `DelayConnection`. FB side is
  healthy, so no throw. Selectable via the const.
- **After the host run:** make the winning combination permanent (fresh-ids always on; the winning
  connect), delete the diagnostic selector, the dead modes, the deferred scaffolding, and the blind
  inline read-back verification.

## Technical Details

- Even delta: `var maxId = protocol.ScadaRootNode.CalcMaxId(); var delta = maxId + 2; if ((delta &
  1) != 0) delta++;` — even, `≥ maxId + 2`. Adding this constant to every id: preserves each id's
  parity (even delta), preserves mutual uniqueness (constant shift, given the unique-snapshot-ids
  precondition), and puts every new id `> maxId` (given non-negative snapshot ids). No snapshot
  regeneration — `tree.json` keeps verbatim ids; the offset is applied only at reconstruction.
- Threading: add an `int idOffset` parameter to `OpcScadaItemDto.ToScadaItem` /
  `ToScadaItemPruned` / `BuildWithoutChildren` (default 0 keeps existing callers/tests working);
  `BuildWithoutChildren` sets `Id = Id + idOffset`. Store the delta on `RebuildContext`; pass it at
  the single construct call site.
- Only `Id` changes. `Name`, `NodeId`, `PinValueType`, `DataType`, subscription/deadband fields,
  and `Items` structure are untouched. The vendor rebuilds its item map on `SynchWihSysTree`; the
  `$Pin` (`id-1`) is derived from the new even `Id`.
- Fallback disconnect uses the **external** (FB) pin as subject — the healthy RCW — distinct from
  attempt #5 (poisoned OPC pin subject → threw) and #6 (`DelayConnection(0)` → didn't clear).

## What Goes Where
- **Implementation Steps**: remap + fallback mode + unit tests + (post-host) ship + docs — all repo.
- **Post-Completion**: the host experiment and ship-verify smoke — require MasterSCADA.

## Implementation Steps

### Task 1: Reconstruct with fresh even ids (id remap)

**Files:**
- Modify: `NtoLib/OpcTreeManager/Entities/OpcScadaItemDto.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Create: `Tests/OpcTreeManager/Unit/OpcScadaItemDtoRemapTests.cs`

- [x] confirmed `OpcUaScadaItem.CalcMaxId()`/`Id`/`Items` are public (`Data/OpcUaScadaItem.cs:98/70/68`) — callable from NtoLib, no walk needed
- [x] added `int idOffset` to `OpcScadaItemDto.ToScadaItem`/`ToScadaItemPruned`/`BuildWithoutChildren`; `BuildWithoutChildren` sets `Id = Id + idOffset`; recursion forwards the offset
- [x] `PlanExecutor.Execute` computes `NextEvenIdOffset(protocol.ScadaRootNode.CalcMaxId())` onto `RebuildContext.IdOffset`; construct branch passes it to `ToScadaItemPruned` (`:460`)
- [x] direct-link connects and the shrink/preserve branches unchanged; only constructed items remapped
- [x] tests in `OpcScadaItemDtoRemapTests.cs` (own `Node(name,id,children)` helper): uniform even shift, delta even, all `> maxId`, parity preserved, names/NodeIds/structure identical, pruning honored, offset 0 = identity, non-negative+unique precondition asserted
- [x] tests pass (321, +9)

### Task 2: Add the FB-side-disconnect fallback mode and select the host experiment

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/DeferredIconnectPartitionTests.cs`

- [x] added `DelayConnectionFbDisconnectFirst` (enum comment notes it supersedes dead #6; object
      `DisconnectByString` on the healthy FB pin then name-based `DelayConnection`); guarded by
      `ConnectAll`'s per-probe try/catch
- [x] set `SelectedIConnectMechanism = DelayConnectionForward`, `DiagnosticApplyChange = true`
- [x] selector kept marked TEMPORARY DIAGNOSTIC
- [x] tests: `DelayConnectionFbDisconnectFirst` classified non-deferred, routes iconnect+direct
      in-pass; direct types unaffected
- [x] tests pass (323)

### Task 3: Ship the winning combination; remove the diagnostic scaffolding

*(gated on the host experiment — record what wired here: __________)*

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/DeferredExecutor.cs`
- Modify: `Tests/OpcTreeManager/Unit/*`

- [ ] keep the fresh-id remap permanently (it is the correct behavior — matches the vendor)
- [ ] make the winning connect the permanent iconnect path: fresh-ids + `DelayConnection` if that
      wired; else fresh-ids + FB-side-disconnect. Delete `IConnectMechanism` and the losing modes
- [ ] remove the deferred two-tick scaffolding and the inline read-back verification (proven blind —
      it cannot verify in-pass); log the connect attempt without a false verdict, or drop the
      per-link verdict entirely
- [ ] adjust tests to the surviving code; keep the remap tests
- [ ] run tests — must pass before Task 4

### Task 4: Verify and document
- [ ] full suite `dotnet test NtoLib.sln`; `dotnet format NtoLib.sln`; diff-review direct connects
      unchanged
- [ ] `Docs/known_issues/`: new entry — "reconstructing an OPC pin with the snapshot's original id
      collides with the FB-side stale descriptor's id-path (duplicate-skip); reconstruct with fresh
      ids as the vendor's import does"
- [ ] `Docs/opc-tree-manager.md`: note fresh-id reconstruction and that iconnect now wires; logging
      stays tree-validated (in-code read-back is blind)
- [ ] mark the two superseded plans (`20260730-opctreemanager-iconnect-reconnect-fix.md`,
      `20260730-opctreemanager-iconnect-deferred-connect.md`) superseded at their top; append the
      RESOLVED outcome to `Docs/plans/iconnect-investigation-log.md`
- [ ] move this plan to `Docs/plans/completed/`

## Post-Completion
*Manual / external — no checkboxes*

**Host experiment (between Task 2 and Task 3, requires MasterSCADA):**
- Deploy the Task-2 build (fresh-ids + `DelayConnectionForward`). Run the reproduction; check the
  tree for the nine iconnect pins AND the FB-side pin's descriptor cleanliness (see Acceptance).
  - Wired and no FB-side orphan → fresh-ids alone is the fix (the id-path uses our id). Ship it.
  - Wired but a stale/duplicate descriptor lingers on the FB side → prefer the FB-side-disconnect
    mode (it clears the orphan); run it, confirm clean, ship that.
  - Not wired → the id-path field is a derived handle. Flip
    `SelectedIConnectMechanism = DelayConnectionFbDisconnectFirst` (fresh-ids stay on), redeploy,
    re-run. Wired → ship fresh-ids + FB-side-disconnect. If still not wired, capture the fresh log
    and re-open the investigation with the new evidence (do not guess further mechanisms).

**Ship-verify smoke (after Task 3, requires MasterSCADA):**
- Re-run the reproduction; confirm all nine iconnect pins wire, persist across save/reopen, direct
  pins remain wired.
