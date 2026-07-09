# 07 - Agentic Infrastructure Framework

**Last Updated:** 2026-07-08  
**Related:** [01-Project-Overview.md](01-Project-Overview.md) · [04-Agent-Delegation.md](04-Agent-Delegation.md) · [06-Database-Intelligence.md](06-Database-Intelligence.md) · [08-Agentic-Architecture.md](08-Agentic-Architecture.md) · [11-Agentic-Mission-Editor.md](11-Agentic-Mission-Editor.md) · [17-Replay-AAR-And-Order-Log.md](17-Replay-AAR-And-Order-Log.md)  
**Status:** Locked  
**FR reverse-ref:** [FR-06](01-Project-Overview.md) — Agentic dev infrastructure (product surfaces + process tooling split below)  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md)  
**GDD:** [agentic-infrastructure.md](../../design/gdd/agentic-infrastructure.md)  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) §07 — **Partial** (Release stage)

## Purpose

Provide a comprehensive set of high-level infrastructure agents that automate and enhance the entire game development and simulation lifecycle — scenario authoring, batch analysis, balance, events, performance, and human-supervised operator assistance — aligned with professional wargaming workflows (Monte Carlo, experiment runners, external telemetry).

### Product infrastructure agents (player/analyst-facing)

**INF-1…8** define product-facing capabilities (with phase tags in acceptance rows). Primary product surfaces today:

| Surface | Role | INF anchors |
|---------|------|-------------|
| Headless batch / replay | Reproducible AvA and score CSV (`BalticReplayHarness`, `BalticBatchRunner`) | INF-2, INF-3, INF-5, INF-6 |
| AAR / order-log consumers | Read-only debriefs over log + checkpoints | INF-2 |
| Mission Editor CLI | `scenario_validate` / export / sample / `mission_plan_suggest` | INF-1, INF-4 |
| Operator copilot (supervised) | Evidence-linked COA suggestions; no silent lethal path | INF-7 |
| Catalog intelligence (CLI-first) | Audits and staging proposals via write gate | INF-8 |
| Runtime Hindsight (config-gated) | Optional retain on `DecisionLog`; sim banks `agent-*` / `aar-*` | INF-2, INF-3 |

These surfaces support players, analysts, and headless research workflows. They are the **product** success surface for this document.

### Studio process tooling (not product success criteria)

