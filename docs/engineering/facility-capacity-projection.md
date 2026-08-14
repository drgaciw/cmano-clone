# Facility capacity-state projection — HP% → Operational / Damaged / Destroyed

> **ADR:** [`adr-009-combat-domain-validators.md`](../architecture/adr-009-combat-domain-validators.md)
> owns the **damage/HP applier** (Combat Domain Validators & Deterministic Damage Order) + combat-domains-damage GDD.
> The **presentation/read-model seam** is ADR-010 §2–3 (projection is a client, not sim authority),
> ADR-007 (map/globe presentation), and ADR-001 (adapter boundary). Do **not** cite ADR-009 as the
> snapshot/projection contract, and never cite Git ADR-018 here (that is sensor side-picture / datalink).
> **Applier counterpart:** [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md)
> owns the per-tick HP **applier**; this page owns the read-model **projection** derived from it.

The **facility capacity-state projection** turns a facility's HP into the three P1 capacity labels a
C2 client shows — **Operational / Damaged / Destroyed** — and emits a transition row only when the
label actually changes. It is a **read-model** derived from the (fingerprinted) order log; it is
**not** authoritative sim state and is **not** itself fingerprinted. Two engine-agnostic pieces:

1. [`FacilityHpCapacity`](../../src/ProjectAegis.Sim/Catalog/FacilityHpCapacity.cs) — the pure HP% →
   label mapping and the emit-a-transition latch.
2. [`OrderLogFacilityDamageProjection`](../../src/ProjectAegis.Delegation/Projection/OrderLogFacilityDamageProjection.cs)
   — derives the ordered [`FacilityDamageChangeRecord`](../../src/ProjectAegis.Delegation/Projection/FacilityDamageChangeRecord.cs)
   transitions from the order log for the facilities in the picture.

> **Scope.** This is the *capacity-label projection* — how HP% becomes a shown label and when a
> transition is emitted. The per-tick HP drain / kill applier that produces those HP numbers lives in
> [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md); the engagement outcomes
> it can fall back to are in [engagement-pipeline.md](engagement-pipeline.md).

---

## Label mapping & latch — `FacilityHpCapacity`

Pure static helper. The label strings are **duplicated** on
[`FacilityCapacityStates`](../../src/ProjectAegis.Delegation/Projection/FacilityCapacityStates.cs)
(`"Operational"` / `"Damaged"` / `"Destroyed"`). There is no shared type: `FacilityHpCapacity` lives
in `Sim.Catalog` and `FacilityCapacityStates` lives in `Delegation.Projection`. Comparisons are
ordinal string equality, so the two catalogs must stay byte-identical — changing one copy without
the other silently breaks the latch.

**`MapHpPctToCapacityState(double hpPct)`** — clamps `hpPct` to `[0, 100]`, then:

| HP% (clamped) | Label |
|---------------|-------|
| `≤ 0` (`DestroyedHpThreshold`) | `Destroyed` |
| `≥ 100` (`OperationalHpThreshold`) | `Operational` |
| otherwise | `Damaged` |

**`ShouldEmitCapacityTransition(previousState, nextState)`** returns true **only** when all hold:

- `previousState != Destroyed` — once destroyed, a facility never emits again (terminal, monotonic);
- `previousState != nextState` — no-op transitions are suppressed;
- **not** (`previousState == Damaged && nextState == Damaged`) — redundant Damaged→Damaged is
  written explicitly in source but is **already implied** by `previousState != nextState`.

The latch itself is **terminal-once-Destroyed + de-duplicated**, not a one-way HP ratchet.
If a ledger row raises HP from 75 to 100, `MapHpPctToCapacityState` returns `Operational` and
`ShouldEmitCapacityTransition("Damaged", "Operational")` is **true** — a Damaged→Operational
row is emitted. The `Operational → Damaged → Destroyed` story is a **producer assumption**
(catalog damage appliers only lower HP); it is not enforced by the latch.

---

