# After Action Report — QA Gauntlet `gauntlet-20260709-1903`

## Executive summary

| Item | Result |
|---|---|
| Run ID | `gauntlet-20260709-1903` |
| Branch | `07-09-qa_gauntlet_gauntlet-20260709-1903` |
| Preflight suite | **1614 passed / 0 failed** |
| Final suite | **1617 passed / 0 failed**
| Replay golden | **6/6 PASS** |
| Catalog | `baltic_patrol.db` openable; **43** platforms (multi-domain) |
| Tiers | **5/5 green** after oracle-0 roster fix |
| Scenarios × seeds | 20 × 3 = **60 batch rows** |
| Defects found | **1** (`scenario-data` FIX-SD-001) |
| Defects sim-code | **0** |
| QUARANTINED-CRITICAL | **none** |
| Parallel agents | Used for independent inventory/math validation earlier session; ladder defects were single scenario-data domain → sequential fix |

## Preflight

| Gate | Result |
|---|---|
| `dotnet test ProjectAegis.sln` | GREEN — 1614 (Sim 311 + Del 260 + UA 286 + Excel 24 + Cli 103 + Data 630) |
| ReplayGoldenSuiteTests | 6/6 |
| Catalog `baltic_patrol.db` | open; 43 platforms |
| Branch | `gt create` → `07-09-qa_gauntlet_gauntlet-20260709-1903` @ `e6ddf5b` baseline SHA |

## Complexity ladder results

| Tier | Ticks | Scenarios | Rows | Stability | Catalog oracle-0 | Notes |
|---|---|---|---|---|---|---|
| 1 | 6 | 4 patrol | 12 | PASS | PASS | Surface seeds + expanded surface |
| 2 | 10 | 4 strike/escort | 12 | PASS | PASS | Air pool in roster metadata |
| 3 | 16 | 4 multi | 12 | PASS | PASS | Air + sub pools in roster |
| 4 | 24 | 4 multi-mission | 12 | PASS | PASS after fix | Joint domains; seed merge |
| 5 | 40 | 4 theater | 12 | PASS | PASS after fix | Full joint ORBAT metadata |

**Harness note:** `BalticReplayHarness` still registers engage units as `u1` / `hostile-1` (positional Baltic). Higher-tier **catalog multi-domain** IDs appear in `roster.json` + `gauntlet.catalogRefs` for oracle-0 and future ORBAT wiring; engage path remains seed-based with escalating ROE/EMCON/jam/tick axes.

## Determinism

- Repeat batch `gauntlet-t1-patrol-a` seed 42: **identical fingerprint** (PASS).
- Per-tier CSVs: one row per (scenario, seed); fingerprints non-empty.

## Defects

| ID | Class | Status | Notes |
|---|---|---|---|
| FIX-SD-001 | scenario-data | **fixed** | Tier 4–5 rosters missing harness seeds; validator + tests |
| QUARANTINED-CRITICAL | — | **none** | — |

## Score / balance trends

- Typical BLUE score 100 with kill on `hostile-1` when weapons free / sufficient Pd.
- Mag budget + jam lanes exercised on higher tiers; no crashes.
- Tight ROE / low-Pd EMCON stand-ins complete without exception.

## Flaky tests

None observed.

## Sign-off (qa-lead)

| Metric | Preflight baseline | Final |
|---|---|---|
| Test count | 1614 | **1617** (monotonic) |
| Failures | 0 | 0 |
| Replay golden | 6/6 | 6/6 |
| Ladder | n/a | 5/5 green |

**Verdict:** Gauntlet ladder **PASS**. One scenario-data defect remediated with failing-first invariant tests + minimal roster fix + shipped validator. No CRITICAL quarantines.

## Recommended follow-ups

1. Wire multi-domain catalog platform IDs into `BalticReplayHarness` unit registration so tiers 4–5 engage true joint ORBAT, not seed stand-ins.
2. Invoke `GauntletRosterValidator` in CI over `production/qa/gauntlet/**` or generation path.
3. `gt submit --stack --no-interactive` when ready for human PR review (do not auto-merge).

## Artifacts

- Manifest: `production/qa/gauntlet/gauntlet-20260709-1903/manifest.yaml`
- Per-tier: `roster.json`, `*.policy.json`, `results.csv`, `run.log`
- `oracle-eval.json`, `fixes.md`, this AAR
- Code: `src/ProjectAegis.Data/Catalog/GauntletRosterValidator.cs` + tests
