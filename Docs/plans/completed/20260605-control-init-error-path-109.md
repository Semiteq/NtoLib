# Control Initialization Error Path: Transactional Cleanup + `throw;` (#109)

## Overview

Closes #109. Fixes the initialization error path of both visual control shells
(`TableControl`, `MbeTableEditorControl`) based on a multi-agent verification of the
issue against the code, the architecture docs, and the decompiled MasterSCADA 3.12 SDK.

### What the verification found

**Issue #109, Problem 1 ("sticky partial initialization via tail steps") — premise
confirmed, named trigger refuted.** The structure is exactly as described:
`MarkInitialized()` sets `_runtimeInitialized = true` (`TableControl.Lifecycle.cs:60`)
before the tail steps `TryReadFromPlc()` (`:61`) and `ApplyInitialPermissions()` (`:62`),
and `HandleInitializationError` rethrows without resetting the flag. However, the
exception path the issue names does not exist:

- `_ = _presenter.ReceiveRecipeAsync()` (`:166`) cannot throw synchronously.
  `ReceiveRecipeAsync` is an `async Task` method (`TablePresenter.cs:98`) — the compiler
  captures *all* body exceptions, including pre-first-await code, into the returned
  `Task`. A discarded task fault never reaches the outer catch.
- `BusyStateManager.Enter()` is `Interlocked.Increment` plus a trivial allocation — it
  has nothing to throw.
- `ApplyInitialPermissions` is already fully wrapped in its own try/catch; the only
  statement outside it (`IsHandleCreated` check) cannot throw.

The "sticky via tail steps" scenario is therefore unreachable today in either control.
The issue's proposed fix (local try/catch around the tail steps) addresses a dead path
and is **not implemented** by this plan.

**Issue #109, Problem 2 (`throw ex;` resets the stack trace) — confirmed.** Both
controls: `TableControl.Lifecycle.cs:213`, `MbeTableEditorControl.Lifecycle.cs:184`.
Per the decompiled SDK, the rethrown exception escapes through bare (no try/catch)
platform bodies (`VisualControlBase.cs:178-182`, `VisualFBRid` setter `:41-50`) across
the COM boundary into the native engine; nothing managed ever catches it, so the
destroyed trace is the only diagnostic that would have existed.

**Defect B (not in the issue) — non-transactional initialization; the most serious
real defect in this area.** An exception thrown *before* `MarkInitialized` (e.g. the
classic pin/XML-mismatch NRE, `Docs/known_issues/09`) is rethrown by
`HandleInitializationError` **without cleanup** while `_runtimeInitialized` stays
`false`. The two independent entry points — `put_DesignMode` (`:309`) and
`OnFBLinkChanged` (`:325`) — can both fire within one control lifetime, and the
decompiled SDK shows `OnFBLinkChanged` is raised unconditionally on every RID
assignment. A second entry passes the guard and re-runs all one-shot steps, producing:

1. a true double subscription of `OnPermissionsChanged` on the singleton
   `StateProvider` (single `-=` in `UnsubscribeGlobalServices` removes only one);
