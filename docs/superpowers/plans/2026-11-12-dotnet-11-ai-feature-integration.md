# Project Aegis .NET 11 AI feature integration plan

Build an evidence-linked after-action assistant first, then structured mission authoring and catalog research assistance. Keep model requests outside simulation ticks. Route accepted authoring changes through the existing validation and proposal workflows.

This is a proposed, unscheduled plan dated 2026-11-12 (revision 2, same day). Implementation, provider selection, performance measurements, and owner acceptance are pending.

Revision 2 was reviewed against `origin/main` at `2ddbd393` and the .NET 11 GA documentation. It corrects three facts, adds repository surfaces the first draft missed, moves the AI code into its own assemblies, and restructures delivery into parallel waves with disjoint file ownership. [Review record](#review-record) lists every change.

## Preconditions

The first draft called the [.NET 11 upgrade](2026-11-12-dotnet-11-upgrade.md) "completed". On `origin/main` at `2ddbd393` it is not:

- All 16 projects target `net10.0`; four also target `netstandard2.1` (Data, Sim, Delegation, Delegation.UnityAdapter).
- `global.json` pins SDK `10.0.403` with `rollForward: latestMajor`. Any machine with the .NET 11 SDK installed already builds with it. Decide in the upgrade plan whether that is intended.
- The upgrade plan and the [dashboard snapshot](../../reports/dashboard-snapshots/2026-11-12-dotnet-11-ai-planning.md) linked here are not on `origin/main`. Land them with this plan or the links break.

Gate G0 therefore blocks only the work that needs a `net11.0` host. Contracts, fixtures, and the fake provider are framework-neutral and start immediately (Wave 0).

**G0 exit criteria** (owned by the upgrade plan, restated because every wave depends on them):

1. `net11.0` host builds; the four shared projects still build for `netstandard2.1`; `LangVersion` stays `12.0` in every headless csproj ([Directory.Build.props](../../../Directory.Build.props) says "Do not raise").
2. Full gate script [`tools/buildkite/dotnet-ci.sh`](../../../tools/buildkite/dotnet-ci.sh) is green: 0 failures, ReplayGolden 6/6, PlayModeSmoke ≥ 20/20, Baltic v2 hash `17144800277401907079`, isolated v3 goldens untouched.
3. Record the measured pass count at G0. [`production/test-floor.json`](../../../production/test-floor.json) holds the floor (1638); the 2026-11-01 dispatch measured 4058. New AI tests are additive. Raising the floor is an owner decision.
4. Replay fingerprints compared before and after on the same hardware. The upgrade plan's compatibility table covers the .NET 11 changes that can alter results (correctly rounded `Math.Round`, saturating float-to-small-integer casts, the x86-64-v2 minimum).

G0 is the exit of PR 2 (retarget) in the upgrade plan's three-PR stack.

## Decisions carried from the upgrade review

1. Separate runtime features from package integrations. Microsoft.Extensions.AI, structured output, embeddings, and evaluations also work on earlier .NET versions. Measure the .NET 11-specific benefits independently. [AI libraries](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) · [Structured output](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/structured-output)
2. Retain the C# 12 contract. C# 15 unions and closed hierarchies need a separate language-policy change. Use records and enums.
3. Runtime Async is opt-in, not preview. **Corrected:** at GA a `net11.0` project enables it with `<Features>runtime-async=on</Features>` and no longer needs `EnablePreviewFeatures`. The .NET runtime libraries are themselves compiled with it, so library async behavior changes on `net11.0` whether or not the host opts in. Benchmark three configurations: `net10.0`, `net11.0` default, `net11.0` opted in. Verify cancellation, exception flow, and tracing with the real provider library before opting in. Roll back with `<UseRuntimeAsync>false</UseRuntimeAsync>`. [Runtime Async](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime)
4. Evaluate AI quality independently of simulation correctness. Preserve authoritative scores and replay fingerprints alongside generated explanations.

## Repository evidence

The first draft used checkout `f999967d` and a GitNexus index 218 commits behind it. Counts below are provisional navigation evidence. Reindex on the implementation revision and repeat method-level impacts before edits.

| Integration point | Source finding | GitNexus upstream result |
| --- | --- | --- |
| `HindsightSessionFinalizer.OnScenarioFinalized` | Builds an `IReadOnlyOrderLog` summary. Retain is fire-and-forget; `ReflectAarAsync` discards its result and swallows failures. | LOW, 2 direct, 3 within depth 2 |
| `MissionPlanSuggestCommand.Run` | Keyword matching; emits anonymous objects with fixture IDs (`u1`, `hostile-1`) and hard-coded coordinates. | LOW, 4 direct, 5 within depth 2 |
| `PlatformDesignAssistant` | Draft uses catalog data and relative scaling; Propose stages write-gate batches. | LOW, 1 direct, 2 total |
| `AdviceSkillService.Invoke` | Emits a validated read-lane `SkillEnvelope` from a tick-bound `AdviceFrame`. Invocation key is `skillId:tick:time:contactId`. | LOW, 3 direct, 4 within depth 2 |
| `DatabaseIntelligenceOrchestrator.Run` | Stable four-agent pipeline; retrieval documented as skipped (P0). | Method impact required |
| `ExperimentSeedGridBatch.Run` | Serial runs; static `BuildId`; doctrine, ROE, and force-ratio IDs are metadata only. | Method impact required |

**Surfaces the first draft missed** (verified at `2ddbd393`):

| Surface | Why it matters |
| --- | --- |
| [`AiAuthoringServices`](../../../src/ProjectAegis.Data/Scenario/Authoring/AiAuthoringServices.cs): `NlScaffold`, `CheckPlacement`, `RunSmokeTestAgent`, `ExplainWithEvidence`, plus the `scenario_ai_scaffold` verb | Deterministic keyword stubs for the same job slice 3 proposes. Slice 3 extends this seam; it does not add a parallel one. `StubScopePinTests` pins the stub behavior, so provider mode must be additive. |
| `ScenarioManifest.ProvenanceTag` (`Source`: `user`, `ai`, `import`, `ai-nl-scaffold`; `AgentId`, `Evidence`, `Hash`) | Existing provenance record. AI artifacts reuse it for authored content instead of inventing a second scheme. |
| [`SkillLane`](../../../src/ProjectAegis.Delegation/Skills/SkillIds.cs) `Read` / `Propose` / `Submit` | The proposal lane already exists. Model output maps to `Propose`; nothing model-generated reaches `Submit` without a human. |
| [`tools/mission-editor/mcp-tools.json`](../../../tools/mission-editor/mcp-tools.json) (schema v2, 52 tools) and `McpToolsManifestTests` | The CLI is already the MCP tool surface. Every new verb needs a manifest entry and a test update. Only a separate MCP *server* is deferred. |
| [`NoDynamicExecutionGateTests`](../../../src/ProjectAegis.Data.Tests/Architecture/NoDynamicExecutionGateTests.cs) | Scans authoring-path sources for `Process.Start`, Roslyn, and `Reflection.Emit`. The .NET 11 `Process.Run*` helpers are not matched by that substring and would pass the scan while breaking its intent. See capability table. |
| [`tools/copy-delegation-assemblies.sh`](../../../tools/copy-delegation-assemblies.sh) | Copies every DLL from the UnityAdapter `netstandard2.1` publish into `Assets/Plugins/ProjectAegis`. Any package referenced by a shared project ships into Unity. |
| `OrderLogEntry.SequenceId` | Stable event identity already exists; evidence IDs derive from it. |
| [`Program.cs`](../../../src/ProjectAegis.MissionEditor.Cli/Program.cs) (1336-line verb switch) | Merge hotspot against ~170 remote branches. Touch it once per wave. |

Hindsight at `localhost:8888` refused the health check during drafting, so historical dev-memory decisions were not recalled. `DelegationBridge` and `CatalogWriteGate` remain CRITICAL: no edits to the first; extend-only for the second.

Source anchors: [Hindsight finalizer](../../../src/ProjectAegis.Delegation/Hindsight/HindsightSessionFinalizer.cs) · [memory contract](../../../src/ProjectAegis.Delegation/Hindsight/IHindsightMemoryClient.cs) · [mission suggestions](../../../src/ProjectAegis.MissionEditor.Cli/MissionPlanSuggestCommand.cs) · [platform assistant](../../../src/ProjectAegis.Data/PlatformAssistant/PlatformDesignAssistant.cs) · [database intelligence](../../../src/ProjectAegis.Data/Agents/DatabaseIntelligenceOrchestrator.cs) · [advice contracts](../../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/AdviceModels.cs) · [read-lane service](../../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/AdviceSkillService.cs) · [experiment runner](../../../src/ProjectAegis.Delegation.Demo/ExperimentSeedGridBatch.cs)

## Capabilities to adopt

| Capability | Proposed use | Adoption condition | Verdict |
| --- | --- | --- | --- |
| .NET 11 `JsonSerializer.SerializeAsyncEnumerable(..., topLevelValues: true)` | Stream AAR evidence and evaluation records as JSONL | Source-generated `JsonTypeInfo<T>` for fixed property order; versioned DTOs; cancellation and partial-file recovery tests | Adopt in slice 1. Output is always LF and ignores `WriteIndented`, so files hash identically on Windows and Linux. |
| Microsoft.Extensions.AI `IChatClient` | Provider abstraction, structured responses, telemetry | One provider plus an offline fake client | Adopt in slice 2 |
| Microsoft.Extensions.AI.Evaluation | Compare explanations and drafts with fixtures | Quality evaluators call a judge model, so they run only in the live-evaluation lane. CI uses plain xUnit assertions on IDs and numbers. | Adopt, live lane only |
| .NET 11 `ReadOnlyMemoryStream`, `ReadOnlySequenceStream`, `StringStream` | Feed existing buffers to parsers without copying | Profiling shows copies matter | Opportunistic. No story until a profile asks for it. |
| .NET 11 `Process.RunAndCaptureTextAsync` and related helpers | First draft: invoke validation tools from the host | See note | **Rejected for authoring and validation.** Allowed only for the Unity-to-host process boundary. |
| `IEmbeddingGenerator` and vector storage | Retrieve catalog and previous-AAR evidence | Pilot beats the existing lookup baseline on the same queries | Slice 4 decision gate |

Process helper note. The validators (`ScenarioValidationEngine`, export gate, write gate) are in-process C# libraries; shelling out to them adds a failure mode and contradicts requirement 11's "no arbitrary code execution" rule. Two follow-ups regardless: extend `NoDynamicExecutionGateTests` to also forbid `Process.Run`, `RunAndCapture`, `StartAndForget`, and `SafeProcessHandle.Start`; and note that `ProcessStartInfo.KillOnParentExit` is Windows-only, so any Linux use needs explicit kill-tree on deadline.

The JSONL, stream-adapter, and process APIs are documented .NET 11 library additions. [Library release documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries) · [Evaluation libraries](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries)

## Host and data boundaries

**Changed from the first draft.** Do not start in an `AI` folder inside the mission-editor CLI. Create the AI assemblies up front:

| Project | Targets | References | Contents |
| --- | --- | --- | --- |
| `ProjectAegis.Ai.Contracts` | `netstandard2.1;net11.0`, no package references | none | Evidence DTOs, artifact envelope, status enum, transport DTOs Unity can read |
| `ProjectAegis.Ai.Host` | `net11.0` | Contracts, Data, Delegation, Microsoft.Extensions.AI | Evidence export, provider adapters, validation, artifact store, DI composition |
| `ProjectAegis.Ai.Host.Tests` | `net11.0` | Host | Deterministic tests with the fake client. In the solution, in the CI gate. |
| `ProjectAegis.Ai.LiveEval` | `net11.0` | Host, Evaluation packages | Live provider evaluations. **Not in `ProjectAegis.sln`.** Run on demand. |

Reasons: new directories conflict with no open branch; AI packages cannot leak into the four shared projects or the Unity plug-in folder; the CI gate stays credential-free by construction; the CLI gains thin verbs that call the host, one `Program.cs` edit per wave.

Guard the boundary with one architecture test in Wave 0: the four `netstandard2.1` csproj files and `Assets/Plugins/ProjectAegis` contain no `Microsoft.Extensions.AI*` reference or DLL, and `Ai.Contracts` has zero package references.

The host receives a completed report or immutable authoring snapshot, validates it, requests bounded model output, validates the response, and publishes a reviewable artifact. Unity integration uses a process boundary with `Ai.Contracts` DTOs and presentation adapters under ADR-010, ADR-007, and ADR-001.

**Artifact envelope** (frozen in Wave 0): schema version, build identity, scenario and catalog revision, order-log fingerprint, provider and model identity, prompt version, evidence IDs, viewing side, generation status, content hash. Cache identity is the hash of all of these. Authored content additionally carries a `ProvenanceTag` with `Source = "ai"`.

**Side isolation.** Evidence is filtered to the viewing side before it reaches the prompt builder, not after generation. Full-truth post-run reports require an explicit debrief mode recorded in the envelope.

**Untrusted input.** Scenario names, briefs, unit names, and retrieved documents are user- or web-authored. Treat all evidence text as data in every slice, not only slice 4: delimit it, never concatenate it into instructions, and test with fixtures that embed instructions.

**Data egress.** Only repository-public content may leave the machine: public-corpus catalog rows, scenario documents, order logs from public fixtures. Nothing derived from Track A of the [dual-track firewall](../../engineering/dual-track-cmo-analysis-and-catalog.md) is ever sent to a provider. Enforce with an allowlist on evidence sources and a test that a Track A-tagged source is refused.

**Failure behavior.** Timeout, cancellation, malformed output, and unavailable models return an explicit status plus the existing deterministic report. Validate complete records before display. Write to a temporary file and publish atomically. Bound request concurrency, evidence size, tool calls, and output tokens through host configuration. Credentials come from environment or user-secrets only. Default telemetry excludes secrets and full prompt bodies.

**ADR needed.** Record the host boundary, egress rule, and artifact envelope as ADR-030 before Wave 1 merges. [ADR-015](../../architecture/adr-015-agent-authored-scenario-transparency.md) (agent-authored labeling) is still Proposed with a 2026-09-01 target date that has passed; slice 3 cannot ship until it is decided.

## Delivery: parallel waves

Follows the [dispatch playbook](../../../production/agentic/linear-parallel-dispatch-playbook.md): isolated worktrees, disjoint surfaces, one coordinator. Shared files (`ProjectAegis.sln`, `Program.cs`, `mcp-tools.json`, `renovate.json`, `AGENTS.md`) are edited only by the coordinator's integration PR at the end of each wave. Story IDs are placeholders until Linear tickets exist.

```
G0 (upgrade) ────────────────┐
Wave 0 contracts ──┬─ A evidence export ──┬─ slice 2 AAR assistant ──┐
(no G0 needed)     ├─ B fake client+host ─┤                          ├─ slice 5 (optional)
                   ├─ C fixtures ─────────┼─ slice 3 authoring ──────┤
                   └─ D gates + ADR-030 ──┴─ slice 4 retrieval pilot ┘
```

### Wave 0: contracts and gates (4 tracks, no G0 dependency except A's writer)

| ID | Track | Allowed surface | Deliverable |
| --- | --- | --- | --- |
| W0-A | Contracts | `src/ProjectAegis.Ai.Contracts/` | Evidence DTO v1, artifact envelope, status enum. Frozen at end of wave; later changes are additive with a version bump. |
| W0-B | Host skeleton | `src/ProjectAegis.Ai.Host/`, `…Host.Tests/` | DI root, options, `FakeChatClient` with scripted responses (success, malformed, timeout, cancel), artifact store with atomic publish. Builds on `net10.0` until G0, then retargets. |
| W0-C | Fixtures | `tests/ai-fixtures/` (new) | Order logs and expected-fact files for: success, failure, empty evidence, policy denials, unavailable memory, malformed response, conflicting sources, embedded instructions, two-sided hidden information. Expected facts and evidence IDs stored separately from prose. |
| W0-D | Gates and ADR | `docs/architecture/adr-030-*.md`, one new test file in `Data.Tests/Architecture/` | ADR-030 draft; package-leak architecture test; extended no-dynamic-execution patterns. |

Exit: contracts frozen, fake client green in CI, fixtures reviewed, ADR-030 accepted or explicitly waived by the owner.

### Slice 1: evidence export (needs G0 for the JSONL writer)

Project `IReadOnlyOrderLog` (chronological entries, records, policy denials, engagements, controller changes, fingerprint), `AgentPerformanceAarProjection`, and authoritative result fields into the evidence DTO. Evidence ID = `{fingerprint-prefix}:{SequenceId}`. Write JSONL through `SerializeAsyncEnumerable` with source-generated metadata.

Persist completed evidence before asking Hindsight or a provider to process it. Do not change `OnScenarioFinalized`; [ADR-019](../../architecture/adr-019-agentic-aar-readonly-order-log.md) already guarantees the export cannot mutate the log.

Acceptance:
- Round-trip parse equals input for every fixture.
- Identical input yields byte-identical output on Windows and Linux.
- Cancellation mid-export leaves no published file; rerun succeeds.
- Replay fingerprints match with export enabled, disabled, delayed, or failing.
- Peak memory recorded for a small and a large log.

### Slice 2: evidence-linked after-action assistant

Opt-in verb `aar_explain` in the CLI, implemented in the host. Returns structured summary, outcomes, uncertainties, and citations. Quantitative values are copied from the source report by the host; the model supplies prose and evidence IDs only. Reject the response if any cited ID is absent from the evidence or any number in prose disagrees with the report.

Hindsight: add a separate recall interface in the host adapter (the `IHindsightMemoryClient` comment mentions recall; the interface exposes only retain and reflect). To expose reflection results, add a new awaitable finalize method beside `OnScenarioFinalized`. Leave the existing method and its fire-and-forget teardown behavior untouched. The first slice reads the exported file and needs neither change.

Acceptance (deterministic, CI): all citations resolve; zero fabricated IDs; zero contradictory numbers; every failure fixture returns its explicit status and the deterministic report.
Acceptance (live lane): p50/p95 latency, tokens, cost, groundedness per provider on identical fixtures; human-rated usefulness against the deterministic summary.

**Go/no-go to default-visible:** 100% citation resolution and zero numeric contradictions across the fixture set on two consecutive live runs, and the owner accepts the measured cost per report. Budgets are set from the first measurement.

### Slice 3: structured mission authoring (blocked on ADR-015)

Add a provider-backed authoring path in `Ai.Host` that produces the same result shape as `AiAuthoringServices.NlScaffold`. `AiAuthoringServices` lives in `ProjectAegis.Data` (a `netstandard2.1` shared project), so no provider code or AI package goes there; keep its keyword stubs as the fallback so `StubScopePinTests` and `MissionPlanSuggestCommandTests` stay green unmodified. Supply allowed unit and target IDs from the open scenario, supported mission types, catalog references, and the authoring schema. Convert output to typed proposed operations on the `Propose` lane, run `ScenarioValidationEngine`, and show a diff before the user applies anything. Each proposal carries an idempotency key so a retry cannot apply twice, and the scenario edit version so a stale proposal is refused.

Acceptance: every fixture yields a valid draft or actionable diagnostics; unknown IDs, ambiguous requests, unsupported roles, duplicate operations, and stale revisions are each covered; `ProvenanceTag` present on every applied operation; valid-draft rate and human correction count measured against the keyword baseline. CLI before editor controls.

### Slice 4: catalog and AAR retrieval pilot

Index approved public fixtures, catalog descriptions, and selected completed AARs with source hashes and revision metadata. Run the same query set through catalog/keyword lookup and an embedding pilot.

**Decision gate:** add vector storage only if the pilot improves recall and citation precision by a margin the owner sets before the pilot runs. Otherwise stop here and keep keyword lookup.

Feed retrieved evidence into `PlatformDesignAssistant` briefs and the existing deterministic validators. Stage batches through the write gate for human approval using new `Propose*` overloads only. Generated platform values remain proposed values with provenance and assumptions.

Acceptance: valid citations; revision-aware invalidation; viewing-side isolation; embedded-instruction fixtures rejected; Track A-tagged sources refused; no `*.db3` in the index.

### Slice 5 (optional): explanation UI and experiment reports

Schedule only after slices 2 and 3 ship.

Explain validated `AdviceFrame` facts. Key the generated text to the envelope's existing invocation key and store it as a host artifact; never write generated text into `SkillEnvelope` or `ReplayProvenance`. Discard responses that arrive after the selection or evidence changes. Preserve the existing stale and unavailable states.

Unity work uses the Unity-MCP stack ([ADR-027](../../architecture/adr-027-unity-editor-mcp-stack.md)) for UI Toolkit wiring and Play Mode checks, with the headless `PlayModeSmokeHarness` proving behavior first. If `:8080` is down, stay headless.

Experiment reports describe observed run differences from `ExperimentSeedGridBatch` output and state that doctrine, ROE, and force-ratio IDs label rows without changing behavior. Use process isolation for parallel batches until static state is audited.

Acceptance: stale-result and hidden-information tests; matching authoritative scores and fingerprints; Unity smoke evidence for UI additions.

## Dependencies

No csproj references Microsoft.Extensions.AI, AI.Evaluation, VectorData, ModelContextProtocol, or ONNX, and there is no `Directory.Packages.props`. The first draft reports stable `11.0.0` for Microsoft.Extensions.AI, .Evaluation, and .Evaluation.Reporting on 2026-11-12; this review did not re-check the registry, so confirm at pin time. [AI package](https://www.nuget.org/packages/Microsoft.Extensions.AI/11.0.0) · [Evaluation package](https://www.nuget.org/packages/Microsoft.Extensions.AI.Evaluation/11.0.0) · [Reporting package](https://www.nuget.org/packages/Microsoft.Extensions.AI.Evaluation.Reporting/11.0.0)

- Pin exact versions in `Ai.Host` and `Ai.LiveEval` only. Check target frameworks, transitive dependencies, and licenses first.
- [`renovate.json`](../../../renovate.json) disables majors and groups minor and patch updates weekly. Add a rule that takes `Microsoft.Extensions.AI*` and the provider SDK out of the weekly group into their own reviewed PR.
- Provider selection is open. Default to the fake client until credentials and an acceptable data boundary exist.

Deferred to measured follow-on experiments, each needing a consumer and a baseline: Runtime Async opt-in, C# 15 unions, local inference, tensor/SIMD work, fine-tuning, a standalone MCP server. A future server exposes the same validated read and proposal capabilities and authorization boundaries as the CLI verbs.

## Risks

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| Upgrade slips; AI work stalls | Medium | Medium | Wave 0 and the host skeleton need no `net11.0` API |
| AI package or DLL leaks into Unity plug-ins | Medium | High | Separate assemblies; architecture test in W0-D |
| Fabricated IDs or numbers shown to the player | High without checks | High | Host copies numbers; cited IDs validated; reject on mismatch |
| Opposing-force information in player advice | Medium | High | Side filter before prompt build; two-sided fixture |
| Instruction text inside evidence or retrieved documents | Medium | Medium | Delimited data; embedded-instruction fixtures in every slice |
| Proprietary-derived data sent to a provider | Low | Critical | Source allowlist; refusal test; firewall doc |
| Nondeterminism reaches replay | Low | Critical | No AI call in tick paths; fingerprint tests with AI on, off, delayed, failing |
| Merge conflicts across ~170 branches | High | Medium | New directories only; coordinator owns shared files |
| Provider cost or latency unacceptable | Medium | Medium | Go/no-go gate; deterministic report always available |
| Slice 3 blocked by undecided ADR-015 | High | Medium | Raise for decision now; slices 1, 2, 4 do not depend on it |

## Open decisions

| Decision | Owner | Needed by |
| --- | --- | --- |
| Is `rollForward: latestMajor` intended? | Owner (upgrade plan) | G0 |
| Accept ADR-030 (host boundary, egress, envelope) | Architecture | End of Wave 0 |
| Decide ADR-015 (labeling of agent-authored content) | Design | Before slice 3 |
| Provider and data-processing terms | Owner | Before first live run |
| Cost ceiling per AAR report | Owner | Slice 2 go/no-go |
| Recall and precision margin for the embedding pilot | Owner | Before slice 4 pilot |
| Raise the test floor to the G0 measurement? | Owner | G0 |

## Verification and handoff

Sequence: G0 and Wave 0 in parallel → slice 1 → slices 2, 3, 4 in parallel → optional slice 5. Use the repository's Graphite workflow, one stack per track.

Reindex GitNexus, run method impacts before edits, and inspect `detect_changes` before commits. Run the full gate script and preserve the Baltic v2 hash and isolated v3 goldens. Keep AI artifacts separately versioned so existing scenario and replay formats do not change.

Report these separately: deterministic validation failures, citation coverage, schema validity, latency, token and cost measurements, human-reviewed usefulness. Live evaluations supplement recorded fixtures and never gate CI. The host and simulation remain usable when providers are unavailable.

This plan links to the [current roadmap snapshot](../../reports/future-sprint-roadmap-09302026.md) and the [scoped dashboard snapshot](../../reports/dashboard-snapshots/2026-11-12-dotnet-11-ai-planning.md). Sprint allocation and delivery dates are pending. Architecture references: [ADR-010](../../architecture/adr-010-headless-first-command-driven-ui.md), [ADR-007](../../architecture/adr-007-c2-map-presentation.md), [ADR-001](../../architecture/adr-001-sim-assembly-boundary.md), [ADR-006](../../architecture/adr-006-data-layer-boundary.md), [ADR-013](../../architecture/adr-013-cmo-scenario-import-policy.md), [ADR-014](../../architecture/adr-014-lua-compatibility-scope.md), [ADR-019](../../architecture/adr-019-agentic-aar-readonly-order-log.md), and the [Unity/C# finish checklist](../../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md).

## Review record

Revision 2, 2026-11-12. Checked against `origin/main` `2ddbd393` and the .NET 11 GA runtime and libraries documentation. Not checked: NuGet package versions, GitNexus counts. Reconciled with revision 2 of the upgrade plan.

Corrected:
- Upgrade is not complete on `origin/main`; added G0 exit criteria and unblocked Wave 0 from it.
- Runtime Async is opt-in at GA, not preview-gated; runtime libraries already use it.
- Process helpers rejected for validation; gate test needs new patterns; `KillOnParentExit` is Windows-only.

Added:
- Existing surfaces: `AiAuthoringServices`, `ProvenanceTag`, `SkillLane.Propose`, MCP tool manifest, no-dynamic-execution gate, plug-in copy script, `OrderLogEntry.SequenceId`.
- Separate AI assemblies with a package-leak architecture test, replacing the in-CLI folder.
- Parallel wave structure with disjoint surfaces and coordinator-owned shared files.
- Egress rule tied to the dual-track firewall; untrusted-input rule for all slices.
- Go/no-go gates for slices 2 and 4; risk register; open-decision table; ADR-030 proposal; ADR-015 blocker; ADR-006, 013, 014, 019, 027 references.
- Live evaluation lane kept out of the solution so CI never needs credentials.

Unchanged in intent: slice order, C# 12 contract, fake-first provider stance, deferred experiments.
