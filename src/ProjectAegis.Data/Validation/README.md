# Scenario Validation (export gate)

`ProjectAegis.Data.Validation` — the **deterministic rule engine** that decides
whether a scenario is safe to export/play. It runs a fixed set of structural and
reachability rules over a `ScenarioDocumentDto` + `ICatalogReader`, sorts the
findings into a stable order, and produces a `ValidationReport` with a content hash
and an `Error`-severity export block.

**Requirement trace:** ADR-008 (deterministic v1 validation), TR-editor-005
(headless `scenario_validate` export gate); GDD §4.1 / §7 reachability + tuning.
**Posture:** *deterministic, reproducible* — same scenario + catalog always yields
the same findings, the same order, and the same `ReportHash` (CI golden hashes).

## Intent

The mission editor must not export a scenario the simulation cannot run (missions
with no units, strikes with no targets, targets beyond combat radius, …). Rather
than scatter ad-hoc checks, every rule is a pure function appending to a shared
finding sink, and the report is the single source of truth for "can this export?".

Determinism matters because validation feeds the export gate and CI golden tests:
findings are sorted and hashed so any drift in rule output fails a pinned-hash test.

## Architecture

```
ScenarioValidationExportGate.EvaluateExport(scenario, catalog, [config])
        │
        ▼
ScenarioValidationEngine.Validate(scenario, catalog, config)
        │   runs each rule, appending to List<ValidationFinding>:
        │     DbRefRule · MissionNoUnitsRule · PatrolZoneRule · StrikeNoTargetsRule
        │     FerryDestinationRule · AirReadyLaunchRule
        │     FerryReachabilityRule · StrikeReachabilityRule  (use ReachabilityCalculator)
        ▼
ValidationReport.FromFindings(findings)
        │   sort: Severity desc, then Code, MissionId, UnitId, TargetId, Message (ordinal)
        │   Passed = no finding >= Error ;  ReportHash = SHA-256 over sorted findings
        ▼
(Allowed = report.CanExport(config), report)
        Allowed = no finding >= config.ExportBlockSeverityFloor (default Error)
```

| Type | Role |
|------|------|
| `ScenarioValidationExportGate` | Static entry point for headless export. `EvaluateExport(...) → (bool Allowed, ValidationReport Report)`. |
| `IScenarioValidationEngine` / `ScenarioValidationEngine` | Runs the fixed ADR-008 rule set in order and returns a `ValidationReport`. |
| `ValidationRules` (`Rules/`) | `internal static` rule methods. Each takes the scenario (+ catalog/config) and appends `ValidationFinding`s. |
| `IScenarioValidationRule` | Rule contract (`RuleId` + `Evaluate(scenario, catalog, config, sink)`) for pluggable rules. |
| `ReachabilityCalculator` | Pure geo/fuel math: `HaversineNm`, `IsReachable`, `TryClassifyStrikeUnreachable` (round-trip combat radius, GDD §4.1). |
| `ValidationConfig` | Tuning record: `IngressEgressPadNm=50`, `FuelFraction=0.85`, `ExportBlockSeverityFloor=Error` (GDD §7 / ADR-008). |
| `ValidationReport` | `(Passed, Findings, ReportHash)`. `FromFindings` sorts + hashes; `CanExport(config)` applies the severity floor. |
| `ValidationFinding` | `(Code, Severity, Message, MissionId?, UnitId?, TargetId?, Data?)`. |
| `ValidationSeverity` | `Info=0 · Warning=1 · Error=2`. |
| `ValidationGoldenHashes` | Pinned `ReportHash` constants for CI golden scenarios. |
| `ValidationPipeline` | Separate **catalog**-intelligence pass (delegates to `Agents.DatabaseIntelligenceOrchestrator`); not the scenario rule engine. See `Agents/README.md`. |

### Rules and codes

