# Internalize System.Text.Json into the merged NtoLib.dll (#127)

## Overview

The merged `NtoLib.dll` holds an **external** strong-named reference to
`System.Text.Json` and `System.Text.Encodings.Web` (the built-against version, pinned in
`Directory.Packages.props` and drifted by dependabot — `10.0.10` as of this writing, the
issue captured `10.0.0.8`). Both are
in the ILRepack exclude list (`NtoLib/ILRepack.targets:44-46`), so neither is merged nor
shipped. At runtime the MasterSCADA host supplies its own `System.Text.Json 4.0.1.2` and
rebinds the reference to it via a process-wide, simple-name `AssemblyResolve` handler,
which bypasses the strong-name version check. Full analysis:
`Docs/known_issues/10-assembly-version-binding-host-resolver.md`.

This works today but rests on two implicit contracts invisible to the xUnit suite (dev
NuGet drops the real 10.x next to the DLL, so the reference resolves normally and tests
stay green):

1. The host keeps installing a simple-name resolver and keeps shipping some
   `System.Text.Json`. A change to the host load model → `FileLoadException 0x80131040`
   in production.
2. NtoLib never calls an STJ API newer than `4.0.1.2`. Any STJ 5+/8+ call
   (positional `record` deserialization, `JsonNode`, source-gen,
   `JsonSerializerOptions.Default`) → `MissingMethodException` at runtime, uncaught by
   tests running on 10.x.

**Fix (issue option 1):** merge and internalize `System.Text.Json` +
`System.Text.Encodings.Web` into `NtoLib.dll` by removing the two excludes. A private
copy of the version the code was built against lives inside the assembly; the host
version no longer participates; both contracts are removed. Safe because STJ types never
cross the COM boundary to the host — `OpcTreeManager` uses them internally only. Cost:
+~0.5–1 MB on the DLL.

## Context (from discovery)

- **Merge config:** `NtoLib/ILRepack.targets:34-46` — the exclude list; STJ +
  Encodings.Web are the last two entries (lines 45-46). The target is gated on
  `RunILRepack=true` (`ILRepack.targets:18`), so a plain `dotnet build` stays un-merged
  and the Tests project (which references the same NuGet packages directly under
  `[InternalsVisibleTo("Tests")]`) still compiles. Merging STJ into the **release**
  artifact does not touch that gate.
- **Transitive STJ deps already merged:** the exclude list names only STJ and
  Encodings.Web; their transitive net48 dependencies (`System.Memory`, `System.Buffers`,
  `System.Runtime.CompilerServices.Unsafe`, etc.) are swept in today by the
  `$(OutputPath)*.dll` glob (`ILRepack.targets:35`). Removing these two excludes only
  adds STJ + Encodings.Web to a merge that already contains their dependency closure.
- **STJ consumers (5 files, all internal, none COM-facing):**
  - `NtoLib/OpcTreeManager/Config/TreeSnapshotWriter.cs`
  - `NtoLib/OpcTreeManager/Config/TreeSnapshotLoader.cs`
  - `NtoLib/OpcTreeManager/Entities/OpcScadaItemDto.cs`
  - `NtoLib/OpcTreeManager/Entities/NodeSnapshot.cs`
  - `NtoLib/OpcTreeManager/Entities/LinkEntry.cs`
  - (test-side only: `Tests/OpcTreeManager/Acceptance/PlanBuilderAcceptanceTests.cs`,
    `Tests/TrendPensManager/Helpers/TrendPensTestHelper.cs`)
- **Non-mergeable exception stays:** `System.Resources.Extensions` (`ILRepack.targets:44`)
  breaks when internalized and remains external, shipped as a file next to the DLL. This
  plan does not touch it.

## Development Approach

- **Testing approach:** Regular. This is a build-configuration change with no new
  production code path — the "tests" are the existing xUnit acceptance suite (run
  un-merged) plus a static reference check on the merged artifact. No new unit tests are
  warranted; adding them would test ILRepack, not NtoLib.
- Small, single focused change: remove two exclude entries and correct the adjacent
  comment.
