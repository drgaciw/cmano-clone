# Architecture Traceability Index

**Last Updated:** 2026-07-08
**Engine:** Unity 6.3 LTS (6000.3.14f1) + .NET 8  
**Review:** [architecture-review-2026-06-02.md](architecture-review-2026-06-02.md)  
**TR IDs:** [tr-registry.yaml](tr-registry.yaml)

## Coverage Summary

- **Total requirements:** 47
- **Covered:** 15 (32%)
- **Partial:** 20 (43%)
- **Gaps:** 12 (25%)
- **Current gates:** solution tests ≥1232; ReplayGolden 6/6; PlayModeSmoke 18/18; hash `17144800277401907079` — see [requirements-traceability.md](requirements-traceability.md) header. Historical Sprint 11–15 closeout (2026-06-08) cited `403/403` / PlayMode **7/7** only as program evidence, not live floors.
- **Sprint 11–15 program:** **CLOSED** @ 2026-06-08 — requirements maturity (docs 01–12) + Wave 5 on `main`; tracker rows **14/16/19/20** at **Partial+** with automated AC (**historical** `403/403` / **7/7**). See [requirements-traceability.md](requirements-traceability.md) Wave 5 overlap spine.
- **Platform editor (FR-19 / req 21):** ADR-011 Partial — see [requirements-traceability.md](requirements-traceability.md) § Platform editor.

## Full Matrix

| Requirement ID | GDD | System | Requirement | ADR Coverage | Status |
|----------------|-----|--------|-------------|--------------|--------|
| TR-simcore-001 | simulation-core-time.md | Sim Core | Fixed timestep | ADR-001, ADR-004 | Covered |
| TR-simcore-002 | simulation-core-time.md | Sim Core | Global seed + domain RNG | ADR-001 | Covered |
| TR-simcore-003 | simulation-core-time.md | Sim Core | Headless runner API | ADR-001 | Partial |
| TR-simcore-004 | simulation-core-time.md | Sim Core | Time compression hooks | ADR-004 | Partial |
| TR-simcore-005 | simulation-core-time.md | Sim Core | World hash per tick/checkpoint | ADR-003, ADR-004 | Partial |
| TR-log-001 | order-log-replay.md | Order Log | Append-only ordered log | ADR-003 | Covered |
| TR-log-002 | order-log-replay.md | Order Log | Entry type union | ADR-003 | Covered |
| TR-log-003 | order-log-replay.md | Order Log | Replay fingerprint + golden CI | ADR-003 | Covered |
| TR-policy-001 | policy-roe-emcon-wra.md | Policy | Inheritance order fixed/cached | ADR-002 | Covered |
| TR-policy-002 | policy-roe-emcon-wra.md | Policy | Policy snapshot on agent assign | ADR-002 | Covered |
| TR-policy-003 | policy-roe-emcon-wra.md | Policy | FireAbortReason on denials | ADR-002, ADR-003 | Covered |
| TR-policy-004 | policy-roe-emcon-wra.md | Policy | PolicyUpdate in order log | ADR-003 | Covered |
| TR-policy-005 | policy-roe-emcon-wra.md | Policy | WRA before engagement geometry | ADR-002, ADR-004 | Partial |
| TR-policy-006 | policy-roe-emcon-wra.md | Policy | EMCON feeds sensor emission | ADR-002 | Partial |
| TR-sensor-001 | sensor-detection-ew.md | Sensors | Contact FSM | ADR-004, ADR-005 | Covered |
| TR-sensor-002 | sensor-detection-ew.md | Sensors | Deterministic detection loop | ADR-004, ADR-005 | Partial |
| TR-sensor-003 | sensor-detection-ew.md | Sensors | EW noise jam MVP | — | Partial |
| TR-sensor-004 | sensor-detection-ew.md | Sensors | Side picture / datalink | — | Gap |
| TR-engage-001 | engagement-fire-control.md | Engage | Unified resolver | ADR-001, ADR-004 | Covered |
| TR-engage-002 | engagement-fire-control.md | Engage | DLZ state + logging | — | Partial |
| TR-engage-003 | engagement-fire-control.md | Engage | Swarm slot order (P1) | — | Gap |
| TR-logistics-001 | logistics-magazines.md | Logistics | Magazine ledger + empty abort | ADR-004 | Partial |
| TR-logistics-002 | logistics-magazines.md | Logistics | MagazineChange in order log | ADR-003 | Covered |
| TR-logistics-003 | logistics-magazines.md | Logistics | Deterministic fuel burn | — | Gap |
| TR-logistics-004 | logistics-magazines.md | Logistics | Editor fuel validation | ADR-006 | Partial |
| TR-combat-dom-001 | combat-domains-damage.md | Combat | Domain validator plug-in | ADR-009 | Partial |
| TR-combat-dom-002 | combat-domains-damage.md | Combat | Deterministic damage order | ADR-009 | Partial |
| TR-combat-dom-003 | combat-domains-damage.md | Combat | BDA feeds contact picture | ADR-009 | Partial |
| TR-c2-001 | command-and-control-ui.md | C2 UI | Left drawer tabs | ADR-007 | Partial |
| TR-c2-002 | command-and-control-ui.md | C2 UI | Full message log | ADR-003 | Partial |
| TR-c2-003 | command-and-control-ui.md | C2 UI | Right unit detail | — | Partial |
| TR-c2-004 | command-and-control-ui.md | C2 UI | Globe map P0 | ADR-007 | Partial |
| TR-score-001 | scoring-losses.md | Scoring | Kill-based projection | — | Partial |
| TR-score-002 | scoring-losses.md | Scoring | Magazine expenditure tally | — | Partial |
| TR-score-003 | scoring-losses.md | Scoring | Headless CSV export (P1) | — | Partial |
| TR-cyber-001 | cyber-comms-degradation.md | Cyber | Comms state machine | — | Partial |
| TR-cyber-002 | cyber-comms-degradation.md | Cyber | Comms/Cyber order-log entries | ADR-003 | Partial |
| TR-cyber-003 | cyber-comms-degradation.md | Cyber | CommsDenied fire abort | ADR-002 | Partial |
| TR-cyber-004 | cyber-comms-degradation.md | Cyber | C2 comms projection | ADR-007 | Partial |
| TR-agentic-001 | agentic-infrastructure.md | Agentic | Batch runner + CSV/fingerprint | — | Partial |
| TR-agentic-002 | agentic-infrastructure.md | Agentic | Hindsight hook (P1) | — | Gap |
| TR-agentic-003 | agentic-infrastructure.md | Agentic | AAR read-only agents (P1) | — | Gap |
| TR-editor-001 | agentic-mission-editor.md | Editor | Canonical scenario / intent compiler | ADR-006 | Partial |
| TR-editor-002 | agentic-mission-editor.md | Editor | Deterministic Validation Engine | ADR-008 | Covered |
| TR-editor-003 | agentic-mission-editor.md | Editor | fire_order + world-state hash | ADR-001, ADR-004 | Partial |
| TR-editor-004 | agentic-mission-editor.md | Editor | editVersion conflict-reject | ADR-008 | Partial |
| TR-editor-005 | agentic-mission-editor.md | Editor | MCP export gate / headless sample | ADR-008 | Covered |

