# Scenario Editor Requirements Improvement Plan

**Date:** 2026-07-11
**Scope:** Review and improve the requirements corpus around `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`.
**Method:** Direct review of the current requirements file, related GDD/review/gate artifacts, implementation tracker rows, traceability tables, source/test evidence searches, and GitNexus pre-checks.

## Executive Summary

The previous 2026-07-01 plan in this file is now stale. Requirement doc 11 has already absorbed most of the original recommendations: it has `AME-*` IDs, testable AC-1...AC-12, implementation mapping, ADR links, ferry/support/undo/event/sides/timeline/diff updates, and explicit headless-vs-Unity honesty.

The remaining improvement work is not another large rewrite of doc 11 from scratch. It is a requirements-corpus cleanup:

1. Make doc 11 easier to audit by separating normative requirements from delivery history.
2. Remove cross-document contradictions between doc 11, the GDD, index, trackers, and traceability tables.
3. Normalize status language so `Shipped`, `Partial+`, `Residual`, `Phase 2.4+`, and `P0/P1/P2` mean one thing everywhere.
4. Refresh open decision handling for ADR-013, ADR-015, and ADR-017 now that scenario editor and Mission Editor Phase 2 are closed.
5. Preserve CMO parity and implementation evidence without overclaiming Unity GUI, visual graph, mining/cargo, layers/minimap, reversible migration, or Phase 3 agents.

## Evidence Reviewed

### Current requirements and design docs

- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`
- `Game-Requirements/requirements/21-Platform-Editor.md`
- `Game-Requirements/Game-Requirements-Index.md`
- `Game-Requirements/implementation-tracker-2026-07-01.md`
- `Game-Requirements/implementation-tracker-2026-07-04.md`
- `Game-Requirements/cmo-manual-traceability.md`
- `Game-Requirements/research-traceability.md`
- `design/gdd/agentic-mission-editor.md`
- `design/gdd/reviews/agentic-mission-editor-review-log.md`
- `docs/research/scenario-editor-research.md`

### Completion and governance artifacts

- `docs/superpowers/plans/2026-07-08-scenario-editor-completion-plan.md`
- `production/qa/mission-editor-phase2-gate-2026-07-09.md`
- `production/agentic/post-editor-status-truth-2026-07-09.md`
- `production/post-editor-hygiene-scope-boundary-2026-07-09.md`
- Scenario editor S81-S88 and ME Phase 2 gate/QA artifacts under `production/`

### Source and test evidence spot-checks

- `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs`
- `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentDto.cs`
- `src/ProjectAegis.Data/Scenario/Authoring/ScenarioMetadataDto.cs`
- `src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs`
- `src/ProjectAegis.Data/Validation/Rules/ValidationRules.cs`
- `src/ProjectAegis.MissionEditor.Cli/Program.cs`
- `tools/mission-editor/mcp-tools.json`
- `data/scenarios/scenario-document.schema.json`
- `src/ProjectAegis.Data.Tests/Scenario/`
- `src/ProjectAegis.Data.Tests/Validation/`
- `src/ProjectAegis.MissionEditor.Cli.Tests/`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/`

### GitNexus pre-check

- `list_repos` identified canonical repo `/home/username01/cmano-clone` on branch `stack/post-editor/s93-asset-production`.
- `detect_changes(scope=all)` before this plan edit reported unrelated docs/dashboard changes only, risk `low`, affected execution flows `0`.
- `impact(ScenarioDocumentEditor, upstream, summaryOnly)` returned **CRITICAL**, 233 impacted symbols, 20 processes affected. No code edits are planned here.
- `impact(ScenarioValidationEngine, upstream, summaryOnly)` returned **CRITICAL**, 53 impacted symbols, 3 processes affected. No code edits are planned here.
- `impact(ValidationRules, upstream, summaryOnly)` returned LOW. No code edits are planned here.

## Current State

### What doc 11 now does well

