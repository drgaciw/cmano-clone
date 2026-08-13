# Scenario & catalog validation — developer guide

Nothing gets **exported, played, simulated, published, or committed** without passing a
validation gate first. Those gates live in
[`ProjectAegis.Data/Validation/`](../../src/ProjectAegis.Data/Validation/) and split into two
parallel tracks that share the same finding/severity/hash idioms:

- The **scenario** track (`ScenarioValidationEngine` → `ValidationReport` →
  `ScenarioValidationExportGate`) checks a `ScenarioDocumentDto` against a catalog before any
  export/play/simulate path is allowed.
- The **catalog** track (`ValidationPipeline` → `DatabaseIntelligenceOrchestrator` →
  `CatalogRulesValidationAgent`, plus the `KillChainCommitGate` reused inside `CatalogWriteGate`)
  checks catalog integrity (kill-chain reachability, link/catalog rules) before a write-gate
  commit lands.

This page documents both as a subsystem — the rule catalog, the deterministic report hash, the
severity/export-floor model, and how to extend either track without moving a golden hash.

The row *schema decision* and the "export gate is the sole gate" rule are
[ADR-008](../architecture/adr-008-mission-editor-validation-engine.md); the event-graph caps are
[ADR-016](../architecture/adr-016-event-graph-complexity-caps.md); the tuning knobs come from the
[agentic mission editor GDD](../../design/gdd/agentic-mission-editor.md) §4.3/§7. This is the
*validation* reference — the document model it consumes is
[scenario-document-authoring.md](scenario-document-authoring.md), the interactive editing host is
[scenario-authoring-host.md](scenario-authoring-host.md), the event-graph static analysis that
feeds in is [scenario-event-system.md](scenario-event-system.md), and the write gate that consumes
the kill-chain verdict is [catalog-write-gate.md](catalog-write-gate.md). Verified against source
and pinned by the tests at the end.

- **Scenario engine:** [`ScenarioValidationEngine.Validate`](../../src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs)
  — runs a fixed ordered rule list → `ValidationReport`.
- **Rule pack:** [`ValidationRules`](../../src/ProjectAegis.Data/Validation/Rules/ValidationRules.cs)
  (internal static) + [`EventStaticAnalyzer`](scenario-event-system.md).
- **Finding / severity:** [`ValidationFinding`](../../src/ProjectAegis.Data/Validation/ValidationFinding.cs) /
  [`ValidationSeverity`](../../src/ProjectAegis.Data/Validation/ValidationSeverity.cs) (`Info=0`, `Warning=1`, `Error=2`).
- **Report + hash:** [`ValidationReport`](../../src/ProjectAegis.Data/Validation/ValidationReport.cs)
  — `(Passed, Findings, ReportHash)`, sorted + SHA-256.
- **Export gate:** [`ScenarioValidationExportGate.EvaluateExport`](../../src/ProjectAegis.Data/Validation/ScenarioValidationExportGate.cs)
  — the single export/play/simulate gate.
- **Config:** [`ValidationConfig`](../../src/ProjectAegis.Data/Validation/ValidationConfig.cs) +
  [`ValidationConfigLoader`](../../src/ProjectAegis.Data/Validation/ValidationConfigLoader.cs)
  (`assets/data/editor/validation-config.json`).
- **Catalog kill-chain gate:** [`KillChainCommitGate`](../../src/ProjectAegis.Data/Validation/KillChainCommitGate.cs)
  over [`KillChainRules`](../../src/ProjectAegis.Data/Validation/KillChainRules.cs).

---

## Design invariants — never break these

Load-bearing and enforced by tests / the golden gates. Preserve them when touching any piece here.

