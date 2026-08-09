# Architecture Traceability Index

**Last Updated:** 2026-08-09 (swarm TR rows stub) (header + gate floors refreshed; requirement rows unchanged — see note)
**Engine:** Unity 6.3 LTS (6000.3.14f1) + .NET 8  
**Authority:** [architecture-re-matrix-post-s93-s96-2026-07-15.md](architecture-re-matrix-post-s93-s96-2026-07-15.md) (layer verdicts) · [architecture-review-2026-06-02.md](architecture-review-2026-06-02.md) (historical)  
**TR IDs:** [tr-registry.yaml](tr-registry.yaml)

> **Row split (2026-07-24, DRG-46):** `TR-engage-003` was one row covering two tiers, which hid a shipped P0 inside a `Gap` status. It is now split into **`TR-engage-003a`** (P0 first-claimant ordering — Covered, `SwarmSalvoDeconfliction`, golden-backed) and **`TR-engage-003b`** (P1 sector coordinator — Deferred). The registry id `TR-engage-003` in [`tr-registry.yaml`](tr-registry.yaml) is unchanged and now maps to both; renumbering it would ripple through 7 referencing documents for no benefit.

> **Refresh note (2026-07-24, DRG-41):** the gate floors below were stale — this index carried **≥1232** while the standing floor had risen to **≥1638**. Floors are now current. The **47 requirement rows below have NOT been re-assessed**; their Covered/Partial/Gap statuses date from 2026-07-08 and predate S94–S107. A full re-assessment is the deferred "full GDD→ADR re-matrix" item, formally accepted as deferred in [architecture-concerns-gate-2026-07-24.md](../../production/gate-checks/architecture-concerns-gate-2026-07-24.md). Do not cite the percentages below as current.

## Coverage Summary

