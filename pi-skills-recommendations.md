# Skill Recommendations for `cmano-clone`

## Purpose

This document summarizes recommended agent skills for `cmano-clone`, organized by workflow stage and optimized for parallel agentic development.

The repo shows strong signals around:

- .NET 8 / C#
- xUnit-style testing
- SQLite persistence via `Microsoft.Data.Sqlite`
- `ProjectAegis.Data.Catalog`
- JSON serialization via `System.Text.Json`
- security-sensitive data handling
- architecture/refactor planning

---

## Mandatory Repo Rules

Before any agent edits code:

- Run GitNexus impact analysis for the target symbol:
  - `gitnexus_impact({ target: "<symbol>", direction: "upstream" })`
- Report blast radius:
  - direct callers
  - affected flows/processes
  - risk level
- If risk is `HIGH` or `CRITICAL`, warn before editing.
- Before commit or final handoff:
  - run `gitnexus_detect_changes()`
- Do not rename symbols manually.
  - Use `gitnexus_rename`.

---

## Recommended Installed Skills

These are already available and should be preferred where applicable.

| Skill | Best Use |
|---|---|
| `c-sharp-engineer` | General C#/.NET implementation |
| `test-helpers` | Unit/integration test work |
| `military-database-research` | Catalog/reference/database investigation |
| `design-system` | UI/design tasks if any appear |

### Project Aegis (milsim)

| Skill | Best Use |
|-------|----------|
| `team-simulation` | Tick, engage, logistics, policy, replay |
| `replay-verify` / `determinism-audit` | Order log, RNG, golden baselines |
| `gitnexus-impact-analysis` | Before symbol edits |
| `hindsight-gitnexus` | Multi-session dev |
| `c-sharp-test-engineer` | NUnit (Delegation) + xUnit (Sim/Data) |

See `docs/engineering/pi-skills-recommendations-review.md`.

---

## Recommended External Skills

### Highest Priority

| Priority | Skill | Use When |
|---:|---|---|
| P0 | `kevintsengtw/dotnet-testing-agent-skills@dotnet-testing-xunit-project-setup` | Working on tests, solution structure, test project setup |
| P0 | `johnrogers/claude-swift-engineering@sqlite-data` | Working on SQLite-backed data/catalog flows |
| P0 | `smithery.ai@security` | Reviewing security, validation, secrets, crypto, injection risk |
| P1 | `aws/agent-toolkit-for-aws@exploring-data-catalog` | Exploring catalog/data flows |
| P1 | `melodic-software/claude-code-plugins@data-architecture` | Architecture and data boundary planning |
| P1 | `kdoronin/claude_code_skills@text-to-sql` | Query/data transformation tasks |
| P2 | `github/awesome-copilot@microsoft-docs` | Microsoft API/docs-backed implementation |
| P2 | `flutter/skills@flutter-implement-json-serialization` | JSON serialization review, despite Flutter origin |

---

## Stage-Based Recommendations

### Implement

Use for feature or bugfix implementation.

Recommended order:

1. `c-sharp-engineer`
2. `test-helpers`
3. `kevintsengtw/dotnet-testing-agent-skills@dotnet-testing-xunit-project-setup`
4. `johnrogers/claude-swift-engineering@sqlite-data`
5. `smithery.ai@security`

Before editing:

- identify symbol
- run GitNexus impact
- add/update tests first when behavior changes

---

### Review

Use for PR or branch review.

Recommended order:

1. `test-helpers`
2. `smithery.ai@security`
3. `johnrogers/claude-swift-engineering@sqlite-data`
4. `melodic-software/claude-code-plugins@data-architecture`

Review checklist:

- Does the change affect catalog/data flows?
- Are tests sufficient?
- Are SQLite queries safe?
- Are JSON contracts stable?
- Are sensitive values logged?
- Did GitNexus detect unexpected affected flows?

---

### Architecture

Use for structural analysis and refactor planning.

Recommended order:

1. `melodic-software/claude-code-plugins@data-architecture`
2. `johnrogers/claude-swift-engineering@sqlite-data`
3. `c-sharp-engineer`
4. `smithery.ai@security`

Architecture agents should produce:

- affected modules
- dependency direction notes
- high-coupling symbols
- safe refactor seams
- suggested issue breakdown

No architecture agent should directly refactor without a separate implementation task.

---

### Investigate

Use for debugging or understanding unknown behavior.

Recommended order:

1. GitNexus query/context tools
2. `military-database-research`
3. `johnrogers/claude-swift-engineering@sqlite-data`
4. `test-helpers`
5. `smithery.ai@security`

Suggested investigation flow:

1. Query GitNexus for the concept.
2. Inspect execution flows.
3. Use `gitnexus_context` on key symbols.
4. Form hypothesis.
5. Add focused regression test.
6. Fix only after test reproduces issue.

---

### Continue

Use when resuming previous work.

Recommended order:

1. Hindsight recall, if server is available
2. GitNexus context
3. `c-sharp-engineer`
4. `test-helpers`

Continue checklist:

- What was last changed?
- What symbols were affected?
- What tests already exist?
- What assumptions were recorded?
- Are there stale findings?

---

### Create GitHub Issue / Issues

Use for turning findings into tracked work.

Recommended order:

1. `melodic-software/claude-code-plugins@data-architecture`
2. `test-helpers`
3. `smithery.ai@security`
4. `johnrogers/claude-swift-engineering@sqlite-data`

Each issue should include:

- problem statement
- affected area
- affected symbols, if known
- GitNexus impact/risk
- acceptance criteria
- validation command
- dependencies
- parallelization notes

---

## Default Decision Tree

### If the task touches tests

Use:

- `test-helpers`
- `dotnet-testing-xunit-project-setup`

### If the task touches SQLite/catalog

Use:

- `military-database-research`
- `sqlite-data`
- GitNexus context/impact

### If the task touches JSON

Use:

- `c-sharp-engineer`
- `flutter-implement-json-serialization`
- `text-to-sql` if transformation-heavy

### If the task touches security

Use:

- `smithery.ai@security`

### If the task is broad or structural

Use:

- GitNexus query/context
- `data-architecture`

### If the task touches sim / replay / fuel / engage

Use:

- `team-simulation`
- `replay-verify`
- `determinism-audit`

### If the task touches mission validation / strike reachability

Use:

- `ProjectAegis.Data.Validation` + `ScenarioValidationEngineTests`
- Codes: `STRIKE_UNREACHABLE`, `STRIKE_UNREACHABLE_FUEL` (logistics GDD AC-4)
- `team-data` if catalog platform rows change

### If the task touches C2 / map (Unity)

Use:

- `team-ui` / `team-unity`
- Headless proxy first: `tools/unity/Invoke-ManualQaHeadlessGate.ps1`
- Manual gate: `production/qa/c2-manual-signoff-2026-06-02.md`

---

## Verification Commands

Preferred verification after code changes:

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```

Before commit/final handoff:

```text
gitnexus_detect_changes()
```

---

## Bottom Line

Default skill stack for most repo work:

1. `c-sharp-engineer`
2. `test-helpers` + `c-sharp-test-engineer` (NUnit Delegation, xUnit Sim/Data)
3. GitNexus impact/context
4. `sqlite-data` for catalog/database work
5. `smithery.ai@security` for trust-boundary work

**Gameplay/sim PRs:** add `team-simulation` + `replay-verify` before merge.

**Current CI baseline (2026-06-04):** `dotnet test ProjectAegis.sln` → **283** tests; PlayMode smoke **7** tests.

**PI plan:** complete — see `production/agentic/pi-plan-completion-2026-06-04.md`.
