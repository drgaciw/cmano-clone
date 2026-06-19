# S32-07 story-done — Release Diff Report CLI (DBI-4.5)

**Story:** `production/epics/sprint-32-release-train-ops/story-032-07-release-diff-cli.md`  
**Status:** Complete  
**Completed:** 2026-06-19  
**Branch:** `stack/sprint32/release-diff-cli`

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| `catalog_release_diff` CLI verb accepts two `ReleaseVersion` identifiers | `CatalogReleaseDiffCommand`, `Program.cs` wiring, `--from`/`--to` + positional | COVERED |
| Deterministic sorted diff output | `UnifiedReleaseTrainDiffComparer`, `ToSortedCanonicalLines`, delta tests | COVERED |
| Empty-diff golden on re-import | `UnifiedReleaseTrainDiffReportTests.CatalogImport_reimport_empty_diff_when_semantic_content_matches` | COVERED |
| No live-table mutation — read-only path | `DbSnapshotStore` read-only in comparer; no `CatalogWriteGate` usage | COVERED |
| Data tests PASS (`TlRelease\|CatalogImport\|Snapshot`) | **63/63** PASS | COVERED |
| Cli tests PASS (`CatalogImport\|TlRelease`) | **10/10** PASS | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff | COVERED |

## Key symbols

| Symbol | Change |
|--------|--------|
| `UnifiedReleaseTrainDiffReport` | **new** — diff row/report model + canonical lines |
| `UnifiedReleaseTrainDiffComparer` | **new** — domain-indexed manifest/domain drop compare |
| `CatalogReleaseDiffCommand` | **new** — JSON CLI output for `catalog_release_diff` |
| `Program.cs` | **extend** — verb switch + usage + `--help` |
| `UnifiedReleaseTrainDiffReportTests` | **new** (+3 tests) |
| `CatalogReleaseDiffCommandTests` | **new** (+2 tests) |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|TlRelease|CatalogImport|Snapshot" -v minimal
# 63/63 PASS

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|TlRelease" -v minimal
# 10/10 PASS

dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_release_diff --help
# catalog_release_diff — deterministic read-only diff between two ReleaseVersion values

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty — zero touch)
```

## Results (2026-06-19)

| Gate | Result |
|------|--------|
| Data filtered | **63/63** PASS |
| Cli filtered | **10/10** PASS |
| DelegationBridge | **zero touch** |
| CatalogWriteGate | **read-only path only** (no mutation) |

## Unblocks

- Curator release-train diff workflows (DBI-4.5)
- Sprint 32 release-train ops closeout