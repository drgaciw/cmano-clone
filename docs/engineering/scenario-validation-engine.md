# Scenario validation engine — export gate runbook

Developer/operator guide for the **scenario validation engine** — the pure,
deterministic rule pipeline that decides whether a `scenario.json` is allowed to
**export, play, or run a headless sample**. The architectural rationale lives in
[`ADR-008`](../architecture/adr-008-mission-editor-validation-engine.md)
(and combat-domain rules in
[`ADR-009`](../architecture/adr-009-combat-domain-validators.md)); this doc is
the **how-to**: the rule pipeline, every finding code, the deterministic ordering
and report hash, the export gate, the CLI/MCP surface, the golden fixtures, and
the constraints you must not break.

The engine lives in `src/ProjectAegis.Data/Validation/` (namespace
`ProjectAegis.Data.Validation`); the CLI verbs that drive it are in
`src/ProjectAegis.MissionEditor.Cli/`. For the verb-level flag reference see
[`mission-editor-mcp-cli-reference.md`](mission-editor-mcp-cli-reference.md).

## Intent

Per ADR-008, validation is a **data-layer concern** over the canonical
`scenario.json` plus an `ICatalogReader`: same file + same `ValidationConfig` +
same catalog snapshot → byte-identical findings on every run, in every host
(Unity editor, MCP CLI, CI). There is no LLM in the blocking path, no Unity
dependency, and `editorState` is never an input.

The engine enforces three rules of engagement:

- **Save is never blocked.** Validation gates only **export / play / sample**.
  Authors can always save a broken scenario.
- **Always re-run, never cache.** Every export-class operation calls the engine
  fresh; there is no authoritative cached pass state.
- **Deterministic + hashable.** Findings are sorted into a total order and folded
  into a SHA-256 `ReportHash` that CI pins as a golden.

> `ScenarioValidationEngine` (this doc, ADR-008, scenario authoring) is **not**
> the same as `ValidationPipeline` (`src/ProjectAegis.Data/Validation/ValidationPipeline.cs`),
> which is the catalog **Database Intelligence** pipeline (req-06) that delegates
> to `DatabaseIntelligenceOrchestrator` and emits `DatabaseAgentFinding`s (see
> the [database intelligence agent pipeline reference](database-intelligence-agent-pipeline.md)).
> The two surfaces grade different inputs and gate different operations — don't
> conflate them.

## Pipeline

`ScenarioValidationEngine.Validate(scenario, catalog, config)` runs each rule in
a **fixed order** (for reproducibility), collecting `ValidationFinding`s into a
single sink, then returns `ValidationReport.FromFindings(...)`.

| # | Rule | Finding code(s) | Severity | Triggers when |
|---|------|-----------------|----------|---------------|
| 1 | `DbRefRule` | `DB_MISMATCH` | Error | `metadata.dbRef`/`dbSnapshotId` is set but `catalog.TryResolveDbRef` fails |
| 2 | `MissionNoUnitsRule` | `MISSION_NO_UNITS` | Error | A mission has zero assigned units |
| 3 | `PatrolZoneRule` | `PATROL_ZONE_DEGENERATE` | Error | A `Patrol` mission has `< 3` patrol-zone waypoints |
| 4 | `StrikeNoTargetsRule` | `STRIKE_NO_TARGETS` | Error | A `Strike` mission has no targets |
| 5 | `FerryDestinationRule` | `FERRY_NO_DESTINATION` | Error | A `Ferry` mission has no destination base |
| 6 | `AirReadyLaunchRule` | `AIR_NOT_READY` | Error | A `Strike` unit is in `metadata.unitReadiness` with `ReadyForLaunch = false` |
| 7 | `FerryReachabilityRule` | `FERRY_UNREACHABLE`, `FERRY_UNREACHABLE_FUEL` | Error | A `Ferry` destination is beyond combat radius / fuel budget |
| 8 | `StrikeReachabilityRule` | `STRIKE_INVALID_PLATFORM`, `STRIKE_UNREACHABLE`, `STRIKE_UNREACHABLE_FUEL` | Error | A `Strike` unit has invalid `combat_radius_nm`, or a target is beyond radius / fuel budget |

