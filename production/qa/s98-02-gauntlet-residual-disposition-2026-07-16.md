# S98-02 Gauntlet Residual Disposition — 2026-07-16

**Sprint:** S98 · Story **S98-02**  
**Stage:** **Release** (not Launch)  
**Registry:** [`production/qa/gauntlet-defect-registry.json`](gauntlet-defect-registry.json)  
**Retest tool:** `tools/qa-gauntlet/retest-defect.sh`  
**Operator runbook:** [`tools/qa-gauntlet/README-expect-regen.md`](../../tools/qa-gauntlet/README-expect-regen.md) · [`gauntlet-expect-ci-discipline-2026-07-14.md`](gauntlet-expect-ci-discipline-2026-07-14.md)

## Closed defects (retest)

| ID | Status | Retest (this sprint) | priorFailureMode | Result |
|----|--------|----------------------|------------------|--------|
| GAUNTLET-SYN-T12-001 | **closed** (retained) | Dual run exit 0 | `CATALOG_UNIT:u1:` | **PASS** — `PRIOR_FAILURE_ABSENT`; oracle `allPassed: true` |
| GAUNTLET-MD-001 | **closed** (retained) | Corroboration exit 0 | `hostile-1` | **PASS** — `PRIOR_FAILURE_ABSENT` |

Closed IDs were **not** reopened. Registry `status: closed` unchanged.

### Dual-run observables (GAUNTLET-SYN-T12-001)

| Run | Out-dir (SCRATCH) | Exit | retest_status | PRIOR | CSV non-empty | meta.ok |
|-----|-------------------|------|---------------|-------|---------------|---------|
| 1 | `gauntlet-retest/` | 0 | PASS | ABSENT `CATALOG_UNIT:u1:` | yes (2431 B) | true |
| 2 | `gauntlet-retest-run2/` | 0 | PASS | ABSENT | yes (2431 B) | true |

Scenario/seed/ticks per registry: `gauntlet-t1-patrol-b` / 42 / 6.

## Residuals — disposition (no fake-close)

| ID | Prior status | Disposition (S98) | Rationale |
|----|--------------|-------------------|-----------|
| GAUNTLET-RES-EXPECT-001 | watched | **still watched** | Expect recalibration drift remains process risk; discipline docs + fail-closed CI contract hold. No silent envelope rewrite this sprint. |
| GAUNTLET-RES-T5-001 | watched | **still watched** | T5 discriminative weakness unchanged; not elevated to hard product gate. |
| GAUNTLET-RES-GHA-001 | watched | **still watched** | GHA billing-gated CI — local retest/oracle remains proof path when Actions blocked. |
| GAUNTLET-RES-BRH-001 | watched | **still watched** | BalticReplayHarness CRITICAL — no C# touch this sprint; impact-first rule retained. |
| GAUNTLET-RES-WORKTREE-001 | watched | **still watched** | Dual-worktree land discipline still applies (nested + standalone sync used for S98 docs). |

**Rule:** Residuals stay `status: watched` in registry. **Do not** set closed without a verified fix + retest trail.

### Registry hygiene check (post-S98)

- Closed count: **2** (`GAUNTLET-MD-001`, `GAUNTLET-SYN-T12-001`)  
- Residual count: **5** all **watched**  
- Fake-close: **none**

## S98-04 Expect discipline smoke (Should Have — light)

| Check | Result |
|-------|--------|
| README-expect-regen present | **PASS** — `tools/qa-gauntlet/README-expect-regen.md` |
| Discipline doc present | **PASS** — `production/qa/gauntlet-expect-ci-discipline-2026-07-14.md` |
| One-tier expect regen | **Not required** — retest/oracle green; no envelope drift evidenced |

## Non-goals confirmed

- No oracle ADR opened  
- No max-variance ladder re-run  
- No Launch / commercial gate  

---
*S98-02 residual disposition — 2026-07-16. Stage Release. Residuals watched, not fake-closed.*