| Invariant | Rule |
|-----------|------|
| **Export gate is the *sole* gate** | Every export/play/simulate/publish path (`scenario_validate`, `ScenarioExportCommand.Prepare`, `scenario_simulate_sample`, play) calls `ScenarioValidationExportGate.EvaluateExport` and is blocked when any finding meets/exceeds `ValidationConfig.ExportBlockSeverityFloor` (default `Error`). Do not add a second, divergent export path. |
| **Save deliberately bypasses validation** | `ScenarioDocumentEditor.Save` persists WIP even with blocking errors (AME-6.5 / AC-12). Validation runs at the *export* boundary, not the save boundary — never move the gate into `Save`. |
| **Reports are deterministic & content-hashed** | `ValidationReport.FromFindings` sorts findings by `(Severity↓, Code, MissionId, UnitId, TargetId, Message)` ordinal and hashes them (SHA-256 over `severity\|code\|missionId\|unitId\|targetId\|message\n`). The hash is a **golden contract** (`ValidationGoldenHashes`). A changed hash means the finding set changed — regenerate goldens, never hand-edit. |
| **Fresh report every call** | `EvaluateExport` always re-runs the engine and computes a fresh report; nothing is cached across edits. Keep rules pure functions of `(scenario, catalog, config)` so identical inputs always hash identically. |
| **`Passed` ≠ `CanExport`** | `Passed` is "no `Error` findings"; `CanExport(config)` is "no finding `>= ExportBlockSeverityFloor`". They coincide at the default floor but diverge if the floor is lowered to `Warning`. Use `CanExport` for gate decisions, `Passed` only for the report summary. |
| **Warnings never block; only the hard cap does** | The ADR-016 event-graph complexity/density findings are `Warning` (export-honest, non-blocking). The only event-graph *blocker* is the per-event `MaxConditionsPerEvent` hard cap (`EVENT_CONDITION_CAP_EXCEEDED`, `Error`). Do not promote a soft warning to `Error`. |
| **`editorState` is derived-only** | `EditorStateSchemaLint` (AC-9) forbids `ProjectAegis.Sim` / `ProjectAegis.Data` from reading `editorState`. Validation rules must never branch on UI/editor state — only on canonical scenario + catalog data. |
| **Kill-chain gate blocks catalog commits** | `KillChainCommitGate.GetBlockingReasons` builds a post-staging catalog preview and returns `kill_chain:<CODE>` reasons for any `KILL_CHAIN_*` error; `CatalogWriteGate` refuses the commit. This is **extend-only** — add codes, never relax an existing block (see [catalog-write-gate.md](catalog-write-gate.md)). |

---

## Track 1 — scenario validation

`ScenarioValidationEngine.Validate(scenario, catalog, config)` is a pure, deterministic v1
pipeline (ADR-008). It appends findings from a **fixed ordered rule list**, then folds them into a
sorted, hashed `ValidationReport`:

```csharp
var findings = new List<ValidationFinding>();
ValidationRules.TlBranchRule(scenario, catalog, findings);
ValidationRules.TlReleaseTrainRule(scenario, catalog, findings);
ValidationRules.DbRefRule(scenario, catalog, findings);
ValidationRules.MissionNoUnitsRule(scenario, findings);
ValidationRules.PatrolZoneRule(scenario, findings);
ValidationRules.StrikeNoTargetsRule(scenario, findings);
ValidationRules.FerryDestinationRule(scenario, findings);
ValidationRules.AirReadyLaunchRule(scenario, findings);
ValidationRules.FerryReachabilityRule(scenario, catalog, config, findings);
ValidationRules.StrikeReachabilityRule(scenario, catalog, config, findings);
ValidationRules.IncompatibleHostRule(scenario, findings);
ValidationRules.BrokenRefRule(scenario, findings);
ValidationRules.DoctrineInheritanceRule(scenario, findings);
ValidationRules.EventGraphComplexityRule(scenario, config, findings);   // ADR-016
findings.AddRange(EventStaticAnalyzer.Analyze(scenario));                // ME-W2 warnings
return ValidationReport.FromFindings(findings);
```

The order the rules run in does **not** affect the report — `FromFindings` re-sorts before hashing
(see the determinism invariant). The rules produce a stable, machine-readable code catalog:

