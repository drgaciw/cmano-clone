# 10 - Speculative & Black Project Systems

**Last Updated:** 2026-07-18  
**Status:** Research-integrated  
**FR reverse-ref:** [FR-08](01-Project-Overview.md) — Near-future and speculative platforms (**speculative half**; near-future half is [09](09-Near-Future-Technologies.md))  
**Research basis:** [Speculative Systems Research Supplement](../../docs/research/speculative-systems-research.md)  
**Related:** 09 Near-Future Technologies, 13 Doctrine/ROE, 14 Engagement, 17 Replay/AAR, 19 Cyber/Comms  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 10 — **Partial+**; row 10b Kessler/orbital DEW runtime — **Phase N / not on main**

## Purpose

Define highly speculative, theoretical, and black-project-level military systems grounded in open-source intelligence, research papers, and defense assessments (2030–2045+). These systems are optional content gated by **Technology Level** and intended for Future Combat Mode, campaign progression, and R&D sandbox scenarios.

Implements hub **[FR-08](01-Project-Overview.md)** (near-future and speculative platforms) for the **speculative / TL-3+ half**. Near-future TL-0–TL-3 fielded systems remain [09-Near-Future-Technologies.md](09-Near-Future-Technologies.md).

## Vision

Push believable near-future warfare into the speculative realm while maintaining hardcore simulation credibility. Every system must have documented scientific plausibility, a TRL assessment, and escalation consequences — "cool factor" comes from research grounding, not fiction.

## Honesty overlay (Wave 3 — shipped gate spine vs design-only)

| Layer | What exists on `main` | What does **not** |
|-------|----------------------|-------------------|
| **Shipped gate spine** | `ScenarioSpeculativeSettings` (TL + `BLACK_PROJECT_MODE`), `SpeculativeEngageGate` (abort reasons before resolve), wiring into `MvpEngagementResolver`, policy JSON `speculative` block, catalog **metadata** rows | Full platform simulation for orbital DEW / NPX / particle beams |
| **Catalog metadata only** | `data/catalog/speculative_platforms.json` entries (`orbital-dew-demo`, `npx-laser-orbital`) — TL flags + black-project require flags for gate tests | Runtime classes, orbital insertion, dwell, power/thermal, strike geometry |
| **Design-only / Phase N** | Research sections below (quantum radar, LAWS full agency, Kessler cascade meter, 5-tier escalation runtime, first-fire `SPACE_WAR_*` events, BCI, lunar weapons, etc.) | **Not on main** as of Wave 3 2026-07-08 |

**Partial+ means:** TL / black-project **engage gate** + scenario fixtures + catalog metadata are shipped and tested. It does **not** mean S54 orbital DEW / Kessler runtime classes landed on trunk.

**Hard demotions (types absent from `src/`):**

| Claim (historical / design) | Reality on trunk (Wave 3 2026-07-08) |
|-----------------------------|-------------------------------------|
| `OrbitalDewPlatform` full det runtime | **Phase N / not on main** — `rg` zero class hits under `src/` |
| `KesslerRiskMeter` (risk 0–12, cascade gates) | **Phase N / not on main** — no type; design ladder text only |
| `EscalationTier` runtime enum / first-fire wiring | **Phase N / not on main** — 5-tier ladder is **design spec** only |
| First-fire `SPACE_WAR_THRESHOLD` / `SPACE_WAR_*` events | **Phase N / not on main** — design event names, no shipped event bus hooks |
| S54 “Implemented (S54)” for Kessler row 10b | **Demoted** → tracker **Phase N / not on main** |

Do **not** re-land S54 worktree-only types as shipped evidence without a new trunk PR and gate re-verification.

**Adversarial CI pin (Wave 3 follow-on, verified 2026-07-18):** `SpeculativeHonestyPinsTests.src_assemblies_have_no_OrbitalDewPlatform_KesslerRiskMeter_or_EscalationTier_types` enumerates every loaded `ProjectAegis.*` assembly at test time and fails if any type named `OrbitalDewPlatform`, `KesslerRiskMeter`, or `EscalationTier` is defined. This makes the Phase N demotion a **compile/test-time hard pin**, not just a documentation claim — any future re-landing on trunk must either delete the pin or land the full gate + tests per the 10b re-land criteria below.

## Technology Level Framework (Extended)

