# S71 Parallel Kickoff — i18n + Launch Docs + l10n QA Prep (E7)

**Date:** 2026-06-25  
**Per:** `production/commercial-launch-scope-boundary-2026-06-25.md`, `docs/reports/roadmap-execute-plan-062526.md` §3/§4/§5/§9 (S71 tracks table exact, wave (W1 i18n ∥ W2 launch) → W3 l10n-qa-plan (post-inventory) → W4 closeout S71-06, prereqs S70 complete, create/update sprint-71 plan + kickoff at dispatch/prep), `docs/reports/future-sprint-roadpmap-062526.md` §3/§4/§9/§10, `production/sprints/sprint-71-i18n-launch-docs.md` (this prep), S70 COMPLETE + S66 v2 + release-checklist-v3.md inputs, sprint-status, stack/sprint71 gt notes.

## Prereqs (S70 complete ✓ + S69/S66)
- [x] S70 full COMPLETE (smoke-sprint-70-closeout-2026-06-25.md + sprint-status s70_status; store + community + checklist-v3 + release-checklist-v3.md i18n/launch sections skeleton)
- [x] S69 COMPLETE (smoke-sprint-69 + boundary published)
- [x] Boundary PUBLISHED: `production/commercial-launch-scope-boundary-2026-06-25.md` (E7 prep in/out; invariants 1232/6/6/18/18/hash `17144800277401907079`/ZERO; GitNexus; stage=Release throughout S69–S72)
- [x] Inputs: S66 v2 `production/release/release-checklist-v2.md` + `production/playtests/baltic-v2-scenario-manifest.yaml` + `production/qa/evidence/baltic-v2-playtest-index.md`; S70 release-checklist-v3.md (i18n/launch unchecked sections)
- [x] Baseline gates (verification-before RUN+READ pre-dispatch + this prep): build 0e/0w; test 1232/0f (279+43+247+5+252+406); ReplayGolden **6/6**; C2 **18/18**; hash preserved; ZERO DelegationBridge; GitNexus pre (list canonical /home/username01/projects/active/cmano-clone/cmano-clone : 19962/37627/2462 @28c582d; detect low/docs; impact CRIT exact §5/§7: CatalogWriteGate 178, Patrol 97, DelegationBridge 127, BalticReplayHarness 52); /tmp/*-s71*.log READ
- Worktrees: ready for tracks (`/home/username01/cmano-clone/.worktrees/stack/sprint71/*`)
- Dispatch: via `dispatching-parallel-agents` + Graphite (gt create / submit --stack) + isolated worktrees (after S71 baseline)
- verification-before: included (RUN gates + READ full outputs before any PASS claims)
- AGENTS.md + superpowers: GitNexus impact() MUST before any CRITICAL symbol touch (none expected); detect_changes() before commit; docs-only for E7 prep (no bridge/hash/catalog behavior / PlayMode changes unless user ack + TDD); local/cloud routing per `production/agentic/local-cloud-agent-routing.md`
- Skills: sprint-plan (light update), qa-plan (for S71-05), c-sharp-devops-engineer (closeout), GitNexus (MCP search_tool first + use_tool), verification-before, dispatching-parallel-agents, using-git-worktrees, gt workflow

**Do not dispatch S71 artifact tracks until S70 complete + baseline + GitNexus pre + commercial boundary + execute-plan cites confirmed. This prep creates/updates S71 plan/kickoff/status for closeout readiness.**

## Tracks (parallel after S71 baseline per execute-plan §4 exact)
| Track | Stack prefix | Worktree path | Agent env | Stories | Owner |
|-------|--------------|---------------|-----------|---------|-------|
| i18n pipeline spec | `stack/sprint71/i18n-pipeline` | `.worktrees/stack/sprint71/i18n-pipeline` | Cloud | S71-01, S71-02 | localization-lead |
| Launch doc pack | `stack/sprint71/launch-docs` | `.worktrees/stack/sprint71/launch-docs` | Cloud | S71-03, S71-04 | technical-writer |
| Localization QA plan | `stack/sprint71/l10n-qa-plan` | `.worktrees/stack/sprint71/l10n-qa-plan` | Cloud | S71-05 | qa-lead |
| Closeout | `stack/sprint71/closeout` | `.worktrees/stack/sprint71/closeout` | **Local** | S71-06 | c-sharp-devops-engineer |

**Wave order (execute §4):** (W1 i18n ∥ W2 launch docs) → W3 l10n QA plan (after string inventory) → W4 Closeout

All tracks: isolated; GitNexus pre + verification-before required on every artifact; cite commercial-launch-scope-boundary-2026-06-25.md + 062526 roadmap + execute-plan + S66 v2 + S69/S70 on every deliverable. Local owns baseline + closeout/merge/human; cloud for i18n/launch/l10n-qa docs.

## S71-03/04 Launch Doc Pack Track Update (Independent Cloud Subagent)
**S71-03/04 COMPLETE (2026-06-25):** 
- Created: production/release/launch/patch-notes-template.md (versioned skeleton), faq-draft.md (player-facing), support-runbook-draft.md, evidence-index.md (S57-S68 + S69-S71 links: baltic-v2-*, release-checklist-*, store/*, community-templates, gate-checks, qa/smoke/closeouts, sprints/sprint-65..70, sprint-status, GitNexus/gates sections).
- GitNexus pre + verification-before (RUN+READ): list 19962/37627/2462; detect 24/0 low; impacts 178/97/127/52 CRIT exact; build 0e/0w; 1232/0f; 6/6; 18/18; hash preserved; ZERO bridge. All outputs READ. Cites enforced.
- Evidence: full self-contained in launch/*.md + sprint-status.yaml s71_* entries.
- Kickoff update: this section. Pre i18n parallel close + S71-06. Docs only. Per execute-plan §4.

Cites: commercial-launch-scope-boundary-2026-06-25.md ; future-sprint-roadpmap-062526.md §3/§4/§6/§7/§10 ; roadmap-execute-plan-062526.md §3/§4 ; AGENTS.md ; S69/S70 + S57–S68 priors. Ready for S71 closeout.
**S71-05 note:** Locale smoke strategy only. **No PlayMode unless ack.**

## S71-01/02/03/04/05 Deliverables (execute §4)
- i18n: `production/release/i18n-pipeline-spec.md` (extraction, P0 en-US, UI Toolkit vs UGUI); `i18n-string-inventory.md` (C2/HUD/menu paths); `i18n-extraction-plan.md` — **S71-01/02 COMPLETE** (cloud independent subagent per execute-plan §4; GitNexus pre search+use list/detect/impact CRITICAL §5 exact: 19962/37627/2462 + 178/97/127/52; verif-before gates 0e/1232/0f/6/6/18/18/hash/ZERO RUN+READ; cites boundary + future-sprint-roadpmap-062526 §3/6/7/10 + roadmap-execute-plan-062526 §3/4 + AGENTS + S69/S70 complete + S66 v2 + Game-Requirements/20-Command-And-Control-UI.md). Low risk docs-only.
- Launch: `production/release/launch/{patch-notes-template.md, faq-draft.md, support-runbook-draft.md, evidence-index.md}` (links S57–S71 + S66 v2)
- l10n QA: `production/qa/qa-plan-sprint-71-l10n-prep-2026-06-25.md` (locale smoke strategy; P0 en-US; string freeze; no PlayMode)
- All under GitNexus discipline + cites. Stack/sprint71 gt notes. S71-01/02 landed; feeds W3.

## Commands (exact from execute-plan §5/§6 + boundary + S70 kickoff pattern + GitNexus pre done)

```bash
cd /home/username01/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"

# Pre-dispatch + this prep: GitNexus pre (MUST, FIRST; search_tool first)
# search_tool "gitnexus list_repos detect_changes impact"
# use_tool:
#   gitnexus__list_repos (limit 5)
#   gitnexus__detect_changes (scope=all or unstaged, repo=/home/username01/projects/active/cmano-clone/cmano-clone)
#   gitnexus__impact (target=CatalogWriteGate, direction=upstream, summaryOnly=true, repo=/home/username01/projects/active/cmano-clone/cmano-clone)
#   (repeat for PatrolCandidateEngagePolicy, DelegationBridge, BalticReplayHarness)
# Expected/confirmed: 19962/37627/2462; impacts 178/97/127/52 CRIT exact §5; detect low for docs

# Gates verif-before (RUN+READ):
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~PlayModeSmokeHarnessTests" -v minimal   # gate only; no changes S71 l10n
rg "17144800277401907079" tests/regression/ -n
rg "DelegationBridge" src/ --glob "!**/DelegationBridge.cs" -l || true

# GT + dispatch:
gt create stack/sprint71/i18n-pipeline
# ... parallel for launch-docs, l10n-qa-plan
# (closeout local)
gt submit --stack --no-interactive
gt sync; gt restack
# re-verif + smoke + status
```

**GT for stack/sprint71:** See stack/sprint71/WORKTREE-README.md (created). Closeout fills smoke stub + status.

**S71 dispatch / closeout readiness prep complete (l10n QA plan + stub + plan/kickoff/status/gt notes).** Ready for tracks. Cites execute-plan §3/§4 everywhere.

*Independent subagent. GitNexus pre + verif-before executed (list/detect/impact + gates 0e/1232/0f/6/6/18/18/hash/ZERO). Docs only. No PlayMode.*
