---
id: S31-03
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint31/tl-release-train-load
estimate_days: 2
dependencies:
  - S31-01 green baseline
  - S30-03 complete
owner: team-data
sprint: 31
req_trace: Req 06 TL-0–TL-5 release train; snapshot resolution at scenario load from tlBranch metadata
sprint_gate: true
---

# Story 031-03 — TL Release-Train Snapshot Resolution at Load

> **Epic:** sprint-31-tl-release-train  
> **ADR:** ADR-006 (database branching release train), ADR-011 (write-gate governance)  
> **Sprint gate:** Sprint fails if TL-tagged snapshots are not resolved at load without physical branch databases or mid-tick DB switching.

## Summary

Extend S30-03 Phase 4 binding to **resolve `dbRef` / `snapshotId`** from scenario **`tlBranch`** + release train metadata at **package load**. `ScenarioValidationEngine` and CLI `scenario_validate` surface mismatch findings. Mission Editor consumes resolved snapshot at load. **No** physical TL SQLite forks or `TlBranchDatabaseResolver` mid-tick behavior.

## Acceptance Criteria

- [x] Scenario package `tlBranch` resolves to correct `dbRef` / `snapshotId` via release train at load
- [x] Mismatch between `tlBranch` and available snapshot produces structured reject paths at load (not mid-tick)
- [x] `rg TlBranchDatabase|BranchDatabase` → zero production runtime bindings
- [x] CLI `scenario_validate` surfaces `tlBranch` / snapshot mismatch findings
- [x] Binding occurs at authoring/load only — zero mid-tick branch switch
- [x] WriteGate regression PASS on curated fixtures
- [x] Evidence: `production/agentic/sprint-31-tl-release-train-*.md`
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Snapshot resolution at load
  - Given: scenario package with valid `tlBranch` (`TL-0`…`TL-5`) and matching release-train snapshot
  - When: scenario loaded; `ScenarioValidationEngine` evaluates package metadata
  - Then: `dbRef` / `snapshotId` resolved at load; catalog reader bound to approved snapshot — no mid-tick switch
  - Edge cases: stale snapshot hash; missing release train entry; tier string mismatch with manifest

- **AC-2**: CLI and grep gates
  - Given: codebase after S31-03 merge
  - When: CLI `scenario_validate` run on curated fixture; grep for `TlBranchDatabase|BranchDatabase`
  - Then: CLI surfaces mismatch findings; zero mid-tick resolver bindings
  - Edge cases: reject paths surfaced before simulation tick; `TlBranchDatabaseResolver` prohibited

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Snapshot|TlTier|Scenario" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform|Scenario" -v minimal
# Confirm no runtime branch bindings / mid-tick resolver
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
# CLI scenario_validate on fixture (adjust path to curated package)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --help
npx gitnexus impact CatalogWriteGate
npx gitnexus impact ScenarioValidationEngine
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — impact before any gate edit |
| `ScenarioValidationEngine` | HIGH |
| `ICatalogReader` | HIGH |
| `PlatformWorkbookWriteBridge` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S30-03 prerequisite: `production/epics/sprint-30-tl-export-phase34/story-030-03-tl-phase4-binding.md`
- S29-02 foundation: `production/agentic/sprint-29-tl-export-phase12-2026-06-18.md`
- S28-11 spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- Skill: `database-branching-release-train`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-03)
- Track plan: `production/agentic/sprint-31-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`