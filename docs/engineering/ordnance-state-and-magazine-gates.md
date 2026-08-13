# Ordnance state bands & magazine engage gates — developer guide

Every shot in the MVP kill chain draws from a per-`(shooter, mount)` **magazine ledger**. On top of
raw round counting, the sim layers aviation-doctrine **ordnance readiness bands** —
`NOMINAL` → `SHOTGUN` → `WINCHESTER` — and two engage gates that turn those bands into
fire-control decisions: a **soft** gate that blocks multi-salvo fire when ordnance is low, and a
**hard** gate that denies any shot once the magazine is empty. Band transitions are emitted to the
order log so the C2 message feed can surface "SHOTGUN" / "WINCHESTER" warnings, mirroring the
[fuel](logistics-fuel-runtime.md) `JOKER` / `BINGO` bands.

This is the ordnance/logistics slice of the tick-8 engage resolver; the full ordered gate chain lives
in [engagement-pipeline.md](engagement-pipeline.md), and these bands are one of the pressure knobs the
[gauntlet logistics axis](gauntlet-logistics-variables.md) drives.

- **Pure source (Sim):**
  [`Logistics/OrdnanceStateBands.cs`](../../src/ProjectAegis.Sim/Logistics/OrdnanceStateBands.cs) (the
  band resolver), [`Engage/LogisticsShotgunEngageGate.cs`](../../src/ProjectAegis.Sim/Engage/LogisticsShotgunEngageGate.cs)
  (soft multi-salvo gate), [`Engage/LogisticsWinchesterEngageGate.cs`](../../src/ProjectAegis.Sim/Engage/LogisticsWinchesterEngageGate.cs)
  (hard empty-magazine gate), and the `RoundsRemaining` / `SalvoSize` / `ShotgunRoundsThreshold`
  fields on [`Engage/EngageContext.cs`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs).
- **Resolver:** the gate ordering + `MagazineLedger` consumption in
  [`Engage/MvpEngagementResolver.cs`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs).
- **Emission (Delegation):** `SimulationSession.MaybeEmitOrdnanceStateChange` in
  [`Orchestration/SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
  and the [`Decision/OrdnanceStateChangeRecord.cs`](../../src/ProjectAegis.Delegation/Decision/OrdnanceStateChangeRecord.cs)
  order-log row (kind `OrdnanceStateChange`).
- **Scenario knobs:** `DefaultMagazineRounds` + `ShotgunRoundsThreshold` on
  [`Scenario/ScenarioEngageDefaults.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioEngageDefaults.cs)
  (authored in [scenario-policy-authoring.md](scenario-policy-authoring.md)).
- **Abort codes:** `ShotgunOrdnance` / `WinchesterOrdnance` / `MagazineEmpty` in
  [`Engage/EngagementAbortReason.cs`](../../src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs) — see
  [abort-reason-catalog.md](abort-reason-catalog.md).

---

## The three bands

[`OrdnanceStateBands.Resolve(roundsRemaining, shotgunRoundsThreshold)`](../../src/ProjectAegis.Sim/Logistics/OrdnanceStateBands.cs)
is a pure function:

| Band | Condition | Doctrine meaning |
|------|-----------|------------------|
| `WINCHESTER` | `roundsRemaining <= 0` | Out of weapons. |
| `SHOTGUN` | `threshold > 0 && roundsRemaining <= threshold` | Pre-briefed minimum / defensive residual only. |
| `NOMINAL` | otherwise | Normal magazine state. |

`shotgunRoundsThreshold == 0` **disables** the `SHOTGUN` band entirely (only `WINCHESTER` at empty) —
the same convention the gate and the scenario default honour.

---

## The two engage gates

Both gates are pure and return a nullable `EngagementAbortReason` (null = pass).

### Soft — `LogisticsShotgunEngageGate`

Denies **multi-salvo** fire (`SalvoSize > 1`) while ordnance is in the `SHOTGUN` band, so a low
magazine is preserved for single-round defensive shots. It is a no-op when the threshold is `0` or the
salvo is a single round. Abort reason: **`ShotgunOrdnance`** (`25`).

```csharp
LogisticsShotgunEngageGate.Evaluate(in ctx, liveRoundsRemaining) // → ShotgunOrdnance | null
```

### Hard — `LogisticsWinchesterEngageGate`

Denies **any** shot once `roundsRemaining <= 0`. Abort reason: **`WinchesterOrdnance`** (`26`).

---

## Where they sit in the resolver

`MvpEngagementResolver.Resolve` reads the **live** round count from the `MagazineLedger` once a mount
is seeded, falling back to `EngageContext.RoundsRemaining` for never-seeded mounts (so an unseeded key
is not treated as empty). The ordnance gates then bracket the doctrine/sensor checks:

1. **`SHOTGUN` soft gate** — evaluated early (before the spoof/EMCON/fire-control checks), so a
   multi-salvo shot on a low magazine is rejected up front.
