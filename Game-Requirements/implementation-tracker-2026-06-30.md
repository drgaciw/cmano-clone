# Game Requirements — Implementation Tracker

**Base:** `stack/sprint73-75/closeout` @ `065c3b4` (`065c3b4768df5d79172843a4e66481fd323f5155` — S73–S80 payload closeout)  
**Last Updated:** 2026-06-30 (post–S80 Baltic v3 content gate; S56 21/21 MVP unchanged; additive post-MVP evidence S57–S80; verification-before + GitNexus)  
**Supersedes:** [implementation-tracker-2026-06-04.md](implementation-tracker-2026-06-04.md)  
**Index:** [Game-Requirements-Index.md](Game-Requirements-Index.md) | [00-Master-Index.md](../00-Master-Index.md)

## Verdict

| Scope | Status |
|-------|--------|
| Requirements **documentation** (01–21) | **Complete** — drafted, research-integrated; **01–12 FULL** (Sprints 11–15 program closeout 2026-06-08) |
| **MVP / Phase 1 gameplay** implementation | **COMPLETE 21/21** (S56 internal gate PASS 2026-06-21) — all rows MVP-done or documented Partial+ with Baltic AC tests (replay 6/6, proxy 18/18, hash `17144800277401907079`); program exit per [post-release-scope-boundary-2026-06-21.md](../production/post-release-scope-boundary-2026-06-21.md) |
| **Post-MVP content programs** (S57–S80) | **COMPLETE** — Baltic v2 (S57–S64), release train (S65–S68), E7 launch prep (S69–S72), Baltic v3 (S73–S80); human acks: S72 commercial launch prep (2026-06-25), S80 **"Baltic v3 content-complete"** (2026-06-26); stage remains **Release**; v3 corpus promotion **no** (explicit decision only) |
| **Forward engineering** (post-S80) | **In progress (WIP)** — scenario editor authoring stack (req 06/07/11); see [Forward stack](#forward-stack-post-s80) |

Completing the full requirements corpus as *shipped game features* is multi-year work (req 07 phases 1–5). This tracker is the execution backlog for stacked `stack/*` branches. **Post-MVP content-only tracks add evidence additively; they do not re-litigate the 21/21 MVP row grades closed @ S56** (per [baltic-v3-scope-boundary-2026-06-25.md](../production/baltic-v3-scope-boundary-2026-06-25.md)).

## Verification baseline

**Gate baseline (S80, 2026-06-26):** `dotnet test ProjectAegis.sln` → **≥1232/0f**; ReplayGolden **6/6**; PlayMode smoke **18/18**; Baltic v2 hash **`17144800277401907079`** preserved; ZERO `DelegationBridge` hotpath edits. Evidence: [smoke-sprint-73-80-closeout-2026-06-26.md](../production/qa/smoke-sprint-73-80-closeout-2026-06-26.md), `production/stage.txt` (S80 gate rows).

**Measured @ 2026-06-30 (closeout branch, RUN):**

| Assembly | Pass | Fail | Total |
|----------|------|------|-------|
| ProjectAegis.Sim.Tests | 281 | 0 | 281 |
| ProjectAegis.Delegation.Tests | 249 | 0 | 249 |
| ProjectAegis.Data.Excel.Tests | 5 | 0 | 5 |
| ProjectAegis.MissionEditor.Cli.Tests | 52 | 0 | 52 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 257 | **2** | 259 |
| ProjectAegis.Data.Tests | 413 | 0 | 413 |
| **Solution total** | **1257** | **2** | **1259** |

**UA failures (WIP regression on dirty branch — fix before merge claims):** `Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log`, `Restricted_engagement_scenario_fingerprint_is_deterministic`.

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal                    # gate floor ≥1232/0f
dotnet test ProjectAegis.sln --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "BalticReplayHarnessSpoofTests|BalticReplayHarnessReadinessPolicyTests|DelegationBridgeAttackOption"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~EngageAttack|AttackMenu|ReplayGoldenBalticEngage"
pwsh tools/unity/Invoke-ManualQaHeadlessGate.ps1
```

## Post-MVP program (S57–S80)

Additive content and ops only. Does **not** change S56 MVP row grades.

| Program | Sprints | Status | Primary reqs touched | Evidence |
|---------|---------|--------|----------------------|----------|
| Baltic v2 content expansion | S57–S64 | **COMPLETE** | 02, 15, 17, 18, 20 | 10 `baltic-v2-*` policies, 9 v2 replay goldens; [s57-s64-program-closeout-2026-06-22.md](../production/qa/s57-s64-program-closeout-2026-06-22.md) |
| Release train | S65–S68 | **COMPLETE** | 06, 17, 21 | [s68-release-train-gate-2026-06-25.md](../production/gate-checks/s68-release-train-gate-2026-06-25.md) |
| E7 commercial launch prep | S69–S72 | **COMPLETE** | 01, 20, 21 | [s72-commercial-launch-prep-gate-2026-06-25.md](../production/gate-checks/s72-commercial-launch-prep-gate-2026-06-25.md); human ack 2026-06-25 |
| Baltic v3 content expansion (E9) | S73–S80 | **COMPLETE** | 02, 03, 06, 11, 13, 15, 17, 18, 20 | 6 policies, 6 v3 goldens, `baltic-v3-scenario-manifest.yaml`; [smoke-sprint-73-80-closeout-2026-06-26.md](../production/qa/smoke-sprint-73-80-closeout-2026-06-26.md) |

**S73–S80 sprint deliverables:**

| Sprint | Focus | Key artifacts |
|--------|-------|---------------|
| S73 | Foundations | [baltic-v3-scope-boundary-2026-06-25.md](../production/baltic-v3-scope-boundary-2026-06-25.md), `baltic-v3-scenario-manifest.yaml`, GitNexus re-index |
| S74 | Scenario wave | 6 `baltic-v3-*` policies + 6 isolated v3 replay goldens (bands A/B/C) |
| S75 | Theater OOB | v3 OOB (`u1`, `hostile-1`, `ucav-blue`, `ucav-red`); catalog sensor slices |
| S76 | Mission / narrative | mission-event policies, contact windows, briefing stubs |
| S77 | Catalog / platform | extend-only catalog slices for v3 OOB |
| S78 | C2 scenario UX | v3 picker, difficulty bands, tooltips (additive UI) |
| S79 | Playtest loop | automated batch (18 rows) + human per-band sessions |
| S80 | Content gate | [smoke-sprint-73-80-closeout-2026-06-26.md](../production/qa/smoke-sprint-73-80-closeout-2026-06-26.md); human ack **"Baltic v3 content-complete"** |

**Standing invariants (all post-S56 programs):** hash `17144800277401907079` unchanged; DelegationBridge zero-touch; CatalogWriteGate extend-only; test baseline monotonic ≥1232.

### Baltic v3 content inventory (S73–S80)

**Policies (`data/scenarios/`):**

- `baltic-v3-patrol.policy.json`
- `baltic-v3-patrol-comms.policy.json`
- `baltic-v3-classify.policy.json`
- `baltic-v3-comms-challenged.policy.json`
- `baltic-v3-mission-band-b.policy.json`
- `baltic-v3-mission-roe-band-c.policy.json`

**Isolated replay goldens (`tests/regression/`):**

- `replay-golden-baltic-v3-patrol-2026-06-25.txt`
- `replay-golden-baltic-v3-patrol-comms-2026-06-25.txt`
- `replay-golden-baltic-v3-classify-2026-06-25.txt`
- `replay-golden-baltic-v3-comms-challenged-2026-06-25.txt`
- `replay-golden-baltic-v3-mission-band-b-2026-06-25.txt`
- `replay-golden-baltic-v3-mission-roe-band-c-2026-06-25.txt`

**Manifest:** `production/playtests/baltic-v3-scenario-manifest.yaml`

**v3 behavior (additive):** contact-triggered dual-side ASuW/AAA via `mission.triggers` / `MissionContactTriggerRuntime`; ROE escalation to Weapons Free on recon contact detection.

## MVP status by requirement

**S56 gate: 21/21 rows MVP-done / documented Partial+ (Baltic AC sufficient) + program exit.** Evidence column carries forward from [implementation-tracker-2026-06-04.md](implementation-tracker-2026-06-04.md) @ S56; **Post-S56 note** is additive only.

| Req | Title | MVP status (S56) | Evidence (paths @ S56) | Post-S56 note | Next stack task |
|-----|-------|------------------|------------------------|---------------|-----------------|
| 01 | Project Overview | **MVP-done (S56)** | `design/gdd/game-concept.md`, `BalticReplayHarness.cs` + S52 multi-k + S56 gate; 21/21 program exit | S72 commercial launch prep complete | (complete @ S56) |
| 02 | Core Gameplay Loop | **Partial** | `SimulationSession.cs`, `data/scenarios/*.policy.json` | S74/S76 v3 policies + mission bands; v3 contact-triggered ROE escalation | Begin Execution UX polish; v3 playtest feedback |
| 03 | Simulation Modes | **Partial+** | `SimulationSessionPhaseTests.cs` + S43-03 Engage/CombatOutcomeResolver updates | S76 mission-event / contact-window policies on v3 fixtures | AvA batch benchmark; mode UI on C2 top bar |
| 04 | Agent Delegation | **Partial+** | `DelegationOrchestrator.cs`, `TrustSignalEmitterTests.cs`, `GetDelegationTrustDataByPlatform` | — | C2 delegation badges; trust emit-only in order log |
| 05 | Dynamic Speculative Systems Agent | **Partial+** | `OsintDigestRunner`, `FileOsintConnector`, `RssOsintConnector`, `OsintStagingPanelHost`, `CesiumGlobeBridge`, sprint-20 closeouts (23 OSINT tests) | — | MCP polish; Data P1 (per req 05/21) |
| 06 | Database Intelligence | **Partial** | CatalogWriteGate through S34 link catalog (S27–S34 chain — see [2026-06-04 tracker](implementation-tracker-2026-06-04.md) row 06 for full sprint list) | S77 v3 catalog slices (extend-only); **WIP:** `ScenarioDocumentEditor`, `ScenarioDbMigrationPreview`, `ScenarioManifest`, `ScenarioValidationEngine` live validation | DB migration preview UX; scenario package publish |
| 07 | Agentic Infrastructure | **Partial** | `BalticReplayHarness`, `MissionEditor.Cli`, Hindsight | S73–S80 playtest + gate automation; **WIP:** `AiAuthoringServices`, `ScenarioPublishCommand`, `ScenarioAiScaffoldCommand` | Experiment workers; scenario smoke-test agent |
| 08 | Agentic Architecture | **Partial** | `ProjectAegis.Sim`, `Delegation`, `architecture.md` | — | DOTS sensor hot path; sim API export |
| 09 | Near-Future Technologies | **Partial** | `NearFutureArchetypeRuntime`, `HypersonicEngageGate`, harness `NF_SPAWN` | — | Full DOTS spawn; MASS tier runtime |
| 10 | Speculative Systems | **Partial+** | Orbital DEW + `KesslerRiskMeter` (S54, 9/9 tests); `ScenarioSpeculativeSettings`, `SpeculativeEngageGate` | — | Escalation ladder integration beyond first-fire hook |
| 10b | (KESSLER) | **Implemented (S54)** | `KesslerRiskMeter` + OrbitalDew debris wiring | — | Escalation ladder use in future tracks |
| 11 | Agentic Mission Editor | **Partial** | `scenario_cyber_status`, `scenario_near_future_spawn`, plan suggest cca/cyber keywords, MCP mission tools | S76 narrative/mission-event policies; S78 C2 picker reads v3 manifest; **WIP:** `AdjudicationWorkspace`, [scenario-editor-research.md](../docs/research/scenario-editor-research.md) | Unity edit mode UX; NL planner; umpire workspace |
| 12 | Terms Glossary | **Partial** | `abort_reason_manifest.json`, `AbortReasonCatalog`, alignment tests | — | UI tooltips; mount-offline abort enum |
| 13 | Doctrine ROE EMCON WRA | **Partial** | `PolicyEvaluator` WRA salvo, `baltic-patrol-mission-roe` / `wra-cap` fixtures | `baltic-v3-mission-roe-band-c` policy | Unity doctrine inheritance panel (ADR-010) |
| 14 | Engagement & Fire Control | **Partial+** | S43-03 `CombatOutcomeResolver` telemetry + eccm/ROE hooks + DLZ bounded | v3 contact-triggered engage paths on patrol variants | Swarm coordinator sectors; DLZ Phase 2 |
| 15 | Sensor Detection & EW | **Partial** (MVP slice **COVERED**) | classify FSM, C2 sensor panels, S34 `DatalinkShareLagResolver`, `baltic-patrol-classify` + datalink fixtures | `baltic-v3-classify`; S75 catalog sensor slices for v3 OOB | ECCM Phase 2; TR-sensor-004 workbook round-trip |
| 16 | Logistics & Magazines | **Partial** | `UnitReadinessMap`, `BalticReplayHarnessReadinessPolicyTests`, readiness golden, `AIR_NOT_READY` | — | UNREP; live magazine counts from catalog |
| 17 | Replay AAR & Order Log | **Partial** | `ReplayGoldenSuiteTests`, CI regression, `tests/regression/README.md` | 6 v3 isolated goldens; S79 playtest batch outputs | Scrub UI; AAR agent; Monte Carlo schema (Phase 5) |
| 18 | Combat Domains | **Partial+** | S28–S33 hot-tick damage/BDA/mine/facility/comms (see 2026-06-04 row 18) | S75/S79 v3 theater + playtest; facility hot-tick UA gate fixed @ S73–S80 closeout | Mine-laying/clearing; full facility combat |
| 19 | Cyber & Comms | **Partial** | spoof harness + golden, `CYBER_SPOOF_TRACK` | `baltic-v3-comms-challenged`, `baltic-v3-patrol-comms` | JADC2 node damage; ECCM Phase 2 |
| 20 | Command & Control UI | **Partial** | C2 proxy through S34 Check 18; headless 58/58 historical; manual sign-off 16/16 | S78 v3 scenario picker + difficulty bands/tooltips; S79 human playtest sign-off | Globe map; `HYPERSONIC_ALERT` UI state |
| 21 | Platform Editor | **MVP-done / Partial+ (S56)** | ADR-011, Phases C–H through S34, platform import/C2 tests (see 2026-06-04 row 21 for full phase history) | S77 extend-only Excel slices for v3; S72/S80 gate evidence | Live Editor screenshots; datalink latency workbook editing |

> **Full S56 evidence chains (S27–S34 sprint IDs, file paths):** preserved verbatim in [implementation-tracker-2026-06-04.md](implementation-tracker-2026-06-04.md) rows 06, 18, 20, 21 — not duplicated here to avoid drift; cite both trackers for audit.

## Forward stack (post-S80)

**Scenario editor program (WIP, unmerged @ 2026-06-30):** per [scenario-editor-research.md](../docs/research/scenario-editor-research.md).

| Component | Path | Req |
|-----------|------|-----|
| Document editor + manifest | `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs`, `ScenarioManifest.cs` | 06, 11 |
| DB migration preview | `ScenarioDbMigrationPreview.cs` | 06 |
| Adjudication workspace | `AdjudicationWorkspace.cs` | 11 |
| AI scaffold services | `AiAuthoringServices.cs` | 07, 11 |
| Live validation | `ScenarioValidationEngine.cs`, `ScenarioDocumentEditorLiveValidationTests.cs` | 06, 11 |
| CLI publish / scaffold | `ScenarioPublishCommand.cs`, `ScenarioAiScaffoldCommand.cs` | 07, 11 |

**Next planning artifact:** new dated `docs/reports/future-sprint-roadpmap-*.md` via `/sprint-plan` when next program is approved (S73–S80 program CLOSED per [future-sprint-roadpmap-062526.01.md](../docs/reports/future-sprint-roadpmap-062526.01.md)).

## Research open gaps (P1)

| Gap | Owner doc | Tracker note |
|-----|-----------|--------------|
| `HYPERSONIC_ALERT` UI state | 20 | Deferred — add when hypersonic track in sim |
| `KESSLER_RISK_METER` | 10 | **Implemented (S54)** — escalation ladder integration remains |
| JADC2 damageable node | 19 | Deferred — entity schema in Data epic |
| Monte Carlo experiment schema | 17 | Phase 5 / req 07 |
| Scenario editor: umpire/adjudication mode | 11 | Research complete; `AdjudicationWorkspace` WIP |
| Scenario editor: DB migration transparency | 06 | Research + `ScenarioDbMigrationPreview` WIP |
| Scenario editor: event graph / static analysis | 11 | Research only — not started |
| v3 corpus default promotion | 02 | **Deferred** — explicit decision only (S80 ack: promotion **no**) |
| UA regression (2 tests) | 13, 14 | `Friendly_weapons_tight_*`, `Restricted_engagement_scenario_fingerprint_*` — fix on closeout branch |

## Workflow (agents)

**S56 MVP closed; S80 content gate closed.**

1. Pick row → read requirement MVP + linked GDD  
2. `gitnexus_impact` on target symbols (CRITICAL: CatalogWriteGate, PatrolCandidateEngagePolicy, DelegationBridge, BalticReplayHarness)  
3. `team-simulation` / `team-data` / `team-unity` by layer  
4. `replay-verify` for sim/delegation changes  
5. **Post-S56:** add evidence to Post-S56 note or Forward stack — do **not** re-grade MVP status without explicit user decision  
6. Update this tracker when new AC tests land on `stack/*`

## Related

- Prior tracker (full S56 evidence): [implementation-tracker-2026-06-04.md](implementation-tracker-2026-06-04.md)
- [research-traceability.md](research-traceability.md)
- [reviews/requirements-13-20-design-review-2026-05-29.md](reviews/requirements-13-20-design-review-2026-05-29.md) — CONCERNS
- Roadmap: [future-sprint-roadpmap-062526.01.md](../docs/reports/future-sprint-roadpmap-062526.01.md) (S73–S80 COMPLETE)
- Execute plan: [roadmap-execute-plan-062526.01.md](../docs/reports/roadmap-execute-plan-062526.01.md)
- Baltic v3 boundary: [baltic-v3-scope-boundary-2026-06-25.md](../production/baltic-v3-scope-boundary-2026-06-25.md)
- Scenario editor research: [scenario-editor-research.md](../docs/research/scenario-editor-research.md)
- S73–S80 closeout: [smoke-sprint-73-80-closeout-2026-06-26.md](../production/qa/smoke-sprint-73-80-closeout-2026-06-26.md)
