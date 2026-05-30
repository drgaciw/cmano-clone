# Near-Future Technologies — Deep Research Supplement

**Document Type:** Research Supplement for `09-Near-Future-Technologies.md`  
**Research Date:** May 29, 2026  
**Scope:** 2025–2035 military technologies grounding gameplay systems in current verified development status

---

## Overview

This document enriches the near-future (2028–2035) technology categories defined in `09-Near-Future-Technologies.md` with current open-source intelligence, program statuses, real-world performance envelopes, and concrete gameplay design recommendations. All systems described herein have verifiable real-world counterparts at TRL 5–9.

---

## 1. Unmanned & Autonomous Systems

### 1.1 Loyal Wingman UAVs / Collaborative Combat Aircraft (CCA)

**Current Status (2026):** Operationally imminent.

The USAF is on track to make a production decision in FY2026 between two prototypes selected in 2024:
- **YFQ-42A** (General Atomics)
- **YFQ-44A Fury** (Anduril)

Boeing's **MQ-28 Ghost Bat** (Australia) is fielded as a multi-role uncrewed loyal wingman with a range of ~3,700 km, while the **XQ-58A Valkyrie** (Kratos) extends to ~5,600 km range. The USAF program targets an operational fleet by **2030**, pairing CCAs directly with the upcoming **F-47** sixth-generation fighter (contract awarded to Boeing in March 2025; first flight target 2028, service entry early 2030s). The USMC is targeting deliverable MQ-58 CTOL prototypes by **summer 2029**. The Army is also opening competition for its own CCA variant.

Total planned USAF investment: **$8.9 billion (FY2025–2029)**.

**Gameplay Design Recommendations:**
- Model two distinct CCA archetypes: **attritable strike** (lower cost, shorter range, expendable) and **persistent ISR** (higher cost, longer dwell, protected).
- CCA autonomy levels: `WINGMAN` (human-supervised engagement), `AUTONOMOUS_STRIKE` (rules-of-engagement-bound independent targeting), `ESCORT_ONLY` (defensive screen only). Player assigns mode per mission phase.
- Failure modes: RF link degradation, GPS spoofing, AI adversarial targeting confusion.
- Pairing with F-47 in-game should unlock a `Quarterback` bonus: player can dynamically task CCA swarms mid-engagement at reduced cognitive cost.

**Suggested Parameters (YFQ-44A Fury baseline):**

| Parameter | Value |
|---|---|
| Speed | Mach 0.95 |
| Combat Radius | ~1,000 km |
| Payload | ~1,000 lb internal |
| Unit Cost (approx.) | $3–8M attritable tier |
| Autonomy Level | SAL-2 (Human-on-the-loop) |

---

### 1.2 Drone Swarms / Collaborative Combat Aircraft (CCA Swarms)

**Current Status (2026):** Early fielding — hundreds of systems transitioning to military hands.

The Pentagon's **Replicator initiative** (announced August 2023, led by the Defense Innovation Unit) has already delivered hundreds of autonomous systems across air and sea domains on its initial 18–24 month schedule. **Replicator 2.0** (announced September 2024) began high-volume production runs, and the first Replicator 2 acquisition (two DroneHunter F700 counter-drone systems) was announced in January 2026.

Sweden's Saab unveiled a drone swarm control suite in January 2025, enabling a single operator to manage **up to 100 UAS simultaneously** across reconnaissance, defense, and payload delivery roles — tested in the Arctic Strike Exercise (March 2025).

China continues advancing distributed intelligence swarm doctrine with loitering munitions and mass-volume saturation strategy.

**Gameplay Design Recommendations:**
- Swarm size tiers: `MICRO` (5–50, precision strike), `MEDIUM` (50–500, area saturation), `MASS` (500–5,000, theater-level). Recommend supporting Medium tier in first release; Mass tier as DLC/expansion.
- Swarm behaviors: `SATURATION_ATTACK`, `DISTRIBUTED_ISR`, `SACRIFICIAL_SCREEN`, `LOITERING_STRIKE`, `JAMMING_CLOUD`.
- Counter-swarm systems should include: high-power microwave (THOR/Leonidas), laser close-in, directed EW jamming, and CCA-vs-swarm intercept tasking.
- Attrition modeling: swarms accept 40–60% attrition before mission failure; this drives saturation tactic game value.

