# 10 - Speculative & Black Project Systems

**Last Updated:** May 29, 2026  
**Research basis:** [Speculative Systems Research Supplement](../../docs/research/speculative-systems-research.md)

## Purpose

Define highly speculative, theoretical, and black-project-level military systems grounded in open-source intelligence, research papers, and defense assessments (2030–2045+). These systems are optional content gated by **Technology Level** and intended for Future Combat Mode, campaign progression, and R&D sandbox scenarios.

## Vision

Push believable near-future warfare into the speculative realm while maintaining hardcore simulation credibility. Every system must have documented scientific plausibility, a TRL assessment, and escalation consequences — "cool factor" comes from research grounding, not fiction.

## Technology Level Framework (Extended)

| TL | Era | Content |
|----|-----|---------|
| **TL-0–TL-2** | Near-future | See [09-Near-Future-Technologies.md](09-Near-Future-Technologies.md) |
| **TL-3** | 2030–2035 | Quantum radar hybrid, full-agency LAWS debate, drone hives, BCI command, predictive warfare AI |
| **TL-4** | 2035–2040 | Orbital DEW, cislunar ops, combat exoskeleton, nuclear space tugs |
| **TL-5** | 2040+ | Particle beams (space-only), NPX lasers, lunar weapons — **Black Project / Future War mode only** |

**Normal campaign runs TL-0 to TL-2.** Speculative systems unlock at TL-3+; TL-5 requires explicit scenario flag `BLACK_PROJECT_MODE`.

### Migration note

Base hypersonic MaRV systems (Avangard, DF-ZF, Dark Eagle) are **not speculative** — they belong in doc 09. This document covers **next-generation variants** (multi-warhead maneuverable HGV, hypersonic saturation packages) and systems beyond near-future fielding.

---

## Core Speculative Systems & Gameplay Implications

### 1. Advanced Directed Energy & Exotic Weapons

#### Space-Based Directed Energy Platforms

**TRL:** 3–4 | **Deployment:** 2035–2040 (TL-4)

- Requires launch event (detectable), orbital insertion (3–7 day build-up), continuous power (RTG or solar — signature implications).
- **Revisit time** based on orbital mechanics — not always overhead.
- Counter-space: adversary attempts kinetic or DE-ASAT on first fire (reveals position).
- Escalation: first use triggers `SPACE_WAR_THRESHOLD` event.
- Damage: 10–30 s dwell vs hardened targets; shorter vs thin-skinned assets.

#### Plasma and Particle Beam Weapons

**TRL:** 2–3 | **Space domain only**

- Atmospheric use excluded — beam dispersion in ionized air breaks simulation credibility.
- Space combat: infinite ammunition, 60–120 s charge cycle between shots.
- Atmospheric plasma as **area denial** (sensor disruption 2–5 min) — not direct kill weapon.
- In-game `TECH_NOTE`: atmospheric particle beams theoretical as of 2035.

#### Nuclear-Pumped X-Ray Lasers

**TRL:** 3 | **TL-5 / Black Project only**

- Ultimate single-use strategic weapon; triggers `NUCLEAR_USE_THRESHOLD`.
- Kessler cascade risk if orbital debris generated.
- Not accessible in standard scenarios.

---

### 2. Quantum & Post-Stealth Technologies

#### Operational Quantum Radar (Hybrid)

**TRL:** 5–7 sensing; 3–5 operational radar | **TL-3**

- `POST_STEALTH_ENVIRONMENT` scenario flag (2033+): F-35/B-21 stealth −40–60%; submarines detectable within 20 km by magnetometer arrays.
- Tiered vulnerability — not universal "stealth is dead."
- Counter-quantum kit: `DEGAUSSING_LOOP`, `SIGNATURE_NOISE_INJECTION`, `QUANTUM_DECOY_EMITTER`.
- Progression pacing: ~one quantum modality per year on operational platform (CSIS recommendation).

#### Quantum-Enhanced Electronic Warfare

**TRL:** 3–5 | **TL-3**

