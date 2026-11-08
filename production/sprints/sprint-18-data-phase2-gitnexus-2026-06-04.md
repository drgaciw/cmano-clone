# Sprint 18 — Catalog Phase 2 plan (GitNexus notes)

**Date:** 2026-06-04  
**Plan:** `docs/superpowers/plans/2026-06-04-catalog-phase2-import.md`  
**Verdict:** Plan-only slice — **no C# delta** on this branch

## Symbols to impact (before P2-1 implementation)

| Symbol | Expected risk | Notes |
|--------|---------------|-------|
| `CmoMarkdownImporter` | **LOW** | Extend chunking; no write path |
| `CatalogWriteGate` | **HIGH** | Propose only from CLI; do not edit gate |
| `DbSnapshotStore` | **MEDIUM** | P2-3 snapshot bind |
| `ScenarioPackageLoader` | **MEDIUM** | Optional snapshot id field |
| `ICatalogReader` | **HIGH** | Read-after-approve verification |

## Detect changes

Doc-only branch — `gitnexus detect_changes` expected **low** (no runtime symbols).