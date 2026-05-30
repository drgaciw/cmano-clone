# Engagement & Fire Control

> **Status:** In Review  
> **Last Updated:** 2026-05-29  
> **Requirements:** [14-Engagement-And-Fire-Control.md](../../Game-Requirements/requirements/14-Engagement-And-Fire-Control.md)  
> **Depends on:** [policy-roe-emcon-wra.md](policy-roe-emcon-wra.md), [sensor-detection-ew.md](sensor-detection-ew.md), [order-log-replay.md](order-log-replay.md)

> **Quick reference** — Layer: **Core** · Priority: **MVP** · System #6

## Summary

One **engagement resolver** handles player clicks, agent intents, and mission auto-fire. Policy runs first; geometry and magazines second; every abort is logged.

## Overview

`IEngagementResolver` in `ProjectAegis.Sim` executes step 8 of the tick pipeline. Inputs: `EngageIntent` + contact + mount; outputs: launches, magazine deltas, `Engagement` order-log rows.

## Player Fantasy

You pick a weapon and see why it will or will not fire before you commit—and when an AI wingman holds fire, you get the same staff answer you would.

## Detailed Design — Pipeline

```
Intent → IPolicyEvaluator → target valid → mount ready → DLZ/envelope → deconflict → launch → log
```

| Step | Fail reason (examples) |
|------|------------------------|
| Policy | `RoeHoldFire`, `WraSalvo`, `EmconOff` |
| Target | `NoFireControlTrack`, wrong side |
| Mount | empty magazine, cooldown |
| Geometry | `WraRange`, DLZ out |
| Deconflict | friendly fire block |

### Intent sources

| Source | Log tag |
|--------|---------|
| Player | `PlayerOrder` / engage variant |
| Agent | `AgentIntent` → pipeline |
| Mission | `intentSource: mission` |

### DLZ (MVP)

State per `(shooter, target, weapon)`: `InZone`, `Approaching`, `OutOfZone`, `Unknown`. Preview in UI; log on abort.

### Swarm (Aegis)

**P1:** Salvo coordinator assigns slots across swarm shooters; **P0:** sequential deterministic slot order by `shooterId`.

## Formulas

```
inEnvelope = range >= minRange && range <= maxRange && aspectOk(aspect)
dlzState = f(inEnvelope, closingRate, weapon.dlzTable)
```

## Edge Cases

| Case | Behavior |
|------|----------|
| Policy pass, DLZ fail | Log `DLZ_OUT`, no launch |
| Agent engage without track | `NoFireControlTrack` |
| Mid-flight target lost | Miss / abort per weapon rules |

## Acceptance Criteria

1. Manual and agent paths share resolver (same seed → same launches).  
2. Every abort has `FireAbortReason` + order log row.  
3. DLZ preview matches resolver at commit tick.

## TR IDs

| ID | Requirement |
|----|-------------|
| TR-engage-001 | Unified resolver |
| TR-engage-002 | DLZ state + logging |
| TR-engage-003 | Swarm slot order (P1) |

## GitNexus

`Order.Engage` in Delegation — impact before extending enum; wire after `IPolicyEvaluator` migration.
