# SWARM-B1 / DRG-94 — Modes + Host + LinkState

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Sim/Swarm/**`, `src/ProjectAegis.Sim.Tests/Swarm/**`

## ACs

| AC | Evidence |
|----|----------|
| SWARM-10 mode orders logged | `IssueMode` → `ModeOrderLog` |
| SWARM-11 host bind + Screen | `BindHost` + Screen `Tick` gravitates to host |
| SWARM-12 linkState C2 only | `SwarmLinkEvaluator` + `RefreshLinkState`; CEC untouched |
| Lost link blocks orders | `EnsureOrdersAccepted` throws on Move/Mode |

## Tests

`SwarmModeHostLinkTests` (7 facts).
