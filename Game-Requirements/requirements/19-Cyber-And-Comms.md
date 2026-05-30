# 19 - Cyber Warfare and Communications

**Last Updated:** May 29, 2026  
**Status:** Draft — ready for design review  
**CMO basis:** Manual §10.7 Comms Disruption & Cyber Attacks; links §9.2.6 EW  
**Related:** 15 Sensors (datalink), 13 Doctrine, 04 Delegation, 09 Near-Future, 11 Events

## Purpose

Define **communications degradation**, **cyber effects** on C2 and datalinks, and their impact on **player UI**, **agent attention**, and **scenario events** — bridging classic EW (doc 15) and near-future cognitive EW (doc 09).

## Vision

In 2030s warfare, the network is a battlespace. Jamming is not only “worse radar” — it delays orders, desynchronizes swarms, and forces agents to act on stale tracks. Effects are **scenario-controlled**, **deterministic**, and **explainable**.

## CMO Parity Requirements

| Capability | CMO §10.7 | Aegis |
|------------|-----------|-------|
| Comms disruption | Described in appendix | **P1** MVP: jamming + link delay |
| Cyber attacks | Scenario/event driven | **P1** |
| Integration with EW | §9.2.6 | **P0** coordination with doc 15 |

## Comms Model

### Link types

- **P0** Side-wide strategic net (orders, mission updates)
- **P0** Tactical datalink (track sharing — doc 15)
- **P1** Unit-local voice (flavor only unless degraded)
- **P1** Satellite / SATCOM with latency

### Link state

```yaml
linkId: NATO_TADIL_J
state: degraded  # up | degraded | down
degradation: { delayMs: 800, dropRate: 0.05, trackAgeMaxSec: 120 }
cause: { type: Jamming, sourceUnitId: RED_EW_01 }
untilSimTick: 190000
```

**P0** State changes logged in order log as `CommsStateChange`.

## Effects on Gameplay

| Effect | Player | Agent | Determinism |
|--------|--------|-------|-------------|
| Order delay | Commands queue; execute at `now+delay` | Same | Sorted queue by `(executeTick, orderId)` |
| Track staleness | Datalink contacts age faster | Agents see stale flag | **P0** |
| Partial picture | Missing subset of contacts | Configurable | **P0** |
| Swarm desync | — | Sub-swarms pause coordination | **P1** |
| Mission patch delay | Mission changes lag | **P1** |

**P0** **Delegation blind** (doc 15) can combine with comms degradation for hard scenarios.

## Cyber Attacks

Scenario-defined **cyber actions** (events doc 11 or special actions):

| Action | Example effect |
|--------|----------------|
| `DegradeLink` | Tactical datalink degraded 10 min |
| `SpoofTracks` | Insert false contacts (marked `spoofed` in debug) |
| `DenyFireControl` | FC radar offline until reboot event |
| `DelayOrders` | Side-wide +2s order delay |

**P1** Cyber actions require **trigger + conditions** (doc 11); no silent mid-game hacks.

**P0** Player briefing lists possible cyber effects in scenario (spoiler level configurable).

### Near-future (doc 09)

- **P1** Cognitive EW: rotating jam profiles
- **P2** AI-on-AI “cyber duels” between agent controllers (research mode)

## Agent Integration (doc 04)

- **P0** Agents receive `worldObservation` with `observationTimestamp` and `stale` flags
- **P0** **Electronic Warfare Specialist** prioritizes restore comms / EMCON tradeoffs
- **P1** **Attention budget** reduced under degraded comms (fewer units actively controlled)
- **P0** Agent rationale mentions comms (“engaging on stale track — caution”)

## Functional Requirements — UI

- **P1** Comms status indicator per side / task force
- **P1** Message log entries for link up/down/degraded
- **P0** Contact list shows stale / datalink vs organic icon

## MCP Tools

| Tool | Description |
|----------|-------------|
| `comms_get_state` | Link states for side |
| `comms_apply_effect` | Editor / event: degrade link |
| `cyber_trigger` | Fire scenario cyber action by id |

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Determinism | Delay queues processed in fixed order |
| Security | No arbitrary network I/O; cyber is sim-internal |
| Scope | No real-world hacking; fictional effects only |

## Acceptance Criteria

1. Jamming event degrades datalink; contacts age and expire per rules.
2. Player order arrives after deterministic delay; order log shows queue time.
3. Agent with degraded link logs stale-track warning before engage (Assisted).
4. Cyber action `DenyFireControl` blocks radar illuminate with `FireAbortReason` + comms cause.
5. Replay shows `CommsStateChange` at correct tick.
6. Headless run with cyber scenario matches golden comms timeline.

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | Datalink degrade, track staleness, order delay, event-driven |
| **Phase 2** | Cyber action library, spoof tracks (debug), agent attention |
| **Phase 3** | Cognitive EW profiles, research cyber duels |

## Open Questions

1. Player-visible spoof contacts or hidden until intel event?
2. Can player “reboot” FC network via special action?
3. Cross-side cyber in agent-vs-agent tournaments — symmetric rules?

## Traceability

| Doc | Relationship |
|-----|----------------|
| 15 | EW and datalink |
| 11 | Events / special actions |
| 04 | Agent observation |
| 09 | Cognitive EW |
| 17 | Comms log entries |
| `cmo-manual-traceability.md` | §10.7 |

---

**References:** CMO Manual §10.7; `docs/manual/index.html`