- Verify the merged DLL loses the external STJ references before claiming the fix.

## Testing Strategy

- **Unit / acceptance:** existing `dotnet test NtoLib.sln` must stay green. It runs
  against the **un-merged** build, so it proves the STJ usage still compiles and behaves,
  but it does **not** exercise the merged/internalized binding — that is the reference
  check plus host smoke below.
- **No new automated tests.** The change has no branch logic to cover. A merged-runtime
  harness was considered and declined; the human host-smoke gate covers the merged path.
- **e2e:** project has no UI e2e harness for this area; n/a.

## Acceptance Evidence

The defect is a *latent* binding fragility, not a live failure, so "reproduction" is a
static assertion about the merged artifact, and "measurement" is the disappearance of the
external references plus a live host round-trip.

**1. Automated — external references gone (the core proof).** After the merged build,
run against the produced DLL:

```powershell
$refs = ([Reflection.Assembly]::ReflectionOnlyLoadFrom(
  '<OutputPath>\NtoLib.dll')).GetReferencedAssemblies() |
  ForEach-Object { $_.Name }
# PASS condition: neither name is present
$refs -contains 'System.Text.Json'          # must be $false
$refs -contains 'System.Text.Encodings.Web' # must be $false
```

Before the fix both return `$true`; after the fix both must return `$false`. This is the
single command that proves the reference is internalized.

**2. Automated — suite still green.** `dotnet test NtoLib.sln` passes (un-merged;
confirms STJ usage compiles and the serialization round-trips at the source level).

**3. Manual — live host round-trip (post-completion gate).** In MasterSCADA with the
merged DLL registered, trigger an `OpcTreeManager` operation that writes and reads a tree
snapshot; confirm the snapshot deserializes, the plan builds, and deferred execution
completes (`fail=0`). This is the only check that exercises the internalized STJ under
the real host load model. Listed under Post-Completion — it needs the host machine and is
not a merge-gate for the PR.

> Note: dotpeek and static analyzers resolve dependencies by simple name and ignore
> strong-name version, so "no broken dependencies" from those tools does not prove the
> runtime bind. The reference-name check in step 1 is the authoritative static signal.

## Progress Tracking

- mark completed items with `[x]` immediately when done
- add newly discovered tasks with ➕ prefix
- document issues/blockers with ⚠️ prefix
- keep this file in sync with actual work

## Solution Overview

Remove the two `System.Text.*` entries from the ILRepack `Exclude` attribute so the
`$(OutputPath)*.dll` glob includes them in the merge, and let the existing
`Internalize="true"` (`ILRepack.targets:52`) make their types private to `NtoLib.dll`.
Correct the exclude-list comment (`ILRepack.targets:28-32`) that currently states STJ is
"kept external; the host environment / MasterSCADA runtime is expected to supply them" —
that rationale is being reversed. Update known-issue #10 to record the STJ instance as
resolved while keeping its general rule about external strong-named deps.

## Technical Details

- **File:** `NtoLib/ILRepack.targets`.
  - Delete the two trailing lines of the `Exclude` list (currently lines 45-46):
    `$(OutputPath)System.Text.Json.dll;` and
    `$(OutputPath)System.Text.Encodings.Web.dll`. Ensure the preceding line
    (`System.Resources.Extensions.dll`) becomes the closing entry and the `Exclude`
    attribute string is still well-formed (no dangling `;` breaking the value, closing
    `"` intact).
  - Rewrite the comment block (lines 28-32) so it no longer lists STJ among "must not be
    merged" and no longer claims the host supplies it. Keep the
    `System.Resources.Extensions` rationale.
- **No csproj change.** `PackageReference` on `System.Text.Json` stays; the package must
  still be restored so its DLL lands in `$(OutputPath)` to be merged.
- **No code change** in `OpcTreeManager` — API usage is unchanged.

## What Goes Where

- **Implementation Steps** (`[ ]`): the targets edit, both builds, the reference check,
  the doc update, the commit.
- **Post-Completion** (no checkboxes): the live MasterSCADA host round-trip, which needs
  the host machine.

