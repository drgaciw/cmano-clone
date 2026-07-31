# QA Gauntlet AAR — gauntlet-20260731-0855

**Commit:** `6142a44` (main post-#368)  
**Driver:** `tools/qa-gauntlet/run-gauntlet.sh --tiers "1 2 3 4 5 extra" --seeds 42,7,123 --roving 2`  
**Exit:** **0** (all tiers green, run verdict pass)

## Ladder results

| Tier | Ticks | Anchor rows | Verdict |
|------|-------|-------------|---------|
| 1 | 6 | 12 | pass |
| 2 | 10 | 12 | pass |
| 3 | 16 | 12 | pass |
| 4 | 24 | 12 | pass |
| 5 | 40 | 12 | pass |
| extra | 12 | 6 | pass |
| **run** | — | — | **pass** |

## Soft signals (non-blocking)

| Signal | Detail |
|--------|--------|
| `token_coverage` warn | `EMCON_OFF` absent (BUG-gauntlet-emcon-dimension-not-exercised); many abort codes 0× including `WRA_SALVO` (aligns with blind spot 03) |
| `gauntlet.emcon` warn | Legacy stand-in on t2-escort-passive, t3-emcon-phases, t5-roe-change |
| `roving_observe` warn | Envelope excursions on non-anchor seeds (expected; anchor-calibrated bounds) |

## Defect class counts

| Class | Count |
|-------|-------|
| sim-code fixed | 0 |
| oracle recalibrated | 0 |
| scenario-data | 0 |
| flaky | 0 |
| quarantined | 0 |

## Calibration

Last oracle calibration: **2026-07-31 live**, kill rate **5/6**  
(`production/qa/gauntlet/calibration-2026-07-31-live-unity-replay/report.md`)

## Follow-ups (not red this run)

1. EMCON variability retrofit → exercise `EMCON_OFF`, flip mutant 06 to `defect`.
2. Oracle blind spots 03 (salvo) / 05 (contact lifecycle) — separate sim work.
3. System TDD skill alignment + evaluate_run hardenings (this PR branch).
