# Epic: Sprint 28 — Platform Editor Write Path

> **Status:** Ready  
> **Sprint:** 28  
> **Dates:** 2026-09-18 → 2026-10-01  
> **Trunk:** `main` @ `a93b55e` (741/741; ReplayGolden 6/6)  
> **Layer:** Data + Presentation  
> **GDD / ADR:** ADR-011 Phase D (Excel-primary write path); Req 21 Platform Editor

## Goal

Land **ADR-011 Phase D** — bounded in-engine Excel write path through extend-only `CatalogWriteGate` (propose→approve), and optionally wire a **read-only export/diff hook** from the Phase C platform viewer — without full Unity import UI chrome or write-gate bypass.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-011 | Accepted | Phase D Excel write; CLI authority; viewer read-only export trigger |
| ADR-010 | Accepted | Headless-first; ZERO touch `DelegationBridge` |
| ADR-006 | Accepted | Engine-free data boundary |

## Graphite Stack (merge order)

```
main
 └── stack/sprint28/full-sln-gate              (S28-01 — shared day-1)
      └── stack/sprint28/excel-write           (S28-04) — CRITICAL
           └── stack/sprint28/platform-write-ui (S28-07 — should-have)
```

**Dependency:** S28-07 blocked on S28-04 (write API must exist before viewer export hook).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 04 | [ADR-011 Phase D — in-engine Excel write path](story-028-04-excel-write-path.md) | S28-04 | Integration | must-have | 2.5d | Ready |
| 07 | [Platform viewer export/diff hook](story-028-07-viewer-export-hook.md) | S28-07 | Integration | should-have | 1d | Not Started |

Note: **S28-01** day-1 baseline lives in `sprint-28-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `IPlatformWorkbookIo`, `PlatformWorkbookImporter`, `CatalogPlatformBrowseProjection`
- **No bypass:** viewer export triggers CLI/data API only — no direct SQLite or gate skip

## Producer Constraints

1. **ADR-011 Excel-primary** — write path via workbook + write gate, not raw SQLite
2. **Full Unity Excel import UI chrome** — out of scope; write path + CLI authority
3. **Data ↔ Unity overlap** — land data API first; single owner per file; write-gate grep on viewer

## Definition of Done

- [ ] S28-04 complete (must-have)
- [ ] Headless write-gate tests PASS; export→edit→propose round-trip on Baltic fixture
- [ ] GitNexus CRITICAL documented on `CatalogWriteGate`
- [ ] S28-07 (if kept): headless export path test; no write-gate bypass in viewer host
- [ ] Tracker row 21 Phase D progress note

## References

- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`
- Phase C spike: `production/qa/sprint-26-platform-viewer-spike-2026-06-18.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md`
- Parallel kickoff: `production/agentic/sprint-28-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*