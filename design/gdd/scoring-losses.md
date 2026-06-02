# Scoring, Losses & Expenditures

> **Status:** Draft — Sprint 5  
> **Last Updated:** 2026-06-02  
> **Requirements:** [17-Replay-AAR-And-Order-Log.md](../../Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md) § Losses/Scoring  
> **Depends on:** [order-log-replay.md](order-log-replay.md), [logistics-magazines.md](logistics-magazines.md), [combat-domains-damage.md](combat-domains-damage.md)

## Overview

Running **losses**, **expenditures**, and **score** are projections of the order log plus scenario scoring metadata—not separate stores. Headless batch exports CSV for agent-vs-agent research.

## Player Fantasy

You see the scoreboard tick when missiles leave magazines and when hostiles vanish—every point traceable to a `sequenceId`.

## Detailed Design

### Losses (P0)

| Metric | Source |
|--------|--------|
| Platforms lost | `EngagementOutcomeCodes.Kill` victims (friendly filter by side P1) |
| Missiles fired | `MagazineChange` negative deltas |
| Policy denials | `PolicyDenial` count |

### Expenditures (P0 partial)

- Magazine burn aggregated per mount and shooter.
- Fuel: deferred to logistics GDD.

### Scoring (P0 MVP)

```
score = scenarioBaseScore
      + (killsHostile * pointsPerKill)
      - (friendlyKills * penaltyFriendlyKill)   // P1
      - (policyDenials * penaltyDenial)
```

Scenario JSON (doc 11) may define `pointsPerKill`, `baseScore` (future field; MVP uses constants in projection).

### Headless CSV (P0)

Columns: `scenarioId`, `seed`, `side`, `score`, `kills`, `missilesFired`, `denials`, `fingerprint`.

## Formulas

| Variable | Default | Range |
|----------|---------|-------|
| `pointsPerKill` | 100 | 10–1000 |
| `penaltyDenial` | 5 | 0–50 |
| `scenarioBaseScore` | 0 | any |

## Edge Cases

| Case | Behavior |
|------|----------|
| Intercept outcome | Count as expenditure; no kill credit |
| Duplicate kill log | Dedupe by `engagementId` |
| Replay scrub | Score recomputed from log slice 0..tick |

## Dependencies

| System | Contract |
|--------|----------|
| Order log | All inputs |
| Message log | May surface score milestones as INFO rows (P1) |
| C2 top bar | `LossesScoringProjection` tally (P1) |

## Tuning Knobs

See formulas table; scenario metadata overrides when present.

## Acceptance Criteria

1. `LossesScoringProjection` matches manual count on `baltic-patrol` golden log.
2. Same log → same score (deterministic).
3. No score stored only in UI—recomputable from log.

## UI Requirements

- Top bar or briefing strip: `SCORE`, `KILLS`, `MISSILES` (Sprint 5+ `C2TopBarPanelHost` extension P1).
- AAR agent cites score events with `sequenceId` only.

## TR IDs

| ID | Requirement |
|----|-------------|
| TR-score-001 | Kill-based score projection |
| TR-score-002 | Magazine expenditure tally |
| TR-score-003 | Headless CSV export (P1) |