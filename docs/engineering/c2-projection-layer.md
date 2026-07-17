# C2 projection layer — order-log read-models for the tactical picture

The `Projection/` folder in [`ProjectAegis.Delegation`](../../src/ProjectAegis.Delegation/Projection/)
(~75 files) is the **read side** of the simulation: the pure, engine-agnostic view-model layer
that turns the append-only order log and per-tick sim indicators into everything the C2
(command-and-control) UI draws — the message log, contact/facility picture, OOB tree, tactical
map, sensor panel, losses/scoring, catalog browsers, and APP-6 map symbology.

This guide explains the layering (`Projection → Binder → State`), the hard "read-only" contract
that keeps the UI from corrupting a deterministic run, the projection catalog, and how to add a
new panel without breaking replay.

> **Who consumes this?** Under Unity, the [`UnityAdapter`](../../src/ProjectAegis.Delegation.UnityAdapter/README.md)
> hosts [`C2PresentationController`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs)
> and the `*Bridge`/`*PanelBinder` glue that drives UI Toolkit panels. Headless tests and the
> Baltic batch/replay harness call the same projections directly — there is no Unity dependency
> in this folder, so the whole read model runs under `dotnet test`.

Related: [Delegation README — order log & projections](../../src/ProjectAegis.Delegation/README.md#order-log--projections) ·
[determinism-and-replay.md](determinism-and-replay.md) ·
[abort-reason-catalog.md](abort-reason-catalog.md) ·
[ADR-003 order-log schema](../architecture/adr-003-order-log-schema.md) ·
[ADR-007 C2 map presentation](../architecture/adr-007-c2-map-presentation.md) ·
[ADR-010 headless-first command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md).

---

## The core rule: projections never mutate

Every type here is a **pure function of already-recorded state**. A projection reads a
[`DecisionLog`](../../src/ProjectAegis.Delegation/Decision/DecisionLog.cs) (the append-only
[`IOrderLog`](../../src/ProjectAegis.Delegation/Decision/IOrderLog.cs), ADR-003) and/or a
read-only per-tick snapshot, and returns an immutable `record`. It **must not**:

- write to the order log, the sim, or the catalog;
- hold mutable static state, wall-clock time, or ambient randomness;
- change the order-log fingerprint that the replay goldens assert.

This is what lets the UI rebuild the entire tactical picture from the log at any tick (including
during a scrub/replay) without perturbing the simulation. Determinism is the load-bearing
invariant of the whole codebase — see [determinism-and-replay.md](determinism-and-replay.md).
Because projections only read, they are safe to run on any thread and to re-run every frame.

Two consequences worth internalizing:

- **Ordering is explicit and total.** Projections that fold the log sort by `(SimTick/SimTime,
  SequenceId)` and break ties with `StringComparer.Ordinal` on ids — never rely on dictionary
  or hash-set enumeration order. See `ContactPictureProjection`, `OobTreeProjection`,
  `MapPictureProjection`.
- **Layout that has no sim source is derived deterministically**, not randomly.
  `MapPictureProjection.Place(key, seed)` hashes `"{seed}:{key}"` via `DeterministicHash` to a
  stable normalized `(x, y)` — so the map is reproducible until the sim publishes real world
  coordinates.

---

## Layering: `Projection → Binder → State`

Panels are built in up to three engine-agnostic stages. Not every panel needs all three, but
the shape is consistent:

| Stage | Suffix | Role | Example |
|-------|--------|------|---------|
| **Projection** | `*Projection` | Fold the order log / snapshot into a semantic model (domain rows, tallies, tracks). Static class, `Project(...)`/`Build(...)`. | `MessageLogProjection.Project(log)` → `IReadOnlyList<MessageLogLine>` |
| **Binder** | `*PanelBinder` | Map the semantic model into display rows: style classes (USS), formatted text, selection/ghost flags. Static class, `Bind(...)`. | `MessageLogPanelBinder.Bind(lines)` → `MessageLogPanelState` |
| **State** | `*PanelState` / `*State` | The immutable view-model the UI host renders. `sealed record` of display rows. | `MessageLogPanelState(IReadOnlyList<MessageLogDisplayRow>)` |

Why split projection from binder? The **projection** is the reusable, testable "what happened"
model (also consumed by CSV exporters, the AAR, and other panels). The **binder** owns the
presentation concerns that are allowed to be opinionated — USS class names, `"[CATEGORY] text"`
formatting, ghost-track offsets — without polluting the semantic model. Keeping USS strings in
the binder means a restyle never touches projection logic or tests.

```text
DecisionLog (append-only order log, ADR-003)
    │  + per-tick ISimWorldSnapshot indicators (EMCON, fire-control, engagement count)
    ▼
*Projection.Project(...)      → semantic model  (MessageLogLine, ContactPictureEntry, …)
    ▼
*PanelBinder.Bind(...)        → display rows     (styles, glyphs, formatted text)
    ▼
*PanelState (sealed record)   → rendered by C2PresentationController / UI Toolkit
```

### Example: message log, end to end

`MessageLogProjection` switches on `OrderLogEntry.Kind` and emits a stable **category** per row
(the same categories the alert tiering keys on — see below). A subset of the mapping, verified
against [`MessageLogProjection.cs`](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs):

| Order-log entry kind | Category | Notes |
|----------------------|----------|-------|
| `EngagementOutcome` = `Kill` | `KILL_CONFIRMED` | Inbound-threat criticality lives here, not on launch |
| `EngagementOutcome` = `Intercept` / `Hit` / `Miss` | `INTERCEPT_SUCCESS` / `HIT` / `MISS` | |
| `Engagement` (launched) | `WEAPON_LAUNCH` | Fires on friendly launches too → `Routine` alert tier |
| `Engagement` (aborted) | `ENGAGE_ABORT` | Carries the [abort reason code](abort-reason-catalog.md) |
| `PolicyDenial` | `POLICY_DENIAL` | "Why can't I fire?" — links the explain |
| `ContactChange` | `CONTACT` | |
| `MagazineChange` | `MAGAZINE` | Signed delta + reason code |
| `ModeChange` | `MODE` | |
| `PlayerOrder` | `PLAYER_ORDER` | |
| `CommsStateChange` | `COMMS` | |
| `FuelStateChange` / `FuelBurn` | `FUEL` | |

Unrecognized entry kinds project to `null` (dropped) rather than throwing, so a new order-log
kind never crashes the log panel — it just isn't surfaced until a case is added.

`MessageLogPanelBinder.Bind` then formats each line as `"[{Category}] {Text}"` into a
`MessageLogPanelState`. The **`MessageLogLine` carries no duplicate storage** — it references
the log's `SequenceId`/`SimTime`, so the message log is a projection, not a second event store.

---

## C2 rev-2 alert & lifecycle contracts

The req-20 rev-2 UI delta added a few small, presentation-only taxonomy contracts here so the
parallel UI tracks and the Unity host share one source of truth (ADR-010). All are read-only
lookups; none touch sim or order-log state.

- **`AlertSeverity`** — alert tier: `Critical` (toast + optional auto-pause) → `Notable`
  (log highlight) → `Routine` (log only). Tier is **never colour-only** (accessibility).
- **`AlertSeverityMap.ForCategory(category)`** — the single mapping from a `MessageLogLine`
  category to an `AlertSeverity`. It is **case-insensitive and fails safe**: unknown/null
  categories default to `Routine`, so adding a new message category never silently escalates it
  to a toast. `WEAPON_LAUNCH` is deliberately `Routine` (it fires on friendly launches);
  inbound criticality is carried by `KILL_CONFIRMED` / `POLICY_DENIAL`.
- **`OrderLifecycleState`** — the player-order lifecycle surfaced to the UI:
  `Accepted → Queued → Executing → Completed | Denied | Aborted` (last three terminal).
  `Denied` links the "Why can't I fire?" explain to the matching `POLICY_DENIAL`.

The remappable input-action IDs the UI binds to (`input.cycle_unit`,
`input.focus_primary_threat`, `input.cancel`) live in the sibling `Input/` folder
([`C2InputActions`](../../src/ProjectAegis.Delegation/Input/C2InputActions.cs)), not here.

---

## APP-6 / MIL-STD-2525C map symbology (ADR-007)

The map layer resolves tactical symbols in a data-driven, atlas-optional way:

- **[`App6Sidc`](../../src/ProjectAegis.Delegation/Projection/App6Sidc.cs)** maps an affiliation
  (`Friendly` / `Hostile` / `Neutral` / `Suspect` / `Pending`) + destroyed flag to three things:
  a **unicode fallback glyph**, a **USS frame class** (`map-app6-frame--*`), and a **15-char
  SIDC** string. It can also parse the Standard Identity character out of an existing SIDC
  (`'F'/'A'/'D'/'M'/'J'/'K'/'L'` → Friendly, `'H'` → Hostile, …). Anything missing or malformed
  resolves to the neutral **`FallbackSidc` / `FallbackGlyph` (`●`)** — never an exception.
- **[`App6GlyphAtlas`](../../src/ProjectAegis.Delegation/Projection/App6GlyphAtlas.cs)** decides
  what the UI actually paints: if a sprite atlas is loaded and has the frame, it returns a
  `DisplayGlyph` that uses the **atlas frame class**; otherwise it **degrades to the unicode
  glyph**. This is the ADR-007 Phase C "atlas-optional" contract — headless tests and unstyled
  hosts still get a legible glyph.
- **[`MapPictureProjection`](../../src/ProjectAegis.Delegation/Projection/MapPictureProjection.cs)**
  builds `MapSymbolEntry` rows from the OOB (friendly) and contact picture (hostile), placing
  each with the deterministic hash-layout above.
- **[`MapPanelBinder`](../../src/ProjectAegis.Delegation/Projection/MapPanelBinder.cs)** applies
  affiliation/selection/comms USS classes and, under `CommsState.Degraded`, appends a **ghost
  row** (`ghost:{id}`, offset by the scenario's `GhostOffset`, labelled with the lag ticks) to
  visualize track staleness; `CommsState.Denied` marks symbols `--frozen`.

---

## Projection catalog

Grouped by the panel/surface they feed. All live in
[`src/ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/).

### Tactical picture & map
| Type | Produces |
|------|----------|
| `MessageLogProjection` → `MessageLogPanelBinder` | CMANO-style message log (`MessageLogLine` → `MessageLogPanelState`) |
| `ContactPictureProjection` | Active contact tracks from `ContactChange` rows; `ProjectWithBda` merges order-log BDA "Lost" rows |
| `SensorC2Projection` | Contact picture + per-tick indicators (radar EMCON, fire-control track, active engagements) via `ISensorC2WorldIndicators` |
| `MapPictureProjection` → `MapPanelBinder` | Tactical map symbols + ghost/frozen comms overlays |
| `App6Sidc` / `App6GlyphAtlas` / `App6AtlasCatalog` / `App6*` | APP-6/2525C glyph + SIDC + atlas resolution |
| `ContactSummaryProjection` | Single-contact inspector line |
| `CesiumBillboardProjection` | Cesium globe billboards (ADR-007 map path) |

### Force status & inspectors
| Type | Produces |
|------|----------|
| `OobTreeProjection` → `OobTreePanelBinder` | Order-of-battle tree (sorted member ids + alive state) |
| `UnitDetailProjection` → `UnitDetailPanelBinder` | Selected-unit detail pane (incl. attack menu) |
| `FacilityPictureProjection` | Facility picture + capacity/damage states |
| `MissionListProjection` → `MissionListPanelBinder` | Mission board rows |
| `FuelStateProjection` / `CommsStateProjection` | Fuel band / comms state panels |

### Combat, BDA & scoring
| Type | Produces |
|------|----------|
| `OrderLogBdaProjection` | Battle-damage-assessment contact-damage states from the log |
| `OrderLogFacilityDamageProjection` | Facility damage change records |
| `EngagePreviewProjection` / `EngageAttackOptions` / `EngageAttackOrderResolver` | Attack menu preview + order resolution |
| `LossesScoringProjection` → `LossesScoringCsvExporter` | Score tally + headless CSV export (doc 17) |

### Catalog / import surfacing (Mission Editor & data QA)
| Type | Produces |
|------|----------|
| `CatalogPlatformBrowseProjection`, `PlatformCatalog{List,Detail,Filter}Projection`, `PlatformCommsListProjection`, `CatalogLinkListProjection`, `PlatformLinkListProjection` | Catalog browsers |
| `CatalogImportProvenanceProjection`, `CatalogImportQuarantineProjection`, `MountLoadoutQuarantineProjection`, `PlatformImportStagingProjection` | Import provenance / quarantine surfacing |
| `DoctrineInheritanceProjection` → `DoctrineInheritancePanelBinder` | Doctrine inheritance panel |

### C2 chrome & shared contracts
| Type | Role |
|------|------|
| `C2TopBarProjection` / `C2PlanningChromeProjection` | Top bar + planning chrome view models |
| `C2SelectionResolver` | Default/valid selection id resolution (pure) |
| `AlertSeverity` / `AlertSeverityMap` | Alert tiering (above) |
| `OrderLifecycleState` | Player-order lifecycle enum (above) |

---

## Losses/scoring CSV export (headless AvA)

`LossesScoringProjection.Project(log, baseScore)` tallies
`score = baseScore + kills×100 − denials×5`, plus `HostileKills`, `MissilesFired`
(sum of negative magazine deltas), and `PolicyDenials`.
[`LossesScoringCsvExporter`](../../src/ProjectAegis.Delegation/Projection/LossesScoringCsvExporter.cs)
formats one CSV row per side for agent-vs-agent batch runs:

```
scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
```

The last column is the order-log `ComputeFingerprint()` — so a batch CSV doubles as a
determinism ledger: same `(scenario, seed)` ⇒ same fingerprint. Values are CSV-escaped
(quotes doubled, newlines flattened) for safe ingestion by the QA Gauntlet oracle
(see [qa-gauntlet.md](qa-gauntlet.md)).

---

## Selection state (Unity host)

Selection is presentation state, so it lives with the host, not in the log:
[`C2PresentationController`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs)
(Unity adapter) holds an ordered, de-duplicated
[`SelectionSet`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/SelectionSet.cs)
exposed read-only as `Selection` (`IReadOnlySelectionSet`), with `SelectedUnitId` as the anchor.

Mutate selection only through `SelectFriendlyUnit` / `SelectHostileContact` /
`ApplyDefaultSelection` so the coordinated side effects stay correct — in particular, moving
selection **clears stale graph-surfacing highlights** (`LastGraphHighlightIds` /
`LastGraphLinkChainDisplay`) so a bound graph panel never shows highlights for a unit that is no
longer selected (fixed under `qa-loop-08`). Graph surfacing itself
(`ApplyGraphSurfacing(catalog)`) reads only from `ICatalogReader.GetSortedDependencyEdges()` —
no `DelegationBridge`, no sim mutation ([ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md), headless-first). The pure default-selection and
symbol→id resolution helpers live in the engine-agnostic
[`C2SelectionResolver`](../../src/ProjectAegis.Delegation/Projection/C2SelectionResolver.cs).

See the [adapter README — selection state](../../src/ProjectAegis.Delegation.UnityAdapter/README.md#selection-state--c2presentationcontroller).

---

## Adding a new panel

1. **Model first.** Add a `*Projection` (static class, `Project(DecisionLog …)` or
   `Build(snapshot, indicators)`) returning an immutable `record`. Fold the log with explicit
   `(SimTick/SimTime, SequenceId)` + `StringComparer.Ordinal` ordering. Read only.
2. **If it surfaces a new order-log entry kind**, add a case to `MessageLogProjection` (and a
   category to `AlertSeverityMap` if it should alert) — don't invent a parallel event store.
   Keep the default arm safe (`_ => null` / `Routine`).
3. **Bind** presentation concerns in a `*PanelBinder` → `*PanelState`. USS class strings and
   text formatting go here, never in the projection.
4. **Symbology?** Reuse `App6Sidc` / `App6GlyphAtlas`; do not hard-code glyphs in the binder.
5. **Test** in
   [`src/ProjectAegis.Delegation.Tests/Projection/`](../../src/ProjectAegis.Delegation.Tests/Projection/)
   (one `*Tests.cs` per type — 39 fixtures today). Assert on the projected model, and for
   anything determinism-sensitive, assert stable ordering and (where relevant) that the order-log
   `ComputeFingerprint()` is unchanged by projecting.
6. **Verify** with the standard block — projections are part of the solution baseline:

```bash
dotnet build ProjectAegis.sln
dotnet test  src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -v minimal
```

## Common pitfalls

- **Mutating from a projection or binder.** The most damaging bug class: it desyncs the UI from
  the log and can perturb replay. Projections read; hosts hold selection/UI state.
- **Relying on enumeration order** of a `Dictionary`/`HashSet` in a projection — replay
  non-determinism. Always apply a total, ordinal ordering before returning.
- **Throwing on an unknown enum/category.** Follow the fail-safe defaults (`_ => null`,
  `Routine`, `FallbackSidc`) so new order-log kinds degrade gracefully.
- **Hard-coding glyphs or SIDC strings** in a binder instead of going through `App6Sidc` —
  breaks the atlas-optional contract and the affiliation table.
- **Leaving stale cross-panel state** on the host across a selection change (see the
  `qa-loop-08` graph-surfacing clear).

---

## See also

| Topic | Doc |
|-------|-----|
| Delegation core & the order log | [`src/ProjectAegis.Delegation/README.md`](../../src/ProjectAegis.Delegation/README.md) |
| Unity adapter, `C2PresentationController`, `SelectionSet` | [`src/ProjectAegis.Delegation.UnityAdapter/README.md`](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) |
| Determinism rules, hashing, golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| Abort-reason codes surfaced in the message log | [abort-reason-catalog.md](abort-reason-catalog.md) |
| Batch CSV → oracle QA loop | [qa-gauntlet.md](qa-gauntlet.md) |
| Order-log schema | [ADR-003](../architecture/adr-003-order-log-schema.md) |
| C2 map / APP-6 presentation | [ADR-007](../architecture/adr-007-c2-map-presentation.md) |
