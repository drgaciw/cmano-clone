# Agentic Clone Blueprint for a Command: Modern Air/Naval Operations–Style Wargame

## Scope
This document summarizes publicly visible information relevant to building an agentic-enhanced fork or spiritual successor inspired by *Command: Modern Air/Naval Operations* (CMANO) and *Command: Modern Operations* (CMO), with emphasis on development process, tooling, technology signals, database workflows, and team structure.[cite:18][cite:21][cite:30]

The goal is not to replicate proprietary code or assets, but to extract architectural and product lessons that can inform a clean-room implementation for a new system.[cite:18][cite:21][cite:24]

## What the public record shows
CMANO was released in 2013 by WarfareSims and published by Matrix Games, while CMO arrived in 2019 as a successor built around the same general simulation paradigm with an updated interface and additional capabilities.[cite:18][cite:21][cite:30]

Dimitris Dranidis publicly identified himself as co-founder and head of development at WarfareSims, and described the broader team as globally distributed across many countries, which implies a small specialist core supported by remote contributors rather than a large centralized studio.[cite:30]

The public product split is important: the consumer game line emphasizes usability and gameplay, while the professional edition emphasizes simulation-first workflows and advanced tools for professional users.[cite:24][cite:25][cite:30]

## Inferred product architecture
The public materials consistently describe Command as a 2D operational simulation built on a long-lived custom engine, with scenario execution, mission planning, database-driven unit modeling, and time-accelerated simulation as the core pillars.[cite:18][cite:21][cite:25]

CMO preserved the underlying simulation core while improving interface and user experience, which suggests a layered architecture where the simulation engine, scenario model, databases, and UI can evolve semi-independently.[cite:21][cite:30]

A clean-room clone should therefore separate at least five major subsystems:
- Simulation kernel, for time advancement, detection, kinematics, weapons, fuel, comms, and doctrine logic.
- Entity database, for platforms, sensors, weapons, facilities, signatures, mounts, and metadata.
- Scenario layer, for orders of battle, geography, events, triggers, and victory conditions.
- UX layer, for map rendering, timelines, sidebars, planners, and agent supervision tools.
- Automation layer, for scripted behaviors, batch analysis, orchestration, and human-in-the-loop review.[cite:18][cite:24][cite:25][cite:30]

## Publicly visible tooling signals
The professional edition is publicly described as including a scenario editor, a database editor, Monte Carlo analysis, and data import/export functions.[cite:24][cite:25][cite:26][cite:27]

A public Matrix Pro Sims presentation also describes interactive and non-interactive Monte Carlo analysis, exporting complete scenario state, importing external data in XML format, and generating speedy results from a high-performance simulation engine.[cite:26][cite:31]

Secondary public descriptions additionally reference Lua TCP/IP connectivity in the professional edition, which is a strong signal that the simulation can be externally driven or integrated with other systems even if the exact API contract is not publicly documented in the sources gathered here.[cite:28]

For a modern fork, the equivalent tooling stack should include:
- A structured simulation API, ideally local-first and scriptable.
- Scenario import/export in a stable interchange format such as JSON or protobuf; XML compatibility can be added for interoperability.[cite:26][cite:31]
- A first-class database editor with validation rules and provenance fields.[cite:24][cite:25]
- Batch execution and experiment runners for Monte Carlo, regression, and doctrine testing.[cite:24][cite:27][cite:31]
- External telemetry export for 3D replay, analytics, and after-action review.[cite:18]

## Database workflow lessons
One of the clearest public windows into Command’s development process is the database workflow. The CMO database request repository on GitHub is explicitly an issue-only public tracker used to replace older forum request threads.[cite:29]

The repository states that it is only one of two trackers used by the database team, with the other being private or internal, and that ticket handling is only one part of the broader database workflow.[cite:29]

