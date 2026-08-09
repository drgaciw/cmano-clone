# Bug Report

## Summary
**Title**: `forge_scorecard.py` keys oracle lookup on filename, not policy `id` — the forge skill's own naming convention makes promotion impossible
**ID**: BUG-forge-scorecard-filename-vs-policy-id
**Severity**: S2-Major (silently blocks the entire forge promote loop; no error, just `promote=0` forever)
**Priority**: P2
**Status**: Open
**Reported**: 2026-07-27
**Reporter**: QA Gauntlet run `gauntlet-20260727-1455`, Tier 2 forge `post-oracle` phase

## Classification
- **Category**: Tooling (QA/forge)
- **System**: `tools/qa-gauntlet/forge_scorecard.py`
- **Frequency**: Always, for any candidate whose filename stem differs from its policy `id`
- **Regression**: Unknown — likely present since the scorecard was written.

## The defect

`score_candidate` derives the scenario id from the **filename**:

```python
sid = policy_path.name.replace(".policy.json", "")   # forge_scorecard.py:170
...
oracle_ok = oracle_map.get(sid)                      # :179
```

but `read_oracle_passed` builds `oracle_map` from `oracle-eval.json`, which is keyed by each policy's **`id` field** (that is what `gauntlet_oracle_eval` emits).

So the lookup only succeeds when `filename stem == policy id`.

## Why this bites every run by default

`.claude/skills/qa-gauntlet-forge/SKILL.md` instructs candidates be written to:

```
production/qa/gauntlet/<RUN_ID>/forge/candidates/candidate-1.policy.json   ... candidate-N
```

with ids of the form `gauntlet-forge-<RUN_ID>-t<N>-c<N>`. Those never match. The result is a **silent** total failure of the promote loop:

```
oracleKnown        = False      (lookup missed)
oraclePassedOrUsefulFail = False
hardGatesPass      = False
-> forge-scorecard: candidates=4 promote=0 discard=4
```

There is no warning that the oracle result simply wasn't found — a candidate that passed the oracle cleanly is indistinguishable from one never evaluated.

## Evidence from this run

Tier 2 produced 4 candidates that passed everything on merit:

| | oracle | newCoverageCell | novelty |
|---|---|---|---|
| c1 | PASS | true | 4.5 |
| c2 | PASS | true | 4.0 |
| c3 | PASS | true | 4.5 |
| c4 | PASS | true | 4.0 |

With skill-conventional filenames: **promote=0, discard=4**.
After renaming each file to `<policy id>.policy.json` and changing nothing else: **promote=4, discard=0**.

The `oracle-eval.json` contained the correct ids the whole time (`gauntlet-forge-20260727-1455-t2-c1` …), and the candidate files carried those same ids internally. Only the filenames differed.

## Impact

- Any forge run that follows the documented naming convention promotes nothing, so the corpus never grows and recipe weights never get positive reinforcement.
- Because it fails closed and silently, it reads as "the candidates weren't good enough" rather than "the tool couldn't find their results". That is the dangerous part — it invites incorrectly down-weighting good recipes.

## Suggested fix (pick one)

1. **Prefer the policy's own id** (most correct):
   ```python
   sid = (policy.get("id") or policy_path.name.replace(".policy.json", ""))
   ```
   Requires moving the `load_json` call above the `sid` assignment. Falls back to filename, so nothing else breaks.
2. **Try both keys**: look up `oracle_map` by policy id first, then filename stem.
3. **Change the skill's convention** to name candidate files after their ids — works, but leaves the trap for anyone who deviates.

Option 1 or 2 is preferable: it makes the tool robust rather than relying on convention discipline.

Additionally, consider **logging when a candidate has no oracle entry**, so "not evaluated" is visibly distinct from "evaluated and failed".

## Workaround used in this run

Candidate files were renamed to `<policy id>.policy.json` before scoring. Promotion then behaved correctly. Recipe weights were **not** down-weighted on the basis of the spurious discards.

## Related Issues
- `production/qa/bugs/BUG-catalog-emcon-tables-empty.md`
- `production/qa/bugs/BUG-t2-escort-passive-emcon-claim-unimplemented.md`
- `production/qa/bugs/BUG-scoring-penalises-roe-correct-refusals.md`
