# Presentation boundary

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md) §3 (presentation boundary)  
**Program:** UCA-M1 · **Audience:** agents writing/reviewing Unity / UnityAdapter / UI Toolkit C#

## ADR citation note

UCA-M0 scaffold incorrectly labeled this topic **“ADR-018”**. In git, **ADR-018** is **sensor side-picture / datalink** (`docs/architecture/adr-018-sensor-side-picture-datalink.md`) — not the presentation wall.

**Presentation boundary is distilled from:**

| ADR | Pointer (do not re-host full ADR text) |
|-----|----------------------------------------|
| **ADR-010** §2–3 | UI is a **client**; read models / projections are the UI contract |
| **ADR-007** | Map never writes sim / `DecisionLog`; `MapPictureProjection` + shared `MapSymbolEntry` |
| **ADR-001** | Delegation consumes `ISimWorldSnapshot`, emits `Order` only |
| **ADR-006** | Presentation **never opens SQLite**; catalog via Data / approved readers only |

Cite ADR numbers as pointers only. Law lives in those ADRs + this skill reference.

---

## Law (one screen)

1. Presentation **reads projections / snapshots only**.
2. **No** live ECS chunks, session caches, or write handles on MonoBehaviours.
3. **Frame interpolation is presentation-only** — never fake sim ticks in `Update`.
4. New UI field → **extend the projection contract**; never pierce the wall.
5. Snapshots / view models are **immutable or `IReadOnly*`** — views do not mutate shared buffers.
6. Intent leaves presentation as **commands** through the **enqueue facade** (`C2PlayerCommandBridge` / `HumanController.Enqueue` / player-command services). `IOrderSink.ApplyOrder` is **downstream** (after queue drain / bridge tick) — not a UI write path.

