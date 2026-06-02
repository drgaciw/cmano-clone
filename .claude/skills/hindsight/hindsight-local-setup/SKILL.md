---
name: hindsight-local-setup
description: "Use when installing or verifying the local Hindsight server on localhost:8888, Docker, or Python client. Examples: \"start Hindsight\", \"Hindsight not running\", \"install hindsight_client\""
---

# Hindsight Local Setup

## Docker (quick start)

Requires an LLM API key for fact extraction on the server:

```powershell
$env:HINDSIGHT_API_LLM_API_KEY = "<your-openai-or-compatible-key>"
docker run --rm -it -p 8888:8888 -p 9999:9999 `
  -e HINDSIGHT_API_LLM_API_KEY=$env:HINDSIGHT_API_LLM_API_KEY `
  -v "${env:USERPROFILE}\.hindsight-docker:/home/hindsight/.pg0" `
  ghcr.io/vectorize-io/hindsight:latest
```

- API: `http://localhost:8888`
- UI: `http://localhost:9999`

## Health check (this repo)

```powershell
.\tools\hindsight\Test-HindsightServer.ps1
```

Exit 0 = reachable; non-zero = start Docker or check firewall.

## Python client (optional)

```powershell
pip install hindsight-client
python -c "from hindsight_client import Hindsight; c=Hindsight('http://localhost:8888'); c.retain('dev-cmano-clone','ping')"
```

## Repo CLI wrapper

```powershell
.\tools\hindsight\Invoke-Hindsight.ps1 -Operation retain -BankId dev-cmano-clone -Content "bootstrap"
.\tools\hindsight\Invoke-Hindsight.ps1 -Operation recall -BankId dev-cmano-clone -Query "bootstrap"
```

## C# simulation hook

See `src/ProjectAegis.Delegation/Hindsight/README.md` — pass `HindsightOptions` into `DelegationOrchestrator`.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Connection refused | Start Docker container; confirm port 8888 |
| 401 / auth | Set `HINDSIGHT_API_KEY` / Bearer if server requires it; pass to `HindsightOptions.ApiKey` |
| Empty recall | Bank new or query too vague; retain first |
| Slow retain | Expected — `async: true`; not blocking sim |

## Docs

- [Main methods](https://hindsight.vectorize.io/developer/api/main-methods)
- [Retain](https://hindsight.vectorize.io/developer/api/retain)
