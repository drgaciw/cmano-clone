# S31-03 story-done — TL Release-Train Snapshot Resolution at Load

**Story:** `production/epics/sprint-31-tl-release-train/story-031-03-tl-release-train-load.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint31/tl-release-train-load`

## Verdict: COMPLETE (sprint gate)

| AC | Evidence | Status |
|----|----------|--------|
| `tlBranch` → `dbRef`/`snapshotId` via release train at load | `ScenarioPackage.ResolveBinding`, `TlReleaseTrainValidationTests` | COVERED |
| Mismatch → structured reject at load | `TL_RELEASE_TRAIN_NOT_FOUND`, `TL_RELEASE_TRAIN_MISMATCH` | COVERED |
| No `TlBranchDatabase` / `BranchDatabase` runtime bindings | grep gate zero matches | COVERED |
| CLI `scenario_validate` surfaces findings | `ScenarioValidateCliTests` | COVERED |
| Bind at authoring/load only | validation engine + package load; no mid-tick resolver | COVERED |
| WriteGate regression PASS | filtered Data.Tests 68/68 | COVERED |
| ReplayGolden 6/6 | `ReplayGoldenSuiteTests` | COVERED |
| Evidence doc | `production/agentic/sprint-31-tl-release-train-2026-06-18.md` | COVERED |
| ZERO touch DelegationBridge | empty diff | COVERED |

## Key symbols

- `ValidationRules.TlReleaseTrainRule` (new)
- `ValidationRules.TlBranchRule` (extend)
- `ScenarioPackage.ResolveBinding`, `FromDocument(..., ICatalogReader?)`
- `ScenarioValidateCommand.ResolveCatalog` (tlBranch-aware)
- `TlReleaseTrainValidationTests` (new, +4 tests)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests --filter "WriteGate|Snapshot|TlTier|Scenario|TlRelease" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests --filter "CatalogImport|Platform|Scenario" -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Results (2026-06-18)

| Gate | Result |
|------|--------|
| Data filtered | **68/68** PASS |
| Cli filtered | **23/23** PASS |
| Full sln | **967/967** PASS |
| ReplayGolden | **6/6** PASS |
| Grep gate | **zero** `TlBranchDatabase`/`BranchDatabase` |
| DelegationBridge | **zero touch** |

## Unblocks

- S31-09 balance drift advisory
- S31-10 weapon nightly approve
- S31-13 closeout hygiene (sprint gate)