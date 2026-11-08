---
id: S35-12
status: Complete
type: Logic
priority: should-have
graphite_branch: stack/sprint35/validation-polish
estimate_days: 1
dependencies:
  - S35-01 green baseline
owner: team-data
sprint: 35
req_trace: Req 06 validation; LinkCatalog rules (S34-05)
governing_adrs: ADR-008; extend-only CatalogWriteGate
---

# Story 035-12 — Platform Editor C–H Validation Polish

> **Epic:** sprint-35-polish-foundation

## Summary

Improve validation diagnostics on Platform Editor C–H import path: clearer messages for `LINK_*` and related codes; edge cases from S34 staging feedback. **Detect-only** — extend-only `CatalogWriteGate`.

## Acceptance Criteria

- [x] `LinkCatalogRules` / orchestrator messages actionable for C–H round-trip failures
- [x] Deterministic `ValidationReport` ordering unchanged
- [x] `dotnet test --filter "Link|Validation|PlatformWorkbook"` — all PASS
- [x] SchemaVersion **010** frozen; no breaking migration
- [x] Evidence: `production/agentic/sprint-35-validation-polish-2026-06-19.md`

## QA Test Cases

```
Test: Link validation messages stable
  Given: Curated invalid link fixture
  When: Run validation engine
  Then: Expected codes LINK_ORPHAN_COMMS / LINK_TYPE_INVALID / LINK_LATENCY_INVALID with stable text

Test: Write gate extend-only
  Given: CatalogWriteGate diff
  When: Review PR
  Then: No forbidden delete surfaces; extend-only pattern preserved
```

## Test Evidence Path

- `src/ProjectAegis.Data.Tests/Validation/LinkCatalogRulePackTests.cs`
- `production/agentic/sprint-35-validation-polish-YYYY-MM-DD.md`

## Out of Scope

- New validation rule families
- TL Phase 5 / corpora CI