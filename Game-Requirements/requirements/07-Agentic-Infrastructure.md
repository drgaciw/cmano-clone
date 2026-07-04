# 07 - Agentic Infrastructure Framework

**Last Updated:** 2026-06-04  
**Related:** [04-Agent-Delegation.md](04-Agent-Delegation.md) · [06-Database-Intelligence.md](06-Database-Intelligence.md) · [08-Agentic-Architecture.md](08-Agentic-Architecture.md) · [11-Agentic-Mission-Editor.md](11-Agentic-Mission-Editor.md) · [17-Replay-AAR-And-Order-Log.md](17-Replay-AAR-And-Order-Log.md)  
**Status:** Locked  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md)  
**GDD:** [agentic-infrastructure.md](../../design/gdd/agentic-infrastructure.md)

## Purpose

Provide a comprehensive set of high-level infrastructure agents that automate and enhance the entire game development and simulation lifecycle — scenario authoring, batch analysis, balance, events, performance, and human-supervised operator assistance — aligned with professional wargaming workflows (Monte Carlo, experiment runners, external telemetry).

## Vision

A self-improving game infrastructure where specialized AI agents support developers, analysts, and players. Agents generate scenarios, orchestrate reproducible experiments, tune balance, manage events, optimize performance, and act as **supervised copilots** — every recommendation evidence-linked and reversible. Inspired by CMO Professional Edition analysis tooling, implemented as a clean-room, API-first platform.

**Sprint 15 scope:** Document maturity + RTM traceability for eight infrastructure agents. **P0 shipped slice:** headless batch replay, mission-editor MCP/CLI validation, optional Hindsight retain on `DecisionLog` (config-gated). **P1+:** NL scenario generation, full Monte Carlo orchestration UI, operator copilot in C2, balance auto-sweep workers.

## Functional Requirements

### 1. Scenario Generation Agent

- Automatically creates balanced, historically plausible, or procedurally generated scenarios based on:
  - Selected theater (Baltic, South China Sea, etc.)
  - Force composition (NATO vs. near-peer adversary)
  - Time period and technology level
  - Mission objectives (strike, defense, escort, swarm attack)
- Supports parameter-driven generation (difficulty, entity count, weather, electronic warfare intensity)
- Outputs complete scenario files ready for immediate play or agent-vs-agent testing

**P0 note:** **Assistive authoring** via `mission_plan_suggest` and validation/export CLI (doc 11); full procedural ORBAT generation is **P1**.

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
- [ ] **INF-3.2** Same seed + scenario + build produces identical `fingerprint` across two runs (determinism gate).
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
- [ ] **INF-4.4** MCP/CLI can list and diff event modules before export (`scenario_validate` includes event feasibility findings).
- [ ] **INF-4.5** Post-P0: agent-suggested event chains enter human review queue (same pattern as doc 05 staging).

### 5. Performance Optimization Agent

- Monitors simulation performance in real time (entity count, system load, frame time)
- Suggests architectural improvements for specific scenarios
- Automatically adjusts detail levels for large drone swarms (LOD, sensor update rates, physics fidelity)
- Provides headless performance benchmarks for agent-vs-agent runs

**P0 note:** Headless benchmarks via batch runner; editor **suggest with one-click apply**; headless **auto-LOD for swarm scenarios only** (Resolved Design Decisions).

**Acceptance**

- [ ] **INF-5.1** Headless benchmark records entity count, ticks executed, and wall-clock duration per run (CSV or JSON artifact).
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

**P0 note:** `DatabaseIntelligenceOrchestrator` + `catalog_*` MCP/CLI; OSINT scheduling deferred to doc 05.

**Acceptance**

- [ ] **INF-8.1** Scheduled audit invokes `DatabaseIntelligenceOrchestrator.Run` and emits deterministic `ValidationReport` hash on Baltic default catalog.
- [ ] **INF-8.2** All catalog mutations use `IWriteGate` — infrastructure agents have no direct SQLite write path.
- [ ] **INF-8.3** Weekly report includes provenance tier breakdown and open staging batch count.
- [ ] **INF-8.4** Post-speculative-system proposals (docs 09/10) enter staging as sensor/platform batches with `actorType=agent`.
- [ ] **INF-8.5** MCP `catalog_intelligence_run` (or CLI equivalent) shares the same orchestrator entry point as headless CI smoke.