| Rule | Code(s) | Severity | Fires when |
|------|---------|----------|-----------|
| `DbRefRule` | `DB_MISMATCH` | Error | `dbRef`/`dbSnapshotId` does not resolve to an available snapshot. |
| `MissionNoUnitsRule` | `MISSION_NO_UNITS` | Error | A mission has zero assigned units. |
| `PatrolZoneRule` | `PATROL_ZONE_DEGENERATE` | Error | A Patrol mission has fewer than 3 waypoints. |
| `StrikeNoTargetsRule` | `STRIKE_NO_TARGETS` | Error | A Strike mission has no targets. |
| `FerryDestinationRule` | `FERRY_NO_DESTINATION` | Error | A Ferry mission has no destination base. |
| `AirReadyLaunchRule` | `AIR_NOT_READY` | Error | A Strike unit is flagged not-ready-for-launch in `UnitReadiness`. |
| `StrikeReachabilityRule` | `STRIKE_INVALID_PLATFORM`, `STRIKE_UNREACHABLE`, `STRIKE_UNREACHABLE_FUEL` | Error | Unit has invalid combat radius, or a target is beyond radius / fuel budget. |
| `FerryReachabilityRule` | `FERRY_UNREACHABLE`, `FERRY_UNREACHABLE_FUEL` | Error | Ferry destination is beyond combat radius / fuel budget. |

Reachability uses the first assigned unit and requires catalog positions +
`combat_radius_nm`; missing data is skipped (no false positive). Fuel budget =
`combatRadius * FuelFraction − IngressEgressPadNm`.

## Usage

```csharp
using ProjectAegis.Data.Validation;

var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(scenario, catalog);

if (!allowed)
    foreach (var f in report.Findings)            // already sorted, stable order
        Console.WriteLine($"[{f.Severity}] {f.Code} {f.MissionId}/{f.TargetId}: {f.Message}");

Console.WriteLine(report.ReportHash);             // deterministic 64-char hex

// Override tuning (e.g. tighter fuel fraction, block on warnings):
var strict = new ValidationConfig(
    FuelFraction: 0.75,
    ExportBlockSeverityFloor: ValidationSeverity.Warning);
var (allowedStrict, _) = ScenarioValidationExportGate.EvaluateExport(scenario, catalog, strict);
```

## CLI / operational runbook

Exposed headlessly as `scenario_validate` (and indirectly via `scenario_export_brief`
and `scenario_simulate_sample`, which validate first):

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_validate --path <scenario.json>
# → { "passed": false, "canExport": false, "reportHash": "<64 hex>",
#     "findings": [ { "code": "STRIKE_UNREACHABLE", "severity": "Error", ... } ] }
# exit 0 = export allowed, 1 = blocking findings
```

See `tools/mission-editor/README.md` for the full verb table and PowerShell wrappers.

## Constraints & gotchas

- **Determinism is load-bearing.** Findings are sorted (`Severity` desc, then
  `Code`/`MissionId`/`UnitId`/`TargetId`/`Message` ordinal) before hashing. Adding,
  reordering, or rewording a rule changes the `ReportHash` and **breaks the golden
  tests** in `ValidationGoldenTests` — update `ValidationGoldenHashes` deliberately.
- **`Passed` vs `CanExport` differ.** `Passed` is hard-coded to "no `Error`
  finding"; `CanExport(config)` applies `ExportBlockSeverityFloor` (default `Error`,
  so they usually agree, but a `Warning` floor diverges them).
- **Missing catalog data is skipped, not failed.** Reachability rules bail out when
  a unit/target lacks a position or combat radius — they never emit a false
  "unreachable". Catch those upstream (e.g. `STRIKE_INVALID_PLATFORM` only fires when
  the unit position resolves but the radius does not).
- **First-unit assumption.** Reachability evaluates `AssignedUnitIds[0]` only; mixed
  squadrons are not range-checked per-unit in v1.
- **`ValidationPipeline` is a different concern.** It runs the *catalog* intelligence
  agents (entity/rules/consistency/diff), not the scenario rules — don't confuse the
  two engines.

## Tests

| Area | Test |
|------|------|
| Rule engine findings + ordering | `ProjectAegis.Data.Tests/Validation/ScenarioValidationEngineTests` |
| Reachability/fuel math | `Validation/ReachabilityCalculatorTests` |
| Pinned golden report hashes | `Validation/ValidationGoldenTests` |
| Logistics reachability rules | `Validation/LogisticsValidationRulesTests` |
| Catalog intelligence pipeline | `Validation/ValidationPipelineTests` |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Validation" -v minimal
```
