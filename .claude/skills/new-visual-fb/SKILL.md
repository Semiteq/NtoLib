---
name: new-visual-fb
description: Use when creating a brand-new visual Function Block in NtoLib — "add a new visual FB", "create a VisualFBBase block with a control", "new FB with a mnemoscheme control", "make an editor-style copy of an FB". Covers the XML Map/VisualMap, FB + VisualControlBase control with distinct GUIDs, palette bmp, DI composition, csproj registration, build, elevated COM registration, and the shared-core / thin-shell copy pattern. NOT for headless FBs with no control (use new-nonvisual-fb), nor for editing an existing FB in place.
---

# Create a New Visual Function Block

Build a new visual FB end to end: the `VisualFBBase` orchestrator, its `VisualControlBase` WinForms control, the `<Map>`/`<VisualMap>` XML, the palette icon, DI wiring, and the elevated COM registration that makes it droppable.

## When to Use

Use this skill when creating a brand-new visual FB: a class extending `VisualFBBase` (or
`VisualFBBaseExtended`) paired with a `VisualControlBase` WinForms control shown on a
MasterSCADA mnemoscheme. Also use it when the task is to **copy/derive** an existing visual
FB with a feature stripped or changed (see "Shared-Core / Thin-Shell" below) -- do NOT
duplicate the whole module tree.

**Base class:** use `VisualFBBase` for a control that builds a DI service graph in
`InitializeRuntime` (the templates below model this -- the MbeTable family). Use
`VisualFBBaseExtended` for simpler device-style FBs driven through `SetVisualAndUiPin` /
`GetVisualPin` / `EventTrigger` without DI (ValveFB/PumpFB). The templates below show the
`VisualFBBase` + DI variant; for the extended variant, model an existing
`VisualFBBaseExtended` FB instead.

For headless FBs (`StaticFBBase`, no control), use `new-nonvisual-fb` instead.

## Pre-Flight

Ask the user if not provided:

1. **FB name** -- e.g. `MbeTable`. Determines FB class (`MbeTableFB`), control class, folder,
   namespace, and the embedded XML/bmp names.
2. **Display name** -- palette name, goes in `[DisplayName(...)]` on BOTH the FB and the control.
3. **Category** -- `CatIDs.CATID_*`. Default `CatIDs.CATID_OTHER`.
4. **Pin layout** -- two groups: `<Map>` (logic pins) and `<VisualMap>` (design-time visual
   properties echoed to pins). Russian type names include `Логический`, `Строковый`,
   `Вещественный`, `Целый`, `БеззнаковыйЦелый`, `БеззнаковыйКороткийЦелый`, `Время` --
   match the exact spelling used in an existing working XML for the type you need.
5. **Does it need services / DI?** -- a non-trivial control uses a DI graph built in
   `InitializeRuntime`. A trivial control may not.
6. **Two fresh GUIDs** -- generate one for the FB class and a DISTINCT one for the control
   class; never reuse an existing type's GUID. Generate both with PowerShell:
   ```powershell
   [guid]::NewGuid(); [guid]::NewGuid()
   ```

## The FB <-> XML <-> resource naming rule (read first -- it is the #1 silent failure)

MasterSCADA locates an FB's embedded XML by **type full name**: the type
`NtoLib.Recipes.MbeTable.MbeTableFB` binds to the embedded resource
`NtoLib.Recipes.MbeTable.MbeTableFB.xml`. With `<RootNamespace>NtoLib</RootNamespace>`, an
`<EmbeddedResource Include="Recipes\MbeTable\MbeTableFB.xml"/>` gets exactly that manifest
name only because the folder path matches the namespace.

Therefore: **folder == namespace == the FB class's containing namespace.** If you move or
rename the folder/namespace, the XML/bmp resource names move with it (good), but every
existing COM registration that mapped the CLSID to the OLD type full name becomes stale --
re-register (see "Deploy and Register"). Verify the manifest name in the built DLL with
`grep -a -o "NtoLib\.[A-Za-z.]*<FBName>\.xml" NtoLib/bin/Debug/NtoLib.dll`.

## Step-by-Step Workflow

Track steps with the harness task tools (`TaskCreate` / `TaskUpdate`).

### Step 1: Folder Structure

```
NtoLib/<Area>/<FBName>/            # folder path MUST mirror the namespace
    <FBName>FB.cs                  # FB orchestrator (VisualFBBase)
    <FBName>FB.xml                 # EmbeddedResource, name == FB type full name
    <FBName>FB.bmp                 # EmbeddedResource palette icon, name == FB type full name
    <Control>.cs                   # control (VisualControlBase) + attributes
    <Control>.Designer.cs          # InitializeComponent (layout)
    <Control>.resx                 # EmbeddedResource, DependentUpon the control .cs
    <Control>.Lifecycle.cs / .Buttons.cs / ...  # optional partials
```

