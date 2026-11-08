# S35-16 — Dependency Graph Platform→Link Edges (Plan-Only)

**Story:** `production/epics/sprint-35-polish-foundation/story-035-16-dependency-graph-plan.md`  
**Date:** 2026-06-19  
**Owner:** team-data  
**Branch:** `stack/sprint35/dependency-graph-plan`  
**Status:** **PLAN-ONLY** — no runtime code, tests, or UI hosts merged in Sprint 35.

## Summary

Extend the S33-02 `CatalogDependencyGraphIndex` kill-chain edge model (`platform→mount`, `platform→mount→weapon`, `platform→sensor`) with **platform→link FK edges** sourced from `platform_comms` and validated against `link_catalog`. The graph surfaces read-only in Platform Editor Phase H (comms fittings + global link catalog viewer) and optional CLI augmentation — deferred to **Sprint 36+** per polish-scope-boundary handoff item 8.

**References:** `production/agentic/sprint-33-dependency-graph-2026-06-19.md`, [ADR-011 Platform Editor Excel Round-Trip](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md), [polish-scope-boundary handoff item 8](../polish-scope-boundary-2026-06-19.md).

---

## Current scope vs proposed link-edge model

### Current (`GetSortedDependencyEdges()` — S33-02 / DBI-1.5)

| Dimension | Behavior |
|-----------|----------|
| **Edge kinds** | `PlatformToMount`, `PlatformToMountToWeapon`, `PlatformToSensor` |
| **DTO** | `CatalogDependencyEdge(PlatformId, MountId, WeaponId, SensorId)` — unused dims = `""` |
| **Sources** | `platform_mount`, `platform_magazine`, `platform_sensor` (approved bindings only) |
| **Sort key** | `platform_id` → `mount_id` → `weapon_id` → `sensor_id` (ordinal) |
| **Consumers** | `KillChainRules` (R1–R4), `catalog_dependency_graph` CLI, `SqliteCatalogReader` cache |
| **Out of scope today** | Comms/datalink fittings, `link_catalog` FK graph, reverse platform lookup by link |

### Proposed (S36+ runtime — read-only extension per ADR-011)

| Dimension | Behavior |
|-----------|----------|
| **New edge kind** | `PlatformToLink` |
| **DTO extension** | Add `LinkId` and `CommsFittingId` to `CatalogDependencyEdge` (default `""`; kill-chain kinds unchanged) |
| **Sources** | `GetSortedComms()` ∩ approved `review_state`; FK target must resolve in `GetSortedLinks()` |
| **Synthetic `CommsFittingId`** | No persisted column — derive from composite PK: `CatalogSortKeyComparer.FormatCommsKey(binding)` → `{platformId}/{linkId}` (matches `catalog_staging_comms` sort key `platform_id \|\| '/' \|\| link_id`) |
| **Excluded edges** | Provisional/rejected comms; orphan `LinkId` (already `LINK_ORPHAN_COMMS` in `LinkCatalogRules`) |
| **No new write surfaces** | Reads only; commits continue via existing `ProposeCommsBatch` / `ProposeLinkBatch` → `ApproveBatch` |

### Edge-type matrix (full graph after S36+)

| Kind | PlatformId | MountId | WeaponId | SensorId | LinkId | CommsFittingId |
|------|------------|---------|----------|----------|--------|----------------|
| `PlatformToMount` | ✓ | ✓ | — | — | — | — |
| `PlatformToMountToWeapon` | ✓ | ✓ | ✓ | — | — | — |
| `PlatformToSensor` | ✓ | — | — | ✓ | — | — |
| **`PlatformToLink`** | ✓ | — | — | — | ✓ | ✓ (`{platformId}/{linkId}`) |

**Design note:** Keep a **single** `GetSortedDependencyEdges()` surface (extend-only on `ICatalogReader`) rather than a parallel graph API. `KillChainRules` continues to branch on `edge.Kind` and ignores `PlatformToLink` until a future comms-aware rule pack is scoped.

