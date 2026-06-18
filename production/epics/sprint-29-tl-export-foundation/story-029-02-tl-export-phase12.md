---
id: S29-02
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint29/tl-export-phase12
estimate_days: 2
dependencies:
  - S29-01 green baseline
owner: team-data
sprint: 29
req_trace: Req 06 TL-0–TL-5 branch databases (Phases 1–2); S28-11 PROCEED export-only
---

# Story 029-02 — TL Export Phase 1–2

> **Epic:** sprint-29-tl-export-foundation  
> **ADR:** ADR-006 (database branching release train), ADR-011 (write-gate governance)

## Summary

Implement TL export **Phases 1–2** from S28-11 PROCEED verdict: `tlTier` on export manifests; migration `007` `catalog_snapshot.branch` (`TL-0`…`TL-5`); metadata only — **no** runtime `tlBranch` binding or physical branch DBs.

## Acceptance Criteria

- [x] Migration `010` applies; `catalog_snapshot.branch` column (`TL-0`…`TL-5`) present
- [x] Export drops carry `tlTier` manifest field
- [x] `rg TlBranch|BranchDatabase` → zero production runtime bindings
- [x] WriteGate regression PASS on curated fixtures
- [x] Evidence: `production/agentic/sprint-29-tl-export-phase12-2026-06-18.md`
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Migration and manifest metadata
  - Given: clean catalog DB @ post-S29-01 baseline
  - When: migration `007` applies; export drop generated
  - Then: `catalog_snapshot.branch` column present; manifest includes `tlTier`
  - Edge cases: rollback path documented; existing snapshots default to `TL-0`

- **AC-2**: No runtime branch binding
  - Given: codebase after S29-02 merge
  - When: grep for TL branch runtime binding
  - Then: zero `TlBranch` / `BranchDatabase` production bindings
  - Edge cases: accidental scenario DB branch switch; Phase 4 deferred explicitly

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Confirm no runtime branch bindings
rg -l "TlBranch|BranchDatabase" src/ --glob "*.cs" || true
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — impact before any gate edit |
| `ICatalogReader` | HIGH |
| `CmoMarkdownImporter` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S28-11 spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- S28-11 pattern: `production/epics/sprint-28-cmo-corpus-v2/story-028-11-tl-branching-spike.md`
- Skill: `database-branching-release-train`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-02)
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*