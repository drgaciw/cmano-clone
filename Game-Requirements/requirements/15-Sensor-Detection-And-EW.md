# 15 - Sensor, Detection, and Electronic Warfare

**Last Updated:** 2026-07-08  
**Status:** Draft — ready for design review  
**FR reverse-ref:** [FR-13](01-Project-Overview.md) — Sensors, detection, EW  
**CMO basis:** Manual §3.3.10, §4.5.2, §6.3.8–9, §9.1.1, §9.2.6; appendix §10.7 (comms/EW overlap → doc 19)  
**Related:** 13 Doctrine/EMCON, 14 Engagement, 18 Combat Domains, 06 Database Intelligence, 17 Order Log  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 15 — **Partial (MVP COVERED)**  
**GDD:** [sensor-detection-ew.md](../../design/gdd/sensor-detection-ew.md)

## Purpose

Define the **sensor model**, **contact lifecycle**, **detection and classification**, **electronic warfare** interactions, and **fog-of-war** rules — including how **agents** perceive and explain the battlespace under EMCON and jamming.

Implements hub **[FR-13](01-Project-Overview.md)** (sensors, detection, EW).

## MVP COVERED vs full CMO P0 (honesty)

| Lane | Meaning | Status |
|------|---------|--------|
| **MVP COVERED** | Headless **deterministic detection loop**, **contact lifecycle FSM** (Unknown→Detected→Classified→Identified→Lost), **Pd-driven classify** promotions, **noise jam** via scenario jammers, Baltic/v3 classify + jam + datalink **policy slices**, order-log contact transitions on replay harness path | **Tracker grade:** Partial (**MVP COVERED**) — do not re-litigate S56 |
| **Partial residual** | `DatalinkSidePictureMerger` lag/comms completeness; **SensorC2** panel/HUD projection polish; full side-picture fidelity under degraded comms (doc 19) | Open polish / Phase 2 |
| **Not full CMO P0** | Full multi-band radar/sonar/EO physics, ECCM tables, coherent jamming, deception/false targets, MAD, satellite pass model, 5k×10k spatial broadphase at product FPS | **Phase N / Deferred** — historical CMO **P0** labels below are **product intent**, not “all shipped at CMO fidelity” |

**Rule of thumb:** “MVP COVERED” = Baltic vertical-slice sensor/EW spine is green under CI (detect/classify/jam/harness). “Full CMO P0” remains the north-star parity table — residual rows stay **Partial** or **Phase N**.

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

**Honesty overlay:** P0 rows above = product intent / CMO basis. Shipped MVP spine is **detect + lifecycle + Pd classify + noise jam + EMCON gate + policy fixtures**; multi-sensor physics depth and ECCM remain **Phase N**.

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

| MCP tool | Description | Honesty |
|----------|-------------|---------|
| `contact_list` | Contacts for side with filters | **Gap** — not shipped as product MCP verb |
| `contact_explain` | Why contact exists / classification level | **Gap** — headless explainability via lifecycle/tests only |
| `sensor_set_emcon` | Change EMCON (policy check doc 13) | **Gap** — EMCON via policy/scenario path; no dedicated MCP |

## Near-Future & Speculative (docs 09–10)

- **P1** Cognitive EW: time-varying jam profiles; logged as `EWProfileChange`
- **P1** Quantum sensor / low-observable modifiers — DB flags, same pipeline
- **P2** Swarm mesh sensing: aggregate detection from many small emitters

## Major IDs (SEN-*)

| ID | Summary | Priority / maturity |
|----|---------|---------------------|
| **SEN-01** | Deterministic detection tick — sorted trials, `SeededRng` Detection domain | **P0** — **Shipped** (`DeterministicDetectionLoop`) |
| **SEN-02** | Contact lifecycle FSM (`Unknown`→`Detected`→`Classified`→`Identified`→`Lost`) | **P0** — **Shipped** (`ContactLifecycleState`, `ContactTransition`) |
| **SEN-03** | Pd-driven detect + classify/identify promotions | **P0** — **Shipped** (`PdDetectionContactSimulator`; evidence `PdContactClassifyTests`) |
| **SEN-04** | Scenario noise jamming on detection trials | **P0** — **Shipped** (`ScenarioJamResolver`, `ScenarioJammer`) |
| **SEN-05** | Side-picture merge / datalink share | **P0** — **Partial** (`DatalinkSidePictureMerger`; lag/comms slices Partial) |
| **SEN-06** | Sensor C2 projection / panel / bridge | **P0** — **Partial** (`SensorC2Projection`, `SensorC2PanelBinder`, `SensorC2Bridge`) |
| **SEN-07** | EMCON-gated active detection (radar off → no active returns) | **P0** — **Shipped spine** (EMCON on Pd sim path; policy doc 13) |
| **SEN-08** | MCP `contact_*` / `sensor_set_emcon` product tools | **P0 intent** — **Gap** (Phase N / residual) |

