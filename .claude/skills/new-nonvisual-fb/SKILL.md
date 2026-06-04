---
name: new-nonvisual-fb
description: Use when creating a brand-new headless (non-visual) Function Block in NtoLib — "add a new headless FB", "create a StaticFBBase block", "new non-visual function block", "add an FB with no UI". Covers XML pin config, service facade, FB orchestrator class, csproj registration, build, and elevated COM registration. NOT for visual FBs that have a control (use new-visual-fb), nor for editing an existing FB.
---

# Create a New Non-Visual Function Block

## When to Use

Use this skill when the task involves creating a brand-new headless FB (one that extends `StaticFBBase` without visual controls, status DTOs, or renderers). Do NOT use this for visual FBs or for modifying existing FBs.

## Pre-Flight

Before starting, gather this information from the user (ask if not provided):

1. **FB name** -- e.g. `LinkSwitcher`. Determines class name (`LinkSwitcherFB`), folder name (`LinkSwitcher/`), and XML file name (`LinkSwitcherFB.xml`).
2. **Display name** -- the human-readable name shown in MasterSCADA's FB palette (goes into `[DisplayName(...)]`).
3. **Category** -- which `CatIDs.CATID_*` constant to use. Default: `CatIDs.CATID_OTHER`.
4. **Pin layout** -- list of input and output pins with names, types, and default values. Types use Russian names: `Логический`, `Строковый`, `Вещественный`, `Целый`, `Время`.
5. **Does the FB modify the project tree?** -- if yes, use the deferred execution pattern: queue the work in runtime and post a DEFERRED action from `ToDesign()`. Do NOT mutate the tree synchronously inside `ToDesign()` while still in runtime -- it throws "modification forbidden in runtime mode". If no, execute immediately in `UpdateData()`.
6. **Does the FB need access to `IProjectHlp`?** -- typically yes if it navigates the MasterSCADA object tree.

## Naming conventions for domain entities

- Device-group entities use the canonical English plural terms: `Shutters`, `Sources`,
  `ChamberHeaters`, `Waters`, `Gases`, `VacuumMeters`, `Pyrometers`, `Interferometers`,
  `Turbines`, `Cryos`, `Ions`. New pins, YAML sections, DTOs, and UI labels for a device
  group reuse the canonical term — do not invent synonyms (one concept = one identifier
  across the whole vertical slice; `Waters` and `WaterChannels` must never coexist).
- Compound adjuncts stay singular (`shutterNames`, `_pyrometerNames`) — standard English,
  matching the existing `ShutterNames`/`HeaterNames` YAML group keys.
- An enum describing the type of a single device is singular (`PumpType.Turbine`,
  `PumpType.Ion`) — plural applies to groups, not to one device's kind.
- An identifier that carries a frozen contract string (legacy YAML key, pin name,
  customer tree node name) follows the contract string exactly, even when it violates the
  conventions above — greppability against customer configs beats style.
- Design-time properties of a `[Serializable]` FB are a serialization contract: their
  backing fields persist in customer project files. Never rename them after release; a
  DTO that mirrors them keeps their names.

## Step-by-Step Workflow

Execute these steps in order. Track each step with the harness task tools (`TaskCreate` / `TaskUpdate`).

### Step 1: Create Folder Structure

```
NtoLib/<FBName>/
    <FBName>FB.cs              # FB orchestrator class
    <FBName>FB.xml             # Pin configuration (EmbeddedResource)
    Facade/
        I<FBName>Service.cs    # Service interface
        <FBName>Service.cs     # Service implementation
```

Add more subfolders (e.g. `Entities/`, `Logging/`) only if the service layer has enough complexity to warrant them. Do not over-engineer.

### Step 2: Write the XML Pin Configuration

The XML must follow this exact format:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<FBConfig>
  <Map>
    <Items>
      <Pin ID="1" Name="InputName" Type="Логический" DefaultValue="false"/>
      <Pout ID="101" Name="OutputName" Type="Строковый" DefaultValue=""/>
    </Items>
  </Map>
