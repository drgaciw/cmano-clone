# Scenario Editor Branch Integration Plan — S81-02

**Date:** 2026-07-04  
**Status:** S81-02 deliverable (docs-only). Produced in parallel after S81-01 boundary.  
**Authority:** `production/scenario-editor-scope-boundary-2026-07-04.md` (S81-01) + `docs/reports/roadmap-execute-plan-07042026.md` §4 S81 + §5/§8 + `docs/reports/future-sprint-roadpmap-07042026.md` §0/§3/§10 + `Game-Requirements/implementation-tracker-2026-07-04.md` + AGENTS.md + `docs/engineering/graphite-github-substitute-plan.md` + `production/release-train-scope-boundary-2026-06-24.md` (verif-before pattern)

---

## Purpose

Document the safe merge of the in-flight `fix-scenario-publish-cli-wiring` stack (validation tracks A–D, +48 tests) into trunk after S81 boundary ack. Provide exact Graphite commands, verification blocks, and risk callouts. **No code changes** — this is the coordination artifact for closeout and future tracks.

---

## Branch Inventory (fix-scenario-publish-cli-wiring @ 17d426c)

Current HEAD: `17d426c`  
Merge base with main: `ed31ded` (approx from log extraction)

Key chronological commits (oldest → newest; derived from `git log --oneline $(git merge-base main HEAD)..HEAD` and tracker):

- Earlier foundation wiring commits (CLI publish, A+B validation, ...)
- `7b0f376` — "wire scenario validation tracks A-D" (core editor + tests)
- ... intermediate
- `17d426c` — "update tracker 2026-07-04" (final pre-S81 state)

**Net impact (per tracker + test counts at authoring):** +48 tests (Cli +11 to 63; Data +37 to 453). Tracks A–D partial implementation present.

**Track mapping (A–D per execute-plan §4):**
- **A (Doctrine / validation rules):** DoctrineInheritanceValidateTests, related ValidationRules
- **B (Event debugger / trace):** EventDebuggerTrace + tests, teleport export prep
- **C (Schema / conformance / save-export):** SchemaConformance, DerivedOnlyInvariant, SaveVsExportGate
- **D (Export / undo / ferry):** ScenarioExportCommand, ScenarioUndo*, Mission*FerryCommand + tests + simulate sample extensions

All changes additive / editor-scoped. Confirmed ZERO edits to DelegationBridge.cs or CatalogWriteGate hot paths.

---

## Current Branch / Worktree / Detect-Changes State (RUN 2026-07-04, pre-gt-submit)

**Current branch:** `fix-scenario-publish-cli-wiring` @ `17d426c` (HEAD of in-flight stack for S81 tracks A–D + prep)

**gt stack summary (gt status / gt log):**
- PR #237 (Draft): "fix(scenario-editor): wire scenario_publish verb into CLI dispatch"
- Stack commits (bottom→top): ... → 7b0f376 (wire validation tracks A-D) → 17d426c (tracker + AGENTS)
- Graphite shows parent `main` @ ed31ded; feature branch ahead.

**Worktree info:**
- Primary: `/home/username01/projects/active/cmano-clone/cmano-clone` on feature branch
- `.worktrees/stack/sprint81/` (and siblings): `closeout/`, `boundary/`, `branch-integration/`, `gitnexus-reindex/` prepared (per execute-plan §4)
- S82+ worktrees already materialized: `.worktrees/stack/sprint82/{closeout,doctrine-validation,schema-conformance,save-export-gate}`, `sprint83/*` etc. (parallel prep)
- `stack/` dir holds legacy sprint manifests; active S81/S82 use `.worktrees/stack/*`

**GitNexus (MCP + CLI verified):**
- `node .gitnexus/run.cjs status`: ✅ up-to-date (indexed commit == 17d426c on branch index)
- `gitnexus__list_repos` + branch index shows `fix-scenario-publish-cli-wiring` fresh
- `node .gitnexus/run.cjs detect-changes` (unstaged): Changes: 9 files, 3 symbols, 4 processes affected, **risk: medium** (doc/schema prep changes; no hot sim/delegation symbols)
  Changed symbols incl. ScenarioValidationExportGate, docs roadmap alias.
