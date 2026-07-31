---
name: qa-gauntlet-calibrate
description: Measure QA Gauntlet oracle sensitivity by running the saboteur mutant catalog — deliberately broken sim builds in throwaway worktrees must turn the ladder red. Use after oracle/expect/golden changes, after sim refactors, or monthly; or when the user asks to "calibrate the gauntlet", "run the saboteur", "check oracle kill rate", or "mutation-test the gauntlet".
---

# QA Gauntlet Calibrate — Oracle Sensitivity (Saboteur)

Measures P(detect | defect): applies each curated mutant patch in a disposable git
worktree, builds, runs the anchor ladder subset (tiers 1/3/5 × anchor seeds 42,7,123)
plus the ReplayGolden test filter, and reports which oracles fired.

## Run

```bash
python3 tools/qa-gauntlet/saboteur.py                          # full catalog
python3 tools/qa-gauntlet/saboteur.py --mutants 01-pd-weakened # one mutant
```

Preconditions (tool enforces): no uncommitted changes under `src/`, `data/`,
`tools/qa-gauntlet/` (worktrees build from HEAD — dirty calibration-relevant paths
would NOT be calibrated); dotnet resolvable. Baseline ladder must be green at HEAD —
run `tools/qa-gauntlet/run-gauntlet.sh` first if in doubt; calibrating oracles against
a broken baseline is meaningless.

## Catalog `role` (required)

Every mutant in `tools/qa-gauntlet/mutants/catalog.yaml` declares:

```yaml
role: control | expected-miss | defect
```

| role | Meaning |
|------|---------|
| `control` | Behavior-neutral no-op (e.g. `00-noop-comment`). Must survive. |
| `expected-miss` | Documented uncatchable today (e.g. `06-emcon-engage-bypass` until EMCON retrofit). Survival is OK; excluded from kill-rate. When catchable, flip role to `defect`. |
| `defect` | Real oracle-sensitivity target. Survival is a blind spot and fails the run. |

`load_catalog` rejects missing or unknown `role`. Locked-eval targets are still refused.

## Kill rate

```
killRate = caught_defects / (caught_defects + survived_defects)
```

`control` and `expected-miss` are excluded from numerator and denominator.
Invalid mutants are also excluded. Cite the formula (and the latest report's
computed fraction) in AARs — do not invent a frozen wrong ratio.

## Exit code matrix

| Outcome | `control` | `expected-miss` | `defect` |
|---------|-----------|-----------------|----------|
| survived | exit 0 | exit 0 | exit 1 |
| caught | exit 1 (false positive) | exit 1 (flip role to defect when intentional) | exit 0 |
| invalid-mutant | exit 1 | exit 1 | exit 1 |

Exit 0 means: no invalid mutants, no caught controls, no caught expected-misses,
and no survived defects.

## Read the report

`production/qa/gauntlet/calibration-<date>/report.md` (+ `report.json`):

- `00-noop-comment` (`role: control`) must SURVIVE. A *caught* control is a
  pipeline bug and fails the run.
- Every SURVIVED `defect` row is a named oracle blind spot: file
  `production/qa/bugs/BUG-oracle-blindspot-<mutant-id>.md` via the `bug-report`
  skill, quoting the report row. Do not delete the mutant.
- `06-emcon-engage-bypass` (`role: expected-miss`) is a documented expected miss
  until the 2026-07-27 variability-plan EMCON retrofit lands — survival is
  tracked (exit OK, excluded from kill-rate), not re-filed. When catchable,
  flip `role` to `defect`.
- `INVALID-MUTANT` (build failure) proves nothing: fix or remove the patch.

## Rules

- Mutants never touch locked-eval files (`saboteur.py` refuses to load such a catalog).
- Never commit from a saboteur worktree; the tool removes worktrees when done
  (`--keep-worktrees` for debugging only).
- Adding a mutant: generate the patch in a temp worktree (edit → `git diff` →
  discard), set `role`, record a GitNexus `impact()` result in `catalog.yaml`,
  and verify the mutant is caught before committing — procedure in
  `docs/superpowers/plans/2026-07-28-qa-gauntlet-effectiveness.md` Task 11.
- Cite the latest kill rate (formula above) in every `/qa-gauntlet` AAR.
