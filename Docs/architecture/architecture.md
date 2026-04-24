# NtoLib Architecture Patterns

This document describes the architectural patterns used across NtoLib FBs.
NtoLib hosts two distinct FB architectures with different layering, lifecycle, and testing strategies.

For platform-level constraints referenced below (deferred execution, FB instance replacement,
pin ID/XML mismatches), see [`../known_issues/`](../known_issues/).

---

## Visual FB Architecture

Visual FBs render device state on the SCADA mnemoscheme via GDI+. They are split into four
collaborating layers, each with a single responsibility.

### 4-Layer Split

| Layer | File suffix | Responsibility |
|-------|-------------|----------------|
| FB / runtime logic | `*FB.cs` | Decode status words, manage pins, raise events |
| Visual control | `*Control.cs` | User interaction, sizing, layout, runtime timers |
| Status DTO | `Status.cs` | Plain struct capturing current device state |
| Renderer | `*Renderer.cs` | Pure drawing logic based on the status DTO |

When implementing a new device feature or flag, expect to touch **all four layers**:

1. **FB** — decode status / commands, expose bits via pins.
2. **Status DTO** — add fields and adjust derived properties (`AnyError`, `AnimationNeeded`).
3. **Control** — update visibility, enabling, and any settings UI that reflects the new state.
4. **Renderers** — change colors, indicators, and shapes to match the new semantics.

**Reference implementations:** `Devices/Valves/`, `Devices/Pumps/`.

### Config–Code Synchronization (XML ↔ ID Constants)

FB classes use **ID constants** that must match entries in their XML mappings for both:

- The UI map (`<Map>` / `Выходы для UI`)
- The visual map (`<VisualMap>`)

Safe evolution pattern when adding a new flag/bit/field:

1. Define the **concept** first — what it means functionally and visually.
2. Add corresponding **pin entries in XML** (UI + Visual maps) with unique IDs.
3. Introduce **ID constants in the FB** and read/write those IDs only after XML is updated.
4. Thread the value through the **status DTO** and **control**.

> Mismatched pin IDs (constant exists in code, but no corresponding XML pin) cause runtime
> `NullReferenceException`. See [`../known_issues/09-mismatched-pin-ids.md`](../known_issues/09-mismatched-pin-ids.md).

### Common Pitfalls

- **Partial updates of semantics** — renderers, FB events, and UI logic share the same
  conceptual flags ("error", "active", "blocked"). Changing only one layer leads to
  inconsistent behavior (error frame without an error event, or vice versa).
- **Combined-semantic flags** — many flags mean "mode A AND lacks capability B" rather than
  acting independently. Implementing them as simple booleans in isolation breaks expectations.

### Domain-Level Patterns for Device Features

Device flags typically influence **both**:

- Logical behavior — which messages/events can occur, whether input is accepted.
- Visual semantics — color, shape, visibility, indicators.

When designing a new flag or mode:

- Explicitly define its **behavioral** effect (events, errors, interactions).
- Explicitly define its **visual** effect (colors, overlays, hidden controls).
- Implement both aspects in lockstep so the UI never lies about the actual state.

Encode flag combinations via helper conditions (e.g., `canRaiseErrors`, `isManualWithoutSensors`)
to keep the logic readable and reuse them across FB, control, and renderers.

### Debugging Priorities for Visual FBs

For anything involving FB/XML integration, prioritize **runtime validation**:

- Run the FB in a host/SCADA-like context and watch for immediate exceptions around pin access.
- Confirm that new pins actually appear and change as expected in the visual layer.

Use **unit tests** to validate higher-level behavior once wiring is stable:

- Whether events are correctly gated by flags.
- Whether visibility/animation toggles behave correctly for different flag combinations.

If you see `NullReferenceException` from `SetPinValue` or similar, **first suspect ID/XML
mismatches**, not business logic.

### Practical Checklist for New Device/Flag Work

1. **Concept** — clarify what the flag/mode means logically and visually.
2. **XML** — add pins to both UI and Visual maps (with clear, unique IDs).
3. **FB** — map bits to constants, read/write them, gate events/commands appropriately.
4. **Status DTO** — add fields and adjust derived properties (errors, animation).
5. **Control** — wire status into visibility, enabling, and any settings/forms.
6. **Renderers** — update drawing to reflect new states/combos.
7. **Runtime check** — verify pin wiring works in MasterSCADA host (no `NullReferenceException`).
8. **Tests** — add/adjust unit tests to lock in expected behavior combinations.
9. **Format** — run `dotnet format NtoLib.sln`.

---

## Headless FB Architecture

Headless FBs extend `StaticFBBase` and have no visual control, status DTO, or renderer layers.
They follow a thin-orchestrator pattern with all business logic in a facade service.