- Impacts (upstream, summaryOnly; exact match authoring):
  - CatalogWriteGate: 178 CRITICAL
  - ScenarioDocumentEditor: 20 CRITICAL
  - ScenarioValidationEngine: 17 HIGH
  - DelegationBridge: 127 CRITICAL
  - BalticReplayHarness: 52 CRITICAL
  - PatrolCandidateEngagePolicy: 97 CRITICAL
- MCP calls (search_tool → list_repos → detect_changes(scope=staged) [0 changes] + impact(CatalogWriteGate upstream summaryOnly)) executed as verification-before.

**Other status (RUN+READ):**
- Build: succeeded 0E/0W
- Targeted tests: Replay 6/6, C2 18/18, editor subset ~49/0 (Data.Tests filter)
- Full suite: ~2 known UA fails only (baseline held ≥1232)
- Hash: 18 files contain `17144800277401907079`
- smoke-ac6.sh: PASS
- DelegationBridge hotpath: ZERO source edits (only expected consumers + definition)

**Cites for this state:** scenario-editor-scope-boundary-2026-07-04.md + roadmap-execute-plan-07042026.md §5 + AGENTS.md (verification-before for trunk resolution) + graphite plan.

---

## Graphite / Stack Workflow (user-run post S81-01 ack)

**Precise commands (from current branch state, execute-plan §5/Phase 2, graphite-github-substitute-plan.md, AGENTS.md verification-before note for trunk resolution / "trunk out of date" blocks):**

```bash
# === PRE-SUBMIT (on feature branch) ===

# 1. Checkout / ensure on the S81 in-flight stack branch
git checkout fix-scenario-publish-cli-wiring
git pull --ff-only || true

# 2. verification-before (MANDATORY before gt submit/sync/restack when trunk resolution or blocked):
# (1) GitNexus pre (search_tool/list_repos + detect_changes(scope=staged) + impact on CatalogWriteGate upstream summaryOnly + editor symbols)
node .gitnexus/run.cjs status
node .gitnexus/run.cjs detect-changes
node .gitnexus/run.cjs impact CatalogWriteGate --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only

# (2) full gates RUN+READ (0 errors, expected test counts)
export PATH="$HOME/.dotnet:$PATH"
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal                 # ≥1232 pass; 0 new failures (2 UA known only)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~ReplayGoldenSuiteTests"   # 6/6
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter PlayModeSmokeHarnessTests                     # 18/18
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly"
rg "17144800277401907079" tests/regression/ data/ -l     # expect 18 (baltic frozen)
bash tools/ci/smoke-ac6.sh                               # PASS (AC-6 touched lightly)
# Confirm: grep -r DelegationBridge src/ --include="*.cs" shows ZERO *new* hotpath source (only expected)

# (3) Stage ONLY S81 payload files (per smoke closeout list + boundary; do not stage unrelated .cursor/hooks etc.)
# git add production/agentic/scenario-editor-branch-integration-plan-2026-07-04.md ... (approved list only)
# git status --short   # review before commit in stack

# 3. Submit the full stack (non-interactive; pushes + updates PR #237)
gt submit --stack --no-interactive

# === POST-MERGE (after bottom PR merged on Graphite dashboard; coordinator on main) ===

# 4. Checkout trunk + sync/restack (resolves "trunk out of date")
git checkout main
gt sync
gt restack

# 5. Post-merge full Phase 0 re-verification (RUN+READ all outputs before claim)
export PATH="$HOME/.dotnet:$PATH"
cd /home/username01/projects/active/cmano-clone/cmano-clone

node .gitnexus/run.cjs status
node .gitnexus/run.cjs detect-changes
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
node .gitnexus/run.cjs impact CatalogWriteGate --direction upstream --summary-only
node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only
node .gitnexus/run.cjs impact BalticReplayHarness --direction upstream --summary-only

dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~ReplayGoldenSuiteTests"   # 6/6
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter PlayModeSmokeHarnessTests                     # 18/18
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly"
rg "17144800277401907079" tests/regression/ data/ -l     # 18 files
bash tools/ci/smoke-ac6.sh
# Re-confirm ZERO DelegationBridge hotpath + GitNexus CRITICAL counts exact + editor impacts (20/17)

# 6. (Optional/local) GitNexus re-index if needed: node .gitnexus/run.cjs analyze
```

