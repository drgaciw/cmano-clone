# QA Plan — Sprint 99 Approved Review Queue + Residual Hygiene (2026-07-16)

**Sprint:** S99 only  
**Stage:** **Release** (not Launch)  
**Authority:** [`sprint-99-approved-review-queue-hygiene.md`](../sprints/sprint-99-approved-review-queue-hygiene.md)

## Scope

| Story | Test type | Required evidence |
|-------|-----------|-------------------|
| S99-01 Review queue package | Structural / manual | Package names ASSET-006 + ASSET-021; links S98 pilot; human phrase **template only**; manifest Approved unchanged without real phrase |
| S99-02 Residual hygiene | Logic / tool | `retest-defect.sh GAUNTLET-SYN-T12-001` exit 0; residuals remain `watched` |
| S99-03 Closeout | Structural | Smoke path; floors ≥1638 cited; stage Release |

## Smoke gate (before closeout)

1. Queue package + residual hygiene note exist  
2. Retest closed id PASS (or honest env-failure log)  
3. No fabricated `asset approved:` claims  
4. `production/stage.txt` still **Release**  
5. S97 FULLY CLOSED + S98 complete preserved in yaml  

## Out of scope for QA this sprint

- Commercial Launch checklist  
- Full suite re-run (unless C# lands)  
- Store submission validation  

---
*QA plan S99 — 2026-07-16. Lean /sprint-plan open.*
