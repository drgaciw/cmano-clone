# S30-03 story-done — TL Export Phase 4 Binding

**Story:** `production/epics/sprint-30-tl-export-phase34/story-030-03-tl-phase4-binding.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint30/tl-phase4-binding`

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Scenario package `tlBranch`; load-time validation | `ScenarioMetadataDto`, `TlBranchRule`, `TlBranchValidationTests` | COVERED |
| Invalid/missing → structured reject at load | `TL_BRANCH_MISSING`, `TL_BRANCH_INVALID`, `TL_BRANCH_SNAPSHOT_MISMATCH` | COVERED |
| No runtime `TlBranchDatabase` / `BranchDatabase` | `rg` gate zero matches | COVERED |
| CLI `scenario_validate` surfaces findings | `ScenarioValidateCliTests` | COVERED |
| Bind at authoring/load only | `ScenarioPackage.TlBranch`; no mid-tick resolver | COVERED |
| WriteGate regression PASS | filtered Data.Tests 58/58 | COVERED |
| Evidence doc | `production/agentic/sprint-30-tl-phase4-2026-06-18.md` | COVERED |
| ZERO touch DelegationBridge | empty diff | COVERED |

## Key symbols touched

- `ScenarioMetadataDto` (extend — `TlBranch`)
- `ScenarioPackage` (extend — `TlBranch`, `ResolveTlBranch`)
- `ValidationRules.TlBranchRule` (new)
- `ScenarioValidationEngine` (wire rule)
- `ICatalogReader.TryGetSnapshotBranch` (extend)
- `InMemoryCatalogReader`, `SqliteCatalogReader` (implement branch lookup)
- `ScenarioDocumentEditor` (default + preserve `tlBranch`)
- Curated fixtures `golden_clean.json`, `golden_strike_unreachable.json`

## Not touched (by design)

- `CatalogWriteGate.cs`
- `DelegationBridge.cs`
- `TlBranchDatabaseResolver` / `BranchDatabase` types

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests --filter "WriteGate|Snapshot|TlTier|Scenario" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests --filter "CatalogImport|Platform|Scenario" -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
```

## Unblocks

- **S30-04** — Nightly ship.md approve at scale (parallel track)
- **Sprint 30 TL gate** — Phase 4 scenario binding complete; Phase 5 physical forks remain Post-MVP