### Thin Orchestrator Pattern

The FB class itself does only three things:

1. **Lifecycle management** — create/destroy the service in `ToRuntime()`/`ToDesign()`.
2. **Pin I/O** — read input pins, write output pins.
3. **Edge detection** — detect rising edges on trigger pins.

All business logic is delegated to a **facade service** behind an interface. The service
receives `IProjectHlp` (or other dependencies) via constructor injection. Service methods
return `FluentResults.Result` or `Result<T>`.

**Reference implementations:** `ConfigLoader/`, `LinkSwitcher/`, `TrendPensManager/`.

### PlanBuilder Pure-Helper Pattern

For headless FBs that build an operation plan from already-resolved inputs (config, snapshot,
current tree state), extract plan construction into a **pure static helper class** with no
vendor COM dependencies. This makes the helper directly testable without mocking COM.

**Rules for a pure helper:**

- Accept only plain data types as inputs — no `IProjectHlp`, no SCADA tree items.
- Perform no I/O, no COM calls, no logging side effects beyond an optional `ILogger?` parameter.
- Return `Result`/`Result<T>` for the same error-handling consistency as the facade service.
- Declare the class `internal static` so it is invisible outside the FB module.

**Flow:**

1. The facade service resolves all COM-dependent inputs (config file, snapshot file, live tree).
2. The facade service calls the pure helper, passing the resolved values.
3. The pure helper computes the plan and returns it.
4. The facade service stores the plan for deferred execution.

Acceptance tests test the pure helper directly via fixture files, with no COM involved.

**Reference implementation:** `OpcTreeManager/Facade/PlanBuilder.cs`.

### Rising-Edge Detection

Trigger pins use a manual boolean-toggle pattern:

```csharp
private bool _previousTrigger;

// In UpdateData():
var currentTrigger = GetPinValue<bool>(TriggerPinId);
var isRisingEdge = currentTrigger && !_previousTrigger;
_previousTrigger = currentTrigger;
if (!isRisingEdge) return;
```

Reset `_previousTrigger` to `false` in `ToRuntime()` so the first `true` value after entering
runtime is detected as a rising edge.

### Deferred Execution Pattern (Tree-Modifying FBs)

MasterSCADA forbids tree modifications during runtime. FBs that need to modify the project
tree must:

1. **Queue** the planned operations during runtime (in `UpdateData()`).
2. **Post a deferred delegate** from `ToDesign()` via `System.Windows.Forms.Timer`.
3. **Guard the delegate with an `InRuntime` check** — re-arm the timer if `InRuntime` is still `true`.
4. **Report** success/failure to a log file on disk.

> The platform constraints driving this pattern are non-trivial. Read these before changing
> any deferred-execution code:
>
> - [`../known_issues/06-runtime-tree-modification-forbidden.md`](../known_issues/06-runtime-tree-modification-forbidden.md) — why direct calls fail and the shutdown sequence
> - [`../known_issues/07-fb-instance-replacement.md`](../known_issues/07-fb-instance-replacement.md) — why results must go to disk, not to FB fields
> - [`../known_issues/08-begininvoke-reposting-fails.md`](../known_issues/08-begininvoke-reposting-fails.md) — why `BeginInvoke` retry doesn't work and `Timer` does

**Reference implementations:** `LinkSwitcher/LinkSwitcherFB.cs`, `OpcTreeManager/Facade/OpcTreeManagerService.cs`.

### File-Based Logging

Headless FBs that need persistent logging (especially deferred-execution FBs) must write to
`C:\DISTR\Logs\<fb-name>.log` using Serilog. Output pins are not suitable for logging
deferred execution results due to FB instance replacement (see
[`../known_issues/07-fb-instance-replacement.md`](../known_issues/07-fb-instance-replacement.md)).

**Lightweight pattern (no DI):**