---

## Interface sketch

### Option A (recommended): extend existing types — extend-only

```csharp
// CatalogDependencyEdgeKind.cs — add one enum member
public enum CatalogDependencyEdgeKind
{
    PlatformToMount,
    PlatformToMountToWeapon,
    PlatformToSensor,
    PlatformToLink,          // S36+: platform_comms FK → link_catalog
}

// CatalogDependencyEdge.cs — additive fields (backward-compatible defaults)
public sealed record CatalogDependencyEdge(
    string PlatformId,
    string MountId = "",
    string WeaponId = "",
    string SensorId = "",
    string LinkId = "",              // populated for PlatformToLink
    string CommsFittingId = "")      // FormatCommsKey; graph node identity for UI
{
    public CatalogDependencyEdgeKind Kind =>
        !string.IsNullOrEmpty(LinkId)
            ? CatalogDependencyEdgeKind.PlatformToLink
            : !string.IsNullOrEmpty(SensorId)
                ? CatalogDependencyEdgeKind.PlatformToSensor
                : !string.IsNullOrEmpty(WeaponId)
                    ? CatalogDependencyEdgeKind.PlatformToMountToWeapon
                    : CatalogDependencyEdgeKind.PlatformToMount;
}

// ICatalogReader.cs — signature unchanged; implementation materializes link edges
IReadOnlyList<CatalogDependencyEdge> GetSortedDependencyEdges();

// CatalogDependencyGraphIndex.cs — new overload + unified sort
public static IReadOnlyList<CatalogDependencyEdge> BuildFrom(ICatalogReader reader) =>
    BuildFrom(
        reader.GetSortedMounts(),
        reader.GetSortedMagazines(),
        reader.GetSortedSensorBindings(),
        reader.GetSortedComms(),
        reader.GetSortedLinks());

public static IReadOnlyList<CatalogDependencyEdge> BuildFrom(
    IReadOnlyList<CatalogMount> mounts,
    IReadOnlyList<CatalogMagazineEntry> magazines,
    IReadOnlyList<CatalogSensorBinding> sensors,
    IReadOnlyList<CatalogCommsBinding> comms,
    IReadOnlyList<CatalogLinkEntry> links);
```

### Option B (adjunct reader — if kill-chain DTO pollution is rejected)

```csharp
public interface ICatalogLinkGraphReader
{
    IReadOnlyList<CatalogLinkDependencyEdge> GetSortedPlatformLinkEdges();
}

public sealed record CatalogLinkDependencyEdge(
    string PlatformId,
    string LinkId,
    string CommsFittingId,
    string Role,
    bool SatcomCapable);
```

**Recommendation:** Option A — matches handoff wording (`CatalogDependencyGraphIndex` extension), reuses S33-02 cache/invalidation path, and keeps `catalog_dependency_graph` CLI as one verb with a `kind` discriminator.

### Build algorithm sketch (`PlatformToLink` emission)

```
knownLinks = HashSet(GetSortedLinks().Select(l => l.LinkId))
foreach comms in GetSortedComms().Where(approved).OrderBy(platform_id).ThenBy(link_id):
    if knownLinks.Contains(comms.LinkId):
        edges.Add(new CatalogDependencyEdge(
            comms.PlatformId,
            LinkId: comms.LinkId,
            CommsFittingId: FormatCommsKey(comms)))
return all edges sorted by unified key (see below)
```

### Reverse lookup helper (UI-only projection — not on `ICatalogReader`)

```csharp
// ProjectAegis.Delegation.Projection — S36+ headless-testable
public static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildLinkToPlatformsIndex(
    IReadOnlyList<CatalogDependencyEdge> edges) =>
    edges.Where(e => e.Kind == CatalogDependencyEdgeKind.PlatformToLink)
         .GroupBy(e => e.LinkId, StringComparer.Ordinal)
         .ToDictionary(g => g.Key, g => g.Select(e => e.PlatformId).OrderBy(...).ToArray());
```

---

## Stable sort keys