2. … track-spoof, EMCON, and (CEC-aware) fire-control gates …
3. **`WINCHESTER` hard gate** — evaluated *after* the doctrine sensor/FC gates, so an empty magazine
   surfaces as an explicit `WINCHESTER_ORDNANCE` abort (load-bearing for the QA gauntlet's saboteur
   coverage) rather than a bare `MagazineEmpty`.
4. `MagazineEmpty` (`3`) remains the fallback when both `EngageContext.RoundsRemaining` and the ledger
   are `≤ 0`, and again if `MagazineLedger.TryConsumeSalvo` fails at the actual consume step.

The full ordered chain (policy → FC → domain → envelope → DLZ → consume → launch) and the
`MagazineLedger` model are documented in [engagement-pipeline.md](engagement-pipeline.md).

---

## Band-transition order-log emission

After a successful fire, `SimulationSession.MaybeEmitOrdnanceStateChange` resolves the shooter's new
band from the ledger and, **only on a change**, appends an
[`OrdnanceStateChangeRecord`](../../src/ProjectAegis.Delegation/Decision/OrdnanceStateChangeRecord.cs)
(`PreviousState`, `NewState`, `RoundsRemaining`) to the order log. Notable behaviour:

- The per-unit last band is tracked in `_lastOrdnanceBand`; a shooter that is already `NOMINAL` and
  stays `NOMINAL` emits nothing (no spurious rows).
- The threshold comes from the scenario policy (`EngageDefaults.ShotgunRoundsThreshold`, default `1`).
- The row is the magazine counterpart to the fuel `FuelStateChangeRecord` (`JOKER` / `BINGO`), and the
  [message-log projection](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs)
  surfaces it as an ordnance warning in the C2 feed.

Because this is a real order-log row, it participates in the order-log fingerprint — regenerate the
relevant magazine replay golden (e.g. `ReplayGoldenBalticMagazineTests`) if you change band or
emission semantics.

---

## Scenario knobs

Authored on the scenario policy's engage defaults (see
[scenario-policy-authoring.md](scenario-policy-authoring.md)):

| Field | Default | Effect |
|-------|---------|--------|
| `DefaultMagazineRounds` | `2` | Seeds each mount's starting rounds when the catalog does not. |
| `ShotgunRoundsThreshold` | `1` | Rounds-remaining at/below which the `SHOTGUN` band + soft gate apply; `0` disables `SHOTGUN`. |

Both are clamped non-negative on `ScenarioEngageDefaults`.

---

## Determinism

All of this is pure/deterministic: `OrdnanceStateBands.Resolve` and both gates are functions of
integer round counts and thresholds (no RNG, no wall-clock); the ledger is consumed in a fixed order
inside the resolver; and band emission is a deterministic, change-only order-log append. The abort
reasons are stable enum codes (see [abort-reason-catalog.md](abort-reason-catalog.md)); the ordnance
order-log row feeds the fingerprint but leaves the Baltic v2 world-state hash untouched.

---

## Tests

| Test | Covers |
|------|--------|
| [`OrdnanceStateBandsTests`](../../src/ProjectAegis.Sim.Tests/Logistics/OrdnanceStateBandsTests.cs) | Band resolution + threshold-0 disable. |
| [`LogisticsShotgunEngageGateTests`](../../src/ProjectAegis.Sim.Tests/Engage/LogisticsShotgunEngageGateTests.cs) | Soft multi-salvo denial + single-round pass. |
| [`LogisticsWinchesterEngageGateTests`](../../src/ProjectAegis.Sim.Tests/Engage/LogisticsWinchesterEngageGateTests.cs) | Hard empty-magazine denial. |
| [`CatalogMagazineLedgerSeederTests`](../../src/ProjectAegis.Sim.Tests/Engage/CatalogMagazineLedgerSeederTests.cs) | Initial-rounds seeding. |
| [`OrdnanceStateChangeOrderLogTests`](../../src/ProjectAegis.Delegation.Tests/Decision/OrdnanceStateChangeOrderLogTests.cs) | Order-log band-transition row. |
| [`ReplayGoldenBalticMagazineTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenBalticMagazineTests.cs) | Golden magazine/ordnance replay. |

Run: `dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter "Ordnance|Shotgun|Winchester"`.

---

## Related references

| Where | What |
|-------|------|
| [engagement-pipeline.md](engagement-pipeline.md) | The full tick-8 gate chain + `MagazineLedger` these gates sit inside. |
| [gauntlet-logistics-variables.md](gauntlet-logistics-variables.md) | The QA gauntlet logistics pressure axis that exercises low-ordnance states. |
| [logistics-fuel-runtime.md](logistics-fuel-runtime.md) | The fuel `JOKER`/`BINGO` band sibling (same order-log-row pattern). |
| [abort-reason-catalog.md](abort-reason-catalog.md) | The stable `ENGAGE_ABORT` code catalog (`ShotgunOrdnance`/`WinchesterOrdnance`/`MagazineEmpty`). |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | Authoring `DefaultMagazineRounds` / `ShotgunRoundsThreshold`. |
