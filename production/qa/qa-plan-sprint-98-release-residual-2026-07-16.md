# QA Plan — Sprint 98 Release Residual Backlog (2026-07-16)

**Sprint:** S98 only  
**Stage:** **Release** (not Launch)  
**Authority:** [`sprint-98-release-residual-backlog.md`](../sprints/sprint-98-release-residual-backlog.md)

## Scope

| Story | Test type | Required evidence |
|-------|-----------|-------------------|
| S98-01 Approved-path pilot | Manual / structural | Pilot package lists A1–A7 for 1–2 Done assets; manifest Approved count unchanged unless human `asset approved: ASSET-NNN` recorded |
| S98-02 Gauntlet residual triage | Logic / tool | `retest-defect.sh GAUNTLET-SYN-T12-001` exit 0; residual disposition note; no residual flipped to closed without fix evidence |
| S98-03 Closeout | Structural | Smoke path present; floors **≥1638** cited; stage Release |

## Smoke gate (before closeout)

1. Nested tree paths for pilot package + residual note exist  
2. Retest closed id PASS (or env-failure log if Demo unavailable — not a fake PASS)  
3. `production/stage.txt` still **Release**  
4. S97 yaml still `complete` + human ack provided  

## Out of scope for QA this sprint

- Commercial Launch checklist  
- Full suite re-run (unless C# lands)  
- Store submission validation  

---
*QA plan S98 — 2026-07-16. Lean /sprint-plan open.*
