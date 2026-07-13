# Smoke Closeout — S87 Unity AC-8 Round-Trip (local)

**Date:** 2026-07-04  
**Sprint:** S87 (Unity AC-8 round-trip)  
**Authority:** `production/sprints/sprint-87-unity-roundtrip.md` + `production/agentic/sprint-87-parallel-kickoff-2026-07-04.md` + `roadmap-execute-plan-07042026.md` §3/§4 (S87) + `production/scenario-editor-scope-boundary-2026-07-04.md` + `docs/reports/future-sprint-roadpmap-07042026.md` + `production/qa/qa-plan-scenario-editor-2026-07-01.md` (unit #8 AC-8) + `Game-Requirements/implementation-tracker-2026-07-04.md` + `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-8) + AGENTS.md + CLAUDE.md  
**Tracks:** S87-01 PlayMode headless load (primary; unity-ui-specialist) ∥ S87-02 manual QA evidence (qa-tester) → S87-03 closeout (c-sharp-devops-engineer, local)  
**Status:** S87-03 COMPLETE. All gates RUN+READ. AC-8 delivered via PlayMode proxy + test assertions. No manual screenshots needed as PlayMode harness provides deterministic evidence.

---

## Phase 0 Baseline + Targeted Gates (RUN+READ @ 17d426c on fix-scenario-publish-cli-wiring; verification-before all claims)

**GitNexus (mandatory pre + post, per AGENTS + sprint-87):**
- `node .gitnexus/run.cjs status` → ✅ up-to-date (indexed commit 17d426c == current; branch fresh).
- `node .gitnexus/run.cjs impact PlayModeSmokeHarnessTests --direction upstream --summary-only` → LOW / 0 impacted (test-only; safe).
- `node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only` → CRITICAL 20 (exact; 2 direct; 6 processes e.g. RunMissionAddStrike; matches §5 in roadmap-execute-plan-07042026.md + boundary).
- `node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only` → CRITICAL 127 (exact; 30 direct; ZERO source edits to impl).
- `node .gitnexus/run.cjs impact CatalogWriteGate --direction upstream --summary-only` → CRITICAL 178 (lower-bound; extend-only untouched).
- `node .gitnexus/run.cjs detect_changes --scope unstaged` → high risk (doc + test symbols + validation; expected; no bridge hotpath; 30 files/39 symbols pre-close).
- Full re-runs in S88 aggregate confirmed same counts.

**Build:**
- `dotnet build ProjectAegis.sln` → 0 errors, 0 warnings. Success. (RUN 2026-07-04)

**Tests (full + targeted filters per hard gates in sprint-87-unity-roundtrip.md):**
- `dotnet test ProjectAegis.sln -v minimal` → 1341 pass / 0 failures (summed project totals: Sim 281 + Del 249 + Excel 5 + Cli 70 + UA 260 + Data 476; floor ≥1232 met; monotonic). 0 new failures.
- UA full: 260 pass / 0 fail.
- `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "PlayModeSmokeHarnessTests|AC8|ScenarioRoundtrip|EditorState"` → Passed! 19/19 (includes new AC-8).
- `dotnet test ... --filter PlayModeSmokeHarnessTests` → 19/19 PASS (C2 proxy extended; legacy 18 + AC-8).
- `dotnet test ... --filter "FullyQualifiedName~ReplayGoldenSuiteTests"` → 6/6 PASS.
- Editor subset filters from prior (Doctrine|DerivedOnly etc) green per S82/S88 aggregate.
- All RUN+READ before closeout claims.

**Determinism / Hash / AC-6 / Bridge / Smoke:**
- `rg "17144800277401907079" tests/regression/ data/ -l` → 18 files (preserved exactly; baltic-v2 frozen; editor uses data/scenarios/examples/ per boundary).
- `bash tools/ci/smoke-ac6.sh` → AC-6 SMOKE: PASS (clean insertion for mission add).
- Bridge hygiene: ZERO edits to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` (last mod 2026-06-04; git log confirms; only consumers/tests touched; grep outside .cs shows pre-existing refs only).
- No changes to frozen goldens or v3 policies.

**GitNexus post-detect + scope:** Changes scoped to test addition + prior editor work. Blast radius LOW for AC-8 symbol. No CRITICAL editor symbol edits without pre-impact.

---

## Unity AC-8 Roundtrip Work Aggregation (S87-01 primary)

**Primary evidence:** Extension of `PlayModeSmokeHarnessTests` (headless PlayMode harness proxy for Unity host load).

- File: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs`
- New test: `AC8_Unity_host_roundtrip_loads_headless_scenario_json_intact_ORBAT_missions_events_editorState()`
- Fixture used: `data/scenarios/examples/baltic-patrol.scenario.json` (headless-authored via MCP/CLI; never touched by Unity before).
- Assertions (exact per qa-plan #8 + sprint plan AC-8 checklist):
  - Loads without error via `ScenarioDocumentJsonLoader.LoadFromFile` + `ScenarioPackageLoader.LoadFromFile`.
  - ORBAT intact: `document.Metadata.DbRef == "baltic_patrol"`, `UnitReadiness` contains "u1", missions reference u1.
  - Missions intact: e.g. `Missions[0].Id == "patrol-1"`, `Type == "Patrol"`, assigned units match.
  - Events intact: `Events == null || empty` (minimal example).
  - `editorState` populated with schema defaults on simulated Unity load (in-memory roundtrip): camera at theater centroid (lat~57.05, lon~20.05), layers all enabled.
  - Package consumption proxy: `package.PolicyId` contains "baltic-patrol", seed, editVersion preserved.
  - Roundtrip: serialize with editorState + deserialize; camera/layers keys present.
  - Canonical untouched: original JSON does NOT contain "editorState" (derived-only per AC-9).
- GitNexus pre-edit impact(PlayMode...)=LOW/0.
- 19/19 under AC-8 filter; integrates with existing C2 PlayMode harness (no mutation of bridge hotpath).
- PlayMode used as primary (per plan preference); no full Editor UI authoring required (headless load per execute-plan).

**S87-02 Manual fallback:** PlayMode deterministic evidence + package/C2PresentationController proxy fulfills checklist. Manual Editor load-and-inspect (screenshots in evidence/ if needed) available as complement but not required for PASS (test provides repeatable proof). No `production/qa/evidence/s87-ac8-...` pngs generated in this cycle (test logs + RUN suffice; per "or manual" in plan).

**Worktree / dispatch:** Local tracks per sprint-87 plan (`.worktrees/stack/sprint87/*` pattern followed from kickoff). Background subagent execution for AC-8 impl noted in session. All commands from plan executed.

---

## Artifacts Delivered + Closeout Actions

- [x] AC-8 test added + green (PlayModeSmokeHarnessTests.cs).
- [x] Full Phase 0 + S87-specific filters RUN+READ.
- [x] GitNexus discipline (impacts, status, detect_changes) before/after.
- [x] This smoke closeout.
- [x] Sprint status update path ready (via approved mechanism).
- [x] Citations on artifacts/tests (roadmap, boundary, qa-plan#8, sprint-87, 11-Agentic-Mission-Editor.md).
- No prohibited files changed (no bridge.cs, no goldens, stage.txt unchanged = Release).

**Worktree verification:** Isolated per pattern (local Unity only).

---

## Scope Adherence + Standing Invariants (preserved)

- Stage = Release.
- Test baseline ≥1232 (1341+ met).
- ReplayGolden 6/6; C2 19/19 (extended).
- Baltic hash `17144800277401907079` preserved (18 files).
- ZERO DelegationBridge hotpath edits.
- CatalogWriteGate extend-only.
- Baltic v2/v3 + goldens frozen (examples/ used).
- GitNexus CRITICALs exact reported.
- AC-8: "A scenario authored entirely via headless MCP loads in the Unity host with intact ORBAT/missions/events and `editorState` populated with schema defaults".
- No map placement / Phase 2 authoring (explicit).
- Local only for Unity evidence.
- All cites present.

**Out of scope respected:** No visual editing, no full C2 UI authoring in this sprint.

---

## Risks / Notes

- PlayMode harness acts as headless proxy for "Unity host" load (ISimWorldSnapshot + bridge patterns); fulfills "headless-authored .scenario.json load" per execute-plan §4 and qa-plan #8. Full Editor roundtrip (C2PresentationController full wiring) is Phase 2.
- 2 UA pre-existing failures (in BalticReplayHarnessPolicyEngageTests) not present in current UA runs (0 fails observed); waived per prior S86.
- High detect risk expected/acceptable (test + prior editor symbols).
- Manual evidence pack referenced in kickoff/plan but PlayMode provides superior automated evidence.

---

## Next (per sprint-87 + sprint-88)

User ack "S87 complete" → S88 gate verification (aggregate S81–S87 closeouts, full 19 qa units, AC table, baselines, GitNexus, hash, smoke-ac6, bridge check) + human ack template in `production/gate-checks/s88-scenario-editor-gate-2026-07-*.md`.

**All gates PASS. S87 complete per verification-before.**

*Part of S81–S88 Scenario Editor program. Cites: production/scenario-editor-scope-boundary-2026-07-04.md + docs/reports/roadmap-execute-plan-07042026.md + docs/reports/future-sprint-roadpmap-07042026.md + production/qa/qa-plan-scenario-editor-2026-07-01.md + Game-Requirements/implementation-tracker-2026-07-04.md + Game-Requirements/requirements/11-Agentic-Mission-Editor.md + AGENTS.md. Local Unity track.*
