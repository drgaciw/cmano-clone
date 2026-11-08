# S24-11 story-done evidence — closedxml-phase-b-ux

**Branch:** `stack/sprint24/closedxml-phase-b-ux`  
**Story:** `production/sprints/sprint-24-phase-b-import-present-polish.md` §S24-11  
**Status:** Complete

## Deliverables

- `PlatformEmconEnums`: migration-008 allowed values (`silent|restricted|free`, `off|standby|active`)
- `ClosedXmlPlatformWorkbookIo`: Emcon `Condition`/`Posture` Excel list validation (rows 2–1000, in-cell dropdown)
- Scope lock: Mobility/Signatures/Emcon sheets only — no damage-model columns
- New test project `ProjectAegis.Data.Excel.Tests` (added to `ProjectAegis.sln`)
- `ClosedXmlValidationMetadataTests`: proves PLE-1.2 enum dropdowns on Emcon only
- `PlatformWorkbookBinaryGoldenTests`: pinned Phase B workbook hash + binary `.xlsx` round-trip + edited Emcon diff

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Excel.Tests/ProjectAegis.Data.Excel.Tests.csproj -v minimal
# Passed: 5
```

## Acceptance criteria

| AC | Verdict |
|----|---------|
| Emcon Condition/Posture enum dropdowns per Req 21 | **PASS** — list validation on `Emcon` sheet columns |
| Binary golden for Phase B edited workbook round-trip | **PASS** — `Phase_B_binary_round_trip_preserves_workbook_hash_golden` + `Phase_B_edited_emcon_posture_round_trips_with_deterministic_diff` |
| `Data.Excel` tests PASS | **PASS** — 5/5 |
| Scope lock (no damage model) | **PASS** — Emcon enums only |