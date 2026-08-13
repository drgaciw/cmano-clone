# Logistics operations runtime — ordnance bands, air-ops & boat-ops FSMs

> **Siblings.** Fuel (Joker/Bingo burn ledger) lives in
> [logistics-fuel-runtime.md](logistics-fuel-runtime.md). The gauntlet-testing view of the four
> aviation-doctrine labels (Joker/Bingo/Shotgun/Winchester) lives in
> [gauntlet-logistics-variables.md](gauntlet-logistics-variables.md). The three *engage* gates
> that read these states (`BingoFuel` / `ShotgunOrdnance` / `WinchesterOrdnance`) are documented
> as gate-chain steps in [engagement-pipeline.md](engagement-pipeline.md#the-gate-chain-exact-order).

This page is the runtime deep-dive for the **non-fuel** logistics subsystems (doc 16 LOG-08…11 /
CMD-24 / CMD-25):

1. **Ordnance readiness bands** — the magazine-side counterpart to the fuel bands
   (`NOMINAL → SHOTGUN → WINCHESTER`), the fingerprinted `OrdnanceStateChange` order-log row, and
   the three engage gates that consume the bands.
2. **Air-ops FSM (LOG-08)** — the airframe launch pipeline
   (`OnGround → Prepping → Taxiing → TakingOff → Airborne`) with abort windows and stable refusal
   codes.
3. **Boat-ops FSM (LOG-09…11)** — the small-craft launch **and** recovery pipelines with a
   sea-state gate (recovery stricter than launch), embarked-load gate, and the first-class
   `Stranded` liability when the host departs.

All three are engine-agnostic (`ProjectAegis.Sim/Logistics/` +
`ProjectAegis.Delegation/Orchestration|Projection`) — **pure, deterministic, no `SeededRng`, no
wall-clock, no Unity**. The Unity adapter only drives and renders them.

> **Determinism split you must know up front.** Ordnance band changes emit a **fingerprinted**
> order-log row (they move replay hashes). The air-ops and boat-ops FSMs are a **host-side ledger**
> surfaced through read-model projections — they emit **no** order-log entry kind and do **not**
> contribute to the replay fingerprint. (The `LaunchAircraft` / `LaunchBoat` *orders* themselves
> still flow through the normal decision/order log.)

---

## Where it runs

The ordnance emission and the FSM advance happen inside `SimulationSession.Tick`, *after* the
engagement resolver has run for the tick:

```
SimulationSession.Tick(state)
  ├─ … decision + engagement resolve (MvpEngagementResolver) …
  ├─ LogEngagementResults(...)                 # per launched shot:
  │     └─ MaybeEmitOrdnanceStateChange(...)    ◄── ordnance band row (fingerprinted, this doc)
  ├─ ApplyCatalogDamageHotTick(...)
  └─ AdvanceLogisticsFsms(state)               ◄── air/boat-ops FSMs (host-side, this doc)
        ├─ ProcessLogisticsOrders(ExecutedOrders)   # Launch/Recover/Abort orders → map mutations
        ├─ AirOps?.TickAll(1)                        # advance every airframe timer
        └─ BoatOps?.TickAll(1)                       # advance every craft timer
```

- Ordnance rows are emitted only when a magazine ledger is wired (`session.Magazines != null`) and
  only for a **launched** shot (see [Ordnance emission](#ordnance-emission-maybeemitordnancestatechange)).
- The FSM state maps are **lazily created** on first relevant order
  (`AirOps ??= new AirOpsStateMap()`), so a scenario that never issues a launch order does zero
  FSM work per tick.

---

## 1 · Ordnance readiness bands

### The band resolver (`OrdnanceStateBands`)

[`OrdnanceStateBands.Resolve(roundsRemaining, shotgunRoundsThreshold)`](../../src/ProjectAegis.Sim/Logistics/OrdnanceStateBands.cs)
is the whole band law — the literal strings `NOMINAL` / `SHOTGUN` / `WINCHESTER`:

| Condition | Band |
|-----------|------|
| `roundsRemaining ≤ 0` | `WINCHESTER` (out of weapons) |
| `shotgunRoundsThreshold > 0` **and** `roundsRemaining ≤ shotgunRoundsThreshold` | `SHOTGUN` (pre-briefed minimum / defensive residual) |
| otherwise | `NOMINAL` |

A `shotgunRoundsThreshold` of `0` **disables** the SHOTGUN band (only WINCHESTER at empty).

### The three engage gates

These live on the engagement gate chain and are documented in full there
([engagement-pipeline.md](engagement-pipeline.md#the-gate-chain-exact-order)); summarized here for
the logistics view:

| Gate | Kind | Rule | Abort reason |
|------|------|------|--------------|
| [`LogisticsBingoEngageGate`](../../src/ProjectAegis.Sim/Engage/LogisticsBingoEngageGate.cs) | hard | shooter **fuel** band is Bingo (`EngageContext.LogisticsBingoBlocked`, primed by the fuel tracker) | `BingoFuel` |
| [`LogisticsShotgunEngageGate`](../../src/ProjectAegis.Sim/Engage/LogisticsShotgunEngageGate.cs) | **soft** | ordnance band is `SHOTGUN` **and** `SalvoSize > 1` — single-round residual/defensive fire is still allowed; threshold `0` disables the gate | `ShotgunOrdnance` |
| [`LogisticsWinchesterEngageGate`](../../src/ProjectAegis.Sim/Engage/LogisticsWinchesterEngageGate.cs) | hard | `roundsRemaining ≤ 0` | `WinchesterOrdnance` |

> **`liveRounds` authority.** Both ordnance gates read `liveRounds` = the **tracked magazine ledger**
> rounds when the mount was seeded, else a fallback to `EngageContext.RoundsRemaining` (so an
> unseeded mount is not treated as empty). Winchester is deliberately placed *after* the
> doctrine/sensor/FC gates so a tracked-empty magazine surfaces `WinchesterOrdnance` rather than a
> pre-launch magazine abort — see the engagement-pipeline gate-chain note.

### Ordnance emission (`MaybeEmitOrdnanceStateChange`)

After a **launched** shot, [`SimulationSession.MaybeEmitOrdnanceStateChange`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
turns the post-fire magazine count into an order-log row — the magazine counterpart to the fuel
tracker's band-crossing emit:

1. Requires a wired `MagazineLedger` (`Magazines == null` ⇒ no row, ever).
2. `remaining = Magazines.GetRounds(shooter, mount)`; `threshold =
   ScenarioPolicy?.EngageDefaults?.ShotgunRoundsThreshold ?? 1`.
3. `band = OrdnanceStateBands.Resolve(remaining, threshold)`.
4. Per-unit last-band memory (`_lastOrdnanceBand`, `Ordinal`):
   - **first sight** of a unit emits a row only if the band is *not* `NOMINAL` (a unit that starts
     full produces no spurious `NOMINAL→NOMINAL` row);
   - otherwise a row is emitted only when the band **differs** from the remembered one.
5. Emits an [`OrdnanceStateChangeRecord`](../../src/ProjectAegis.Delegation/Decision/OrdnanceStateChangeRecord.cs)
   and updates the remembered band.

### Order-log record & fingerprint

| Record | `OrderLogEntryKind` | Fingerprint token (`{}` = field) |
|--------|---------------------|----------------------------------|
| [`OrdnanceStateChangeRecord`](../../src/ProjectAegis.Delegation/Decision/OrdnanceStateChangeRecord.cs) | `OrdnanceStateChange = 18` | `{SimTick}\|{UnitId}\|{PreviousState}\|{NewState}\|{RoundsRemaining}` |

It folds into `ChronologicalEntries` via `DecisionLog`, so it **participates in the replay
fingerprint and world-state hash** — changing a golden scenario's magazine numbers or the
`shotgunRoundsThreshold` changes its hash; regenerate the affected golden. In the message log
([`MessageLogProjection`](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs)) it
renders under the **`ORDNANCE`** category:
`Ordnance <unit>: SHOTGUN → WINCHESTER (rem 0)`.

---

## 2 · Air-ops FSM (LOG-08 / CMD-24 Phase N)

[`AirOpsFsm`](../../src/ProjectAegis.Sim/Logistics/AirOpsFsm.cs) is a pure static transition
function over the immutable [`AirOpsUnitState`](../../src/ProjectAegis.Sim/Logistics/AirOpsUnitState.cs)
record; [`AirOpsStateMap`](../../src/ProjectAegis.Sim/Logistics/AirOpsStateMap.cs) is the optional
per-unit ledger (an `Ordinal` dictionary + FSM wrappers).

### Phases & launch pipeline

Phases ([`AirOpsPhase`](../../src/ProjectAegis.Sim/Logistics/AirOpsPhase.cs)):
`OnGround`, `Prepping`, `Taxiing`, `TakingOff`, `Airborne`, `Landing`, `Maintenance`.

```
OnGround ──RequestLaunch (ready)──▶ Prepping ─(prep timer)─▶ Taxiing ─(taxi)─▶ TakingOff ─(takeoff)─▶ Airborne
   ▲                                   │           │             │
   └──────────── AbortLaunch ──────────┴───────────┴─────────────┘   (back to OnGround, ReadyForLaunch=true)
```

- `RequestLaunch(state, prepDurationTicks = 3, force = false)` → `BeginPrep` when `OnGround`;
  requires `ReadyForLaunch` unless `force`.
- `Tick(state, deltaTicks = 1, taxi = 2, takeoff = 1)` walks the launch pipeline as each phase
  timer reaches 0; default cadence **prep 3 → taxi 2 → takeoff 1 = 6 ticks to Airborne**.
  `Airborne` clears `ReadyForLaunch` and `CanAbortLaunch`.
- `AbortLaunch(state)`: from `Prepping | Taxiing | TakingOff` → back to `OnGround` (re-armed);
  from `Airborne | Landing` → refused `AIR_ABORT_TOO_LATE`.
- `RequestGroupLaunch(states, …)` returns one result per unit **ordered by unit id** (deterministic).

Every transition returns an `AirOpsFsmResult(Accepted, State, RefusalReason?)`; `State` is always
the post-attempt state (unchanged when refused).

### Refusal codes

| Code | When |
|------|------|
| `AIR_NOT_READY` | `OnGround` but not `ReadyForLaunch` (and not forced). Aligns with the engage `AirOperationsReady` gate / `AbortReasonCatalog.Engage.AIR_NOT_READY`. |
| `AIR_ALREADY_AIRBORNE` | launch requested while `Airborne` / `Landing`. |
| `AIR_IN_MAINTENANCE` | launch requested from `Maintenance`. |
| `AIR_LAUNCH_IN_PROGRESS` | launch requested while already in the launch pipeline. |
| `AIR_ABORT_TOO_LATE` | abort requested while `Airborne` / `Landing`. |
| `AIR_ABORT_NOT_ACTIVE` | abort requested from `OnGround` / `Maintenance`. |

### Session wiring

[`SimulationSession.ProcessLogisticsOrders`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
maps executed orders to the FSM each tick, then `AirOps.TickAll(1)` advances every airframe in
`Ordinal` order:

| `OrderKind` | Effect |
|-------------|--------|
| `LaunchAircraft` | `EnsureAirUnit` (seeds `OnGround`, readiness from `UnitReadiness.IsReadyForLaunch`, default `true`) then `AirOps.TryLaunch(target)`. |
| `AbortLaunchAircraft` | `EnsureAirUnit` then `AirOps.TryAbort(target)`. |

### Read-model (`AirOpsProjection`)

[`AirOpsProjection`](../../src/ProjectAegis.Delegation/Projection/AirOpsProjection.cs) is the pure
CMD-24 Air Operations panel projection → [`AirOpsEntry`](../../src/ProjectAegis.Delegation/Projection/AirOpsEntry.cs)
rows (`ProjectLifecycle` from FSM states, or `Project` from readiness tuples). It surfaces the phase
label, `TimeToReadyTicks` ETA, `CanLaunch` / `CanAbort` action flags, a stable
`LaunchDisabledReason` (`ResolveLaunchDisabledReason`), and an `Aggregate` `READY n/total` header —
all read-only, no mutation.

---

## 3 · Boat-ops FSM (LOG-09…11 / CMD-25)

[`BoatOpsFsm`](../../src/ProjectAegis.Sim/Logistics/BoatOpsFsm.cs) over
[`BoatOpsUnitState`](../../src/ProjectAegis.Sim/Logistics/BoatOpsUnitState.cs) is richer than
air-ops: it has a **recovery** pipeline, a **sea-state** gate, an **embarked-load** gate, and a
`Stranded` liability. State ledger: [`BoatOpsStateMap`](../../src/ProjectAegis.Sim/Logistics/BoatOpsStateMap.cs).

### Phases & pipelines

Phases ([`BoatOpsPhase`](../../src/ProjectAegis.Sim/Logistics/BoatOpsPhase.cs)):
`Stowed`, `Prepping`, `Launching`, `Waterborne`, `Returning`, `Alongside`, `Recovering`,
`Maintenance`, `Stranded`.

```
Launch:   Stowed ──RequestLaunch──▶ Prepping ─(prep 2)─▶ Launching ─(launch 2)─▶ Waterborne
Recovery: Waterborne ──RequestRecover──▶ Returning ─(2)─▶ Alongside ─(1)─▶ Recovering ─(2)─▶ Stowed
Liability: {Waterborne | Returning | Alongside} ──HostDepartWhileWaterborne──▶ Stranded
```

- `RequestLaunch(state, seaStateDouglas, prep = 2)` → `BeginPrep` when `Stowed` and both gates pass.
- `RequestRecover(state, seaStateDouglas, return = 2)` → `Returning` only from `Waterborne` and only
  when sea ≤ `MaxRecoverySeaState`.
- `Tick(...)` advances whichever pipeline the craft is in (launch cadence 2/2; recovery cadence
  2/1/2).
- `AbortLaunch(state)`: from `Prepping | Launching` → `Stowed`; once `Waterborne`/recovery →
  `BOAT_ABORT_TOO_LATE`.
- `HostDepartWhileWaterborne(state)` (LOG-11): host leaves while the craft is off-ship
  (`Waterborne | Returning | Alongside`) → `Stranded`.

### The gates

| Gate | Rule |
|------|------|
| **Sea-state (launch)** | `seaState ≤ MaxLaunchSeaState`; else refused with `BOAT_NOT_READY:maxLaunchSeaState=<n>`. |
| **Sea-state (recovery)** | `seaState ≤ MaxRecoverySeaState`; else `BOAT_NOT_READY:maxRecoverySeaState=<n>`. **Recovery is stricter** — a craft may launch when `maxRecovery < seaState ≤ maxLaunch` but then be unable to recover. |
| **Embarked load** | `IsEmbarkedOverloaded` when `PersonnelEmbarked > PersonnelCapacity` **or** `CargoTons > CargoCapacityTons` ⇒ `EMBARKED_OVERLOAD`. |

Sea state is a static scalar, [`ScenarioSeaState`](../../src/ProjectAegis.Sim/Logistics/ScenarioSeaState.cs)
(Douglas **0–9**, clamped by construction — no weather model). Default craft limits are
`maxLaunch = 4` / `maxRecovery = 3`, capacity `12` personnel / `2.0 t` cargo.

### Refusal codes

`BOAT_NOT_READY` (+ `:maxLaunchSeaState=` / `:maxRecoverySeaState=` suffix), `EMBARKED_OVERLOAD`,
`BOAT_ALREADY_WATERBORNE`, `BOAT_IN_MAINTENANCE`, `BOAT_LAUNCH_IN_PROGRESS`,
`BOAT_RECOVERY_IN_PROGRESS`, `BOAT_NOT_WATERBORNE`, `BOAT_ABORT_TOO_LATE`, `BOAT_ABORT_NOT_ACTIVE`,
`BOAT_STRANDED`, `BOAT_HOST_DEPART_NOT_WATERBORNE`.

### Session wiring

| `OrderKind` | Effect |
|-------------|--------|
| `LaunchBoat` | `EnsureBoatCraft` (seeds `Stowed` defaults) then `BoatOps.TryLaunch(target)`. |
| `RecoverBoat` | `EnsureBoatCraft` then `BoatOps.TryRecover(target)`. |
| `AbortBoatLaunch` | `EnsureBoatCraft` then `BoatOps.TryAbort(target)`. |

`BoatOps.TickAll(1)` advances every craft in `Ordinal` order each tick.

### Read-model (`BoatOpsProjection`)

[`BoatOpsProjection`](../../src/ProjectAegis.Delegation/Projection/BoatOpsProjection.cs) → CMD-25
[`BoatOpsEntry`](../../src/ProjectAegis.Delegation/Projection/BoatOpsEntry.cs) rows carry the phase
label, both sea-state limits + `SeaStateLaunchOk` / `SeaStateRecoveryOk` flags, embarked
personnel/cargo vs capacity, `CanLaunch` / `CanRecover` / `CanAbort`, and stable
`LaunchDisabledReason` / `RecoverDisabledReason`. `Stranded` renders as a first-class status. Pure,
read-only.

---

## Determinism

| Concern | Guarantee |
|---------|-----------|
| No stochastic input | The band resolver and both FSMs use **no** `SeededRng`, no wall-clock, no Unity — they are pure functions of `(state, order, tick, sea state)`. |
| Ordering | Every ledger (`AirOpsStateMap`, `BoatOpsStateMap`, ordnance last-band memory) is keyed `Ordinal`; `TickAll` and group launch iterate sorted. |
| Fixed cadence | `TickAll(1)` advances one tick per sim tick; phase durations are constants (overridable in tests). |
| Fingerprint | **Ordnance** band changes emit a fingerprinted `OrdnanceStateChange` row. **Air/boat-ops FSM state is NOT fingerprinted** — no order-log entry kind — so it is host-side/read-model only and cannot move a replay hash on its own. |

Pinned by
[`OrdnanceStateBandsTests`](../../src/ProjectAegis.Sim.Tests/Logistics/OrdnanceStateBandsTests.cs),
[`LogisticsShotgunEngageGateTests`](../../src/ProjectAegis.Sim.Tests/Engage/LogisticsShotgunEngageGateTests.cs) /
[`LogisticsWinchesterEngageGateTests`](../../src/ProjectAegis.Sim.Tests/Engage/LogisticsWinchesterEngageGateTests.cs),
[`OrdnanceStateChangeOrderLogTests`](../../src/ProjectAegis.Delegation.Tests/Decision/OrdnanceStateChangeOrderLogTests.cs),
[`AirOpsFsmTests`](../../src/ProjectAegis.Sim.Tests/Logistics/AirOpsFsmTests.cs) /
[`BoatOpsFsmTests`](../../src/ProjectAegis.Sim.Tests/Logistics/BoatOpsFsmTests.cs),
[`AirOpsProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/AirOpsProjectionTests.cs) /
[`BoatOpsProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/BoatOpsProjectionTests.cs),
and the end-to-end
[`SimulationSessionLogisticsFsmTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionLogisticsFsmTests.cs).

---

## Extending the runtime

| Change | How | Replay impact |
|--------|-----|---------------|
| Tune the SHOTGUN threshold | Set `engage.shotgunRoundsThreshold` in the scenario policy (data, not a C# constant). | Changes `OrdnanceStateChange` tokens / `ShotgunOrdnance` gating on any affected golden → regenerate it. |
| Add an ordnance band / order-log field | Extend `OrdnanceStateBands.Resolve` + `OrdnanceStateChangeRecord`, wire the `DecisionLog` fingerprint case + the `ORDNANCE` `MessageLogProjection` line. | New fingerprint tokens → regenerate affected goldens. |
| Add an FSM phase / refusal code | Add the enum value + transition arm in `AirOpsFsm` / `BoatOpsFsm`, mirror in the projection's `ResolveLaunchDisabledReason`. | None on the replay hash (FSM state is not fingerprinted) — but add FSM unit tests. |
| Change a phase cadence | Adjust the `Default*Ticks` constants (or pass overrides from the state map). | None on the replay hash. |

Keep new fields additive and defaulted-off so existing content (and the v2 hash) is unchanged
until a scenario opts in.

---

## Common pitfalls

| Symptom | Cause / fix |
|---------|-------------|
| No `OrdnanceStateChange` rows despite firing | No `MagazineLedger` is wired (`session.Magazines == null`), or the band never left `NOMINAL`. Rows are emitted only on a band **change** after a launched shot. |
| First shot emits no row even though rounds dropped | First sight of a unit is suppressed unless the resolved band is already non-`NOMINAL`; the next crossing emits. |
| SHOTGUN never triggers | `shotgunRoundsThreshold` is `0` (SHOTGUN disabled — only WINCHESTER at empty). Set it `> 0`. |
| Single-round fire still allowed at SHOTGUN | Intended — the Shotgun gate is *soft* and only blocks `SalvoSize > 1`. Winchester (`≤ 0`) is the hard deny. |
| Boat launched but can't be recovered | Sea state is `> MaxRecoverySeaState` but `≤ MaxLaunchSeaState` — recovery is deliberately stricter than launch (the recovery-asymmetry rule). |
| Craft stuck `Stranded` | `HostDepartWhileWaterborne` fired while it was off-ship (LOG-11). There is no recovery from `Stranded` in the MVP FSM. |
| Air/boat-ops change didn't move a golden hash | Expected — FSM state is host-side/read-model, not in the fingerprint. Only ordnance/fuel band rows are fingerprinted. |

---

## See also

| Topic | Where |
|-------|-------|
| Fuel burn ledger + Joker/Bingo bands (sibling) | [logistics-fuel-runtime.md](logistics-fuel-runtime.md) |
| Joker/Bingo/Shotgun/Winchester as gauntlet variables | [gauntlet-logistics-variables.md](gauntlet-logistics-variables.md) |
| The engage gates that read these states | [engagement-pipeline.md](engagement-pipeline.md#the-gate-chain-exact-order) |
| Stable abort codes (`BingoFuel` / `ShotgunOrdnance` / `WinchesterOrdnance`) | [abort-reason-catalog.md](abort-reason-catalog.md) |
| `logistics` / `engage` JSON field reference | [scenario-policy-authoring.md](scenario-policy-authoring.md#top-level-fields) |
| Order-log / fingerprint / golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| C2 read-model / projection layer (panels) | [c2-projection-layer.md](c2-projection-layer.md) |
