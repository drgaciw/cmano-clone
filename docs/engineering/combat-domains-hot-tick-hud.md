# Combat-domains hot-tick HUD — message-log activity → Air/Surface/Subsurface/Land/Mine/Facility

> **Presentation boundary.** This is a **read-model / HUD** seam: the projection is a *client* of the
> (fingerprinted) order log, **not** sim authority (ADR-010 §2–3,
> [`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md);
> adapter boundary [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)).
> The Approved USS wire is authored under **ASSET-021** (`design/assets/specs/baltic-patrol-assets.md`).
> Do **not** treat this HUD as a sim input, and never cite Git ADR-018 here (that is sensor
> side-picture / datalink).

The **combat-domains hot-tick HUD** (ASSET-021, S104/S105 Epic A) turns the per-tick player
message log into a small always-on overlay that lights up the six warfare domains — **Air, Surface,
Subsurface, Land, Mine, Facility** — as `IDLE` / `ENGAGED` / `DEGRADED`. It is a **read-model**
derived from the order-log-backed message log; it is **not** authoritative sim state and is **not**
fingerprinted. Four engine-agnostic pieces plus one Unity host:

1. [`CombatDomainsHotTickTracker`](../../src/ProjectAegis.Delegation/Projection/CombatDomainsHotTickTracker.cs)
   — the stateful per-tick domain→engagement map (supports the `DEGRADED` state).
2. [`CombatDomainActivityTags`](../../src/ProjectAegis.Delegation/Projection/CombatDomainActivityTags.cs)
   — the pure "active domain tags" mapper (a stateless `ENGAGED`-only subset).
3. [`CombatDomainsHotTickPanelBinder`](../../src/ProjectAegis.Delegation/Projection/CombatDomainsHotTickPanelBinder.cs)
   — maps a domain map / tracker into display rows (labels + USS state-class suffixes).
4. [`CombatDomainsHotTickHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/CombatDomainsHotTickHost.cs)
   — the UI Toolkit MonoBehaviour that rebuilds the tracker off the bridge each frame.

> **Scope.** This is the *combat-domain activity HUD* — how message-log categories become a per-domain
> engagement light. It is distinct from the engage-time [combat-domain
> validators](../architecture/adr-009-combat-domain-validators.md) (which gate *whether* a shot is
> legal) and from the facility HP capacity labels in
> [facility-capacity-projection.md](facility-capacity-projection.md). This HUD only *reflects* activity;
> it never authorizes anything.

---

## Domain keys & the engagement enum

`CombatDomainsHotTickPanelBinder.DefaultDomainKeys` is the canonical, ordered six-domain set —
`{ "Air", "Surface", "Subsurface", "Land", "Mine", "Facility" }`. Both the tracker and the binder
key off this list with `StringComparer.OrdinalIgnoreCase`; **unknown keys are ignored** and any
default key not touched stays `Idle`.

`CombatDomainHudEngagement` has a **counter-intuitive ordinal**:

| Member | Enum ordinal | Promotion rank |
|--------|:------------:|:--------------:|
| `Idle` | `0` | `0` |
| `Engaged` | `1` | `2` |
| `Degraded` | `2` | `1` |

> ⚠️ **The promotion order is not the enum ordinal.** Promotion uses the private `Rank` map
> (`Engaged > Degraded > Idle`), so within one tick `Engaged` always wins over `Degraded`, which wins
> over `Idle`. If you compared the raw enum values you would (wrongly) rank `Degraded` highest.
> Preserve `Rank` — do not "simplify" it to an enum comparison.

---

## The tracker — `CombatDomainsHotTickTracker`

A `sealed class` holding a domain→engagement dictionary, seeded so every default domain is present as
`Idle`. Pure projection DTO — no sim hot-path, no `UnityEngine` dependency.

- **`ObserveActiveDomainTags(int simTick, IEnumerable<string> tags)`** — stamps `LastSimTick`, then
  promotes each non-blank tag to `Engaged`. Null enumerable throws `ArgumentNullException`.
- **`ObserveMessageLog(int simTick, IReadOnlyList<MessageLogLine> lines)`** — stamps `LastSimTick`,
  then folds each line's `Category` through the category map below (this is the only path that can
  raise `DEGRADED`). Null list throws.
- **`SetEngagement(string domainKey, CombatDomainHudEngagement)`** — direct promote (blank key is a
  no-op); still subject to the `Rank` promotion rule.
- **`SnapshotEngagements()`** — returns a fresh `OrdinalIgnoreCase` copy containing **exactly** the six
  default keys (missing entries reported as `Idle`).
- **`Clear()`** — resets `LastSimTick = 0` and every domain back to `Idle`.

