# SWARM-C2 / DRG-106 — Multi-axis auto-split assault

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Sim/Swarm/Assault/**`, `src/ProjectAegis.Sim.Tests/Swarm/Assault/**`  
**Requirement:** SWARM-17

## Scope boundary

| In | Out |
|----|-----|
| Pure planner/splitter (`SwarmAssaultAxisSplitter`) | Per-drone physics SoT (SWARM-28) |
| Deterministic axis allocations + bearings | Formation/** (SWARM-16 / C1) |
| Doctrine + Assault mode gates | SoftKill/** (SWARM-18 / C3) |
| Shares sum to `droneCount` | Policy / Data / Delegation edits |
| | `SwarmController.cs` mutations |

**Independence:** Assault planner is allocation-only; it does not tick units, apply integrity, or issue orders.

## ACs

| AC | Evidence |
|----|----------|
| Split N logical mass into K≥2 axes, shares sum to droneCount | `Assault_splits_mass_across_K_axes_summing_to_droneCount` |
| Min ≥1 drone per axis when split | `Min_one_drone_per_axis_when_split_applied` |
| Reduce K when droneCount < K | `Reduces_K_when_droneCount_less_than_requested_axes` |
| Disabled when mode ≠ Assault | `Non_Assault_mode_returns_single_axis_without_split` |
| Disabled when doctrineAllowSplit=false | `Doctrine_disallow_returns_single_axis_without_split` |
| Deterministic same-seed path | `Same_seed_is_deterministic` |
| Seed can vary remainder assignment | `Different_seeds_can_vary_remainder_assignment` |
| Approach bearings fan around target | `Approach_bearings_fan_around_target_bearing` |

## Types

- `SwarmAssaultAxisAllocation` — `AxisIndex`, `DroneShare`, `ApproachBearingDeg`
- `SwarmAssaultSplitPlan` — `SplitApplied`, requested/effective K, axes list, `TotalDroneShare`
- `SwarmAssaultAxisSplitter.Plan(...)` — pure static planner
  - Inputs: `droneCount`, `axisCount`, `mode`, `seed`, `doctrineAllowSplit`, optional `targetBearingDeg`
  - `DefaultAxisCount = 2`, `DefaultAxisSpreadDeg = 30`

## Gates

- `mode == Assault` **and** `doctrineAllowSplit` **and** effective K ≥ 2 → `SplitApplied=true`
- Otherwise single-axis plan (`SplitApplied=false`) or empty when `droneCount ≤ 0`

## Verification

```bash
export PATH="/root/.dotnet:$PATH"
export VSTEST_CONNECTION_TIMEOUT=300
cd /workspace/artifacts/cmano-clone/.worktrees/drg-106-c2
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~Assault|FullyQualifiedName~MultiAxis" -v minimal
```

## Result

```
Passed!  - Failed: 0, Passed: 16, Skipped: 0, Total: 16
```

Filter: `FullyQualifiedName~Assault|FullyQualifiedName~MultiAxis`
