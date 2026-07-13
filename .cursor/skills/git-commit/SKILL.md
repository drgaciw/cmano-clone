---
name: git-commit
description: >
  Draft conventional commit messages, commit local changes, push to main on origin,
  merge branches into main, and clean/prune git worktrees. Use when the user asks
  to commit, push to main, merge and commit, clean worktrees, prune worktrees,
  or mentions "/commit", "git commit", or "ship to main".
disable-model-invocation: true
---

# Git Commit (Project Aegis)

End-to-end git commit workflow for **cmano-clone**. Trunk is **`main`** (see [`.graphite_repo_config`](../../.graphite_repo_config)).

## When to use which path

| User intent | Path |
|-------------|------|
| Draft or refine a message only | [1. Commit message](#1-commit-message) |
| Commit locally (any branch) | [2. Local commit](#2-local-commit) |
| Land on remote `main` (direct trunk) | [3. Commit and push main](#3-commit-and-push-main) |
| Merge a branch into `main`, then push | [4. Merge and commit](#4-merge-and-commit) |
| Remove stale worktrees / branches | [5. Worktree cleanup](#5-worktree-cleanup) |

**Graphite stacks:** If the current branch is Graphite-tracked (`gt log short` shows it in a stack), use `gt submit` / `gt sync` instead of raw `git push` — see [graphite-github-substitute-plan.md](../../docs/engineering/graphite-github-substitute-plan.md). This skill’s push-to-main path is for **direct trunk work on `main`** or when the user explicitly requests trunk push.

## Hard rules

- **Only commit or push when the user explicitly asks** (this skill invocation counts as explicit).
- **NEVER** update git config.
- **NEVER** `--force`, `--hard`, or force-push to `main`/`master` unless the user explicitly requests it — warn first.
- **NEVER** skip hooks (`--no-verify`, `--no-gpg-sign`) unless the user explicitly requests it.
- **NEVER** commit secrets (`.env`, credentials, keys, tokens).
- **NEVER** use interactive git (`-i` flags: rebase -i, add -i).
- **NEVER** amend unless: user asked to amend, HEAD commit is yours and unpushed, and hook did not reject the prior commit.
- If a hook rejects a commit, **fix and create a new commit** — do not amend a failed commit.
- Before commit/submit on code changes: run GitNexus `detect_changes()` (see [AGENTS.md](../../AGENTS.md)).

---

## 1. Commit message

Analyze changes, then produce a conventional commit message (do not commit unless the user also asked to commit).

### Gather context (run in parallel)

```bash
git status
git diff
git diff --staged
git log -5 --oneline
```

Use staged diff when something is staged; otherwise use working-tree diff.

### Message format

```
<type>[optional scope]: <description>

[optional body — why, not what]

[optional footer: Closes #123, BREAKING CHANGE: ...]
```

| Type | Use for |
|------|---------|
| `feat` | New behavior |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `test` | Tests only |
| `refactor` | Restructure, no behavior change |
| `perf` | Performance |
| `build` / `ci` | Build or CI |
| `chore` | Maintenance |

**Style:** imperative, present tense, ≤72 chars in subject, one logical change per commit.

### Scope hints (this repo)

- `data` — ProjectAegis.Data / catalog
- `sim` — ProjectAegis.Sim
- `delegation` — ProjectAegis.Delegation*
- `unity` — unity/ProjectAegis
- `production` — production/ sprint docs
- `ci` — Buildkite / GitHub Actions

Present the draft message to the user before committing unless they asked for a fully automated commit.

---

## 2. Local commit

### Checklist

```
- [ ] git status + diff reviewed
- [ ] No secrets in diff
- [ ] GitNexus detect_changes() run (code changes)
- [ ] Message drafted (section 1)
- [ ] User asked to commit
```

### Stage and commit

```bash
# Stage explicit paths only — avoid git add -A unless user wants everything
git add path/to/changed/files

git commit -m "$(cat <<'EOF'
type(scope): short description

Optional body explaining why.
EOF
)"
```

Verify:

```bash
git status
git log -1 --stat
```

---

## 3. Commit and push main

Direct trunk landing: work happens on **`main`**, commit locally, push to **`origin/main`**.

### Preconditions

```bash
git branch --show-current    # must be main (or user approved committing on main from detached/other)
git fetch origin
git status                   # warn if behind/ahead of origin/main
```

If not on `main`:

```bash
git checkout main
git pull --ff-only origin main   # integrate remote first; stop if merge required and user didn't ask to merge
```

### Workflow

1. Complete [2. Local commit](#2-local-commit) on `main`.
2. Push:

```bash
git push origin main
```

3. Confirm:

```bash
git status
git log origin/main -1 --oneline
```

**Do not** `git push --force` to `main`. If push is rejected (non-fast-forward), stop and report — user must merge/rebase explicitly.

---

## 4. Merge and commit

Merge another branch into `main`, commit the merge, push to remote.

### Identify branches

```bash
git branch --show-current
git branch -a
git log --oneline main..OTHER_BRANCH   # commits to merge
git log --oneline OTHER_BRANCH..main   # commits on main not in branch
```

Confirm with the user which branch merges into `main` if ambiguous.

### Merge workflow

```bash
git fetch origin
git checkout main
git pull --ff-only origin main

git merge OTHER_BRANCH -m "$(cat <<'EOF'
merge(scope): integrate OTHER_BRANCH into main

Brief summary of what the merged work delivers.
EOF
)"
```

If merge conflicts:

1. List conflicted files: `git diff --name-only --diff-filter=U`
2. Resolve in working tree.
3. `git add` resolved files.
4. `git commit` (no `-i`); reuse merge message if Git opened one.

Push after successful merge:

```bash
git push origin main
```

### Graphite alternative

When merging stack work prefer Graphite merge order (bottom PR first) and `gt sync` after merge — not manual merge — unless user explicitly wants a local merge commit.

---

## 5. Worktree cleanup

Project worktrees often live under **`.worktrees/`** (see `production/sprint-status.yaml`).

### Inspect

```bash
git worktree list
git branch -vv | grep ': gone]'    # branches whose upstream vanished
```

### Safe prune (recommended order)

```bash
# Drop stale worktree metadata (does not delete branches)
git worktree prune

# Remove a specific worktree (must be clean or use --force with care)
git worktree remove .worktrees/NAME
# or: git worktree remove --force .worktrees/NAME  # only if user confirms dirty tree loss

# Delete local branch after worktree removed (not main)
git branch -d branch/name
# git branch -D branch/name   # only if user confirms force delete
```

### Remote sync before branch delete

```bash
git fetch origin --prune
```

### Graphite / merged stacks

After PRs merge:

```bash
gt sync    # prunes merged Graphite branches when available
```

### Bulk cleanup pattern

For each path in `git worktree list` under `.worktrees/`:

1. Confirm with user if work is uncommitted.
2. `git worktree remove <path>`
3. `git branch -d <branch>` if no longer needed
4. Finish with `git worktree prune` and `git fetch origin --prune`

**Never** remove the primary checkout worktree (repo root).

---

## Quick reference

| Task | Command |
|------|---------|
| Draft message | Section 1 — parallel `status`, `diff`, `log` |
| Commit | `git commit -m "$(cat <<'EOF' ... EOF)"` |
| Push main | `git push origin main` |
| Merge to main | `git checkout main && git merge BRANCH && git push origin main` |
| Prune worktrees | `git worktree prune` |
| Remove worktree | `git worktree remove .worktrees/NAME` |
| Prune remote refs | `git fetch origin --prune` |

## Related docs

- [Graphite workflow](../../docs/engineering/graphite-github-substitute-plan.md)
- [AGENTS.md — GitNexus detect_changes](../../AGENTS.md)
- Global conventional-commit reference: `~/.claude/skills/git-commit/SKILL.md` (message types only)
