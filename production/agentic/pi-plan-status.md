# PI TODO Plan — Completion Status

| Phase | Status | Artifact |
|-------|--------|----------|
| 1 Discovery | **Done** | `pi-phase1-discovery.md`, `pi-security-findings.md` |
| 2 Planning | **Done** | `pi-issue-backlog.md` |
| 3 Implementation | **Done** | Round-trip tests, SQL whitelist |
| 4 Verification | **Done** | `pi-verification-2026-06-03.md` |

## Skills review

**Done:** `docs/engineering/pi-skills-recommendations-review.md`

## Agent checklist

| Agent | Discovery | Implementation |
|-------|-------------|----------------|
| A | Test inventory | Regression tests via C round-trips |
| B | Catalog map | `SqliteCatalogReader` whitelist |
| C | JSON inventory | `ScenarioPolicyJsonRoundTripTests`, `CatalogJsonRoundTripTests` |
| D | Security map | `pi-security-findings.md` + SEC-01 fix |
| E | Architecture map | In `pi-phase1-discovery.md` |
| F | Issues | `pi-issue-backlog.md` |
| G | Verify | See verification doc |

## Deferred (explicit)

- PI-006 Unity manual QA (`production/qa/c2-manual-signoff-2026-06-02.md`)  
- Broad architecture refactors (Agent E)

## Completed after merge (2026-06-04)

- **PI-004:** `STRIKE_UNREACHABLE_FUEL` when target is inside combat radius but over fuel budget; `STRIKE_UNREACHABLE` when beyond combat radius.  
- **PI-005:** `FINGERPRINT_SHA256=` pinned in `replay-golden-baltic-comms` and `replay-golden-baltic-engage`; `ReplayGoldenAssertions` helper.