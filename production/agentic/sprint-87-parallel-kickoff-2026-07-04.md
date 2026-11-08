# Sprint 87 Parallel Kickoff — Unity AC-8 Round-Trip (local)

**Date:** 2026-07-04 (post S86)  
**Sprint:** S87  
**Authority:** `roadmap-execute-plan-07042026.md` §4 (S87), `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md`, `future-sprint-roadpmap-07042026.md`, `implementation-tracker-2026-07-04.md`, `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-8)

## Context
S86 (CLI/MCP polish + UA triage) complete. In-flight editor work integrated or ready. S81–S86 validation tracks A–D, commands, determinism, etc. delivered.

S87 focuses exclusively on **Unity host round-trip (AC-8)** — 3 local-only tracks (no cloud; Unity evidence requires local coordinator + Editor or headless PlayMode harness):

1. PlayMode headless load (S87-01) — unity-ui-specialist
2. Manual QA evidence pack (S87-02) — qa-tester
3. Closeout (S87-03) — c-sharp-devops-engineer

All tracks cite boundary + sprint plan + execute-plan + qa-plan #8. Primary: extension of `PlayModeSmokeHarnessTests` in `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` to load headless-authored `data/scenarios/examples/*.scenario.json` (e.g. strike-package.scenario.json or baltic-patrol.scenario.json) and assert ORBAT/missions/events + default `editorState` intact.

**From execute-plan S87:** PlayMode headless load (S87-01 unity-ui), Manual QA evidence (S87-02), closeout.

## Dispatch Rules (superpowers + local)
- Use `dispatching-parallel-agents` (or serial if coordination needed) for the tracks with isolated context.
- Each subagent: **isolated context**, GitNexus preflight on editor symbols + CRITICALs (DelegationBridge, ScenarioDocumentEditor), cite all authorities, verification-before-completion on all PASS claims.
- Work exclusively in `.worktrees/stack/sprint87/<track>` (Graphite stack prefixes per execute-plan).
- **Local only** (unity-ui-specialist / qa-tester / devops). No cloud for S87.
- Prefer extension of existing PlayModeSmokeHarnessTests + harness patterns (BalticReplayHarness, PlayModeHarness) for scenario load. Use `ScenarioPackage` / loader from Data if bridged.
- Run targeted UA tests + full C2 proxy filter before/after every change.
- No DelegationBridge edits (ZERO hotpath), no CatalogWriteGate mutation, no frozen Baltic goldens.
- Stage remains Release.
- Use `data/scenarios/examples/` fixtures only (not baltic-* policy corpora).
- Manual fallback: full Editor load checklist + evidence screenshots + sign-off per qa-plan #8.
- **No map placement** (Phase 2 explicitly out).

## Track Details (S87-01 primary)

**S87-01 PlayMode headless load (S87-01) — unity-ui-specialist (Local)**
- Primary files: `PlayModeSmokeHarnessTests.cs` (add AC-8 round-trip test methods), harness setup for scenario JSON load.
- Goal: Headless PlayMode load of a fully headless-authored `.scenario.json` succeeds. Assert:
  - ORBAT (units, groups, bases) intact and matches source.
  - Missions and events arrays intact.
  - `editorState` populated with schema defaults (camera positioned at theater centroid, all layers toggled on).
- Test filter component: PlayModeSmokeHarnessTests|AC8|ScenarioRoundtrip|EditorState
- Evidence: Passing test assertions (or harness snapshot equality). Use one of `strike-package.scenario.json`, `baltic-patrol.scenario.json`, `ferry-redeploy.scenario.json`.
- GitNexus: impact ScenarioDocumentEditor + any touched UnityAdapter symbols + DelegationBridge (report: must remain ZERO source changes).
- Output: New green test(s) exercising the round-trip load path. Preserve existing 18/18 C2 proxy.
- Manual note: If full automation of scenario load into PlayModeHarness / bridge snapshot not yet wired in this cycle, document the gap and rely on S87-02 manual.

**S87-02 Manual QA evidence pack (S87-02) — qa-tester (Local)**
- Primary deliverable: `production/qa/evidence/s87-ac8-unity-roundtrip-evidence.md` (or update attachments).
- Goal: Manual Editor load-and-inspect evidence pack as fallback / complement.
- Checklist execution (per qa-plan #8 Manual QA Checklist):
  - Headless-authored scenario (MCP/CLI only) loads in Unity host without error.
  - ORBAT/missions/events visible and match source file content.
  - `editorState` defaults observed (camera at centroid, layers on).
  - Optional round-trip save from host + headless reload check.
- Evidence to capture: Screenshots of loaded C2 / editor views showing intact data + default state; signed checklist.
- Who signs: qa-lead or lead-programmer.
- GitNexus: read-only impact where possible; report on load paths.
- Output: Documented evidence + PASS/FAIL with date + sign-off.

**S87-03 Closeout (local)**
- Aggregate evidence from S87-01 (test results) + S87-02 (manual pack).
- `gt submit --stack --no-interactive` on each; `gt sync`; `gt restack` on main.
- Re-run full Phase 0 gates (see sprint plan) + C2 proxy 18/18 + targeted AC-8 filter + hash + ZERO bridge grep.
- GitNexus post: `detect_changes` (compare vs main), re-index if needed, report stats + impacts (exact CRITICALs).
- Produce `production/qa/smoke-sprint-87-closeout-2026-07-*.md`
- Update `production/sprint-status.yaml`
- Record: test deltas (esp. UA), GitNexus exact numbers, no unexpected blast radius, AC-8 evidence table.
- Verification: all gates RUN+READ before close claim. Cite "local worktree", "AC-8", "no map placement (Phase 2 out)", "manual fallback".
- Human gate handoff to S88.

## Commands (run in every track + closeout)
```bash
# Preflight (MANDATORY per AGENTS + execute-plan)
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only

# Targeted (AC-8 focus + C2 proxy)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmokeHarnessTests|AC8|ScenarioRoundtrip|EditorState"

# Full baseline gates (Phase 0)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter PlayModeSmokeHarnessTests
rg "17144800277401907079" tests/regression/ data/ -l
bash tools/ci/smoke-ac6.sh

# Bridge hygiene (ZERO expected new)
grep -r "DelegationBridge" src/ --include="*.cs" | grep -v "DelegationBridge.cs" | wc -l   # should not increase
```

## Standing Invariants & Exclusions
- Test floor ≥1232 (0 failures excl. known UA pair until S86 resolution)
- ReplayGolden 6/6; C2 proxy 18/18
- Hash `17144800277401907079` preserved
- ZERO DelegationBridge (grep `DelegationBridge` in src/ outside the bridge file == 0 new)
- Catalog extend-only
- Stage: Release
- GitNexus discipline on all edits (impact before symbol change)
- Frozen: baltic-v* corpora/goldens; use `data/scenarios/examples/*.scenario.json` + schema
- **Local worktree only** for S87 (Unity AC-8)
- Never commit: `.cursor/hooks/`, `.pi/settings.json`, `.polly/`
- **No map placement** (Phase 2 explicitly out of scope for S87)
- Manual fallback allowed and documented per qa-plan

## Cites (include in every commit message / doc / PR description)
`roadmap-execute-plan-07042026.md` §4 S87 + `future-sprint-roadpmap-07042026.md` + `scenario-editor-scope-boundary-2026-07-04.md` + `qa-plan-scenario-editor-2026-07-01.md` (unit #8 AC-8) + `implementation-tracker-2026-07-04.md` + `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-8) + AGENTS.md + this kickoff + `production/sprints/sprint-87-unity-roundtrip.md`

## GitNexus Pre (per AGENTS.md — always before edit)
Run `impact({target: "...", direction: "upstream"})` and report blast radius (direct callers, affected processes, risk HIGH/CRITICAL). Use `context()` for full callers/callees/flows on symbols. `detect_changes()` before any commit.

Current baseline (from execute-plan): CatalogWriteGate 178 CRIT, Patrol 97, Bridge 127, Baltic 52 (exact); ScenarioDocumentEditor 20 CRITICAL. Recompute on dispatch. **Report any increase in hotpath DelegationBridge references.**

## Ready for Dispatch
Kickoff complete. Boundary + S81–S86 assumed complete + user ack received.

**Dispatch the local tracks now** (S87-01 unity-ui-specialist, S87-02 qa-tester) using subagent-driven-development + worktree isolation. S87-03 closeout local coordinator.

Local coordinator owns S87-03 closeout + final verif + human gates + evidence aggregation.

**Primary target (S87-01):** PlayModeSmokeHarnessTests extension for headless .scenario.json load, ORBAT/missions/events + editorState intact. Cites AC-8. Manual fallback S87-02. No Phase 2 map work.

**Next after close:** S88 Gate (full verification + human ack "scenario editor program complete").

---
*Part of S81–S88 Scenario Editor program. Graphite-first. Local-only for Unity AC-8 round-trip. Superpowers dispatching-parallel-agents. All gates RUN+READ. Manual fallback supported.*
