# Bug Report

## Summary
**Title**: `CatalogRadarEmconResolver.MapPosture` case-sensitive default silently mislabels validly-authored EMCON postures as `Active`
**ID**: BUG-emcon-posture-case-sensitive-default
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Fixed (this loop) — TDD test + minimal production fix committed
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-r2-09-doctrine-glossary)

## Classification
- **Category**: Gameplay (EMCON/doctrine discipline, detection, engagement gating)
- **System**: Catalog → Doctrine/EMCON resolution (`ProjectAegis.Sim.Catalog.CatalogRadarEmconResolver`, `ProjectAegis.Sim.Scenario.ScenarioEmconResolver`), consumed by `ProjectAegis.Sim.Sensors.DeterministicDetectionLoop`, `ProjectAegis.Sim.Sensors.ScenarioContactSimulator`, `ProjectAegis.Sim.Engage.MvpEngagementResolver` (via `EngageContext.RadarEmconActive`), and `ProjectAegis.Delegation.Projection.DoctrineInheritanceProjection` (doctrine panel EMCON label)
- **Frequency**: Always, for any catalog-authored `Posture` value that isn't the exact lowercase literal `"off"`, `"standby"`, or `"active"` (e.g. `"Off"`, `"OFF"`, `"Standby"`, `"STANDBY"` — all of which are explicitly valid, unflagged input per the workbook validator)
- **Regression**: No — since-inception gap between two doctrine-adjacent components that disagree on case sensitivity for the same domain vocabulary

## Environment
- **Build**: worktree `qa-r2-09-doctrine-glossary`, branch head at `e2e1342` (pre-fix)
- **Platform**: headless `dotnet test` (no Unity Editor available in this sandbox)
- **Scene/Level**: N/A — plain .NET sim layer (`ProjectAegis.Sim.Tests`)
- **Game State**: Any scenario/campaign whose platform catalog defines an `Emcon` row with a `Posture` value that isn't exact-lowercase

## Reproduction Steps
**Preconditions**: A catalog `Emcon` row authored via the platform workbook (or any `ICatalogReader`) with `Posture` set to a mixed/upper-case but otherwise valid value, e.g. `"Off"`.

1. Author (or construct in-memory) a `CatalogEmcon(platformId, condition, emitterId, Posture: "Off")`.
2. Note that `ProjectAegis.Data.Platform.PlatformWorkbookValidator.AllowedEmconPostures` is a `HashSet<string>(StringComparer.OrdinalIgnoreCase) { "off", "standby", "active" }` — this value passes catalog validation with **no** finding/warning.
3. Call `CatalogRadarEmconResolver.ResolveRadar(platformId, unitRadarEmcon: null, catalog)` (or the equivalent `ScenarioEmconResolver.ResolveRadar`), which falls through to `MapPosture(emcon.Posture)` since there's no scenario-level override.

**Expected Result**: `MapPosture("Off")` returns `EmconState.Off`, consistent with the value the catalog author intentionally set and that the validator accepted as valid.