- `QUANTUM_EW_ADVANTAGE` doctrine: wins automatic `WAVEFORM_CONTEST`.
- Post-quantum cryptography (PQC) for C2: without PQC upgrade, adversary with quantum computing decodes orders with ~30 s delay.

#### Post-Quantum Cryptography Arms Race *(new — gap from research)*

- Not a weapon — strategic layer. Side without PQC-encrypted C2 is vulnerable to quantum decryption.
- Active technology race mechanic tied to cyber domain (req 19).

#### Metamaterial Adaptive Camouflage

**TRL:** 7–8 thermal (infantry); 4–5 broadband platform | **TL-3–TL-4**

- Four tracks: `RADAR_CLOAK`, `IR_CLOAK`, `VISUAL_CLOAK`, `ACOUSTIC_CLOAK`.
- Power budget: choose 2–3 active simultaneously; all four requires advanced nuclear power.
- `SHIMMER_EFFECT`: 5–15% detection probability while moving.

---

### 3. Autonomous & Cognitive Weapon Systems

#### LAWS with Full Agency

**TRL:** 8–9 counter-drone/missile; 4–6 anti-personnel | **TL-3**

ROE delegation modes (aligns with req 13):

| Mode | Behavior | Risk |
|------|----------|------|
| `HUMAN_IN_LOOP` | Every engagement requires confirmation | Slowest, most controlled |
| `HUMAN_ON_LOOP` | AI selects; player override within 10 s | Balanced |
| `FULL_AUTONOMOUS` | AI engages independently | Fastest; escalation risk |

- `ACCOUNTABILITY_EVENT`: unintended civilian engagement → escalation, political penalties, coalition fracture.
- `ETHICAL_OVERRIDE`: force stand-down at cost of missed engagement.
- Narrative weight: LAWS misuse creates lasting campaign consequences.

#### Self-Propagating Autonomous Cyber Weapons

**TRL:** 7–9 state actors | **TL-2+** (extends req 09 / req 19)

- Cascade timer: T+0 compromised → T+30s adjacent at risk → T+90s partition → T+3min fog-of-war cascade.
- Defense: `AIR_GAP_SWITCH`, `CYBER_KILL_CHAIN`, `AI_WATCHDOG`.
- Offensive: degrade adversary IADS (ghost tracks, IFF spoofing).
- Risk: propagation to neutral/allied infrastructure if paths unbounded.

#### Collective Intelligence Drone Hives

**TRL:** 5–7 coordinated; 3–4 emergent hive-mind | **TL-3**

- States: `HUNTING`, `SWARMING`, `SCATTERING`, `REFORMING`.
- Coherence threshold: >40% attrition → `DEGRADED_SWARM`.
- Counter-hive: jam emergent layer vs attrit nodes (different tool sets).
- `HIVE_COMMAND_NODE`: destroying queen forces predictable fallback protocol.

#### Hypersonic Interceptor *(new — gap from research)*

- Directed-energy intercept and hypervelocity kill vehicles (2032+ horizon).
- **P1 at TL-3** — prevents hypersonic offense from making defense unplayable.

#### Deepfake / Information Warfare AI *(new — gap from research)*

- AI-generated disinformation, synthetic media for deception operations.
- Non-kinetic domain with escalation implications (req 19, req 20 UI alerts).

#### Quantum Navigation (GPS-denied) *(new — gap from research)*

- Cold-atom inertial sensors, quantum clocks for precision navigation post-GPS denial.
- Enables operations in post-ASAT / jammed environments.

#### Autonomous Resupply & Logistics Drones *(new — gap from research)*

- Attrition-resistant supply chains (Replicator 2.0 doctrine).
- Fully autonomous forward resupply under fire — **TL-3**.

---

### 4. Counter-Space & Orbital Warfare

#### Co-Orbital and Kinetic ASAT

**TRL:** 8–9 | **TL-2** (already deployed — near-future baseline for escalation scenarios)

- `KESSLER_RISK_METER`: global counter rising with each kinetic space engagement.
- `ORBITAL_DEBRIS_PROPAGATION`: debris field over 3–6 hours; adjacent passes increase attrition.
- Strategic calculus: tactical gain vs long-term ISR/comms degradation for all factions.

