# Saboteur calibration report

**Kill rate: 9/9** (caught_defects / (caught_defects + survived_defects); control and expected-miss excluded from num/denom). Totals: caught 9, survived 1, invalid 0; defects caught 9, defects survived 0.

| Mutant | Role | Outcome | Fired oracles | Expected |
|---|---|---|---|---|
| 00-noop-comment | control | SURVIVED | — | — |
| 01-pd-weakened | defect | caught | goldens, replay_golden, tiers | goldens, victory_roe |
| 02-roe-tight-inverted | defect | caught | goldens, tiers, token_coverage, victory_roe | victory_roe, goldens, token_coverage |
| 03-salvo-off-by-one | defect | caught | goldens, tiers, victory_roe | goldens, victory_roe |
| 04-rng-seed-ignored | defect | caught | goldens, replay_golden, tiers | sanity, goldens, replay_golden |
| 05-contact-lifecycle-skip | defect | caught | goldens, replay_golden, tiers, victory_roe | goldens |
| 06-emcon-engage-bypass | defect | caught | goldens, tiers, token_coverage, victory_roe | token_coverage, goldens, victory_roe |
| 07-magazine-not-decremented | defect | caught | goldens, replay_golden, tiers, token_coverage, victory_roe | token_coverage, goldens, victory_roe |
| 08-bingo-gate-bypass | defect | caught | goldens, tiers, token_coverage, victory_roe | goldens, token_coverage, victory_roe |
| 09-winchester-gate-bypass | defect | caught | goldens, replay_golden, tiers, token_coverage | goldens, token_coverage, victory_roe |

Every SURVIVED `defect` row is a named oracle blind spot — file a bug per row. `expected-miss` survivors are tracked (not fail); flip role to `defect` when the miss becomes catchable. A caught `control` or caught `expected-miss` fails the run.
