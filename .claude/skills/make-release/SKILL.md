---
name: release
description: Use this skill when the user asks to release a new NtoLib version — "let's release v1.12.0", "cut a release", "выпустить новую версию", "пуш тега", "сделать beta1". Walks through gathering changes since the last release, drafting release notes in the project's established Russian style, creating an annotated tag with the right cleanup flag, pushing it to trigger the GitHub Actions release workflow, and verifying the published GitHub Release.
---

# Releasing a new NtoLib version

The release flow is fully automated by `.github/workflows/release.yml`: pushing an annotated tag `vX.Y.Z` (or `vX.Y.Z-suffix` for prereleases) on `windows-2025` builds the merged `NtoLib.dll` and `Installer.exe` with the tag-derived version, packages `NtoLib_v<ver>.zip`, and publishes a GitHub Release with both attached. SemVer suffix → automatic `prerelease=true` and `make_latest=false`.

This skill exists because there are several recurring gotchas (tag annotation cleanup, `git tag -m` stripping `#` lines, version-extraction expecting `vX.Y.Z` shape, build tested locally before tagging) that are easy to forget across releases.

## Phase 1 — Pre-flight

Before touching any tag:

1. Confirm with the user **what version** they want to release. Ask if not stated. SemVer rules apply:
   - Stable: `vX.Y.Z`
   - Prerelease: `vX.Y.Z-beta1`, `vX.Y.Z-rc1`, etc. (workflow auto-marks these as prerelease via `contains(version, '-')`).
2. Confirm working tree state:
   ```bash
   git status                         # must be clean
   git rev-parse --abbrev-ref HEAD    # should be master (tag from master unless user says otherwise)
   git fetch origin
   git log master..origin/master      # local must equal origin/master, no drift
   ```
   If local lags, fast-forward: `git checkout master && git merge --ff-only origin/master`.
3. Confirm CI is green for the commit being tagged: `gh run list --workflow=ci.yml --branch master --limit 1` → conclusion `success`.
4. Run a local smoke build to fail fast before pushing a tag that would crash CI:
   ```powershell
   dotnet build NtoLib.sln -c Release
   dotnet test NtoLib.sln -c Release --no-build --logger "console;verbosity=minimal"
   dotnet build NtoLib/NtoLib.csproj -c Release -p:RunILRepack=true --no-restore
   ```
   All green before proceeding.

## Phase 2 — Gather changes since the last release

Find the previous release of the same series and list what shipped since then:

```bash
# Last release tag of any kind:
gh release list --limit 5

# Commits since last release (replace v1.11.0 with the actual prior tag):
git log v1.11.0..master --oneline

# Merged PRs since last release date:
gh pr list --state merged --limit 20 --json number,title,mergedAt
```

Group what you find into:
- **Новый функционал** — user-facing features (new FBs, new config options, new UI)
- **Исправления** — bug fixes referencing issue numbers
- **Ломающий совместимость функционал** — anything requiring user action on upgrade
- **Внутренние изменения** — build/CI/refactor that doesn't affect users
- **Не будет исправлено** — closed-as-wontfix issues if relevant

Skip empty sections in the final notes.

## Phase 3 — Draft release notes (Russian, project house style)

Read 4–5 recent releases to match the tone:
```bash
gh release view v1.11.0 --json body
gh release view v1.11.0-beta1 --json body
```

House-style rules observed across `v1.10.0` … `v1.11.0`:

1. **Always open with the standard intro paragraph**:
   ```
   Для установки рекомендуется сделать бэкап проекта и текущей версии библиотеки.
   См. актуальный статус [ошибок](https://github.com/Semiteq/NtoLib/issues).
   ```
2. **Reference issues by `#N`** (e.g. `Добавлен блок #86`). Don't expand to full URLs unless screenshot/asset is being linked.
3. **Important! Always link to in-repo docs when introducing a new FB**: `См. [документацию](https://github.com/Semiteq/NtoLib/blob/master/Docs/<file>.md).`
4. **For betas** — flatter narrative, no section headings or just one or two (`## Внутренние изменения`).
5. **For minor/major releases** — full section structure (`## Новый функционал`, `## Исправления`, `## Ломающий совместимость функционал`, `## Внутренние изменения`).
6. Russian throughout. Backticks for filenames/types/commands.
7. If a change is breaking, **call it out explicitly with bold** (`Это ломающее изменение!`) and tell the user what to do in old projects.

