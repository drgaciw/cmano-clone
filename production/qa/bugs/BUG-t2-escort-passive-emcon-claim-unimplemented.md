# Bug Report

## Summary
**Title**: `gauntlet-t2-escort-passive` claims low-Pd passive EMCON but ships all `basePd=1.0 / envMask=1.0`
**ID**: BUG-t2-escort-passive-emcon-claim-unimplemented
**Severity**: S3-Minor (test-coverage integrity — the scenario passes, but does not test what its name and intent claim)
**Priority**: P3 — backlog
**Status**: Fixed
**Reported**: 2026-07-27
**Reporter**: QA Gauntlet run `gauntlet-20260727-1455`, Tier 2. Found by the scenario architect while it was being cited as the reference implementation for passive-EMCON modelling — it checked the file rather than taking the citation on trust.

## Classification
- **Category**: Scenario data / test integrity
- **System**: gauntlet corpus (`data/scenarios/`)
- **Frequency**: Always
- **Regression**: No — appears to have shipped this way.

## The defect

`data/scenarios/gauntlet-t2-escort-passive.policy.json` declares:

> `"intent": "Escort passive-EMCON stand-in (low Pd/env mask) [catalog ORBAT: Visby vs Sovremenny] ..."`

But its detection block contains exactly one distinct value pair across **every** entry:

```
distinct (basePd, envMask) pairs: {(1.0, 1.0)}
```

`basePd = 1.0` is *perfect* detection probability and `envMask = 1.0` is *no* environmental masking — the opposite of the degraded sensing the intent describes. Nothing in the file implements a passive-EMCON posture.

## Why it matters

1. **It tests nothing it claims.** The scenario is the corpus's designated passive-EMCON coverage for Tier 2. It currently exercises the same full-detection path as an unrestricted scenario.
2. **It misleads by citation.** In this run it was handed to a scenario architect as *the* reference for how to model passive EMCON. Had the architect copied it uncritically, the new Tier 2 scenarios would have inherited the same empty gesture. (It didn't — it read the file, found the contradiction, and implemented real reduced values instead, then flagged it.)
3. **The coverage map counts it as EMCON coverage** it does not actually provide.

## Related root cause

This is downstream of `production/qa/bugs/BUG-catalog-emcon-tables-empty.md` — the catalog has no `platform_emcon` data at all (0 rows), so there is no data-driven way to express EMCON. The corpus adopted a prose "stand-in" convention. This scenario took the prose and skipped the stand-in.

## Suggested fix

Implement the stand-in it advertises: reduce `basePd` / `envMask` on the passive side's detection entries (the Tier 2 scenarios generated in this run use ~0.3–0.45, which produced clearly differentiated behaviour). Then regenerate its `gauntlet.expect` envelope per `tools/qa-gauntlet/README-expect-regen.md`, since detection changes will legitimately move kills/score.

Worth a sweep of the other EMCON-flavoured corpus scenarios for the same gap — this was found by chance while using the file as a reference, not by a systematic check.

## Resolution (2026-08-20)

Implemented the advertised stand-in on `gauntlet-t2-escort-passive` only: blue detection
trials now use `basePd`/`envMask` in the ~0.3–0.45 range. Distinct pairs went from
`{(1.0, 1.0)}` to `{(0.3, 0.35), (0.35, 0.4), (0.4, 0.45)}`. There are no hostile
observer trials in this file. Top-level `emcon.units` was already present (Passive on
`k-22-gavle-ex-goteborg-class`, Active on `jas-39e-gripen-ng-2021`).

T2 batch at ticks=10, seeds 42,7,123: numeric `gauntlet.expect` still matched observed
rows (`allPassed: true`); fingerprint goldens for this scenario were re-blessed from
that CSV. `gauntlet-t3-emcon-phases` / `gauntlet-t5-roe-change` left to other workers.

**P2 re-bless (2026-08-27):** anchors for `gauntlet-t2-escort-passive|{7,42,123}` were
re-derived via `tools/qa-gauntlet/evaluate_run.py bless` from green run
`gauntlet-t2-escort-passive-pd-bless-20260827` (Demo batch ticks=10, seeds 42/7/123,
`oracle-eval.json` allPassed, tier-2 `verdict.json` pass). Hashes unchanged vs prior
hand-edit; `blessedFrom` now names this run instead of `gauntlet-winchester-full-20260801b`.
Other anchor keys in `tools/qa-gauntlet/goldens/anchors.json` untouched.

## Related Issues
- `production/qa/bugs/BUG-catalog-emcon-tables-empty.md` — the underlying data gap
- `production/qa/bugs/BUG-scoring-penalises-roe-correct-refusals.md` — separate design question from the same run

---

## Scope correction (2026-07-27, later same day)

**This defect affects three scenarios, not one.** The original report named only
`gauntlet-t2-escort-passive`. A systematic sweep during implementation planning found the
same pattern in two more:

| Scenario | `gauntlet.emcon` value | Real top-level `emcon` block? |
|---|---|---|
| `gauntlet-t2-escort-passive` | `"passive-blue-standin"` | absent |
| `gauntlet-t3-emcon-phases` | `"phased"` | absent |
| `gauntlet-t5-roe-change` | `"contested"` | absent |

**The precise mechanism is narrower and worse than first described.** These scenarios declare
EMCON as a **prose string at `gauntlet.emcon`** — a location the engine never reads. The real,
engine-consumed block is **top-level** `emcon` with a `units` map
(`{"units": {"<unitId>": {"radar": "Active|Passive"}}}`, per `ScenarioEmconJsonDto`), as used by
7 non-gauntlet scenarios such as `baltic-v3-patrol`.

So it is not merely that the detection values were left at 1.0: the EMCON declaration is in a
field the deserializer ignores entirely. `ScenarioGauntletJsonDto` has no `Emcon` property —
only `Intent`, `Oracle`, `CatalogRefs`, and `Units` — so `gauntlet.emcon` is silently dropped at
load.

**Note on verification method:** an initial check for this scope expansion scanned only
*top-level* `emcon` and found nothing, appearing to contradict the finding. The prose values are
nested under `gauntlet`, so the top-level scan was looking in the wrong place. Recorded because
the same mistake would hide the defect from any future audit that greps only the top level.

**Remediation** is unchanged in kind but now applies to all three: move EMCON to the real
top-level `emcon.units` block, then regenerate each `gauntlet.expect` at its tier tick budget per
`tools/qa-gauntlet/README-expect-regen.md`, since real EMCON will legitimately move detection
and therefore kills/score.