| Area | Current state |
|---|---|
| Requirement IDs | `AME-*` IDs are present across the major functional areas. |
| Acceptance criteria | AC-1...AC-12 are mechanically testable and currently checked with co-located test evidence. |
| Implementation mapping | Doc 11 has a large headless implementation mapping table. |
| Determinism | Validation Engine, `editVersion`, world-state hash, `fire_order`, save-vs-export, and no-LLM blocking path are explicit. |
| Honesty | The doc distinguishes v1 headless reality from Unity GUI residuals and Phase 3 agents. |
| Tool surface | Ferry/support/undo/event/sides/timeline/diff tool surfaces are represented. |
| ADR links | ADR-008 and ADR-013...017 are linked. |
| CMO parity | Mission editor, scenario editor, ScenEdit, mission archetypes, and event parity are mapped. |

### What remains weak

| Gap | Problem | Evidence |
|---|---|---|
| G1 | Cross-document status drift | `Game-Requirements-Index.md` and `cmo-manual-traceability.md` still describe req 11 as revised 2026-07-01, while doc 11 says 2026-07-09 and completion artifacts say scenario editor / ME Phase 2 are complete. |
| G2 | GDD contradicts doc 11 | `design/gdd/agentic-mission-editor.md` still says Draft, says v1 ships map-first placement, and describes `*.aegis-scenario` ZIP as the canonical file, while doc 11 now says v1 is headless/file-based and current schema is JSON. |
| G3 | Status vocabulary is overloaded | `P0/P1/P2`, `v1`, `Phase 2`, `P2.1`, `ME-W*`, `Phase 2.4+`, `Partial+`, `Residual`, and `Shipped headless` appear together without a compact glossary. |
| G4 | Normative requirements and closeout history are mixed | Doc 11 includes long maturity notes and wave closeout text inside requirement bodies. This is useful history but makes the requirements harder to audit. |
| G5 | ADR decision state is stale | ADR-013, ADR-015, and ADR-017 remain Proposed even after editor programs closed. The doc does not state whether these are intentionally still pending or simply not reviewed. |
| G6 | CMO traceability can overread as full product parity | `cmo-manual-traceability.md` marks scenario/mission editor rows Full, while doc 11 still has explicit Unity map GUI, visual graph, mining/cargo, layers/minimap, and reversible migration residuals. |
| G7 | Implementation tracker split is confusing | `implementation-tracker-2026-07-01.md` is stale by design; `implementation-tracker-2026-07-04.md` is closer but still older than 2026-07-09 post-editor closure. The active tracker source is not obvious. |
| G8 | Requirement evidence can rot silently | AC evidence paths are good today, but there is no compact "req 11 evidence manifest" that can be refreshed without rereading the whole doc. |
| G9 | Phase 3 agent scope is underspecified | Mission Planner, Red Force, Briefing Writer, Balance, and Migration agents are listed as advisory, but acceptance gates, provenance requirements, and data contracts are not decomposed enough for implementation. |
| G10 | Deferred product UI requirements lack exit gates | Unity map host, Mission Board window, Gantt UI, visual event graph, layers/minimap, mining/cargo, and reversible migration are named residuals but do not all have crisp "done means" checks. |

## Improvement Plan

### Phase 0 - Do not regress current truth

**Goal:** Preserve the current doc-11 improvements while making the corpus easier to maintain.

1. Treat `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` as the current authority for req 11 unless contradicted by newer gate artifacts.
2. Do not reintroduce the stale 2026-07-01 claims:
   - Ferry verbs missing.
   - Undo not wired.
   - ACs unchecked.
   - AC-7 not full projection.
   - AC-8 not met.
   - Map authoring "not started" as a blanket claim.
3. Preserve these explicit non-ship boundaries:
   - Unity full map-first Edit Mode GUI remains residual.
   - Unity Mission Board window / product chrome remains residual.
   - Gantt UI remains residual.
   - Layers/minimap remains deferred.
   - Mining/cargo archetypes remain deferred.
   - Reversible DB migration persistence remains partial.
   - Phase 3 LLM agents are not started.

**Deliverable:** A short "current truth" preamble in any req-11 cleanup PR citing:

