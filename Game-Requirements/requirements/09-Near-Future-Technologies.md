# 09 - Near-Future Technologies

**Last Updated:** May 29, 2026  
**Research basis:** [Near-Future Tech Research Supplement](../../docs/research/near-future-tech-research.md)

## Purpose

Define the core set of near-future (2028–2035) military technologies that form the backbone of gameplay systems, sensors, weapons, and platforms in the simulation. All entries must have verifiable real-world counterparts at TRL 5–9.

## Vision

A rich, believable technology base grounded in current open-source intelligence and program status, enabling credible mechanics around drone swarms, collaborative combat aircraft, hypersonics, directed energy, cognitive EW, and AI sensor fusion — with explicit performance envelopes and trade-offs.

## Technology Level Framework

Scenarios bind entities to a **Technology Level (TL)** that gates availability:

| TL | Era | Scope |
|----|-----|-------|
| **TL-0** | 2025 baseline | Current fielded systems only |
| **TL-1** | 2026–2028 | Early fielding (Dark Eagle, HELIOS, Replicator attritables) |
| **TL-2** | 2028–2030 | Primary near-future release target (CCA fleet, Orca XLUUV, HACM) |
| **TL-3** | 2030–2032 | Advanced near-future (HELCAP, quantum sensing doctrine, railgun) |

Speculative systems beyond TL-3 belong in [10-Speculative-Systems.md](10-Speculative-Systems.md).

## Core Technology Categories & Gameplay Implications

### 1. Unmanned & Autonomous Systems

#### Loyal Wingman UAVs / Collaborative Combat Aircraft (CCA)

**Real-world anchor:** YFQ-42A (GA), YFQ-44A Fury (Anduril), MQ-28 Ghost Bat, XQ-58A Valkyrie; USAF production decision FY2026; F-47 pairing early 2030s.

- Two archetypes: **attritable strike** (lower cost, shorter range, expendable) and **persistent ISR** (higher cost, longer dwell).
- Autonomy modes: `WINGMAN` (human-supervised), `AUTONOMOUS_STRIKE` (ROE-bound), `ESCORT_ONLY` (defensive screen). Assign per mission phase.
- Failure modes: RF link degradation, GPS spoofing, adversarial AI targeting confusion.
- F-47 + CCA pairing unlocks `Quarterback` bonus: dynamic mid-engagement swarm tasking at reduced cognitive cost.

**Baseline parameters (YFQ-44A Fury):**

| Parameter | Value |
|-----------|-------|
| Speed | Mach 0.95 |
| Combat radius | ~1,000 km |
| Payload | ~1,000 lb internal |
| Unit cost | $3–8M (attritable tier) |
| Autonomy | SAL-2 (human-on-the-loop) |

#### Drone Swarms / CCA Swarms

**Real-world anchor:** Replicator initiative (hundreds delivered); Replicator 2.0 high-volume production; Saab 100-UAS swarm control (Arctic Strike 2025).

- Swarm size tiers: `MICRO` (5–50), `MEDIUM` (50–500), `MASS` (500–5,000). **v1.0 supports MEDIUM (max 500 entities per swarm)**; MASS tier is TL-3 / expansion content.
- Behaviors: `SATURATION_ATTACK`, `DISTRIBUTED_ISR`, `SACRIFICIAL_SCREEN`, `LOITERING_STRIKE`, `JAMMING_CLOUD`.
- Attrition modeling: swarms accept 40–60% attrition before mission failure.
- Counter-swarm: high-power microwave (THOR/Leonidas), laser CIWS, directed EW, CCA-vs-swarm intercept.

#### Replicator-Class Attritable Drone *(new — gap from research)*

- Sub-$50K expendable UAS representing USAF "affordable mass" doctrine.
- Distinct from full CCA platforms: shorter range, minimal autonomy, high volume.
- **P1** for v1.0 content; required for credible saturation scenarios.

#### Autonomous Underwater Vehicles (AUVs)

**Real-world anchor:** Boeing Orca XLUUV; HSU-001 (China).

- Mission profiles: `PERSISTENT_ISR` (30–90 day endurance), `MINE_LAYING`, `TORPEDO_DELIVERY`, `COMMS_RELAY`.
- **Undersea Fog of War:** low Pd until fixed arrays (SOSUS successor) or quantum magnetometer detect magnetic signature.
- Quantum gravimeter/magnetometer upgrades create cat-and-mouse detection loop with AUV stealth.

---

### 2. Advanced Weapons

#### Hypersonic Weapons