Keep the COM-bound shell thin; put domain/service logic in separate, COM-neutral classes
(reused, not duplicated -- see shared-core pattern).

### Step 2: XML -- `<Map>` and `<VisualMap>`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<FBConfig>
  <Map>
    <Items>
      <Pin  ID="1"   Name="SomeInput"  Type="Логический" DefaultValue="false"/>
      <Pout ID="101" Name="SomeOutput" Type="Вещественный"/>
    </Items>
  </Map>
  <VisualMap>
    <!-- design-time visual properties echoed to pins -->
    <Items>
      <Pout ID="1003" Name="SomeAddr" Type="БеззнаковыйЦелый"/>
    </Items>
  </VisualMap>
</FBConfig>
```

Rules: `<Pin>` inputs, `<Pout>` outputs; uppercase `ID`. `ID` is a free integer with no
platform-enforced ranges -- real NtoLib FBs use IDs like 5, 10, 50, 100, 1000. Any consistent
numbering works; a common (optional) convention keeps logic pins low and `<VisualMap>` pins in
a high band (~1000+). Empty `<Items></Items>` is valid if pins are added dynamically in code
(as the editor FB does), but the file must still exist and bind by name.

### Step 3: FB Class (`VisualFBBase`)

```csharp
[CatID(CatIDs.CATID_OTHER)]
[Guid("FRESH-GUID-FOR-THE-FB")]
[FBOptions(FBOptions.EnableChangeConfigInRT)]
[VisualControls(typeof(<Control>))]
[DisplayName("<Display Name>")]
[ComVisible(true)]
[Serializable]
public partial class <FBName>FB : VisualFBBase
{
    [NonSerialized] private IServiceProvider? _serviceProvider;

    protected override void ToDesign()  { base.ToDesign();  CleanupRuntime(); }
    protected override void ToRuntime() { base.ToRuntime(); InitializeRuntime(); }
    public override void Dispose()      { CleanupRuntime(); base.Dispose(); }

    private void InitializeRuntime()
    {
        if (_serviceProvider != null) return;
        // build config + DI graph here; surface failures (MessageBox + rethrow) so they
        // are visible at runtime. Design-time (drag-drop) must NOT throw.
    }

    private void CleanupRuntime() { /* dispose provider, null it */ }
}
```

Rules:
- `[VisualControls(typeof(<Control>))]` registers the default control. The attribute also has
  a params overload `[VisualControls(typeof(Default), typeof(Other), ...)]` that registers
  several controls; MasterSCADA then shows a right-click chooser at drop time. One default
  control is the common case.
- Fresh, unique `[Guid]`. `[ComVisible(true)]`, `[Serializable]`, public parameterless ctor
  (implicit is fine) -- COM activation requires it.
- All caches/services/providers `[NonSerialized]`.
- `ToDesign`/drag-drop path must not throw (the host swallows design-time exceptions -> the
  control silently fails to add). Do heavy work in `ToRuntime`/`InitializeRuntime`.
- Logging only bootstraps at runtime; do not expect design-time logs.

### Step 4: Control Class (`VisualControlBase`)

```csharp
[ComVisible(true)]
[DisplayName("<Display Name>")]
[Guid("FRESH-GUID-FOR-THE-CONTROL")]   // MUST differ from the FB's GUID
public partial class <Control> : VisualControlBase
{
    public <Control>() : base(true)
    {
        InitializeComponent();
    }
}
```

- Distinct fresh `[Guid]`, `[ComVisible(true)]`, public parameterless ctor.
- `InitializeComponent()` lives in `.Designer.cs`. If it references the control's own `.resx`
  via `new ComponentResourceManager(typeof(<Control>))`, the resx manifest name must equal
  the control's type full name (it does automatically when folder == namespace and the resx
  is `DependentUpon` the control `.cs`). Shared images via `global::NtoLib.Properties.Resources.*`
  do not depend on the control's resx.
- `VisualControlBase` exposes `FBConnector`; read `FBConnector.Fb` and bail out cleanly when
  it is not your FB type (design-time guard) -- do not throw.

### Step 5: Palette bmp

Add `<FBName>FB.bmp` next to the FB. EmbeddedResource; its manifest name must equal the FB
type full name + `.bmp`. A standard small palette icon; copy an existing FB's bmp as a
starting point if unsure.

### Step 6: csproj (globs are disabled -- add everything explicitly)

```xml
<Compile Include="<Area>\<FBName>\<FBName>FB.cs" />
<!-- all FB + control partials ... -->
<Compile Include="<Area>\<FBName>\<Control>.Designer.cs">
  <DependentUpon><Control>.cs</DependentUpon>
