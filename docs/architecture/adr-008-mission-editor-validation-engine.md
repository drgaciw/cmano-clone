# ADR-008: Mission Editor Deterministic Validation Engine

## Status

**Accepted**

## Date

2026-06-02

## Last Verified

2026-06-03 (engine + headless MCP CLI + golden hashes in CI)

## Decision Makers

Architecture review 2026-06-02; Mission Editor GDD (system 11) design gate

## Summary

The Agentic Mission Editor needs a **pure, deterministic rule engine** over the canonical `scenario.json` that returns the same findings for the same inputs on every run — no LLM, no Unity, no cached pass state. **Decided:** implement `IScenarioValidationEngine` in **`ProjectAegis.Data.Validation`** (`ProjectAegis.Data` assembly), consuming scenario DTOs and `ICatalogReader` for `dbRef` resolution and `combat_radius_nm`, with six v1 rules and stable-sorted `ValidationFinding` output. The engine **blocks export, play, and headless sample** but **never blocks save**; MCP `scenario_validate` and export paths always re-run validation fresh.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.14f1) + .NET 8 headless |
| **Domain** | Core / Data (authoring validation — not sim tick, not rendering) |
| **Knowledge Risk** | LOW — pure .NET 8 library; no `UnityEngine` reference |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `design/gdd/agentic-mission-editor.md` §3.6–§4.1, `docs/architecture/adr-006-data-layer-boundary.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Byte-identical `ValidationReport` JSON for fixed fixture + config across two runs on Linux and Windows CI; schema lint confirms no `editorState` reads (AC-9) |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | **ADR-006** (Accepted — validation lives in `ProjectAegis.Data`, uses `ICatalogReader`, no SQLite in editor host); **ADR-001** (Accepted — Sim does not own scenario authoring validation); **ADR-004** (Accepted — deterministic ordering discipline for findings and rule evaluation) |
| **Enables** | TR-editor-002; MCP `scenario_validate` / export gate (TR-editor-005); Unity editor export/play gate; Phase 2/3 LLM agents (advisory only — never replace this engine) |
| **Blocks** | `scenario_export_brief`, `scenario_simulate_sample`, and play-from-editor until this ADR is **Accepted** and six v1 rules are green in CI |
| **Ordering Note** | Accept **after** ADR-006; extend `ICatalogReader` with platform reachability queries before or in parallel with EDITOR-VAL-1 (DATA catalog field `combat_radius_nm` must match GDD §4.1 round-trip convention) |

## Context

### Problem Statement

The mission editor GDD defines an **intent compiler**: one canonical `scenario.json` is the sole authority for sim, validation, and MCP. Without a specified, headless, deterministic validation layer, export/play can ship broken strikes, empty patrols, or DB-bound scenarios that fail only at runtime — undermining the v1 differentiator ("never ship something broken silently"). The cost of deferring this ADR is continued **TR-editor-002 Gap** and ad hoc validation in Unity or MCP hosts, which violates ADR-006 and breaks headless/CI parity.

### Current State

- `ProjectAegis.Data` exists with `ICatalogReader` (sensor `basePd` paths); **no** scenario validation engine or canonical scenario DTO loader in Data yet.
- GDD `agentic-mission-editor.md` locks six v1 rules, fuel-reachability formula (§4.1), save-vs-export gates (§5), and AC-1/AC-3/AC-12 test contracts.
- Requirements doc 11 still uses legacy "Validation Agent" wording; implementation follows GDD **Validation Engine** (deterministic code only).

### Constraints

- **No Unity** in `ProjectAegis.Data.Validation` — same assembly boundary as ADR-006.
- **No LLM** in any blocking path (GDD §3.6).
- **No authoritative cached validation** — export/play/sample always re-run (GDD §3.3).
- **`editorState` is never an input** to validation (GDD §3.3, AC-9).
- Findings must be **reproducible**: same `scenario.json` + same `ValidationConfig` + same catalog snapshot → identical ordered findings (ADR-004 spirit).
- v1 scope is **exactly six rules**; doctrine inheritance display (AC-4), event complexity warnings (§4.3), and catalog P0 schema rules remain separate concerns.

### Requirements

- Pure C# rule engine: same file in → same findings out.
- Six v1 blocking rules with stable error codes (GDD §3.6, AC-3).
- Strike fuel-reachability per §4.1 with tunable `ingress_egress_pad_nm` and `fuel_fraction` from `validation-config.json`.
- Consume canonical scenario JSON (deserialized to immutable DTOs in Data).
- Resolve `metadata.dbRef` and platform `combat_radius_nm` via `ICatalogReader` (and snapshot binding when DATA-4 `dbSnapshotId` is wired — until then, `dbRef` string match against reader layer version / snapshot id).
- **Save allowed** with blocking errors; **export / play / `scenario_simulate_sample` rejected** with full finding list (AC-12).
- MCP mutating tools do not bypass validation; `scenario_validate` returns the report; export tools call validation and fail on blocking severity per config floor.
- Performance: full v1 pass on Baltic-scale fixture (≤ ~500 ORBAT entries, ≤ ~50 missions) in **&lt; 100 ms** on CI hardware (see Performance Implications).

## Decision

Implement a **deterministic validation pipeline** in namespace **`ProjectAegis.Data.Validation`**, hosted in the existing **`ProjectAegis.Data`** project (`net8.0` for headless tests; consumable from Unity editor host and MCP CLI via project reference only — no `UnityEngine`).

### Architecture

```
  *.aegis-scenario / scenario.json
           │
           ▼
  ┌────────────────────────────┐
  │ ScenarioPackageLoader     │  (Data — canonical DTOs, ignores editorState)
  └─────────────┬──────────────┘
                │ ScenarioDocumentDto
                ▼
  ┌────────────────────────────┐     assets/data/editor/validation-config.json
  │ IScenarioValidationEngine │◄── ValidationConfig (pad, fuel_fraction, severity floor)
  └─────────────┬──────────────┘
                │
    ┌───────────┼───────────┬──────────────┐
    ▼           ▼           ▼              ▼
 Rule 1..6   ICatalogReader  PolicyResolver  (v2: event complexity)
 (ordered)   dbRef/combat_radius   (AC-4, not v1 six)
                │
                ▼
  ┌────────────────────────────┐
  │ ValidationReport            │  findings[] stable-sorted
  │  - findings                 │  blocking = severity >= floor
  │  - passed                   │
  │  - reportHash (SHA-256)     │  optional CI fingerprint
  └─────────────┬──────────────┘
                │
     ┌──────────┴──────────┬─────────────────────┐
     ▼                     ▼                     ▼
 Unity Editor          MCP scenario_validate   Export / play / sample gates
 (inline + export)      (headless)              (re-run every time)
