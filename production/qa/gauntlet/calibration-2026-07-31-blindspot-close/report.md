# Saboteur calibration report

**Kill rate: 3/3** (caught_defects / (caught_defects + survived_defects); control and expected-miss excluded from num/denom). Totals: caught 3, survived 0, invalid 0; defects caught 3, defects survived 0.

| Mutant | Role | Outcome | Fired oracles | Expected |
|---|---|---|---|---|
| 03-salvo-off-by-one | defect | caught | goldens, tiers, victory_roe | goldens, victory_roe |
| 05-contact-lifecycle-skip | defect | caught | goldens, replay_golden, tiers, victory_roe | goldens |
| 06-emcon-engage-bypass | defect | caught | goldens, tiers, token_coverage, victory_roe | token_coverage, goldens, victory_roe |

Every SURVIVED `defect` row is a named oracle blind spot — file a bug per row. `expected-miss` survivors are tracked (not fail); flip role to `defect` when the miss becomes catchable. A caught `control` or caught `expected-miss` fails the run.
