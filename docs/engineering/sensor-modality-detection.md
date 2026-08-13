# Sensor modality & IR/Visual detection — developer guide

Detection trials carry a **sensor modality** — `Radar`, `Infrared`, or `Visual` (S111 / S112 /
DRG-10). Modality does *not* change the `Pd` formula or the seeded RNG draw; it changes **which
gates apply** to a trial:

- **Radar** folds in RF noise-jamming (`ScenarioJamResolver`) and normally requires the observer's
  radar EMCON to be `Active`.
- **Infrared / Visual (EO)** are *passive*: they ignore RF jammers (they only see the per-trial
  `JamStrength`, which defaults to `0`) and, when resolved from the catalog, do **not** require
  active radar emission.

This page documents that modality layer end-to-end: the enum + catalog strings, the optical/thermal
env-mask helpers, the one RF-jam branch in the roll loop, how the trial resolver maps catalog sensor
rows onto trials, and the extend-only catalog schema (migration 016). It is a focused companion to
the [detection / contact pipeline](detection-pipeline.md) guide — read that first for the `Pd`
formula, the contact-lifecycle FSM, the detection sub-hash, and the RNG rules; this page only covers
what modality adds on top. Verified against source and pinned by the tests listed at the end.

- **Modality enum:** [`SensorModality`](../../src/ProjectAegis.Sim/Sensors/SensorModality.cs) —
  `Radar = 0`, `Infrared = 1`, `Visual = 2`.
- **Catalog strings:** [`CatalogSensorModalities`](../../src/ProjectAegis.Data/Catalog/CatalogSensorModalities.cs) —
  `"Radar"` / `"Infrared"` / `"Visual"` (Radar is the schema default).
- **Env-mask helpers:** [`IrVisualDetection`](../../src/ProjectAegis.Sim/Sensors/IrVisualDetection.cs) —
  `ComputeVisualEnvMask` / `ComputeInfraredEnvMask`.
- **Roll loop:** [`DeterministicDetectionLoop.RollTick`](../../src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs)
  (the one `Modality == Radar` RF-jam branch).
- **Trial input:** [`ScenarioDetectionTrial.Modality`](../../src/ProjectAegis.Sim/Scenario/ScenarioDetectionTrial.cs)
  (default `Radar`).
- **Catalog → trial mapping:** [`DetectionTrialResolver`](../../src/ProjectAegis.Sim/Scenario/DetectionTrialResolver.cs)
  (`Resolve` + `ParseSensorModality`).
- **Catalog schema:** [`CatalogSensorBinding.Modality`](../../src/ProjectAegis.Data/Catalog/CatalogSensorBinding.cs)
  + migration [`016_sensor_modality.sql`](../../assets/data/catalog/migrations/016_sensor_modality.sql).

---

## Design invariants — never break these

Load-bearing and enforced by tests. Preserve them when touching any piece here.

