# Bug Report

## Summary
**Title**: `gauntlet-t2-escort-passive` claims low-Pd passive EMCON but ships all `basePd=1.0 / envMask=1.0`
**ID**: BUG-t2-escort-passive-emcon-claim-unimplemented
**Severity**: S3-Minor (test-coverage integrity — the scenario passes, but does not test what its name and intent claim)
**Priority**: P3 — backlog
**Status**: Open
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

## Related Issues
- `production/qa/bugs/BUG-catalog-emcon-tables-empty.md` — the underlying data gap
- `production/qa/bugs/BUG-scoring-penalises-roe-correct-refusals.md` — separate design question from the same run
