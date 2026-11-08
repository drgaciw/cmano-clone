# S34-05 — Link FK + Validation Rules Evidence

**Story:** `production/epics/sprint-34-link-catalog-data/story-034-05-link-validation.md`  
**Date:** 2026-06-19  
**Owner:** team-data  
**Branch:** `stack/sprint34/link-validation`

## Summary

Detect-only link-catalog integrity rules are implemented in `LinkCatalogRules` and wired into `CatalogRulesValidationAgent` after existing `RULE_GATE_REJECT` and `KillChainRules` findings. Findings are deterministic (ordinal sort on code, then message). Report path is read-only — no live-table mutation.

## Rules (Req-21 / DBI-3.1)

| Code | Rule | Severity | Heuristic |
|------|------|----------|-----------|
| `LINK_ORPHAN_COMMS` | FK | error | `Comms.LinkId` not present in `GetSortedLinks()` lookup |
| `LINK_TYPE_INVALID` | type | error | `link_type` not in `CatalogLinkTypes` (`strategic`, `tactical`, `voice`, `satcom`) |
| `LINK_LATENCY_INVALID` | bounds | error | `LatencyMsNominal` outside `[0, 300_000]` ms |

## Golden hash (clean Baltic)

| Fixture | Link validation findings hash |
|---------|-------------------------------|
| `InMemoryCatalogReader.BalticPatrolFixture()` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` (empty) |

Pinned in `LinkCatalogGoldenHashes.BalticPatrolClean`. Baltic `WORLD_HASH` unchanged.

## Verify commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Link|KillChain|Validation" -v minimal
```

## Test counts (2026-06-19)

| Suite | Before (S34-04) | After (S34-05) | Delta |
|-------|-----------------|----------------|-------|
| `Link\|KillChain\|Validation` filter | 85 | **102** | +17 |
| `ProjectAegis.Data.Tests` full | — | **102** (filter) | — |

### New tests (`LinkCatalogRulePackTests.cs` — 17)

1. `Link_orphan_comms_flags_missing_link_catalog_entry`
2. `Link_orphan_comms_passes_when_comms_resolves_to_catalog_entry`
3. `Link_type_invalid_flags_unknown_link_type`
4. `Link_type_invalid_passes_for_allowed_catalog_link_types` (×4 theory cases)
5. `Link_latency_invalid_flags_negative_latency`
6. `Link_latency_invalid_flags_latency_above_max`
7. `Link_latency_invalid_passes_within_bounds` (×3 theory cases)
8. `Link_Baltic_patrol_golden_hash_stable_on_clean_fixture`
9. `Link_findings_are_deterministically_sorted`
10. `CatalogRulesValidationAgent_appends_link_findings_after_kill_chain`
11. `CrossSystem_orchestrator_Baltic_default_has_no_link_codes_on_clean_catalog`
12. `CrossSystem_orchestrator_surfaces_link_codes_on_curated_fixture`
13. `CatalogRules_detect_only_does_not_mutate_catalog_on_link_report_path`

## Hard gates

| Gate | Result |
|------|--------|
| Detect-only (no live-table mutation on report path) | PASS |
| ZERO touch `DelegationBridge.cs` | PASS — no diff |
| `CatalogWriteGate` extend-only — no bypass | PASS — no write-gate changes |
| Baltic clean catalog golden stable | PASS |
| Filter suite `Link\|KillChain\|Validation` | PASS — **102/102** |

## Files changed

- **NEW** `src/ProjectAegis.Data/Validation/LinkCatalogRules.cs`
- **NEW** `src/ProjectAegis.Data.Tests/Validation/LinkCatalogRulePackTests.cs`
- **MOD** `src/ProjectAegis.Data/Agents/CatalogRulesValidationAgent.cs` — append `LinkCatalogRules.Evaluate`
- **MOD** `src/ProjectAegis.Data/Catalog/LinkCatalogGoldenHashes.cs` — add `BalticPatrolClean` findings hash