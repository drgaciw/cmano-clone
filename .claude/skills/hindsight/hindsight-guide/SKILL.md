---
name: hindsight-guide
description: "Use when the user asks about Hindsight memory, bank IDs, local server setup, or how Hindsight fits with GitNexus in this repo. Examples: \"How do I use Hindsight?\", \"What memory banks exist?\", \"Hindsight + GitNexus workflow\""
---

# Hindsight Guide (Project Aegis)

Hindsight is a **local episodic memory sidecar** (default `http://localhost:8888`). It stores narrative facts across sessions. **GitNexus** maps code structure and blast radius; **Hindsight** remembers decisions, failed attempts, tuning outcomes, and AAR narratives.

## Always Start Here

| Step | Action |
|------|--------|
| 1 | Confirm Hindsight is up: `.\tools\hindsight\Test-HindsightServer.ps1` |
| 2 | Pick the skill for your task (table below) |
| 3 | If editing **C# symbols**, still follow **GitNexus** rules in `AGENTS.md` first |

## Skills

| Task | Skill |
|------|--------|
| Combined dev workflow (GitNexus + Hindsight) | `hindsight-gitnexus` |
| Store session learnings / decisions | `hindsight-retain` |
| Search prior attempts / context | `hindsight-recall` |
| Synthesize AAR or post-mortem narrative | `hindsight-reflect` |
| Simulation run AAR (delegation order log) | `hindsight-aar` |
| Agentic dev memory banks & conventions | `hindsight-dev-memory` |
| Docker install, health, CLI shortcuts | `hindsight-local-setup` |

## Memory bank conventions (this repo)

| Bank ID | Owner | Purpose |
|---------|--------|---------|
| `dev-cmano-clone` | Coding agents | Cross-session implementation memory (attempts, fixes, ADR links) |
| `dev-story-{slug}` | Story implementation | Scoped to one production story file |
| `dev-pr-{number}` | PR / review loops | Review feedback and resolution notes |
| `balance-tuning` | Balance / tuning agent | Trait and parameter experiments + outcomes |
| `agent-{personality}-{id}` | In-game `AgentController` | Auto-retain via `HindsightOrderLogHook` when sim Hindsight enabled |
| `aar-{scenario}-{runId}` | AAR agent | Post-scenario summary (auto at `FinalizeScenario`) |
| `agent-xp-{id}` | Campaign / trust | Trust signals at `FinalizeScenario` |

Slug rules match `HindsightBankIds` in `src/ProjectAegis.Delegation/Hindsight/`.

## API surface (local)

| Operation | HTTP | When |
|-----------|------|------|
| Retain | `POST /v1/default/banks/{bank_id}/memories/retain` | After decisions, fixes, failed attempts |
| Recall | `POST /v1/default/banks/{bank_id}/memories/recall` | Before large edits or when resuming work |
| Reflect | `POST /v1/default/banks/{bank_id}/reflect` | AAR narratives, tuning retrospectives |

Use `.\tools\hindsight\Invoke-Hindsight.ps1` or Python `hindsight_client` (see `hindsight-local-setup`).

## In-simulation integration

C# wiring lives under `src/ProjectAegis.Delegation/Hindsight/`. Enabled only when passing `HindsightOptions` to `DelegationOrchestrator` — **off by default** for deterministic CI/replay.

See `src/ProjectAegis.Delegation/Hindsight/README.md`.

## Agents (local)

| Agent | Use for |
|-------|---------|
| `hindsight-dev-memory-lead` | Pre/post edit loops with GitNexus + dev bank retains |
| `hindsight-aar-analyst` | Post-run reflection on `aar-*` and `agent-*` banks |
| `balance-tuning-memory-agent` | Trait tuning with `balance-tuning` bank |

## Never

- Call **recall/reflect** inside simulation `Tick()` or policy code (breaks determinism).
- Skip **GitNexus impact** because Hindsight has context — they answer different questions.
- Retain secrets, API keys, or full file dumps — summarize with file paths and symbol names only.