| Code | Severity | Rule / meaning |
|------|----------|----------------|
| `TL_BRANCH_MISSING` / `TL_BRANCH_INVALID` | Error | `metadata.tlBranch` missing or not a `TL-0…TL-5` tier. |
| `TL_BRANCH_SNAPSHOT_MISMATCH` | Error | Resolved snapshot's `catalog_snapshot.branch` ≠ scenario `tlBranch`. |
| `TL_RELEASE_TRAIN_NOT_FOUND` / `TL_RELEASE_TRAIN_MISMATCH` | Error | No snapshot in the release train for the tier, or an explicit `dbRef` resolves off the tier's train (see [catalog-release-train.md](catalog-release-train.md)). |
| `DB_MISMATCH` | Error | `metadata.dbRef` / `dbSnapshotId` does not resolve to an available snapshot. |
| `MISSION_NO_UNITS` | Error | A mission has no assigned units. |
| `PATROL_ZONE_DEGENERATE` | Error | A `Patrol` mission has `< 3` waypoints. |
| `STRIKE_NO_TARGETS` | Error | A `Strike` mission has no targets. |
| `FERRY_NO_DESTINATION` | Error | A `Ferry` mission has no destination base. |
| `AIR_NOT_READY` | Error | A `Strike`-assigned unit has `readyForLaunch=false` in `metadata.unitReadiness`. |
| `STRIKE_INVALID_PLATFORM` | Error | A `Strike` unit has an invalid `combat_radius_nm`. |
| `STRIKE_UNREACHABLE` / `STRIKE_UNREACHABLE_FUEL` | Error | Target beyond combat radius / within radius but over the fuel budget (see reachability below). |
| `FERRY_UNREACHABLE` / `FERRY_UNREACHABLE_FUEL` | Error | Same reachability classification for a ferry destination. |
| `INCOMPATIBLE_HOST` | Error | Model-integrity check (e.g. air unit with no carrier host). |
| `BROKEN_REF` | Error | A `ref:`-prefixed target id points at no known unit. |
| `EVENT_CONDITION_CAP_EXCEEDED` | Error | An event exceeds `MaxConditionsPerEvent` (hard ADR-016 cap). |
| `EVENT_GRAPH_COMPLEXITY_HIGH` / `EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH` | Warning | ADR-016 soft caps — **never block export**. |
| `DOCTRINE_RESOLVED` | Info | Per-mission ROE resolution provenance (`override` vs `side`); surfaced in the JSON report's `doctrineResolution` block. |

`EventStaticAnalyzer` adds its own event-graph warnings (dead triggers, unreachable actions,
contradictions, cycles); those codes are documented in
[scenario-event-system.md](scenario-event-system.md).

> **Note on the rule seam.** `IScenarioValidationRule` (`RuleId` + `Evaluate`) is defined as the
> declared extension interface, but the v1 engine wires the `ValidationRules` methods **explicitly**
> rather than iterating a registry. Add a new rule by following the "add a scenario rule" runbook
> below, not by expecting auto-discovery.

### The report and its hash

```csharp
public sealed record ValidationReport(bool Passed, IReadOnlyList<ValidationFinding> Findings, string ReportHash);

// Passed  = Findings.All(f => f.Severity < Error)
// CanExport(config) = !Findings.Any(f => f.Severity >= config.ExportBlockSeverityFloor)
```

`ReportHash` is a lower-hex SHA-256 over the sorted findings, one `severity|code|missionId|unitId|targetId|message`
line each. It is the value pinned by `ValidationGoldenHashes` (e.g. `CleanPatrol`,
`StrikeUnreachable`) and by the CI golden tests, so it is a *behavioural fingerprint* of the whole
rule pack.

### Fuel reachability

`ReachabilityCalculator` backs the `STRIKE_*` / `FERRY_*` codes with a haversine great-circle
distance (`EarthRadiusNm = 3440.065`) and a round-trip fuel budget (GDD §4.1):

```
fuelBudgetNm = combatRadiusNm * FuelFraction − IngressEgressPadNm   // default 0.85 / 50 nm

distanceNm <= fuelBudgetNm        → reachable
distanceNm >  combatRadiusNm      → STRIKE_UNREACHABLE       (excess = distance − combatRadius)
otherwise                         → STRIKE_UNREACHABLE_FUEL  (excess = distance − fuelBudget)
```

A non-positive `combatRadiusNm` is treated as "cannot classify" and is skipped (the strike rule
raises `STRIKE_INVALID_PLATFORM` separately). `excess_nm` is rounded to one decimal and attached to
the finding `Data`.

### Config

