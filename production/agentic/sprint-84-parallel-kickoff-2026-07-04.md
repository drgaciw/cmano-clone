# Sprint 84 Parallel Kickoff — Event Debugger Track B + ADR-016

**Date:** 2026-07-04 (post S83)  
**Sprint:** S84  
**Authority:** `roadmap-execute-plan-07042026.md` §4 (S84), `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md`, `future-sprint-roadpmap-07042026.md`, `implementation-tracker-2026-07-04.md`, `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`

## Context
S83 (export/undo + ferry track D) complete. In-flight editor work integrated or ready.

S84 focuses on **Event debugger track B + ADR-016** (3 parallel cloud tracks + local closeout):

1. Event debugger JSON (AC-7) — S84-01 team-simulation
2. Teleport export transform (AC-11) — S84-02 team-simulation
3. ADR-016 complexity caps — S84-03 c-sharp-test-engineer
4. Closeout S84-04 (local)

All tracks cite boundary + sprint plan + execute-plan + qa units. Primary: `EventDebuggerTrace`, `EventDebuggerTests`, CLI `scenario_event_trace` / `ScenarioEventTraceCommand`, `TeleportUnitExportTests`, `ScenarioExportCommand`, new `EventGraphComplexityTests`, `ValidationRules`.

## Dispatch Rules (superpowers)
- Use `dispatching-parallel-agents` for the three tracks (S84-01∥S84-02∥S84-03).
- Each subagent: **isolated context**, GitNexus preflight on editor symbols + CRITICALs (see gates), cite all authorities, TDD where extending (RED→GREEN on new tests), verification-before-completion on all PASS claims.
- Work exclusively in `.worktrees/stack/sprint84/<track>` (Graphite stack prefixes per execute-plan).
- Prefer extending existing (EventDebuggerTrace.cs, TeleportUnitExportTests.cs, ValidationRules.cs) or create the missing `EventGraphComplexityTests.cs`.
- Run targeted Data.Tests filter before/after every change.
- No DelegationBridge, no CatalogWriteGate mutation, no frozen Baltic goldens.
- Stage remains Release.
- Single-owner coordination if ValidationRules touched by multiple tracks.

## Track Details

**S84-01 Event debugger JSON (AC-7) — team-simulation (Cloud)**
- Primary files: `EventDebuggerTrace.cs`, `EventDebuggerTests.cs`, CLI command wiring (`Program.cs` or `ScenarioEventTraceCommand.cs`), `ScenarioDocumentEditor.ExplainEventTrace`
- Goal: Full AC-7 coverage — structured JSON with `event_id`, `fired`, `last_evaluated_tick` (DefaultEvaluationHorizonTicks=32), `unmet_conditions[]` array for never-holding cases (UnitEntersZone etc.). Aligns to order-log `EventFired` projection (AME-5.5, doc 17).
- Test filter component: EventDebugger
- Evidence: `EventDebuggerTests.UnitEntersZone_never_holds_emits_fired_false_with_unmet_conditions` + ExplainEventTrace delegation + blank fallback.
- GitNexus: impact ScenarioDocumentEditor + EventDebuggerTrace paths.
- Output: green tests + CLI `scenario_event_trace --path <file> --event <id>` producing valid JSON.

**S84-02 Teleport export transform (AC-11) — team-simulation (Cloud)**
- Primary files: `TeleportUnitExportTests.cs`, `ScenarioExportCommand.ApplyTeleportUnitExportTransform` + `Prepare`, `ExportTransformManifest.cs` / `ManifestBuilder` (log entries)
- Goal: Export removes all TeleportUnit actions + logs each removal in manifest (explicit, never silent strip). Post-transform event set identical between direct transform, export package, and simulate sample. "edit-test only" logged.
- Test filter component: TeleportUnitExport
- Evidence: removal + manifest entry assertions; roundtrip equality; manifest in publish output.
- Note (from qa-plan): AC-11 requires TeleportUnit action impl in authoring DTOs/models first — implement if missing as build task.
- GitNexus: impact ScenarioExportCommand + Validation paths.

