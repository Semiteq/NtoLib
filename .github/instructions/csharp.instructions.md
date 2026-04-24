---
applyTo: "**/*.cs"
---

<!-- Reviewer scope: apply these rules only to C# source files.
     Do not review or comment on Markdown documents (plans, docs, README, etc.). -->

# NtoLib — Copilot Code Review Instructions

NtoLib is a MasterSCADA 3.12 Function Block library by Semiteq for industrial SCADA
automation (MBE/MOCVD semiconductor equipment). It ships as a single DLL registered as a
COM component via `netreg.exe`.

- **Framework:** .NET Framework 4.8, C# 10 (`LangVersion`), `Nullable` enabled
- **Solution:** `NtoLib.sln` — two projects: `NtoLib` (main library) and `Tests` (xUnit)
- **csproj style:** Old-style with explicit `<Compile Include>` entries (no wildcards)
- **Build:** `dotnet build` → ILRepack merges NuGet DLLs into `NtoLib.dll` (vendor SDK DLLs
  in `Resources/` are excluded); `netreg.exe` registers the COM component for MasterSCADA

---

## How to review this codebase

Apply the rules below. Cite the specific rule for every finding. Distinguish severity:
**CRITICAL** (must fix before merge), **MODERATE** (should fix), **MINOR** (optional polish).
Do not flag choices that are not covered by any rule below — review against documented rules,
not personal preferences.

---

## Platform-specific rules (MasterSCADA vendor)

### Function Block architectures

Two architectures exist and the rules differ:

| Aspect | Headless FB | Visual FB |
|--------|-------------|-----------|
| Base class | `StaticFBBase` | `VisualFBBase` / `VisualFBBaseExtended` |
| Layers | FB orchestrator + service facade | FB + Control + Status DTO + Renderer |
| XML sections | `<Map>` only | `<Map>` + `<VisualMap>` + optional `<Events>` |

Headless FBs follow the **thin orchestrator** pattern — the FB class only does lifecycle,
pin I/O, and rising-edge detection; all logic belongs in a facade service behind an
interface. Visual FBs have four layers (FB, Control, Status DTO, Renderer) and a
feature typically touches all four.

### COM / serialization constraints (CRITICAL)

- **`[NonSerialized]` on all runtime-only fields.** MasterSCADA serializes FB instances;
  non-serializable fields without this attribute cause exceptions on project save.
- **Pin IDs in code must match XML exactly.** Mismatches compile but cause runtime
  `NullReferenceException` from `SetPinValue` by ID. ID/XML mismatch is always suspected
  first for such NREs, not business logic.
- **STA threading model.** All COM objects must be accessed from the STA thread. Never
  access vendor COM interfaces from background threads.
- **Timestamps must be UTC.** Use `DateTime.UtcNow` when writing pin values.
- **New `.cs` files must be added to `NtoLib.csproj` manually** as `<Compile Include>`
  entries — the csproj does not use wildcards. Missing entries compile locally but break
  in clean builds.

### Tree modifications (CRITICAL)

- MasterSCADA forbids tree/link modifications in Runtime. Calls like `Connect`,
  `Disconnect`, `ApplyChange`, and structural swaps throw
  `"Изменение проекта в режиме исполнения запрещено."` if attempted in Runtime.
- FBs that mutate the project tree must use the **timer-based deferred execution
  pattern**: queue in `UpdateData`, post a `System.Windows.Forms.Timer` from `ToDesign()`
  that polls `IProjectHlp.InRuntime` every 200ms and executes only when it becomes
  `false`. `BeginInvoke` self-reposting is **not sufficient** — delegates execute
  back-to-back during message pumping and exhaust retries before `_inRuntime` flips.
- Results produced by the deferred delegate cannot be written to FB fields — the FB
  instance is replaced between Runtime cycles. Use file-based logging for those results.

---

## Error handling

### FluentResults

- `Result` / `Result<T>` is used at all facade and service boundaries.
- Always check `result.IsFailed` before using the value. Discarding a `Result` without
  inspection is a CRITICAL finding.
- Each module may use internal error types but must convert to `Result` at its public
  boundary.

### Exceptions

- Throw exceptions only for truly exceptional conditions (programmer error, corrupted
  state, malformed snapshot data).
