# Catalog magazine readiness & ledger seeding — developer guide

Before a shooter can be told *whether* a shot launches (the engage gate chain) or *how* its ordnance
band degrades (the logistics runtime), something has to decide **how many rounds it starts with**.
This page documents that seam: the pure `ProjectAegis.Sim/Catalog/` resolvers that turn
**catalog loadout/magazine rows** into a shooter's initial `MagazineLedger` count, plus the small
companion resolver (`FacilityHpCapacity`) that maps a ledger HP% to a facility capacity label.

It is a **bind-time / pre-decision** concern, not part of `DelegationOrchestrator.Tick` itself: the
seeder runs once per shooter+mount (at scenario bind or on the first prime for a shooter) and never
refills after that. Everything here reads the catalog read-only and is deterministic, so it never
perturbs replay goldens.

Governed by **req-16** (readiness / initial magazine) and **[ADR-006](../architecture/adr-006-data-layer-boundary.md)**
(the data-layer boundary — the sim consumes the catalog through `ICatalogReader` and **never**
mutates it). The whole subsystem is **additive-only**: when the catalog has no loadout/magazine rows
for a platform, callers fall back to scenario defaults and behaviour is byte-identical to the legacy
Baltic fixtures.

- **Source:** the resolvers in [`src/ProjectAegis.Sim/Catalog/`](../../src/ProjectAegis.Sim/Catalog/)
  and the seeder in [`src/ProjectAegis.Sim/Engage/`](../../src/ProjectAegis.Sim/Engage/); the mutable
  count store in [`MagazineLedger.cs`](../../src/ProjectAegis.Sim/Engage/MagazineLedger.cs); the catalog
  row types in [`src/ProjectAegis.Data/Catalog/`](../../src/ProjectAegis.Data/Catalog/); the bind
  wiring in [`SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
  and [`BalticReplayHarness.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs).
- **Related:** the gate chain that **consumes** the seeded rounds (`MagazineEmpty` /
  `WinchesterOrdnance` / `ShotgunOrdnance`) is in [`engagement-pipeline.md`](engagement-pipeline.md);
  the HP-side readiness/withdraw math and the `ReadinessPolicyEvaluator.EvaluateUnit` merge point are
  in [`catalog-damage-readiness-runtime.md`](catalog-damage-readiness-runtime.md); the read-models
  that surface capacity/ammo to the UI are in [`c2-projection-layer.md`](c2-projection-layer.md); the
  loadout/magazine rows themselves are authored through the
  [catalog write gate](catalog-write-gate.md) / [catalog seeding](catalog-seeding.md); determinism
  rules are in [`determinism-and-replay.md`](determinism-and-replay.md).

---

## Where it lives

| File | Role |
|------|------|
| [`CatalogMagazineResolver.cs`](../../src/ProjectAegis.Sim/Catalog/CatalogMagazineResolver.cs) | Pure req-16 resolver: catalog loadout/magazine rows → `MagazineReadiness(TotalRounds, LoadoutId, CatalogResolved)`. |
| [`CatalogMagazineLedgerSeeder.cs`](../../src/ProjectAegis.Sim/Engage/CatalogMagazineLedgerSeeder.cs) | Bind-time bridge: seeds the `MagazineLedger` from the resolver, else from a caller fallback (ADR-006 read-only). |
| [`MagazineLedger.cs`](../../src/ProjectAegis.Sim/Engage/MagazineLedger.cs) | Mutable per-`(shooter, mount)` count + capacity store; one-shot `EnsureInitialRounds`, no refill on consume. |
| [`ReadinessPolicyEvaluator.cs`](../../src/ProjectAegis.Sim/Policy/ReadinessPolicyEvaluator.cs) | `EvaluateMagazine(platformId, catalog)` — thin policy-facing pass-through to the resolver. |
| [`FacilityHpCapacity.cs`](../../src/ProjectAegis.Sim/Catalog/FacilityHpCapacity.cs) | Companion resolver: ledger HP% → `Operational`/`Damaged`/`Destroyed` capacity label + transition dedup. |
| [`CatalogLoadout.cs`](../../src/ProjectAegis.Data/Catalog/CatalogLoadout.cs) / [`CatalogMagazineEntry.cs`](../../src/ProjectAegis.Data/Catalog/CatalogMagazineEntry.cs) | The catalog rows consumed: `PlatformId` / `LoadoutId` / `IsDefault` and `PlatformId` / `LoadoutId` / `WeaponId` / `Quantity`. |

---

## 1. Resolving the initial magazine (`CatalogMagazineResolver`)

`EvaluateInitialMagazine(platformId, catalog, loadoutId?)` is a **pure** function of the catalog and
returns a `MagazineReadiness` record:

```csharp
public sealed record MagazineReadiness(
    int TotalRounds,       // summed magazine quantity for the resolved loadout
    string? LoadoutId,     // which loadout was chosen (null when none apply)
    bool CatalogResolved); // true only when a matching magazine row was found
```

