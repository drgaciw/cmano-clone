---
id: S30-03
status: Complete
last_updated: 2026-06-18
type: Integration
priority: must-have
graphite_branch: stack/sprint30/tl-phase4-binding
estimate_days: 2.5
dependencies:
  - S30-02 complete
owner: team-data
sprint: 30
req_trace: Req 06 TL-0–TL-5 branch databases (Phase 4); scenario package tlBranch binding at load
---

# Story 030-03 — TL Export Phase 4 Binding

> **Epic:** sprint-30-tl-export-phase34  
> **ADR:** ADR-006 (database branching release train), ADR-011 (write-gate governance)  
> **Sprint gate:** Sprint fails if `tlBranch` is not validated at scenario load with zero `TlBranchDatabaseResolver` mid-tick behavior.

## Summary

Implement TL export **Phase 4** from S28-11 PROCEED verdict: scenario package `tlBranch` field (`TL-0`…`TL-5`) with load-time validation via `ScenarioValidationEngine`. Bind at authoring/load — **not** mid-tick. CLI `scenario_validate` surfaces findings. Mission Editor consumes validated package metadata. **No** physical branch DBs or mid-tick resolver.

## Acceptance Criteria

- [x] Scenario package carries `tlBranch` field; validated at load by `ScenarioValidationEngine`
- [x] Invalid or missing `tlBranch` produces structured reject paths at load (not mid-tick)
- [x] `rg TlBranchDatabase|BranchDatabase` → zero production runtime bindings
- [x] CLI `scenario_validate` surfaces `tlBranch` validation findings
- [x] Binding occurs at authoring/load only — zero `TlBranchDatabaseResolver` mid-tick behavior
- [x] WriteGate regression PASS on curated fixtures
- [x] Evidence: `production/agentic/sprint-30-tl-phase4-*.md`
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Scenario load validation
  - Given: scenario package with valid `tlBranch` (`TL-0`…`TL-5`) @ post-S30-02 baseline
  - When: scenario loaded; `ScenarioValidationEngine` evaluates package metadata
  - Then: `tlBranch` validated at load; binding resolved at authoring — no mid-tick branch switch
  - Edge cases: invalid tier string; missing `tlBranch`; mismatch with snapshot `catalog_snapshot.branch`

- **AC-2**: CLI and grep gates
  - Given: codebase after S30-03 merge
  - When: CLI `scenario_validate` run on curated fixture; grep for `TlBranchDatabase|BranchDatabase`
  - Then: CLI surfaces `tlBranch` findings; zero mid-tick resolver bindings
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

- S30-02 prerequisite: `production/epics/sprint-30-tl-export-phase34/story-030-02-tl-phase3-export.md`
- S29-02 foundation: `production/agentic/sprint-29-tl-export-phase12-2026-06-18.md`
- S28-11 spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- Skill: `database-branching-release-train`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-03)
- Track plan: `production/agentic/sprint-30-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`