**Actual Result** (before fix): `MapPosture` switches on the raw string with exact-case literals only (`"off"`, `"standby"`, `"active"`), so `"Off"` falls into the `_ => EmconState.Active` default arm. The unit's radar is silently reported/treated as **actively radiating** instead of **off**. This corrupts:
  - `DeterministicDetectionLoop` / `ScenarioContactSimulator` detection gating (radar-silent units are incorrectly eligible for detection rolls that require active radar),
  - `MvpEngagementResolver`'s `ctx.RadarEmconActive` gate (a unit whose radar should be off is incorrectly allowed past the `EmconOff` abort check),
  - the C2 doctrine-inheritance UI panel (`DoctrineInheritanceProjection.FormatRadarEmconLabel`), which would display `"EMCON: ACTIVE"` for a unit the scenario author explicitly configured as radar-off.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Sim/Catalog/CatalogRadarEmconResolver.cs` (`MapPosture`) — the fixed symbol
  - `src/ProjectAegis.Data/Platform/PlatformWorkbookValidator.cs` (`AllowedEmconPostures`) — the case-insensitive validator that permits the mismatched casing through
  - `src/ProjectAegis.Data/Platform/PlatformWorkbookImporter.cs` (`BuildChangedEmconRows`) — imports the raw string verbatim with no case normalization
  - `src/ProjectAegis.Sim/Scenario/ScenarioEmconResolver.cs`, `src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs`, `src/ProjectAegis.Sim/Sensors/ScenarioContactSimulator.cs`, `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs`, `src/ProjectAegis.Delegation/Projection/DoctrineInheritanceProjection.cs` — downstream consumers of the mismapped state
- **Related systems**: Catalog authoring (Excel workbook), Doctrine/EMCON gameplay mechanic, Detection, Engagement resolution, C2 doctrine-inheritance UI
- **Possible root cause**: `PlatformWorkbookValidator` and `CatalogRadarEmconResolver.MapPosture` were written independently against the same three-value vocabulary (`off`/`standby`/`active`) without a shared case-normalization contract — the validator treats the vocabulary as case-insensitive (reasonable for a spreadsheet-authored enum column) while the gameplay-facing mapper assumed exact-lowercase input and silently defaulted anything else to `Active` rather than normalizing or throwing.

## Evidence
- **New failing test (red)**: `ProjectAegis.Sim.Tests.Catalog.PhaseBCatalogConsumerTests.PhaseB_Catalog_posture_mapping_is_case_insensitive`
  - Before fix: 4 of 5 cases failed — `Assert.Equal() Failure: Expected: Off / Actual: Active` (for `"Off"`, `"OFF"`), `Expected: Passive / Actual: Active` (for `"Standby"`, `"STANDBY"`); the `"Active"`-cased case passed only because the buggy default also happens to be `Active`.
  - After fix: All 5 cases passed.
- **Regression run (green)**:
  - `ProjectAegis.Sim.Tests`: **290/290** (baseline 285/285 + 5 new `[InlineData]` cases on the new `Theory`)
  - `ProjectAegis.Delegation.Tests`: **253/253** (unchanged from baseline)
  - `ProjectAegis.Delegation.UnityAdapter.Tests`: **261/261** (unchanged from baseline)
  - `ProjectAegis.Data.Tests` / `ProjectAegis.Data.Excel.Tests` / `ProjectAegis.MissionEditor.Cli.Tests`: not re-run — no code in those assemblies was touched, and none of them reference `ProjectAegis.Sim` (`ProjectAegis.Sim` depends on `ProjectAegis.Data`, not vice versa), so they are unaffected by this change.

## Impact / Blast Radius (manual analysis — GitNexus MCP/CLI not reachable inside this isolated worktree; `.gitnexus/run.cjs` absent)
`CatalogRadarEmconResolver.MapPosture` is `public static`. Grep across `src/` found exactly two call sites: (1) `CatalogRadarEmconResolver.ResolveRadar` (same class, line 29) and (2) the test file `PhaseBCatalogConsumerTests.cs` (direct unit test). `ResolveRadar` is itself wrapped by `ScenarioEmconResolver.ResolveRadar`, which fans out to five production call sites: `DeterministicDetectionLoop.RollTick`, `ScenarioContactSimulator`, `ReadinessPolicyEvaluator.EvaluateRadarEmcon`, `DoctrineInheritanceProjection.FormatRadarEmconLabel`, and `SimulationSession` (line ~480). All existing test fixtures that construct `CatalogEmcon(...)` across the whole repo (`ProjectAegis.Sim.Tests`, `ProjectAegis.Delegation.Tests`, `ProjectAegis.Data.Tests`, `ProjectAegis.Data.Excel.Tests`) already use exact-lowercase `"off"`/`"standby"`/`"active"` posture strings (confirmed via grep of every `new CatalogEmcon(` call site), so the fix — normalizing via `.Trim().ToLowerInvariant()` before the switch — is behavior-neutral for every existing scenario/replay/golden fixture and only changes behavior for previously-mismapped mixed-case input. Risk classified **LOW**: the change is a pure input-normalization addition to a single switch expression, with no new arms and no change to the exact-lowercase code paths already exercised by the full green regression run above.

## Related Issues
- Same defect *class* as `BUG-wra-range-abort-reason-mismapped` (round 1): a silent, non-obvious default arm swallowing a legitimate-but-differently-shaped input instead of mapping it correctly — there it was a non-exhaustive enum switch, here it's a case-sensitivity mismatch between an authoring-time validator and a gameplay-time mapper for the same string vocabulary.
- Independently re-verified round 1's follow-up note for `MvpEngagementResolver.MapPolicyDenial`: confirmed `FireAbortReason.CommsDenied` is still intercepted upstream in `SimulationSession.RunExecutingTick` (comms-blocked engage orders are `continue`d before ever reaching `Sim.EnqueueEngagement`/`MvpEngagementResolver`), and the six `*AspectBlock` reasons are still only ever produced via `DomainValidatorRegistry` → `MapDomainDenial` (a separate, already-exhaustive switch) — neither shipped `IPolicyEvaluator` implementation (`PolicyEvaluator`, `PassthroughPolicyEvaluator`) emits `CommsDenied` or any `*AspectBlock` value. The round-1 note still holds; no action needed there.

## Notes
Also reviewed and ruled out as not-a-bug during this pass: `ScenarioPolicyProfile.ResolveUnitPolicy`'s unit-override-vs-mission-ROE precedence (unit override always correctly wins, confirmed by existing `ScenarioPolicyInheritanceTests`); a theoretical case-sensitivity mismatch between the constructor's default (case-sensitive) `UnitOverrides` dictionary and the case-insensitive `_missionUnitIdSet` is real in the abstract but unreachable in practice — the sole production constructor call site, `ScenarioPolicyJsonLoader.ToProfile`, always builds `UnitOverrides` with `StringComparer.OrdinalIgnoreCase`, so no live scenario data can trigger it.
