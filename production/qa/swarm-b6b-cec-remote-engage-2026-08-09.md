# SWARM-B6b / DRG-103 — CEC remote engage-on-remote-data

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Sim/Engage/**`, `src/ProjectAegis.Sim.Tests/Engage/**`  
**Requirement:** SWARM-31 (engage half) — mesh health is B6a.

## Scope boundary

| In | Out |
|----|-----|
| Remote FC from CEC composite | CecMeshController mutations (B6a) |
| Explicit mesh-loss abort | Swarm linkState / C2 |
| EngageContext remote flags | Policy doctrine (B7) |

## ACs

| AC | Evidence |
|----|----------|
| Third shooter remote engage via composite | `Ship_plus_CEC_swarm_composite_allows_third_shooter_remote_engage` |
| Mesh loss → CecRemoteTrackUnavailable | `Mesh_loss_aborts_remote_with_CecRemoteTrackUnavailable` |
| Non-CEC cannot remote | `Non_CEC_shooter_cannot_remote_engage` |
| Aggregate swarm SoT unchanged | `Aggregate_swarm_SoT_unchanged_target_integrity_fields_still_apply` |
| Jam drops eligibility | `Jam_drops_remote_eligibility_without_organic` |

## Types

- `CecRemoteEngageGate` — pure eligibility + mesh lookup helper
- `EngagementAbortReason.CecRemoteTrackUnavailable`
- `EngageContext.UsesRemoteCecTrack` / `CecRemoteFireControlEligible` / `ShooterCecCapable`
