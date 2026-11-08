# S33-03 — Kill-Chain Rule Pack Evidence

**Story:** `production/epics/sprint-33-kill-chain-intelligence/story-033-03-kill-chain-rule-pack.md`  
**Date:** 2026-06-19  
**Owner:** team-data  
**Branch:** `stack/sprint33/kill-chain-rule-pack`

## Summary

Bounded detect-only kill-chain rules R1–R4 are implemented in `KillChainRules` and wired into `CatalogRulesValidationAgent` after existing `RULE_GATE_REJECT` logic. Findings are deterministic (ordinal sort on code, then message). Quarantined / non-approved dependency edges are excluded via `GetSortedDependencyEdges()` (S33-02 graph index). Report path is read-only — no live-table mutation.

## Rules (DBI-3.5)

| Code | Rule | Severity | Heuristic |
|------|------|----------|-----------|
| `KILL_CHAIN_ORPHAN_EDGE` | R1 | error | Edge references missing platform (`TryGetCombatRadiusNm`), mount, sensor (approved binding), or weapon (`TryGetWeaponEnvelope`) |
| `KILL_CHAIN_RANGE_EXCEEDS_SENSOR` | R2 | error | Weapon `MaxRangeMeters` > platform sensor envelope (`combatRadiusNm × 1852 × BasePd` max over approved sensors) |
| `KILL_CHAIN_SPEED_MISMATCH` | R3 | error / warning | Weapon inferred min speed > `TryGetMobility` max speed; missing mobility → warning only |
| `KILL_CHAIN_WEAPON_EXCEEDS_PLATFORM_REACH` | R4 | error | Weapon `MaxRangeMeters` > `TryGetCombatRadiusNm × 1852` |

## Golden hash (clean Baltic)

| Fixture | Kill-chain findings hash |
|---------|--------------------------|
| `InMemoryCatalogReader.BalticPatrolFixture()` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` (empty) |

Pinned in `KillChainGoldenHashes.BalticPatrolClean`. Baltic `WORLD_HASH` unchanged.

## Verify commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "KillChain|CatalogRules|Validation|CrossSystem" -v minimal
dotnet test ProjectAegis.sln -v minimal
```

## Test counts (2026-06-19)

| Suite | Before (S33-02) | After (S33-03) | Delta |
|-------|-----------------|----------------|-------|
| `KillChain\|CatalogRules\|Validation\|CrossSystem` filter | 57 | **74** | +17 |
| `ProjectAegis.Data.Tests` full | 348 | **365** | +17 |
| `ProjectAegis.sln` full | 1092 | **1118** | +26 |

### New tests (`KillChainRulePackTests.cs` — 17)

1. `KillChain_R1_orphan_edge_flags_missing_platform`
2. `KillChain_R1_orphan_edge_flags_missing_mount`
3. `KillChain_R1_orphan_edge_flags_missing_weapon`
4. `KillChain_R1_orphan_edge_flags_missing_sensor`
5. `KillChain_R2_range_exceeds_sensor_when_weapon_outranges_envelope`
6. `KillChain_R2_boundary_short_range_weapon_passes_sensor_envelope`
7. `KillChain_R3_speed_mismatch_when_hypersonic_weapon_exceeds_platform_speed`
8. `KillChain_R3_missing_mobility_emits_warning_not_error`
9. `KillChain_R4_weapon_exceeds_platform_reach_when_range_beyond_combat_radius`
10. `KillChain_R4_boundary_weapon_within_platform_reach_passes`
11. `KillChain_Baltic_patrol_golden_hash_stable_on_clean_fixture`
12. `KillChain_findings_are_deterministically_sorted`
13. `KillChain_quarantined_sensor_edges_excluded_from_checks`
14. `CatalogRulesValidationAgent_appends_kill_chain_findings_after_rule_gate`
15. `CrossSystem_orchestrator_Baltic_default_has_no_kill_chain_codes_on_clean_catalog`
16. `CrossSystem_orchestrator_surfaces_kill_chain_codes_on_curated_fixture`
17. `CatalogRules_detect_only_does_not_mutate_catalog_on_report_path`

## Hard gates

| Gate | Result |
|------|--------|
| Detect-only (no live-table mutation on report path) | PASS |
| Quarantined edges excluded from kill-chain checks | PASS — graph index + approved-sensor envelope partition |
| ZERO touch `DelegationBridge.cs` | PASS — no diff |
| Baltic + in-memory fixtures only in CI | PASS |
| Full sln ≥1092 | PASS — **1118/1118** |

## Files changed

- **NEW** `src/ProjectAegis.Data/Validation/KillChainRules.cs`
- **NEW** `src/ProjectAegis.Data/Validation/KillChainGoldenHashes.cs`
- **MODIFY** `src/ProjectAegis.Data/Agents/CatalogRulesValidationAgent.cs`
- **MODIFY** `src/ProjectAegis.Data/Catalog/CatalogWeaponDefaults.cs`
- **MODIFY** `src/ProjectAegis.Data/Catalog/CatalogWeaponIds.cs`
- **NEW** `src/ProjectAegis.Data.Tests/Validation/KillChainRulePackTests.cs`
- **NEW** `production/agentic/sprint-33-kill-chain-rules-2026-06-19.md`