**Real-world anchor:** LRHW Dark Eagle (Mach 17, ~2,800 km), HACM (air-launched), DF-ZF, Avangard.

- Two types: **Boost-Glide** (ballistic ascent + unpowered glide, TBM-like signature) and **Hypersonic Cruise Missile** (air-breathing, lower altitude).
- Intercept windows: <120 s reaction for boost-glide; near-zero for Avangard-class. Only THAAD+ / SM-3 Block IIA have meaningful intercept probability.
- Game state: `HYPERSONIC_ALERT` — tension clock with limited decision branches on enemy launch detection.

#### Hypersonic Defense Layer *(new — gap from research)*

- THAAD+, SM-3 Block IIA as intercept assets for 2028–2032 defense balance.
- **P0** — without this layer, hypersonic offense dominates gameplay.

#### Directed Energy Weapons (DEW)

**Real-world anchor:** ODIN (8 DDGs), HELIOS 60 kW (USS Preble), Iron Beam 100 kW, DragonFire 50 kW; HELCAP 300 kW pipeline.

- **Model thermal and power constraints in detail** — this is a core balancing mechanic.
- Continuous power draw with thermal accumulation; sustained fire → `THERMAL_LIMIT` degraded output.
- Atmospheric penalties: rain/fog reduce range 30–70%.
- Power tiers: `DAZZLE` (0–10 kW), `DISABLE` (10–100 kW), `DESTROY` (100–500 kW).
- Ship integration: DEW competes with radar, propulsion — tactical power management.
- Anti-drone role credible at TL-1; anti-missile role TL-3+.

#### Counter-Drone Systems (C-UAS) *(new — gap from research)*

- THOR (HPM), Leonidas, L-MADIS (USMC), DroneHunter F700 (Replicator 2).
- **P1** — required counterpart to swarm offense.

#### Electromagnetic Railguns

**Real-world anchor:** 32 MJ system, White Sands live-fire Feb 2025; ~185 km range, Mach 7.5.

- Capacitor charge time 30–60 s between full-power shots.
- Full-power shot → `REDUCED_SHIP_SPEED` + `RADAR_DEGRADED` for 5–10 s.
- HVP dual-purpose: surface strike, shore bombardment, anti-air vs drones/cruise missiles.
- **TL-3 only** — not in 2025 baseline scenarios.

---

### 3. Sensors & Detection

#### Quantum Sensors

**Real-world anchor:** DIA 2025 Threat Assessment; quantum magnetometers (submarine detection); hybrid quantum-classical radar.

- **Abstract as doctrine unlock in v1.0:** `QUANTUM_SENSING_DOCTRINE` reduces LO platform stealth 20–40% force-wide.
- Individual quantum sensor unit modeling at **TL-3+**.
- Counter-measures: real-time degaussing, decoy signatures, noise injection.
- Scenario toggle: `QUANTUM_SENSING_ENABLED`.

#### AI-Enhanced Sensor Fusion / JADC2

**Real-world anchor:** Capstone 2025; Project Maven; JADC2 sensor-shooter network.

- **JADC2 / C2 Network Node** *(new entity archetype)* — contested resource; destruction or jamming degrades force-wide effectiveness.
- Composite Air Picture (CAP): quality degrades under EW, attrition, or network disruption.
- Latency: fused picture 2–10 s update; EW disruption → 30–120 s or ghost tracks.
- `AUTOMATIC_CUEING` upgrade: system assigns intercept assets without micromanagement.
- Primary target for autonomous cyber weapons (see §4).

#### Undersea Fixed Sensor Arrays *(new — gap from research)*

- SOSUS-successor distributed acoustic/quantum detection grids.
- **P1** — counter-AUV gameplay loop.

#### Space Domain Awareness *(limited — decision resolved)*

- **Include limited SDA from v1.0:** satellite ISR degradation and ASAT threat as escalation-tier events, not base-layer orbital mechanics.
- Full cislunar operations → [10-Speculative-Systems.md](10-Speculative-Systems.md) TL-4+.

---

### 4. Electronic Warfare & Cyber

#### Cognitive Electronic Warfare (CEW)

**Real-world anchor:** DARPA ARC, BLADE (BAE Systems).

- Learning curve: first encounter with unknown waveform 3–5 s characterization; subsequent near-instant.
- `EW_DUEL` mechanic: dynamic waveform competition when both sides have CEW.
- Quantum-enhanced radar (TL-3+) can render current CEW moot unless CEW also uses quantum processing.

#### Autonomous Cyber Weapons

