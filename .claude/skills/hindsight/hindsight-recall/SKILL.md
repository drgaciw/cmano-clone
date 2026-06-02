---
name: hindsight-recall
description: "Use when searching prior session memory before implementing, debugging, or resuming work. Examples: \"what did we try before\", \"recall past decisions\", \"have we changed this symbol already\""
---

# Hindsight Recall

Search stored memories by natural language. Use **before** large edits or when resuming a thread.

## Pair with GitNexus

| Question type | Tool |
|---------------|------|
| What calls this / blast radius? | `gitnexus_impact`, `gitnexus_context` |
| What did **we** try last week? | `hindsight recall` |
| How does the flow work? | `gitnexus_query` + process resource |

Always run both when resuming non-trivial work.

## How (PowerShell)

```powershell
.\tools\hindsight\Invoke-Hindsight.ps1 `
  -Operation recall `
  -BankId dev-cmano-clone `
  -Query "What changed for HindsightOrderLogHook and DecisionLog?"
```

## How (Python)

```python
from hindsight_client import Hindsight
client = Hindsight(base_url="http://localhost:8888")
results = client.recall(bank_id="dev-cmano-clone", query="Failed attempts on DelegationOrchestrator")
for r in results.results:
    print(r.text)
```

## HTTP

```http
POST http://localhost:8888/v1/default/banks/{bank_id}/memories/recall
Content-Type: application/json

{
  "query": "What did we try for Hindsight integration?",
  "budget": "mid",
  "maxTokens": 4096
}
```

## Query tips

- Name **symbols** and **files** explicitly (`DecisionLog.Append`, `src/ProjectAegis.Delegation/Hindsight/`).
- Ask for **failures** separately: `FAILED attempts for …`
- For tuning: bank `balance-tuning`, query `ROE violations aggression trait`
- For sim AAR: bank `aar-{scenario}-{runId}`

## Checklist

```
- [ ] Picked bank matching scope (dev vs story vs aar vs balance-tuning)
- [ ] Query names symbols or story slug
- [ ] Cross-check hits with gitnexus_context on symbols mentioned
- [ ] Do not treat recall as substitute for gitnexus_impact before edits
```
