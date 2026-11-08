# S27-02 story-done — Nightly CMO corpus import job

**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- `tools/cmo-nightly-import.sh` — sensor + weapon propose-only, chunk 500, quarantine JSON
- Evidence: `production/qa/sprint-27-nightly-cmo-import-2026-06-18.md`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Job script per entity | `tools/cmo-nightly-import.sh` | COVERED |
| Sensor + weapon v1 | script paths to `docs/reference/cmano-db/` | COVERED |
| Quarantine artifact | `--report-out` JSON files | COVERED |
| Off-CI / not in dotnet test | standalone tool | COVERED |