## The projection — `OrderLogFacilityDamageProjection.ProjectDamageChanges`

Given a `DecisionLog` (or its `PlatformDamageChangeRecord` + `EngagementOutcomeRecord` lists) and a
`facilityByTargetId` map of [`FacilityPictureEntry`](../../src/ProjectAegis.Delegation/Projection/FacilityPictureEntry.cs)
(each carrying the facility's current `CapacityState`), it returns the ordered capacity transitions.
An **empty facility map short-circuits to no changes**. It then chooses one of two paths:

### 1. HP-ledger path (S31-05, preferred)

Used when any `PlatformDamageChangeRecord` targets a facility. Filters to facility rows, sorts by
**`SimTick` then `SequenceId`**, and folds:

- `previous` starts from the facility's seeded `CapacityState` (default `Operational`);
- `next = FacilityHpCapacity.MapHpPctToCapacityState(hpChange.NewHpPct)`;
- emit a `FacilityDamageChangeRecord` **only if** `ShouldEmitCapacityTransition(previous, next)`, then
  advance the running capacity for that target.

### 2. Engagement-outcome fallback (S28-09)

Used only when there are **no** facility HP-ledger rows. Filters `Hit`/`Kill` outcomes on facility
victims, orders them with the deterministic `DeterministicDamageApplyBatch.Sort`, and maps
`Hit → Damaged`, `Kill → Destroyed`. It applies the same latch semantics inline: skip if the target
is already `Destroyed`, skip a null map, and skip a redundant `Damaged → Damaged`.

Each emitted `FacilityDamageChangeRecord(SequenceId, SimTime, SimTick, FacilityId, TargetId,
PreviousState, NewState)` is a projection-only row. The consumer
[`FacilityPictureProjection`](../../src/ProjectAegis.Delegation/Projection/FacilityPictureProjection.cs)
binds these into the facility picture.

---

## Determinism & invariants

- **Read-model, off the fingerprint.** The projection is derived *from* the fingerprinted order log
  (`PlatformDamageChangeRecord` / `EngagementOutcomeRecord`); it never writes the log and is not part
  of the world-state hash or order-log fingerprint. Changing label mapping cannot move a replay
  golden.
- **Deterministic ordering.** The HP-ledger path sorts by `(SimTick, SequenceId)`; the outcome
  fallback uses `DeterministicDamageApplyBatch.Sort` — the same input always yields the same
  transition sequence.
- **Terminal & de-duplicated (not HP-monotonic).** The latch never re-emits after `Destroyed` and
  suppresses same-label no-ops. Healing HP (75→100) *will* emit Damaged→Operational. The
  one-way `Operational → Damaged → Destroyed` story holds only while producers never raise HP.
- **Pure.** Both helpers read only their arguments — no RNG, no wall-clock.
- **Dual string catalogs.** `FacilityHpCapacity` and `FacilityCapacityStates` must stay ordinal-equal;
  they are not a shared enum.

---

## Tests (pins)

| Test | Covers |
|------|--------|
| [`Projection/OrderLogFacilityDamageProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/OrderLogFacilityDamageProjectionTests.cs) | Outcome-fallback + HP-ledger transition projection, latch suppression, and destroyed-terminal behavior. |
| [`Projection/OrderLogFacilityDamageProjectionHotTickTests`](../../src/ProjectAegis.Delegation.Tests/Projection/OrderLogFacilityDamageProjectionHotTickTests.cs) | Per-tick HP-ledger projection ordering. |
| [`Catalog/CatalogFacilityDamageHotTickApplierTests`](../../src/ProjectAegis.Sim.Tests/Catalog/CatalogFacilityDamageHotTickApplierTests.cs) | The upstream facility HP applier that feeds the HP-ledger path. |

The projection fixtures are NUnit under `ProjectAegis.Delegation.Tests/Projection`; the applier
fixture is xUnit under `ProjectAegis.Sim.Tests`. All are part of the ≥1638-test baseline.
