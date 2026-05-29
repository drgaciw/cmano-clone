---
name: microsoft-learn-dotnet
description: "Route .NET/C# documentation lookups for ProjectAegis.Sim and ProjectAegis.Delegation to Microsoft Learn and Context7. Use when implementing or reviewing pure C# assemblies, tests, TimeProvider, DI, or before writing ADR references for platform decisions."
argument-hint: "[topic: testing | time | di | performance | migration]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Task
model: sonnet
---

# Microsoft Learn — .NET / C# for Project Aegis

## When to use

- Editing anything under `src/ProjectAegis.Sim/` or `src/ProjectAegis.Delegation/` (no `UnityEngine`)
- Writing or reviewing **ADR** / **story** references for platform behavior
- **Not** for `unity/` — use `/setup-engine unity` and `docs/engine-reference/unity/`

## Phase 1: Load pins

Read [docs/engine-reference/dotnet/README.md](../../../docs/engine-reference/dotnet/README.md) for SDK pin, support dates, and the topic → URL table.

## Phase 2: Fetch docs (agents)

1. **Microsoft Learn MCP** (configured in `.cursor/mcp.json` / `.mcp.json`): `microsoft_docs_search`, `microsoft_code_sample_search`, `microsoft_docs_fetch` on `https://learn.microsoft.com/api/mcp`.
2. **Context7** (fallback): `resolve-library-id` → **.NET** → `/websites/learn_microsoft_en-us_dotnet`, then `query-docs`.
3. **CLI fallback** (no MCP): `npx @microsoft/learn-cli search "your query"`.
4. **GitNexus** before code edits: `npx gitnexus impact "<Symbol>" --repo cmano-clone`.

## Phase 3: Apply to task

| Topic arg | Start here |
|-----------|------------|
| `testing` | [Unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices) |
| `time` | [TimeProvider testing](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing) |
| `di` | [Dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) |
| `performance` | [C# performance](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/performance/) |
| `migration` | [.NET releases and support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support) |

Paste the ADR reference block from `dotnet/README.md` when the user approves doc updates to ADRs or stories.

## Hand off

- Implementation → `/c-sharp-engineer` or `c-sharp-engineer` agent
- Tests → `/c-sharp-test-engineer`
- Determinism scan → `/determinism-audit`
