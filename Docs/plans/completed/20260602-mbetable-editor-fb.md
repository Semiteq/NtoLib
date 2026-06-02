# Plan: Editor-only MBE recipe FB (MbeTableEditorFB)

Date: 2026-06-02. Status: ready to execute (revised after auto-review — Seams 3 and 4
corrected, DI split enumerated, task/test gates added).

## Task

The customer wants a "copy" of `MbeTableFB` with all PLC-communication functionality
removed (Modbus TCP, recipe send/receive, runtime execution monitoring), leaving only
recipe editing: create/edit rows, CSV import/export, clipboard, static recipe-time
calculation. The block must appear in MasterSCADA as a separate palette element.

## Architecture decision (confirmed by customer)

Not a literal duplicate of the whole module tree, but a **shared editing core + a new thin
COM shell** (option b1):

- COM identity is carried only by two classes (`MbeTableFB`, `TableControl`) + embedded
  XML + bmp. The core (`ModuleCore`, `ModuleApplication`, `ModulePresentation`,
  `ServiceCsv`, `ServiceClipboard`, `ServiceRecipeAssembly`, formula engine) is
  COM-neutral and is reused as-is.
- A new FB `MbeTableEditorFB` + control `RecipeEditorControl` with **fresh GUIDs** and a
  trimmed XML build the **same** DI graph through a shared configurator, but without
  registering the PLC services.
- A literal duplicate is rejected (gross DRY violation: two copies of the editor, every
  bug fix applied twice). An "editor mode" flag on the single FB is rejected (one COM type
  cannot be two palette blocks; the Modbus properties and PLC pins would remain on the
  block).

### Locked customer decisions

1. **Architecture:** shared core + new shell (b1).
2. **Monitoring:** static time calculation only (`RecipeAnalyzer`/`TimingCalculator`).
   Live current-row highlighting and time countdown (PLC-driven) are removed.
3. **Target pins:** the dynamic target-name input pins are **kept** (as on the full FB).
   The MasterSCADA integrator wires string pins with device names — these are editing
   links, not PLC communication.
4. **Config:** shared YAML files, same `ConfigDirPath`. `PlcMapping` in `ColumnDefs.yaml`
   stays, ignored by the editor.

## Implementation steps

All new files live under `NtoLib/Recipes/MbeTable/Editor/`, namespace
`NtoLib.Recipes.MbeTable.Editor`.

### 1. New FB shell

> FB→XML binding is by embedded-resource name matching the class name.
> `MbeTableEditorFB.xml` resolves only for the class `MbeTableEditorFB`.

- `Editor/MbeTableEditorFB.cs` — `[Guid("<FRESH-GUID-1>")]`,
  `[DisplayName("Редактор рецептов MBE")]`, `[VisualControls(typeof(RecipeEditorControl))]`,
  `[ComVisible(true)]`, `[Serializable]`, `[CatID(CatIDs.CATID_OTHER)]`, subclass of
  `VisualFBBase`. Lifecycle mirrors `MbeTableFB.cs`, but:
  - `InitializeRuntime()` calls `MbeTableServiceConfigurator.ConfigureEditorServices(...)`;
  - no `RuntimeServiceHost`; `UpdateData()` does not call `Poll()` / `UpdateUiConnectionPins()`;
  - no `UpdateTimerPins` / `UpdateRecipeConsistentPin` (these are the Map-pin writers);
  - keeps `EnsureConfigurationLoaded`, formula precompilation, and the dynamic pins
    (`CreatePinMap`/`CreatePinsFromConfiguration`/`ReadPinGroup`/`GetDefinedGroupNames`/`ReadTargets`).
- `Editor/MbeTableEditorFB.Configuration.cs` — config loading + the
  `ReadTargets`/`GetDefinedGroupNames` bridge (from `MbeTableFB.Configuration.cs`).
- `Editor/MbeTableEditorFB.Pins.cs` — keep `CreatePinMap`/`CreatePinsFromConfiguration`/
  `ReadPinGroup`. Drop `UpdateUiConnectionPins` and the HMI ID constants 1003–1014.
