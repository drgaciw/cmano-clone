# ADR-020: Deterministic Fuel Burn Model

## Status

**Accepted** (records the shipped constant-burn model; establishes the tick↔wallclock contract; catalog binding and throttle/altitude deferred)

## Date

2026-07-24

## Last Verified

2026-07-24 (DRG-44 closeout research; call-site audit verified against source)

## Decision Makers

Owner sign-off 2026-07-24; DRG-44 research + closeout; `logistics-magazines.md` GDD

## Summary

Records the architecture of **deterministic fuel burn** — per-unit fuel drain over sim time with NOMINAL / JOKER / BINGO band transitions logged to the order log. The mechanism ships (`FuelLedger`, `FuelTimelineTracker`, `ScenarioLogisticsSettings`) and is enabled in three scenarios. This ADR closes `TR-logistics-003` by recording five decisions that were previously implicit — most importantly **the tick↔wallclock contract**, whose absence is a live defect (DRG-50).

## Engine Compatibility

| Field | Value |
|-------|-------|
| Engine | Unity 6.3 LTS + .NET 8 headless |
| Unity APIs | None — `FuelLedger` is plain C# in `ProjectAegis.Sim` (ADR-001 boundary holds) |
| Burst/Jobs | Not applicable |
| Risk | **MEDIUM** — the contract in Decision 1 requires a fix in `DelegationBridge`, which is under the ZERO-hotpath-edit invariant |

## ADR Dependencies

| Relationship | ADR / artifact |
|--------------|----------------|
| **Depends on** | ADR-001 (sim boundary), ADR-003 (order log), ADR-004 (tick pipeline order) |
| **Related** | ADR-006 (data layer boundary — constrains Decision 5) |
| **Enables** | `TR-logistics-003`; unblocks `TR-logistics-004` (editor fuel validation) |
| **Fix tracked by** | Linear **DRG-50** — the live cadence defect |

## GDD Requirements Addressed

| TR-ID | GDD | Requirement |
|-------|-----|-------------|
| TR-logistics-003 | logistics-magazines.md | Deterministic fuel burn |

## What ships today

| Component | Location |
|---|---|
| `FuelLedger` | `src/ProjectAegis.Sim/Logistics/FuelLedger.cs` — `AdvanceTick(unitId, deltaSeconds)` at `:29`, `burn = rate * deltaSeconds`, `Math.Clamp(previous - burn, 0, capacity)` at `:37` |
| `FuelTimelineTracker` | `src/ProjectAegis.Delegation/Logistics/` — drains all units in `TargetId` ordinal order, emits `FuelBurnRecord` / `FuelStateChangeRecord` on band crossings |
| `ScenarioLogisticsSettings` | `src/ProjectAegis.Sim/Scenario/` — all tunables; `UsesFuelBurnModel => FuelCapacityKg > 0 && BurnRateKgPerSecond > 0` at `:65` |
| Enabled in | `baltic-patrol-comms`, `baltic-v2-comms-challenged`, `baltic-v3-patrol-comms` (all `cap=10000kg, burn=80kg/s`) |

Fingerprint safety is already handled: fuel doubles route through `FingerprintFloat.Format` (`DecisionLog.cs:303,305`), the invariant-culture 6-decimal formatter introduced for exactly this class of drift.

## Decision

### 1. `Tick()` callers must report **real elapsed sim-time**; the bridge must not assume a cadence

This is the load-bearing decision, and it is **currently violated**.

`DelegationBridge.cs:378` passes a hardcoded `1.0`:

```csharp
var drain = _fuelTimeline.Drain(simTick, snapshot.SimTime, 1.0, unitIds);
```

That is correct only if every caller advances exactly one simulated second per call. `BalticReplayHarness` does. **`SimplePlayModeSimHost` does not** — it advances `simTimeStep` (default `1f/60f`) inside `Update()` and still drains a full second, producing a **~60× over-drain**. Verified end-to-end; tracked as **DRG-50**.

**Decision: `deltaSeconds` must be derived from actual elapsed sim time, never a literal.** The bridge should track the previous tick's `SimTime` and pass the difference.

> ⚠️ The fix touches `DelegationBridge`, which is under the **ZERO hotpath edit** invariant. It is therefore gated on its own impact analysis and full-suite verification — this ADR authorises the *contract*, not an unreviewed edit.

### 2. Constant burn rate for MVP-2; throttle and altitude deferred

The GDD describes `burn = baseBurnRate * throttleFactor` with an altitude term. Neither exists — the code is a flat constant, and there are no `throttle` or `altitude` symbols anywhere in `Logistics/`.

**Decision: keep the constant-rate model.** Throttle regimes and altitude curves stay P1, matching the precedent set by magazines (which likewise deferred catalog binding). Landing new tunable dimensions alongside a contract fix would make any regression impossible to attribute.

### 3. Fuel parameters stay scenario-policy-sourced; catalog binding gets its own ADR

`CatalogPlatformEntry` is `(PlatformId, LatDeg, LonDeg, CombatRadiusNm)` — it has **no fuel fields at all**. Per-platform burn profiles (a frigate and a UAV should not share a rate) would require extending the catalog schema plus `PlatformWorkbook` import/export, the workbook validator, and the export resolver.

