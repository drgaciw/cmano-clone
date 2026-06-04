# PI Security Findings (Agent D)

**Date:** 2026-06-03 | **Scope:** `ProjectAegis.Data` catalog + validation

## Summary

No **Critical** issues. One **Medium** issue remediated in PI-003 (`TableHasColumn` pragma interpolation).

## Findings

| ID | Severity | Location | Issue | Status |
|----|----------|----------|-------|--------|
| SEC-01 | Medium | `SqliteCatalogReader.TableHasColumn` | Table name in SQL string | **Fixed** — whitelist + safe column id |
| SEC-02 | Medium | `CatalogJsonImporter.ImportToSqlite` | Deserializes untrusted JSON from path | Mitigated — host validates paths; add size cap in future |
| SEC-03 | Low | Connection strings | `databasePath` in connection string | Accept — test/CLI controlled paths |
| SEC-04 | Low | Validation messages | User strings in finding text | Accept — no secrets |
| SEC-05 | Info | SHA-256 fingerprints | Not a secret channel | OK for replay gate |

## Trust boundaries

1. **Catalog JSON files** — authoring / MCP / import pipeline  
2. **SQLite DB path** — local scenario binding  
3. **Scenario JSON** — mission editor / MCP  

## Recommended follow-ups (issues)

- SEC-02: max JSON file size on import (PI backlog)  
- Redact paths in production logs if cloud agents log full exception messages