```

**Rule pipeline (fixed order for determinism):**

| Order | Rule ID | Code | Severity | Trigger |
|-------|---------|------|----------|---------|
| 1 | `DbRefRule` | `DB_MISMATCH` | Error | `metadata.dbRef` does not resolve to available catalog snapshot / reader binding |
| 2 | `MissionNoUnitsRule` | `MISSION_NO_UNITS` | Error | Any mission with zero assigned units |
| 3 | `PatrolZoneRule` | `PATROL_ZONE_DEGENERATE` | Error | Patrol mission zone &lt; 3 vertices or zero area |
| 4 | `StrikeNoTargetsRule` | `STRIKE_NO_TARGETS` | Error | Strike with empty `targets[]` |
| 5 | `FerryDestinationRule` | `FERRY_NO_DESTINATION` | Error | Ferry missing/invalid friendly destination base |
| 6 | `StrikeReachabilityRule` | `STRIKE_UNREACHABLE` | Error | Strike target not fuel-reachable per §4.1; message includes excess nm `N` |

**Non-v1 (explicitly out of this ADR's implementation gate):** zero-distance strike **warning** (`STRIKE_COLOCATED_WARNING`), event complexity (§4.3), doctrine resolved display (AC-4), dangling event refs, TeleportUnit export transform (AC-11) — may be separate rule packs once six core rules are green.

**Export gate semantics:**

```csharp
bool CanExport(ValidationReport report, ValidationConfig config) =>
    !report.Findings.Any(f => f.Severity >= config.ExportBlockSeverityFloor);
// Default floor: Error. Save does not call CanExport.
```

### Key Interfaces

```csharp
namespace ProjectAegis.Data.Validation;