Show the draft to the user before tagging. Iterate until they approve.

## Phase 4 — Create the annotated tag

**CRITICAL**: use `--cleanup=verbatim`. By default `git tag -m` strips lines starting with `#` (treating them as comments) — your `## Новый функционал` headings will silently disappear from the tag annotation, and from the GitHub Release body that the workflow extracts from it.

```bash
git tag --cleanup=verbatim -a vX.Y.Z -m "$(cat <<'EOF'
Для установки рекомендуется сделать бэкап проекта и текущей версии библиотеки.
См. актуальный статус [ошибок](https://github.com/Semiteq/NtoLib/issues).

## Новый функционал

…

## Внутренние изменения

…
EOF
)"
```

Verify the annotation came through correctly:
```bash
git tag -l --format='%(contents)' vX.Y.Z
```
The output must contain `## …` headings literally. If it doesn't, **delete the tag and re-create with `--cleanup=verbatim`** — pushing a tag with stripped headings means GitHub Release body is wrong, and editing it later requires `gh release edit` (the tag annotation itself is immutable on remote without force-push).

## Phase 5 — Push and watch

```bash
git push origin vX.Y.Z

# Find the run id and watch:
gh run list --workflow=release.yml --limit 1
gh run watch <run-id>
# Or open the URL:
# https://github.com/Semiteq/NtoLib/actions/workflows/release.yml
```

Expected timing on `windows-2025`: ~3 minutes (no test step in release.yml — relies on ci.yml gate).

## Phase 6 — Verify the release

After the workflow completes:

```bash
gh release view vX.Y.Z --json name,tagName,isPrerelease,assets
```

Check:
- `name` equals the bare tag (e.g., `v1.12.0-beta1`).
- `isPrerelease` matches expectation (`true` if tag has `-suffix`, `false` otherwise).
- `assets` contains exactly two: `NtoLib_v<ver>.zip` (~2 MB) and `Installer.exe` (~28 KB).
- Open the release page; confirm body has section headings and links rendered correctly.

If the release name or body needs fixing post-publish:
```bash
gh release edit vX.Y.Z --title "v1.12.0"
gh release edit vX.Y.Z --notes "..."
```
This edits the GitHub Release object (separate from the git tag annotation, which stays as-is).

## Rollback

If the release is broken (wrong DLL, bad notes that need to be re-shipped, wrong commit tagged):

```bash
gh release delete vX.Y.Z --yes
git push origin :refs/tags/vX.Y.Z
git tag -d vX.Y.Z
```

Then re-tag and push. **Do not reuse the same tag name** for a fixed release — bump the version (`vX.Y.Z+1` or `vX.Y.Z-suffix2`) so existing downloads aren't silently swapped.

## Common pitfalls (do not repeat)

- **`git tag -m` strips `#` lines** → use `--cleanup=verbatim` always.
- **Tag without `v` prefix** → workflow strips `v` via `${TAG#v}`; without a `v`, `VERSION` ends up identical to `TAG` and `name: ${{ tag }}` looks ugly. Always use `vX.Y.Z`.
- **Lightweight tag (`git tag vX.Y.Z` without `-a`)** → no annotation body → workflow falls back to `Release X.Y.Z` for the GitHub Release notes. Always use `-a -m`.
- **Tag on a non-master branch** → workflow doesn't care, will release whatever the tag points at. Only do this for hotfix branches the user explicitly named.
- **Don't bump version in `NtoLib/NtoLib.csproj`** — `<Version>` there is a local-build fallback only; CI overrides with `-p:Version=$VERSION` from the tag. The csproj bump is unnecessary and would just create churn.
- **Don't reuse tags**. GitHub caches release assets and the tag's history is immutable for downstream users who already pulled.

## Files referenced by this flow

- `.github/workflows/release.yml` — the workflow itself
- `NtoLib/NtoLib.csproj` — where `-p:Version=` lands (assembly metadata)
- `Installer/Installer.csproj` — same `-p:Version=` for Installer.exe metadata
- `NtoLib/NtoLib_reg.bat`, `DefaultConfig/` — bundled into the zip artifact
- `Build/Deploy.ps1` — local debug-deploy script (not part of release flow)
