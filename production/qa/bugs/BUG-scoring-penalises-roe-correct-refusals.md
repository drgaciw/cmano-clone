# Bug Report

## Summary
**Title**: Score penalises ROE-correct refusals identically to genuine policy violations
**ID**: BUG-scoring-penalises-roe-correct-refusals
**Severity**: S3-Minor (scoring/design semantics; no crash, no incorrect combat resolution)
**Priority**: P3 — needs a human **design** decision, not a code fix; filing so it is not lost
**Status**: Open — DESIGN QUESTION (not a defect with an obvious correct implementation)
**Reported**: 2026-07-27
**Reporter**: QA Gauntlet run `gauntlet-20260727-1455`, surfaced while re-baselining the corpus after fixing BUG-engagement-resolver-shooter-liveness and BUG-losses-scoring-side-unaware

## Classification
- **Category**: Gameplay (scoring design)
- **System**: `ProjectAegis.Delegation` scoring projection
- **Frequency**: Always, in any scenario with restrictive ROE/EMCON
- **Regression**: No — pre-existing. Made *visible* (not caused) by the two fixes above.

## The question

`LossesScoringProjection.Project` computes:

```
score = baseScore + (kills * 100) - (denials * 5)
```

`denials` is `log.PolicyDenials.Count` — every engagement the policy layer refused. But that count does not distinguish between:

1. **A ROE-correct refusal** — the unit was *supposed* to hold fire (WeaponsTight, HoldFire, ID-required not met, EMCON discipline). The system worked exactly as designed.
2. **A genuine violation or failure** — an attempt that should have succeeded but was blocked.

Both cost −5. So a scenario whose entire design point is *restraint* is scored as if it were failing.

## Evidence

Post-fix corpus run (correct per-tier tick budgets, seeds 42/7/123):

| Scenario | Intent | kills | denials | score |
|---|---|---|---|---|
| `gauntlet-t1-patrol-c` | "Patrol with **tight Blue ROE**" | 0 | 18 | **−90** |
| `gauntlet-t2-escort-passive` | "Escort **passive-EMCON** stand-in (low Pd/env mask)" | 0 | 40 | **−200** |
| `gauntlet-theater-inject` | theater op w/ injects | 1–3 | 81 | −105 … −305 |

`gauntlet-t2-escort-passive` scores −200 for doing precisely what its name says: staying passive and not shooting. Its denial count *is* the evidence the EMCON/ROE gating works.

Note this also interacts with the shooter-liveness fix: a dead shooter's blocked engagement is now recorded as a denial rather than a launch, so denial counts rose across the corpus — correct behaviour, but it amplifies this penalty.

## Why this is filed as a design question, not a fix

There are several defensible answers and picking one is a game-design call, not a QA call:

1. **Exclude ROE-correct refusals from the penalty** — only count denials that represent a genuine failure. Requires classifying `FireAbortReason` into "correct restraint" vs "failure" (e.g. `RoeHoldFire`/`WeaponsTight` are correct restraint; `NoFireControlTrack` arguably is not).
2. **Weight denials by reason** rather than a flat −5.
3. **Keep the flat penalty but give restraint-oriented scenarios a positive objective** so the score reflects mission success rather than shot count.
4. **Accept it** — treat negative scores as the expected signature of a restrained posture, and rely on `gauntlet.expect` envelopes (which now encode this per scenario) rather than on score sign meaning "good/bad".

Option 4 is the current de-facto state after this run's re-baseline, and is a legitimate choice — but it should be a *chosen* one rather than an accident.

## Related Issues
- `production/qa/bugs/BUG-engagement-resolver-shooter-liveness.md` (fixed this session)
- `production/qa/bugs/BUG-losses-scoring-side-unaware.md` (fixed this session)
- Envelope re-baseline that surfaced this: see `qa(gauntlet): regen corpus expect envelopes` on branch `07-27-qa_gauntlet_fix_...`

## Notes

No code change is proposed here. The corpus envelopes were re-baselined against observed post-fix behaviour with **denial floors preserved** (`minDenials` kept meaningful), so restriction-oriented scenarios still assert that ROE/EMCON gating actually fires — the re-baseline did not turn those tests into no-ops. If option 1 or 2 above is later chosen, those envelopes will need regenerating again per `tools/qa-gauntlet/README-expect-regen.md`.
