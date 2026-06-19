# C2 manual QA sign-off — 2026-06-02

**Sprint / task:** Sprint 19 / **S19-01** (carryover from S18-01); refreshed Sprint 31 / **S31-08** (checks 14–16); upgraded Sprint 32 / **S32-11** (checks 14–16 Phase F damage); upgraded Sprint 33 / **S33-11** (check 17 Phase G comms; checks 14–16 S33 evidence refresh); upgraded Sprint 34 / **S34-11** (check 18 Phase H link catalog; checks 14–17 S34 evidence refresh)  
**Build:** `main` @ `d3db76d` (post-S34 refresh 2026-06-19)
**Prior baseline:** `7401fac` (S19-01 closeout 2026-06-08, checks 1–13); `3406bc4` (S31-08 checks 14–16)  
**Headless/proxy evidence:** `production/qa/smoke-2026-06-08.md` + `c2-automated-proxy-2026-06-02.md` (412 tests, 7/7 PlayMode harness, 17/17 replay)  
**S31 presentation evidence:** `production/qa/sprint-31-presentation-evidence-2026-06-18.md` + `production/qa/evidence/*-s31-*.png`  
**S31 C2 sign-off evidence:** `production/qa/sprint-31-c2-signoff-2026-06-18.md`  
**S32 presentation evidence:** `production/qa/sprint-32-presentation-evidence-2026-06-19.md` + `production/qa/evidence/*-s32-*.png`  
**S32 C2 sign-off evidence:** `production/qa/sprint-32-c2-signoff-2026-06-19.md`  
**S33 presentation evidence:** `production/qa/sprint-33-presentation-evidence-2026-06-19.md` + `production/qa/evidence/*-s33-*.png`  
**S33 C2 sign-off evidence:** `production/qa/sprint-33-c2-signoff-2026-06-19.md`  
**S33-06 dependency:** `production/agentic/stacks/sprint33/S33-06-DONE.md` (Phase G comms surfacing)
**S34 presentation evidence:** `production/qa/sprint-34-presentation-evidence-2026-06-19.md` + `production/qa/evidence/*-s34-*.png` *(protocol placeholder references — S34-10 advisory)*  
**S34 C2 sign-off evidence:** `production/qa/sprint-34-c2-signoff-2026-06-19.md`  
**S34-06 dependency:** `production/agentic/sprint-34-platform-phase-h-link-catalog-2026-06-19.md` (Phase H link catalog surfacing)
**Batch Play Mode (check 1):** `tools/unity/Invoke-C2PlayModeSignoffBatch.ps1` — comms + classify **PASS** @ 2026-06-08 (`unity-c2-playmode-signoff.log`); import + begin-execution scenarios documented (S30-06/S31-07), not run on headless Linux  
**Playtest notes:** `production/session-logs/playtest-sprint19-c2-signoff.md`  
**Scenarios:** `baltic-patrol-classify` (selection), `baltic-patrol-comms` (COMMS + map stale), `baltic-patrol-mission-roe` (doctrine), `DelegationSmoke` + `PlatformImportPanelHost` (import staging)  
**Reference:** `unity/ProjectAegis/PLAYMODE-SMOKE.md`  
**Automated proxy:** `production/qa/c2-automated-proxy-2026-06-02.md` (checks 2–13; PI-006: `production/qa/pi-006-headless-proxy-2026-06-04.md`)

