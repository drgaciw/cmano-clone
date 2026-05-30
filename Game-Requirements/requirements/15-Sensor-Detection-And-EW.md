# 15 - Sensor, Detection, and Electronic Warfare

**Last Updated:** May 29, 2026  
**Status:** Draft — ready for design review  
**CMO basis:** Manual §3.3.10, §4.5.2, §6.3.8–9, §9.1.1, §9.2.6; appendix §10.7 (comms/EW overlap → doc 19)  
**Related:** 13 Doctrine/EMCON, 14 Engagement, 18 Combat Domains, 06 Database Intelligence, 17 Order Log

## Purpose

Define the **sensor model**, **contact lifecycle**, **detection and classification**, **electronic warfare** interactions, and **fog-of-war** rules — including how **agents** perceive and explain the battlespace under EMCON and jamming.

## Vision

Awareness is the core resource in theater command. Detection must be **physically plausible**, **deterministic**, and **legible**: players and agents see the same contact objects, with optional player-side fog layers. Every new track and every lost track is an order-log event researchers can replay.

## CMO Parity Requirements

| Capability | CMO | Aegis |
|------------|-----|-------|
| Sensor on/off, active/passive | §3.3.10 | **P0** |
| Side/unit EMCON | §3.3.14, §13 | **P0** |
| Contact list and filtering | §6.3.8 | **P0** |
| Radar, sonar, EO/IR, ESM | §9.1.1 | **P0** |
| EW / jamming effects | §9.2.6 | **P0** |
| Editor test contacts | §11 layers | **P0** — same model as runtime |

## Contact Lifecycle

**P0** States (forward-only unless event rewinds in editor test):

```
Unknown → Detected → Classified → Identified → Lost
```

| State | Meaning |
|-------|---------|
| Detected | Bearing/range/altitude estimate exists |
| Classified | Platform category (e.g., fighter, merchant) |
| Identified | Specific unit type or name |
| Lost | No longer held; decay timer before drop from list |

**P0** Each contact has:

- `contactId`, `sideId` (observer), `sourceSensorIds[]`
- Position uncertainty (ellipse or covariance — simplified to radius for MVP UI)
- `lastUpdatedSimTick`, `stale` flag
- **Provenance:** which sensors contributed (for explainability)

**P0** `ContactChange` entries in order log (doc 17): detect, update, classify, identify, lost.

## Sensor Model

### Sensor classes

- **P0** Radar (air/surface), sonar (active/passive), EO/IR, ESM, MAD (P2), datalink-fed tracks
- **P0** Mount-bound sensors respect damage, EMCON, and crew readiness
- **P0** Parameters from DB (doc 06): frequency band, power, aperture, processing gain, environment modifiers

### Detection tick (deterministic)

Fixed evaluation order per tick:

1. Build emitter list from EMCON-active sensors (doc 13)  
2. Apply environment (weather, terrain mask, sea state)  
3. Compute detection attempts sorted by `(observerId, sensorId, targetId)`  
4. Apply EW degradation (jamming, decoys — §EW below)  
5. Merge returns into contacts (stable merge rules)  
6. Emit contact changes  

**P0** No wall-clock or unordered iteration in detection loop.

### Datalinks and sharing

- **P0** Tracks shared within side per datalink doctrine (group/side picture)
- **P1** Delayed or partial sharing (degraded comms — doc 19)
- **P0** Agent sees **side picture** unless scenario restricts to organic sensors only

## Electronic Warfare

### Offense

- **P0** Noise jamming reduces detection probability / range
- **P0** Deception / false targets (P1 full; MVP: range gate noise)
- **P1** Coherent jamming vs specific radars (DB-tagged)

### Defense

- **P0** ESM detects emitters; links to contacts without active own-ship radar
- **P1** Onboard ECCM flags from DB reduce jam effectiveness

### Agent integration

- **Electronic Warfare Specialist** personality weights jamming vs kinetic fires
- **P0** Agents cite EW state in intent rationale (“hold fire — no fire control track”)

## Fog of War

| Mode | Player view | Agent view | Use |
|------|-------------|------------|-----|
| **Full picture** | All side contacts | Same | Training, editor |
| **Side realistic** | Organic + datalink as configured | Same | Default competitive |
| **Delegation blind** | Subset | Full side picture optional | Hard mode; agents “know” more than player if enabled |

**P0** Fog mode is scenario feature flag (doc 11).  
**P0** UI clearly labels fog mode in briefing.

## Functional Requirements — UI & MCP

- **P0** Contact list: sort by threat, range, age, domain
- **P0** Map symbology: hostile/unknown/neutral, air/surface/sub, group markers
- **P0** Sensor panel: per-mount state, EMCON, last detection
- **P0** Hover: contributing sensors, classification confidence
- **P1** **Sensor fan / range ring** toggle (performance LOD)

| MCP tool | Description |
|----------|-------------|
| `contact_list` | Contacts for side with filters |
| `contact_explain` | Why contact exists / classification level |
| `sensor_set_emcon` | Change EMCON (policy check doc 13) |

## Near-Future & Speculative (docs 09–10)

- **P1** Cognitive EW: time-varying jam profiles; logged as `EWProfileChange`
- **P1** Quantum sensor / low-observable modifiers — DB flags, same pipeline
- **P2** Swarm mesh sensing: aggregate detection from many small emitters

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Performance | 5k emitters × 10k targets budgeted via spatial broadphase |
| Determinism | Golden contact timeline per seed |
| Scale | Contact merge stable at 1000+ active tracks |

## Acceptance Criteria

1. Two runs, same seed → identical contact ids and state transition times.
2. EMCON radar off → no active radar detections; ESM still legal.
3. Jamming reduces detection rate measurably in scripted test scenario.
4. Player fog hides unidentified contacts beyond organic range; agents respect same when configured.
5. Editor “test contacts” layer uses production detection code path.
6. `contact_explain` returns sensor ids and EMCON state for a track.

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | Core sensors, contact FSM, side picture, EMCON, basic jamming |
| **Phase 2** | Datalink delay, delegation blind, ECCM, decoys |
| **Phase 3** | Near-future EW profiles, swarm mesh |

## Open Questions

1. Contact merge when two sensors disagree on position — weighted average vs primary sensor?
2. Classification degradation over time without re-detect?
3. Satellite pass modeling depth for v1 (doc 11 satellites)?

## Traceability

| Doc | Relationship |
|-----|----------------|
| 13 | EMCON gates emission |
| 14 | Identification for engage |
| 18 | Domain-specific sensing |
| 19 | Comms degradation of datalink |
| 17 | ContactChange log |
| `cmo-manual-traceability.md` | §3.3.10, §9.1.1, §9.2.6 |

---

**References:** CMO Manual §9.1.1, §9.2.6; `docs/manual/index.html`
