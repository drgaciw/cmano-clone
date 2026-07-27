# QA Gauntlet AAR — gauntlet-20260723-1543

**Date:** 2026-07-23  
**Branch:** `cursor/gauntlet-expanded-orbats-5949`  
**Run type:** Forge-integrated validation (tier 1 focus)

## Ladder results

| Tier | Scenarios | Oracle | Defects | Status |
|------|-----------|--------|---------|--------|
| 1 | patrol-a/b/c/d | 4/4 PASS | 0 sim-code | GREEN |

Baseline test count: **1728** (unchanged). ReplayGolden filter: **17/17 PASS**.

## Forge lifecycle

All required forge phases exercised: `pre` → `a0` → `post-oracle` → `e` → `final`.

Evidence: [`forge/promote-log.md`](forge/promote-log.md)

## Defect class counts

| Class | Count |
|-------|-------|
| `sim-code` fixed | 1 |
| `oracle` recalibrated | 0 |
| `scenario-data` | 0 |
| `flaky` | 0 |
| quarantined | 0 |

### sim-code fix

- **FORGE-SCORECARD-01:** `read_oracle_passed` mis-parsed CLI list-shaped `scenarios[]`, falling back to `allPassed` wildcard and falsely promoting unbatched candidates.
- **TDD:** RED `test_read_oracle_passed_list_scenarios_no_wildcard` → GREEN fix in `forge_scorecard.py`.
- **Verify:** pytest 11/11; scorecard re-run promote=0 discard=1 for candidate.

## GitNexus detect_changes (compare → main)

```
Changes: 132 files, 95 symbols
Affected processes: 0
Risk level: low
```

## Forge promote summary

- Promotions: 0 (tier policies duplicate corpus; candidate discarded without batch oracle)
- Next recommended forge work: batch+oracle ephemeral candidates before post-oracle scorecard; ASW T1 cell gap

## Hard gates

- [x] `forge/promote-log.md` exists and linked
- [x] Oracle CLI script-first (exit 0)
- [x] `forge_scorecard.py` authoritative (not hyphen-only CLI)
- [x] Replay hash preserved
- [x] DelegationBridge zero-touch
