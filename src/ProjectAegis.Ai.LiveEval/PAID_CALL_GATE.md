# DRG-745 paid-call gate — owner decision required

**Status:** STOP before spend. Scaffold + fake path only shipped in this PR.

## What works now ($0)

- `FakeLiveEvalRunner` → host `AarAssistantPipeline` + `AarScriptedChatClient`
- Track A source refusal
- Isolation: project **not** in `ProjectAegis.sln` / CI
- No provider SDK, no Evaluation judge packages pinned

## What is needed before any paid call

| Decision | Options / notes |
| --- | --- |
| Provider | OpenAI · Azure OpenAI · Anthropic (pick one for first adapter) |
| Chat model | Model id for AAR draft generation |
| Judge model | Only if enabling `Microsoft.Extensions.AI.Evaluation` (extra spend per case) |
| Budget ceiling | Max USD per AAR report and per full fixture-set run |
| Credential store | Env or user-secrets only — never commit; never print values |
| Allow switch | `AEGIS_LIVE_EVAL_ALLOW_PAID=1` after the above |

## Estimated cost (order-of-magnitude — confirm with provider pricing)

Not measured yet. Rough planning envelope for owner (fixture set ~10 cases, two consecutive runs for go/no-go):

- Chat-only (no judge): typically low cents–few dollars depending on model/context
- Chat + Evaluation judge: roughly 2×–3× token volume

**Do not treat this as a quote.** Owner sets the ceiling; agent reports measured cost after the first authorized dry run.

## Agent rules

1. Never print secrets or API keys
2. Never merge without reporting first
3. Never call paid APIs without owner budget acknowledgement on this gate
