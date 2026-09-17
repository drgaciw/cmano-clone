# Combat Domains Hot-Tick HUD read-model

> **Scope.** The pure, headless read-model behind the **ASSET-021 "Combat Domains" hot-tick
> overlay** (S105 A1) — the small bottom-right HUD panel that shows, at a glance, which of the
> six warfare domains (Air / Surface / Subsurface / Land / Mine / Facility) are currently
> **ENGAGED**, **DEGRADED**, or **IDLE**. It lives in
> [`ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/) and
> follows the read-only projection contract of ADR-010 §2–3 / ADR-007: it is derived from the
> order-log message stream, has **no `UnityEngine` dependency**, does **no mutation**, and never
> touches the sim hot path or the replay fingerprint. The Unity host that binds it is
> [`CombatDomainsHotTickHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/CombatDomainsHotTickHost.cs).
>
> **Not to be confused with** the *engage-time* `CombatDomainValidator` gate documented in
> [engagement-pipeline.md](engagement-pipeline.md). That is a `ProjectAegis.Sim/Engage/` combat
> *authorization* check inside the 22-step kill-chain; this page is a *presentation* activity
> indicator built off the message log. They share the word "domain" and nothing else.

---

## Where it sits

The HUD is the tail end of a one-way, per-tick presentation fold — no state flows back into the sim:

```
DecisionLog (order log)
  └─ MessageLogBridge.ProjectFrom(...)  ──►  DelegationBridgeHost.LastMessageLog  (IReadOnlyList<MessageLogLine>)
                                                │
                    ┌───────────────────────────┴───────────────────────────┐
                    ▼                                                         ▼
   CombatDomainActivityTags.FromMessageLog(log)              CombatDomainsHotTickTracker.ObserveMessageLog(tick, log)
     → LastCombatDomainActivityTags (Engaged-only tags)        + .ObserveActiveDomainTags(tick, tags)
                    │                                                         │
                    └───────────────────────────┬───────────────────────────┘
                                                 ▼
                       CombatDomainsHotTickPanelBinder.BindFromTracker(tracker)
                                                 ▼
                       CombatDomainsHotTickPanelState (6 CombatDomainDisplayRow)
                                                 ▼
                       CombatDomainsHotTickHost  →  UXML <Label> per domain (text + USS state class)
```

