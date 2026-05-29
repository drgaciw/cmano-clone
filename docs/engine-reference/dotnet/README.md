# .NET — Engine Reference

| Field | Value |
|-------|-------|
| **Runtime** | .NET 8 (LTS) |
| **Target framework** | `net8.0` |
| **SDK (pinned in `global.json`)** | **8.0.400** (`rollForward`: `latestFeature`) |
| **Test frameworks** | **NUnit 4.2.2** (`ProjectAegis.Delegation*Tests`); **xUnit 2.9.2** (`ProjectAegis.Sim.Tests`) |
| **Test SDK** | Microsoft.NET.Test.Sdk **17.11.1** |
| **Project scope** | `ProjectAegis.Sim`, `ProjectAegis.Delegation` — pure C# (no `UnityEngine`) |
| **LLM knowledge cutoff** | May 2025 (use this doc + Microsoft Learn for .NET 8 APIs) |
| **Last verified** | 2026-05-29 |

## Support timeline (May 2026)

| Release | Kind | End of support |
|---------|------|----------------|
| **.NET 8** | LTS (**current pin**) | **November 10, 2026** |
| **.NET 10** | LTS (planned successor) | **November 14, 2028** |

Official policy: [.NET releases and support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support).

## SDK verification

| Check | Command |
|-------|---------|
| Active SDK | `dotnet --version` → expect `8.0.4xx` per `global.json` |
| Installed SDKs | `dotnet --list-sdks` |
| Build + test gate | `dotnet test ProjectAegis.sln --configuration Release` |

---

## Topic → Microsoft Learn

Use these for **implementation and ADR references** on `src/ProjectAegis.*` (not Unity).

| Topic | Official URL |
|-------|--------------|
| Unit testing best practices | https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices |
| TimeProvider / FakeTimeProvider testing | https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing |
| Dependency injection | https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection |
| C# records / `readonly record struct` | https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records |
| `Span` / `Memory` | https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/ |
| .NET releases and support | https://learn.microsoft.com/en-us/dotnet/core/releases-and-support |
| Choosing class vs struct | https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/choosing-between-class-and-struct |
| C# performance | https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/performance/ |
| SDK install in CI | https://learn.microsoft.com/en-us/dotnet/devops/dotnet-cli-and-continuous-integration |

### Contributor learning paths (optional)

- [Build .NET applications with C#](https://learn.microsoft.com/en-us/training/paths/build-dotnet-applications-csharp/)
- [Test a .NET class library with dotnet test](https://learn.microsoft.com/en-us/training/modules/unit-test-csharp/)

---

## Agent & MCP routing

| Path | Documentation source |
|------|----------------------|
| `src/ProjectAegis.*` (no `UnityEngine`) | **Microsoft Learn** (table above) |
| `unity/` | **Unity** — [../unity/VERSION.md](../unity/VERSION.md) |

| Tool | When to use |
|------|-------------|
| **GitNexus** | Impact analysis and execution flows before editing symbols (`npx gitnexus impact "Symbol" --repo cmano-clone`) |
| **Context7** | Cached .NET snippets — library ID `/websites/learn_microsoft_en-us_dotnet` |
| **[Microsoft Learn MCP](https://github.com/microsoftdocs/mcp)** | **Configured** in `.cursor/mcp.json` and `.mcp.json` as `microsoft-learn` → `https://learn.microsoft.com/api/mcp`. Tools: `microsoft_docs_search`, `microsoft_code_sample_search`, `microsoft_docs_fetch`. Cursor rule: `.cursor/rules/microsoft-learn-dotnet.mdc`. |
| **Unity docs / Context7 Entities** | DOTS, Editor, rendering only |

**Do not** use Unity Entities docs for pure `ProjectAegis.Sim` policy/RNG/tick code.

---

## ADR / story reference block

When an ADR or story touches sim, delegation, tests, or time/RNG, add under **References**:

```markdown
## Platform references (.NET)

- [Unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [TimeProvider testing](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing) — if sim/delegation uses time
- Engine reference: [docs/engine-reference/dotnet/README.md](../../../docs/engine-reference/dotnet/README.md)
```

---

## Project-specific reminders

- **Determinism:** No `DateTime.Now` / `Random.Shared` on sim or controller paths; prefer `TimeProvider` in new code ([Learn: TimeProvider testing](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing)). Run `/determinism-audit` before sim merges.
- **Seeded RNG:** `ProjectAegis.Sim.Core.SeededRng` — domain-scoped draws; do not mix with unseeded APIs.
- **Assembly boundary:** Sim evaluates policy; Delegation emits orders ([ADR-001](../../architecture/adr-001-sim-assembly-boundary.md)).

---

## References

- [Tech-Stack.md](../../../Tech-Stack.md)
- [Unity VERSION.md](../unity/VERSION.md)
- [Master architecture](../../architecture/architecture.md)