## Non-Functional Requirements

- All agents must be **headless-capable** (run without Unity Editor open)
- Agents must integrate with Unity-MCP for live editing when the editor is open
- Full logging and traceability of all agent decisions and changes
- Support for parallel execution (multiple scenarios or tuning runs simultaneously)
- **Determinism:** No `recall` / `reflect` on simulation hot path (`Tick()`, policy selection) — Hindsight retain-only when enabled (see `src/ProjectAegis.Delegation/Hindsight/README.md`)
- **GitNexus:** Symbol edits to `DelegationBridge`, harness, or orchestrator require `gitnexus_impact` per `AGENTS.md`

## Agentic Capabilities

- Every infrastructure agent exposes **MCP tools** so Claude/Cursor can:
  - Trigger scenario generation with natural language prompts
  - Request detailed AARs on specific replays
  - Ask the Balance Tuning Agent for recommendations on specific systems
  - Define new event rules conversationally
  - Monitor and adjust performance settings during large-scale tests

- Agents can collaborate (e.g., Scenario Generation Agent → Balance Tuning Agent → AAR Agent in a single workflow)

**Skill priority (dev sessions):** user instructions → GitNexus → Hindsight → `.claude/skills/` → Superpowers globals (`AGENTS.md`).

## Technical Considerations

- Built as Unity Editor tools + standalone headless executables
- Uses Unity’s Job System and Burst for high-speed simulation batches (doc 08 hot paths)
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

**Current tracker (2026-06-04):** Phase 3 partial — `BalticReplayHarness`, `MissionEditor.Cli`, Hindsight hooks; Phase 4–5 items tracked via INF-6.x / INF-7.x post-P0 rows.

## Cross-Domain Traceability

| Doc | How infrastructure interacts |
|-----|------------------------------|
| [04](04-Agent-Delegation.md) | Unit-level agents execute inside `DelegationOrchestrator`; infrastructure **Operator Copilot** suggests COAs but does not replace controllers. Batch runs drive delegation metrics for balance and AAR. Trust/`TrustSignal` remain emit-only per scenario. |
| [06](06-Database-Intelligence.md) | Balance proposals, normalization audits, and DB Research Assistants **only** commit via `IWriteGate`. Scenario gen binds `dbRef` / snapshot IDs from catalog reader. |
| [08](08-Agentic-Architecture.md) | Automation layer hosts MCP tools, headless API, and batch workers atop `ProjectAegis.Sim` + `ProjectAegis.Delegation`. Deterministic stepping (ADR-003/004) is mandatory for experiment agent. |
| [11](11-Agentic-Mission-Editor.md) | Scenario Generation and Event agents output doc 11 packages; `scenario_validate` / `scenario_simulate_sample` / `mission_plan_suggest` are the P0 MCP/CLI surface. |
| [17](17-Replay-AAR-And-Order-Log.md) | AAR, batch scoring, and Monte Carlo artifacts consume canonical order log; replay-verify golden hashes are regression gates for infrastructure changes. |

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

## Implementation Mapping (headless + agent tooling)

