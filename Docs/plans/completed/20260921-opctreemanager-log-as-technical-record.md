# OpcTreeManager: the log as a technical record

## Overview

The log is the only record of a rebuild: the destructive half runs deferred, after the host leaves
runtime, with no UI and no debugger. A commissioning engineer has to answer four questions from the
file alone, and today can answer none of them without the source and `tree.json`: what was planned,
what changed, what failed, what to do next.

Two defects carry most of that. The 21 September run failed to issue 119 links and closed with
`Deferred execution completed successfully`, a claim nothing verified. Every one of those 119 error
lines names only the external end, so no line attributes a single failure to the OPC node that owns
it.

This plan holds the log to one standard: every line is a claim that is true at the instant it is
written, worded minimally, emitted where the fact becomes known, naming domain things. It deletes
what fails that standard, rewrites what can meet it, and adds the small number of facts the module
knows and never records. It does not turn the log into a report: no summaries for management, no
banners, no reassurance, no new abstractions.

Scope is `NtoLib/OpcTreeManager` plus its tests and `Docs/opc-tree-manager.md` section 8.

## Context (from discovery)

The module has **40 logging call sites** (41 grep hits; `Logging/OpcLoggerFactory.cs:22` is
`.MinimumLevel.Debug()`, a config call). `Config/*.cs` and `TreeOperations/OpcProtocolAccessor.cs`
carry none: their facts surface through `Result` and are logged by the service. Measured
distribution on this branch: **13 Information, 6 Warning, 12 Error, 9 Debug**.

Evidence, all re-measured against the production log
`C:/DISTR/Logs/OpcTreeManager/opc-tree-manager.log` (6124 lines, five runs, 2026-08-03 to
2026-09-21; the last run starts at line 4763):

- The 119 unresolved links of the last run, mapped against the snapshot the run used: `CBr4` 94 of
  94 not issued, `Water` 12 of 12, `GasLine` 8 of 14, `Shutters` 5 of 13. Two nodes came back with
  no links at all. None of that is derivable from the file; it took a script over `tree.json`.
- `DeferredExecutor.cs:95` logs `Deferred execution completed successfully` whenever `Execute`
  returned `Ok`, which it did with `unresolved=119`. It carries the same timestamp as the summary
  line, `14:36:11.7391778`.
- `PlanExecutor.cs:223` logs only `link.ExternalPinPath`. The 119 lines hold 93 distinct paths, so 26
  read as exact duplicates: a base pin and its `$` sibling target the same external element.
- The output template is
  `{Timestamp:O} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}`
  (`OpcLoggerFactory.cs:16`). `PlanExecutor` hands its own contextual logger to `TreeReshaper` and
  `ConnectRunner`, so `SourceContext` reads `PlanExecutor` on 1231 of the 1233 Debug lines in the
  last run, at 49 characters per line.
- `ProjectSubtreeDisconnector.cs:35` returns `(1, 0, 1)` for a node missing from the project, so the
  summary's `disconnect links fail=1` counts a node as a link. Visible at log lines 123 and 4760.
- FluentResults' `Error with Message='...'` wrapper reaches the file from three places.
  `OpcTreeManagerService.cs:198` joins `IError` directly, which is witnessed at log line 1966.
  `:81-84` and `:186-187` each flatten an already-failed `Result` into a string, wrap it in a new
  `Error`, and let `LogAndFail` join it again; both are latent, because no run in the file
  exercises a snapshot load or write failure. The FB-level re-log that used to double it a fourth
  time was removed on this branch (`OpcTreeManagerFB.cs:167-168` carries the comment).
- `LinkCollector.cs:185` and `:197` are Warnings for a lossless deduplication. Neither fired once in
  five runs (`grep -c dedup` over the whole log returns 0).
- `PlanBuilder.cs:149-155` logs one Error per unrestorable node, but the block sits inside
  `CheckEmptyGroupRestoresSomething`, so for a non-empty group the same fact goes unlogged at plan
  time and surfaces only during execution as a Warning at `TreeReshaper.cs:107`.
- `TreeReshaper.cs:63-70` already computes `(total, success, fail)` per removed node and records only
  the aggregate. That aggregate lives on `ReshapeResult` and accumulates across the whole recursive
  walk, so it cannot be reused for a per-container line.
- `Config/*.cs` contains no logging call, while `Docs/opc-tree-manager.md:282` lists "загрузка
  конфигурации" as log content. The document is wrong there. `:284` lists "список изменений", which
  the code does not produce; there the document is right and the code is wrong.
