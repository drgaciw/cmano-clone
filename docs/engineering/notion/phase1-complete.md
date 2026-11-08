# Phase 1 complete — additive schema

**Date:** 2026-07-24  
**Result:** ALL_OK

## Applied properties (all six DBs)

| Property | Type (DDL) | Present |
|----------|------------|---------|
| `Source path` | `rich_text` | Yes |
| `UID` | `rich_text` | Yes |
| `Last synced` | `date` | Yes |
| `Sync note` | `rich_text` | Yes |
| `Status` (Runbooks only) | `select('Draft','Current','Deprecated')` | Yes |

### DDL note

Notion MCP `notion-update-data-source` expects Notion type keywords (`rich_text`, `date`, `select(...)`), **not** SQL `TEXT`.

## Final schemas (property names)

- **requirements:** Created, Last reviewed, Last synced, Last updated, Name, Notes / Open questions, Owner, Priority, Source (legacy), Source path, Specs, Status, Sync note, Type, UID  
- **specs:** Created, Determinism-impacting, GitHub Issue/PR, Last synced, Last updated, Name, Owner, Requirements, Source path, Spec status, Subsystem, Sync note, Target, UID  
- **adrs:** Consequences (short), Created, Date, Decision type, GitHub PR/commit, Last synced, Last updated, Source path, Status, Sync note, Title, UID  
- **milestones:** Created, Last synced, Last updated, Name, Release tag/URL, Scope (short), Source path, Status, Sync note, Target date, UID  
- **replays:** CI link, Created, Expected hash/artifact, Last synced, Last updated, Last verified, Name, Notes / known diffs, Purpose, Scenario, Seed, Source path, Status, Sync note, UID  
- **runbooks:** Area, Created, Last synced, Last updated, Related systems, Severity, Source path, **Status**, Sync note, Title, UID  

## Agent views (per DB)

Created via `notion-create-view` on each database:

1. **Agent - All** (table) — SHOW UID, Source path, Last synced, Sync note  
2. **Agent - Missing UID** (table) — FILTER UID IS EMPTY  
3. **Agent - Draft** (table) — FILTER Status/Spec status = Draft (where applicable)

## Evidence

- `schema-baseline-*.md` — pre-change  
- `schema-after-phase1-*.md` — post-change  
- `phase1-schema-results.json` — apply log + verification  

## Next

**Phase 2:** Backfill `Source path` + `UID` on existing ~250 rows; write `path-page-map.json`.
