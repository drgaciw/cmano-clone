# Sprint 43 Parallel Kickoff — Content Wave 2 + Art Bible Complete

**Date:** 2026-06-20 (planning artifact)  
**Sprint plan:** `production/sprints/sprint-43-content-wave2-art-bible-complete.md`  

> **⛔ PLANNING ONLY** — requires scope gate + S42 complete.

## Sprint Goal

Complete B1 (Engage/features + remainder) and B2 (sections 5–9 + asset specs).

## Wave plan

| Wave | Stories | Track | Est. | Agent env |
|------|---------|-------|------|-----------|
| Day-1 | S43-01 | Baseline | 1d | Cloud |
| W0 | S43-02 | QA plan | 1d | Cloud |
| W1 | S43-03 | Content Engage | 3d | Cloud + determinism |
| W2 | S43-04 | Content remainder | 2.5d | Cloud |
| W3 | S43-05 | Art bible 5–9 | 2.5d | Cloud |
| W4 | S43-07 | Evidence | 1.5d | **Local** |
| W5 | S43-06 | Closeout | 0.5d | Local |

## Track ownership

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| Content Engage | `stack/sprint43/content-engage` | Cloud | S43-03 |
| Content remainder | `stack/sprint43/content-remainder` | Cloud | S43-04 |
| Art bible complete | `stack/sprint43/art-bible-complete` | Cloud | S43-05 |
| Evidence | `stack/sprint43/evidence` | **Local** | S43-07 |
| Closeout | `stack/sprint43/closeout` | Local | S43-06 |

## File ownership matrix

| Path | Owner |
|------|-------|
| `src/**/Engage*` | Content Engage |
| `data/**` (scenario remainder) | Content remainder |
| `design/art/art-bible.md` | Art bible |
| `production/playtests/*` | Evidence |
| `production/qa/evidence/README-s43*` + playtests cadence | Evidence (Beta-Evidence-QA) |
| `production/sprint-status.yaml` + closeout smoke | Closeout coordinator |

**S43-07 (Evidence, Beta-Evidence-QA declarative):** Playtest cadence 12-13 focus; evidence pack update B1 W2 + B2; local Editor if needed; test-evidence-review verdicts (ADEQUATE). Worktree: sprint43-evidence.

**S43-06 (Closeout):** Final smoke (full regression, replay 6/6, proxy); status update; B1+B2 exit noted in sprint-status/kickoff; retrospective. Worktree: sprint43-closeout. c-sharp-devops-engineer + coordinator.

---

**S43 Complete (post closeout):** All gates held (1226/1226, 6/6 replay, 18/18 proxy); B1+B2 exit MET per release-enablement-scope-boundary-2026-06-20.md; S44 dispatch ready. Cites S41 ack packet + S42 closeout. Beta-Evidence-QA + coordinator manifests executed. Parallel S43 summary in smoke-sprint-43-closeout-2026-06-20.md + sprint-status.yaml.

*Planning + execution. Post S42. Cite S41 ack + S42.*