| Requirement area | Path / type | Notes |
|------------------|-------------|-------|
| Batch replay & CSV scores | `BalticReplayHarness`, `ProjectAegis.Delegation.Demo` (`--batch`), `tools/batch-replay/README.md` | `LossesScoringCsvExporter`; CI: `BalticBatchRunnerTests` |
| Scenario validate / export / sample sim | `src/ProjectAegis.MissionEditor.Cli`, `tools/mission-editor/Invoke-*.ps1`, `tools/mission-editor/mcp-tools.json` | ADR-008; Unity-MCP registers same contract |
| Headless QA gate | `tools/unity/Invoke-ManualQaHeadlessGate.ps1` | Play Mode smoke + build gate |
| Catalog intelligence / audits | `DatabaseIntelligenceOrchestrator`, `ProjectAegis.Data` MCP `catalog_*` | INF-8.x; pairs with `tools/cmano-db-crawler/` for import |
| Delegation + order log source | `DelegationOrchestrator`, `DelegationBridge`, `DecisionLog` | AAR read-only; **GitNexus CRITICAL** on bridge |
| Hindsight dev CLI | `tools/hindsight/Invoke-Hindsight.ps1`, `tools/hindsight/Test-HindsightServer.ps1` | Banks: `dev-cmano-clone`, `dev-story-{slug}`, `dev-pr-{n}`, `balance-tuning` |
| Hindsight sim integration | `src/ProjectAegis.Delegation/Hindsight/`, `HindsightBankIds.cs` | Runtime: `agent-*`, `aar-*`, `agent-xp-*`; retain-only on hot path |
| GitNexus MCP | `gitnexus_impact`, `gitnexus_detect_changes`, `gitnexus://repo/cmano-clone/*` | Required before bridge/harness edits (`AGENTS.md`) |
| Unity-MCP (editor) | `http://localhost:8080` host + `tools/mission-editor/mcp-tools.json` | Optional for CI; headless path preferred in Cloud Agent VM |
| Superpowers install | `tools/install-superpowers.ps1` | `brainstorming`, `writing-plans`, `test-driven-development` for infra epics |

### Hindsight memory banks (AGENTS.md)

| Bank | Use |
|------|-----|
| `dev-cmano-clone` | Default repo-wide agent memory (Sprint decisions, experiment outcomes) |
| `dev-story-{slug}` | Active production story / epic |
| `dev-pr-{number}` | PR / review cycle |
| `balance-tuning` | Trait and balance experiment matrices (`balance-tuning-memory-agent`) |
| `agent-{personality}-{agentId}` | Per-agent decision memory (sim runtime, config-gated) |
| `aar-{scenario}-{runId}` | Post-run AAR (`hindsight-aar-analyst`) |
| `agent-xp-{agentId}` | Trust signals at `FinalizeScenario` |

### Agent skills (`.claude/skills/`)

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

### Local agents (`.claude/agents/`)

| Agent | Role |
|-------|------|
| `hindsight-dev-memory-lead` | GitNexus + Hindsight implementation loop |
| `hindsight-aar-analyst` | Post-run AAR over simulation banks |
| `balance-tuning-memory-agent` | Cross-session trait tuning memory |

**Recommended dev loop:** `gitnexus://repo/cmano-clone/context` → `gitnexus_impact` → `Invoke-Hindsight.ps1 -Operation recall -BankId dev-cmano-clone` → implement → `retain` with `[OUTCOME:]` → `gitnexus_detect_changes()` before commit.

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
| Mission Editor validate/simulate MCP | NL scenario generation publish path |
| Optional Hindsight retain on `DecisionLog` | Copilot NL state query (INF-7.5) |
| `DatabaseIntelligenceOrchestrator` audits | Scheduled OSINT ingestion (doc 05) |
| `replay-verify` golden suite | Cloud scenario farm |
| `tools/mission-editor` MCP bindings | Unity editor perf one-click panel (INF-5.5) |

---

## Traceability

| Epic / FR | This document |
|-----------|---------------|
| FR-06 (doc 01) | Agentic dev infrastructure |
| Doc 04 Delegation | Unit agents vs Operator Copilot §7 |
| Doc 06 Database | DB Research Assistants §8, balance staging §3 |
| Doc 08 Architecture | Automation layer, headless API |
| Doc 11 Mission Editor | Scenario gen + events §1, §4 |
| Doc 17 Replay/AAR | Order log truth, batch CSV, experiment artifacts §2, §6 |
| GDD | [agentic-infrastructure.md](../../design/gdd/agentic-infrastructure.md) |
| Tracker | [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) §07 |

---

**Status:** Locked (Sprint 15)