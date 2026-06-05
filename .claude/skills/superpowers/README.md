# obra/superpowers (v5.1.0)

Global agent methodology from [obra/superpowers](https://github.com/obra/superpowers) (MIT).

Skill bodies are **not vendored** in git. After clone, run:

```powershell
.\tools\install-superpowers.ps1
```

That creates directory junctions here (and under `%USERPROFILE%\.cursor\skills`, `.grok\skills`, `.agents\skills`) pointing at the Claude plugin cache.

## Skills (invoke before coding)

| Skill | Use when |
|-------|----------|
| `using-superpowers` | Start of session — how to load skills |
| `brainstorming` | New feature or ambiguous request |
| `using-git-worktrees` | Isolated branch before implementation |
| `writing-plans` | Approved design → bite-sized tasks |
| `subagent-driven-development` | Execute plan with per-task subagents |
| `executing-plans` | Batch execution with human checkpoints |
| `test-driven-development` | All implementation (RED → GREEN) |
| `systematic-debugging` | Bugs and test failures |
| `verification-before-completion` | Before claiming done |
| `requesting-code-review` | Between tasks |
| `receiving-code-review` | Review feedback |
| `dispatching-parallel-agents` | Independent parallel work |
| `finishing-a-development-branch` | Merge / PR / cleanup |
| `writing-skills` | Author or edit skills |

## With Project Aegis

- Studio skills: `.claude/skills/` (`/dev-story`, GitNexus impact, etc.)
- Local plans/specs: `docs/superpowers/` (project docs, not the plugin)
- Priority: see `docs/engineering/superpowers-setup.md` and `Agents.md`

## Update

```powershell
claude plugin update superpowers@superpowers-marketplace
.\tools\install-superpowers.ps1 -SkillsOnly
```

Pinned version: `production/superpowers-version.txt`.