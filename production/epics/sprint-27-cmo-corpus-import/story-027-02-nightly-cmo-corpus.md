---
id: S27-02
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint27/nightly-cmo-corpus
estimate_days: 2
dependencies:
  - S27-01 green baseline
owner: team-data
sprint: 27
req_trace: Req 06 Phase 2; DBI-2.4 bulk path
---

# Story 027-02 — Nightly Full CMO Corpus Import Job

> **Epic:** sprint-27-cmo-corpus-import  
> **ADR:** ADR-006 (engine-free), ADR-011 (write-gate governance)

## Summary

Off-CI nightly pipeline for full `sensor.md` (7208) + `weapon.md` corpora — propose-only with quarantine artifacts. **Producer v1: sensor + weapon only** (no platform slices in first job). CI keeps curated fixtures + `--max-records`.

## Acceptance Criteria

- [x] Job script/workflow (e.g. `tools/cmo-nightly-import.sh`) invokes `catalog_import_markdown` per entity
- [x] Full sensor propose-only run completes with batch count + quarantine JSON
- [x] Weapon corpus included in v1 job
- [x] Chunk 500/batch; no direct SQLite writes
- [x] **Not** wired into `dotnet test` CI
- [x] Evidence: `production/qa/sprint-27-nightly-cmo-import-*.md`

## QA Test Cases

- **AC-1**: Nightly job completes propose-only
  - Given: full `sensor.md` path
  - When: job runs with chunk 500
  - Then: staged batches + quarantine artifact; no commit without approve
  - Edge cases: job timeout; partial failure recovery documented

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — impact before any gate edit |
| `CmoMarkdownImporter` | HIGH |

## References

- Phase 2 plan: `docs/superpowers/plans/2026-06-04-catalog-phase2-import.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 6/6 passing (local gate `MAX_RECORDS=12`; full 7208 off-CI)
**Deviations**: None
**Test Evidence**: `production/qa/sprint-27-nightly-cmo-import-2026-06-18.md`
**Code Review**: Skipped (lean mode)