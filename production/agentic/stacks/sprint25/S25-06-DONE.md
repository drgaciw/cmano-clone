# S25-06 story-done evidence — damage-validator

**Branch:** `stack/sprint25/damage-validator`  
**Story:** `production/sprints/sprint-25-phase-b-damage-assurance.md` §S25-06  
**Status:** Complete

## Deliverables

- `PlatformWorkbookValidator`: Platforms header parity + damage rule pack
  - `PLE-DMG-HP` — MaxHp ≤ 0
  - `PLE-DMG-HP-CEIL` — MaxHp > 100_000
  - `PLE-DMG-WITHDRAW` — WithdrawThresholdPct < 0 or > MaxHp (sign-off DBI-2.2)
  - `PLE-DMG-FLAGS` — CriticalFlags < 0
- `ValidationGoldenHashes.PhaseBDamageFixtureErrors` pinned
- `CatalogPhaseBDamageValidationTests.cs` — 13 tests (bounds, golden, staging gate)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Platform|CatalogPhaseB|CatalogPhaseBDamage|Validation" -v minimal
dotnet test ProjectAegis.sln -v minimal
# Data.Tests: 245 PASS; full solution: 623 PASS
```

## Acceptance criteria

| AC | Verdict |
|----|---------|
| HP bounds blocking (MaxHp ≤ 0, ceiling) | **PASS** |
| Withdraw threshold sanity (≤ MaxHp, non-negative) | **PASS** |
| CriticalFlags bitmask sanity | **PASS** |
| Validation hash golden pinned | **PASS** — `PhaseBDamageFixtureErrors` |
| Blocking findings prevent staging | **PASS** — importer `Plan.Blocked` |
| Phase A/B regression unchanged | **PASS** — existing `CatalogPhaseBValidationTests` green |