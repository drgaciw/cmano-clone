# Sensor modality & IR/Visual detection — developer guide

The tick-4 [detection pipeline](detection-pipeline.md) originally modelled one sensor kind: an
active **radar** that requires an `Active` EMCON state and is degraded by RF noise jammers. S111 /
S112 (DRG-10) adds a per-trial **`SensorModality`** so passive **Infrared** (thermal) and **Visual**
(electro-optical) sensors detect through the *same* deterministic roll but with two behaviours
changed: they **do not fold RF jammers** and they **do not require an active radar emission**. Radar
stays the schema/enum default, so every existing call site and every Baltic v2 replay golden is
unchanged.

This is a thin, additive slice layered on top of the detection loop — it does not touch the `Pd`
formula, the RNG stream, or trial ordering. It changes only *which* jam inputs are folded and
*whether* the EMCON gate applies.

- **Pure source (Sim):** [`Sensors/SensorModality.cs`](../../src/ProjectAegis.Sim/Sensors/SensorModality.cs)
  (the enum), [`Sensors/IrVisualDetection.cs`](../../src/ProjectAegis.Sim/Sensors/IrVisualDetection.cs)
  (optical/thermal env-mask helpers), the `Modality` field on
  [`Scenario/ScenarioDetectionTrial.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioDetectionTrial.cs),
  the modality branch in
  [`Sensors/DeterministicDetectionLoop.cs`](../../src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs),
  and the catalog→trial mapping in
  [`Scenario/DetectionTrialResolver.cs`](../../src/ProjectAegis.Sim/Scenario/DetectionTrialResolver.cs).
- **Catalog source (Data):**
  [`Catalog/CatalogSensorModalities.cs`](../../src/ProjectAegis.Data/Catalog/CatalogSensorModalities.cs)
  (the string constants), the `Modality` column on
  [`Catalog/CatalogSensorBinding.cs`](../../src/ProjectAegis.Data/Catalog/CatalogSensorBinding.cs),
  migration [`016_sensor_modality.sql`](../../assets/data/catalog/migrations/016_sensor_modality.sql),
  and the fixtures in
  [`Catalog/CatalogSeedBootstrap.cs`](../../src/ProjectAegis.Data/Catalog/CatalogSeedBootstrap.cs).
- **Related:** the surrounding tick-4 roll, contact FSM, and `Pd` formula live in the
  [detection pipeline](detection-pipeline.md); catalog seeding is in [catalog-seeding.md](catalog-seeding.md);
  authoring the `catalogDetection` / `detection` JSON is in
  [scenario-policy-authoring.md](scenario-policy-authoring.md).

> **Additive by construction.** Radar is the enum default (`0`), the record-field default, the SQL
> column default (`'Radar'`), and the fallback for every unknown/empty token. Nothing that predates
> modality changes behaviour, so the Baltic v2 replay hash is untouched.

---

## Where it lives

| File | Role |
|------|------|
| [`SensorModality`](../../src/ProjectAegis.Sim/Sensors/SensorModality.cs) | The enum: `Radar = 0`, `Infrared = 1`, `Visual = 2`. Radar folds RF jam + EMCON; IR/Visual use optical/thermal env masks and skip the RF-jam fold. |
| [`IrVisualDetection`](../../src/ProjectAegis.Sim/Sensors/IrVisualDetection.cs) | Pure **env-mask helpers** for IR/EO trials — `ComputeVisualEnvMask` (day/night + weather) and `ComputeInfraredEnvMask` (thermal contrast + weather). |
| [`ScenarioDetectionTrial.Modality`](../../src/ProjectAegis.Sim/Scenario/ScenarioDetectionTrial.cs) | The per-trial modality (default `Radar`), alongside `RequiresActiveRadar`, `JamStrength`, `EnvMask`. |
| [`DeterministicDetectionLoop.RollTick`](../../src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs) | The single line that gates the RF-jam fold on `Modality == Radar`. |
| [`DetectionTrialResolver`](../../src/ProjectAegis.Sim/Scenario/DetectionTrialResolver.cs) | Maps catalog `sensor.modality` onto catalog-sourced trials and clears `RequiresActiveRadar` for IR/Visual. `ParseSensorModality` is the string→enum map. |
| [`CatalogSensorModalities`](../../src/ProjectAegis.Data/Catalog/CatalogSensorModalities.cs) | The three catalog string constants (extend-only; `Radar` is the schema default). |
| [`CatalogSensorBinding.Modality`](../../src/ProjectAegis.Data/Catalog/CatalogSensorBinding.cs) | The sensor-row modality string (default `Radar`). |
| [`016_sensor_modality.sql`](../../assets/data/catalog/migrations/016_sensor_modality.sql) | Additive `ALTER TABLE sensor ADD COLUMN modality TEXT NOT NULL DEFAULT 'Radar'`. |

---

## The enum and what each modality changes

```csharp
public enum SensorModality { Radar = 0, Infrared = 1, Visual = 2 }
```

Modality changes exactly **two** things in the roll; everything else (sort order, `Pd` formula, the
seeded `RngDomain.Detection` draw, the `draw < Pd` test) is identical across modalities:

| Behaviour | `Radar` | `Infrared` / `Visual` |
|-----------|---------|-----------------------|
| RF noise jammers (`ScenarioJamResolver`) | Folded into `jamStrength` | **Not folded** — only the trial's own `JamStrength` is used (optical/IR jam is a separate, default-`0` input) |
| EMCON / active-radar gate (`RequiresActiveRadar`) | Applies as authored | **Cleared to `false`** for catalog-sourced IR/Visual trials (passive sensors don't emit) |
| `Pd` inputs (`basePd`, `envMask`, `eccmFactor`, swarm scale) | Same | Same — the `EnvMask` is just computed differently (see below) |

> The `RequiresActiveRadar` clear happens in the **resolver** for *catalog* trials. Explicitly
> authored `detection` trials keep whatever `Modality` and `RequiresActiveRadar` the scenario set —
> the resolver never rewrites authored trials.

---

## The roll: one gated line (`DeterministicDetectionLoop.RollTick`)

The tick-4 roll is unchanged except for the jam fold. For each sorted, non-skipped trial:

```csharp
// RF ScenarioJamResolver applies only to radar. IR/visual use trial.JamStrength only.
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

So an RF standoff jammer that zeroes a radar contact leaves an *identical* IR trial (same base Pd,
same target) fully detecting. This is the load-bearing test
(`Radar_rf_jam_suppresses_identical_ir_trial_does_not`): a `JamStrength: 1.0` jammer drives the
radar trial's `Pd` to `0` while the IR trial keeps `Pd == 1.0`.

The EMCON gate (`if (trial.RequiresActiveRadar && …ResolveRadar(...) != Active) continue;`) is
untouched in `RollTick`; IR/Visual trials only bypass it because the resolver already set
`RequiresActiveRadar: false` for them.

---

## Environment masks (`IrVisualDetection`)

Radar `EnvMask` is authored/catalog-supplied as before. IR/EO trials get a physically-flavoured mask
from two **pure, stateless** helpers. Feed the result into the trial's `EnvMask`:

| Helper | Formula | Notes |
|--------|---------|-------|
| `ComputeVisualEnvMask(dayFraction, weatherMask = 1.0, nightFloor = 0.05)` | `clamp(dayFraction·weatherMask, 0, 1)`, then raised to at least `nightFloor` | `dayFraction` 0 = night … 1 = full day. `DefaultVisualNightFloor = 0.05` avoids forcing total blackout at night. All args clamped `[0,1]`. |
| `ComputeInfraredEnvMask(thermalContrast, weatherMask = 1.0)` | `clamp(thermalContrast·weatherMask, 0, 1)` | Independent of day/night — thermal sensors see at night. |

```csharp
double dayMask   = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 1.0);   // 1.0
double nightMask = IrVisualDetection.ComputeVisualEnvMask(dayFraction: 0.0);   // 0.05 (night floor)
double irMask    = IrVisualDetection.ComputeInfraredEnvMask(thermalContrast: 0.9, weatherMask: 0.5); // 0.45
```

> **These helpers are not auto-applied.** They are a convenience for scenario authors / spawners to
> precompute `ScenarioDetectionTrial.EnvMask`; the catalog `DetectionTrialResolver` path uses the
> catalog/authored `EnvMask` (via `PhaseBCatalogDetectionModifier.Apply`), **not** these functions.
> As of S112 they are referenced only by tests and by whatever content code chooses to call them —
> there is no day/night clock feeding them inside the sim loop yet. Document a scenario's day
> fraction / thermal contrast where you set the trial up.

---

## Catalog side (S111-02)

Sensors carry their modality as a **string** so the catalog stays engine-agnostic and extend-only.

- **Constants** ([`CatalogSensorModalities`](../../src/ProjectAegis.Data/Catalog/CatalogSensorModalities.cs)):
  `"Radar"`, `"Infrared"`, `"Visual"`. `Radar` is the schema default; add new tokens here, never
  reuse an existing one for a different meaning.
- **Row field** ([`CatalogSensorBinding.Modality`](../../src/ProjectAegis.Data/Catalog/CatalogSensorBinding.cs)):
  defaults to `CatalogSensorModalities.Radar`, so in-memory fixtures and older code get radar
  automatically.
- **Migration** ([`016_sensor_modality.sql`](../../assets/data/catalog/migrations/016_sensor_modality.sql)):
  a single additive `ALTER TABLE sensor ADD COLUMN modality TEXT NOT NULL DEFAULT 'Radar'`. It is
  idempotent — `SqliteCatalogReader.ShouldSkipMigration` skips it when the file id contains `016`
  *and* the `sensor.modality` column already exists, so re-opening a DB does not re-`ALTER`.
- **Reader tolerance** (`SqliteCatalogReader`): reads work against DBs both with and without the
  column (a `hasModality` switch picks the SELECT). A present-but-null/empty value is normalised to
  `Radar` (`NormalizeModality`). Deterministic reads still `ORDER BY platform_id, sensor_id`.

### Seeded fixtures (`CatalogSeedBootstrap.SeedSensorModalityFixtures`)

The Baltic seed adds IR/Visual sensors so the modality path has coverage without new scenarios
(idempotent `INSERT OR REPLACE`, guarded on the column existing):

| Platform | Sensor id | `basePd` | Modality |
|----------|-----------|----------|----------|
| `u1` | `fixture-ir-1` | `0.80` | `Infrared` |
| `u1` | `fixture-visual-1` | `0.70` | `Visual` |
| (any) | `internal-ir` | — | `Infrared` (tags the existing Baltic v3 UCAV recon sensor via `UPDATE`) |

In-memory fixtures mirror this: `InMemoryCatalogReader.BalticPatrolFixture()` sensors default to
`Radar`; `BalticV3Fixture()` tags `ucav-blue/internal-ir` as `Infrared`.

---

## Catalog → trial mapping (`DetectionTrialResolver`, S112-C residual)

`DetectionTrialResolver.Resolve(profile, catalog)` decides each catalog trial's modality:

1. **Authored trials win.** If `profile.DetectionTrials` is non-empty they are returned as-is — *no
   modality rewrite*. (Explicit `detection` JSON keeps its authored `Modality`/`RequiresActiveRadar`.)
2. **Catalog targets** (`profile.CatalogDetectionTargets`) are turned into trials in Ordinal
   `Observer → Sensor → Target` order. For each:
   - Resolve `basePd` (throws if the catalog has none) and the Phase-B env modifier.
   - Look up the `(platformId, sensorId)` sensor binding and map its string via
     `ParseSensorModality(binding.Modality)`; a missing binding defaults to `Radar`.
   - For `Infrared`/`Visual`, force `RequiresActiveRadar: false`; radar keeps the target's authored
     value.

`ParseSensorModality(string?)` is the single case-insensitive map (unknown/empty → `Radar`):

| Input (any case) | Result |
|------------------|--------|
| `null`, `""`, whitespace | `Radar` |
| `Radar` | `Radar` |
| `Infrared` | `Infrared` |
| `Visual` | `Visual` |
| any other token (e.g. `"unknown-mod"`) | `Radar` |

Worked example (from `DetectionTrialResolverTests`): a catalog target on `ucav-blue/internal-ir`
resolves to `Modality == Infrared`, `RequiresActiveRadar == false`, `basePd == 0.85`; the same
platform's `recon-radar` sensor stays `Radar` with `RequiresActiveRadar == true`.

---

## Determinism & safety notes

- **No new draws, no reordering.** Modality changes neither the sort key (`Observer → Sensor →
  Target`) nor the per-trial `SeededRng.UnitFloat(seed, RngDomain.Detection, entityId, simTick,
  drawIndex)` stream — `drawIndex` still increments over every sorted, non-skipped trial regardless
  of modality. Mixed-modality rolls are pinned bit-for-bit
  (`Mixed_modality_rolls_are_deterministic_for_same_seed`).
- **Radar path unchanged → v2 goldens safe.** Radar trials still fold RF jam and honour EMCON, and
  the fixtures/migration are purely additive, so the Baltic v2 replay hash `17144800277401907079`
  does not move.
- **`Pd` formula is shared.** Modality never edits `DetectionProbability.ComputePd`; only the
  `jamStrength` input differs (RF fold vs. trial-only).
- **Fail-safe strings.** Any unrecognised or null catalog modality resolves to `Radar` rather than
  throwing — a mistyped column degrades to radar behaviour, not a crash.
- **Extend-only catalog.** Add modalities by appending a `CatalogSensorModalities` constant and a
  `ParseSensorModality` branch; never repurpose an existing token or reorder the enum (it is
  persisted as a string, but the enum's `int` values back the trial default).

---

## Common pitfalls

- **Expecting IR to be jammed by an RF jammer.** It isn't — RF `ScenarioJammer`s only fold into
  radar trials. Model optical/IR degradation through the trial's own `JamStrength` or a lower
  `EnvMask`.
- **Expecting the env-mask helpers to run automatically.** `IrVisualDetection.*` are pure helpers
  you call when *building* a trial's `EnvMask`; the resolver does not invoke them. There is no
  day/night clock wired into the loop.
- **Authoring an IR trial with `RequiresActiveRadar: true` via explicit `detection` JSON.** The
  resolver only clears the flag for *catalog* trials; authored trials keep your value, so an authored
  IR trial left `true` will be EMCON-gated.
- **Adding a modality token only in the catalog.** Wire it into `CatalogSensorModalities` *and*
  `ParseSensorModality` (and the enum), or it silently maps to `Radar`.
- **Assuming the reader needs the column.** It doesn't — reads tolerate legacy DBs; but a *write*
  path that inserts sensors must include `modality` (fixtures set it explicitly).

---

## Tests

| Test file | Covers |
|-----------|--------|
| [`IrVisualDetectionTests`](../../src/ProjectAegis.Sim.Tests/Sensors/IrVisualDetectionTests.cs) | Visual day > night + night floor, weather clamping, IR thermal-contrast mask, RF-jam suppresses radar but not an identical IR/Visual trial, mixed-modality determinism. |
| [`DetectionTrialResolverTests`](../../src/ProjectAegis.Sim.Tests/Scenario/DetectionTrialResolverTests.cs) | Catalog→trial modality mapping, IR clears `RequiresActiveRadar`, radar stays gated, authored-trial precedence, full `ParseSensorModality` string table. |
| [`SensorModalityCatalogTests`](../../src/ProjectAegis.Data.Tests/Catalog/SensorModalityCatalogTests.cs) | Migration 016 add-column + safe re-skip, seeded IR/Visual fixtures + radar default, in-memory fixture modality, null-modality → `Radar`. |

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -v minimal
```

---

## See also

| Topic | Doc |
|-------|-----|
| The tick-4 roll, contact FSM, `Pd` formula, and datalink merge this plugs into | [detection-pipeline.md](detection-pipeline.md) |
| How catalog fixtures / seeds reach a headless run | [catalog-seeding.md](catalog-seeding.md) |
| Authoring `catalogDetection` / `detection` / `jammers` / `emcon` JSON | [scenario-policy-authoring.md](scenario-policy-authoring.md) |
| Seeded RNG domains and the replay golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| The `Sensors/` folder in the wider simulation core | [`ProjectAegis.Sim/README.md`](../../src/ProjectAegis.Sim/README.md) |
