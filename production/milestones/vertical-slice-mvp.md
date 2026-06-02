# Milestone: Baltic-Style Vertical Slice (MVP)

## Overview

- **Target Date**: 2026-07-15 (proposed)
- **Type**: Vertical Slice
- **Duration**: 6 weeks (from 2026-06-02)
- **Number of Sprints**: 2–3 (Sprint 1 complete; Sprint 2–3 sensor/UI)

## Milestone Goal

Demonstrate the core military simulation loop **plan → fight → replay** headlessly and in Unity: seeded scenario load, detection/engage with policy gates, order log + checkpoints, combat outcomes, and AAR message projection. This slice validates that Project Aegis can ship a CMANO-style MVP without blocking on full GDD coverage (6/20 systems documented).

## Success Criteria

- [x] `dotnet test ProjectAegis.sln` — 0 failures (verified 2026-06-02: **181** passed on `main` @ `1f7423e`)
- [x] PlayMode smoke harness — 3 tests pass (headless adapter)
- [x] Sprint 1 epics complete (Baltic, sensor, PD, DATA basePd, mission, replay, combat) — merged via PR #35–#36
- [x] Contact Classify/Identify FSM (TR-sensor-001 remainder) — headless + order log
- [x] Unity sensor C2 presentation slice (UI Toolkit panel + OnGUI fallback)
- [x] Requirements design review blockers C1–C5 closed or explicitly deferred with ADR ([architecture-review-2026-06-02](../../docs/architecture/architecture-review-2026-06-02.md): C1–C4 closed, C5 deferred per ADR-001)
- [x] `/replay-verify` golden baseline stored for Baltic harness seed ([replay-2026-06-02](../determinism/replay-2026-06-02.md) **PASS**)
- [ ] 0 open S1 bugs; S2 bugs triaged

## Feature List

### Must Ship (Milestone Fails Without These)

| Feature | Design Doc | Owner | Sprint Target | Status |
|---------|-----------|-------|--------------|--------|
| Headless plan→fight→replay | `baltic-headless-slice` | Engineering | Sprint 1 | **Complete** |
| Pd detection + catalog basePd | `pd-detection-loop`, `platform-db-basepd` | Engineering | Sprint 1 | **Complete** |
| Order log + checkpoints + C1 combat | `order-log-replay`, `combat-outcomes` | Engineering | Sprint 1 | **Complete** |
| Contact Classify FSM | `sensor-detection-ew.md` | Engineering | Sprint 2 | **Complete** |
| Minimal sensor C2 UI | `sensor-detection-ew.md` (approved) | UI + Unity | Sprint 2 | **Complete** (UI Toolkit `SensorC2PanelHost`) |

### Should Ship (Planned but Cuttable)

| Feature | Design Doc | Owner | Sprint Target | Cut Impact | Status |
|---------|-----------|-------|--------------|-----------|--------|
| CMO catalog import pipeline | `platform-db-basepd` | Content | Sprint 2 | Manual catalog only | Complete (export bridge) |
| Message log UI bridge | requirements C2 | UI | Sprint 3 | AAR text-only | **Partial** (`MessageLogPanelHost` combat strip) |

### Stretch Goals (Only if Ahead of Schedule)

| Feature | Design Doc | Owner | Value Add |
|---------|-----------|-------|----------|
| Full 20-system GDD coverage | systems-index | Design | Reduces rework |
| `/vertical-slice` formal gate | — | Producer | Production → Polish signal |

## Quality Gates

| Gate | Threshold | Measurement Method |
|------|-----------|-------------------|
| Test suite | 0 failures | `dotnet test ProjectAegis.sln` |
| Determinism | No new HIGH findings | `/determinism-audit` |
| Replay | Golden hash match | `/replay-verify` |
| GitNexus impact | Review before HIGH symbols | `gitnexus impact` |

## Risk Register

| Risk | Probability | Impact | Mitigation | Owner | Status |
|------|------------|--------|-----------|-------|--------|
| DecisionLog / Orchestrator HIGH blast radius | Medium | High | Impact analysis before C1 edits | Engineering | Open |
| GDD coverage 30% | High | Medium | Sprint 2+ `/design-system` for C2 UI | Design | Open |
| Unity Editor not in CI | Medium | Medium | Headless PlayMode harness | DevOps | Mitigated |

## Dependencies

### Internal Dependencies

| Feature | Depends On | Owner of Dependency | Status |
|---------|-----------|-------------------|--------|
| Sensor C2 UI | `sensor-detection-ew` GDD approved | Design | **Done** (2026-06-02) |
| Classify FSM | Contact headless slice | Engineering | **Done** |

### External Dependencies

| Dependency | Provider | Status | Risk if Delayed |
|-----------|---------|--------|----------------|
| Unity 6.3 LTS | Unity | Pinned | Editor-only features slip |

## Review Schedule

| Date | Review Type | Attendees |
|------|-----------|-----------|
| 2026-06-09 | Sprint 2 kickoff | Producer, Engineering |
| 2026-06-23 | Mid-milestone | Full team |
| 2026-07-15 | Milestone review | Full team |