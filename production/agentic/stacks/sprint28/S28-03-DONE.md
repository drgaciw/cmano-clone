# S28-03 story-done — Platform Corpus E2E + Golden Hygiene

**Story:** `production/epics/sprint-28-cmo-corpus-v2/story-028-03-corpus-golden-hygiene.md`  
**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- `CatalogSortKeyGoldenHashes.ShipSlice100PlatformV2` pinned for curated `ship-slice-100.md`
- `CmoMarkdownImportGoldenTests` extended:
  - `Platform_ship_slice100_reimport_identical_slice_preserves_catalog_ordering_hash`
  - `Platform_ship_slice100_corpus_roundtrip_through_WriteGate_pins_ordering_hash`
- Platform corpus round-trip: `ProposePlatformsFromMarkdown` → `CatalogWriteGate.ApproveBatch` on 100-record curated slice
- ReplayGolden 6/6 unchanged; ZERO touch `DelegationBridge.cs`

## Pinned hash

`ShipSlice100PlatformV2` = `f0712b4225b14186d080636afdbcb0cdacdba895bb3247ae1b274f6c4421db90`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogSortKey" -v minimal   # 169/169 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal                             # 6/6 PASS
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs  # empty
```

## Acceptance criteria

| AC | Test / Evidence | Verdict |
|----|-----------------|---------|
| Golden tests extended for platform v2 curated slice + hash pin | `CmoMarkdownImportGoldenTests` + `ShipSlice100PlatformV2` | **PASS** |
| Re-import identical slice → stable ordering hash | `Platform_ship_slice100_reimport_identical_slice_*` | **PASS** |
| WriteGate regression unchanged | `CmoMarkdown\|WriteGate\|Platform\|CatalogSortKey` — 169/169 | **PASS** |
| ReplayGolden 6/6 unchanged | `ReplayGoldenSuiteTests` | **PASS** |
| Platform corpus round-trip through WriteGate | `Platform_ship_slice100_corpus_roundtrip_through_WriteGate_*` | **PASS** |
| No 7208-record sensor in CI | curated fixtures + `--max-records` only | **PASS** |
| ZERO touch DelegationBridge | `git diff` empty | **PASS** |

## Test-criterion traceability

| Criterion | Test | Status |
|-----------|------|--------|
| Re-import hash stability | `CmoMarkdownImportGoldenTests::Platform_ship_slice100_reimport_identical_slice_preserves_catalog_ordering_hash` | COVERED |
| WriteGate E2E round-trip | `CmoMarkdownImportGoldenTests::Platform_ship_slice100_corpus_roundtrip_through_WriteGate_pins_ordering_hash` | COVERED |
| Prior golden regressions | existing `Weapon_*`, `Platform_fittings_*`, `Sensor_*` tests | COVERED |
| Replay golden | `ReplayGoldenSuiteTests` (6 tests) | COVERED |