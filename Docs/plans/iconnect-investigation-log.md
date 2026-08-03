# iconnect reconnect — investigation log (living doc, do not circle)

Records every mechanism tried, its host result, and WHY it failed, so we never repeat an
experiment. Append new attempts; never delete. Companion to plan
`completed/20260730-opctreemanager-iconnect-deferred-connect.md`.

## Problem

`OpcTreeManager` reconstructs a deleted OPC channel (`TemperatureControllers.CH17..CH23`) from
the `tree.json` snapshot, then reconnects its external links. **Direct** links
(`directPin`/`directPout`: StatusWord, ControlWord, ActualTemperature, …) wire. **iconnect**
links (Kp, Ti, Td, MaxOutput, MinOutput, Setpoint, SpeedSP, TempOffset, PowerOffset) do NOT —
confirmed by live tree inspection, host repro from `C:\Users\admin\MBE_Avangard2.zip`.

## Hard constraints (proven)

- **In-code read-back is BLIND**, even in a deferred second tick post-`InRuntime=false`:
  `GetConnections` returns empty for direct links that ARE connected in the tree. So the log's
  `ok/fail` is meaningless; **validate by tree only**.
- The reconstructed OPC pin is in a bad state after `CommitStructuralChange`:
  - its COM wrapper (`pin.TreeObject` RCW) is **poisoned** — any object method (`Connect`,
    `Disconnect`) throws `ArgumentOutOfRangeException` ("Значение не попадает в ожидаемый
    диапазон"). This is a .NET interop marshaling failure on a stale RCW, not a native reject.
  - it carries a **stale connection descriptor** from the deleted subtree, so name-based
    connects hit a "Duplicate connection" short-circuit and silently append nothing.

## Attempt matrix (mechanism → host result → why)

| # | Mechanism (selector) | Host result (tree) | Log signal | Why it failed |
|---|---|---|---|---|
| 1 | `ObjectForward` `localPin.Connect(ext, ctIConnect)` | not wired | fresh: silent; committed: throws 70× | object path on poisoned RCW |
| 2 | `DelayConnectionForward` + ApplyChange | not wired | silent, no throw | name-based, but stale-descriptor duplicate-skip |
| 3 | `DeferredObjectForward` (2nd tick, object Connect) | not wired | throws 70× on committed pins | object path throws even post-finalize |
| 4 | `PasteConnection` (`AddPasteConnection`+`ApplyPasteConnections`) | not wired | pops modal "Восстановление внешних связей" dialog; report "cannot-restore" list EMPTY (all resolved); Restore no-op | restore's `ConnectByString` returns S_OK but duplicate-skips the append |
| 5 | `DelayConnectionDisconnectFirst` (object `Disconnect` then DelayConnection) | not wired | throws 70× | object `Disconnect` ALSO throws on poisoned RCW (fable had assumed it wouldn't) |
| 6 | `DelayConnectionDisconnectFirst` (name-based `DelayConnection(0)` then `(1)`) | not wired | silent, ZERO throws | name-based disconnect did NOT clear the stale descriptor; connect duplicate-skipped |

**Conclusion from the matrix:** the entire connection-API surface is exhausted. Object calls
throw (poisoned RCW); every name-based connect silently duplicate-skips (stale descriptor);
paste is dialog-bound and also duplicate-skips. **No connect API can wire a corrupt pin.**

## Native primitives (fable disasm, HIGH confidence — full write-up in
`scratchpad/fable-conclusion.md`, dumps in `scratchpad/*_*.txt`)

| Managed call | Native | Route | Throws? |
|---|---|---|---|
| direct `ConnectByName(name,1,0)` | vtable slot 0x84 | by NAME | no — works |
| iconnect object `Connect(obj,1,1)` | `IConnectImpl::Connect` 0x46e370 | live RCW ptr | YES on committed pin |
| `ConnectByString(name,1,1)` | 0x46f640 → resolves peer by name → 0x46e370 | by NAME | no |
| `DelayConnection(pout,pin,1,ctIConnect)` | queued, commit-materialized | by NAME | no |
| `AddConnectionRecord(pout,pin,b)` | 0x5ec940 → `ConnectByName` slot 0x84 | by NAME, **direct-only** | no |
| `AddPasteConnection`+`ApplyPasteConnections` | 0x5f5140 → restore DIALOG → per-record `ConnectByString` | by NAME, interactive | no |

- The S_OK-but-no-append is the **"Duplicate connection"** flag at `0x46e529–0x46e581` skipping
  the list append at `0x46e5e6` (the pin's existing connection list `[edi+8]`/count `[edi+0x10]`
  already contains a match). Either a residual half-descriptor from the deleted subtree, or the
  descriptor's baked-in pin id `[+0x80]` is stale/inconsistent with the OPC group's ItemDef.
- `AddConnectionRecord` is **direct-only** (never iconnect); `ApplyPasteConnections` is
  **dialog-bound**. There is NO silent code-reachable iconnect apply beyond
  `DelayConnection`/`ConnectByString` — and both duplicate-skip on the corrupt pin.

## Current root cause (the live hypothesis)

The bug is **upstream of the connect** — in how `OpcTreeManager` RECONSTRUCTS the deleted
channel from the snapshot DTO. The rebuilt pin comes out corrupt: poisoned RCW + stale
connection descriptor / inconsistent pin id in the OPC group's ItemDef. Direct links tolerate
this (name-route, re-resolved live); iconnect does not (duplicate-skip on the stale descriptor).

Reconstruction path to investigate: `PlanExecutor.ApplyDesiredSpec` →
`OpcScadaItemDto.ToScadaItemPruned` (builds `OpcUaScadaItem` from the DTO) → `SwapContainerItems`
→ `CommitStructuralChange` (`ApplyChange`) → `SynchWihSysTree`. Pin materialization + id
assignment is in `OpcUaProtocol.AddPinToTree` (the `id`/`id-1` Pout/`$`Pin split).

## RESOLVED root cause (fable run 3, disasm-proven) — ID-PATH COLLISION

> **RETRACTED — this theory is falsified. Do not act on it or resurrect it.**
> Attempt 7 disproved it: fresh ids changed nothing because the native id-path
> `[item+0x68]` is a **derived handle**, not the `AddPinWithID` id, so shifting our id
> cannot dodge any id-path match. Worse, all 8 attempts ran on **malformed snapshots**
> that were missing half the links (the capture-side dedup dropped the directPin input
> half of every settings iconnect pin), so the whole "reconstructed pin is permanently
> corrupt" premise was an artifact of incomplete capture, not a platform property.
> The real root cause is capture-side (dropped directPin input half); once both halves
> are captured and directs are wired before iconnects, stock `ObjectForward` reconnects
> everything. Durable platform facts and the corrected root cause live in
> [`Docs/known_issues/11-opc-pinpout-sibling-and-iconnect-connect.md`](../known_issues/11-opc-pinpout-sibling-and-iconnect-connect.md).
> The text below is kept as history only.

The iconnect "Duplicate connection" skip keys on an **ID-PATH**, not a name. `CVisavisDoc::GetRelativeName`
(0x5ef450), called from `IConnectImpl::Connect` (0x46e458) with its `bByName` flag = **0**, builds the
duplicate-check string from each item's NUMERIC id `[item+0x68]/[+0x6c]` via format `"%d"` (0x7709bc);
the pin name at `[+0x80]` is used only when `bByName=1`. The dup check is a CString compare (0x41ae40 →
mfc ord2611) of those id-paths.

The reconstruction restores the pre-deletion pin `Id` **verbatim** (`OpcScadaItemDto` → `AddPinWithID(id)`),
while the vendor's own OPC import NEVER reuses ids (`SyncNewElementWithSerializableObjectRec` → `GetNextId`
= `CalcMaxId()+2`). So the rebuilt pin's id-path is byte-identical to the stale id-path baked into the
surviving FB-side descriptor (left dangling because CH17-23 are deleted OUTSIDE NtoLib's `DisconnectSubtree`
— they hit the construct branch, never the shrink/disconnect branch). Every connect — ours AND MasterSCADA's
own restore — recomputes that same id-path on the reciprocal FB side, matches, and silently skips the append.
Direct links use `ConnectByName` (single-source slot 0x84, no two-sided id-path descriptor) → immune →
they wire. This explains the whole asymmetry AND why native "Восстановление внешних связей" no-oped with an
empty cannot-restore list (peer resolves by name; the append is what's skipped).

### Fix: reconstruct with FRESH ids (id remap)
Stop restoring `OpcScadaItemDto.Id` verbatim. Seed an even allocator from `protocol.ScadaRootNode.CalcMaxId()+2`
and apply a uniform even delta to every reconstructed item's id (preserves relative structure + the derived
`id-1` `$Pin` pairing; all fresh ids land above the existing range). Keep the existing name-based
`DelayConnection(...,ctIConnect)` + `ApplyChange`. Files: `OpcScadaItemDto.ToScadaItem*` +
`PlanExecutor.ApplyDesiredSpec` construct branch. NodeId/FullName/subscriptions unaffected.