| # | Check | Pass | Tester | Notes |
|---|--------|------|--------|-------|
| 1 | Play starts without console errors | ☑ | batch-runner-2026-06-08 | `Invoke-C2PlayModeSignoffBatch.ps1` comms + classify; Unity 6000.3.14f1 batchmode; no game errors; re-confirmed headless proxy @ `3406bc4` |
| 2 | Default unit selected (OOB + map ring) | ☑ (headless) | S31-08 | C2SelectionFlowTests + PlayModeSmokeHarnessTests.Baltic_classify_map_symbols_* |
| 3 | Map ■ click updates unit detail | ☑ (proxy) | S31-08 | UnitDetailBridgeTests + selection projection; visual click feel optional |
| 4 | OOB row click syncs map | ☑ (proxy) | S31-08 | OobTreeBridgeTests + harness |
| 5 | Hostile ◆ shows CONTACT line | ☑ (headless) | S31-08 | C2SelectionFlowTests + contact picture tests |
| 6 | CONTACTS tab click matches map | ☑ (proxy) | S31-08 | Harness + selection tests |
| 7 | Top bar score ticks on engage | ☑ (headless) | S31-08 | LossesScoringCsvExporterTests + Baltic_patrol_scoring_csv_row_is_deterministic |
| 8 | Message log shows CONTACT/MISSION lines | ☑ (headless) | S31-08 | BalticReplayHarnessCommsTests + comms harness messages |
| 9 | `baltic-patrol-comms`: COMMS bar DEGRADED → DENIED | ☑ (headless) | S31-08 | Baltic_patrol_comms_harness_matches_manual_qa_preconditions |
| 10 | Hostile ◆ dimmed (degraded), all symbols dimmer (denied) | ☑ (headless) | S31-08 | MapPanelBinderTests + BalticReplayHarnessCommsTests |
| 11 | Engage denied in log after DENIED (no new launches) | ☑ (headless) | S31-08 | BalticReplayHarnessCommsTests.Comms_denied_appends_policy_denial_* |
| 12 | Unit detail FUEL line updates over long sim time | ☑ (headless) | S31-08 | FuelStateProjectionTests + FuelTimelineTrackerTests + FuelLedgerTests |
| 13 | Attack menu: select **Fire Single** → engage order in log (req 14/20) | ☑ (headless) | S31-08 | DelegationBridgeAttackOptionTests + UnitDetailBridgeTests + AttackMenuPanelBinderTests; PlayMode 7/7 @ 7401fac; headless proxy green @ `3406bc4` |
| 14 | Platform import staging review visible; approve gated until acknowledge; Phase F damage viewer columns; Phase G Comms staging diff; Phase H LinkCatalog staging diff (S29-04 / S30-06 / S31-07 / **S32-06 / S32-10 / S33-06 / S33-10 / S34-06 / S34-10**) | ☑ (headless + S32 damage + S33 comms + S34 link catalog) | S34-11 | `PlatformImportPanelTests` **10/10**; `PlatformCatalogViewerTests` **11/11**; evidence `production/qa/evidence/platform-import-staging-s32-baltic-diff.png` (MaxHp `DAMAGE row=…` diff); `production/qa/evidence/platform-import-staging-s33-comms-diff.png` (Comms `COMMS row=…` diff per S33-06); `production/qa/evidence/platform-import-staging-s34-link-diff.png` (LinkCatalog `LINK row=…` diff per S34-06); `production/qa/evidence/platform-catalog-damage-s32-viewer-columns.png` (list/detail damage columns); proxy: `Import_damage_MaxHp_round_trip_propose_acknowledge_approve_readback_baltic_fixture`, `Baltic_fixture_damage_row_surfaces_workbook_values_in_list_and_detail`, `PlatformComms_import_round_trip_propose_acknowledge_approve_readback_baltic_fixture`, `PlatformComms_staging_diff_surfaces_added_comms_row`, `PlatformLinkCatalog_import_round_trip_propose_acknowledge_approve_readback_baltic_fixture`, `PlatformLinkCatalog_staging_diff_surfaces_added_link_row` |
| 15 | Doctrine inheritance panel ROE override round-trip on friendly unit (S29-07 / S30-06 / S31-07) | ☑ (headless) | S34-11 | `DoctrineOverrideCommandTests` + `Doctrine*` filter **7/7** @ `d3db76d`; evidence `production/qa/evidence/doctrine-panel-s31-roe-override.png` (S31 fallback per S32-10/S33-10/S34-10); proxy: `Doctrine_override_round_trip_updates_policy_log_and_projection_bind`; re-confirmed headless @ S34-11 gates |
| 16 | Begin Execution top bar while `SimulationPhase.Planning`; score/loss frozen (S29-08 / S30-06 / S31-07) | ☑ (headless) | S34-11 | `C2TopBarBeginExecutionTests` **5/5** @ `d3db76d`; evidence `production/qa/evidence/begin-execution-s31-planning-topbar.png` (S31 fallback per S32-10/S33-10/S34-10); proxy: `BeginExecution_transitions_planning_to_executing_via_bridge`; re-confirmed headless @ S34-11 gates |
| 17 | Platform comms/datalink fittings visible (`LinkId`, `Role`, `SatcomCapable`) in catalog viewer; comms resolve link `DisplayName` when present in link catalog (S33-06 / S33-10 / **S34-06**) | ☑ (headless + S33 comms + S34 display-name) | S34-11 | `PlatformCommsTests` **12/12** @ `d3db76d`; evidence `production/qa/evidence/platform-catalog-comms-s33-viewer-columns.png` (comms list section); `production/qa/evidence/platform-import-staging-s33-comms-diff.png` (import staging Comms delta); proxy: `PlatformComms_baltic_fixture_comms_surfaces_workbook_values_in_list_projection`, `PlatformComms_viewer_host_binds_comms_list_on_platform_selection`, `PlatformLinkCatalog_comms_rows_resolve_link_display_name_when_present` |
| 18 | Platform link catalog visible (`LinkId`, `DisplayName`, `LinkType`, `LatencyMsNominal`) in catalog viewer + import round-trip (S34-06 / S34-10) | ☑ (headless + S34 link catalog) | S34-11 | `PlatformLinkCatalogTests` **13/13** @ `d3db76d`; evidence `production/qa/evidence/platform-catalog-link-s34-viewer-columns.png` (link catalog list section); `production/qa/evidence/platform-import-staging-s34-link-diff.png` (import staging LinkCatalog delta); proxy: `PlatformLinkCatalog_baltic_fixture_links_surface_workbook_values_in_list_projection`, `PlatformLinkCatalog_viewer_host_binds_global_link_list_on_refresh`, `PlatformLinkCatalog_import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` |

