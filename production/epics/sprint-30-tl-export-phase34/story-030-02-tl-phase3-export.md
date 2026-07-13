---
id: S30-02
status: Complete
last_updated: 2026-06-18
type: Integration
priority: must-have
graphite_branch: stack/sprint30/tl-phase3-export
estimate_days: 2
dependencies:
  - S30-01 green baseline
owner: team-data
sprint: 30
req_trace: Req 06 TL-0–TL-5 branch databases (Phase 3); S29-02 export metadata foundation
---

# Story 030-02 — TL Export Phase 3

> **Epic:** sprint-30-tl-export-phase34  
> **ADR:** ADR-006 (database branching release train), ADR-011 (write-gate governance)

## Summary

Implement TL export **Phase 3** from S28-11 PROCEED verdict: per-tier filtered `ICatalogReader` export (read-only); `platform_export_xlsx` and JSON drops honor `tlTier` filter with deterministic sort keys. **No** runtime branch DBs or `TlBranchDatabaseResolver` — read-only filter path only.

## Acceptance Criteria

- [x] Filtered export tests PASS for per-tier `ICatalogReader` slices
- [x] `platform_export_xlsx` / JSON drops honor `tlTier` filter; manifest field present
- [x] Deterministic sort keys locked: `(canonicalId, tlTier, valueTier)` ascending with `StringComparer.Ordinal`
- [x] `rg TlBranchDatabase|BranchDatabase` → zero production runtime bindings
- [x] WriteGate regression PASS on curated fixtures
- [x] Evidence: `production/agentic/sprint-30-tl-phase3-*.md`
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Per-tier filtered export
  - Given: catalog with records across TL tiers @ post-S30-01 baseline
  - When: export invoked with `tlTier` filter (e.g. `TL-2`) via `ICatalogReader`
  - Then: drop contains only records at or below requested tier; manifest honors `tlTier`; xlsx and JSON parity
  - Edge cases: empty tier slice; `TL-0` default; deterministic reorder across repeated runs

- **AC-2**: No runtime branch DB
  - Given: codebase after S30-02 merge
  - When: grep for TL branch runtime binding; WriteGate regression suite runs
  - Then: zero `TlBranchDatabase` / `BranchDatabase` production bindings; WriteGate tests PASS
  - Edge cases: read-only filter path only; Phase 5 physical forks explicitly deferred

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogImport|Snapshot|TlTier" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Confirm no runtime branch bindings
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
npx gitnexus impact CatalogWriteGate
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — impact before any gate edit |
| `ICatalogReader` | HIGH |
| `PlatformWorkbookWriteBridge` | HIGH |
| `CmoMarkdownImporter` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S29-02 foundation: `production/agentic/sprint-29-tl-export-phase12-2026-06-18.md`
- S28-11 spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- S28-11 pattern: `production/epics/sprint-28-cmo-corpus-v2/story-028-11-tl-branching-spike.md`
- Skill: `database-branching-release-train`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-02)
- Track plan: `production/agentic/sprint-30-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`