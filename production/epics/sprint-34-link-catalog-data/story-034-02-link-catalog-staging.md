---
id: S34-02
status: Complete
type: Logic
priority: must-have
graphite_branch: stack/sprint34/link-catalog-staging
estimate_days: 2
dependencies:
  - S34-01 green baseline
owner: team-data
sprint: 34
req_trace: Req 21 LinkCatalog; ADR-011; DBI-1.4
sprint_gate: true
---

# Story 034-02 — Link Catalog Data Model + Write-Gate

> **Epic:** sprint-34-link-catalog-data

## Summary

Add `CatalogLinkEntry`, `ICatalogReader.GetSortedLinks()`, `catalog_staging_link`, and `ProposeLinkCatalogBatch` / approve upsert. Seed Baltic fixture link rows for comms FK resolution. Extend-only `CatalogWriteGate`.

## Acceptance Criteria

- [x] `GetSortedLinks()` stable `link_id ASC` ordering
- [x] `ApproveBatch` commits link rows; `RejectBatch` unchanged
- [x] Baltic fixture seeded (`NATO_TADIL_J`, `SATCOM_B` or equivalent)
- [x] `WriteGate|LinkCatalog` filter PASS
- [x] Evidence: `production/agentic/sprint-34-link-catalog-staging-*.md`