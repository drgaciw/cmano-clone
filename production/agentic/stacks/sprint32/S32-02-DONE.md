# S32-02 story-done — Unified Release-Train Manifest

**Story:** `production/epics/sprint-32-release-train-ops/story-032-02-unified-release-train-manifest.md`  
**Status:** Complete  
**Completed:** 2026-06-19  
**Branch:** `stack/sprint32/unified-release-train-manifest`

## Verdict: COMPLETE (sprint gate)

| AC | Evidence | Status |
|----|----------|--------|
| `RecordRelease` publishes consolidated unified manifest | `RecordUnifiedRelease`, `UnifiedReleaseTrainManifest` | COVERED |
| `scenario_validate` resolves manifest-backed `dbRef` at load | `UnifiedReleaseTrainManifestTests`, `SqliteCatalogReader.TryResolveDbRef` | COVERED |
| Deterministic sorted export — stable hash | `ComputeManifestHash`, re-consolidation test | COVERED |
| WriteGate regression PASS | filtered Data.Tests 219/219 | COVERED |
| Grep gate zero `TlBranchDatabase`/`BranchDatabase` | `rg` gate | COVERED |
| Evidence doc | `production/agentic/sprint-32-release-train-manifest-2026-06-19.md` | COVERED |
| ZERO touch DelegationBridge | empty diff | COVERED |

## Key symbols

- `UnifiedReleaseTrainManifest` (new)
- `CatalogReleaseTrainDomains` (new)
- `DbSnapshotStore.RecordUnifiedRelease` (new)
- `SqliteCatalogReader.TryResolveDbRef` (extend)
- `UnifiedReleaseTrainManifestTests` (+5 tests)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests --filter "WriteGate|Snapshot|TlTier|Scenario|TlRelease|UnifiedRelease" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests --filter "CatalogImport|Platform|Scenario" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Results (2026-06-19)

| Gate | Result |
|------|--------|
| Data filtered | **219/219** PASS |
| Cli filtered | **24/24** PASS |
| Grep gate | **zero** `TlBranchDatabase`/`BranchDatabase` |
| DelegationBridge | **zero touch** |

## Unblocks

- S32-03 mount/loadout quarantine triage
- S32-07 release diff CLI