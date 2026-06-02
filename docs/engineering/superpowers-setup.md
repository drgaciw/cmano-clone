# Superpowers — global agent methodology

[Superpowers](https://github.com/obra/superpowers) (MIT, v5.1.0) is the **cross-project** agentic skills framework: TDD, systematic debugging, brainstorming, plan-driven execution, and subagent workflows.

This repo also has **`docs/superpowers/`** — **project-local** design specs and implementation plans (delegation, DATA P0, phase gates). That folder is **not** the obra plugin; it is Project Aegis documentation that *references* superpowers workflows.

## Skill priority (this repo)

1. **User instructions** (chat, `CLAUDE.md`, `Agents.md`)
2. **GitNexus rules** (`Agents.md` — impact analysis before edits)
3. **Project Aegis studio skills** (`.claude/skills/` — GDD, sprint, milsim teams)
4. **obra Superpowers skills** (global — TDD, debugging, plans, worktrees)
5. Default agent behavior

When implementing engine code, prefer **`/dev-story`** + **`test-driven-development`** together: studio story context + superpowers RED/GREEN discipline.

## Install / refresh (Windows)

```powershell
.\tools\install-superpowers.ps1
```

Claude-only refresh after marketplace update:

```powershell
claude plugin update superpowers@superpowers-marketplace
.\tools\install-superpowers.ps1 -SkillsOnly
```

## What gets installed

| Harness | Location |
|---------|----------|
| Claude Code | Plugin `superpowers@superpowers-marketplace` (user scope) |
| Cursor | Junctions in `%USERPROFILE%\.cursor\skills\` |
| Grok | Junctions in `%USERPROFILE%\.grok\skills\` |
| Codex / other | See [superpowers README](https://github.com/obra/superpowers#installation) |

Optional in Cursor chat: `/add-plugin superpowers` (marketplace plugin + session hooks).

## Core workflow (obra)

1. `brainstorming` — design before code  
2. `using-git-worktrees` — isolated branch  
3. `writing-plans` — bite-sized tasks with file paths  
4. `subagent-driven-development` or `executing-plans` — execute with review  
5. `test-driven-development` — RED → GREEN → REFACTOR  
6. `verification-before-completion` — prove fixes  
7. `finishing-a-development-branch` — merge / PR / cleanup  

## Pinned version

See `production/superpowers-version.txt`.