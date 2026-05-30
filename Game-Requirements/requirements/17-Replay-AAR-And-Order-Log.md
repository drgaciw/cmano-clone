# 17 - Replay, AAR, and Order Log

**Last Updated:** May 29, 2026  
**Status:** Draft — ready for design review  
**CMO basis:** Manual §6.3.11 Recorder, §6.3.12 Message Log, §6.3.13 Losses, §6.3.14 Scoring; §9.2.10 losses note  
**Related:** 02 Core Gameplay Loop, 03 Simulation Modes, 08 Agentic Architecture, 13–14 policy/engage, 04 Delegation

## Purpose

Define the **order log** as the canonical deterministic timeline, **replay** playback, **message log** presentation, **losses/expenditures/scoring** accounting, and **AI-generated after-action review (AAR)** — including headless batch metrics for agent-vs-agent research.

## Vision

Replays are not video — they are **verifiable simulations**. Researchers and players scrub the same ordered events: human orders, agent intents, policy blocks, engagements, and scenario events. AAR agents narrate what happened; they cannot contradict the log.

## CMO Parity Requirements

| Capability | CMO | Aegis |
|------------|-----|-------|
| Recorder / replay | §6.3.11 | **P0** — seed + order log, not just graphics |
| Message log | §6.3.12 | **P0** |
| Losses and expenditures | §6.3.13 | **P0** |
| Scoring | §6.3.14 | **P0** |
| Tacview export | §10.8, §6.4.6 | **P2** |

## Order Log (Canonical)

**P0:** Append-only, totally ordered by `(simTick, sequenceId)`.

### Entry types (minimum)

| type | Description |
|------|-------------|
| `PlayerOrder` | Course, speed, altitude, mission assign, etc. |
| `AgentIntent` | Proposed/executed intent (doc 04, 14) |
| `PolicyDenial` | FireAbortReason + snapshot id (doc 13) |
| `Engagement` | Launch, impact, miss, abort |
| `ContactChange` | Detect, drop, classify |
| `MissionTransition` | Activate/deactivate, timeline (doc 11) |
| `EventFired` | Scenario event id + actions |
| `PolicyUpdate` | ROE/EMCON/WRA change |
| `ModeChange` | Human/mixed/agent mode switch (doc 03) |

### Required fields (all entries)

```yaml
simTick: 184502
sequenceId: 9918234
simTime: "2028-06-01T14:32:10Z"
scenarioSeed: 9034412
type: Engagement
payload: { ... }
hashChain: "sha256-prev+this"  # optional P1 for tamper-evident exports
```

**Unique Aegis:**

- **P0** Headless runs write the **same schema** as interactive play.
- **P0** `/replay-verify` compares order log hash + final world-state hash (studio skill).
- **P0** Agent entries include `agentId`, `personality`, `policySnapshotId`, `autonomyLevel`.

## Replay System

### Recording

- **P0** Auto-record on scenario start; optional disable for perf tests.
- **P0** Store: scenario file hash, DB version, build id, seed, order log, periodic world-state checkpoints (configurable interval).

### Playback

- **P0** Timeline scrub by sim time and tick.
- **P0** Map playback: unit positions interpolated from checkpoints + log-derived events.
- **P0** Filter lanes: engagements, agent decisions, policy denials, messages, events.
- **P1** **Agent heatmap**: density of agent intents per theater grid cell.
- **P1** Kill chain view: detect → track → engage → BDA linked by `engagementId`.

### Determinism gate

- **P0** Re-sim from checkpoint + log through divergent tick → FAIL build.
- **P0** CI runs golden scenario comparison on merge to sim branches.

## Message Log

- **P0** Player-facing feed derived from order log subset (not a second truth).
- **P0** Severity: info, warning, critical; filter by side, unit, mission.
- **P0** Click message → select unit + open explain panel (policy/engage).
- **P1** Radio-style templates for immersion without hiding codes.

## Losses, Expenditures, and Scoring

- **P0** Running totals: platforms lost, missiles fired, fuel consumed (doc 16 linkage).
- **P0** Scoring model in scenario metadata (doc 11); event-triggered score changes logged.
- **P0** Headless batch CSV: side, score, losses, sorties, magazine burn rate.
- **P1** Campaign carry-forward hook (deferred v1 per doc 01) — schema only.

## After-Action Review (AAR)

### Player AAR (post-scenario)

- **P0** Timeline summary: key engagements, mission success/fail, policy denials count.
- **P0** **AAR Agent** (doc 07) generates narrative **grounded in order log only**; cites `sequenceId` links.
- **P1** Suggested improvements: mission timing, ROE changes, agent personality swaps.
- **P1** Export PDF/Markdown for research papers.

### Agent performance analytics

- **P0** Per-agent stats: intents proposed vs executed vs denied vs overridden.
- **P0** Compare two replays with same scenario, different seeds or agent assignments.
- **P1** Balance Agent consumes batch metrics (doc 11).

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Storage | 24h scenario log &lt; 500 MB compressed (target; tune per profiling) |
| Performance | Logging &lt; 5% sim tick overhead at 5k entities |
| Privacy | No PII in logs; local/export controlled |
| Interop | **P2** Tacview ACMI export from Engagement entries |

## MCP / Agentic Tools

| Tool | Description |
|------|-------------|
| `replay_list` | Replays for scenario |
| `replay_export` | Order log JSON |
| `replay_verify` | Golden hash compare |
| `aar_generate` | NL summary from log |
| `metrics_batch` | Headless run aggregates |

## Acceptance Criteria

1. Interactive run and headless run (same inputs) produce **identical** order log hash.
2. Scrub to engagement event; map shows units at correct historical positions.
3. Policy denial in play appears in message log with same `sequenceId` as order log.
4. AAR Agent summary references only events present in log (spot-check 10 claims).
5. Batch of 100 agent-vs-agent runs exports scoring CSV without UI.
6. `/replay-verify` PASS on golden Baltic scenario after sim merge.

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | Order log schema, record/playback scrub, message log, losses tally |
| **Phase 2** | AAR Agent, agent heatmap, kill chain UI, CI golden replay |
| **Phase 3** | Tacview export, campaign metrics, tamper-evident hash chain |

## Implementation Mapping (Existing Code — GitNexus 2026-05-29)

| Requirement | Current code | Migration |
|-------------|--------------|-----------|
| Order log | `DecisionLog` + `DecisionRecord` in `ProjectAegis.Delegation` | Extend types; add `OrderLogEntry` discriminated union; keep append-only API |
| Replay golden | `ReplayGoldenTests`, `DeterministicHash` | Expand fingerprint to include new entry types when added |
| Order dispatch | `OrderDispatcher` / `IOrderSink` | Emit `PlayerOrder` / `Engagement` log entries alongside sink |

**Blast radius:** `npx gitnexus impact --repo cmano-clone -d upstream DecisionLog` → **LOW** (3 symbols).

## Open Questions

1. Checkpoint interval default: time-based vs event-based (every engagement)?
2. Store full world state every tick vs delta compression for research exports?
3. Multiplayer replay merge (future): single ordered log per side?

## Traceability

| Doc | Relationship |
|-----|----------------|
| 02 | Phase 4 AAR |
| 03 | Mode changes in log |
| 08 | Serialization, headless |
| 13–14 | Denial and engage entries |
| 04 | Agent intent entries |
| `cmo-manual-traceability.md` | §6.3.11–14 |

---

**References:** CMO Manual §6.3.11–14, §9.2.10; `docs/manual/index.html`; `.claude/skills/replay-verify/SKILL.md`
