# Logistics fuel runtime — burn ledger & Joker/Bingo bands

The **logistics fuel runtime** is the per-tick fuel model behind the scenario `logistics`
block (logistics GDD § Fuel & endurance). It is the last of the per-tick "hot-tick" runtimes —
a sibling of the [comms degradation](comms-degradation-runtime.md) and
[catalog damage](catalog-damage-readiness-runtime.md) runtimes — that run off the scenario
policy each tick and write deterministic order-log evidence.

There are **two distinct fuel models**, and which one is live depends purely on the scenario
policy:

1. **Time-band model (always on, default).** Fuel state is a pure function of elapsed sim time
   against `jokerSimSeconds` / `bingoSimSeconds`. No ledger, no order-log rows — it only
   surfaces through the read-model ([`FuelStateProjection`](#read-model-fuelstateprojection)).
2. **Burn ledger model (opt-in).** Activated when a scenario sets `fuelCapacityKg > 0` **and**
   `burnRateKgPerSecond > 0`. A per-unit [`FuelLedger`](#the-ledger-fuelledger) drains kilograms
   each tick and emits `FuelStateChange` / `FuelBurn` order-log rows that participate in the
   replay fingerprint.

> **Scope.** This page is the runtime deep-dive. The `logistics` **JSON field reference** lives in
> [scenario-policy-authoring.md](scenario-policy-authoring.md#top-level-fields); this page is what
> you read to understand *what the numbers do at tick time*. The two models are engine-agnostic
> (`ProjectAegis.Sim` + `ProjectAegis.Delegation`); the Unity adapter only drives them.

---

## Where it runs

```
DelegationBridge.Tick(snapshot, orderSink)
  ├─ EmitCommsTransitions(snapshot)          # comms-degradation-runtime.md
  ├─ AdvanceSpoofTimeline(snapshot)
  ├─ EmitFuelTransitions(snapshot)   ◄── fuel drain + band transitions (this doc)
  │     _fuelTimeline?.Drain(simTick, simTime, deltaSeconds: 1.0, Registry.CollectMemberIds())
  │       → DecisionLog.AppendFuelBurn / AppendFuelStateChange
  ▼
  ObservedStateBuilder.Build(...) → DelegationOrchestrator.Tick() → engagement / damage
```

Fuel is emitted **at the top of the bridge tick**, before the observed state is built and before
any decision/engagement runs — the same pre-decision slot as comms. Key call-site facts
([`DelegationBridge.EmitFuelTransitions`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs)):

- The tracker is created **once** at bridge construction via `FuelTimelineTracker.TryCreate` and
  is `null` unless the scenario enables the burn model — so a time-band-only scenario does zero
  fuel work per tick.
- `deltaSeconds` is a fixed **`1.0`** per tick; burn per tick is therefore exactly
  `burnRateKgPerSecond`.
- `simTick` is derived as `(ulong)Math.Max(0, (long)snapshot.SimTime)`.
- Units are `Registry.CollectMemberIds()`; if empty, the drain is skipped.

---

## The ledger (`FuelLedger`)

[`FuelLedger`](../../src/ProjectAegis.Sim/Logistics/FuelLedger.cs) holds per-unit remaining
kilograms in an `Ordinal` dictionary and applies the GDD burn law `F' = F − burn·Δt`.

| Member | Behaviour |
|--------|-----------|
| ctor `(capacityKg, burnRateKgPerSecond)` | Throws `ArgumentOutOfRangeException` if either is negative. |
| `EnsureUnit(id)` | Lazily seeds a unit's tank to full `capacityKg` on first sight. |
| `AdvanceTick(id, deltaSeconds)` | `next = clamp(previous − rate·Δt, 0, capacity)`; stores it; returns `(deltaKg, remainingKg)`. |
| `GetRemainingKg(id)` | Remaining kg, or full `capacityKg` for an unseen unit. |
| `ResolveBand(id, jokerFrac, bingoFrac)` | `frac = remaining / capacity`; `frac ≤ bingoFrac → "BINGO"`, else `frac ≤ jokerFrac → "JOKER"`, else `"NOMINAL"`. Capacity ≤ 0 ⇒ `frac = 1` (NOMINAL). |

The clamp is deliberately two-sided: burn is normally non-negative, but a **negative
`deltaSeconds`** (an out-of-order or clock-corrected tick during a replay re-sync) must never let
a tank refill past capacity. Bands are the literal strings `NOMINAL` / `JOKER` / `BINGO`.

---

## The tracker (`FuelTimelineTracker`)

[`FuelTimelineTracker`](../../src/ProjectAegis.Delegation/Logistics/FuelTimelineTracker.cs) wraps
the ledger, remembers each unit's last band, and turns a tick into order-log rows.

- **`TryCreate(profile)`** returns `null` unless `profile.Logistics.UsesFuelBurnModel` is true
  (i.e. capacity and burn are both `> 0`). This is the single gate that decides whether the burn
  model is live.
- **`Drain(simTick, simTime, deltaSeconds, unitIds)`** iterates `unitIds` **sorted `Ordinal`**
  (determinism), and per unit:
  1. `EnsureUnit` + `AdvanceTick` → `(deltaKg, remainingKg)`.
  2. If `LogTickBurn` is set, append a `FuelBurnRecord` (every tick).
  3. `ResolveBand`; append a `FuelStateChangeRecord` **only on a band change**:
     - **first sight** of a unit emits a row only if the band is *not* `NOMINAL` (a unit that
       starts full produces no spurious `NOMINAL→NOMINAL` row);
     - otherwise a row is emitted only when the band differs from the remembered one.
  4. Returns `FuelTickDrainResult(Burns, BandChanges)`.

`FuelBurn` rows are **opt-in** (`logTickBurn`, default `false`) because they are per-tick and
noisy; `FuelStateChange` rows are always emitted on band crossings when the burn model is active.

---

## Order-log records & fingerprint

Both records are appended through `DecisionLog` and fold into `ChronologicalEntries`, so they
participate in the replay fingerprint and world-state hash.

| Record | `OrderLogEntryKind` | Fingerprint token (`{}` = field) |
|--------|---------------------|----------------------------------|
| [`FuelStateChangeRecord`](../../src/ProjectAegis.Delegation/Decision/FuelStateChangeRecord.cs) | `FuelStateChange = 15` | `{SimTick}\|{UnitId}\|{PreviousState}\|{NewState}\|{RemainingFuelKg}` |
| [`FuelBurnRecord`](../../src/ProjectAegis.Delegation/Decision/FuelBurnRecord.cs) | `FuelBurn = 16` | `{SimTick}\|{UnitId}\|{DeltaKg}\|{RemainingFuelKg}` |

Floating-point fields are serialized with `FingerprintFloat.Format` for a culture-stable,
bit-reproducible token. Because these rows are in the fingerprint, **changing a golden scenario's
fuel numbers (or toggling the burn model / `logTickBurn`) changes its replay hash** — regenerate
the affected golden.

### Read-model (`FuelStateProjection`)

[`FuelStateProjection`](../../src/ProjectAegis.Delegation/Projection/FuelStateProjection.cs) is
the pure read-model for a unit's fuel line (no mutation). `FormatUnitFuelLine(unitId, simTime,
logistics)` returns:

- burn model on → `FUEL: <BAND> <remaining>kg (<unitId>)`, band from the *remaining fraction*;
- burn model off → `FUEL: <BAND> (<unitId>)`, band from *elapsed sim time* vs
  `jokerSimSeconds`/`bingoSimSeconds`.

The `FuelStateChange` / `FuelBurn` order-log rows also render in the message log
([`MessageLogProjection`](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs))
under the **`FUEL`** category (`Fuel <unit>: NOMINAL → JOKER (…kg)` / `Fuel burn <unit>: … kg`).

---

## Scenario policy `logistics` block

The block maps to
[`ScenarioLogisticsJsonDto`](../../src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonDto.cs)
→ [`ScenarioLogisticsSettings`](../../src/ProjectAegis.Sim/Scenario/ScenarioLogisticsSettings.cs)
via `ScenarioPolicyJsonLoader.ParseLogistics`. Omitting the block yields
`ScenarioLogisticsSettings.Default` (time bands `300` / `600`, burn model off).

```jsonc
"logistics": {
  "jokerSimSeconds": 90,          // time-band model: elapsed-time Joker threshold (default 300)
  "bingoSimSeconds": 180,         // time-band model: elapsed-time Bingo threshold (default 600)
  "fuelCapacityKg": 10000,        // > 0 AND burnRate > 0 ⇒ burn ledger model is ON
  "burnRateKgPerSecond": 80,      // kg burned per sim-second (1.0 s per tick)
  "jokerFuelFraction": 0.25,      // burn model: Joker at ≤ 25% tank (default 0.25)
  "bingoFuelFraction": 0.10,      // burn model: Bingo at ≤ 10% tank (default 0.10)
  "logTickBurn": false            // burn model: also emit per-tick FuelBurn rows (default false)
}
```

| Field | Default | Meaning |
|-------|---------|---------|
| `jokerSimSeconds` | `300` | Time-band Joker (elapsed sim seconds). |
| `bingoSimSeconds` | `600` | Time-band Bingo; must be `≥ jokerSimSeconds`. |
| `fuelCapacityKg` | `0` | Tank size. `> 0` (with burn) turns on the ledger model. |
| `burnRateKgPerSecond` | `0` | Burn rate. `> 0` (with capacity) turns on the ledger model. |
| `jokerFuelFraction` | `0.25` | Burn-model Joker band (fraction of tank). |
| `bingoFuelFraction` | `0.10` | Burn-model Bingo band; must be `≤ jokerFuelFraction`. |
| `logTickBurn` | `false` | Emit a `FuelBurn` row every tick (noisy; off by default). |

### Validation (loader-time)

`ScenarioLogisticsSettings`'s constructor throws `ArgumentOutOfRangeException` (failing the load)
for: negative `jokerSimSeconds`/`bingoSimSeconds`; `bingoSimSeconds < jokerSimSeconds`; negative
`fuelCapacityKg`/`burnRateKgPerSecond`. The **fuel fractions are only validated when the burn
model is active** (`capacity > 0 && burn > 0`) — then each must be in `(0, 1]` with
`bingoFuelFraction ≤ jokerFuelFraction`. This lets content leave a stray/staged fraction override
in place while the burn model is off without crashing scenario load.

---

## Determinism

The fuel runtime is a pure function of `(scenario policy, tick sequence)`:

- units iterate in `Ordinal` order;
- `deltaSeconds` is a fixed `1.0` per tick (no wall-clock; no `SeededRng` needed — fuel is not
  stochastic);
- the two-sided ledger clamp keeps out-of-order/negative Δt replay-safe;
- floats serialize through `FingerprintFloat.Format`.

Pinned by [`FuelLedgerTests`](../../src/ProjectAegis.Sim.Tests/Logistics/FuelLedgerTests.cs),
[`FuelTimelineTrackerTests`](../../src/ProjectAegis.Delegation.Tests/Logistics/FuelTimelineTrackerTests.cs),
[`FuelBurnOrderLogTests`](../../src/ProjectAegis.Delegation.Tests/Decision/FuelBurnOrderLogTests.cs) /
[`FuelStateChangeOrderLogTests`](../../src/ProjectAegis.Delegation.Tests/Decision/FuelStateChangeOrderLogTests.cs),
[`FuelStateProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/FuelStateProjectionTests.cs),
[`ScenarioLogisticsSettingsTests`](../../src/ProjectAegis.Sim.Tests/Scenario/ScenarioLogisticsSettingsTests.cs),
and the end-to-end
[`BalticReplayHarnessFuelTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessFuelTests.cs).

### Replay goldens

The burn model is enabled in the comms-family goldens (`baltic-patrol-comms`,
`baltic-v2-comms-challenged`, `baltic-v3-patrol-comms`), so their replay fingerprints contain
`FuelStateChange` tokens. `logTickBurn` is left off in those, so they carry band-crossing rows
only, not per-tick `FuelBurn` rows. The legacy `baltic-patrol` fixture has **no** burn model, so
it emits no fuel rows — the production v2 replay hash `17144800277401907079` is unaffected by fuel.

---

## Extending the runtime

| Change | How | Replay impact |
|--------|-----|---------------|
| Tune fuel numbers | Edit the scenario's `logistics` block (gameplay values are data, not C# constants). | Changes tokens on any golden with the burn model → regenerate that golden. |
| Turn the burn model on for a scenario | Set `fuelCapacityKg` **and** `burnRateKgPerSecond` `> 0`. | New `FuelStateChange` rows appear → new hash. |
| Add a new band / threshold | Extend `FuelLedger.ResolveBand` + `ScenarioLogisticsSettings`; add the fraction field to the DTO/loader. | Changes band tokens → regenerate affected goldens. |
| Add a new fuel order-log row | Add the record + `OrderLogEntryKind`, wire `DecisionLog.Append*` + a fingerprint case + `MessageLogProjection` line. | New fingerprint tokens → regenerate affected goldens. |

Keep new fields additive and defaulted-off so existing content (and the v2 hash) is unchanged
until a scenario opts in.

---

## Common pitfalls

| Symptom | Cause / fix |
|---------|-------------|
| No `FuelBurn` / `FuelStateChange` rows despite a `logistics` block | The burn model needs **both** `fuelCapacityKg > 0` and `burnRateKgPerSecond > 0`. With only time bands, fuel is read-model only. |
| Burn model on but no `FuelBurn` rows | `logTickBurn` defaults `false`; only band-crossing `FuelStateChange` rows are emitted. Set `logTickBurn: true` for per-tick burn rows. |
| Scenario load throws on fuel fractions | Fractions are validated only when the burn model is active: each must be in `(0, 1]` with `bingoFuelFraction ≤ jokerFuelFraction`. |
| A comms golden's hash changed after a fuel edit | Expected — the burn model is live in the comms-family goldens; regenerate them (never the v2 `baltic-patrol` hash, which has no burn model). |
| Band never reaches BINGO in a short run | Bands are fraction-of-tank (burn model) — at `1.0 s/tick`, reaching `≤ bingoFuelFraction` needs `capacity·(1−bingoFrac)/burnRate` ticks. Time-band model uses `bingoSimSeconds` instead. |

---

## See also

| Topic | Where |
|-------|-------|
| `logistics` JSON field reference | [scenario-policy-authoring.md](scenario-policy-authoring.md#top-level-fields) |
| Sibling per-tick runtimes (same bridge slot) | [comms-degradation-runtime.md](comms-degradation-runtime.md) · [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) |
| Order-log / fingerprint / golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| The headless runner behind the goldens | [baltic-replay-harness.md](baltic-replay-harness.md) |
| Decision tick that consumes the same bridge | [agent-decision-pipeline.md](agent-decision-pipeline.md) |
