# Platform Design Assistant — developer guide

**Status:** Headless production path (DRG-73 / PDA-04)
**Gate:** ADR-011 Excel-primary + `CatalogWriteGate` extend-only
**Namespace:** `ProjectAegis.Data.PlatformAssistant`

> The Platform Design Assistant (PDA) drafts **future / hypothetical** platform archetypes
> **relative to existing catalog peers**, then stages them as **extend-only** write-gate batches. It
> is a deterministic *proposal agent* — not a WYSIWYG editor and not an LLM. Every number it emits is
> a pure function of the live catalog snapshot plus the curator brief, and nothing reaches the live
> catalog until a human `ApproveBatch`. This page documents the internals (peer scoring, relative
> scaling, the proposal/stage flow, and the workbook hand-off) verified against source; the
> extend-only write path itself is [catalog-write-gate.md](catalog-write-gate.md) and the Excel
> round-trip is [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md).

## Purpose

Draft archetypes grounded in real peers, so a curator can propose "an OPV a bit lighter than the
existing corvettes" without hand-authoring every field, and have the result staged behind the same
extend-only gate as any other catalog edit.

## Where it lives

| File | Role |
|------|------|
| [`PlatformDesignBrief`](../../src/ProjectAegis.Data/PlatformAssistant/PlatformDesignBrief.cs) | Curator input: `(PlatformId, DisplayName, Domain, RoleWeight, Concept, WhatIf, PeerPlatformIds?)`. |
| [`PlatformPeerScorer`](../../src/ProjectAegis.Data/PlatformAssistant/PlatformPeerScorer.cs) | `catalog-grounding` skill — ranks candidate peers from a live export snapshot. |
| [`PlatformRelativeScaler`](../../src/ProjectAegis.Data/PlatformAssistant/PlatformRelativeScaler.cs) | `relative-scaling` + `archetype-schema` + `provenance` skills — turns ranked peers into a scaled `PlatformDesignProposal`. |
| [`PlatformDesignAssistant`](../../src/ProjectAegis.Data/PlatformAssistant/PlatformDesignAssistant.cs) | The façade: `Draft` (pure) and `Propose` (stage via the write gate). |
| [`PlatformDesignProposal`](../../src/ProjectAegis.Data/PlatformAssistant/PlatformDesignProposal.cs) | Draft + result records (`PlatformPeerScore`, `PlatformFieldBasis`, `PlatformDesignProposeResult`). |
| [`PlatformDesignWorkbookEmitter`](../../src/ProjectAegis.Data/PlatformAssistant/PlatformDesignWorkbookEmitter.cs) | `workbook-emit` skill — appends a proposal onto an exported `.xlsx` workbook for Excel hand-off. |

Host bridge: `PlatformDesignAssistantBridge` (UnityAdapter — see
[c2-presentation-bridges.md](c2-presentation-bridges.md) for the façade pattern).
CLI verb: `platform_design_propose` (also registered in `tools/mission-editor/mcp-tools.json`).

---

## Design invariants — never break these

| Invariant | Rule |
|-----------|------|
| **Deterministic & RNG-free** | `Draft` / `Scale` / `Score` are pure functions of `(export snapshot, brief)` — no `SeededRng`, no `DateTime.UtcNow`, no ordering ambiguity (peers sort by `Score` desc then `PlatformId` ordinal). The only clock is `ICatalogClock`, used solely to mint write-gate batch ids in `Propose`. |
| **Proposal only — never a live write** | `Draft` returns a `PlatformDesignProposal` with **no** DB side effect. `Propose` only calls `CatalogWriteGate.Propose*Batch` (staging). Live `platform` / `platform_damage` / `mobility` rows change **only** on explicit `ApproveBatch`. |
| **New ids only (extend-only)** | The proposed `PlatformId` is made unique against existing catalog ids (`UniqueId` suffixes `-2`, `-3`, …). PDA never targets an existing row — consistent with the [write gate](catalog-write-gate.md) extend-only contract. |
| **Provisional + provenance-tagged** | Every emitted binding/damage/mobility row is `ReviewState = Provisional`, `ValueTier = GameplayAbstraction`, `CitationRef = assistant:{peerIds}`, `SourceFactId/SourceFile = platform-design-assistant` — so a scaled archetype is never mistaken for sourced data. |
| **`ApplyCorePosition` opt-in only** | Scaled `LatDeg` / `LonDeg` / `CombatRadiusNm` ride on the binding with `ApplyCorePosition: true` (migration `015`). Non-PDA callers leave it `false` and keep the historical upsert behaviour. |
| **No `DelegationBridge` touch** | PDA is a data-layer authoring tool; the unedited-workbook empty-diff golden stays green. |