- The reference snapshot `C:/DISTR/Config/OpcTreeManager/tree.json` is dated 2026-08-11 and the
  failing run is 2026-09-21. A 41-day-old snapshot is a live candidate cause of the 119 missing
  externals, and no line in the file records which snapshot content a rebuild used.
- `InitializeRuntime` assigns `_logger = OpcLoggerFactory.Build(LogFilePath)` as the first statement
  inside its `try` (`OpcTreeManagerFB.cs:106`). An unwritable path was expected to make
  `CreateLogger()` throw there, disabling the block for the rest of the session. Measured on this
  branch, it does not: `WriteTo.File` catches a sink construction failure, reports it to `SelfLog`
  and installs a null sink, so `Build` returns a working sink-less logger and the first write throws
  nothing. Four unopenable paths were driven through `Build` and a write (a file used as a directory
  component, `CON`, a path holding `|`, a file held open with `FileShare.None`); none threw, and the
  first and third produced no file. `AuditTo.File` is the propagating variant, and the module does
  not use it. The FB therefore already keeps running with logging off; what is missing is a test
  pinning that.

## Development Approach

- **testing approach**: regular, code first, tests in the same task
- one task per coherent group of call sites; complete each fully before the next
- run `dotnet format NtoLib.sln` after each code change
- keep this plan in sync when scope changes

## Testing Strategy

`Tests/OpcTreeManager/CapturingSink.cs` captures `LogEvent` objects, so a test asserts level,
rendered message and structured properties. What that reaches, and what it does not, decides the
acceptance criteria:

- **Reachable today**: `TreeReshaper` (`ApplyDesiredSpecTests` already drives it and passes
  `Logger.None`), `ConnectRunner` (driven through the `ConnectCommand` delegate seam with fakes),
  `PlanBuilder` (pure), `OpcLoggerFactory` (build against a temp file and read the first line back,
  the pattern `TreeSnapshotWriterTests` already uses).
- **Not reachable**: `DeferredExecutor.Post` needs `IProjectHlp.InRuntime`, a concrete
  `PlanExecutor` and a WinForms timer tick; `OpcTreeManagerService` needs the FB host.
- **Reachable only after this plan's seam**: `PlanExecutor.BuildCommands` resolves pins through
  `_project.SafeItem<ITreePinHlp>`. Task 2 threads that resolution in as a delegate, the same shape
  `TreeReshaper.Reshape` uses for `resolveChild` and `ConnectCommand` uses for the connect call. It
  is an existing house pattern, not a new abstraction, and it is what makes the not-issued lines and
  the per-node tally testable.

Assert only lines that carry a decision. A test per surviving line would pin wording no one reads.

## Acceptance Evidence

Automated:

```powershell
dotnet test NtoLib.sln --filter "FullyQualifiedName~OpcTreeManager"
```

Four assertions define the change, each on a seam the harness can drive:

- `BuildCommands_ExternalPinMissing_LineNamesBothEnds`: the not-issued event carries `LocalPin`,
  `ExternalPin` and `LinkType` as structured properties. Today the same site logs one path.
- `BuildCommands_NodeWithNoResolvableLink_LogsTheNodeTally`: a construction whose links all fail to
  resolve produces one Error naming the node with its resolved and unresolved counts. This is the
  line that would have read `CBr4: 94 links, 0 resolved, 94 unresolved`.
- `Reshape_NodeConstructedWithLinks_LogsOneInformationPerNode`: one Information event per node whose
  membership changed, carrying `NodePath` and `LinkCount`. Its sibling
  `Reshape_NodeConstructedWithoutLinks_IsRecordedAtErrorAndCounted` pins the other half: a node
  constructed with nothing to connect is an Error and feeds the summary's `noLinks`.
- `Build_OutputTemplate_OmitsSourceContext`: a logger built by `OpcLoggerFactory` writes a first
  line matching `^\S+ \[\w{3}\] `, with no class name between the level and the message.

Manual, in the MasterSCADA host, re-running the 21 September scenario against the same project:

1. Raise `Execute`, leave runtime, let the deferred pass finish
2. The file must answer, without opening `tree.json`: what the plan was before the destructive half
   ran, which nodes were constructed, that `CBr4` received 0 of its 94 links and `Water` 0 of 12,
   which snapshot file and which write date the rebuild used, and that the run did not succeed
3. No line claims a link is connected, and no line claims success

The old file answers none of 2. The clustering of missing externals under
`MBE_Avangard2.Установка.Шкаф CBr4` is the one thing it does show, which is why the criterion above
is the attribution to the OPC node, not the clustering.