- **Total requirements:** 47
- **Covered:** 15 (32%) — *as of 2026-07-08, not re-assessed*
- **Partial:** 20 (43%) — *as of 2026-07-08, not re-assessed*
- **Gaps:** 12 (25%) — *as of 2026-07-08, not re-assessed*
- **Current gates (2026-07-24):** solution tests **≥1638/0f**; ReplayGolden **6/6**; C2 proxy **≥20/20**; PlayModeSmoke 18/18; hash `17144800277401907079` (18 paths) — see [requirements-traceability.md](requirements-traceability.md) header.
- **ADR inventory (updated 2026-07-26):** **22 ADRs present** — ADR-001…ADR-011, ADR-013…ADR-022, plus `adr-simulation-session-frozen-hub-spirit1`. **ADR-012 is absent — a numbering gap, not a missing decision.** New this cycle: **018** sensor side picture / datalink · **019** agentic AAR read-only order log · **020** logistics fuel model · **021** mission timeline runtime · **022** target OS and CPU architectures (renumbered from a duplicate 018; see that ADR's Status note). Historical Sprint 11–15 closeout (2026-06-08) cited `403/403` / PlayMode **7/7** only as program evidence, not live floors.
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
| TR-sensor-004 | sensor-detection-ew.md | Sensors | Side picture / datalink | **ADR-018** | **Covered** (mechanism shipped + 18 tests; harness-scoped, outside pinned goldens) |
| TR-engage-001 | engagement-fire-control.md | Engage | Unified resolver | ADR-001, ADR-004 | Covered |
| TR-engage-002 | engagement-fire-control.md | Engage | DLZ state + logging | — | Partial |
| TR-engage-003a | engagement-fire-control.md | Engage | Swarm slot order — P0 first-claimant per target, sorted by shooter | — | **Covered** (`SwarmSalvoDeconfliction`, golden-backed) |
| TR-engage-003b | engagement-fire-control.md | Engage | Swarm **sector coordinator** — fire distribution across 50+ shooters (P1) | — | **Deferred** (signed off 2026-07-24) |
| TR-logistics-001 | logistics-magazines.md | Logistics | Magazine ledger + empty abort | ADR-004 | Partial |
| TR-logistics-002 | logistics-magazines.md | Logistics | MagazineChange in order log | ADR-003 | Covered |
| TR-logistics-003 | logistics-magazines.md | Logistics | Deterministic fuel burn | **ADR-020** | **Covered** (constant-burn model shipped; tick↔wallclock contract recorded — live fix tracked as DRG-50) |
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
| TR-agentic-002 | agentic-infrastructure.md | Agentic | Hindsight hook (P1) | **ADR-019** | **Covered** (hook shipped; boundary now recorded) |
| TR-agentic-003 | agentic-infrastructure.md | Agentic | AAR read-only agents (P1) | **ADR-019** | **Covered** (`IReadOnlyOrderLog` — enforced by type, not convention) |
| TR-editor-001 | agentic-mission-editor.md | Editor | Canonical scenario / intent compiler | ADR-006 | Partial |
| TR-editor-002 | agentic-mission-editor.md | Editor | Deterministic Validation Engine | ADR-008 | Covered |
| TR-editor-003 | agentic-mission-editor.md | Editor | fire_order + world-state hash | ADR-001, ADR-004 | Partial |
| TR-editor-004 | agentic-mission-editor.md | Editor | editVersion conflict-reject | ADR-008 | Partial |
| TR-editor-005 | agentic-mission-editor.md | Editor | MCP export gate / headless sample | ADR-008 | Covered |


## Drone swarm platforms (doc 22 / H8 — landed 2026-08-09)

Requirements corpus only until Phase A code ships. Proposed TR IDs (not yet in `tr-registry.yaml`):

| Requirement ID | GDD | System | Requirement | ADR Coverage | Status |
|----------------|-----|--------|-------------|--------------|--------|
| TR-swarm-001 | — | Swarm platform | First-class platform + integrity fields (SWARM-01/02) | — | Gap |
| TR-swarm-002 | — | Swarm platform | Headless Move/Attack/Hold + aggregate SoT (SWARM-03/06/07) | ADR-010, ADR-003 | Gap |
| TR-swarm-003 | — | Swarm platform | DPS/ISR scale + hard-counter AA (SWARM-04/08) | — | Gap |
| TR-swarm-004 | — | Swarm platform | Map/panel integrity readout (SWARM-09) | ADR-007 | Gap |
| TR-swarm-005 | — | Swarm platform | Replay integrity deltas + LOD caps (SWARM-24/25) | ADR-003 | Gap |

**Note:** Distinct from `TR-engage-003a/b` (salvo slot deconfliction). See [22-Drone-Swarm-Platforms.md](../../Game-Requirements/requirements/22-Drone-Swarm-Platforms.md).

## Known Gaps

| TR-ID | Domain | Suggested action |
|-------|--------|------------------|
| ~~TR-sensor-004~~ | Sensors | **Closed 2026-07-24 by [ADR-018](adr-018-sensor-side-picture-datalink.md)** (Linear DRG-43). One open validation item: a test asserting the world hash is unchanged whether or not the datalink merger fires |
| ~~TR-logistics-003~~ | Logistics | **Closed 2026-07-24 by [ADR-020](adr-020-logistics-fuel-model.md)** (Linear DRG-44). Three OPEN validation items remain, incl. the live cadence defect **DRG-50** |
| TR-combat-dom-001..003 | Combat | ADR-009 Proposed — implement `IDomainValidator` + damage order |
| TR-editor-004 | Editor | editVersion persistence (guard only) |
| TR-engage-003b | Engage | **Deferred 2026-07-24** (Linear DRG-46). P0 half is Covered as TR-engage-003a. Reopen trigger: a 50+-shooter scenario entering the corpus — not a date. No ADR required while deferred |
| ~~TR-agentic-002..003~~ | Agentic | **Closed 2026-07-24 by [ADR-019](adr-019-agentic-aar-readonly-order-log.md)** (Linear DRG-45) |
| Systems #9, #15, #19 | Systems index | GDD + ADR backlog |

## Systems Without GDD

| System # | Name | ADR | Notes |
|----------|------|-----|-------|
| 4 | Platform Database | ADR-006 Accepted | Implement DATA-2..5 per migration plan |
| 9 | Mission Runtime | **ADR-021** | **GDD + ADR present** (2026-07-24) — [`mission-runtime.md`](../../design/gdd/mission-runtime.md). Headless/CI-scoped by decision; does **not** run in interactive play |
| 10 | Agent Delegation | ADR-001 | Boundary only |
| 15 | Near-Future Systems | — | Vertical slice |
| 19 | Speculative Systems | — | Full vision |
| 20 | Database Intelligence Pipeline | ADR-006 Accepted | Tied to #4 |

## Superseded Requirements

None identified this review.

## Documentation Conflicts (fix separately)

- [requirements-traceability.md](requirements-traceability.md) labels ADR-005 as engagement; file is DOTS/ECS.