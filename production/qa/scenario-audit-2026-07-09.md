# Scenario Audit Report

**Date**: 2026-07-09
**Scope**: `production/qa/gauntlet/gauntlet-20260709-1242/tier-1/` (4 scenarios)
**Scenarios audited**: 4
**GitNexus index**: fresh
**Audited by**: 4 parallel scenario-content-specialist workers via `/scenario-audit` (dispatched per `dispatching-parallel-agents`, N=4 > 2)
**Quick mode**: no — all 7 checks run per scenario
**Context**: run under `/qa-gauntlet` Tier 1 Phase A validation (`production/qa/gauntlet/gauntlet-20260709-1242/manifest.yaml`)

---

## Executive Summary

| Verdict | Count |
|---------|-------|
| PASS | 3 |
| PASS-WITH-FINDINGS | 1 |
| FAIL | 0 |
| NOT RUN (partial) | 0 |

**Overall Verdict**: PASS-WITH-FINDINGS (no BLOCKERs; one MEDIUM finding, fixed and re-validated during this audit)

**Re-run required after fixes**: Yes — tier1-patrol-04 re-validated after content fix (see below). tier1-patrol-03's initial worker finding was investigated and determined to be a false positive; no scenario edit was needed.

---

## Per-Scenario Results

| Scenario | Format | Checks Passed | BLOCKERs | Verdict | Evidence |
|----------|--------|----------------|----------|---------|----------|
| tier1-patrol-01.policy.json | schemaVersion 1 | 7/7 | 0 | PASS | CLI exit 0, canExport=true; 2/2 unit IDs resolved (u1, hostile-1) against `baltic_patrol` dbRef; seed=42; briefing "survive 6 ticks" matches Tier-1 spec; no triggers/events; 1v1 balance sane. Minor non-blocking note: `schemaVersion` metadata field not present in canonical reference scenarios but tolerated by CLI. |
| tier1-patrol-02.policy.json | schemaVersion 1 | 7/7 | 0 | PASS | CLI exit 0, canExport=true; 2/2 unit IDs resolved (u1, hostile-far); seed=7; long-separation (225.5 nm) briefing consistent with description; no triggers; 1v1 balance sane. |
| tier1-patrol-03.policy.json | schemaVersion 1 | 7/7 | 0 | PASS | CLI exit 0, canExport=true; 3/3 unit IDs resolved (u1, hostile-far, hostile-1); seed=123; description "2 Blue vs 1 Red" matches actual mission grouping (`blue-patrol-03a`=u1, `blue-patrol-03b`=hostile-far, `red-patrol-03`=hostile-1) — same pattern validated correct in tier1-patrol-04; no triggers; balance sane. **Investigated and cleared a worker-reported false positive** — see Detailed Findings. |
| tier1-patrol-04.policy.json | schemaVersion 1 | 7/7 (post-fix) | 0 | PASS (fixed) | CLI exit 0, canExport=true (both pre- and post-fix); 3/3 unit IDs resolved; seed=999; mission grouping is 2 Blue (u1, hostile-far) vs 1 Red (hostile-1), matching `oracles.md`. **Description text originally read "2 vs 2" — corrected to "2 vs 1" and re-validated.** |

---

## Detailed Findings