## Progress Tracking

- mark completed items `[x]` when done
- new tasks get a ➕ prefix, blockers a ⚠️ prefix

## Solution Overview

**Level policy**, three lines, and every rewrite follows from it:

- **Error**: one line per domain item (node, link, group, file) that will not be in the planned
  state, or per operation that did not run; carries that item's identifiers. The rebuild as a whole
  is such an item: its terminal summary goes to Error whenever any of its counters is non-zero, and
  the duplication with the per-item Errors above it is deliberate, because the deferred pass cannot
  raise `Failed` and that line is the operator's only verdict.
- **Warning**: a fact that may leave an item in an unplanned state where the code cannot tell.
- **Information**: one line per operation boundary (runtime entered, group resolved, snapshot loaded
  or written, plan, container reshaped, rebuild finished) plus one per node whose membership
  changed. **Debug**: one line per link, plus lifecycle.

Nine call sites contradict it today: `ConnectRunner.cs:44` (Warning for a definite non-issue),
`TreeReshaper.cs:107` (Warning for a node that will not exist), `LinkCollector.cs:185` and `:197`
(Warning for a lossless fold), `LinkCollector.cs:32` and `TreeReshaper.cs:121` (Debug for per-node
membership facts), `DeferredExecutor.cs:95` (Information for an unverified outcome),
`PlanExecutor.cs:76` (Information for a restatement), `ProjectSubtreeDisconnector.cs:80`
(`Disconnected` claims an outcome nobody read back).

The per-task checkboxes are the complete list of verdicts; there is no separate tally to keep in
sync with them.

**What a link line may claim.** `Docs/known_issues/11` establishes that a connection cannot be read
back in-process: a connected link reads back as absent. So no line may say a link *is* connected.
`issued` is the strongest available claim and stays, on both the connect and the disconnect side.

**Decision: the log file is ASCII.** Link lines use `<->`, not `↔` or `←`. The file is read by
`Select-String`, `grep` and by whatever an engineer has to hand; a symbol that survives none of
those reliably buys nothing. This also removes the last non-ASCII character the module writes into
its own output.

**Decision: drop `{SourceContext}` from the output template.** It is wrong on 1231 of 1233 Debug
lines, costs 49 bytes each, and names a class rather than a domain thing. The alternative, giving
`TreeReshaper` and `ConnectRunner` their own contextual loggers, makes the field correct but keeps a
55-character namespace prefix on every line to identify something the rewritten messages already
name. Rejected on width.

**Decision: errors travel as `IError`, not as text.** The three wrapper sites flatten a failed
`Result` into a string and re-wrap it, which is what produces `Error with Message='...'`. The fix is
to keep the original error objects and attach context through `CausedBy`, so `LogAndFail` renders
messages once, from real errors. `Result.Try` does not apply here: it converts a throwing delegate
into a failed `Result`, and these sites have no exception to capture. It is already used where it
belongs, in `OpcConfigLoader.cs:24`, `TreeSnapshotLoader.cs:26` and `TreeSnapshotWriter.cs:32`.

**Decision: the deferred outcome reaches the operator through the log and nothing else.** Not a
preference: `ExecuteDeferred` hands `DeferredExecutor.Post` only `onFinished: () => PendingPlan =
null` (`OpcTreeManagerService.cs:121`), and no `SetPinValue` exists on that path.
`Docs/known_issues/07-fb-instance-replacement.md` explains why one cannot be added: the host
replaces the FB instance between runtime cycles, so a pin written after `ToDesign` is discarded. A
rebuild that fails therefore leaves `Failed` low and `IsPending` low, and the file is the only
record. Section 8 of the user document has to say so.

**Decision: the terminal line ends with the tree's state and the operator's next step** (added in
review). `Execute` returns `Ok` when the reshape succeeded and only the connects failed, which is the
21 September path, so the `close the project without saving` clause on `DeferredExecutor`'s exception
path never reaches that run. The `with failures` line therefore carries its own clause, and the
counters split it in two: unresolved links or `noLinks` alone mean the wiring is incomplete
(re-capture the snapshot or repair the consumers); `threw`, `nodesMissing` or `notRestored` mean the
tree itself is half built, and only the Error lines above say which part.

**Decision: `ExecuteSnapshot` counts nodes captured without links and leaves `Failed` low** (added in
review). The count enters the terminal capture line and raises it to Error, by the same rule the
rebuild summary follows. `Failed` stays low on purpose: the file was written, and a node with no
consumers legitimately has no links, so raising the pin would fire on a healthy capture. Section 8.4
of the user document carries the carve-out explicitly.