## Implementation Steps

### Task 1: Remove the STJ excludes and fix the comment

**Files:**
- Modify: `NtoLib/ILRepack.targets`

- [ ] delete the `System.Text.Json.dll` and `System.Text.Encodings.Web.dll` entries from
      the `Exclude` attribute (lines 45-46); verify the attribute value stays well-formed
- [ ] rewrite the comment block (lines 28-32) to drop STJ from the "must not be merged"
      list and remove the "host is expected to supply them" claim; keep the
      `System.Resources.Extensions` note
- [ ] while in the file, fix the stale `Build/Package.ps1` reference in the comment at
      lines 14-15 (that script no longer exists per CLAUDE.md; the merged build is driven
      by `release.yml` / `Build/Deploy.ps1`) — near-free since this edit already touches
      comments in the same file for accuracy
- [ ] un-merged build compiles: `dotnet build NtoLib.sln`
- [ ] full suite green (proves STJ usage still compiles/round-trips un-merged):
      `dotnet test NtoLib.sln`

### Task 2: Produce the merged artifact and prove internalization

**Files:**
- (no source changes — verification only)

- [ ] merged release build:
      `dotnet build NtoLib/NtoLib.csproj -c Release -p:RunILRepack=true`
- [ ] run the reference-name check from **Acceptance Evidence step 1** against the
      produced `NtoLib.dll`; both `-contains` results must be `$false`
- [ ] confirm build output contains no ILRepack warning about failing to merge STJ /
      Encodings.Web
- [ ] record the observed DLL size delta (expected +~0.5–1 MB) in this plan under
      Progress Tracking

### Task 3: Update the known-issue doc

**Files:**
- Modify: `Docs/known_issues/10-assembly-version-binding-host-resolver.md`

- [ ] mark the `System.Text.Json` instance resolved (internalized via #127); keep the
      general rule about external strong-named deps and the host resolver intact
- [ ] keep the doc in **Russian** — `Docs/known_issues/10-...md` is Russian; the update
      matches the existing language, do not switch it to English
- [ ] use the **as-restored** version, not a hardcoded `10.0.0.8`:
      `Directory.Packages.props` currently pins `10.0.10` and dependabot drifts it, so
      re-measure the actual `System.Text.Json.dll` version next to the merged DLL before
      writing any version string
- [ ] confirm no other doc or comment still asserts STJ is shipped external
      (grep `System.Text.Json` across `Docs/` and `NtoLib/ILRepack.targets`)

### Task 4: Verify acceptance criteria

- [ ] Overview contract 1 (host resolver dependency) removed: merged DLL no longer
      references STJ externally — confirmed by Task 2 reference check
- [ ] Overview contract 2 (API-level ceiling) removed: internalized copy is the build
      version (10.x), not the host `4.0.1.2`
- [ ] full suite green: `dotnet test NtoLib.sln`
- [ ] `dotnet format NtoLib.sln` reports no changes (or apply and re-run)

### Task 5: Commit and finalize

- [ ] `dotnet format NtoLib.sln`
- [ ] commit on `feature/127`:
      `fix(opctreemanager): internalize System.Text.Json into merged DLL (#127)`
- [ ] move this plan to `Docs/plans/completed/`

## Post-Completion

*Manual / external — no checkboxes, informational only.*

**Manual verification (host machine required):**
- Live MasterSCADA host round-trip per **Acceptance Evidence step 3**: register the merged
  DLL, trigger an `OpcTreeManager` snapshot write+read, confirm the snapshot deserializes,
  the plan builds, and deferred execution completes with `fail=0`. This is the only check
  that exercises the internalized STJ under the real host load model. If it fails with
  `FileLoadException 0x80131040` or `MissingMethodException`, capture the Fusion binding
  log (procedure in `Docs/known_issues/10-...md`) before diagnosing.

**Release note:**
- Next `vX.Y.Z` release: the shipped `NtoLib.dll` grows ~0.5–1 MB and no longer depends on
  the host supplying `System.Text.Json`. No consumer-side action required.
