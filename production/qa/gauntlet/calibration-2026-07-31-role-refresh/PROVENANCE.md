# Calibration role-refresh provenance (2026-07-31)

## What this is

Post-remediation (**PR #370** → **#368**) artifact refresh so calibration
reports match catalog `role` contracts and the kill-rate formula in
`tools/qa-gauntlet/saboteur.py` / `/qa-gauntlet-calibrate`.

## What was measured vs recomputed

| Field | Source |
|-------|--------|
| `outcome` (caught / survived / invalid-mutant) | **Measured** — live saboteur on post-rebase tip (`calibration-2026-07-28-postrebase`) |
| `firedOracles` | **Measured** — same live run |
| `role` | **Catalog** — `tools/qa-gauntlet/mutants/catalog.yaml` (required after Agent B) |
| `expectedOracles` | **Catalog** (authoritative) |
| `summary.killRate`, `caughtDefects`, `survivedDefects` | **Recomputed** via `summarize()` |
| Exit semantics | **Recomputed** via `exit_code_for()` → exit **1** because defect survivors **03** and **05** (not because of expected-miss **06**) |

## Kill rate

```
killRate = caught_defects / (caught_defects + survived_defects) = 4/6
```

`control` (`00`) and `expected-miss` (`06`) are excluded from num/denom.

Do **not** cite legacy `4/7` or raw `4/8` (pre-role denominator) for post-#370 AARs.

## Why not a full live rerun here

Full catalog saboteur (8 worktree builds × subset ladder + ReplayGolden) was not
re-executed in this environment. Outcomes are unchanged by pure summarizer/
catalog-role work; a live re-run remains optional for CI time and should land
as a new `calibration-<date>/` if mutant surfaces or oracles change.

## Related

- Blind spots already filed: `BUG-oracle-blindspot-03-salvo-off-by-one`,
  `BUG-oracle-blindspot-05-contact-lifecycle-skip`
- EMCON expected miss: `06-emcon-engage-bypass` (`role: expected-miss`)
