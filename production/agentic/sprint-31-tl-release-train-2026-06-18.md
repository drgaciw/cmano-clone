# Sprint 31 — TL Release-Train Snapshot Resolution at Load (S31-03)

**Date:** 2026-06-18  
**Story:** S31-03 — TL release-train snapshot resolution at load  
**Branch:** `stack/sprint31/tl-release-train-load`  
**Verdict:** **PASS** (sprint gate)

## Summary

Extended S30-03 Phase 4 `tlBranch` binding to resolve `dbRef` / `snapshotId` from release-train metadata at **package load** via `ICatalogReader.TryResolveSnapshotForTlBranch`. Validation surfaces structured reject paths before simulation tick. No physical TL SQLite forks or mid-tick branch switching.

## Acceptance Criteria

| AC | Evidence | Status |
|----|----------|--------|
| `tlBranch` resolves to `dbRef` / `snapshotId` at load | `ScenarioPackage.ResolveBinding`, `FromDocument(..., catalog)` | PASS |
| Mismatch → structured reject at load | `TL_RELEASE_TRAIN_NOT_FOUND`, `TL_RELEASE_TRAIN_MISMATCH` | PASS |
| `rg TlBranchDatabase\|BranchDatabase` → zero | grep gate (no matches) | PASS |
| CLI `scenario_validate` surfaces findings | `ScenarioValidateCliTests.scenario_validate_missing_release_train_*` | PASS |
| Bind at authoring/load only | `ScenarioValidateCommand.ResolveCatalog` + validation engine; no mid-tick resolver | PASS |
| WriteGate regression PASS | filtered Data.Tests 68/68 | PASS |
| ReplayGolden 6/6 | `ReplayGoldenSuiteTests` | PASS |
| ZERO touch `DelegationBridge.cs` | empty diff | PASS |

## Key finding codes

| Code | When |
|------|------|
| `TL_RELEASE_TRAIN_NOT_FOUND` | Valid `tlBranch` but no matching snapshot in release train |
| `TL_RELEASE_TRAIN_MISMATCH` | Explicit `dbRef`/`dbSnapshotId` resolves to different snapshot than release train |
| `TL_BRANCH_SNAPSHOT_MISMATCH` | (S30-03 carryover) `tlBranch` ≠ `catalog_snapshot.branch` for bound snapshot |

## Symbols touched

- `ValidationRules.TlReleaseTrainRule` (new)
- `ValidationRules.TlBranchRule` (extend — release-train snapshot when explicit db absent)
- `ScenarioValidationEngine` (wire `TlReleaseTrainRule` after `TlBranchRule`)
- `ScenarioPackage.ResolveBinding`, `HasExplicitDbBinding`, `FromDocument(..., ICatalogReader?)`
- `ScenarioValidateCommand.ResolveCatalog` (tlBranch-aware SQLite resolution)

## Not touched (by design)

- `CatalogWriteGate.cs`
- `DelegationBridge.cs`
- `TlBranchDatabaseResolver` / `BranchDatabase` types

## Verify (2026-06-18)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests --filter "WriteGate|Snapshot|TlTier|Scenario|TlRelease" -v minimal
# 68/68 PASS

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests --filter "CatalogImport|Platform|Scenario" -v minimal
# 23/23 PASS

dotnet test ProjectAegis.sln -v minimal
# 967/967 PASS

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal
# 6/6 PASS

rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
# (no output — zero matches)

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty — zero touch)
```

## Test traceability

| Test | AC |
|------|-----|
| `TlReleaseTrainValidationTests.Valid_tlBranch_without_explicit_dbRef_resolves_snapshot_at_load` | AC-1 |
| `TlReleaseTrainValidationTests.Missing_release_train_entry_emits_TL_RELEASE_TRAIN_NOT_FOUND` | AC-1 |
| `TlReleaseTrainValidationTests.Explicit_dbRef_conflicting_with_release_train_emits_TL_RELEASE_TRAIN_MISMATCH` | AC-1 |
| `TlReleaseTrainValidationTests.FromDocument_with_catalog_binds_resolved_snapshot_without_explicit_dbRef` | AC-1 |
| `ScenarioValidateCliTests.scenario_validate_missing_release_train_returns_exit_1_with_finding` | AC-2 |
| `TlBranchValidationTests` (carryover S30-03) | regression |

## Unblocks

- **S31-09** — Balance drift advisory (release train binding stable)
- **S31-10** — Weapon nightly approve at scale
- **S31-13** — Sprint 31 closeout hygiene (sprint gate landed)