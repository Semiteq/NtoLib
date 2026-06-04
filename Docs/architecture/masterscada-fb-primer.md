# MasterSCADA 3.12 — FB Platform Primer

Practical distillation of the vendor platform model that NtoLib targets.
Covers FB classification, base classes, pin system, lifecycle, XML
configuration, COM registration, and the constraints that drive NtoLib's
architectural patterns (see [`architecture.md`](architecture.md) for how we
apply these rules).

This document exists because the vendor distributes no English SDK
documentation; the material below is reconstructed from decompiled
`FB.dll` / `MasterSCADA.Common.dll` / `MasterSCADALib.dll` and vendor
Russian-language PDFs. Read this once before writing an FB and reference
when something doesn't behave as expected.

Platform-level bug-class gotchas live in [`../known_issues/`](../known_issues/) —
always check there before debugging unusual failures.

---

## 1. What is an FB

A **Function Block** (Функциональный Блок / ФБ) is the unit of
user-defined logic in a MasterSCADA project. Every FB instance is a node
in the project's object tree (`ITree` / `ITreeItem`), participates in the
cyclic runtime, and exchanges data with other blocks through typed pins.

FBs come in two architectural families:

- **Headless FB** — pure logic, no on-screen representation. Base class
  `StaticFBBase`. XML describes pins only.
- **Visual FB (VFB)** — logic plus one or more WinForms / windowless
  controls rendered on mnemonic diagrams. Base class `VisualFBBase`. XML
  describes both pins and the visual channel.

NtoLib has both kinds. See [`architecture.md`](architecture.md) for the
NtoLib-specific layering (thin-orchestrator for headless, 4-layer for
visual).

---

## 2. Base Class Hierarchy

```
FBDesignBase                         design-time shell
  └── FBBase                         adds runtime: ToRuntime/UpdateData/ToDesign
        └── DynamicFBBase            runtime-variable pin layouts (rare)
              └── StaticFBBase       XML-defined pins — headless FBs extend this
                    └── VisualFBBase adds VisualPins + WinForms binding layer
```

### Static vs Dynamic

- **`StaticFBBase`** — the standard choice. Pin layout is fixed at build
  time by an XML configuration cached in `FBsConfigCache`. Every NtoLib
  headless FB uses this base.
- **`DynamicFBBase`** — supports runtime-defined pin structures via
  `[DynamicFBAttribute]`. Used when the same FB class must expose
  different pin counts/types per instance (configurable at design time).
  NtoLib does not currently use this; avoid unless there's a concrete
  need. `StaticFBBase` inherits from `DynamicFBBase` but the dynamic
  machinery is dormant when XML pins are used.

### Visual vs Headless — when to pick which

| Question | Visual FB | Headless FB |
|---|---|---|
| Needs a mnemoscheme widget the operator clicks/sees? | yes | no |
| Pure data transformation / tree management / logging? | no | yes |
| Reference | `Devices/Valves/`, `Devices/Pumps/` | `ConfigLoader/`, `LinkSwitcher/`, `OpcTreeManager/` |

A headless FB can expose operator-facing diagnostics via pins and scripts
without ever becoming a VFB. Reach for VFB only when you need custom
rendering or click handling, not for data display.

---

## 3. The Pin System

### Terminology

- **Pin** (Вход) — an input. Values flow into the FB.
- **Pout** (Выход) — an output. Values flow out from the FB.
- **Group** — a named cluster of pins for organisation; can carry default
  `Options` like `Archiving`.
- **VisualPin / VisualPout** — for VFBs, the channel between the FB and
  its WinForms control. Lives in `<VisualMap>`, separate ID space from
  regular pins.

At design time a pin is described by a `PinDef`; at runtime by an
`RTPinHlp` holding value, quality (OPC DA model), and timestamp.

### Pin types (the XML `Type=` attribute)

MasterSCADA uses Russian-language type names mapped to .NET types by
`EMSPinTypeConvertor`:

| XML `Type=` | .NET type | Notes |
|---|---|---|
| `Логический` | `bool` | |
| `Целый` | `int` / `uint` (variant) | |
| `Вещественный` | `double` | |
| `Строковый` | `string` | |
| `Время` | `DateTime` | UTC required in runtime writes |
| `Нет` | `object` | used for visual pins where type is inferred |

### Pin IDs

Every `<Pin>` / `<Pout>` in XML declares an `ID` attribute. This ID is
**the contract** between XML and code:

