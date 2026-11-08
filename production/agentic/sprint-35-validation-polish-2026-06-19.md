# S35-12 — Platform Editor C–H Validation Polish Evidence

**Story:** `production/epics/sprint-35-polish-foundation/story-035-12-validation-polish.md`  
**Date:** 2026-06-19  
**Owner:** team-data  
**Branch:** `stack/sprint35/validation-polish`

## Summary

Polished `LINK_*` diagnostics in `LinkCatalogRules` for Platform Editor phases G–H round-trip failures. Messages now reference workbook sheet/column names (`Comms`, `LinkCatalog`, `LinkId`, `LinkType`, `LatencyMsNominal`) and include repair hints. Detect-only — no `CatalogWriteGate` behavior changes; SchemaVersion **010** frozen (no migration edits).

## Message improvements

| Code | Before (S34-05) | After (S35-12) |
|------|-----------------|----------------|
| `LINK_ORPHAN_COMMS` | `{platform}/{link}: link '{link}' missing from link_catalog` | `Comms/{platform}: LinkId '{link}' not found in LinkCatalog sheet — add a LinkCatalog row or correct Comms.LinkId before re-import` |
| `LINK_TYPE_INVALID` | `{link}: link_type '{type}' not in CatalogLinkTypes` | `LinkCatalog/{link}: LinkType '{type}' invalid — set LinkType to one of strategic, tactical, voice, satcom` |
| `LINK_LATENCY_INVALID` | `{link}: latency_ms_nominal={ms} out of bounds [0,300000]` | `LinkCatalog/{link}: LatencyMsNominal={ms} out of range [0,300000] ms — fix LinkCatalog row before approve` |

Public formatters (`FormatOrphanCommsMessage`, `FormatTypeInvalidMessage`, `FormatLatencyInvalidMessage`) pin stable text for tests and orchestrator consumers.

## Verify commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test --filter "Link|Validation|PlatformWorkbook" -v minimal
```

## Test results (2026-06-19)

| Project | Passed | Failed | Skipped |
|---------|--------|--------|---------|
| `ProjectAegis.Data.Tests` | **150** | 0 | 0 |
| `ProjectAegis.Delegation.UnityAdapter.Tests` | **17** | 0 | 0 |
| `ProjectAegis.Sim.Tests` | **26** | 0 | 0 |
| `ProjectAegis.Data.Excel.Tests` | **5** | 0 | 0 |
| `ProjectAegis.MissionEditor.Cli.Tests` | **3** | 0 | 0 |
| **Filter total** | **201** | **0** | 0 |

### New / updated tests

1. `Link_validation_messages_stable_on_curated_invalid_fixture` — pins all three `LINK_*` messages on curated fixture
2. `Approve_link_batch_extend_only_preserves_existing_catalog_rows` — seed + extend approve keeps prior rows
3. `CatalogWriteGate_has_zero_forbidden_link_catalog_delete_surfaces` — source scan: no `DELETE FROM link_catalog`

Existing `LinkCatalogRulePackTests` assertions updated to exact formatter output.

## Hard gates

| Gate | Result |
|------|--------|
| Detect-only (no live-table mutation on report path) | PASS — unchanged |
| Deterministic `ValidationReport` ordering | PASS — code → message ordinal sort unchanged |
| `CatalogWriteGate` extend-only | PASS — **ZERO** `DELETE FROM link_catalog` in `CatalogWriteGate.cs`; link commit uses `INSERT OR REPLACE` upsert only |
| SchemaVersion **010** frozen | PASS — no migration files touched |
| Baltic clean catalog golden stable | PASS — empty findings hash unchanged (`e3b0c442…`) |
| Filter suite `Link\|Validation\|PlatformWorkbook` | PASS — **201/201** |

## Files changed

- **MOD** `src/ProjectAegis.Data/Validation/LinkCatalogRules.cs` — actionable sheet-aware messages + public formatters
- **MOD** `src/ProjectAegis.Data.Tests/Validation/LinkCatalogRulePackTests.cs` — stable message pins + curated fixture test
- **MOD** `src/ProjectAegis.Data.Tests/WriteGate/LinkCatalogStagingTests.cs` — extend-only behavioral + source scan tests