**Decision: the two failure states are separated by control flow, not by wording.** Failing to
resolve the protocol or the group returns before anything mutates; `CommitStructuralChange` returns
after the reshape has already swapped `Items` and fired live disconnects. Task 1 moves the FB item
lookup ahead of the reshape, which removes that second return site, leaving one post-mutation exit:
an exception. The failed-`Result` line can then state the tree is unchanged as a fact, and the catch
states the opposite. The fuller version of this, a `RebuildTarget` record and an `Apply` that
returns `void` so the compiler forbids a post-mutation `Result`, is rejected here: it is a refactor
of the execution path, and this plan is about what the file says. Revisit it if that path is opened
for another reason.

**Decision: throws stay in the summary, not in the per-node tally.** `ConnectRunner.ConnectAll`
returns `(Issued, Threw)` and nothing per command, so attributing a throw to its node means changing
that return type and threading per-command results back. The tally counts what `BuildCommands`
knows, links resolvable and not, and the summary keeps the throw count. Revisit only if a throw is
ever observed in production; across five runs `threw=0` every time.

## Technical Details

`PlanExecutor.BuildCommands` already iterates `foreach (var construction in constructions) foreach
(var link in construction.Links)`, so the per-node tally needs no new field on `ConnectCommand` and
no new type: count resolvable and unresolvable per construction inside that loop and emit one line
at its end. The pin resolution moves behind a delegate parameter so the loop is drivable from a
test; `TryBuildCommand` keeps its shape.

`TreeReshaper.cs:126` cannot reuse `result.ShrinkCount` for a per-container line: `ReshapeResult`
accumulates across the whole recursive walk (`ApplyDesiredSpec` recurses at `:90`), so the field
holds a running total. The reshaped line needs two per-invocation locals, one for removals and one
for skips; the skip count is not tracked anywhere today.

`DeferredExecutor.cs:124` and `:139` sit in the static helpers `RunTickGuarded` and
`AbortWithTimeout`, which never receive the plan. The `ForContext("GroupName", plan.GroupName)`
enrichment this plan first chose was replaced during review: it left `{GroupName}` and
`{TimeoutSeconds}` unbound in two templates, three frames from the only thing that filled them, and
dropping the enrichment would have written a literal `{GroupName}` into the file. Both helpers now
take the group name as a parameter and bind every token positionally.

`ProjectSubtreeDisconnector.DisconnectPinConnections` iterates `EConnectionTypeMask` values and has
no `LinkTypes` value in scope; the disconnect line needs a small mask-to-type mapping.

The snapshot write date comes from `File.GetLastWriteTime`, not from the file's content: `tree.json`
is a bare `Dictionary<string, NodeSnapshot>` with `links` and `scadaItem` only.

## What Goes Where

- **Implementation Steps**: code, tests and documentation in this repository
- **Post-Completion**: the host run, which no test can stand in for

## Implementation Steps