The algorithm, over the **Ordinal-sorted** catalog reads (`GetSortedLoadouts` /
`GetSortedMagazines`), is:

1. **Filter loadouts** to those whose `PlatformId` matches (Ordinal). If none exist →
   `(0, null, CatalogResolved: false)` (the platform simply has no catalog loadout).
2. **Choose the loadout**: the explicit `loadoutId` argument if supplied, else the **default** —
   the first row with `IsDefault == true`, else the first loadout in sorted order. If that yields
   `null` → `(0, null, false)`.
3. **Sum the magazine rows** whose `PlatformId` **and** `LoadoutId` match the chosen loadout, adding
   `Math.Max(0, Quantity)` per row (negative quantities are floored to zero). Track whether at least
   one row matched.
4. Return `(found ? total : 0, resolvedLoadoutId, CatalogResolved: found)`.

`CatalogResolved` is the load-bearing flag: it is **not** "rounds > 0", it is "a magazine row
existed for this platform+loadout". A loadout with an explicit zero-quantity magazine is still
`CatalogResolved: true` (the catalog *did* speak) — that distinction is what lets the seeder decide
between "trust the catalog" and "use the caller fallback".

`ReadinessPolicyEvaluator.EvaluateMagazine(platformId, catalog)` is a one-line pass-through that
exposes the same `MagazineReadiness` to policy/readiness callers, alongside its HP/mobility siblings
(`EvaluateMobility`, `EvaluateUnit` — see
[`catalog-damage-readiness-runtime.md`](catalog-damage-readiness-runtime.md)).

---

## 2. Seeding the ledger (`CatalogMagazineLedgerSeeder`)

`TrySeedInitialRounds(ledger, catalog?, platformId, shooterUnitId, mountId, fallbackRounds, out seededRounds)`
turns the resolver result into a concrete ledger entry. It returns a `bool` meaning **"was this
catalog-resolved?"** (not "did anything get seeded"):

| Case | Ledger effect | `seededRounds` | returns |
|------|---------------|----------------|---------|
| `catalog != null` **and** `CatalogResolved` | `EnsureInitialRounds(shooter, mount, TotalRounds)` — but only when `TotalRounds > 0` | `TotalRounds` | `true` |
| catalog absent / not resolved, `fallbackRounds > 0` | `EnsureInitialRounds(shooter, mount, fallbackRounds)` | `fallbackRounds` | `false` |
| catalog absent / not resolved, `fallbackRounds <= 0` | *(none)* | `0` | `false` |

Two subtleties worth internalising:

- A `CatalogResolved` loadout with `TotalRounds == 0` still **short-circuits the fallback** (it
  returns `true` without touching the ledger) — an explicitly empty catalog magazine is honoured, it
  is not silently topped up from the scenario default.
- Seeding goes through `MagazineLedger.EnsureInitialRounds`, which is **one-shot**: it only writes
  when the `(shooter, mount)` key is *absent*. Re-seeding an already-tracked shooter is a no-op, so
  the ledger never refills mid-run after consumption. Capacity is captured on that first write and
  never shrinks (`GetCapacity` reports the seeded max, useful for `Remaining/Capacity` UI rows).

---

## 3. Bind sites — where seeding actually happens

Two runtimes call the seeder, and each layers its own **policy cap** on top.

### `BalticReplayHarness` (scenario bind, per `gauntlet.units`)

For each catalog unit with a non-empty `PlatformId`, the harness first computes the unit's max weapon
range from its magazine rows' weapon envelopes, then — **only when `maxRange > 0`** and a
`session.Magazines` ledger exists — seeds:

```csharp
CatalogMagazineLedgerSeeder.TrySeedInitialRounds(
    session.Magazines, catalogReader, unit.PlatformId,
    shooterUnitId: (ulong)key, mountId: 0,
    fallbackRounds: Math.Max(1, session.DefaultMagazineRounds ?? 4),
    out seededRounds);
```

It then appends a `MAGAZINE_SEED:<platformId>:<seededRounds>:<maxRange>` order-log event, so the
seeded count and range are visible in the deterministic event stream (and diffable across runs).

### `SimulationSession.PrimeEngageWorld` (first prime per shooter)

When priming a shot the session seeds from the catalog, then **caps** the seeded total at the
scenario policy's `DefaultMagazineRounds` when that value is set (`> 0`):

```csharp
CatalogMagazineLedgerSeeder.TrySeedInitialRounds(
    Magazines, CatalogReader, shooterUnitId,
    request.ShooterUnitId, request.MountId, fallbackRounds: DefaultMagazineRounds ?? 0, out _);

if (DefaultMagazineRounds is int policyRounds && policyRounds > 0
    && Magazines.GetRounds(request.ShooterUnitId, request.MountId) > policyRounds)
{
    Magazines.SetRounds(request.ShooterUnitId, request.MountId, policyRounds);
}
```

