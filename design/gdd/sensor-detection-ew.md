# Sensor, Detection, and Electronic Warfare

> **Status:** In Review  
> **Author:** design-system  
> **Last Updated:** 2026-05-29  
> **Implements Pillar:** Simulation fidelity  
> **Requirements:** [15-Sensor-Detection-And-EW.md](../../Game-Requirements/requirements/15-Sensor-Detection-And-EW.md)  
> **Architecture:** [architecture.md](../../docs/architecture/architecture.md) (tick step 4)  
> **Depends on:** [simulation-core-time.md](simulation-core-time.md), [policy-roe-emcon-wra.md](policy-roe-emcon-wra.md)

> **Quick reference** — Layer: **Core** · Priority: **MVP** · Key deps: Sim Core, Platform DB, Policy · Depended on by: Engagement, C2 UI, Agents

## Summary

Sensors turn emitters and propagation into **contacts** the commander and agents share. Detection is **deterministic**, **sorted**, and **logged**; EMCON and EW modify whether a track exists—not whether the rules are fair.

## Overview

Each tick, active emitters (per EMCON) attempt detection against targets in sorted `(observerId, sensorId, targetId)` order. Returns merge into a stable contact list with lifecycle states. EW applies before merge. Every transition emits `ContactChange` in the order log.

## Player Fantasy

You feel the fog lift when your ESM catches a raid radar, and you feel it fall when jamming blanks your fire-control picture—always with a reason you can replay.

## Detailed Design

### Contact lifecycle

```
Unknown → Detected → Classified → Identified → Lost
```

| State | Promotion trigger |
|-------|-------------------|
| Detected | First valid detection this observer |
| Classified | Category confidence threshold |
| Identified | Type ID confidence or visual ID |
| Lost | No update for `staleTicks`; removed after `dropTicks` |

### Detection tick (step 4)

1. Collect emitters where EMCON ≠ Off for that class  
2. Sort emitters `(observerId, sensorId)`  
3. For each emitter, sort targets `targetId`  
4. Apply environment mask (terrain, weather, sea state)  
5. Roll detection via `SeededRng(Detection, …)` — table-driven Pd from DB  
6. Apply EW modifiers (noise jam → Pd multiplier)  
7. Merge into contacts (stable `contactId` per observer+target key)  
8. Emit `ContactChange` entries  

### Sensor classes (MVP)

Radar (air/surface), passive sonar, active sonar, EO/IR, ESM. Datalink-fed tracks ingested as sensor type `Datalink` with sharing rules.

### Side picture

**P0:** Observers on a side share contacts per scenario datalink doctrine unless `organicOnly` flag set.

### EW (MVP)

| Effect | Rule |
|--------|------|
| Noise jamming | `Pd *= (1 - jamStrength)` clamped |
| ESM | Detect emitter → contact without own active radar |
| Deception | P1; MVP: range gate noise only |

### Agent / UI

- Agents consume `ObservedState` contacts only (no omniscience unless cheat flag).  
- C2 UI filters by stale, classification, weapon assignment.  
- Hover shows contributing sensors + last Pd roll bucket (explainability).

## Formulas

### Detection probability (MVP)

```
Pd = clamp01( basePd(db, sensor, target, range) * envMask * eccmFactor * (1 - jamStrength) )
detected = draw < Pd   // draw from SeededRng Detection domain
```

| Variable | Source |
|----------|--------|
| basePd | Platform DB |
| envMask | Scenario weather/terrain |
| jamStrength | Active jammers in range |

### Stale / drop

```
stale = (simTick - lastUpdatedSimTick) > staleThresholdTicks
drop = stale && (simTick - lastUpdatedSimTick) > dropThresholdTicks
```

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| EMCON Off → Active mid-tick | Applies next tick only |
| Friendly fire identification | Classified as friendly after IFF gate |
| Duplicate merges | Same observer+target → one contactId |
| Jammer destroyed | Strength drops next tick |
| Editor test contact | Uses runtime FSM, flagged `testOnly` |

## Dependencies

| System | Direction |
|--------|-----------|
| Policy / EMCON | Upstream |
| Platform DB | Upstream |
| Engagement | Downstream — needs track quality |
| Order Log | Downstream |
| Cyber/Comms (19) | P1 delayed sharing |

## Tuning Knobs

| Knob | Default |
|------|---------|
| `staleThresholdTicks` | 30 |
| `dropThresholdTicks` | 120 |
| `defaultPdFloor` | 0.01 |

## Acceptance Criteria

1. Two runs same seed → identical contact list at tick T.  
2. Sorted iteration order documented and tested (property test).  
3. EMCON Active required for active radar detection attempts.  
4. Every state change has `ContactChange` in order log.  
5. Agent intent on unit without track cites `NO_FIRE_CONTROL_TRACK` when applicable.

## TR IDs

| ID | Requirement |
|----|-------------|
| TR-sensor-001 | Contact FSM |
| TR-sensor-002 | Deterministic detection loop |
| TR-sensor-003 | EW noise jam MVP |
| TR-sensor-004 | Side picture / datalink sharing |

## GitNexus

No sensor symbols indexed yet — **greenfield** in `ProjectAegis.Sim.Sensors` (future). Re-run `npx gitnexus analyze` after implementation.

## Open Questions

1. Covariance ellipse vs radius for MVP UI.  
2. Classify/identify automatic vs player command.
