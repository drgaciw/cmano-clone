# Forge promote log — gauntlet-20260723-1543

Branch: `cursor/gauntlet-expanded-orbats-5949`

## Phase `pre` (2026-07-23T15:43Z)

- Hindsight recall: skipped (server not verified in cloud VM)
- Loaded corpus: coverage-map (20 cells, 24 scenarios), recipe-weights, index.yaml
- Emitted `forge/mid-tier-plan.yaml` for tier 1
- Top recipes: `hard-case-replay` (1.3), `platform-swap-underused` (1.2)
- Underused platforms: mig-31-foxhound, borei-ii, kilo, type-212a, akula (count=1)
- Tier-1 ladder scenarios: patrol-a/b/c/d from `data/scenarios/`

## Phase `a0` (2026-07-23T15:45Z)

- Roster: copied from prior expanded-ORBAT tier-1 roster (26 catalog platforms)
- Ephemeral candidate: `gauntlet-t1-patrol-b-candidate-underused.policy.json`
  - Recipe: `platform-swap-underused`
  - Added rare platform hint: `mig-31-foxhound`
  - Location: `forge/candidates/` (gitignored)
- No promotion — candidate requires batch+oracle at tier ticks before scorecard promote

## Phase `post-oracle` (2026-07-23T15:46Z)

- Batch: 12 rows (4 scenarios × 3 seeds) @ T1 ticks=6 → `tier-1/results.csv`
- Oracle CLI: exit 0, `allPassed=true` for patrol-a/b/c/d
- Scorecard: `python3 tools/qa-gauntlet/forge_scorecard.py --run-dir … --tier 1`
  - Candidate `gauntlet-t1-patrol-b-candidate-underused`: **DISCARD** (oracle unknown — not batched)
  - Tier policy copies: promote=0 (duplicate intent — already in corpus/index)
- TDD fix applied: `read_oracle_passed` now parses list-shaped `scenarios[]` from CLI output (no erroneous `allPassed` wildcard)
- pytest: 11/11 pass (`test_read_oracle_passed_list_scenarios_no_wildcard`)

## Phase `e` (2026-07-23T15:47Z)

- Up-weight intent: `platform-swap-underused` produced pressure signal (rare platform hit)
- Next-tier plan: defer tier 2 forge run; hard-case replays none this cycle
- Stuck families: none (1 discard on candidate — below stuck threshold of 5)

## Phase `final` (2026-07-23T15:47Z)

- Promotions committed: **0** (correct — no new oracle-validated winners)
- Corpus unchanged (cell count 20 stable)
- Coverage gaps remain: ASW T1-T3, event-chain below T3, id-roe single T3 cell
- Invariants: DelegationBridge zero-touch; replay hash `17144800277401907079` preserved
- GitNexus detect_changes vs main: 132 files, 95 symbols, **0 affected processes**, risk **low**

## Summary

| Metric | Value |
|--------|-------|
| Tier 1 oracle | 4/4 PASS |
| Candidates drafted | 1 |
| Promoted | 0 |
| Discarded | 1 (candidate — no batch oracle) |
| sim-code fixes | 1 (forge_scorecard list-oracle parse) |
| pytest forge suite | 11/11 |
