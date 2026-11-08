# Epic: Sprint 27 — CMO Corpus Import Pipeline

> **Status:** Ready  
> **Sprint:** 27  
> **Dates:** 2026-09-04 → 2026-09-17  
> **Trunk:** `main` @ `ab30d35` (698/698; ReplayGolden 6/6)  
> **Layer:** Content / Data  
> **GDD:** `design/gdd/` (catalog population); Req 06, Req 21 continuity

## Goal

Scale bounded CMO corpus intake to a **nightly off-CI pipeline** (sensor + weapon v1), complete **mount→loadout→magazine** markdown import through extend-only `CatalogWriteGate`, and enrich browse projection for Phase C viewer.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Engine-free `ProjectAegis.Data` boundary |
| ADR-011 | Accepted | Write-gate import; Phase C browse enrichment |

## Graphite Stack (merge order)

```
main
 └── stack/sprint27/full-sln-gate              (S27-01)
      ├── stack/sprint27/nightly-cmo-corpus    (S27-02) — parallel after S27-01
      └── stack/sprint27/cmo-loadout-magazine  (S27-03) — CRITICAL
           └── stack/sprint27/import-golden-hygiene (S27-04)
                ├── stack/sprint27/browse-projection-enrich (S27-09)
                └── stack/sprint27/platform-corpus-slice   (S27-14 — nice)
```

**Sprint fails** if S27-04 loadout/magazine round-trip does not land.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 01 | [Full-solution re-baseline](story-027-01-full-sln-gate.md) | S27-01 | Config | must-have | 1d | Ready |
| 02 | [Nightly CMO corpus job](story-027-02-nightly-cmo-corpus.md) | S27-02 | Integration | must-have | 2d | Ready |
| 03 | [Loadout/magazine CMO import](story-027-03-loadout-magazine-import.md) | S27-03 | Integration | must-have | 2d | Ready |
| 04 | [Import golden hygiene](story-027-04-import-golden-hygiene.md) | S27-04 | Integration | must-have | 1d | Ready |
| 09 | [Browse projection enrichment](story-027-09-browse-enrichment.md) | S27-09 | Logic | should-have | 0.5d | Ready |
| 14 | [Platform corpus slice](story-027-14-platform-corpus-slice.md) | S27-14 | Integration | nice-to-have | 1d | Ready |

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CmoMarkdownImporter`, `ICatalogReader`, `CatalogPlatformBrowseProjection`

## Definition of Done

- [ ] S27-01..04 complete (must-have)
- [ ] Nightly job v1: sensor + weapon only (producer-approved)
- [ ] `dotnet test` WriteGate + CmoMarkdown filters green
- [ ] ReplayGolden 6/6 unchanged
- [ ] Tracker row 06 updated

## References

- Kickoff: `production/sprints/sprint-27-cmo-corpus-combat-bounded.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`
- Track plan: `production/agentic/sprint-27-plan-data-2026-06-18.md`