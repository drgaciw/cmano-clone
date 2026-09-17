# Combat VFX projection read-model

> **Scope.** The pure, headless read-model behind the **CMO-style map combat VFX** (Track C,
> 2026-08-17) — the transient shooter→target *fire lines* and victim *impact markers* the tactical
> map draws when an engagement resolves. It lives in
> [`ProjectAegis.Delegation/Projection/CombatVfxProjection.cs`](../../src/ProjectAegis.Delegation/Projection/CombatVfxProjection.cs)
> and follows the read-only projection contract of ADR-010 §2–3 / ADR-007: it is derived from the
> `DecisionLog` order-log stream, has **no `UnityEngine` dependency**, does **no mutation**, and
> never touches the sim hot path or the replay fingerprint. It explicitly **ignores**
> `EngagementOutcomeRecord.PkDraw` so it can never read or advance sim RNG. The Unity layer that
> draws it is
> [`MapCanvasTransientEffectsRenderer`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapCanvasTransientEffectsRenderer.cs).
>
> Sibling of the [combat-domains-hot-tick-hud.md](combat-domains-hot-tick-hud.md) overlay: both are
> presentation folds off the order log. This one places *geometry on the map*; that one is a
> *domain status* HUD. Neither is combat authority — the *engage-time* kill-chain lives in
> [engagement-pipeline.md](engagement-pipeline.md).

---

## Where it sits

The VFX frame is a one-way, per-refresh presentation fold; no state flows back into the sim:

```
DecisionLog (order log)                        LastMapSymbols (MapPictureBridge)
   │  EngagementOutcome entries                    │  unit-id → normalized (X,Y)
   └───────────────────────┬───────────────────────┘
                           ▼
     CombatVfxProjection.Project(log, symbols, nowSimTime)
                           ▼
        CombatVfxFrame  (FireLines[], ImpactMarkers[])
                           ▼
     DelegationBridgeHost.LastCombatVfx   (off the DelegationBridge.Tick hot path)
                           ▼
     MapCanvasTransientEffectsRenderer.Sync(frame)  →  UI Toolkit layer (USS state class per shape)
```