### Unified edge sort (deterministic rebuild)

Extend S33-02 terminal sort to include link dimensions:

```
OrderBy(platform_id, Ordinal)
  .ThenBy(mount_id, Ordinal)
  .ThenBy(weapon_id, Ordinal)
  .ThenBy(sensor_id, Ordinal)
  .ThenBy(link_id, Ordinal)
  .ThenBy(comms_fitting_id, Ordinal)
```

Kill-chain edges keep trailing link fields empty, preserving existing ordering among weapon/mount/sensor edges.

### Per-source upstream sort (already implemented)

| Source | Sort key | Comparer / helper |
|--------|----------|-------------------|
| `platform_comms` | `platform_id`, `link_id` | `CatalogSortKeyComparer.SortComms` |
| `link_catalog` | `link_id` | `CatalogSortKeyComparer.SortLinks` |
| Staging comms | `platform_id \|\| '/' \|\| link_id` | `CatalogSortKeyDeterminismTests` fixture |

### Canonical CLI line format (S36+ extend `CatalogDependencyGraphCommand`)

```
link:{platformId}:{linkId}:{commsFittingId}
```

Existing lines (`mount:`, `weapon:`, `sensor:`) unchanged for golden compatibility.

---

## Invalidation contract (aligned with S33-02)

| Event | Current behavior | After S36+ link edges |
|-------|------------------|----------------------|
| `CatalogWriteGate.ApproveBatch` — mount/magazine/sensor | `NotifyDependencyGraphCommitted()` → `CatalogDependencyGraphCacheInvalidator.InvalidateForDatabase` | Unchanged |
| `ApproveCommsStaging` (`platform_comms`) | Already calls `NotifyDependencyGraphCommitted()` (line ~690) | Rebuild includes new `PlatformToLink` edges |
| `ApproveLinkStaging` (`link_catalog`) | Already calls `NotifyDependencyGraphCommitted()` (line ~596) | Orphan comms edges drop/add as link rows change |
| `SqliteCatalogReader` cache | `_dependencyEdgesCache` cleared via `InvalidateDependencyGraphCache()` | Same field; no second cache |
| `InMemoryCatalogReader` / tests | Eager `BuildFrom` on seed | Pass comms+links into extended `BuildFrom` |
| `CatalogStagingOverlayReader` | Overlays staging; rebuilds graph from overlay reader | Staging comms/link rows participate when overlay exposes them |

**Extend-only `CatalogWriteGate` rule:** No new `Notify*` hooks — link-edge freshness piggybacks on existing `NotifyDependencyGraphCommitted()` after approve commits. **ZERO touch `DelegationBridge.cs`.**

**Quarantine gate:** Same as S33-02 — only `review_state == approved` (ordinal ignore-case) comms bindings emit edges. Orphan FKs excluded at build time (consistent with `LINK_ORPHAN_COMMS` validation).

---

## UI consumption sketch (read-only — no S35 implementation)

### Platform Editor Phase H touchpoints (ADR-011; S34-06 delivered)

| Surface | File / symbol | S36+ consumption |
|---------|---------------|------------------|
| **Per-platform comms list** | `PlatformCatalogViewerHost` → `platform-catalog-comms-list` | On platform select, highlight comms rows whose `LinkId` matches an edge in `GetSortedDependencyEdges()`; tooltip shows `CommsFittingId` + resolved `DisplayName` from `CatalogLinkListProjection` |
| **Global link catalog list** | `PlatformCatalogViewerHost` → `platform-catalog-links-list` | On link row select, show reverse FK panel: platforms referencing this `LinkId` via `BuildLinkToPlatformsIndex` (new read-only detail sub-list or badge count) |
| **Import staging diff** | `PlatformImportStagingProjection` — `COMMS row=…`, `LINK row=…` | After approve, dependency graph invalidates; optional staging preview calls `GetSortedDependencyEdges()` on `CatalogStagingOverlayReader` to show pending link-edge delta count |
| **Headless proxy tests** | `PlatformLinkCatalogTests`, `PlatformCommsTests` | Add grep/assertions for FK cross-link labels (mirror S33-02 `DependencyGraphIndexTests` pattern in Data.Tests, not Unity runtime) |

