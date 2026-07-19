# 17 - Replay, AAR, and Order Log

**Last Updated:** 2026-07-18  
**Status:** Draft — ready for design review  
**FR reverse-ref:** [FR-15](01-Project-Overview.md) — Replay, order log, AAR  
**CMO basis:** Manual §6.3.11 Recorder, §6.3.12 Message Log, §6.3.13 Losses, §6.3.14 Scoring; §9.2.10 losses note  
**Related:** [02](02-Core-Gameplay-Loop.md) Core Gameplay Loop, [03](03-Simulation-Modes.md) Simulation Modes, [08](08-Agentic-Architecture.md) Agentic Architecture, [13](13-Doctrine-ROE-EMCON-WRA.md)–[14](14-Engagement-And-Fire-Control.md) policy/engage, [04](04-Agent-Delegation.md) Delegation  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) §17 — **Partial** (Release stage)  
**ADR:** [ADR-003](../../docs/architecture/adr-003-order-log-schema.md) Unified Order Log Schema

## Purpose

Define the **order log** as the canonical deterministic timeline, **replay** playback, **message log** presentation, **losses/expenditures/scoring** accounting, and **AI-generated after-action review (AAR)** — including headless batch metrics for agent-vs-agent research.

Implements hub **[FR-15](01-Project-Overview.md#functional-requirements)** (replay, order log, AAR).

## Shipped vs residual (headless-first)

| Lane | Scope | Status |
|------|-------|--------|
| **SHIPPED** | `DecisionLog` / `IOrderLog` append-only stream; `OrderLogEntry` + `OrderLogEntryKind` (incl. `AgentDecision`); `OrderLogReplayFingerprint` / `ComputeFingerprint`; **ReplayGolden 6/6** (Baltic v2 suite) + **6 v3 goldens** (isolated `baltic-v3-*` policies, hash unchanged — see `tests/regression/replay-golden-baltic-v3-*.txt`); production Baltic v2 hash **`17144800277401907079`**; `MessageLogProjection` (log → player-facing lines); `LossesScoringCsvExporter` / `LossesScoringProjection` batch CSV; `ReplayCheckpointStore` / `ReplayCheckpoint` | Headless-complete; CI-gated |
| **RESIDUAL** | Interactive timeline **scrub UI**; product **AAR natural-language** narrative (`aar_generate` product surface); agent decision **heatmaps** / kill-chain scrub chrome | Product UI / Phase 2 |
| **STUDIO** | `hindsight-aar-analyst` agent; `/replay-verify` skill (`.claude/skills/replay-verify/`) | Optional studio process — **not** product ACs |

Honesty note: MVP delivery for FR-15 is **headless log + golden + hash**, not scrub UI. Prior draft phased delivery inverted that priority; corrected below (Wave 2 re-honesty 2026-07-08).

## Vision

Replays are not video — they are **verifiable simulations**. Researchers and players scrub the same ordered events: human orders, agent decisions, policy blocks, engagements, and scenario events. AAR agents narrate what happened; they cannot contradict the log.

## CMO Parity Requirements

| Capability | CMO | Aegis | Honesty |
|------------|-----|-------|---------|
| Recorder / replay | §6.3.11 | **P0** — seed + order log, not just graphics | Headless record + fingerprint **met**; scrub UI **open** |
| Message log | §6.3.12 | **P0** | Projection **met**; full interactive chrome residual |
| Losses and expenditures | §6.3.13 | **P0** | Headless CSV **met** |
| Scoring | §6.3.14 | **P0** | Headless CSV **met** |
| Tacview export | §10.8, §6.4.6 | **P2** | Residual |

## Order Log (Canonical)

**RPL-01 — Append-only log.** Totally ordered by `(simTick, sequenceId)`. Single writer: `DecisionLog` implementing `IOrderLog` ([ADR-003](../../docs/architecture/adr-003-order-log-schema.md)).

### Entry types (minimum)

Canonical kind enum: `OrderLogEntryKind`. Prefer product naming **`AgentDecision`** (not “AgentIntent” alone). Spec language may still say “intent” when describing candidate actions; the logged entry kind is **`AgentDecision`** (`AgentDecisionPayload` / legacy `DecisionRecord` payload).

| type (`OrderLogEntryKind`) | Description |
|----------------------------|-------------|
| `PlayerOrder` | Course, speed, altitude, mission assign, etc. |
| `AgentDecision` | Proposed/executed agent decision (doc 04, 14); payload `AgentDecisionPayload` / `DecisionRecord` |
| `PolicyDenial` | FireAbortReason + snapshot id (doc 13) |
| `Engagement` | Launch, impact, miss, abort |
| `EngagementOutcome` | Resolved outcome record when distinct from launch |
| `ContactChange` | Detect, drop, classify |
| `MissionTransition` | Activate/deactivate, timeline (doc 11) |
| `EventFired` | Scenario event id + actions; event debugger projects from this entry (AME-5.5, doc 11 — `EventDebuggerTrace` / `scenario_event_trace` emit a filtered view, not a second store) |
| `PolicyUpdate` | ROE/EMCON/WRA change |
| `ModeChange` | Human/mixed/agent mode switch (doc 03) |
| `ControllerChange` | Controller bind / rebind |
| `GroupMemberDetach` / `GroupMemberRejoin` | Group override path (doc 04) |
| `MagazineChange` / `FuelBurn` / `FuelStateChange` / `PlatformDamageChange` / `CommsStateChange` | Logistics / domain side-effects |

### Required fields (all entries)

```yaml
simTick: 184502
sequenceId: 9918234
simTime: "2028-06-01T14:32:10Z"
scenarioSeed: 9034412
type: Engagement   # OrderLogEntryKind
payload: { ... }
hashChain: "sha256-prev+this"  # optional P1 for tamper-evident exports
```

**Unique Aegis:**

- **RPL-02 — Headless schema parity.** Headless runs write the **same schema** as interactive play. **Met** via shared `DecisionLog` on `DelegationOrchestrator`.
- **RPL-03 — Replay verify.** `/replay-verify` studio skill compares order log hash + final world-state hash. Product CI gate is **ReplayGolden 6/6** (Baltic v2) + **6 v3 goldens** (Baltic v3, isolated policies) + hash `17144800277401907079` (v2 production; v3 isolated, unchanged). Studio skill is optional (STUDIO lane).
- **RPL-04 — Agent provenance.** Agent entries include `agentId`, personality/traits context, `policySnapshotId`, `autonomyLevel` (as carried on `AgentDecision` / `DecisionRecord` payloads).

## Replay System

### Recording

- **RPL-05 — Auto-record.** Record order log for scenario runs used in golden / batch paths; optional disable for perf tests.
- **RPL-06 — Store bundle.** Scenario file hash / policy id, DB version where bound, build id, seed, order log, periodic world-state checkpoints via `ReplayCheckpointStore` (configurable interval).

### Playback

- **RPL-07 — Timeline scrub.** Scrub by sim time and tick — **RESIDUAL** product scrub UI; headless re-sim from seed + log is the shipped path.
- **RPL-08 — Map playback.** Unit positions from checkpoints + log-derived events — **RESIDUAL** interactive map scrub.
- **RPL-09 — Filter lanes.** Engagements, agent decisions, policy denials, messages, events — projection filters **partial** (`PlayerInfoFilter` / live order-log view); full scrub chrome open.
- **RPL-10 — Agent heatmap (P1).** Density of agent decisions per theater grid cell — **RESIDUAL**.
- **RPL-11 — Kill chain view (P1).** Detect → track → engage → BDA linked by `engagementId` — **RESIDUAL** UI (BDA projection helpers exist headless).

### Determinism gate

- **RPL-12 — Divergent re-sim FAIL.** Re-sim from checkpoint + log through divergent tick → FAIL build. **Met** via ReplayGolden suite.
- **RPL-13 — CI golden.** CI runs golden scenario comparison; Baltic v2 production fingerprint **`17144800277401907079`** must remain preserved unless an ADR explicitly changes it (AGENTS.md hard invariant). **Met** (ReplayGolden **6/6** v2 suite; **6 v3 goldens** added under `baltic-v3-*` isolation, hash unchanged).

## Message Log

- **RPL-14 — Projection-only.** Player-facing feed derived from order log subset (not a second truth) via `MessageLogProjection` / `MessageLogBridge`. **Met** headless.
- **RPL-15 — Severity + filters.** Severity: info, warning, critical; filter by side, unit, mission — **Partial** (projection + `playerInfoModel` filter; full chrome residual).
- **RPL-16 — Click-through.** Click message → select unit + open explain panel (policy/engage) — **RESIDUAL** C2 chrome.
- **RPL-17 — Radio templates (P1).** Immersion templates without hiding codes — residual.

## Losses, Expenditures, and Scoring

- **RPL-18 — Running totals.** Platforms lost, missiles fired, fuel consumed (doc 16 linkage) — headless projection **met** (`LossesScoringProjection` / snapshot).
- **RPL-19 — Scenario scoring.** Scoring model in scenario metadata (doc 11); event-triggered score changes logged — **Partial** (batch CSV path shipped).
- **RPL-20 — Batch CSV.** Headless batch CSV: side, score, losses, sorties, magazine burn rate — **Met** (`LossesScoringCsvExporter`, `BalticBatchRunner`).
- **RPL-21 — Campaign carry-forward (P1).** Deferred v1 per doc 01 — schema only.

## After-Action Review (AAR)

### Player AAR (post-scenario)

- **RPL-22 — Timeline summary.** Key engagements, mission success/fail, policy denials count — **Partial** headless metrics; product NL surface open.
- **RPL-23 — AAR Agent narrative.** Product AAR agent generates narrative **grounded in order log only**; cites `sequenceId` links — **RESIDUAL** product (`aar_generate`). Studio: `hindsight-aar-analyst` + Hindsight hooks optional.
- **RPL-24 — Suggested improvements (P1).** Mission timing, ROE changes, agent personality swaps — residual.
- **RPL-25 — Export PDF/Markdown (P1).** Research-paper export — residual.

### Agent performance analytics

- **RPL-26 — Per-agent stats.** Intents/decisions proposed vs executed vs denied vs overridden — **Partial** via order-log enumeration.
- **RPL-27 — Replay compare.** Compare two replays with same scenario, different seeds or agent assignments — residual product UI; headless re-run **met**.
- **RPL-28 — Balance consumer (P1).** Balance Agent consumes batch metrics (doc 11) — residual product wiring.

## Non-Functional Requirements

| ID | Area | Target |
|----|------|--------|
| **RPL-NFR-01** | Storage | 24h scenario log &lt; 500 MB compressed (target; tune per profiling) |
| **RPL-NFR-02** | Performance | Logging &lt; 5% sim tick overhead at 5k entities |
| **RPL-NFR-03** | Privacy | No PII in logs; local/export controlled |
| **RPL-NFR-04** | Interop | **P2** Tacview ACMI export from Engagement entries |

## MCP / Agentic Tools

| Tool | Description | Honesty |
|------|-------------|---------|
| `replay_list` | Replays for scenario | Residual product / CLI |
| `replay_export` | Order log JSON | Partial (fingerprint + harness export path) |
| `replay_verify` | Golden hash compare | **STUDIO** skill + CI ReplayGolden (**met** for golden gate) |
| `aar_generate` | NL summary from log | **RESIDUAL** product |
| `metrics_batch` | Headless run aggregates | **Met** (`BalticBatchRunner` / CSV) |

## Acceptance Criteria

| # | Criterion | Tag |
|---|-----------|-----|
| AC-1 | Interactive run and headless run (same inputs) produce **identical** order log hash. | **Met** (shared `DecisionLog` / fingerprint path; CI ReplayGolden) |
| AC-2 | Scrub to engagement event; map shows units at correct historical positions. | **Open** (scrub / map playback UI residual) |
| AC-3 | Policy denial in play appears in message log with same `sequenceId` as order log. | **Met** (projection from same log; interactive chrome polish residual) |
| AC-4 | AAR Agent summary references only events present in log (spot-check 10 claims). | **Open** (product NL AAR residual; studio Hindsight optional) |
| AC-5 | Batch of 100 agent-vs-agent runs exports scoring CSV without UI. | **Met** (headless CSV exporter + batch runner) |
| AC-6 | `/replay-verify` PASS on golden Baltic scenario after sim merge. | **Met** as CI ReplayGolden **6/6** (v2) + **6 v3 goldens** (isolated) + hash `17144800277401907079`; studio skill optional adjunct |

## Phased Delivery

Corrected headless-first order (Wave 2 re-honesty — **not** scrub-first):

| Phase | Scope | Honesty |
|-------|--------|---------|
| **MVP (shipped)** | Order log schema (`IOrderLog` / `DecisionLog` / `OrderLogEntry`); headless record; fingerprint; **ReplayGolden 6/6** (Baltic v2) + **6 v3 goldens** (Baltic v3 isolation); production hash **`17144800277401907079`** (v2; v3 isolated, unchanged); `MessageLogProjection`; losses/scoring CSV; `ReplayCheckpointStore` | **Complete** for headless gate |
| **Phase 2** | Interactive **scrub UI**; product **AAR NL** narrative (`aar_generate`); agent heatmap; kill-chain scrub chrome | **Residual** product surfaces |
| **Phase 3** | Tacview export, campaign metrics, tamper-evident hash chain | Deferred / P2 |

## Implementation Mapping (headless-first)

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| Order log core | `DecisionLog` : `IOrderLog`; `OrderLogEntry`, `OrderLogEntryKind`, `OrderLogEntryFactories` (`ProjectAegis.Delegation` · `Decision/`) | **Shipped** | Append-only; kinds include `AgentDecision`, `PlayerOrder`, `PolicyDenial`, `Engagement`, … |
| Agent decision payload | `AgentDecisionPayload`, `DecisionRecord` | **Shipped** | Prefer **`AgentDecision`** naming in docs; intent language maps to this kind |
| Fingerprint / golden | `OrderLogReplayFingerprint`, `DecisionLog.ComputeFingerprint` (`Replay/`) | **Shipped** | ReplayGolden **6/6** (Baltic v2) + **6 v3 goldens** (`baltic-v3-*` isolation); hash **`17144800277401907079`** (v2 production; v3 isolated, unchanged) |
| Checkpoints | `ReplayCheckpointStore`, `ReplayCheckpoint` (`Replay/`) | **Shipped** | Used by `BalticReplayHarness` |
| Message log projection | `MessageLogProjection`, `MessageLogLine`, `MessageLogBridge` (`Projection/`, UnityAdapter `Bridge/`) | **Shipped** (projection); UI residual | Headless project-from-log |
| Losses / scoring | `LossesScoringProjection`, `LossesScoringCsvExporter`, `LossesScoringSnapshot` (`Projection/`) | **Shipped** | `BalticBatchRunner` CSV path |
| Live filter | `PlayerInfoFilter`, `DelegationOrchestrator.GetLiveOrderLogView()` | **Shipped** | HUD/message filter without mutating stored log |
| Headless harness | `BalticReplayHarness`, `BalticBatchRunner` (`UnityAdapter` · `Baltic/`) | **Shipped** | CI golden + batch metrics |
| Order dispatch side-effects | `OrderDispatcher` / `IOrderSink`, `SimulationSession` engage appends | **Shipped** | Engagements/orders enter same log |
| Studio verify skill | `.claude/skills/replay-verify/SKILL.md` | **STUDIO** | Optional; not product AC alone |
| Studio AAR analyst | `hindsight-aar-analyst`; `HindsightOrderLogHook` / session finalizer | **STUDIO** | Optional sidecar; no Tick-path recall |
| Scrub UI / product AAR NL / heatmaps | C2 presentation hosts | **Residual** | Phase 2 |

**Blast radius:** Prefer order-log / projection-only diffs. `DelegationBridge` remains **zero-touch hotpath** through Release v1. GitNexus: impact `DecisionLog` / `IOrderLog` upstream before schema changes (historically LOW for pure append extensions).

## Open Questions

1. Checkpoint interval default: time-based vs event-based (every engagement)?
2. Store full world state every tick vs delta compression for research exports?
3. Multiplayer replay merge (future): single ordered log per side?

## Traceability

| Doc / FR | Relationship |
|----------|----------------|
| **FR-15** ([01](01-Project-Overview.md)) | Replay, order log, AAR — this document |
| [02](02-Core-Gameplay-Loop.md) | Phase 4 AAR |
| [03](03-Simulation-Modes.md) | Mode changes in log; headless AvA |
| [08](08-Agentic-Architecture.md) | Serialization, headless; ARCH order-log boundary |
| [13](13-Doctrine-ROE-EMCON-WRA.md)–[14](14-Engagement-And-Fire-Control.md) | Denial and engage entries |
| [04](04-Agent-Delegation.md) | `AgentDecision` entries; controller / detach-rejoin |
| [ADR-003](../../docs/architecture/adr-003-order-log-schema.md) | Unified order log schema |
| `cmo-manual-traceability.md` | §6.3.11–14 |
| Tracker | [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) §17 — **Partial** |

---

**References:** CMO Manual §6.3.11–14, §9.2.10; `docs/manual/index.html`; `.claude/skills/replay-verify/SKILL.md`; hash invariant `17144800277401907079`

**Tracker row 17:** **Partial** — [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md)  
**Next stack tasks (tracker row 17):** Interactive **scrub UI** (RPL-07/08, AC-2); product **AAR agent** NL narrative `aar_generate` (RPL-23, AC-4).  
**Implementation grade:** Partial — headless MVP (log + v2 golden 6/6 + 6 v3 goldens + hash) shipped; scrub UI / product AAR NL residual. Design Status remains **Draft**. Charter re-honesty: Wave 2 2026-07-08; v3 golden verification 2026-07-18.