---

## The pipeline

```
brief ─▶ PlatformDesignAssistant.Draft(catalog, brief)
            catalog.LoadExportData()                     (catalog-grounding)
            PlatformPeerScorer.Score(export, brief)      → ranked peers
            PlatformRelativeScaler.Scale(export, brief)  → PlatformDesignProposal
         └▶ (no DB write)

brief ─▶ PlatformDesignAssistant.Propose(dbPath, catalog, brief, clock, …)
            Draft(...)                                   → proposal
            CatalogWriteGate.ProposePlatformBatch([binding])   → platformBatchId
            CatalogWriteGate.ProposePlatformDamageBatch([dmg]) → damageBatchId
            CatalogWriteGate.ProposeMobilityBatch([mob])       → mobilityBatchId?  (skipped if all speed/range 0)
         └▶ PlatformDesignProposeResult (staged; awaiting human ApproveBatch)
```

### 1. Peer scoring (`PlatformPeerScorer.Score`)

Every catalog platform except the brief's own id is scored, then sorted **`Score` descending, then
`PlatformId` ordinal** (stable, deterministic). Two modes:

- **Curator-fixed peers** (`brief.PeerPlatformIds` non-empty): selected ids score `+100`
  (`"curator-selected peer"`); every other platform scores `+1` (`"non-selected fallback"`) so a
  fallback still exists if the curator's ids are missing.
- **Heuristic** (no fixed peers): base `+20` (`"catalog peer"`), then
  - `+12` per concept token that appears in the platform id (tokens come from
    `Concept + DisplayName + Domain`, lowercased, split on separators, length > 2, distinct),
  - `+10` if the platform has a non-zero `CombatRadiusNm` (`"has combat radius"` — favours real units
    over placeholders),
  - `+5` if it has a damage model with `MaxHp > 0` (`"has damage model"`).

Each `PlatformPeerScore` carries its `CombatRadiusNm`, `MaxHp` (default `100` when no damage row), and
`MaxSpeedKnots` (default `0` when no mobility row), plus the human-readable `Reasons`.

### 2. Peer selection (`PlatformRelativeScaler.Scale`)

- **Explicit `PeerPlatformIds`** → keep the ranked entries matching those ids; if none match, fall
  back to the **top 3** ranked.
- **Otherwise** → the **top 4** ranked peers.
- **Empty catalog** → a deterministic `synthetic-peer` (`CombatRadiusNm 100`, `MaxHp 100`,
  `MaxSpeedKnots 20`) so callers always get a draft.

### 3. Relative scaling (`WeightedToward`)

Numeric fields are interpolated across the selected peers' `[min, median, max]` by a **role weight**:

| `RoleWeight` string | Weight |
|---------------------|--------|
| `light` | `0.25` |
| `heavy` | `0.75` |
| anything else (`standard`) | `0.5` |

`WeightedToward(values, weight, decimals)` walks half the range at a time:

```
t    = weight <= 0.5 ?  weight * 2        : (weight - 0.5) * 2
from = weight <= 0.5 ?  min(values)       :  median(values)
to   = weight <= 0.5 ?  median(values)    :  max(values)
value = round( from + (to - from) * t , decimals )
```

So `light` interpolates **min → median**, `heavy` interpolates **median → max**, and `standard`
lands on the **median**. This is what makes a `light` archetype come out with lower HP / radius /
speed than a `heavy` one built from the same peers.

### 4. Derived fields

| Field | How it is derived |
|-------|-------------------|
| `CombatRadiusNm` | `WeightedToward(peer radii, roleWeight, 2)` |
| `MaxHp` | `WeightedToward(peer MaxHp, roleWeight, 0)` |
| `WithdrawThresholdPct` | `WeightedToward(peer withdraw %, 1 − roleWeight, 2)` — **inverse** weight (a heavier unit withdraws later) |
| `MaxSpeedKnots` | `WeightedToward(peer speeds, roleWeight, 1)` |
| `LatDeg` / `LonDeg` | Average of the **real** selected peers' positions (synthetic peers skipped), rounded to 4 dp |
| `CruiseSpeedKnots` | `round(MaxSpeedKnots · 0.75, 1)` |
| `RangeNm` | `round(CombatRadiusNm · 2, 1)` |
| `MaxAltitudeFt` / `MaxDepthM` | `30000` when domain `air` / `300` when domain `subsurface`, else `0` |
| `TrlLevel` | `5` when `WhatIf`, else `7` |
| `PlatformClass` | the raw `RoleWeight` string |
| `Domain` | normalized (`air` / `subsurface` (from `sub`/`undersea`) / `land` (from `ground`) / default `surface`) |

