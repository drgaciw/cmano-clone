# S67 Parallel Kickoff — Buildkite Baseline Protection (E10)

**Date:** 2026-06-25
**Per:** release-train-scope-boundary-2026-06-24.md (S67 row), roadmap-execute-plan-062426.md §4/5/9, future-sprint-roadpmap-062426.md §9/10, sprint-67 plan, sprint-66 closeout

## Prereqs (✓ or status)
- Boundary PUBLISHED: production/release-train-scope-boundary-2026-06-24.md (exact S67: Buildkite preflight ∥ Regression baseline lock ∥ Branch-protection ; .buildkite/ alignment with §7 gates; locked baseline docs; ci-and-branch-protection update. All cites, invariants)
- S66 COMPLETE: content manifest (10+9), playtest corpus index, release-checklist-v2.md, gates PASS (1229/0f, 6/6, 18/18, hash preserved, ZERO), GitNexus impacts/detect done
- Baseline gates: ≥1229/0f, ReplayGolden 6/6, C2 18/18, hash 17144800277401907079, ZERO DelegationBridge (verified in S66 closeout + verification-before)
- GitNexus preflights: performed on buildkite agents (LOW risk), protection tools (LOW); Catalog CRITICAL etc untouched per boundary
- Worktrees: ready for tracks
- Dispatch: via dispatching-parallel-agents (after S66 gate complete)
- verification-before plan: included in sprint-67 plan + this kickoff
- AGENTS.md + superpowers: GitNexus impact() MUST before any symbol touch; detect_changes() before commit; verification-before on claims

## Tracks (parallel after S66)
- Buildkite preflight: align .buildkite/pipeline.yml + tools/buildkite/ with §7 gates; preflight harness + local parity (buildkite-ci-lead)
- Regression baseline lock: freeze/lock baselines (tests >=1229, replay/C2/hash/ZERO); produce locked baseline docs + evidence (devops-engineer / qa)
- Branch-protection: update ci-and-branch-protection.md + .github/branch-protection.main.json; enforce `buildkite/cmano-clone`; Graphite compat audit (devops / release)

All tracks isolated; GitNexus pre + verification-before required; cite boundary + S66 + this kickoff on artifacts.

## Commands (exact from boundary/S66/S65 + buildkite-ci.md + AGENTS)
```bash
# Pre-dispatch: GitNexus pre (MUST)
# impact on CI/build files (use MCP gitnexus__impact or equiv)
# detect_changes scope:unstaged or compare main

# Dispatch example (superpowers dispatching-parallel-agents + worktrees)
# gt worktree add ... for each track; dispatch agents

# Per track gates (verification-before):
dotnet build ProjectAegis.sln -c Release
dotnet test ProjectAegis.sln -c Release -v minimal --filter "FullyQualifiedName~Replay|PlayMode"
bash tools/buildkite/agent-baltic-replay.sh
bash tools/verify-ci-local.ps1 || true
grep -r 17144800277401907079 tests/ production/ --include="*.md" | head -5

# Buildkite local preflight
bash tools/buildkite/agent-dotnet-ci.sh

# Graphite
gt create ; gt submit --stack --no-interactive ; gt restack
```

## Skills
- qa-plan, sprint-plan, c-sharp-devops-engineer, buildkite-*, buildkite-pipelines, buildkite-preflight, buildkite-cli, verification-before, GitNexus (search + use first per AGENTS.md), dispatching-parallel-agents, using-git-worktrees

## Dispatch Notes
- Use dispatching-parallel-agents + isolated worktrees post S66 (per sprint-65-parallel-kickoff pattern + execute-plan §5)
- One owner per track file (e.g. pipeline.yml owner = preflight track)
- Closeout track after parallel: integrate, gt restack, full gates re-run + verification-before, update status + docs, GitNexus reindex note
- Monitor subs; no merge until all PASS + boundary cites

## Next
- Monitor tracks via worktrees
- Integrate on closeout: re-verif, sprint-status.yaml, production/qa/ , ci-and-branch-protection.md
- S68 prep (gate verification) only after S67 full PASS
- All artifacts: cite release-train-scope-boundary-2026-06-24.md (S67 row) + S66 + invariants

Cites: release-train-scope-boundary-2026-06-24.md + future-sprint-roadpmap-062426.md §0/3/5/7/10 + roadmap-execute-plan-062426.md §4/5/6/8/9 + AGENTS.md (GitNexus + superpowers) + sprint-66-content-manifest-playtest.md + sprint-65-parallel-kickoff-2026-06-24.md + superpowers (dispatching-parallel-agents + verification-before-completion)

*Kickoff for S67 dispatch. Scaffolds only. verification-before plan included. GitNexus pre completed (LOW). Isolated to S67 prep.*

## GitNexus Preflight Summary (prep)
- Performed pre-dispatch: impact() on agent-dotnet-ci.sh / agent-baltic-replay.sh / apply-branch-protection.ps1 / related: LOW risk, 0-26 impacted (doc/alignment scope)
- detect_changes pre: medium (prior S65/S66 changes to manifest/tests; no S67 code changes)
- New docs created: production/sprints/... and production/agentic/... (non-code; will show in future detect as expected)
- No .buildkite/ files or protection scripts edited (prefer new docs); future tracks to re-run impact/detect before any touch
- Invariants preserved: baseline gates, hash, ZERO, GitNexus rules per boundary

Status: S67 PREP COMPLETE (isolated)