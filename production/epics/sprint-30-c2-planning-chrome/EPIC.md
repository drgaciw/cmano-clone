# Epic: Sprint 30 — C2 Planning Chrome

> **Status:** Ready  
> **Sprint:** 30  
> **Dates:** 2026-10-16 → 2026-10-29  
> **Trunk:** `main` @ `3406bc4` (878/878; ReplayGolden 6/6)  
> **Layer:** Presentation  
> **GDD / UX:** `design/ux/c2-command-post.md`; Req 02 Core Loop, Req 20 Command and Control UI

## Goal

Close **S29 lean-mode presentation debt** and complete **Planning-phase UX chrome**: capture Editor/PlayMode evidence for import staging, doctrine panel, and Begin Execution (S30-06, closes S29-04/07/08 QA conditions) and wire **Planning-state chrome** — map dimmed, drawer read-only while `SimulationPhase.Planning` (S30-07, extends S29-08) — headless tests remain merge authority; ZERO touch `DelegationBridge`.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-010 | Accepted | Headless-first; read-only projections; panel host seams |
| ADR-001 | Accepted | Deterministic phase transitions (Planning → Executing) |

## Graphite Stack (merge order)

```
main
 └── stack/sprint30/full-sln-gate              (S30-01 — shared day-1)
      ├── stack/sprint30/presentation-evidence (S30-06)
      └── stack/sprint30/planning-chrome       (S30-07)
```

**Dependency:** S30-06 and S30-07 can run in parallel after S30-01.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 06 | [Editor presentation evidence batch](story-030-06-presentation-evidence.md) | S30-06 | Visual / UI | should-have | 1d | Not Started |
| 07 | [Begin Execution planning chrome](story-030-07-planning-chrome.md) | S30-07 | UI+Integration | should-have | 1.5d | Not Started |

Note: **S30-01** day-1 baseline lives in `sprint-30-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **ZERO touch:** `DelegationBridge.cs`
- Planning chrome is **read-only projection + host binding** — no direct `Orchestrator` mutation from UI
- Begin Execution routes through `DelegationBridgeHost.BeginExecution()` only (S29-08 seam preserved)
- Doctrine writes route `DoctrineInheritancePanelHost` → `DelegationBridgeHost.TrySetDoctrineOverride` only
- Import UI writes route `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only (no write-gate bypass)

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 3 | S30-07 (planning chrome) | S30-04 ship.md approve |
| — | S30-06 (presentation evidence) | S30-05 land validator |

## Definition of Done

- [ ] S30-06 PNG evidence captured under `production/qa/evidence/*-s30-*.png` (import staging, doctrine, Begin Execution)
- [ ] S30-06 signoff script scenarios extended; headless regression filters unchanged PASS
- [ ] S30-07 `C2PlanningChromeTests` PASS; score/loss freeze regression PASS
- [ ] Map dimmed + drawer read-only while `SimulationPhase.Planning` per UX spec §5
- [ ] Headless regression filters unchanged PASS
- [ ] Tracker rows 02/20 progress notes updated

## References

- UX spec: `design/ux/c2-command-post.md` (Planning state §5)
- S29 QA sign-off conditions: `production/qa/qa-signoff-sprint-29-2026-06-18.md`
- S29 C2 core loop: `production/epics/sprint-29-c2-core-loop/`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md`
- Parallel kickoff: `production/agentic/sprint-30-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`