> **Drift note.** ADR-008 was accepted with **six v1 rules**. The implemented
> engine now runs **eight** rules (the `AirReadyLaunchRule` and a split
> `Ferry`/`Strike` reachability pair add the `AIR_NOT_READY`, `FERRY_UNREACHABLE`,
> `FERRY_UNREACHABLE_FUEL`, `STRIKE_UNREACHABLE_FUEL`, and `STRIKE_INVALID_PLATFORM`
> codes). This runbook reflects the **current code**; treat it as the source of
> truth for finding codes until ADR-008's rule table is refreshed.

All rules emit `Severity.Error` today, so any finding blocks export under the
default config. The reachability rules attach `Data["excess_nm"]` (rounded to one
decimal) so a UI/agent can show "by N nm".

## Determinism: sort order and report hash

Rule order does **not** determine output order. `ValidationReport.FromFindings`
re-sorts every finding into a single total order before hashing or display:

```
Severity DESC, Code ASC, MissionId ASC, UnitId ASC, TargetId ASC, Message ASC
```

All comparisons use `StringComparer.Ordinal` (locale-independent). `ReportHash`
is the lower-case hex SHA-256 of the sorted findings rendered as:

```
<severityInt>|<code>|<missionId>|<unitId>|<targetId>|<message>\n
```

Because the message templates use invariant numeric formatting and the sort is
ordinal, the hash is byte-stable across machines and OSes — that is what the
golden tests pin.

## Pass vs. export gate

Two predicates exist and are **not** identical:

| Predicate | Definition | Used for |
|-----------|------------|----------|
| `report.Passed` | No finding with `Severity >= Error` | "Is the scenario clean?" |
| `report.CanExport(config)` | No finding with `Severity >= config.ExportBlockSeverityFloor` | The actual export/play/sample gate |

With the default `ExportBlockSeverityFloor = Error` the two coincide. They
diverge only if you lower the floor (e.g. to `Warning`), in which case
`CanExport` becomes stricter than `Passed`. The gate always checks `CanExport`.

`ScenarioValidationExportGate.EvaluateExport(scenario, catalog, config?)` is the
single shared entry point: it constructs the engine, runs `Validate`, and returns
`(Allowed, Report)`. The MCP CLI verbs all call it rather than re-implementing the
gate.

## Configuration

`ValidationConfig` (sourced from `assets/data/editor/validation-config.json` in
hosts; defaults below):

| Field | Default | Meaning |
|-------|---------|---------|
| `IngressEgressPadNm` | `50` | Reserve subtracted from the fuel budget for ingress/egress |
| `FuelFraction` | `0.85` | Usable fraction of round-trip combat radius |
| `ExportBlockSeverityFloor` | `Error` | Lowest severity that blocks export |

### Reachability math

`ReachabilityCalculator` treats `combat_radius_nm` as the **round-trip** budget
(GDD §4.1 — do **not** double the distance). For a great-circle `distanceNm`
(haversine, Earth radius `3440.065` nm):

```
fuelBudgetNm = combatRadiusNm * fuelFraction - ingressEgressPadNm
reachable    = distanceNm <= fuelBudgetNm
```

When unreachable, the classifier distinguishes two causes:

- `distanceNm > combatRadiusNm` → `STRIKE_UNREACHABLE` / `FERRY_UNREACHABLE`
  (`excess = distance - combatRadius`).
- within radius but over the fuel budget → `STRIKE_UNREACHABLE_FUEL` /
  `FERRY_UNREACHABLE_FUEL` (`excess = distance - fuelBudget`).

A `combat_radius_nm <= 0` is treated as *reachable* by the calculator, but the
strike rule guards for it first and emits `STRIKE_INVALID_PLATFORM`. Rules skip
silently (no finding) when a position or combat radius cannot be resolved from the
catalog — missing catalog data is not a scenario error.

## CLI / MCP surface

| Verb | What it does | Exit codes |
|------|--------------|------------|
| `scenario_validate --path <f>` | Run the engine, print the JSON report | `0` allowed · `1` blocked / missing `--path` · `2` file not found |
| `scenario_export_brief --path <f> [--out <p>]` | Validate, then write a stub brief only if `canExport` | propagates validate codes; nothing written on block |
| `scenario_simulate_sample --path <f> [--ticks N]` | Validate, then run an isolated Baltic harness sample | `0` digests JSON · `1` blocked · `2` file not found |

```bash
# Validate a scenario (exit 1 if any blocking finding)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_validate --path assets/data/scenarios/validation/golden_clean.json
```

