# Epic: Sprint 31 — Presentation Polish

> **Status:** Ready  
> **Sprint:** 31  
> **Dates:** 2026-10-30 → 2026-11-12  
> **Trunk:** `main` @ `3406bc4` (956/956; ReplayGolden 6/6; S30 QA APPROVED)  
> **Layer:** Presentation  
> **GDD / UX:** `design/ux/c2-command-post.md`; Req 02 Core Loop, Req 20 Command and Control UI, Req 21 Platform Editor

## Goal

Close **S30 lean presentation debt** by replacing protocol placeholder PNGs with **live Editor/PlayMode evidence** (S31-07) and refreshing the **C2 manual sign-off checklist** for import staging, doctrine inheritance, and Begin Execution (S31-08). Headless tests remain merge authority; ZERO touch `DelegationBridge`.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-010 | Accepted | Headless-first; read-only projections; panel host seams |
| ADR-011 | Accepted | Platform import write-gate; staging review before approve |
| ADR-001 | Accepted | Deterministic phase transitions (Planning → Executing) |

## Graphite Stack (merge order)

```
main
 └── stack/sprint31/full-sln-gate              (S31-01 — shared day-1)
      ├── stack/sprint31/presentation-evidence (S31-07)
      └── stack/sprint31/c2-signoff-refresh    (S31-08)
```

**Dependency:** S31-08 depends on S31-07 evidence package. S31-07 can start after S31-01.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 07 | [Live Editor presentation evidence](story-031-07-presentation-evidence.md) | S31-07 | Visual / UI | should-have | 1d | Not Started |
| 08 | [C2 manual sign-off refresh](story-031-08-c2-signoff-refresh.md) | S31-08 | Config | should-have | 1d | Not Started |

Note: **S31-01** day-1 baseline lives in `sprint-31-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **ZERO touch:** `DelegationBridge.cs`
- Presentation evidence is **capture-only** — no production logic changes unless signoff script scenarios require extension
- Begin Execution routes through `DelegationBridgeHost.BeginExecution()` only (S29-08 seam preserved)
- Doctrine writes route `DoctrineInheritancePanelHost` → `DelegationBridgeHost.TrySetDoctrineOverride` only
- Import UI writes route `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only (no write-gate bypass)

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 3 | S31-08 (C2 sign-off refresh) | S31-02 sensor approve |
| — | S31-07 (presentation evidence) | S31-05 facility hot-tick |

## Definition of Done

- [ ] S31-07 live PNG evidence under `production/qa/evidence/*-s31-*.png` replaces S30 protocol placeholders
- [ ] S31-07 signoff script scenarios (`import`, `begin-execution`) produce clean batch log when Editor available
- [ ] S31-08 `c2-manual-signoff-*.md` extended for import staging, doctrine, Begin Execution checks
- [ ] Headless regression filters unchanged PASS (`PlatformImport|Doctrine|C2TopBar|PlayModeSmoke`)
- [ ] Lean PASS WITH NOTES documented if no Unity Editor host
- [ ] Tracker rows 02/20 progress notes updated

## References

- UX spec: `design/ux/c2-command-post.md` (Planning state §5)
- S30 presentation evidence: `production/epics/sprint-30-c2-planning-chrome/story-030-06-presentation-evidence.md`
- S30 protocol placeholders: `production/qa/sprint-30-presentation-evidence-2026-06-18.md`
- C2 checklist baseline: `production/qa/c2-manual-signoff-2026-06-02.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md`
- Parallel kickoff: `production/agentic/sprint-31-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md` *(create before implementation)*