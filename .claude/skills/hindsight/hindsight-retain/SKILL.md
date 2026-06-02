---
name: hindsight-retain
description: "Use when storing decisions, implementation notes, failed attempts, or review outcomes into local Hindsight memory. Examples: \"remember this fix\", \"retain what we learned\", \"save tuning result\""
---

# Hindsight Retain

Store **short, structured** text into a memory bank. Hindsight extracts facts asynchronously when `async: true`.

## When to retain

- After a successful fix or feature slice (with tests run)
- After a **failed** attempt (prefix `FAILED:` so recall can avoid repetition)
- After architecture or ADR decisions (link `docs/architecture/adr-*.md`)
- After balance/tuning runs (bank `balance-tuning`)
- End of PR review when resolution is non-obvious

## How (PowerShell — preferred in this repo)

```powershell
.\tools\hindsight\Invoke-Hindsight.ps1 `
  -Operation retain `
  -BankId dev-cmano-clone `
  -Content "[OUTCOME: success] Symbols: DelegationOrchestrator.FinalizeScenario. Fixed Hindsight hook invocation order."
```

## How (Python)

```python
from hindsight_client import Hindsight
client = Hindsight(base_url="http://localhost:8888")
client.retain(
    bank_id="dev-cmano-clone",
    content="[OUTCOME: success] Added Hindsight skills under .claude/skills/hindsight/",
    context="agentic-dev",
)
```

## HTTP

```http
POST http://localhost:8888/v1/default/banks/{bank_id}/memories/retain
Content-Type: application/json

{
  "items": [{ "content": "...", "context": "agentic-dev" }],
  "async": true
}
```

## Content rules

- **Do** include symbol names, story slugs, test commands, outcome.
- **Do** reference GitNexus process names when known.
- **Do not** paste credentials, `.env` values, or entire files.
- Keep under ~2 KB per retain; batch related facts in one retain call.

## Banks (quick reference)

| Bank | Use |
|------|-----|
| `dev-cmano-clone` | General coding agent memory |
| `dev-story-{slug}` | Story-scoped |
| `dev-pr-{n}` | PR-scoped |
| `balance-tuning` | Trait / attention experiments |

See `hindsight-guide` for simulation banks (`agent-*`, `aar-*`, `agent-xp-*`).

## Checklist

```
- [ ] Server healthy (Test-HindsightServer.ps1)
- [ ] Correct bank ID for scope
- [ ] Content includes OUTCOME and symbol/process identifiers
- [ ] No secrets in content
```