### Message-log category → domain map

`ObserveMessageLog` upper-cases and trims each [`MessageLogLine.Category`](../../src/ProjectAegis.Delegation/Projection/MessageLogLine.cs)
and applies:

| Message-log category | Domain | Engagement |
|----------------------|--------|------------|
| `KILL_CONFIRMED`, `HIT` | Surface | `Engaged` |
| `COMMS`, `WEAPON_LAUNCH` | Air | `Engaged` |
| `CONTACT`, `CONTACT_CHANGE` | Subsurface | `Engaged` |
| `POLICY_DENIAL` | Land | **`Degraded`** |
| `MAGAZINE` | Facility | `Engaged` |
| `MISSION`, `MISSION_TRANSITION` | Mine | `Engaged` |

Unrecognized categories are dropped. The mapping is a **display heuristic** (there is no literal
"land" or "mine" content in Baltic yet — those domains borrow `MISSION` / `POLICY` activity so the
HUD row is not permanently dark); it is not a doctrinal domain classification.

---

## The tag mapper — `CombatDomainActivityTags`

`CombatDomainActivityTags.FromMessageLog(lines)` is the **stateless** cousin: it returns a
de-duplicated `IReadOnlyList<string>` of the domains that are *active* this tick, using the same
category map **minus `POLICY_DENIAL`**. Because a tag can only mean "active" (→ `Engaged`), the
`Degraded`-only `POLICY_DENIAL → Land` case is intentionally omitted here and is reachable **only**
through the tracker's `ObserveMessageLog`. Empty / null input returns `Array.Empty<string>()`.

