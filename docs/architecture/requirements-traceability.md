# Requirements Traceability Matrix (RTM)

> **Last updated:** 2026-09-30 (additive reconciliation; corpus W4 complete 2026-07-08; S56 history retained)
> **Scope:** Historical requirements **01–21**, additive **22** and drafts **23–27**, plus proposed clauses below; frozen MVP grades and current evidence are distinct in the Game-Requirements tracker ([`implementation-tracker.md`](../../Game-Requirements/implementation-tracker.md))
> **Coverage:** Headless chain GDD → ADR → code → test (see [architecture-review-2026-06-02.md](architecture-review-2026-06-02.md))  
> **Gates (current):** Solution test floor and gates are governed by [`AGENTS.md`](../../AGENTS.md) §Hard Invariants (baseline floor ≥1638 / 0 failures; prior ≥1232/≥1599 superseded; ReplayGolden 6/6; PlayModeSmoke ≥20/20 [historical 18/18]; hash `17144800277401907079`) — supersedes historical 403/7 baselines in older closeout notes below

## How to read

| Status | Meaning |
|--------|---------|
| COVERED | ADR + implementation + automated test |
| PARTIAL | Implemented subset; design doc scope larger |
| DEFERRED | Explicit MVP deferral with ADR/story note |
| GAP | Not implemented |

## Additive current evidence — docs 22–27

Historical **COVERED/PARTIAL/DEFERRED/GAP** rows below describe their named implementation scope and dated closeouts. They do not approve later drafts or accept a newer revision. Document completeness, implementation delivery, automated proof, and owner acceptance are tracked separately.

| Doc / IDs | Document maturity | Implementation evidence / limit | Test evidence | Owner acceptance |
| --- | --- | --- | --- | --- |
| [22 / SWARM](../../Game-Requirements/requirements/22-Drone-Swarm-Platforms.md) | Draft; existing phase decisions retained | DRG-83 Phase A–C landing recorded; Phase N SWARM-27…30 deferred under DRG-47 | Existing document mappings; no new phase completion asserted | Existing deferral retained; no new decision |
| [23 / KCX-01…07](../../Game-Requirements/drafts/23-Kill-Chain-Explainability.md) | Draft; formal review/promotion pending | Provenance, sensor-to-shooter, custody and targetability projections exist; full UX/criterion scope exceeds code presence | Source mappings in [tracker evidence table](../../Game-Requirements/implementation-tracker.md#post-mvp-requirement-evidence-22-27); revision-bound run record required | Pending; [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208) remains owner gate |
| [24 / HOL-01…10](../../Game-Requirements/drafts/24-Human-On-The-Loop-Authority.md) | Draft; formal review/promotion pending | Approval queue and authority/projector subsets; lethal Phase N opt-in is deferred | Queue, authority and presentation candidate mappings in tracker; layer-specific acceptance remains | Pending; no inferred GUI or product signoff |
| [25 / C2N-01…04](../../Game-Requirements/drafts/25-C2-Nodes-Mission-Command.md) | Draft; formal review/promotion pending | Packages, network health and mission intent DTO/projection subsets | Candidate package/network/intent tests in tracker; run identity/result recorded separately | Pending |
| [26 / VER-01…07](../../Game-Requirements/drafts/26-Verification-CI-Gauntlet.md) | Draft; VER-07.3…6 proposed | Gauntlet/oracle/provenance subset exists; complete acceptance schema/validator pending | Existing oracle candidate tests; new revision-bound evidence tests planned | Pending; engineering green cannot assign an accepting owner |
| [27 / LIB-01…05](../../Game-Requirements/drafts/27-Scenario-Library-Campaigns.md) | Draft; LIB-05.1…3 proposed | Library/package/campaign subsets; advanced ZIP/media residual; H5 loader/fallback pilot pending | Existing package/library candidate tests; presentation failure isolation tests planned | Pending |

### Proposed criteria requiring new delivery evidence

| Clause | Requirement / boundary | Required proof | Current state |
| --- | --- | --- | --- |
| AME-6.11 | [Req 11](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md); domain validation shared by CLI/GUI | Support matrix; unsupported role finding on Export/Play; Save allowed; persisted round-trip; front-end parity | Proposed; [DRG-345](https://linear.app/drgamtd-workspace/issue/DRG-345) / [DRG-346](https://linear.app/drgamtd-workspace/issue/DRG-346); H4 gates/owner acceptance pending |
| RPL-29…31 | [Req 17](../../Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md); authoritative session restore | Same-build/scenario/catalog/policy uninterrupted vs resumed state/log equality; corrupt/mismatch load leaves active session unchanged | Proposed; [DRG-348](https://linear.app/drgamtd-workspace/issue/DRG-348) → [DRG-349](https://linear.app/drgamtd-workspace/issue/DRG-349); feasibility/scope/ADR pending |
| LIB-05.1…3 | [Draft 27](../../Game-Requirements/drafts/27-Scenario-Library-Campaigns.md); ADR-010 §2–3 / ADR-007 / ADR-001 | Presentation asset failure injection; documented fallback; unchanged authority/RNG/orders/replay; agreed measured budgets | Proposed; [DRG-347](https://linear.app/drgamtd-workspace/issue/DRG-347); H5 inventory/ADR/pilot pending |
| VER-07.3…6 | [Draft 26](../../Game-Requirements/drafts/26-Verification-CI-Gauntlet.md); explicit revision and owner acceptance | Complete schema; mismatched/stale evidence rejected; historical identity retained; owner action independent of test pass | Proposed; existing provenance is only a subset |

The [2026-09-30 reconciliation](../superpowers/specs/2026-09-30-requirements-planning-reconciliation.md) defines evidence vocabulary and entry order. No historical S56 grade or owner-controlled Phase N scope is changed.

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

> **Sprint 11–15 program closeout (2026-06-08) — historical only:** Requirements maturity track + Wave 5 implementation closed at that date with baseline `dotnet test ProjectAegis.sln` → **403/403 PASS** and PlayMode smoke **7/7**. Those floors are **not current**. Current solution test-floor and standing invariant specifications are governed by [`AGENTS.md`](../../AGENTS.md) §Hard Invariants (baseline floor ≥1638 / 0 failures; prior ≥1232 superseded; ReplayGolden 6/6 / PlayModeSmoke ≥20/20 / hash `17144800277401907079`). Tracker rows 14/16/19/20 were Partial+ with automated AC at closeout. Evidence: [post-mvp-requirements-program.md](../../production/milestones/post-mvp-requirements-program.md), [smoke-2026-06-08.md](../../production/qa/smoke-2026-06-08.md).

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