</FBConfig>
```

Rules:
- `<Pin>` for inputs, `<Pout>` for outputs
- `ID` attribute is uppercase (not `Id`)
- `DefaultValue` attribute is optional but recommended
- `Type` uses Russian type names: `Логический` (bool), `Строковый` (string), `Вещественный` (double), `Целый` (int), `Время` (DateTime)
- Do NOT use `Direction` or `Description` attributes
- No `<VisualMap>` section (this is a headless FB)
- ID numbering: `ID` is a free integer with no platform-enforced ranges. By convention NtoLib starts inputs at 1 and outputs at 101 for visual separation -- this is an optional convention, not a requirement

### Step 3: Write the Service Interface

```csharp
using FluentResults;

namespace NtoLib.<FBName>.Facade;

public interface I<FBName>Service
{
    // Return Result or Result<T> for operations that can fail
    Result DoSomething(string input);

    // Properties for state inspection
    string GetLog();
}
```

Design rules:
- All methods that can fail return `FluentResults.Result` or `Result<T>`
- Methods that cannot fail (e.g. `Cancel()`, property getters) may return void or direct values
- Accept `IProjectHlp` via constructor if tree access is needed
- Do not reference pin IDs or FB-specific types -- the service knows nothing about the FB's pin wiring

### Step 4: Write the Service Implementation

The service contains ALL business logic. The FB class must never contain business logic.

- Receive dependencies via constructor injection (`IProjectHlp`, configuration values)
- Use `Result.Ok()` and `Result.Fail("message")` for all fallible operations
- If the FB modifies the tree, expose `HasPendingTask`, `PendingPlan`, and a `FlushPending()` or `Execute(plan)` method

### Step 5: Write the FB Class

Use this template. Adapt based on whether the FB modifies the tree or not.

```csharp
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;

using InSAT.Library.Interop;

using NtoLib.<FBName>.Facade;

namespace NtoLib.<FBName>;

[Serializable]
[ComVisible(true)]
[Guid("GENERATE-A-NEW-GUID")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("<Display Name>")]
public class <FBName>FB : StaticFBBase
{
    // Pin ID constants -- must match XML exactly
    private const int TriggerPinId = 1;
    private const int SuccessPinId = 101;
    private const int ErrorMessagePinId = 102;

    [NonSerialized] private I<FBName>Service? _service;
    [NonSerialized] private bool _previousTrigger;
    [NonSerialized] private bool _isRuntimeInitialized;

    protected override void ToRuntime()
    {
        base.ToRuntime();
        InitializeRuntime();
    }

    protected override void ToDesign()
    {
        // If tree-modifying: POST a deferred flush here (do not mutate the tree
        // synchronously -- it throws "modification forbidden in runtime mode").
        base.ToDesign();
        CleanupRuntime();
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        if (!_isRuntimeInitialized)
        {
            return;
        }

        ProcessCommand();
    }

    private void InitializeRuntime()
    {
        if (_isRuntimeInitialized)
        {
            return;
        }

        try
        {
            // If tree access needed:
            if (TreeItemHlp?.Project == null)
            {
                throw new InvalidOperationException("TreeItemHlp.Project is null.");
            }

            _service = new <FBName>Service(TreeItemHlp.Project);
            _previousTrigger = false;
            _isRuntimeInitialized = true;
        }
        catch (Exception exception)
        {
            _isRuntimeInitialized = false;
            WriteOutputs(false, $"Initialization failed: {exception.Message}");
        }
    }

    private void CleanupRuntime()
    {
        _isRuntimeInitialized = false;
        _service = null;
    }

    private void ProcessCommand()
    {
        var currentTrigger = GetPinValue<bool>(TriggerPinId);
        var isRisingEdge = currentTrigger && !_previousTrigger;
        _previousTrigger = currentTrigger;

        if (!isRisingEdge || _service == null)
        {
            return;
        }

        var result = _service.DoSomething("input");
        WriteOutputs(result.IsSuccess, result.IsFailed ? string.Join("; ", result.Errors) : string.Empty);
    }

