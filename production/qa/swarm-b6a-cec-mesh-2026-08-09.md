# SWARM-B6a / DRG-102 — CEC mesh health + composite track

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Sim/Cec/**`, `src/ProjectAegis.Sim.Tests/Cec/**`  
**Requirement:** SWARM-31 (mesh half only) — remote engage is B6b / separate.

## Scope boundary

| In | Out |
|----|-----|
| CEC mesh membership / health | C2 `SwarmLinkState` (B1 / Swarm) |
| Composite track picture | Remote engage / fire-through-mesh (B6b) |
| Jam/range drop mesh without implying C2 lost | Sensors B5 contact classification |
| Non-CEC gate | Catalog `CecCapable` seed (B2 already) |

**Independence:** `ProjectAegis.Sim.Cec` never references `SwarmLinkState` or Swarm C2 order path.

## ACs

| AC | Evidence |
|----|----------|
| Non-CEC cannot join mesh | `Non_CEC_cannot_join_mesh` |
| ≥2 USN CEC in range → InMesh both | `Two_USN_CEC_nodes_in_range_are_both_InMesh` |
| Jam → OutOfMesh (no Swarm types) | `Jam_forces_OutOfMesh_without_any_Swarm_types` |
| Range stretch → Degraded then OutOfMesh | `Range_stretch_moves_InMesh_to_Degraded_then_OutOfMesh` |
| Composite track when two nodes contribute same target | `Composite_track_forms_when_two_nodes_contribute_same_target` |
| Fire-control quality false when only degraded mesh | `Fire_control_quality_false_when_only_degraded_mesh` |
| Deterministic refresh order | `Deterministic_refresh_order_produces_stable_event_log` |

## Types

- `CecMeshState` — `InMesh` / `Degraded` / `OutOfMesh`
- `CecNodeRegistration` — unit/side/capability/geometry
- `CecCompositeTrack` — fused track + FC quality flag
- `CecMeshEvaluator` — pure range bands (`DefaultConnectedRangeDeg=2.0`, `DefaultDegradedRangeDeg=4.0`)
- `CecMeshController` — register / refresh / contribute / composite / event log

## Verification

```bash
export PATH="$HOME/.dotnet:$PATH"
cd /workspace/artifacts/cmano-clone/.worktrees/drg-102-b6a
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter "FullyQualifiedName~Cec" -v minimal
dotnet build src/ProjectAegis.Sim/ProjectAegis.Sim.csproj -v minimal
```

## Result

```
Passed!  - Failed: 0, Passed: 9, Skipped: 0, Total: 9
Build succeeded. 0 Warning(s) 0 Error(s)
```
