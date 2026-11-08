---
id: S32-05
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint32/eccm-scenario-factor
estimate_days: 1.5
dependencies:
  - S32-01 green baseline
owner: team-simulation
sprint: 32
req_trace: Req 18; sensor-detection-ew bounded ECCM Phase 2; TR-sensor-ew-002
---

# Story 032-05 — ECCM Scenario Factor (Bounded Phase 2)

> **Epic:** sprint-32-combat-domains-phase6  
> **ADR:** ADR-001 (deterministic sim), ADR-009 (bounded scenario factors)  
> **GDD:** `design/gdd/sensor-detection-ew.md`  
> **QA Classification:** Integration + Logic

## Summary

Add optional **`eccmFactor`** on **`ScenarioDetectionTrial`** + policy JSON. Isolated **`baltic-patrol-jammed`** fixture exercises jammed detection path. Default production Baltic path unchanged; no catalog onboard ECCM flags. ReplayGolden 6/6 on default path.

## Acceptance Criteria

- [x] Optional `eccmFactor` field on `ScenarioDetectionTrial` with policy JSON binding
- [x] `baltic-patrol-jammed` isolated fixture demonstrates ECCM factor effect on detection trial
- [x] Default path (`combatDomainsEnabled=false` production Baltic) unchanged — ReplayGolden 6/6
- [x] Sim tests PASS (`Combat|Eccm|Domain` filters)
- [x] `/replay-verify` PASS on isolated jammed fixture
- [x] No catalog onboard ECCM flags; no hot-path SQLite
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: ECCM factor on detection trial
  - Given: isolated `baltic-patrol-jammed` fixture with `eccmFactor` set on `ScenarioDetectionTrial`
  - When: detection trial evaluation runs across N ticks
  - Then: detection outcomes reflect ECCM factor adjustment deterministically
  - Edge cases: `eccmFactor=1.0` (neutral); missing factor defaults; boundary factor values

- **AC-2**: Production Baltic regression
  - Given: default production fixtures without jammed policy
  - When: ReplayGolden suite runs
  - Then: 6/6 PASS; production Baltic world hash unchanged vs S31 closeout
  - Edge cases: accidental ECCM apply on flag-off path; jammed fixture not in golden catalog

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Eccm|Domain" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `ScenarioDetectionTrial` | HIGH |
| `ScenarioValidationEngine` | HIGH — READ |
| `DomainValidatorRegistry` | READ |
| `DelegationBridge.cs` | ZERO touch |

## References

- GDD: `design/gdd/sensor-detection-ew.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-05)
- Track plan: `production/agentic/sprint-32-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*