- `Editor/MbeTableEditorFB.VisualProperties.cs` — keep only the shared non-Modbus
  properties: `Epsilon`, `ConfigDirPath`, `LogToFile`, `LogDirPath`. Drop all Modbus
  properties (IP1–4, port, UnitId, base addresses, area sizes, MagicNumber, WordOrder,
  timeouts, retries).
- `Editor/MbeTableEditorFB.xml` (EmbeddedResource, name = class name) — see pin contract.
- `Editor/MbeTableEditorFB.bmp` — a distinct palette icon.
- No `.Communication.cs` needed (those are PLC Map constants).

### 2. New control

- `Editor/RecipeEditorControl.cs` (+ `.Lifecycle.cs`, `.Buttons.cs`, `.Helpers.cs`,
  `.DesignTime.cs`, `.Designer.cs`, `.resx`) — `[Guid("<FRESH-GUID-2>")]`, `[DisplayName(...)]`,
  subclass of `VisualControlBase`. A trimmed copy of the `TableControl` partials:
  - `is MbeTableFB` (TableControl.Lifecycle.cs:34) → `is MbeTableEditorFB`;
  - remove the call to and the body of `TryReadFromPlc()` (line 61, 142–168) — auto-read
    from PLC on startup would hang without a controller;
  - remove the Send/Write button from the Designer layout (not just disable it);
  - remove `ClickButton_Send` and its wiring (TableControl.Buttons.cs);
  - remove the `ApplyButtonPermission(_buttonWrite, permissions.CanSendRecipe, scheme)`
    call in `ApplyPermissionsNow` (TableControl.Lifecycle.cs:265) — it references the
    removed `_buttonWrite` field and would NRE otherwise.

  Only the UI shell is duplicated (a few hundred lines of WinForms glue), not the domain.

### 3. PLC files excluded from the editor graph (kept in repo, not registered)

- `ServiceModbusTCP/` (entire tree)
- `ServiceRecipeAssembly/Modbus/`
- `ModuleApplication/Operations/Modbus/`
- `ModuleInfrastructure/PinDataManager/` (`FbPinAccessor`, `RecipeRuntimeStatePoller`,
  `RecipeRuntimeSnapshot`)
- `ModuleInfrastructure/RuntimeServiceHost.cs`
- `ModuleInfrastructure/RuntimeOptions/FbRuntimeOptionsProvider.cs` (replaced by a slim version)

Kept (editing core): `ServiceCsv/`, `ServiceClipboard/`,
`ServiceRecipeAssembly/{Csv,Clipboard,Common}/`, `ServiceStatus/`, `ServiceLogger/`,
`ModuleCore/`, `ModulePresentation/`, the pipeline, `StateProvider`, `TimerService`,
the `ActionTarget` abstraction.

### 4. Cutting the seams (the actual engineering)

**Seam 1 — `RecipeOperationService` depends on `IModbusTcpService`**
(RecipeOperationService.cs:34,45,56,215,228,243). Make `IModbusTcpService` optional
(nullable in the ctor); `SendRecipeAsync`/`ReceiveRecipeAsync` return a failed `Result`
when it is null. The editor graph simply does not register `IModbusTcpService`. One
`RecipeOperationService` for both FBs; the `OperationId.Send/Receive` members stay (avoids
switch churn) and are unreachable in the editor.