---

### 1.3 Autonomous Underwater Vehicles (AUVs) / Drone Submarines

**Current Status (2026):** Early operational deployment.

Boeing's **Orca XLUUV** (Extra Large Uncrewed Undersea Vehicle) is under development for the US Navy — a fully autonomous submarine for long-duration missions including mine countermeasures, ISR, and torpedo delivery. It leverages modular payload bays for mission reconfigurability.

India and China are both accelerating AUV programs; China's **HSU-001** has been publicly demonstrated.

**Gameplay Design Recommendations:**
- AUV mission profiles: `PERSISTENT_ISR` (weeks-long silent patrol), `MINE_LAYING`, `TORPEDO_DELIVERY`, `COMMS_RELAY`.
- Introduce an **Undersea Fog of War** system: AUVs have low probability-of-detection until quantum magnetometer or SOSUS-type fixed arrays detect their magnetic signature.
- Quantum gravimeter/magnetometer upgrades directly counter AUV stealth — create a cat-and-mouse detection gameplay loop.
- Endurance modeling: 30–90 day missions before resupply/recovery.

---

## 2. Advanced Weapons

### 2.1 Hypersonic Weapons

**Current Status (2026):** Active fielding — multiple programs in final development.

The United States currently operates **three parallel hypersonic programs**:
1. **LRHW Dark Eagle** (Army, land-based, boost-glide) — Missed 2025 deployment; early 2026 fielding confirmed.
2. **Conventional Prompt Strike (CPS)** (Navy, submarine-launched) — Army FY2026 RDT&E request: $513M.
3. **HACM – Hypersonic Attack Cruise Missile** (Air Force, air-launched) — Designed for B-52s (20+ per aircraft).

China's **DF-ZF** (Mach 5–10 glide vehicle, operational since ~2019) and Russia's **Avangard** (Mach 20+, nuclear-capable, >6,000 km range) represent mature adversary systems already in service.

**Gameplay Design Recommendations:**
- Model two distinct hypersonic types: **Boost-Glide** (ballistic ascent + unpowered glide phase, highly maneuverable, TBM-like signature) and **Hypersonic Cruise Missile** (air-breathing, lower altitude, more detectable but sustained thrust).
- Intercept windows: reaction time less than 120 seconds for boost-glide; near-zero for Avangard-class systems. Only THAAD or upgraded SM-3 Block IIA have any intercept probability.
- Dark Eagle baseline parameters: Range ~2,800 km, CEP less than 10 m, flight time ~10–15 min, Mach 17 peak.
- Introduce `HYPERSONIC ALERT` game state: when player detects enemy hypersonic launch, a tension clock begins with limited decision branches.

---

### 2.2 Directed Energy Weapons (DEW)

**Current Status (2026):** Limited operational deployment — rapid scaling.

As of May 2026, the US Navy has armed **nine guided-missile destroyers** with shipboard solid-state lasers:
- **ODIN** — Low-power dazzler on 8 DDGs; disrupts EO/IR seekers on drones.
- **HELIOS (60 kW)** — Aboard USS Preble (DDG-88); can destroy small drones and fast attack craft; integrated with Aegis.

Development pipeline:
- **HELCAP (300 kW)** — Navy next-gen system.
- **Lockheed HELSI (500 kW)** — In development.
- Israel's **Iron Beam (100 kW)** — Deployment expected from 2025; ground-based rocket/mortar/UAV intercept.
- UK's **DragonFire (50 kW)** — Ship, aircraft, and ground vehicle deployment from 2027.

Key limitations: range measured in **single miles in adverse weather**; atmospheric diffraction; thermal management; dwell time requirements against maneuvering targets.

