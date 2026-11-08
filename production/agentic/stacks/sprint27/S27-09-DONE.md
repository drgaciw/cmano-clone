# S27-09 story-done — Browse Projection Enrichment

**Story:** `production/epics/sprint-27-cmo-corpus-import/story-027-09-browse-enrichment.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| MountCount/SensorCount from reader | `CatalogPlatformBrowseProjectionTests` 5/5 | COVERED |
| CLI schemaVersion 2 + counts | `CatalogPlatformBrowseCommandTests` 2/2 | COVERED |
| Stable PlatformId sort | existing + new count tests | COVERED |
| ICatalogReader.GetSortedMounts | SqliteCatalogReader + InMemoryCatalogReader | COVERED |