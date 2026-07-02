# Data Population — CMO DB Reference (Clean-Room)

**Last Updated:** 2026-06-04  
**Requirement:** [06-Database-Intelligence](requirements/06-Database-Intelligence.md), [01-Project-Overview](requirements/01-Project-Overview.md) § Goal 5

## Purpose

Document how Project Aegis uses **cmano-db.com** as an **offline design reference only** — not as a shipped database or redistributable dataset. Production catalog data lives in `ProjectAegis.Data` / `assets/data/catalog/` with provenance and TL gates.

## Legal / product boundary

- **No** CMO proprietary DB, scenarios, or manual text in the product
- **Yes** observable patterns: structured platforms, sensors, weapons, validation, community intake workflow
- Reference export: `docs/reference/cmano-db/` (markdown, internal)
- Crawler: `tools/cmano-db-crawler/` (Node 18+, rate-limited)

## Pipeline

```text
cmano-db.com (viewer)
    → tools/cmano-db-crawler/harvest.mjs
    → _raw/*.json (gitignored)
    → render.mjs → docs/reference/cmano-db/*.md
    → export-catalog-sensors.mjs → assets/data/catalog/import/ (curated slices)
    → human review → SQLite migrations → ProjectAegis.Data.Catalog
```

## Commands

```bash
cd tools/cmano-db-crawler
node harvest.mjs all          # resumable crawl (~50 min full)
node render.mjs all           # markdown export
node export-catalog-sensors.mjs
node verify.mjs               # spot-check vs live site
```

## Agent roles (req 06)

| Agent | Tooling |
|-------|---------|
| Retrieval / normalization | `military-database-research`, catalog importer (P1) |
| Cross-system validation | `ScenarioValidationEngine`, `CatalogRulesValidationAgent` |
| Entity resolution / diff / consistency | `DatabaseIntelligenceOrchestrator`, MCP `catalog_intelligence_run` |
| Staged writes | `CatalogWriteGate` — `catalog_write_propose` / `catalog_write_approve` |
| Public intake | GitHub issue templates (future); quarantine → promote (`CatalogQuarantinePromoter`) |
| Balance drift | Agent-vs-agent batch runs (Phase 5) |

## Skills

- `team-data`, `sqlite-schema-management`, `deterministic-data-access`
- `database-branching-release-train` for versioned drops

## Status

| Item | Status |
|------|--------|
| Reference export v511 | Done — `docs/reference/cmano-db/cmano-db-data.md` |
| Crawler tooling | Done — `tools/cmano-db-crawler/README.md` |
| Baltic fixture / SQLite seed | Done — `CatalogSeedBootstrap`, `SqliteCatalogReader`, migration `005` |
| Entity-to-table map + audit | Done — `CatalogEntityMap`, `catalog_change_log`, `db_release` |
| DB agent stubs (headless) | Done — `DatabaseIntelligenceOrchestrator` + 4 agents |
| Full platform import | Not started — epic under req 06 Phase 2 |

## Related

- [implementation-tracker-2026-07-01.md](implementation-tracker-2026-07-01.md) — req 06 row (prior: [2026-06-30](implementation-tracker-2026-06-30.md), [2026-06-04](implementation-tracker-2026-06-04.md))
- [research-traceability.md](research-traceability.md)