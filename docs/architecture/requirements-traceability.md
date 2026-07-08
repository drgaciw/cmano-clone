# Requirements Traceability Matrix (RTM)

> **Last updated:** 2026-07-08 (corpus W4 complete)
> **Scope:** Requirements docs **01–21** on disk (hub + Template A 01–12 + MVP slice 13–20 + **21 Platform Editor**); implementation grades in Game-Requirements tracker  
> **Coverage:** Headless chain GDD → ADR → code → test (see [architecture-review-2026-06-02.md](architecture-review-2026-06-02.md))  
> **Gates (current):** solution tests ≥1232; ReplayGolden 6/6; PlayModeSmoke 18/18; hash `17144800277401907079` — supersedes historical 403/7 baselines in older closeout notes below

## How to read

| Status | Meaning |
|--------|---------|
| COVERED | ADR + implementation + automated test |
| PARTIAL | Implemented subset; design doc scope larger |
| DEFERRED | Explicit MVP deferral with ADR/story note |
| GAP | Not implemented |

## C1–C5 design-review blockers

| ID | Requirement doc | ADR | Implementation | Test evidence | Status |
|----|-----------------|-----|----------------|---------------|--------|
| C1 | 17 Order log / replay | ADR-003 | `DecisionLog`, `ReplayCheckpointStore` | `ReplayGolden*`, `replay-2026-06-02.md` | COVERED |
| C2 | 18 Combat outcomes | ADR-001, ADR-004 | `EngagementOutcomeRecord`, `MessageLogProjection` | `MessageLogProjectionTests`, `MessageLogBridgeTests` | COVERED |
| C2-UI | 18 Message HUD | — | `MessageLogPanelBinder`, `MessageLogPanelHost`, `OobTreePanelHost` | `MessageLogPanelBinderTests`, `OobTreeProjectionTests` | COVERED |
| C3 | 19 ROE / policy | ADR-002 | `PassthroughRoeFilter`, scenario ROE JSON | `PolicyDenialOrderLogTests` | COVERED |
| C4 | 20 EMCON | ADR-002 | `EmconPolicyEvaluator`, `ScenarioEmconResolver` | `EmconPolicyEvaluatorTests`, emcon scenario JSON | COVERED |
| C5 | 13 Human-in-the-loop | ADR-001 | `SimulationModeProfile`, `PlayerOrderRecord` | `PlayModeSmokeHarnessTests` | DEFERRED |

## Sensor / C2 (TR-sensor-001)

| TR-ID | GDD | ADR / note | Story | Test | Status |
|-------|-----|------------|-------|------|--------|
| TR-sensor-001a | sensor-detection-ew | Pd loop | sensor-headless-slice | `BalticReplayHarnessPdDetectionTests` | COVERED |
| TR-sensor-001b | sensor-detection-ew | Classify FSM | sensor-classify-slice | `PdContactClassifyTests`, `ReplayGoldenBalticClassifyTests` | COVERED |
| TR-sensor-001c | sensor-detection-ew | C2 projection | sensor-c2-ui-slice | `SensorC2PanelBinderTests`, `SensorC2BridgeTests` | COVERED |

> **Sprint 2 closeout (2026-06-08):** TR-sensor-001a/b/c verified via classify FSM (`PdContactClassifyTests`, `ReplayGoldenBalticClassifyTests`), C2 bridge/panel (`SensorC2BridgeTests`, `SensorC2PanelBinderTests`, `SensorC2PanelHost`), and `baltic-patrol-classify` scenario. Parent **TR-sensor-001** marked **COVERED** in [tr-registry.yaml](tr-registry.yaml). **TR-sensor-004** (side picture / datalink) remains deferred.

## Combat / engage spine

> **Note:** ADR-005 is **DOTS/ECS world state**, not engagement. Engage/outcomes trace to ADR-001 (sim boundary) + ADR-004 (tick pipeline) + ADR-003 (order log).

| TR-ID | GDD | ADR | Test | Status |
|-------|-----|-----|------|--------|
| TR-combat-001 | combat-outcomes | ADR-001, ADR-004 | `EngagementOrderLogContractTests` | COVERED |
| TR-combat-002 | combat-outcomes | ADR-001, ADR-003 | `ReplayGoldenBalticEngageTests` | COVERED |
| TR-mag-001 | combat-outcomes | ADR-003 | `ReplayGoldenBalticMagazineTests` | COVERED |

## Wave 5 overlap spine (Sprints 11–15)

Shared symbols across **policy-engage-unification-slice**, **wave5-engage-cyber-logistics-slice**, and **platform-db-basepd-slice** epics.

| Area | Symbols | Epics | Test evidence | Status |
|------|---------|-------|---------------|--------|
| Engage | `IEngageWorldQuery`, `IEngagementResolver`, `MvpEngagementResolver` | policy-engage-unification-slice, wave5-engage-cyber-logistics-slice | `MvpEngagementResolverTests`, `MvpEngagementSpoofTrackTests`, `MvpEngagementAirNotReadyTests`, `ReplayGoldenBalticEngageTests`, `BalticReplayHarnessPolicyEngageTests` | COVERED |
| Combat | `CombatDomainValidator`, `CombatDomain` enum | policy-engage-unification-slice | `CombatDomainValidatorTests` (in `MvpEngagementResolver` gate chain) | COVERED |
| Catalog | `ICatalogReader`, `CatalogEntityMap`, `CatalogWriteGate`, `RunCatalog*` CLI verbs in `MissionEditor.Cli/Program.cs` | platform-db-basepd-slice | `CatalogEntityMapTests`, `CatalogWriteGateTests`, `CmoMarkdownImportSmokeTests` | PARTIAL |
| Delegation bridge | `DelegationBridge`, `EngageAttackOptions`, `EngageAttackOrderResolver` | wave5-engage-cyber-logistics-slice | `DelegationBridgeAttackOptionTests`, `EngageAttackOrderResolverTests`, `AttackMenuPanelBinderTests` | COVERED |
| Wave5 specifics | `SpoofTrackTimelineSimulator`, `UnitReadinessMap` | wave5-engage-cyber-logistics-slice | `BalticReplayHarnessSpoofTests`, `BalticReplayHarnessReadinessPolicyTests` | COVERED |