| TL | Era | Content |
|----|-----|---------|
| **TL-0–TL-2** | Near-future | See [09-Near-Future-Technologies.md](09-Near-Future-Technologies.md) |
| **TL-3** | 2030–2035 | Quantum radar hybrid, full-agency LAWS debate, drone hives, BCI command, predictive warfare AI |
| **TL-4** | 2035–2040 | Orbital DEW, cislunar ops, combat exoskeleton, nuclear space tugs |
| **TL-5** | 2040+ | Particle beams (space-only), NPX lasers, lunar weapons — **Black Project / Future War mode only** |

**Normal campaign runs TL-0 to TL-2** (`ScenarioSpeculativeSettings.CampaignDefault`: `maxTechnologyLevel: 2`, `blackProjectMode: false`). Speculative systems unlock at TL-3+; TL-5 requires explicit scenario flag `BLACK_PROJECT_MODE` (`blackProjectMode: true` in policy `speculative` block).

### Migration note

Base hypersonic MaRV systems (Avangard, DF-ZF, Dark Eagle) are **not speculative** — they belong in doc 09. This document covers **next-generation variants** (multi-warhead maneuverable HGV, hypersonic saturation packages) and systems beyond near-future fielding.

---

## Core Speculative Systems & Gameplay Implications

> **Status legend for subsections:** **Design** = research/GDD only; **Gate metadata** = catalog/policy TL flags only; **Shipped runtime** = executable on `main`. Unless marked otherwise, systems below are **Design** with optional **Gate metadata** IDs for future content.

### 1. Advanced Directed Energy & Exotic Weapons

#### Space-Based Directed Energy Platforms

**TRL:** 3–4 | **Deployment:** 2035–2040 (TL-4) | **Implementation:** **Phase N / not on main**

- Design: Requires launch event (detectable), orbital insertion (3–7 day build-up), continuous power (RTG or solar — signature implications).
- Design: **Revisit time** based on orbital mechanics — not always overhead.
- Design: Counter-space: adversary attempts kinetic or DE-ASAT on first fire (reveals position).
- Design: Escalation: first use triggers `SPACE_WAR_THRESHOLD` event — **event not shipped**; no first-fire SPACE_WAR bus on `main`.
- Design: Damage: 10–30 s dwell vs hardened targets; shorter vs thin-skinned assets.
- **On trunk today:** catalog id `orbital-dew-demo` is **metadata only** (`gameTechnologyLevel: 4`) for TL gate tests — **not** a full runtime platform. No `OrbitalDewPlatform` class under `src/`.

#### Plasma and Particle Beam Weapons

**TRL:** 2–3 | **Space domain only** | **Implementation:** Design / Phase N

- Atmospheric use excluded — beam dispersion in ionized air breaks simulation credibility.
- Space combat: infinite ammunition, 60–120 s charge cycle between shots.
- Atmospheric plasma as **area denial** (sensor disruption 2–5 min) — not direct kill weapon.
- In-game `TECH_NOTE`: atmospheric particle beams theoretical as of 2035.

#### Nuclear-Pumped X-Ray Lasers

**TRL:** 3 | **TL-5 / Black Project only** | **Implementation:** Gate metadata only / Phase N runtime

- Design: Ultimate single-use strategic weapon; triggers `NUCLEAR_USE_THRESHOLD`.
- Design: Kessler cascade risk if orbital debris generated — **no `KesslerRiskMeter` on main**.
- Not accessible in standard scenarios without `blackProjectMode` + high TL.
- **On trunk today:** catalog id `npx-laser-orbital` (`gameTechnologyLevel: 5`, `requiresBlackProject: true`) — **metadata only**.

---

### 2. Quantum & Post-Stealth Technologies

#### Operational Quantum Radar (Hybrid)

**TRL:** 5–7 sensing; 3–5 operational radar | **TL-3** | **Implementation:** Design

- `POST_STEALTH_ENVIRONMENT` scenario flag (2033+): F-35/B-21 stealth −40–60%; submarines detectable within 20 km by magnetometer arrays.
- Tiered vulnerability — not universal "stealth is dead."
- Counter-quantum kit: `DEGAUSSING_LOOP`, `SIGNATURE_NOISE_INJECTION`, `QUANTUM_DECOY_EMITTER`.
- Progression pacing: ~one quantum modality per year on operational platform (CSIS recommendation).

#### Quantum-Enhanced Electronic Warfare

