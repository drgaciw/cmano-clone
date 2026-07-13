# Gauntlet Slice 1 (ladder injects) + Slice 2 (multi-domain t3+) — 2026-07-13

## Goal

Close dual-slice residual for Project Aegis QA gauntlet:

1. **Slice 1 — ladder injects:** real mid-run state deltas on `gauntlet-t4-random-inject`, `gauntlet-t5-cascade`, `gauntlet-t5-roe-change` via policy `comms[]` → `CommsStateChange` (and ROE path where applicable).
2. **Slice 2 — multi-domain default (sample bar):** `gauntlet-joint-orbat-smoke` + `gauntlet-t3-emcon-phases` concurrent air+sub `True|Launched` against distinct catalog reds (Buyan triple pairing).

## What shipped

### Slice 1 policies

| Policy | Mid-run inject |
|--------|----------------|
| `gauntlet-t4-random-inject` | `comms[]` Degraded@2 (`seeded_inject_jamming`) + Nominal@5 |
| `gauntlet-t5-cascade` | Degraded@2 → Denied@4 → Degraded@8 |
| `gauntlet-t5-roe-change` | WeaponsTight start + contact → WeaponsFree + Degraded@3 |

Batch fingerprints include `CommsStateChange|…|Degraded|…` (not EventFired-only). Unit tests: `BalticReplayHarnessLadderInjectTests`.

### Slice 2 policies

| Policy | Pairing |
|--------|---------|
| `gauntlet-joint-orbat-smoke` | Visby→Sovremenny, Gripen→Steregushchiy, Gotland→Buyan |
| `gauntlet-t3-emcon-phases` | same |

Harness multi-agent blues + `PreferredHostileByShooter` enable concurrent domain launches under `SwarmSalvoDeconfliction`. Tests: `BalticReplayHarnessLadderMultiDomainTests`, `BalticReplayHarnessMultiDomainShooterTests`.

### Oracle expects (numeric + fingerprint fail-closed)

Recalibrated from Demo `--batch` seeds **42,7,123** ticks **10** for the five touched policies. Audit: implementer scratch `oracle-expect-recalibration.json`.

**Slice fingerprint gates** (acceptance criterion 3) on `gauntlet.expect`:

| Policy | Gate |
|--------|------|
| t4-random-inject / t5-cascade / t5-roe-change | `requireFingerprintSubstrings`: `CommsStateChange` + inject tokens |
| joint-orbat-smoke / t3-emcon-phases | `requireTrueLaunchedShooters`: Gripen + Gotland |

Evaluator: `GauntletOracleEvaluator` + unit tests in `GauntletOracleEvaluatorTests` (strip → fail).

**Harness negatives:** `BalticReplayHarnessLadderNegativeGateTests` — strip `comms[]` → no mid-run CommsStateChange; collapse multi-domain pairing → air+sub distinct launch bar fails.

### CI

`.github/workflows/gauntlet-oracle.yml` now stages:

- prior: multidomain-shooters, theater-inject, theater-dynamic-victory, t1-patrol-b
- **new:** t4-random-inject, t5-cascade, t5-roe-change, joint-orbat-smoke, t3-emcon-phases

Local dry-run: pass path `allPassed=true`; **fail-closed** strips inject / multi-domain fingerprint tokens from CSV while keeping policy expects → `allPassed=false` exit ≠ 0.

## Verification evidence (scratch)

Root: `/tmp/grok-goal-31ba54177760/implementer/` (goal `{SCRATCH}`)

| Artifact | Result |
|----------|--------|
| `ladder-inject-tests.log` | 6 passed (inject + multi-domain filter run) |
| `ladder-multidomain-tests.log` | 3 passed |
| `ladder-batch-results.csv` | 15 rows (5 scenarios × 3 seeds) |
| `ladder-oracle-eval.json` | `allPassed: true` |
| `ci-ladder-dry-run.log` | pass + fail-closed OK |
| `suite-summary.txt` | targeted suite green |

## Commands

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "FullyQualifiedName~LadderInject|FullyQualifiedName~LadderMultiDomain" -c Release
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t4-random-inject,gauntlet-t5-cascade,gauntlet-t5-roe-change,gauntlet-joint-orbat-smoke,gauntlet-t3-emcon-phases \
  --seeds 42,7,123 --ticks 10 --csv-out results.csv
dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy-dir policies --csv results.csv --out oracle-eval.json
```

## Out of scope / follow-ups

- Propagating Buyan triple to **every** remaining t3–t5 ladder policy (goal bar was joint + one t3; optional next wave).
- Strengthening inject tests to theater-level reason-string asserts.
- Removing hollow Free→Free contact triggers on t4/cascade (comms path already proves injects).
- Full 5-tier unattended `/qa-gauntlet` matrix in GHA.
- Org Actions billing may block remote GHA; local dry-run is the gating proof when Actions cannot run.
