# Validation — deterministic scenario & catalog validation

Two related but distinct validation surfaces live here:

1. **Scenario validation** ([ADR-008](../../../docs/architecture/adr-008-mission-editor-validation-engine.md)) —
   the `ScenarioValidationEngine` checks an authored scenario against the catalog
   and produces a deterministic, hashable `ValidationReport`. This is what gates
   scenario export in the mission editor (MCP `scenario_validate`).
2. **Catalog validation** ([requirement 06](../../../Game-Requirements/requirements/06-Database-Intelligence.md)) —
   the `ValidationPipeline` is a thin entry point that delegates to the headless
   [`DatabaseIntelligenceOrchestrator`](../Agents) agent pipeline to check the
   catalog itself (entity resolution, rules, consistency, diff).

Both are **pure functions of their inputs** — no wall-clock, no LLM, no network —
so identical inputs always produce identical findings and report hashes. CI pins
golden hashes (`ValidationGoldenHashes`); see [Determinism](#determinism).

## Scenario validation engine

`ScenarioValidationEngine.Validate(scenario, catalog, config)` runs a fixed,
ordered set of rules and returns a sorted, hashed `ValidationReport`:

```csharp
var engine = new ScenarioValidationEngine();
ValidationReport report = engine.Validate(scenario, catalog, new ValidationConfig());

if (!report.Passed)                       // any finding >= Error
    foreach (var f in report.Findings)
        Console.Error.WriteLine($"{f.Severity} {f.Code} {f.Message}");
```

### Rule catalog

Each rule appends `ValidationFinding`s to a shared sink. Rules that need geometry
or platform stats read them through `ICatalogReader`.

| Code | Severity | Trigger |
|------|----------|---------|
| `DB_MISMATCH` | Error | Scenario `dbRef` / `dbSnapshotId` does not resolve to an available catalog snapshot. |
| `MISSION_NO_UNITS` | Error | A mission has no assigned units. |
| `PATROL_ZONE_DEGENERATE` | Error | A `Patrol` mission has fewer than 3 waypoints. |
| `STRIKE_NO_TARGETS` | Error | A `Strike` mission has no targets. |
| `FERRY_NO_DESTINATION` | Error | A `Ferry` mission has no destination base. |
| `AIR_NOT_READY` | Error | An air unit is launched before it is ready. |
| `STRIKE_INVALID_PLATFORM` | Error | A strike unit has an invalid/missing `combat_radius_nm`. |
| `STRIKE_UNREACHABLE` | Error | Target lies beyond the unit's combat radius. |
| `STRIKE_UNREACHABLE_FUEL` | Error | Target is within radius but exceeds the fuel budget (`radius × fuelFraction − pad`). |
| `FERRY_UNREACHABLE` / `FERRY_UNREACHABLE_FUEL` | Error | Ferry destination is out of combat radius / fuel range. |

Reachability uses `ReachabilityCalculator`: great-circle distance via
`HaversineNm` (Earth radius `3440.065` nm) compared to a fuel budget of
`combatRadiusNm × FuelFraction − IngressEgressPadNm`. A target beyond the raw
combat radius classifies as `*_UNREACHABLE`; within radius but over budget is
`*_UNREACHABLE_FUEL`.

### Tuning knobs (`ValidationConfig`)

| Field | Default | Meaning |
|-------|---------|---------|
| `IngressEgressPadNm` | `50` | Distance reserved for ingress/egress before the fuel budget. |
| `FuelFraction` | `0.85` | Fraction of combat radius usable for the round trip (GDD §4.1). |
| `ExportBlockSeverityFloor` | `Error` | Findings at or above this severity block export. |

`ValidationSeverity` is `Info (0) < Warning (1) < Error (2)`.

### Report shape & export gate

`ValidationReport` is a record `(bool Passed, IReadOnlyList<ValidationFinding> Findings, string ReportHash)`:

- `Passed` is true when no finding is `>= Error`.
- `CanExport(config)` is true when no finding is `>= ExportBlockSeverityFloor`.
  This is the export decision — distinct from `Passed` so the floor can be tuned
  without changing pass/fail semantics.
- Findings are sorted (severity desc, then `Code`, `MissionId`, `UnitId`,
  `TargetId`, `Message`, all ordinal) before hashing.
- `ReportHash` is a lowercase SHA-256 hex over the sorted findings, so the hash
  is stable regardless of the order rules emit findings.

`ScenarioValidationExportGate.EvaluateExport(scenario, catalog, config?)` is the
headless façade that wires this for MCP/CLI (`TR-editor-005`): it runs the engine
and returns `(bool Allowed, ValidationReport Report)`. The mission-editor CLI verb
`scenario_validate` calls it and maps a blocking report to a non-zero exit code —
see [`ScenarioValidateCommand`](../../ProjectAegis.MissionEditor.Cli/ScenarioValidateCommand.cs)
and the [CLI verb reference](../../ProjectAegis.MissionEditor.Cli/README.md).

## Catalog validation pipeline

`ValidationPipeline.Run(catalog, databasePath?)` delegates to the
`DatabaseIntelligenceOrchestrator`, which runs the deterministic agent chain
(entity resolution → rules → consistency → diff). `ValidationPipeline.RunBalticDefault()`
resolves the seeded Baltic catalog reader (falling back to the in-memory fixture)
for CI smoke runs. See [`Agents/`](../Agents).

## Determinism

- No `DateTime.UtcNow`, no RNG, no I/O in the rule path — findings depend only on
  the scenario, catalog, and config.
- Findings are sorted before hashing, and the hash uses ordinal comparisons and
  invariant formatting, so `ReportHash` is reproducible across machines.
- `ValidationGoldenHashes` pins report hashes for the CI golden scenarios
  (`StrikeUnreachable`, `CleanPatrol`). **If you change a rule, the finding set,
  or the hash layout, re-run the golden test and update the pinned constant** —
  do not silently edit the hash.

## Tests

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter Validation -v minimal
```

`src/ProjectAegis.Data.Tests/Validation/` covers the engine
(`ScenarioValidationEngineTests`), reachability math
(`ReachabilityCalculatorTests`), logistics rules
(`LogisticsValidationRulesTests`), the catalog pipeline
(`ValidationPipelineTests`), and the pinned goldens (`ValidationGoldenTests`).

## Adding a rule

1. Add the check to `Rules/ValidationRules.cs` (emit `ValidationFinding`s into the
   sink; read catalog data through `ICatalogReader`, never directly from SQLite).
2. Call it from `ScenarioValidationEngine.Validate` in a fixed position.
3. Add or update a golden scenario fixture and re-pin `ValidationGoldenHashes`.

## See also

- [ProjectAegis.Data overview](../README.md)
- [ADR-008 — mission-editor validation engine](../../../docs/architecture/adr-008-mission-editor-validation-engine.md)
- [WriteGate — staged catalog writes](../WriteGate/README.md)
- [MissionEditor.Cli — `scenario_validate`](../../ProjectAegis.MissionEditor.Cli/README.md)