- In code, declare a constant: `private const int ExecutePinId = 1;`
- In XML, `<Pin ID="1" Name="Execute" Type="Логический"/>`
- At runtime, `GetPinValue<bool>(ExecutePinId)` and
  `SetPinValue(OutputPinId, value, PinQuality.Good, DateTime.UtcNow)`
  resolve against the XML.

IDs must be unique per FB (across both `<Map>` and `<VisualMap>` for
VFBs). Mismatches between XML ID and code constant compile cleanly but
fail at runtime with `NullReferenceException` from helpers like
`SetPinValue`. Always suspect ID/XML mismatch first for such NREs —
[`../known_issues/09-mismatched-pin-ids.md`](../known_issues/09-mismatched-pin-ids.md).

### Pin access helpers on `StaticFBBase`

```csharp
bool     GetPinBool(int pinID);
int      GetPinInt(int pinID);
uint     GetPinUint(int pinID);
double   GetPinDouble(int pinID);
string   GetPinString(int pinID);
DateTime GetPinDateTime(int pinID);
T        GetPinValue<T>(int pinID);          // generic
object   GetPinValue(int pinID);              // untyped

void SetPinValue(int pinID, object value,
                 PinQuality quality = PinQuality.Good,
                 DateTime timeStamp = default);

// Diagnostics:
bool IsValueExist(int pinID);
bool IsValueExistOnAllPins();
PinQuality GetPinQuality(int pinID);
DateTime   GetPinTimeStamp(int pinID);
```

`SetPinValue` with `timeStamp = default` uses `DateTime.MinValue`, which
is almost always wrong. Pass `DateTime.UtcNow` explicitly. Never pass
local time — MasterSCADA's archive and synchronisation subsystems
assume UTC.

---

## 4. FB Lifecycle

Three callback points, invoked by the runtime host on the STA thread:

```csharp
public override void ToRuntime()
{
    base.ToRuntime();
    // allocate services, open files, reset state
}

public override void UpdateData()
{
    base.UpdateData();
    // called once per scan cycle: read pins, compute, write pouts
}

public override void ToDesign()
{
    // release runtime resources, queue deferred tree mutations
    base.ToDesign();
}
```

### `UpdateData()` constraints

- Called on the main scan thread (STA). Deterministic, short, non-blocking.
- No long-running I/O, no database calls, no `Thread.Sleep`, no
  synchronous network requests.
- If heavier work is needed, queue it to a background worker and
  publish a summary through pouts on completion.

### `ToRuntime` / `ToDesign`

- `ToRuntime`: allocate everything that must not persist in the project
  file. Typical: logger, service facade, plan queue, cached config.
- `ToDesign`: dispose runtime resources. If the FB has a pending tree
  mutation, this is where you post the deferred execution (see below).
  Return to design mode invokes this; the FB instance is then typically
  **replaced** before the next `ToRuntime`
  ([`../known_issues/07-fb-instance-replacement.md`](../known_issues/07-fb-instance-replacement.md)).

### Design mode vs Runtime

- **Design mode** — the project is being edited. Tree mutations are
  allowed. `FBConnector.DesignMode == true` in visual controls.
- **Runtime** — the project is executing. `IProjectHlp.InRuntime == true`.
  Tree mutations throw
  `"Изменение проекта в режиме исполнения запрещено."`.
  See [`../known_issues/06-runtime-tree-modification-forbidden.md`](../known_issues/06-runtime-tree-modification-forbidden.md).

### Runtime preparation order

The Design → Runtime transition is a staged state machine,
`EPrepareRuntimeState` (`MasterSCADALib/EPrepareRuntimeState.cs`):

```
prsInitRuntime(0) → prsBeforePrepareFB(1) → prsAfterPrepareFB(2)
  → prsAfterSetConnections(3) → prsAfterPrepareArchives(4)
  → prsAfterPrepareDocuments(5) → prsBeforeStart(6) → prsStartRuntime(7)
```