### UX interaction pattern (from `design/ux/interaction-patterns.md`)

- `COMMS` diff lines (`LinkId` change) should correlate to graph edge add/remove in a future **Dependencies** sub-panel (read-only).
- `LINK_*` validation findings map to missing graph targets — viewer can grey-out comms rows with no resolving link edge.

### Explicitly out of scope for UI (S36+ epic guardrails)

- No new Unity write path — writes stay `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge`.
- No sim/runtime datalink behavior changes (`DatalinkShareLagResolver` unchanged).
- No graph visualization engine — list/badge FK surfacing only (ADR-011 headless-first).

---

## Sprint 36+ deferral

| Item | Tracker reference | Proposed epic hook |
|------|-------------------|-------------------|
| Implement `PlatformToLink` in `CatalogDependencyGraphIndex` | Handoff item 8 → `polish-scope-boundary-2026-06-19.md` §Carry-forward | **S36-DBI-1.5b** or sprint-36 data epic (TBD at `/sprint-plan`) |
| Extend `catalog_dependency_graph` CLI + golden lines | `implementation-tracker-2026-06-04.md` Req 06 DBI-1.5 | S36 data story (~1d) |
| `DependencyGraphIndexTests` + approve comms/link invalidation tests | S33-02 precedent (+13 tests) | S36 data story (~0.5d) |
| Platform Editor reverse FK surfacing | Req 21 Phase H extension | S36 Unity story (~1d); `PlatformLinkCatalogTests` extension |
| Optional ADR-011 addendum or `/architecture-decision` | Governing ADR read-only graph extension | Architect review at S36 kickoff |
| SchemaVersion 011 / breaking migrations | S35 explicit OUT | Not required — composite PK sufficient |

**Sprint 35 deliverable:** This plan doc only. Story S35-16 closes with zero diffs under `src/`, `unity/`, or `tests/`.

---

## Optional ADR outline (for S36 `/architecture-decision`)

**Title:** ADR-011 Addendum — Platform→Link Dependency Graph Extension  
**Status:** Proposed (not filed in S35)  
**Decision:** Extend `CatalogDependencyEdge` + `CatalogDependencyGraphIndex` (Option A) with `PlatformToLink` edges from approved `platform_comms`, synthetic `CommsFittingId`, unified sort keys, existing `NotifyDependencyGraphCommitted` invalidation.  
**Alternatives rejected:** Separate graph store; persisted `comms_fitting_id` column; sim-side graph materialization.  
**Consequences:** `KillChainRules` unchanged; CLI golden adds `link:` lines; UI gains FK navigation without new write surfaces.

---

## Hard gates (future implementation)

| Gate | Requirement |
|------|-------------|
| `CatalogWriteGate` extend-only | Reuse `NotifyDependencyGraphCommitted()` — no new commit hooks |
| ZERO touch `DelegationBridge.cs` | UI via projection layer only |
| Baltic + `ship-slice-100` CI fixtures | No full corpora in CI |
| SchemaVersion 010 frozen | No migration 011 for link graph |
| Determinism | Rebuild hash stable across `BuildFrom` invocations (S33-02 test precedent) |

---

## Verify commands (S36+ implementation checklist)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "DependencyGraph|WriteGate|Platform|LinkCatalog" -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_dependency_graph --db <baltic.db>
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# must be empty
```

---

## Files touched (S35-16 plan-only)

| File | Change |
|------|--------|
| `production/agentic/sprint-35-dependency-graph-platform-link-plan-2026-06-19.md` | **NEW** — this document |
| `production/epics/sprint-35-polish-foundation/story-035-16-dependency-graph-plan.md` | Status → Complete |
| `production/sprint-status.yaml` | `35-16` → complete @ 2026-06-19 |

**No runtime files changed.**