2. orphaned pass-1 `TableRenderCoordinator` / `TablePresenter` instances whose
   subscriptions to DI singletons (which outlive the control) leak until the FB
   container dies. (`TableInputManager`/`TableBehaviorManager` are *not* leak channels —
   they bind only to the control's own `DataGridView`.)

`CleanupRuntimeState` was verified safe and idempotent on partially-initialized state
(early-return guard requires both `_services` and `_serviceProvider` null; every step is
null-guarded and wrapped in `SafeDisposal.RunAll`), which makes the fix a one-line call.

**Defect C (minor) — unobserved fault in the no-handle branch of `TryReadFromPlc`.**
The ordinary unreachable-PLC case is *not* silent: lower layers log every path
(`OperationPipelineRunner` `LogWarning`/`LogError`, `RecipeOperationService`
`LogCritical`) and return `Result.Fail` in a completed task. But a genuinely *faulted*
task (unexpected exception) in the `_ =` discard branch is observed by nobody and logged
by nobody at the call site. A fault-logging continuation closes this defensively.

### What this PR fixes

| # | Change | Controls |
|---|--------|----------|
| 1 | `CleanupRuntimeState()` before the rethrow in `HandleInitializationError` — makes every initialization attempt transactional: either fully initialized or fully rolled back with safe re-entry | both |
| 2 | `throw;` instead of `throw ex;` — preserves the original stack trace | both |
| 3 | Fault-logging continuation on the discarded `ReceiveRecipeAsync()` task in the no-handle branch of `TryReadFromPlc` | `TableControl` only (editor has no `TryReadFromPlc`) |
| 4 | Invariant comment at `MarkInitialized` call site documenting why the flag is set before the tail steps and why the tail steps must not throw | both |

### Explicitly out of scope (decided against)

- Local try/catch around `ApplyInitialPermissions` — already self-protected (dead code).
- Moving `MarkInitialized` to the end — the flag must keep protecting one-shot steps;
  transactional rollback is the correct mechanism instead.
- Control-level logging of ordinary PLC read failures — already logged below.
- Removing the rethrow entirely — native host behavior on an escaping exception is
  unverifiable (obfuscated engine); keep the minimal change, preserve current contract.

## Context (from discovery)

- Files involved: `NtoLib/Recipes/MbeTable/TableControl.Lifecycle.cs`,
  `NtoLib/Recipes/MbeTableEditor/MbeTableEditorControl.Lifecycle.cs`
- Pattern: thin-shell duplication — both lifecycle files are intentionally mirrored
  COM-bound glue; the same edit is applied to both by convention.
- `CleanupRuntimeState` → `UnsubscribeGlobalServices` + `DisposeRuntimeComponents` +
  `ResetRuntimeFields` — verified idempotent and partial-state-safe (`SafeDisposal`).
- Platform facts verified against decompiled SDK at `MasterScada3Wiki` (see Overview).

## Development Approach

- **Testing approach**: Regular. Most touched code is WinForms/COM glue with no
  unit-testable surface (private members of a `VisualControlBase` control that needs a
  live host `FBConnector`); per project convention (CLAUDE.md), runtime validation in the
  MasterSCADA host precedes unit tests for FB integration paths. The single
  vendor-independent unit (the fault-logging continuation) is extracted and unit-tested.
  The existing suite serves as the regression gate.
- Complete each task fully before moving to the next.
- **CRITICAL: all tests must pass before starting next task.**
- **CRITICAL: update this plan file when scope changes during implementation.**
- Run `dotnet format NtoLib.sln` after all code changes.

## Testing Strategy

- The one vendor-independent logic unit — the fault-logging continuation — is extracted
  into `Recipes/MbeTable/Utilities/TaskFaultLogger.cs` and covered by
  `Tests/MbeTable/Utilities/TaskFaultLoggerTests.cs` (faulted → logged once; completed →
  not logged; canceled → not logged).
- The remaining changed lines live in control lifecycle glue. They are private members of
  `TableControl` / `MbeTableEditorControl` (`: VisualControlBase`), which cannot be
  instantiated without a live host `FBConnector`, so they stay covered by manual host
  scenarios rather than unit tests. (`Tests.csproj` does reference the vendor SDK —
  `FB.dll`, `MasterSCADA.*`, `InSAT.Library`, `System.Windows.Forms` — so the barrier is
  the missing live `FBConnector`, not a missing assembly reference.)
- Regression gate: full existing suite (`dotnet test NtoLib.sln`, 290 tests) must stay
  green after each task.
- Behavioral verification: manual MasterSCADA host scenarios (see Post-Completion).

## Progress Tracking

- mark completed items with `[x]` immediately when done
- add newly discovered tasks with ➕ prefix
- document issues/blockers with ⚠️ prefix

## Solution Overview

`HandleInitializationError` becomes the transaction rollback point: cleanup first (so
leaked subscriptions cannot fire into a half-built control while the modal `MessageBox`
pumps messages), then notify the operator, then rethrow with `throw;`. The cleanup call
resets `_runtimeInitialized` via `ResetRuntimeFields`, so a later `put_DesignMode` /
`OnFBLinkChanged` entry retries initialization from a clean slate instead of either
double-subscribing (pre-flag failure today) or being permanently blocked.

The discarded-task branch gains a `ContinueWith(..., TaskContinuationOptions.OnlyOnFaulted)`
continuation that logs via the already-initialized `_logger` — one statement, no
behavioral change on the success path, no scheduler/context requirements for logging.

## Technical Details

`HandleInitializationError` target shape (both controls, identical except log message):

```csharp
private void HandleInitializationError(Exception ex)
{
    _logger?.LogCritical(ex, "TableControl initialization failed");

    CleanupRuntimeState();

    MessageBox.Show(
        $@"Failed to initialize table: {ex.Message}",
        @"Initialization Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);

    throw;   // preserves original stack trace; cleanup above makes init transactional
}
```

Note: `throw;` is only valid inside a catch block — since `HandleInitializationError`
is a separate method, the rethrow must move to the catch site:

```csharp
catch (Exception ex)
{
    HandleInitializationError(ex);
    throw;
}
```

with `HandleInitializationError` no longer throwing (log + cleanup + MessageBox only).
This is the standard C# resolution and keeps the helper single-purpose.

`TryReadFromPlc` no-handle branch (`TableControl` only):

```csharp
else
{
    TaskFaultLogger.LogOnFault(
        _presenter.ReceiveRecipeAsync(),
        _logger,
        "Initial PLC recipe read faulted");
}
```

The continuation logic lives in `Recipes/MbeTable/Utilities/TaskFaultLogger.cs`
(`OnlyOnFaulted`, `CancellationToken.None`, `TaskScheduler.Default`) so it is unit-testable
in isolation. `TaskScheduler.Default` is passed explicitly to remove the
`TaskScheduler.Current` ambiguity (the body only logs — no UI affinity needed). The
*has-handle* branch
(`async void` lambda with its own catch) is intentionally left as-is: it already
observes the fault; unifying the two branches is cosmetic and out of scope.

## Implementation Steps

### Task 1: Transactional error path in TableControl

**Files:**
- Modify: `NtoLib/Recipes/MbeTable/TableControl.Lifecycle.cs`

- [x] change `HandleInitializationError` to: `LogCritical` → `CleanupRuntimeState()` → `MessageBox` (remove `throw ex;`)
- [x] add `throw;` after the `HandleInitializationError(ex)` call inside the catch of `InitializeServicesAndRuntime`
- [x] add invariant comment at the `MarkInitialized()` call site (flag before tail steps; tail steps must not throw; error path rolls back via `CleanupRuntimeState`)
- [x] add fault-logging `ContinueWith` to the no-handle branch of `TryReadFromPlc`
- [x] build + run full suite — must pass before task 2

### Task 2: Transactional error path in MbeTableEditorControl (mirror)

**Files:**
- Modify: `NtoLib/Recipes/MbeTableEditor/MbeTableEditorControl.Lifecycle.cs`

- [x] apply the same `HandleInitializationError` restructuring (cleanup before MessageBox, no throw inside helper)
- [x] add `throw;` at the catch site in `InitializeServicesAndRuntime`
- [x] add the same invariant comment at `MarkInitialized()` (no `TryReadFromPlc` here — editor variant)
- [x] build + run full suite — must pass before task 3

### Task 3: Verify acceptance criteria

- [x] verify both controls compile with identical mirrored error-path structure
- [x] verify no other callers of `HandleInitializationError` exist (grep)
- [x] run full test suite: `dotnet test NtoLib.sln` — 290 green
- [x] run `dotnet format NtoLib.sln` — no diff

### Task 4: [Final] Documentation and PR

- [x] check `Docs/known_issues/` — reviewed all nine entries; no update needed. The defect is NtoLib-level (non-transactional control init: double `OnPermissionsChanged` subscription + orphaned coordinator/presenter on re-entry), not a platform bug class. Its platform trigger (pin/XML-mismatch NRE) is already catalogued in `09-mismatched-pin-ids.md`.
- [x] move this plan to `Docs/plans/completed/` (via `git mv`)
- [x] PR with `Closes #109` (description drafted in `.pr-draft.md`; PR created after review phases) — documents the corrected analysis (Problem 1 trigger refuted, real defect = non-transactional init) so the issue history is not misleading

## Post-Completion

**Manual verification in MasterSCADA host** (per project convention, before release):

- Normal open/close of the mnemoscheme: recipe loads, permissions applied, no behavior change.
- Forced init failure (e.g. temporarily break a YAML config path): MessageBox appears,
  log contains `LogCritical` with full original stack trace, control is fully rolled
  back; re-opening the mnemoscheme after fixing the config initializes cleanly with no
  double-subscription symptoms (no duplicated permission-driven repaints).
- Editor FB variant: same failure/recovery scenario without PLC.

**Open platform questions** (documented, not blocking):

- Native host behavior on an exception escaping `put_DesignMode`/`OnFBLinkChanged`
  remains unverifiable (obfuscated engine); the rethrow contract is kept as-is.
- Live reproducibility of the second `InitializeServicesAndRuntime` entry was
  established structurally (SDK: unconditional `OnFBLinkChanged` on RID set) but not
  observed in a running host.