**Real-world anchor:** AI-driven adaptive malware; Pentagon AI integration creates new attack surfaces.

- `PROPAGATION` mechanic: T+0 injection → T+30s primary degraded → T+90s secondary infected → T+180s network collapse.
- Defense layers: air-gapped backup C2, intrusion-detection AI, network segmentation (each adds resistance, reduces integration benefit).
- `ACCOUNTABILITY_GAP` narrative event: unintended civilian infrastructure damage → escalation meter increase.

#### Directed Energy Countermeasures

- Laser dazzling and HPM vs seekers/sensors — non-kinetic disablement.

---

### 5. Stealth & Survivability

#### Signature Management Subsystem *(new — from research)*

Each platform tracks independently:

- `RADAR_SIGNATURE`, `IR_SIGNATURE`, `ACOUSTIC_SIGNATURE`, `VISUAL_SIGNATURE`

Adaptive systems reduce one or two signatures at cost of power draw and heat (paradoxically raising IR).

#### Adaptive Camouflage & Metamaterials

- HT4 thermal cloaking: credible **TL-1** infantry item.
- Multi-spectral platform camouflage: **TL-3+**.
- Metamaterial coatings degrade with combat damage — maintenance modeling.

#### Next-Generation Low-Observable Coatings

- Broadband RAM in active R&D; underwater tiles for submarines (DRDO/US programs, Feb 2025).
- AI-driven mid-mission signature reconfiguration: **TL-3**.

---

## Performance Envelope Reference

| System | Speed | Range | TRL (2026) | Game TL |
|--------|-------|-------|------------|---------|
| YFQ-44A Fury CCA | Mach 0.95 | ~1,000 km combat radius | 7–8 | TL-2 |
| XQ-58A Valkyrie | Mach 0.95 | 5,600 km | 8 | TL-1 |
| LRHW Dark Eagle | Mach 17 peak | ~2,800 km | 7–8 | TL-1 |
| HACM | Mach 5+ | ~1,000 km | 6–7 | TL-2 |
| HELIOS 60 kW | c | ~2–5 km effective | 8 | TL-1 |
| HELCAP 300 kW | c | ~10–20 km (clear sky) | 5–6 | TL-3 |
| Orca XLUUV | ~8 kn | 6,500 km | 6–7 | TL-2 |
| 32 MJ Railgun | Mach 7.5 | 185 km | 5–6 | TL-3 |

---

## Non-Functional Requirements

- All systems must have realistic performance envelopes (range, speed, endurance, signature, cost).
- Clear trade-offs between capability, cost, and vulnerability.
- Support TL-0 through TL-3 within the same scenario engine via entity gating.
- Each system entry in the database must cite TRL and research source (see doc 06 provenance).
- Swarm simulation must remain deterministic at max v1.0 size (500 per swarm) under ADR-004 tick ordering.

---

## Agentic Capabilities

- **Dynamic Speculative Systems Agent** proposes new variants from open-source intelligence — routed through Database Intelligence Layer (propose, not auto-merge).
- Unity-MCP tools: create entity archetypes, tune parameters, run balance batches against existing systems.

---

## Technical Considerations

- Modular ECS components (ADR-005); deterministic sim for agent-vs-agent and replay (req 17).
- Sensor and weapon models support both deterministic batch and real-time interactive play.
- Clear data schemas for Database Intelligence Layer with temporal validity windows per variant.

---

## Resolved Decisions (May 29, 2026)

| Question | Decision | Rationale |
|----------|----------|-----------|
| Max swarm size v1.0? | **500 per swarm (MEDIUM tier)** | Matches Replicator doctrine; 2,000+ at TL-3 / expansion |
| DEW thermal/power detail? | **Yes — full modeling** | Real systems (HELIOS, Iron Beam) require it for balance |
| Quantum sensor abstraction? | **Doctrine unlock v1.0; unit-level at TL-3+** | DIA confirms near-term relevance; full sim premature |
| Space domain from day one? | **Limited SDA + ASAT escalation events** | Cislunar is 2035+ scope (doc 10) |

---

## Traceability

| Research section | Requirement section |
|------------------|---------------------|
| §1 Unmanned & Autonomous | §1 above |
| §2 Advanced Weapons | §2 above |
| §3 Sensors & Detection | §3 above |
| §4 EW & Cyber | §4 above |
| §5 Stealth | §5 above |
| Gap analysis (5 items) | New entities marked *(new)* |
| Open questions table | Resolved Decisions |

---

**Status:** Research-integrated — ready for GDD authoring and database schema design
