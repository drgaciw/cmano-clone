# Sprint 116 — WatchAttention emit call-sites (S115 residual)

**Dates:** 2026-08-11  
**Stage:** **Release** · **Not Launch**  
**Predecessor:** S115 WatchAttention + auto-pause spine (merged)  
**Authority:** PRD P0-6/P0-7 · S115 residual list

## Goal

Thin pure fact sources that produce stable <code>WatchAttentionEvent</code> instances for:

1. First hostile/unknown contact detection (contact transition Unknown → Detected/Classified/Identified)
2. Own-side loss / BDA Lost promotion

## Tracks (surface-disjoint)

| Track | Story | Surface |
|-------|-------|--------|
| A | Contact transition → factory | `WatchAttentionEmitFactory` (pure) |
| B | BDA / own-side loss → factory + session wire | `SimulationSession.ApplyBda…` + `ReportContactTransitions` |
| C | Tests + residual update | Delegation.Tests · production docs |

## Must Have

| ID | AC |
|----|-----|
| S116-01 | Stable EventId per subject (`watch:contact:{targetId}`, `watch:loss:{unitId}`) |
| S116-02 | First-detect only (Unknown → Detected/Classified/Identified); re-detect does not re-emit |
| S116-03 | Own-side loss only for blue/`u1`; hostile losses do not emit OwnSideLoss |
| S116-04 | Session BDA MarkLost path reports own-side loss when applicable |
| S116-05 | Tests green; ReplayGolden untouched; ZERO Bridge |

## Non-goals

- Unity attention panel chrome
- P0-8 weapons-release forced 1×
- BalticReplayHarness / DelegationBridge hotpath wire (residual — pure API ready)
- CatalogWriteGate / asset waves / Phase N

## Hard gates

| Gate | Criterion |
|------|----------|
| Stage | Release |
| Bridge | ZERO hotpath |
| Hash | Baltic `17144800277401907079` preserved |
| Suite | Touched filters green |