**TRL:** 3–5 | **TL-3** | **Implementation:** Design

- `QUANTUM_EW_ADVANTAGE` doctrine: wins automatic `WAVEFORM_CONTEST`.
- Post-quantum cryptography (PQC) for C2: without PQC upgrade, adversary with quantum computing decodes orders with ~30 s delay.

#### Post-Quantum Cryptography Arms Race *(new — gap from research)*

- Not a weapon — strategic layer. Side without PQC-encrypted C2 is vulnerable to quantum decryption.
- Active technology race mechanic tied to cyber domain (req 19). **Phase N.**

#### Metamaterial Adaptive Camouflage

**TRL:** 7–8 thermal (infantry); 4–5 broadband platform | **TL-3–TL-4** | **Implementation:** Design

- Four tracks: `RADAR_CLOAK`, `IR_CLOAK`, `VISUAL_CLOAK`, `ACOUSTIC_CLOAK`.
- Power budget: choose 2–3 active simultaneously; all four requires advanced nuclear power.
- `SHIMMER_EFFECT`: 5–15% detection probability while moving.

---

### 3. Autonomous & Cognitive Weapon Systems

#### LAWS with Full Agency

**TRL:** 8–9 counter-drone/missile; 4–6 anti-personnel | **TL-3** | **Implementation:** Design (MVP autonomy is req 04/13 spine — not full-agency LAWS narrative)

ROE delegation modes (aligns with req 13):

| Mode | Behavior | Risk |
|------|----------|------|
| `HUMAN_IN_LOOP` | Every engagement requires confirmation | Slowest, most controlled |
| `HUMAN_ON_LOOP` | AI selects; player override within 10 s | Balanced |
| `FULL_AUTONOMOUS` | AI engages independently | Fastest; escalation risk |

- `ACCOUNTABILITY_EVENT`: unintended civilian engagement → escalation, political penalties, coalition fracture. **Phase N events.**
- `ETHICAL_OVERRIDE`: force stand-down at cost of missed engagement.
- Narrative weight: LAWS misuse creates lasting campaign consequences.

#### Self-Propagating Autonomous Cyber Weapons

**TRL:** 7–9 state actors | **TL-2+** (extends req 09 / req 19) | **Implementation:** Design / Phase N beyond comms Partial spine in req 19

- Cascade timer: T+0 compromised → T+30s adjacent at risk → T+90s partition → T+3min fog-of-war cascade.
- Defense: `AIR_GAP_SWITCH`, `CYBER_KILL_CHAIN`, `AI_WATCHDOG`.
- Offensive: degrade adversary IADS (ghost tracks, IFF spoofing).
- Risk: propagation to neutral/allied infrastructure if paths unbounded.

#### Collective Intelligence Drone Hives

**TRL:** 5–7 coordinated; 3–4 emergent hive-mind | **TL-3** | **Implementation:** Design

- States: `HUNTING`, `SWARMING`, `SCATTERING`, `REFORMING`.
- Coherence threshold: >40% attrition → `DEGRADED_SWARM`.
- Counter-hive: jam emergent layer vs attrit nodes (different tool sets).
- `HIVE_COMMAND_NODE`: destroying queen forces predictable fallback protocol.

#### Hypersonic Interceptor *(new — gap from research)*

- Directed-energy intercept and hypervelocity kill vehicles (2032+ horizon).
- **P1 at TL-3** — prevents hypersonic offense from making defense unplayable. **Design / Phase N.**

#### Deepfake / Information Warfare AI *(new — gap from research)*

- AI-generated disinformation, synthetic media for deception operations.
- Non-kinetic domain with escalation implications (req 19, req 20 UI alerts). **Design.**

#### Quantum Navigation (GPS-denied) *(new — gap from research)*

- Cold-atom inertial sensors, quantum clocks for precision navigation post-GPS denial.
- Enables operations in post-ASAT / jammed environments. **Design.**

#### Autonomous Resupply & Logistics Drones *(new — gap from research)*

- Attrition-resistant supply chains (Replicator 2.0 doctrine).
- Fully autonomous forward resupply under fire — **TL-3**. **Design.**

---

### 4. Counter-Space & Orbital Warfare

#### Co-Orbital and Kinetic ASAT

**TRL:** 8–9 | **TL-2** (already deployed — near-future baseline for escalation scenarios) | **Implementation:** Design