Parent doctrine: [`SKILL.md` §3](../SKILL.md#3-presentation-boundary-adr-010-23-adr-007-adr-001).

---

## Layering

```text
ProjectAegis.Sim / ECS host
    │  builds per-tick ISimWorldSnapshot
    ▼
Delegation (ADR-001)  ──► Order / DecisionLog append (authoritative path only)
    │
    ▼
Projection/*  (pure, engine-agnostic read models)
    │  e.g. MapPictureProjection → MapSymbolEntry
    ▼
UnityAdapter Bridge/*  (glue)
    │  e.g. MapPictureBridge.Build(snapshot, registry, log, seed)
    ▼
C2PresentationController + *PanelHost / binders
    │  selection, camera, USS, interpolation
    ▼
UI Toolkit / Cesium / MonoBehaviour views
```

| Layer | Owns | Clock |
|-------|------|-------|
| Sim + Delegation | Truth, orders, append-only log, fingerprints | Sim tick |
| Projection / Bridge | Read models from snapshot + log | Per tick (or per edit) |
| Presentation | Selection, camera, layout, interpolation | Frame rate |

---

## Allow / deny

| Allow | Deny |
|-------|------|
| Bind `IReadOnlyList<MapSymbolEntry>` (or other projection DTOs) on hosts | Hold `EntityQuery`, `SystemState`, or live ECS chunk refs on MBs |
| Rebuild projections each sim tick via bridge | Cache `SimulationSession` / orchestrator internals on a view |
| Selection in `C2PresentationController` (presentation-only) | Write `DecisionLog` / order log from UI or binder |
| Lerp symbol poses between last two **received** snapshots | Advance sim, roll RNG, or “step” policies inside `Update` |
| Issue player intent via enqueue facade (`C2PlayerCommandBridge` / `HumanController.Enqueue` / player-command services) | Call `IOrderSink.ApplyOrder` from UI, or mutate member alive / contacts / magazines from a panel |
| Read catalog edges via `ICatalogReader` for graph surfacing | Open SQLite from presentation (ADR-006) |
| Headless-test projections under `dotnet test` | Put authoritative scenario/sim state in scene / SO / widget fields |

| Data / log | Presentation rule |
|------------|-------------------|
| `DecisionLog` | **Append-only** on the authoritative path. UI **never** writes. UI may **project** chronological entries via `*Projection` / `*Bridge`. |
| `ISimWorldSnapshot` | Per-tick **read** contract into bridges/projections — not a free-for-all ECS poke. |
| SQLite / catalog DB | **No** direct open from UI (ADR-006). DTOs / `ICatalogReader` only where already approved. |
| Replay hashes | Projection rebuild **OK**; projections are **not** in golden hashes unless explicitly defined as artifacts (ADR-010 §3, ADR-007). |

---

## Real anchors (repo)

| Anchor | Role |
|--------|------|
| `MapPictureBridge.Build(snapshot, registry, log, seed)` | UnityAdapter bridge → `IReadOnlyList<MapSymbolEntry>` |
| `MapPictureProjection` / `MapSymbolEntry` | Headless map read model (ADR-007) |
| `C2PresentationController` | Presentation-only selection + graph surfacing (`UnityAdapter/Presentation/`) |
| `ISimWorldSnapshot` | Per-tick world indicators into bridges (ADR-001 seam) |
| `DecisionLog` | Append-only order/decision stream — UI reads via projections only |
| `DelegationBridgeHost` | Composition: builds `LastMapSymbols` etc. from bridges each tick |

Canonical map path (simplified):

```csharp
// MapPictureBridge.Build — real signature shape
public static IReadOnlyList<MapSymbolEntry> Build(
    ISimWorldSnapshot snapshot,
    TargetRegistry registry,
    DecisionLog log,
    int layoutSeed)
{
    var oob = OobTreeProjection.Project(registry.CollectMemberIds(), snapshot.IsMemberAlive);
    var contacts = ContactPictureProjection.Project(log);
    return MapPictureProjection.Project(oob, contacts, layoutSeed);
}
```

`C2PresentationController`: holds `SelectionSet`, exposes `IReadOnlySelectionSet` / `SelectedUnitId`; mutates selection only through `SelectFriendlyUnit` / `SelectHostileContact` / `ApplyDefaultSelection`. Does **not** append to `DecisionLog` or touch sim world.

---

## Snapshots & immutability (c-sharp-engineer)

| Prefer | Avoid |
|--------|-------|
| `IReadOnlyList<T>`, `IReadOnlyDictionary<K,V>`, `sealed record` DTOs | `List<T>` fields on hosts that views clear/add into shared instance |
| Treat bridge output as **read-only for the frame** | Mutating `MapSymbolEntry` / projection rows in-place from a view |
| Copy or pool **private** presentation buffers if you need mutability | Sharing a mutable buffer with projection builders and UI |
| Rebuild projection per tick (alloc budget conscious) | Long-lived MB field pointing at “current ECS world” |

**Guidance:**

- Prefer **immutable records** for projection rows (`MapSymbolEntry` is a `sealed record`).
- If a binder needs display rows, produce a **new** `*PanelState` — do not edit the semantic projection list in place.
- Do not store the last `ISimWorldSnapshot` implementer on a panel and re-query live ECS from `OnGUI`/`Update`.
- Allocation: rebuild on **sim tick boundary** (or dirty flag), not every render frame unless measured and cheap; avoid per-frame LINQ/string concat on symbol lists (see future `performance-unity.md`).

---

## Frame clock vs sim clock

| Presentation `Update` may | Presentation `Update` must not |
|---------------------------|--------------------------------|
| Interpolate transforms between snapshot A → B | Call sim step / `RunTick` “to smooth motion” |
| Animate USS, camera, selection chrome | Sample RNG / policy / sensors for display truth |
| Apply layout-only ghosts (e.g. comms lag visuals from binder) | Invent contact/alive state not in projection |

**Fake sim tick in Update** = hard fail on review. Smoothness is cosmetic; truth advances only on the sim/delegation tick path.

---

## New field → extend the contract (never pierce)

When a panel needs a new piece of world truth:

1. Add/extend the **projection DTO** (and `*Projection` / `*Bridge` if needed).
2. Feed it from snapshot indicators and/or `DecisionLog` fold — headless-testable.
3. Bind in `*PanelBinder` / host.
4. **Do not** reach into ECS, session, or SQLite from the view.

### Good — extend projection contract

```csharp
// Illustrative: new display need → new optional field on the shared contract
public sealed record MapSymbolEntry(
    string SymbolId,
    string Affiliation,
    string ShapeGlyph,
    string Label,
    float NormalizedX,
    float NormalizedY,
    bool IsDestroyed,
    string? FuelBand = null); // NEW: projected, not live-sim poke

// Headless projection fills it from approved inputs only
public static IReadOnlyList<MapSymbolEntry> Project(
    IReadOnlyList<OobTreeEntry> oob,
    IReadOnlyList<ContactPictureEntry> contacts,
    int layoutSeed,
    IReadOnlyDictionary<string, string>? fuelBandsById = null)
{
    // pure fold → new records; no UnityEngine; no log.Append
}

// View binds read-only rows only
void Bind(IReadOnlyList<MapSymbolEntry> symbols)
{
    foreach (var s in symbols)
        SetLabel(s.SymbolId, s.FuelBand ?? "—");
}
```

### Bad — pierce the wall

```csharp
// BAD: MonoBehaviour reaches live session / ECS / log write
sealed class MapPanelHost : MonoBehaviour
{
    SimulationSession _session; // do not cache authoritative session on a view
    EntityQuery _units;         // do not hold live ECS queries on UI MBs

    void Update()
    {
        _session.Step(Time.deltaTime); // BAD: invent sim motion
        foreach (var e in _units.ToEntityArray(Allocator.Temp))
            Draw(e); // BAD: raw ECS instead of MapPictureBridge
        _session.DecisionLog.Append(/* chrome */); // BAD: UI writes log
    }
}
```

### Bad — mutate shared projection buffer

```csharp
// BAD: view mutates shared list used by other panels / next tick
void Highlight(List<MapSymbolEntry> symbols, string id)
{
    for (int i = 0; i < symbols.Count; i++)
        if (symbols[i].SymbolId == id)
            symbols[i] = symbols[i] with { Label = ">>" + symbols[i].Label };
}
```

Prefer a **presentation-only** highlight set on `C2PresentationController` (or binder flags), leaving projection rows intact.

---

## DecisionLog & orders

| Action | Who |
|--------|-----|
| Append decisions / engagements / contact changes | Orchestrator / sim / approved order path |
| Project message log, contacts, map, OOB | `*Projection` / `*Bridge` (read) |
| Selection, graph highlight ids | `C2PresentationController` (presentation state) |
| Player order intent | Command → enqueue facade → queue drain → `IOrderSink` (bridge tick) — **not** ad-hoc `DecisionLog` or direct sink from UI |

Map rule (ADR-007): **Map UI never writes to `DecisionLog` or sim world.** Symbol list is a **per-tick** projection via `MapPictureBridge.Build(...)`.

---

## Agent checklist (before Done)

- [ ] UI path reads **snapshot/projection** only — no live ECS/session fields on MBs
- [ ] No sim step / policy / RNG advanced from `Update` for “smoothness”
- [ ] New display field extended **projection/bridge contract** + headless test where practical
- [ ] View models `IReadOnly*` / records; no mutating shared projection buffers
- [ ] No `DecisionLog` write from presentation; no SQLite from UI (ADR-006)
- [ ] Player intent uses enqueue facade — not direct `IOrderSink.ApplyOrder` from view code
- [ ] Selection stays in `C2PresentationController` (or equivalent presentation store)
- [ ] Projection rebuild understood as **non-hash** unless ADR/artifact says otherwise
- [ ] PR cites **ADR-010 / 007 / 001** (and **006** if data) — **not** ADR-018 for this topic

---

## See also

| Doc | Use |
|-----|-----|
| [`../SKILL.md`](../SKILL.md) §3 | Parent presentation-boundary doctrine |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | UI client + projection contract |
| `docs/architecture/adr-007-c2-map-presentation.md` | Map / `MapSymbolEntry` rules |
| `docs/architecture/adr-001-sim-assembly-boundary.md` | Snapshot in / Order out |
| `docs/architecture/adr-006-data-layer-boundary.md` | No SQLite from presentation |
| `docs/engineering/c2-projection-layer.md` | Projection → binder → state playbook |
| `src/ProjectAegis.Delegation.UnityAdapter/README.md` | Bridges + `C2PresentationController` |

**Not this topic:** ADR-018 (datalink / side-picture merge) — different boundary.