public enum ValidationSeverity { Info = 0, Warning = 1, Error = 2 }

public sealed record ValidationFinding(
    string Code,                    // e.g. "STRIKE_UNREACHABLE"
    ValidationSeverity Severity,
    string Message,                 // human-facing; stable template + parameters
    string? MissionId = null,
    string? UnitId = null,
    string? TargetId = null,
    IReadOnlyDictionary<string, string>? Data = null  // e.g. ["excess_nm"] = "68"
);

public sealed record ValidationReport(
    bool Passed,
    IReadOnlyList<ValidationFinding> Findings,
    string ReportHash);             // SHA-256 over canonical serialized findings

public interface IScenarioValidationEngine
{
    ValidationReport Validate(
        ScenarioDocumentDto scenario,
        ICatalogReader catalog,
        ValidationConfig config);
}

// Stable sort for findings (total order):
// (Severity DESC, Code ASC, MissionId ASC, UnitId ASC, TargetId ASC, Message ASC)
```

**`ICatalogReader` extension (Data catalog seam):**

```csharp
// Add to ProjectAegis.Data.Catalog.ICatalogReader
bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId);
bool TryGetCombatRadiusNm(string platformId, out double combatRadiusNm);
bool TryResolvePlatform(string platformOrTargetId, out PlatformCatalogEntry entry);
```

Reads use **stable sort keys** already required by ADR-006. Strike reachability implementation must use **haversine** in nautical miles, **`combat_radius_nm` as round-trip budget** (do not double distance — GDD §4.1), and reject `combat_radius_nm <= 0` as `STRIKE_INVALID_PLATFORM` (Error) before reachability math.

**Config binding** (`ValidationConfig`):

| Field | Source | Default |
|-------|--------|---------|
| `IngressEgressPadNm` | `validation-config.json` | 50 |
| `FuelFraction` | `validation-config.json` | 0.85 |
| `ExportBlockSeverityFloor` | `validation-config.json` | `Error` |

Reject config values outside GDD ranges at load time (configuration error, not scenario finding).

### Implementation Guidelines

1. **Project layout:** `src/ProjectAegis.Data/Validation/` — `IScenarioValidationEngine.cs`, `ScenarioValidationEngine.cs`, `Rules/*.cs`, `ValidationConfig.cs`, `ReachabilityCalculator.cs` (pure static math, injectable only for test doubles of Earth model if needed).
2. **DTOs:** Scenario types live under `ProjectAegis.Data.Scenario` (or `Scenario/Authoring/`); loader strips/ignores `editorState` on deserialize; no validation rule reads `editorState`.
3. **Determinism checklist (ADR-004 aligned):**
   - Rules run in fixed order; each rule emits findings sorted locally, then merge-sort by global finding key.
   - No `DateTime.UtcNow`, no `Guid.NewGuid()`, no dictionary enumeration without sort.
   - No parallelism in v1 pipeline.
4. **MCP contract:** `scenario_validate` deserializes package, loads catalog for `dbRef`, runs `Validate`, returns JSON report. `scenario_export_brief` and export paths call the same engine; on `!Passed`, return findings and do not write export artifact.
5. **Unity host:** Editor assembly references `ProjectAegis.Data` only for validation; UI shows findings inline (red outline) but does not persist pass/fail into `editorState`.
6. **Tests:** One test class per rule under `tests/unit/editor/validation/` (AC-3a–f); AC-1 asserts `STRIKE_UNREACHABLE` and `excess_nm`; AC-12 save-vs-export; golden report hash optional in `tests/integration/editor/validation/`.
7. **GitNexus:** Run `gitnexus impact` before extending `ICatalogReader` or adding public validation types consumed by MCP.

## Alternatives Considered

### Alternative 1: LLM-based validation (GDD "agents")

- **Description:** Use an LLM to review scenario JSON and flag issues in natural language.
- **Pros:** Flexible wording; catches novel authoring mistakes.
- **Cons:** Non-deterministic; cannot satisfy AC-2/AC-3; blocks headless CI; violates v1 determinism pillar.
- **Estimated Effort:** Similar integration cost, unbounded ops cost.
- **Rejection Reason:** GDD explicitly reserves LLM for Phase 2/3 advisory agents; **deterministic engine is the sole export gate**.

### Alternative 2: Validation in `ProjectAegis.Sim` or Unity editor scripts

- **Description:** Run rules inside sim tick setup or Unity `OnValidate`.
- **Pros:** Convenient access to sim-specific state.
- **Cons:** Blurs ADR-001/ADR-006; no headless MCP parity; tempts reading `editorState` or sim-only caches.
- **Estimated Effort:** Lower short-term, high rework.
- **Rejection Reason:** Authoring validation is a **data-layer concern** over canonical file + catalog, pre-sim.

### Alternative 3: Cache validation results in `editorState` or sidecar file

- **Description:** Store last `ValidationReport` to avoid re-running on every export click.
- **Pros:** Faster UI if unchanged.
- **Cons:** Stale-cache export bypass (GDD §3.3 blocker in review).
- **Estimated Effort:** Low.
- **Rejection Reason:** GDD forbids authoritative cached validation; hosts may memoize **only** with content hash keyed re-run (optimization, not semantics).

### Alternative 4: Merge with Database Intelligence `ValidationPipeline` (doc 06)

- **Description:** Single validation pipeline for catalog writes and scenarios.
- **Pros:** One `ValidationFinding` type system-wide.
- **Cons:** Different inputs (catalog rows vs scenario missions); couples editor delivery to DATA-4 write gate.
- **Estimated Effort:** Medium consolidation.
- **Rejection Reason:** **Share types** (`ValidationFinding`, `ValidationSeverity`) where practical, but **keep scenario engine separate** from catalog `IWriteGate` validation to preserve ADR-006 phase boundaries.

## Consequences

### Positive

- Closes TR-editor-002; unblocks trustworthy export and MCP headless workflows.
- Same validation in Unity, CLI, and CI — intent-compiler invariant enforced.
- Six rules are exhaustively testable (AC-3); fuel math matches logistics GDD cross-reference.
- Clear extension point for v2 rules (events, warnings) without changing export contract.

### Negative

- Requires `ICatalogReader` and Platform DB schema extension for `combat_radius_nm` before rule 6 is meaningful in production DBs.
- Every export/play pays full validation cost (mitigate with hash-keyed memoization in hosts only).
- Scenario DTO + loader must be maintained in parallel with `scenario.json` schema evolution.

### Neutral

- Renames requirements "Validation Agent" to **Validation Engine** in code/docs over time.
- `ValidationReport.ReportHash` becomes an optional CI artifact alongside replay hashes (orthogonal to sim tick hashes).

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| `combat_radius_nm` missing or wrong convention in Platform DB | Medium | High — false positives/negatives on rule 6 | GDD §4.1 locked; DATA schema sign-off; unit tests with fixture catalog |
| `dbRef` binding ambiguous pre-DATA-4 snapshots | Medium | Medium — `DB_MISMATCH` false positives | `TryResolveDbRef` documents resolution rules; test with Baltic snapshot id |
| Finding sort instability across locales | Low | High — breaks determinism | Ordinal string sort; invariant culture for numeric formatting in messages |
| Scope creep (AC-4 doctrine, §4.3 warnings) delays v1 six rules | Medium | Medium | Explicit non-v1 list in this ADR; separate stories |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|----------------|--------|
| CPU (validation pass) | N/A (not implemented) | &lt; 100 ms for Baltic-scale scenario | 100 ms per `scenario_validate` call |
| Memory | N/A | Transient DTO + catalog lookups; no scenario mutation | &lt; 50 MB peak for validation call |
| Load Time | N/A | Config + catalog reader already loaded in editor session | Amortized |
| Network | N/A | None (local file + local catalog) | N/A |

## Migration Plan

| Phase | Scope | Verification |
|-------|--------|--------------|
| **EDITOR-VAL-0** | Namespace scaffold, `ValidationFinding` / `ValidationSeverity` / `IScenarioValidationEngine`, empty engine returns `Passed` | `dotnet test` Data.Tests |
| **EDITOR-VAL-1** | Scenario DTO loader (ignores `editorState`), `ValidationConfig` from JSON | Loader round-trip fixtures |
| **EDITOR-VAL-2** | Rules 1–5 (DB mismatch, no units, patrol, strike targets, ferry) | AC-3a–e unit tests |
| **EDITOR-VAL-3** | `ICatalogReader` reachability + rule 6 + `ReachabilityCalculator` | AC-1, AC-3f |
| **EDITOR-VAL-4** | MCP `scenario_validate` + export/play/sample gates | AC-5, AC-12 integration |
| **EDITOR-VAL-5** | CI schema lint: no `editorState` reads in Validation assembly (AC-9) | CI lint job |

1. Land types and interfaces without wiring Unity (no user-visible change).
2. Implement six rules with fixtures from `tests/fixtures/editor/`.
3. Wire MCP and editor export gate to call engine on every blocking operation.
4. Update `architecture-traceability-index.md` TR-editor-002 → **Partial** then **Met** when AC-3 green.

**Rollback plan:** Feature-flag export gate to warning-only via `ExportBlockSeverityFloor` temporarily; remove MCP validation call only if critical regression — **do not** remove engine from Data layer once consumers depend on it.

## Validation Criteria

- [ ] **Determinism:** Two consecutive `Validate` calls on the same fixture + config produce byte-identical canonical `Findings` JSON and `ReportHash`.
- [ ] **AC-3:** Each of the six v1 rules has a dedicated unit test with expected `Code` (`MISSION_NO_UNITS`, `PATROL_ZONE_DEGENERATE`, `STRIKE_NO_TARGETS`, `FERRY_NO_DESTINATION`, `DB_MISMATCH`, `STRIKE_UNREACHABLE`).
- [ ] **AC-1:** `STRIKE_UNREACHABLE` message includes excess nm; `Data["excess_nm"]` matches computed `N`.
- [ ] **AC-12:** Save succeeds with blocking errors; export/play/sample reject with same finding list.
- [ ] **AC-9:** No type under `ProjectAegis.Data.Validation` references `editorState` fields (CI lint).
- [ ] **ADR-006:** No `UnityEngine` reference in `ProjectAegis.Data`; no SQLite opened from validation rules (catalog via `ICatalogReader` only).
- [ ] **Performance:** Baltic-scale fixture validation &lt; 100 ms on CI runner (logged in test output).

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|---------------------------|
| `design/gdd/agentic-mission-editor.md` | Editor (11) | Deterministic Validation Engine — six v1 rules, no LLM (§3.6) | `IScenarioValidationEngine` + six ordered rules with stable codes |
| `design/gdd/agentic-mission-editor.md` | Editor (11) | Strike fuel-reachability (§4.1) | `StrikeReachabilityRule` + `ReachabilityCalculator` using `combat_radius_nm` from `ICatalogReader` |
| `design/gdd/agentic-mission-editor.md` | Editor (11) | Save vs export gate (§5, AC-12) | `CanExport` uses severity floor; save path does not require `Passed` |
| `design/gdd/agentic-mission-editor.md` | Editor (11) | No cached validation / no `editorState` input (§3.3, AC-9) | Hosts re-run engine; loader ignores `editorState` |
| `design/gdd/agentic-mission-editor.md` | Editor (11) | MCP `scenario_validate` before export (§3.7) | Shared `ValidationReport` contract for MCP and Unity |
| `design/gdd/logistics-magazines.md` | Logistics | Editor strike fuel check aligns with sustainment | Same §4.1 formula as GDD editor (code `STRIKE_UNREACHABLE`) |
| `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` | Editor | Continuous validation blocks broken export | Export/play/sample gate on `ValidationReport.Passed` |
| `Game-Requirements/requirements/06-Database-Intelligence.md` | DB | Scenario ↔ DB version binding | Rule 1 `DB_MISMATCH` via `dbRef` / snapshot resolution |

## Related

- **Depends on:** [ADR-006](adr-006-data-layer-boundary.md), [ADR-001](adr-001-sim-assembly-boundary.md), [ADR-004](adr-004-tick-pipeline-order.md)
- **Enables:** Mission editor MCP suite export gate; closes architecture traceability **TR-editor-002**
- **Does not supersede:** Catalog `IWriteGate` validation (DATA-4) — orthogonal pipeline, shared finding types where useful
- **Implementation (when exists):** `src/ProjectAegis.Data/Validation/`, `tests/unit/editor/validation/`, `assets/data/editor/validation-config.json`