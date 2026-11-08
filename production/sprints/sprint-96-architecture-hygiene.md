# Sprint 96 — Architecture / Docs Hygiene

**Dates:** 2026-07-15 → 2026-07-21 (est. 3–5 days)  
**Lead:** architect / producer (local closeout)  
**Program:** S94–S97 Release Continuity — **S96 only** (Architecture / docs hygiene)  
**Authority:** [`docs/reports/roadmap-execution-plan-071426.md`](../../docs/reports/roadmap-execution-plan-071426.md), [`docs/reports/future-sprint-roadmap-07142026.md`](../../docs/reports/future-sprint-roadmap-07142026.md), [`architecture-review-post-s93-2026-07-14.md`](../../docs/architecture/architecture-review-post-s93-2026-07-14.md), [`critical-hub-merge-playbook-2026-07-14.md`](../agentic/critical-hub-merge-playbook-2026-07-14.md)

**Predecessor:** **S95 COMPLETE** — gauntlet expect/CI discipline + defect-registry hygiene; smoke `smoke-sprint-95-closeout-2026-07-14.md`; suite **≥1638** cited; stage **Release**.  
**QA plan:** [`production/qa/qa-plan-sprint-96-architecture-hygiene-2026-07-15.md`](../qa/qa-plan-sprint-96-architecture-hygiene-2026-07-15.md)  
**Parallel kickoff:** [`production/agentic/sprint-96-parallel-kickoff-2026-07-15.md`](../agentic/sprint-96-parallel-kickoff-2026-07-15.md)

## Sprint Goal

Close architecture-doc freshness gap: promote or re-matrix Draft `architecture.md` against post-S93 + gauntlet + editor completion, and enforce CRITICAL hub merge playbook discoverability in `AGENTS.md`. **No Launch. Does not implement S97.**

## Tracks

| Track | Stack prefix | Env | Story | Owner |
|-------|--------------|-----|-------|-------|
| Architecture promote / re-matrix | `stack/sprint96/arch-docs` | Cloud | S96-01 | architect |
| AGENTS hub playbook + closeout | `stack/sprint96/agents-hub` → Local closeout | Cloud then **Local** | S96-02 / S96-03 | producer |

## Must Have

| ID | Task | Acceptance Criteria |
|----|------|---------------------|
| S96-01 | Architecture freshness | `docs/architecture/architecture.md` status/date/post-S93+gauntlet context **or** dated re-matrix report under `docs/architecture/` |
| S96-02 | Hub playbook in AGENTS | `AGENTS.md` cites `critical-hub-merge-playbook-2026-07-14.md` + watchlist hubs / impact-first |
| S96-03 | Closeout | Smoke; stage **Release**; sprint-status S96; execute-plan S96 `[x]` |

## Hard Gates

| Gate | Pass |
|------|------|
| Scope | Docs only preferred |
| Suite | Cite **≥1638/0f** if no C#; re-run if C# |
| Stage | **Release** |
| Bridge | ZERO hotpath |

## Non-goals

- S95 rework; S97 continuity gate; Launch; full ADR×GDD re-acceptance; C# refactors; DelegationBridge; hash change

---
*Created 2026-07-15 from execute plan 071426 §6 S96.*
<!-- harness workspace copy -->
