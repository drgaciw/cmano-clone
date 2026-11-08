# Sprint 29 — TL Export Phase 1–2 (S29-02)

**Date:** 2026-06-18  
**Story:** `production/epics/sprint-29-tl-export-foundation/story-029-02-tl-export-phase12.md`  
**Epic:** sprint-29-tl-export-foundation  
**ADR:** ADR-006 (snapshot binding), ADR-011 (write-gate governance)  
**Spike:** `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md` — PROCEED (export-only)

## Verdict: **COMPLETE (Phases 1–2)**

Export-only TL metadata landed. **No** runtime `tlBranch` binding, **no** `TlBranch` / `BranchDatabase` types, **ZERO** touch `DelegationBridge.cs`.

## Phase map (S28-11)

| Phase | Scope | Status |
|-------|-------|--------|
| **1** | Export manifest `tlTier` on workbook/JSON drops | **Done** |
| **2** | Migration `010` `catalog_snapshot.branch` (`TL-0`…`TL-5`) | **Done** |
| 3 | Per-tier filtered `ICatalogReader` export filters | Deferred (S29-03+) |
| 4 | Scenario package `tlBranch` + validation | Deferred |
| 5 | Physical SQLite fork per TL | Post-MVP |

## Implementation summary

### Migration `010_tl_snapshot_branch.sql`

- Adds `catalog_snapshot.branch TEXT NOT NULL DEFAULT 'TL-0'`
- Idempotent skip when column already present (`SqliteCatalogReader.ShouldSkipMigration`)
- Existing snapshot rows backfill to `TL-0` via SQL default

### Snapshot recording (approve path)

- `DbSnapshotStore.RecordRelease` / `RecordApprovedImport` persist `branch` (default `TL-0`)
- `CatalogSnapshotBinder.BindAfterApprove` accepts optional `tlTier`; writes branch on release row
- `CatalogWriteGate.ApproveSensorStaging` records approved import with `CatalogTlTier.Default`

### Export manifest shape (locked)

```json
{
  "dbVersion": "<release_version>",
  "tlTier": "TL-0",
  "schemaVersion": "010",
  "contentHash": "<sha256>",
  "exportSchemaVersion": "1"
}
```

**Surfaces:**

- Workbook `_Meta` sheet keys: `DbVersion`, `TlTier`, `CatalogSchemaVersion`, `ContentHash`, `ExportSchemaVersion`
- `platform_export_xlsx` CLI JSON payload `manifest` object
- `catalog_write_approve` CLI JSON `tlTier` field

### Types (metadata only)

- `CatalogTlTier` — `TL-0`…`TL-5` constants + validation (not `TlBranch`)
- `CatalogExportManifest` — manifest record + `Resolve(databasePath, snapshotId)`

## Acceptance criteria

| AC | Status | Evidence |
|----|--------|----------|
| Migration applies; `catalog_snapshot.branch` present | PASS | `010_tl_snapshot_branch.sql`; `Migration_010_applies_idempotently` |
| Export drops carry `tlTier` | PASS | `CatalogExportManifestTests`; workbook meta + CLI manifest |
| `rg TlBranch\|BranchDatabase` → zero | PASS | grep gate below |
| WriteGate regression PASS | PASS | filtered `dotnet test` below |
| ZERO touch `DelegationBridge.cs` | PASS | empty `git diff` |
| Evidence doc | PASS | this file |

## Verify commands (recorded)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogImport|Snapshot" -v minimal

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal

rg -l "TlBranch|BranchDatabase" src/ --glob "*.cs" || true

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Rollback

- **Schema:** branch column is additive; downgrade = ignore column (readers default `TL-0` when missing)
- **Release train:** scenario/package `dbRef` → prior `snapshotId` (unchanged P0 model)
- **Export:** omit `TlTier` meta keys only if rolling back exporter — not required for DB rollback

## S29-03 merge-conflict risks

| Area | Risk | Mitigation |
|------|------|------------|
| `CatalogWriteGate.cs` | Medium — S29-03 extends approve/nightly path | Extend-only; one-line `RecordApprovedImport` branch arg already in place |
| `DbSnapshotStore.cs` | Low — S29-03 may add `RecordRelease` calls | Branch param is optional with `TL-0` default |
| Migration numbering | **Resolved** — used `010` not `007` (007–009 = platform editor) | S29-03 should not re-use `010` |
| `CatalogSnapshotBinder` | Low | `tlTier` param optional; default `TL-0` |

## References

- Spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` § S29-02
- Skill: `database-branching-release-train`