**Gameplay Design Recommendations:**
- Model DEW as a **continuous power draw** subsystem with thermal accumulation. At sustained fire, platform enters `THERMAL_LIMIT` state with degraded output.
- Atmospheric penalties: rain/fog reduce effective range by 30–70%; clear-sky conditions at altitude allow full performance.
- Power tiers: `DAZZLE` (0–10 kW, blinds optics/seekers), `DISABLE` (10–100 kW, kills drone motors), `DESTROY` (100–500 kW, cuts through missile airframes).
- Navy ship integration: DEW competes for power allocation with radar, propulsion, and other systems — tactical power management mini-game.
- Counter-drone role is most credible 2028 use case; anti-missile role is 2032+.

---

### 2.3 Electromagnetic Railguns

**Current Status (2026):** Revived — return to active testing after 4-year hiatus.

The US Navy conducted live-fire railgun tests at White Sands Missile Range in **February 2025** (joint effort with NSWC Dahlgren). President Trump's "Golden Fleet" battleship initiative (20–25 new battleships) calls for **32 megajoule railguns** on each vessel. This range enables fire of **100+ nautical miles** versus 10–20 nm for conventional naval guns. The program is now formally realigned to the Joint Hypersonics Transition Office.

Technical parameters (32 MJ system): projectile velocity ~Mach 7.5, range ~185 km, no explosive propellant required.

**Gameplay Design Recommendations:**
- Railgun in-game replaces traditional naval gun with dramatically extended range but requires **capacitor charge time** (30–60 seconds between shots at full power).
- Power surge mechanic: full-power shot forces ship into `REDUCED SHIP SPEED` and `RADAR DEGRADED` for 5–10 seconds.
- Hypervelocity projectile (HVP) can serve dual-purpose: surface strike, shore bombardment, or anti-air intercept against drones/cruise missiles.
- Currently `near-future` category (2030+); should not be in 2025 scenario baseline.

---

## 3. Sensors & Detection

### 3.1 Quantum Sensors & Quantum Radar

**Current Status (2026):** Pre-deployment — TRL 4–7 depending on modality.

The **2025 DIA Worldwide Threat Assessment** flags quantum sensing as "nearing battlefield relevance." Key developments:
- **Quantum magnetometers**: detect submarine-scale magnetic anomalies without GPS — China and Russia are deploying these in expanded city-scale quantum networks.
- **Quantum gravimeters**: map density anomalies to detect tunnels, submarines, underground structures.
- **Quantum-enhanced radar**: converts microwave to optical frequencies for higher sensitivity against low-observable targets.
- JAPCC assessment: quantum sensing TRL **ranges from 3 to 9**, with near-term field deployment expected within three or more years.

Critical caveat from DIA: a quantum computer capable of breaking current encryption is **unlikely this decade** — but quantum sensors (not computers) are the near-term game-changer.

Most likely evolution: quantum radar will be **hybrid systems paired with conventional radar** rather than standalone replacements.

**Gameplay Design Recommendations:**
- Model quantum sensors as a **doctrine upgrade**, not a single platform: activating `QUANTUM_SENSING_DOCTRINE` reduces stealth effectiveness across all low-observable platforms by 20–40%.
- Counter-quantum measures: **real-time degaussing** (submarine demagnetization), **decoy signatures**, **noise injection** — player countermeasures when adversary has quantum sensing doctrine unlocked.
- Recommend: abstract quantum radar as a scenario toggle (`QUANTUM_SENSING_ENABLED`) in first release. Full simulation in 2030+ scenario tier.

---

### 3.2 AI-Enhanced Sensor Fusion

**Current Status (2026):** Actively fielded — cornerstone of JADC2 doctrine.

The USAF **Capstone 2025** exercise (November 2025, Nellis AFB) demonstrated AI-enabled battle management across US Army, Marines, Air Force, and all Five Eyes partners. **Maven Smart System (Project Maven)** serves as the AI backbone, integrating sensor data for dynamic mission re-planning and targeting.

The DoD's **JADC2** architecture aims to connect all sensor/shooter nodes — from satellites to ground sensors to ship radar — into a single AI-processed kill chain. Pentagon has signed AI integration agreements with OpenAI, Google, Microsoft, Amazon, and SpaceX for classified network integration (confirmed May 2026).

