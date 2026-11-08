# S32-03 — Mount/Loadout Quarantine Triage Evidence

**Story:** `production/epics/sprint-32-release-train-ops/story-032-03-mount-loadout-quarantine.md`  
**Date:** 2026-06-19  
**Owner:** team-data  
**Branch:** `stack/sprint32/mount-loadout-quarantine`

## Summary

Curator triage for ship/facility/submarine mount/loadout child rows is operational via `MountLoadoutQuarantineTriage`, CLI verb `catalog_mount_loadout_quarantine_triage`, and off-CI script `tools/cmo-mount-loadout-quarantine-triage.sh`. Bounded FK repair commits only through `CatalogWriteGate.ApproveBatch` (ADR-011 extend-only).

## Repair envelope (bounded — no scope creep)

| Rule | Code | Description |
|------|------|-------------|
| Live platform FK | `platform_live_fk` | `platform_id` exists in live `platform` table |
| Staging platform FK | `platform_staging_fk` | `platform_id` in `catalog_staging_platform` — approve platform batch first |
| Baltic seed FK | `baltic_seed_fk` | `platform_id` in `CatalogValidationDefaults.BalticPlatforms()` |

**Out of envelope (remain quarantined):**

| Reason | Code |
|--------|------|
| Orphan platform | `orphan_platform` |
| Circular FK | `circular_fk` |
| Duplicate loadout key | `duplicate_loadout_key` |
| Fitting/magazine orphan weapon | `orphan_weapon_id` |

## Per-domain quarantine counts (off-CI scratch DB)

**DB:** `scratch/nightly-cmo-20260618/catalog-proposed.db` (post-S31 nightly approve)

| Domain | Mount pending | Loadout pending | Fitting quarantined | Repairable | Out-of-envelope |
|--------|---------------|-----------------|---------------------|------------|-----------------|
| platform (ship) | 0 | 0 | 25239 | 0 | 0 |
| submarine | 0 | 0 | 3218 | 0 | 0 |
| facility | 0 | 0 | 5631 | 0 | 0 |

**Interpretation:** S31 nightly approve already committed all mount/loadout staging batches. Remaining `fittingQuarantined` counts are **magazine orphan-weapon FK** (`orphan_weapon_id`) — outside the mount/loadout platform-FK repair envelope; documented as out-of-envelope in triage output.

## Curated repair validation (before → after)

Synthetic scratch fixture (`MountLoadoutQuarantineTriageTests.Apply_repairs_in_envelope_rows_via_WriteGate_only`):

| Metric | Before | After |
|--------|--------|-------|
| Mount quarantined (proposed staging) | 1 | 0 |
| Loadout quarantined (proposed staging) | 1 | 0 |
| Live `platform_mount` rows | 0 | 1 |
| Live `platform_loadout` rows | 0 | 1 |
| Repaired batch IDs | — | 2 (`batch-mount-*`, `batch-loadout-*`) |

Curated slice fixtures (submarine/facility `*-slice-100.md`, max 12 records): **0** pending mount/loadout after full WriteGate approve.

## WriteGate extend-only changes

- `ApproveMountStaging` / `ApproveLoadoutStaging` now reject `orphan_platform:{id}` (aligned with mobility/damage Phase B FK gates).
- No live-table mutation outside `CatalogWriteGate.ApproveBatch`.

## Verify commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|CmoMarkdown|Platform|CatalogImport|Snapshot|Quarantine" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Results (2026-06-19)

| Gate | Result |
|------|--------|
| Data filtered | **212/212** PASS (+7 triage tests) |
| Grep gate | **zero** `TlBranchDatabase`/`BranchDatabase` |
| DelegationBridge | **zero touch** |
| CI corpora | Curated slices only (`*-slice-100.md`, `maxRecords` caps) |

## New symbols

- `MountLoadoutQuarantineTriage`
- `MountLoadoutQuarantineRepairEnvelope`
- `MountLoadoutQuarantineDomain`
- `catalog_mount_loadout_quarantine_triage` (CLI)
- `tools/cmo-mount-loadout-quarantine-triage.sh`
- `MountLoadoutQuarantineTriageTests` (+7 tests)

## Unblocks

- S32-07 release diff CLI (cleaner mount/loadout child-row posture)
- S33 kill-chain rule noise reduction (fitting quarantine partitioned from committed edges)