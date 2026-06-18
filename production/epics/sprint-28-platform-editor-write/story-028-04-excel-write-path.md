---
id: S28-04
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint28/excel-write
estimate_days: 2.5
dependencies:
  - S28-01 green baseline
owner: team-data + team-unity
sprint: 28
req_trace: Req 21 Platform Editor; ADR-011 Phase D
---

# Story 028-04 — ADR-011 Phase D — In-Engine Excel Write Path

> **Epic:** sprint-28-platform-editor-write  
> **ADR:** ADR-011 (Accepted — Excel-primary write path)

## Summary

Unity/CLI hook to stage platform workbook changes via `CatalogWriteGate` (propose→approve). Bounded in-engine Excel write path — no bypass, no raw SQLite writes. Export→edit→propose round-trip on Baltic fixture.

## Acceptance Criteria

- [x] Headless write-gate tests PASS for Phase D platform workbook staging
- [x] Unity or CLI hook invokes propose path (no direct DB writes)
- [x] Export→edit→propose round-trip on Baltic fixture completes
- [x] `CatalogWriteGate` extend-only — `gitnexus impact` before edit
- [x] GitNexus CRITICAL documented on `CatalogWriteGate`
- [x] No full Unity Excel import UI chrome (write path + CLI authority only)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Propose→approve round-trip
  - Given: exported Baltic platform workbook
  - When: edit + propose via Phase D hook + approve batch
  - Then: read-back matches staged changes; empty-diff golden holds
  - Edge cases: reject batch; multi-entity propose ordering

- **AC-2**: No write-gate bypass
  - Given: viewer/Unity host wiring
  - When: grep for direct SQLite or gate skip patterns
  - Then: all writes route through `CatalogWriteGate`
  - Edge cases: accidental `IWriteGate` bypass in presentation layer

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|Excel" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformCatalog|Excel" -v minimal
npx gitnexus impact CatalogWriteGate
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — extend-only |
| `IPlatformWorkbookIo` | HIGH |
| `PlatformWorkbookImporter` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`
- S23-01 pattern: `production/epics/sprint-23-platform-phase-b/story-023-01-closedxml-xlsx.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-04)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*