**The policy magazine is authoritative.** A catalog mount with a large capacity cannot silently
exceed a tight policy budget, so depletion scenarios (e.g. `baltic-patrol-magazine`) still reach the
`NO_AMMO` / `MagazineEmpty` abort at the expected shot count. Leave `DefaultMagazineRounds` unset to
use the raw catalog-seeded rounds. (See the magazine-cap note in
[`engagement-pipeline.md`](engagement-pipeline.md).)

Once seeded, the count flows into the engage gate chain: `TryConsumeSalvo` is the **only** ledger
mutation on the hot path, and the ledger-vs-context authority drives the `MagazineEmpty`,
`WinchesterOrdnance`, and `ShotgunOrdnance` aborts documented in
[`engagement-pipeline.md`](engagement-pipeline.md).

---

## 4. Companion resolver — facility HP capacity labels (`FacilityHpCapacity`)

The same `Sim/Catalog/` folder holds a second pure "catalog/ledger value → readiness state" helper,
used for **facility** platforms (GDD combat-domains-damage § Facility). It maps a ledger HP% to a P1
capacity label and decides when a transition is worth emitting:

| Helper | Contract |
|--------|----------|
| `MapHpPctToCapacityState(hpPct)` | Clamps `hpPct` to `0..100`, then `<= 0 → "Destroyed"`, `>= 100 → "Operational"`, else `"Damaged"`. |
| `ShouldEmitCapacityTransition(prev, next)` | Emit only when `prev != next`, **and** `prev` isn't already `Destroyed` (terminal), **and** it isn't a `Damaged → Damaged` self-edge. |

`OrderLogFacilityDamageProjection` consumes both to turn `PlatformDamageChange` HP deltas into
capacity-state transitions in the read-model (surfaced by `FacilityPictureProjection` /
`OrderLogFacilityDamageProjection` — see [`c2-projection-layer.md`](c2-projection-layer.md)). The
per-tick HP drain that *feeds* these labels lives in
[`catalog-damage-readiness-runtime.md`](catalog-damage-readiness-runtime.md); this helper is only the
label mapping, kept pure and side-effect-free so the projection stays deterministic.

---

## Determinism rules

- **Read-only catalog (ADR-006).** Both resolvers take `ICatalogReader`; neither mutates it. The
  only mutation is on the sim-owned `MagazineLedger`.
- **Sorted inputs.** Loadout/magazine iteration uses the `GetSorted*` accessors; string comparisons
  are `Ordinal`. Never introduce dictionary/hashset iteration or culture-sensitive compares.
- **One-shot seed.** `EnsureInitialRounds` writes only on first sight of a `(shooter, mount)` key —
  re-binding is idempotent, so seeding order across units cannot change a run.
- **Additive-only.** No catalog loadout rows → resolver returns `CatalogResolved: false` → callers
  keep their existing scenario-default behaviour, and the Baltic v2 hash (`17144800277401907079`)
  is untouched.

---

## Extending it without breaking goldens

- **New magazine source (e.g. per-mount capacity)** — extend `CatalogMagazineResolver` to read the
  new catalog columns, keep the `CatalogResolved` semantics (existed-vs-nonzero), and preserve the
  additive-only fall-through so catalog-absent scenarios are byte-identical.
- **New readiness consumer** — call `ReadinessPolicyEvaluator.EvaluateMagazine` rather than reaching
  into the resolver directly, so policy and engage share one resolution path.
- **New facility capacity band** — add the label + threshold to `FacilityHpCapacity` and update
  `ShouldEmitCapacityTransition` so the terminal/self-edge dedup still holds; the projection change
  will move any facility-damage golden, so re-run and re-grep the hash.
- **Always** re-run the full suite plus the replay golden and grep the Baltic v2 hash before
  submitting:

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
grep -r "17144800277401907079" tests/ data/
```

---

## Tests to read first

| Test | Shows |
|------|-------|
| [`CatalogMagazineResolverTests`](../../src/ProjectAegis.Sim.Tests/Catalog/CatalogMagazineResolverTests.cs) | Default-loadout selection, magazine-quantity summing, and the `CatalogResolved` existed-vs-nonzero contract. |
| [`CatalogMagazineLedgerSeederTests`](../../src/ProjectAegis.Sim.Tests/Engage/CatalogMagazineLedgerSeederTests.cs) | Catalog-resolved vs fallback seeding, the empty-catalog-magazine short-circuit, and the one-shot `EnsureInitialRounds` behaviour. |
| [`CatalogFacilityDamageHotTickApplierTests`](../../src/ProjectAegis.Sim.Tests/Catalog/CatalogFacilityDamageHotTickApplierTests.cs) | Facility HP% → capacity label mapping and the transition dedup rules end-to-end. |
