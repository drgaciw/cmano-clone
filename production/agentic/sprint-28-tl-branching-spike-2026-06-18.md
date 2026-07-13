# Sprint 28 — TL Branching Spike (Export-Only) — S28-11

**Date:** 2026-06-18  
**Story:** `production/epics/sprint-28-cmo-corpus-v2/story-028-11-tl-branching-spike.md`  
**Requirement:** Req 06 §4 TL branches and database snapshots; DBI-7.5 (post-P0)  
**ADR:** ADR-006 (data layer boundary — snapshot binding, no direct SQLite in Sim)  
**Skill:** `database-branching-release-train`  
**Branch:** `stack/sprint28/tl-branch-spike`  
**GitNexus:** `OsintCatalogMapper` — **LOW** (TL routing reference only); `DelegationBridge.cs` — **ZERO touch**

## Verdict: **PROCEED (export-only evaluation)**

Proceed with the **export-only TL branch evaluation workflow** documented below. **Defer** physical TL-0–TL-5 forked branch databases and runtime scenario DB branch binding until a dedicated data sprint (post-S28) with migration `007+` and curator sign-off.

**Rationale**

| Factor | Assessment |
|--------|------------|
| P0 infrastructure | `DbSnapshotStore`, `db_release`, `CatalogSnapshotHasher`, `content_hash_sha256` (migration 006) already support immutable drops and rollback-by-snapshot-id |
| TL metadata today | S22-07 `OsintCatalogMapper` tags staged rows via `ImportBatchId` (`branch:doc-09` / `branch:doc-10`) + `TrlLevel` — doc-gate routing, not TL-0–TL-5 forks |
| Req 06 locked decision | Full product requires TL-0–TL-5 branches with shared canonical IDs; P0 explicitly defers forks to tagged snapshots + scenario flags |
| Corpus pipeline | S28-02 nightly v2 (sensor + weapon + platform) runs propose-only off-CI; safe evaluation surface for export diffs without runtime binding |
| Risk | Parallel track overload + GHA billing constraints favor doc/eval path before multi-DB ops |
| Blockers to production forks | No `branch` column on `catalog_snapshot` yet; no `TlBranch` / `BranchDatabase` resolver; no scenario `tlBranch` binding field |

**What PROCEED means here:** curators and nightly jobs may produce **diffable export drops** tagged by TL tier metadata; scenarios continue to bind a single `dbRef` / `dbSnapshotId`. **What is deferred:** separate SQLite files per TL branch, sim-time branch switch, and `BLACK_PROJECT_MODE` TL-5 fork.

---

## Current state (baseline @ S28-02)

| Piece | Status | Notes |
|-------|--------|-------|
| Single `main` catalog | **Shipped (P0)** | `data/catalog/aegis-catalog.dev.sqlite`; one writer via `IWriteGate` |
| Immutable snapshots | **Shipped** | `catalog_snapshot.snapshot_id` + `content_hash_sha256` |
| Release train rows | **Shipped** | `db_release` (`ReleaseVersion` → `SnapshotId` → `SchemaVersion`) |
| Deterministic hash | **Shipped** | `CatalogSnapshotHasher.ComputeSha256Hex` over sorted sensor bindings |
| OSINT TL routing | **Shipped (S22-07)** | `OsintCatalogMapper.ResolveBranchTag` / `ResolveTrlLevel` on staged metadata |
| TL-0–TL-5 branch DBs | **Not started** | Reserved in Req 06 §4; P0 spec: `branch` column reserve only |
| Runtime branch binding | **None** | `rg TlBranch\|BranchDatabase src/**/*.cs` → zero matches |

### S22-07 pattern (metadata routing, not DB fork)

```csharp
// OsintCatalogMapper.cs — branch tag on ImportBatchId for doc 09/10 gates
public const string BranchTagPrefix = "branch:doc-";
public static string ResolveBranchTag(string targetDoc) => BranchTagPrefix + NormalizeTargetDoc(targetDoc);
```

Staged proposals carry `TrlLevel` (1–9) and `ImportBatchId` (`branch:doc-09` | `branch:doc-10`). This is **provenance routing** for near-future / speculative gates — not a separate database per TL tier.

---

## TL-0–TL-5 branch model (Req 06 §4)

