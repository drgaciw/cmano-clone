# Epic: Sprint 29 — C2 Core Loop

> **Status:** Ready  
> **Sprint:** 29  
> **Dates:** 2026-10-02 → 2026-10-15  
> **Trunk:** `main` @ `1d93e86` (801/801; ReplayGolden 6/6)  
> **Layer:** Presentation  
> **GDD / UX:** `design/ux/c2-command-post.md`; Req 02 Core Loop, Req 13 Doctrine

## Goal

Close deferred **C2 core-loop UX gates**: **Doctrine Inheritance Panel** visual sign-off (S29-07, closes S22/S23 deferred gate) and **Begin Execution** Planning→Executing control in C2 top bar (S29-08) — headless tests remain merge authority; ZERO touch `DelegationBridge`.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-010 | Accepted | Doctrine panel seam; headless-first UI |
| ADR-001 | Accepted | Deterministic phase transitions |

## Graphite Stack (merge order)

```
main
 └── stack/sprint29/full-sln-gate              (S29-01 — shared day-1)
      ├── stack/sprint29/doctrine-visual       (S29-07)
      └── stack/sprint29/begin-execution       (S29-08)
```

**Dependency:** S29-07 and S29-08 can run in parallel after S29-01.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 07 | [Doctrine Inheritance Panel visual sign-off](story-029-07-doctrine-visual.md) | S29-07 | UI | should-have | 1d | Not Started |
| 08 | [Begin Execution UX](story-029-08-begin-execution.md) | S29-08 | UI+Integration | should-have | 1d | Not Started |

Note: **S29-01** day-1 baseline lives in `sprint-29-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **ZERO touch:** `DelegationBridge.cs`
- Doctrine writes route `DoctrineInheritancePanelHost` → `DelegationBridgeHost.TrySetDoctrineOverride` only
- Begin Execution routes through `DelegationBridgeHost.BeginExecution()` — no direct `Orchestrator` mutation from UI

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 2 | S29-08 (Begin Execution) | S29-06 catalog sim consumption |
| 3 | S29-07 (doctrine visual) | S29-04 import UI |

## Definition of Done

- [ ] S29-07 evidence captured (Editor/PlayMode or lean proxy doc)
- [ ] S29-08 phase transition tests PASS; score/loss counters frozen until execution
- [ ] Headless regression filters unchanged PASS
- [ ] Tracker rows 02/13 progress notes updated

## References

- UX spec: `design/ux/c2-command-post.md`
- S22 doctrine pattern: `production/agentic/sprint-22-pr-description-2026-06-17.md`
- S23-U01 plan: `production/agentic/sprint-23-plan-unity-2026-06-17.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md`
- Parallel kickoff: `production/agentic/sprint-29-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- Track plan: `production/agentic/sprint-29-plan-unity-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*