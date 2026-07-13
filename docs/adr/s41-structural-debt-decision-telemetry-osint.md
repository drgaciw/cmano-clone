# ADR S41-03: Structural Debt Characterization — Decision (60%), Telemetry (67%), Osint (68%)

**Status:** Accepted (read-only characterization; refactor deferred)  
**Date:** 2026-06-20  
**Sprint:** 41 (Horizon 3 per roadmap)  
**Authors:** c-sharp-architect (lead), csharpexpert analysis  
**Authority:**  
- `production/sprints/sprint-41-polish-hardening-release-preflight.md` (S41-03)  
- `production/polish-scope-boundary-2026-06-19.md`  
- `docs/reports/future-sprint-roadpmap.md` §Horizon 3, §5 (B3)  
- `production/release-enablement-scope-boundary-2026-06-20.md` §B3  

**Explicit Declaration:** This is a **read-only** analysis and ADR. **ZERO edits** were made to `src/**`, any production code, `DelegationBridge.cs`, or related implementation files. All findings derive from GitNexus tool calls (`impact` run **before** any conclusions), file reads of docs + limited supporting `.cs` (for evidence only), and sequential analysis. No launch artifacts, B2+, or S42+ work performed.

## Context

Per GitNexus index (roadmap baseline @ ~90c9a5f and later snapshots) and module/community metrics:

- `Decision` module: **72 symbols**, **60% cohesion** (lowest in codebase).  
- `Telemetry` module: **63 symbols**, **67% cohesion**.  
- `Osint` module: **48 symbols**, **68% cohesion**.  

Horizon 3 (S41) requires read-only spike to characterize before Track B gate. B3 (S44) will execute refactor using this ADR + GitNexus `rename`/`impact` + replay gates (target: Decision ≥70%, Telemetry ≥72%; Osint audit).

**GitNexus impacts run mandatorily before conclusions (key results):**
- `impact(target: "DecisionLog", direction: "upstream", summaryOnly: true)` → **CRITICAL** risk; 112 direct (d=1) callers/importers; 261 impacted total (d<=3); 4 affected processes (RunTick, RunBatch, Run, RunExecutingTick); 14 affected modules (incl. Decision 11 direct hits, Baltic 77, Projection 55, Orchestration 53).  
- `impact(target: "BalanceTelemetryAccumulator", direction: "upstream", summaryOnly: true)` → **HIGH** risk; 10 direct; 32 impacted; hits Telemetry (24), Import (5), WriteGate (1), Orchestration (indirect).  
- `impact(target: "OsintDigestRunner", direction: "upstream", summaryOnly: true)` → **MEDIUM** risk; 8 direct; 9 impacted; hits Osint (7), Platform (1).  
- Supporting: `impact("Decision")` (ambiguous; folder + usages), `impact("OsintCatalogMapper")` (LOW, 0 upstream direct), cypher for communities (Decision clusters 0.516–0.666 cohesion with 9–15 symbols; similar low for Osint/Telemetry subsets).  
- `context("DecisionLog")`, `context("OsintDigestRunner")`, `context("BalanceTelemetryAccumulator")`, `query("Decision Telemetry Osint cohesion low")`, `cypher` on Community labels/cohesion/symbolCount.  
- `list_repos` confirmed "cmano-clone" (17789 nodes, 35073 edges, 386 communities, 300 processes).

Roadmap explicitly calls S41-03 "Structural-debt spike (read-only first): characterize `Decision` (60%) and `Telemetry` (67%) cohesion problems — produce a refactor ADR, *no code change in Polish*. Tees up Track B." B3 covers Decision + Telemetry refactor + Osint audit.

Previous GitNexus in architecture.md (older snapshot) rated `DecisionLog` LOW; current index shows growth to CRITICAL blast radius.

## Problem

Low cohesion in these clusters produces:
- Maintainability debt: changes to one record/concern type ripple across monolithic or split surfaces.
- High coupling: DecisionLog centralizes; Telemetry crosses Data/Sim + ties to Import/WriteGate; Osint mixes connectors, mappers, runners, and WriteGate/Catalog concerns.
- Test and replay seams are present but brittle due to large surface (many direct tests on impls).
- Violates C#/.NET SOLID in core delegation/data/sim layers:
  - **SRP**: Single types/files responsible for 10–20+ unrelated concerns.
  - **OCP**: Switches on discriminated kinds (e.g., OrderLogEntryKind) for append/format/impact logic.
  - High afferent coupling on key classes; cross-namespace sprawl.

No production symptoms today (replay 6/6, determinism maintained), but debt compounds ahead of B1/B4 scale and release.

## Evidence (GitNexus + Code Structure)

### Symbol / Module Counts (roadmap + cypher)
- Decision: 72 symbols (roadmap); communities e.g. comm_170 (15 sym, 51.6%), comm_169 (12 sym, 53.7%), comm_171 (10 sym, 66.7%).
- Telemetry: 63 symbols (roadmap); low communities e.g. 31.6%–48% clusters + higher ~70%+.
- Osint: 48 symbols (roadmap); communities e.g. 50%–66% subsets.

