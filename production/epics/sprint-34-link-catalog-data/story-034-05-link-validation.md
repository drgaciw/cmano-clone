---
id: S34-05
status: Complete
type: Logic
priority: should-have
graphite_branch: stack/sprint34/link-validation
estimate_days: 1
dependencies:
  - S34-03 merged
owner: team-data
sprint: 34
req_trace: Req 21 PLE-4; DBI-3.1/3.5
---

# Story 034-05 — Link FK + Validation Rules

> **Epic:** sprint-34-link-catalog-data

## Summary

Extend `CatalogRulesValidationAgent` with `LINK_ORPHAN_COMMS`, `LINK_TYPE_INVALID`, `LINK_LATENCY_INVALID`. Detect-only — no auto-repair. Orchestrator surfaces codes on platform batches.

## Acceptance Criteria

- [x] Orphan `Comms.LinkId` → `LINK_ORPHAN_COMMS`
- [x] Deterministic `ValidationReport` golden stable on Baltic
- [x] Evidence: `production/agentic/sprint-34-link-validation-*.md`