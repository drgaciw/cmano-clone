---
id: S34-08
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint34/catalog-link-cli
estimate_days: 0.5
dependencies:
  - S34-02 merged
owner: team-data
sprint: 34
req_trace: DBI-4.5; Req 21
---

# Story 034-08 — catalog_link_report CLI

> **Epic:** sprint-34-link-catalog-data

## Summary

Read-only CLI verb `catalog_link_report` — deterministic sorted stdout; no live-table mutation. Mirrors S33-08 kill-chain CLI contract.

## Acceptance Criteria

- [x] Sorted link rows on stdout
- [x] Cli tests PASS
- [x] No write-gate bypass