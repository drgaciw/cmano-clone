# C2 automated QA proxy — 2026-06-02

**Purpose:** Headless substitutes for Unity manual checks where presentation logic lives in `ProjectAegis.Delegation` (milsim QA gate).

**Build:** `main` @ `5546c5d` (PI plan completion)  
**Headless evidence:** `production/qa/pi-006-headless-proxy-2026-06-04.md`  
**Manual sign-off still required:** `production/qa/c2-manual-signoff-2026-06-02.md` (Editor Play Mode)

## Automated coverage map

| Manual # | Check | Headless proxy test |
|----------|--------|---------------------|
| 1 | No console errors | N/A (Editor only) |
| 2–6 | Selection / OOB / map | `C2SelectionFlowTests`, `PlayModeSmokeHarnessTests.Baltic_classify_map_symbols_*` |
| 7 | Score on engage | `LossesScoringCsvExporterTests`, `Baltic_patrol_scoring_csv_row_is_deterministic` |
| 8 | Message log categories | `BalticReplayHarnessCommsTests`, comms harness messages |
| 9 | COMMS DEGRADED → DENIED | `Baltic_patrol_comms_harness_matches_manual_qa_preconditions` |
| 10 | Hostile dim + ghost | `MapPanelBinderTests.Bind_degraded_comms_*`, `BalticReplayHarnessCommsTests.Comms_scenario_policy_exposes_*` |
| 11 | Engage denied | `BalticReplayHarnessCommsTests.Comms_denied_appends_policy_denial_*` |
| 12 | FUEL line | `FuelStateProjectionTests`, `FuelTimelineTrackerTests`, `FuelLedgerTests`, `baltic-patrol-comms.policy.json` |

## Run gate

```powershell
./tools/unity/Invoke-ManualQaHeadlessGate.ps1
```

## Verdict

- **Headless proxy:** PASS when all filtered tests green (**283** solution total as of 2026-06-04).
- **Unity manual:** PENDING until Editor checklist signed.
- **PI-006 (agentic):** CLOSED via headless proxy; Editor checklist open.