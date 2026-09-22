# Jev UAT classifier (`tools/jev/`)

Off-tick advisory CLI for TypeSafe **Jev 1.13** (`jev-1.13.0`). Classifies a filtered Unity UAT evidence packet. Never referenced from `ProjectAegis.Sim` or Delegation core.

## Evidence packet shape (C2 / Baltic)

```json
{
  "policy_id": "baltic-patrol-classify",
  "rows": [
    {
      "id": "UAT-TYPE",
      "result": "fail",
      "observation": "OOB labels a few pixels tall in Game view."
    }
  ]
}
```

| Field | Required | Notes |
|-------|----------|-------|
| `policy_id` | yes | Live scene policy id |
| `rows[].id` | yes | Acceptance row id (`UAT-START`, …) |
| `rows[].result` | yes | `pass` / `fail` / `blocked` |
| `rows[].observation` | yes | One-line human observation (no screenshot paths required) |

Do **not** include screenshots, catalogs, lat/lon, secrets, or proprietary CMO `.db3` data.

## Rubric

Pinned questions: [`data/jev/uat-v1.questions.json`](../../data/jev/uat-v1.questions.json) — `failure_class` (Choice), `severity` (Score), `needs_code_change` (Noul). Thresholds documented in that file and [`data/jev/README.md`](../../data/jev/README.md).

## Run CLI

```powershell
# Dry-run: print System One request only
python tools/jev/classify_uat.py --evidence tools/jev/fixtures/sample-evidence.json --dry-request

# Live classify (requires TYPESAFE_API_KEY or OPENROUTER_API_KEY)
$env:TYPESAFE_API_KEY = "…"   # do not commit
python tools/jev/classify_uat.py --evidence path\to\evidence.json --out production/qa/evidence/uat-2026-09-21/jev-c2.json
```

Routing defaults to TypeSafe, then OpenRouter, then Cloudflare, then Vercel. Use
`--provider cloudflare` to select Cloudflare explicitly without falling back.

| Key | Endpoint | Model |
|-----|----------|-------|
| `TYPESAFE_API_KEY` | `https://api.typesafe.ai/v1/systemone` | `jev-1.13.0` |
| `OPENROUTER_API_KEY` | `https://openrouter.ai/api/v1/systemone` (not chat/completions) | `typesafe/jev-1.13` |
| `CLOUDFLARE_API_TOKEN` + `CLOUDFLARE_ACCOUNT_ID` | `https://api.cloudflare.com/client/v4/accounts/{account_id}/ai/run` | `typesafe/jev` |
| `AI_GATEWAY_API_KEY` or `VERCEL_OIDC_TOKEN` | `https://ai-gateway.vercel.sh/typesafe/v1/systemone` | `typesafe-ai/jev` |

### Vercel AI Gateway

Set `AI_GATEWAY_API_KEY` (preferred), or `VERCEL_OIDC_TOKEN` in the environment.
The CLI does not refresh expired OIDC tokens. Explicit selection never falls
back to another provider when credentials are missing or a request fails.

```powershell
python tools/jev/classify_uat.py --provider vercel --evidence tools/jev/fixtures/sample-evidence.json --dry-request
python tools/jev/classify_uat.py --provider vercel --evidence path\to\evidence.json
```

Uses Vercel's [TypeSafe-compatible API](https://vercel.com/docs/ai-gateway/sdks-and-apis/typesafe),
preserving `noul`, Choice/Score answers, confidence, and token usage without an
AI SDK dependency. The `typesafe-ai/jev` model is an alias, not a version pin.
This CLI does not set per-request zero-retention or no-training options.

### Cloudflare

Cloudflare example (set both variables in your environment):

```powershell
python tools/jev/classify_uat.py --provider cloudflare --evidence tools/jev/fixtures/sample-evidence.json --dry-request
python tools/jev/classify_uat.py --provider cloudflare --evidence path\to\evidence.json
```

The [Cloudflare Jev API](https://developers.cloudflare.com/ai/models/typesafe/jev/)
uses `{model, input: {state, questions}}`. The CLI accepts direct Jev answers
and Cloudflare `result` envelopes; unsuccessful envelopes degrade to no advisory.
Cloudflare's `typesafe/jev` alias is not a version pin; the returned model version
is retained in the advisory. No additional Python dependencies are required.

The CLI also loads the first existing dotenv among repo `.env` / `.env.local` / `tools/jev/.env` / parent-of-repo `.env`.

Budget: **800 ms**. On timeout / HTTP error / parse failure → `{ "status": "no_advisory", ... }`. Missing key → `{ "status": "blocked", ... }` exit 2.

Logs to stderr: model, question ids, token usage.

## Fixture tests

```powershell
python -m pytest tools/jev/test_classify_uat.py -v
```

Uses recorded HTTP body in `fixtures/systemone-success.json`; no live network required.