The producer is wired in
[`DelegationBridgeHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs),
in the post-`Tick` C2 refresh (right after `LastMapSymbols` is built), never inside
`DelegationBridge.Tick`:

```csharp
LastCombatVfx = CombatVfxProjection.Project(
    Bridge.Orchestrator.DecisionLog,
    LastMapSymbols,
    snapshot.SimTime);
```

## Inputs

| Input | Source | Notes |
|-------|--------|-------|
| `DecisionLog? log` | `Bridge.Orchestrator.DecisionLog` | Only `OrderLogEntryKind.EngagementOutcome` (=10) entries carrying an `EngagementOutcomeRecord` payload are read (via `log.ChronologicalEntries()`). Launches and aborted engagements (`EngagementRecord`) are ignored. |
| positions | `IReadOnlyList<MapSymbolEntry>?` **or** `IReadOnlyDictionary<string,(float X,float Y)>?` | Two `Project` overloads. The symbol overload calls `BuildPositionIndex` to fold `MapSymbolEntry.SymbolId → (NormalizedX, NormalizedY)`. |
| `double nowSimTime` | `snapshot.SimTime` | Drives the age-based expiry of each shape. Sim-time, not wall-clock. |

`Project` returns [`CombatVfxFrame.Empty`](../../src/ProjectAegis.Delegation/Projection/CombatVfxProjection.cs) when
`log` is null, positions is null/empty, or the log has no chronological entries.

### `BuildPositionIndex`

Folds map symbols into an **ordinal** unit-id → xy index:

- Null/empty symbol list → shared empty dictionary.
- Null symbols or blank `SymbolId` are skipped.
- `TryAdd` — the **first** pose for an id wins (later duplicates ignored).
- **Destroyed symbols are kept in the index on purpose**, so a `Kill` impact marker can still land
  on the victim's last pose.

## What one outcome produces

For each `EngagementOutcomeRecord` (fields: `SequenceId`, `SimTime`, `SimTick`,
`ShooterTargetId`, `VictimTargetId`, `EngagementId`, `OutcomeCode`, `PkDraw`) the projection:

1. Resolves `shooter = ShooterTargetId.Value`, `victim = VictimTargetId.Value`. **Skips** the
   outcome if either id is blank or **either endpoint is missing** from the position index (both
   ends must be known to draw a line or a marker).
2. Computes `age = nowSimTime - outcome.SimTime`; **skips negative age** (a future-stamped outcome).
3. Emits a **fire line** while `age <= FireLineHoldSeconds` (`4.0`).
4. Emits an **impact marker** while `age <= ImpactHoldSeconds` (`6.0`).

> **Gotcha — the impact marker outlives the fire line.** `ImpactHoldSeconds` (6 s) is *longer* than
> `FireLineHoldSeconds` (4 s), so between `t+4s` and `t+6s` the map shows the impact marker with no
> line, then both clear. (The source XML comment calling the impact "slightly shorter than the
> line" is stale; the constants and `CombatVfxProjectionTests` are the source of truth.)

Both shapes carry a stable de-dupe key derived from the engagement id, so the renderer can update
in place across refreshes rather than flickering:

| Shape | Key | Anchor | Style class |
|-------|-----|--------|-------------|
| `CombatVfxFireLine` | `vfx-line:{EngagementId}` | shooter `(FromX,FromY)` → victim `(ToX,ToY)` | `map-combat-vfx-fireline` |
| `CombatVfxImpactMarker` | `vfx-impact:{EngagementId}` | victim `(X,Y)` | `ResolveImpactStyle(OutcomeCode)` (below) |

### Outcome → impact style

`ResolveImpactStyle` maps the `EngagementOutcomeCodes` string (from `ProjectAegis.Sim.Engage`) to a
USS suffix; any unrecognized code falls back to `--unknown`:

| `OutcomeCode` | Constant | Impact style class |
|---------------|----------|--------------------|
| `Hit` | `StyleImpactHit` | `map-combat-vfx-impact--hit` |
| `Kill` | `StyleImpactKill` | `map-combat-vfx-impact--kill` |
| `Miss` | `StyleImpactMiss` | `map-combat-vfx-impact--miss` |
| `Intercept` | `StyleImpactIntercept` | `map-combat-vfx-impact--intercept` |
| *(anything else / null)* | `StyleImpactUnknown` | `map-combat-vfx-impact--unknown` |

## Output DTOs

- `CombatVfxFireLine(Key, EngagementId, ShooterUnitId, TargetUnitId, FromX, FromY, ToX, ToY, SimTime, StyleClass)`
- `CombatVfxImpactMarker(Key, EngagementId, TargetUnitId, OutcomeCode, X, Y, SimTime, StyleClass)`
- `CombatVfxFrame(IReadOnlyList<CombatVfxFireLine> FireLines, IReadOnlyList<CombatVfxImpactMarker> ImpactMarkers)`

`CombatVfxFrame` overrides `Equals` / `GetHashCode` with **`SequenceEqual`** over both lists (value
equality, order-sensitive), so identical inputs at the same `nowSimTime` produce an equal frame —
this is what the purity tests assert. `CombatVfxFrame.Empty` is the canonical no-op frame (both
lists empty); `Project` returns it whenever nothing is live.

## The Unity consumer

[`MapCanvasTransientEffectsRenderer.Sync(frame)`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapCanvasTransientEffectsRenderer.cs)
draws the frame on a **separate UI Toolkit layer from the static range rings** (contract-tested).
It keys shapes by `Key` to add/prune/update in place, applies each shape's `StyleClass` as a USS
class (from
[`MapCombatVfx.uss`](../../unity/ProjectAegis/Assets/UI/MapPlaceholder/MapCombatVfx.uss):
`.map-combat-vfx-layer`, `.map-combat-vfx-fireline`, `.map-combat-vfx-impact[--hit|--kill|--miss|--intercept|--unknown]`),
and carries pixel-fallback tint colors for the impact classes.
[`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs)
change-detects `bridgeHost.LastCombatVfx` by reference (`_dirtyCombatVfxRef`) so it only re-syncs the
layer when the projected frame actually changes.

