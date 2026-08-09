# SWARM-A4 — Sensor effectiveness scales with integrity (DRG-89)

**Date:** 2026-08-09  
**Linear:** [DRG-89](https://linear.app/drgamtd-workspace/issue/DRG-89)  
**Requirements:** SWARM-04 (sensors)  
**Surface:** `src/ProjectAegis.Sim/Sensors/**` · Sim.Tests · this note  
**Verdict:** PASS

## Tuning knob

| Constant | Default | Meaning |
|----------|---------|---------|
| `SwarmSensorScale.IntegrityPower` | `1.0` | Power on integrity fraction (linear) |
| `SwarmSensorScale.MinLivingScale` | `0.0` | Floor when drones remain |

Default curve: **linear** `Pd_eff = basePd × (droneCount / maxDrones)`.

`DetectionProbability.ComputePd` gained optional `swarmIntegrityScale` (default `1.0`) — existing callers unchanged.

## AC

| AC | Test | Verdict |
|----|------|---------|
| Half integrity worse than full | `Half_integrity_swarm_detects_worse_than_full_under_controlled_fixture` | **PASS** |
| Curve documented | this note + constants | **PASS** |

## Verify

```bash
dotnet test src/ProjectAegis.Sim.Tests --filter "FullyQualifiedName~SwarmSensorScale" -v minimal
```
