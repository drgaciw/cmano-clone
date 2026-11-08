# Epic: Sprint 32 — Platform Editor Phase F

> **Status:** Ready  
> **Sprint:** 32  
> **Dates:** 2026-11-13 → 2026-11-26  
> **Trunk:** `main` @ `3406bc4` (1006/1006; ReplayGolden 6/6; S31 QA APPROVED)  
> **Layer:** Presentation / Data Authoring  
> **GDD / UX:** Req 21 Platform Editor; ADR-011 write-gate governance

## Goal

Land **Platform Editor Phase F** — damage workbook surfacing in Unity (S32-06 must-have, **sprint gate**) and refresh **live Editor presentation evidence** for damage/import flows (S32-10). Headless tests remain merge authority; ZERO touch `DelegationBridge`.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-011 | Accepted | Platform import write-gate; staging review before approve; damage workbook columns |
| ADR-010 | Accepted | Headless-first; panel host seams |

## Graphite Stack (merge order)

```
main
 └── stack/sprint32/full-sln-gate              (S32-01 — shared day-1)
      ├── stack/sprint32/platform-phase-f-damage (S32-06) — SPRINT GATE
      └── stack/sprint32/presentation-evidence   (S32-10)
```

**Dependency:** S32-10 depends on S32-06 damage viewer surfacing. S32-06 blocked on S32-01.

Note: **S32-01** day-1 baseline lives in `sprint-32-closeout-devops` epic (shared gate).

## Sprint Gate (S32-06)

**Sprint fails** if S32-06 does not surface damage workbook round-trip in Unity (viewer columns + staging diff + headless propose→approve).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 06 | [Platform Editor Phase F — damage Unity surfacing](story-032-06-platform-phase-f-damage.md) | S32-06 | Integration + Visual / UI | must-have | 2d | Complete |
| 10 | [Live Editor presentation evidence](story-032-10-presentation-evidence.md) | S32-10 | Visual / UI | should-have | 1.5d | Not Started |

## GitNexus Mandatory Rules

- **ZERO touch:** `DelegationBridge.cs`
- **CRITICAL extend-only:** `CatalogWriteGate` (if data path touched)
- **HIGH:** `PlatformCatalogViewerHost`, `PlatformWorkbookWriteBridge`, `PlatformImportPanelHost`
- Import UI writes route `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only (no write-gate bypass)
- Read-only viewer + staging diff only — no new migrations

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 2 | S32-10 (live Editor — lean placeholders) | S32-06 Phase F |

## Definition of Done

- [x] S32-06 complete (**sprint gate**); `PlatformCatalogViewerHost` shows damage fields; staging diff for `MaxHp` edits; headless propose→approve tests PASS
- [ ] S32-10 live PNG evidence under `production/qa/evidence/*-s32-*.png` (or lean PASS WITH NOTES)
- [ ] Headless regression filters unchanged PASS (`PlatformImport|PlatformCatalogViewer`)
- [ ] Tracker row 21 progress note updated

## References

- S28-07 pattern: `production/epics/sprint-28-platform-editor-write/story-028-07-viewer-export-hook.md`
- S31-07 pattern: `production/epics/sprint-31-presentation-polish/story-031-07-presentation-evidence.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md`
- Parallel kickoff: `production/agentic/sprint-32-parallel-kickoff-2026-06-18.md`
- Track plan: `production/agentic/sprint-32-plan-unity-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*