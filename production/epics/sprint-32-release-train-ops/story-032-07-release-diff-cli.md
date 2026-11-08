---
id: S32-07
status: Complete
Last Updated: 2026-06-19
type: Integration
priority: should-have
graphite_branch: stack/sprint32/release-diff-cli
estimate_days: 1.5
dependencies:
  - S32-02 unified manifest landed
owner: team-data
sprint: 32
req_trace: Req 06 release train; DBI-4.5 deterministic release diff
---

# Story 032-07 — Release Diff Report CLI (DBI-4.5)

> **Epic:** sprint-32-release-train-ops  
> **ADR:** ADR-006 (release train), ADR-011 (write-gate governance)

## Summary

Add CLI verb **`catalog_release_diff`** — deterministic diff between two `ReleaseVersion` values. Empty-diff golden on re-import; no live-table mutation. Read-only report path.

## Acceptance Criteria

- [x] `catalog_release_diff` CLI verb accepts two `ReleaseVersion` identifiers
- [x] Deterministic sorted diff output — stable across runs on same inputs
- [x] Empty-diff golden on re-import of identical release versions
- [x] No live-table mutation — read-only diff path only
- [x] Data tests PASS (`TlRelease|CatalogImport|Snapshot` filters)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Deterministic release diff
  - Given: two `ReleaseVersion` values with known row deltas
  - When: `catalog_release_diff` runs
  - Then: sorted diff report lists added/removed/changed rows deterministically
  - Edge cases: identical versions (empty diff); single-row delta; cross-domain manifest rows

- **AC-2**: Re-import empty-diff golden
  - Given: same release version imported twice via WriteGate path
  - When: diff between both import snapshots
  - Then: empty diff; golden test PASS
  - Edge cases: timestamp-only metadata changes excluded; hash-stable ordering

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|TlRelease|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|TlRelease" -v minimal
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_release_diff --help
npx gitnexus impact CatalogWriteGate
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — read-only path only |
| `RecordRelease` | HIGH — READ |
| `ICatalogReader` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S32-02 prerequisite: `production/epics/sprint-32-release-train-ops/story-032-02-unified-release-train-manifest.md`
- Skill: `database-branching-release-train`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-07)
- Track plan: `production/agentic/sprint-32-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*