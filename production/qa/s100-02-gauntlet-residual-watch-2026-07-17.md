# S100-02 Gauntlet Residual Watch — 2026-07-17

**Sprint:** S100 · Story **S100-02**  
**Stage:** **Release**  
**Registry:** [`gauntlet-defect-registry.json`](gauntlet-defect-registry.json)  
**Prior:** S99-02 hygiene · S98 disposition  

## Closed-id retest (this sprint)

| ID | Dual/corroboration | Result |
|----|--------------------|--------|
| GAUNTLET-SYN-T12-001 | dual exit 0 | **PASS** — `PRIOR_FAILURE_ABSENT: CATALOG_UNIT:u1:` |
| GAUNTLET-MD-001 | corroboration | **PASS** — `PRIOR_FAILURE_ABSENT: hostile-1` |

Closed IDs retained closed. Not reopened.

## Residuals — still watched (no fake-close)

| ID | Disposition |
|----|-------------|
| GAUNTLET-RES-EXPECT-001 | **still watched** |
| GAUNTLET-RES-T5-001 | **still watched** |
| GAUNTLET-RES-GHA-001 | **still watched** |
| GAUNTLET-RES-BRH-001 | **still watched** |
| GAUNTLET-RES-WORKTREE-001 | **still watched** |

## S100-06 Expect discipline (Nice to Have)

| Check | Result |
|-------|--------|
| `tools/qa-gauntlet/README-expect-regen.md` | PRESENT |
| `production/qa/gauntlet-expect-ci-discipline-2026-07-14.md` | PRESENT |
| One-tier expect regen | **Not required** — retest/oracle green |

---
*S100-02 residual watch — 2026-07-17. Residuals watched.*