    private void WriteOutputs(bool success, string errorMessage)
    {
        var now = DateTime.UtcNow;

        SetPinValue(SuccessPinId, success, now);
        SetPinValue(ErrorMessagePinId, errorMessage, now);
    }
}
```

Critical rules for the FB class:
- `sealed` is optional -- most NtoLib FBs are not sealed; add it only to forbid subclassing
- `[Serializable]` is required
- `[ComVisible(true)]` is required
- Generate a unique GUID for each new FB (use `Guid.NewGuid()` or an online generator)
- All non-serializable fields must be marked `[NonSerialized]`
- `_previousTrigger` must be reset to `false` in `InitializeRuntime()` so the first `true` after runtime start is detected
- Rising-edge pattern: `var isRisingEdge = currentTrigger && !_previousTrigger;`
- `WriteOutputs` helper centralizes all pin writes with a shared `DateTime.UtcNow` timestamp
- For tree-modifying FBs, call flush BEFORE `base.ToDesign()`

### Step 6: Update csproj

NtoLib uses explicit `<Compile Include>` entries. Add entries for every new `.cs` file and an `<EmbeddedResource>` entry for the XML.

```xml
<!-- In NtoLib.csproj, within an appropriate <ItemGroup> -->
<Compile Include="<FBName>\<FBName>FB.cs" />
<Compile Include="<FBName>\Facade\I<FBName>Service.cs" />
<Compile Include="<FBName>\Facade\<FBName>Service.cs" />
<EmbeddedResource Include="<FBName>\<FBName>FB.xml" />
```

Find the existing `<Compile Include>` entries for similar FBs (e.g. `LinkSwitcher\`) and add the new entries nearby for consistency.

### Step 7: Build and Verify

Run `dotnet build NtoLib.sln --no-incremental` from the repo root. The build must produce:
- **Zero new errors** (pre-existing warnings are acceptable)
- **Zero new warnings from the new FB code**

If the build fails, fix the issues before proceeding. Common causes:
- Missing `<Compile Include>` entry in csproj
- Pin ID mismatch between XML and code constants
- Missing `using` directives

### Step 8: Verify XML-Code Alignment

After the build passes, manually verify:
- Every `const int *PinId` in the FB class has a matching `<Pin>` or `<Pout>` entry in the XML with the same `ID` value
- No XML pin exists without a corresponding constant in code (dead pins cause confusion)
- Input pins use `GetPinValue<T>()`, output pins use `SetPinValue()`

### Step 9: Deploy and Register (local MasterSCADA testing)

A green build is NOT enough to see the FB in MasterSCADA. The DLL must be deployed AND
COM-registered, and registration **must run elevated**.

1. Deploy: `Build/Deploy.ps1` (build -> ILRepack merge -> copy `NtoLib.dll` to the install dir).
   **`Deploy.ps1` only copies -- it does NOT register COM.**
2. Register from an **Administrator** prompt in the MasterSCADA install dir:
   ```
   netreg.exe NtoLib.dll /showerror      (or: NtoLib_reg.bat, "Run as administrator")
   ```
   `netreg` writes `netreg.log` in its own (Program Files) directory at startup and aborts
   with `UnauthorizedAccessException` if not elevated -- registering nothing, often with no
   visible error. The HKLM COM keys also require elevation.
3. **Restart MasterSCADA** -- it caches library metadata and will not pick up new/updated
   types until restarted.

A **new GUID** (every new FB) or a **renamed type/namespace** requires re-running `netreg`
elevated; routine edits to an already-registered type do not. Skipping this is the #1 cause
of "the FB silently won't appear / won't instantiate, no exception, no logs" (the failure is
at COM `CoCreateInstance`, before any managed code runs).

## Common Pitfalls

- **netreg run non-elevated**: aborts on the `netreg.log` write to Program Files, registers
  nothing, FB never appears / silently fails to instantiate. Always run `NtoLib_reg.bat` as
  Administrator, then restart MasterSCADA.
- **Changing a `[Guid]` after first release** (or renaming the type's full name): existing
  projects can no longer create saved instances, and the new identity needs re-registration.
- **XML marked as `<Content>` instead of `<EmbeddedResource>`**: compiles fine but fails at runtime with missing resource errors.
- **`Id` instead of `ID` in XML**: silently ignored, pins will not be created.
- **Forgetting `[NonSerialized]` on service field**: causes serialization exceptions when MasterSCADA saves the project.
- **Calling `Connect()`/`Disconnect()` during runtime**: throws "modification forbidden in runtime mode" exception. Use deferred execution pattern.
- **Not resetting `_previousTrigger` in `InitializeRuntime()`**: first trigger after runtime start may be missed or falsely detected.
- **Redeclaring `[ComRegisterFunction]`/`[ComUnregisterFunction]`**: unnecessary -- `StaticFBBase` already provides them via inheritance, and no shipping NtoLib FB redeclares them. An FB missing from the palette is almost always an un-elevated or forgotten `netreg`, not these methods.
