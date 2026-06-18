---
id: S27-06
status: Ready
type: Integration
priority: should-have
graphite_branch: stack/sprint27/adr009-validators-bda
estimate_days: 1
dependencies:
  - S27-05 complete
owner: team-simulation
sprint: 27
req_trace: TR-combat-dom-003; Req 18 BDA
producer_approved: 2026-06-18
---

# Story 027-06 — Order-Log BDA Slice (Producer Approved)

> **Epic:** sprint-27-adr009-bounded  
> **Producer decision:** APPROVED 2026-06-18 — projection-only; no hot-tick world mutation

## Summary

After S27-05 batch sort, emit order-log-only BDA: sorted kill outcomes → `ContactChangeRecord` state `Lost` (or equivalent); `ContactPictureProjection` proves killed contact drops without sim-kernel contact mutation in default config.

## Acceptance Criteria

- [ ] Projection tests: killed contact drops from contact picture
- [ ] **No** change to `KilledTargetRegistry` apply path in default config
- [ ] Flag-gated test fixture only (`combatDomainsEnabled=true`)
- [ ] Baltic ReplayGolden 6/6 unchanged
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Contact picture drop
  - Given: flag-on fixture with sorted kill outcomes
  - When: BDA projection runs
  - Then: contact picture excludes killed contact id
  - Edge cases: miss outcome unchanged; multiple kills stable order

## References

- GDD: `design/gdd/combat-domains-damage.md` § BDA
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`