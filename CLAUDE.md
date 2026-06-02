# NtoLib — Agent Instructions

NtoLib is a MasterSCADA 3.x Function Block library by Semiteq for industrial SCADA automation
(MBE/MOCVD semiconductor equipment). It produces a single DLL registered as a COM component
via `netreg.exe`.

- **Framework:** .NET Framework 4.8, C# 10, Nullable enabled
- **SDK:** .NET SDK 8.x (pinned in `global.json` to `8.0.420`, `latestPatch`).
- **Solution:** `NtoLib.sln` — three projects: `NtoLib` (main library), `Tests` (xUnit),
  `Installer` (WinForms `.exe` self-installer used by release artifacts).
- **csproj style:** SDK-style (`<Project Sdk="Microsoft.NET.Sdk">`) with explicit
  `<Compile Include>` entries — default item globs are disabled
  (`EnableDefaultCompileItems=false`, `EnableDefaultEmbeddedResourceItems=false`,
  `EnableDefaultNoneItems=false`) so the "no wildcards" rule still holds.
- **NuGet:** `PackageReference` only; versions centrally managed in
  `Directory.Packages.props` at the repo root (Central Package Management).

## Build

```powershell
dotnet build NtoLib.sln                        # un-merged DLL (used by tests)
dotnet build NtoLib/NtoLib.csproj -p:RunILRepack=true   # merged DLL (used for deployment)
Build/Deploy.ps1   # build + merge + copy to target machine (local debug only)
```

Releases are built **in CI** (`.github/workflows/release.yml`) on push of `v*` tag —
local `Build/Package.ps1` no longer exists. Locally, `Build/Deploy.ps1` is the only
script: build → ILRepack merge → copy to MasterSCADA install dir + config dir.
Required env vars: `BUILD_CONFIGURATION`, `REPO_ROOT`, `NTOLIB_DEST_DIR`,
`NTOLIB_CONFIG_DIR` (in Rider injected via `.run/Deploy Debug.run.xml`).

ILRepack merges all NuGet DLLs into a single `NtoLib.dll`, excluding vendor SDK DLLs
(`FB.dll`, `InSAT.*`, `MasterSCADA.*`). The merge step is implemented in
`NtoLib/ILRepack.targets` (invoked through MSBuild via the
`ILRepack.Lib.MSBuild.Task` package). The target is gated on the `RunILRepack` MSBuild
property so a plain `dotnet build` produces an un-merged DLL — this is intentional
and required, because `NtoLib` carries `[InternalsVisibleTo("Tests")]` and merging
internalised NuGet types into the assembly would collide with the Tests project's
own `<PackageReference>`s on the same packages (CS0433 ambiguity on
`FluentResults.Result<>`, `Microsoft.Extensions.*`, etc.). After the merged build,
`netreg.exe NtoLib.dll /showerror` registers the COM component for MasterSCADA.

## Release

Tag `vX.Y.Z` (or `vX.Y.Z-suffix` for prerelease) → `.github/workflows/release.yml`
runs on `windows-2025`: restore → build with `-p:Version=X.Y.Z` → test → ILRepack →
zip (`NtoLib_v<ver>.zip` containing `NtoLib.dll`, `System.Resources.Extensions.dll`,
`NtoLib_reg.bat`, `DefaultConfig/`) → build `Installer.exe` with the same `-p:Version=`
→ `softprops/action-gh-release@v2` attaches both the zip and the installer.exe to a
GitHub Release named `NtoLib X.Y.Z`. Annotated tag message becomes the release body
(fallback: `Release X.Y.Z`).

The `<Version>` in `NtoLib.csproj` is a **fallback for local builds only**; CI always
overrides via `-p:Version=$VERSION`. With `GenerateAssemblyInfo=true`, MSBuild propagates:
`AssemblyVersion`/`AssemblyFileVersion` = numeric prefix (suffix stripped),
`AssemblyInformationalVersion` = full string. `IncludeSourceRevisionInInformationalVersion=false`
suppresses the `+SourceRevisionId` suffix that .NET 8+ SDK adds by default.

## Test

```powershell
dotnet test NtoLib.sln
```

