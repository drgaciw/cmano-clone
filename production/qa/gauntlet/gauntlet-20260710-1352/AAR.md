# After Action Report — QA Gauntlet `gauntlet-20260710-1352`

## Executive summary

| Item | Result |
|---|---|
| Run ID | `gauntlet-20260710-1352` |
| Branch | `07-10-qa_gauntlet_gauntlet-20260710-1352` |
| Preflight suite | **1627 passed / 0 failed** |
| Final suite | **1627 passed / 0 failed** |
| Replay golden | **6/6 PASS** (within UnityAdapter.Tests) |
| Catalog | `baltic_patrol.db` openable; **79** platforms (surface 35 / air 24 / subsurface 20) |
| Tiers | **5/5 green** |
| Scenarios × seeds | 20 × 3 = **60 batch rows** |
| Defects found | **0** |
| Defects sim-code | **0** |
| QUARANTINED-CRITICAL | **none** |

## Preflight

| Gate | Result |
|---|---|
| `dotnet test ProjectAegis.sln` | GREEN — 1627 (Sim 311 + Del 260 + UA 286 + Excel 24 + Cli 103 + Data 643) |
| ReplayGoldenSuiteTests | 6/6 (UA suite green) |
| Catalog `baltic_patrol.db` | open; 79 platforms; multi-domain |
| Smoke | tier-1 batch 12/12 rows, non-empty fingerprints |
| Branch | `gt create` → `07-10-qa_gauntlet_gauntlet-20260710-1352` @ `108c829` baseline SHA |

Scratch captures: `/tmp/grok-goal-94ab35ee47a0/implementer/gauntlet-preflight.log`, `catalog-gate.txt`.

## Complexity ladder results

| Tier | Ticks | Scenarios | Rows | Stability | Catalog oracle-0 | Score range | Notes |
|---|---|---|---|---|---|---|---|
| 1 | 6 | 4 patrol | 12 | PASS | PASS | −30…100 | Surface roster + harness seeds |
| 2 | 10 | 4 strike/escort | 12 | PASS | PASS | −50…100 | Surface + air pools |
| 3 | 16 | 4 multi | 12 | PASS | PASS | −80…100 | Surface + air + sub pools |
| 4 | 24 | 4 multi-mission | 12 | PASS | PASS | 0…100 | Joint domains in roster metadata |
| 5 | 40 | 4 theater | 12 | PASS | PASS | −200…200 | Full joint ORBAT metadata |

**Harness note:** `BalticReplayHarness` engage units remain `u1` / `hostile-1` (positional Baltic). Higher-tier **catalog multi-domain** IDs appear in each `tier-N/roster.json` and `gauntlet.catalogRefs` for oracle-0 and future ORBAT wiring; ROE/EMCON/jam/tick axes escalate via policy JSON.

## Determinism

- Repeat batch `gauntlet-t1-patrol-a` seed **42**: **identical fingerprint** (PASS).
- Evidence: `{SCRATCH}/determinism-check.log`, `determinism-repeat.csv`.
- Per-tier CSVs: one row per (scenario, seed); fingerprints non-empty.

## Defects

| ID | Class | Status | Notes |
|---|---|---|---|
| — | — | none | No remediation required |
| QUARANTINED-CRITICAL | — | **none** | — |

## Score / balance trends

- Weapons-free patrol/strike scenarios typically score BLUE 100 with kill on `hostile-1` when Pd and magazine allow.
- Weapons-tight / low-Pd / jam lanes produce denials and negative or zero scores without crashes.
- Tier 5 longer tick budget (40) and weighted objectives produce wider score range (−200…200) without instability.

## Flaky tests

None observed (suite 1627/1627 both preflight and final).

## Sign-off (qa-lead)

| Metric | Preflight baseline | Final |
|---|---|---|
| Test count | 1627 | **1627** (monotonic) |
| Failures | 0 | 0 |
| Replay golden | 6/6 | 6/6 |
| Ladder | n/a | **5/5 green** |

**Verdict:** Gauntlet ladder **PASS**. Zero defects. No CRITICAL quarantines. No code fixes this run (data/artifacts only).

## Recommended follow-ups

1. Wire multi-domain catalog platform IDs into `BalticReplayHarness` unit registration so tiers 4–5 engage true joint ORBAT, not seed stand-ins.
2. Invoke `GauntletRosterValidator` in CI over `production/qa/gauntlet/**`.
3. `gt submit --stack --no-interactive` when ready for human PR review (no sim-code fix commits this run; optional for artifact branch).

## Artifacts

- Manifest: `production/qa/gauntlet/gauntlet-20260710-1352/manifest.yaml`
- Per-tier: `roster.json`, `*.policy.json`, `results.csv`, `run.log`
- `oracle-eval.json`, `fixes.md`, this AAR
- Scratch: preflight / final test logs, determinism check, AAR path file