| TL | Era | Branch content | Scenario gate |
|----|-----|----------------|---------------|
| **TL-0** | 2025 baseline | Fielded systems only | Default patrol / historical |
| **TL-1** | 2026–2028 | Early fielding | `scenario.tlFloor` ≥ 1 |
| **TL-2** | 2028–2030 | Primary near-future target | Doc 09 primary band |
| **TL-3** | 2030–2032 | Advanced near-future | Doc 09 upper band |
| **TL-4** | 2035–2040 | Speculative systems | Doc 10 |
| **TL-5** | 2040+ | Black-project / Future War | Doc 10 `BLACK_PROJECT_MODE` flag required |

**Invariant (full product):** canonical `PlatformId` / `SensorId` / `WeaponId` are **shared across all TL branches**. Branch-local tables hold interpreted/gameplay values and temporal validity windows — not duplicate identity namespaces.

---

## Export-only workflow outline (TL-0 → TL-5)

### Phase map

```
TL-0 baseline export (fielded)
    │
    ├─► nightly CMO import (propose-only) ──► staging batches tagged importBatchId
    │
    ├─► human ApproveBatch ──► new snapshot_id + content_hash_sha256
    │
    ├─► RecordRelease(releaseVersion, snapshotId, hash) ──► db_release row
    │
    └─► export drop (JSON/SQLite read-only bundle) per TL evaluation slice
            │
            ├─► diff vs prior release (empty-diff gate = DBI-4.4 / S28-03 hygiene)
            ├─► manifest: { dbVersion, tlTier, schemaVersion, contentHash, exportSchemaVersion }
            └─► rollback = re-bind scenario dbRef to prior snapshotId (no in-place undo)
```

### Per-tier evaluation steps (export-only — no runtime binding)

| Step | TL-0 | TL-1 … TL-3 | TL-4 … TL-5 |
|------|------|--------------|-------------|
| 1. Source intake | CMO public + internal curation | OSINT doc-09 proposals | OSINT doc-10 + speculative archetypes |
| 2. Staging tag | `importBatchId=nightly-cmo-{date}` | `branch:doc-09` + `TrlLevel` | `branch:doc-10` + `TrlLevel` + archetype gate |
| 3. Gate | `CatalogWriteGate.Propose*` → `ApproveBatch` | Same; `CatalogArchetypeGate` advisory | TL-5 rows quarantined until `BLACK_PROJECT_MODE` reviewer flag |
| 4. Snapshot | `snap-{batch}-{shortHash}` | Same pattern | Same; separate **evaluation export** manifest field `tlTier` |
| 5. Export drop | Sorted sensor/platform/weapon JSON or workbook | Filter export by `TrlLevel` band + doc tag | Isolated drop; never merged into TL-0 default export |
| 6. Diff | `diff(release_N, release_N-1)` on sorted canonical rows | Cross-tier diff on shared IDs (interpreted values only) | Speculative-only diff report for curators |
| 7. Rollback | `dbRef` → prior `snapshotId` from `GetSortedReleases()` | Same | Discard staging batch (`RejectBatch`); live tables unchanged |

### Snapshot hash contract (deterministic)

Existing `CatalogSnapshotHasher` fingerprints sorted sensor bindings:

```
(platformId, sensorId, basePd, confidence, importBatchId, sourceFile) → SHA-256 hex
```

**Export-only extension (future, no code in S28-11):** include `trlLevel`, `valueTier`, `reviewState` in hash input once per-field provenance is on all entity types (DBI-6 post-P0). Until then, TL evaluation uses **release-level** `content_hash_sha256` on `catalog_snapshot` plus manifest `tlTier` annotation on the export bundle — not a second runtime DB.

### Rollback model

| Operation | Mechanism | Live tables |
|-----------|-----------|-------------|
| Reject pending batch | `RejectBatch(batchId)` | Unchanged (DBI-4.4) |
| Revert approved drop | Scenario / package `dbRef` → earlier `snapshotId` | Immutable snapshots; no DELETE |
| Emergency curator rollback | Publish `db_release` row pointing at prior `snapshotId` | Readers resolve via `TryResolveDbRef` |

Rollback is **binding change**, not in-place row undo — aligns with P0 spec and ADR-006.

### Diffable drops

| Artifact | Purpose | Producer |
|----------|---------|----------|
| `*-propose.json` | Nightly propose-only report (S28-02) | `tools/cmo-nightly-import.sh` |
| `*-quarantine.json` | Orphan / validation failures | Same |
| Workbook export | Curator human review (S28-04 path) | `PlatformWorkbookExporter` |
| Release diff report | Post-P0 DBI-4.5 | Future CLI verb; spike defines sort keys only |
| Golden empty-diff | Re-import same slice → zero delta | S28-03 hygiene tests |

