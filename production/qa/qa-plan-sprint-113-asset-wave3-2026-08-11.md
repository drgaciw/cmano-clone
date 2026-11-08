# QA Plan — Sprint 113 Asset Specced→Done Wave 3

**Date:** 2026-08-11  
**Sprint:** S113  
**Mode:** lean / docs+assets only

## Automated (serial closeout)

| Gate | Command / check | Pass |
|------|-----------------|------|
| Build | `dotnet build ProjectAegis.sln` | 0 Error(s) |
| Suite | `dotnet test ProjectAegis.sln -v minimal` | ≥1638, 0 failed |
| PlayModeSmoke | filter `PlayModeSmokeHarnessTests` | ≥20 passed |
| ReplayGolden | 6/6 in suite or filter | 6/6 |
| Hash | grep `17144800277401907079` | present |
| Bridge | no new DelegationBridge hotpath product edits | ZERO |

## Manual / static

| Check | Method |
|-------|--------|
| ASSET-007/008 Done | Files under `production/assets/c2/`; manifest Status **Done** |
| Count honesty | Specced decreased by 2; Done increased by 2 vs pre-wave |
| No Approved | Neither 007 nor 008 marked Approved |
| S36 pack | DRG-22…28 Done/Canceled with evidence paths |
| Stage | `production/stage.txt` still Release |

## Out of scope

Unity Editor PNG capture · Play Mode human smoke · store screenshots · audio generation
