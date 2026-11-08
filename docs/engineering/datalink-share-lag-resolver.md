# Datalink share-lag resolver — catalog link latency → share-lag ticks

The **datalink share-lag resolver** is the tiny, deterministic bind-time step that answers one
question the detection layer cannot: *how many ticks late does a shared contact arrive on the
datalink?* It lives in
[`ProjectAegis.Sim/Scenario/DatalinkShareLagResolver.cs`](../../src/ProjectAegis.Sim/Scenario/DatalinkShareLagResolver.cs)
and runs **once**, when the Baltic harness binds the side-picture merger — not per tick (TR-sensor-004,
S34-04).

It sits between two things that are already documented elsewhere:

- the **catalog link latency** (a `LinkCatalog` field, latency-validated by the
  [`LinkCatalogRules`](catalog-write-gate.md) pack), and
- the **`DatalinkSidePictureMerger`**, which *consumes* a `ShareLagTicks` count to delay when an
  organic contact becomes shareable to same-side units (documented in
  [detection-pipeline.md](detection-pipeline.md)).

The resolver is the **derivation** in the middle: it converts a real-world millisecond latency
into an integer tick delay, so a scenario author can either pin the delay explicitly or let the
catalog's link characteristics decide.

> **Scope — derivation, not consumption or authoring.** *Where the delay is used* is the merger
> ([detection-pipeline.md](detection-pipeline.md)); *how you author the `datalink` block*
> (`organicOnly`, `unitSides`, `shareLagTicks`) is
> [scenario-policy-authoring.md](scenario-policy-authoring.md). This page is only about how a
> catalog latency in milliseconds becomes `ShareLagTicks`.

---

## Source of truth

Verified against these files (docs-only page — no source changes):

| Concern | File |
|---------|------|
| Resolver (latency → ticks) | [`Scenario/DatalinkShareLagResolver.cs`](../../src/ProjectAegis.Sim/Scenario/DatalinkShareLagResolver.cs) |
| Doctrine record | [`Scenario/ScenarioDatalinkDoctrine.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioDatalinkDoctrine.cs) |
| Latency accessor | [`Catalog/ICatalogReader.cs`](../../src/ProjectAegis.Data/Catalog/ICatalogReader.cs) (`TryGetLinkLatencyMs`) |
| Latency validation | [`Validation/LinkCatalogRules.cs`](../../src/ProjectAegis.Data/Validation/LinkCatalogRules.cs) (`LINK_LATENCY_INVALID`, `[0, 300000]` ms) |
| Bind site | [`Baltic/BalticReplayHarness.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs) (`if (…IsSharingEnabled) … Resolve(…)`) |
| Consumer | [`Sensors/DatalinkSidePictureMerger.cs`](../../src/ProjectAegis.Sim/Sensors/DatalinkSidePictureMerger.cs) |
| Test pins | [`Tests/Scenario/DatalinkShareLagResolverTests.cs`](../../src/ProjectAegis.Sim.Tests/Scenario/DatalinkShareLagResolverTests.cs) |

Requirement anchor: TR-sensor-004 (bounded side-picture sharing), S34-04. The consuming merger is
part of the [detection pipeline](detection-pipeline.md); the latency source is a catalog link row
under the [write gate](catalog-write-gate.md).

---

## The doctrine record

`ScenarioDatalinkDoctrine` is the immutable input and output of the resolver:

```csharp
public sealed record ScenarioDatalinkDoctrine(
    bool OrganicOnly = true,
    IReadOnlyDictionary<string, string>? UnitSides = null,
    int ShareLagTicks = 0,
    bool ShareLagTicksSpecified = false)
{
    public static ScenarioDatalinkDoctrine Default { get; } = new();
    public bool IsSharingEnabled => !OrganicOnly && UnitSides is { Count: > 0 };
    public string ResolveSide(string observerId) => /* UnitSides lookup, "" if absent */;
}
```

- **`OrganicOnly`** (default `true`) — when true, each unit sees only its own sensors; there is no
  datalink and nothing to lag. `Default` is organic-only, so a scenario with **no `datalink`
  block behaves exactly as before** (the production `baltic-patrol.policy.json` case).
- **`IsSharingEnabled`** is the single guard: sharing is on **iff** `OrganicOnly` is false **and**
  at least one `UnitSides` entry exists. The harness only builds a merger — and only calls the
  resolver — when this is true.
- **`ShareLagTicks`** (default `0`) — the resolved integer delay the merger consumes.
- **`ShareLagTicksSpecified`** — the crucial "author pinned it" flag. It distinguishes *"the
  author explicitly wrote `shareLagTicks: 0`"* (a real zero) from *"the field was omitted"* (let
  the catalog decide). Without this flag the resolver could not tell an intentional `0` from a
  default `0`.

---

## The resolution rule

`DatalinkShareLagResolver.Resolve(doctrine, catalog)` returns a doctrine, applying at most one
change — a `with { ShareLagTicks = … }`. It short-circuits, in order:

1. **Author pinned it** — if `ShareLagTicksSpecified`, return the doctrine unchanged. An explicit
   `shareLagTicks` in JSON **always wins** over the catalog.
2. **Sharing disabled** — if `!IsSharingEnabled`, return unchanged (nothing to lag).
3. **No usable link latency** — resolve the primary link id (below); if the catalog has no latency
   for it (`TryGetLinkLatencyMs` false), return unchanged (stays at the default `0`).
