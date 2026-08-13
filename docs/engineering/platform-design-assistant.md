# Platform Design Assistant

**Status:** Headless production path (DRG-73 / PDA-04)  
**Gate:** ADR-011 Excel-primary + CatalogWriteGate extend-only  
**Namespace:** `ProjectAegis.Data.PlatformAssistant`

## Purpose

Draft **future / hypothetical** platform archetypes **relative to existing catalog peers**, then stage them as **extend-only** write-gate batches. This is a **proposal agent**, not a WYSIWYG Platform Editor.

## Skills

| Skill | Role |
| --- | --- |
| catalog-grounding | Peers from `ICatalogReader.LoadExportData()` |
| archetype-schema | `CatalogPlatformBinding` + damage + mobility |
| relative-scaling | Role weight light 0.25 / standard 0.5 / heavy 0.75 |
| provenance | `CitationRef = assistant:{peerIds}` |
| gate-policy | New ids only; stage via Propose*Batch |
| workbook-emit | Optional Excel append via `PlatformDesignWorkbookEmitter` |
| what-if | TRL 5 + staged until ApproveBatch |

## API

```csharp
var assistant = new PlatformDesignAssistant();
var draft = assistant.Draft(catalog, brief);
var staged = assistant.Propose(dbPath, catalog, brief, clock);
// Approve platform batch BEFORE damage/mobility (FK).
writeService.ApproveBatches(dbPath, [staged.PlatformBatchId], clock, "human", "reviewer");
writeService.ApproveBatches(dbPath, [staged.DamageBatchId], clock, "human", "reviewer");
```

Scaled `LatDeg` / `LonDeg` / `CombatRadiusNm` ride on `CatalogPlatformBinding` with
`ApplyCorePosition: true` so `ApproveBatch` writes them into live `platform` rows
(migration `015_platform_staging_core_position.sql`). Callers that leave
`ApplyCorePosition` false keep the historical UpsertPlatform behavior
(existing lat/lon/radius, else `0/0/1.0` for new rows).

Host bridge: `PlatformDesignAssistantBridge` (UnityAdapter).  
CLI verb: `platform_design_propose` (also registered in `tools/mission-editor/mcp-tools.json`).

## Peer scoring & relative scaling (the math)

The `Draft` path is two pure, deterministic static helpers over a catalog export snapshot
(`PlatformCatalogExportData`). Both are engine-agnostic and have **no** RNG or clock, so the same
`(export, brief)` always yields the same proposal.

### 1. Peer ranking — [`PlatformPeerScorer.Score`](../../src/ProjectAegis.Data/PlatformAssistant/PlatformPeerScorer.cs)

Scores every export platform except the brief's own `PlatformId`, then returns them
**`OrderByDescending(Score).ThenBy(PlatformId, Ordinal)`** (stable). Two modes:

- **Curator-fixed peers** (`brief.PeerPlatformIds` non-empty): a listed id scores `+100`
  (`"curator-selected peer"`); any other platform scores `+1` (`"non-selected fallback"`) so it can
  still act as a fallback.
- **Concept-driven** (no fixed peers): base `+20` (`"catalog peer"`), then `+12` per concept token
  found (case-insensitive) in the platform id (tokens come from `Concept + DisplayName + Domain`,
  lower-cased, split on ` -_/,;\t\n`, length `> 2`, distinct), `+10` if `CombatRadiusNm > 0`
  (`"has combat radius"`), `+5` if a damage model with `MaxHp > 0` exists.

Each result is a `PlatformPeerScore(PlatformId, Score, Reasons, CombatRadiusNm, MaxHp, MaxSpeedKnots)`
— the `Reasons` list is the human-readable audit of why a peer ranked where it did.

### 2. Relative scaling — [`PlatformRelativeScaler.Scale`](../../src/ProjectAegis.Data/PlatformAssistant/PlatformRelativeScaler.cs)

1. **Pick peers** from the ranked list: the curator's selected ids if any survive ranking (else the
   top ≤3), otherwise the top ≤4. An **empty catalog** falls back to a single deterministic
   `synthetic-peer` (`100/100/20`) so callers still get a draft.
2. **Role weight** (`RoleWeight`): `light → 0.25`, `heavy → 0.75`, anything else → `0.5`.
3. **Interpolate each field** with `WeightedToward(values, weight, decimals)` — a triangular
   interpolation across the peers' `[min, median, max]`: for `weight ≤ 0.5` it runs `min → median`
   (`t = weight * 2`), for `weight > 0.5` it runs `median → max` (`t = (weight − 0.5) * 2`), then
   rounds to `decimals`. Applied to `CombatRadiusNm` (2dp), `MaxHp` (0dp), `MaxSpeedKnots` (1dp), and
   `WithdrawThresholdPct` — the latter uses the **inverse** weight `1 − weight` (tankier platforms
   withdraw later). `CruiseSpeedKnots = speed * 0.75`, `RangeNm = combatRadius * 2`.
4. **Position** `LatDeg`/`LonDeg` = the average of the *real* selected peers (synthetic skipped),
   rounded 4dp.
5. **Id & provenance**: `UniqueId` appends `-2`, `-3`, … until the id is free; `CitationRef =
   assistant:{peerIds}`; `SourceFactId = "platform-design-assistant"`; TRL `5` for `--what-if`, else
   `7`; all rows staged `Provisional` at `GameplayAbstraction` tier.
6. **Outliers**: warns if scaled `MaxHp` is `> 1.15×` the domain max or `< 0.5×` the domain min.

The result is a `PlatformDesignProposal` carrying the `Binding`/`Damage`/`Mobility` rows, the ranked
`Peers`, a `PlatformFieldBasis[]` (one per scaled field, citing the peer ids + method string), the
`Outliers`, the `SkillsApplied` list (adds `what-if` when set), and a human `Summary`. Nothing here
touches the live catalog — staging happens later via `Propose*Batch` (see **API** above).

> **Determinism.** Ordinal sorting, median/min/max over the fixed peer set, and fixed rounding make
> `Scale` reproducible; only `Propose` introduces a clock (batch ids), which `--clock` pins for tests.

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

Flags: `--draft-only`, `--no-what-if`, `--peer <id>` (repeatable), `--actor-type`, `--actor-id`, `--clock`.

`--clock <UtcTicks>` is optional and intended for **deterministic tests**. When omitted,
the CLI uses `DateTime.UtcNow.Ticks` so consecutive proposes get unique batch ids
(avoids FixedCatalogClock(0) overwrite collisions).

## Invariants

- Core position fields are opt-in via `ApplyCorePosition` (minimal gate extension only).
- **No** `DelegationBridge` touch.
- Unedited workbook empty-diff golden remains green.
- Live catalog unchanged until explicit ApproveBatch.