FBs are driven into runtime around `prsBeforePrepareFB`; mnemoscheme
documents and their visual controls around `prsAfterPrepareDocuments`.
**In the normal startup path, every FB's `ToRuntime` completes before any
visual control's `put_DesignMode(0)` fires.** This is why a control that
reads state prepared by its FB (e.g. MbeTable's `ServiceProvider`) almost
never observes it missing.

Caveat: the per-object dispatch order lives in the native kernel
(`IStarter.PrepareRuntime` is `[ComImport]`); the ordering is an observed
invariant, not a managed-code contract. Control-side init must still
tolerate a missing/not-ready FB (null-bail and retry on the next
lifecycle callback).

`RTManager.Instance` (process-static singleton,
`MasterSCADA.Common/MasterSCADA/RT/RTManager.cs`) exposes ordered
`PrepareRuntime(RTManager, EPrepareRuntimeState)` and
`RollbackRuntime(RTManager, ERollbackRuntimeState)` events. The vendor's
own `ControllerProxyFB` subscribes to them to build/tear down
infrastructure at an exact stage — a legal hook when `ToRuntime` arrives
too early or too late for a need. Warning: handler exceptions propagate
(`FireEvent` with `throwException: true`) and abort the runtime prepare
sequence; handlers must be defensive. Use `RTManager.HasInstance` /
`DirectInstance` for null-safe access outside runtime.

---

## 5. XML Configuration

### Required files for every FB

Three artifacts, all in the same assembly, with matching base names:

1. `MyFeatureFB.cs` — the FB class.
2. `MyFeatureFB.xml` — pin configuration, **`<EmbeddedResource>`** in
   csproj (not `<Content>`).
3. `MyFeatureFB.bmp` — 16×16, 16-colour palette icon,
   **`<EmbeddedResource>`** in csproj.

All three must share the class base name. `FBsConfigCache` locates the
XML through the FB type, and the palette uses the BMP.

### `<FBConfig>` skeleton

```xml
<?xml version="1.0" encoding="utf-8" ?>
<FBConfig>
  <Map>
    <Items>
      <Pin  ID="1" Name="Execute" Type="Логический"/>
      <Pin  ID="2" Name="Cancel"  Type="Логический"/>
      <Pout ID="101" Name="IsPending" Type="Логический"/>
    </Items>
  </Map>

  <!-- VFBs add: -->
  <VisualMap>
    <Items>
      <Pout ID="10" Name="В контрол"   Type="Нет"/>
      <Pin  ID="11" Name="От контрола" Type="Нет"/>
    </Items>
  </VisualMap>
</FBConfig>
```

- `<Map>` — always required.
- `<VisualMap>` — VFBs only. Separate ID space from `<Map>`; keep the
  ranges clearly disjoint (e.g., 1-99 for Map, 100+ for VisualMap, and
  10-99 for VisualMap in VFB-only FBs) to avoid confusion.
- `<Events>`, `<Rights>` — advanced, rarely needed in NtoLib.

### `<Items>` vs `<ItemsRange>`

- `<Items>` — explicit list of `<Pin>` / `<Pout>`. Use this by default.
- `<ItemsRange>` — template-generated pin series (numbered array). Only
  needed for FBs with large, uniform pin sets (e.g., `MbeTable`). The
  two are mutually exclusive within one `<Map>`.

### Pin attributes

| Attribute | Required | Meaning |
|---|---|---|
| `ID` | yes | Unique identifier within the FB |
| `Name` | yes | Display name in the FB's pin list; Cyrillic allowed |
| `Type` | yes | Russian type name (see table in §3) |
| `DefaultValue` | no | Initial value when the FB is instantiated |
| `Options` | no | `Archiving`, `DisableArchivingByChange` |
| `TypeProperty` | no | Name of a C# property that overrides the type at design time |
| `VisibleProperty` | no | Name of a C# property that controls pin visibility |

---

## 6. Class Attributes

A typical headless FB class declaration:

```csharp
[Serializable]
[ComVisible(true)]
[Guid("3F7A9C2E-B841-4D56-A0F3-8C1E2D5B7094")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("My Feature")]
public sealed class MyFeatureFB : StaticFBBase
{
    // ...
}
```

- **`[Serializable]`** — required. The runtime serialises the FB instance
  on project save. Every non-transient field is persisted; transient
  fields (logger, service, cached COM references) must be marked
  `[NonSerialized]`. Forgetting this attribute on a runtime-only field
  causes a save-time exception.
- **`[ComVisible(true)]`** + **`[Guid("...")]`** — generate a fresh GUID
  per FB class (Visual Studio → Tools → Create GUID). This is the CLSID
  used by `netreg.exe` and the MasterSCADA palette to identify the FB.
  **Never reuse or regenerate the GUID once shipped** — existing project
  files reference it.
- **`[CatID(CatIDs.CATID_...)]`** — palette category. Common values:
  `CATID_CALCUL`, `CATID_OTHER`, `CATID_VAV_PROCESS_SIGNAL`,
  `CATID_MECHANISM`.
- **`[DisplayName("...")]`** — human-readable name in the palette.
- **`[ControllerCode(...)]`** — binds the FB to a controller-side C
  implementation. NtoLib does not target controllers; leave unset.
- **`[FBOptions(...)]`** — behaviour flags, e.g., `UseScanByTime`,
  `EnableChangeConfigInRT`. Default is usually fine.
- **`[VisualControls(typeof(MyControl))]`** — VFB-only, binds the FB to
  one or more WinForms control types. First type is the default control
  when the FB is dragged into a mnemoscheme.

### Per-field / per-property attributes

- **`[NonSerialized]`** on runtime-only fields — critical, see above.
- **`[FBRetain]` / `[FBNonRetain]`** — mark fields that should / should
  not survive a runtime hot-restart (when "Restore on restart" is
  enabled in the FB properties). Default is no retention.
- **`[ConfigProperty(order)]`** — includes the property in the
  configuration block sent to a controller. Controller-integrated FBs
  only.

---

## 7. Threading

MasterSCADA is a **Single-Threaded Apartment (STA)** host:

- `UpdateData()` runs on the scan thread (STA).
- `ToRuntime`, `ToDesign`, and COM callbacks run on STA threads.
- All MasterSCADALib COM interfaces are **not thread-safe**.

Consequences:

- Do not spawn background threads that mutate FB fields or touch COM
  without proper marshaling.
- WinForms controls in VFBs live on the UI STA thread; cross-thread
  access requires `Control.Invoke`.
- `async` / `await` inside `UpdateData()` is dangerous — the scan cycle
  expects synchronous completion. If you need asynchrony, own your
  threading externally and synchronise updates to pouts through a lock.

---

## 8. VFB — Visual-Specific Additions

Beyond the headless FB contract, VFBs add:

- **`VisualPins`** collection — the FB reads/writes visual pin values
  via `VisualPins.GetPinValue`/`SetPinValue` using IDs from `<VisualMap>`.
- **`CreateVisualPinMap()`** — virtual, override to build the visual
  pin structure programmatically (rare; usually the XML `<VisualMap>` is
  enough).
- **`OnVisualPinChanged(int pinId)`** — virtual, called when a visual
  pin changes. Override to validate or reformat values before they
  propagate.
- **`[VisualControls(typeof(ControlType1), typeof(ControlType2))]`** —
  class attribute listing the allowed WinForms controls.

### Control classes

WinForms controls derive from **`VisualControlBase`**:

```csharp
[ComVisible(true)]
[Guid("A3B12F40-...-...")]
[DisplayName("Valve Control")]
public partial class ValveControl : VisualControlBase
{
    public ValveControl() : base(enableTimerRedraw: true) { /*...*/ }

    protected override void OnPinReceive(int pinID, bool valueChanged)
    {
        // React to FB → control data flow
    }

    // Use FBConnector.SetPinValue(pinId, value) to push control → FB
}
```

- `VisualControlBase` constructor takes a `bool` enabling a timer-based
  repaint loop — required for blinking, animation, and any
  auto-refreshing visuals.
- `FBConnector.DesignMode` distinguishes editor from runtime; skip
  animations in design mode.
- `FBConnector.SetPinValue(id, value)` writes back into the FB's
  visual pin (user interaction → FB logic).
- `[ComVisible(true)]` + `[Guid("...")]` are as strict as on the FB
  class itself; mismatches between build versions break runtime activation.

### FB ↔ Control rendezvous internals

The FB and its visual control are **two independent COM objects with
unsynchronized lifetimes**. The mechanics of how they find each other
live in `FB.dll` (`VisualFB/`); the facts below determine how control
initialization must be written.

**There is no separate "mnemoscheme FB instance".**
`VisualFBConnector.Connect()` resolves
`Attribute.TreeItem.GetChild(Rid).FBObject` — the *same* tree FB object
the runtime drives (`VisualFBConnector.cs:315-323`). The managed wrapper
is cached per tree node (`ITreeItemHlp.cs:217-226`) and deduplicated by
`ItemWrapperManager`. Whatever object graph the FB builds in `ToRuntime`,
the control sees that exact instance through `FBConnector.Fb`. (Multiple
live instances per node exist only for "callable"/multi-cycle objects —
a per-project customer setting, not something an FB class declares.)

**`FBConnector.Fb` is transient before the connector's own `ToRuntime`.**
When the cached `_fb` is null, the getter does Connect → read →
**Disconnect**, re-resolving on every access (`VisualFBConnector.cs:287-299`).
Only after `FBConnector.DesignMode = false` (which calls the connector's
`ToRuntime` → `Connect()` without a trailing `Disconnect`) is the
reference held persistently. Do not hammer `Fb` in hot paths at design
time. For a cheap, non-throwing, side-effect-free "am I linked" check use
`FBConnector.IsConnected` (`VisualFBConnector.cs:331-337`) — the platform
itself calls it from `OnPaint`.

**Control init triggers are asymmetric.**

| Trigger | Fires when | FB ready? |
|---|---|---|
| `put_DesignMode(0)` | design→runtime toggle; connector `ToRuntime` runs first, persisting `_fb` | yes, in the normal startup path (see §4 preparation order) |
| `OnFBLinkChanged` | `VisualFBRid` setter, i.e. (re)link to an FB — possibly at design time, possibly before the FB's `ToRuntime` | **no guarantee** |

Idempotent init triggered from *both*, guarded by an `initialized` flag
and a null-check on FB-prepared state, is the correct shape (this is what
MbeTable's `TableControl` does). Neither trigger alone covers all
orderings.

**Platform-sanctioned FB → Control signal: a visual pin.** The FB writes
a dedicated visual `Pout`; the control reacts in
`OnPinReceive(pinId, valueChanged)` (`VisualControlBase.cs:103`). The
subscription is held by the platform, so it survives FB instance
replacement with no leak — unlike a .NET event on the FB, which the
control would have to unsubscribe from exactly the right (possibly
already replaced) instance. Caveat: `OnPinReceive` arrives on the scan
thread; marshal UI work with `BeginInvoke` (see `ValveControl` for the
existing idiom, including the `+1000` visual pin ID offset).

**Mechanisms that look usable but are not:**

- *Shared project-level service container* (`IProjectHlp.GetService<T>` /
  `RTManager.Services`). The resolution chain only consults
  `ProjectHlp.Services` with `MasterSCADAHlp` as fallback,
  `DefaultServiceContainer.AddService` silently ignores duplicate type
  registrations, and the container is SCADA-scoped, not FB-scoped. Not
  suitable for sharing FB-specific object graphs.
- *.NET readiness event on the FB* (e.g. `ServicesReady`). The control
  cannot manage its subscription lifetime safely: the FB instance it
  subscribed to may be replaced at any cycle boundary
  ([`../known_issues/07-fb-instance-replacement.md`](../known_issues/07-fb-instance-replacement.md)),
  leaving leaked delegates on dead instances. Signal through a visual pin
  instead.
- *`ITreeItem.ReinstallObject` for forcing instance replacement.* Its only
  managed call site is a design-time pin resync. The actual replacement
  mechanism is native-side and not visible in the decompiled .NET code;
  `known_issues/07` documents the observable behaviour.

### Windowless controls

`VisualWindowlessControlBase` — for lightweight indicators without a
`Control` handle (fewer OS resources, scales better for dense
mnemoschemes). No WinForms designer; code all drawing, layout, and
hit-testing manually.

### WPF inside VFB

MasterSCADA's designer does not directly host WPF. If you need WPF,
wrap it in a WinForms `UserControl` deriving from `VisualControlBase`
and host the WPF child via `ElementHost`. All COM/SCADA integration
stays in the WinForms wrapper.

### Transparent backgrounds — DO NOT just set `BackColor = Color.Transparent`

Setting `BackColor = Color.Transparent` on a `VisualControlBase`-derived
control triggers the `WS_EX_TRANSPARENT` window style and causes a
repaint storm that freezes MasterSCADA's designer. See
[`../known_issues/01-back-color-transparent.md`](../known_issues/01-back-color-transparent.md)
for the workaround.

---

## 9. COM Registration

An FB class, to be loadable by MasterSCADA, must be registered as a COM
type. The chain:

1. Class carries `[ComVisible(true)]` + `[Guid]`.
2. `[ComRegisterFunction]` / `[ComUnregisterFunction]` on a static
   method of the class (or on `StaticFBBase.RegisterFunction`) writes
   the CLSID into the registry.
3. `netreg.exe NtoLib.dll /showerror` during deployment walks the
   assembly and applies the registration.

NtoLib's deployment pipeline (`Build/Deploy.ps1`) handles the
`netreg.exe` step. For local iteration, build the merged DLL
(`dotnet build NtoLib/NtoLib.csproj -p:RunILRepack=true`), copy it to the
MasterSCADA install directory and re-run `netreg.exe` from an elevated
console.

COM registration caches persist — if MasterSCADA is loading a stale
version of an FB, check the registry under
`HKEY_CLASSES_ROOT\CLSID\{GUID}`.

---

## 10. Error Handling

### Per-pin quality

`PinQuality` (OPC DA model) accompanies every pin value:

- `Good` — value is trusted.
- Bad-quality codes — value should be treated as stale / invalid.

Patterns:

- Check input quality with `GetPinQuality(pinID)` before computing on
  the value. Propagate bad quality to outputs when appropriate.
- `SetErrorOnPout(quality)` marks all outputs at once — useful when a
  fatal condition (missing config, dead communication) invalidates
  everything the FB produces.

### Logging

- `ReportError(string msg, bool isError)` — writes to MasterSCADA's
  operation log. Useful for design-time / config-level problems that
  the operator should see.
- For FB-internal diagnostics, NtoLib uses **Serilog** file logging (see
  [`architecture.md`](architecture.md) "File-Based Logging"). Output
  pins are unreliable for reporting deferred-execution results because
  the FB instance is replaced between runtime cycles
  ([`../known_issues/07-fb-instance-replacement.md`](../known_issues/07-fb-instance-replacement.md)).

### Exceptions

- Do not let exceptions escape `UpdateData()` unhandled. The runtime
  tolerates this but reports errors in an unhelpful way; wrap the
  per-cycle work in `try { ... } catch { _log.Error(ex, "..."); }`.
- Exceptions thrown from `ToRuntime` typically prevent the runtime
  cycle from starting — this is the desired behaviour for
  initialisation failures.

---

## 11. Known Platform Pitfalls

These are bug classes that have bitten NtoLib multiple times. Each has
a dedicated `../known_issues/` entry with the full symptom / cause / fix.
Before writing new code in these areas, read the referenced issue.

| Platform behaviour | Symptom | KnownIssue |
|---|---|---|
| Tree mutations forbidden in Runtime | `"Изменение проекта в режиме исполнения запрещено."` | `06-runtime-tree-modification-forbidden.md` |
| FB instance is replaced between runtime cycles | Deferred-execution results lost; fields reset | `07-fb-instance-replacement.md` |
| `BeginInvoke` self-reposting doesn't wait for Runtime → Design | Deferred delegate fires too early | `08-begininvoke-reposting-fails.md` |
| Mismatched pin IDs between code and XML | `NullReferenceException` from `SetPinValue` / `GetPinValue` | `09-mismatched-pin-ids.md` |
| `BackColor = Color.Transparent` on visual controls | MasterSCADA designer freezes, paint storm | `01-back-color-transparent.md` |
| DLL merge constraints with ILRepack | Missing types at runtime, COM activation failure | `02-dll-merge-constraints.md` |
| Project caching across hot restarts | Stale FB state or config | `03-project-caching-and-serialization.md` |
| Deployment / registration failures | FB not visible in palette | `04-deployment-errors.md` |
| Command-pin connects require `ctIConnect` | `ArgumentOutOfRangeException` on Connect | `05-opc-command-pin-connect-overload.md` |

The list grows — always `ls Docs/known_issues/` before assuming a new
failure mode hasn't been documented.

---

## 12. When the Platform Model Breaks Down

Some patterns look right according to this primer but fail in practice
because the vendor model has undocumented edges. NtoLib captures these
in `../known_issues/`. When something fails unexpectedly:

1. Check `Docs/known_issues/` first — the specific failure mode is
   often already logged.
2. If the vendor's behaviour contradicts this primer, the vendor wins —
   update the primer or add a `../known_issues/` entry, don't paper over it.
3. The decompiled sources in `C:\Users\admin\projects\MasterScada3Wiki`
   (internal, not distributed) are the ground truth when interpretation
   diverges. Cite them in PR discussions when necessary.

The decompiled vendor code is ground truth for **behaviour**, not a style
reference. The vendor codebase uses no DI container (collaborators come
from direct construction, reflection, or service location against static
singletons), concentrates thousands of lines in single classes, and
swallows exceptions broadly. When looking for a pattern to imitate,
prefer the conventions in [`architecture.md`](architecture.md) over
anything found in the decompiled sources.
