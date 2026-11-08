---
id: S32-06
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint32/platform-phase-f-damage
estimate_days: 2
dependencies:
  - S32-01 green baseline
owner: team-unity
sprint: 32
req_trace: Req 21 Platform Editor Phase F; ADR-011 damage workbook surfacing; TR-platform-ed-003
sprint_gate: true
---

# Story 032-06 — Platform Editor Phase F — Damage Unity Surfacing

> **Epic:** sprint-32-platform-editor-phase-f  
> **ADR:** ADR-011 (write-gate governance), ADR-010 (headless-first panel seams)  
> **Sprint gate:** Sprint fails if damage workbook round-trip is not surfaced in Unity.

## Summary

Surface **damage workbook columns** in **`PlatformCatalogViewerHost`**; staging diff for **`MaxHp`** edits via import panel; headless **propose→approve** round-trip tests PASS. Read-only viewer + staging diff only — no new migrations.

## Acceptance Criteria

- [x] `PlatformCatalogViewerHost` displays damage fields (`MaxHp`, resilience, damage level columns per workbook schema)
- [x] Import staging diff surfaces `MaxHp` edit deltas before approve
- [x] Headless propose→approve round-trip tests PASS (`PlatformImport|PlatformCatalogViewer` filters)
- [x] Writes route `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only (WriteGate path)
- [x] No new SQLite migrations; no write-gate bypass
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Damage viewer columns
  - Given: Baltic fixture platform with damage workbook rows
  - When: `PlatformCatalogViewerHost` renders catalog entry
  - Then: damage fields visible and match workbook snapshot values
  - Edge cases: missing damage row (graceful empty); zero MaxHp; read-only mode enforced

- **AC-2**: Staging diff + approve round-trip
  - Given: proposed `MaxHp` edit via import staging panel
  - When: headless propose→approve flow runs
  - Then: staging diff shows delta; approve commits via WriteGate; post-approve viewer reflects new value
  - Edge cases: reject path clears staging; duplicate propose blocked; unapproved edit not in catalog

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer" -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
npx gitnexus impact PlatformCatalogViewerHost
npx gitnexus impact PlatformWorkbookWriteBridge
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `PlatformCatalogViewerHost` | HIGH |
| `PlatformImportPanelHost` | HIGH |
| `PlatformWorkbookWriteBridge` | HIGH |
| `CatalogWriteGate` | CRITICAL — extend-only |
| `DelegationBridge.cs` | ZERO touch |

## References

- S28-07 pattern: `production/epics/sprint-28-platform-editor-write/story-028-07-viewer-export-hook.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-06)
- Track plan: `production/agentic/sprint-32-plan-unity-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*