**Sort keys for diff (locked):** `(canonicalId, tlTier, valueTier)` ascending with `StringComparer.Ordinal` — matches `ICatalogReader` deterministic ordering policy.

---

## CI / nightly separation

| Lane | Scope | TL / corpus load | Gate |
|------|-------|------------------|------|
| **CI (`dotnet test`)** | Curated fixtures only | `ship-slice-100.md`, `--max-records 12` | WriteGate + replay 6/6; no full corpus |
| **Nightly (off-CI)** | Full CMO slices | sensor (up to 7208) + weapon + platform | `tools/cmo-nightly-import.sh`; propose-only scratch DB |
| **TL branch evaluation** | Export-only manifest | Per-tier filtered exports from approved snapshots | Manual / scheduled; **never** in PR CI |

**7208 sensor rule (unchanged):** full `sensor.md` import stays **off-CI** per S27-02 / S28-02. TL-tier evaluation drops are produced from nightly or curator-approved batches — not wired into `dotnet test`.

**GHA / billing edge case:** if nightly workflow is paused, TL evaluation uses last pinned `db_release` + local `MAX_RECORDS` smoke — CI gate does not depend on nightly artifacts.

---

## Scenario binding (future — explicitly out of scope S28-11)

Today: scenario `metadata.dbRef` → `TryResolveDbRef` → single snapshot ID.

Future (deferred):

```json
{
  "dbRef": "db-20261001-tl2-001",
  "tlBranch": "TL-2",
  "contentHash": "<sha256>",
  "blackProjectMode": false
}
```

Sim/Delegation read **one** resolved snapshot per scenario load. Branch selection happens at **package authoring time**, not mid-tick. No `TlBranchDatabaseResolver` in S28.

---

## Risks and mitigations

| Risk | Severity | Mitigation |
|------|----------|------------|
| Accidental scenario DB branch switch | High | No runtime binding code; spike doc-only; grep gate on `TlBranch` / `BranchDatabase` |
| Parallel track overload (corpus + branching) | Medium | Export-only eval first; physical forks deferred |
| Hash drift across TL tiers | Medium | Shared canonical IDs; diff interpreted columns only |
| Non-deterministic export order | High | Fixed sort keys; `ICatalogClock` injectable; no `DateTime.Now` in commit path |
| GHA billing / nightly pause | Low | CI uses curated fixtures; TL eval not blocking merge |
| `DelegationBridge` touch | CRITICAL | ZERO touch — verified |

---

## Implementation phases (recommended sequencing)

| Phase | Scope | Sprint target | Runtime binding |
|-------|-------|---------------|-----------------|
| **0 (this spike)** | Workflow doc + PROCEED/DEFER | S28-11 | None |
| **1** | Export manifest `tlTier` field on workbook/JSON drops | S29+ data | None |
| **2** | Migration `007`: `catalog_snapshot.branch` column (`TL-0`…`TL-5`) | S29+ data | None |
| **3** | Per-tier filtered `ICatalogReader` export filters | S29+ data | Read-only |
| **4** | Scenario package `tlBranch` + validation | Mission editor | Bind at load |
| **5** | Optional physical SQLite fork per TL | Post-MVP | Full branch DB |

---

## Acceptance (spike close)

- [x] TL-0–TL-5 workflow outline documented
- [x] Export-only diffable drops, snapshot hashes, rollback documented
- [x] CI/nightly separation (7208 sensor off-CI) documented
- [x] PROCEED/DEFER verdict with rationale — **PROCEED (export-only); defer production forks**
- [x] No runtime branch binding in code — grep zero matches
- [x] No TL-0–TL-5 production branch databases shipped
- [x] ZERO touch `DelegationBridge.cs`

## Verify commands

```bash
# No new runtime branch bindings
rg -l "TlBranch|BranchDatabase" src/ --glob "*.cs" || echo "zero matches"

# Spike doc exists
ls production/agentic/sprint-28-tl-branching-spike-2026-06-18.md

# DelegationBridge untouched
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## References

- Req 06 §4 TL branches: `Game-Requirements/requirements/06-Database-Intelligence.md`
- P0 deferral: `docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md` (branch column reserve)
- S22-07: `production/sprint-status.yaml` — `OsintCatalogMapper` TL routing (done 2026-06-17)
- S28-02 nightly: `production/qa/sprint-28-nightly-cmo-import-2026-06-18.md`
- Skill: `.claude/skills/database-branching-release-train/SKILL.md`