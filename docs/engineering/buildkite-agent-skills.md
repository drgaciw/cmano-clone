# Buildkite agent skills (local install)

Official [Buildkite skills](https://github.com/buildkite/skills) are vendored
into this repo so Claude Code and Cursor agents can generate correct pipeline
YAML, run `bk` / `buildkite-agent` commands, and operate the Buildkite API.

## Installed skills

| Skill | Path | Agent owner |
|-------|------|-------------|
| Pipelines | `.claude/skills/buildkite-pipelines/` | `buildkite-pipelines-engineer` |
| Migration | `.claude/skills/buildkite-migration/` | `buildkite-migration-engineer` |
| Preflight | `.claude/skills/buildkite-preflight/` | `buildkite-operations-engineer` |
| CLI | `.claude/skills/buildkite-cli/` | `buildkite-operations-engineer` |
| API | `.claude/skills/buildkite-api/` | `buildkite-operations-engineer` |
| Agent runtime | `.claude/skills/buildkite-agent-runtime/` | `buildkite-pipelines-engineer` |

Cursor copies live under `.cursor/skills/buildkite-*/` (same content).

## Agents

| Agent | When to use |
|-------|-------------|
| `buildkite-ci-lead` | Default for Buildkite CI work; routes specialists |
| `buildkite-pipelines-engineer` | `.buildkite/pipeline.yml`, plugins, step design |
| `buildkite-migration-engineer` | CI migration from GitHub Actions or other providers |
| `buildkite-operations-engineer` | `bk`, API, preflight, build triage |

Team manifest: [`.claude/teams/buildkite-team.yaml`](../../.claude/teams/buildkite-team.yaml)

Pipeline context: [`buildkite-ci.md`](./buildkite-ci.md)

## MCP

Buildkite MCP is configured in [`.cursor/mcp.json`](../../.cursor/mcp.json):

```json
"buildkite": { "url": "https://mcp.buildkite.com/mcp" }
```

Authenticate per [Buildkite MCP docs](https://buildkite.com/docs/apis/mcp-server).

## Refresh / upgrade

```bash
bash tools/buildkite/install-buildkite-skills.sh
```

Pin a ref:

```bash
BUILDKITE_SKILLS_REF=v1.0.0 bash tools/buildkite/install-buildkite-skills.sh
```

## License

Skills are MIT — see upstream [LICENSE](https://github.com/buildkite/skills/blob/main/LICENSE).