- `production/qa/mission-editor-phase2-gate-2026-07-09.md`
- `production/agentic/post-editor-status-truth-2026-07-09.md`
- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`

### Phase 1 - Cross-document status synchronization

**Goal:** Make the top-level corpus point at the same req-11 state.

| File | Required update |
|---|---|
| `Game-Requirements/Game-Requirements-Index.md` | Change the doc-11 row from "Revised 2026-07-01" to "Revised 2026-07-09; implementation-aligned; AC-1...AC-12 green headless; ME Phase 2 complete; residual UI/Phase 3 scoped." |
| `Game-Requirements/cmo-manual-traceability.md` | Update req-11 row date/status and add a note that "Full" means documented parity requirements and headless/editor-core coverage, not full Unity GUI parity. |
| `Game-Requirements/research-traceability.md` | Add explicit links from scenario editor research themes to AME-10.1...10.5 and residual gates. |
| `Game-Requirements/implementation-tracker-2026-07-04.md` or a new dated tracker | Refresh row 11 to reflect post-2026-07-09 closure: scenario editor complete on trunk, Mission Editor Phase 2 complete, Phase 2.4+/Phase 3 residuals only. |
| `Game-Requirements/implementation-tracker-2026-07-01.md` | Add a supersession note pointing readers to the newer tracker instead of editing old historical rows deeply. |

**Acceptance checks:**

- `rg -n "11-Agentic|Agentic Mission|Scenario editor|2026-07-01" Game-Requirements` no longer leaves stale active-status references without a supersession note.
- Index, tracker, doc 11, and post-editor status truth all agree on Release stage and completed editor programs.

### Phase 2 - Split normative requirements from delivery history

**Goal:** Keep doc 11 authoritative but easier to audit.

Doc 11 currently combines:

- Product requirements.
- Implementation mapping.
- Sprint/wave closeout notes.
- Maturity and residual notes.
- AC evidence.

That made sense during the completion push, but it is now heavy for a requirements document. Improve it by restructuring, not deleting evidence.

1. Add a compact `Status Vocabulary` section near the top:

   | Term | Meaning |
   |---|---|
   | P0 | v1 / blocking requirement |
   | P1 | Phase 2 product requirement |
   | P2 | Phase 3 advisory / agentic requirement |
   | Shipped | Implemented and tested for the stated surface |
   | Partial+ | Useful implemented subset with explicit residuals |
   | Residual | Known unbuilt product surface |
   | Deferred | Intentionally out of current train |

2. Convert long inline maturity notes into a single `Requirement Status Matrix`:

   `AME ID | Priority | Phase | Status | Evidence | Residual`.

3. Move historical wave notes to an appendix:

   - SE-W1 honesty.
   - P2.1 ME-W0-c.
   - ME-W2 event graph.
   - ME-W3 sides/timeline/diff.

4. Keep each functional requirement body concise:

   - Requirement statement.
   - Priority.
   - Normative behavior.
   - Link to status matrix row.

**Acceptance checks:**

- Doc 11 still contains every `AME-*` ID.
- AC-1...AC-12 evidence remains cited.
- A reader can answer "what is required?" without reading sprint history.
- A reviewer can answer "what shipped?" from one matrix.

### Phase 3 - Resolve GDD/doc-11 contradictions

**Goal:** Ensure the GDD does not mislead future implementers.

The GDD still reads as pre-implementation design review material and conflicts with the current requirements doc. Choose one of two approaches:

#### Recommended approach: convert the GDD to a historical design source

1. Change the GDD status header from `Draft (for design review)` to something like:
   `Historical design source; superseded for implementation truth by req 11 as of 2026-07-09`.
2. Add a banner:
   - `*.aegis-scenario` ZIP package is a future/package direction, not current v1 truth.
   - v1 did not ship full map-first Unity GUI; headless ORBAT/RP mutations shipped Partial+.
   - Acceptance criteria evidence moved to doc 11.
3. Link to doc 11 and the 2026-07-09 gate as current authority.

#### Alternative approach: fully revise the GDD

Rewrite:

- Section 1 v1 scope.
- Section 2 v1 fantasy.
- Section 3.2 canonical file.
- Section 3.8 map interaction contract.
- Section 8 AC evidence paths.

This produces cleaner docs but is larger and risks duplicating doc 11.

**Acceptance checks:**

- `rg -n "\\*\\.aegis-scenario|Draft \\(for design review\\)|map-first|v1 ships" design/gdd/agentic-mission-editor.md` returns only text that is explicitly labeled historical or future-scope.
- GDD and doc 11 no longer disagree about current v1 file format or GUI scope.

### Phase 4 - Refresh ADR decision state

**Goal:** Prevent "Proposed" ADRs from becoming permanent ambiguity.

| ADR | Current state | Recommended action |
|---|---|---|
| ADR-013 CMO Scenario Import Policy | Proposed | Keep Proposed if legal/licensing review is still pending, but add owner and review date. |
| ADR-015 Agent-Authored Scenario Transparency | Proposed | Decide before Phase 3 agents. Recommended: Accept labeling/provenance requirement. |
| ADR-017 Editor Topology | Proposed | Decide whether Scenario Lab is still a product direction after headless/editor programs closed. If not ready, add explicit decision deadline. |
| ADR-014 Lua Compatibility Scope | Accepted | No change. Keep "typed DSL v1, Lua deferred" in doc 11. |
| ADR-016 Event-Graph Complexity Caps | Accepted | No change. Ensure thresholds are tied to validation config. |

**Acceptance checks:**

- Doc 11 Open Questions table has owner/date for every Proposed ADR.
- No ADR is marked Accepted without user/architect decision.
- Phase 3 requirements cite the decision they depend on.

### Phase 5 - Add a req-11 evidence manifest

**Goal:** Make future audits cheaper.

Create a compact evidence manifest, either as a new file or appendix:

`Game-Requirements/evidence/req-11-scenario-editor-evidence-2026-07-11.md`

Suggested fields:

| Evidence item | Path | Proves |
|---|---|---|
| AC-1 strike reachability | `ScenarioValidationEngineTests.cs` | `STRIKE_UNREACHABLE` and message format |
| AC-2 sample determinism | `ScenarioSimulateSampleCliTests.cs` | `fire_order`, hash, seed output, parallel isolation |
| AC-3 six validation rules | `ValidationGoldenTests.cs` + per-rule tests | Codes for six v1 rules |
| AC-4 doctrine inheritance | `DoctrineInheritanceValidateTests.cs` | resolved ROE inheritance |
| AC-5 pipeline | `SampleCompletePipelineTests.cs` | Strike+Patrol+Support+Ferry headless sample |
| AC-6 deterministic serialization | `tools/ci/smoke-ac6.sh` | byte-stability and editVersion diff honesty |
| AC-7 debugger projection | `EventDebuggerTests.cs` + `StubScopePinTests.cs` | full event trace shape |
| AC-8 Unity host load | `PlayModeSmokeHarnessTests.cs` + evidence markdown | host load + editorState defaults |
| AC-9 derived-only invariant | `DerivedOnlyInvariantTests.cs` | no validation/sim use of editorState |
| AC-10 editVersion conflict | `ScenarioEditVersionGuardTests.cs` + CLI tests | `CONFLICT` with version/hash |
| AC-11 TeleportUnit export | `TeleportUnitExportTests.cs` | logged transform |
| AC-12 save-vs-export | `SaveVsExportGateTests.cs` | save allowed, export/sample blocked |

**Acceptance checks:**

- Every checked AC in doc 11 points to the manifest or the exact test path.
- Every test path in the manifest exists.
- The manifest states the last gate run used as evidence.

### Phase 6 - Decompose residual product requirements

**Goal:** Turn residual labels into implementable future work.

Add or refine "done means" gates for these residuals:

| Residual | Current issue | Requirement improvement |
|---|---|---|
| Unity map GUI host | Named residual, but broad | Define minimum EditorWindow/host load, selection, commit-on-gesture-end, invalid overlay, and screenshot evidence. |
| Mission Board product window | Headless APIs shipped | Define UI acceptance: filter by side/type/status, clone/template UX, keyboard navigation. |
| Gantt/timeline UI | Headless timeline shipped | Define render, drag/edit, validation, and scrub-preview gates. |
| Visual event graph | Static analyzer/debugger shipped | Define graph nodes/edges, dead/circular highlighting, trace drill-down, and no second event store. |
| Layers/minimap | Deferred | Define layer inventory, LOD behavior, minimap bounds, and performance thresholds. |
| Mining/cargo archetypes | Deferred | Define mission DTO shape, validation formulas, CLI/MCP verbs, and sample fixtures. |
| Reversible migration persistence | Partial | Define persisted snapshot/rollback format, manifest evidence, and failure recovery. |
| Phase 3 agents | Not started | Define proposal diff contract, provenance fields, transparency labeling, approval policy, and no blocking-path guarantee. |

**Acceptance checks:**

- Each residual maps to at least one `AME-*` ID.
- Each residual has an acceptance test type: unit, CLI, PlayMode proxy, manual screenshot evidence, or governance gate.
- Residuals are not described as shipped unless the evidence exists.

### Phase 7 - Verification workflow for the requirements cleanup

Run after implementing the doc updates:

```bash
# No duplicate AME IDs in doc 11
rg -o 'AME-[0-9]+\\.[0-9]+' Game-Requirements/requirements/11-Agentic-Mission-Editor.md | sort | uniq -d

