# Migrate NtoLib to PackageReference + Central Package Management

## Overview

Today the solution mixes two NuGet formats: `NtoLib.csproj` is old-style with
`packages.config` (50+ packages, restored to `..\packages\`), while
`Tests.csproj` is sdk-style with `PackageReference` (restored to the global
NuGet cache). Because the formats are isolated, dependabot bumps in NtoLib
break the test project at runtime — Tests sees `NtoLib.dll` but its own
auto-generated `Tests.dll.config` does not include binding redirects for
NtoLib's transitive dependencies. The dependabot fallout that triggered this
plan was exactly that: AngouriMath 1.4 + transitive bumps (PeterO.Numbers,
GenericTensor, IndexRange, Microsoft.Bcl.HashCode) caused
`FileNotFoundException` in `MathS` static ctor inside the xunit test host.

The migration consolidates dependency management:

1. Convert `NtoLib.csproj` from `packages.config` to `PackageReference`.
2. Adopt Central Package Management via `Directory.Packages.props` at repo root.
3. Both projects then reference packages by id only; versions live in one file.
4. ILRepack moves from a `<Import>` of a `..\packages\` props file to the
   sdk-friendly `ILRepack.Lib.MSBuild.Task` package, keeping the bundling step
   inside MSBuild. Fallback: invoke ILRepack as a `dotnet tool` from
   `Build/tools/Merge.ps1` if the MSBuild integration misbehaves.

After migration:
- `Tests.dll.config` is auto-generated correctly because Tests now sees the
  full transitive graph through `ProjectReference` (PackageReference flows
  through, packages.config did not).
- `Tests/App.config` (the band-aid added in the dependabot PR) becomes
  redundant and is removed.
- Future dependabot bumps update `Directory.Packages.props` only.

## Context (from discovery)

Files/components involved:
- `NtoLib/NtoLib.csproj` — old-style, ~270 lines, ~50 `<Reference HintPath=..\packages\..>`
- `NtoLib/packages.config` — 50+ entries, deletes
- `NtoLib/App.config` — does not exist; binding redirects auto-generated into
  `bin\$(Configuration)\NtoLib.dll.config`
- `Tests/Tests.csproj` — sdk-style, already uses `PackageReference`
- `Tests/App.config` — binding-redirect band-aid from PR #94, deletes after migration
- `Build/tools/Merge.ps1` — hardcoded `packages\ILRepack.2.0.44\tools\ILRepack.exe`
- `.config/dotnet-tools.json` — already has `dotnet-reportgenerator-globaltool`,
  prior art for global tools
- `.github/workflows/ci.yml` (master) — runs `nuget restore NtoLib.sln && dotnet restore NtoLib.sln`;
  the `nuget restore` step becomes unnecessary after migration
- `Build/Package.ps1`, `Build/Deploy.ps1` — orchestrators, don't touch packages directly
- `Resources/*.dll` — vendor SDK references via `<Reference HintPath=..\Resources\..>`,
  unchanged by this migration

Patterns/constraints discovered:
- `NtoLib.csproj` `<Import>`s three package-provided MSBuild targets:
  `ILRepack.props`, `System.ValueTuple.targets`, `Serilog.4.3.1\build\Serilog.targets`.
  PackageReference handles `Serilog.targets` automatically. `System.ValueTuple` is
  redundant on net48. ILRepack moves to `ILRepack.Lib.MSBuild.Task`.
- The csproj enforces "explicit `<Compile Include>` entries — no wildcards"
  (CLAUDE.md). PackageReference does not change that — the `<ItemGroup>` for
  source files stays as-is.
- COM registration via `netreg.exe NtoLib.dll` post-build is unaffected — it
  consumes the merged DLL, not the package layout.
- Tests pass on dependabot branch only after `Tests/App.config` was added as
  a band-aid; that file is deleted by Task 5 once migration completes.

Dependencies identified (NuGet, all currently in `NtoLib/packages.config` per
last dependabot bump):
- AngouriMath 1.4.0 (+ transitives: Antlr4.Runtime.Standard, GenericTensor,
  HonkPerf.NET.Core, HonkSharp, IndexRange, PeterO.Numbers)
- CsvHelper 33.1.0
- EasyModbusTCP 5.6.0
- FluentResults 4.0.0
- ILRepack 2.0.44 (replaced by ILRepack.Lib.MSBuild.Task)
- Microsoft.Bcl.* 10.0.7 / 6.0.0
- Microsoft.Extensions.* 10.0.7
- OneOf 3.0.271
- Polly + Polly.Core 8.6.6
- Serilog 4.3.1, Serilog.Extensions.Logging 10.0.0, Serilog.Sinks.{Console,Debug,File}
- System.* (Buffers, Collections.Immutable, ComponentModel.Annotations,
  Diagnostics.DiagnosticSource, Formats.Nrbf, IO.Pipelines, Memory,
  Numerics.Vectors, Reflection.Metadata, Resources.Extensions,
  Runtime.CompilerServices.Unsafe, Text.Encodings.Web, Text.Json,
  Threading.Channels, Threading.Tasks.Extensions)
- YamlDotNet 17.0.1

## Development Approach

- **testing approach**: Regular — verify each task by running existing test
  suite (225 tests). No new tests required; this is a build-system migration
  with no behavioral code changes. The existing test suite is the regression
  oracle.
- complete each task fully before moving to the next
- after each task: `dotnet build NtoLib.sln -c Release` must succeed AND
  `dotnet test NtoLib.sln -c Release` must report 225/225 passing
- if a task breaks the build or tests, fix it inside the same task before moving on
- maintain backward compatibility: produced `NtoLib.dll` must remain
  byte-equivalent in surface (same merged types, same COM-visible entrypoints).
  The acceptance gate is: `Build/Package.ps1` produces a merged DLL that
  passes `netreg.exe NtoLib.dll /showerror` (verifiable manually post-merge).
- **CRITICAL: update this plan file when scope changes during implementation**

## Testing Strategy

- **regression**: `dotnet test NtoLib.sln -c Release` after every task,
  225/225 passing as the bar.
- **build artifact verification**: after Task 4 (ILRepack migration),
  produce `NtoLib.dll` via `Build/Package.ps1` and inspect it (e.g.
  `ilspycmd NtoLib.dll -l class` to confirm types from merged libs are
  internalized as before).
- **no new unit tests**: this is build-config plumbing, not product code.

## Progress Tracking

- mark completed items with `[x]` immediately when done
- add newly discovered tasks with the prefix
- document issues/blockers with prefix
- update plan if implementation deviates

## Solution Overview

Two-axis change:

1. **Format migration** (per project): old-style csproj + packages.config →
   sdk-style csproj would be the textbook move, but NtoLib.csproj has heavy
   custom `<Compile Include>` and embedded resource layout that an
   sdk-style migration would either flatten with wildcards (against
   CLAUDE.md rule) or require enumerating manually anyway. So we keep
   `NtoLib.csproj` as **old-style csproj with `PackageReference`** — this
   is supported by MSBuild and is a smaller, safer diff than sdk-style
   conversion.

2. **Central Package Management**: `Directory.Packages.props` at repo root
   declares `<PackageVersion Include="X" Version="..." />` for every
   package used anywhere. Both `NtoLib.csproj` and `Tests.csproj` then
   reference packages with `<PackageReference Include="X" />` (no version).

3. **ILRepack**: replace `<Import Project="..\packages\ILRepack.2.0.44\build\ILRepack.props">`
   with `<PackageReference Include="ILRepack.Lib.MSBuild.Task" PrivateAssets="all" />`
   plus a `<Target Name="ILRepacker" AfterTargets="Build">` invocation in
   the csproj. `Build/tools/Merge.ps1` is deleted (its work moves into
   MSBuild). `Build/Package.ps1` simplifies to: build → archive (Test step
   stays).
   - **Fallback**: if the MSBuild task misbehaves, restore `Build/tools/Merge.ps1`
     using the `dotnet ilrepack` global tool (added to `.config/dotnet-tools.json`).
     Document the fallback in `Build/tools/Merge.ps1` header comment if used.

## Technical Details

### `Directory.Packages.props` skeleton

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="AngouriMath" Version="1.4.0" />
    <PackageVersion Include="CsvHelper" Version="33.1.0" />
    <!-- ... all packages used by either project ... -->
  </ItemGroup>
</Project>
```

### `NtoLib.csproj` reference style after migration

```xml
<ItemGroup>
  <PackageReference Include="AngouriMath" />
  <PackageReference Include="CsvHelper" />
  <!-- ... -->
  <PackageReference Include="ILRepack.Lib.MSBuild.Task">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

All `<Reference Include="X, Version=..., ..."><HintPath>..\packages\..</HintPath></Reference>`
items go away — MSBuild resolves the assemblies from the package via
PackageReference.

Vendor SDK references stay (`<Reference Include="FB"><HintPath>..\Resources\FB.dll</HintPath></Reference>`)
— they are not NuGet packages.

### ILRepack target inside `NtoLib.csproj`

```xml
<Target Name="ILRepacker" AfterTargets="Build">
  <ItemGroup>
    <ILRepackInputAssemblies Include="$(OutputPath)\NtoLib.dll" />
    <ILRepackInputAssemblies Include="$(OutputPath)\*.dll"
      Exclude="$(OutputPath)\NtoLib.dll;
               $(OutputPath)\System.Resources.Extensions.dll;
               $(OutputPath)\FB.dll;
               $(OutputPath)\InSAT.*.dll;
               $(OutputPath)\MasterSCADA*.dll;
               $(OutputPath)\OpcUaClient.dll;
               $(OutputPath)\Opc.Ua.Core.dll;
               $(OutputPath)\ICSharpCode.*.dll;
               $(OutputPath)\System.Text.Json.dll;
               $(OutputPath)\System.Text.Encodings.Web.dll" />
  </ItemGroup>
  <ILRepack
    Internalize="true"
    InputAssemblies="@(ILRepackInputAssemblies)"
    OutputFile="$(OutputPath)\NtoLib.dll"
    LibraryPath="$(OutputPath);$(MSBuildProjectDirectory)\..\Resources" />
</Target>
```

Exact exclude list mirrors the regex set currently in `Build/tools/Merge.ps1`.

### CI workflow update

`.github/workflows/ci.yml` (master) currently runs:

```yaml
- run: nuget restore NtoLib.sln
- run: dotnet restore NtoLib.sln
```

After migration `nuget restore` is unnecessary; the workflow simplifies to
just `dotnet restore`. The dependabot PR's branch already only runs
`dotnet restore`, so on master we drop the `nuget restore` line and the
`NuGet/setup-nuget@v2` action.

## What Goes Where

- **Implementation Steps** (`[ ]` checkboxes): all changes inside this repo —
  csproj surgery, new files, deletions, CI workflow tweaks, test runs.
- **Post-Completion** (no checkboxes): manual verification that the merged
  `NtoLib.dll` registers cleanly via `netreg.exe` on a target MasterSCADA
  machine; rebasing the open dependabot PR #94 onto master after this work
  lands.

## Implementation Steps

### Task 1: Add `Directory.Packages.props` (declare versions, do not consume yet)

**Files:**
- Create: `Directory.Packages.props` (repo root)

- [x] create `Directory.Packages.props` with `ManagePackageVersionsCentrally=true`
  and `<PackageVersion>` entries for every package currently in
  `NtoLib/packages.config` and `Tests/Tests.csproj`
- [x] include `<PackageVersion Include="ILRepack.Lib.MSBuild.Task" Version="2.0.44" />`
  (latest stable; verify on nuget.org during execution)
- [x] do not yet remove `<Version>` from existing `<PackageReference>` in `Tests.csproj` —
  that lands in Task 3
  - **deviation:** plan premise was incorrect. Enabling
    `ManagePackageVersionsCentrally=true` makes NuGet emit `NU1008` (hard error,
    not warning) when any `<PackageReference>` retains a `Version=` attribute, so
    restore fails. Resolution: stripped `Version=` attributes from
    `Tests/Tests.csproj` PackageReferences during Task 1. This is the same edit
    Task 3 prescribes for `Tests.csproj`; Task 3 still owns the equivalent edit
    for `NtoLib.csproj` (which currently uses `packages.config`, not
    `PackageReference`, so it is unaffected by NU1008 for now).
- [x] run `dotnet restore NtoLib.sln && dotnet build NtoLib.sln -c Release` —
  build still uses old paths, so it must succeed unchanged
- [x] run `dotnet test NtoLib.sln -c Release` — 225/225

### Task 2: Convert `NtoLib.csproj` from packages.config to PackageReference

**Files:**
- Modify: `NtoLib/NtoLib.csproj`
- Delete: `NtoLib/packages.config`

- [x] remove `<Import Project="..\packages\ILRepack.2.0.44\build\ILRepack.props">` line
- [x] remove `<Import Project="..\packages\System.ValueTuple.4.6.2\build\..\System.ValueTuple.targets">` line
  - n/a: original csproj had only `<Reference Include="System.ValueTuple"/>` (framework
    reference, redundant on net48); no `<Import>` for `System.ValueTuple.targets` was
    present. Removed the framework reference along with the rest in the SDK migration.
- [x] remove `<Import Project="..\packages\Serilog.4.3.1\build\Serilog.targets">` line
  - actual path was `..\packages\Serilog.4.3.0\build\Serilog.targets`; removed.
- [x] remove the `<Target>` block at end of csproj that errors on missing `..\packages\..\*.props/.targets`
- [x] replace every `<Reference Include="<id>, Version=..., ...">..<HintPath>..\packages\..` block with
  `<PackageReference Include="<id>" Version="<current>" />` (Version retained for now;
  Task 3 strips it after CPM kicks in fully)
  - **deviation:** CPM is already active (Task 1 enabled `ManagePackageVersionsCentrally=true`),
    so `Version=` attributes on `<PackageReference>` produce NU1008 errors at restore
    (same situation as Tests.csproj in Task 1). Versions were therefore omitted
    from new `<PackageReference>` entries in this task; Task 3's NtoLib version-strip
    bullet is consequently a no-op.
- [x] vendor SDK `<Reference>` items (`FB`, `InSAT.Library`, `MasterSCADA.*`,
  `OpcUaClient`, `Opc.Ua.Core`, `ICSharpCode.*`, `COMDeviceSDK`) stay untouched
- [x] delete `NtoLib/packages.config`
- [x] run `dotnet restore NtoLib.sln && dotnet build NtoLib.sln -c Release` —
  expect: build succeeds, `bin\Release\NtoLib.dll.config` is auto-generated
  with binding redirects for the same set of packages as before
  - **deviation: csproj converted to SDK-style** (`<Project Sdk="Microsoft.NET.Sdk">`)
    rather than kept as old-style. Old-style csproj + PackageReference does not import
    NuGet's `ResolvePackageAssets` target chain, so package compile assets never reach
    the C# compiler's reference list (verified: `dotnet msbuild -t:ResolvePackageAssets`
    fails with MSB4057 "no such target", and 849 CS0246 errors on every NuGet-supplied
    type during build). SDK-style is the supported path. To preserve the "no wildcards"
    rule from CLAUDE.md, default item globs are disabled via
    `EnableDefaultCompileItems=false`, `EnableDefaultEmbeddedResourceItems=false`,
    `EnableDefaultNoneItems=false`, `GenerateAssemblyInfo=false`. To keep
    `bin\Release\NtoLib.dll` flat (no `net48\` suffix) so `Build/tools/Merge.ps1`
    keeps working, set `AppendTargetFrameworkToOutputPath=false` and
    `AppendTargetFrameworkToIntermediateOutputPath=false`. WPF/WinForms toggled on via
    `UseWindowsForms=true` and `UseWPF=true`. Framework references that the SDK provides
    automatically (`mscorlib`, `System`, `System.Core`, `System.Drawing`, `System.Xml`,
    `System.Xml.Linq`, `System.Numerics`, `System.Runtime`, `System.Runtime.Serialization`,
    `System.Data`, `System.Windows.Forms`, `PresentationCore`, `PresentationFramework`,
    `WindowsBase`) were dropped from the csproj; non-default ones
    (`System.ComponentModel.DataAnnotations`, `System.Configuration`,
    `System.Data.DataSetExtensions`, `System.Net.Http`) kept as explicit `<Reference>`.
- [x] run `dotnet test NtoLib.sln -c Release` — 225/225, even **without**
  `Tests/App.config` (because Tests now resolves NtoLib's transitive graph
  via PackageReference). Verify by `git stash` of `Tests/App.config` before
  the test run; if green, restore from stash for now (Task 5 deletes it)
  - `Tests/App.config` does not exist on this branch (already absent), so the
    "without App.config" verification is satisfied implicitly: `dotnet test` ran
    225/225 with no App.config file present. Task 5's deletion is therefore a no-op
    on this branch.

### Task 3: Strip versions from PackageReferences (activate CPM)

**Files:**
- Modify: `NtoLib/NtoLib.csproj`
- Modify: `Tests/Tests.csproj`

- [x] remove `Version="..."` attributes from every `<PackageReference>` in `NtoLib.csproj`
  (already done in Task 2 — NU1008 hard error forced version omission when CPM was
  enabled in Task 1; verified 0 `Version=` occurrences in `<PackageReference>` tags)
- [x] remove `Version="..."` attributes from every `<PackageReference>` in `Tests.csproj`
  (already done in Task 1 — same NU1008 reason; verified 0 `Version=` occurrences)
- [x] confirm versions in `Directory.Packages.props` cover every package id used
  (build will fail with `NU1010` if one is missing) — verified all ids in both
  csprojs map to a `<PackageVersion>` entry
- [x] run `dotnet restore NtoLib.sln && dotnet build NtoLib.sln -c Release` — must succeed
  (clean build, 0 errors)
- [x] run `dotnet test NtoLib.sln -c Release` — 225/225 (passed 225, failed 0, skipped 0)

### Task 4: Migrate ILRepack from packages.config to MSBuild.Task

**Files:**
- Modify: `NtoLib/NtoLib.csproj`
- Modify: `Build/Package.ps1`
- Delete: `Build/tools/Merge.ps1`

- [x] add `<PackageReference Include="ILRepack.Lib.MSBuild.Task"><PrivateAssets>all</PrivateAssets>...</PackageReference>`
  to `NtoLib.csproj` (replaces the previous `ILRepack` PackageReference)
- [x] add `<PackageVersion Include="ILRepack.Lib.MSBuild.Task" Version="..." />` to
  `Directory.Packages.props` (was already declared during Task 1; the obsolete
  `ILRepack` PackageVersion entry was removed)
- [x] add `<Target Name="ILRepacker" AfterTargets="Build">` block, mirroring the
  exclude list from the deleted `Merge.ps1`
  - **deviation:** the package's targets file declares its own `ILRepack` target
    with `AfterTargets="Build"` whenever `$(Configuration).Contains('Release')`.
    A custom target sitting in the same csproj does not suppress the default —
    both run, and the default has no LibraryPath set, so it fails to resolve the
    vendor `FB` reference. Resolution: place the project-specific target in
    `NtoLib/ILRepack.targets` and point `$(ILRepackTargetsFile)` at it; the
    package then imports the file (line 9) and skips its built-in target
    (line 10 condition).
  - **deviation:** `LibraryPath` on the `ILRepack` task is `ITaskItem[]`, so it
    must be passed as an item list (`@(ILRepackLibraryPath)`), not as a
    semicolon-separated string. The Technical Details snippet in this plan was
    a string and would not have worked.
  - **deviation:** the merge target is gated on `$(RunILRepack)=true` instead of
    running on every Release build. NtoLib carries
    `[InternalsVisibleTo("Tests")]`; running ILRepack at the end of every
    `dotnet build` makes the internalised NuGet types visible to the Tests
    project alongside the real NuGet references, producing CS0433 ambiguity on
    types like `FluentResults.Result<>`, `Microsoft.Extensions.DependencyInjection.ServiceProvider`,
    and `Microsoft.Extensions.Logging.ILoggerFactory`. The plan's note that
    "When ILRepack runs as part of `Build`, it will run after every dotnet
    build. That's expected behavior" was incorrect for this codebase. The
    merge therefore runs only when `Build/Package.ps1` invokes
    `dotnet build NtoLib.csproj -p:RunILRepack=true` after the test step.
- [x] delete `Build/tools/Merge.ps1`
- [x] remove `& (Join-Path $ToolsDir 'Merge.ps1') ...` line from `Build/Package.ps1`
  (replaced with an inline `dotnet build -p:RunILRepack=true` invocation)
- [x] run `dotnet build NtoLib.sln -c Release` — verified `bin\Release\NtoLib.dll`
  goes from 0.86 MB (un-merged) to 5.46 MB (merged); `ilspycmd ... -l class`
  shows internalised types from `AngouriMath`, `Polly`, `Serilog`, `YamlDotNet`,
  `Microsoft.Extensions.*` etc.
- [x] run `Build/Package.ps1 -Configuration Release -RepoRoot .` — succeeds
  end-to-end (Build, Test 225/225, Merge, Archive → `Releases/NtoLib_v1.12.0-beta1.zip`)
- [x] run `dotnet test NtoLib.sln -c Release` — 225/225 (passed 225, failed 0,
  skipped 0)
- [x] **fallback gate**: not triggered. Option A (ILRepack.Lib.MSBuild.Task)
  succeeded; merged DLL contains internalised NuGet namespaces and the test
  suite passes against the un-merged build.

### Task 5: Remove the `Tests/App.config` band-aid

**Files:**
- Delete: `Tests/App.config`

- [x] delete `Tests/App.config` (Tests/App.config never existed on this branch — verified)
- [x] run `dotnet build NtoLib.sln -c Release` — must succeed (Tests/App.config never existed on this branch — verified)
- [x] run `dotnet test NtoLib.sln -c Release` — 225/225 (this is the proof
  that CPM migration solved the actual problem; if any test fails the
  migration is incomplete) (Tests/App.config never existed on this branch — verified)

### Task 6: Update CI workflow

**Files:**
- Modify: `.github/workflows/ci.yml`

- [x] remove the `NuGet/setup-nuget@v2` step (no longer needed, no packages.config)
- [x] simplify the `restore` step to a single `dotnet restore NtoLib.sln`
- [x] note: this lands on master after the migration PR is merged. On the
  migration branch itself, `nuget restore` is harmless (no packages.config
  found = no-op), so leaving it in until the PR merges is fine.

### Task 7: Verify acceptance criteria

- [x] all requirements from Overview implemented:
      - [x] `NtoLib.csproj` uses `PackageReference` (no `<Reference HintPath=..\packages\..`) — `git grep` for `packages\\` in NtoLib.csproj returns nothing
      - [x] `NtoLib/packages.config` deleted — file does not exist
      - [x] `Directory.Packages.props` exists and is the single source of versions
      - [x] `Tests/App.config` deleted — file does not exist
      - [x] ILRepack invoked via MSBuild — `ILRepack.Lib.MSBuild.Task` PackageReference + `NtoLib/ILRepack.targets`; `Build/tools/Merge.ps1` is gone
- [x] full test suite passes: `dotnet test NtoLib.sln -c Release` → 225/225 (passed 225, failed 0, skipped 0)
- [x] `Build/Package.ps1` end-to-end produces a merged `NtoLib.dll` — Build, Test 225/225, ILRepack merged 41 assemblies, archive written to `Releases/`
- [x] inspect `bin\Release\NtoLib.dll.config` — binding redirects auto-generated. Note: most NuGet dependencies (AngouriMath, Antlr4.Runtime.Standard, GenericTensor, HonkSharp, IndexRange, PeterO.Numbers, Serilog, Polly, YamlDotNet, etc.) are internalised by ILRepack and therefore correctly do **not** appear in the redirect list. External (excluded) assemblies that do require redirects — `System.Text.Json`, `System.Text.Encodings.Web`, `System.Resources.Extensions`, plus `Microsoft.Extensions.Configuration.*`, `Microsoft.Extensions.Logging.Configuration`, `Microsoft.Extensions.Options.ConfigurationExtensions` — all have entries. No missing-redirect surprises.
- [x] `dotnet format NtoLib.sln --verify-no-changes` — clean (no formatting issues)
- [x] dependabot config (`.github/dependabot.yml`) updated: `directory` for the `nuget` ecosystem switched from `/NtoLib` to `/` so dependabot v2 discovers `Directory.Packages.props` at repo root and scans both `NtoLib.csproj` and `Tests/Tests.csproj`. Dependabot v2 supports CPM natively and now proposes version bumps against `Directory.Packages.props`.

### Task 8: [Final] Update documentation

**Files:**
- Modify: `CLAUDE.md` / `AGENTS.md` (root + NtoLib)
- Modify: `Docs/architecture/architecture.md` if it mentions packages.config

- [x] update `CLAUDE.md` / `AGENTS.md` "csproj Conventions" section: remove
  packages.config references, add note about `Directory.Packages.props`
  (NtoLib/AGENTS.md updated; CLAUDE.md is mirrored automatically by
  `.githooks/pre-commit`. Root AGENTS.md/CLAUDE.md had no csproj/packages.config
  content to update.)
- [x] note in CLAUDE.md that `<PackageReference>` versions are managed centrally —
  bumps go to `Directory.Packages.props` (covered in NtoLib/AGENTS.md "Central
  Package Management" subsection)
- [x] note removal of `Build/tools/Merge.ps1` and shift of merging into MSBuild
  (covered in NtoLib/AGENTS.md "ILRepack and `[InternalsVisibleTo(\"Tests\")]`"
  subsection, plus the `Build` shell snippet now distinguishes
  `-p:RunILRepack=true` for merged builds)
- [x] move this plan to `docs/plans/completed/`

## Post-Completion

*Manual / external verification — no checkboxes.*

**Manual verification:**
- Run `Build/Deploy.ps1` against a real MasterSCADA target machine; confirm
  `netreg.exe NtoLib.dll /showerror` registers cleanly. The merged-DLL
  layout is the most likely place for CPM-induced regressions to surface
  (e.g. ILRepack internalizing or skipping a different set of assemblies
  than before).
- Open the merged `NtoLib.dll` in a MasterSCADA project, instantiate one of
  each FB family (one headless, one visual), confirm pin behavior is
  unchanged.

**External system updates:**
- Rebase open dependabot PR #94 onto migration-merged master. Most of its
  diff (csproj reference rewrites) becomes a no-op because the post-migration
  csproj has no version-pinned hint paths to update — dependabot now updates
  `Directory.Packages.props` only. Either close PR #94 and let dependabot
  reopen against `Directory.Packages.props`, or rebase manually if version
  bumps need to ship in this same window.
- Verify dependabot config in `dot-config/` (or `.github/dependabot.yml`
  depending on which one is active) handles CPM — modern dependabot does
  natively, but if `dot-config/` pins a specific manifest path, update it.
