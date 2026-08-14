# Magazine ledger seeding — catalog loadout → engage rounds

The engage kill chain gates a shot on how many rounds a shooter's mount has left
([engagement-pipeline.md](engagement-pipeline.md) — the `MagazineEmpty` / Winchester / Shotgun
gates read a `MagazineLedger`). This guide covers the **other half**: where those round counts come
from — the catalog-driven **seeding** path that populates the ledger before the first shot, and its
scenario-default fallback.

It documents `MagazineLedger` (the deterministic per-`(shooter, mount)` count store),
`CatalogMagazineResolver` (catalog loadout + magazine rows → an initial round total),
`CatalogMagazineLedgerSeeder` (the catalog-first, fallback-second seeding step), and the two call
sites that wire it in (`SimulationSession` and `BalticReplayHarness`). It complements — and does not
duplicate — [engagement-pipeline.md](engagement-pipeline.md) (the engage-time **consume** and the
`MagazineEmpty` / Winchester / Shotgun **gates** that read the ledger this path fills).

> **Scope / boundary.** This is the deterministic sim engage path (req 16 / req 21 Phase A). The
> resolver and seeder are **read-only over the catalog** (ADR-006) and **additive-only**: when the
> catalog has no matching rows they leave the caller on its scenario default, so turning the catalog
> on never breaks a scenario that predates it. Nothing here mutates the catalog or the order log.
> Catalog seeding of a headless run is [catalog-seeding.md](catalog-seeding.md); the catalog write
> path is [catalog-write-gate.md](catalog-write-gate.md).

Related:
[engagement-pipeline.md](engagement-pipeline.md) ·
[catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) ·
[catalog-seeding.md](catalog-seeding.md) ·
[catalog-write-gate.md](catalog-write-gate.md) ·
[ADR-006 data-layer boundary](../architecture/adr-006-data-layer-boundary.md).

---

## `MagazineLedger` — the deterministic count store

[`MagazineLedger`](../../src/ProjectAegis.Sim/Engage/MagazineLedger.cs) is the authoritative,
deterministic store of remaining rounds keyed by `(shooterUnitId, mountId)` (both `ulong`). It also
tracks a per-key **capacity** captured on the first write. Key behaviors, verified against source:

| Member | Behavior |
|--------|----------|
| `EnsureInitialRounds(shooter, mount, rounds)` | **Seed once.** Sets rounds + capacity only if the key is not already present — a later engage-time seed never refills a mount that has already fired. |
| `SetRounds(shooter, mount, rounds)` | Sets remaining (clamped `>= 0`); records capacity on first write. Later sets **never shrink** capacity, but **can grow** it when the new remaining exceeds the recorded capacity. |
| `GetRounds` / `GetCapacity` | Remaining / initial capacity (capacity falls back to remaining when never recorded). |
| `TryGetRounds(…, out rounds)` | **Distinguishes tracked-empty from never-seeded** — both read `GetRounds == 0`, but `TryGetRounds` returns `true` only once a key is seeded/consumed. This is what lets the engage gates treat a *tracked* empty mount as Winchester rather than "unknown". |
| `TryConsumeSalvo(shooter, mount, salvoSize)` | Deducts `max(1, salvoSize)` if enough remain; returns `false` (no mutation) otherwise. This is the engage-time consume the resolver calls. |
| `Snapshot()` | Read-only rows ordered by shooter then mount, for the magazine-loadout UI projection (no weapon labels — presentation fills those). |

`GetCapacity` has **no production callers** today — only `MagazineLedgerSnapshotTests`. The
Shotgun/Winchester gates do **not** read captured capacity: [`LogisticsShotgunEngageGate`](../../src/ProjectAegis.Sim/Engage/LogisticsShotgunEngageGate.cs)
compares `liveRounds` to `EngageContext.ShotgunRoundsThreshold`, and
[`LogisticsWinchesterEngageGate`](../../src/ProjectAegis.Sim/Engage/LogisticsWinchesterEngageGate.cs)
hard-denies when remaining is `<= 0`. Capacity is recorded for the `Snapshot()` UI projection
(and for any future % remaining consumers), not for those engage gates.

---

## `CatalogMagazineResolver` — catalog rows → initial rounds

