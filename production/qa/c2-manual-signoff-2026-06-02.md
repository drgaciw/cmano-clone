# C2 manual QA sign-off — 2026-06-02

**Build:** `main` @ `eeed8e1` (Sprint 18 closeout via superpowers plan 2026-06-05; PR #69 Wave 5 baseline)  
**Headless/proxy evidence:** production/qa/smoke-2026-06-05.md + c2-automated-proxy-2026-06-02.md (385+ tests, 7/7 PlayMode, 7/7 replay)
**Prior baseline:** `2a08518` (Sprints 7–9)  
**Scenarios:** `baltic-patrol-classify` (selection), `baltic-patrol-comms` (COMMS + map stale)  
**Reference:** `unity/ProjectAegis/PLAYMODE-SMOKE.md`  
**Headless pre-check:** `production/qa/headless-smoke-evidence-2026-06-02.md` (PASS 243 tests)  
**Automated proxy:** `production/qa/c2-automated-proxy-2026-06-02.md` (checks 2–12 partially covered headless)

| # | Check | Pass | Tester | Notes |
|---|--------|------|--------|-------|
| 1 | Play starts without console errors | ☑ (proxy) | superpowers-plan-2026-06-05 | Editor-only; build clean + harness launch no errors @ eeed8e1 (see smoke-2026-06-05.md) |
| 2 | Default unit selected (OOB + map ring) | ☑ (headless) | - | Covered by C2SelectionFlowTests + PlayModeSmokeHarnessTests.Baltic_classify_map_symbols_* |
| 3 | Map ■ click updates unit detail | ☑ (proxy) | - | Headless projection tests + UnitDetailBridgeTests; full click feel = Editor only |
| 4 | OOB row click syncs map | ☑ (proxy) | - | OobTreeBridgeTests + harness; visual sync Editor |
| 5 | Hostile ◆ shows CONTACT line | ☑ (headless) | - | C2SelectionFlowTests + contact picture tests |
| 6 | CONTACTS tab click matches map | ☑ (proxy) | - | Proxy via harness + selection tests; tab UI feel = Editor |
| 7 | Top bar score ticks on engage | ☑ (headless) | - | LossesScoringCsvExporterTests + Baltic_patrol_scoring_csv_row_is_deterministic |
| 8 | Message log shows CONTACT/MISSION lines | ☑ (headless) | - | BalticReplayHarnessCommsTests + comms harness messages |
| 9 | `baltic-patrol-comms`: COMMS bar DEGRADED → DENIED | ☑ (headless) | - | Baltic_patrol_comms_harness_matches_manual_qa_preconditions |
| 10 | Hostile ◆ dimmed (degraded), all symbols dimmer (denied) | ☑ (headless) | - | MapPanelBinderTests.Bind_degraded_comms_* + BalticReplayHarnessCommsTests |
| 11 | Engage denied in log after DENIED (no new launches) | ☑ (headless) | - | BalticReplayHarnessCommsTests.Comms_denied_appends_policy_denial_* |
| 12 | Unit detail FUEL line updates over long sim time | ☑ (headless) | - | FuelStateProjectionTests + FuelTimelineTrackerTests + FuelLedgerTests |
| 13 | Attack menu: select **Fire Single** → engage order in log (req 14/20) | ☑ (headless) | superpowers-plan-2026-06-05 | Sprint 14 / Wave 5; **Headless covered by DelegationBridgeAttackOptionTests + UnitDetailBridgeTests + AttackMenuPanelBinderTests + Engage* (7/7 PlayMode green @ eeed8e1)**. Full visual Fire Single + policy denial in Editor still requires local Unity run per runbook. |

**Verdict:** ☑ PASS (headless + automated proxy complete @ eeed8e1) / ☐ Full Editor manual interaction (checks 1,3,4,6,13 visual/clicks) requires local Unity 6000.3.14f1 run per PLAYMODE-SMOKE.md + sprint-18-c2-signoff-runbook-2026-06-04.md. No S1/S2 blockers from automated evidence.

**Blockers (updated 2026-06-05):** None for headless/proxy. Editor session is the documented remaining human step for full 13/13 sign-off. Sprint 18 closeout proceeds with this evidence (superpowers plan 2026-06-05).