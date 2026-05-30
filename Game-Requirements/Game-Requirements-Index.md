# Game Requirements - Master Index

**Last Updated:** May 29, 2026

## Traceability

- [Research → Requirements Traceability](research-traceability.md) — maps `docs/research/*.md` to requirement docs (May 29, 2026)
- [CMO Manual Traceability Matrix](cmo-manual-traceability.md) — maps `docs/manual/` sections to requirement docs
- [CMO Manual (HTML)](../docs/manual/index.html)
- [Impact Analysis: Sim & C2 bundle (2026-05-29)](../docs/requirements/impact-sim-c2-requirements-2026-05-29.md) — GitNexus blast radius
- [Design Review: Requirements 13–20 (2026-05-29)](reviews/requirements-13-20-design-review-2026-05-29.md) — **CONCERNS** verdict
- [GitNexus Validation: Research → Requirements (2026-05-29)](reviews/research-gitnexus-validation-2026-05-29.md) — **CONCERNS**; code alignment for docs 01/04/06–10
- [Systems Index](../design/gdd/systems-index.md) — decomposition for GDD authoring
- [Master Architecture](../docs/architecture/architecture.md) — assemblies, tick pipeline, ADR index (2026-05-29)
- [Policy GDD](../design/gdd/policy-roe-emcon-wra.md) — implements req 13
- [Order Log GDD](../design/gdd/order-log-replay.md) — implements req 17; resolves DecisionLog migration (ADR-003)
- [Sim Core GDD](../design/gdd/simulation-core-time.md) — implements req 03/08
- [Sensor GDD](../design/gdd/sensor-detection-ew.md) — implements req 15
- [Unity 6.3 LTS pin](../docs/engine-reference/unity/VERSION.md)
- [Agentic CMO Research](../docs/research/agentic-cmano-research.md) — architecture, DB workflow, agent opportunities
- [Near-Future Tech Research](../docs/research/near-future-tech-research.md) — TRL 5–9 systems for doc 09
- [Speculative Systems Research](../docs/research/speculative-systems-research.md) — TL 3–5 content for doc 10
- [Delegation ↔ Sim wiring (ADR-002)](../docs/architecture/wiring-delegation-sim-2026-05-29.md) — snapshots, denials, engage pipeline, scenario ROE
- [Scenario policy IDs](../data/scenarios/scenario-policy-ids.md)

## Research-integrated requirements (May 29, 2026)

Docs **01, 04, 06, 07, 08, 09, 10** updated from `docs/research/*.md`. Full mapping: [research-traceability.md](research-traceability.md). Remaining gaps tracked for req **13–20** (hypersonic alert UI, Kessler meter, JADC2 entity, Monte Carlo schema).

## Core Documents

- [01-Project-Overview.md](requirements/01-Project-Overview.md)
- [02-Core-Gameplay-Loop.md](requirements/02-Core-Gameplay-Loop.md)
- [03-Simulation-Modes.md](requirements/03-Simulation-Modes.md)

## Agent & Intelligence Systems

- [04-Agent-Delegation.md](requirements/04-Agent-Delegation.md)
- [05-Dynamic-Systems-Agent.md](requirements/05-Dynamic-Systems-Agent.md)
- [06-Database-Intelligence.md](requirements/06-Database-Intelligence.md)

## Technical Architecture

- [07-Agentic-Infrastructure.md](requirements/07-Agentic-Infrastructure.md)
- [08-Agentic-Architecture.md](requirements/08-Agentic-Architecture.md)

## Content & Systems

- [09-Near-Future-Technologies.md](requirements/09-Near-Future-Technologies.md)
- [10-Speculative-Systems.md](requirements/10-Speculative-Systems.md)

## Authoring & Design Tools

- [11-Agentic-Mission-Editor.md](requirements/11-Agentic-Mission-Editor.md) — Agentic mission/scenario editor (CMO parity + NL/MCP authoring, validation agents, operations timeline)

## Reference

- [12-Terms-Glossary.md](requirements/12-Terms-Glossary.md) — Simulation vocabulary (CMO terms + Project Aegis extensions)

## Simulation, C2, and Combat (CMO manual coverage)

| Doc | Title | Status |
|-----|-------|--------|
| [13](requirements/13-Doctrine-ROE-EMCON-WRA.md) | Doctrine, ROE, EMCON, WRA | Draft |
| [14](requirements/14-Engagement-And-Fire-Control.md) | Engagement & fire control | Draft |
| [15](requirements/15-Sensor-Detection-And-EW.md) | Sensors, detection, EW | Draft |
| [16](requirements/16-Logistics-And-Magazines.md) | Logistics & magazines | Draft |
| [17](requirements/17-Replay-AAR-And-Order-Log.md) | Replay, AAR, order log | Draft |
| [18](requirements/18-Combat-Domains.md) | Combat domains & damage | Draft |
| [19](requirements/19-Cyber-And-Comms.md) | Cyber & communications | Draft |
| [20](requirements/20-Command-And-Control-UI.md) | Command & control UI | Draft |

## Reading order (for design review)

1. **12** Glossary → **13** Policy → **15** Sensors → **14** Engagement  
2. **16** Logistics → **18** Combat → **19** Cyber  
3. **20** UI → **17** Replay (ties systems to player evidence)  
4. **11** Mission editor (authoring) with **06** Database

## Next workflow steps

- Run `/design-review` on updated docs **01, 04, 06–10** + **13–20** for cross-consistency
- Propagate TL framework into **12** Glossary
- Run `/military-requirements-impact` on new gap entities (JADC2 node, C-UAS, hypersonic defense) before DB schema
- Update GDDs (sensor, engagement, policy) with mechanics from doc 09
