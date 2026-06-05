---
name: hindsight-gitnexus
description: "Use when implementing or refactoring code in cmano-clone and you need both structural safety (GitNexus) and session continuity (Hindsight). Examples: \"resume work on X\", \"what did we try before\", \"safe change with memory\""
---

# GitNexus + Hindsight (agentic development)

**GitNexus** = what the code *is* and *what breaks*. **Hindsight** = what we *tried*, *decided*, and *learned* across chats and sessions.

## Standard loop

```
┌─────────────────────────────────────────────────────────────┐
│ 1. ORIENT (GitNexus)                                        │
│    READ gitnexus://repo/cmano-clone/context                 │
│    gitnexus_query({ query: "<feature or symbol area>" })     │
│    gitnexus_context({ name: "<SymbolUnderEdit>" })  (if known)│
├─────────────────────────────────────────────────────────────┤
│ 2. REMEMBER (Hindsight)                                     │
│    recall bank dev-cmano-clone OR dev-story-{slug}           │
│    query: "What did we change or try for <topic>?"          │
├─────────────────────────────────────────────────────────────┤
│ 3. IMPACT (GitNexus) — before any edit                      │
│    gitnexus_impact({ target: "<Symbol>", direction: "upstream" })│
│    Report risk; stop on HIGH/CRITICAL unless user accepts   │
├─────────────────────────────────────────────────────────────┤
│ 4. IMPLEMENT (user-approved paths only)                     │
├─────────────────────────────────────────────────────────────┤
│ 5. RECORD (Hindsight)                                       │
│    retain summary: outcome, symbols touched, tests run      │
│    tag FAILED: if attempt did not work                      │
├─────────────────────────────────────────────────────────────┤
│ 6. VERIFY (GitNexus) — before commit                        │
│    gitnexus_detect_changes()                                  │
│    dotnet test / project verification per AGENTS.md          │
└─────────────────────────────────────────────────────────────┘
```

> Stale GitNexus index → `npx gitnexus analyze` first. Hindsight down → continue with GitNexus only; note memory gap to user.

## Retain template (dev bank)

After a meaningful chunk of work, retain to `dev-cmano-clone` (or `dev-story-{slug}`):

```text
[OUTCOME: success|failed|partial] [AREA: Delegation|Sim|Data|Unity|CI]
Symbols: HindsightOrderLogHook, DecisionLog.Append (gitnexus_context names)
Change: <1-2 sentences>
Tests: dotnet test … → pass/fail
GitNexus risk was: LOW|MEDIUM|HIGH
Next: <optional follow-up>
```

Use `.\tools\hindsight\Invoke-Hindsight.ps1 -Operation retain -BankId dev-cmano-clone -Content "..."`.

## Recall queries (examples)

| Situation | Query |
|-----------|--------|
| Resuming a story | `What was implemented and what is left for story {slug}?` |
| Debugging regression | `What recent changes touched {symbol or process name}?` |
| Tuning / balance | `What aggression or attention values caused ROE violations?` (bank `balance-tuning`) |
| Refactor safety | `Did we already attempt to extract or rename {symbol}?` |

## When to use which bank

| Scope | Bank |
|-------|------|
| Default repo-wide dev | `dev-cmano-clone` |
| Single production story | `dev-story-{story-slug}` |
| PR review cycle | `dev-pr-{number}` |
| Gameplay trait experiments | `balance-tuning` |
| In-sim agent decisions (runtime) | `agent-{personality}-{id}` (C# hook, optional) |

## Spawn local agents

| Phase | Agent |
|-------|--------|
| Multi-step implementation | `hindsight-dev-memory-lead` |
| After playtest / replay run | `hindsight-aar-analyst` |
| Trait / attention tuning | `balance-tuning-memory-agent` |

## Checklist

```
- [ ] gitnexus://repo/cmano-clone/context read; index fresh
- [ ] hindsight recall on relevant dev bank(s)
- [ ] gitnexus_impact on symbols to edit (upstream)
- [ ] User approved file paths (collaborative protocol)
- [ ] Implementation + tests
- [ ] hindsight retain with OUTCOME and symbol names
- [ ] gitnexus_detect_changes before commit
```
