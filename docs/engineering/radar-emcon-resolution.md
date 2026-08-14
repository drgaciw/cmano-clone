# Radar EMCON resolution — developer guide

Two different subsystems need to know one thing about a unit before they act: **is its radar
emitting?** The detection loop skips active-radar trials for a silent emitter; the engage kill-chain
aborts a shot that needs an active fire-control radar it isn't running. Both ask the same small
resolver. This page documents that seam — how a unit's **radar EMCON state** is resolved from
scenario overrides with a catalog-posture fallback, and where the answer is consumed.

It is a **pure, read-only** lookup (no per-tick state, no RNG, no clock), evaluated wherever a
consumer needs the current posture. Governed by **req-21 Phase B** (catalog EMCON posture) and
**[ADR-006](../architecture/adr-006-data-layer-boundary.md)** (the sim reads the catalog through
`ICatalogReader` and never mutates it). The whole thing is **additive-only**: a scenario without an
`emcon` override and a catalog without an EMCON row both resolve to `EmconState.Active`, so legacy
Baltic fixtures behave exactly as before.

- **Source:** the resolver pair in
  [`src/ProjectAegis.Sim/Catalog/CatalogRadarEmconResolver.cs`](../../src/ProjectAegis.Sim/Catalog/CatalogRadarEmconResolver.cs)
  (posture math) and its thin facade
  [`src/ProjectAegis.Sim/Scenario/ScenarioEmconResolver.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioEmconResolver.cs);
  the `EmconState` enum in
  [`src/ProjectAegis.Sim/Policy/EmconState.cs`](../../src/ProjectAegis.Sim/Policy/EmconState.cs);
  the catalog row in
  [`src/ProjectAegis.Data/Catalog/CatalogEmcon.cs`](../../src/ProjectAegis.Data/Catalog/CatalogEmcon.cs).
- **Related:** the **detection** EMCON gate that consumes it is in
  [`detection-pipeline.md`](detection-pipeline.md); the **engage** `EmconOff` abort is in
  [`engagement-pipeline.md`](engagement-pipeline.md); the readiness evaluator that re-exposes it
  (`ReadinessPolicyEvaluator.EvaluateRadarEmcon`) is a sibling of the HP path in
  [`catalog-damage-readiness-runtime.md`](catalog-damage-readiness-runtime.md); authoring the
  scenario `emcon` block is in [`scenario-policy-authoring.md`](scenario-policy-authoring.md); the
  read-model that surfaces it (`SensorC2Snapshot.ObserverRadarEmconActive`) is in
  [`c2-projection-layer.md`](c2-projection-layer.md); determinism rules are in
  [`determinism-and-replay.md`](determinism-and-replay.md).

---

## The resolution order

`ScenarioEmconResolver.ResolveRadar(unitId, unitRadarEmcon, catalog?, condition, emitterId)` is a
one-line delegate to `CatalogRadarEmconResolver.ResolveRadar`, which applies a strict three-step
precedence and returns an `EmconState`:

1. **Scenario override wins.** If `unitRadarEmcon` (the profile's `UnitRadarEmcon` map, authored via
   the scenario `emcon` block) contains `unitId`, return that `EmconState` verbatim. The catalog is
   never consulted for a unit the scenario pins.
2. **Catalog posture fallback.** Otherwise, if a `catalog` is supplied and
   `catalog.TryGetEmcon(unitId, condition, emitterId, out var emcon)` finds a row, map its posture
   string to an `EmconState` (below). The default query uses `condition = "free"` and
   `emitterId = "radar-1"`.
3. **Default active.** If neither source answers, return `EmconState.Active` — a unit with no EMCON
   data is assumed to be emitting (the permissive, legacy-compatible default).

```csharp
public enum EmconState { Off = 0, Passive = 1, Active = 2 }
```

### Posture string → `EmconState` (`MapPosture`)

| Catalog `Posture` (any casing) | `EmconState` |
|--------------------------------|--------------|
| `off` | `Off` |
| `standby` | `Passive` |
| `active` | `Active` |
| anything else / null | `Active` |

`MapPosture` trims and lower-cases (`Trim().ToLowerInvariant()`) before matching. This casing
tolerance is **load-bearing** and deliberate: `PlatformWorkbookValidator.AllowedEmconPostures`
accepts `off` / `standby` / `active` case-insensitively (`OrdinalIgnoreCase`), so a validly-authored
`"Off"` or `"Standby"` posture **must** map here too. If this switch were case-sensitive, an
`"Off"` posture would silently fall through to the `Active` default and defeat EMCON discipline for
both detection and the engage gate — the exact failure the in-code comment warns about. When adding a
posture keyword, keep the two lists in sync.

> The [`CatalogEmcon`](../../src/ProjectAegis.Data/Catalog/CatalogEmcon.cs) row itself defaults to
> `Condition = "silent"`, `Posture = "off"`; the resolver's default *query* uses `condition = "free"`,
> `emitterId = "radar-1"`. A catalog row is only matched when its `(PlatformId, Condition, EmitterId)`
> triple matches the query — otherwise resolution falls through to `Active`.

---

## Who consumes the resolved state

The same resolver answers three consumers, so EMCON is applied consistently across the picture and
the kill-chain:

| Consumer | Effect |
|----------|--------|
| **Detection** — [`DeterministicDetectionLoop`](../../src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs) / [`ScenarioContactSimulator`](../../src/ProjectAegis.Sim/Sensors/ScenarioContactSimulator.cs) | A trial with `RequiresActiveRadar` is **skipped** when `ResolveRadar(observerId, …) != Active`. (IR/Visual modality trials set `RequiresActiveRadar = false`, so they are never EMCON-gated — see [detection-pipeline.md](detection-pipeline.md).) |
| **Engage** — [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) → [`MvpEngagementResolver`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs) | When priming a shot, `EngageContext.RadarEmconActive = state.RadarEmconActive && ResolveRadar(shooterId, …) == Active`. The resolver's EMCON gate then aborts with `EmconOff` when `!RadarEmconActive` (the sim `PolicyEvaluator` can also raise `EmconOff` at the ROE layer — see the [engage gate chain](engagement-pipeline.md#the-gate-chain-exact-order)). |
| **Readiness** — [`ReadinessPolicyEvaluator.EvaluateRadarEmcon`](../../src/ProjectAegis.Sim/Policy/ReadinessPolicyEvaluator.cs) | Re-exposes `ResolveRadar` for policy/readiness callers, alongside `EvaluateMagazine` / `EvaluateMobility` / `EvaluateUnit`. |

The resolved posture is also surfaced to the UI read-model as
`SensorC2Snapshot.ObserverRadarEmconActive` (see [c2-projection-layer.md](c2-projection-layer.md)).

---

## Determinism rules

- **Pure & read-only.** Both methods are static, take `ICatalogReader` (never mutate it), and hold no
  state — the same inputs always yield the same `EmconState`.
- **Scenario-wins precedence is total.** A scenario override short-circuits the catalog entirely, so
  fixtures pin EMCON deterministically regardless of catalog contents.
- **Additive-only.** No scenario override + no catalog row → `Active`, preserving the Baltic v2 hash
  (`17144800277401907079`). Introducing a catalog EMCON row for a previously-defaulted unit *will*
  change detection/engage outcomes for that unit — treat it as a golden-moving data change.
- **Keep casing tolerant.** Any change to `MapPosture` must stay case-insensitive and mirror
  `PlatformWorkbookValidator.AllowedEmconPostures`.

---

## Extending it without breaking goldens

- **New posture keyword** — add it to both `MapPosture` and
  `PlatformWorkbookValidator.AllowedEmconPostures`; pick the `EmconState` deliberately (`Passive`
  hides from active-radar trials but is not fully dark).
- **New consumer** — call `ScenarioEmconResolver.ResolveRadar` (or `EvaluateRadarEmcon` for the
  policy path) rather than reading catalog EMCON rows directly, so every consumer honours the
  scenario-override precedence identically.
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
| [`PhaseBCatalogConsumerTests`](../../src/ProjectAegis.Sim.Tests/Catalog/PhaseBCatalogConsumerTests.cs) | Catalog-posture → `EmconState` mapping and the scenario-override precedence. |
| [`ScenarioContactEmconTests`](../../src/ProjectAegis.Sim.Tests/Sensors/ScenarioContactEmconTests.cs) | The detection EMCON gate skipping active-radar trials for a non-`Active` observer. |
| [`MvpEngagementResolverTests`](../../src/ProjectAegis.Sim.Tests/Engage/MvpEngagementResolverTests.cs) | The engage `EmconOff` abort when `RadarEmconActive` is false. |
