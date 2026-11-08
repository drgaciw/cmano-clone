# Gauntlet Phase 3 + multi-domain shooters + hindsight + CI

**Date:** 2026-07-13  
**Commit scope:** multi-domain air/sub shooters, theater inject/dynamic victory, defect re-test loop, PR CI `gauntlet_oracle_eval`.

## 1. Air/sub as shooters

| Piece | Detail |
|---|---|
| Policy | `gauntlet-multidomain-shooters` |
| Blue | Visby (surface), Gripen (air), Gotland (sub) |
| Red (paired) | Sovremenny, Steregushchiy, Buyan |
| Harness | All blue gauntlet units get engage agents; `PreferredHostileByShooter` from detection trials so SwarmSalvoDeconfliction allows concurrent launches (one shooter per distinct red) |
| Test | `BalticReplayHarnessMultiDomainShooterTests` — asserts `True\|Launched` for air + sub with opposite-side victims |

## 2. Matrix / injects / dynamic victory

| Policy | Mechanism |
|---|---|
| `gauntlet-theater-inject` | **Real mid-run inject:** `comms[]` Degraded@tick3 (`inject_jamming`) + Denied@tick6; mission contact trigger → `PolicyUpdate` ROE via `ApplyRoeToUnits`; EventFired labels accompany state delta |
| `gauntlet-theater-dynamic-victory` | Dual blues vs dual reds; event `dynamic_victory_require_dual_kill`; `minKills: 2` |

Tests assert `DecisionLog.CommsStateChanges` (Nominal→Degraded) and `PolicyUpdate` field=`roe` — not string markers alone.

Oracle: `gauntlet_oracle_eval` allPassed=true on seeds 42,7,123 (see scratch `theater-oracle-eval.json`).

## 3. Hindsight defect re-test

| Artifact | Path |
|---|---|
| Registry | `production/qa/gauntlet-defect-registry.json` |
| Runner | `tools/qa-gauntlet/retest-defect.sh <defect-id>` |

Closed defects `GAUNTLET-MD-001`, `GAUNTLET-SYN-T12-001`. Re-test re-batches scenario×seed, runs oracle when policy path set, fails if `priorFailureMode` string still appears in CSV.

## 4. CI wire on PR

`.github/workflows/gauntlet-oracle.yml`:

1. Build solution  
2. Demo `--batch` for fixture policy set  
3. `gauntlet_oracle_eval` (must exit 0 / `allPassed`)  
4. Broken-expect fixture must exit non-zero  

**Note:** Org GHA may still be billing-gated; workflow is the product gate when Actions run. Local dry-run: `{SCRATCH}/ci-oracle-dry-run.log`.

## Verification summary

- Multi-domain shooter tests green  
- Theater tests green  
- Theater batch + oracle allPassed  
- Hindsight retest PASS  
- CI dry-run pass + fail-closed  
- Targeted suite / ReplayGolden green (see scratch logs)