1. Create a `Serilog.Core.Logger` directly in `InitializeRuntime()` with File + Debug sinks.
2. Pass the `ILogger` to the service/facade via constructor.
3. Dispose the logger in `CleanupRuntime()` (or inside the deferred delegate's `finally`).
4. The deferred delegate captures the logger via closure — it remains valid after the FB
   instance is discarded.

**Serilog configuration:**

- Log file: `C:\DISTR\Logs\<fb-name>.log` (e.g., `link-switcher.log`, `mbe-table.log`)
- `rollingInterval: RollingInterval.Infinite`
- `fileSizeLimitBytes: 5 * 1024 * 1024`
- `rollOnFileSizeLimit: true`
- `retainedFileCountLimit: 5`
- `shared: true`
- Output template: `"{Timestamp:O} [{Level:u3}] {Message:lj}{NewLine}{Exception}"`

**Reference implementations:** `LinkSwitcher/LinkSwitcherFB.cs` (lightweight),
`Recipes/MbeTable/ServiceLogger/LoggingBootstrapper.cs` (full DI).

### Lifecycle Template

```csharp
public override void ToRuntime()
{
    base.ToRuntime();
    if (TreeItemHlp?.Project == null)
        throw new InvalidOperationException("TreeItemHlp.Project is null.");
    _service = new MyService(TreeItemHlp.Project);
    _previousTrigger = false;
    _isRuntimeInitialized = true;
}

public override void ToDesign()
{
    // Post deferred execution BEFORE base.ToDesign() for tree-modifying FBs
    PostDeferredIfNeeded();
    base.ToDesign();
    _service = null;
    _isRuntimeInitialized = false;
}

public override void UpdateData()
{
    base.UpdateData();
    if (!_isRuntimeInitialized) return;
    ProcessCommand();
}
```

### Timer-Based Deferred Execution Template

```csharp
private const int MaxDeferredRetries = 100;
private const int DeferredRetryIntervalMs = 200; // total timeout: ~20s

private static void PostDeferredExecution(
    IMyService service, MyPlan plan, Logger? logger,
    IProjectHlp project, int retriesRemaining)
{
    var timer = new Timer { Interval = DeferredRetryIntervalMs };
    var retries = retriesRemaining;

    timer.Tick += (_, _) =>
    {
        if (project.InRuntime)
        {
            retries--;
            if (retries <= 0)
            {
                timer.Stop();
                timer.Dispose();
                logger?.Error(
                    "Deferred execution aborted: InRuntime still true after {Max} retries",
                    MaxDeferredRetries);
                logger?.Dispose();
            }
            return;
        }

        timer.Stop();
        timer.Dispose();
        try
        {
            service.Execute(plan);
        }
        finally
        {
            logger?.Dispose();
        }
    };

    timer.Start();
}
```

### Practical Checklist for New Headless FB Work

1. **Define pins** — inputs, outputs, types, ID numbering. Use `<Pin>` for inputs and
   `<Pout>` for outputs in XML.
2. **Create XML** — follow the `<FBConfig><Map><Items>` format.
3. **Create service interface and implementation** — service receives dependencies via
   constructor, returns `Result`/`Result<T>`.
4. **Create FB class** — thin orchestrator with lifecycle, edge detection, and pin I/O only.
5. **Update csproj** — add all `<Compile Include>` and `<EmbeddedResource>` entries.
6. **Build and verify** — zero new errors. Resolve warnings from new code.
7. **Format** — run `dotnet format NtoLib.sln`.
8. **Runtime check** — verify pin wiring works in MasterSCADA host.

---

## Branching & Issue-Driven Workflow

- Branches are typically one-per-issue using the pattern `feature/<issue_number>` (e.g.,
  `feature/72`, `feature/73`).
- New feature branches are created from **up-to-date `master`** after merging earlier related
  work, keeping cross-cutting device behavior consistent.
- Treat each issue as a **vertical slice** — include config (XML), runtime logic (FB),
  UI/control, and renderers in a single branch. Avoid spreading one logical feature across
  multiple unrelated branches.

---

## Test Infrastructure Notes

### Two-Tier Test Structure for OpcTreeManager

OpcTreeManager tests are split into two tiers to keep vendor COM dependencies out of unit/
integration tests:

| Tier | Location | What it tests | Vendor COM required |
|------|----------|---------------|---------------------|
| A — Acceptance | `Tests/OpcTreeManager/Acceptance/` | `PlanBuilder` pure helper via fixture files (`config.yaml`, `tree.json`, `expected.json`) | No |
| B — Integration seam | `Tests/OpcTreeManager/Integration/` | `PlanExecutor.TestApplyDesiredSpec` via `ISubtreeDisconnector` fake | No |

- **Tier A fixtures** live under `Tests/OpcTreeManager/Fixtures/Acceptance/<case-name>/`. Each
  case has three files: `config.yaml`, `tree.json`, `expected.json`. Add new fixture
  directories to add new acceptance cases — no code changes required.
- **Tier B seam** uses `FakeSubtreeDisconnector` (in `Tests/OpcTreeManager/Integration/Fakes/`)
  to replace the COM `IProjectHlp` dependency at the `ISubtreeDisconnector` boundary. The
  `PlanExecutor` internal test constructor `PlanExecutor(ISubtreeDisconnector, ILogger)`
  accepts `null` for `_project` and must only be used with `TestApplyDesiredSpec`.
- Fixture-driven cases that depend on future tasks declare a `skipReason` in `expected.json`.
  These cases use `[SkippableTheory]` + `Skip.If(true, reason)` so they are reported as SKIP
  (not PASS) in CI.
