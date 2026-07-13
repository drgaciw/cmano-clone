# Sprint 32 — Unified Release-Train Manifest (S32-02)

**Date:** 2026-06-19  
**Story:** S32-02 — Unified release-train manifest  
**Branch:** `stack/sprint32/unified-release-train-manifest`  
**Verdict:** **PASS** (sprint gate)

## Summary

Consolidated S31 per-domain `releaseVersion` rows into one curator drop + export manifest via `DbSnapshotStore.RecordUnifiedRelease`. `scenario_validate` and `ScenarioPackage.ResolveBinding` resolve manifest-backed `dbRef` / `snapshotId` at load through `SqliteCatalogReader.TryResolveDbRef` and `TryResolveSnapshotForTlBranch`. Deterministic sorted domain export hash is stable across re-consolidation. WriteGate-only path preserved; zero `DelegationBridge.cs` touch.

## Acceptance Criteria

| AC | Evidence | Status |
|----|----------|--------|
| `RecordRelease` publishes consolidated unified manifest from S31 domain drops | `RecordUnifiedRelease`, `UnifiedReleaseTrainManifest.Consolidate` | PASS |
| `scenario_validate` resolves manifest-backed `dbRef` / `snapshotId` at load | `UnifiedReleaseTrainManifestTests.Scenario_validate_resolves_manifest_backed_dbRef_at_load` | PASS |
| Deterministic sorted export — stable hash across re-import | `RecordUnifiedRelease_consolidates_sorted_domain_rows_with_stable_hash`, `Manifest_hash_is_order_independent_for_domain_drops` | PASS |
| WriteGate regression PASS on curated fixtures | Data.Tests filtered 219/219 | PASS |
| `rg TlBranchDatabase\|BranchDatabase` → zero | grep gate (no matches) | PASS |
| Evidence doc | this file | PASS |
| ZERO touch `DelegationBridge.cs` | empty diff | PASS |

## Key symbols

| Symbol | Change |
|--------|--------|
| `UnifiedReleaseTrainManifest` | **new** — consolidate domain drops, compute hash, export manifest |
| `CatalogReleaseTrainDomains` | **new** — canonical nightly domain labels |
| `DbSnapshotStore.RecordUnifiedRelease` | **new** — publish unified curator drop |
| `DbSnapshotStore.TryResolveReleaseVersion` | **new** — manifest-backed dbRef resolution |
| `SqliteCatalogReader.TryResolveDbRef` | **extend** — resolve `db_release.release_version` |
| `CatalogSnapshotBinder.BindAfterApprove` | **extend** — persist `contentHash` in release notes |
| `CatalogExportManifest.Resolve` | **extend** — unified manifest export fields |

## Manifest model

- Per-domain nightly drops identified by `nightly-{domain}-*` release versions (sensor, weapon, platform, aircraft, submarine, facility).
- Unified manifest stored in `db_release.notes` as `unified-manifest:{json}` with sorted `domainDrops`.
- Consolidated `contentHashSha256` = SHA-256 over sorted `domain|releaseVersion|snapshotId|contentHash` rows.
- Domain `contentHash` pinned in per-domain release notes (`batch=…;contentHash=…`) for stable re-consolidation.

## Verify (2026-06-19)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|CmoMarkdown|Platform|CatalogImport|Snapshot|TlTier|Scenario|TlRelease|UnifiedRelease" -v minimal
# 219/219 PASS (+5 unified manifest tests)

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform|Scenario" -v minimal
# 24/24 PASS (+1 DB_MISMATCH reject path)

rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
# (no output — zero matches)

dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --help
# scenario_validate requires --path <scenario.json>

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty — zero touch)
```

## Test traceability

| Test | AC |
|------|-----|
| `UnifiedReleaseTrainManifestTests.RecordUnifiedRelease_consolidates_sorted_domain_rows_with_stable_hash` | AC-1 |
| `UnifiedReleaseTrainManifestTests.Scenario_validate_resolves_manifest_backed_dbRef_at_load` | AC-1 |
| `UnifiedReleaseTrainManifestTests.TlBranch_resolution_prefers_unified_manifest_dbRef_when_present` | AC-1 |
| `UnifiedReleaseTrainManifestTests.CatalogExportManifest_resolves_unified_manifest_fields` | AC-1 |
| `ScenarioValidateCliTests.scenario_validate_unknown_manifest_dbRef_returns_exit_1_with_DB_MISMATCH` | AC-2 |
| `TlReleaseTrainValidationTests` (carryover S31-03) | regression |

## Not touched (by design)

- `CatalogWriteGate.cs` (extend-only via `BindAfterApprove` notes)
- `DelegationBridge.cs`
- `TlBranchDatabaseResolver` / `BranchDatabase` types

## Unblocks

- **S32-03** — Mount/loadout quarantine triage
- **S32-07** — Release diff CLI (cross-domain manifest rows)