That statement is especially important for a clone effort because it reveals a dual-track content pipeline:
- Public intake channel for community requests and corrections.[cite:29]
- Private/internal tracker for coordinated work, larger overhauls, and non-public priorities.[cite:29]
- Out-of-order handling driven by broader database objectives rather than simple FIFO ticket processing.[cite:29]

The public tracker also shows a process bias toward evidence-rich issue intake using templates, discussions, labels such as help wanted, and a contributor model that does not require pull requests or direct database editing access.[cite:29]

For a fork, this is a strong pattern to reuse. A robust database pipeline should include:
1. Public evidence-backed issue submission for new units, corrections, doctrine changes, and sensor/weapon updates.
2. Internal curation queue with triage states such as accepted, needs evidence, deferred, merged into overhaul, blocked, or rejected.
3. Schema-aware editing tools that prevent invalid combinations of mounts, sensors, dates, and magazine contents.
4. Provenance metadata for every field, including source link, confidence, reviewer, and revision date.
5. Release trains for database drops separate from engine releases, so data can ship more frequently than code.[cite:29][cite:35][cite:37]

## Agentic enhancement opportunities
An agentic fork can improve precisely where Command appears to rely on expert human operators and database maintainers.[cite:24][cite:29][cite:30]

### Database agents
Agents can ingest open-source defense reporting, extract candidate entities and parameter changes, normalize terminology, and draft change proposals with confidence scores and supporting evidence for human review.[cite:29][cite:37]

The correct posture is “propose, not auto-merge.” Military data is noisy, contradictory, and politically distorted, so the agent should function as a research assistant and validator, not the final authority.[cite:29][cite:37]

Recommended workflow:
- Retrieval agent gathers articles, manuals, procurement notices, satellite imagery references, and prior issue history.
- Entity resolution agent maps aliases and variants to canonical platform IDs.
- Diff agent proposes additions or edits against the current DB state.
- Rules agent runs schema and temporal consistency checks.
- Human reviewer approves or rejects the patch bundle.

### Scenario agents
Scenario generation can be partially automated by agents that assemble plausible orders of battle, logistics states, political constraints, and initial mission packages from a scenario brief.[cite:18][cite:24]

Another agent layer can create red-team plans, adversary doctrine variants, and branch scenarios for Monte Carlo experimentation, producing a richer test harness than hand-authored scenarios alone.[cite:24][cite:27][cite:31]

### Operator copilots
A player or analyst copilot can sit above the simulation and help with:
- Course-of-action generation.
- Doctrine explanation.
- Sensor and weapon employment suggestions.
- Alert triage and prioritization.
- Natural-language querying over the scenario state.

This agent must remain supervised and reversible, with every recommendation linked to transparent state evidence rather than hidden black-box actions.

## Clean-room technical design
A modern implementation inspired by Command should avoid monolithic desktop-era assumptions and instead expose the sim as a service-oriented core, even if it still ships as a desktop app.[cite:21][cite:24][cite:30]

### Suggested architecture
| Layer | Responsibilities | Suggested implementation direction |
|---|---|---|
| Simulation core | Time step / event queue, kinematics, detection, weapon resolution, fuel, comms, doctrine | Rust, C++, or high-performance C# engine with deterministic stepping |
| Domain model | Platforms, sensors, weapons, facilities, emissions, damage states | Strongly typed schemas, versioned IDs, validation-rich model layer |
| Scenario system | Sides, geography, orders of battle, triggers, events, objectives | Human-editable JSON/YAML plus compiled binary cache |
| DB workflow | Requests, review, provenance, release packaging | Git-based content repo plus admin UI and review queue |
| Agent runtime | Research, planning, triage, explanations, batch assistants | Python or TypeScript services around the core engine |
| UX | 2D map, planners, inspectors, timeline, event log, copilot chat | Desktop web stack or game UI framework with map-centric layout |

The most important clean-room decision is to make the database and scenario systems auditable and testable as first-class products rather than as support files hidden behind the executable.[cite:24][cite:29]

