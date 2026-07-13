# S27-03 story-done — CMO loadout/magazine import

**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- `CmoMarkdownImporter.ReadPlatformLoadouts`, `PartitionPlatformMagazines`, `BuildWeaponNameLookup`
- `CmoMarkdownImportProposer` — `ProposeLoadoutBatch` + `ProposeMagazineBatch` in platform paths (extend-only)
- Tests: `CmoMarkdownLoadoutMagazineTests.cs`

## Verify

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdownLoadoutMagazine|CmoMarkdownPlatform" -v minimal
```

## Verdict: COMPLETE

| AC | Test | Status |
|----|------|--------|
| Loadout + magazine from platform tables | `ReadPlatformLoadouts` / `PartitionPlatformMagazines` | COVERED |
| Write-gate batches wired | `ProposePlatformWeaponMounts_stages_loadout_and_magazine_batches` | COVERED |
| Baltic E2E read-back | 3 loadouts + 3 magazines committed | COVERED |
| Orphan FK quarantine | `hostile-far` generic missile → `orphan_weapon_id` | COVERED |
| Chunk 500 preserved | existing platform chunk path unchanged | COVERED |