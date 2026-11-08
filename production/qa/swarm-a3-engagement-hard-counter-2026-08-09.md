# SWARM-A3 — Engagement scaling + hard-counter AA (DRG-88)

**Date:** 2026-08-09  
**Linear:** [DRG-88](https://linear.app/drgamtd-workspace/issue/DRG-88) · Epic [DRG-83](https://linear.app/drgamtd-workspace/issue/DRG-83)  
**Requirements:** SWARM-04 (offensive), SWARM-08 (hard-counter)  
**Surface:** `src/ProjectAegis.Sim/Engage/**` · Sim.Tests · this QA note  
**Verdict:** PASS (Phase A)

## Scope

| Concern | Implementation |
|---------|----------------|
| Offensive scale with living drones | `SwarmOffensiveEffect.Scale` — linear integrity fraction (tuning: `ScaleFactorPower`) |
| Hard-counter AA | `SwarmHardCounterAa` — AreaAa 8 drones/hit vs PointFire 1 drone/hit @ equal nominal DPS units |
| Aggregate integrity SoT | `SwarmEngagementIntegrityApplier` → `SwarmController.TryApplyIntegrityDamage` only |

**Not in this PR:** catalog (A1), sensors (A4), C2 UI (A5), DelegationBridge.

## Acceptance

| AC | Evidence | Verdict |
|----|----------|---------|
| Full swarm > half effect | `SwarmOffensiveEffectTests.Full_swarm_deals_more_effect_than_half_depleted_under_identical_geometry` | **PASS** |
| Hard-counter QA scenario | Area vs point after 5 hits: area destroyed (40 lost), point 35 remaining | **PASS** |
| Integrity via authorized API | `SwarmEngagementIntegrityApplier` only path; timeline logged | **PASS** |

## Equal-nominal framing

Both profiles declare `EqualNominalDpsUnits = 10`. Hard-counter advantage is **drones lost per hit**, not a higher DPS number — area volume fire shreds the cloud.

## Verify

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~SwarmOffensive|FullyQualifiedName~SwarmHardCounter" -v minimal
```

## Review follow-up (hotpath)

- `MvpEngagementResolver` scales `PkBase` when `EngageContext.ShooterMaxDrones > 0`.
- On Hit/Kill with `TargetMaxDrones > 0`, optional `ISwarmIntegrityDamageSink` applies AA profile loss via authorized API.
- `EngageContext.PointFireDronesLostPerHit` / `AreaAaDronesLostPerHit` override table defaults (0 = default).