## Data layer (TR-registry)

| TR-ID | GDD | ADR | Test | Status |
|-------|-----|-----|------|--------|
| TR-logistics-004 | logistics-magazines | ADR-006 | Editor validation (planned) | PARTIAL |
| TR-editor-001 | agentic-mission-editor | ADR-006 | MCP/editor (planned) | PARTIAL |
| TR-sensor-002 (catalog) | sensor-detection-ew | ADR-006 | `platform-db-basepd-slice` / catalog reader tests | PARTIAL |

## Platform editor (req 21 / FR-19)

Hub **[FR-19](../../Game-Requirements/requirements/01-Project-Overview.md)** — platform/catalog editor Excel write-gate round-trip. Spec: [21-Platform-Editor.md](../../Game-Requirements/requirements/21-Platform-Editor.md); decision: [ADR-011](adr-011-platform-editor-excel-roundtrip.md).

| Area | Detail |
|------|--------|
| ADR | [ADR-011](adr-011-platform-editor-excel-roundtrip.md) (Accepted) |
| Symbols | `IPlatformWorkbookIo`, `PlatformWorkbookExporter` / `PlatformWorkbookImporter` / `PlatformWorkbookDiff`, `CatalogWriteGate` platform `Propose*` consumer (**extend-only**) |
| Tests | `PlatformWorkbook*` / ClosedXml I/O / `CatalogWriteGatePlatformApprove*`; Wave 3 honesty docs optional |
| Status | **PARTIAL** |

## Requirements maturity (docs 01–12 + 21 note) — Sprint 11–15 + corpus W0

| Doc | Title | Maturity | Locked spec / notes | GDD |
|-----|-------|----------|---------------------|-----|
| 01 | Project Overview | **FULL** | Template A; charter name open; hub re-baseline 2026-07-08 (FR-19/index/invariants — corpus maturity W0) | STUB — `/map-systems` backlog |
| 02 | Core Gameplay Loop | **FULL** | [core-gameplay-loop spec](../superpowers/specs/2026-05-30-core-gameplay-loop-decisions-design.md) | STUB |
| 03 | Simulation Modes | **FULL** | [simulation-modes spec](../superpowers/specs/2026-05-30-simulation-modes-decisions-design.md) | STUB |
| 04 | Agent Delegation | **FULL** | [agent-delegation spec](../superpowers/specs/2026-05-30-agent-delegation-decisions-design.md) | STUB |
| 05 | Dynamic Systems Agent | **FULL** | Sprint 13 resolved Q1–Q3 | STUB |
| 06 | Database Intelligence | **FULL** | [database-intelligence P0](../superpowers/specs/2026-05-30-database-intelligence-p0-design.md) | STUB |
| 07 | Agentic Infrastructure | **FULL** | INF acceptance; tools/MCP mapping | STUB |
| 08 | Agentic Architecture | **FULL** | ADR-001–006; assembly mapping | STUB |
| 09 | Near-Future Tech | **FULL** (pre-existing) | — | PARTIAL |
| 10 | Speculative Systems | **FULL** (pre-existing) | — | PARTIAL |
| 11 | Agentic Mission Editor | **FULL** (pre-existing) | — | PARTIAL |
| 12 | Terms Glossary | **FULL** | Wave 5 + slice 13–20 index | N/A |
| 21 | Platform Editor | **PARTIAL** (doc Draft; impl Partial+) | [ADR-011](adr-011-platform-editor-excel-roundtrip.md); hub FR-19 as of 2026-07-08; Wave 3 doc honesty 2026-07-08 | — |

**Maturity** = requirement document completeness (Template A/B). **GDD** = separate game design doc under `design/gdd/` per Agentic-Development-Plan follow-on.

> **Sprint 11–15 program closeout (2026-06-08) — historical only:** Requirements maturity track + Wave 5 implementation closed at that date with baseline `dotnet test ProjectAegis.sln` → **403/403 PASS** and PlayMode smoke **7/7**. Those floors are **not current**. Use the **Gates (current)** line in this file’s header (≥1232 / ReplayGolden 6/6 / PlayModeSmoke 18/18 / hash `17144800277401907079`). Tracker rows 14/16/19/20 were Partial+ with automated AC at closeout. Evidence: [post-mvp-requirements-program.md](../../production/milestones/post-mvp-requirements-program.md), [smoke-2026-06-08.md](../../production/qa/smoke-2026-06-08.md).

## Uncovered (post-MVP)

| Area | Gap | Suggested action |
|------|-----|------------------|
| Doc 20 full C2 | Globe map, mission editor, doctrine UI | Sprint 4+ `/ux-design` + `/team-ui` (OOB/missions projections done Sprint 3) |
| GDD for reqs 01–12 | No system GDDs yet | `/map-systems` backlog (requirements locked) |
| C5 player override | Pause / direct order UX | Story under simulation-control epic |

## Coverage summary

| Status | Count |
|--------|-------|
| COVERED | 15 |
| PARTIAL | 3 |
| DEFERRED | 1 |
| GAP (post-MVP) | 3+ |

*Counts include C1–C5 block, sensor/combat spine, and Wave 5 overlap spine (Sprints 11–15).*