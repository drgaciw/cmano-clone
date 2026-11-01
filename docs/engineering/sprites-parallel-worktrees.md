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

1. Run the DLL copy step in that worktree (see `delegation-plugin-dll-copy.md`).
2. `dotnet test ProjectAegis.sln` once.
3. `jb inspectcode` scoped to changed projects.
4. Commit, push, `gh pr create --draft`.

## Contention

- Concurrent `dotnet` builds in separate worktrees share `~/.nuget/packages`;
  restore is safe but the first restore in each worktree is slow.
- Full-solution test + inspect dominates wall time; docs jobs finish in seconds.
- Watch memory with `free -h`; ReSharper inspect is the heaviest step.
- Clean up with `git worktree remove ~/wt/jN` after the PR is open.
