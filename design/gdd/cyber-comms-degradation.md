# Cyber & Comms Degradation

> **Status:** Draft — Sprint 7  
> **Last Updated:** 2026-06-02  
> **Requirements:** [19-Cyber-And-Comms-Degradation.md](../../Game-Requirements/requirements/19-Cyber-And-Comms-Degradation.md)  
> **Depends on:** [sensor-detection-ew.md](sensor-detection-ew.md), [order-log-replay.md](order-log-replay.md), [command-and-control-ui.md](command-and-control-ui.md)

## Overview

Cyber and communications degradation models **contested C2**: datalink latency, jamming, spoofing, and node isolation affect what the player sees and what agents can execute—without breaking deterministic replay. All effects emit order-log rows so AAR and message log stay authoritative.

## Player Fantasy

Your picture degrades when the enemy jams or hacks—you still command, but tracks stutter, mission updates arrive late, and autonomous units may act on stale ROE until comms restore.

## Detailed Rules

### Comms states (P0 vertical slice)

| State | Effect on UI | Effect on agents |
|-------|----------------|------------------|
| `Nominal` | Full refresh rate | Normal delegation tick |
| `Degraded` | Contact positions lag 1–N ticks | Intent queue holds fire unless ROE `WeaponsFree` |
| `Denied` | Message log only; map symbology frozen | Agents hold last lawful intent |

Transitions driven by scenario events (doc 11) or EW outcomes from sensor GDD.

### Cyber actions (P1)

- `Probe`, `Exploit`, `Disrupt` against comms nodes—success rolls logged with seed.
- No real-network I/O; all scripted for replay.

### Order log (P0)

New entry kinds: `CommsStateChange`, `CyberActionResult` with `{nodeId, fromState, toState, reason, sequenceId}`.

### C2 UI (P0 presentation)

- Top bar `COMMS:` indicator (green/amber/red).
- Message log `COMMS` category lines.
- Map placeholder: ghost symbology when `Degraded` (P1 — shipped: `map-symbol--ghost`, scenario `commsDisplay`).

## Formulas

**Degraded display lag**

```
displayTick = simTick - lagTicks
lagTicks = scenario.commsDegradedLag  // default 2, range 1–10
```

**Jam duty cycle (P1)**

```
jamActive = (simTick % jamPeriod) < jamOnTicks
Pd_eff = Pd_base * (jamActive ? jamMultiplier : 1.0)
```

| Variable | Default | Range |
|----------|---------|-------|
| `lagTicks` | 2 | 1–10 |
| `jamMultiplier` | 0.5 | 0.1–1.0 |

## Edge Cases

- Comms `Denied` during engagement: in-flight shots complete; new engages blocked until `Nominal`.
- Checkpoint restore mid-degrade: comms state restored from order log, not UI cache.
- Friendly jamming own side: scenario flag `jamAffectsFriendly` default false.

## Dependencies

- **Sensor & Contact Model** — EW/jammer hooks.  
- **Order Log & Replay** — new entry kinds and fingerprint segments.  
- **Command & Control UI** — indicators and message categories.  
- **Agent Delegation** — hold/fire on stale picture.

## Tuning Knobs

| Knob | Affects | Safe range |
|------|---------|------------|
| `lagTicks` | UI staleness vs readability | 1–5 for MVP |
| `jamMultiplier` | Detection difficulty | 0.3–0.8 |
| Degrade duration | Scenario pacing | 30s–10 min sim time |

## Acceptance Criteria

- [x] `CommsStateChange` rows appear in replay fingerprint (`baltic-patrol-comms` scenario).
- [x] C2 top bar shows comms state via `CommsStateProjection` (order log only).
- [x] New engages blocked while `Denied` (`FireAbortReason.CommsDenied`, headless test).
- [x] Message log lists comms transitions with `sequenceId` traceability.