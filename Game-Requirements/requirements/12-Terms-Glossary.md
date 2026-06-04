# 12 - Terms Glossary

**Purpose:**
This glossary defines the core simulation terms used by the project, starting from the CMO manual's opening terminology and adapting them into a clearer, more agent-friendly vocabulary for this game.

**Reference Reviewed:**
[Command: Modern Operations manual PDF](https://www.matrixgames.com/amazon/PDF/CMO/CMO_manual_EBOOK.pdf), starting at page 19.

## Glossary

| Term | Definition |
| --- | --- |
| Unit | A single simulated entity that can act, be controlled, or be carried by another entity. Units include aircraft, ships, submarines, ground platforms, and some special entities such as sensors, weapons, facilities, and support objects. |
| Group | A collection of units that the simulation treats as a coordinated package. Groups are useful when a formation, flight, or task force should move and act as one operational element. |
| Mount | A physical installation on a unit that holds a weapon or sensor. A mount is what makes the equipment usable in the simulation, and it may depend on ammo, magazines, or platform state. |
| Magazine | A storage location for weapons, ammunition, or stores. Magazines feed mounts and define whether a unit can reload, rearm, or launch certain systems. |
| Mission | A persistent order that tells units what to do, where to operate, what rules to follow, and when to stop. In this project, missions are also agent-editable plans with validation, rationale, and runtime supervision. |
| Formation | A coordinated arrangement of units with a central element and one or more escorts or supporting elements. Formations are used when protection, spacing, and mutual support matter as much as the destination. |
| Rules of Engagement | The policy that governs when a unit may observe, challenge, shadow, fire, or disengage. In this project, ROE is always visible as a mission constraint, not a hidden background setting. |
| Doctrine | The broader behavioral policy that shapes how a unit or mission should act when the player has not specified every detail. Doctrine inherits downward through side, formation, and mission layers unless explicitly overridden. |
| EMCON | Emissions control posture, meaning how active or quiet a unit should be with radar, sonar, datalinks, and other emitters. EMCON is treated here as a phase-based tactical choice with agent explanations and risk warnings. |
| Event | A scenario rule that responds to a condition and performs an action. Events are how the game turns world state, score, time, or contact changes into scripted consequences. |
| Trigger | The condition that starts an event or mission change. Triggers can be time-based, contact-based, score-based, area-based, or state-based, and they should be readable enough for scenario authors to audit. |
| Sensor | Any system that detects or identifies something else in the battlespace. Sensors can be active or passive, and their value depends on range, environment, emissions, and target behavior. |
| Radar | An active electromagnetic sensor used to detect and classify objects at long and medium range. Radar is powerful, but it creates emissions that can be detected or degraded by enemy countermeasures. |
| Sonar | A sound-based sensing system used mainly in the maritime and submarine domain. Sonar can listen passively or transmit actively, which trades stealth for detection power. |
| Electro-Optical Sensor | A passive visual or infrared sensor that does not emit energy of its own. It is useful for low-signature detection, but it is constrained by range, weather, light, and line of sight. |
| ESM | Electronic support measures, meaning systems that detect and interpret enemy emissions instead of directly sensing the target itself. ESM is especially valuable for passive surveillance and early warning. |
| Passive Sensor | A sensor that observes without broadcasting energy. Passive sensing improves survivability and stealth, but usually gives up some range or certainty. |
| Active Sensor | A sensor that emits energy in order to detect or classify targets. Active sensing increases awareness, but it also increases the chance of being detected in return. |
| Contact | A detected object that has not yet been fully identified. Contacts are the bridge between raw sensor returns and confirmed tactical understanding. |
| Readiness | The current ability of a unit to carry out its assigned task without delay. Readiness reflects fuel, weapons, damage, crew state, maintenance state, and platform availability. |
| Task Force | A mission-oriented collection of ships, aircraft, submarines, or land units operating under a shared purpose. In this project, task forces can be manually assembled or proposed by the mission agent. |

## Technology Level (TL)

From [09-Near-Future-Technologies](09-Near-Future-Technologies.md) and [10-Speculative-Systems](10-Speculative-Systems.md). Scenarios bind catalog entities to a TL gate.

| TL | Era | Scope |
|----|-----|-------|
| **TL-0** | 2025 baseline | Current fielded systems only |
| **TL-1** | 2026–2028 | Early fielding |
| **TL-2** | 2028–2030 | Primary near-future release target |
| **TL-3** | 2030–2032 | Advanced near-future |
| **TL-4+** | 2035+ | Speculative / black-project (doc 10, `BLACK_PROJECT_MODE`) |

**TRL** (Technology Readiness Level) gates import via `CatalogImportGate` — distinct from scenario **TL** binding.

## Project Terms

| Term | Definition |
| --- | --- |
| Mission Graph | The structured internal representation of a mission, including objectives, assets, triggers, geometry, doctrine, dependencies, and contingency branches. |
| Autonomy Mode | The degree to which the mission agent may propose, edit, or execute changes without asking the player first. |
| Validation Blocker | A hard error that prevents mission execution until the player resolves it. |
| Advisory Warning | A non-blocking warning that the mission is legal but risky, inefficient, or fragile. |
| Provenance | The origin of a mission field or recommendation, such as user input, doctrine inheritance, scenario template, database fact, or agent inference. |
| Contingency Branch | A predefined fallback path that can be taken if the original mission plan breaks, fails, or becomes unsafe. |
| Policy Snapshot | A frozen, versioned copy of doctrine, ROE, EMCON, and WRA settings applied to a unit or agent for deterministic evaluation (see doc 13). |
| Fire Abort Reason | A machine-readable code explaining why an engagement was not authorized (see doc 14). |
| Order Log Entry | One immutable record in the deterministic timeline of commands, agent intents, doctrine blocks, and event firings (see doc 17). |
| Logistics Abort Reason | A machine-readable code explaining why an operation cannot proceed due to fuel, magazine, readiness, or basing constraints (see doc 16). |
| Comms State | The health of a communications or datalink channel—up, degraded, or down—including delay and track staleness effects (see doc 19). |

## Glossary Notes

- Terms are written to support both player-facing UI text and scenario-designer documentation.
- Where the simulation has a strict technical meaning, that meaning takes priority over casual language.
- Where the project introduces a new agentic concept, the definition should explain how it differs from a legacy manual workflow.
