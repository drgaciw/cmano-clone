# AAR — QA Gauntlet `gauntlet-20260720-2000` (10x)

**Date (UTC):** 2026-07-21T01:13:56Z  
**SHA (start):** `7caae00`  
**Branch:** `feat/s107-epic-a-panel-runtime-depth`  
**Mode:** Shipped-policy ladder × seeds `42,7,123` + **10× full-ladder flakiness** + CI dry-run  
**Skill:** `/qa-gauntlet` + TDD + `dispatching-parallel-agents` (preflight ∥ ladder; fix sequential)

## Preflight

| Gate | Result |
|------|--------|
| Catalog `baltic_patrol.db` | **PASS** — 79 platforms, 423 magazines |
| `dotnet build -c Release` | **PASS** — 0e/0w |
| Full suite (pre) | **PASS** — **1779**/0f (Sim 321 + Del 318 + UA 360 + Excel 24 + Data 645 + Cli 111) ≥1638 |
| Policy inventory | Shipped `gauntlet-*.policy.json` ladder (Phase A regen skipped) |

## Ladder results (first pass)

| Tier | Ticks | Oracle (first) | After fix |
|------|-------|----------------|-----------|
| 1 | 6 | **allPassed** | — |
| 2 | 10 | **allPassed** | — |
| 3 | 16 | **allPassed** | — |
| 4 | 24 | **allPassed** | — |
| 5 | 40 | **FAIL** `t5-roe-change` | **allPassed** |
| extra | 12 | **allPassed** | — |

## 10× flakiness

| Metric | Value |
|--------|-------|
| Iterations | 10 full ladders (T1–T5 + extra) |
| Pass | **10** |
| Fail | **0** |
| Flaky | **0** |

## Defects

| ID | Class | Status | Notes |
|----|-------|--------|-------|
| GAUNTLET-ORACLE-T5-ROE-001 | oracle | **fixed** | Dual-profile expect (ladder + expectCi); see `fixes.md` |
| QUARANTINED-CRITICAL | — | **0** | — |

## Hindsight retest

| Defect | Result |
|--------|--------|
| GAUNTLET-SYN-T12-001 | **PASS** |
| GAUNTLET-MD-001 | **PASS** |

## CI dry-run

`gauntlet_oracle_eval --profile ci` @ ticks=10 seed=42 on 9 fixture policies: **allPassed**.

## Suite (post-fix)

**1782** / 0 failed (Data +3 dual-profile tests). Monotonic ≥ baseline.

## Sign-off

- Ladder oracle green after dual-profile fix  
- 10× ladder stable  
- Stage remains **Release** (no Launch)  
- Residual EXPECT-001 partially mitigated by dual-profile machinery  

## Artifacts

| Path | Content |
|------|---------|
| `manifest.yaml` | Run plan |
| `tier-N/` | results + oracle-eval |
| `x10/` | 10 iteration ladder runs |
| `ci-dry-run/` | CI profile dry-run |
| `hindsight-retest/` | closed defect retests |
| `fixes.md` | TDD fix log |
| `oracle-expect-recalibration-t5-roe-dual-profile.json` | audit |
