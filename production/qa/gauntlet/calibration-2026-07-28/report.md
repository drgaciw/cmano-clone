# Saboteur calibration report

**Kill rate: 4/8** (caught 4, survived 4, invalid 0; controls excluded from pass/fail by id prefix 00-)

| Mutant | Outcome | Fired oracles | Expected |
|---|---|---|---|
| 00-noop-comment | SURVIVED | — | — |
| 01-pd-weakened | caught | goldens, tiers | goldens, victory_roe |
| 02-roe-tight-inverted | caught | goldens, tiers, token_coverage, victory_roe | victory_roe, goldens, token_coverage |
| 03-salvo-off-by-one | SURVIVED | — | goldens, victory_roe |
| 04-rng-seed-ignored | caught | goldens, tiers, token_coverage | sanity, goldens, replay_golden |
| 05-contact-lifecycle-skip | SURVIVED | — | goldens |
| 06-emcon-engage-bypass | SURVIVED | — | — |
| 07-magazine-not-decremented | caught | goldens, tiers, token_coverage, victory_roe | token_coverage, goldens, victory_roe |

Every non-control SURVIVED row is a named oracle blind spot — file a bug per row.
