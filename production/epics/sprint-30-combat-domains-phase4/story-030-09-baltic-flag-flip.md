---
id: S30-09
status: Complete
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint30/baltic-flag-flip
estimate_days: 0.5
dependencies:
  - S30-05 land aspect domain validator
owner: team-simulation
sprint: 30
req_trace: TR-combat-dom-001, TR-combat-dom-002; Req 18; ADR-009 migration step 4
---

# Story 030-09 — Production Baltic `combatDomainsEnabled` Flip (Producer-Gated)

> **Epic:** sprint-30-combat-domains-phase4  
> **ADR:** ADR-009 (Accepted), ADR-003 (order log), ADR-001 (deterministic sim)  
> **Producer gate:** Flag flip requires explicit producer sign-off before merge

## Summary

ADR-009 **migration step 4**: flip **`combatDomainsEnabled=true`** on production **`baltic-patrol.policy.json`** **only if producer approves**. Pin new replay hash; keep `combat-domains-smoke` and other isolated pins unchanged. Extends S29-05 isolated Baltic golden — this story is the **production policy** flip, not the isolated golden pin.

## Acceptance Criteria

- [x] Producer approval documented before merge (`production/qa/` or `production/agentic/` decision record)
- [x] `combatDomainsEnabled=true` on `baltic-patrol.policy.json` **only after producer sign-off**; otherwise story deferred/skipped
- [x] New production Baltic golden world-state hash pinned; `/replay-verify` PASS
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS (updated production pin only; smoke pin hash unchanged)
- [x] `combat-domains-smoke.policy.json` remains on separate pin (unchanged hash)
- [x] `baltic-patrol-combat-domains` isolated pin unchanged (distinct from production flip)
- [x] Domain validators (air/surface/subsurface/land) active on flag-on production path
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Producer gate enforced
  - Given: S30-09 branch ready for merge
  - When: producer decision record reviewed
  - Then: explicit APPROVE before `baltic-patrol.policy.json` flag flip merges; DEFER leaves flag `false`
  - Edge cases: partial flip (policy JSON only without golden pin); merge without approval doc

- **AC-2**: Production golden pins after flip
  - Given: producer-approved `combatDomainsEnabled=true` on `baltic-patrol.policy.json`
  - When: `/replay-verify` runs twice + against stored golden
  - Then: identical world hash; new hash recorded in regression artifact
  - Edge cases: validator deny abort codes in order log; allow path unchanged hash on repeat

- **AC-3**: Isolated pin isolation
  - Given: `combat-domains-smoke.policy.json` and `baltic-patrol-combat-domains` on separate pins
  - When: ReplayGolden 6/6 suite runs
  - Then: smoke + isolated combat hashes unchanged unless intentionally updated with separate evidence
  - Edge cases: accidental conflation of smoke, isolated, and production pins

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Mandatory sim merge gate
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DomainValidatorRegistry` | HIGH |
| `baltic-patrol.policy.json` | HIGH — production policy flip |
| `AirAspectDomainValidator` | READ |
| `SurfaceAspectDomainValidator` | READ |
| `SubsurfaceAspectDomainValidator` | READ |
| `LandAspectDomainValidator` | READ — post S30-05 |
| `DelegationBridge.cs` | ZERO touch |

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- S29-05 pattern: `production/epics/sprint-29-combat-domains-phase3/story-029-05-baltic-combat-enable.md`
- Production policy: `data/scenarios/baltic-patrol.policy.json`
- Smoke fixture: `data/scenarios/combat-domains-smoke.policy.json`
- Isolated golden: `data/scenarios/baltic-patrol-combat-domains.policy.json`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-09)
- Track plan: `production/agentic/sprint-30-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`