**Merge order (per execute-plan §5 Phase 2 + branch plan):** S81-04 closeout → user `gt submit --stack --no-interactive` (from feature) → main restack/sync → full Phase 0 re-verif (build/tests/Replay/C2/editor-subset/GitNexus impacts on ScenarioDocumentEditor/ValidationEngine/Catalog/Delegation/Baltic + hash + smoke-ac6) → post-merge GitNexus + closeout update → ack → S82 dispatch.

Use `.worktrees/stack/sprint81/*` (and sprint82+) for S81+ tracks per roadmap/execute-plan. **Never** use `gh pr create` or raw `git push` on Graphite stacks.

**Cites:** scenario-editor-scope-boundary-2026-07-04.md + roadmap-execute-plan-07042026.md §5/§6 + future-sprint-roadpmap-07042026.md + AGENTS.md (verification-before + detect_changes before commit) + graphite-github-substitute-plan.md + release-train-scope-boundary-2026-06-24.md.

---

## Phase 0 Re-verification Block (mandatory before/after merge)

**Full expanded post-merge (and pre-submit) block (execute-plan §5 + user task requirements):** includes explicit GitNexus impacts on `ScenarioDocumentEditor`/`ScenarioValidationEngine`/`Catalog`/`Delegation`/`Baltic`, full suite, editor subset, hash, smoke-ac6, plus pre-step verification-before.

See the **Graphite / Stack Workflow** section above for the complete ready-to-run user commands (includes MCP pre + CLI).

**Evidence at S81 authoring (17d426c):**
- Build 0E/0W
- 1308 pass / 2 fail (UA pair only)
- Replay 6/6, C2 18/18
- Hash present (18 files)
- GitNexus up-to-date, CRITICAL counts exact (178/97/127/52)
- ZERO DelegationBridge hotpath source edits
- (Updated in verif RUN above + closeout)

Re-run the full block (from the commands section) and record deltas in S81-04 closeout. All RUN outputs must be READ before PASS claims.

---

## Risks & Exclusions

- **2 known UA failures** (in `BalticReplayHarnessPolicyEngageTests`):
  1. Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log
  2. Restricted_engagement_scenario_fingerprint_is_deterministic
  **Owner:** S86 UA triage track. Do **not** address in S81–S85. Waive or fix with ack at gate.

- In-flight stack is +9 commits ahead of remote PR #237 at time of authoring. Re-submit after boundary.

- Env dotnet 8.0.422 (plan notes 8.0.400 target via global.json) — build/tests passed cleanly.

---

## Next Actions

1. User reviews + acks this plan + S81-01 boundary.
2. On `fix-scenario-publish-cli-wiring`: run the exact **verification-before + gt submit --stack --no-interactive** block above.
3. Coordinator: `git checkout main; gt sync; gt restack`; re-run full Phase 0 (impacts on ScenarioDocumentEditor/ValidationEngine/Catalog/Delegation/Baltic + all gates + hash + smoke-ac6).
4. S81-04 closeout update + GitNexus post + sprint-status (via approved path).
5. User ack "S81 complete" → dispatch/continue S82 validation tracks.

**S82 note (per execute-plan §4/§5 + roadmap):** After S81 restack on main, S82 tracks (doctrine-validation, schema-conformance, save-export-gate, closeout) run in their `.worktrees/stack/sprint82/*` branches (already prepped). Each S82 track performs its own GitNexus pre (impact on ValidationRules/Scenario* + Catalog/Bridge) + gates + `gt submit --stack --no-interactive` from its branch. Closeout track on main does sync/restack + Phase 0. Use same verification-before pattern before any trunk ops. S82 artifacts cite this plan + boundary + execute-plan. Do not touch v2 goldens or DelegationBridge.

**All S81-02 requirements satisfied.** Cites and verification-before applied throughout.

---
*Updated with current branch state + exact commands + post-merge verif (S81 + S82 note) per user task. Matches execute-plan §5/§12 + scenario-editor-scope-boundary-2026-07-04.md + AGENTS.md.*