- Design `KESSLER_RISK_METER`: global counter rising with each kinetic space engagement — **`KesslerRiskMeter` type is Phase N / not on main** (Wave 3 precondition: zero `src/` hits).
- Design `ORBITAL_DEBRIS_PROPAGATION`: debris field over 3–6 hours; adjacent passes increase attrition.
- Strategic calculus: tactical gain vs long-term ISR/comms degradation for all factions.

#### DE-ASAT Systems

**TRL:** 6–8 ground dazzle; 3–4 space destroy | **TL-2–TL-4** | **Implementation:** Design

- Escalation tiers (design labels only — **not** a shipped `EscalationTier` enum): `DAZZLE` (reversible, low political cost) → `BLIND` (permanent sensor damage) → `DESTROY` (debris, high cost).
- Ground dazzle: hard to attribute — ambiguity events for affected player.
- Non-kinetic DE-ASAT preferred upgrade path vs kinetic ASAT.

#### Reusable Nuclear Space Tugs & Cislunar Operations

**TRL:** 4–5; DRACO at 5–6 | **TL-4 only (2035+)** | **Implementation:** Design / Phase N

- Orbital mechanics transit times; Lagrange refueling depots as strategic assets.
- `CISLUNAR_PRESENCE`: high ground covering Earth approach corridors.
- Penalties: `GPS_CONSTELLATION_RISK`, `SATELLITE_RESUPPLY_DENIED`.

---

### 5. Human & Biological Enhancement

#### Brain-Computer Interface (BCI) Command

**TRL:** 6–7 non-invasive; 4–5 high-bandwidth | **TL-3** | **Implementation:** Design

- `BCI_OPERATOR`: manages 3–5× more autonomous systems vs standard operator.
- Failure modes: `NEURAL_FATIGUE` (~30 min), `CYBER_HIJACK_RISK`, `SIGNAL_OVERLOAD` (1–3 min cooldown).
- `MORAL_INJURY` status after friendly-fire through system error.
- IHL: BCI-directed engagement still requires human target verification.

#### Exoskeleton & Powered Armor

**TRL:** 5–6 load-bearing; 2–3 combat grade | **TL-3 logistics / TL-4 combat** | **Implementation:** Design

- TL-3: logistics exoskeleton (resupply speed, sustainment cycles).
- TL-4: combat exoskeleton (15 km/h, 2× carry, light vehicle armor equivalent).
- Failure: power depletion, EMP, hydraulic lockup.

#### Biochemical & Genetic Enhancements

**TRL:** 3–4 | **TL-4 campaign R&D tree** | **Implementation:** Design

- `REDUCED_SLEEP_REQUIREMENT`, `ACCELERATED_HEALING`, `COGNITIVE_STIMULANT` (with crash).
- `DEGRADATION_CLOCK`: peak 5–10 years then sharp decline.
- Enhanced vs unenhanced forces → asymmetric escalation / treaty violation flags.

---

### 6. Exotic & Theoretical Concepts

#### Multi-Warhead Maneuverable HGV (speculative variant)

**TRL:** 6–7 next-gen | **TL-3** | **Implementation:** Design (base HGV envelopes may live under doc 09)

- One launch, 3–5 independently maneuvering reentry vehicles.
- `HYPERSONIC_SATURATION` requires full defensive battery per target.
- Intercept: `DIRECTED_ENERGY_INTERCEPT`, `HYPERVELOCITY_KILL_VEHICLE` at TL-3+.

#### Lunar-Based Weapon Systems

**TRL:** 2–3 | **TL-5 / 2045+ campaign only** | **Implementation:** Design / Phase N

- `LUNAR_FORTRESS` mission type: strategic deterrence, 3–5 day projectile transit.
- Mass drivers impractical for tactical use; lunar lasers benefit from no atmosphere.

#### Cognitive & Predictive Warfare Algorithms

**TRL:** 5–7 battle management; 3–5 predictive pre-positioning | **TL-3** | **Implementation:** Design

**Real-world anchor:** DARPA Mosaic Warfare; Pentagon AI agreements (May 2026).

- `PREDICTIVE_WARFARE` doctrine: AI advisor suggests optimal force packages.
- Adversary `ADAPTIVE_RESPONSE`: preemptive repositioning.
- Counter: deception operations (dummy units, false emissions).
- `OODA_LOOP_DOMINANCE` stat: side with higher quantum + AI processing acts first.

