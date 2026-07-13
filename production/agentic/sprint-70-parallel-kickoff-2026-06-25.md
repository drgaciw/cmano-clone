# S70 Parallel Kickoff — Store + Community Prep (E7)

**Date:** 2026-06-25  
**Per:** `production/commercial-launch-scope-boundary-2026-06-25.md`, `docs/reports/roadmap-execute-plan-062526.md` §3/§4/§5/§9 (S70 tracks table exact, wave, prereqs S69 complete, create sprint-70 plan + kickoff at dispatch), `docs/reports/future-sprint-roadpmap-062526.md` §3/§4/§9/§10, `production/sprints/sprint-70-store-community-prep.md` (light), S69 complete + S66 v2 checklist input, sprint-status

## Prereqs (S69 complete ✓)
- [x] S69 full COMPLETE (smoke-sprint-69-closeout-2026-06-25.md + sprint-status.yaml s69_status: S69-04 COMPLETE; gates 0e/1232/0f/6/6/18/18/hash `17144800277401907079`/ZERO=0; GitNexus ~19962/37627/2462 @28c582d + impacts 178/97/127/52 CRIT exact)
- [x] Boundary PUBLISHED: `production/commercial-launch-scope-boundary-2026-06-25.md` (supersedes release-train for S69+; E7 prep in/out; invariants carry; stage=Release throughout)
- [x] S66 v2 + inputs: `production/release/release-checklist-v2.md`, `production/playtests/baltic-v2-scenario-manifest.yaml`, `production/qa/evidence/baltic-v2-playtest-index.md`
- Baseline gates (verification-before RUN+READ pre-dispatch): build 0e/0w; test 1232/0f; ReplayGolden **6/6**; C2 **18/18**; hash preserved; ZERO DelegationBridge; GitNexus pre (list canonical, detect low/docs, impact CRIT exact §5/§7 per execute + boundary)
- Worktrees: ready for 4 tracks (`/home/username01/cmano-clone/.worktrees/stack/sprint70/*`)
- Dispatch: via `dispatching-parallel-agents` + Graphite (gt create / submit --stack) + isolated worktrees (after S70 baseline)
- verification-before: included (RUN gates + READ full outputs before any PASS claims)
- AGENTS.md + superpowers: GitNexus impact() MUST before any CRITICAL symbol touch; detect_changes() before commit; docs-only for E7 prep (no bridge/hash/catalog behavior changes); local/cloud routing per `production/agentic/local-cloud-agent-routing.md`
- Skills: sprint-plan (light), c-sharp-devops-engineer (closeout), GitNexus (MCP search_tool first + use_tool), verification-before, dispatching-parallel-agents, using-git-worktrees, gt workflow

**Do not dispatch S70 artifact tracks until S69 complete + baseline + GitNexus pre + commercial boundary + execute-plan cites confirmed.**

## Tracks (parallel after S70 baseline per execute-plan §4 exact)
| Track | Stack prefix | Worktree path | Agent env | Stories | Owner |
|-------|--------------|---------------|-----------|---------|-------|
| Store page drafts | `stack/sprint70/store-pages` | `.worktrees/stack/sprint70/store-pages` | Cloud | S70-01, S70-02 | community-manager |
| Community templates | `stack/sprint70/community-templates` | `.worktrees/stack/sprint70/community-templates` | Cloud | S70-03 | community-manager |
| Checklist v3 skeleton | `stack/sprint70/checklist-v3` | `.worktrees/stack/sprint70/checklist-v3` | Cloud | S70-04 | release-manager |
| Closeout | `stack/sprint70/closeout` | `.worktrees/stack/sprint70/closeout` | **Local** | S70-05 | c-sharp-devops-engineer |

**Wave order (execute §4):** S70 baseline (coordinator) → (W1 store pages ∥ W2 community ∥ W3 checklist skeleton) → W4 Closeout

All tracks: isolated; GitNexus pre + verification-before required on every artifact; cite commercial-launch-scope-boundary-2026-06-25.md + 062526 roadmap + execute-plan + S66 v2 on every deliverable. Local owns baseline + closeout/merge/human; cloud for store/community/checklist docs.

## S70-01/02/03/04 Deliverables (execute §4)
- Store: `production/release/store/store-page-draft.md`, `asset-checklist.md`, `platform-notes.md` (Steam-style; extends S46 B5 paths; v2 corpus aware)
- Community: templates (TBD per track; e.g. discussion starters)
- Checklist: `production/release/release-checklist-v3.md` (skeleton superseding v2 for E7 prep; cites S66 Baltic items as prereqs; store/i18n/launch sections unchecked pending S71/S72)
- All under GitNexus discipline + cites

## Commands (exact from execute-plan §5/§6 + boundary + S69 kickoff pattern)

```bash
cd /home/username01/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"

# Pre-dispatch: GitNexus pre (MUST, FIRST; search_tool first)
# 1. search_tool "gitnexus list_repos detect_changes impact"
# 2. use_tool:
#    gitnexus__list_repos (canonical /home/username01/projects/active/cmano-clone/cmano-clone)
#    gitnexus__detect_changes (scope=unstaged or compare base_ref=main; repo=canonical)
#    gitnexus__impact (target=CatalogWriteGate / PatrolCandidateEngagePolicy / DelegationBridge / BalticReplayHarness, direction=upstream, summaryOnly=true; repo=canonical)
# Expected: ~19962/37627/2462; impacts 178/97/127/52 §7 exact; detect low for docs

# Full gates (verification-before; RUN+READ before claims)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~PlayModeSmokeHarnessTests" -v minimal
rg "17144800277401907079" tests/regression/ -n
rg "DelegationBridge" src/ --glob "!**/DelegationBridge.cs" -l || true

# Dispatch example (superpowers dispatching-parallel-agents + worktrees + gt)
# gt worktree add ... or gt create stack/sprint70/xxx for each track; dispatch agents per track (isolated)

# Per track (doc-only E7 prep):
# - store/community/checklist: read S66 v2 + manifest + evidence; produce drafts/skeleton; GitNexus pre; cite boundary + execute §4
# - closeout: gt submit --stack --no-interactive; gt sync; gt restack; re-run gates; expand smoke-sprint-70-closeout; update sprint-status.yaml + qa/

# Gt notes for S70: use stack/sprint70/ (store-pages etc); gt submit --stack; closeout restack. See stack/sprint70/README.md + AGENTS.md
```

## Skills & Dispatch Note
- Skills: sprint-plan (for light plan), c-sharp-devops-engineer (close S70-05), GitNexus MCP, verification-before, gt (per AGENTS), team-release patterns (checklist), community-manager role.
- Dispatch: AFTER S69 complete + this kickoff + baseline gates + GitNexus pre. Use isolated .worktrees/stack/sprint70/* + parallel agents. Local coordinator for baseline/closeout. 
- Post close S70: S71 dispatch (i18n + launch docs).
- GT: stack/sprint70/* ; user sync post staged.

All artifacts cite: commercial-launch-scope-boundary-2026-06-25.md + roadmap-execute-plan-062526.md §3/§4/§5/§9 + future-sprint-roadpmap-062526.md §3/§6/§7/§10 + AGENTS.md + S69 artifacts + S66 release-checklist-v2.md .

**S70 dispatch ready.** (S69 complete; prereqs met; plan + this kickoff created; gates/GitNexus PASS pre.)

*Independent subagent per execute-plan style. Cites all. Self-contained.*