[`CatalogMagazineResolver.EvaluateInitialMagazine(platformId, catalog, loadoutId?)`](../../src/ProjectAegis.Sim/Catalog/CatalogMagazineResolver.cs)
(req 16) resolves a platform's initial magazine total from the catalog's **loadout** and
**magazine** rows, returning an immutable `MagazineReadiness(TotalRounds, LoadoutId?, CatalogResolved)`.
The algorithm (all ordinal, deterministic):

1. Collect `catalog.GetSortedLoadouts()` filtered to the platform (ordinal `PlatformId` match). **No
   loadouts ⇒ `(0, null, CatalogResolved: false)`** — the "catalog can't answer" signal.
2. Pick the loadout: the caller's `loadoutId` if given, else the **default loadout**
   (`ResolveDefaultLoadoutId` — the first `IsDefault` loadout, else the first loadout in sorted
   order). If none resolves ⇒ `(0, null, false)`.
3. Sum `Math.Max(0, Quantity)` over `catalog.GetSortedMagazines()` rows whose `PlatformId` **and**
   `LoadoutId` match the resolved loadout (a [`CatalogMagazineEntry`](../../src/ProjectAegis.Data/Catalog/CatalogMagazineEntry.cs)
   is one weapon's stores in a mount under a loadout: `PlatformId, LoadoutId, MountId, WeaponId,
   Quantity, ReloadTimeSec, Depth`).
4. Return `(total, resolvedLoadoutId, CatalogResolved: found)` — where `found` is set only if at
   least one magazine row matched. If the loadout exists but has **no** magazine rows, the result is
   `(0, resolvedLoadoutId, CatalogResolved: false)`.

The critical output is **`CatalogResolved`**: it is the boolean the seeder switches on to decide
whether the catalog actually supplied an answer, versus falling through to the scenario default.
`ReadinessPolicyEvaluator.EvaluateMagazine` re-exposes this same call as part of the bounded
readiness rollup (req 16/21) — the resolver is the single source of "initial magazine from catalog".

---

## `CatalogMagazineLedgerSeeder` — catalog-first, fallback-second

[`CatalogMagazineLedgerSeeder.TrySeedInitialRounds(ledger, catalog, platformId, shooterUnitId,
mountId, fallbackRounds, out seededRounds)`](../../src/ProjectAegis.Sim/Engage/CatalogMagazineLedgerSeeder.cs)
is the one step that actually seeds the ledger. It is the **additive-only bridge** between the
catalog and the engage magazine gate (read-only over the catalog, ADR-006):

```text
catalog != null?
 ├─ CatalogMagazineResolver.EvaluateInitialMagazine(platformId, catalog)
 │    └─ CatalogResolved?
 │         ├─ yes → if TotalRounds > 0: ledger.EnsureInitialRounds(shooter, mount, TotalRounds)
 │         │         seededRounds = TotalRounds ; return TRUE   (catalog authoritative)
 │         └─ no  → fall through
 └─ fallbackRounds > 0 → ledger.EnsureInitialRounds(shooter, mount, fallbackRounds)
                          seededRounds = fallbackRounds ; return FALSE  (scenario default)
    else               → seededRounds = 0 ; return FALSE               (nothing to seed)
```

The **return value is "was the catalog authoritative?"**, not "did anything seed": a scenario-default
seed returns `false` even though it wrote `fallbackRounds`. A catalog-resolved platform with
`TotalRounds == 0` still returns `true` (the catalog answered "empty") but **does not** call
`EnsureInitialRounds` — the mount stays **never-seeded** (`TryGetRounds` is `false`, `GetRounds` is
`0`). Winchester / tracked-empty only applies after a key has been written. Because positive-round
seeding goes through `EnsureInitialRounds`, re-invoking the seeder for an already-active mount is a
no-op (never a refill).

---

## Where it is wired in

Two call sites seed the ledger; both pass the combat **unit id as `platformId`** so the resolver can
map shooter → catalog rows.

- **`SimulationSession` (per-engage, lazy).** When priming an engage context, if a `Magazines`
  ledger is present the session calls `TrySeedInitialRounds` with `fallbackRounds =
  DefaultMagazineRounds ?? 0`. It then applies a **policy cap**: if the scenario's
  `DefaultMagazineRounds` is a positive value and the seeded rounds exceed it, it `SetRounds` down to
  the policy value — so a scenario can cap a generous catalog loadout without editing the catalog.
- **`BalticReplayHarness` (ORBAT build).** While building the order of battle, for each unit with a
  catalog weapon envelope (`maxRange > 0`) it seeds `session.Magazines` with `fallbackRounds =
  max(1, DefaultMagazineRounds ?? 4)` at `mountId: 0`, capturing `seededRounds` for the run record.

In both cases the ledger the engage resolver later reads (`_magazines.TryGetRounds` /
`TryConsumeSalvo` and the Winchester/Shotgun gates — see
[engagement-pipeline.md](engagement-pipeline.md)) is exactly the one seeded here.

---

## Determinism & invariants

- **Ordinal, pure resolution.** `EvaluateInitialMagazine` reads only the catalog's sorted loadout /
  magazine rows with ordinal id comparison and integer sums — no RNG, no wall-clock — so the same
  catalog yields the same initial magazine on every run and thread.
- **Additive-only / catalog-optional.** Absent catalog rows ⇒ `CatalogResolved: false` ⇒ the caller
  keeps its scenario default. A scenario that never had catalog magazine rows is unaffected, which is
  why enabling catalog seeding does not move the Baltic v2 replay hash `17144800277401907079` on the
  legacy fixtures. Confirm with `grep -r "17144800277401907079" tests/ data/`.
- **Seed-once.** `EnsureInitialRounds` never refills an already-tracked mount, so a mid-run reseed
  (e.g. a later engage on the same mount) cannot silently top up a depleted magazine.
- **Read-only over the catalog (ADR-006).** The resolver/seeder never write the catalog or the order
  log; they only read catalog rows and write the in-memory `MagazineLedger`.

---

## Extending it

1. **New catalog magazine data?** Add rows through the [write gate](catalog-write-gate.md) as
   `CatalogMagazineEntry`/loadout rows; they flow into `EvaluateInitialMagazine` with no code change.
2. **New seeding call site?** Call `CatalogMagazineLedgerSeeder.TrySeedInitialRounds` with the unit's
   `platformId` and a sensible `fallbackRounds`; seed **once** at ORBAT/prime time and let
   `EnsureInitialRounds` protect against refills. Do not seed inside the tight engage loop.
3. **Scenario cap?** Follow `SimulationSession`: seed, then `SetRounds` down to a positive
   `DefaultMagazineRounds` when the catalog loadout is larger.
4. **Verify** with the standard block and confirm no golden moved:

```bash
dotnet build ProjectAegis.sln
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj -v minimal
grep -r "17144800277401907079" tests/ data/   # Baltic v2 hash must be unchanged
```

## Tests

| Test file | Covers |
|-----------|--------|
| [`CatalogMagazineResolverTests.cs`](../../src/ProjectAegis.Sim.Tests/Catalog/CatalogMagazineResolverTests.cs) | Default-loadout resolution, per-loadout magazine sum, `CatalogResolved` true/false (no loadouts, no magazine rows). |
| [`CatalogMagazineLedgerSeederTests.cs`](../../src/ProjectAegis.Sim.Tests/Engage/CatalogMagazineLedgerSeederTests.cs) | Catalog-first seed, scenario-default fallback, the `return`-value = catalog-authoritative contract, seed-once. |
| [`CatalogMagazineReadinessEngageTests.cs`](../../src/ProjectAegis.Delegation.Tests/Sim/CatalogMagazineReadinessEngageTests.cs) | End-to-end: seeded ledger → engage magazine/Winchester gate outcomes. |
| [`CatalogMagazineReaderTests.cs`](../../src/ProjectAegis.Data.Tests/Catalog/CatalogMagazineReaderTests.cs) | Catalog reader returns sorted `CatalogMagazineEntry` rows the resolver consumes. |

---

## See also

| Topic | Doc |
|-------|-----|
| Engage-time magazine consume + the `MagazineEmpty` / Shotgun / Winchester gates | [engagement-pipeline.md](engagement-pipeline.md) |
| Catalog readiness / withdraw trials (sibling req-16/21 rollup) | [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) |
| How a headless run gets a catalog at all | [catalog-seeding.md](catalog-seeding.md) |
| Catalog write path for loadout/magazine rows | [catalog-write-gate.md](catalog-write-gate.md) |
