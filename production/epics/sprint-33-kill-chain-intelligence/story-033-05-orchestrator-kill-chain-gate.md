---
id: S33-05
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint33/orchestrator-kill-chain
estimate_days: 1.5
dependencies:
  - S33-03
owner: team-data
sprint: 33
req_trace: DBI-8.1, DBI-7.2, DBI-3.4
---

# Story 033-05 — Orchestrator + Write-Gate Kill-Chain Gate

> **Epic:** sprint-33-kill-chain-intelligence

## Summary

`DatabaseIntelligenceOrchestrator.Run` includes kill-chain agent step; `ApproveBatch` on platform/weapon/mount batches blocks commit when blocking `KILL_CHAIN_*` errors present.

## Acceptance Criteria

- [x] Orchestrator ordering documented and tested
- [x] Quarantined bindings excluded from sim export
- [x] WriteGate regression PASS

## Verify Commands

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "DatabaseIntelligence|WriteGate|KillChain" -v minimal
```