| Invariant | Rule |
|-----------|------|
| **Radar is the default everywhere** | `SensorModality.Radar` is the default on `ScenarioDetectionTrial`, `CatalogSensorBinding`, the SQL column (`DEFAULT 'Radar'`), and every "unknown / empty" parse. This is what keeps every pre-modality call site — and the **Baltic v2 replay golden hash `17144800277401907079`** — unchanged. Never make a non-Radar value the fallback. |
| **Modality never touches the RNG coordinates** | The draw is still `SeededRng.UnitFloat(seed, RngDomain.Detection, entityId, simTick, drawIndex++)` with `entityId = FNV(observer, sensor, target)`. Modality only decides whether RF jam is folded into `Pd`. Two trials that differ *only* in modality draw the same random number for the same coordinates. |
| **`Pd` formula is shared & unchanged** | All modalities go through the same [`DetectionProbability.ComputePd`](../../src/ProjectAegis.Sim/Sensors/DetectionProbability.cs): `clamp(basePd · envMask · eccm · (1 − jam) · swarm, 0, 1)`. IR/Visual differ only in that `jam` is the per-trial `JamStrength` (not the RF resolver's). |
| **Catalog column is extend-only** | Migration 016 is a single additive `ALTER TABLE sensor ADD COLUMN modality TEXT NOT NULL DEFAULT 'Radar'`. It is idempotent (skipped when the column already exists) and does **not** change the deterministic `ORDER BY platform_id, sensor_id` read order. Follow [ADR-006 / the write gate](catalog-write-gate.md) extend-only rules. |
| **Authored trials win as-authored** | When a scenario profile supplies inline `DetectionTrials`, `DetectionTrialResolver` returns them verbatim — it never rewrites their modality or `RequiresActiveRadar` from the catalog. Catalog modality mapping applies **only** to trials built from `CatalogDetectionTargets`. |

---

## The modality enum & catalog strings

```csharp
public enum SensorModality { Radar = 0, Infrared = 1, Visual = 2 }

public static class CatalogSensorModalities
{
    public const string Radar    = "Radar";
    public const string Infrared = "Infrared";
    public const string Visual   = "Visual";
}
```

The enum lives in the sim; the strings live in the data layer (they are the on-disk catalog
values). The bridge between them is `DetectionTrialResolver.ParseSensorModality`, which is
**case-insensitive** and **fail-safe to Radar**:

| Catalog string (any case) | Parsed modality |
|---------------------------|-----------------|
| `null`, `""`, whitespace  | `Radar` |
| `"Radar"` / `"radar"`     | `Radar` |
| `"Infrared"` / `"infrared"` / `"INFRARED"` | `Infrared` |
| `"Visual"` / `"visual"`   | `Visual` |
| any other token (e.g. `"lidar"`) | `Radar` |

Because unknown tokens fall back to Radar, a typo in a catalog row degrades to the (jam-gated,
EMCON-gated) radar path rather than throwing.

---

## Env-mask helpers (`IrVisualDetection`)

Optical and thermal sensors are attenuated by different conditions than radar. `IrVisualDetection`
provides two **pure helper functions** that produce an `envMask` in `[0, 1]` for the `Pd` formula:

```csharp
// Visual/EO: clamp(dayFraction) · clamp(weatherMask), floored so night is not a total blackout.
public static double ComputeVisualEnvMask(
    double dayFraction /* 0 = night … 1 = full day */,
    double weatherMask = 1.0,
    double nightFloor  = DefaultVisualNightFloor /* 0.05 */);

// Infrared: clamp(thermalContrast) · clamp(weatherMask). No day/night term.
public static double ComputeInfraredEnvMask(
    double thermalContrast /* 0..1 */,
    double weatherMask = 1.0);
```

- **Visual** scales with daylight and weather, then is raised to at least `nightFloor` (default
  `0.05`) so a fully dark scene still allows a small residual chance rather than a forced zero.
  `ComputeVisualEnvMask(1.0)` → `1.0`; `ComputeVisualEnvMask(0.0)` → `0.05`.
- **Infrared** is a function of thermal contrast and weather only — it is **independent of day /
  night** (a hot target is hot at 02:00). `ComputeInfraredEnvMask(0.8, weatherMask: 0.5)` → `0.4`.

> **These helpers are authoring/spawn-time utilities, not auto-wired.** `RollTick` and
> `DetectionTrialResolver` consume whatever `EnvMask` a trial already carries; there is **no**
> day/night model inside the tick loop. A scenario, spawner, or test that wants day/night or thermal
> behaviour calls these helpers when it builds the trial's `EnvMask`. Radar continues to use the
> authored `ScenarioDetectionTrial.EnvMask` as-is.

---

## The one modality branch in the roll loop

`DeterministicDetectionLoop.RollTick` is otherwise the pipeline described in
[detection-pipeline.md](detection-pipeline.md). Modality adds exactly one branch — the RF-jam fold:

```csharp
// RF ScenarioJamResolver applies only to radar. IR/visual use trial.JamStrength only
// (optical/IR jam is separate; default 0).
var jamStrength = trial.JamStrength;
if (trial.Modality == SensorModality.Radar && jammers is { Count: > 0 })
{
    jamStrength = Math.Max(
        jamStrength,
        ScenarioJamResolver.ResolveJam(trial.ObserverId, trial.TargetId, simTick, jammers));
}

var pd = DetectionProbability.ComputePd(
    trial.BasePd, trial.EnvMask, trial.EccmFactor,
    jamStrength: jamStrength, swarmIntegrityScale: trial.SwarmIntegrityScale);
```

Concretely, for an active RF jammer that fully suppresses a target (`jamStrength → 1`):

- a **Radar** trial gets `Pd = 0` (jammed out), while
- an otherwise-identical **Infrared / Visual** trial keeps its full `Pd` (RF jam ignored).

The `RequiresActiveRadar` EMCON gate (skip the trial when the observer's radar EMCON is not
`Active`) is orthogonal to this branch — see the next section for how catalog-resolved IR/Visual
trials get that flag cleared.

---

## Catalog → trial mapping (`DetectionTrialResolver`)

`DetectionTrialResolver.Resolve` decides where each tick's trials come from:

1. **Inline `profile.DetectionTrials` present** → returned **verbatim** (modality and
   `RequiresActiveRadar` left exactly as authored — see the "authored trials win" invariant).
2. **Otherwise, from `profile.CatalogDetectionTargets`** → for each target it looks up the catalog
   `basePd`, applies the Phase-B modifier, and maps the catalog sensor row's `Modality`:

```csharp
var modality = SensorModality.Radar;
if (bindingByKey.TryGetValue((target.ObserverId, target.SensorId), out var sensorBinding))
{
    modality = ParseSensorModality(sensorBinding.Modality);
}

// Optical / IR sensors do not require active radar emission.
var requiresActiveRadar = modality is SensorModality.Infrared or SensorModality.Visual
    ? false
    : target.RequiresActiveRadar;
```

So a catalog **IR/Visual** binding produces a passive trial (`RequiresActiveRadar = false`), while a
catalog **Radar** binding preserves the target's authored `RequiresActiveRadar`. A missing binding
falls back to `Radar` with the authored flag.

---

## Catalog schema & reader

The `sensor` table gained one extend-only column:

```sql
-- assets/data/catalog/migrations/016_sensor_modality.sql
ALTER TABLE sensor ADD COLUMN modality TEXT NOT NULL DEFAULT 'Radar';
```

- **Idempotent.** `SqliteCatalogReader.ShouldSkipMigration` skips `016` when `sensor.modality`
  already exists, so re-opening a migrated DB never re-`ALTER`s.
- **Back-compat reads.** The reader only selects `modality` when the column is present, and
  `NormalizeModality` maps `null` / whitespace → `"Radar"`. A legacy row inserted with an empty
  modality string reads back as `Radar`.
- **Deterministic order preserved.** Reads still `ORDER BY platform_id, sensor_id`; the new column
  does not participate in ordering.

**Seed / fixture coverage** (so headless runs and tests exercise all three modalities):

| Source | Row | `basePd` | Modality |
|--------|-----|---------|----------|
| `CatalogSeedBootstrap.SeedBalticPatrol` | `u1` / `fixture-ir-1` | `0.80` | Infrared |
| `CatalogSeedBootstrap.SeedBalticPatrol` | `u1` / `fixture-visual-1` | `0.70` | Visual |
| `CatalogSeedBootstrap.SeedBalticPatrol` | `u1` / `radar-1` | — | Radar (default) |
| `InMemoryCatalogReader.BalticV3Fixture` | `ucav-blue` / `internal-ir` | `0.85` | Infrared |
| `InMemoryCatalogReader.BalticV3Fixture` | `ucav-blue` / `recon-radar` | `0.70` | Radar |

The seed also tags any existing `internal-ir` sensor row `Infrared` for the Baltic v3 seed path.

---

## Adding a modality or an IR/Visual sensor

- **Tag an existing catalog sensor IR/Visual.** Set its `modality` via the extend-only
  [write gate](catalog-write-gate.md) / a fixture (`CatalogSensorModalities.Infrared` /
  `.Visual`). No sim change is needed — the resolver picks it up and clears `RequiresActiveRadar`.
- **Author an inline IR/Visual trial.** Construct `ScenarioDetectionTrial` with
  `Modality: SensorModality.Infrared|Visual`, set `RequiresActiveRadar: false` if the sensor is
  passive, and compute `EnvMask` with `IrVisualDetection.ComputeInfraredEnvMask` /
  `ComputeVisualEnvMask`. Inline trials are used verbatim.
- **Add a brand-new modality value.** Extend the `SensorModality` enum (append; do not renumber
  existing members), add the matching string to `CatalogSensorModalities`, teach
  `ParseSensorModality` the new token, and decide its gate behaviour in `RollTick` (RF jam? EMCON?).
  Keep `Radar` the fallback so existing rows and the v2 hash are untouched, and add a fixture + a
  `ParseSensorModality` theory row.

**Pitfalls**

- Don't expect IR/Visual to be dimmed by RF jammers — they aren't; only per-trial `JamStrength`
  affects them.
- Don't assume the tick loop applies day/night or thermal attenuation automatically — it uses the
  trial's `EnvMask`; call the `IrVisualDetection` helpers when you build the trial.
- Don't renumber the `SensorModality` enum or change the SQL default off `Radar` — both are on-disk
  / golden contracts.

---

## Tests that pin this

| Test | What it locks |
|------|---------------|
| [`IrVisualDetectionTests`](../../src/ProjectAegis.Sim.Tests/Sensors/IrVisualDetectionTests.cs) | Visual day > night + night floor, weather/clamp behaviour, infrared thermal-contrast + weather; RF jam suppresses a Radar trial but **not** an identical IR/Visual trial; mixed-modality rolls are deterministic for the same seed. |
| [`DetectionTrialResolverTests`](../../src/ProjectAegis.Sim.Tests/Scenario/DetectionTrialResolverTests.cs) | Catalog Radar stays Radar (keeps `RequiresActiveRadar`); catalog IR maps modality **and** clears `RequiresActiveRadar`; inline trials keep authored modality/flag; `ParseSensorModality` string table (case-insensitive, unknown → Radar). |
| [`SensorModalityCatalogTests`](../../src/ProjectAegis.Data.Tests/Catalog/SensorModalityCatalogTests.cs) | Migration 016 adds the column and is safe to re-run; seed exposes IR/Visual fixtures with Radar default; in-memory default is Radar and v3 IR is tagged; a null/empty modality reads back as Radar. |

---

## Related

- [detection-pipeline.md](detection-pipeline.md) — the full tick-4 detection slice this layer sits inside (Pd, contact FSM, detection sub-hash, RNG rules).
- [scenario-policy-authoring.md](scenario-policy-authoring.md) — authoring the `detection` / `catalogDetection` / `jammers` / `emcon` JSON inputs.
- [determinism-and-replay.md](determinism-and-replay.md) — determinism rules and the golden-fixture workflow.
- [catalog-write-gate.md](catalog-write-gate.md) — the extend-only propose/approve path for catalog sensor rows.