### Decision Cluster (src/ProjectAegis.Delegation/Decision/* + callers)
- Primary hotspot: `DecisionLog.cs` (implements `IOrderLog`; ~336 LOC).
  - 18+ private `List<T>` fields: `_agentDecisions`, `_policyDenials`, `_engagements`, `_controllerChanges`, `_groupMemberDetaches`, `_magazineChanges`, `_contactChanges`, `_missionTransitions`, `_eventFired`, `_engagementOutcomes`, `_playerOrders`, `_policyUpdates`, `_modeChanges`, `_commsStateChanges`, `_fuelStateChanges`, `_fuelBurns`, `_platformDamageChanges`, `_chronological`, etc.
  - Giant `switch` in `Append(OrderLogEntry)` + dedicated `Append*` overloads + `FormatPayload` switch + `ComputeFingerprint`.
  - `HindsightHook` side-effect + chronological binary-search insert.
- Supporting: `DecisionPipeline.cs` (static choose), ~25 record types + `OrderLogEntry*`, `IOrderLog.cs`, factories, extensions.
- Impact evidence: CRITICAL upstream from Orchestration (`DelegationOrchestrator`), Projections (C2 top bar, losses, contacts, message log), many tests, SimulationSession flows. Affects Baltic, Projection, Orchestration, Bridge (host), Runtime, etc.
- C# smell: God class + discriminated union handled via runtime type switches (instead of visitors/polymorphism or separate appenders per concern). Good: deterministic fingerprint, sequence IDs, implements interface for order log.

### Telemetry Cluster (ProjectAegis.Data.Telemetry/* + ProjectAegis.Sim.Telemetry + cross)
- `BalanceTelemetryAccumulator.cs`: implements `IBalanceTelemetrySink`; SortedDicts for entities/overrides; inner `EntityAccumulator`; `RecordOutcome`, `EvaluateDrift`, hash computation.
- `BalanceTelemetrySinkFactory.cs` (static Create → accumulator or NoOp).
- `CatalogBalanceDriftPipelineEvaluator.cs` (static; ties to diffEntityIds + WriteGate invariant).
- `BalanceDriftAdvisoryConsumer.cs` (Sim.Telemetry): wraps sink, records from engagements (uses Engage outcome codes).
- Also: `ScenarioBalanceTelemetrySettings.cs`, `IBalanceTelemetrySink`, golden hashes, entity kinds, usage in `ScenarioPolicyJsonLoader.ParseBalanceTelemetry`, Import tests/pipelines, CLI.
- Impact: HIGH on accumulator (Telemetry + Import coupling).
- C# smell: Feature-flag + factory + sink abstraction good, but logic duplication (evaluate, record) across Data vs Sim; cross-assembly dependency (Sim depends on Data.Telemetry); accumulator holds mutable state + hash side concerns. "Instrumentation sprawl".

### Osint Cluster (src/ProjectAegis.Data/Osint/* + Connectors/ + callers)
- `OsintCatalogMapper.cs` (static): `ToSensorBinding`, `ToSensorBindings`, TRL/branch/source-fact resolvers (ties to CatalogSensorBinding, TargetDoc 09/10).
- `OsintDigestRunner.cs` (~200 LOC): Run/Partition via gate, JSON read/dedupe, MapProposalsToBindings, RunFromDigestFile, fixture paths. Ties directly to `CatalogJsonImporter`, `WriteGate`.
- `OsintProposalGate.cs` (static Partition).
- Connectors/: `IOsintConnector`, `InMemoryOsintConnector`, `FileOsintConnector`, `RssOsintConnector`.
- `OsintDiscoveryRecord.cs`, `OsintStaging*` (CLI/Unity).
- Impact: MEDIUM on runner (Osint + Platform).
- C# smell: Static mappers + runners mix concerns (I/O, partitioning, catalog mapping, WriteGate proposal flow). Multiple connector impls good for abstraction but surface scattered (no clear facade or DI registration visible at module level). 68% cohesion reflects cross-cutting to Catalog/WriteGate/Platform + tests.

**Supporting GitNexus data (query/context/cypher):** Confirmed Osint/Telemetry/Decision module labels in processes/definitions; many cross-community flows (e.g. OnApproveSelected hitting Osint mapper).

**Files read (read-only):** All mandatory docs + DecisionLog.cs, IOrderLog.cs, DecisionPipeline.cs, Osint* (mapper, runner, gate), Telemetry* (accumulator, factory, sim consumer, pipeline evaluator), related architecture ADRs (003,006, etc.), sprint plans, boundaries, gate checks, reports.

## Recommended Refactor Approaches (for B3 / S44 only; no code now)

Per c-sharp-architect + csharpexpert (SOLID, cohesion, test seams, maintainability):

