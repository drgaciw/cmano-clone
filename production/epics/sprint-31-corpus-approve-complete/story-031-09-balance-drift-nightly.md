---
id: S31-09
status: Complete
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint31/balance-drift-nightly
estimate_days: 0.5
dependencies:
  - S31-02 complete
owner: team-data
sprint: 31
req_trace: S29-10 carryover; enableBalanceDrift advisory in nightly approve summary
---

# Story 031-09 — Balance Drift Advisory on Nightly Approve

> **Epic:** sprint-31-corpus-approve-complete  
> **ADR:** ADR-011 (write-gate governance), ADR-001 (deterministic default path)

## Summary

Wire **S29-10 `CatalogBalanceDriftPipelineEvaluator`** into the **nightly approve summary** (`nightly-approve-summary.json` or equivalent). Advisory emitted when `enableBalanceDrift=true`; **default off**. No `CatalogWriteGate` bypass; Sim golden unchanged.

## Acceptance Criteria

- [x] Nightly approve summary includes balance drift advisory when `enableBalanceDrift=true`
- [x] `enableBalanceDrift` default **false** — no advisory on default nightly path
- [x] Pipeline tests PASS on curated fixtures
- [x] `ReplayGoldenSuiteTests` — 6/6 unchanged on default path
- [x] No `CatalogWriteGate` bypass
- [x] Evidence: `production/qa/sprint-31-balance-drift-nightly-*.md` (optional)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Advisory on nightly approve summary
  - Given: curated approve batch with drift beyond ±8% threshold and `enableBalanceDrift=true`
  - When: nightly approve completes and summary written
  - Then: balance drift advisory surfaced in summary output; no silent commit
  - Edge cases: exactly at threshold; zero trials; empty diff; default-off path omits advisory

- **AC-2**: Sim default unchanged
  - Given: Baltic fixture (default `enableBalanceDrift=false`)
  - When: replay golden runs
  - Then: 6/6 PASS; no telemetry side effects from nightly approve wiring
  - Edge cases: accidental default flip in Sim layer; write-gate path unaffected

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogImport|Balance" -v minimal
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Telemetry|Balance" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Nightly approve with drift advisory (off-CI / dry-run)
./tools/cmo-nightly-approve.sh --entity sensor --dry-run --enable-balance-drift
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — no bypass |
| `CatalogBalanceDriftPipelineEvaluator` | HIGH |
| `BalanceTelemetryAccumulator` | MEDIUM |
| `CmoMarkdownImporter` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S29-10 pattern: `production/epics/sprint-29-corpus-approve/story-029-10-balance-drift-pipeline.md`
- S28-10 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-10-balance-drift-consumer.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-09)
- Track plan: `production/agentic/sprint-31-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`