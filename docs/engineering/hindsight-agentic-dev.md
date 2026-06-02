# Hindsight for agentic development (with GitNexus)

Project Aegis uses two complementary local intelligence layers:

| Layer | Tool | Question it answers |
|-------|------|-------------------|
| Code graph | **GitNexus** (MCP) | What calls this? What breaks if I change it? |
| Session memory | **Hindsight** (HTTP :8888) | What did we already try? What was the tuning outcome? |

## Quick start

1. Start Hindsight — `.claude/skills/hindsight/hindsight-local-setup/SKILL.md`
2. `.\tools\hindsight\Test-HindsightServer.ps1`
3. Read `.claude/skills/hindsight/hindsight-gitnexus/SKILL.md`
4. For large tasks, spawn agent **`hindsight-dev-memory-lead`** or run **`/team-hindsight-dev`**

## Artifacts in this repo

| Path | Content |
|------|---------|
| `.claude/skills/hindsight/*` | Skills (retain, recall, reflect, AAR, dev memory) |
| `.claude/agents/hindsight-*.md` | Local agent definitions |
| `tools/hindsight/` | PowerShell CLI |
| `src/ProjectAegis.Delegation/Hindsight/` | In-simulation retain hook (optional) |

## Bank ID cheat sheet

- **Dev:** `dev-cmano-clone`, `dev-story-{slug}`, `dev-pr-{n}`, `balance-tuning`
- **Sim:** `agent-{personality}-{id}`, `aar-{scenario}-{runId}`, `agent-xp-{id}`

Registered in `AGENTS.md` and `CLAUDE.md` between gitnexus and superpowers sections.