### Data model direction
The Command lineage strongly suggests a large relational or graph-like equipment model with many cross-references across platforms, mounts, weapons, sensors, dates, and doctrine availability windows.[cite:18][cite:24][cite:29]

For a new build, use:
- Canonical immutable IDs.
- Temporal validity windows for capabilities and variants.
- Provenance per field, not only per record.
- Constraint rules for incompatible loadouts and platform generations.
- Explicit separation between source facts, interpreted values, and gameplay abstractions.

## Simulation and batch analysis
The professional edition’s Monte Carlo positioning is especially useful for an agentic clone because it points to a high-value capability beyond single-playthrough simulation: repeated execution for sensitivity analysis and doctrine comparison.[cite:24][cite:26][cite:31]

A fork should formalize this as a reproducible experiment system:
- Scenario seed control.
- Parameter sweep definitions.
- Batch-run workers.
- Statistical output and artifact storage.
- Automatic report generation for kill chains, sortie effectiveness, loss distributions, and mission completion rates.

This is where agents add significant leverage. An experiment agent can generate parameter matrices, launch batches, detect anomalies, summarize results, and propose follow-up tests for a human analyst.[cite:24][cite:27][cite:31]

## Team and operating model
The public team description suggests that Command succeeds through concentrated domain expertise, distributed collaboration, and a separation between game-facing and professional-facing priorities.[cite:30]

A clone effort should likely organize around these roles:
- Simulation lead, responsible for determinism, correctness, and performance.
- Data lead, responsible for schema, provenance, release trains, and review quality.
- UX/product lead, responsible for accessibility of a very dense operational workflow.
- Agent systems lead, responsible for retrieval, orchestration, safety rails, and observability.
- Domain reviewers, including aviation, naval, missile, EW, and doctrine specialists.
- Scenario team, responsible for test content and benchmark problems.

The highest-risk failure mode is not rendering or UI polish; it is uncontrolled complexity in the equipment database and doctrine logic. That is the area where staffing, workflow, and tooling discipline matter most.[cite:24][cite:29][cite:30]

## Build roadmap
### Phase 1: simulation kernel
Implement 2D map, units, movement, sensors, tracks, basic weapons, time controls, and deterministic saves. Keep the first vertical slice small: one airbase, one surface action group, one threat envelope, one mission cycle.

### Phase 2: database platform
Create the canonical schema, evidence model, editor UI, validation engine, and Git-backed public request workflow modeled after the Command DB request process.[cite:29]

### Phase 3: scenario and automation
Add scenario editor, trigger/event system, import/export, replay, and initial batch execution tools inspired by the professional edition feature set.[cite:24][cite:25][cite:26]

### Phase 4: agentic layer
Add research assistants for DB updates, scenario drafting agents, operator copilots, and experiment orchestration over batch runs.

### Phase 5: pro workflow
Add analyst-grade Monte Carlo management, external system connectors, richer report generation, and approval workflows for institutional users.[cite:24][cite:25][cite:31]

## Non-goals and legal boundaries
A clone should not reuse proprietary databases, scenario content, code, art, or manuals from the Command products.[cite:18][cite:21][cite:24]

Instead, it should reuse publicly observable product patterns: database-driven simulation, scenario-centric workflows, split consumer/pro tooling, community-backed issue intake, and simulation-plus-analysis integration.[cite:24][cite:29][cite:30]

## Practical design principles
- Keep the simulation core deterministic.
- Treat data provenance as a product feature.
- Make agent outputs reviewable, reversible, and evidence-linked.
- Separate engine releases from database releases.[cite:29][cite:35][cite:37]
- Design the UI for analysts first, then add onboarding layers for new users.[cite:30]
- Build batch experimentation early; it differentiates the platform from a game-only clone.[cite:24][cite:31]

## Sources most worth reading next
- The CMO developer interview for product direction and team structure.[cite:30]
- The Command Professional Edition material for professional features and analysis tooling.[cite:24][cite:25][cite:31]
- The public CMO DB request tracker for evidence of the content maintenance pipeline.[cite:29]
