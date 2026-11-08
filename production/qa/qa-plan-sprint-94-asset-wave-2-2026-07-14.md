# QA Plan — Sprint 94 Asset Wave 2 (2026-07-14)

**Sprint:** S94 — Asset wave 2 + Approved path  
**Authority:** [`sprint-94-asset-wave-2.md`](../sprints/sprint-94-asset-wave-2.md), [`roadmap-execution-plan-071426.md`](../../docs/reports/roadmap-execution-plan-071426.md)  
**Stage:** **Release** (no Launch)

## Scope

| In scope | Out of scope |
|----------|----------------|
| Manifest honesty (Specced→Done only when files exist) | Addressables bulk import |
| Placeholder USS/PNG/MD under `production/assets/{c2,baltic,store}/` | Store upload / commercial submit |
| Approved criteria doc | C# hotpath / DelegationBridge |
| Closeout smoke doc | S95–S97 work |
| Stage remains Release | Hash change / Baltic reopen |

## Tracks under test

| Track | Story | Env | What QA checks |
|-------|-------|-----|----------------|
| C2 children | S94-01 | Cloud | ASSET-006 (or agreed C2 child) **Done** + path exists |
| Baltic + store | S94-02 | Cloud | ≥1 Baltic **or** store child **Done** + path exists |
| Closeout | S94-03 | **Local** | Approved criteria present; stage Release; smoke closeout |

## Test cases

| ID | Type | Case | Pass |
|----|------|------|------|
| QA-94-01 | Visual/Static | C2 Done artifact on disk | `test -f production/assets/c2/<file>` |
| QA-94-02 | Visual/Static | Baltic or store Done artifact on disk | `test -f production/assets/baltic/*` or `store/*` new file |
| QA-94-03 | Static | Manifest status matches disk | Done rows cite real paths |
| QA-94-04 | Static | Approved criteria defines Done→Approved | Doc non-empty; criteria table present |
| QA-94-05 | Static | Stage Release | `production/stage.txt` starts with Release / no Launch flip this sprint |
| QA-94-06 | Static | No Launch / Addressables / store submit in sprint docs | Grep sprint plan + kickoff |

## Automation / suite

- **Assets/docs only:** full suite **not** required; cite last gate evidence (**≥1638/0f**, Replay 6/6, C2 ≥20/20, hash).
- **If C# touched:** RUN+READ full suite ≥1638/0f before closeout.

## Sign-off

QA sign-off for S94 = smoke closeout APPROVED WITH CONDITIONS if placeholders used (document quality bar). Full Approved column remains **0** until human art review.

---
*QA plan S94 — 2026-07-14*
