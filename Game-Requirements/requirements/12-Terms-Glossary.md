# 12 - Terms Glossary

**Last Updated:** 2026-06-04  
**Related:** [13](13-Doctrine-ROE-EMCON-WRA.md)–[20](20-Command-And-Control-UI.md) (simulation slice)  
**Status:** Locked (Sprint 15)

**Purpose:**
This glossary defines the core simulation terms used by the project, starting from the CMO manual's opening terminology and adapting them into a clearer, more agent-friendly vocabulary for this game.

**Reference Reviewed:**
[Command: Modern Operations manual PDF](https://www.matrixgames.com/amazon/PDF/CMO/CMO_manual_EBOOK.pdf), starting at page 19.

## Glossary

| Term | Definition | Authoritative source |
| --- | --- | --- |
| Unit | A single simulated entity that can act, be controlled, or be carried by another entity. Units include aircraft, ships, submarines, ground platforms, and some special entities such as sensors, weapons, facilities, and support objects. | CMO manual §2 |
| Group | A collection of units that the simulation treats as a coordinated package. Groups are useful when a formation, flight, or task force should move and act as one operational element. | CMO manual §2 |
| Mount | A physical installation on a unit that holds a weapon or sensor. A mount is what makes the equipment usable in the simulation, and it may depend on ammo, magazines, or platform state. | CMO manual §2; [06](06-Database-Intelligence.md) |
| Magazine | A storage location for weapons, ammunition, or stores. Magazines feed mounts and define whether a unit can reload, rearm, or launch certain systems. | CMO manual §2; [16](16-Logistics-And-Magazines.md) |
| Mission | A persistent order that tells units what to do, where to operate, what rules to follow, and when to stop. In this project, missions are also agent-editable plans with validation, rationale, and runtime supervision. | CMO manual §2; [11](11-Agentic-Mission-Editor.md) |
| Formation | A coordinated arrangement of units with a central element and one or more escorts or supporting elements. Formations are used when protection, spacing, and mutual support matter as much as the destination. | CMO manual §2 |
| Rules of Engagement | The policy that governs when a unit may observe, challenge, shadow, fire, or disengage. In this project, ROE is always visible as a mission constraint, not a hidden background setting. | CMO manual §2; [13](13-Doctrine-ROE-EMCON-WRA.md) |
| Doctrine | The broader behavioral policy that shapes how a unit or mission should act when the player has not specified every detail. Doctrine inherits downward through side, formation, and mission layers unless explicitly overridden. | CMO manual §2; [13](13-Doctrine-ROE-EMCON-WRA.md) |
| EMCON | Emissions control posture, meaning how active or quiet a unit should be with radar, sonar, datalinks, and other emitters. EMCON is treated here as a phase-based tactical choice with agent explanations and risk warnings. | [13](13-Doctrine-ROE-EMCON-WRA.md); [15](15-Sensor-Detection-And-EW.md) |
| Event | A scenario rule that responds to a condition and performs an action. Events are how the game turns world state, score, time, or contact changes into scripted consequences. | CMO manual §2; [11](11-Agentic-Mission-Editor.md) |
| Trigger | The condition that starts an event or mission change. Triggers can be time-based, contact-based, score-based, area-based, or state-based, and they should be readable enough for scenario authors to audit. | CMO manual §2; [11](11-Agentic-Mission-Editor.md) |
| Sensor | Any system that detects or identifies something else in the battlespace. Sensors can be active or passive, and their value depends on range, environment, emissions, and target behavior. | CMO manual §2; [15](15-Sensor-Detection-And-EW.md) |
| Radar | An active electromagnetic sensor used to detect and classify objects at long and medium range. Radar is powerful, but it creates emissions that can be detected or degraded by enemy countermeasures. | CMO manual §2; [15](15-Sensor-Detection-And-EW.md) |
| Sonar | A sound-based sensing system used mainly in the maritime and submarine domain. Sonar can listen passively or transmit actively, which trades stealth for detection power. | CMO manual §2; [15](15-Sensor-Detection-And-EW.md) |
| Electro-Optical Sensor | A passive visual or infrared sensor that does not emit energy of its own. It is useful for low-signature detection, but it is constrained by range, weather, light, and line of sight. | CMO manual §2; [15](15-Sensor-Detection-And-EW.md) |
| ESM | Electronic support measures, meaning systems that detect and interpret enemy emissions instead of directly sensing the target itself. ESM is especially valuable for passive surveillance and early warning. | CMO manual §2; [15](15-Sensor-Detection-And-EW.md) |
| Passive Sensor | A sensor that observes without broadcasting energy. Passive sensing improves survivability and stealth, but usually gives up some range or certainty. | CMO manual §2; [15](15-Sensor-Detection-And-EW.md) |
| Active Sensor | A sensor that emits energy in order to detect or classify targets. Active sensing increases awareness, but it also increases the chance of being detected in return. | CMO manual §2; [15](15-Sensor-Detection-And-EW.md) |
| Contact | A detected object that has not yet been fully identified. Contacts are the bridge between raw sensor returns and confirmed tactical understanding. | CMO manual §2; [15](15-Sensor-Detection-And-EW.md) |
| Readiness | The current ability of a unit to carry out its assigned task without delay. Readiness reflects fuel, weapons, damage, crew state, maintenance state, and platform availability. | CMO manual §2; [16](16-Logistics-And-Magazines.md) |
| Task Force | A mission-oriented collection of ships, aircraft, submarines, or land units operating under a shared purpose. In this project, task forces can be manually assembled or proposed by the mission agent. | CMO manual §2; [11](11-Agentic-Mission-Editor.md) |

## Technology Level (TL)

From [09-Near-Future-Technologies](09-Near-Future-Technologies.md) and [10-Speculative-Systems](10-Speculative-Systems.md). Scenarios bind catalog entities to a TL gate.

| TL | Era | Scope | Authoritative source |
|----|-----|-------|---------------------|
| **TL-0** | 2025 baseline | Current fielded systems only | [09](09-Near-Future-Technologies.md) |
| **TL-1** | 2026–2028 | Early fielding | [09](09-Near-Future-Technologies.md) |
| **TL-2** | 2028–2030 | Primary near-future release target | [09](09-Near-Future-Technologies.md) |
| **TL-3** | 2030–2032 | Advanced near-future | [09](09-Near-Future-Technologies.md) |
| **TL-4+** | 2035+ | Speculative / black-project (doc 10, `BLACK_PROJECT_MODE`) | [10](10-Speculative-Systems.md) |

**TRL** (Technology Readiness Level) gates import via `CatalogImportGate` — distinct from scenario **TL** binding. Authoritative source: [09](09-Near-Future-Technologies.md).

## Project Terms

| Term | Definition | Authoritative source |
| --- | --- | --- |
| Mission Graph | The structured internal representation of a mission, including objectives, assets, triggers, geometry, doctrine, dependencies, and contingency branches. | [11](11-Agentic-Mission-Editor.md) |
| Autonomy Mode | The degree to which the mission agent may propose, edit, or execute changes without asking the player first. | [04](04-Agent-Delegation.md) |
| Validation Blocker | A hard error that prevents mission execution until the player resolves it. | [11](11-Agentic-Mission-Editor.md) |
| Advisory Warning | A non-blocking warning that the mission is legal but risky, inefficient, or fragile. | [11](11-Agentic-Mission-Editor.md) |
| Provenance | The origin of a mission field or recommendation, such as user input, doctrine inheritance, scenario template, database fact, or agent inference. | [11](11-Agentic-Mission-Editor.md) |
| Contingency Branch | A predefined fallback path that can be taken if the original mission plan breaks, fails, or becomes unsafe. | [11](11-Agentic-Mission-Editor.md) |
| Policy Snapshot | A frozen, versioned copy of doctrine, ROE, EMCON, and WRA settings applied to a unit or agent for deterministic evaluation. | [13](13-Doctrine-ROE-EMCON-WRA.md) |
| Fire Abort Reason | A machine-readable code explaining why an engagement was not authorized. | [13](13-Doctrine-ROE-EMCON-WRA.md), [14](14-Engagement-And-Fire-Control.md) |
| Order Log Entry | One immutable record in the deterministic timeline of commands, agent intents, doctrine blocks, and event firings. | [17](17-Replay-AAR-And-Order-Log.md) |
| Logistics Abort Reason | A machine-readable code explaining why an operation cannot proceed due to fuel, magazine, readiness, or basing constraints. | [16](16-Logistics-And-Magazines.md) |
| Comms State | The health of a communications or datalink channel—up, degraded, or down—including delay and track staleness effects. | [19](19-Cyber-And-Comms.md) |
| **CYBER_SPOOF_TRACK** | Message-log / order-log code emitted when engage is blocked because the fire-control track is marked **spoofed** (`TrackSpoofed` → `CYBER_SPOOF_TRACK` in `abort_reason_manifest.json`). | [19](19-Cyber-And-Comms.md), [14](14-Engagement-And-Fire-Control.md) |
| **Spoof Track** | A scenario-driven false or corrupted contact introduced by cyber/EW (`SpoofTracks` policy timeline); when active on a shooter’s track, manual and agent engage abort with **CYBER_SPOOF_TRACK**. | [19](19-Cyber-And-Comms.md) |
| **AIR_NOT_READY** | Message-log / engage-abort code when `ReadyForLaunch == false` for the shooting unit (live readiness gate before the engagement pipeline commits). | [16](16-Logistics-And-Magazines.md), [14](14-Engagement-And-Fire-Control.md) |
| **Live Readiness** | Runtime readiness from scenario/session state (`UnitReadiness` / `ReadyForLaunch`), evaluated during play—not only at export validation—so sortie and launch gates affect engage and the attack menu immediately. | [16](16-Logistics-And-Magazines.md) |
| **Attack Menu** | Unit-detail and context-menu controls for priming engage: weapon/mount context, **Fire 1**, **Salvo**, and abort preview before commit (CMO attack-options parity). | [14](14-Engagement-And-Fire-Control.md), [20](20-Command-And-Control-UI.md) |
| **Fire 1** | Interactive attack option that primes a single-round engage (`SalvoSize = 1`); UI label “Fire 1” / “Fire 1 round”. | [14](14-Engagement-And-Fire-Control.md), [20](20-Command-And-Control-UI.md) |
| **Salvo** | Multi-round engage quantity for one firing action; capped by WRA `maxSalvo`, magazine depth, and deterministic salvo deconfliction across swarm shooters. | [13](13-Doctrine-ROE-EMCON-WRA.md), [14](14-Engagement-And-Fire-Control.md) |
| **Hold Fire** | ROE state that prohibits weapons release; denials log `ROE_HOLD_FIRE` / `FireAbortReason.RoeHoldFire` and grey out attack options even when geometry and magazines allow a shot. | [13](13-Doctrine-ROE-EMCON-WRA.md), [14](14-Engagement-And-Fire-Control.md) |

## Terms from Simulation Slice (13–20)

One-line pointers only—full rules, enums, and acceptance criteria live in the linked requirement doc (no orphan definitions).

| Term | Summary (see authoritative doc for full spec) | Authoritative source |
| --- | --- | --- |
| **EMCON** | Per-emitter off / passive / active posture; gates detection (doc 15) and illuminate-for-fire (doc 14). | [13](13-Doctrine-ROE-EMCON-WRA.md) |
| **WRA** | Weapon release authority: salvo limits, range bands, and target categories evaluated before engage resolution. | [13](13-Doctrine-ROE-EMCON-WRA.md) |
| **ROE** | Hold fire, weapons tight, weapons free, and fine-grained engage gates shared by player and agents. | [13](13-Doctrine-ROE-EMCON-WRA.md) |
| **Policy Snapshot** | Immutable policy contract captured at delegation time; referenced on denials and replay. | [13](13-Doctrine-ROE-EMCON-WRA.md) |
| **FireAbortReason** | Canonical enum + manifest codes (`ROE_*`, `WRA_*`, `EMCON_*`, `DLZ_*`, logistics/cyber extensions) for blocked fire. | [13](13-Doctrine-ROE-EMCON-WRA.md), [14](14-Engagement-And-Fire-Control.md) |
| **DLZ** | Dynamic launch zone: in / approaching / out / unknown envelope state for shooter–target–weapon triples. | [14](14-Engagement-And-Fire-Control.md) |
| **ContactChange** | Order-log event type for detect, update, classify, identify, and lost contact transitions. | [17](17-Replay-AAR-And-Order-Log.md), [15](15-Sensor-Detection-And-EW.md) |
| **LogisticsAbortReason** | Parallel abort family for fuel, magazine, readiness, and basing blocks (e.g. **AIR_NOT_READY**). | [16](16-Logistics-And-Magazines.md) |
| **CommsStateChange** | Order-log entry when a datalink or strategic net moves up, degraded, or down (delay, staleness). | [19](19-Cyber-And-Comms.md) |
| **PolicyDenial** | Order-log row tying a blocked action to `FireAbortReason` and `policySnapshotId`. | [17](17-Replay-AAR-And-Order-Log.md), [13](13-Doctrine-ROE-EMCON-WRA.md) |

## Glossary Notes

- Terms are written to support both player-facing UI text and scenario-designer documentation.
- Where the simulation has a strict technical meaning, that meaning takes priority over casual language.
- Where the project introduces a new agentic concept, the definition should explain how it differs from a legacy manual workflow.
- **Authoritative source rule:** Every glossary table includes an **Authoritative source** column. If a term’s normative definition, enum, or acceptance criteria appear in another requirement doc (especially [13](13-Doctrine-ROE-EMCON-WRA.md)–[20](20-Command-And-Control-UI.md)), that doc is the source of truth; this file indexes and disambiguates only. Do not duplicate full specs here—link instead.
- Wave 5 runtime codes (**CYBER_SPOOF_TRACK**, **AIR_NOT_READY**) are also listed in `data/glossary/abort_reason_manifest.json`; requirement docs [14](14-Engagement-And-Fire-Control.md), [16](16-Logistics-And-Magazines.md), and [19](19-Cyber-And-Comms.md) define when they fire.