## Invariants

| Invariant | Why |
|-----------|-----|
| **No `UnityEngine` in the projection** | `CombatVfxProjection` + its DTOs are pure `Delegation/Projection/`; only the renderer/host reference Unity. Keeps the read-model headless-testable. |
| **Off the Tick hot path** | The producer runs in `DelegationBridgeHost` after `Bridge.Tick`, never inside `DelegationBridge.Tick`. |
| **`PkDraw` ignored / RNG-free** | The projection reads outcome geometry only and never touches `PkDraw`, so it cannot read or advance sim RNG (ADR-010). |
| **Not fingerprinted** | It is a derived view of already-logged `EngagementOutcomeRecord`s (which *are* fingerprinted); it writes nothing back, so the Baltic v2 replay hash `17144800277401907079` is untouched. |
| **Both endpoints required** | A fire line/marker is emitted only when shooter *and* victim poses are both in the index. |
| **Destroyed poses retained** | `BuildPositionIndex` keeps destroyed symbols so `Kill` markers land on the victim. |
| **Sim-time expiry, not wall-clock** | Shape lifetime is `nowSimTime - outcome.SimTime` against the two hold constants — deterministic and replay-safe. |

## Extending it

- **Add an outcome style** — add the `EngagementOutcomeCodes` case to `ResolveImpactStyle`, a
  `StyleImpact*` constant, and the matching `.map-combat-vfx-impact--<x>` rule in
  [`MapCombatVfx.uss`](../../unity/ProjectAegis/Assets/UI/MapPlaceholder/MapCombatVfx.uss).
  Unknown codes already render `--unknown`, so existing goldens stay valid.
- **Change a hold duration** — edit `FireLineHoldSeconds` / `ImpactHoldSeconds`. Keep the impact
  hold ≥ the fire-line hold if you want the marker to persist through the line.
- Nothing here needs a replay-golden re-bless — it is read-only presentation off the order log.

## Tests

| Suite | Assembly | Covers |
|-------|----------|--------|
| `CombatVfxProjectionTests` (8) | `ProjectAegis.Delegation.Tests` (NUnit) | Null/empty → `Empty`; line+marker at unit poses; skip when either endpoint unknown; sim-time expiry (line clears at `+4s`, marker at `+6s`); launch/aborted engagements emit nothing; Hit/Miss/Intercept/Kill style map; symbol-overload uses normalized xy; purity + `PkDraw`-independence. |
| `MapCanvasTransientEffectsRendererContractTests` (2) | `ProjectAegis.Delegation.UnityAdapter.Tests` (NUnit) | Transient VFX renders on a separate layer from the static rings; the host wires the projection *after* Tick, not inside the bridge. |

## See also

- [combat-domains-hot-tick-hud.md](combat-domains-hot-tick-hud.md) — the sibling presentation overlay off the order log.
- [engagement-pipeline.md](engagement-pipeline.md) — where `EngagementOutcomeRecord` (Hit/Miss/Kill/Intercept) is produced.
- [order-log-runtime.md](order-log-runtime.md) — the `DecisionLog` / `ChronologicalEntries()` source of truth.
- [c2-projection-layer.md](c2-projection-layer.md) — the read-only projection contract this follows.
- [c2-presentation-bridges.md](c2-presentation-bridges.md) — the `DelegationBridgeHost` per-tick refresh pattern.
- [globe-map-view-projection.md](globe-map-view-projection.md) — the map read-model that supplies `MapSymbolEntry` poses.
