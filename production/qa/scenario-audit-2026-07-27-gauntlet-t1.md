# Scenario Audit Report

**Date**: 2026-07-27
**Scope**: QA Gauntlet run `gauntlet-20260727-1455`, Tier 1 (4 main scenarios + 4 forge candidates)
**Scenarios audited**: 8
**GitNexus index**: fresh (rebuilt this run — 29,338 nodes / 55,559 edges / 300 flows)
**Audited by**: gauntlet orchestrator (direct, not dispatched — see adaptation note; all 8 files are short `.policy.json` and were read in full)
**Quick mode**: no (all applicable checks run)

---

## Adaptation notes (schema mismatch vs. skill's generic checks)

Two of this skill's generic checks target the MissionEditor's full `ScenarioDocument` schema, which does **not** apply to gauntlet `.policy.json` artifacts (confirmed by checking the already-promoted references `data/scenarios/gauntlet-t1-patrol-a.policy.json` / `-b.policy.json`, which have the identical lightweight shape and would fail the same way):

- **`scenario_validate` CLI (check 1)**: fails all 8 with `TL_BRANCH_MISSING` — this validates `metadata.tlBranch`/`dbSnapshotId`/`unitReadiness` etc., fields that belong to the *different* full-document schema, not the gauntlet policy schema (`friendlyRoe`/`opposingRoe`/`engage`/`detection`/`gauntlet`/`id`). **NOT RUN** for this schema — not treated as FAIL.
- **`metadata.seed` presence (check 4)**: no `.policy.json` in the corpus (including patrol-a/b) carries a `seed` field. Confirmed this is by design — seeds are supplied externally at batch-run time via the harness's `--seeds 42,7,123` CLI flag (Phase B), not baked into the policy document. Not a finding.

All checks below use direct Read + Grep + catalog cross-reference instead, per the skill's fallback path.

---

## Executive Summary

| Verdict | Count |
|---------|-------|
| PASS | 8 |
| PASS-WITH-FINDINGS | 0 |
| FAIL | 0 |
| NOT RUN (partial) | 0 (schema-mismatch checks noted above, not counted as partial per-scenario) |

**Overall Verdict**: PASS

**Re-run required after fixes**: No fixes needed this pass.

---

## Per-Scenario Results

| Scenario | Format | Checks Passed | BLOCKERs | Verdict | Evidence |
|----------|--------|---------------|----------|---------|----------|
| tier-1/scenario-1.policy.json | policy v-current (gauntlet) | 5/5 applicable | 0 | PASS | 6/6 unit IDs + 6/6 detection observer/target IDs resolve against `tier-1/roster.json`; no hand-typed catalog stat overrides; intent ("Blue survives 6 ticks") matches expect (side BLUE, minKills 0, minDenials 0, requireNonEmptyFingerprint true); engage params plausible (pkBase 0.85, range 40km) |
| tier-1/scenario-2.policy.json | policy v-current | 5/5 | 0 | PASS | Same checks; 6/6 IDs resolve; expect envelope maxMissiles 16/maxDenials 14 scaled sensibly for its DDG-inclusive ORBAT |
| tier-1/scenario-3.policy.json | policy v-current | 5/5 | 0 | PASS | Same checks; 6/6 IDs resolve; distinct blue/red split from s1/s2/s4 confirmed |
| tier-1/scenario-4.policy.json | policy v-current | 5/5 | 0 | PASS | Same checks; 6/6 IDs resolve; distinct split confirmed |
| forge/candidates/candidate-1.policy.json | policy v-current (ephemeral) | 5/5 | 0 | PASS | 6/6 IDs resolve; `gauntlet.forge` metadata block present (recipe `platform-swap-underused`, candidateId, noveltyTags); expect block explicitly self-flagged as "carried from baseline, pending regen" — matches recipe's `forbiddenTouches: [gauntlet.expect-without-regen]` constraint |
| forge/candidates/candidate-2.policy.json | policy v-current (ephemeral) | 5/5 | 0 | PASS | Same; first all-PCFG symmetric ORBAT in the tier-1 batch (novel vs. existing coverage-map cell) |
| forge/candidates/candidate-3.policy.json | policy v-current (ephemeral) | 5/5 | 0 | PASS | Same; 5-nationality heterogeneous mix, both sides |
| forge/candidates/candidate-4.policy.json | policy v-current (ephemeral) | 5/5 | 0 | PASS | Same; DDG role-reversed onto red for the first time — flagged `role-reversal` noveltyTag |

---

## Detailed Findings

None — no BLOCKER, HIGH, or MEDIUM findings. Two LOW/informational observations (not corpus deviations — both match established convention in the already-promoted `gauntlet-t1-patrol-a/b.policy.json`, verified directly):

- **LOW**: Main scenarios' `gauntlet.catalogRefs` lists all 10 tier roster platformIds regardless of which 6 are actually used in that scenario (matches `patrol-a`'s convention of listing a broader "expanded ORBAT" catalogRefs set). Forge candidates instead list only their 6 used IDs. Cosmetic inconsistency between the two artifact groups; harmless (catalogRefs is not consumed by the sim engine, only `gauntlet.units`/`detection[]` are).
- **LOW**: Forge candidates' intent text says "Blue survives N ticks" (pure survival framing) while their inherited `expect.minKills` is 1, not 0. This exact pattern already exists in the promoted `gauntlet-t1-patrol-a.policy.json` baseline (intent: "survives N ticks", expect: `minKills:1, minDenials:2`), so it's established corpus convention, not a new inconsistency — flagging only because forge's own post-oracle regen pass should reconcile it against a live batch run rather than leaving it as copied-baseline guesswork.

---

## Routing Summary

No routing needed — all PASS, no BLOCKERs. The two LOW observations are informational for `qa-gauntlet-forge`'s upcoming `post-oracle` expect-regen pass (Phase C), not action items requiring a separate fix loop.

---

## Next Steps

1. Proceed to Phase B (batch execution) for the 4 main scenarios at Tier 1's 6-tick budget.
2. Forge candidates await Phase C `post-oracle` scorecard before any promotion decision.
3. No re-audit needed pre-batch — all 8 artifacts clean.