#### DE-ASAT Systems

**TRL:** 6–8 ground dazzle; 3–4 space destroy | **TL-2–TL-4**

- Escalation tiers: `DAZZLE` (reversible, low political cost) → `BLIND` (permanent sensor damage) → `DESTROY` (debris, high cost).
- Ground dazzle: hard to attribute — ambiguity events for affected player.
- Non-kinetic DE-ASAT preferred upgrade path vs kinetic ASAT.

#### Reusable Nuclear Space Tugs & Cislunar Operations

**TRL:** 4–5; DRACO at 5–6 | **TL-4 only (2035+)**

- Orbital mechanics transit times; Lagrange refueling depots as strategic assets.
- `CISLUNAR_PRESENCE`: high ground covering Earth approach corridors.
- Penalties: `GPS_CONSTELLATION_RISK`, `SATELLITE_RESUPPLY_DENIED`.

---

### 5. Human & Biological Enhancement

#### Brain-Computer Interface (BCI) Command

**TRL:** 6–7 non-invasive; 4–5 high-bandwidth | **TL-3**

- `BCI_OPERATOR`: manages 3–5× more autonomous systems vs standard operator.
- Failure modes: `NEURAL_FATIGUE` (~30 min), `CYBER_HIJACK_RISK`, `SIGNAL_OVERLOAD` (1–3 min cooldown).
- `MORAL_INJURY` status after friendly-fire through system error.
- IHL: BCI-directed engagement still requires human target verification.

#### Exoskeleton & Powered Armor

**TRL:** 5–6 load-bearing; 2–3 combat grade | **TL-3 logistics / TL-4 combat**

- TL-3: logistics exoskeleton (resupply speed, sustainment cycles).
- TL-4: combat exoskeleton (15 km/h, 2× carry, light vehicle armor equivalent).
- Failure: power depletion, EMP, hydraulic lockup.

#### Biochemical & Genetic Enhancements

**TRL:** 3–4 | **TL-4 campaign R&D tree**

- `REDUCED_SLEEP_REQUIREMENT`, `ACCELERATED_HEALING`, `COGNITIVE_STIMULANT` (with crash).
- `DEGRADATION_CLOCK`: peak 5–10 years then sharp decline.
- Enhanced vs unenhanced forces → asymmetric escalation / treaty violation flags.

---

### 6. Exotic & Theoretical Concepts

#### Multi-Warhead Maneuverable HGV (speculative variant)

**TRL:** 6–7 next-gen | **TL-3**

- One launch, 3–5 independently maneuvering reentry vehicles.
- `HYPERSONIC_SATURATION` requires full defensive battery per target.
- Intercept: `DIRECTED_ENERGY_INTERCEPT`, `HYPERVELOCITY_KILL_VEHICLE` at TL-3+.

#### Lunar-Based Weapon Systems

**TRL:** 2–3 | **TL-5 / 2045+ campaign only**

- `LUNAR_FORTRESS` mission type: strategic deterrence, 3–5 day projectile transit.
- Mass drivers impractical for tactical use; lunar lasers benefit from no atmosphere.

#### Cognitive & Predictive Warfare Algorithms

**TRL:** 5–7 battle management; 3–5 predictive pre-positioning | **TL-3**

**Real-world anchor:** DARPA Mosaic Warfare; Pentagon AI agreements (May 2026).

- `PREDICTIVE_WARFARE` doctrine: AI advisor suggests optimal force packages.
- Adversary `ADAPTIVE_RESPONSE`: preemptive repositioning.
- Counter: deception operations (dummy units, false emissions).
- `OODA_LOOP_DOMINANCE` stat: side with higher quantum + AI processing acts first.

---

## Escalation Ladder (5-Tier)

All speculative systems map to an escalation tier — use triggers political cost, coalition effects, and optional loss conditions:

1. **Conventional** — TL-0–TL-2 standard warfare
2. **High-Precision Conventional** — hypersonics, mass swarms, CEW
3. **Space Domain** — ASAT, DE-ASAT, orbital DEW
4. **Autonomous Lethal** — full-agency LAWS, self-propagating cyber
5. **Nuclear Threshold** — NPX lasers, strategic MaRV, Kessler-critical debris events

---

## Non-Functional Requirements

- Full **9-level TRL scale** on every system; include TRL 8–9 for already-deployed systems (kinetic ASAT, CEW).
- Failure probability = f(TRL × environmental stress × maintenance level).
- Each system has `TECH_NOTE` tooltip with real-world TRL and research basis.
- **No physically impossible systems in main simulation** (FTL, psionics). Reserve for distinctly labeled `UNLIMITED_FICTION_MODE` if ever implemented.
- Strong emphasis on **political and escalation consequences** — primary balance mechanism for speculative power.
- Modular optional components toggled per scenario/campaign (ECS, ADR-005).

---

## Agentic Capabilities

- Dynamic Speculative Systems Agent proposes systems from fresh OSINT — **propose, not auto-merge** (doc 06).
- MCP tools: prototype archetypes, run what-if batches, generate balance/risk assessments.

---

## Technical Considerations

- Optional physics extensions: space debris propagation, orbital mechanics windows, quantum doctrine modifiers.
- Integration with Database Intelligence Layer for TRL tracking and balance drift.
- Cross-ref req 13 (ROE), req 17 (replay evidence for accountability events), req 19 (cyber/comms).

---

## Resolved Decisions (May 29, 2026)

| Question | Decision | Rationale |
|----------|----------|-----------|
| Normal vs Black Project mode? | **Graduated TL slider TL-0–TL-5**; TL-5 requires `BLACK_PROJECT_MODE` flag | Hybrid approach from research |
| Model political/escalation consequences? | **Yes — essential** | LAWS governance, Kessler risk, BCI ethics are documented current concerns |
| Physically impossible systems? | **No in main sim** | FTL/psionics only in `UNLIMITED_FICTION_MODE` if implemented |
| Cool vs credibility balance? | **Research-grounded cool factor** | Zero scientific basis excluded from main simulation |

---

## Technology Readiness Reference

| System | TRL (2026) | First Credible Deployment | Game TL |
|--------|------------|---------------------------|---------|
| Co-Orbital Kinetic ASAT | 8–9 | Deployed | TL-2 |
| DE-ASAT (Ground Dazzle) | 6–8 | 2026–2028 | TL-2 |
| Quantum Radar (Hybrid) | 5–7 | 2029–2033 | TL-3 |
| Full-Agency LAWS (Anti-personnel) | 4–6 | 2030–2035 | TL-3 |
| Drone Hive (Emergent) | 4–5 | 2030–2035 | TL-3 |
| BCI Command (Non-invasive) | 5–6 | 2030–2035 | TL-3 |
| Predictive Warfare AI | 5–7 | 2028–2032 | TL-3 |
| Quantum-Enhanced EW | 3–5 | 2030–2035 | TL-3 |
| Orbital DEW Platform | 3–4 | 2035–2040 | TL-4 |
| Combat Exoskeleton | 2–3 | 2040–2045 | TL-4 |
| Nuclear Space Tug (Cislunar) | 4–5 | 2035–2040 | TL-4 |
| Particle Beam (Atmospheric) | 2–3 | 2040+ (if ever) | TL-5 only |
| NPX Laser (Space) | 3 | 2040+ | TL-5 only |
| Lunar Weapons | 2–3 | 2050+ | TL-5 only |

---

## Traceability

| Research section | Requirement section |
|------------------|---------------------|
| §1 Exotic DEW | §1 above |
| §2 Quantum/post-stealth | §2 above |
| §3 Autonomous/cognitive | §3 above |
| §4 Counter-space | §4 above |
| §5 Human enhancement | §5 above |
| §6 Exotic concepts | §6 above |
| Gap analysis (5 items) | New entities marked *(new)* |
| NFR table | Escalation Ladder + NFR |
| Open questions | Resolved Decisions |

---

**Status:** Research-integrated — ready for GDD authoring and TL-gated content design
