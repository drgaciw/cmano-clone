# Scenario validation engine

> **Scope:** `ProjectAegis.Data/Validation/` — the deterministic, engine-agnostic rule
> engine that decides whether a scenario **document** is allowed to export / publish /
> sample-simulate (ADR-008, GDD §7). It is the single source of truth behind the
> `scenario_validate` CLI verb and `ScenarioValidationExportGate`. Play is a **separate**
> overridable Error-only check (`EditModeController.TryEnterPlay`).
>
> This is the *scenario* validation engine. The same `Validation/` folder also holds the
> **catalog-side** kill-chain / link-integrity rules that feed the Database Intelligence
> pipeline and the `CatalogWriteGate`; those are a separate concern and are summarized in
> [Catalog-side validators](#catalog-side-validators-shared-folder-different-concern) below.
> Related: [scenario-document-authoring.md](scenario-document-authoring.md) (the document model
> and per-mission field rules), [scenario-event-system.md](scenario-event-system.md) (the
> `EventStaticAnalyzer` warnings), [catalog-write-gate.md](catalog-write-gate.md),
> and [ADR-008](../architecture/adr-008-mission-editor-validation-engine.md) /
> [ADR-016](../architecture/adr-016-event-graph-complexity-caps.md).

---

## Why it exists

A scenario document (`ScenarioDocumentDto`) can be **saved** at any time — including with
blocking errors — so authors never lose work-in-progress. But before a scenario is allowed to
**leave** the editor (export a brief, publish, run a sample simulation, or enter Play), it must
pass a fixed set of integrity checks: the referenced catalog snapshot resolves, missions are
well-formed, strikes are fuel-reachable, the event graph is within complexity caps, and so on.

The validation engine centralizes those checks into one deterministic, pure function so that:

- every export path enforces the **same** rules (no drift between publish / play / CLI),
- the result is **reproducible** — the same `(document, catalog, config)` always yields the
  same findings *and* the same report hash, which lets CI pin golden hashes, and
- the engine has **no engine/Unity dependency** — it lives in `ProjectAegis.Data` and runs
  headless in tests and the Mission Editor CLI.

---

## The pipeline at a glance

```
ScenarioDocumentDto ─┐
ICatalogReader ──────┤→ ScenarioValidationEngine.Validate(scenario, catalog, config)
ValidationConfig ────┘        │  (runs an ordered list of pure rules, each appending
                              │   ValidationFinding rows to a shared sink)
                              ▼
                     ValidationReport.FromFindings(findings)
                       • SortFindings  (severity desc → Code → Mission → Unit → Target → Message)
                       • Passed     = no finding ≥ Error
                       • ReportHash = SHA-256 over the sorted rows
                              │
        ┌─────────────────────┴─────────────────────┐
        ▼                                            ▼
ScenarioValidationExportGate.EvaluateExport      ValidationReportJsonDto.Serialize
  (Allowed = report.CanExport(config)              (stable camelCase JSON for the
   = no finding ≥ ExportBlockSeverityFloor)         scenario_validate verb + editor)
```

`ScenarioValidationEngine` implements `IScenarioValidationEngine`; the concrete engine is
constructed fresh per call (no shared state), which is what keeps validation deterministic and
side-effect free.

---

## Rule catalog

`ScenarioValidationEngine.Validate` runs the rules **in this fixed order** (order does not
affect the result — findings are re-sorted before hashing — but it documents intent). Each rule
is a pure static method in `Validation/Rules/ValidationRules.cs` that appends zero or more
`ValidationFinding` rows.

| # | Rule | Emits (code → severity) | What it checks |
|---|------|-------------------------|----------------|
| 1 | `TlBranchRule` | `TL_BRANCH_MISSING` / `TL_BRANCH_INVALID` / `TL_BRANCH_SNAPSHOT_MISMATCH` → Error | `metadata.tlBranch` is present and a valid TL tier (`TL-0…TL-5`), and (for explicit DB bindings) the resolved snapshot's branch matches. |
| 2 | `TlReleaseTrainRule` | `TL_RELEASE_TRAIN_NOT_FOUND` / `TL_RELEASE_TRAIN_MISMATCH` → Error | A catalog snapshot exists in the release train for the branch, and an explicit `dbRef`/`dbSnapshotId` resolves to that same snapshot. |
| 3 | `DbRefRule` | `DB_MISMATCH` → Error | An explicit `dbRef` / `dbSnapshotId` resolves to an available catalog snapshot. |
| 4 | `MissionNoUnitsRule` | `MISSION_NO_UNITS` → Error | Every mission has ≥1 assigned unit. |
| 5 | `PatrolZoneRule` | `PATROL_ZONE_DEGENERATE` → Error | Each `Patrol` mission has ≥3 waypoints. |
| 6 | `StrikeNoTargetsRule` | `STRIKE_NO_TARGETS` → Error | Each `Strike` mission has ≥1 target. |
| 7 | `FerryDestinationRule` | `FERRY_NO_DESTINATION` → Error | Each `Ferry` mission names a destination base. |
| 8 | `AirReadyLaunchRule` | `AIR_NOT_READY` → Error | For `Strike` missions, assigned units flagged in `metadata.unitReadiness` are `ReadyForLaunch` (units iterated in ordinal order). |
| 9 | `FerryReachabilityRule` | `FERRY_UNREACHABLE` / `FERRY_UNREACHABLE_FUEL` → Error | Ferry destination is within the assigned unit's fuel-budgeted combat radius (see [Reachability math](#reachability-math)). |
| 10 | `StrikeReachabilityRule` | `STRIKE_UNREACHABLE` / `STRIKE_UNREACHABLE_FUEL` / `STRIKE_INVALID_PLATFORM` → Error | Every strike target is reachable; the shooter has a valid `combat_radius_nm` (targets iterated in ordinal order). |
| 11 | `IncompatibleHostRule` | `INCOMPATIBLE_HOST` → Error | Model-integrity check for incompatible host relationships (e.g. an `air` unit with no carrier host). |
| 12 | `BrokenRefRule` | `BROKEN_REF` → Error | Detects dangling `ref:` target references. |
| 13 | `DoctrineInheritanceRule` | `DOCTRINE_RESOLVED` → **Info** | Resolves each mission's ROE from `mission.roeOverride` else `metadata.sideRoe` else `WeaponsFree`; records the resolution + source (`override` / `side`). AME-3.2 / AC-4. Informational — never blocks. |
| 14 | `EventGraphComplexityRule` | `EVENT_GRAPH_COMPLEXITY_HIGH` / `EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH` → **Warning**; `EVENT_CONDITION_CAP_EXCEEDED` → Error | ADR-016 event-graph caps (see [Event-graph complexity caps](#event-graph-complexity-caps-adr-016)). |
| — | `EventStaticAnalyzer.Analyze` | Warning-level codes | Pure event static analysis appended last (dead triggers, unreachable actions, contradictions, cycles). Documented in [scenario-event-system.md](scenario-event-system.md); ME-W2 export-honesty warnings that do **not** block at the Error floor. |

> **Info/Warning findings never block export by default.** Only findings at or above
> `ExportBlockSeverityFloor` (default `Error`) do. `DOCTRINE_RESOLVED` and the soft event-graph
> warnings are reported for transparency but are export-safe.

### Reachability math

`ReachabilityCalculator` (pure, `Validation/ReachabilityCalculator.cs`, GDD §4.1) provides the
great-circle distance and the round-trip fuel classification used by rules 9 and 10:

- `HaversineNm(lat1, lon1, lat2, lon2)` — great-circle nautical miles using
  `EarthRadiusNm = 3440.065`.
- `TryClassifyStrikeUnreachable(distanceNm, combatRadiusNm, ingressEgressPadNm, fuelFraction, …)`:
  - `fuelBudgetNm = combatRadiusNm × fuelFraction − ingressEgressPadNm`.
  - Reachable when `distanceNm ≤ fuelBudgetNm` (and always reachable when `combatRadiusNm ≤ 0`,
    so the reachability rule never fires on a platform with no radius — that gap is caught by the
    separate `STRIKE_INVALID_PLATFORM` code).
  - Otherwise unreachable: code `STRIKE_UNREACHABLE` when `distanceNm > combatRadiusNm` (beyond
    the physical radius) else `STRIKE_UNREACHABLE_FUEL` (inside the radius but over the fuel
    budget). `excess_nm` is reported rounded to 0.1 nm.

`FerryReachabilityRule` reuses the same classifier and remaps the codes to
`FERRY_UNREACHABLE` / `FERRY_UNREACHABLE_FUEL`.

### Event-graph complexity caps (ADR-016)

`EventGraphComplexityRule` enforces GDD §4.3 / ADR-016. With the scenario's events:

- `complexity = E + Σ conditions + CrossRefWeight × crossRefs` (cross-refs are conditions
  naming a `unitId`/`zoneId`, plus actions naming a `unitId` when the scenario has missions).
  If `complexity > ComplexityWarnThreshold` → `EVENT_GRAPH_COMPLEXITY_HIGH` **Warning**.
- `peakDensity = max(1, #Time-triggered events)`. If `> DensityWarnThreshold` →
  `EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH` **Warning**.
- Per event, if `conditions > MaxConditionsPerEvent` → `EVENT_CONDITION_CAP_EXCEEDED`
  **Error** (the only hard, blocking limit — soft caps are warnings and never block export).

---

## Report model, severity & determinism

`ValidationSeverity` is an ordered enum: `Info = 0`, `Warning = 1`, `Error = 2`.

`ValidationFinding` is an immutable record: `Code`, `Severity`, `Message`, optional
`MissionId` / `UnitId` / `TargetId`, and an optional `Data` string→string map for structured
context (e.g. `excess_nm`, `complexity`, `resolvedRoe`).

`ValidationReport.FromFindings(findings)`:

- **`Passed`** = every finding is below `Error`.
- **`SortFindings`** — a stable deterministic ordering: severity **descending**, then `Code`,
  `MissionId`, `UnitId`, `TargetId`, `Message` — all `Ordinal`. This ordering is what makes the
  report (and its hash) reproducible regardless of rule execution order.
- **`ReportHash`** = lowercase-hex SHA-256 over the sorted rows, one line per finding formatted
  as `{(int)severity}|{code}|{missionId}|{unitId}|{targetId}|{message}\n`. Pinned in CI (see
  [Golden hashes](#golden-hashes)).

`report.CanExport(config)` is a **separate** predicate from `Passed`: it returns `true` unless
some finding is at or above `config.ExportBlockSeverityFloor` (default `Error`, so `CanExport`
and `Passed` coincide under the default config — but a `warning` floor can be configured to
block on warnings too).

`ValidationReportJsonDto.Serialize(report, config)` produces the stable camelCase JSON emitted
by the CLI: `passed`, `canExport`, `reportHash`, the `findings[]`, and a convenience
`doctrineResolution[]` array projected from the `DOCTRINE_RESOLVED` info findings.

---

## Configuration

`ValidationConfig` (GDD §7 tuning knobs, extended in S84 for ADR-016):

| Field | Default | Meaning |
|-------|---------|---------|
| `IngressEgressPadNm` | `50` | Fuel reserve subtracted from the strike/ferry budget. |
| `FuelFraction` | `0.85` | Fraction of combat radius usable for the round trip. |
| `ExportBlockSeverityFloor` | `Error` | Findings at/above this severity block export. |
| `ComplexityWarnThreshold` | `400` | Soft event-graph complexity warning. |
| `DensityWarnThreshold` | `20` | Soft peak-tick-density warning. |
| `CrossRefWeight` | `2` | Weight `C` on cross-references in the complexity formula. |
| `MaxConditionsPerEvent` | `32` | **Hard** per-event condition cap (blocking error). |

`ValidationConfigLoader.LoadFromRepo()` reads
[`assets/data/editor/validation-config.json`](../../assets/data/editor/validation-config.json)
(walking up to the `ProjectAegis.sln` root); a missing file falls back to the record defaults,
and `exportBlockSeverityFloor` accepts `"warning"` / `"error"` (case-insensitive).

**Who actually loads that file:** only `ScenarioSimulateSampleCommand` calls `LoadFromRepo()`.
`scenario_validate`, `scenario_export_brief`, `ScenarioExportCommand.Prepare` (publish path),
and the default `ScenarioValidationExportGate` construct `new ValidationConfig()` — the
record defaults — and **ignore** a tuned `validation-config.json`. Raising the file's floor to
`warning` can make sample-simulate reject a document that `scenario_validate` / publish still
allow. Same-rules holds under the default config; a non-default file is not shared across verbs.

---

## The export gate (the enforcement boundary)

`ScenarioValidationExportGate.EvaluateExport(scenario, catalog, config?)` is the **export /
publish / sample-simulate** gate (ADR-008). It constructs a fresh engine, validates, and returns
`(Allowed, Report)` where `Allowed = report.CanExport(config)`. Play is **not** this gate (see
below).

Load-bearing distinction (AME-6.5 / AC-12):

- **Save** (`ScenarioDocumentEditor.Save`) deliberately **bypasses** validation, so WIP states
  with blocking errors can be persisted.
- **Export / publish / sample-simulate** call the gate: `scenario_export_brief` (via
  `ScenarioValidateCommand`), `ScenarioExportCommand.Prepare` (publish + `scenario_simulate_sample`).
  Any finding meeting/exceeding that path's floor blocks the operation.
- **Play is a separate, overridable gate.** `EditModeController.TryEnterPlay` refreshes
  `LiveFindingsPresenter` and blocks only on `HasErrorSeverity`, unless the caller passes
  `forceConfirmInvalid: true`. It does **not** call `ScenarioValidationExportGate` and does **not**
  honor a Warning export floor. Do not treat Play as part of this sole enforcement boundary.

---

## Operational usage — `scenario_validate`

`ScenarioValidateCommand.Run(scenarioPath, quiet, output)` (Mission Editor CLI) loads the
document, resolves the catalog (SQLite Baltic reader if the requested snapshot/branch resolves,
else the in-memory Baltic patrol fixture), runs the export gate, and prints the report JSON
(unless `--quiet`).

**Exit codes:** `0` = export allowed · `1` = blocked (findings ≥ floor) · `2` = file not found.

`Program.RunScenarioValidate` only parses `--path` and always calls
`ScenarioValidateCommand.Run(path, quiet: false, …)` — there is **no** `--quiet` flag on this
verb. The `quiet` parameter exists on the command type and is used internally by
`scenario_export_brief` (`quiet: true`) so the brief writer can suppress JSON. Scripts that
need only the exit code should ignore stdout or call the command API directly.

```bash
# JSON report + gate decision (from repo root). Always prints JSON; exit is 0/1/2.
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_validate --path data/scenarios/<scenario>.scenario.json
```

Representative JSON shape:

```json
{
  "passed": false,
  "canExport": false,
  "reportHash": "…64-hex…",
  "findings": [
    { "code": "STRIKE_UNREACHABLE_FUEL", "severity": "Error",
      "message": "Strike mission 's1' target 't1' exceeds fuel range by 42.7 nm.",
      "missionId": "s1", "unitId": "u1", "targetId": "t1",
      "data": { "excess_nm": "42.7" } }
  ],
  "doctrineResolution": [ { "missionId": "s1", "resolvedRoe": "WeaponsFree" } ]
}
```

See [mission-editor-cli.md](mission-editor-cli.md) for the full verb surface.

---

## Golden hashes

`ValidationGoldenHashes` pins report hashes for CI golden scenarios (ADR-008): `CleanPatrol`,
`StrikeUnreachable`, and three Phase-B workbook fixtures (`PhaseBCleanWorkbook`,
`PhaseBFixtureErrors`, `PhaseBDamageFixtureErrors`). If you intentionally change a finding's
`Code` / `Message` / severity or the sort order, the report hash changes and these constants
(plus `Validation/ValidationGoldenTests`) must be regenerated in the same change — treat an
unexpected hash diff as a regression.

---

## Editor-state schema lint (AC-9)

`EditorStateSchemaLint.FindViolations()` enforces that `editorState` is **derived-only** — the
sim and validation layers must not read it. It reflects over `ProjectAegis.Sim` and
`ProjectAegis.Data` for members named `EditorState` outside the allowed authoring/lint/package
types and returns any violations. This guards the layering invariant rather than a specific
scenario document.

---

## Catalog-side validators (shared folder, different concern)

The `Validation/` namespace also hosts **catalog / kill-chain** integrity rules that are *not*
part of the scenario export gate. They validate the *catalog* (platforms, mounts, weapons,
sensors, comms links) and feed the [Database Intelligence pipeline](../../src/ProjectAegis.Data/Agents/)
and the [`CatalogWriteGate`](catalog-write-gate.md):

- **`KillChainRules`** (DBI-3.5, R1–R4) — detect-only kill-chain impossibility findings over
  catalog dependency edges: `KILL_CHAIN_ORPHAN_EDGE`, `KILL_CHAIN_RANGE_EXCEEDS_SENSOR`,
  `KILL_CHAIN_SPEED_MISMATCH`, `KILL_CHAIN_WEAPON_EXCEEDS_PLATFORM_REACH`.
- **`LinkCatalogRules`** (Req-21 / DBI-3.1) — comms/link-catalog integrity: `LINK_ORPHAN_COMMS`,
  `LINK_TYPE_INVALID`, `LINK_LATENCY_INVALID`.
- **`KillChainCommitGate`** (DBI-3.4 / DBI-7.2) — blocks write-gate commits when the
  post-staging catalog has `error`-severity `KILL_CHAIN_*` findings; returns sorted,
  de-duplicated `kill_chain:<CODE>` reasons.
- **`ValidationPipeline`** (P0 / req-06) — a thin wrapper that delegates to the
  `DatabaseIntelligenceOrchestrator`; `RunBalticDefault()` runs the standard Baltic reader.
- **`KillChainGoldenHashes`** — pins the clean Baltic kill-chain finding hash for CI.

These emit `DatabaseAgentFinding` rows (string severity `"error"`/`"warning"`/`"info"`), not the
scenario `ValidationFinding` model. See the `ProjectAegis.Data/Agents/` sources and
[catalog-write-gate.md](catalog-write-gate.md) for the write-gate flow.

---

## Extending the engine — runbook

1. **Add a scenario rule.** Add a pure static method to `Validation/Rules/ValidationRules.cs`
   that takes the document (and `catalog`/`config` if needed) plus the `List<ValidationFinding>`
   sink, and wire it into `ScenarioValidationEngine.Validate`. Use a **new, stable finding
   `Code`** (machine-readable, `SCREAMING_SNAKE_CASE`) and pick the right severity — `Error` only
   if it must block export; `Warning`/`Info` otherwise. Never mutate the document.
2. **Keep it deterministic.** No wall-clock, no unseeded RNG, no unordered iteration — order any
   per-mission/unit/target loops with `StringComparer.Ordinal` (the report sort already
   normalizes output, but ordinal loops keep `Data` payloads stable).
3. **Regenerate goldens.** Adding/altering a finding changes the report hash for affected
   fixtures — update `ValidationGoldenHashes` + `Validation/ValidationGoldenTests` in the same
   change, and confirm the intended fixtures move.
4. **Prefer new codes over reusing.** Consumers (CLI JSON, editor, CI) key off `Code`; renaming
   an existing code is a breaking change.
5. **Tune, don't hardcode.** New numeric thresholds belong in `ValidationConfig` +
   `validation-config.json`, not as C# literals.

---

## Tests

| Area | Tests |
|------|-------|
| Engine + rules | `src/ProjectAegis.Data.Tests/Validation/ScenarioValidationEngineTests.cs` |
| TL branch / release train | `Validation/TlBranchValidationTests.cs`, `Validation/TlReleaseTrainValidationTests.cs` |
| Reachability | `Validation/ReachabilityCalculatorTests.cs` |
| Event-graph caps | `Validation/EventGraphComplexityTests.cs` |
| Doctrine inheritance | `Validation/DoctrineInheritanceValidateTests.cs` |
| Logistics rules | `Validation/LogisticsValidationRulesTests.cs` |
| Save-vs-export gate | `Validation/SaveVsExportGateTests.cs`, `Validation/ScenarioDocumentEditorLiveValidationTests.cs` |
| Golden hashes | `Validation/ValidationGoldenTests.cs` |
| CLI verb | `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioValidateCliTests.cs` |
| Catalog-side | `Validation/KillChainRulePackTests.cs`, `Validation/LinkCatalogRulePackTests.cs`, `Validation/ValidationPipelineTests.cs`, `Agents/OrchestratorKillChainGateTests.cs` |

Run the scenario-validation slice headless:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Validation"
```

---

## See also

| Doc / source | For |
|--------------|-----|
| [scenario-document-authoring.md](scenario-document-authoring.md) | The `ScenarioDocumentDto` model, per-mission field rules, and the finding catalog from the author's view. |
| [scenario-event-system.md](scenario-event-system.md) | The `EventStaticAnalyzer` warning codes appended by rule 14. |
| [scenario-authoring-host.md](scenario-authoring-host.md) | Live validation inside the interactive edit command bus. |
| [mission-editor-cli.md](mission-editor-cli.md) | The full CLI verb surface (`scenario_validate`, export, simulate, publish). |
| [catalog-write-gate.md](catalog-write-gate.md) | The catalog propose→approve flow the kill-chain gate protects. |
| [ADR-008](../architecture/adr-008-mission-editor-validation-engine.md) · [ADR-016](../architecture/adr-016-event-graph-complexity-caps.md) | The validation-engine and event-graph-cap decisions. |
