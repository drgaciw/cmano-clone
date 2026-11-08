# Epic: Sprint 29 — Platform Editor Phase E

> **Status:** Ready  
> **Sprint:** 29  
> **Dates:** 2026-10-02 → 2026-10-15  
> **Trunk:** `main` @ `1d93e86` (801/801; ReplayGolden 6/6)  
> **Layer:** Data + Presentation  
> **GDD / ADR:** ADR-011 Phase E (Unity import UI); Req 21 Platform Editor

## Goal

Land **ADR-011 Phase E** — in-engine Unity import UI (import → propose → approve) atop S28-04 `PlatformWorkbookWriteBridge`, with staging review UX wired — without write-gate bypass or `DelegationBridge` edits.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-011 | Accepted | Phase E Unity import UI atop Phase D write path |
| ADR-010 | Accepted | Headless-first; ZERO touch `DelegationBridge` |
| ADR-006 | Accepted | Engine-free data boundary |

## Graphite Stack (merge order)

```
main
 └── stack/sprint29/full-sln-gate              (S29-01 — shared day-1)
      └── stack/sprint29/corpus-approve        (S29-03 — sprint gate)
           └── stack/sprint29/unity-import-ui   (S29-04) — CRITICAL
```

**Dependency:** S29-04 blocked on S29-03 (nightly approve workflow must land before import UI chrome).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 04 | [Platform Editor Phase E — Unity import UI](story-029-04-unity-import-ui.md) | S29-04 | UI+Integration | must-have | 2.5d | Ready |

Note: **S29-01** day-1 baseline lives in `sprint-29-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `PlatformWorkbookWriteBridge`, `IPlatformWorkbookIo`, `CatalogPlatformBrowseProjection`
- **No bypass:** import UI triggers propose→approve via write bridge only — no direct SQLite or gate skip

## Producer Constraints

1. **ADR-011 Excel-primary** — all writes via workbook + write gate
2. **S28-04 Phase D** — data API must exist; UI chrome is Phase E only
3. **Nightly corpus** — full sensor corpus stays off-CI; approve workflow off-CI

## Definition of Done

- [ ] S29-04 complete (must-have)
- [ ] Headless + viewer tests PASS; staging review UX wired
- [ ] Import→propose→approve round-trip on Baltic fixture
- [ ] GitNexus CRITICAL documented on `CatalogWriteGate`
- [ ] ZERO touch `DelegationBridge.cs`
- [ ] Tracker row 21 Phase E progress note

## References

- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`
- S28-04 pattern: `production/epics/sprint-28-platform-editor-write/story-028-04-excel-write-path.md`
- S28-07 pattern: `production/epics/sprint-28-platform-editor-write/story-028-07-viewer-export-hook.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md`
- Parallel kickoff: `production/agentic/sprint-29-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- Track plan: `production/agentic/sprint-29-plan-unity-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*