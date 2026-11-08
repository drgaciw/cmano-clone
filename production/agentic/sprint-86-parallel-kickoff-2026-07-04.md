# Sprint 86 Parallel Kickoff — CLI/MCP Polish + UA Triage + No-Lua Gate

**Date:** 2026-07-04  
**Sprint:** S86  
**Authority:** `roadmap-execute-plan-07042026.md` §3/§4 (S86), `scenario-editor-scope-boundary-2026-07-04.md`, `future-sprint-roadpmap-07042026.md` §3/§4/§10, `qa-plan-scenario-editor-2026-07-01.md` (units #19, UA pair), `implementation-tracker-2026-07-04.md`, `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`, S81–S85 parallel kickoffs + sprint plans + closeouts, AGENTS.md + CLAUDE.md

## Context
S85 (determinism + AC-6 CI + stub pins) complete. In-flight work from `fix-scenario-publish-cli-wiring` @ 17d426c integrated per prior merge plan.

**S86 focuses on CLI/MCP completeness + triage + gate (3 parallel cloud tracks + local closeout):**
- MCP manifest + verb polish (S86-01)
- UA engage test triage (S86-02) — **note the 2 UA failures for triage**
- No-Lua architecture gate (#19) (S86-03)

All 3 tracks **cloud** (parallel) → local closeout.

**GitNexus status (pre-kickoff):** ✅ up-to-date (branch 17d426c). ScenarioDocumentEditor upstream: CRITICAL (20 impacted, 6 processes). BalticReplayHarness CRITICAL (52). MCP/CLI symbols and NoDynamic gate require preflight impact before edits.

**Baseline @ kickoff (RUN+READ verified):**
- Build: 0e/0w
- Full sln: ~1310 pass / 2 failed (exactly the 2 pre-existing in `BalticReplayHarnessPolicyEngageTests`)
- ReplayGolden 6/6; C2 proxy 18/18
- Hash `17144800277401907079` preserved
- ZERO DelegationBridge changes
- GitNexus: status clean; impacts per roadmap §1

## Dispatch Model (superpowers + AGENTS.md)
- Use `dispatching-parallel-agents` skill for the 3 independent tracks.
- **Isolated worktrees required:** `.worktrees/stack/sprint86/mcp-polish`, `/ua-engage-fix`, `/no-lua-gate`
- **Mandatory per track (before any symbol edit):**
  - GitNexus: `node .gitnexus/run.cjs status` + `impact <symbol> --direction upstream --summary-only` (ScenarioDocumentEditor, ScenarioValidationEngine, BalticReplayHarness, Program, McpToolsManifestTests targets, Validation* files)
  - Cite **boundary** + execute-plan + qa-plan #19 + UA pair + S81–S85 in every artifact, commit, and subagent prompt.
- TDD (RED→GREEN) + verification-before-completion on all PASS claims.
- No hotpath changes to DelegationBridge; extend-only on CatalogWriteGate.
- UA triage: produce explicit decision (fix/waive/exclude) with user ack path for S88.
- MCP: maintain schemaVersion 2 contract; parity between CLI dispatch and manifest.
- Use `gt` for stack work (no raw git push or gh pr create).

## Track Assignments (this sprint)

**S86-01 MCP manifest + verb polish (Cloud, team-data)**
- Goal: Ensure `mcp-tools.json` and `McpToolsManifestTests` cover all active headless scenario/mission verbs from `Program.cs` (scenario_*, mission_* including ferry/undo/event_trace/simulate etc.). Polish entries (schemas, descriptions) for completeness. Update RequiredCliVerbs array; run manifest test green + CLI smoke.
- Primary files: `tools/mission-editor/mcp-tools.json`, `src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs`, `src/ProjectAegis.MissionEditor.Cli/Program.cs` (review verbs).
- Constraints: Impact pre on editor + CLI symbols; run McpToolsManifest filter; no bridge touch; cite AME-8.2.
- Output: Updated manifest + passing McpToolsManifestTests + notes citing roadmap §4 + boundary.

**S86-02 UA engage test triage (Cloud, team-simulation)**
- Goal: Triage the two failing tests in `BalticReplayHarnessPolicyEngageTests`:
  - `Restricted_engagement_scenario_fingerprint_is_deterministic`
  - `Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log`
- Root cause analysis (see `baltic-headless-slice-gate-2026-07-04.md` — contract/representation regression on main, not determinism). Decide: fix (update expectations or harness surface if safe), waive with documented exclusion + user ack, or skip + gate note. Produce analysis + recommendation for S86 closeout / S88.
- Primary files: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessPolicyEngageTests.cs`
- Constraints: GitNexus impact on BalticReplayHarness (CRITICAL 52); **do not alter replay golden hash**; preserve 6/6 and 18/18; run UA filter + full gates; do not edit DelegationBridge.
- Output: Triage report + passing or explicitly excluded tests + decision documented. Flag #19 context if related.

**S86-03 No-Lua architecture gate (Cloud, security-engineer)**
- Goal: Complete / verify the #19 NFR gate (`NoDynamicExecutionGateTests`). Assert no Lua packages in Data/Cli csproj; no forbidden dynamic substrings (`System.Reflection.Emit`, Roslyn, CSharpScript, Process.Start, eval() patterns) in `ScenarioValidationEngine.cs`, `ValidationRules.cs`, `ScenarioDocumentEditor.cs` (and any additional reachable authoring/validation sources per qa-plan #19). Ensure test green. Optionally produce `tools/ci/no-dynamic-execution-gate.sh` shim if needed for CI parity.
- Primary files: `src/ProjectAegis.Data.Tests/Architecture/NoDynamicExecutionGateTests.cs`; target source: `src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs`, `src/ProjectAegis.Data/Validation/Rules/ValidationRules.cs`, `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs`; project files for Data + MissionEditor.Cli.
- Constraints: GitNexus impact on ValidationEngine / DocumentEditor (HIGH/CRITICAL); cite ADR-014 + doc 11 NFR + qa-plan #19; run NoDynamic filter + full editor subsets.
- Output: Green gate test + evidence of clean scan + any extension notes.

**S86-04 Closeout (Local, c-sharp-devops-engineer)**
- Aggregate evidence from 3 tracks (manifest diffs, UA triage decision + logs, gate test output + source scans).
- Re-run **full** Phase 0 gates (build, sln test, Replay 6/6, C2 18/18, hash grep, GitNexus detect_changes + impacts on CRITICALs + editor/Baltic).
- Produce `production/qa/smoke-sprint-86-closeout-2026-07-*.md` (or per pattern).
- Update `production/sprint-status.yaml`.
- Drive user ack + `gt submit --stack --no-interactive` + restack/sync.
- Confirm ZERO bridge, editor filters, UA decision recorded, #19 gate documented, MCP manifest parity.
- Record baseline numbers (e.g. 1310+/2 UA) and GitNexus stats.

## Hard Gates for S86 Close
- Build: 0 errors / 0 warnings (`dotnet build ProjectAegis.sln`)
- Tests: ≥1232 floor, 0 new failures (excl. 2 known UA in UA.Tests now triaged); full sln + targeted Mcp/NoDynamic/UA/editor filters
- ReplayGolden 6/6
- PlayModeSmokeHarnessTests 18/18
- Hash `17144800277401907079` preserved (no change)
- ZERO `DelegationBridge.cs` source edits (grep verification)
- GitNexus: fresh status + impacts on CRITICALs (ScenarioDocumentEditor 20 CRITICAL, BalticReplayHarness 52) + editor symbols; `detect_changes` pre-commit
- MCP manifest test green + CLI verbs parity confirmed
- NoDynamicExecutionGateTests green + clean scan on targeted files
- UA triage decision + evidence in closeout (fix/waive/exclude documented)
- All artifacts cite boundary + roadmap-execute-plan-07042026.md + qa-plan #19 + UA pair + S81–S85
- Stage remains Release
- (Optional AC-6 if touched): `bash tools/ci/smoke-ac6.sh` PASS

**Full gate script (RUN+READ before closeout claim):**
```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"

node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only || true
node .gitnexus/run.cjs impact BalticReplayHarness --direction upstream --summary-only || true

dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
rg "17144800277401907079" tests/regression/ data/ -l
# S86-specific:
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter McpToolsManifest
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter NoDynamicExecutionGate
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~BalticReplayHarnessPolicyEngageTests"
bash tools/ci/smoke-ac6.sh || true
```

## Communication
- Surface questions early (NEEDS_CONTEXT / BLOCKED).
- Coordinator (local) owns merge order, `ScenarioDocumentEditor` contention, human gates, UA decision escalation.
- Report GitNexus blast radius + risk (HIGH/CRITICAL on editor/Baltic) in track artifacts.
- Do not dispatch S87 until S86 closeout PASS + user ack + gt submit.
- UA triage decision must be explicit for S88 gate readiness.

## References (cite in all work)
- `docs/reports/roadmap-execute-plan-07042026.md` (S86 tables, primary files mcp-tools.json/McpToolsManifestTests/NoDynamic..., UA failures exact names, wave, Phase 0/1/2, hard gates, ownership)
- `docs/reports/future-sprint-roadpmap-07042026.md`
- `production/scenario-editor-scope-boundary-2026-07-04.md` (standing invariants, scope in/out, file ownership, CRITICAL symbols incl. BalticReplayHarness 52)
- `production/qa/qa-plan-scenario-editor-2026-07-01.md` (#19 NFR details, test path, UA context)
- `Game-Requirements/implementation-tracker-2026-07-04.md` (CLI/MCP surface status, #19)
- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AME-8.2, NFR no dynamic exec)
- `docs/architecture/adr-014-lua-compatibility-scope.md`
- `docs/reports/baltic-headless-slice-gate-2026-07-04.md` (UA failures details)
- S81: `production/sprints/sprint-81-scenario-editor-foundations.md`, `production/agentic/sprint-81-parallel-kickoff-2026-07-04.md`
- S82: `production/sprints/sprint-82-validation-tracks-ac.md`, `production/agentic/sprint-82-parallel-kickoff-2026-07-04.md`
- S83: `production/sprints/sprint-83-export-undo-ferry.md`, `production/agentic/sprint-83-parallel-kickoff-2026-07-04.md`
- S84: `production/sprints/sprint-84-event-debugger.md`, `production/agentic/sprint-84-parallel-kickoff-2026-07-04.md`
- AGENTS.md (GitNexus MUSTs, test baseline, ZERO bridge, graphite, collab protocol)
- `production/agentic/local-cloud-agent-routing.md`

**Kickoff complete.** GitNexus pre passed on current branch. Dispatch S86-01, S86-02, S86-03 in parallel (3 cloud agents). S86-04 local after. Note 2 UA failures explicitly for S86-02 triage; #19 gate for S86-03.

---
*Parallel prep style per roadmap-execute-plan-07042026.md §4/§10. Ready for subagent dispatch. 2026-07-04. All artifacts must carry full cite chain.*
