# S70 Worktree / GT Notes — Store + Community Prep

**Per:** roadmap-execute-plan-062526.md §4/§5 (S70 tracks: store-pages, community-templates, checklist-v3, closeout; wave baseline → parallel → closeout; stack prefix `stack/sprint70/*`; worktree `.worktrees/stack/sprint70/*`; local closeout)

**Stack prefixes (exact):**
- `stack/sprint70/store-pages`
- `stack/sprint70/community-templates`
- `stack/sprint70/checklist-v3`
- `stack/sprint70/closeout` (local coordinator)

**Commands (gt per AGENTS.md + execute §5 Phase 2 + S69 closeout):**
```
# Create / dispatch
gt create stack/sprint70/store-pages
# ... similar for others; isolated worktrees

# Per track work + submit
git add ...
git commit ...
gt submit --stack --no-interactive

# Closeout (local on main after all):
gt sync
gt restack
# re-verif gates + GitNexus detect_changes pre any final
gt submit --stack --no-interactive
```

**Pre-commit:** GitNexus (search_tool + use_tool list/detect/impact on CRITICALs); detect_changes before commit; low risk expected (docs).

**Verification:** Always RUN+READ full build/test/replay/C2/hash/bridge + GitNexus after restack.

**Cites:** commercial-launch-scope-boundary-2026-06-25.md + roadmap-execute-plan-062526.md §3/§4/§5/§9 + future-sprint-roadpmap-062526.md + AGENTS.md + sprint-70-*.md + smoke-70

**S70-05 closeout note:** Use this for gt notes. S70 dispatch ready.

See also: production/sprints/sprint-70-store-community-prep.md , production/agentic/sprint-70-parallel-kickoff-2026-06-25.md , smoke-sprint-70-closeout-2026-06-25.md (stub + to expand)
