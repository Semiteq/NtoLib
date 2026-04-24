# NtoLib — Agent Instructions

NtoLib is a MasterSCADA 3.x Function Block library by Semiteq for industrial SCADA automation
(MBE/MOCVD semiconductor equipment). It produces a single DLL registered as a COM component
via `netreg.exe`.

- **Framework:** .NET Framework 4.8, C# 10, Nullable enabled
- **Solution:** `NtoLib.sln` — two projects: `NtoLib` (main library) and `Tests` (xUnit)
- **csproj style:** Old-style with explicit `<Compile Include>` entries (no wildcards)

## Build

```powershell
dotnet build NtoLib.sln
Build/Package.ps1   # build + ILRepack merge + archive
Build/Deploy.ps1    # build + merge + copy to target machine
```

ILRepack merges all NuGet DLLs into a single `NtoLib.dll`, excluding vendor SDK DLLs
(`FB.dll`, `InSAT.*`, `MasterSCADA.*`). After build, `netreg.exe NtoLib.dll /showerror`
registers the COM component for MasterSCADA.

## Test

```powershell
dotnet test NtoLib.sln
```

xUnit 2.9.3 + FluentAssertions 8.8.0 + Moq 4.20.72 + Xunit.SkippableFact 1.4.13.
Coverage: MbeTable, ConfigLoader, TrendPensManager, OpcTreeManager (acceptance,
integration, unit).

## Format

```powershell
dotnet format NtoLib.sln
```

Always run before presenting changes. Enforces all standard editorconfig rules at
`:warning` severity. Does not enforce ReSharper-specific rules — those require Rider
Code Cleanup or `jb cleanupcode`.

## Project Layout

```
NtoLib/
├── NtoLib/          main library (FB implementations)
├── Tests/           xUnit + FluentAssertions + Moq
├── Build/           PowerShell pipeline (Package.ps1, Deploy.ps1, tools/)
├── Docs/            documentation
│   ├── architecture/     primer + NtoLib patterns (LLM-targeted, not user-facing)
│   ├── known_issues/     platform-level bug classes with cause and workaround
│   ├── mbe_table/        MBE recipe table sub-modules
│   └── <feature>.md      per-FB user documentation
├── DefaultConfig/   YAML configs shipped alongside the DLL
├── Resources/       vendor SDK DLLs (FB.dll, InSAT.*, MasterSCADA.*)
└── Releases/        versioned zip archives
```

## Two FB Architectures

| Aspect | Headless FB | Visual FB |
|--------|-------------|-----------|
| Base class | `StaticFBBase` | `VisualFBBase` (or `VisualFBBaseExtended`) |
| Layers | FB orchestrator + service facade | FB + Control + Status DTO + Renderer |
| XML sections | `<Map>` only | `<Map>` + `<VisualMap>` + optional `<Events>` |
| Reference | `ConfigLoader/`, `LinkSwitcher/` | `Devices/Valves/`, `Devices/Pumps/` |

Platform primer and detailed NtoLib-specific patterns, lifecycle templates, and
checklists: [`Docs/architecture/`](Docs/architecture/) (see Documentation Index below
for the reading order).

## csproj Conventions

- `NtoLib.csproj` uses **explicit `<Compile Include>` entries** — no wildcards. Every new
  `.cs` file must be added manually.
- FB XML pin configuration files must be included as **`<EmbeddedResource>`**, not `<Content>`:
  ```xml
  <EmbeddedResource Include="MyFeature\MyFB.xml" />
  ```
- When adding a new FB with multiple source files, add all `<Compile Include>` and the
  `<EmbeddedResource>` entry in a single csproj edit to avoid partial builds.

## Dependencies

- **Vendor SDK** (in `Resources/`, never merged): `FB.dll`, `InSAT.Library.dll`,
  `MasterSCADA.Common.dll`, `MasterSCADA.Trend.dll`, `MasterSCADALib.dll`.
- **NuGet** (merged into `NtoLib.dll` by ILRepack): `FluentResults`, `YamlDotNet`, `Serilog`
  (+ Console/Debug/File sinks), `Microsoft.Extensions.DependencyInjection`,
  `Microsoft.Extensions.Logging`, `CsvHelper`, `EasyModbusTCP`, `AngouriMath`,
  `System.Text.Json`, `Polly`, `OneOf`, `System.Collections.Immutable`.

## Documentation Index

**Required reading before touching any FB code** (both files, in order):

- [`Docs/architecture/masterscada-fb-primer.md`](Docs/architecture/masterscada-fb-primer.md) —
  MasterSCADA 3.12 platform primer. What an FB is, base class hierarchy
  (`StaticFBBase` vs `VisualFBBase`), the pin system, lifecycle (`ToRuntime` / `UpdateData`
  / `ToDesign`), XML configuration, COM registration, threading model, and the
  platform-level pitfalls that keep recurring.
- [`Docs/architecture/architecture.md`](Docs/architecture/architecture.md) — NtoLib-specific
  patterns on top of the platform: visual FB 4-layer split, headless FB thin-orchestrator
  pattern, deferred execution template, file-based logging, test-tier structure.

Reference material:

- [`Docs/readme.md`](Docs/readme.md) — TOC of all per-FB user documentation
- [`Docs/known_issues/`](Docs/known_issues/) — **catalogue of platform-level bug classes
  that NtoLib has been bitten by, with symptom / cause / workaround per entry.** Always
  `ls Docs/known_issues/` before assuming a new failure mode is novel. Specifically check
  it before touching deferred-execution FBs, deployment scripts, pin/XML mappings, or
  visual-control `BackColor` behaviour.

## Working Conventions

- Branches: `feature/<issue_number>` (e.g., `feature/72`). Created from up-to-date `master`.
- Treat each issue as a **vertical slice**: include XML, runtime logic, UI/control, and
  renderers (where applicable) in a single branch.
- After any code change: run `dotnet format NtoLib.sln`.
- For FB/XML integration changes, runtime validation in MasterSCADA host comes before
  unit tests — `NullReferenceException` from `SetPinValue` is almost always an ID/XML
  mismatch, not a logic bug. See
  [`Docs/known_issues/09-mismatched-pin-ids.md`](Docs/known_issues/09-mismatched-pin-ids.md).

## Note on `CLAUDE.md`

`CLAUDE.md` is kept in sync with `AGENTS.md` automatically by a pre-commit hook
(`.githooks/pre-commit`). To enable the hook locally, run once after cloning:

```powershell
git config core.hooksPath .githooks
```

Edit `AGENTS.md` only — the hook will copy it to `CLAUDE.md` on commit.
