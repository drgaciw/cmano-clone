---
name: hindsight-reflect
description: "Use when generating a reasoned summary or AAR narrative from stored Hindsight memories. Examples: \"reflect on tuning session\", \"summarize agent failures\", \"post-mortem from memory bank\""
---

# Hindsight Reflect

Disposition-aware **synthesis** over memories in a bank. Uses an LLM on the Hindsight server — not for simulation ticks.

## When to use

- After a playtest or replay batch (bank `aar-{scenario}-{runId}`)
- End of sprint / story for `dev-story-{slug}`
- Balance tuning retrospective (`balance-tuning`)
- User asks for narrative AAR or "what went wrong"

## How (PowerShell)

```powershell
.\tools\hindsight\Invoke-Hindsight.ps1 `
  -Operation reflect `
  -BankId aar-baltic-run-001 `
  -Query "Summarize EW specialist failures and attention overload episodes"
```

## How (Python)

```python
from hindsight_client import Hindsight
client = Hindsight(base_url="http://localhost:8888")
answer = client.reflect(
    bank_id="aar-baltic-run-001",
    query="Summarize agent performance and key failure points",
)
print(answer.text)
```

## HTTP

```http
POST http://localhost:8888/v1/default/banks/{bank_id}/reflect
Content-Type: application/json

{
  "query": "Summarize key failure points",
  "budget": "mid",
  "includeFacts": true
}
```

## C# runtime hook

`HindsightOptions.AarReflectQuery` triggers reflect after `FinalizeScenario` retain (tooling only). Do not enable in headless replay CI.

## Checklist

```
- [ ] Bank already has retains (run sim with Hindsight or manual retains)
- [ ] Query is specific (agent id, sim_time band, failure mode)
- [ ] Output cross-checked against DecisionLog / gitnexus process if making code changes
- [ ] Findings retained back to dev-cmano-clone if they drive new work
```
