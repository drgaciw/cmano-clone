# ProjectAegis.Ai.LiveEval (DRG-745 / CMA-16)

On-demand AAR evaluation lane. **Not** a member of `ProjectAegis.sln` and **not** built or run by CI.

## Modes

| Mode | When | Spend |
| --- | --- | --- |
| **Fake** (default) | `LiveEvalMode.Fake` / no env allow | **$0** — `AarScriptedChatClient` / host pipeline only |
| **Paid provider** | Explicit owner allow + credentials | **Blocked until owner budget** — see paid-call gate below |

## Run (fake, local)

```bash
dotnet test src/ProjectAegis.Ai.LiveEval/ProjectAegis.Ai.LiveEval.csproj -v minimal
```

## Paid-call gate (STOP — owner decision required)

This project does **not** call paid providers in the default path. Before any live spend, owner must approve:

| Item | Needed from owner |
| --- | --- |
| Provider | e.g. OpenAI / Azure OpenAI / Anthropic (choose one) |
| Model | chat model id for AAR drafts |
| Judge model | only if enabling `Microsoft.Extensions.AI.Evaluation` quality evaluators |
| Budget | max USD per report and per fixture-set run |
| Credentials | env or user-secrets only — never commit; never print |

Env allow switch (intentionally inert until adapter lands): `AEGIS_LIVE_EVAL_ALLOW_PAID=1` **and** provider credentials. Without both, `PaidProviderGate` refuses.

Packages deferred until approval: provider SDK + `Microsoft.Extensions.AI.Evaluation` / `.Evaluation.Reporting` (judge model = additional spend).

## Invariants

- Repository-public fixtures only; Track A–tagged sources refused
- No secrets in logs or artifacts
- No merge of paid adapters without reporting spend needs first
