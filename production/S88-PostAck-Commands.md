# S88 Post-Ack Commands — Safe Sequence (fix-scenario-publish-cli-wiring)

**Date:** 2026-07-04  
**Branch/Head (confirmed):** `fix-scenario-publish-cli-wiring` @ `cb14438`  
**GitNexus status (RUN+READ):** ✅ up-to-date (indexed commit cb14438 matches current; branch snapshot fresh).  
**Unstaged changes (RUN+READ + GitNexus detect):** 7 files only (docs/tools/buildkite/cursor state):  
`.buildkite/pipeline.yml`, `.cursor/hooks/state/continual-learning-index.json`, `.cursor/hooks/state/continual-learning.json`, `docs/engineering/buildkite-ci.md`, `docs/reports/future-sprint-roadpmap-062126.md`, `tools/buildkite/dotnet-ci.sh`, `tools/verify-ci-local.ps1`.  
**GitNexus detect_changes --scope unstaged:** Changes: 7 files, 3 symbols, Affected processes: 0, Risk level: **low**. (No high-risk source, no DelegationBridge hotpath, no goldens, no CatalogWriteGate/Policy edits.)  
**Cites (mandatory):** `production/gate-checks/s88-scenario-editor-gate-2026-07-04.md` + `production/scenario-editor-scope-boundary-2026-07-04.md` + `production/agentic/scenario-editor-branch-integration-plan-2026-07-04.md` + `docs/reports/roadmap-execute-plan-07042026.md` + `docs/reports/future-sprint-roadpmap-07042026.md` + AGENTS.md + `production/qa/qa-plan-scenario-editor-2026-07-01.md` + implementation-tracker-2026-07-04.md.

**Status:** POST-ACK PREP COMPLETE (read-only additive note only). No git push/submit executed. Awaiting human ack phrase ("scenario editor program complete" or equivalent per S88 gate) to proceed with user-driven gt commands.

**SE-W0 update (2026-07-08):** Headless engineering is **on main** (`fix-scenario-publish-cli-wiring` is an ancestor of `main`). Formal S88 phrase still optional for historical headless-only close; the **active completion epic** (`scenario-editor-completion`) collects a single combined ack at **SE-W3**: *"scenario editor headless + AC-8 program complete"*. See `production/agentic/se-w0-status-truth-2026-07-08.md`.

## Exact Safe Post-Ack Commands (from gate + branch-integration-plan + scope-boundary + AGENTS.md verification-before)

**Always:** Graphite-first. Use `gt` (never `gh pr create` or raw `git push` on stack). Run **full verification-before** (GitNexus pre + gates RUN+READ all outputs) before any `gt submit` / sync / restack when trunk resolution or blocked. Stage **only** approved payload files (S88 / editor closeout list; no .cursor/hooks etc). Re-verif post each gt step. Cite gate/boundary everywhere.

### 1. Pre-Submit (on feature branch `fix-scenario-publish-cli-wiring`)
```bash
# Ensure on branch
git checkout fix-scenario-publish-cli-wiring
git pull --ff-only || true

# GitNexus pre (mandatory per AGENTS + boundary § GitNexus preflight + gate)
node .gitnexus/run.cjs status
node .gitnexus/run.cjs detect_changes --scope unstaged
node .gitnexus/run.cjs detect_changes --scope compare --base-ref main   # expected critical for program delta
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only
node .gitnexus/run.cjs impact CatalogWriteGate --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
node .gitnexus/run.cjs impact BalticReplayHarness --direction upstream --summary-only
node .gitnexus/run.cjs impact PatrolCandidateEngagePolicy --direction upstream --summary-only

# Full gates RUN+READ (verification-before; all outputs READ before proceed)
export PATH="$HOME/.dotnet:$PATH"
cd /home/username01/projects/active/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln                   # 0 errors, 0 warnings
dotnet test ProjectAegis.sln -v minimal          # ≥1232 / 0 failures (1341 observed; UA waived pair only)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests   # 19/19 (incl AC-8)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests"   # 6/6
rg "17144800277401907079" tests/regression/ data/ -l   # 18 files (baltic-v2/v3 frozen preserved)
bash tools/ci/smoke-ac6.sh                       # "AC-6 SMOKE: PASS"
# Confirm ZERO DelegationBridge hotpath source (grep -r "DelegationBridge" src/ --include="*.cs" | grep -v "Tests\|README\|Bridge" or exact count 0 new)
# Editor subset (optional targeted): dotnet test ... --filter "ScenarioDocumentEditor|ScenarioValidation|..."
```

**Stage only approved (post RUN+READ, per closeout lists):** `git add` (approved S88 payload docs/tests only); `git status --short`; review.

**Submit stack:**
```bash
gt submit --stack --no-interactive
```

### 2. Post-Merge / Trunk Resolution (after bottom PR merged via Graphite dashboard; coordinator switches)
```bash
git checkout main
gt sync
gt restack
```

### 3. Post-Merge / Post-Restack Full Re-Verification (RUN+READ before any further claims)
```bash
export PATH="$HOME/.dotnet:$PATH"
cd /home/username01/projects/active/cmano-clone/cmano-clone

# GitNexus pre/post (MCP or CLI; impacts on §5 CRITICALs + editor symbols)
node .gitnexus/run.cjs status
node .gitnexus/run.cjs detect_changes --scope unstaged
node .gitnexus/run.cjs detect_changes --scope compare --base-ref main
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only
node .gitnexus/run.cjs impact CatalogWriteGate --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
node .gitnexus/run.cjs impact BalticReplayHarness --direction upstream --summary-only

dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests"   # 6/6
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests   # 19/19
rg "17144800277401907079" tests/regression/ data/ -l     # 18 files
bash tools/ci/smoke-ac6.sh
# Re-confirm: ZERO new DelegationBridge hotpath source edits; GitNexus CRITICAL counts exact (178/97/127/52 + editor 20/17); no regressions.
```

**Also (per AGENTS verification-before for trunk out-of-date blocks):** GitNexus pre (search_tool + list_repos + detect_changes(scope=staged) + impact(CatalogWriteGate upstream summaryOnly)) before gt sync/restack/submit.

## Notes
- All commands from S88 gate doc (Commands section), scenario-editor-branch-integration-plan-2026-07-04.md (Graphite / Stack Workflow + Phase 0 Re-verif), scenario-editor-scope-boundary-2026-07-04.md (GitNexus preflight + worktree/gt convention), AGENTS.md (full block + hash grep + ZERO bridge + gt table), roadmap-execute-plan-07042026.md.
- Stage remains **Release**. No hotpath changes. Baltic v2 hash `17144800277401907079` preserved; baltic-v3 isolated.
- After re-verif + user ack phrase: proceed to gt submit (user), then S88 gate sign-off close, optional trunk merge decision.
- Update sprint-status.yaml, implementation-tracker only on approved path post-ack.
- **Do not execute gt submit / push here** (this subagent is read-only prep + additive note).

**POST-ACK PREP COMPLETE - awaiting human ack phrase to execute.**

(Cites: production/gate-checks/s88-scenario-editor-gate-2026-07-04.md + production/scenario-editor-scope-boundary-2026-07-04.md + production/agentic/scenario-editor-branch-integration-plan-2026-07-04.md + AGENTS.md)
