# AAR — QA Gauntlet `gauntlet-20260716-2346`

**Date (UTC):** 2026-07-17T04:49:55Z  
**SHA:** `d98e7ef`  
**Branch:** `qa/gauntlet-gauntlet-20260716-2346`  
**Mode:** Shipped-policy ladder (catalog-grounded inventory) × seeds `42,7,123`  
**Skill:** `/qa-gauntlet` + `dispatching-parallel-agents` (preflight ∥ ladder waves)

## Preflight

| Gate | Result |
|------|--------|
| GitNexus re-analyze | **PASS** — index refreshed (26,081 nodes / 49,869 edges) |
| `dotnet build -c Release` | **PASS** — 0 errors / 0 warnings |
| Catalog `baltic_patrol.db` | **PASS** — 79 platforms, 423 magazines |
| Full suite | **PASS** — **1699** total (Sim 321 + Del 281 + UA 324 + Excel 24 + Data 642 + Cli 107), **0 failed** (≥1638 floor) |
| Replay filter | No tests matched `~Replay` name filter in this assembly layout; suite includes sim determinism coverage via full green suite |
| Catalog/policy inventory | Shipped `gauntlet-*.policy.json` ladder used (Phase A regen skipped — inventory already ladder-complete) |

## Ladder results

| Tier | Ticks | Scenarios | Rows (sc×seed) | Oracle |
|------|-------|-----------|----------------|--------|
| 1 | 6 | 4 patrol | 12 | **allPassed** |
| 2 | 10 | 4 escort/strike | 12 | **allPassed** |
| 3 | 16 | 4 joint/EMCON/ROE | 12 | **allPassed** |
| 4 | 24 | 4 multi/inject | 12 | **allPassed** |
| 5 | 40 | 4 cascade/theater/dynamic/ROE | 12 | **allPassed** |
| extra | 12 | joint-orbat + multidomain | 6 | **allPassed** |

**Total ladder:** 20 scenarios × 3 seeds = **60** batch rows + **6** extra = **66** stable rows. Zero crashes. Zero oracle fails.

## Hindsight retest (closed registry)

| Defect | Result |
|--------|--------|
| GAUNTLET-SYN-T12-001 | **PASS** — `PRIOR_FAILURE_ABSENT: CATALOG_UNIT:u1:` |
| GAUNTLET-MD-001 | **PASS** — `PRIOR_FAILURE_ABSENT: hostile-1` |

Residuals remain **watched** (EXPECT/T5/GHA/BRH/WORKTREE) — not fake-closed.

## Defects

| Class | Count | Notes |
|-------|-------|-------|
| sim-code | 0 | No remediation required |
| scenario-data | 0 | — |
| oracle | 0 | Envelopes from prior recalibration held |
| flaky | 0 | — |
| QUARANTINED-CRITICAL | **0** | — |

## Fixes

None this run (see `fixes.md`).

## Parallel agents

| Wave | Work | Result |
|------|------|--------|
| Preflight | GitNexus analyze + build + suite + catalog | All green |
| Ladder T1→T5 | Sequential Demo batch (shared process) + oracle per tier | All tiers green |
| Hindsight | Closed-id retest SYN-T12 + MD-001 | PASS |
| Extra | Joint ORBAT + multi-domain shooters | allPassed |

## Artifacts

| Path | Content |
|------|---------|
| `manifest.yaml` | Run plan + seeds + tier map |
| `tier-N/` | policies, roster, results.csv, oracle-eval.json, run.log |
| `tier-extra/` | joint + multidomain |
| `hindsight-retest/` | closed-defect retest logs |
| `preflight-*.log` | build/suite/replay |

## Sign-off (qa-lead)

- Baseline suite **1699/0f** ≥ floor **1638**  
- Ladder oracle **allPassed** all tiers  
- Hindsight closed defects **PASS**  
- Stage remains **Release** (gauntlet ops; no Launch)  
- **No `gt submit`** — zero fix commits this run  

## QUARANTINED-CRITICAL

None.

---
*Gauntlet AAR — gauntlet-20260716-2346. Stage Release. Not Launch.*
