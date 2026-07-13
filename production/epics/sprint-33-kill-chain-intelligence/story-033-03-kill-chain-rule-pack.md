---
id: S33-03
status: Complete
last_updated: 2026-06-19
completed: 2026-06-19
type: Logic
priority: must-have
graphite_branch: stack/sprint33/kill-chain-rule-pack
estimate_days: 2.5
dependencies:
  - S33-02
owner: team-data
sprint: 33
req_trace: DBI-3.5, DBI-3.1, DBI-3.3, DBI-2.1, DBI-2.2
---

# Story 033-03 — Kill-Chain Impossibility Rule Pack (DBI-3.5)

> **Epic:** sprint-33-kill-chain-intelligence

## Summary

Extend `CatalogRulesValidationAgent` with bounded detect-only rules: R1 orphan edge, R2 range exceeds sensor, R3 speed mismatch, R4 weapon exceeds platform reach. Deterministic sorted findings; golden hash on Baltic.

## Acceptance Criteria

- [x] ≥3 `KILL_CHAIN_*` finding codes on curated fixtures
- [x] Golden hash stable on Baltic patrol fixture
- [x] `DatabaseIntelligenceOrchestrator.RunBalticDefault()` surfaces new codes
- [x] No live-table mutation on report path
- [x] Evidence: `production/agentic/sprint-33-kill-chain-rules-2026-06-19.md`

## Verify Commands

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "KillChain|CatalogRules|Validation" -v minimal
npx gitnexus impact CatalogRulesValidationAgent
```