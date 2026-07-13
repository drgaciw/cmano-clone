---
id: S33-08
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint33/kill-chain-cli
estimate_days: 1
dependencies:
  - S33-02, S33-03
owner: team-data
sprint: 33
req_trace: DBI-1.5, DBI-4.5 (read-only tooling)
---

# Story 033-08 — Kill-Chain CLI Verbs

> **Epic:** sprint-33-kill-chain-intelligence

## Summary

CLI verbs `catalog_dependency_graph` and `catalog_kill_chain_report`; deterministic sorted stdout; read-only — no `ApproveBatch`.

## Acceptance Criteria

- [x] Both verbs registered with `--help`
- [x] Empty report golden on clean Baltic re-import
- [x] MissionEditor.Cli tests PASS

## Verify Commands

```bash
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "KillChain|DependencyGraph" -v minimal
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_dependency_graph --help
```