**Verdict:** ☑ **PASS WITH NOTES** (18/18) @ `d3db76d` — batch Play Mode check 1 (S19) + headless proxy checks 2–18 (S31-08 baseline; S32-11 upgrade checks 14–16; S33-11 adds check 17 + S33 Phase G comms evidence on check 14; S34-11 adds check 18 + S34 Phase H link catalog evidence on checks 14 + 17). Check 14 extended with S34-10 LinkCatalog staging diff (`platform-import-staging-s34-link-diff.png`) alongside S33 Comms + S32 damage evidence; check 17 refreshed with S34-06 comms display-name resolution; check 18 adds S34-06 Phase H link catalog viewer (`platform-catalog-link-s34-viewer-columns.png`). Checks 14–18 proxy filter `PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog` **58/58** (≥55/55; `PlatformImport` **10/10**, `PlatformCatalogViewer` **11/11**, `PlatformComms` **12/12**, `PlatformLinkCatalog` **13/13**, `Doctrine` **7/7**, `C2TopBar` **5/5**). Baseline checks 1–13 remain PASS @ `3406bc4` headless proxy **61/61** (`PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu`). Lean mode: no Unity Editor host on Linux agent; merge authority per ADR-010 / PI-006. Optional human visual walk (click feel, live Editor re-capture) non-blocking.

**Blockers:** None.

**Advisory (non-blocking):**

- Live Editor re-capture of `*-s32-*.png` (damage viewer + import MaxHp diff), `*-s33-*.png` (comms viewer + Comms staging diff), and `*-s34-*.png` (link catalog viewer + LinkCatalog staging diff) on Windows/macOS Unity host optional before Production → Polish gate.
- Live Editor re-capture of `*-s31-*.png` for doctrine/begin-execution optional (valid S31 fallbacks per S32-10).
- Run `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import` and `-Scenario begin-execution` on local Unity host; archive `unity-c2-playmode-signoff.log`.