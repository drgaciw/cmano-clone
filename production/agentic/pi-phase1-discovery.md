# PI Phase 1 — Discovery (Agents A–E)

**Worktree:** `.worktrees/pi-agentic-impl` @ `stack/pi-agentic-impl` (merged sprint10)  
**Date:** 2026-06-03

## Agent A — Test infrastructure

| Project | Framework | Count (approx) |
|---------|-----------|----------------|
| `ProjectAegis.Data.Tests` | xUnit | 24 |
| `ProjectAegis.Sim.Tests` | xUnit | 50 |
| `ProjectAegis.Delegation.Tests` | NUnit | 132 |
| `ProjectAegis.Delegation.UnityAdapter.Tests` | NUnit | 65 |
| `ProjectAegis.MissionEditor.Cli.Tests` | xUnit | 5 |

**Coverage strengths:** catalog import/quarantine, validation engine (`STRIKE_UNREACHABLE`), Baltic replay goldens, fuel ledger, order-log fingerprints.

**Gaps addressed in Phase 3:** JSON round-trip tests (policy + catalog DTOs).

## Agent B — SQLite / catalog

| Flow | Types |
|------|--------|
| Read | `SqliteCatalogReader`, `InMemoryCatalogReader` |
| Write | `CatalogJsonImporter`, `CatalogBulkImporter`, `CatalogQuarantinePromoter`, `CatalogSeedBootstrap` |
| Gate | `CatalogImportGate.PartitionForImport` |

**Risk:** `SqliteCatalogReader.TableHasColumn` interpolated table name into SQL (internal callers only). **Fixed:** identifier whitelist in Phase 3.

All INSERT/SELECT paths reviewed use `$parameters`.

## Agent C — JSON contracts

| Area | Serializer | DTOs |
|------|------------|------|
| Scenario policy | `ScenarioPolicyJsonLoader` | `ScenarioPolicyJsonDto`, `ScenarioLogisticsJsonDto` |
| Catalog import | `CatalogJsonImporter` | `CatalogSensorsFileDto` |
| Scenario authoring | `ScenarioDocumentJsonLoader` | `ScenarioDocumentDto` |
| Validation export | `ValidationReportJson` | findings hash canonical |

**Risk:** camelCase + case-insensitive deserialize; extra JSON fields ignored by default (good). Missing fields get defaults — needs round-trip tests.

## Agent D — Security

| Finding | Severity | Notes |
|---------|----------|-------|
| `TableHasColumn` string interpolation | Medium | Table names internal; fixed with whitelist |
| Catalog JSON from disk | Medium | Trust path; validate in MCP/host |
| `Data Source={path}` connection strings | Low | Path is host-controlled |
| No secrets in test fixtures | OK | — |

See `pi-security-findings.md`.

## Agent E — Architecture

```
ProjectAegis.Data  →  (read-only catalog/validation)
ProjectAegis.Sim   →  tick, engage, policy, scenario JSON
ProjectAegis.Delegation → order log, orchestration (no UnityEngine)
UnityAdapter     →  DelegationBridge, BalticReplayHarness
```

**High coupling:** `DelegationBridge`, `DecisionLog`, `BalticReplayHarness`, `SimulationSession`.

**Safe seams:** `ICatalogReader`, `IEngagementResolver`, `IOrderLog`, `ScenarioPolicyRepository`.

**No broad refactor in PI slice** — tests + SQL hardening only.