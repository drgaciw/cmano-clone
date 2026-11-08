---
name: hindsight-local-setup
description: "Use when installing or verifying the local Hindsight server on localhost:8888, Docker, or Python client. Examples: \"start Hindsight\", \"Hindsight not running\", \"install hindsight_client\""
---

# Hindsight Local Setup

## Project-local server (quick start)

Default for this repository: run the local Hindsight-compatible server from
`tools/hindsight/local_server.py`. It stores JSONL memories under
`.hindsight-local/`, uses no Docker, and does not require `OPENAI_API_KEY`.

```bash
tools/hindsight/start-hindsight-server.sh --detach
```

- API: `http://localhost:8888`
- Storage: `.hindsight-local/`

## Docker (deprecated)

The previous Docker quick start passed `HINDSIGHT_API_LLM_API_KEY` /
`OPENAI_API_KEY` into `ghcr.io/vectorize-io/hindsight`. That path is deprecated
for this project. Use the project-local server unless a future decision
explicitly reinstates a containerized Hindsight service.

## Health check (this repo)

```powershell
.\tools\hindsight\Test-HindsightServer.ps1
```

Exit 0 = reachable; non-zero = start the project-local server or check firewall.

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
| Connection refused | Run `tools/hindsight/start-hindsight-server.sh --detach`; confirm port 8888 |
| 401 / auth | Set `HINDSIGHT_API_KEY` / Bearer if server requires it; pass to `HindsightOptions.ApiKey` |
| Empty recall | Bank new or query too vague; retain first |
| Slow retain | Expected — `async: true`; not blocking sim |

## Docs

- [Main methods](https://hindsight.vectorize.io/developer/api/main-methods)
- [Retain](https://hindsight.vectorize.io/developer/api/retain)