1. **Decision (target ≥70% cohesion):**
   - Split `DecisionLog` into thin `IOrderLog` impl + pluggable appenders/strategies per record family (or event-sourced timeline with projection builders).
   - Move fingerprint/format to dedicated `OrderLogFingerprinter` + per-kind formatters (eliminate giant switch).
   - Keep `DecisionPipeline` (pure) separate; consider `IDecisionStrategy`.
   - Preserve: deterministic sequence, chronological, replay hash, IOrderLog surface.
   - GitNexus discipline: `impact()` + `rename` on all; zero shared files with Telemetry track; replay gate after each.
   - Test seams: existing Decision/*Tests + ReplayGoldenTests provide coverage.

2. **Telemetry (target ≥72%; zero shared files with Decision):**
   - Consolidate under single `ProjectAegis.Data.Telemetry` (or dedicated assembly?) with clear `IBalanceTelemetrySink` + domain events.
   - Extract per-EntityKind accumulators or visitor; separate catalog-drift pipeline from core win-rate.
   - Keep advisory consumer in Sim but via interface only (decouple settings).
   - Factory remains; add explicit registration seams.
   - Maintain: "never mutates WriteGate", golden hashes, no side effects.

3. **Osint (audit + minimal fixes, 68% → higher):**
   - Introduce thin `IOsintService` facade over mapper + runner.
   - Strategy or registry for connectors (better DI).
   - Keep mapping pure; move WriteGate/Catalog proposal flow to caller (e.g. CLI command or dedicated proposer) to reduce coupling.
   - Audit surface: connectors + discovery + gate + mapper.

**General:**
- Use GitNexus `context`/`impact` before every symbol touch in S44.
- Post-refactor: re-run cypher for cohesion, `detect_changes`, full ReplayGolden 6/6.
- Alternatives considered (not chosen for now): full event sourcing (too broad for B3), keep monolithic (defers debt), per-record separate logs (increases orchestration complexity).

## Consequences

**Positive (after B3):**
- Higher cohesion → easier maintenance, lower blast radius (GitNexus risk down from CRITICAL/HIGH).
- Better SRP/OCP in C# core layers.
- Clearer seams for future scale (B4) and agentic features.
- Input directly to S44 stories (S44-02/03/04).

**Negative / Risks:**
- Refactor of high-impact symbols (DecisionLog especially) requires exhaustive replay + determinism verification.
- Risk of premature refactor during S41 (mitigated: read-only + explicit boundary cites).
- Temporary test churn; must keep baseline ≥ post-S41 count.
- Cross-track file sharing forbidden in S44 per plan.

**Invariants preserved:** Baltic hash, ReplayGolden 6/6, ZERO DelegationBridge, extend-only CatalogWriteGate, GitNexus `impact()` pre-edit.

## References & Citations

- Sprint plan: `production/sprints/sprint-41-polish-hardening-release-preflight.md` (S41-03 must; AC: ADR + GitNexus cites + input to B3).
- Boundary: `production/polish-scope-boundary-2026-06-19.md` (read-only structural-debt; no production refactor; cite required).
- Roadmap: `docs/reports/future-sprint-roadpmap.md` (Horizon 3; B3 §5: "Refactor `Decision` (60%) + `Telemetry` (67%); audit `Osint` (68%)"; "S41 ADR" prerequisite).
- Release boundary: `production/release-enablement-scope-boundary-2026-06-20.md` (B3 details + gates).
- GitNexus: impacts/context/cypher outputs (this session); `docs/engineering/gitnexus-index-health.md`; architecture.md (historical).
- Related ADRs: `docs/architecture/adr-003-order-log-schema.md` (DecisionLog/IOrderLog), adr-006 etc.
- S44 plan: `production/sprints/sprint-44-structural-debt-refactor.md` (depends on this ADR).
- Agentic: `production/agentic/sprint-41-parallel-kickoff-2026-06-20.md`.

**S41-03 ACs Met:**
- [x] ADR published (this file in `docs/adr/`).
- [x] GitNexus cohesion cites + impact evidence.
- [x] Read-only (explicit); no src/production code changes.
- [x] Characterizes Decision/Telemetry/Osint; feeds B3.
- [x] Cites boundary + roadmap.

## Next Recommended Action (for Coordinator)

1. Complete remaining S41 musts: S41-04 determinism audit + GitNexus re-index @ HEAD; S41-05 Polish-exit evidence pack; S41-01/02 baseline + QA plan.
2. Produce scope-expansion decision packet (S41-06) + gate-checks using template.
3. **Block S42 dispatch** until S41 closeout + human gate recorded.
4. After gate: dispatch S44 (B3) with this ADR as governing artifact; pair determinism-engineer on replay.
5. Re-index GitNexus post any merges.

**Risks noted:**
- ADR visibility may tempt early B3 work inside Polish (mit: boundary + "no production code" enforcement).
- GitNexus index staleness (mit: S41-04 mandatory re-index).
- Scope packet incomplete (use template).

---

*Produced per c-sharp-architect + csharpexpert + architecture-decision roles + verification-before-completion + GitNexus safety. All claims backed by tool outputs. Read-only S41-03 complete.*