### Task 1: Stop the run claiming an outcome it never verified

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/DeferredExecutor.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`

- [x] delete `DeferredExecutor.cs:95`, `Deferred execution completed successfully`; the tally line
      emitted 0 ms earlier is the true terminal record
- [x] delete `:85`, `InRuntime=false; executing plan`; the first reshape line carries the execution
      timestamp
- [x] bind the group name onto the logger in `Post` so every message below can name it without a new
      parameter on the static helpers
- [x] rewrite `:61` as `Rebuild of group '{GroupName}' queued; waits for the host to leave runtime,
      timeout {TimeoutSeconds}s`, the only record that the FB saw `ToDesign` with a plan pending, and
      what separates "never left runtime" from "left runtime, process died"
- [x] inline `CommitStructuralChange` into `PlanExecutor.Execute`. The lookup could not be hoisted
      ahead of the reshape: the vendor takes the handle after `SynchWihSysTree`, so `Execute` still
      returns `Result.Fail` there, after the reshape. A post-mutation failed `Result` therefore
      exists, and what makes the next two lines true is the `onTreeMutationStarting` flag, not the
      removal of that exit
- [x] rewrite `:91` as `Rebuild of group '{GroupName}' not started, the tree is unchanged:
      {Reason}`, with `Reason` from the error objects. The flag picks the wording: a failed `Result`
      raised before the first write gets this line, one raised after it gets the partially-changed
      line below
- [x] rewrite `:100` as `Rebuild of group '{GroupName}' failed after the reshape began; the tree is
      partially changed, close the project without saving: {Reason}`, passing the exception object
      when there is one. Both a throw and a post-mutation failed `Result` land here, and the state is
      concrete: at the throwing level the removed subtrees are already live-disconnected
      (`TreeReshaper.cs:63-70` runs before any construction) while `Items` is not yet swapped
      (`:135` is last), and deeper levels of earlier siblings are already swapped (`:90`)
- [x] rewrite `:124` as `Rebuild of group '{GroupName}' abandoned`, exception object, dropping the
      timer aside
- [x] rewrite `:139` as `Rebuild of group '{GroupName}' not started: the host stayed in runtime
      {TimeoutSeconds}s after ToDesign; the plan is discarded`. The discard is the fact the operator
      needs and the current line omits it
- [x] delete `PlanExecutor.cs:76`: every value restates `Initialized`, `Group resolved` or the plan
      line, and `{Count}` is anonymous
- [x] no test: every site in this task is inside `DeferredExecutor.Post`, which needs a live
      `IProjectHlp` and a timer tick. Verified by the host run in Acceptance Evidence
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 2: Make every not-issued link name both ends, and stop counting a node as a link

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/ConnectRunner.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/ProjectSubtreeDisconnector.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/ISubtreeDisconnector.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/TreeReshaper.cs`
- Modify: `Tests/OpcTreeManager/Integration/Fakes/FakeSubtreeDisconnector.cs`
- Modify: `Tests/OpcTreeManager/Unit/ConnectRunnerTests.cs`
- Create: `Tests/OpcTreeManager/Unit/BuildCommandsTests.cs`

- [x] thread pin resolution into `BuildCommands` as a delegate so the loop runs without
      `IProjectHlp`, mirroring `TreeReshaper.Reshape`'s `resolveChild`
- [x] rewrite `PlanExecutor.cs:216` as `Link not issued, local pin missing: {LocalPin} <->
      {ExternalPin} ({LinkType})`, one grep prefix for all three not-issued cases
- [x] rewrite `:223` the same way with `external pin missing`; this is the line that appeared 119
      times naming only one end
- [x] rewrite `:230` as `Link not issued, unknown link type '{LinkType}': {LocalPin} <->
      {ExternalPin}`
- [x] raise `ConnectRunner.cs:44` to Error and pass the exception object instead of `ex.Message`: a
      vendor rejection leaves the link out of its planned state, and the HRESULT is the only handle
      on it
- [x] rewrite `ProjectSubtreeDisconnector.cs:80` as `Disconnect issued {LocalPin} <-> {ExternalPin}
      ({LinkType})` with a mask-to-type mapping. `Disconnected` claims a read-back nobody performed
- [x] rewrite `:88` to pass the exception object, same template shape
- [x] rewrite `:35` as `Remove '{NodePath}': node not found in the project, links not disconnected`
- [x] widen `ISubtreeDisconnector.DisconnectSubtree` with a missing-node count so `(1, 0, 1)` stops
      reporting a node as a failed link, and thread it through `ReshapeResult` and the summary;
      update `FakeSubtreeDisconnector`
- [x] rewrite the summary at `PlanExecutor.cs:136` against the final tuple: `Rebuild of group
      '{GroupName}' finished: removed={RemovedCount} constructed={ConstructedCount}; disconnects
      issued={DisconnectsIssued} threw={DisconnectsThrew} nodesMissing={NodesMissing}; connects
      issued={ConnectsIssued} threw={ConnectsThrew} unresolved={LinksUnresolved}`; drop `total` (it
      is `ok + fail`) and the read-back aside
- [x] write `BuildCommands_ExternalPinMissing_LineNamesBothEnds` and the local-pin and unknown-type
      cases against the new delegate