GitNexus, Superpowers skills, Hindsight **dev** banks (`dev-cmano-clone`, `dev-story-*`, `dev-pr-*`), and the Claude/Cursor session loop are **engineering process** tooling. They improve developer velocity and blast-radius hygiene; they are **not** OV success criteria. See hub doc [01 Agentic Capabilities](01-Project-Overview.md#agentic-capabilities) process note: *“Agentic development workflow quality is a process goal tracked in 07, not a product success criterion.”*

## Vision

A self-improving game infrastructure where specialized AI agents support developers, analysts, and players. Agents generate scenarios, orchestrate reproducible experiments, tune balance, manage events, optimize performance, and act as **supervised copilots** — every recommendation evidence-linked and reversible. Inspired by CMO Professional Edition analysis tooling, implemented as a clean-room, API-first platform.

**Sprint 15 scope (design lock):** Document maturity + RTM traceability for eight infrastructure agents. **P0 shipped slice (Release):** headless batch replay, Mission Editor **CLI** validation (MCP host optional/partial), optional Hindsight retain on `DecisionLog` (config-gated). **P1+:** NL scenario generation, full Monte Carlo orchestration UI, operator copilot in C2, balance auto-sweep workers.

## Functional Requirements

### 1. Scenario Generation Agent

- Automatically creates balanced, historically plausible, or procedurally generated scenarios based on:
  - Selected theater (Baltic, South China Sea, etc.)
  - Force composition (NATO vs. near-peer adversary)
  - Time period and technology level
  - Mission objectives (strike, defense, escort, swarm attack)
- Supports parameter-driven generation (difficulty, entity count, weather, electronic warfare intensity)
- Outputs complete scenario files ready for immediate play or agent-vs-agent testing

**P0 note:** **Assistive authoring** via `mission_plan_suggest` and validation/export **CLI** (doc 11; MCP host optional); full procedural ORBAT generation is **P1**.

**Acceptance**

- [ ] **INF-1.1** Generated scenario package includes `metadata.dbRef` / `dbSnapshotId` resolvable via doc 06 `ICatalogReader.TryResolveDbRef`.
- [ ] **INF-1.2** `scenario_validate` (Mission Editor CLI) returns deterministic `reportHash` for the same input JSON (no wall-clock in hash).
- [ ] **INF-1.3** Parameter overrides (theater, side count, TL flags) are expressed as typed scenario metadata fields, not ad-hoc Lua-only state.
- [ ] **INF-1.4** Exported scenario passes `canExport` gate before `scenario_export_brief` or batch harness bind.
- [ ] **INF-1.5** Post-P0: NL prompt → draft scenario with human review queue (no auto-publish without validate PASS).

### 2. After Action Review (AAR) Agent

- Analyzes completed scenarios and generates detailed, human-readable debriefs including:
  - Kill chains and engagement timelines
  - Key decision points and their outcomes
  - Agent performance metrics (if agents were used)
  - Lessons learned and tactical recommendations
- Supports natural language summaries and visual timeline generation

**P0 note:** Order log + `ReplayGoldenSuite` are canonical truth (doc 17); AAR agents are **read-only** over log projections. NL summary **P1** per Resolved Design Decisions.

**Acceptance**

- [ ] **INF-2.1** AAR inputs are derived only from order log + checkpoint exports — never live `Tick()` mutation.
- [ ] **INF-2.2** Kill-chain reconstruction links `Engagement` entries by shared `engagementId` (doc 17 minimum schema).
- [ ] **INF-2.3** Agent performance section includes per-`agentId` intent counts, policy denials, and override rate from `DecisionLog`.
- [ ] **INF-2.4** `hindsight-aar-analyst` (or equivalent) can `recall` from `aar-{scenario}-{runId}` bank without changing replay fingerprint when Hindsight is off.
- [ ] **INF-2.5** Post-P0: NL executive summary cites `sequenceId` anchors for each claimed decision point.

### 3. Balance Tuning Agent

- Runs thousands of agent-vs-agent simulations in headless mode
- Analyzes win rates, engagement statistics, and cost-effectiveness across weapon systems
- Proposes specific balance adjustments (range, damage, stealth, cost, reload times)
- Maintains a balance dashboard with confidence scores for each change

**P0 note:** Batch CSV export exists (`BalticReplayHarness` / `LossesScoringCsvExporter`); **suggest-only** proposals enter doc 06 staging — no auto-apply (Resolved Design Decisions).

**Acceptance**

- [ ] **INF-3.1** Headless batch runs accept scenario id list + seed grid + tick count and emit CSV rows (`scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint`).
- [ ] **INF-3.2** Same seed + scenario + build produces identical `fingerprint` across two runs (determinism gate: ReplayGolden suite + production hash `17144800277401907079`).
- [ ] **INF-3.3** Balance proposals are structured patch bundles with confidence score — never direct catalog SQL.
- [ ] **INF-3.4** Proposals route through `IWriteGate` / `ApproveBatch` (doc 06); human approval required for any stat change.
- [ ] **INF-3.5** Post-P0: `balance-tuning` Hindsight bank retains experiment matrices and rejected hypotheses (`balance-tuning-memory-agent`).

### 4. Event & Trigger System Agent

- Dynamically manages world events, random occurrences, and mission triggers
- Supports complex conditional logic (e.g., “if 3+ ships detected in zone X, launch drone swarm”)
- Enables emergent gameplay through intelligent event chaining
- Exposes tools for designers to define high-level event rules that the agent expands

**P0 note:** Declarative YAML event model and validation live in doc 11; infrastructure agent **expands** designer rules into versioned event modules — does not bypass scenario load validation.

**Acceptance**

- [ ] **INF-4.1** Agent-expanded events compile to doc 11 declarative schema (`events[]` with `trigger` / `conditions` / `actions`).
- [ ] **INF-4.2** Conditional chains reference scenario variables store (`ScenEdit_SetKeyValue` successor) — no hidden global Lua state.
- [ ] **INF-4.3** Every fired event produces `EventFired` order-log entry (doc 17) with stable `eventId`.
- [ ] **INF-4.4** CLI (primary) can list and diff event modules before export (`scenario_validate` includes event feasibility findings); MCP host optional where registered.
- [ ] **INF-4.5** Post-P0: agent-suggested event chains enter human review queue (same pattern as doc 05 staging).

### 5. Performance Optimization Agent

- Monitors simulation performance in real time (entity count, system load, frame time)
- Suggests architectural improvements for specific scenarios
- Automatically adjusts detail levels for large drone swarms (LOD, sensor update rates, physics fidelity)
- Provides headless performance benchmarks for agent-vs-agent runs

**P0 note:** Headless benchmarks via batch runner; editor **suggest with one-click apply**; headless **auto-LOD for swarm scenarios only** (Resolved Design Decisions).

**Acceptance**

- [x] **INF-5.1** Headless benchmark records entity count, ticks executed, and wall-clock duration per run (CSV or JSON artifact). — `src/ProjectAegis.Sim.Benchmark` (CSV+JSON, unit-tested); see [sim entity-scale benchmark & gap report](../../docs/reports/sim-entity-scale-benchmark-2026-07-08.md). *Note: the 25k@1000× NFR (doc 08 §2) is not yet measurable — the sim has no per-entity per-tick workload; the report derives the required per-entity budget.*
- [ ] **INF-5.2** Swarm scenario policy flag enables automatic LOD / sensor-rate degradation without changing order-log schema.
- [ ] **INF-5.3** Performance suggestions are advisory artifacts — no silent sim-parameter mutation in CI golden paths.
- [ ] **INF-5.4** `Invoke-ManualQaHeadlessGate.ps1` documents baseline PASS thresholds for Baltic fixtures.
- [ ] **INF-5.5** Post-P0: Unity editor panel shows one-click apply for suggested LOD tiers with undo snapshot.

### 6. Experiment & Monte Carlo Agent *(CMO Pro parity)*

Formalizes reproducible batch analysis beyond single-playthrough simulation:

- **Scenario seed control** — deterministic runs (ADR-003, req 17)
- **Parameter sweep definitions** — doctrine, ROE, force ratios, TL gates
- **Batch-run workers** — headless parallel execution (1000×+ compression)
- **Statistical output** — kill chains, sortie effectiveness, loss distributions, mission completion rates
- **Artifact storage** — structured results for AAR and regression baselines

An **experiment orchestration agent** can generate parameter matrices, launch batches, detect anomalies, summarize results, and propose follow-up tests for human analysts.

**Priority:** **P1 for v1.0** (infrastructure stub acceptable); **P0 for pro/analyst workflow** (Phase 5 roadmap).

**Acceptance**

- [ ] **INF-6.1** Experiment definition schema lists swept parameters (doctrine, ROE, seed, force ratio) with Cartesian or Latin-hypercube grid metadata.
- [ ] **INF-6.2** Each run records `scenarioSeed`, `scenarioId`, `buildId`, and order-log fingerprint in artifact manifest.
- [ ] **INF-6.3** Batch worker can execute N runs without Unity Editor (CLI or `dotnet run` demo `--batch`).
- [ ] **INF-6.4** Aggregated output includes per-side win rate, loss distribution, and denial rate columns compatible with doc 17 headless CSV.
- [ ] **INF-6.5** Post-P0: anomaly detection flags outlier fingerprints; follow-up test proposals stored in `dev-cmano-clone` Hindsight bank.

### 7. Operator Copilot *(supervised player assistance)*

Sits above the simulation; does **not** execute hidden black-box actions. Capabilities:

- Course-of-action generation with transparent state evidence
- Doctrine and ROE explanation (req 13)
- Sensor/weapon employment suggestions
- Alert triage and prioritization (req 20)
- Natural-language querying over scenario state

Every recommendation must link to observable sim state and be **reversible** by the player. Integrates with Agent Delegation (doc 04) but does not replace unit-level agents.

**P0 note:** Explain-only overlays and policy citations; no autonomous order execution without player confirm (contrast doc 04 autonomy tiers).

**Acceptance**

- [ ] **INF-7.1** Every copilot suggestion includes evidence pointers (`unitId`, `contactId`, `policySnapshotId`, or `sequenceId`).
- [ ] **INF-7.2** Accepting a suggestion enqueues a normal `PlayerOrder` or assisted intent — same path as doc 04 / 14.
- [ ] **INF-7.3** Copilot cannot bypass `IPolicyEvaluator` / `FireAbortReason` gates (doc 13–14).
- [ ] **INF-7.4** Dismissed suggestions leave no mutation in order log (audit trail shows only explicit player/agent commits).
- [ ] **INF-7.5** Post-P0: NL query over scenario state returns answers grounded in read-only bridge projections (doc 20), not hidden sim APIs.

### 8. Database Research Assistants *(extends doc 06)*

Infrastructure wrappers for the five-stage DB agent pipeline:

- Scheduled OSINT ingestion runs
- Weekly consistency and provenance audit reports
- Batch normalization proposals after speculative system additions (docs 09/10)

All writes route through Database Intelligence Layer; no direct merge.

**P0 note:** `DatabaseIntelligenceOrchestrator` + `catalog_*` surfaces are **CLI-primary** (`ProjectAegis.MissionEditor.Cli` / Data headless entry points). There is no full “Data MCP server” product surface; any MCP wrapping is optional/partial host glue over the same CLI contracts. OSINT scheduling deferred to doc 05.

**Acceptance**

- [ ] **INF-8.1** Scheduled audit invokes `DatabaseIntelligenceOrchestrator.Run` and emits deterministic `ValidationReport` hash on Baltic default catalog.
- [ ] **INF-8.2** All catalog mutations use `IWriteGate` — infrastructure agents have no direct SQLite write path.
- [ ] **INF-8.3** Weekly report includes provenance tier breakdown and open staging batch count.
- [ ] **INF-8.4** Post-speculative-system proposals (docs 09/10) enter staging as sensor/platform batches with `actorType=agent`.
- [ ] **INF-8.5** CLI `catalog_intelligence_run` (or equivalent MissionEditor.Cli command) shares the same orchestrator entry point as headless CI smoke; optional MCP host must call that path, not a shadow write API.

## Non-Functional Requirements

- All **product** agents must be **headless-capable** (run without Unity Editor open)
- Optional Unity-MCP / editor host may wrap the same CLI contracts for live editing when the editor is open — not required for CI or product gates
- Full logging and traceability of all agent decisions and changes
- Support for parallel execution (multiple scenarios or tuning runs simultaneously)
- **Determinism (product gate):** ReplayGolden suite **6/6** (Baltic v2) and production hash **`17144800277401907079`** must stay preserved; no `recall` / `reflect` on simulation hot path (`Tick()`, policy selection) — Hindsight retain-only when enabled (see `src/ProjectAegis.Delegation/Hindsight/README.md`)
- **Studio process:** Symbol edits to `DelegationBridge`, harness, or orchestrator require `gitnexus_impact` per `AGENTS.md` (process hygiene, not a player-facing AC)

## Agentic Capabilities

**Product (player/analyst):** infrastructure capabilities are exposed primarily as **CLI and headless runners**. Phase N / partial MCP bindings may wrap those CLIs for conversational hosts; they are not a separate product surface.

- Target workflows (CLI today; optional MCP where registered):
  - Trigger scenario generation / assistive plan suggest with typed inputs (NL prompts Phase N)
  - Request detailed AARs on specific replays (log + checkpoint inputs)
  - Ask the Balance Tuning Agent for recommendations on specific systems (suggest-only)
  - Define new event rules as declarative modules (doc 11 schema)
  - Monitor performance settings during large-scale headless tests

- Agents can collaborate (e.g., Scenario Generation Agent → Balance Tuning Agent → AAR Agent in a single workflow)

**Studio process (not product ACs):** skill priority for dev sessions — user instructions → GitNexus → Hindsight → `.claude/skills/` → Superpowers globals (`AGENTS.md`).

## Technical Considerations

- Product path: standalone headless executables / `dotnet` CLI (`MissionEditor.Cli`, Baltic harness) first
- Optional Unity Editor tools and Unity-MCP host for interactive authoring when installed
- Uses Unity’s Job System and Burst for high-speed simulation batches where applicable (doc 08 hot paths; post-P0 DOTS)
- Stores results in a structured database (SQLite or similar) for querying and trend analysis
- Designed to work with the Database Intelligence Layer for consistent data (doc 06)

## Future Extensibility

- Cloud-based scenario farm for running 10,000+ simulations in parallel
- Integration with external wargaming tools and military scenario databases
- Reinforcement learning integration for the Balance Tuning Agent
- Multiplayer scenario co-generation with human designers

## Build Phase Alignment *(from agentic research roadmap)*

| Phase | Infrastructure focus |
|-------|---------------------|
| 1 — Sim kernel | Headless runner, deterministic saves |
| 2 — Database platform | DB agents, validation, public intake workflow |
| 3 — Scenario/automation | Scenario gen, import/export, replay, initial batch tools |
| 4 — Agentic layer | Copilot, experiment orchestration, DB research assistants |
| 5 — Pro workflow | Full Monte Carlo management, external connectors, institutional approval flows |

**Current tracker (2026-07-04 / Release stage):** Phase 3 partial — `BalticReplayHarness`, `MissionEditor.Cli` (CLI primary), runtime Hindsight hooks; Phase 4–5 items tracked via INF-6.x / INF-7.x post-P0 rows. Implementation grade: **Partial** ([tracker §07](../implementation-tracker-2026-07-04.md)).

## Cross-Domain Traceability

| Doc | How infrastructure interacts |
|-----|------------------------------|
| [04](04-Agent-Delegation.md) | Unit-level agents execute inside `DelegationOrchestrator`; infrastructure **Operator Copilot** suggests COAs but does not replace controllers. Batch runs drive delegation metrics for balance and AAR. Trust/`TrustSignal` remain emit-only per scenario. |
| [06](06-Database-Intelligence.md) | Balance proposals, normalization audits, and DB Research Assistants **only** commit via `IWriteGate`. Scenario gen binds `dbRef` / snapshot IDs from catalog reader. |
| [08](08-Agentic-Architecture.md) | Automation layer hosts headless CLI / batch workers atop `ProjectAegis.Sim` + `ProjectAegis.Delegation` (optional MCP host). Deterministic stepping (ADR-003/004) + ReplayGolden hash `17144800277401907079` are mandatory for experiment agent. |
| [11](11-Agentic-Mission-Editor.md) | Scenario Generation and Event agents output doc 11 packages; `scenario_validate` / `scenario_simulate_sample` / `mission_plan_suggest` are the P0 **CLI** surface (MCP optional). |
| [17](17-Replay-AAR-And-Order-Log.md) | AAR, batch scoring, and Monte Carlo artifacts consume canonical order log; ReplayGolden / replay-verify hashes (`17144800277401907079`) are regression gates for infrastructure changes. |

## Open Questions / Decisions Needed

All charter questions for agentic infrastructure are **locked** for Sprint 15 design review. See [Resolved Design Decisions](#resolved-design-decisions). No reopen without user approval.

| Former open question | Resolution location |
|---------------------|---------------------|
| Scenario generation priority? | [§1 Scenario generation priority](#1-scenario-generation-priority) |
| AAR detail v1.0? | [§2 AAR detail v1.0](#2-aar-detail-v10) |
| Balance agent autonomy? | [§3 Balance agent autonomy](#3-balance-agent-autonomy) |
| Performance optimization? | [§4 Performance optimization](#4-performance-optimization) |
| Monte Carlo v1.0 scope? | [§5 Experiment / Monte Carlo scope](#5-experiment--monte-carlo-scope) |
| Operator copilot vs delegation? | [§6 Operator copilot boundaries](#6-operator-copilot-boundaries) |

## Implementation Mapping

### Product surfaces (player / analyst)

| Requirement area | Path / type | Notes |
|------------------|-------------|-------|
| Batch replay & CSV scores | `BalticReplayHarness`, `BalticBatchRunner`, `ProjectAegis.Delegation.Demo` (`--batch`), `tools/batch-replay/README.md` | `LossesScoringCsvExporter`; CI: ReplayGolden **6/6**, hash `17144800277401907079` |
| Scenario validate / export / sample sim | `src/ProjectAegis.MissionEditor.Cli`, `tools/mission-editor/Invoke-*.ps1` | **CLI primary** (ADR-008); optional `tools/mission-editor/mcp-tools.json` host bindings |
| Headless QA gate | `tools/unity/Invoke-ManualQaHeadlessGate.ps1` | Play Mode smoke + build gate |
| Catalog intelligence / audits | `DatabaseIntelligenceOrchestrator`, MissionEditor.Cli / Data CLI `catalog_*` | INF-8.x; **not** a standalone Data MCP product; pairs with `tools/cmano-db-crawler/` for import |
| Delegation + order log source | `DelegationOrchestrator`, `DelegationBridge`, `DecisionLog` | AAR read-only; bridge **zero-touch** hotpath through Release v1 |
| Runtime Hindsight (sim) | `src/ProjectAegis.Delegation/Hindsight/`, `HindsightBankIds.cs` | Banks: `agent-*`, `aar-*`, `agent-xp-*`; retain-only on hot path |
| Unity-MCP (editor, optional) | `http://localhost:8080` host + `tools/mission-editor/mcp-tools.json` | Partial/optional; headless CLI preferred in CI and Cloud Agent VM |

### Studio process tooling (not product success criteria)

| Requirement area | Path / type | Notes |
|------------------|-------------|-------|
| Hindsight **dev** CLI | `tools/hindsight/Invoke-Hindsight.ps1`, `tools/hindsight/Test-HindsightServer.ps1` | Dev banks only — not player-facing |
| GitNexus MCP | `gitnexus_impact`, `gitnexus_detect_changes`, `gitnexus://repo/cmano-clone/*` | Required before bridge/harness edits (`AGENTS.md`) — process hygiene |
| Superpowers install | `tools/install-superpowers.ps1` | `brainstorming`, `writing-plans`, `test-driven-development` for infra epics |
| Claude/Cursor session loop | `.claude/skills/`, `.claude/agents/`, `AGENTS.md` | Engineering process; see [01 process note](01-Project-Overview.md#agentic-capabilities) |

#### Hindsight memory banks (split)

| Bank | Track | Use |
|------|-------|-----|
| `dev-cmano-clone` | Studio process | Default repo-wide agent memory (Sprint decisions, experiment outcomes) |
| `dev-story-{slug}` | Studio process | Active production story / epic |
| `dev-pr-{number}` | Studio process | PR / review cycle |
| `balance-tuning` | Studio process + product experiment memory | Trait and balance experiment matrices (`balance-tuning-memory-agent`) |
| `agent-{personality}-{agentId}` | Product (runtime) | Per-agent decision memory (sim runtime, config-gated) |
| `aar-{scenario}-{runId}` | Product (runtime AAR) | Post-run AAR (`hindsight-aar-analyst` over sim banks) |
| `agent-xp-{agentId}` | Product (runtime) | Trust signals at `FinalizeScenario` |

#### Agent skills (`.claude/skills/`) — studio process

| Task | Skill file |
|------|------------|
| GitNexus + Hindsight dev loop | `.claude/skills/hindsight/hindsight-gitnexus/SKILL.md` |
| Retain / recall / reflect | `.claude/skills/hindsight/hindsight-{retain,recall,reflect}/SKILL.md` |
| Dev bank conventions | `.claude/skills/hindsight/hindsight-dev-memory/SKILL.md` |
| Simulation AAR | `.claude/skills/hindsight/hindsight-aar/SKILL.md` |
| Local server setup | `.claude/skills/hindsight/hindsight-local-setup/SKILL.md` + `tools/hindsight/README.md` |
| Hub / bank reference | `.claude/skills/hindsight/hindsight-guide/SKILL.md` |
| Team orchestration | `.claude/skills/team-hindsight-dev/SKILL.md` |
| Replay verification | `.claude/skills/replay-verify/SKILL.md` |
| GitNexus impact / explore | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md`, `gitnexus-exploring/SKILL.md` |

#### Local agents (`.claude/agents/`) — studio process

| Agent | Role |
|-------|------|
| `hindsight-dev-memory-lead` | GitNexus + Hindsight implementation loop |
| `hindsight-aar-analyst` | Post-run AAR over simulation banks |
| `balance-tuning-memory-agent` | Cross-session trait tuning memory |

**Recommended studio dev loop (not a product AC):** `gitnexus://repo/cmano-clone/context` → `gitnexus_impact` → `Invoke-Hindsight.ps1 -Operation recall -BankId dev-cmano-clone` → implement → `retain` with `[OUTCOME:]` → `gitnexus_detect_changes()` before commit.

## Resolved Design Decisions

Decisions locked **2026-06-04** for Sprint 15 design review. May 2026 charter table preserved below.

| Question | Decision |
|----------|----------|
| Scenario generation priority? | **Realism first**, balance second, variety third — with explicit difficulty/variety overrides |
| AAR detail v1.0? | Kill chains, key decision points, agent metrics; NL summary **P1** |
| Balance agent autonomy? | **Suggest only** with confidence scores; auto-apply never without human approval |
| Performance optimization? | **Suggest with one-click apply** in editor; headless auto-LOD for swarm scenarios only |

### 1. Scenario generation priority

**Decision:** **Realism first**, balance second, variety third.

| Override | When | Behavior |
|----------|------|----------|
| `difficulty` | Designer or agent prompt | Scales force ratios and asset sophistication within theater norms |
| `variety` | Batch/regression runs | May relax historical ORBAT for coverage; must tag scenario metadata `varietyMode: true` |
| `balance` | After batch telemetry | Second-pass tuning only via doc 06 staging — never silent stat edits |

Generated packages must pass doc 11 validation before export (INF-1.2, INF-1.4).

### 2. AAR detail v1.0

**Decision:** **Structured first**, narrative second.

| In v1.0 (P0) | Deferred (P1) |
|--------------|---------------|
| Kill chains from order log | NL executive summary |
| Key decision points with `sequenceId` anchors | Visual timeline UI |
| Agent metrics (intents, denials, overrides) | Tacview / 3D replay export |

AAR agents must not contradict doc 17 log entries (INF-2.1).

### 3. Balance agent autonomy

**Decision:** **Suggest only** with confidence scores; **auto-apply never** without human approval.

```
Batch runs → telemetry CSV → proposal bundle → CatalogWriteGate.Propose → human ApproveBatch
```

Aligns with doc 06 §3 balance drift (post-P0 ±8% threshold). Use `balance-tuning` Hindsight bank for cross-session experiment memory, not live trait mutation.

### 4. Performance optimization

**Decision:** **Suggest with one-click apply** in Unity editor; **headless auto-LOD for swarm scenarios only**.

| Context | Auto-apply | Suggest-only |
|---------|------------|--------------|
| Headless batch / CI | Swarm LOD policy only (INF-5.2) | Architecture changes |
| Unity editor | One-click LOD tier apply with undo | Sensor backend swaps |

Golden replay paths disable auto-LOD unless scenario explicitly enables `swarmLodPolicy`.

### 5. Experiment / Monte Carlo scope

**Decision:** **P1 infrastructure stub for v1.0 product**; **P0 for pro/analyst workflow** (Phase 5).

| In stub (now) | Full pro workflow (Phase 5) |
|---------------|----------------------------|
| `--batch` seed grid + CSV | Parameter sweep DSL + artifact store |
| Fingerprint regression | Anomaly detection + follow-up test agent |
| doc 17 shared schema | External connector export |

Experiment definitions must not introduce nondeterministic fields into order log (ADR-003).

### 6. Operator copilot boundaries

**Decision:** Copilot is **supervised** and **evidence-linked**; doc 04 agents remain the execution layer for delegated units.

- Copilot outputs COA cards with citations; player accept → normal order path.
- No `FULL_AUTONOMOUS` lethal engage via copilot without explicit player opt-in (doc 13).
- Alert triage (doc 20) ranks contacts; does not auto-fire weapons.

### 7. P0 scope boundary (explicit deferrals)

| In P0 / partial now | Deferred |
|---------------------|----------|
| `BalticReplayHarness` + batch CSV | Full Monte Carlo orchestration UI |
| Mission Editor validate/simulate **CLI** (MCP optional) | NL scenario generation publish path |
| Optional Hindsight retain on `DecisionLog` | Copilot NL state query (INF-7.5) |
| `DatabaseIntelligenceOrchestrator` audits (CLI) | Scheduled OSINT ingestion (doc 05) |
| `replay-verify` / ReplayGolden suite (hash `17144800277401907079`) | Cloud scenario farm |
| Partial optional MCP host bindings (`tools/mission-editor/mcp-tools.json`) | Unity editor perf one-click panel (INF-5.5) |

---

## Traceability

| Epic / FR | This document |
|-----------|---------------|
| **FR-06** ([01](01-Project-Overview.md)) | Agentic infrastructure — product INF-1…8 + studio process tooling (not OV success criteria) |
| Doc 04 Delegation | Unit agents vs Operator Copilot §7 |
| Doc 06 Database | DB Research Assistants §8, balance staging §3 |
| Doc 08 Architecture | Automation layer, headless CLI / batch API |
| Doc 11 Mission Editor | Scenario gen + events §1, §4 |
| Doc 17 Replay/AAR | Order log truth, batch CSV, ReplayGolden hash, experiment artifacts §2, §6 |
| GDD | [agentic-infrastructure.md](../../design/gdd/agentic-infrastructure.md) |
| Tracker | [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) §07 — **Partial** (Release) |

---

**Status:** Locked (Sprint 15)  
**Tracker row 07:** **Partial** — [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md)  
**Implementation grade:** Partial — Design Status remains **Locked**. Charter re-honesty: Wave 1 2026-07-08.