### Cheapest confirming experiment (by tree)
Add the uniform id offset to the reconstructed subtree, keep `DelayConnectionForward` + `DiagnosticApplyChange=true`,
delete CH17-23, Execute, inspect tree. Wired → ship fresh-id remap. Not wired → native `[+0x68]` is a derived
handle, not the AddPinWithID id; fall back to purging the stale FB descriptor from the FB side (healthy RCW):
`((IConnect)externalPin.TreeObject).DisconnectByString(localPin.FullName,1,1)` BEFORE the name-based connect
(un-tried: #5 used the poisoned OPC pin as subject, #6 used DelayConnection). Full write-up:
`scratchpad/fable-solve.md`; new dumps `descbuilder_5ef450.txt`, `cmp_41ae40.txt`, `gate_573ca0.txt`.

## Open question (the ONE thing left to answer) — superseded by the RESOLVED section above

Why does DTO reconstruction leave the OPC pin with a stale connection descriptor / inconsistent
id, and how do we reconstruct it CLEAN (no residual descriptor, id consistent with the ItemDef)
so a plain name-based `DelayConnection` wires it — OR should we not reconstruct OPC pins from the
snapshot at all and instead let MasterSCADA's OPC layer re-import them (producing clean pins)?
Compare against a WORKING CH1–16 pin: what does its connection descriptor / id look like vs a
freshly-reconstructed CH17 pin?

## Artifacts (durable)

- `scratchpad/fable-conclusion.md` — full disasm write-up (run 2).
- `scratchpad/fable-recovered-leads.md` — run 1 recovered leads.
- `scratchpad/*_*.txt` — raw disasm dumps (rec_connect_533180, iconnect_connect_46e370,
  iconnectimpl_connectbystring_46f640, addconnrecord_5ec940, …).
- `scratchpad/objmodel.chm` + `chmout/html/` — extracted official object-model help.
- Decompile: `C:\Users\admin\projects\MasterScada3Wiki\srcs\`.

## Attempts 7-8 (fresh-id remap era) — both failed

| # | Mechanism | Host result | Log | Why |
|---|---|---|---|---|
| 7 | Fresh even-id remap + `DelayConnectionForward` | not wired | silent, ZERO throws, ok=0 | fresh ids applied cleanly but duplicate-skip persists → the id-path field `[+0x68]` is a DERIVED handle, not our `AddPinWithID` id. Shifting our id does not change the id-path. |
| 8 | Fresh-ids + `DelayConnectionFbDisconnectFirst` (`((IConnect)externalPin.TreeObject).DisconnectByString(local.FullName,1,1)` then DelayConnection) | not wired | threw 70× (ArgumentOutOfRange) | the FB-side `(IConnect)externalPin.TreeObject` object call ALSO throws the stale-RCW marshaling error — the healthy-`directPout`-wrapper precedent did not carry over to the raw `IConnect` cast. |

**Updated conclusion:** the id-path is a derived handle (fresh ids can't dodge the collision), AND
no object call on either side survives (both RCWs throw on the raw `IConnect` cast). The
delete-(via OPC param list)-then-reconstruct-from-snapshot flow leaves the FB side with a dangling
descriptor whose id-path our reconstructed pin re-derives identically, and every route to fix it at
connect time is blocked. Open strategic question: does this reproduce on a CLEAN deployment (FB side
NEVER connected to these channels), or only when re-deploying over an already-connected state? The
fix may belong at the DELETE side (properly clear the FB descriptor) or in the deployment flow, not
at reconnect.
