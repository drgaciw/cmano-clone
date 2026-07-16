# Claude Code Game Studios -- Game Studio Agent Architecture

Indie game development managed through **67** coordinated Claude Code subagents
(on-disk count under `.claude/agents/` as of 2026-07-09).
Each agent owns a specific domain, enforcing separation of concerns and quality.

## Technology Stack

- **Engine**: Unity 6.3 LTS (editor 6000.3.14f1)
- **Language**: C#
- **Version Control**: Git with trunk-based development
- **Build System**: Unity Build Pipeline + `dotnet` for headless assemblies
- **Asset Pipeline**: Unity Asset Import Pipeline + Addressables

> **Note:** This repo uses the **Unity** engine-specialist set only
> (`unity-specialist` + DOTS/UI/Addressables/shader sub-specialists).
> Godot/Unreal agent sets are not active here.

## Project Structure

@.claude/docs/directory-structure.md
<!-- S39-04 hygiene: hybrid (co-located src/*Tests + tests/regression) retained; see appended sign-off in directory-structure.md + polish-scope-boundary-2026-06-19.md -->

## Engine Version Reference

@docs/engine-reference/unity/VERSION.md

@docs/engine-reference/dotnet/README.md

## Technical Preferences

@.claude/docs/technical-preferences.md

## Coordination Rules

@.claude/docs/coordination-rules.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

## Graphite-first PR workflow

Repo is Graphite-initialized. For branch/PR/stack work use **`gt`** (`gt create`, `gt submit --stack --no-interactive`, `gt sync`) — not `gh pr create` or raw `git push` on stack branches. Canonical guide: [`docs/engineering/graphite-github-substitute-plan.md`](docs/engineering/graphite-github-substitute-plan.md).

> **First session?** If the project has no engine configured and no game concept,
> run `/start` to begin the guided onboarding flow.

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

@.claude/docs/context-management.md

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **cmano-clone** (25952 symbols, 49707 relationships, 300 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "main"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method without first running `impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit changes without running `detect_changes()` to check affected scope.

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/cmano-clone/context` | Codebase overview, check index freshness |
| `gitnexus://repo/cmano-clone/clusters` | All functional areas |
| `gitnexus://repo/cmano-clone/processes` | All execution flows |
| `gitnexus://repo/cmano-clone/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->

<!-- hindsight:start -->
## Hindsight — Session Memory (with GitNexus)

Local episodic memory (`http://localhost:8888`) for agentic dev + simulation AAR. **Pair with GitNexus** — see `AGENTS.md` and `.claude/skills/hindsight/hindsight-gitnexus/SKILL.md`.

| Task | Skill |
|------|--------|
| Combined dev loop | `.claude/skills/hindsight/hindsight-gitnexus/SKILL.md` |
| Hub / banks / agents | `.claude/skills/hindsight/hindsight-guide/SKILL.md` |
| Retain / recall / reflect | `.claude/skills/hindsight/hindsight-retain|recall|reflect/SKILL.md` |
| Local Docker / CLI | `.claude/skills/hindsight/hindsight-local-setup/SKILL.md` + `tools/hindsight/` |

**Agents:** `hindsight-dev-memory-lead`, `hindsight-aar-analyst`, `balance-tuning-memory-agent` — **Team:** `/team-hindsight-dev`

<!-- hindsight:end -->