- [x] write a test that a throwing connect is logged at Error with its exception object
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 3: Record what changed, per node

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/TreeReshaper.cs`
- Modify: `NtoLib/OpcTreeManager/TreeOperations/PlanExecutor.cs`
- Modify: `Tests/OpcTreeManager/Integration/ApplyDesiredSpecTests.cs`
- Modify: `Tests/OpcTreeManager/Unit/BuildCommandsTests.cs`

- [x] raise `TreeReshaper.cs:121` to Information and reword as `Constructed '{NodePath}':
      {LinkCount} links to connect`
- [x] rewrite `:86` as `Preserved '{NodePath}'` at Debug; `links intact` is unverified
- [x] reword `:107` as `Not restored '{NodePath}': not in the container, not in the snapshot`, at
      Debug for a top-level node and at Error for a nested one. After Task 4 the plan-time line owns
      the top-level fact, and two Errors for one fact breaks the standard this plan sets; but the
      plan checks only top-level names against the snapshot, so a nested node has no other record
      and keeps its Error (corrected during review: the original checkbox dropped every level to
      Debug and left nested losses with no Error anywhere)
- [x] rewrite `:126` as `Reshaped '{ContainerPath}': removed={RemovedCount}
      constructed={ConstructedCount} preserved={PreservedCount} skipped={SkippedCount}` using two new
      per-invocation locals; `result.ShrinkCount` accumulates across the recursive walk and would
      print a running total
- [x] add an Information line in the removal loop at `:63-70`: `Disconnect issued for '{NodePath}':
      links={IssuedCount} threw={ThrewCount}`. Removal is the destructive half and has no per-node
      record at all today
- [x] count resolvable and unresolvable links per construction inside `BuildCommands` and emit
      `Node '{NodePath}': {LinkCount} links, {ResolvedCount} resolved, {UnresolvedCount} unresolved`,
      at Error when any is unresolved. Both halves are build-time words: the line is emitted inside
      `BuildCommands`, before any connect is attempted (corrected during review: the original
      checkbox said `issued`, a connect-time word for a number counted at build time)
- [x] write `Reshape_NodeConstructedWithLinks_LogsOneInformationPerNode` (it shipped under that name,
      with `Reshape_NodeConstructedWithoutLinks_IsRecordedAtErrorAndCounted` beside it) and a
      per-container reshaped-line
      test that would fail on the running-total bug
- [x] write `BuildCommands_NodeWithNoResolvableLink_LogsTheNodeTally`
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 4: State the plan before the operator commits to it

**Files:**
- Modify: `NtoLib/OpcTreeManager/Facade/PlanBuilder.cs`
- Modify: `Tests/OpcTreeManager/Unit/PlanBuilderValidationTests.cs`

- [x] replace `PlanBuilder.cs:88` with `Plan for group '{GroupName}', project '{TargetProject}':
      remove {RemoveNodes}; construct {ConstructNodes} ({LinkCount} links in the snapshot); preserve
      {PreserveNodes}`, the three name lists computed from `desiredTree` against
      `currentTopLevelNames`
- [x] rewrite `:64` as `Group '{GroupName}' top-level node names already match project
      '{TargetProject}'; no rebuild`. The check compares names only, and `contents already match`
      overclaims to a reader whose links are broken
- [x] rewrite `:151` as `Node '{NodePath}' of project '{TargetProject}' will not be restored: not in
      the group, not in the snapshot`
- [x] restructure so that record is emitted for a non-empty group too: `missingFromSnapshot` also
      feeds the empty-group refusal at `:142`, so compute it once above the gate and log below it
- [x] append the available project keys to the failure message at `:48`, `projects present: [...]`,
      so the property can be corrected without opening the YAML
- [x] write tests for the plan line's three lists and for the non-empty-container case now logging
      the unrestorable node at plan time
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 5: Carry errors as errors across the service boundary

**Files:**
- Modify: `NtoLib/OpcTreeManager/Facade/OpcTreeManagerService.cs`
- Modify: `NtoLib/OpcTreeManager/OpcTreeManagerFB.cs`

- [x] stop flattening failed results into strings at `:81-84` and `:186-187`: pass the original
      errors on and attach context with `CausedBy`, so `LogAndFail` renders them once
- [x] fix `:198` to render `IError.Message` rather than `IError.ToString()`, which is what prints
      the `Error with Message='...'` wrapper
- [x] delete `:62`: `Tree scan begin` restates three properties from the initialization line, and the
      only new fact arrives 6 ms later
- [x] delete `:169`: `Snapshot built` is emitted 0 ms before `Snapshot written`; its count folds into
      that line
- [x] rewrite `:191` as `Snapshot of group '{GroupName}' written to '{TreeJsonPath}': {NodeCount}
      nodes, {LinkCount} links`
- [x] rewrite `:93` as `Snapshot '{TreeJsonPath}': {DroppedLinkCount} links with a blank pin path
      ignored`
- [x] rewrite `:128` as `Pending rebuild of group '{GroupName}' cancelled`; `by user` is unverified,
      the `Cancel` pin can be driven by logic
- [x] add after the snapshot load: `Snapshot '{TreeJsonPath}' loaded: {NodeCount} nodes,
      {LinkCount} links, file written {SnapshotWrittenAt:O}` from `File.GetLastWriteTime`, and state
      what the line says when the timestamp cannot be read
- [x] rewrite `OpcTreeManagerFB.cs:116` as `Runtime entered; OpcFbPath={OpcFbPath}
      GroupName={GroupName} TargetProject={TargetProject} TreeJsonPath={TreeJsonPath}
      ConfigYamlPath={ConfigYamlPath}`; the two file paths are never recorded today
- [x] no test: `OpcTreeManagerService` needs the FB host, as the Testing Strategy states. Verified by
      reading and by the host run
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 6: Cut the capture-path noise

**Files:**
- Modify: `NtoLib/OpcTreeManager/TreeOperations/LinkCollector.cs`
- Modify: `Tests/OpcTreeManager/Unit/LinkCollectorTests.cs`

- [x] delete `:185` and `:197`: both are Warnings for a lossless deduplication, neither fired once in
      five production runs, and a Warning for a no-loss event trains the reader to ignore Warnings
- [x] keep `:144` at Debug and shorten it to `Iconnect {LocalPin} <-> {ExternalPin}: input sibling
      '{Sibling}' captured to '{SiblingExternal}'`. It fired in production (log line 4720) and this
      shape is the signature of the `DedupByWire` defect in `known_issues/11`, so the fact stays;
      only the prose about the expected shape goes
- [x] rewrite `:151` as `Iconnect {LocalPin} <-> {ExternalPin} captured without an input link on
      '{Sibling}'`, dropping the alternatives and the document pointer
- [x] raise `:32` to Information and reword as `Captured '{NodePath}': {LinkCount} links`: the
      per-node tally at capture is what a later rebuild is compared against
- [x] rewrite `:219` as `Captured {LinkType} {LocalPin} <-> {ExternalPin}` and keep it: `tree.json`
      is overwritten by the next capture, so the log is the only history of what an earlier snapshot
      held
- [x] update the tests that assert on the deleted warnings
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 7: Template and property hygiene

**Files:**
- Modify: `NtoLib/OpcTreeManager/Logging/OpcLoggerFactory.cs`
- Modify: every file touched in Tasks 1-6
- Create: `Tests/OpcTreeManager/Unit/OpcLoggerFactoryTests.cs`

- [x] drop `{SourceContext}` from the output template at `OpcLoggerFactory.cs:16`
- [x] give `{Path}` a distinct name per thing it names: `TreeJsonPath`, `LocalPin`, `ExternalPin`
- [x] rename `{NodeFullName}` to `{NodePath}`, the name used everywhere else
- [x] remove `{Message}` as a property name: it collides with the output template's built-in token;
      both sites that used it now pass the exception object instead
- [x] replace every `↔` and `←` in a message template with `<->`, so the file the module writes is
      ASCII throughout
- [x] confirm no anonymous `{Count}` remains
- [x] write `Build_OutputTemplate_OmitsSourceContext` against a temp file, the pattern
      `TreeSnapshotWriterTests` already uses
- [x] run `dotnet format NtoLib.sln` and the full suite

### Task 8: A log that cannot be written must not stop the FB

Failing to log is not a failed operation. The FB must keep working with logging off.

Measured before writing any fix, against four unopenable paths: `Build` does not throw and neither
does the first write. `WriteTo.File` already installs a null sink when it cannot open the file, so
`Build` is total as written, `InitializeRuntime`'s catch never fires for this cause and
`_isRuntimeInitialized` stays true. The task is therefore a test that pins the behaviour, plus the
one-line note that says which Serilog contract it rests on.

**Files:**
- Modify: `NtoLib/OpcTreeManager/Logging/OpcLoggerFactory.cs`
- Modify: `Tests/OpcTreeManager/Unit/OpcLoggerFactoryTests.cs`

- [x] measure where the failure lands before changing anything: `Build` returns, the write returns,
      no file appears, nothing throws. The plan's assumption of an eager open was wrong and the
      Context bullet is corrected
- [x] `Build` needs no change to be total; note on it that `WriteTo.File` swallows sink construction
      failures, because nothing in the method body says so and `AuditTo.File` behaves the opposite way
- [x] leave `OpcTreeManagerFB.InitializeRuntime` untouched: its catch does not fire for this cause,
      `_isRuntimeInitialized` stays true and the `Failed` pin stays low, which is correct because no
      operation failed
- [x] do not add a fallback file, a second path or a pin: the log is either at the configured path or
      absent, and nothing else in the module changes behaviour on that
- [x] write `Build_UnwritablePath_ReturnsLoggerThatWritesNothing`: `Build` against a path whose
      directory component is a file returns a logger, writing to it throws nothing, and neither the
      directory nor the file appears
- [x] run `dotnet format NtoLib.sln` and the OpcTreeManager test filter

### Task 9: Make section 8 describe the shipped log

**Files:**
- Modify: `Docs/opc-tree-manager.md`

- [x] correct `:282`: nothing logs a successful config load and nothing should; it becomes "ошибки
      загрузки конфигурации и снапшота"
- [x] state the level policy in three lines so a reader knows what a Warning means in this file
- [x] describe the plan line, the per-node construction and removal records, and the per-node link
      tally, which is what `:284` "список изменений" has always promised
- [x] state that no line claims a link is connected, and why, pointing at `known_issues/11`
- [x] state that an unwritable `LogFilePath` disables logging and nothing else: the FB keeps running
      and `Failed` stays low
- [x] state that `Failed` reports scan and snapshot failures only. The deferred rebuild cannot raise
      it, because the host replaces the FB instance between runtime cycles
      (`known_issues/07-fb-instance-replacement.md`), so its outcome is in the log and nowhere else.
      An operator waiting for `Failed` to tell them a rebuild went wrong is waiting for something
      that cannot happen
- [x] confirm the remaining bullets still match the code

### Task 10: Verify acceptance criteria

- [x] confirm every requirement in the Overview is implemented
- [x] run the full suite: `dotnet test NtoLib.sln`
- [x] run `dotnet format NtoLib.sln`; every file this branch touches is clean. `--verify-no-changes`
      still exits 2 on a pre-existing IDE1006 in
      `Tests/MbeTable/Infrastructure/EditorRuntimeOptionsProviderTests.cs:74`, outside this branch
      and not fixed here
- [x] confirm each of the four Acceptance Evidence assertions exists and passes
- [x] confirm every message template the module writes is ASCII. The values are not and cannot be:
      `OpcFbPath` defaults to `Система.АРМ.OPC UA Siemens` and reaches the file through `Runtime
      entered`
- [x] reread every comment added by Tasks 1-9: English, ASCII, one paragraph, no restating of what
      the code already says

## Verify it yourself

The change is a change to a file nobody can unit-test end to end, so the demonstration is a
before-and-after on the same input.

Automated, and it fails before the change:

```powershell
dotnet test NtoLib.sln --filter "FullyQualifiedName~BuildCommands_NodeWithNoResolvableLink_LogsTheNodeTally"
```

The test does not compile before Task 2, because `BuildCommands` has no seam to drive; it fails on
its assertion before Task 3, because no line names the node. Both are the point.

By hand, on the two logs:

```powershell
Select-String -Path C:\DISTR\Logs\OpcTreeManager\opc-tree-manager.log -Pattern 'ServerInterfaces\.MBE\.CBr4'
```

The pattern is the OPC-side path, which is what the old lines never carry: filtering on the
external end instead (`-notmatch 'Шкаф CBr4'`) would drop the new lines too, because each of them
names `Шкаф CBr4...` as its external end.

Within the 21 September run this returns exactly one line, the construction record: the 94
not-issued lines of that run name only the external end (measured: 95 lines of the run hold
`CBr4`, 94 of them only as `Шкаф CBr4`). After the change the same query returns 96 lines for that
run: the construction record, the per-node tally reading 94 links 0 resolved 94 unresolved, and 94
not-issued lines that each name `...ServerInterfaces.MBE.CBr4...` as the local end. Earlier runs in
the same file also match the pattern, so scope the query to the run under test.

## Post-Completion

**Manual verification:**

- the two-step host run in Acceptance Evidence, on a project copy, re-running the 21 September
  scenario so the same 119 unresolved links are produced against the new lines
- read the resulting file end to end once as a technician would, and check that the four questions
  are answerable from it alone: what was planned, what changed, what failed, what to do next

**Branching:**

- this plan edits the same files as `opctreemanager-empty-group-resolution`, which is not yet
  delivered. Branch from that branch's tip, or from `master` after it merges; do not run the two in
  parallel

**Executed by exec:**

- branch: `opctreemanager-log-as-technical-record`, stacked on
  `opctreemanager-empty-group-resolution`. Deliver the parent first; every review in this run diffed
  against it rather than `master`
- `Docs/plans/20260921-opctreemanager-opcfbpath-resolution.md`, the queued backlog plan, is
  committed on this branch. It belongs to a different change and the delivery step decides whether
  to split it out; this run could not, because removing it needs a history rewrite