xUnit 2.9.3 + FluentAssertions 8.8.0 + Moq 4.20.72 + Xunit.SkippableFact 1.4.13.
Test areas: MbeTable, ConfigLoader, TrendPensManager, OpcTreeManager (acceptance,
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
├── Installer/       WinForms self-installer (.exe attached to GitHub Releases)
├── Build/           local debug-deploy script (Deploy.ps1)
├── Docs/            documentation
│   ├── architecture/     primer + NtoLib patterns (LLM-targeted, not user-facing)
│   ├── known_issues/     platform-level bug classes with cause and workaround
│   ├── mbe_table/        MBE recipe table sub-modules
│   └── <feature>.md      per-FB user documentation
├── DefaultConfig/   YAML configs shipped alongside the DLL
└── Resources/       vendor SDK DLLs (FB.dll, InSAT.*, MasterSCADA.*)
```

Release zip archives are produced by CI and live in GitHub Releases, not in the
working tree. The local `Releases/` directory (if present) is gitignored.

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

- `NtoLib.csproj` is **SDK-style** (`<Project Sdk="Microsoft.NET.Sdk">`) but uses
  **explicit `<Compile Include>` entries** — no wildcards. Default item globs are
  disabled via `EnableDefaultCompileItems=false`,
  `EnableDefaultEmbeddedResourceItems=false`, `EnableDefaultNoneItems=false`. Every new
  `.cs` file must be added manually.
- `GenerateAssemblyInfo=true` (SDK default). Assembly metadata properties (`AssemblyTitle`,
  `Description`, `Company`, `Product`, `Copyright`, `Version`) live in csproj `<PropertyGroup>`.
  `AssemblyTrademarkAttribute`, `[ComVisible(false)]`, and `[Guid("...")]` stay in
  `Properties/AssemblyInfo.cs` because the SDK has no MSBuild equivalent for them.
  `[InternalsVisibleTo("Tests")]` is in csproj as `<ItemGroup><InternalsVisibleTo Include="Tests" /></ItemGroup>`.
- FB XML pin configuration files must be included as **`<EmbeddedResource>`**, not `<Content>`:
  ```xml
  <EmbeddedResource Include="MyFeature\MyFB.xml" />
  ```
- When adding a new FB with multiple source files, add all `<Compile Include>` and the
  `<EmbeddedResource>` entry in a single csproj edit to avoid partial builds.
- `packages.config` is **gone** — the project uses `<PackageReference>` exclusively.

### Central Package Management

- NuGet package versions are centrally managed in `Directory.Packages.props` at the
  repo root (`ManagePackageVersionsCentrally=true`,
  `CentralPackageTransitivePinningEnabled=true`).
- Add a new package by appending a `<PackageVersion Include="..." Version="..." />`
  entry to `Directory.Packages.props`, then reference it from the consuming csproj
  with `<PackageReference Include="..." />` — **no `Version=` attribute on the
  `PackageReference`**. NuGet emits `NU1008` (hard error) if a `Version=` attribute
  is left in place under CPM, and `NU1010` if a referenced id has no
  `<PackageVersion>` entry.
- Dependabot is configured against the repo root and bumps versions in
  `Directory.Packages.props` only; csproj files are not touched by version updates.

### ILRepack and `[InternalsVisibleTo("Tests")]`

- The merge target lives in `NtoLib/ILRepack.targets` and is wired in via the
  `ILRepack.Lib.MSBuild.Task` package's `$(ILRepackTargetsFile)` hook.
- The target only runs when the `RunILRepack` MSBuild property is `true`. A plain
  `dotnet build` therefore produces an **un-merged** DLL — this is required for the
  Tests project to build, because merging internalises NuGet types that Tests also
  references directly.
- `release.yml` (and locally `Build/Deploy.ps1`) builds the solution un-merged first,
  then re-invokes `dotnet build NtoLib/NtoLib.csproj -c Release -p:RunILRepack=true`
  to produce the deployable merged artifact between the Test and Archive steps.
- `Build/Package.ps1`, `Build/tools/`, and `Build/tools/Merge.ps1` no longer exist;
  do not re-introduce PowerShell wrappers — the only local script is `Build/Deploy.ps1`.

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
