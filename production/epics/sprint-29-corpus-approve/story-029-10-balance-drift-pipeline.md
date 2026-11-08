---
id: S29-10
status: Complete
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint29/balance-drift-pipeline
estimate_days: 0.5
dependencies:
  - S29-01 green baseline
owner: team-data
sprint: 29
req_trace: S28-10 carryover; enableBalanceDrift advisory in catalog pipeline
---

# Story 029-10 — Balance Drift in Catalog Pipeline

> **Epic:** sprint-29-corpus-approve  
> **ADR:** ADR-011 (write-gate governance), ADR-001 (deterministic default path)

## Summary

Surface `enableBalanceDrift` advisory on import/approve diffs in the catalog pipeline. Pipeline tests PASS; Sim default `enableBalanceDrift=false` unchanged; golden hash pinned.

## Acceptance Criteria

- [x] Import/approve diff path emits balance drift advisory when `enableBalanceDrift=true`
- [x] Pipeline tests PASS on curated fixtures
- [x] `enableBalanceDrift` Sim default **false** (unchanged)
- [x] `ReplayGoldenSuiteTests` — 6/6 unchanged on default path
- [x] No `CatalogWriteGate` bypass
- [x] Evidence: `production/qa/sprint-29-balance-drift-pipeline-2026-06-18.md`
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Advisory on import/approve diff
  - Given: curated fixture with drift beyond ±8% threshold and `enableBalanceDrift=true`
  - When: import propose or approve diff evaluated
  - Then: advisory surfaced in pipeline output; no silent commit
  - Edge cases: exactly at threshold; zero trials; empty diff

- **AC-2**: Sim default unchanged
  - Given: Baltic fixture (default `enableBalanceDrift=false`)
  - When: replay golden runs
  - Then: 6/6 PASS; no telemetry side effects
  - Edge cases: accidental default flip in Sim layer

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogImport|Balance" -v minimal
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Telemetry|Balance" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — no bypass |
| `BalanceTelemetryAccumulator` | MEDIUM |
| `CmoMarkdownImporter` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S28-10 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-10-balance-drift-consumer.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-10)
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*