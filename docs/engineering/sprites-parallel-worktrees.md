# Parallel jobs on one Sprite with git worktrees

Status: DRG-698. Pattern from Fly.io Sprite trial 3 on `my-first-sprite`.

## Layout

```bash
cd ~/cmano-clone && git fetch origin
git worktree add -b sprite/drg-NNN-<slug> ~/wt/jN origin/main   # one per story
mkdir -p ~/wt/logs
```

Each worktree gets one branch, one draft PR, and one log file
(`~/wt/logs/jN.log`). Launch jobs as background processes and `wait` for all of
them inside the same `sprite exec` session, because processes do not survive
the session ending or the Sprite sleeping.

## Before starting

- Count open product PRs (exclude docs-only and dependency PRs). Product PRs
  plus new code jobs must stay at 4 or fewer. Fill remaining slots with docs.
- Skip stories with an open PR or owned by the Slice bots, SprintSync, or
  HerdrGrok. No hard-no areas.

## Per code job

1. Run `bash tools/copy-delegation-assemblies.sh` in that worktree; see
   [delegation-plugin-dll-copy.md](delegation-plugin-dll-copy.md).
2. Run the build, full solution tests, replay golden, and headless Play Mode
   smoke gates in [local-ci-verification.md](local-ci-verification.md), plus
   the required GitNexus impact and change analysis in [`AGENTS.md`](../../AGENTS.md).
3. Run `jb inspectcode` scoped to changed projects.
4. Commit and submit a draft through the canonical
   [Graphite workflow](graphite-github-substitute-plan.md): track the story
   branch with `gt track --parent main` if needed, then
   `gt submit --draft --no-interactive`. Use `gh pr create --draft` only when Graphite is
   unavailable and the user explicitly approves that fallback.

## Contention

- Concurrent `dotnet` builds in separate worktrees share `~/.nuget/packages`;
  restore is safe but the first restore in each worktree is slow.
- Full-solution test + inspect dominates wall time; docs jobs finish in seconds.
- Watch memory with `free -h`; ReSharper inspect is the heaviest step.
- Clean up with `git worktree remove ~/wt/jN` after the PR is open.