---

## Escalation Ladder (5-Tier) — design only

All speculative systems map to an escalation tier — use triggers political cost, coalition effects, and optional loss conditions:

1. **Conventional** — TL-0–TL-2 standard warfare
2. **High-Precision Conventional** — hypersonics, mass swarms, CEW
3. **Space Domain** — ASAT, DE-ASAT, orbital DEW
4. **Autonomous Lethal** — full-agency LAWS, self-propagating cyber
5. **Nuclear Threshold** — NPX lasers, strategic MaRV, Kessler-critical debris events

**Honesty:** This ladder is a **design specification**. There is **no shipped `EscalationTier` runtime type**, no first-fire political-cost resolver, and no Kessler-critical automatic loss condition on `main` as of Wave 3 2026-07-08. Political/escalation consequences remain Phase N product work.

---

## Non-Functional Requirements

- Full **9-level TRL scale** on every system; include TRL 8–9 for already-deployed systems (kinetic ASAT, CEW).
- Failure probability = f(TRL × environmental stress × maintenance level).
- Each system has `TECH_NOTE` tooltip with real-world TRL and research basis. **Phase N UI.**
- **No physically impossible systems in main simulation** (FTL, psionics). Reserve for distinctly labeled `UNLIMITED_FICTION_MODE` if ever implemented.
- Strong emphasis on **political and escalation consequences** — primary balance mechanism for speculative power (**design**; not yet a runtime meter).
- Modular optional components toggled per scenario/campaign (ECS, ADR-005).
- **Shipped NFR slice:** engage-time TL / black-project checks are deterministic and unit-tested (`SpeculativeEngageGate` + `ScenarioSpeculativeGateTests`).

---

## Agentic Capabilities

- Dynamic Speculative Systems Agent proposes systems from fresh OSINT — **propose, not auto-merge** (doc 05 / 06). **Partial+ under req 05 OSINT mapping; not auto-import into catalog.**
- MCP tools: prototype archetypes, run what-if batches, generate balance/risk assessments. **Studio tooling Partial under req 07; not a live OSINT feed.**

---

## Technical Considerations

- Optional physics extensions: space debris propagation, orbital mechanics windows, quantum doctrine modifiers — **Phase N**.
- Integration with Database Intelligence Layer for TRL tracking and balance drift (doc 06).
- Cross-ref req 13 (ROE), req 17 (replay evidence for accountability events), req 19 (cyber/comms).
- **Shipped integration point:** speculative gate runs inside `MvpEngagementResolver` before launch resolution (same single-resolver spine as doc 14).

---

## Implementation Mapping (headless)

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| TL / black-project settings | `ScenarioSpeculativeSettings` (`ProjectAegis.Sim` · `Scenario/`) | **Shipped** | `CampaignDefault` TL-2 / no black-project; ctor clamps TL 0–5; loaded via `ScenarioPolicyJsonLoader.ParseSpeculative` into `ScenarioPolicyProfile.Speculative` |
| Engage gate | `SpeculativeEngageGate.Evaluate` | **Shipped** | Aborts `TechnologyLevelExceeded` / `BlackProjectRequired`; called from `MvpEngagementResolver` pre-resolve |
| Catalog metadata loader | `SpeculativePlatformCatalog` + `data/catalog/speculative_platforms.json` | **Shipped (metadata only)** | Rows `orbital-dew-demo` (TL-4), `npx-laser-orbital` (TL-5 + black-project) — **not** full runtime platforms |
| Scenario fixtures | `data/scenarios/baltic-patrol-black-project.policy.json`, `baltic-patrol-speculative-gated.policy.json` | **Shipped (test/isolation)** | Black-project allows TL-5 weapon; gated campaign TL-2 aborts TL-5 / black-project weapon |
| Gate unit tests | `ScenarioSpeculativeGateTests` + `SpeculativeHonestyPinsTests` (`ProjectAegis.Sim.Tests`) | **Shipped** | Gate asserts null abort vs TL/black-project aborts; catalog path discovery; **adversarial pin** enumerates loaded `ProjectAegis.*` assemblies and fails if `OrbitalDewPlatform` / `KesslerRiskMeter` / `EscalationTier` types appear (verified 2026-07-18) |
| Orbital DEW full runtime | ~~`OrbitalDewPlatform`~~ | **Phase N / not on main** | Precondition `rg OrbitalDewPlatform src/` → **zero** class hits (Wave 3 2026-07-08) |
| Kessler risk runtime | ~~`KesslerRiskMeter`~~ | **Phase N / not on main** | Precondition `rg KesslerRiskMeter src/` → **zero**; design `KESSLER_RISK_METER` text only |
| 5-tier escalation runtime / SPACE_WAR first-fire | ~~`EscalationTier`~~, `SPACE_WAR_THRESHOLD` | **Phase N / not on main** | Precondition `rg EscalationTier src/` → **zero**; ladder + event names are design-only |

