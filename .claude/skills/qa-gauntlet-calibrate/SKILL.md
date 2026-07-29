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

## Read the report

`production/qa/gauntlet/calibration-<date>/report.md` (+ `report.json`):

- `00-noop-comment` must SURVIVE (behavior-neutral control — proves no false
  positives). A *caught* control is a pipeline bug and fails the run.
- Every other SURVIVED row is a named oracle blind spot: file
  `production/qa/bugs/BUG-oracle-blindspot-<mutant-id>.md` via the `bug-report`
  skill, quoting the report row. Do not delete the mutant.
- `06-emcon-engage-bypass` is a *documented expected miss* until the 2026-07-27
  variability-plan EMCON retrofit lands (see catalog comment) — its survival is
  tracked, not re-filed.
- `INVALID-MUTANT` (build failure) proves nothing: fix or remove the patch.
- Exit code: 0 iff no invalid mutants, no non-control survivors, no caught controls.

## Rules

- Mutants never touch locked-eval files (`saboteur.py` refuses to load such a catalog).
- Never commit from a saboteur worktree; the tool removes worktrees when done
  (`--keep-worktrees` for debugging only).
- Adding a mutant: generate the patch in a temp worktree (edit → `git diff` →
  discard), record a GitNexus `impact()` result in `catalog.yaml`, and verify the
  mutant is caught before committing — procedure in
  `docs/superpowers/plans/2026-07-28-qa-gauntlet-effectiveness.md` Task 11.
- Cite the latest kill rate in every `/qa-gauntlet` AAR.