**Decision: fuel stays in `ScenarioLogisticsSettings`.** Catalog binding is deferred to a dedicated ADR, since ADR-006 places platform-capability schema in coordination territory rather than a side effect of closing this row.

### 4. One authoritative fuel computation; consumers project from it

Two computations are live today:

- `FuelLedger.AdvanceTick` — per-tick accumulator; feeds the order log, and therefore the fingerprint
- `ScenarioLogisticsSettings.RemainingFuelKg(simTime)` / `RemainingFuelFraction(simTime)` — closed form; feeds `FuelStateProjection` (`:19,30`), which uses it for **both the displayed kg and the band state**

They agree only while `Σ(deltaSeconds passed to Drain) == elapsed simTime`. Under the Decision 1 violation they diverge, and the gap **compounds every frame** — so the message log can announce BINGO while the unit-detail panel still reads NOMINAL. The disagreement is on *state*, not just a number.

**Decision: the fuel model has exactly one authoritative computation, and consumers project from it rather than recomputing independently.**

Note the ordering: once Decision 1 holds, accumulator and closed form agree by construction. Decision 4 removes the *duplication risk* so they cannot silently drift apart again; it does not by itself fix DRG-50.

### 5. Editor and sim fuel formulas stay independent, cross-checked by test

`ReachabilityCalculator.TryClassifyStrikeUnreachable` (`src/ProjectAegis.Data/Validation/`) uses an independent closed form: `combatRadiusNm * fuelFraction - ingressEgressPadNm`. It has no dependency on `FuelLedger`, and **structurally cannot have one** — `ProjectAegis.Data` sits *below* `ProjectAegis.Sim` in the ADR-006 layering.

**Decision: keep them independent.** They answer different questions — the editor produces a *planning estimate* at authoring time, the sim produces *runtime truth*. Unifying them means a new lower-tier sustainment assembly, which is disproportionate.

**Required instead:** a cross-verification test asserting the two stay within a documented tolerance for a shared scenario, so divergence is caught rather than discovered. This resolves GDD Open Question #4 and is what moves `TR-logistics-004` off Partial.

## Alternatives Considered

| Option | Why rejected |
|--------|--------------|
| Keep the implicit "1 `Tick()` = 1 sim-second" contract and document it | It is already violated in shipped Editor tooling. A contract nothing enforces is how DRG-50 happened |
| Add throttle/altitude now | Scope inflation; the GDD itself lists both as P1 |
| Extend `CatalogPlatformEntry` with fuel fields in this ADR | Touches workbook import/export + validator + export resolver; deserves its own review |
| Extract a shared editor/sim sustainment library | Requires a new assembly below `ProjectAegis.Data`; solves a problem better addressed by a tolerance test |
| Make the ledger closed-form to eliminate accumulation drift | Attractive, but loses per-unit state (units entering after t=0) and is moot once Decision 1 holds |

## Consequences

### Positive

- The tick↔wallclock contract is now stated, so DRG-50 is a contract violation with a named fix rather than a mystery
- `TR-logistics-003` closes on the shipped mechanism; determinism is already protected via `FingerprintFloat`
- `TR-logistics-004` gets a concrete unblock path (the tolerance test)

### Negative

- Fuel remains scenario-uniform: every unit in a scenario burns at the same rate, which is unrealistic and will need Decision 3 revisited before fuel becomes a tactical lever
- The Decision 1 fix is blocked behind the ZERO-hotpath gate, so the live defect persists until that is scheduled
- Decision 4 is stated as a principle; the actual de-duplication is not yet implemented

## Validation Criteria

- [x] Fuel doubles route through `FingerprintFloat` — `DecisionLog.cs:303,305`
- [x] Band thresholds are data-driven, not hardcoded — `ScenarioLogisticsSettings` (`jokerFuelFraction` 0.25, `bingoFuelFraction` 0.10)
- [x] Model is opt-in per scenario — `UsesFuelBurnModel` at `:65`; absent `logistics` block ⇒ no fuel rows in the fingerprint
- [x] Drain order is deterministic — `FuelTimelineTracker` iterates units in `TargetId` ordinal order
- [ ] **OPEN (DRG-50):** `deltaSeconds` derived from real elapsed sim time; regression test driving the bridge at 1/60 cadence asserting fuel consumed tracks elapsed time, not call count
- [ ] **OPEN:** single-source-of-truth de-duplication so `FuelStateProjection` no longer recomputes independently
- [ ] **OPEN (TR-logistics-004):** editor/sim cross-verification tolerance test

## Migration Plan

1. Mark `TR-logistics-003` **Covered** — done with this ADR.
2. Fix DRG-50 under its own impact analysis and full-suite verification. **Do not bundle with documentation work.**
3. Add the cadence regression test alongside that fix.
4. De-duplicate the fuel computation per Decision 4.
5. Add the editor/sim tolerance test; move `TR-logistics-004` off Partial.
6. Revisit Decision 3 (catalog binding) when per-platform fuel becomes a gameplay requirement rather than a modelling nicety.