### tier1-patrol-04.policy.json — MEDIUM-001 (FIXED)
**Severity**: MEDIUM
**Check**: 5 (Victory/briefing honesty)
**Evidence**: `metadata.description` originally read "2 surface units Blue vs 2 surface units Red" / "symmetric 2-vs-2 surface OOB", but actual mission assignments are 2 Blue (`u1` in `blue-patrol-04a`, `hostile-far` in `blue-patrol-04b`) vs 1 Red (`hostile-1` in `red-patrol-04a`) — matching `oracles.md`'s correct "2 Blue surface units ... vs 1 Red surface unit" oracle, not the in-file description.
**Fix applied**: Corrected `metadata.description` to "2 surface units Blue vs 1 surface unit Red ... asymmetric 2-vs-1 surface OOB", matching actual OOB and the oracle.
**Re-validation**: `scenario_validate --path tier1-patrol-04.policy.json` → `passed: true, canExport: true` (identical clean result, only Info-level DOCTRINE_RESOLVED findings). No regression.
**Route**: closed — content-only fix, applied directly under `/qa-gauntlet` autonomy (scenario-data defect within the gauntlet's `production/qa/gauntlet/<RUN_ID>/` scope).

### tier1-patrol-03.policy.json — Worker finding investigated, cleared as false positive
**Original claim** (from the parallel audit worker): `hostile-far`, assigned to mission `blue-patrol-03b`, is a "Red-catalog platform" per `BalticV3SideRegistry.cs`, making the actual composition 1 Blue vs 2 Red — inverted from the stated "2 Blue vs 1 Red".
**Verification performed**:
- Read `tier1-patrol-03.policy.json` directly: mission grouping is `blue-patrol-03a` (u1) + `blue-patrol-03b` (hostile-far) = 2 Blue missions, `red-patrol-03` (hostile-1) = 1 Red mission — structurally identical to tier1-patrol-04's validated-correct pattern (`hostile-far` used as the second Blue unit there too).
- Read `src/ProjectAegis.Sim/Scenario/BalticV3SideRegistry.cs:6-11` directly: `GetSideForUnit` only maps `u1`/`ucav-blue`→`"blue"` and `hostile-1`/`ucav-red`→`"red"`; every other unit ID, including `hostile-far`, returns `null` (unregistered) — it is *not* classified as Red anywhere in this file.
- Ran `impact({target: "BalticV3SideRegistry", direction: "upstream"})` via GitNexus: **0 upstream callers, risk LOW** — this class is unused/dead code, not part of the live sim pipeline, so it has no bearing on how the scenario actually assigns sides during execution.
**Conclusion**: The worker's finding was incorrect (likely misread the registry's narrow `u1`/`hostile-1` example mapping as an exhaustive side authority). tier1-patrol-03's description, mission naming, and unit assignments are mutually consistent. **No scenario edit required.** Verdict revised from the worker's PASS-WITH-FINDINGS (5/7) to PASS (7/7).
**Advisory (non-blocking)**: `BalticV3SideRegistry` being dead code with only 2 of the fixture's 3 usable unit IDs mapped is itself mildly notable — if any future sim-code path comes to depend on it for side resolution instead of mission-name grouping, `hostile-far` would silently resolve to an unregistered side. Not a defect today (0 callers); noting for awareness only, no action required.

---

## Routing Summary

- Content fixes → applied directly (tier1-patrol-04 description correction), within `/qa-gauntlet` autonomy scope for `production/qa/gauntlet/<RUN_ID>/` scenario files.
- No CLI defects, no determinism issues (all 4 scenarios have explicit seeds: 42, 7, 123, 999), no DB/catalog gaps.
- No golden-baseline impact — all Tier-1 scenarios are non-golden ad hoc QA-gauntlet fixtures; replay-verify does not apply to them.
- No sim-code defects routed — the one non-trivial claim (tier1-patrol-03 side composition) was resolved as a false positive via direct file read + GitNexus impact analysis, not routed to determinism-engineer or database-intelligence-lead.

---

## Next Steps

1. All 4 Tier-1 scenarios are validated PASS (`canExport: true`) and cleared for Phase B batch execution.
2. Proceed to `/qa-gauntlet` Phase B: run the batch harness (`dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch --scenarios tier1-patrol-01,tier1-patrol-02,tier1-patrol-03,tier1-patrol-04 --seeds 42,7,123 --ticks 6 --csv-out production/qa/gauntlet/gauntlet-20260709-1242/tier-1/results.csv`).
3. Commit the tier1-patrol-04 content fix + this report to the QA branch (`07-09-qa_gauntlet_gauntlet-20260709-1242`) per gauntlet autonomy, after `detect_changes()` confirms only scenario/report files are affected.