`ValidationConfig` is a `record` of GDD §7 / ADR-008 / ADR-016 knobs, loaded from
`assets/data/editor/validation-config.json` by `ValidationConfigLoader` (falling back to the
record defaults when the file is absent):

| Field | Default | Purpose |
|-------|---------|---------|
| `IngressEgressPadNm` | `50` | Reserved reach for ingress/egress in the reachability budget. |
| `FuelFraction` | `0.85` | Fraction of combat radius usable for the mission leg. |
| `ExportBlockSeverityFloor` | `Error` | Findings at/above this severity block export. Lower to `Warning` to make warnings blocking. |
| `ComplexityWarnThreshold` | `400` | Soft event-graph complexity warning (`E + Σconds + CrossRefWeight·refs`). |
| `DensityWarnThreshold` | `20` | Soft peak-tick-density warning. |
| `CrossRefWeight` | `2` | Weight applied to cross-references in the complexity proxy. |
| `MaxConditionsPerEvent` | `32` | **Hard** cap; the only event-graph blocker. |

### Where the gate is called

`ScenarioValidationExportGate.EvaluateExport(scenario, catalog, config)` returns
`(bool Allowed, ValidationReport Report)` and is the single choke point:

| Caller | Path | On block |
|--------|------|----------|
| `ScenarioValidateCommand` (`scenario_validate` CLI/MCP) | headless validate | prints the JSON report; exit code `1`. |
| `ScenarioExportCommand.Prepare` | publish + `scenario_simulate_sample` + play | `export_allowed=false`, no export document produced. |
| `scenario_export_brief` | brief export | blocked at the same floor. |

The report is serialized for tooling by `ValidationReportJsonDto.Serialize` (camelCase, null-ignoring,
indented) — carrying `passed`, `canExport`, `reportHash`, the `findings` array, and the derived
`doctrineResolution` list.

---

## Track 2 — catalog validation & the kill-chain commit gate

The catalog track validates *catalog integrity* rather than a scenario. It runs through the
`ValidationPipeline` (a thin P0 wrapper, req-06) which delegates to the
`DatabaseIntelligenceOrchestrator`; the orchestrator's `CatalogRulesValidationAgent` runs the
`KillChainRules` and `LinkCatalogRules` packs over an `ICatalogReader`.

`KillChainRules` (DBI-3.5, bounded detect-only R1–R4) emits `DatabaseAgentFinding`s with these codes:

| Code | Meaning |
|------|---------|
| `KILL_CHAIN_ORPHAN_EDGE` | A dependency edge references a missing platform / approved sensor / mount / weapon. |
| `KILL_CHAIN_RANGE_EXCEEDS_SENSOR` | Weapon max range exceeds the platform's approved-sensor envelope. |
| `KILL_CHAIN_SPEED_MISMATCH` | Inferred weapon min speed exceeds platform max speed (skipped silently when mobility is absent, to keep the clean Baltic seed empty). |
| `KILL_CHAIN_WEAPON_EXCEEDS_PLATFORM_REACH` | Weapon max range exceeds the platform's combat-radius reach. |

`KillChainCommitGate` reuses those rules as an **extend-only write-gate blocker**:

```csharp
// CatalogWriteGate: build a post-staging preview, then block on any KILL_CHAIN_* error
var reasons = KillChainCommitGate.GetBlockingReasons(preview);   // ["kill_chain:KILL_CHAIN_ORPHAN_EDGE", …]
if (reasons.Count > 0) return new WriteGateDecision(false, batchId, reasons);
```

Only `error`-severity `KILL_CHAIN_*` findings block; the reasons are `kill_chain:`-prefixed, sorted,
and de-duplicated. The same pack drives the read-only `catalog_kill_chain_report` CLI verb.

Both tracks share the sorted-then-SHA-256 hashing idiom, so catalog findings are pinned too
(`KillChainGoldenHashes.BalticPatrolClean`, and the Phase-B workbook fixtures in
`ValidationGoldenHashes`).

---

## Producer / consumer map

