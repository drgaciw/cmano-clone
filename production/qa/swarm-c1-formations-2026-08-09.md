# SWARM-C1 / DRG-105 — Formations (Cloud / Wall / Spear / Orbit)

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Sim/Swarm/Formation/**`, thin `SwarmController` hooks, `src/ProjectAegis.Sim.Tests/Swarm/Formation/**`  
**Linear:** [DRG-105](https://linear.app/drgamtd-workspace/issue/DRG-105) · SWARM-16

## ACs

| AC | Evidence |
|----|----------|
| Enum Cloud/Wall/Spear/Orbit | `SwarmFormation` |
| Soft deterministic offsets (dx,dy deg) | `SwarmFormationLayout.ComputeOffsets` |
| `IssueSetFormation` logged | `FormationOrderLog` (mode-order pattern) |
| Default formation Cloud on Register | `SwarmRuntimeUnit` ctor + `GetFormation` |
| Orbit biases toward host when bound | Layout host bearing + host publish via public API |

## Tests

Filter: `FullyQualifiedName~Formation|FullyQualifiedName~SwarmMode`

`SwarmFormationTests` + existing `SwarmModeHostLinkTests`.

## Notes

Formations are **cosmetic soft constraints** on member layout — not engagement SoT (SWARM-07 aggregate authority unchanged).
