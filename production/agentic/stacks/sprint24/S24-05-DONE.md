# S24-05 story-done evidence — phase-b-validator

**Branch:** `stack/sprint24/phase-b-validator`  
**Story:** `production/sprints/sprint-24-phase-b-import-present-polish.md` §S24-05  
**Status:** Complete

## Deliverables

- `PlatformWorkbookValidator`: Phase B header parity (Mobility/Signatures/Emcon), FK/orphan guard on `PlatformId`, Emcon enum sanity (`Condition`/`Posture`), mobility sanity (`MaxSpeedKnots`/`RangeNm` ≥ 0)
- Blocking `Error` findings via existing `PlatformImportPlan.Blocked` → `Stage()` refuses before propose
- `ValidationGoldenHashes`: pinned `PhaseBCleanWorkbook` + `PhaseBFixtureErrors` report hashes (PLE-4.3)
- Tests: `CatalogPhaseBValidationTests.cs` (16 tests); extended `PlatformWorkbookValidatorTests.cs` (+2 Phase A regression)
- Import test alignment: orphan mobility + E2E emcon posture updated for validator-first blocking

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Platform|CatalogPhaseB|Validation" -v minimal
# Passed: 105

dotnet test ProjectAegis.sln -v minimal
# Passed: 578 (87 Sim + 214 Data + 162 Delegation + 94 UnityAdapter + 21 Cli)
```

## Acceptance criteria

| AC | Verdict |
|----|---------|
| Header parity (Mobility/Signatures/Emcon) | **PASS** — `PLE-MOB/SIG/EMC-HEADER` blocking findings |
| FK/orphan guard (`PlatformId` ∈ Platforms) | **PASS** — `PLE-PHB-ORPHAN` on all Phase B sheets |
| Emcon enum sanity (case-insensitive) | **PASS** — `PLE-EMCON-CONDITION` / `PLE-EMCON-POSTURE` |
| Mobility sanity (non-negative speed/range) | **PASS** — `PLE-MOB-SPEED` / `PLE-MOB-RANGE` |
| Blocking findings before staging | **PASS** — `CatalogPhaseB_validation_error_blocks_importer_staging` |
| Validation hash golden pinned | **PASS** — clean + fixture error hashes in `ValidationGoldenHashes` |
| Phase A validation regression unchanged | **PASS** — existing `PlatformWorkbookValidatorTests` green |