**Seam 2 — `ActionTargetProvider` is typed to `MbeTableFB`**
(ActionTargetProvider.cs:54,62,71,76). Extract an interface `IPinGroupReader`, implemented
by **both** FBs. Signatures must match the existing FB methods exactly:
`IReadOnlyCollection<string> GetDefinedGroupNames()` (MbeTableFB.Configuration.cs:19) and
`IReadOnlyDictionary<int,string> ReadTargets(string)` (currently `Dictionary<int,string>`
at line 28 — widen the FB return type to `IReadOnlyDictionary<int,string>`;
`ActionTargetProvider`'s LINQ usage is compatible). Register the concrete FB as
`IPinGroupReader` in each configurator. The one interface extraction that genuinely earns
its keep (two implementations, real polymorphism).

**Seam 3 — row-execution state (CORRECTED — this is not "no stubs needed").**
`ThreadSafeRowExecutionStateProvider` (kept; registered at DiContainer.cs:282) has a
**non-nullable** ctor dependency on `RecipeRuntimeStatePoller`
(ThreadSafeRowExecutionStateProvider.cs:14), which in turn needs `FbPinAccessor` and the
PLC pin-ID constants. And `ThreadSafeRowExecutionStateProvider` is itself a required ctor
dependency of four kept presentation services: `TableControlServices` (line 33),
`TablePresenter` (line 25), `TableRenderCoordinator` (line 36), `CellFormattingEngine`
(line 31). So the editor graph would fail to build at runtime unless this is resolved.
This is exactly the live current-row-highlight subsystem the customer asked to remove
(locked decision 2), but it is structurally woven into the kept rendering pipeline.

Fix: extract an interface `IRowExecutionStateProvider` over the members the four consumers
use, retarget those four consumers to the interface. The full FB keeps
`ThreadSafeRowExecutionStateProvider` (PLC-driven). The editor registers a no-op
`StaticRowExecutionStateProvider` (`Editor/StaticRowExecutionStateProvider.cs`) that always
reports `RowExecutionState.Upcoming` and raises no change events — so no poller,
`FbPinAccessor`, or PLC pin-IDs are pulled into the editor graph. A genuine
two-implementation case, not over-engineering. (`RuntimeServiceHost` is still simply absent
from the editor — that part of the original Seam 3 stands.)

**Seam 4 — `FbRuntimeOptionsProvider` (CORRECTED — `LoggingOptions` binds to the concrete
type).** `LoggingOptions` (ServiceLogger/LoggingOptions.cs:7), part of the kept core,
depends on the **concrete** `FbRuntimeOptionsProvider` — there is no abstraction today, so
registering only an editor provider would fail to resolve `LoggingOptions`. Fix: extract
`IRuntimeOptionsProvider` over the fields shared consumers read (logging + `Epsilon`),
change `LoggingOptions` (and any other shared consumer) to depend on the abstraction. The
full FB registers `FbRuntimeOptionsProvider`; the editor registers
`Editor/EditorRuntimeOptionsProvider.cs`, which populates only the shared fields (logging +
epsilon) with Modbus fields at defaults (the Modbus stack is not registered — harmless).

### 5. Shared configurator

In `ModuleInfrastructure/DiContainer.cs` add
`ConfigureEditorServices(MbeTableEditorFB, AppConfiguration, compiledFormulas)`. It calls
the same `Register*` helpers, but:

- `RegisterRuntimeState` — omitted (this is the `RecipeRuntimeStatePoller` registration);
- `RegisterModbusTcpServices` — omitted;
- `RegisterRecipeAssemblyServices` — extract a `RegisterModbusAssembly` sub-method holding
  **only** the two Modbus lines (DiContainer.cs:236 `ModbusAssemblyStrategy` and :239
  `ModbusRecipeAssemblyService`), called only by the full configurator. Everything else in
  that method **stays in the shared path**: `AssemblyValidator`, `TargetAvailabilityValidator`,
  and the Clipboard assembly services (`ClipboardSchemaDescriptor`, `ClipboardSchemaValidator`,
  `ClipboardParser`, `ClipboardStepsTransformer`, `ClipboardAssemblyService`) — `CsvService`
  (CsvService.cs:26), `CsvRecipeAssemblyService` (:22) and `ClipboardAssemblyService` (:29)
  all require `AssemblyValidator` → `TargetAvailabilityValidator`, so dropping them breaks
  CSV import / paste. (`TargetAvailabilityValidator.Validate` takes `IActionTargetProvider`
  as a method parameter, so it flows through Seam 2 correctly.)
- `RegisterApplicationServices` — without `IModbusTcpService` (line 272);
- `RegisterPresentationServices` — register `IRowExecutionStateProvider` →
  `StaticRowExecutionStateProvider` (editor) vs `ThreadSafeRowExecutionStateProvider` (full);
- `RegisterSharedInstances` (DiContainer.cs:128-136) — `FbPinAccessor` registration moves
  out of the shared path into the PLC-only path (it needs `MbeTableFB`, not the editor FB);
  the FB instance and `AppConfiguration` registrations stay shared (editor FB registered as
  `IPinGroupReader`);
- `IRuntimeOptionsProvider` → `FbRuntimeOptionsProvider` (full) vs
  `EditorRuntimeOptionsProvider` (editor);
- `RegisterInfrastructureServices` — `IActionTargetProvider → ActionTargetProvider`
  (now via `IPinGroupReader`).

To avoid duplicating the shared `Register*` calls, extract a private `RegisterShared(...)`
with the common registrations; each public entry point adds its PLC-or-not delta.

### 6. csproj edits (single edit, globs disabled)

In `NtoLib/NtoLib.csproj`:

- `<Compile Include>` for each new `Editor/*.cs` (FB partials, `RecipeEditorControl`
  partials, `EditorRuntimeOptionsProvider.cs`, `StaticRowExecutionStateProvider.cs`) and
  for the new interfaces wherever they live (`IPinGroupReader.cs`,
  `IRowExecutionStateProvider.cs`, `IRuntimeOptionsProvider.cs`).
- Near lines 757–759:
  ```xml
  <EmbeddedResource Include="Recipes\MbeTable\Editor\MbeTableEditorFB.bmp"/>
  <EmbeddedResource Include="Recipes\MbeTable\Editor\MbeTableEditorFB.xml"/>
  <EmbeddedResource Include="Recipes\MbeTable\Editor\RecipeEditorControl.resx">
    <DependentUpon>RecipeEditorControl.cs</DependentUpon>
  </EmbeddedResource>
  ```
- `Directory.Packages.props` — no change (no new NuGet packages).

### 7. COM registration

Both GUIDs are freshly generated (reusing existing ones → CLSID collision, `netreg.exe`
would register only one). The release `netreg.exe NtoLib.dll /showerror` step picks up the
new `[ComVisible]` types automatically.

## Editor FB pin contract (`MbeTableEditorFB.xml`)

| Group | ID | Editor |
|---|---|---|
| Map inputs (PLC monitoring): RecipeActive, ActualLineNumber, StepCurrentTime, ForLoopCount1-3, EnaSend | 1, 3–8 | Removed |
| Map outputs (status): TotalTimeLeft, LineTimeLeft, IsRecipeConsistent | 101–103 | Removed |
| VisualMap (Modbus addressing / connection echo) | 1003–1014 (XML also defines unused 1015 Timeout) | Removed |
| Dynamic target-name pins (ShutterNames, HeaterNames, NitrogenSourcesNames, ChamberHeaterNames) from PinGroupDefs.yaml | 201–316 | Kept |

`<Map>`/`<VisualMap>` `<Items>` are empty/absent; the block relies on code-generated
dynamic pin groups. `IsRecipeConsistent` is still computed
(`OperationPipelineRunner.ApplyPostSuccessEffects`), it is simply not written to a pin.

## Effort estimate

| Work item | Relative |
|---|---|
| New FB shell + trimmed XML + bmp + csproj | Low |
| `RecipeEditorControl` partials (remove Send + `is` check) | Low–Medium |
| `IPinGroupReader` + retarget `ActionTargetProvider` | Low |
| `IRowExecutionStateProvider` + `StaticRowExecutionStateProvider` + retarget 4 consumers | Medium |
| `IRuntimeOptionsProvider` + retarget `LoggingOptions` | Low |
| Optional `IModbusTcpService` in `RecipeOperationService` | Low |
| `ConfigureEditorServices` + `RegisterShared` split in DiContainer | Medium |
| `EditorRuntimeOptionsProvider` (logging/epsilon) | Low |
| Runtime validation in MasterSCADA (pin map, COM reg, no-PLC startup) | Medium (this is where `SetPinValue`/pin-ID issues surface — Docs/known_issues/09) |
| `dotnet format` + build (un-merged + RunILRepack) + xUnit | Low |

**Total ≈ 1–1.5× of building one new visual FB from scratch** (vs 3–4× for a literal
duplicate). The dominant unknown is runtime validation in the host, not the code.

## Task breakdown (with gates)

Convention (CLAUDE.md): for FB/XML integration, runtime validation in the MasterSCADA host
precedes unit tests. Tasks 1–4 are pure-logic shared-code refactors, each green-to-green
(the full FB keeps working at every step); they are unit-testable and ship with tests.
Task 5 builds the editor surface. Every new `.cs` file must get a `<Compile Include>` entry
in `NtoLib/NtoLib.csproj` in the same change (globs are disabled). Every task ends with
`dotnet build NtoLib.sln` and `dotnet test NtoLib.sln` green.

Host validation in the live MasterSCADA host is a manual follow-up — see the section after
the task list; it is intentionally NOT an exec task (no subagent can drive the host).

### Task 1: Extract IPinGroupReader and retarget ActionTargetProvider

- [x] Add interface `IPinGroupReader` with `IReadOnlyCollection<string> GetDefinedGroupNames()`
      and `IReadOnlyDictionary<int,string> ReadTargets(string)`. Implement it on `MbeTableFB`
      (widen `ReadTargets` return type from `Dictionary<int,string>` to `IReadOnlyDictionary`
      at MbeTableFB.Configuration.cs:28).
- [x] Retarget `ActionTargetProvider` ctor dependency from `MbeTableFB` to `IPinGroupReader`.
      Register the FB instance as `IPinGroupReader` in the existing configurator so the full
      FB keeps resolving.
- [x] Add `<Compile Include>` for the new interface file.
- [x] Unit test: `ActionTargetProvider` against a fake `IPinGroupReader`.
- [x] Gate: `dotnet build NtoLib.sln` + `dotnet test NtoLib.sln` green.

### Task 2: Extract IRowExecutionStateProvider and add StaticRowExecutionStateProvider

- [x] Extract interface `IRowExecutionStateProvider` over the members the four consumers
      (`TableControlServices`, `TablePresenter`, `TableRenderCoordinator`,
      `CellFormattingEngine`) use from `ThreadSafeRowExecutionStateProvider`. Retarget those
      four ctor dependencies to the interface.
- [x] Add `Editor/StaticRowExecutionStateProvider.cs` — always returns
      `RowExecutionState.Upcoming`, raises no change events, no poller dependency.
- [x] Keep the full FB registering `ThreadSafeRowExecutionStateProvider` as the interface in
      the existing configurator.
- [x] Add `<Compile Include>` for the new files.
- [x] Unit test: `StaticRowExecutionStateProvider` returns `Upcoming` and emits no events.
- [x] Gate: build + test green.

### Task 3: Extract IRuntimeOptionsProvider and add EditorRuntimeOptionsProvider

- [x] Extract interface `IRuntimeOptionsProvider` over the fields shared consumers read
      (logging fields + `Epsilon`). Retarget `LoggingOptions` (ServiceLogger/LoggingOptions.cs:7)
      and any other shared consumer from concrete `FbRuntimeOptionsProvider` to the interface.
- [x] Add `Editor/EditorRuntimeOptionsProvider.cs` — populates only logging + epsilon; Modbus
      fields left at defaults.
- [x] Keep the full FB registering `FbRuntimeOptionsProvider` as the interface.
- [x] Add `<Compile Include>` for the new files.
- [x] Unit test: `EditorRuntimeOptionsProvider` exposes logging + epsilon, Modbus at defaults.
- [x] Gate: build + test green.

### Task 4: Make IModbusTcpService optional in RecipeOperationService

- [x] Make the `IModbusTcpService` ctor dependency optional (nullable) in
      `RecipeOperationService` (RecipeOperationService.cs:34). Guard `SendRecipeAsync` /
      `ReceiveRecipeAsync` to return a failed `Result` when it is null. Leave the
      `OperationId.Send/Receive` enum members untouched.
- [x] Unit test: `SendRecipeAsync`/`ReceiveRecipeAsync` return a failed `Result` when the
      service is null.
- [x] Gate: build + test green.

### Task 5: Editor FB shell + control + DI fork + csproj

- [x] In `DiContainer.cs`: extract a private `RegisterShared(...)` with the common
      registrations; extract `RegisterModbusAssembly(...)` holding only the two Modbus lines
      (236/239); move `FbPinAccessor` registration out of `RegisterSharedInstances` into the
      PLC-only path. Keep `AssemblyValidator`/`TargetAvailabilityValidator`/Clipboard services
      shared.
- [x] Add `ConfigureEditorServices(MbeTableEditorFB, AppConfiguration, compiledFormulas)` per
      plan §5: omit runtime-state/Modbus-TCP/Modbus-assembly/`IModbusTcpService`; register
      `StaticRowExecutionStateProvider`, `EditorRuntimeOptionsProvider`, editor FB as
      `IPinGroupReader`.
- [x] Create `Editor/MbeTableEditorFB.cs` + `.Configuration.cs` + `.Pins.cs` +
      `.VisualProperties.cs` per plan §1 (fresh GUID, `[DisplayName("Редактор рецептов MBE")]`,
      trimmed VisualProperties, no PLC Map/VisualMap pins, no `RuntimeServiceHost`/`Poll`).
- [x] Create `Editor/MbeTableEditorFB.xml` (EmbeddedResource, name = class) with empty/absent
      `<Map>`/`<VisualMap>` items; copy `Editor/MbeTableEditorFB.bmp` from the existing bmp.
- [x] Create `Editor/RecipeEditorControl.*` partials per plan §2 (fresh GUID; trimmed copy of
      `TableControl`: `is MbeTableEditorFB`, remove `TryReadFromPlc` call+body, remove Send
      button from Designer, remove `ClickButton_Send`, remove the `_buttonWrite`
      `ApplyButtonPermission` call at Lifecycle:265).
- [x] Add all `<Compile Include>` / `<EmbeddedResource>` entries per plan §6 in a single
      csproj edit.
- [x] Unit test (DI regression guard): build the editor `IServiceProvider` via
      `ConfigureEditorServices` and assert `GetRequiredService<TableControlServices>()`
      resolves with no PLC services registered.
- [x] `dotnet format NtoLib.sln`.
- [x] Gate: un-merged `dotnet build NtoLib.sln` + `dotnet test NtoLib.sln` green; merged
      `dotnet build NtoLib/NtoLib.csproj -p:RunILRepack=true` green.

## Manual validation in MasterSCADA host (post-exec, human-driven — not an exec task)

- `netreg.exe NtoLib.dll /showerror` registers both FBs (no CLSID collision).
- Editor block appears as a distinct palette element; starts with **no PLC connected** (no
  hang from removed `TryReadFromPlc`); grid is editable; target dropdowns populate from wired
  name pins; CSV import/export + clipboard work; no Send button present.
- Confirm the nested-subfolder XML→class-name binding resolves (no existing FB lives in a
  sub-subfolder — unverified layout; this is the dominant residual risk per known_issues/09).

## Key reference files

- `MbeTableFB.cs` — lifecycle template
- `DiContainer.cs:68-308` — configurator to fork
- `RecipeOperationService.cs:34-62` — optional-PLC seam
- `ActionTargetProvider.cs:14-83` — `IPinGroupReader` seam
- `TableControl.Lifecycle.cs:34,61,142-168,265` — control couplings
- `MbeTableFB.xml` — pin contract to trim
- `MbeTableFB.Pins.cs:69-81` — drop `UpdateUiConnectionPins`
- `NtoLib.csproj:269-273,757-759` — entries to parallel