4. **Otherwise** — convert latency to ticks and return `doctrine with { ShareLagTicks = … }`.

**The conversion** (fixed `60 Hz` tick rate, so `1000/60 ≈ 16.667` ms/tick):

```
shareLagTicks = ceil( latencyMs / (1000 / 60) )
```

`Math.Ceiling` means any nonzero latency rounds **up** to at least one tick — a link is never
"free". Worked examples from the test pins:

| Latency (ms) | `ceil(latency / 16.667)` | `ShareLagTicks` |
|--------------|--------------------------|-----------------|
| ~50 (Baltic default link) | `ceil(3.0)` | `3` |
| ~250 (`SATCOM_B`) | `ceil(15.0)` | `15` |
| (explicit `2` in JSON) | rule 1 wins | `2` |
| (missing link) | rule 3 | `0` |

### Primary link selection

`ResolvePrimaryLinkId` picks *which* link's latency to use. It does **not** look at
`UnitSides` or the scenario's own units — it takes the **globally first** catalog row:

1. `catalog.GetSortedComms()[0].LinkId` — comms bindings sorted `(PlatformId, LinkId)` ordinal
   (`InMemoryCatalogReader` and SQLite `ORDER BY platform_id, link_id`), else
2. `catalog.GetSortedLinks()[0].LinkId` — the raw `LinkCatalog`, else
3. the hard fallback `"NATO_TADIL_J"`.

So an alphabetically earlier *other* platform's comms binding wins over a later `SATCOM_B` on a
scenario unit. The test pin `Primary_link_prefers_first_sorted_comms_binding_over_link_catalog`
constructs a catalog whose **only** comms row is `SATCOM_B` (so that row *is* index 0) and shows
that a present comms list beats a tactical `LinkCatalog` fallback — not that the resolver selects
"the scenario unit's SATCOM".

---

## Where it runs

The resolver is called from exactly one place — `BalticReplayHarness`, during composition, and
only when `detectionTrials.Count > 0 && profile != null` **and** sharing is on. A sharing-enabled
profile with an empty detection-trial list never builds a merger and never calls `Resolve`:

```csharp
if (profile.DatalinkDoctrine.IsSharingEnabled)
{
    var datalinkDoctrine = DatalinkShareLagResolver.Resolve(profile.DatalinkDoctrine, catalogReader);
    datalinkMerger = new DatalinkSidePictureMerger(datalinkDoctrine, detectionTrials);
}
```

So the resolved `ShareLagTicks` is baked into the merger **once at bind**, then applied every tick
by the merger's queue: an organic transition becomes shareable `shareLagTicks` later, and a `Lost`
cancels a pending share (see [detection-pipeline.md](detection-pipeline.md)).

---

## Determinism & invariants

- **Pure & bind-time.** `Resolve` is a static pure function of `(doctrine, catalog)` with no RNG,
  clock, or I/O; it runs once at harness composition, never on the tick hotpath, so it cannot
  perturb a replay hash mid-run.
- **Author intent wins.** An explicit `shareLagTicks` (`ShareLagTicksSpecified`) is never
  overridden — the catalog only fills an *omitted* value. This keeps hand-tuned latency goldens
  stable regardless of catalog edits.
- **Fail-safe to zero.** Missing link, missing latency, or sharing-off all leave `ShareLagTicks`
  at its default `0` (immediate share) rather than throwing — the merger still works, just without
  lag.
- **Latency is bounded only on the write-gate path.** `LinkCatalogRules` errors
  (`LINK_LATENCY_INVALID`) any `LatencyMsNominal` outside `[0, 300000]` ms at **approve** time.
  `ICatalogReader` / `DatalinkShareLagResolver.Resolve` do **not** re-validate: an
  `InMemoryCatalogReader` (or harness catalog override) can carry a negative latency, `ceil` then
  yields a negative `ShareLagTicks`, and `DatalinkSidePictureMerger` casts that to `ulong` when
  computing the apply tick (wrap / huge delay). Production SQLite catalogs stay in-range because
  they passed the write gate; test/override catalogs must be validated by the caller.
- **Round up, never free.** `Math.Ceiling` guarantees a shared contact from a non-zero-latency
  link is delayed by at least one tick.

---

## Runbook — change the derivation

**Add a new latency source or override.** Keep author intent first: only ever fill `ShareLagTicks`
when `!ShareLagTicksSpecified`. If you add a per-unit or per-link override, resolve it inside
`Resolve` before the primary-link fallback and add a `*_wins_over_catalog` test mirroring
`Explicit_shareLagTicks_in_json_wins_over_catalog`.

**Change the tick rate assumption.** The `DefaultTickRateHz = 60.0` constant must match the sim
fixed step ([sim-clock-time-compression.md](sim-clock-time-compression.md), `FixedDeltaSeconds`).
Changing it moves every catalog-derived `ShareLagTicks` and therefore any `*-datalink*` replay
golden — re-pin the goldens deliberately, and never touch the Baltic v2 hash
(`17144800277401907079`), which uses organic-only scenarios with no share lag.

**Add a fallback link.** The `"NATO_TADIL_J"` fallback is the last resort when a scenario enables
sharing but the catalog has neither comms bindings nor `LinkCatalog` rows. Prefer seeding a real
link over broadening the fallback.
