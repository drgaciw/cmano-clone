---
id: S34-03
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint34/linkcatalog-roundtrip
estimate_days: 1.5
dependencies:
  - S34-02 merged
owner: team-data
sprint: 34
req_trace: Req 21 PLE-1/2; ADR-011
sprint_gate: true
---

# Story 034-03 — LinkCatalog Workbook Round-Trip

> **Epic:** sprint-34-link-catalog-data

## Summary

Emit `LinkCatalog` sheet from exporter; importer diffs and stages via `ProposeLinkCatalogBatch`. Unedited round-trip yields empty diff. Bump `SchemaVersion` with golden hash update.

## Acceptance Criteria

- [ ] Export sheet: `LinkId`, `DisplayName`, `LinkType`, `LatencyMsNominal`
- [ ] `PlatformWorkbookRoundTripTests` empty-diff PASS
- [ ] Bulk-author threshold (>10 rows) requires explicit approve
- [ ] Evidence: `production/agentic/sprint-34-linkcatalog-roundtrip-*.md`