# QA Plan — Sprint 95 Gauntlet Productization (2026-07-14)

**Sprint:** S95 only  
**Authority:** [`sprint-95-gauntlet-productization.md`](../sprints/sprint-95-gauntlet-productization.md), execute-plan 071426  
**Stage:** **Release** (no Launch)

## Scope

| In scope | Out of scope |
|----------|----------------|
| Expect/CI discipline docs or tools | Launch / store submit |
| Defect-registry hygiene | S94 asset rework as open work |
| Closeout smoke + floor citation | S96–S97 implementation |
| Max-variance / oracle family green language | Addressables bulk |

## Tracks

| Track | Story | Env | QA checks |
|-------|-------|-----|-----------|
| Expect/CI | S95-01 | Cloud | Deliverable path exists; documents tier-tick expect regen + fail-closed CI |
| Defects | S95-02 | Cloud | `gauntlet-defect-registry.json` valid JSON; residual hygiene fields; closed IDs preserved |
| Closeout | S95-03 | **Local** | Smoke; stage Release; ≥1638 cited |

## Test cases

| ID | Type | Case | Pass |
|----|------|------|------|
| QA-95-01 | Static | Expect/CI artifact on disk | `test -f` on published path |
| QA-95-02 | Static | Registry JSON parses | `python3 -c json.load(...)` |
| QA-95-03 | Static | Closed defects retained | GAUNTLET-MD-001 + SYN-T12-001 still closed |
| QA-95-04 | Static | Residual risks documented | open/watched residuals present |
| QA-95-05 | Static | Stage Release | stage.txt starts with Release |
| QA-95-06 | Evidence | Suite floor | ≥1638/0f run **or** last-gate cite |

## Automation

- Prefer docs/fixtures-only; if C#/harness touched → RUN suite ≥1638/0f + GitNexus impact on `BalticReplayHarness`.

---
*QA plan S95 — 2026-07-14*
