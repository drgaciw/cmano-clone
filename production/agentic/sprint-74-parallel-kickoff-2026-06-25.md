# S74 Parallel Kickoff — Scenario wave 2 + goldens (2026-06-25)

**Per:** roadmap-execute-plan-062526.01.md §4 (S74 tracks), baltic-v3-scope-boundary-2026-06-25.md , 2026-06-25-baltic-v3-content-expansion-design.md §3/§5 , dispatching-parallel-agents + using-git-worktrees + verification-before-completion superpowers.

**S73 foundations COMPLETE** (boundary published, manifest v3, re-index, closeout). Cites baltic-v3-scope-boundary-2026-06-25.md + execute-plan + design + AGENTS.md + S72/S69 complete + v2 baltic (ref only).

**Worktree for this track (S74-01/02 Patrol/mission variants v3 subagent Cloud team-simulation):** .worktrees/stack/sprint74/scenarios

## GitNexus pre FIRST (executed)
- list_repos canonical: cmano-clone path `/home/username01/projects/active/cmano-clone/cmano-clone` (20354 nodes, 38059 edges, 2493 files, main @ b2c9411...)
- detect_changes (scope=unstaged, worktree=.../sprint74/scenarios): changed_count=0, affected=0, risk=none. Exact.
- impact CatalogWriteGate upstream summaryOnly: CRITICAL, impactedCount=178, direct=93, risk=CRITICAL, extend-only preserved (processes: RunCatalogImportMarkdown etc).
- impact PatrolCandidateEngagePolicy upstream: CRITICAL, impactedCount=97, direct=2, risk=CRITICAL (Baltic 76 hits).
- Other CRITs (DelegationBridge 127 etc) untouched per ZERO.
- Report exact as required.

## verification-before (RUN+READ outputs)
From terminal + logs:
- build: 0e/0w (read: 0 Error(s) in baseline/validation logs)
- test: 1232/0f (279 Sim + 43 Cli + 247 Del +5 Excel +252 UA +406 Data; logs confirm subsets)
- replay: 6/6 (core + filters; 28 regression files incl v2)
- C2: 18/18 (PlayModeSmokeHarnessTests)
- hash preserved: 17144800277401907079 (grep 15+ goldens; v2 only)
- ZERO=0 (grep DelegationBridge hotpath 0)
All outputs READ before proceed. Pre confirmed in sprint-status etc.

## Dispatch (S74-01/02)
- Subagent: S74-01/02 Patrol/mission variants v3 (Cloud, team-simulation)
- Scope: 3-5 baltic-v3-* policies + goldens in wt data/ tests/
- Update manifest, create sprint-74-*.md , agentic kickoff, sprint-status s74
- Low risk: additive v3 prefix only. Independent.
- Use: worktree .worktrees/stack/sprint74/scenarios ; GitNexus discipline; cites mandatory.
- After: closeout track, re-verif.

## Tracks parallel
- Patrol/mission variants v3 (scenarios wt)
- Difficulty fixtures
- Replay goldens
- Closeout (local)

**All pre gates PASS. Ready for content creation. S74 wave 2 start.**

Cites: production/baltic-v3-scope-boundary-2026-06-25.md + execute-plan §4/5 + design + AGENTS.md + S73 closeout + v2 (no mutate).