Each scaled field is echoed in `proposal.Basis` (`PlatformFieldBasis(Field, Value, PeerIds, Method)`)
so a reviewer can see exactly which peers and which method produced each number.

### 5. Outlier flags

Against the domain's own damage rows, `Scale` flags (non-blocking, surfaced in `proposal.Outliers`):

- `MaxHp` above `domainMax · 1.15`, or
- `MaxHp` below `domainMin · 0.5`.

---

## Staging & the proposal result

`Propose` stages three extend-only batches in order and returns a `PlatformDesignProposeResult`:

```csharp
public sealed record PlatformDesignProposeResult(
    PlatformDesignProposal Proposal,
    string  PlatformBatchId,
    string  DamageBatchId,
    string? MobilityBatchId,   // null when all speed/range are 0
    IReadOnlyList<string> Notes);   // summary + skills + peers + outliers + per-batch confirmations
```

Approve **platform metadata first** (damage/mobility carry an FK to the platform row):

```csharp
var assistant = new PlatformDesignAssistant();
var staged    = assistant.Propose(dbPath, catalog, brief, clock);

writeService.ApproveBatches(dbPath, [staged.PlatformBatchId], clock, "human", "reviewer");
writeService.ApproveBatches(dbPath, [staged.DamageBatchId],   clock, "human", "reviewer");
if (staged.MobilityBatchId is not null)
    writeService.ApproveBatches(dbPath, [staged.MobilityBatchId], clock, "human", "reviewer");
```

`staged.BatchIds` returns the ordered, non-null batch ids for convenience.

## Workbook hand-off (`PlatformDesignWorkbookEmitter`)

For an Excel-first workflow, `Emit(export, proposal, snapshotId, clock)` exports a base workbook and
appends the proposal onto the **Platforms** and **Mobility** sheets; it drops the `_Meta` sheet so a
caller can recompute the content hash. It is a **pure** function and does **not** touch the write
gate — the workbook is then edited/validated/staged through the normal
[platform-workbook-roundtrip.md](platform-workbook-roundtrip.md) pipeline.

---

## CLI

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_design_propose \
  --db data/baltic.db \
  --id opv-scout \
  --name "OPV Baltic Scout" \
  --domain surface \
  --role light \
  --concept "coastal patrol" \
  --what-if
```

Flags: `--draft-only`, `--no-what-if`, `--peer <id>` (repeatable), `--actor-type`, `--actor-id`,
`--clock <UtcTicks>`. `--clock` is optional and intended for **deterministic tests**; when omitted the
CLI uses `DateTime.UtcNow.Ticks` so consecutive proposes get unique batch ids (avoids
`FixedCatalogClock(0)` overwrite collisions).

---

## Tests that pin this

| Test | What it locks |
|------|---------------|
| [`PlatformRelativeScalerTests`](../../src/ProjectAegis.Data.Tests/PlatformAssistant/PlatformRelativeScalerTests.cs) | `light` ≤ `heavy` for HP/radius/speed from the same peers; `WeightedToward` skews toward min/max by role; `UniqueId` collision suffixing; `assistant:` citation + skill pack (incl. `what-if`); `ApplyCorePosition` + core-position fields on the binding. |
| [`PlatformDesignAssistantTests`](../../src/ProjectAegis.Data.Tests/PlatformAssistant/PlatformDesignAssistantTests.cs) | The `Draft` → `Propose` façade: staged batch ids, propose-not-approve behaviour, and mobility-batch skip when speed/range are all zero. |

---

## Related

- [catalog-write-gate.md](catalog-write-gate.md) — the extend-only propose/approve path PDA stages into.
- [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md) — the Excel export/edit/diff/stage round-trip the workbook emitter feeds.
- [catalog-seeding.md](catalog-seeding.md) — how a headless run/test gets the catalog PDA grounds on.
- [ADR-011](../architecture/adr-011-platform-editor-excel-roundtrip.md) — the Excel-primary platform-editor decision.
