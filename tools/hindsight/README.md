# Hindsight local tooling

| Script | Purpose |
|--------|---------|
| `Test-HindsightServer.ps1` | Health check (exit 0/1) |
| `Invoke-Hindsight.ps1` | `retain` / `recall` / `reflect` against `http://localhost:8888` |
| `test-hindsight-server.sh` | Bash health check for Linux/macOS shells |
| `invoke-hindsight.sh` | Bash `retain` / `recall` / `reflect` client using `curl` + `jq` |
| `start-hindsight-server.sh` | Bash launcher for the project-local Hindsight-compatible server |
| `local_server.py` | Local stdlib HTTP server implementing the repo's retain/recall/reflect API subset |

Skills: `.claude/skills/hindsight/hindsight-guide/SKILL.md`  
Agents: `.claude/agents/hindsight-*.md`

Default dev bank: **`dev-cmano-clone`**

## Linux/macOS shell usage

```bash
tools/hindsight/test-hindsight-server.sh
tools/hindsight/invoke-hindsight.sh --operation recall --bank-id dev-cmano-clone --query "Hindsight integration"
tools/hindsight/invoke-hindsight.sh --operation retain --bank-id dev-cmano-clone --content "[OUTCOME: success] Hindsight shell wrapper verified."
```

Set `HINDSIGHT_API_KEY` or pass `--api-key` if the local server requires Bearer auth.

## Start local server on Linux/macOS

This is the default supported path for this repo. It does not use Docker and
does not require `OPENAI_API_KEY`.

```bash
tools/hindsight/start-hindsight-server.sh --detach
tools/hindsight/test-hindsight-server.sh
```

API: `http://localhost:8888`  
Storage: `.hindsight-local/` (ignored by git)

## Docker config deprecated

The old Docker path passed `HINDSIGHT_API_LLM_API_KEY` / `OPENAI_API_KEY` into
`ghcr.io/vectorize-io/hindsight`. That path is deprecated for this project. Use
the project-local server above unless a future decision explicitly reinstates a
containerized Hindsight service.
