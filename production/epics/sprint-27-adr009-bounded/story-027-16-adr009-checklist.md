---
id: S27-16
status: Ready
type: Config
priority: nice-to-have
graphite_branch: stack/sprint27/adr009-validators-bda
estimate_days: 0.25
dependencies:
  - S27-05 complete
owner: c-sharp-architect
sprint: 27
req_trace: ADR-009 validation criteria
---

# Story 027-16 — ADR-009 Validation Checklist Closeout

> **Epic:** sprint-27-adr009-bounded

## Summary

Close ADR-009 unchecked validation boxes; add `combat-domains-smoke` test-only policy JSON with `combatDomainsEnabled=true` and separate stored hash (does not replace ReplayGolden 6/6).

## Acceptance Criteria

- [ ] ADR-009 criteria 1–4 marked satisfied with file:line evidence
- [ ] Flag-on smoke fixture documented
- [ ] Separate hash pin (not Baltic golden)

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`