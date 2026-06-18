---
id: S29-04
status: Complete
type: UI+Integration
priority: must-have
graphite_branch: stack/sprint29/unity-import-ui
estimate_days: 2.5
dependencies:
  - S29-03 nightly approve workflow
owner: team-unity + team-data
sprint: 29
req_trace: Req 21 Platform Editor; ADR-011 Phase E
---

# Story 029-04 — Platform Editor Phase E — Unity Import UI

> **Epic:** sprint-29-platform-editor-phase-e  
> **ADR:** ADR-011 (Phase E import UI), ADR-010 (headless-first)

## Summary

In-engine Unity import UI — import → propose → approve atop S28-04 `PlatformWorkbookWriteBridge`. Staging review UX wired; headless + viewer tests PASS. No write-gate bypass; no raw SQLite writes.

## Acceptance Criteria

- [x] In-engine import flow invokes `PlatformWorkbookWriteBridge` propose path (no direct DB writes)
- [x] Staging review UX wired (diff preview before approve)
- [x] Import→propose→approve round-trip on Baltic fixture completes
- [x] Headless write-gate + viewer tests PASS
- [x] `CatalogWriteGate` extend-only — `gitnexus impact` before edit
- [x] No `CatalogWriteGate` / `IWriteGate` bypass in viewer or import host
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Import→propose→approve round-trip
  - Given: exported Baltic platform workbook staged for import
  - When: Unity import UI triggers import + propose + curator approve
  - Then: read-back matches staged changes; empty-diff golden holds
  - Edge cases: reject batch; multi-entity propose ordering; unchanged workbook empty diff

- **AC-2**: No write-gate bypass
  - Given: import UI + viewer host wiring
  - When: grep for direct SQLite or gate skip patterns
  - Then: all writes route through `CatalogWriteGate` via `PlatformWorkbookWriteBridge`
  - Edge cases: accidental propose call bypassing write bridge

- **AC-3**: Staging review UX
  - Given: workbook with bounded edits
  - When: user opens staging review before approve
  - Then: diff preview shows entity-level changes; approve disabled until review acknowledged
  - Edge cases: zero-diff import; large batch over threshold requires explicit approve

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|Excel|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|PlatformCatalog|Excel" -v minimal
npx gitnexus impact CatalogWriteGate
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — extend-only |
| `PlatformWorkbookWriteBridge` | HIGH |
| `IPlatformWorkbookIo` | HIGH |
| `CatalogPlatformBrowseProjection` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`
- S28-04 pattern: `production/epics/sprint-28-platform-editor-write/story-028-04-excel-write-path.md`
- S28-07 pattern: `production/epics/sprint-28-platform-editor-write/story-028-07-viewer-export-hook.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-04)
- Track plan: `production/agentic/sprint-29-plan-unity-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*