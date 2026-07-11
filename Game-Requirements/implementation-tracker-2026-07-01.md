# Game Requirements — Implementation Tracker

> **Historical tracker:** Superseded by [implementation-tracker-2026-07-04.md](implementation-tracker-2026-07-04.md) and post-editor status truth dated 2026-07-09. Current req 11 state: AC-1…AC-12 green headless; Mission Editor Phase 2 complete; residual UI/Phase 2.4+/Phase 3 scoped; production stage remains Release.

**Base:** `fix-scenario-publish-cli-wiring` @ `ee4dc58` (`ee4dc58dc85dea4d9083f3f74772432482aa5532` — scenario editor validation + event trace + doctrine inspection)  
**Last Updated:** 2026-07-01 (post–S80 Baltic v3 gate; scenario editor stack active on branch; S56 21/21 MVP unchanged; verification-before + GitNexus)  
**Supersedes:** [implementation-tracker-2026-06-30.md](implementation-tracker-2026-06-30.md)  
**Index:** [Game-Requirements-Index.md](Game-Requirements-Index.md) | [00-Master-Index.md](../00-Master-Index.md)

## Verdict

| Scope | Status |
|-------|--------|
| Requirements **documentation** (01–21) | **Complete** — drafted, research-integrated; **01–12 FULL** (Sprints 11–15 program closeout 2026-06-08) |
| **MVP / Phase 1 gameplay** implementation | **COMPLETE 21/21** (S56 internal gate PASS 2026-06-21) — all rows MVP-done or documented Partial+ with Baltic AC tests (replay 6/6, proxy 18/18, hash `17144800277401907079`); program exit per [post-release-scope-boundary-2026-06-21.md](../production/post-release-scope-boundary-2026-06-21.md) |
| **Post-MVP content programs** (S57–S80) | **COMPLETE** — Baltic v2 (S57–S64), release train (S65–S68), E7 launch prep (S69–S72), Baltic v3 (S73–S80); human acks: S72 commercial launch prep (2026-06-25), S80 **"Baltic v3 content-complete"** (2026-06-26); stage remains **Release**; v3 corpus promotion **no** (explicit decision only) |
| **Forward engineering** (post-S80) | **In progress** — scenario editor authoring stack on `fix-scenario-publish-cli-wiring` (req 06/07/11/13); see [Scenario editor program](#scenario-editor-program-post-s80) |

Completing the full requirements corpus as *shipped game features* is multi-year work (req 07 phases 1–5). This tracker is the execution backlog for stacked `stack/*` branches. **Post-MVP content-only tracks add evidence additively; they do not re-litigate the 21/21 MVP row grades closed @ S56** (per [baltic-v3-scope-boundary-2026-06-25.md](../production/baltic-v3-scope-boundary-2026-06-25.md)).

## Verification baseline

**Gate baseline (S80, 2026-06-26):** `dotnet test ProjectAegis.sln` → **≥1232/0f**; ReplayGolden **6/6**; PlayMode smoke **18/18**; Baltic v2 hash **`17144800277401907079`** preserved; ZERO `DelegationBridge` hotpath edits. Evidence: [smoke-sprint-73-80-closeout-2026-06-26.md](../production/qa/smoke-sprint-73-80-closeout-2026-06-26.md), `production/stage.txt` (S80 gate rows).

**Measured @ 2026-07-01 (`fix-scenario-publish-cli-wiring`, RUN):**

| Assembly | Pass | Fail | Total |
|----------|------|------|-------|
| ProjectAegis.Sim.Tests | 281 | 0 | 281 |
| ProjectAegis.Delegation.Tests | 249 | 0 | 249 |
| ProjectAegis.Data.Excel.Tests | 5 | 0 | 5 |
| ProjectAegis.MissionEditor.Cli.Tests | 52 | 0 | 52 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 257 | **2** | 259 |
| ProjectAegis.Data.Tests | **416** | 0 | **416** |
| **Solution total** | **1260** | **2** | **1262** |

**Hard gates (subset, RUN @ 2026-07-01):** ReplayGoldenSuite **6/6 PASS**; PlayModeSmokeHarnessTests **18/18 PASS**; hash invariant preserved.

**UA failures (open — fix before merge claims):** `Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log`, `Restricted_engagement_scenario_fingerprint_is_deterministic` (`BalticReplayHarnessPolicyEngageTests`).

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal                    # gate floor ≥1232/0f
dotnet test ProjectAegis.sln --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "ScenarioDocumentEditorLiveValidation|ScenarioValidationEngine"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "ScenarioPublish|AiScaffold|McpMission"
pwsh tools/unity/Invoke-ManualQaHeadlessGate.ps1
```

## Post-MVP program (S57–S80)

Additive content and ops only. Does **not** change S56 MVP row grades. (Unchanged from [2026-06-30 tracker](implementation-tracker-2026-06-30.md) — see that file for full sprint table and Baltic v3 inventory.)

| Program | Sprints | Status |
|---------|---------|--------|
| Baltic v2 content expansion | S57–S64 | **COMPLETE** |
| Release train | S65–S68 | **COMPLETE** |
| E7 commercial launch prep | S69–S72 | **COMPLETE** |
| Baltic v3 content expansion (E9) | S73–S80 | **COMPLETE** |

**Baltic v3 inventory:** 6 `baltic-v3-*` policies, 6 isolated v3 replay goldens, `production/playtests/baltic-v3-scenario-manifest.yaml`. **Standing invariants:** hash `17144800277401907079` unchanged; DelegationBridge zero-touch; CatalogWriteGate extend-only; test baseline monotonic ≥1232.

## Scenario editor program (post-S80)

**Branch:** `fix-scenario-publish-cli-wiring` (stacked on main after S73–S80 closeout merge `ed31ded`).  
**Research:** [scenario-editor-research.md](../docs/research/scenario-editor-research.md).  
**Requirement mapping (2026-07-01):** req 11 revised to house style — capabilities below now trace to `AME-*` IDs in [11-Agentic-Mission-Editor.md](requirements/11-Agentic-Mission-Editor.md) (adjudication AME-10.1, migration preview AME-10.2, publish governance AME-10.3, live validation AME-6.9/10.4, event static analysis AME-5.7/10.5; Validation Engine + determinism AME-6.*, editVersion AME-7.1). Open decisions recorded in ADR-013…017. Scenario doc schema + fixtures: `data/scenarios/scenario-document.schema.json`, `data/scenarios/examples/`.

| Milestone | Commit | Deliverable | Req |
|-----------|--------|-------------|-----|
| Authoring validation + publish tools | `b3a784d` | `ScenarioValidationEngine`, `ScenarioValidateCommand`, `ScenarioPublishCommand`, `ScenarioManifest`, `ManifestBuilder` | 06, 11 |
| CLI dispatch fix | `d1709a7` | `scenario_publish` wired in `Program.cs` | 07 |
| Per-rule validation + trace + doctrine | `ee4dc58` | `IncompatibleHostRule`, `BrokenRefRule`, live editor validation tests (+3 Data.Tests → **416**), `ExplainEventTrace` + `scenario_event_trace`, `ScenarioDbMigrationPreview` doctrine inspection via `PlatformHasDoctrine`, TCA static analysis hook on editor | 06, 11, 13 |

**Implemented components (headless):**

| Area | Path / CLI verb | Status |
|------|-----------------|--------|
| Document editor | `ScenarioDocumentEditor.cs` — missions, live validate, migration preview, umpire hooks | **Partial+** |
| Adjudication workspace | `AdjudicationWorkspace.cs` — snapshot, diff, audit, freeze/step/inject/resume, role guard | **Partial** (headless; no Unity UX) |
| DB migration preview | `ScenarioDbMigrationPreview.cs`, `scenario_migrate_preview` | **Partial** |
| Live validation rules | `ValidationRules.cs` + `ScenarioDocumentEditorLiveValidationTests` (6 rules: mission/patrol/strike/ferry/host/ref) | **Partial+** |
| Event trace | `ExplainEventTrace`, `scenario_event_trace` | **Partial** (minimal trace strings) |
| Publish pipeline | `ScenarioPublishCommand`, embedded validation + provenance manifest | **Partial+** |
| AI scaffold | `ScenarioAiScaffoldCommand`, `AiAuthoringServices.NlScaffold` | **Partial** |
| Umpire CLI | `scenario_umpire_snapshot`, inject/freeze via editor + workspace | **Partial** |

**CLI verbs (MissionEditor.Cli):** `scenario_create`, `scenario_validate`, `scenario_publish`, `scenario_ai_scaffold`, `scenario_event_trace`, `scenario_migrate_preview`, `scenario_umpire_snapshot`, `scenario_export_brief`, `scenario_simulate_sample`, plus existing `scenario_comms_status` / `scenario_cyber_status` / `scenario_near_future_spawn`.

**Not started / deferred:** visual event graph editor; full Monte Carlo handoff; Unity edit-mode UX; reversible migration persistence to disk; `HYPERSONIC_ALERT` UI.

## MVP status by requirement

**S56 gate: 21/21 rows MVP-done / documented Partial+ (Baltic AC sufficient) + program exit.** Evidence @ S56 preserved in [implementation-tracker-2026-06-04.md](implementation-tracker-2026-06-04.md). **Post-S56 note** is additive only.

| Req | Title | MVP status (S56) | Post-S56 note (through 2026-07-01) | Next stack task |
|-----|-------|------------------|--------------------------------------|-----------------|
| 01 | Project Overview | **MVP-done (S56)** | S72 commercial launch prep complete | (complete @ S56) |
| 02 | Core Gameplay Loop | **Partial** | S74/S76 v3 policies + contact-triggered ROE escalation | Begin Execution UX polish |
| 03 | Simulation Modes | **Partial+** | S76 mission-event / contact-window on v3 fixtures | Mode UI on C2 top bar |
| 04 | Agent Delegation | **Partial+** | — | C2 delegation badges |
| 05 | Dynamic Speculative Systems Agent | **Partial+** | — | MCP polish; Data P1 |
| 06 | Database Intelligence | **Partial** | S77 v3 catalog slices; scenario editor: live validation, migration preview w/ doctrine inspection, publish manifest | Scenario package persistence; full corpora in CI |
| 07 | Agentic Infrastructure | **Partial** | S73–S80 gates; scenario editor CLI suite (`scenario_*` verbs); smoke-test agent string on editor | Experiment workers; MCP mission tool polish |
| 08 | Agentic Architecture | **Partial** | — | DOTS sensor hot path |
| 09 | Near-Future Technologies | **Partial** | — | Full DOTS spawn |
| 10 | Speculative Systems | **Partial+** | S54 orbital DEW + Kessler (9/9) | Escalation ladder integration |
| 10b | (KESSLER) | **Implemented (S54)** | — | Escalation ladder |
| 11 | Agentic Mission Editor | **Partial** | S76/S78 v3 content + C2 picker; **scenario editor:** adjudication workspace, event trace, AI scaffold, publish, TCA static analysis stub | Unity edit mode; visual event graph; NL planner UX |
| 12 | Terms Glossary | **Partial** | — | UI tooltips |
| 13 | Doctrine ROE EMCON WRA | **Partial** | `baltic-v3-mission-roe-band-c`; migration preview `broken_doctrine` counts; red-team doctrine variant assistant on editor | Unity doctrine inheritance panel (ADR-010) |
| 14 | Engagement & Fire Control | **Partial+** | v3 contact-triggered engage on patrol variants | Swarm coordinator; DLZ Phase 2; **fix UA engage regression (2 tests)** |
| 15 | Sensor Detection & EW | **Partial** (MVP slice **COVERED**) | `baltic-v3-classify`; S75 catalog sensor slices | ECCM Phase 2 |
| 16 | Logistics & Magazines | **Partial** | — | UNREP; live magazine counts |
| 17 | Replay AAR & Order Log | **Partial** | 6 v3 goldens; S79 playtest batch | Scrub UI; AAR agent; Monte Carlo schema |
| 18 | Combat Domains | **Partial+** | S75/S79 v3 theater + playtest | Mine-laying/clearing; full facility combat |
| 19 | Cyber & Comms | **Partial** | `baltic-v3-comms-challenged`, `baltic-v3-patrol-comms` | JADC2 node damage |
| 20 | Command & Control UI | **Partial** | S78 v3 picker + bands; S79 playtest sign-off | Globe map; `HYPERSONIC_ALERT` UI |
| 21 | Platform Editor | **MVP-done / Partial+ (S56)** | S77 v3 Excel slices; S72/S80 gate evidence | Live Editor screenshots |

> **Full S56 evidence chains (S27–S34 sprint IDs):** [implementation-tracker-2026-06-04.md](implementation-tracker-2026-06-04.md) rows 06, 18, 20, 21.

## Research open gaps (P1)

| Gap | Owner doc | Tracker note |
|-----|-----------|--------------|
| `HYPERSONIC_ALERT` UI state | 20 | Deferred |
| `KESSLER_RISK_METER` | 10 | **Implemented (S54)** — escalation integration remains |
| JADC2 damageable node | 19 | Deferred — entity schema in Data epic |
| Monte Carlo experiment schema | 17 | Phase 5 / req 07 |
| Scenario editor: visual event graph | 11 | TCA static analysis stub only; no graph UI |
| Scenario editor: umpire/adjudication mode | 11 | **Partial** — `AdjudicationWorkspace` headless; Unity UX not started |
| Scenario editor: DB migration reversibility | 06 | Snapshot/rollback in-memory on editor; not persisted |
| v3 corpus default promotion | 02 | **Deferred** — S80 ack: promotion **no** |
| UA regression (2 tests) | 14 | Pre-existing on branch; unrelated to scenario editor commits |

## Workflow (agents)

**S56 MVP closed; S80 content gate closed; scenario editor active post-S80.**

1. Pick row → read requirement MVP + linked GDD  
2. `gitnexus_impact` on target symbols (CRITICAL: CatalogWriteGate, PatrolCandidateEngagePolicy, DelegationBridge, BalticReplayHarness)  
3. `team-simulation` / `team-data` / `team-unity` by layer  
4. `replay-verify` for sim/delegation changes  
5. **Post-S56:** add evidence to Post-S56 note or Scenario editor program — do **not** re-grade MVP status without explicit user decision  
6. Update this tracker when new AC tests land on `stack/*`

## Related

- Prior trackers: [2026-06-30](implementation-tracker-2026-06-30.md) | [2026-06-04 (full S56 evidence)](implementation-tracker-2026-06-04.md)
- [research-traceability.md](research-traceability.md)
- Roadmap: [future-sprint-roadpmap-062526.01.md](../docs/reports/future-sprint-roadpmap-062526.01.md) (S73–S80 COMPLETE)
- Scenario editor research: [scenario-editor-research.md](../docs/research/scenario-editor-research.md)
- S73–S80 closeout: [smoke-sprint-73-80-closeout-2026-06-26.md](../production/qa/smoke-sprint-73-80-closeout-2026-06-26.md)
- Baltic v3 boundary: [baltic-v3-scope-boundary-2026-06-25.md](../production/baltic-v3-scope-boundary-2026-06-25.md)
