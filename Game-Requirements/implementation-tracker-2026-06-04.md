# Game Requirements — Implementation Tracker

**Base:** `main` @ `2e4fb07` (`2e4fb07` on `main`)
**Last Updated:** 2026-06-18 (S27-13 closeout — Req 06 corpus pipeline + Req 18 ADR-009 + Req 21 Phase C viewer)
**Index:** [Game-Requirements-Index.md](Game-Requirements-Index.md) | [00-Master-Index.md](../00-Master-Index.md)

## Verdict

| Scope | Status |
|-------|--------|
| Requirements **documentation** (01–20) | **Complete** — drafted, research-integrated; **01–12 FULL** (Sprints 11–15 program closeout 2026-06-08) |
| **MVP / Phase 1 gameplay** implementation | **In progress** — headless Baltic vertical slice; no doc is 100% MVP-done |
| **Index housekeeping** (this pass) | **Complete** — canonical index, CMAO DB doc, tracker |

Completing the full requirements corpus as *shipped game features* is multi-year work (req 07 phases 1–5). This tracker is the execution backlog for stacked `stack/*` branches.

## Verification baseline

**Test baseline (Sprint 19, 2026-06-08):** `dotnet test ProjectAegis.sln` → **403/403 PASS**; PlayMode smoke **7/7**; headless QA proxy **PASS**. Evidence: [pi-006-headless-proxy-2026-06-04.md](../production/qa/pi-006-headless-proxy-2026-06-04.md), [c2-automated-proxy-2026-06-02.md](../production/qa/c2-automated-proxy-2026-06-02.md), [wave5 EPIC](../production/epics/wave5-engage-cyber-logistics-slice/EPIC.md).

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal   # 403 tests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "BalticReplayHarnessSpoofTests|BalticReplayHarnessReadinessPolicyTests|DelegationBridgeAttackOption"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~EngageAttack|AttackMenu|ReplayGoldenBalticEngage"
pwsh tools/unity/Invoke-ManualQaHeadlessGate.ps1
```

## MVP status by requirement

| Req | Title | MVP status | Evidence (paths) | Next stack task |
|-----|-------|------------|------------------|-----------------|
| 01 | Project Overview | **Partial** | `design/gdd/game-concept.md`, `BalticReplayHarness.cs` | Multi-thousand-entity perf gate; agent personality on engage |
| 02 | Core Gameplay Loop | **Partial** | `SimulationSession.cs`, `data/scenarios/*.policy.json` | Phase 1–2 UX; explicit **Begin Execution** in Unity |
| 03 | Simulation Modes | **Partial** | `SimulationSessionPhaseTests.cs`, headless smoke | AvA batch benchmark; mode UI on C2 top bar |
| 04 | Agent Delegation | **Partial** | `DelegationOrchestrator.cs`, `TrustSignalEmitterTests.cs` | C2 delegation badges; trust emit-only in order log |
| 05 | Dynamic Speculative Systems Agent | **Partial+** (S19 slice + S20 full per 2026-06-09 completion plan Tasks 1-5; historical overclaims corrected first) | `OsintDigestRunner`, `InMemoryOsintConnector` + `FileOsintConnector` + `RssOsintConnector` (full IOsintConnector impls + stable), `data/osint_facts.json` (exact fixture), `Osint*Tests` (23 pass), `OsintStagingPanelHost` (full live proxy/gate + bind + approve + Refresh), `CesiumGlobeBridge` (real CesiumForUnity + GetCurrentPositions from MapPanelBinder), `CesiumGlobeHost`, cesium-phase-b-spike-checklist.md (fully marked), `production/qa/cesium-s20-local-editor-evidence.md`, `production/agentic/sprint-20-closeout-2026-06-07.md` + `sprint-20-accurate-closeout-2026-06-09.md`, `production/qa/sprint-20-2026-06-09-reality-and-recommendations.md`, `docs/superpowers/plans/2026-06-09-sprint-20-completion-...md`, Program.cs osint_search + OsintStagingReviewCommand,  `production/sprints/sprint-20-...md` | S20 Musts actually delivered (real connectors w/ fixture+interface, full panel w/ live calls/state, real Cesium runtime+pre-exist pin+scene+checklist+evidence). S20 QA gap addressed via plan + /qa-plan equiv; local Editor signoffs for visuals. Tracker updated Task 5 accurate. Next: S21 MCP + polish + Data P1 (per req 05/21). |
| 06 | Database Intelligence | **Partial** (P0 + CMO Phase 2 + corpus pipeline) | P0 on `main`: `CatalogWriteGate`, `ValidationPipeline`, `ScenarioPackage`, `CmoMarkdownImporter`, migrations `001`–`005`; **S26-02..04** weapon+platform markdown import; **S27-02** nightly CMO corpus job (`tools/cmo-nightly-import.sh`, sensor+weapon v1 off-CI); **S27-03..04** mount→loadout→magazine import + `CmoMarkdownImportGoldenTests`; **S27-09** browse enrichment (`MountCount`/`SensorCount`); **S27-14** `ship-slice-100.md` curated platform slice + chunk 501→2; evidence [sprint-27-nightly-cmo-import-2026-06-18.md](../production/qa/sprint-27-nightly-cmo-import-2026-06-18.md), [smoke-sprint-27-closeout-2026-06-18.md](../production/qa/smoke-sprint-27-closeout-2026-06-18.md) | Nightly platform corpus v2; full 7208-record sensor in CI; balance drift; TL branching |
| 07 | Agentic Infrastructure | **Partial** | `BalticReplayHarness`, `MissionEditor.Cli`, Hindsight | Scenario gen + experiment workers |
| 08 | Agentic Architecture | **Partial** | `ProjectAegis.Sim`, `Delegation`, `architecture.md` | DOTS sensor hot path; sim API export |
| 09 | Near-Future Technologies | **Partial** | `NearFutureArchetypeRuntime`, `hypersonic-boost-glide`, `HypersonicEngageGate`, harness `NF_SPAWN` | Full DOTS spawn; MASS tier runtime |
| 10 | Speculative Systems | **Partial** | `ScenarioSpeculativeSettings`, `SpeculativeEngageGate`, `speculative_platforms.json`, `baltic-patrol-black-project` fixture | Orbital DEW runtime; escalation ladder events |
| 11 | Agentic Mission Editor | **Partial** | `scenario_cyber_status`, `scenario_near_future_spawn`, plan suggest cca/cyber keywords | Unity edit mode UX; full NL planner |
| 12 | Terms Glossary | **Partial** | `abort_reason_manifest.json`, Sensor/Cyber families in `AbortReasonCatalog`, alignment tests | UI tooltips; mount-offline abort enum |
| 13 | Doctrine ROE EMCON WRA | **Partial** | `PolicyEvaluator` WRA salvo, `ResolvedUnitPolicy` mission ROE, `baltic-patrol-mission-roe` / `wra-cap` fixtures | Unity doctrine inheritance panel (ADR-010) |
| 14 | Engagement & Fire Control | **Partial** | `EngageAttackOptions`, `DelegationBridgeAttackOptionTests`, `EngageAttackOrderResolverTests`, `AttackMenuPanelBinderTests`, `tests/regression/replay-golden-baltic-engage-2026-06-02.txt` | Swarm coordinator sectors; DLZ Phase 2 |
| 15 | Sensor Detection & EW | **Partial** (MVP slice **COVERED**) | `ReplayGoldenSuite` + `baltic-patrol-stale` golden, contact FSM harness; classify FSM (`PdContactClassifyTests`, `ReplayGoldenBalticClassifyTests`); C2 (`SensorC2BridgeTests`, `SensorC2PanelBinderTests`, `SensorC2PanelHost`); `data/scenarios/baltic-patrol-classify.policy.json` | ECCM Phase 2; datalink delay (TR-sensor-004) |
| 16 | Logistics & Magazines | **Partial** | `UnitReadinessMap`, `BalticReplayHarnessReadinessPolicyTests`, `tests/regression/replay-golden-baltic-readiness-2026-06-04.txt`, `AIR_NOT_READY` | UNREP; live magazine counts from catalog |
| 17 | Replay AAR & Order Log | **Partial** | `ReplayGoldenSuiteTests`, CI step in `dotnet-reusable.yml`, `tests/regression/README.md` | Scrub UI; AAR agent |
| 18 | Combat Domains | **Partial** | ADR-009 bounded validators: `IDomainValidator`, `DomainValidatorRegistry`, `DeterministicDamageApplyBatch`, `AirAspectDomainValidator`; `combat-domains-smoke.policy.json` (test-only `combatDomainsEnabled=true`); `CombatDomainsSmokePolicyTests` pinned world hash `17144800277401907079`; Baltic production `combatDomainsEnabled=false` | Mine/land runtime; facility damage; flip flag on Baltic catalog when golden passes |
| 19 | Cyber & Comms | **Partial** | `SpoofTrackTimelineSimulator`, `BalticReplayHarnessSpoofTests`, `tests/regression/replay-golden-baltic-spoof-2026-06-04.txt`, `CYBER_SPOOF_TRACK` | JADC2 node damage; ECCM Phase 2 |
| 20 | Command & Control UI | **Partial** | `RightUnitPanelHost`, `DelegationBridgeAttackOptionTests`, `AttackMenuPanelBinderTests`, `UnitDetailPanelBinderAttackMenuTests`, [c2-automated-proxy-2026-06-02.md](../production/qa/c2-automated-proxy-2026-06-02.md), [pi-006-headless-proxy-2026-06-04.md](../production/qa/pi-006-headless-proxy-2026-06-04.md) | Globe map; Unity manual C2 sign-off (S19-01 pending) |
| 21 | Platform Editor (Excel round-trip) | **Partial — Phase C viewer landed (S27-08..11, S27-15)** | Req [21-Platform-Editor.md](requirements/21-Platform-Editor.md), [ADR-011](../docs/architecture/adr-011-platform-editor-excel-roundtrip.md); Phase A/B complete (S24–S25); **S26-10** browse spike PROCEED; **S27-08** `PlatformCatalogPanel` UXML/USS + filter projection (read-only); **S27-09** browse row enrichment + CLI `schemaVersion` 2; **S27-11** PlayMode smoke harness; **S27-15** detail pane (`LatDeg`, `LonDeg`, `CombatRadiusNm`, `MaxHp`, `MaxSpeedKnots`); tests `PlatformCatalogViewerTests`, `PlatformCatalogDetailProjectionTests`; evidence [sprint-27-presentation-evidence-2026-06-18.md](../production/qa/sprint-27-presentation-evidence-2026-06-18.md), [smoke-sprint-27-closeout-2026-06-18.md](../production/qa/smoke-sprint-27-closeout-2026-06-18.md) | In-engine Excel write path; damage sim consumer beyond stub; CMO full corpus nightly v2 |

## Research open gaps (P1)

| Gap | Owner doc | Tracker note |
|-----|-----------|--------------|
| `HYPERSONIC_ALERT` UI state | 20 | Deferred — add when hypersonic track in sim |
| `KESSLER_RISK_METER` | 18/19 | Deferred — speculative TL-4+ |
| JADC2 damageable node | 19 | Deferred — entity schema in Data epic |
| Monte Carlo experiment schema | 17 | Phase 5 / req 07 |

## Workflow (agents)

1. Pick row → read requirement MVP + linked GDD  
2. `gitnexus_impact` on target symbols  
3. `team-simulation` / `team-data` / `team-unity` by layer  
4. `replay-verify` for sim/delegation changes  
5. Update this row when MVP AC tests exist  

## Related

- [research-traceability.md](research-traceability.md)
- [reviews/requirements-13-20-design-review-2026-05-29.md](reviews/requirements-13-20-design-review-2026-05-29.md) — CONCERNS
- [production/agentic/pi-plan-completion-2026-06-04.md](../production/agentic/pi-plan-completion-2026-06-04.md)