# Stale status/date references
rg -n '2026-07-01|ferry.*missing|AC-7.*stub|AC-8.*unchecked|map.*not started' \
  Game-Requirements design/gdd docs/research production

# Requirement evidence paths still exist
rg -n 'src/ProjectAegis|data/scenarios|tools/ci' \
  Game-Requirements/requirements/11-Agentic-Mission-Editor.md \
  Game-Requirements/evidence/req-11-scenario-editor-evidence-*.md

# Markdown sanity
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```

For docs-only changes, full build/test may be optional during drafting, but required before any Graphite submit or completion claim per project rules.

## Recommended Work Order

1. **Phase 1 first:** update index/tracker/traceability status drift. This is low risk and prevents readers from landing on stale state.
2. **Phase 3 next:** add a GDD supersession banner. This removes the largest contradiction with minimal rewriting.
3. **Phase 2 next:** restructure doc 11 into normative text + status matrix + history appendix.
4. **Phase 5 next:** add the evidence manifest to make AC maintenance cheap.
5. **Phase 4 and 6 last:** resolve ADRs and decompose residuals when the next product train is scoped.

## Risks

- **Over-editing doc 11:** The doc is currently evidence-rich. Do not "simplify" by deleting important acceptance evidence.
- **False product parity:** CMO parity rows must not imply full Unity GUI parity when the current truth is headless/Partial+.
- **ADR authority:** Do not flip Proposed ADRs to Accepted without an explicit decision.
- **Historical trackers:** Older trackers should usually get supersession notes, not large retroactive rewrites.
- **Code blast radius:** `ScenarioDocumentEditor` and `ScenarioValidationEngine` are CRITICAL by GitNexus impact. This plan is intentionally docs-only unless a future implementation story is explicitly scoped.

## Definition of Done

- [ ] `scenario-editor-fable-plan.md` reflects the 2026-07-09+ current state, not the stale 2026-07-01 gap list.
- [ ] Index, tracker, and traceability docs agree with doc 11 and post-editor status truth.
- [ ] GDD is clearly marked historical/superseded or revised to match doc 11.
- [ ] Doc 11 has a compact status vocabulary and status matrix.
- [ ] Checked ACs cite existing evidence paths, ideally through a req-11 evidence manifest.
- [ ] Proposed ADRs have explicit owner/date/status.
- [ ] Residual product UI and Phase 3 agent requirements have implementable acceptance gates.
- [ ] Verification commands/searches above have been run and read before claiming completion.