This is what [`DelegationBridgeHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs)
exposes as `LastCombatDomainActivityTags` (see [host wiring](#host-wiring--combatdomainshottickhost--delegationbridgehost)).

---

## The binder — `CombatDomainsHotTickPanelBinder`

A `static` class that turns domain state into a `CombatDomainsHotTickPanelState` (an ordered list of
`CombatDomainDisplayRow(DomainKey, DisplayName, StateLabel, StateCssClass)`). `DisplayName` is the
key upper-cased; `ToRow` maps engagement → label + USS state-class suffix:

| Engagement | `StateLabel` | `StateCssClass` |
|------------|--------------|-----------------|
| `Engaged` | `ENGAGED` | `combat-domains-hot-tick__state--engaged` |
| `Degraded` | `DEGRADED` | `combat-domains-hot-tick__state--degraded` |
| `Idle` | `IDLE` | `combat-domains-hot-tick__state` |

Entry points (all emit exactly the six default rows in order):

- **`BindIdle()`** — every domain `IDLE`.
- **`BindFromDomainActivity(map)`** — reads a domain→engagement map (`OrdinalIgnoreCase` match; missing
  defaults stay `Idle`; unknown keys ignored). Null map throws.
- **`BindFromActiveDomainTags(tags)`** — promotes each tag to `Engaged`, then delegates to
  `BindFromDomainActivity`.
- **`BindFromTracker(tracker)`** — binds `tracker.SnapshotEngagements()` (the preferred S105 path,
  because it preserves `DEGRADED`). Null tracker throws.

---

## Host wiring — `CombatDomainsHotTickHost` + `DelegationBridgeHost`

`CombatDomainsHotTickHost` is a `[RequireComponent(typeof(UIDocument))]` MonoBehaviour (guarded by
`#if UNITY_5_3_OR_NEWER`). Each `LateUpdate` (when `showPanel`) it:

1. Rebuilds a private tracker from scratch — `_tracker.Clear()`, then `ObserveMessageLog(...)` and
   `ObserveActiveDomainTags(...)` from the bridge host's `LastMessageLog` / `LastCombatDomainActivityTags`;
2. Binds via `BindFromTracker` and writes each row's label + state class onto the mapped UXML
   `Label`s (`domain-air-state`, `domain-surface-state`, `domain-sub-state`, `domain-land-state`,
   `domain-mine-state`, `domain-facility-state` under `combat-domains-hot-tick-root`).

The **tick stamp is the message-log line count**, not a wall-clock — `var tick = bridgeHost.LastMessageLog?.Count ?? 0`.
This keeps the HUD **headless-safe and deterministic**; `LastSimTick` is diagnostic only and never
drives sim behavior. `ApplyPanelState` / `ApplyTracker` are provided for tests / offline preview.

Crucially, the bridge feed is computed **after** the tick, off the sim hot path. In
[`DelegationBridgeHost.RunTick`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs):

```csharp
var result = Bridge.Tick(snapshot, groupSink);        // sim tick (fingerprinted)
// … then, off the DelegationBridge.Tick hot path:
LastMessageLog = MessageLogBridge.ProjectFrom(Bridge.Orchestrator.DecisionLog);
LastCombatDomainActivityTags = CombatDomainActivityTags.FromMessageLog(LastMessageLog);
```

So `CombatDomainsHotTickHost` never touches `DelegationBridge.Tick` and cannot perturb the Baltic v2
replay hash.

---

## USS / Approved wire — ASSET-021

The overlay style is an **Approved (Path A) asset**. The source of truth is
`production/assets/baltic/CombatDomainsHotTick.uss` and the Unity copy at
`unity/ProjectAegis/Assets/UI/CombatDomains/CombatDomainsHotTick.uss` must mirror it — both are
resolved by [`ApprovedC2AssetPaths`](../../src/ProjectAegis.Delegation/Projection/ApprovedC2AssetPaths.cs)
(`Asset021CombatDomainsUss` / `UnityCombatDomainsUss`) and pinned by
[`CombatDomainsAsset021WireTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/CombatDomainsAsset021WireTests.cs).

- Layout: bottom-right absolute panel (`min-width: 220px`), one `__row` per domain (`__domain` label +
  `__state` value).
- Tokens (via `AegisTokens.uss`): idle `--comms-nominal`, `ENGAGED` uses `--log-kill` (KILL tint —
  **no arcade VFX**), `DEGRADED` uses `--comms-degraded`.
- The class names (`.combat-domains-hot-tick`, `__title`, `__row`, `__domain`, `__state`,
  `__state--engaged`, `__state--degraded`) are asserted **byte-identical** across the production and
  Unity copies by the wire test.

---

## Determinism & invariants

- **Read-model, off the fingerprint.** The HUD is derived *from* the order-log-backed message log; it
  never writes the log and is not part of the world-state hash or order-log fingerprint. Changing the
  category map or a label cannot move a replay golden.
- **Off the `DelegationBridge.Tick` hot path.** The bridge feed (`LastMessageLog` /
  `LastCombatDomainActivityTags`) is computed after `Bridge.Tick` returns; the host reads it in
  `LateUpdate`. No sim coupling.
- **Deterministic, no wall-clock, no RNG.** Both helpers read only their arguments; the host's tick
  stamp is the message-log count. Same log ⇒ same panel.
- **Promotion by `Rank`, not enum ordinal.** `Engaged > Degraded > Idle`; the enum ordinal is not the
  precedence (see the table above).
- **`Degraded` is tracker-only.** The stateless tag mapper omits `POLICY_DENIAL`; only
  `CombatDomainsHotTickTracker.ObserveMessageLog` can raise `DEGRADED`. Prefer `BindFromTracker` when a
  degraded state matters.
- **Fixed six-domain shape.** `SnapshotEngagements()` and every binder entry always emit exactly the
  six `DefaultDomainKeys`, in order — never null, never a partial set.
- **Approved-USS parity.** Production and Unity USS copies must stay class-identical (wire test).

---

## Tests (pins)

| Test | Covers |
|------|--------|
| [`Projection/CombatDomainsHotTickTrackerTests`](../../src/ProjectAegis.Delegation.Tests/Projection/CombatDomainsHotTickTrackerTests.cs) | Tag promotion without sim coupling, the full category map (incl. `DEGRADED` policy), `Engaged`-wins-over-`Degraded`, and `Clear` → idle. |
| [`Projection/CombatDomainsHotTickPanelBinderTests`](../../src/ProjectAegis.Delegation.Tests/Projection/CombatDomainsHotTickPanelBinderTests.cs) | `BindIdle` default set, engaged/degraded label + CSS mapping, and tag-promotion binding. |
| [`Bridge/DelegationBridgeHostDomainTagsTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeHostDomainTagsTests.cs) | `FromMessageLog` → tags → binder engaged, and tracker `DEGRADED` Land + `ENGAGED` Surface through `BindFromTracker`. |
| [`Bridge/CombatDomainsAsset021WireTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/CombatDomainsAsset021WireTests.cs) | ASSET-021 Approved USS + UXML wire: class names, tokenized engaged/degraded states, and production↔Unity parity. |
| [`Projection/UnityPluginEpicATypesTests`](../../src/ProjectAegis.Delegation.Tests/Projection/UnityPluginEpicATypesTests.cs) | Type-surface registry pins `CombatDomainsHotTickPanelState` / `CombatDomainsHotTickPanelBinder`. |

The projection/binder fixtures are NUnit under `ProjectAegis.Delegation.Tests/Projection`; the wire and
domain-tag fixtures are NUnit under `ProjectAegis.Delegation.UnityAdapter.Tests/Bridge`. All are part of
the ≥1638-test baseline.
