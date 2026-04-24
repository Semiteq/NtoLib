<!--
PR template for NtoLib. Keep sections that apply, delete the rest.
CI runs build + test + format-verify on push/pull_request — wait for green
before requesting review.
-->

## Summary

<!-- 1–3 sentences: what changed and why. Focus on the "why". -->

## Type of change

<!-- Pick one or more. Delete the rest. -->

- [ ] feat — new functionality
- [ ] fix — bug fix
- [ ] refactor — code restructure with no behaviour change
- [ ] chore — tooling, build, docs, infrastructure
- [ ] test — test-only changes
- [ ] docs — documentation only

## Linked issues

<!-- e.g. Closes #72, Refs #88. If none, delete this section. -->

## Changes touching FB code

<!-- Delete the whole section if no FB code changed. -->

- [ ] XML pin IDs match the `const int *PinId` constants in the FB class
- [ ] New runtime-only fields carry `[NonSerialized]`
- [ ] `[ComVisible(true)]` + `[Guid]` untouched on existing FBs (breaking GUID
      changes invalidate deployed projects)
- [ ] Read `Docs/architecture/masterscada-fb-primer.md` and
      `Docs/architecture/architecture.md` for the affected FB family
- [ ] Checked `Docs/known_issues/` for any entry covering the area touched

## Tree-modifying FBs only

<!-- Delete if no deferred-execution / tree-mutating code changed. -->

- [ ] Mutations are queued in `UpdateData` and executed from a
      `System.Windows.Forms.Timer` polling `IProjectHlp.InRuntime`
- [ ] Results written to the file log (FB instance is replaced between
      runtime cycles — output pins are unreliable)
- [ ] No `BeginInvoke` self-reposting (see
      `Docs/known_issues/08-begininvoke-reposting-fails.md`)

## Testing

<!-- What you ran locally. Keep the list short but honest. -->

- [ ] `dotnet build NtoLib.sln` — 0 errors
- [ ] `dotnet test NtoLib.sln` — all green
- [ ] `dotnet format NtoLib.sln --verify-no-changes` — exit 0
- [ ] Manual host smoke (runtime cycle in MasterSCADA), if FB pin wiring changed

## Breaking changes / migration

<!-- Delete if none. Otherwise describe what consuming projects must do. -->

## Deployment notes

<!-- Delete if standard Build/Deploy.ps1 works unchanged. Otherwise: new
     DefaultConfig files, new vendor DLLs, COM registration side effects, etc. -->
