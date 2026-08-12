# PR #367 — review, conflict resolution, and 5-PR split

**Date:** 2026-08-07
**Original PR:** [#367](https://github.com/drgaciw/cmano-clone/pull/367) — `qa(forge): close out gauntlet tier 3, plan + roster tier-4 candidate wave`
**Original head:** `claude/model-sonnet-4fnig1` → `main` · 55 files, +10,604 / −200, 11 commits

## Why #367 could not merge

Not size. GitHub reported `mergeable_state: dirty` — a **merge conflict** in exactly one
file, `production/qa/gauntlet/corpus/coverage-map.json`.

The conflict is a clean disjoint-add:

| Side | Cells added since fork | Dropped |
|---|---|---|
| `main` (regenerated 2026-07-31, DRG-60) | 6 | 0 |
| PR #367 | 16 | 0 |
| Overlap | 0 | — |

Every other file the PR shares with `main` was **unchanged on `main` since the fork**, so
they apply cleanly. `coverage-map.json` was the sole blocker.

## What the split had to fix beyond the conflict

`main` gained two commits that change the rules for this content:

- `78191cb2 ci(gauntlet)` — runs `tools/qa-gauntlet` pytest on **every PR**
- `f8e7380d fix(gauntlet)` — regenerated coverage counts and added a drift guard

`test_coverage_map_bootstrap_consistency` requires a coverage cell for **every**
`data/scenarios/gauntlet-*.policy.json`, and `assert_counts_consistent` requires
`cellCount == scenarioCount == len(cells)` with all six count histograms matching a
rebuild.

A naive split that put all scenario files in early PRs and the coverage-map update in a
later one would have made PRs 2 and 3 **red in CI**. Instead each PR that adds scenarios
also regenerates `coverage-map.json` using the canonical
`forge_scorecard.infer_cell` / `rebuild_counts` helpers — so counts are derived, not
asserted, and every branch is green standing alone.

## The stack

Linear stack on `main` @ `f8e7380d`. Each row verified against the CI guards.

| # | Branch | Files | Diff | Guard |
|---|---|---|---|---|
| 1 | `pr367-1-sprint36-ux-docs` | 7 | +252 / −12 | 50 scenarios / 50 cells ✅ |
| 2 | `pr367-2-gauntlet-tier4` | 14 | +3231 / −53 | 54 / 54 ✅ |
| 3 | `pr367-3-gauntlet-tier5` | 14 | +3664 / −52 | 58 / 58 ✅ |
| 4 | `pr367-4-forge-corpus` | 16 | +3098 / −179 | 66 / 66 ✅ |
| 5 | `pr367-5-qa-records` | 5 | +350 | 66 / 66 ✅ |

**Total: 54 files** — the original PR's 55 minus
`production/qa/accessibility-signoff-s36-01-2026-08-01.md`, held back by decision (see
below). That file remains only on `claude/model-sonnet-4fnig1` / PR #367.

Faithfulness check: within the PR's scope, the stack tip differs from `pr-367-head` in
**only** `coverage-map.json` (the intentional regeneration) and the held-back sign-off.

### PR 1 — `pr367-1-sprint36-ux-docs`
Sprint-36 UX docs + evidence. Fully independent of the gauntlet content; **can merge on
its own** without the rest of the stack.

### PR 2 / PR 3 — tier-4 / tier-5 run artifacts
Archival backfill for run `gauntlet-20260727-1455`: rosters, 4 scenarios each, results
and oracle-eval (`allPassed: true` both tiers).

### PR 4 — `pr367-4-forge-corpus`
Promotes 8 forge candidates, registers them in `corpus/index.yaml`, refreshes recipe
weights / scorecard / promote-log / manifest / AAR. **Carries the conflict resolution.**

### PR 5 — `pr367-5-qa-records`
Two open sim bug reports, determinism replay, smoke, tier-4 scenario audit. Last in the
stack because the bug reports and audit cite tier-4/tier-5 artifacts from PRs 2–4.

## Review findings

### Blocking — RESOLVED: sign-off dropped from the stack

**Decision (2026-08-07): the file was removed from PR 1** rather than landing a sign-off
that did not verify what it cites. It still needs a re-run before it can land.

`production/qa/accessibility-signoff-s36-01-2026-08-01.md:61,71` cites host names
`MapPanelHost`, `UnitDetailPanelHost`, `MessageLogHost`, `C2TopBarHost`. The document it
certifies — `design/accessibility-requirements.md:19`, **changed in this same PR** — was
corrected to `MapPlaceholderPanelHost`, `RightUnitPanelHost`, `MessageLogPanelHost`,
`C2TopBarPanelHost`. The APPROVED verdict quotes names that no longer appear in the file
it reviewed, so the check was not re-run against final content.

This was deliberately **not** auto-corrected: editing a QA sign-off to match content it
did not actually verify would falsify the record. Either re-run the sign-off against the
corrected doc, or drop that file from PR 1.

### Non-blocking

- **Ladder "5/5 complete" is overstated.** Run `20260727-1455` has no `verdict.json` or
  `roving-seeds.txt` — the `run-gauntlet.sh` driver contract postdates it. This is
  *consistent*: tiers 1–3 of the same run, already on `main`, also lack them. Current
  ladder health is evidenced by `gauntlet-20260731-0855` on `main` (run-level
  `verdict.json`, `pass: true`) plus two later re-bless runs. **These tier-4/5 artifacts
  are historical backfill of a superseded run, not current evidence.**
- **`MissionContactTargetClass.Classify` remains broken.**
  `src/ProjectAegis.Sim/Scenario/MissionContactTargetClass.cs:12-15` returns `Air` only
  for ids starting with `ucav`; everything else classifies `Surface`. The PR patched the
  two scenarios an external bot caught, but the classifier is quarantined, not fixed.
  Separately, `ScenarioPolicyJsonLoader.cs:362-365` silently degrades unrecognised
  `targetClass` values to `Any` — 6 `"Subsurface"` instances ship that way. **This stack
  contains zero `src/` changes and fixes neither.**
- **Story bookkeeping not advanced.** All three `story-036-*` files keep `status: Ready`
  and unchecked ACs; only a `Last Updated:` line changed. No AC is falsely marked done.
- **Perf baseline is honest.** Real measurements (`mean=0.081 ms p95=0.149 ms
  max=0.164 ms, n=20`) with commands shown; the Unity 16.67 ms frame metric is reported
  **NOT RUN** rather than fabricated.
- Scenario data validates: all 117 merged `*.policy.json` parse with unique
  case-insensitive ids (`ScenarioPolicyJsonIndex` throws on either).
- `data/scenarios/*` and `tier-N/scenario-N.policy.json` copies are byte-identical.

## Remaining steps (need `gt` or `gh` auth — not available in this environment)

The five branches are **pushed**. Creating and merging the PRs needs Graphite/GitHub
auth, which this WSL environment does not have (`gt` and `gh` are not installed and
credential access is restricted).

```bash
# Graphite (repo convention — see docs/engineering/graphite-github-substitute-plan.md)
gt track pr367-1-sprint36-ux-docs --parent main
gt track pr367-2-gauntlet-tier4   --parent pr367-1-sprint36-ux-docs
gt track pr367-3-gauntlet-tier5   --parent pr367-2-gauntlet-tier4
gt track pr367-4-forge-corpus     --parent pr367-3-gauntlet-tier5
gt track pr367-5-qa-records       --parent pr367-4-forge-corpus
gt submit --stack --no-interactive
```

Or open them in the browser:

- [PR 1](https://github.com/drgaciw/cmano-clone/compare/main...pr367-1-sprint36-ux-docs?expand=1)
- [PR 2](https://github.com/drgaciw/cmano-clone/compare/pr367-1-sprint36-ux-docs...pr367-2-gauntlet-tier4?expand=1)
- [PR 3](https://github.com/drgaciw/cmano-clone/compare/pr367-2-gauntlet-tier4...pr367-3-gauntlet-tier5?expand=1)
- [PR 4](https://github.com/drgaciw/cmano-clone/compare/pr367-3-gauntlet-tier5...pr367-4-forge-corpus?expand=1)
- [PR 5](https://github.com/drgaciw/cmano-clone/compare/pr367-4-forge-corpus...pr367-5-qa-records?expand=1)

Then close #367 referencing this stack.
