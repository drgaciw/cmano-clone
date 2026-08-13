# Scenario validation engine

> **Scope.** The **engine** behind `scenario_validate` and the export/play/simulate gate
> (`ProjectAegis.Data/Validation/`, ADR-008): the fixed `ScenarioValidationEngine` rule pipeline,
> the rule model, the `ReachabilityCalculator` great-circle math, the deterministic
> `ValidationReport` (stable sort + SHA-256 `reportHash`), the `CanExport` /
> `ExportBlockSeverityFloor` decision, `ValidationConfig` + its JSON loader, the golden-hash
> regression pins, and the catalog-side siblings (`KillChainCommitGate`, `ValidationPipeline`).
> This is the *mechanics* companion to
> [`scenario-document-authoring.md` → Validation findings](scenario-document-authoring.md#validation-findings),
> which is the authoritative **finding-code catalog** — this page does not re-list the codes.
> The event-graph analysis half is in [`scenario-event-system.md`](scenario-event-system.md).
>
> Design: [ADR-008 (mission-editor validation engine)](../architecture/adr-008-mission-editor-validation-engine.md).
> All of it is pure, headless `ProjectAegis.Data` — no Unity, no I/O beyond the optional config file.

---

## Where it lives

All in `src/ProjectAegis.Data/Validation/`:

| Type | Role |
|------|------|
| `ScenarioValidationEngine` (`IScenarioValidationEngine`) | The entry point. `Validate(scenario, catalog, config)` runs the fixed rule sequence and folds findings into a `ValidationReport`. |
| `IScenarioValidationRule` | Rule contract (`RuleId` + `Evaluate(scenario, catalog, config, sink)`). |
| `Rules/ValidationRules` *(internal static)* | The v1 rule bodies (TL branch, DB ref, mission shape, reachability, doctrine, event-graph caps). |
| `ReachabilityCalculator` | Haversine distance (nm) + strike/ferry reachability classification. |
| `ValidationFinding` / `ValidationSeverity` | One finding (`Code`, `Severity`, `Message`, optional `MissionId`/`UnitId`/`TargetId`/`Data`); `Info(0) < Warning(1) < Error(2)`. |
| `ValidationReport` | `(Passed, Findings, ReportHash)` — stable-sorted findings + SHA-256 hash; `CanExport(config)`. |
| `ValidationReportJson` (`…JsonDto`) | Deterministic camelCase JSON serialization (adds `canExport` + a `doctrineResolution[]` projection). |
| `ValidationConfig` / `ValidationConfigLoader` | Tuning knobs (GDD §7 / ADR-008 / ADR-016) + the optional `assets/data/editor/validation-config.json` loader. |
| `ScenarioValidationExportGate` | The **sole** export/play/simulate gate — always recomputes the report fresh. |
| `ValidationGoldenHashes` / `KillChainGoldenHashes` | Pinned CI report/finding hashes (regression gates). |
| `KillChainCommitGate` / `KillChainRules` / `LinkCatalogRules` | Catalog-side kill-chain validation (DBI-3.4/3.5) — a sibling, not part of the scenario engine. |
| `EditorStateSchemaLint` | AC-9 reflection lint: `Sim`/`Data` must never read `editorState`. |
| `ValidationPipeline` | **Catalog** (req-06) pipeline — a thin delegator to the DBI orchestrator (not the scenario engine). |

---

## The pipeline — `ScenarioValidationEngine.Validate`

`Validate(scenario, catalog, config)` runs a **fixed, ordered** rule list into a single findings
`sink`, then folds it into a report:

```text
ValidationRules.TlBranchRule            # metadata.tlBranch present + valid + matches bound snapshot
ValidationRules.TlReleaseTrainRule      # tlBranch resolves to a release-train snapshot
ValidationRules.DbRefRule               # dbRef/dbSnapshotId resolves
ValidationRules.MissionNoUnitsRule      # every mission has assigned units
ValidationRules.PatrolZoneRule          # patrol has ≥3 waypoints
ValidationRules.StrikeNoTargetsRule     # strike has targets
ValidationRules.FerryDestinationRule    # ferry has a destination base
ValidationRules.AirReadyLaunchRule      # strike units are readyForLaunch
ValidationRules.FerryReachabilityRule   # ┐ ReachabilityCalculator (needs catalog + config)
ValidationRules.StrikeReachabilityRule  # ┘
ValidationRules.IncompatibleHostRule    # model-integrity host relationships
ValidationRules.BrokenRefRule           # ref:-prefixed ids resolve
ValidationRules.DoctrineInheritanceRule # DOCTRINE_RESOLVED info per mission
ValidationRules.EventGraphComplexityRule# ADR-016 soft complexity/density warnings + hard 32-condition cap
EventStaticAnalyzer.Analyze(scenario)   # dead triggers / unreachable / contradictory / cyclic (warnings)
→ ValidationReport.FromFindings(findings)
```

Notes:

- **Ordering is source-code order, not severity** — determinism comes from the *report* sort (below), so the rule order is free to change without moving a golden hash as long as the finding *set* is identical.
- **Reachability rules take `config`** (ingress/egress pad, fuel fraction); the shape rules do not.
- **The `IScenarioValidationRule` interface is the extension seam**; the v1 engine calls the `ValidationRules` static bodies directly, but new rules should implement the interface (`RuleId` + `Evaluate`) so they compose uniformly.
- `EventStaticAnalyzer` is documented separately — see [`scenario-event-system.md`](scenario-event-system.md).

### Reachability — `ReachabilityCalculator`

Great-circle distance over a spherical earth (`EarthRadiusNm = 3440.065`), then a two-way classify:

```text
fuelBudgetNm = combatRadiusNm × fuelFraction − ingressEgressPadNm
distance ≤ fuelBudgetNm            → reachable
distance > combatRadiusNm          → STRIKE_UNREACHABLE      (beyond radius)
fuelBudgetNm < distance ≤ radius   → STRIKE_UNREACHABLE_FUEL (in radius, over fuel budget)
combatRadiusNm ≤ 0                 → treated as reachable (skipped — no false positive)
```

Ferry rules reuse the same calculator with the `FERRY_UNREACHABLE*` codes. Unresolved unit/target
ids are **skipped** by reachability (no false positives); broken bindings surface separately as
`DB_MISMATCH` / `BROKEN_REF`.

---

## The report — deterministic by construction

`ValidationReport.FromFindings` is what makes validation replay-stable:

1. **Stable sort** — `Severity` desc, then `Code`, `MissionId`, `UnitId`, `TargetId`, `Message`, all `StringComparer.Ordinal`. Rule execution order never leaks into the output.
2. **`Passed`** — true iff no finding is `≥ Error`.
3. **`ReportHash`** — SHA-256 (lowercase hex) over the sorted findings, each rendered as `"{(int)Severity}|{Code}|{MissionId}|{UnitId}|{TargetId}|{Message}\n"`.

`CanExport(config)` is a *separate* decision from `Passed`: it blocks on `Severity >=
config.ExportBlockSeverityFloor` (default `Error`). With the floor at `Error` the two agree, but a
`Warning` floor would let `Passed == true` still fail export.

`ValidationReportJsonDto.FromReport` adds `canExport` and a mission-sorted `doctrineResolution[]`
(projected from the `DOCTRINE_RESOLVED` info findings) for MCP/CLI consumers.

---

## Config — `ValidationConfig`

`readonly record` of GDD §7 / ADR-008 / ADR-016 knobs (defaults shown):

| Field | Default | Used by |
|-------|---------|---------|
| `IngressEgressPadNm` | `50` | reachability fuel budget |
| `FuelFraction` | `0.85` | reachability fuel budget |
| `ExportBlockSeverityFloor` | `Error` | `CanExport` |
| `ComplexityWarnThreshold` | `400` | event-graph soft warning (ADR-016) |
| `DensityWarnThreshold` | `20` | peak-tick-density soft warning |
| `CrossRefWeight` | `2` | complexity scoring |
| `MaxConditionsPerEvent` | `32` | **hard** cap (`EVENT_CONDITION_CAP_EXCEEDED`) |

`ValidationConfigLoader.LoadFromRepo()` reads `assets/data/editor/validation-config.json` (walking
up to the `ProjectAegis.sln` root); a **missing file returns the defaults** — the loader never
throws for absence, only for malformed JSON.

---

## Gates built on the engine

### Export/play/simulate — `ScenarioValidationExportGate`

The **single** gate for every export path (`scenario_export_brief`,
`ScenarioExportCommand.Prepare` used by publish + `scenario_simulate_sample`, play).
`EvaluateExport(scenario, catalog, config?)` always constructs a fresh engine and recomputes the
report (determinism), returning `(Allowed, Report)`.

> **Save deliberately bypasses validation** (`ScenarioDocumentEditor.Save`, AME-6.5 / AC-12) so a
> work-in-progress scenario with blocking errors can still be persisted. Only *export/play/simulate*
> is gated.

### Kill-chain commit — `KillChainCommitGate` *(catalog side)*

A distinct gate that blocks **write-gate commits** (DBI-3.4/3.5): `KillChainRules.Evaluate(catalog)`
findings whose `Severity == "error"` and `Code` starts with `KILL_CHAIN_` become
`kill_chain:{Code}` blocking reasons (sorted + de-duped). It runs over the *catalog*, not a scenario
document, so it lives beside the scenario engine rather than inside it. `ValidationPipeline` is
likewise catalog-scoped and delegates to the
[`DatabaseIntelligenceOrchestrator`](../../src/ProjectAegis.Data/Agents/DatabaseIntelligenceOrchestrator.cs).

### Golden-hash regression pins

`ValidationGoldenHashes` / `KillChainGoldenHashes` pin known-good `reportHash` values for CI golden
scenarios/fixtures (clean patrol, strike-unreachable, Phase-B workbook error fixtures, clean
kill-chain). A rule change that shifts a finding set moves these hashes — **regenerate the pin
intentionally** (never to make a red test green blindly), the same discipline as the replay goldens
in [`determinism-and-replay.md`](determinism-and-replay.md).

### `editorState` lint — `EditorStateSchemaLint`

AC-9 reflection check: no member named `EditorState` may be read by `ProjectAegis.Sim` or
`ProjectAegis.Data` outside the DTO/lint/packaging types. Keeps derived UI state out of the
authoritative validation/sim path.

---

## CLI

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --path <scenario.json>
```

`scenario_validate` (MCP verb) calls `ScenarioValidationExportGate.EvaluateExport` and prints
`ValidationReportJsonDto.Serialize(report, config)` (`passed`, `canExport`, `reportHash`,
`findings[]`, `doctrineResolution[]`). Full CLI surface: [`mission-editor-cli.md`](mission-editor-cli.md).

---

## Extend it

| You want to… | Do this |
|--------------|---------|
| Add a scenario rule | Implement `IScenarioValidationRule` (or add a `ValidationRules.*` body), add it to the `ScenarioValidationEngine.Validate` sequence, and add a finding **code** to the [catalog](scenario-document-authoring.md#validation-findings). Then regenerate any affected `ValidationGoldenHashes`. |
| Add a tuning knob | Extend `ValidationConfig` + `ValidationConfigFileDto` + the loader mapping; document the default here. |
| Make a warning block export | Ship it at `Warning` and set `ExportBlockSeverityFloor = Warning` (affects **all** warnings) — prefer a dedicated `Error` code instead. |
| Add a catalog commit check | It belongs with `KillChainRules` / the DBI pipeline, **not** the scenario engine. |

---

## See also

| Doc | For |
|-----|-----|
| [scenario-document-authoring.md](scenario-document-authoring.md) | The **finding-code catalog**, document model, and worked examples. |
| [scenario-event-system.md](scenario-event-system.md) | The `EventStaticAnalyzer` warning codes + event-graph model. |
| [`ProjectAegis.Data/Agents/`](../../src/ProjectAegis.Data/Agents/DatabaseIntelligenceOrchestrator.cs) | The catalog-side `ValidationPipeline` / DBI orchestrator that the catalog checks delegate to. |
| [mission-editor-cli.md](mission-editor-cli.md) | `scenario_validate` and the other MCP verbs. |
| [catalog-release-train.md](catalog-release-train.md) | TL-branch / snapshot resolution the TL rules validate against. |
| [determinism-and-replay.md](determinism-and-replay.md) | The golden-hash regeneration discipline. |

## Tests

`src/ProjectAegis.Data.Tests/Validation/`:

| Test | Pins |
|------|------|
| `ScenarioValidationEngineTests` | Rule coverage + report shape. |
| `ValidationGoldenTests` | `reportHash` == `ValidationGoldenHashes.*` for the golden scenarios. |
| `TlBranchValidationTests` · `TlReleaseTrainValidationTests` | TL-branch / release-train rules. |
| `DoctrineInheritanceValidateTests` | `DOCTRINE_RESOLVED` inheritance. |
| `EventGraphComplexityTests` | ADR-016 soft warnings + hard 32-condition cap. |
| `SaveVsExportGateTests` · `ScenarioDocumentEditorLiveValidationTests` | Save bypass vs export gate. |
| `Platform/CatalogPhaseB*ValidationTests` | Phase-B workbook golden hashes. |