</Compile>
<EmbeddedResource Include="<Area>\<FBName>\<FBName>FB.bmp" />
<EmbeddedResource Include="<Area>\<FBName>\<FBName>FB.xml" />
<EmbeddedResource Include="<Area>\<FBName>\<Control>.resx">
  <DependentUpon><Control>.cs</DependentUpon>
</EmbeddedResource>
```

### Step 7: Build

- Un-merged (for tests): `dotnet build NtoLib.sln`
- Merged (deployable): `dotnet build NtoLib/NtoLib.csproj -p:RunILRepack=true`
- Confirm the embedded resource names in the DLL match the type full names (grep, see naming
  rule above). Run `dotnet format NtoLib.sln`.

### Step 8: Deploy and Register (REQUIRED to see/drop the control in MasterSCADA)

A green build is not enough. The control instantiates via COM `CoCreateInstance`; if the
CLSID is unregistered, it **silently fails to drop -- no exception, no logs**.

1. Deploy with `Build/Deploy.ps1` (build -> merge -> copy). **It does NOT register COM.**
2. From an **Administrator** prompt in the MasterSCADA install dir:
   `netreg.exe NtoLib.dll /showerror`  (or `NtoLib_reg.bat`, Run as administrator).
   netreg aborts non-elevated (cannot write `netreg.log` in Program Files) and registers
   nothing. HKLM COM keys also need elevation. Registering both the FB and control CLSIDs
   requires this elevated run.
3. **Restart MasterSCADA** (it caches library metadata).

Verify (optional):
`reg query "HKLM\SOFTWARE\WOW6432Node\Classes\CLSID\{<control-guid>}\InprocServer32"` ->
`Class` should equal the control's type full name.

## Shared-Core / Thin-Shell (copying an existing FB)

When asked for a "copy" of an FB with a feature stripped (e.g. a PLC-less editor), do NOT
duplicate the module. Create a new thin shell (FB + control + XML + bmp) with **fresh GUIDs**
and reuse the COM-neutral core verbatim:

- Never share a base class between two COM-visible FB/control types (it risks COM identity /
  serialization). Reuse via **static helpers and composition** (e.g.
  `RecipeFbConfigurationHelper`) -- pass platform bits (pin read via `OpcQuality`, tree
  mutation) in as delegates so the helper stays unit-testable from `Tests`.
- Fork the DI graph through one configurator with a shared `RegisterShared` and per-variant
  deltas (omit PLC services, substitute no-op/slim providers behind interfaces).
- The WinForms control shell is the one piece intentionally duplicated (COM-bound glue).

## Common Pitfalls

- **netreg run non-elevated** -> aborts on `netreg.log` write -> nothing registered -> control
  silently fails to drop, no exception, no logs. Run `NtoLib_reg.bat` as Administrator, then
  restart MasterSCADA. This is the dominant cause of "fails to add, no error."
- **Renaming the folder/namespace** of an existing FB changes its type full name -> the old
  CLSID registration is stale and `CoCreateInstance` silently fails until you re-register
  elevated. (The embedded XML/bmp names follow the namespace and stay correct automatically.)
- **FB and control sharing one GUID, or reusing an existing FB's GUID** -> CLSID collision;
  only one registers.
- **Throwing at design time** (FB ctor / `ToDesign` / control `InitializeComponent`) -> the
  host swallows it and the drop silently fails. Defer heavy work to `ToRuntime`.
- **XML/bmp/resx as `<Content>` instead of `<EmbeddedResource>`, or a folder that does not
  match the namespace** -> resource name will not match the type full name; the FB will not
  bind its config.
- **Changing a `[Guid]` or a public FB property name/type after release** -> existing projects
  can no longer load their saved instances (the COM identity / serialization contract changed).
- **Expecting design-time logs** -> logging only starts at `ToRuntime`. A design-time failure
  produces no logs by design.

## Key Rules (the load-bearing invariants)

- FB and control each carry a **distinct, fresh** `[Guid]`; never reuse one.
- **folder == namespace**, so the embedded XML/bmp manifest name equals the type full name.
- XML/bmp/resx are `<EmbeddedResource>` (never `<Content>`); every new `.cs` is an explicit
  `<Compile Include>` (globs are disabled).
- **Never throw at design time** (ctor / `ToDesign` / `InitializeComponent`) -- defer heavy
  work to `ToRuntime`.
- **Never share a base class** across two COM-visible FB/control types; reuse via static
  helpers / composition.
- After deploy, run **`netreg` elevated + restart MasterSCADA** whenever a type is added,
  renamed, or its GUID changes.
