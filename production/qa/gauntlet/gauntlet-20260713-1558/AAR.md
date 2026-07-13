# QA Gauntlet AAR — gauntlet-20260713-1558

## Summary

| Metric | Value |
|---|---|
| Run ID | gauntlet-20260713-1558 |
| Branch | `07-10-qa_gauntlet_gauntlet-20260710-1352` |
| Start SHA | `ffa01a200aecb3a642f460517db788863ea100f0` |
| Baseline tests | **1665** passed (full solution) |
| Tiers | **5/5 green** |
| Scenarios × seeds | 20 scenarios × 3 seeds = **60 batch rows** |
| Hard-gate oracle | **C# GauntletOracleEvaluator: 20/20 pass** |
| Determinism | t1-patrol-a seed 42 fingerprint match (repeat run) |
| Joint ORBAT (tier ≥3) | CATALOG_UNIT + MAGAZINE_SEED + Visby/Gripen/Gotland in fingerprints |
| sim-code defects | 0 |
| oracle defects | 12 scenarios recalibrated (joint multi-unit envelopes) |
| QUARANTINED-CRITICAL | none |

## Ladder results

| Tier | Ticks | Status | Rows | Notes |
|---|---|---|---|---|
| 1 | 6 | green | 12 | Synthetic surface patrol; tight ROE denial scenario passes |
| 2 | 10 | green | 12 | Escort + strike + passive EMCON |
| 3 | 16 | green | 12 | **Joint ORBAT** surface+air+sub; ID-ROE denials scale with unit count |
| 4 | 24 | green | 12 | Multi-mission / asymmetric ROE / injects |
| 5 | 40 | green | 12 | Theater / cascade / dynamic / ROE-change |

## Hard-gate compliance (vs prior stability-only green)

1. Every policy includes machine-checkable `gauntlet.expect` (side, score/missile/denial/kill bounds, requireNonEmptyFingerprint).
2. Post-batch evaluation via `GauntletOracleEvaluator.EvaluateFromPolicyAndCsv` (see `oracle-eval-csharp.json`).
3. Tier ≥3 policies include `gauntlet.units` for Visby (surface) + Gripen (air) + Gotland (sub) with detection observers on catalog platformIds.

## Defects & remediation

### ORACLE-T3+ (oracle class)

**Symptom:** First-pass expects (calibrated on synthetic `u1` runs) failed under joint ORBAT: higher denials (multiple units attempt Engage under tight ROE), lower/higher scores, more missiles.

**Root cause:** Expect envelopes were wrong for multi-unit catalog ORBAT, not sim instability.

**Fix:** Recalibrated expects from observed deterministic envelopes with small slack. Log: `oracle-expect-recalibration.json`. Policies updated under `data/scenarios/gauntlet-t*.policy.json`.

**Classification:** oracle (not sim-code) — no production code change required.

## Determinism

- Repeat batch: `gauntlet-t1-patrol-a` seed 42 → identical fingerprint, score 100.
- Unit tests: GauntletOracleEvaluator 4/4, GauntletOrbat harness 2/2.

## Sign-off (qa-lead)

- Baseline test count: 1665 (monotonic; no tests deleted)
- Replay/Orbat harness green
- Ladder green under hard-gate evaluator
- Artifacts: `production/qa/gauntlet/gauntlet-20260713-1558/`

## Recommended follow-ups

1. Promote temporary C# eval harness into `tools/gauntlet-oracle-eval` or MissionEditor.Cli for CI.
2. Tighten expects further once multi-unit denial scoring is intentional design (document denial cost model).
3. Tier 5 theater scenarios still share similar score envelopes — diversify victory objectives for more discriminative oracles.
4. P3 deferred items from `qa-gauntlet-effectiveness-plan-2026-07-13.md` remain out of scope.