`scenario_validate` returns `{ passed, canExport, reportHash, findings[] }`
(camelCase, null fields omitted) via `ValidationReportJsonDto`. Each finding
carries `code, severity, message` plus optional `missionId, unitId, targetId,
data`. `scenario_simulate_sample` runs the gate **first** and, on success,
appends the same `reportHash` to its replay digest JSON (see
[`replay-determinism-harness.md`](replay-determinism-harness.md) for the
`scenario_simulate_sample` digest format).

## Golden fixtures & CI

Two pinned report hashes live in `ValidationGoldenHashes.cs`:

| Constant | Fixture | Expectation |
|----------|---------|-------------|
| `CleanPatrol` | `assets/data/scenarios/validation/golden_clean.json` | `Passed`, `CanExport`, stable hash |
| `StrikeUnreachable` | `assets/data/scenarios/validation/golden_strike_unreachable.json` | blocked, contains `STRIKE_UNREACHABLE*`, stable hash |

`ValidationGoldenTests` loads each fixture against `InMemoryCatalogReader.BalticPatrolFixture()`,
asserts the expected gate result, and asserts the `ReportHash` matches the pinned
constant **and** is reproducible across two runs (the intra-run determinism
check). The fixture resolver walks up to 10 parent directories from
`AppContext.BaseDirectory`; if a fixture is absent the test no-ops rather than
failing, so keep the fixtures committed.

Run the validation suite locally:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter FullyQualifiedName~Validation
```

### Regenerating a golden (deliberate)

Re-record only after confirming the rule/behavior change is **intentional** —
silent re-recording hides a regression.

1. Run `scenario_validate --path <fixture>` (or the matching golden test) and read
   the printed `reportHash`.
2. Replace the constant in `ValidationGoldenHashes.cs`.
3. Note the reason in the commit message and re-run the `~Validation` filter.

## Constraints / pitfalls

- **Save is not gated; export/play/sample are.** Don't add a save-time block —
  it violates GDD §5 / AC-12. The gate belongs at export boundaries only.
- **Never cache pass state.** Hosts may memoize on a content hash as an
  optimization, but the engine must re-run on every export-class call; there is
  no authoritative cached `ValidationReport`.
- **Output order ≠ rule order.** If you assert on finding position, sort first by
  the total order above — rule execution order is an implementation detail.
- **Keep it ordinal + invariant.** Any culture-sensitive string compare or
  numeric format in a rule message breaks the `ReportHash` across locales.
- **No `editorState`, no Unity, no LLM** in any type under
  `ProjectAegis.Data.Validation` (ADR-006/ADR-008 assembly boundary).
- **Missing catalog data is not an error.** Reachability rules skip when a
  position/radius can't be resolved; only an *explicitly bad* `combat_radius_nm`
  (`<= 0`) raises `STRIKE_INVALID_PLATFORM`.
- **Severity floor is a knob, not a per-rule switch.** All v1/v2 rules emit
  `Error`; to soften a gate, change `ExportBlockSeverityFloor`, don't downgrade a
  rule's severity ad hoc.

## Tests

- `src/ProjectAegis.Data.Tests/Validation/ScenarioValidationEngineTests.cs` — per-rule codes and determinism.
- `src/ProjectAegis.Data.Tests/Validation/ReachabilityCalculatorTests.cs` — haversine + fuel-budget classification.
- `src/ProjectAegis.Data.Tests/Validation/LogisticsValidationRulesTests.cs` — ferry/air-ready logistics rules.
- `src/ProjectAegis.Data.Tests/Validation/ValidationGoldenTests.cs` — pinned `ReportHash` goldens + SQLite catalog hooks.
- `src/ProjectAegis.Data.Tests/Validation/ValidationPipelineTests.cs` — the separate catalog DB-intelligence pipeline (req-06).

## Related docs

- [`ADR-008`](../architecture/adr-008-mission-editor-validation-engine.md) — the
  validation-engine decision (note the six-rule drift above).
- [`ADR-009`](../architecture/adr-009-combat-domain-validators.md) —
  combat-domain validator rationale.
- [`mission-editor-mcp-cli-reference.md`](mission-editor-mcp-cli-reference.md) —
  flag-level reference for `scenario_validate` / `scenario_export_brief` /
  `scenario_simulate_sample`.
- [`replay-determinism-harness.md`](replay-determinism-harness.md) — the sim
  replay gate that `scenario_simulate_sample` runs **after** this export gate.
