# After Action Report — QA Gauntlet `gauntlet-20260709-1744`

## Executive summary

| Item | Result |
|---|---|
| Run ID | `gauntlet-20260709-1744` |
| Branch | `07-09-qa_gauntlet_gauntlet-20260709-1744` |
| Fix commit | `d417301` |
| Final suite | **1609 passed / 0 failed** (baseline after fix) |
| Replay golden | **6/6 PASS** |
| Smoke | **PASS** (phase0 quick) |
| Tiers | **5/5 green** |
| Scenarios × seeds | 20 scenarios × 3 seeds = **60 batch rows** |
| Defects found | **1 cluster (preflight)** fixed via TDD |
| Defects quarantined CRITICAL | **none** |
| Graphite submit | not run (user merge decision); branch ready |

## Preflight

| Gate | Result |
|---|---|
| GitNexus reindex | 24,703 nodes / 47,459 edges |
| Initial `dotnet test` | **RED** (32 failures across Data / UnityAdapter / Cli) |
| TDD remediation | Commit `d417301` (see `fixes.md`) |
| Post-fix suite | **GREEN** 1609 |
| ReplayGoldenSuiteTests | 6/6 |
| smoke-scenario-editor-phase0 --quick | PASS |
| Catalog `baltic_patrol.db` | openable; engage rows present; gun mag qty=1 |

## Complexity ladder results

| Tier | Ticks | Scenarios | Rows | Stability | Notes |
|---|---|---|---|---|---|
| 1 | 6 | 4 patrol | 12 | PASS | Survive/patrol; scores 100 typical |
| 2 | 10 | 4 strike/escort | 12 | PASS | ROE tight/free + EMCON passive |
| 3 | 16 | 4 multi-sensor | 12 | PASS | Dual contacts; salvo 2 |
| 4 | 24 | 4 multi-mission | 12 | PASS | Jam/env mask; asymmetric ROE |
| 5 | 40 | 4 theater stand-in | 12 | PASS | NO_AMMO observed after mag budget; multi-contact |

**Catalog constraint:** live DB only exposes 3 surface platforms (`u1`, `hostile-1`, `hostile-far`), 2 sensors, 2 weapons. Higher-tier platform mix (air/sub/UAV/swarm) is **stand-in** via multi-sensor, jam, and longer ticks — not true multi-domain ORBAT. Flagged as follow-up.

## Determinism

- Repeat single-run `gauntlet-t1-patrol-a` seed 42: **identical fingerprint** (PASS).
- Batch fingerprints stable per (scenario, seed) within each tier CSV.
- Golden replay suite 6/6 still green after fixes.

## Defects

| ID | Class | Status | Commit / reason |
|---|---|---|---|
| FIX-001 | sim-code + catalog seed | **fixed** | `d417301` |
| QUARANTINED-CRITICAL | — | **none** | — |

## Score / balance trends

- Tier 1–5 batch scores typically 100 for BLUE with 1 kill on hostile-1 when weapons free.
- Magazines deplete under policy cap (tier 5 shows `NO_AMMO` after salvo expenditure) — validates FIX-001 in-ladder.
- Tight ROE / EMCON-off scenarios still complete without crashes; denials surface via abort codes in fingerprints where applicable.

## Flaky tests

None observed this run (suite 0 failures; determinism re-run matched).

## Sign-off (qa-lead)

| Metric | Baseline (post-fix) | Final |
|---|---|---|
| Test count | 1609 | 1609 (≥ baseline) |
| Failures | 0 | 0 |
| Replay golden | 6/6 | 6/6 |
| Regression anchors | N/A (first green ladder) | all tiers green |

**Verdict:** Gauntlet ladder **PASS** for available catalog surface. Preflight defects remediated under TDD. No CRITICAL quarantines.

## Recommended follow-ups

1. Expand catalog ORBAT (air/sub/UAV) so tiers 4–5 can exercise true multi-domain platform mix instead of stand-ins.
2. Wire scripted event DSL / mid-mission ROE change into policy JSON (today approximated via jam/EMCON/ROE static fields).
3. Consider per-mount magazine seeding (ledger currently seeds totals onto engage mount).
4. `gt submit --stack --no-interactive` when ready for PR review (do not merge from automation).

## Artifacts

- Manifest: `production/qa/gauntlet/gauntlet-20260709-1744/manifest.yaml`
- Per-tier: `roster.json`, `*.policy.json`, `results.csv`, `run.log`
- Fixes: `fixes.md`
- Oracle eval: `oracle-eval.json`
- Policies also installed under `data/scenarios/gauntlet-*.policy.json` for harness discovery