- Using exceptions for expected business logic failures (parsing, validation) is a finding.
- Empty catch blocks (silently swallowing exceptions) are always a CRITICAL finding.
- Deferred-execution paths must catch exceptions around the executor call so the FB
  instance replacement does not leak unhandled exceptions into the vendor COM layer.

### Null safety

- `<Nullable>enable</Nullable>` is active in both projects.
- Missing null checks on parameters or return values from vendor COM methods are valid
  findings — `IProjectHlp.SafeItem<T>(path)` commonly returns `null`.
- `!` suppressor without a verified justification comment is a finding.
- Use `?.` and `??` where the value may be null.

### Unused parameters

- A parameter never read inside the method body is a MODERATE finding. Suggest removing
  it and updating all call sites.

---

## Code style

### Naming

- Public types, methods, properties: PascalCase.
- Private fields: `_camelCase` (underscore prefix). Class instance fields named after
  their type, unabbreviated: `_planExecutor`, `_linkCollector`, not `_pe` / `_lc`.
- Interfaces: `I`-prefix + PascalCase.
- Local variables and parameters: camelCase.
- No abbreviations in any identifier.

### Formatting

- Tabs (size 4). Max line length 120 characters (per `.editorconfig`).
- Braces on a new line for all blocks, including single-line `if`/`else`/`for`/`foreach`
  bodies.
- Expression-bodied members: allowed only for simple properties and indexers, not for
  methods or constructors.

### File layout

- File-scoped namespace: `namespace Foo.Bar;` (not block-scoped).
- `using` directives at the top; `System` namespaces first, blank line, then others.
- One class per file. No fully-qualified type names inline — use `using` directives.

### Types

- `var` for all local variable declarations, even when the type is obvious.
- Predefined aliases: `int`, `string`, `bool`, not `Int32`, `String`, `Boolean`.

### Size limits

- Class preferably under 300 lines. Method preferably under 50 lines.

### Comments

- Only for genuinely non-obvious business logic (vendor quirks, MasterSCADA COM gotchas,
  references to `Docs/known_issues/`).
- No `// TODO`, `// HACK`, `// in new version`, or other transient process notes.
- English only — no Russian/other-language comments in code (`.ru` strings in user-facing
  error messages or log templates are allowed).

---

## Code smells

- **Dead code**: unused private methods, unreachable branches, unused private fields,
  unused `using` directives, variables assigned but never read.
- **Duplication**: identical or near-identical logic blocks that should be extracted —
  especially around pin handling, deferred execution, and Status DTO decoding.
- **Deep nesting**: more than 3 levels of `if`/`else`/`for`; suggest guard clauses or
  early returns.
- **Inconsistent abstraction**: mixing high-level domain operations with low-level COM
  details in the same method without helper methods.
- **God class**: a class accumulating responsibilities that belong elsewhere. Headless FB
  orchestrators specifically must stay thin (lifecycle + pins + edge detection only).
- **Disabled/skipped tests**: tests must not be skipped (`[Fact(Skip=...)]`,
  `[SkippableFact]`, `Skip.If`). Rewrite or delete them instead.

---

## Testing

- Framework: xUnit 2.9.3 + FluentAssertions 8.8.0 + Moq 4.20.72.
- Unit tests live under `Tests/<Module>/Unit/`, integration under
  `Tests/<Module>/Integration/`, fixture-driven acceptance under
  `Tests/<Module>/Acceptance/` with fixture data under
  `Tests/<Module>/Fixtures/Acceptance/<case-name>/`.
- Test classes must not mock vendor concrete types declared with the `I`-prefix
  (`IProjectHlp`, `ITreeItemHlp`, `ITreePinHlp`) — these are **classes, not interfaces**
  in the decompile despite the naming. If a test needs one of these, introduce a narrow
  seam interface wrapping only the methods actually used, and Mock the seam.

---

## Design and simplicity

### Interface design

- Create an interface when: 2+ implementations exist, the class is mocked in tests, it
  crosses an architectural layer boundary, or it implements Strategy/Factory.
- Do NOT create an interface for a single concrete class with no extension plans, or for
  POCOs/DTOs/immutable records.
- Interfaces belong on the consumer side (where injected), not the producer side (where
  implemented).

### YAGNI

- Do not flag patterns that already exist elsewhere in the codebase — consistency is a
  virtue.
- Flag unused extension points, hooks with no subscribers, and fallback paths that can
  never be reached.
- Flag speculative abstractions added "for future flexibility" with no current second
  implementation.
