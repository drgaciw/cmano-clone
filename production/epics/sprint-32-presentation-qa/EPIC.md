# Epic: Sprint 32 — Presentation QA

> **Status:** Ready  
> **Sprint:** 32  
> **Dates:** 2026-11-13 → 2026-11-26  
> **Trunk:** `main` @ `3406bc4` (1006/1006; ReplayGolden 6/6; S31 QA APPROVED)  
> **Layer:** QA / Presentation  
> **GDD / UX:** `design/ux/c2-command-post.md`; Req 20 Command and Control UI, Req 21 Platform Editor

## Goal

Upgrade **C2 manual sign-off** post-S31: re-run checklist checks 14–16 with **S32-10 live evidence** and **S32-06 Phase F damage surfacing**, upgrading S31 PASS WITH NOTES where live captures exist.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-010 | Accepted | Headless-first; read-only projections; panel host seams |
| ADR-011 | Accepted | Platform import write-gate; staging review before approve |

## Graphite Stack (merge order)

```
main
 └── stack/sprint32/full-sln-gate              (S32-01 — shared day-1)
      ├── stack/sprint32/platform-phase-f-damage (S32-06)
      ├── stack/sprint32/presentation-evidence   (S32-10)
      └── stack/sprint32/c2-signoff-upgrade    (S32-11)
```

**Dependency:** S32-11 blocked on S32-06 and S32-10.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 11 | [C2 manual sign-off upgrade](story-032-11-c2-signoff-upgrade.md) | S32-11 | Config | should-have | 1d | Not Started |

## GitNexus Mandatory Rules

- **ZERO touch:** `DelegationBridge.cs`
- Sign-off is **evidence + checklist only** — no production logic changes
- Headless tests remain merge authority

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 5 | S32-11 (C2 live upgrade) | S32-07 release diff CLI |

## Definition of Done

- [ ] S32-11 `c2-manual-signoff-*.md` updated with post-S32 build SHA + verdict
- [ ] Checks 14–16 re-run; upgraded from S31 PASS WITH NOTES when S32-10 live evidence exists
- [ ] Evidence doc: `production/qa/sprint-32-c2-signoff-*.md`
- [ ] Lean PASS WITH NOTES documented if no Unity Editor host
- [ ] Tracker rows 02/20 progress notes updated

## References

- C2 checklist baseline: `production/qa/c2-manual-signoff-2026-06-02.md`
- S31-08 pattern: `production/epics/sprint-31-presentation-polish/story-031-08-c2-signoff-refresh.md`
- S32-10 dependency: `production/epics/sprint-32-platform-editor-phase-f/story-032-10-presentation-evidence.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md`
- Parallel kickoff: `production/agentic/sprint-32-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*