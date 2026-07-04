# Sprint 86 — CLI/MCP Polish + UA Triage + No-Lua Gate

**Dates:** Following S85 (est. 5–7 days)  
**Lead:** E11 / team-data + team-simulation + security-engineer + c-sharp-devops-engineer  
**Program:** S81–S88 Scenario Editor (req 11)  
**Authority:** `roadmap-execute-plan-07042026.md` §3/§4 (S86), `future-sprint-roadpmap-07042026.md`, `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md` (units #19, UA pair), `implementation-tracker-2026-07-04.md`, `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`, prior S81–S85 sprint plans + kickoffs + closeouts, AGENTS.md

## Sprint Goal
Complete CLI/MCP polish (remaining verbs + manifest completeness), triage the 2 pre-existing UA engage failures in `BalticReplayHarnessPolicyEngageTests` (fix/waive/document exclusion), and deliver the No-Lua architecture gate (#19 / ADR-014) via `NoDynamicExecutionGateTests`. Advance primary files `mcp-tools.json`, `McpToolsManifestTests.cs`, `NoDynamicExecutionGateTests.cs`, and UA tests. Maintain all standing invariants. Prepare for S87 Unity round-trip and S88 gate.

## Tracks (parallel day 1)

| Track | Stack prefix | Worktree path | Agent env | Owner |
|-------|--------------|---------------|-----------|-------|
| MCP manifest + verb polish (S86-01) | `stack/sprint86/mcp-polish` | `.worktrees/stack/sprint86/mcp-polish` | Cloud | team-data |
| UA engage test triage (S86-02) | `stack/sprint86/ua-engage-fix` | `.worktrees/stack/sprint86/ua-engage-fix` | Cloud | team-simulation |
| No-Lua architecture gate (S86-03) | `stack/sprint86/no-lua-gate` | `.worktrees/stack/sprint86/no-lua-gate` | Cloud | security-engineer |
| Closeout (S86-04) | `stack/sprint86/closeout` | `.worktrees/stack/sprint86/closeout` | **Local** | c-sharp-devops-engineer |

**Wave order:** MCP polish ∥ UA triage ∥ No-Lua gate (different assemblies; coordinate on manifest if verbs added) → Closeout

**Primary files (per execute-plan §4):**

| File | QA unit / Track |
|------|-----------------|
| `tools/mission-editor/mcp-tools.json` | MCP / S86-01 |
| `src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs` | MCP / S86-01 |
| `src/ProjectAegis.Data.Tests/Architecture/NoDynamicExecutionGateTests.cs` | #19 / S86-03 |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessPolicyEngageTests.cs` | UA triage / S86-02 |
| Supporting: `src/ProjectAegis.MissionEditor.Cli/Program.cs` (verb surface), `src/ProjectAegis.MissionEditor.Cli/*Command.cs` (if new/updated verbs), manifest schema, CI references to gate test | — |

## UA Failures (open @ S86; triage owner per roadmap-execute-plan-07042026.md §4 + future-sprint-roadpmap-07042026.md)

- `Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log`
- `Restricted_engagement_scenario_fingerprint_is_deterministic`

Location: `BalticReplayHarnessPolicyEngageTests` (UnityAdapter.Tests).

**S86-02 actions:** Triage root cause (contract/representation vs determinism regression per baltic-headless-slice-gate-2026-07-04.md); options:
- Fix (preferred if isolated to test expectations and non-determinism safe).
- Waive with explicit user ack + documented exclusion (until later spine contract work).
- Document exclusion in gate artifacts + test attributes (Skip or conditional) + cite in S86 closeout and S88 gate.

**Do not regress:** ReplayGolden 6/6, C2 18/18, or other Baltic harness paths. Do not touch `DelegationBridge` or `BalticReplayHarness` core logic without CRITICAL impact pre + approval.

**Note from prior:** These are pre-existing on main (inherited); unrelated to scenario-editor commits. Flag for S86; do not block S81–S85.

## #19 No-Lua Gate (ADR-014 / doc 11 NFR)

Per `qa-plan-scenario-editor-2026-07-01.md` #19 and `NoDynamicExecutionGateTests.cs`:

- No Lua interpreter package refs (NLua, MoonSharp, KeraLua, Lua) in `ProjectAegis.Data.csproj`, `ProjectAegis.MissionEditor.Cli.csproj`.
- No dynamic code execution substrings (`System.Reflection.Emit`, `Roslyn`, `CSharpScript`, `Process.Start`, `eval(`) in key validation/authoring sources: `ScenarioValidationEngine.cs`, `ValidationRules.cs`, `ScenarioDocumentEditor.cs`.
- The gate is a source/manifest-scanning architecture test (not runtime).

**S86-03 actions:** Ensure gate is complete, green, and integrated (extend if needed for additional reachable files per #19; verify against `docs/architecture/adr-014-lua-compatibility-scope.md`; possibly wire a `tools/ci/no-dynamic-execution-gate.sh` shim if referenced in CI/qa-plan). Run on editor assemblies. Update manifest test or related if MCP surface touches.

## Hard Gates (per roadmap-execute-plan-07042026.md §5/§6 + boundary + AGENTS.md)
- Editor subset (MCP + architecture + UA relevant): 
  ```bash
  dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "McpToolsManifest"
  dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "NoDynamicExecutionGate"
  dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "BalticReplayHarnessPolicyEngage"
  ```
- Full test: `dotnet test ProjectAegis.sln -v minimal` (floor **≥1232**; 0 new failures; 2 known UA tracked separately for triage)
- ReplayGolden 6/6 (`--filter FullyQualifiedName~ReplayGoldenSuiteTests`)
- PlayModeSmokeHarnessTests 18/18
- Production Baltic hash **`17144800277401907079`** preserved (`rg "17144800277401907079" tests/regression/ data/ -l`)
- **ZERO DelegationBridge edits** (grep verification: hotpath .cs changes = 0)
- GitNexus preflight + post (see below)
- `bash tools/ci/smoke-ac6.sh` (if serialization or AC-6 paths touched)
- Cite boundary + this sprint plan + roadmap-execute-plan-07042026.md + qa-plan #19 + UA notes in all changes
- Stage remains **Release**

**Mandatory full verification-before-completion (RUN+READ all before closeout claim, per AGENTS + execute-plan §5 Phase 0):**
```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"

node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only || true
node .gitnexus/run.cjs impact BalticReplayHarness --direction upstream --summary-only || true

dotnet build ProjectAegis.sln                   # 0 errors, 0 warnings
dotnet test ProjectAegis.sln -v minimal          # ≥1232 / 0 failures (excl. known UA pair until S86 resolution)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests   # 18/18
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests"   # 6/6
rg "17144800277401907079" tests/regression/ data/ -l
# Editor/MCP subset:
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter McpToolsManifest
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter NoDynamicExecutionGate
bash tools/ci/smoke-ac6.sh || true
```

## GitNexus Discipline (MANDATORY)
Before any symbol edit (McpToolsManifestTests, NoDynamic..., BalticReplayHarnessPolicyEngageTests, Program.cs, any *Command.cs, Scenario*Editor, Validation*):

```bash
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
node .gitnexus/run.cjs impact BalticReplayHarness --direction upstream --summary-only
node .gitnexus/run.cjs detect_changes --scope unstaged
```

Report blast radius (e.g. ScenarioDocumentEditor CRITICAL 20; direct callers in Cli, tests, authoring). Run `detect_changes` before closeout commit.

From roadmap §1 (plan authoring baseline @ 17d426c): CatalogWriteGate 178 CRITICAL, Patrol 97, DelegationBridge 127, Baltic 52; editor symbols 20 CRITICAL / 17 HIGH. Re-verify at sprint start + per track.

## Verification-before + TDD
- GitNexus impact preflight **before** any edit to primary symbols.
- TDD (RED→GREEN) where extending coverage (new verbs in manifest, gate extensions, UA triage tests).
- Use `verification-before-completion`.
- No hotpath changes to DelegationBridge (ZERO); extend-only CatalogWriteGate.
- UA triage: document decision explicitly; do not alter replay golden hash.

## Standing Invariants (from boundary + execute-plan)
- Stage = Release (no stage.txt change)
- Test baseline ≥1232 monotonic
- ReplayGolden 6/6; C2 18/18
- Baltic hash `17144800277401907079` preserved (no ADR)
- ZERO DelegationBridge
- CatalogWriteGate extend-only
- Baltic v2/v3 + goldens frozen (use examples/ + schema only)
- Single owner per sprint for ScenarioDocumentEditor (read/coordinate here)
- GitNexus exact CRITICAL counts reported in closeout
- 2 UA failures tracked for S86 triage only (excluded from gate counts until resolved/waived)

## Parallel Wave & Dispatch
Per execute-plan §5 Phase 1 and §10: After S85 close + S86 plan/kickoff + user ack, dispatch 3 cloud tracks in parallel via superpowers dispatching-parallel-agents + isolated worktrees (`.worktrees/stack/sprint86/*`).

- S86-01 team-data (mcp-polish): focus manifest completeness for remaining scenario/mission verbs; update `mcp-tools.json` + `McpToolsManifestTests.RequiredCliVerbs`; ensure parity with Program.cs; CLI/MCP smoke.
- S86-02 team-simulation (ua-engage-fix): triage + resolve (fix/waive) the two `BalticReplayHarnessPolicyEngageTests` cases; produce analysis + decision record.
- S86-03 security-engineer (no-lua-gate): harden/verify `NoDynamicExecutionGateTests` (and shim if needed); assert clean on Data + Cli projects + key source files; cite ADR-014 + doc 11.
- S86-04 local closeout: aggregate, gt submit --stack, restack, full Phase 0 re-run, produce smoke closeout doc `production/qa/smoke-sprint-86-closeout-2026-07-*.md`, update `production/sprint-status.yaml`, `detect_changes`, user ack.

Coordinate: MCP polish may touch Program/manifest; No-Lua and UA are independent assemblies.

## Cites (MANDATORY on all artifacts / commits / PRs / subagent prompts)
- `roadmap-execute-plan-07042026.md` §3/§4 (S86) + §5 Phase 0/1/2 + UA table
- `future-sprint-roadpmap-07042026.md`
- `production/scenario-editor-scope-boundary-2026-07-04.md`
- `production/qa/qa-plan-scenario-editor-2026-07-01.md` (#19 + UA context)
- `Game-Requirements/implementation-tracker-2026-07-04.md`
- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AME-8.2 MCP/CLI, #19 NFR)
- `docs/architecture/adr-014-lua-compatibility-scope.md` (for #19)
- S81–S85 sprint plans + agentic kickoffs + closeouts
- AGENTS.md (GitNexus MUSTs, test baseline, ZERO bridge, graphite, collab protocol)
- `baltic-headless-slice-gate-2026-07-04.md` (UA context)

## Risks / Notes
- MCP polish: ensure no breaking schemaVersion 2 changes; add verbs only if missing for completeness (ferry/undo/event_trace/simulate etc. already partially in manifest per prior).
- UA triage: may require user ack for waiver; produce evidence in closeout.
- #19 gate: lightweight scan; do not introduce dynamic paths.
- 2 UA failures explicitly **not** to be fixed in prior tracks.
- Use Graphite `gt` exclusively for stack branches.
- After S86: S87 (Unity AC-8) → S88 gate.

## Next after Closeout
User ack + `gt submit --stack --no-interactive` + restack/sync → S87 dispatch (playmode-roundtrip + manual-qa-ac8).

**References (heavy citation required):**
- `docs/reports/roadmap-execute-plan-07042026.md` (S86 tables, primary files, UA failures list, wave, Phase 0 gates, ownership)
- `docs/reports/future-sprint-roadpmap-07042026.md` §3/§4/§10/§11
- `production/scenario-editor-scope-boundary-2026-07-04.md` (invariants, scope, CRITICAL symbols incl. Baltic 52)
- `production/qa/qa-plan-scenario-editor-2026-07-01.md` (#19 NFR, test path)
- S81: `production/sprints/sprint-81-scenario-editor-foundations.md` + kickoff
- S82: `production/sprints/sprint-82-validation-tracks-ac.md` + kickoff
- S83: `production/sprints/sprint-83-export-undo-ferry.md` + kickoff
- S84: `production/sprints/sprint-84-event-debugger.md` + kickoff
- AGENTS.md + CLAUDE.md

---
*Part of S81–S88 Scenario Editor program (E11). Parallel tracks per roadmap-execute-plan-07042026.md §4/§10. Created 2026-07-04. All work cites authority chain.*
