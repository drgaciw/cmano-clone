# Sprint 30 — TL Export Phase 3 (S30-02)

**Date:** 2026-06-18  
**Story:** `production/epics/sprint-30-tl-export-phase34/story-030-02-tl-phase3-export.md`  
**Epic:** sprint-30-tl-export-phase34  
**ADR:** ADR-006 (snapshot binding), ADR-011 (write-gate governance)  
**Foundation:** `production/agentic/sprint-29-tl-export-phase12-2026-06-18.md` (Phases 1–2)  
**Spike:** `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md` — PROCEED (export-only)

## Verdict: **COMPLETE (Phase 3)**

Per-tier filtered read-only export landed. **No** runtime `tlBranch` binding, **no** `TlBranchDatabaseResolver`, **ZERO** touch `DelegationBridge.cs`.

## Phase map (S28-11)

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Export manifest `tlTier` on workbook/JSON drops | Done (S29-02) |
| 2 | Migration `010` `catalog_snapshot.branch` | Done (S29-02) |
| **3** | Per-tier filtered `ICatalogReader` export filters | **Done (S30-02)** |
| 4 | Scenario package `tlBranch` + validation | Deferred (S30-03) |
| 5 | Physical SQLite fork per TL | Post-MVP |

## Implementation summary

### Per-tier export filter (read-only)

- `CatalogTlTierResolver` — infers record TL from `game_technology_level`, OSINT `branch:doc-09/10` tags, and TRL bands (S28-11 table)
- `CatalogTlExportFilter` — retains rows at or below requested ceiling; re-sorts with TL export keys
- `CatalogTlExportSortKey` — locked ordering `(canonicalId, tlTier, valueTier)` ascending `StringComparer.Ordinal`
- `SqliteCatalogReader.LoadExportData(maxTlTier?)` — loads platform GTL map, applies filter
- `ICatalogReader.LoadExportData` — default empty slice; SQLite reader overrides
- `PlatformCatalogExportResolver.TryResolve(..., maxTlTier?)` — passes filter to reader
- `platform_export_xlsx` CLI — `--tl-tier TL-0..TL-5`; JSON payload `tlTierFilter` + manifest `tlTier` honored

### Filter semantics

| Input | Behavior |
|-------|----------|
| No `--tl-tier` | Full unfiltered export (backward compatible) |
| `--tl-tier TL-2` | Records with inferred tier ≤ TL-2; manifest `TlTier` = `TL-2` |
| Empty slice | Zero rows when ceiling below all record tiers |

### Sort keys (locked)

```
(canonicalId, tlTier, valueTier) → StringComparer.Ordinal ascending
```

Canonical IDs reuse `CatalogSortKeyComparer.Format*Key` (e.g. `platform_id/sensor_id`).

## Acceptance criteria

| AC | Status | Evidence |
|----|--------|----------|
| Filtered export tests PASS | PASS | `CatalogTlExportFilterTests` (9 tests) |
| `platform_export_xlsx` / JSON honor `tlTier` | PASS | `PlatformExportXlsxCommand` `--tl-tier`; manifest override |
| Deterministic sort keys locked | PASS | `CatalogTlExportSortKey`; `Tl_export_sort_keys_are_deterministic` |
| `rg TlBranchDatabase\|BranchDatabase` → zero | PASS | grep gate below |
| WriteGate regression PASS | PASS | filtered `dotnet test` 160/160 |
| Evidence doc | PASS | this file |
| ZERO touch `DelegationBridge.cs` | PASS | empty `git diff` |

## Verify commands (recorded)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogImport|Snapshot|TlTier" -v minimal
# Passed: 160

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Passed: 8

dotnet test ProjectAegis.sln -v minimal
# Passed: 887/887 (baseline 878 + 9 new)

rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
# (empty)

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Rollback

- **Filter path:** omit `--tl-tier` flag → unfiltered export (S29 behavior)
- **Code:** revert `CatalogTlExportFilter` + reader overload; manifest/export unchanged
- **Release train:** scenario `dbRef` rollback unchanged (P0 model)

## S30-03 merge-conflict risks

| Area | Risk | Mitigation |
|------|------|------------|
| `CatalogExportManifest` | Low | Phase 4 adds scenario binding; manifest shape unchanged |
| `ICatalogReader` | Low | `LoadExportData` default returns empty; Sim does not call |
| `PlatformExportXlsxCommand` | Low | `--tl-tier` additive flag |
| `CatalogWriteGate` | **None** | ZERO touch |

## References

- S29-02: `production/agentic/sprint-29-tl-export-phase12-2026-06-18.md`
- Spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md` § S30-02