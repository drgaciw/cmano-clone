# S33-08 story-done — Kill-Chain CLI Verbs

**Story:** `production/epics/sprint-33-kill-chain-intelligence/story-033-08-kill-chain-cli.md`  
**Status:** Complete  
**Completed:** 2026-06-19

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Both verbs registered with `--help` | `Program.cs` cases; `catalog_dependency_graph --help` smoke | **PASS** |
| Empty report golden on clean Baltic | `CatalogKillChainReportCommandTests` clean fixture golden | **PASS** |
| MissionEditor.Cli tests PASS | Filter `KillChain\|DependencyGraph` 4/4 | **PASS** |
| Read-only — no `ApproveBatch` | Commands use `ICatalogReader` only | **PASS** |

## Architecture

- **`catalog_dependency_graph`:** Emits deterministic sorted platform→mount→weapon and platform→sensor edges from `GetSortedDependencyEdges()`.
- **`catalog_kill_chain_report`:** Runs `KillChainRules.Evaluate()` and prints sorted `KILL_CHAIN_*` findings; empty stdout golden on clean Baltic.

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "KillChain|DependencyGraph" -v minimal
# Passed: 4/4

dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_dependency_graph --help
# Usage banner OK

dotnet test ProjectAegis.sln -v minimal
# Passed: 1138/1138
```

## Files changed

| File | Change |
|------|--------|
| `src/ProjectAegis.MissionEditor.Cli/CatalogDependencyGraphCommand.cs` | **NEW** — read-only graph stdout |
| `src/ProjectAegis.MissionEditor.Cli/CatalogKillChainReportCommand.cs` | **NEW** — read-only kill-chain report |
| `src/ProjectAegis.MissionEditor.Cli/Program.cs` | Register verbs + `--help` |
| `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogDependencyGraphCommandTests.cs` | **NEW** — 2 tests |
| `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogKillChainReportCommandTests.cs` | **NEW** — 2 tests |