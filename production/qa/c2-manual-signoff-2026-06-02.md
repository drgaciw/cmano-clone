# C2 manual QA sign-off — 2026-06-02

**Sprint / task:** Sprint 19 / **S19-01** (carryover from S18-01)  
**Build:** `main` @ `7401fac`  
**Headless/proxy evidence:** `production/qa/smoke-2026-06-08.md` + `c2-automated-proxy-2026-06-02.md` (412 tests, 7/7 PlayMode harness, 17/17 replay)  
**Batch Play Mode (check 1):** `tools/unity/Invoke-C2PlayModeSignoffBatch.ps1` — comms + classify **PASS** @ 2026-06-08 (`unity-c2-playmode-signoff.log`)  
**Playtest notes:** `production/session-logs/playtest-sprint19-c2-signoff.md`  
**Prior baseline:** `eeed8e1` (Sprint 18 closeout)  
**Scenarios:** `baltic-patrol-classify` (selection), `baltic-patrol-comms` (COMMS + map stale)  
**Reference:** `unity/ProjectAegis/PLAYMODE-SMOKE.md`  
**Automated proxy:** `production/qa/c2-automated-proxy-2026-06-02.md` (checks 2–13; PI-006: `production/qa/pi-006-headless-proxy-2026-06-04.md`)

| # | Check | Pass | Tester | Notes |
|---|--------|------|--------|-------|
| 1 | Play starts without console errors | ☑ | batch-runner-2026-06-08 | `Invoke-C2PlayModeSignoffBatch.ps1` comms + classify; Unity 6000.3.14f1 batchmode; no game errors |
| 2 | Default unit selected (OOB + map ring) | ☑ (headless) | - | C2SelectionFlowTests + PlayModeSmokeHarnessTests.Baltic_classify_map_symbols_* |
| 3 | Map ■ click updates unit detail | ☑ (proxy) | - | UnitDetailBridgeTests + selection projection; visual click feel optional |
| 4 | OOB row click syncs map | ☑ (proxy) | - | OobTreeBridgeTests + harness |
| 5 | Hostile ◆ shows CONTACT line | ☑ (headless) | - | C2SelectionFlowTests + contact picture tests |
| 6 | CONTACTS tab click matches map | ☑ (proxy) | - | Harness + selection tests |
| 7 | Top bar score ticks on engage | ☑ (headless) | - | LossesScoringCsvExporterTests + Baltic_patrol_scoring_csv_row_is_deterministic |
| 8 | Message log shows CONTACT/MISSION lines | ☑ (headless) | - | BalticReplayHarnessCommsTests + comms harness messages |
| 9 | `baltic-patrol-comms`: COMMS bar DEGRADED → DENIED | ☑ (headless) | - | Baltic_patrol_comms_harness_matches_manual_qa_preconditions |
| 10 | Hostile ◆ dimmed (degraded), all symbols dimmer (denied) | ☑ (headless) | - | MapPanelBinderTests + BalticReplayHarnessCommsTests |
| 11 | Engage denied in log after DENIED (no new launches) | ☑ (headless) | - | BalticReplayHarnessCommsTests.Comms_denied_appends_policy_denial_* |
| 12 | Unit detail FUEL line updates over long sim time | ☑ (headless) | - | FuelStateProjectionTests + FuelTimelineTrackerTests + FuelLedgerTests |
| 13 | Attack menu: select **Fire Single** → engage order in log (req 14/20) | ☑ (headless) | batch-runner-2026-06-08 | DelegationBridgeAttackOptionTests + UnitDetailBridgeTests + AttackMenuPanelBinderTests; PlayMode 7/7 @ 7401fac |

**Verdict:** ☑ **PASS** (13/13) @ `7401fac` — batch Play Mode check 1 + headless proxy checks 2–13. Optional human visual walk (click feel) non-blocking per PI-006.

**Blockers:** None.