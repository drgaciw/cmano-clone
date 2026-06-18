---
id: S27-09
status: Ready
type: Logic
priority: should-have
graphite_branch: stack/sprint27/browse-projection-enrich
estimate_days: 0.5
dependencies:
  - S27-03 complete
owner: team-data
sprint: 27
req_trace: Req 21 Phase C; ADR-011 browse
---

# Story 027-09 — Browse Projection Enrichment

> **Epic:** sprint-27-cmo-corpus-import  
> **Unlocks:** S27-08 platform viewer (recommended before Unity bind)

## Summary

Extend `CatalogPlatformBrowseProjection` + `CatalogPlatformBrowseRow` with `MountCount` and `SensorCount`; version CLI `catalog_platform_browse` JSON schema.

## Acceptance Criteria

- [ ] `MountCount`, `SensorCount` populated from `ICatalogReader` sorted paths
- [ ] CLI JSON schema versioned; existing tests updated
- [ ] Stable sort by `PlatformId` preserved (determinism)
- [ ] `CatalogPlatformBrowseProjectionTests` PASS

## QA Test Cases

- **AC-1**: Counts match reader
  - Given: Baltic fixture with known mount/sensor counts
  - When: browse projection runs
  - Then: counts match `ICatalogReader` queries
  - Edge cases: platform with zero mounts

## References

- S26-10 spike: `production/qa/sprint-26-platform-viewer-spike-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`