## Known Gaps

| TR-ID | Domain | Suggested action |
|-------|--------|------------------|
| TR-sensor-004 | Sensors | `/architecture-decision sensor-side-picture` |
| TR-logistics-003 | Logistics | `/architecture-decision logistics-fuel-model` |
| TR-combat-dom-001..003 | Combat | ADR-009 Proposed — implement `IDomainValidator` + damage order |
| TR-editor-004 | Editor | editVersion persistence (guard only) |
| TR-engage-003 | Engage | P1 — defer or engage ADR amendment |
| TR-agentic-002..003 | Agentic | P1 — `/architecture-decision agentic-aar-infrastructure` |
| Systems #9, #15, #19 | Systems index | GDD + ADR backlog |

## Systems Without GDD

| System # | Name | ADR | Notes |
|----------|------|-----|-------|
| 4 | Platform Database | ADR-006 Accepted | Implement DATA-2..5 per migration plan |
| 9 | Mission Runtime | — | Needs GDD + ADR |
| 10 | Agent Delegation | ADR-001 | Boundary only |
| 15 | Near-Future Systems | — | Vertical slice |
| 19 | Speculative Systems | — | Full vision |
| 20 | Database Intelligence Pipeline | ADR-006 Accepted | Tied to #4 |

## Superseded Requirements

None identified this review.

## Documentation Conflicts (fix separately)

- [requirements-traceability.md](requirements-traceability.md) labels ADR-005 as engagement; file is DOTS/ECS.