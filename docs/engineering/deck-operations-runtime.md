# Deck operations runtime — air-ops & boat-ops launch/recovery FSMs

> **Scope.** This page documents the pure launch/recovery **state machines** in
> [`ProjectAegis.Sim/Logistics/`](../../src/ProjectAegis.Sim/Logistics/) that model an airframe or
> small craft moving through its deck cycle (LOG-08 for air, LOG-09…11 for boats; CMD-24 / CMD-25
> on the C2 side). These are the *implemented spine* of the larger carrier/amphibious deck-ops
> feature tracked as **GAP-10** in the [sim capability gap backlog](sim-capability-gap-backlog.md) —
> they resolve launch/abort/recovery ordering and readiness refusals, but do **not** model deck
> spotting, elevator/hangar geometry, or crew fatigue. The runtime is engine-agnostic
> (`ProjectAegis.Sim` + a thin `ProjectAegis.Delegation` driver); the Unity adapter only surfaces it.

The deck-operations runtime is a pair of **pure, deterministic finite-state machines** — one for
fixed-wing/rotary air ops, one for boats and small craft. They turn player/agent
launch-and-recover orders into ordered phase transitions with stable refusal codes, and they feed
the read-model projections behind the C2 Air Ops / Boat Ops panels. Unlike the sibling
[fuel](logistics-fuel-runtime.md), [comms](comms-degradation-runtime.md) and
[catalog damage](catalog-damage-readiness-runtime.md) hot-tick runtimes, **the FSMs do not write
order-log rows and are not part of the replay fingerprint** (see [Determinism & replay](#determinism--replay)).

---

## Where it runs

The FSMs are advanced once per executing tick, at the **end** of the tick, by
[`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs):

```
SimulationSession.RunExecutingTick(...)
  ├─ DelegationOrchestrator.Tick()      # decisions → gated, logged orders
  ├─ … engage / damage / logistics-fuel …
  ▼
  AdvanceLogisticsFsms(state)
    ├─ ProcessLogisticsOrders(Orchestrator.ExecutedOrders, state)   # order → FSM call
    │     LaunchAircraft      → AirOps.TryLaunch(unit)
    │     AbortLaunchAircraft → AirOps.TryAbort(unit)
    │     LaunchBoat          → BoatOps.TryLaunch(craft)
    │     RecoverBoat         → BoatOps.TryRecover(craft)
    │     AbortBoatLaunch     → BoatOps.TryAbort(craft)
    ├─ AirOps?.TickAll(1)      # advance every airframe timer by 1 tick
    └─ BoatOps?.TickAll(1)     # advance every craft timer by 1 tick
```

Key call-site facts:

- The `AirOpsStateMap` / `BoatOpsStateMap` ledgers on the session are **lazily created** (`AirOps`
  / `BoatOps` start `null`); a scenario that issues no deck orders does **zero** deck-ops work per
  tick. `EnsureAirOpsMap` / `EnsureBoatOpsMap` create a default map on first relevant order, and
  `EnsureAirUnit` / `EnsureBoatCraft` seed a missing unit before the FSM call.
- Deck-ops orders are ordinary [`OrderKind`](../../src/ProjectAegis.Delegation/Core/Order.cs)
  values (`LaunchAircraft`, `AbortLaunchAircraft`, `LaunchBoat`, `RecoverBoat`, `AbortBoatLaunch`).
  They are **append-only at the end of the enum** ("do not reorder above") because order-kind
  ordinals are frozen for replay stability.
- `TickAll(deltaTicks)` walks units/craft in **ordinal id order**, so the advance is deterministic
  regardless of dictionary insertion order.

---

## Common shape

Both FSMs share the same design:

- **Pure static engine** (`AirOpsFsm` / `BoatOpsFsm`) — no random, no wall-clock, no Unity. Every
  transition method takes an immutable state and returns a result whose `State` is *always* the
  post-attempt state (**unchanged when refused**):

  ```csharp
  public readonly record struct AirOpsFsmResult(bool Accepted, AirOpsUnitState State, string? RefusalReason);
  public readonly record struct BoatOpsFsmResult(bool Accepted, BoatOpsUnitState State, string? RefusalReason);
  ```

- **Immutable per-unit state record** (`AirOpsUnitState` / `BoatOpsUnitState`) with `with`-expression
  transitions and static factories (`OnGround`, `InMaintenance`, `Airborne`, `Stowed`, `Waterborne`).
- **Optional runtime ledger** (`AirOpsStateMap` / `BoatOpsStateMap`) — an ordinal dictionary plus
  thin `TryLaunch` / `TryAbort` / `TryRecover` wrappers and an ordinal-ordered `TickAll`.
- A **timer** (`TimeToReadyTicks`) drives the pipeline: `Tick` decrements it and, when it reaches
  `0`, advances to the next phase — draining multiple phases in a single call when intermediate
  durations are `0`. Durations are constructor/argument defaults (constants below), **not**
  scenario-policy fields.

---

## Air ops (`AirOpsFsm`, LOG-08 / CMD-24)

Phases ([`AirOpsPhase`](../../src/ProjectAegis.Sim/Logistics/AirOpsPhase.cs)):

```
OnGround → Prepping → Taxiing → TakingOff → Airborne
                 (abort ↺ back to OnGround)
Maintenance   Landing        # first-class phases; launch refused
```

Launch pipeline durations (default constants): `Prep 3`, `Taxi 2`, `Takeoff 1` ticks.

| Method | Effect |
|--------|--------|
| `RequestLaunch(state, prep=3, force=false)` | From `OnGround` **and** `ReadyForLaunch` → begins `Prepping`. `force` bypasses the readiness check (test/override). |
| `Tick(state, delta=1, taxi=2, takeoff=1)` | Advances the launch pipeline only; on reaching `Airborne`, clears `ReadyForLaunch`. |
| `AbortLaunch(state)` | While `Prepping`/`Taxiing`/`TakingOff` → returns to `OnGround`, `ReadyForLaunch = true` (re-launchable). |
| `RequestGroupLaunch(states, …)` | One result per unit, **ordered by unit id (ordinal)**. |

Refusal codes (stable strings; `AIR_NOT_READY` aligns with the
[abort-reason catalog](abort-reason-catalog.md) LOG-04):

| Code | When |
|------|------|
| `AIR_NOT_READY` | Launch from `OnGround` but `ReadyForLaunch == false` (and not forced). |
| `AIR_ALREADY_AIRBORNE` | Launch requested while `Airborne`/`Landing`. |
| `AIR_IN_MAINTENANCE` | Launch requested while `Maintenance`. |
| `AIR_LAUNCH_IN_PROGRESS` | Launch requested while already `Prepping`/`Taxiing`/`TakingOff`. |
| `AIR_ABORT_TOO_LATE` | Abort requested while `Airborne`/`Landing`. |
| `AIR_ABORT_NOT_ACTIVE` | Abort requested with no launch in progress. |

State: `AirOpsUnitState(UnitId, Phase, ReadyForLaunch, TimeToReadyTicks, HostId, CanAbortLaunch)`.

---

## Boat ops (`BoatOpsFsm`, LOG-09…11 / CMD-25)

Boats differ from air ops in three ways: a **full recovery pipeline**, an **asymmetric sea-state
gate** (recovery is stricter than launch), and **`Stranded`** as a first-class liability.

Phases ([`BoatOpsPhase`](../../src/ProjectAegis.Sim/Logistics/BoatOpsPhase.cs)):

```
Stowed → Prepping → Launching → Waterborne          # launch pipeline
Waterborne → Returning → Alongside → Recovering → Stowed   # recovery pipeline
Waterborne/Returning/Alongside ──(host departs)──▶ Stranded   # LOG-11
Maintenance                                          # launch refused
```

Durations (default constants): `Prep 2`, `Launch 2`, `Return 2`, `Alongside 1`, `Recover 2` ticks.

| Method | Effect |
|--------|--------|
| `RequestLaunch(state, seaState, prep=2)` | From `Stowed`, gated by embarked load and `seaState ≤ MaxLaunchSeaState` → `Prepping`. |
| `RequestRecover(state, seaState, return=2)` | From `Waterborne`, gated by `seaState ≤ MaxRecoverySeaState` → `Returning`. |
| `Tick(state, delta=1, …)` | Advances whichever pipeline (launch or recovery) the craft is in. |
| `AbortLaunch(state)` | While `Prepping`/`Launching` → back to `Stowed`. |
| `HostDepartWhileWaterborne(state)` | While off-ship (`Waterborne`/`Returning`/`Alongside`) → `Stranded` (LOG-11). |

**Sea-state gates.** Sea state is a Douglas scalar `0–9`
([`ScenarioSeaState`](../../src/ProjectAegis.Sim/Logistics/ScenarioSeaState.cs), always clamped).
Because recovery is stricter, a craft can legally **launch** at a sea state where it can no longer
**recover** (`MaxRecoverySeaState < seaState ≤ MaxLaunchSeaState`). The refusal text names the
exceeded limit, e.g. `BOAT_NOT_READY:maxRecoverySeaState=3`
(`FormatLaunchSeaStateRefusal` / `FormatRecoverySeaStateRefusal`).

**Embarked overload.** `IsEmbarkedOverloaded` is true when `PersonnelEmbarked > PersonnelCapacity`
or `CargoTons > CargoCapacityTons`; launch prep then refuses with `EMBARKED_OVERLOAD`.

Refusal codes: `BOAT_NOT_READY`, `EMBARKED_OVERLOAD`, `BOAT_ALREADY_WATERBORNE`,
`BOAT_IN_MAINTENANCE`, `BOAT_ABORT_TOO_LATE`, `BOAT_ABORT_NOT_ACTIVE`, `BOAT_LAUNCH_IN_PROGRESS`,
`BOAT_RECOVERY_IN_PROGRESS`, `BOAT_NOT_WATERBORNE`, `BOAT_STRANDED`,
`BOAT_HOST_DEPART_NOT_WATERBORNE`.

State: `BoatOpsUnitState(CraftId, HostId, Phase, MaxLaunchSeaState, MaxRecoverySeaState,
PersonnelEmbarked, PersonnelCapacity, CargoTons, CargoCapacityTons, TimeToReadyTicks,
CanAbortLaunch)`. The `Stowed` factory defaults to sea-state limits `4`/`3`, capacity `12`
personnel / `2.0` t cargo. `BoatOpsStateMap` additionally holds the scenario sea state
(`SeaStateDouglas` / `SetSeaState`) so its `TryLaunch` / `TryRecover` wrappers can apply the gate.

---

## Read-model projections (CMD-24 / CMD-25)

The C2 Air Ops / Boat Ops panels consume pure, no-mutation projections in
[`ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/) — part of the
[C2 projection layer](c2-projection-layer.md):

- [`AirOpsProjection`](../../src/ProjectAegis.Delegation/Projection/AirOpsProjection.cs) —
  `Project(...)` (Phase-A readiness tuples) and `ProjectLifecycle(states)` (Phase-N FSM rows) build
  ordinal-sorted `AirOpsEntry` rows with a status line, refusal code, phase label,
  `TimeToReadyTicks` ETA, and `CanLaunch` / `CanAbort` flags. `Aggregate(...)` folds a
  `READY r/t` header summary. Non-launchable rows carry `ResolveLaunchDisabledReason`.
- [`BoatOpsProjection`](../../src/ProjectAegis.Delegation/Projection/BoatOpsProjection.cs) — the boat
  equivalent, surfacing sea-state / overload / stranded status and recovery-blocked hints.

These projections are **read-only** and never call back into the FSM.

---

## Determinism & replay

- The FSMs are **pure**: given a state and inputs they produce the same result on every machine —
  no `Random`, no `DateTime`, no Unity. `TickAll` and `RequestGroupLaunch` iterate in **ordinal id
  order**, so multi-unit advances are order-stable.
- **Not fingerprinted.** Unlike fuel/comms/damage, `AdvanceLogisticsFsms` does **not** append to
  `DecisionLog` — the FSM phase ledgers live on the session and feed the read-model only. The orders
  that *drive* them (`LaunchAircraft`, etc.) are logged by the decision tick like any order, but the
  resulting phase transitions add no order-log rows and no fingerprint tokens. **The production
  Baltic v2 replay hash `17144800277401907079` is therefore unaffected by deck ops.**
- Because the maps are lazily created, a scenario without deck orders keeps identical behaviour to
  before this runtime existed.

---

## Extending the runtime

| Change | How | Replay impact |
|--------|-----|---------------|
| Tune a pipeline duration | Pass a non-default `prep`/`taxi`/`takeoff` (air) or `launch`/`return`/`alongside`/`recover` (boat) into the FSM or map ctor. | None on the fingerprint (FSMs are not logged); may change read-model ETA labels. |
| Add a deck phase | Add the `enum` value + a `Tick` pipeline branch + refusal cases; extend the projection status map. | None on the v2 hash unless you also add order-log emission. |
| Add a deck order kind | Append (only) to `OrderKind`, add a `ProcessLogisticsOrders` case + FSM entry point. | Order ordinals are frozen — **append at the end only**; existing goldens unaffected. |
| Make deck state replay-visible | Add an `OrderLogEntryKind` + `DecisionLog.Append*` + fingerprint case in `AdvanceLogisticsFsms`. | New fingerprint tokens → regenerate affected goldens. Do this deliberately. |

---

## Common pitfalls

| Symptom | Cause / fix |
|---------|-------------|
| `RequestLaunch` refuses with `AIR_NOT_READY` on a grounded airframe | `ReadyForLaunch` is `false`. Set it true (or pass `force: true` in tests/overrides). |
| Boat launches but later refuses recovery | Expected asymmetry: `seaState ≤ MaxLaunchSeaState` but `> MaxRecoverySeaState`. The refusal names the limit (`…:maxRecoverySeaState=N`). |
| Craft becomes `Stranded` unexpectedly | `HostDepartWhileWaterborne` fired while the craft was off-ship. `Stranded` is terminal for launch/recover/abort. |
| A unit advanced more than one phase in a single `TickAll(1)` | Correct when an intermediate phase has a `0`-tick duration — the timer drains through zero-length phases in one call. |
| Deck-ops changes moved a replay golden hash | They shouldn't — the FSMs aren't fingerprinted. A moved hash means you added order-log emission; regenerate that golden intentionally. |
| Map is `null` at runtime | Maps are lazily created on the first relevant order. Seed one explicitly (`session.AirOps = new AirOpsStateMap(...)`) if you need pre-populated state. |

---

## See also

| Topic | Where |
|-------|-------|
| The tick that advances the FSMs | [simulation-session-orchestration.md](simulation-session-orchestration.md) |
| Sibling per-tick logistics (fuel bands / burn ledger) | [logistics-fuel-runtime.md](logistics-fuel-runtime.md) |
| Read-model panels that surface deck state | [c2-projection-layer.md](c2-projection-layer.md) |
| Stable refusal / abort code catalog | [abort-reason-catalog.md](abort-reason-catalog.md) |
| Order-log / fingerprint / golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| The larger unimplemented deck-ops scope | [sim-capability-gap-backlog.md](sim-capability-gap-backlog.md) (GAP-10) |

## Tests

| Area | File | Count |
|------|------|-------|
| Air-ops FSM (phases, abort windows, refusals, group launch) | [`AirOpsFsmTests.cs`](../../src/ProjectAegis.Sim.Tests/Logistics/AirOpsFsmTests.cs) | 12 |
| Boat-ops FSM (launch/recovery, sea-state gates, overload, strand) | [`BoatOpsFsmTests.cs`](../../src/ProjectAegis.Sim.Tests/Logistics/BoatOpsFsmTests.cs) | 14 |
| Air-ops projection rows | [`AirOpsProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/Projection/AirOpsProjectionTests.cs) | 14 |
| Boat-ops projection rows | [`BoatOpsProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/Projection/BoatOpsProjectionTests.cs) | 13 |
