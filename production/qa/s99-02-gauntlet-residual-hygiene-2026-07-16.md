# S99-02 Gauntlet Residual Hygiene — 2026-07-16

**Sprint:** S99 · Story **S99-02**  
**Stage:** **Release** (not Launch)  
**Registry:** [`gauntlet-defect-registry.json`](gauntlet-defect-registry.json)  
**Prior disposition:** [`s98-02-gauntlet-residual-disposition-2026-07-16.md`](s98-02-gauntlet-residual-disposition-2026-07-16.md)  
**Retest:** `tools/qa-gauntlet/retest-defect.sh`  
**Expect ops:** [`tools/qa-gauntlet/README-expect-regen.md`](../../tools/qa-gauntlet/README-expect-regen.md) · [`gauntlet-expect-ci-discipline-2026-07-14.md`](gauntlet-expect-ci-discipline-2026-07-14.md)

## Closed defects — retest (this sprint)

| ID | Status | Dual retest | priorFailureMode | Result |
|----|--------|-------------|------------------|--------|
| GAUNTLET-SYN-T12-001 | **closed** (retained) | exit 0 ×2 | `CATALOG_UNIT:u1:` | **PASS** — `PRIOR_FAILURE_ABSENT`; oracle `allPassed: true` |
| GAUNTLET-MD-001 | **closed** (retained) | corroboration exit 0 | `hostile-1` | **PASS** — `PRIOR_FAILURE_ABSENT` |

Closed IDs **not** reopened. Registry `status: closed` unchanged.

### Dual-run observables (GAUNTLET-SYN-T12-001)

| Run | SCRATCH out-dir | Exit | retest_status | PRIOR | CSV | meta.ok |
|-----|-----------------|------|---------------|-------|-----|---------|
| 1 | `gauntlet-retest/` | 0 | PASS | ABSENT | non-empty | true |
| 2 | `gauntlet-retest-run2/` | 0 | PASS | ABSENT | non-empty | true |

Scenario/seed/ticks: `gauntlet-t1-patrol-b` / 42 / 6 per registry.

## Residuals — re-disposition (no fake-close)

| ID | Status | S99 disposition | Notes |
|----|--------|-----------------|-------|
| GAUNTLET-RES-EXPECT-001 | watched | **still watched** | Fail-closed expect discipline holds; no envelope regen required (oracle green) |
| GAUNTLET-RES-T5-001 | watched | **still watched** | T5 not elevated to hard product gate |
| GAUNTLET-RES-GHA-001 | watched | **still watched** | Local retest remains proof when GHA billing-gated |
| GAUNTLET-RES-BRH-001 | watched | **still watched** | No CRITICAL C# / BalticReplayHarness touch this sprint |
| GAUNTLET-RES-WORKTREE-001 | watched | **still watched** | Nested + standalone sync discipline retained |

**Rule:** Do not set residual `status: closed` without verified fix + retest trail.

### Registry hygiene (post-S99)

- Closed: **2** retained  
- Residuals: **5** all **watched**  
- Fake-close: **none**

## S99-04 Expect discipline pointer (Should Have)

| Check | Result |
|-------|--------|
| README-expect-regen | **PRESENT** |
| Discipline doc | **PRESENT** |
| One-tier expect regen | **Not required** — dual retest/oracle green |

---
*S99-02 residual hygiene — 2026-07-16. Stage Release. Residuals watched, not fake-closed.*