| Role | Type | What it does |
|------|------|--------------|
| **Producer** | `ScenarioValidationEngine` | Runs the scenario rule pack → `ValidationReport`. |
| **Producer** | `KillChainRules` / `LinkCatalogRules` (via `CatalogRulesValidationAgent`) | Runs the catalog rule packs → `DatabaseAgentFinding`s. |
| **Gate** | `ScenarioValidationExportGate` | Sole export/play/simulate/publish gate for scenarios. |
| **Gate** | `KillChainCommitGate` | Blocks `CatalogWriteGate` commits on catalog kill-chain errors. |
| **Consumer** | `ScenarioValidateCommand` (`scenario_validate`) | CLI/MCP verb; serializes the report, sets exit code. |
| **Consumer** | `ScenarioExportCommand.Prepare` | Publish / simulate-sample / play export preparation. |
| **Consumer** | `ValidationReportJsonDto` | Tooling-facing JSON projection (incl. `doctrineResolution`). |
| **Consumer** | `ValidationGoldenHashes` / `KillChainGoldenHashes` + golden tests | Pin the report/finding hashes in CI. |
| **Guardrail** | `EditorStateSchemaLint` | Asserts `editorState` stays derived-only (AC-9). |

---

## Runbooks

### Add a scenario validation rule

1. Add a static method to `ValidationRules` that appends `ValidationFinding`s to the `sink`. Keep it
   a **pure function** of `(scenario, catalog, config)` — no clocks, no RNG, no `editorState`. Pick a
   new stable `SCREAMING_SNAKE_CASE` code and the correct severity (`Error` blocks export at the
   default floor; `Warning`/`Info` do not).
2. Wire the call into `ScenarioValidationEngine.Validate` (position is irrelevant — the report
   re-sorts before hashing).
3. Add unit tests next to the existing rule tests, and — because you changed the finding set — expect
   the `CleanPatrol` / `StrikeUnreachable` golden report hashes to move. Regenerate them from the
   fixtures and update `ValidationGoldenHashes`; **never** hand-edit a hash to make a test pass.

### Add a kill-chain / catalog rule

Add the rule to `KillChainRules` (or `LinkCatalogRules`) with a new `KILL_CHAIN_*` code and `error`
severity if it should block commits. It automatically flows through `KillChainCommitGate` (extend-only)
and the `catalog_kill_chain_report` verb. Update `KillChainGoldenHashes.BalticPatrolClean` only after
confirming the clean Baltic seed stays empty — a non-empty clean seed breaks the CLI empty-golden and
CrossSystem pins.

### Change a tuning knob

Edit `assets/data/editor/validation-config.json` (not the C# defaults) — the loader reads it at
`ValidationConfigLoader.LoadFromRepo`. Lowering `exportBlockSeverityFloor` to `warning` makes soft
warnings blocking; changing thresholds only affects the soft warnings, never the `MaxConditionsPerEvent`
hard cap. Any knob change that alters a golden scenario's finding set requires a golden-hash regen.

---

## Pinned by tests

| Test | Guards |
|------|--------|
| `ScenarioValidationEngineTests` | The rule pack produces the expected findings for representative scenarios. |
| `ValidationGoldenTests` | `CleanPatrol` / `StrikeUnreachable` (+ Phase-B workbook) report hashes are stable. |
| `ValidationPipelineTests` | The catalog P0 pipeline runs end-to-end over the Baltic reader. |
| `SaveVsExportGateTests` | `Save` persists blocking-error WIP; export is refused at the `Error` floor. |
| `EventGraphComplexityTests` | ADR-016 soft warnings never block; the `MaxConditionsPerEvent` hard cap blocks. |
| `ReachabilityCalculatorTests` | Haversine + fuel-budget classification (`STRIKE_UNREACHABLE` vs `_FUEL`). |
| `TlBranchValidationTests` / `TlReleaseTrainValidationTests` | TL-tier + release-train binding rules. |
| `DoctrineInheritanceValidateTests` | ROE `override` vs `side` resolution → `DOCTRINE_RESOLVED`. |
| `KillChainRulePackTests` / `LinkCatalogRulePackTests` | Catalog rule-pack findings + hashes. |
| `ScenarioDocumentEditorLiveValidationTests` | Live validation during interactive editing. |
| `NoDynamicExecutionGateTests` | The export gate carries no dynamic-execution / LLM surface (ADR-014). |
