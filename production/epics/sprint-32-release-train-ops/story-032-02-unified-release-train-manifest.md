---
id: S32-02
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint32/unified-release-train-manifest
estimate_days: 2
dependencies:
  - S32-01 green baseline
owner: team-data
sprint: 32
req_trace: Req 06 TL-0–TL-5 release train; unified manifest consolidation; DBI-4.4
sprint_gate: true
---

# Story 032-02 — Unified Release-Train Manifest

> **Epic:** sprint-32-release-train-ops  
> **ADR:** ADR-006 (database branching release train), ADR-011 (write-gate governance)  
> **Sprint gate:** Sprint fails if unified release-train manifest is not operational.

## Summary

Consolidate S31 per-domain `releaseVersion` rows into one **curator drop + export manifest** via `RecordRelease`. `scenario_validate` resolves manifest-backed `dbRef` at load. Deterministic sorted export; WriteGate only. Evidence `sprint-32-release-train-manifest-*.md`.

## Acceptance Criteria

- [x] `RecordRelease` publishes consolidated unified manifest from S31 domain drops
- [x] `scenario_validate` resolves manifest-backed `dbRef` / `snapshotId` at load
- [x] Deterministic sorted export — stable hash across re-import
- [x] WriteGate regression PASS on curated fixtures
- [x] `rg TlBranchDatabase|BranchDatabase` → zero production runtime bindings
- [x] Evidence: `production/agentic/sprint-32-release-train-manifest-*.md`
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Manifest consolidation at load
  - Given: S31 per-domain `releaseVersion` rows and curator export fixtures
  - When: `RecordRelease` publishes consolidated manifest; scenario loaded
  - Then: `dbRef` / `snapshotId` resolved from unified manifest at load — no mid-tick switch
  - Edge cases: stale snapshot hash; missing domain row; tier string mismatch with manifest

- **AC-2**: CLI and grep gates
  - Given: codebase after S32-02 merge
  - When: CLI `scenario_validate` run on curated fixture; grep for `TlBranchDatabase|BranchDatabase`
  - Then: CLI surfaces mismatch findings; zero mid-tick resolver bindings; WriteGate tests PASS
  - Edge cases: reject paths surfaced before simulation tick; live-table mutation prohibited

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|CmoMarkdown|Platform|CatalogImport|Snapshot|TlTier|Scenario|TlRelease" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform|Scenario" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --help
npx gitnexus impact CatalogWriteGate
npx gitnexus impact ScenarioValidationEngine
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — impact before any gate edit |
| `ScenarioValidationEngine` | HIGH |
| `RecordRelease` | HIGH |
| `ICatalogReader` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S31-03 prerequisite: `production/epics/sprint-31-tl-release-train/story-031-03-tl-release-train-load.md`
- Skill: `database-branching-release-train`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-02)
- Track plan: `production/agentic/sprint-32-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*