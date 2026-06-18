---
id: S27-14
status: Ready
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint27/platform-corpus-slice
estimate_days: 1
dependencies:
  - S27-03 complete
owner: team-data
sprint: 27
req_trace: Req 06 corpus scale
---

# Story 027-14 — Curated Platform Corpus Slice

> **Epic:** sprint-27-cmo-corpus-import  
> **Note:** Platform slices deferred from nightly v1 (S27-02); this is bounded CI/nightly expansion.

## Summary

Add `ship-slice-100.md` fixture (≥100 platforms) + chunk boundary test (501 records → 2 batches) mirroring S26-02 weapon pattern.

## Acceptance Criteria

- [ ] Fixture under `tools/cmano-db-crawler/fixtures/` (≥100 platforms)
- [ ] Chunk boundary test: 501 → 2 batches
- [ ] Quarantine report JSON emitted on invalid rows
- [ ] Scoped CmoMarkdown tests PASS

## QA Test Cases

- **AC-1**: Chunk boundary
  - Given: slice with 501 platform records
  - When: import with chunk 500
  - Then: exactly 2 propose batches

## References

- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`