## Non-Functional Requirements

| Area | Target | Honesty |
|------|--------|---------|
| Performance | 5k emitters × 10k targets budgeted via spatial broadphase | **Deferred** — not a Release CI gate; north-star scale (hub OV-SC-N1). Baltic-scale loops measured via harness, not 5k×10k |
| Determinism | Golden contact timeline per seed | **Shipped spine** — sorted trials + seeded rolls |
| Scale | Contact merge stable at 1000+ active tracks | **Partial / unmeasured at 1k+** — merge exists; scale not CI-gated |

## Acceptance Criteria

1. Two runs, same seed → identical contact ids and state transition times.
2. EMCON radar off → no active radar detections; ESM still legal.
3. Jamming reduces detection rate measurably in scripted test scenario.
4. Player fog hides unidentified contacts beyond organic range; agents respect same when configured.
5. Editor “test contacts” layer uses production detection code path.
6. `contact_explain` returns sensor ids and EMCON state for a track. **(Residual / Gap — MCP not shipped; headless lifecycle evidence covers transitions.)**

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP (COVERED)** | Core sensors path, contact FSM, Pd classify, side-picture merge spine, EMCON gate, basic noise jamming, Baltic/v3 classify + jam policies |
| **Phase 2** | Datalink delay polish, delegation blind, ECCM, decoys, SensorC2 UI completeness |
| **Phase 3 / Phase N** | Near-future EW profiles, swarm mesh, full multi-band physics, **5k×10k broadphase**, MCP contact tools |

## Implementation Mapping (headless)

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| Deterministic detection loop | `DeterministicDetectionLoop` (`ProjectAegis.Sim` · `Sensors/`) | **Shipped** | `DeterministicDetectionLoopTests`; sorted `(observer, sensor, target)` trials |
| Contact lifecycle states | `ContactLifecycleState`, `ContactTransition` | **Shipped** | Enum FSM; used by Pd sim + datalink merger |
| Pd detect / classify | `PdDetectionContactSimulator`, `ScenarioContactLifecycle`, `DetectionTrialResolver` | **Shipped** | `PdContactClassifyTests`, `PdDetectionContactSimulatorTests`; policies `baltic-patrol-classify`, `baltic-v3-classify` |
| Noise jam | `ScenarioJamResolver`, `ScenarioJammer` | **Shipped** | `ScenarioJamResolverTests`; `baltic-patrol-jammed`, `baltic-v2-jammed`; `BalticReplayHarnessJamTests` |
| Datalink side picture | `DatalinkSidePictureMerger`, `DatalinkShareLagResolver`, `ScenarioDatalinkDoctrine` | **Partial** | `DatalinkSidePictureMergerTests`, `DatalinkShareLagResolverTests`; `baltic-patrol-datalink*`; lag/comms completeness residual |
| Sensor C2 | `SensorC2Projection`, `SensorC2PanelBinder`, `SensorC2Bridge`, `SensorC2PanelHost` | **Partial** | `SensorC2BridgeTests`, `SensorC2PanelBinderTests`; Unity hosts present — full CMO contact-list chrome residual |
| Catalog detection modifiers | `PhaseBCatalogDetectionModifier`, sensor catalog slices | **Partial** | Catalog sensor bindings / Baltic sensor JSON |
| MCP contact / EMCON tools | — | **Gap** | Spec tools `contact_list`, `contact_explain`, `sensor_set_emcon` not product MCP verbs |
| 5k emitter × 10k target broadphase | — | **Deferred** | Performance NFR; not Release gate |

**Honesty note:** Design Status remains **Draft** (Template B). Tracker **Partial (MVP COVERED)** = headless detect/lifecycle/classify/jam spine + Baltic fixtures. Full CMO multi-sensor P0 fidelity, ECCM, MCP, and 5k-scale perf are **not** claimed Shipped.

## Open Questions

1. Contact merge when two sensors disagree on position — weighted average vs primary sensor?
2. Classification degradation over time without re-detect?
3. Satellite pass modeling depth for v1 (doc 11 satellites)?

## Traceability

| Doc | Relationship |
|-----|----------------|
| Hub **FR-13** ([01](01-Project-Overview.md)) | Sensors, detection, EW — this doc |
| 13 | EMCON gates emission |
| 14 | Identification for engage |
| 18 | Domain-specific sensing |
| 19 | Comms degradation of datalink |
| 17 | ContactChange log |
| `cmo-manual-traceability.md` | §3.3.10, §9.1.1, §9.2.6 |

---

**Implementation grade:** Partial (MVP COVERED) — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 15.  
Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.

**References:** CMO Manual §9.1.1, §9.2.6; `docs/manual/index.html`
