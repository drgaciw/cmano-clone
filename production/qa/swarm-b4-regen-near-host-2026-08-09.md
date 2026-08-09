# SWARM-B4 / DRG-97 — Regen near host with stores

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Sim/Swarm/**`, `src/ProjectAegis.Sim.Tests/Swarm/**`  
**Requirement:** SWARM-13

## ACs

| AC | Evidence |
|----|----------|
| Near host + stores → regen up to maxDrones | `TryRegenNearHost` + `SwarmRegenEvaluator.CanRegen` |
| Without stores → no regen | `No_regen_without_stores` |
| Far from host → no regen | `No_regen_when_far_from_host` (DefaultMaxRangeDeg=0.5) |
| At maxDrones → no regen | `No_regen_above_maxDrones` |
| Host dead → no regen | `No_regen_when_host_dead` |
| Timeline reason `regen-host` | `IntegrityTimeline` via authorized `TryApplyIntegrityRegen` |
| Deterministic same-seed path | `Same_seed_path_is_deterministic` |

## Design notes

- Host proximity uses `SwarmLinkEvaluator.RangeDeg` vs `SwarmRegenEvaluator.DefaultMaxRangeDeg` (0.5°).
- Stores are a call-site boolean (`hostHasStores`); no Logistics surface change required for B4 pulse API.
- Regen keeps `SwarmIntegrityChange` shape: `DronesLost = 0`, `NewDroneCount > PreviousDroneCount`, reason `regen-host`.
- `ReplayIntegrityTimeline` applies regen rows (New > Previous) via `TryApplyIntegrityRegen`.

## Tests

`SwarmRegenTests` (9 facts) + prior Swarm suite (B1/A2/A6) must all pass:

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter "FullyQualifiedName~Swarm" -v minimal
```