The two producers wired in [`DelegationBridgeHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs)
(off the `DelegationBridge.Tick` hot path, alongside the other C2 feed refreshes):

```csharp
LastMessageLog = MessageLogBridge.ProjectFrom(Bridge.Orchestrator.DecisionLog);
LastCombatDomainActivityTags = CombatDomainActivityTags.FromMessageLog(LastMessageLog);
```

## The six domains and the engagement enum

`CombatDomainsHotTickPanelBinder.DefaultDomainKeys` is the fixed, ordered set — **no Space / Cyber**:

```
Air, Surface, Subsurface, Land, Mine, Facility
```

`CombatDomainHudEngagement` has three members, but note the **enum value is not the priority order**:

| Member | Enum value | Promotion `Rank` | Display label | USS state class suffix |
|--------|-----------|------------------|---------------|------------------------|
| `Idle` | `0` | `0` | `IDLE` | `combat-domains-hot-tick__state` |
| `Engaged` | `1` | `2` | `ENGAGED` | `combat-domains-hot-tick__state--engaged` |
| `Degraded` | `2` | `1` | `DEGRADED` | `combat-domains-hot-tick__state--degraded` |

> **Gotcha — promotion is by `Rank`, not enum ordinal.** `Engaged` (rank 2) beats `Degraded`
> (rank 1) beats `Idle` (rank 0), even though `Degraded` has the *higher* enum value. Within one
> refresh a domain only ever moves *up* — a later `Engaged` observation overrides an earlier
> `Degraded`, but not vice-versa (`CombatDomainsHotTickTracker.Promote`).

## Category → domain mapping

Both producers translate `MessageLogLine.Category` strings into domains (matched
`ToUpperInvariant`). The **tracker** is the full map and the only path that can emit `Degraded`;
the **tag helper** is an Engaged-only subset used for the host's activity-tag input.

`CombatDomainsHotTickTracker.ObserveMessageLog` (`ApplyCategory`):

| Message-log category | Domain | Engagement |
|----------------------|--------|-----------|
| `KILL_CONFIRMED`, `HIT` | Surface | Engaged |
| `COMMS`, `WEAPON_LAUNCH` | Air | Engaged |
| `CONTACT`, `CONTACT_CHANGE` | Subsurface | Engaged |
| `POLICY_DENIAL` | Land | **Degraded** |
| `MAGAZINE` | Facility | Engaged |
| `MISSION`, `MISSION_TRANSITION` | Mine | Engaged |

`CombatDomainActivityTags.FromMessageLog` — same map **minus `POLICY_DENIAL`** (a denial is not a
simple "active" tag), returns an ordinal-ignore-case de-duped set, and yields
`Array.Empty<string>()` when nothing matches. Because it never emits Land, the only way `Land`
reaches `DEGRADED` is through the tracker's `ObserveMessageLog`.

## The binder (`CombatDomainsHotTickPanelBinder`, `static`)

Turns a domain→engagement map into display rows. Every entry point emits **exactly the six
default domains** in order; unknown keys are ignored and missing defaults stay `Idle`.

| Method | Input | Behaviour |
|--------|-------|-----------|
| `BindIdle()` | — | All six domains `IDLE`. |
| `BindFromDomainActivity(map)` | `IReadOnlyDictionary<string, CombatDomainHudEngagement>` | Per default key, case-insensitive lookup; unknown keys ignored, missing keys `Idle`. Throws `ArgumentNullException` on null. |
| `BindFromActiveDomainTags(tags)` | `IEnumerable<string>` | Promotes every non-blank tag to `Engaged`, then binds. |
| `BindFromTracker(tracker)` | `CombatDomainsHotTickTracker` | Binds `tracker.SnapshotEngagements()`. Primary host path (S105 A1). |

Each row is a `CombatDomainDisplayRow(DomainKey, DisplayName, StateLabel, StateCssClass)` where
`DisplayName = DomainKey.ToUpperInvariant()`. The output DTO is
`CombatDomainsHotTickPanelState(IReadOnlyList<CombatDomainDisplayRow> Rows)`.

## The tracker (`CombatDomainsHotTickTracker`)

A tiny mutable accumulator (case-insensitive domain map, seeded all-`Idle`) that the host rebuilds
each refresh:

- `SetEngagement(domainKey, engagement)` — direct set (subject to the same `Rank` promotion rule).
- `ObserveActiveDomainTags(simTick, tags)` — promotes each tag to `Engaged`; sets `LastSimTick`.
- `ObserveMessageLog(simTick, lines)` — applies the full category map above; sets `LastSimTick`.
- `SnapshotEngagements()` — a fresh dictionary containing **all six** default keys (missing → `Idle`).
- `Clear()` — resets every domain to `Idle` and `LastSimTick` to `0`.

`ObserveActiveDomainTags` / `ObserveMessageLog` throw `ArgumentNullException` on a null sequence.
Keys are canonicalized to one of the six defaults; anything else is silently dropped.

## The Unity host (`CombatDomainsHotTickHost`)

A `MonoBehaviour` (`[RequireComponent(typeof(UIDocument))]`, `#if UNITY_5_3_OR_NEWER`) that binds
the panel each `LateUpdate` while `showPanel` is set. It maps the six UXML labels by name —
`domain-air-state`, `domain-surface-state`, `domain-sub-state`, `domain-land-state`,
`domain-mine-state`, `domain-facility-state` under root `combat-domains-hot-tick-root` — and in
`RefreshFromBridge`:

```csharp
_tracker.Clear();
var tick = bridgeHost.LastMessageLog?.Count ?? 0;          // stable, headless-safe stamp — not wall-clock
_tracker.ObserveMessageLog(tick, bridgeHost.LastMessageLog);
_tracker.ObserveActiveDomainTags(tick, bridgeHost.LastCombatDomainActivityTags);
_panelState = CombatDomainsHotTickPanelBinder.BindFromTracker(_tracker);
```

`ApplyPanelState(state)` and `ApplyTracker(tracker)` are public for tests / offline preview.

**Styling (ASSET-021, Approved).** The USS source of truth is
[`production/assets/baltic/CombatDomainsHotTick.uss`](../../production/assets/baltic/CombatDomainsHotTick.uss);
the Unity copy imports `AegisTokens.uss` and tints `--engaged` with `var(--log-kill)` (the kill
tint — *no arcade VFX*) and `--degraded` with `var(--comms-degraded)`. The
`CombatDomainsAsset021WireTests` pin the two files to share the same class names.

## Invariants

| Invariant | Why |
|-----------|-----|
| **No `UnityEngine` in the projection types** | Tracker / binder / tags are pure `Delegation/Projection/`; only the host references Unity. Keeps the read-model headless-testable. |
| **Off the Tick hot path** | Producers run in `DelegationBridgeHost` after `Bridge.Tick`, never inside `DelegationBridge.Tick`. |
| **Not fingerprinted / not in `DecisionLog`** | Pure derived view; it reads the order-log message stream but writes nothing back, so the Baltic v2 replay hash `17144800277401907079` is untouched. |
| **Promotion by `Rank`, not enum value** | `Engaged` > `Degraded` > `Idle`; a domain only moves up within a refresh. |
| **Always six domains, in order** | Every binder path emits `DefaultDomainKeys`; hosts can bind labels 1:1 without null checks. |
| **Unknown keys dropped, `Degraded` is tracker-only** | Only the six canonical domains render; `POLICY_DENIAL → Land Degraded` never arrives via the tag path. |
| **Tick stamp is message-log depth** | The host stamps `LastSimTick` with `LastMessageLog.Count` — deterministic and headless-safe, never wall-clock. |

## Extending it

- **Add a domain** — append the key to `CombatDomainsHotTickPanelBinder.DefaultDomainKeys`, add a
  matching `<Label name="domain-<x>-state" …>` row to
  [`CombatDomainsHotTick.uxml`](../../unity/ProjectAegis/Assets/UI/CombatDomains/CombatDomainsHotTick.uxml),
  and map it in `CombatDomainsHotTickHost.TryWireElements`. Existing maps stay valid (missing keys
  render `Idle`).
- **Map a new message category** — add the `case` to `CombatDomainsHotTickTracker.ApplyCategory`
  (and to `CombatDomainActivityTags.FromMessageLog` if it should also count as an active tag). Use
  `Degraded` only via the tracker.
- Nothing here needs a replay-golden re-bless — it is read-only presentation.

## Tests

| Suite | Assembly | Covers |
|-------|----------|--------|
| `CombatDomainsHotTickTrackerTests` (5) | `ProjectAegis.Delegation.Tests` (NUnit) | Tag promotion without sim coupling, the full category map incl. `POLICY_DENIAL → Degraded Land`, `Engaged`-beats-`Degraded`, `Clear` → all-`Idle`. |
| `CombatDomainsHotTickPanelBinderTests` (3) | `ProjectAegis.Delegation.Tests` (NUnit) | `BindIdle`, engaged/degraded rows + CSS suffixes, tag promotion. |
| `DelegationBridgeHostDomainTagsTests` (4) | `ProjectAegis.Delegation.UnityAdapter.Tests` (NUnit) | Host-path derivation: `FromMessageLog` → binder engaged, tracker `Degraded Land` + `Engaged Surface`. |
| `CombatDomainsAsset021WireTests` (4) | `ProjectAegis.Delegation.UnityAdapter.Tests` (NUnit) | Approved production USS + Unity USS/UXML share the ASSET-021 class names and token wiring. |

## See also

- [c2-projection-layer.md](c2-projection-layer.md) — the read-only projection contract this follows.
- [c2-presentation-bridges.md](c2-presentation-bridges.md) — the Unity-host feed pattern (`DelegationBridgeHost` per-tick refresh).
- [engagement-pipeline.md](engagement-pipeline.md) — the *engage-time* `CombatDomainValidator` gate (distinct subsystem).
- [order-log-runtime.md](order-log-runtime.md) — the `DecisionLog` / message-log source of truth.
