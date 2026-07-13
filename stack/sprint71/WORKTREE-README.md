# S71 Worktree / GT Notes — i18n + Launch Docs + l10n QA Prep

**Per:** roadmap-execute-plan-062526.md §3/§4/§5 (S71 tracks: i18n-pipeline, launch-docs, l10n-qa-plan S71-05, closeout S71-06; cloud/local mix: l10n-qa-plan Cloud, closeout **Local**; wave (W1 i18n ∥ W2 launch docs) → W3 l10n QA plan (post string inventory) → W4 Closeout; stack prefix `stack/sprint71/*`; worktree `.worktrees/stack/sprint71/*`)

**Stack prefixes (exact from execute-plan §4):**
- `stack/sprint71/i18n-pipeline` (S71-01, S71-02; localization-lead; Cloud)
- `stack/sprint71/launch-docs` (S71-03, S71-04; technical-writer; Cloud)
- `stack/sprint71/l10n-qa-plan` (S71-05; qa-lead; Cloud)
- `stack/sprint71/closeout` (S71-06; c-sharp-devops-engineer; **Local**)

**Commands (gt per AGENTS.md + execute-plan §5 Phase 2 + S70 closeout pattern):**
```
# Pre: GitNexus + verif-before (MANDATORY; search_tool first then use)
# list_repos (repo=/home/username01/projects/active/cmano-clone/cmano-clone)
# detect_changes (scope=unstaged, repo=canonical)
# impact (target=CatalogWriteGate / PatrolCandidateEngagePolicy / DelegationBridge / BalticReplayHarness, direction=upstream, summaryOnly=true, repo=canonical)

# Create / dispatch (isolated)
gt create stack/sprint71/i18n-pipeline
# ... similar for launch-docs, l10n-qa-plan
# closeout local only at end

# Per track: work, commit, 
gt submit --stack --no-interactive

# Closeout (local on main):
gt sync
gt restack
# re-verif (build/test/replay/C2/hash/ZERO + GitNexus detect pre-commit)
gt submit --stack --no-interactive
```

**Pre-commit / verification-before (every step + closeout):** 
GitNexus (search_tool first + list_repos/detect_changes/impact CRITICAL upstream summaryOnly); full gates RUN+READ: dotnet build 0e/0w; dotnet test sln 1232/0f; ReplayGolden 6/6; C2/PlayModeSmoke 18/18 (but **no PlayMode changes in S71 unless explicit user ack + TDD** per execute-plan §4 S71-05); hash `17144800277401907079` preserved; ZERO DelegationBridge (grep outside its file only); gt status. All outputs READ. Cite boundary + execute-plan §3/§4/§6.

**S71-05 specific (l10n QA plan):** Locale smoke strategy only (P0 en-US inventory verification, string externalization checks, future locale tiers smoke notes). Docs-only; no PlayMode / UI behavior edits. Smoke for post-prep (S72+). See qa-plan-sprint-71-l10n-prep-*.md

**GT notes:** Use stack/sprint71/* for isolated tracks. Closeout owns merge + smoke-sprint-71 + sprint-status + gt notes update. Low risk (docs). Re-index GitNexus post if symbols touched (none expected).

**Cites (MANDATORY on all artifacts):** production/commercial-launch-scope-boundary-2026-06-25.md ; docs/reports/roadmap-execute-plan-062526.md §3/§4/§5/§9 ; docs/reports/future-sprint-roadpmap-062526.md §3/§4/§9/§10 ; AGENTS.md ; S69/S70 complete + smoke-69/70 + sprint-69/70-*.md ; S66 release-checklist-v2.md + Baltic v2 corpus ; production/release/release-checklist-v3.md (i18n/launch sections) ; GitNexus pre results (19962/37627/2462 + impacts 178/97/127/52 exact).

**S71 dispatch / closeout readiness:** S70 COMPLETE assumed; S71 plan/kickoff + l10n-qa-plan + smoke stub + status update in this prep. Ready for S71 tracks dispatch per execute-plan. Closeout S71-06 will fill smoke + final verif + gt integration.

See: production/sprints/sprint-71-i18n-launch-docs.md , production/agentic/sprint-71-*-kickoff-2026-06-25.md , production/qa/qa-plan-sprint-71-l10n-prep-2026-06-25.md , production/qa/smoke-sprint-71-closeout-2026-06-25.md (stub)

*Independent subagent: S71-05 l10n QA plan + S71 closeout prep (cloud/local mix per execute-plan §4). GitNexus pre + verif-before executed. Docs only.*