**Gameplay Design Recommendations:**
- Sensor fusion creates a **Composite Air Picture (CAP)** — quality degrades under EW attack, platform attrition, or network disruption.
- Model latency: fused picture has 2–10 second update cycle; EW disruption increases this to 30–120 seconds or introduces ghost tracks.
- AI fusion upgrade unlocks `AUTOMATIC CUEING`: system automatically assigns intercept assets to tracked threats without player micromanagement.
- AI fusion is the primary target for autonomous cyber weapons.

---

## 4. Electronic Warfare & Cyber

### 4.1 Cognitive Electronic Warfare (CEW)

**Current Status (2026):** In advanced development — DARPA programs transitioning to platforms.

DARPA's **ARC (Adaptive Radar Countermeasures)** and **BLADE (Behavioral Learning for Adaptive EW)** programs, executed by BAE Systems (contract extended: $13.3M), use machine learning to characterize and counter unknown/adaptive radar systems in real time. As of 2025, cognitive EW systems are described as: "observe a threat system, characterize it on the fly, devise a countermeasure, and do all of this in real time."

**Gameplay Design Recommendations:**
- CEW system has a **learning curve**: first encounter with an enemy waveform takes 3–5 seconds for characterization; second encounter is near-instant.
- `EW DUEL` mechanic: when two CEW-equipped platforms engage, a dynamic waveform competition unfolds — each side's system adapts, requiring player to decide when to commit to a jamming mode vs. preserve a new waveform signature.
- CEW vulnerability: quantum-enhanced radar (post-2030 tier) renders current CEW moot unless CEW system also uses quantum processing.

---

### 4.2 Autonomous Cyber Weapons

**Current Status (2026):** Operationally relevant — AI-driven cyber attack is a current reality.

AI already enables adaptive malware that evolves in real time, hyper-personalized phishing at scale, and automated vulnerability scanning. As of May 2026, cyber warfare is described as "no longer theoretical — an operational reality," with the Pentagon's own AI integrations creating new attack surfaces. Space systems, 5G/6G networks, and autonomous robotic systems are identified as highest-priority new cyber targets.

**Gameplay Design Recommendations:**
- Autonomous cyber weapon `PROPAGATION` mechanic: once injected into a C4I node, the weapon spreads laterally — player must detect, isolate, and purge before cascade failure.
- Cascade timeline: T+0 injection, T+30s primary node degraded, T+90s secondary nodes infected, T+180s network collapse.
- Cyber defense layers: air-gapped backup C2, intrusion detection AI, network segmentation — each adds resistance but reduces integration benefit.
- Include `ACCOUNTABILITY_GAP` narrative event: AI cyber weapon creates unintended civilian infrastructure damage, triggering escalation meter increase.

---

## 5. Stealth & Survivability

### 5.1 Adaptive Camouflage & Metamaterials

**Current Status (2026):** Multiple modalities at different TRLs.

- **Thermal / IR camouflage (HT4)**: European researchers field-tested the HT4 "thermal invisibility cloak" in late 2025 — a heat-regulating fabric that matches ambient temperature, concealing body heat from thermal imaging. Can mask heat sources up to ~1,000 degrees Celsius.
- **Radar metamaterials**: Engineered meta-atom structures with sub-wavelength architecture can control microwave, IR, and acoustic signatures. Core challenge: **narrowband performance** — each design is optimized for specific frequency ranges.
- **AI-adaptive stealth**: AI systems can analyze adversary sensor emissions in real time and adjust platform radar absorbency, heat emission, and ECM profile dynamically.

True multi-spectral adaptive camouflage (visible + IR + radar + acoustic simultaneously) is 2030+ TRL.