**S84-03 ADR-016 event-graph caps — c-sharp-test-engineer (Cloud)**
- Primary files: `EventGraphComplexityTests.cs` (new if absent), `ValidationRules.cs` (add complexity / peak_tick_density / 32-cap rule), `ScenarioValidationEngine`
- Goal: 
  - Event with 33 conditions → blocking validation error (hard cap).
  - Exactly 32 conditions → valid (boundary).
  - Total complexity > WARN_THRESHOLD (default 400) or peak_tick_density > DENSITY_THRESHOLD (default 20) → soft warning (severity warning, export Allowed=true).
  - Zero events, exact-threshold, determinism of warnings.
- Test filter component: EventGraphComplexity
- Per ADR-016 + GDD §4.3: soft warnings never block; only 32+ is error. Thresholds data-driven (config).
- GitNexus: impact ScenarioValidationEngine (HIGH), ValidationRules. Report upstream callers.
- Single owner: coordinate with S84-01 if ValidationRules shared.

**S84-04 Closeout (local)**
- Aggregate evidence from all tracks.
- `gt submit --stack --no-interactive` on each; `gt sync`; `gt restack` on main.
- Re-run full Phase 0 gates (see sprint plan) + editor subset filter.
- GitNexus post: `detect_changes` (compare vs main), re-index if needed, report stats + impacts.
- Produce `production/qa/smoke-sprint-84-closeout-2026-07-*.md`
- Update `production/sprint-status.yaml`
- Record: test deltas, GitNexus exact numbers, no unexpected blast radius.
- Verification: all gates RUN+READ before close claim.

## Commands (run in every track + closeout)
```bash
# Preflight (MANDATORY)
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ValidationRules --direction upstream --summary-only

# Targeted
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "EventDebugger|TeleportUnitExport|EventGraphComplexity"

# Editor subset (full relevant)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly"

# Full baseline gates (Phase 0)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter PlayModeSmokeHarnessTests
rg "17144800277401907079" tests/regression/ data/ -l
bash tools/ci/smoke-ac6.sh
```

## Standing Invariants & Exclusions
- Test floor ≥1232 (0 failures excl. known UA pair until S86)
- ReplayGolden 6/6; C2 proxy 18/18
- Hash `17144800277401907079` preserved
- ZERO DelegationBridge (grep `DelegationBridge` in src/ outside the bridge file == 0 new)
- Catalog extend-only
- Stage: Release
- GitNexus discipline on all edits
- Frozen: baltic-v* corpora/goldens; use `data/scenarios/examples/`
- Never commit: `.cursor/hooks/`, `.pi/settings.json`, `.polly/`

## Cites (include in every commit message / doc / PR description)
`roadmap-execute-plan-07042026.md` §4 S84 + `future-sprint-roadpmap-07042026.md` + `scenario-editor-scope-boundary-2026-07-04.md` + `qa-plan-scenario-editor-2026-07-01.md` (units #7,#11,#16) + `implementation-tracker-2026-07-04.md` + AGENTS.md + this kickoff + sprint-84-event-debugger.md

## GitNexus Pre (per AGENTS.md — always before edit)
Run `impact({target: "...", direction: "upstream"})` and report blast radius (direct callers, affected processes, risk HIGH/CRITICAL). Use `context()` for full callers/callees/flows on symbols. `detect_changes()` before any commit.

Current baseline (from execute-plan): ScenarioValidationEngine HIGH (17 upstream, 1 process, modules Cli.Tests/Validation/Authoring). Recompute on dispatch.

## Ready for Dispatch
Kickoff complete. Boundary + S81–S83 assumed complete + user ack received.

**Dispatch the three tracks in parallel now** (S84-01, S84-02, S84-03) using subagent-driven-development + worktree isolation.

Local coordinator owns S84-04 closeout + final verif + human gates.

**Next after close:** S85 Determinism + AC-6 CI + stub pins.

---
*Part of S81–S88 Scenario Editor program. Graphite-first. Superpowers dispatching-parallel-agents. All gates RUN+READ.*
