# S34-02 — Link Catalog Staging Evidence

**Date:** 2026-06-19  
**Story:** S34-02  
**Commit:** (uncommitted workspace)

## Scope

- `CatalogLinkEntry`, `CatalogLinkTypes`
- `ICatalogReader.GetSortedLinks()`, `TryGetLinkLatencyMs()`
- Migration `011_link_catalog_staging.sql` → `catalog_staging_link`
- `CatalogWriteGate.ProposeLinkCatalogBatch` / approve upsert / reject purge
- `CatalogValidationDefaults.BalticLinks()` (`NATO_TADIL_J`, `SATCOM_B`)
- `InMemoryCatalogReader` links + Baltic fixture seed

## Verification

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|LinkCatalog" -v minimal
# Passed: 30/30

dotnet test ProjectAegis.sln -v minimal
# Passed: 1148/1148 (+5 new link tests vs S33 baseline 1143)
```

## Acceptance

| Criterion | Result |
|-----------|--------|
| `GetSortedLinks()` stable `link_id ASC` | PASS — `LinkCatalogStagingTests.GetSortedLinks_orders_by_link_id_ordinal` |
| `ApproveBatch` commits; `RejectBatch` purges staging | PASS — propose/approve/reject tests |
| Baltic fixture seeded | PASS — `Baltic_fixture_seeds_NATO_and_SATCOM_links_with_latency` |
| `WriteGate\|LinkCatalog` filter | PASS — 30/30 |
| Extend-only `CatalogWriteGate` | PASS — no DelegationBridge touch |

## Notes

- `CatalogEntityMapTests` extended for `CatalogLinkEntry` → `link_catalog`
- Four `FakeWriteGate` stubs updated for `ProposeLinkCatalogBatch`