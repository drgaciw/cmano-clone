# Smoke check — Sprints 7–10 (scoring CSV, comms/fuel, batch replay, fuel ledger)

**Date:** 2026-06-08  
**Build:** `main` @ `4050abe302401721bf41849198c9a589c09d3f9f`  
**Mode:** sprint closeout (automated headless gate)  
**Verdict:** **PASS**

## Commands (Sprints 7–10 scoped filters)

| Step | Command | Result |
|------|---------|--------|
| Fuel ledger + timeline + harness | `dotnet test ProjectAegis.sln --filter "FullyQualifiedName~FuelLedger\|FullyQualifiedName~FuelTimeline\|FullyQualifiedName~BalticReplayHarnessFuel"` | **14/14 PASS** |
| Comms state + projection + harness + CLI | `dotnet test ProjectAegis.sln --filter "FullyQualifiedName~Comms\|FullyQualifiedName~BalticReplayHarnessComms"` | **15/15 PASS** |
| LossesScoring + batch + golden comms | `dotnet test ProjectAegis.sln --filter "FullyQualifiedName~LossesScoring\|FullyQualifiedName~Batch\|FullyQualifiedName~ReplayGoldenBalticComms"` | **6/6 PASS** |
| Full solution | `dotnet test ProjectAegis.sln -v minimal` | **443/443 PASS** |
| PlayMode smoke | `dotnet test ...UnityAdapter.Tests --filter PlayModeSmokeHarnessTests` | **8/8 PASS** |

## QA gate resolution

| Former open item | Resolution |
|------------------|------------|
| Unity manual C2 sign-off (12 checks) | **PASS 13/13** — `production/qa/c2-manual-signoff-2026-06-02.md` @ 2026-06-08 batch PlayMode |
| Cesium Phase B spike | **PASS** — S20/S21 `CesiumGlobeBridge` + manifest pin; checklist at `docs/engineering/cesium-phase-b-spike-checklist.md` |
| Sprint 7 `code-complete` | Promoted to **complete** — all headless + deferred Editor items closed |

## Evidence cross-links

- `production/agentic/sprints-7-10-closeout-2026-06-08.md`
- `production/qa/c2-automated-proxy-2026-06-02.md`
- `production/qa/pi-006-headless-proxy-2026-06-04.md`
- `tests/regression/replay-golden-baltic-comms-2026-06-02.txt`
- `tools/batch-replay/README.md`