**Honesty note:** Tracker **Partial+** for row 10 = gate spine + fixtures + catalog metadata. Row **10b** (Kessler / orbital DEW runtime) is **Phase N / not on main** — historical S54 “Implemented” claims referred to worktree-only or non-trunk artifacts and must not be treated as shipped product. The Phase N invariant is **adversarially pinned in CI** by `SpeculativeHonestyPinsTests` (see Gate unit tests row above); re-landing 10b runtime on `main` requires deleting or rewriting that pin alongside the new gate + tests.

---

## Resolved Decisions (May 29, 2026)

| Question | Decision | Rationale |
|----------|----------|-----------|
| Normal vs Black Project mode? | **Graduated TL slider TL-0–TL-5**; TL-5 requires `BLACK_PROJECT_MODE` flag | Hybrid approach from research; **shipped as** `blackProjectMode` + `maxTechnologyLevel` on policy |
| Model political/escalation consequences? | **Yes — essential** (design) | LAWS governance, Kessler risk, BCI ethics are documented current concerns; **runtime meters Phase N** |
| Physically impossible systems? | **No in main sim** | FTL/psionics only in `UNLIMITED_FICTION_MODE` if implemented |
| Cool vs credibility balance? | **Research-grounded cool factor** | Zero scientific basis excluded from main simulation |
| S54 orbital DEW / Kessler on trunk? | **No — Phase N / not on main** (Wave 3 honesty 2026-07-08) | Gate + metadata only; full runtime demoted until re-landed and gated |

---

## Technology Readiness Reference

| System | TRL (2026) | First Credible Deployment | Game TL | Trunk status |
|--------|------------|---------------------------|---------|--------------|
| Co-Orbital Kinetic ASAT | 8–9 | Deployed | TL-2 | Design |
| DE-ASAT (Ground Dazzle) | 6–8 | 2026–2028 | TL-2 | Design |
| Quantum Radar (Hybrid) | 5–7 | 2029–2033 | TL-3 | Design |
| Full-Agency LAWS (Anti-personnel) | 4–6 | 2030–2035 | TL-3 | Design |
| Drone Hive (Emergent) | 4–5 | 2030–2035 | TL-3 | Design |
| BCI Command (Non-invasive) | 5–6 | 2030–2035 | TL-3 | Design |
| Predictive Warfare AI | 5–7 | 2028–2032 | TL-3 | Design |
| Quantum-Enhanced EW | 3–5 | 2030–2035 | TL-3 | Design |
| Orbital DEW Platform | 3–4 | 2035–2040 | TL-4 | **Metadata only** (`orbital-dew-demo`); runtime **Phase N** |
| Combat Exoskeleton | 2–3 | 2040–2045 | TL-4 | Design |
| Nuclear Space Tug (Cislunar) | 4–5 | 2035–2040 | TL-4 | Design |
| Particle Beam (Atmospheric) | 2–3 | 2040+ (if ever) | TL-5 only | Design |
| NPX Laser (Space) | 3 | 2040+ | TL-5 only | **Metadata only** (`npx-laser-orbital`); runtime **Phase N** |
| Lunar Weapons | 2–3 | 2050+ | TL-5 only | Design |

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
| Hub **FR-08** ([01](01-Project-Overview.md)) | Speculative half of near-future + speculative platforms — this doc |
| Doc 09 | Near-future TL-0–TL-3 half of FR-08 |
| Doc 14 | Single resolver hosts `SpeculativeEngageGate` |
| Tracker rows 10 / 10b | Partial+ gate spine; Kessler/orbital DEW runtime demoted |

---

**Implementation grade:** Partial+ — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 10 (gate + metadata); row 10b **Phase N / not on main**.  
Design Status remains **Research-integrated**. Charter re-honesty: Wave 3 2026-07-08.
