---
name: hindsight-dev-memory
description: "Use when managing agentic development memory banks (dev-cmano-clone, dev-story-*, dev-pr-*) separate from in-simulation agent banks. Examples: \"dev memory bank\", \"remember across sessions\", \"story context bank\""
---

# Hindsight Dev Memory

Episodic memory for **coding agents** — complements GitNexus code intelligence.

## Bank layout

| Bank ID | When to create / use |
|---------|----------------------|
| `dev-cmano-clone` | Default; all repo-wide implementation memory |
| `dev-story-{slug}` | During `/dev-story` on `production/**/story-*.md` |
| `dev-pr-{number}` | PR review, `/ce-resolve-pr-feedback`, babysit loops |
| `balance-tuning` | Trait vector / attention experiments (shared with game tuning agent) |

Slug = lowercase, spaces → hyphens (same rules as `HindsightBankIds.Slug`).

## Lifecycle

### Session start

```powershell
.\tools\hindsight\Invoke-Hindsight.ps1 -Operation recall -BankId dev-cmano-clone -Query "Open work on <topic> and recent failures"
```

Plus GitNexus `context` + `query` for the same topic.

### Session end

Retain **one** summary per logical chunk:

```text
[OUTCOME: success] [STORY: story-003-c1-orderlog] 
Symbols: OrderLogEntry, IOrderLog
Tests: dotnet test ProjectAegis.Delegation.Tests → 107 passed
Notes: Hindsight hook must stay off default ctor for replay tests
```

### Story complete

Reflect on `dev-story-{slug}`, then retain distilled lesson to `dev-cmano-clone`.

## FAILED attempts (critical)

Always retain failed paths — saves tokens later:

```text
FAILED: Tried wrapping DecisionLog subclass; broke fingerprint tests. Use IHindsightOrderLogHook on Append instead.
```

## Relation to in-sim banks

| Layer | Banks | Tooling |
|-------|-------|---------|
| Coding agents | `dev-*`, `dev-pr-*` | This skill + `hindsight-gitnexus` |
| In-game agents | `agent-*`, `aar-*`, `agent-xp-*` | C# `HindsightIntegration` + `hindsight-aar` |

Do not mix dev retains into `agent-*` banks.

## Lead agent

Use **`hindsight-dev-memory-lead`** for multi-step work with enforced GitNexus + Hindsight loop.

## Checklist

```
- [ ] Bank scope matches task (repo vs story vs PR)
- [ ] Recall at start; retain at end
- [ ] FAILED attempts recorded
- [ ] Symbol names align with gitnexus_impact targets
```
