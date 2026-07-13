# Requirements Traceability Matrix (RTM)

> **Last updated:** 2026-06-08
> **Scope:** Requirements docs **01–12** (maturity program) + MVP slice **13–20** (GDD → ADR → code → test)  
> **Coverage:** Headless chain GDD → ADR → code → test (see [architecture-review-2026-06-02.md](architecture-review-2026-06-02.md))

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

## C2 UI rev 2 (TR-c2-005..008)

GDD: [command-and-control-ui](../../design/gdd/command-and-control-ui.md) · Req: [20-Command-And-Control-UI](../../Game-Requirements/requirements/20-Command-And-Control-UI.md) (rev 2) · ADR-010 (presentation binds projections only; UI never mutates sim). Headless-first: every row is asserted in `ProjectAegis.Delegation(.UnityAdapter).Tests` before Editor work.

| TR-ID | Requirement | Req AC | Test | Status |
|-------|-------------|--------|------|--------|
| TR-c2-005 | Multi-select (drag-box / shift / ctrl) + group-order fan-out (one intent per eligible unit) | AC-7 | `SelectionBoxResolverTests`, `C2PresentationControllerMultiSelectTests`, `GroupOrderPlanTests`, `GroupOrderFanOutTests`, `C2Rev2IntegrationProxyTests` | COVERED (headless) |
| TR-c2-006 | Order lifecycle states in panel + log; weapons-release confirmation gate; cancel | AC-8 | `OrderLifecycleProjectionTests`, `WeaponsReleaseConfirmationGateTests`, `MessageLogPanelBinderTests` | PARTIAL — display + gate COVERED; **cancel emission deferred to Phase 2b** (needs bridge cancel affordance) |
| TR-c2-007 | Severity tiers, toasts (max 3 + `+N`), auto-pause command | AC-9 | `AlertProjectionTests`, `ToastStackModelTests`, `AutoPausePolicyTests`, `AlertingIntegrationTests` | PARTIAL — severity/toast/command COVERED; **auto-pause actuation deferred to Phase 2b** (needs pause-reason stack) |
| TR-c2-008 | Per-category message-log filters | AC-8/§Alerting | `MessageLogFilterModelTests` | COVERED (headless) |

> **rev 2 integration (2026-07-08):** Phases 0–2a landed on `c2-req20-integration` (full sln **1551/0**, C2 proxy incl. 3 new rev-2 seams, ReplayGolden green, **DelegationBridge zero-diff**, Baltic hash `17144800277401907079` unchanged). TR-c2-006/007 "actuation" halves (`PlayerOrderCancelled` emit, sim auto-pause) are presentation-complete but gated on the Phase 2b scoped extension (order-cancel affordance + pause-reason stack) per the 2026-07-08 decision. Weapons-release positive-control uses a `WeaponsTight` proxy pending a real policy flag (systems-designer follow-up). Unity host/UXML/USS verified via models only — Editor PlayMode pass pending.

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

## Requirements maturity (docs 01–12) — Sprint 11–15

| Doc | Title | Maturity | Locked spec / notes | GDD |
|-----|-------|----------|---------------------|-----|
| 01 | Project Overview | **FULL** | Template A; charter name open | STUB — `/map-systems` backlog |
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

**Maturity** = requirement document completeness (Template A/B). **GDD** = separate game design doc under `design/gdd/` per Agentic-Development-Plan follow-on.

> **Sprint 11–15 program closeout (2026-06-08):** Requirements maturity track + Wave 5 implementation **COMPLETE**. Baseline: `dotnet test ProjectAegis.sln` → **403/403 PASS**; PlayMode smoke **7/7**. Tracker rows 14/16/19/20 at **Partial+** with automated AC. Evidence: [post-mvp-requirements-program.md](../../production/milestones/post-mvp-requirements-program.md), [smoke-2026-06-08.md](../../production/qa/smoke-2026-06-08.md).

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