**Gameplay Design Recommendations:**
- Introduce **Signature Management** as a dedicated subsystem: each platform has `RADAR_SIGNATURE`, `IR_SIGNATURE`, `ACOUSTIC_SIGNATURE`, and `VISUAL_SIGNATURE` tracks — each independently manageable.
- Adaptive camouflage systems reduce one or two signatures simultaneously at the cost of **power draw and heat generation** (itself raising IR signature — a meaningful trade-off).
- HT4-type thermal cloaking is credible as a 2028 infantry-level item; multi-spectral platform camouflage is 2033+.
- Metamaterial coatings degrade over time and combat damage — maintenance cost and availability modeling.

### 5.2 Next-Generation Low-Observable Coatings

Broadband radar-absorbing materials (RAM) are in active R&D with DRDO and US programs — including underwater tiles for submarines tested in February 2025 (DRDO/NIT Rourkela collaboration). AI-driven stealth adaptation allows platforms to reconfigure their signature profile mid-mission — a 2030–2035 capability.

---

## Open Questions — Research-Informed Answers

| Original Question | Research Finding |
|---|---|
| Max swarm size in first release? | **500 is credible** for current-gen simulation (matches Replicator doctrine); 2,000+ is feasible in a 2030+ scenario tier. Recommend 500 for v1.0, expandable. |
| DEW thermal and power constraints? | **Yes — model them.** Real systems (HELIOS, Iron Beam) have strict thermal limits and power competition. This is a key balancing mechanic. |
| Quantum sensor abstraction vs. full sim? | **Abstract as doctrine unlock** in first release; individual sensor unit modeling for 2030+ scenario tier. DIA confirms quantum sensors are near-term; full-fidelity simulation premature. |
| Space domain awareness from day one? | **Yes — limited.** Include satellite ISR degradation mechanics and ASAT threat as escalation-tier events, not base-layer systems. Cislunar operations are 2035+ scope. |

---

## Suggested New Systems (Gap Analysis)

The following near-future systems are real-world verified but absent from the original document:

1. **Replicator-class Attritable Drone** — Sub-$50K expendable UAS; represents the actual procurement doctrine driving USAF "affordable mass." Distinct from full CCA platforms.
2. **Counter-Drone Systems (C-UAS)** — THOR (high-power microwave), Leonidas, L-MADIS (USMC), and DroneHunter F700 are deployed counter-swarm systems missing from the document.
3. **JADC2 / C2 Network Node** — The sensor fusion backbone deserves its own entity archetype: a contested resource that, if destroyed or jammed, degrades entire force effectiveness.
4. **Hypersonic Defense Layer (THAAD+, SM-3 IIA)** — Intercept assets for the 2028–2032 window; critical for defense gameplay balance.
5. **Undersea Fixed Sensor Arrays (SOSUS successor)** — Distributed undersea acoustic/quantum detection grids to counter AUV threats.

---

## Performance Envelope Reference Table

| System | Speed | Range | TRL (2026) | Game Category |
|---|---|---|---|---|
| YFQ-44A Fury CCA | Mach 0.95 | ~1,000 km combat radius | 7–8 | Near-Future (2028) |
| XQ-58A Valkyrie | Mach 0.95 | 5,600 km | 8 | Current+ |
| LRHW Dark Eagle | Mach 17 (peak) | ~2,800 km | 7–8 | Near-Future (2026+) |
| HACM | Mach 5+ | ~1,000 km | 6–7 | Near-Future (2028) |
| HELIOS 60 kW | Speed of light | ~2–5 km effective | 8 | Current+ |
| HELCAP 300 kW | Speed of light | ~10–20 km (clear sky) | 5–6 | Near-Future (2030) |
| Orca XLUUV | ~8 knots | 6,500 km | 6–7 | Near-Future (2028) |
| 32 MJ Railgun | Mach 7.5 | 185 km | 5–6 | Near-Future (2030+) |

---

*Sources: USAF Aerospace America, The War Zone (TWZ), USNI News, Defense Scoop, Forecast International DSM, CSIS Quantum Sensing Report, DIA 2025 Threat Assessment, JAPCC Quantum Technologies for Air and Space, Air and Space Forces Magazine, Boeing XLUUV, Wikipedia Manned-Unmanned Teaming.*

**Status:** Ready for integration into game design documentation — content population and balancing phase.
