# Game Requirements — Implementation Tracker

**Base:** `fix-scenario-publish-cli-wiring` @ `7b0f376` (`7b0f3766e8fe22946438ffe84a8643117cf90976` — scenario editor validation tracks A–D for doc-11)  
**Last Updated:** 2026-07-04 (GitNexus pre: branch index @ `e0f8a91`, 1 commit stale vs HEAD; `ScenarioDocumentEditor` CRITICAL / `ScenarioValidationEngine` HIGH upstream; verification-before RUN)  
**Supersedes:** [implementation-tracker-2026-07-01.md](implementation-tracker-2026-07-01.md)  
**Index:** [Game-Requirements-Index.md](Game-Requirements-Index.md) | [00-Master-Index.md](../00-Master-Index.md)  
**Open PR:** [Graphite #237](https://app.graphite.com/github/pr/drgaciw/cmano-clone/237) (draft; **local branch +9 commits** ahead of remote at submit time — re-run `gt submit --stack --no-interactive` to refresh)

## Verdict

| Scope | Status |
|-------|--------|
| Requirements **documentation** (01–21) | **Complete** — **req 11 revised 2026-07-01** (`AME-*` IDs, AC-1…AC-12, ADR-008/013–017 cross-refs); other docs 01–10, 12–21 unchanged |
| **MVP / Phase 1 gameplay** implementation | **COMPLETE 21/21** (S56 gate PASS 2026-06-21) — grades frozen; Baltic ACs (replay 6/6, proxy 18/18, hash `17144800277401907079`) |
| **Post-MVP content programs** (S57–S80) | **COMPLETE** — Baltic v2/v3, release train, E7 prep; S80 ack **"Baltic v3 content-complete"** (2026-06-26); stage **Release** |
| **Forward engineering** (post-S80) | **SCOPED — S89–S92 post-editor hygiene** (2026-07-09). Editors complete: S81–S88 scenario editor, ME Phase 2, PE (req 21). Epic hygiene train; roadmap [`future-sprint-roadpmap-07092026.md`](../docs/reports/future-sprint-roadpmap-07092026.md); boundary [`post-editor-hygiene-scope-boundary-2026-07-09.md`](../production/post-editor-hygiene-scope-boundary-2026-07-09.md); status truth [`post-editor-status-truth-2026-07-09.md`](../production/agentic/post-editor-status-truth-2026-07-09.md). Stage **Release**. |

Completing the full requirements corpus as *shipped game features* is multi-year work (req 07 phases 1–5). **Post-MVP tracks add evidence additively; they do not re-litigate S56 MVP row grades** ([baltic-v3-scope-boundary-2026-06-25.md](../production/baltic-v3-scope-boundary-2026-06-25.md)).

## Verification baseline

**Gate baseline (post-PE, 2026-07-09):** ≥**1599/0f**; ReplayGolden **6/6**; PlayMode smoke **≥20/20**; hash **`17144800277401907079`** preserved; ZERO `DelegationBridge` hotpath edits. Evidence: [`production/qa/evidence/gates-post-editor-hygiene-2026-07-09.log`](../production/qa/evidence/gates-post-editor-hygiene-2026-07-09.log).

**Gate baseline (S80, 2026-06-26, superseded floor):** ≥**1232/0f**; ReplayGolden **6/6**; PlayMode smoke **18/18**; hash preserved; ZERO bridge.

**Measured @ 2026-07-04 (`fix-scenario-publish-cli-wiring`, RUN):**

| Assembly | Pass | Fail | Total | Δ vs 2026-07-01 |
|----------|------|------|-------|-----------------|
| ProjectAegis.Sim.Tests | 281 | 0 | 281 | — |
| ProjectAegis.Delegation.Tests | 249 | 0 | 249 | — |
| ProjectAegis.MissionEditor.Cli.Tests | **63** | 0 | **63** | **+11** |
| ProjectAegis.Data.Excel.Tests | 5 | 0 | 5 | — |
| ProjectAegis.Delegation.UnityAdapter.Tests | 257 | **2** | 259 | — |
| ProjectAegis.Data.Tests | **453** | 0 | **453** | **+37** |
| **Solution total** | **1308** | **2** | **1310** | **+48 pass** |

**Hard gates (subset, RUN @ 2026-07-04):** ReplayGoldenSuite **6/6 PASS**; PlayModeSmokeHarnessTests **18/18 PASS**; hash invariant preserved.

**UA engage (req 14):** **CLOSED @ green** (2026-07-09). `BalticReplayHarnessPolicyEngageTests` **3/3** on trunk @ `223a5fe`. Historically 2 failures (2026-07-04) — resolved; see [`production/qa/ua-engage-triage-2026-07-09.md`](../production/qa/ua-engage-triage-2026-07-09.md). **Include in gate** — no exclusion.

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal                    # floor ≥1599/0f (post-PE 2026-07-09)
dotnet test ProjectAegis.sln --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "ScenarioPublish|AiScaffold|MissionAddFerry|ScenarioValidate|SampleComplete"
bash tools/ci/smoke-ac6.sh                              # AC-6 byte-determinism smoke (scenario editor)
pwsh tools/unity/Invoke-ManualQaHeadlessGate.ps1
```

## GitNexus intelligence (2026-07-04)

| Symbol | Upstream impact | Risk | Notes |
|--------|-----------------|------|-------|
| `ScenarioDocumentEditor` | 20 symbols, 6 CLI mission flows | **CRITICAL** | Hub for all `mission_*` / `scenario_create` / adjudication paths |
| `ScenarioValidationEngine` | 17 symbols, export + simulate flows | **HIGH** | Sole export gate (ADR-008); extended rules in tracks A–D |
| `CatalogWriteGate` | (unchanged §5) | **CRITICAL** | extend-only — not touched this program |
| `DelegationBridge` | ZERO | **CRITICAL** | no hotpath edits |
| `BalticReplayHarness` | read/verify only | **CRITICAL** | v2 hash preserved |

**Index freshness:** GitNexus branch snapshot for `fix-scenario-publish-cli-wiring` indexed @ `e0f8a91` (2026-07-03); HEAD is `7b0f376` — run `node .gitnexus/run.cjs analyze` post-merge.

## Post-MVP program (S57–S80)

Unchanged — **COMPLETE**. See [implementation-tracker-2026-07-01.md](implementation-tracker-2026-07-01.md) for sprint table and Baltic v3 inventory (6 policies, 6 goldens, manifest).

## Scenario editor program (post-S80)

**Branch:** `fix-scenario-publish-cli-wiring` (stacked after S73–S80 closeout merge `ed31ded`).  
**Requirement:** [11-Agentic-Mission-Editor.md](requirements/11-Agentic-Mission-Editor.md) (revised 2026-07-01, `AME-*` / AC-1…AC-12).  
**QA plan:** [qa-plan-scenario-editor-2026-07-01.md](../production/qa/qa-plan-scenario-editor-2026-07-01.md) (19 testable units).  
**ADRs:** [013](../docs/architecture/adr-013-cmo-scenario-import-policy.md) (CMO import), [014](../docs/architecture/adr-014-lua-compatibility-scope.md) (Lua deferred), [015](../docs/architecture/adr-015-agent-authored-scenario-transparency.md), [016](../docs/architecture/adr-016-event-graph-complexity-caps.md), [017](../docs/architecture/adr-017-editor-topology-client-vs-scenario-lab.md).  
**Schema + fixtures:** `data/scenarios/scenario-document.schema.json`; `data/scenarios/examples/*.scenario.json` (3); `data/scenarios/validation/doctrine-inheritance.json`.

### Commit timeline (post-S80)

| Commit | Summary | Req |
|--------|---------|-----|
| `b3a784d` | Authoring validation + publish tools (`ScenarioValidationEngine`, `ScenarioPublishCommand`, `ManifestBuilder`) | 06, 11 |
| `d1709a7` | Wire `scenario_publish` CLI dispatch | 07 |
| `ee4dc58` | Per-rule live validation, event trace stub, doctrine inspection in migration preview | 06, 11, 13 |
| `457b5cd` | Implementation tracker 2026-07-01 | (docs) |
| `648af6c` | Disable reference assemblies (parallel build CS0006 fix) | build |
| `9d25e70` | Event id tracking + trace stub on editor | 11 |
| `7f566b1` | Scenario document JSON schema + example fixtures | 06, 11 |
| `3a4c61e` | Ferry mission CLI + MCP (`MissionAddFerryCommand`, `MissionUpdateFerryCommand`) | 11 (AME-8.4) |
| `e8f2139` | Tests: save-vs-export gate, schema conformance, stub pins, no-Lua architecture gate | 06, 11 |
| `865e694` | `tools/ci/smoke-ac6.sh` — AC-6 byte-determinism smoke | 11 (AC-6) |
| `25685ad` | ADRs 013–017 | 11 |
| `e0f8a91` | Align req 11 + QA plan + improvement plans | 11 |
| `7b0f376` | **Validation tracks A–D** for doc-11 (see below) | 06, 11, 13, 17 |

### Validation tracks A–D (@ `7b0f376`)

| Track | Focus | Key deliverables | Maturity |
|-------|-------|------------------|----------|
| **A** | Live validation + doctrine | `DoctrineInheritanceValidateTests`, doctrine fixture, expanded `ValidationRules` | **Partial+** |
| **B** | Event debugger / export transforms | `EventDebuggerTrace.cs`, `EventDebuggerTests`, `TeleportUnitExportTests`, `ExportTransformManifest` | **Partial** (debugger beyond stub) |
| **C** | Schema / derived-only invariants | Expanded schema (+127 lines), `DerivedOnlyInvariantTests`, `ScenarioDocumentSchemaConformanceTests` | **Partial+** |
| **D** | Undo / export command surface | `ScenarioUndoStackStore`, `ScenarioExportCommand`, `ScenarioUndoCliTests`, scenario_export verb, AC-5 ferry sample | **Partial+** (S83: export polish, undo disk wiring+test, ferry AC-5 fixture; cites sprint-83 + execute-plan + boundary + qa #5/13/14) |

### Headless component inventory

| Component | Path / verb | Status |
|-----------|-------------|--------|
| Document editor | `ScenarioDocumentEditor.cs` — missions, live validate, migration, umpire, undo stack | **Partial+** |
| Validation engine | `ScenarioValidationEngine.cs` + `ValidationRules.cs` | **Partial+** |
| Export gate | `ScenarioValidationExportGate`, `SaveVsExportGateTests` (AC-12) | **Partial+** |
| Adjudication | `AdjudicationWorkspace.cs`, `scenario_umpire_snapshot` | **Partial** |
| DB migration preview | `ScenarioDbMigrationPreview.cs`, `scenario_migrate_preview` | **Partial** |
| Publish | `ScenarioPublishCommand`, `scenario_publish` | **Partial+** |
| Export | `ScenarioExportCommand` (track D) | **Partial** (new) |
| Ferry missions | `mission_add_ferry` / `mission_update_ferry`, MCP tools | **Partial+** (AME-8.4 unblocked) |
| Event trace / debugger | `ExplainEventTrace`, `EventDebuggerTrace`, `scenario_event_trace` | **Partial** (stub → trace object in track B) |
| AI scaffold | `ScenarioAiScaffoldCommand` | **Partial** |
| Undo | `ScenarioUndoStackStore`, undo CLI tests | **Partial** (AME-8.5 wiring) |
| CI smoke | `tools/ci/smoke-ac6.sh` | **Partial+** (AC-6 approximated; see script header) |

**CLI verbs:** `scenario_create`, `scenario_validate`, `scenario_publish`, `scenario_export` (track D), `scenario_ai_scaffold`, `scenario_event_trace`, `scenario_migrate_preview`, `scenario_umpire_snapshot`, `scenario_export_brief`, `scenario_simulate_sample`, `mission_add_ferry`, `mission_update_ferry`, plus existing status/spawn verbs.

**Phase 2 residual / Phase 2.4+ / Phase 3:** Unity map host/product chrome, Unity Mission Board window, visual event graph chrome, Gantt UI, layers/minimap, mining/cargo archetypes, reversible migration UX, CMO import, Lua, and Phase 3 authoring agents remain deferred/scoped. Headless AC-1…AC-12 are green.

## MVP status by requirement

**S56 gate: 21/21 MVP-done / documented Partial+.** Evidence @ S56: [implementation-tracker-2026-06-04.md](implementation-tracker-2026-06-04.md). **Post-S56 note** is additive only.

**Program note (corpus maturity W0–W4, 2026-07-08):** Waves 0–4 corpus honesty complete; **no MVP regrade**. Req **10b** remains **Phase N / not on main**.

**Program note (post-editor forward scope, 2026-07-09):** S81–S88 + scenario-editor-completion + ME Phase 2 + PE **COMPLETE** on trunk. Forward program = **S89–S92 engineering hygiene + asset specs** per `future-sprint-roadpmap-07092026.md`. Floors: **1599/0f**, C2 **20/20**. GitNexus fresh @ `223a5fe` (24,418 / 47,032). Launch stage **not** advanced.

| Req | Title | MVP status (S56) | Post-S56 note (through 2026-07-04) | Next stack task |
|-----|-------|------------------|--------------------------------------|-----------------|
| 01 | Project Overview | **MVP-done (S56)** | S72 commercial launch prep complete; **doc charter re-baseline 2026-07-08** (hub FR-19/index/invariants — corpus maturity W0; MVP grade unchanged) | (complete @ S56); corpus maturity W0–W4 complete 2026-07-08; editor train (req 11) still active |
| 02 | Core Gameplay Loop | **Partial** | S74/S76 v3 policies + contact-triggered ROE; **doc honesty Wave 1 2026-07-08** (mapping; MVP grade unchanged) | Begin Execution UX polish |
| 03 | Simulation Modes | **Partial+** | S76 mission-event policies on v3 fixtures; **doc honesty Wave 1 2026-07-08** | Mode UI on C2 top bar |
| 04 | Agent Delegation | **Partial+** | **doc honesty Wave 1 2026-07-08** (expanded mapping) | C2 delegation badges |
| 05 | Dynamic Speculative Systems Agent | **Partial+** | **doc honesty Wave 1 2026-07-08** (OSINT mapping) | MCP polish; Data P1 |
| 06 | Database Intelligence | **Partial** | Scenario schema (AME-2.6), validation engine extensions, save-vs-export gate, migration preview w/ doctrine counts; **doc honesty Wave 1 2026-07-08** | Full corpora in CI; persisted migration rollback |
| 07 | Agentic Infrastructure | **Partial** | Expanded `scenario_*` + ferry MCP/CLI; AC-6 smoke script; QA plan for editor program; **doc honesty Wave 1 2026-07-08** (product vs process) | Experiment workers; re-submit PR stack |
| 08 | Agentic Architecture | **Partial** | ADR-017 editor topology (client vs scenario lab); **doc honesty Wave 1 2026-07-08** | DOTS sensor hot path |
| 09 | Near-Future Technologies | **Partial** | **doc honesty Wave 3 2026-07-08** (FR-08; headless archetype/TL/spawn-plan spine mapped; full DOTS NF content Phase N; MVP grade unchanged) | Full DOTS spawn |
| 10 | Speculative Systems | **Partial+** | Wave 3 honesty: Partial+ = TL/black-project SpeculativeEngageGate + catalog metadata; S54 OrbitalDew/Kessler runtime **not on main** 2026-07-08 | Escalation ladder (design); full orbital DEW/Kessler runtime Phase N |
| 10b | (KESSLER) | **Phase N / not on main** | Wave 3: `OrbitalDewPlatform` / `KesslerRiskMeter` / `EscalationTier` absent from `src/` (rg zero hits 2026-07-08); design ladder only | Re-land runtime only with gate + tests; do not treat S54 wt claims as trunk |
| 11 | Agentic Mission Editor | **Partial+** (headless; AC-1…AC-12 green) | **S81–S88 + ME Phase 2 COMPLETE 2026-07-09:** ferry/support/undo, AC-7, AC-8, sides, timeline, semantic diff, event/static analysis shipped headless; stage remains Release | Phase 2.4+ Unity/product chrome; Phase 3 agents/import/Lua deferred |
| 12 | Terms Glossary | **Partial** | **doc honesty Wave 1 2026-07-08** (additive terms) | UI tooltips |
| 13 | Doctrine ROE EMCON WRA | **Partial** | `DoctrineInheritanceValidateTests` + fixture; v3 mission-roe policy; **doc honesty Wave 2 2026-07-08** | Unity doctrine panel (ADR-010) |
| 14 | Engagement & Fire Control | **Partial+** | v3 contact-triggered engage; **doc honesty Wave 2 2026-07-08** | **Fix 2 UA engage tests**; DLZ Phase 2 |
| 15 | Sensor Detection & EW | **Partial** (MVP **COVERED**) | v3 classify + catalog sensor slices; **doc honesty Wave 2 2026-07-08** | ECCM Phase 2 |
| 16 | Logistics & Magazines | **Partial** | **doc honesty Wave 2 2026-07-08** (mapping: MagazineLedger/FuelLedger/AIR_NOT_READY) | UNREP; live magazines |
| 17 | Replay AAR & Order Log | **Partial** | 6 v3 goldens; event debugger aligned to order-log projection (AME-5.5); **doc honesty Wave 2 2026-07-08** (headless P0 vs scrub residual) | Scrub UI; AAR agent |
| 18 | Combat Domains | **Partial+** | S75/S79 v3 theater; **doc honesty Wave 2 2026-07-08** (ADR-009) | Mine-laying/clearing |
| 19 | Cyber & Comms | **Partial** | v3 comms policies; **doc honesty Wave 2 2026-07-08** (spoof Shipped) | JADC2 node damage |
| 20 | Command & Control UI | **Partial** | S78 v3 picker + bands; **doc honesty Wave 2 2026-07-08** (ADR-010) | Globe map; `HYPERSONIC_ALERT` UI |
| 21 | Platform Editor | **MVP-done / Partial+ (S56)** | S77 v3 Excel slices; **doc honesty Wave 3 2026-07-08** (FR-19; mapping New→Shipped/Partial+; residual live Editor screenshots Phase N; MVP grade frozen) | Live Editor screenshots |

## Research open gaps (P1)

| Gap | Owner | Note |
|-----|-------|------|
| `HYPERSONIC_ALERT` UI | 20 | Deferred |
| Visual event graph editor | 11 | ADR-016 caps specified; TCA stub only |
| Unity map/product chrome residual (AME-4.x) | 11 | Headless ORBAT/RP map mutations shipped Partial+; Unity host chrome remains Phase 2.4+ residual |
| Lua compatibility shim | 11 | **Deferred** — ADR-014 |
| Monte Carlo experiment schema | 17 | Phase 5 / req 07 |
| v3 corpus promotion | 02 | S80 ack: **no** unless explicit |
| UA regression (2 tests) | 14 | Open on branch |
| PR stack stale vs local | 07 | Re-run `gt submit` after review |

## Workflow (agents)

**S56 MVP closed; S80 content gate closed; scenario editor tracks A–D active.**

1. Pick row → read requirement MVP + linked GDD  
2. **GitNexus pre:** `impact()` on `ScenarioDocumentEditor`, `ScenarioValidationEngine`, and any §5 CRITICAL before edits  
3. `team-data` / `team-simulation` / `team-unity` by layer  
4. `replay-verify` for sim/delegation changes; `bash tools/ci/smoke-ac6.sh` for editor serialization  
5. **Post-S56:** additive evidence only — do **not** re-grade MVP status without user decision  
6. Update this tracker when AC tests land on `stack/*`

## Related

- Prior trackers: [2026-07-01](implementation-tracker-2026-07-01.md) | [2026-06-30](implementation-tracker-2026-06-30.md) | [2026-06-04 (S56 evidence)](implementation-tracker-2026-06-04.md)
- Req 11 (revised): [11-Agentic-Mission-Editor.md](requirements/11-Agentic-Mission-Editor.md)
- QA plan: [qa-plan-scenario-editor-2026-07-01.md](../production/qa/qa-plan-scenario-editor-2026-07-01.md)
- Research: [scenario-editor-research.md](../docs/research/scenario-editor-research.md)
- Roadmap: [future-sprint-roadpmap-062526.01.md](../docs/reports/future-sprint-roadpmap-062526.01.md) (S73–S80 COMPLETE)
- S73–S80 closeout: [smoke-sprint-73-80-closeout-2026-06-26.md](../production/qa/smoke-sprint-73-80-closeout-2026-06-26.md)
