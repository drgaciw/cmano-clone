# jev-worker

Authenticated forwarder from a Cloudflare Worker to OpenRouter's Jev decision endpoint
(`typesafe/jev-1.14`). It accepts `{ "state": ..., "questions": { ... } }` and returns Jev's
`answers`. The model is fixed server-side; the body is size-capped and shape-validated, so the
Worker cannot be used as an open relay for OpenRouter credits.

## Replaces the deployed demo

A Worker named `jev-worker` already exists in the Cloudflare account. It ignores the request body and
always scores a hardcoded account-risk example. Deploying this directory **overwrites it**.

## Deploy (run in your own shell)

```bash
cd tools/jev-worker
npx wrangler deploy
```

Secrets are Worker secrets, not source. They persist across deploys; confirm they exist with
`npx wrangler secret list`, and set any missing one interactively:

```bash
npx wrangler secret put JEV_WORKER_TOKEN
npx wrangler secret put OPENROUTER_API_KEY
```

Optional: set `OPENROUTER_MODEL` as a plain variable to pick another Jev model.

## Smoke test

Export the URL and token in your shell (never in a file that is committed), then:

```bash
export JEV_WORKER_URL=https://jev-worker.<account-subdomain>.workers.dev
export JEV_WORKER_TOKEN=...   # the worker secret
tools/uat/jev-decide.sh production/qa/uat-dod-smoke-request.json
```

Expect a JSON object with `answers`. `401` means the token is wrong, `400` names the invalid field.

## Behaviour

| Status | Meaning |
|--------|---------|
| 200 | `answers` passed through from Jev |
| 400 | invalid JSON or request shape (message names the field) |
| 401 | missing or wrong bearer token |
| 405 | not POST |
| 413 | body over 32 KB |
| 502 | upstream failure (secrets are redacted from logs) |
| 503 | a required secret is not set |
