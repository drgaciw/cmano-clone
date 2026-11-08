# Story 001 — Hub re-baseline (req 01 + index)

**Epic:** requirements-corpus-maturity  
**Wave:** 0  
**Status:** Complete  
**Type:** Documentation + traceability  
**Design:** docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md §5 Wave 0  
**Plan:** docs/superpowers/plans/2026-07-08-requirements-corpus-maturity-wave0-plan.md

## Context

S12 story-001 completed Template A for doc 01. By 2026-07 the hub is stale: missing doc 21, Related Index statuses frozen, success criteria over-claim vs Release/Baltic gates, no standing invariants, no scenario-editor forward program. This story re-baselines the hub only.

## Acceptance

1. `01-Project-Overview.md` has FR-19 → 21 and Related Index rows for docs **02–21** with resolving relative links.
2. Success criteria are split into gate-backed vs north-star; each original bullet tagged Measured | Proxy | Deferred with evidence path.
3. NFR or Technical Considerations includes standing invariants (test floor ≥1232, ReplayGolden/hash, C2 18/18, DelegationBridge zero-touch, CatalogWriteGate extend-only, v3 isolation).
4. Scope has explicit tiers: shipped vertical slice / active program (req 11) / ambition.
5. Future Extensibility leads with scenario editor program (req 11).
6. FR-09 wording is not “NL-only”; reflects schema/MCP/validation + optional NL as deferred.
7. Master index program note + reading order mention doc 21; Last Updated ≥ 2026-07-08.
8. Tracker req 01 Post-S56 note records re-baseline date without MVP regrade.
9. RTM scope notes doc 21 present in corpus; maturity table has row 21.
10. Design-review memo under `production/qa/` with full-mode hub checks (not lean 4-row only).
11. Verification script/commands in plan all PASS.
12. Collaborative protocol: no secret commits; user-instructed commits only.

## Verify commands

```bash
test -f Game-Requirements/requirements/21-Platform-Editor.md
rg -n 'FR-19' Game-Requirements/requirements/01-Project-Overview.md
rg -n '21-Platform-Editor' Game-Requirements/requirements/01-Project-Overview.md
rg -n '17144800277401907079' Game-Requirements/requirements/01-Project-Overview.md
# Related targets: see Task 10 verification loop
```

## Completion Notes

- **Completed:** 2026-07-08
- **Status:** Complete — all ACs 1–12 PASS (docs-only Wave 0)
- **Per-AC evidence:**
  1. FR-19 + Related Index 02–21 — `Game-Requirements/requirements/01-Project-Overview.md` (§ Functional Requirements, § Related Requirements Index)
  2. Gate vs north-star success criteria — same file § Success Criteria (OV-SC-G*, OV-SC-N*)
  3. Standing invariants + hash — § Non-Functional Requirements / Standing engineering invariants
  4. Scope tiers A/B/C — § Scope Boundaries
  5. Future Extensibility leads with scenario editor — § Future Extensibility item 1
  6. FR-09 schema/MCP/validation + NL Phase N — FR table
  7. Master index 2026-07-08 + reading order steps 5–6 (doc 21, hub re-check) — `Game-Requirements/Game-Requirements-Index.md`
  8. Tracker req 01 Post-S56 re-baseline note; MVP-done (S56) unchanged — `Game-Requirements/implementation-tracker-2026-07-04.md`
  9. RTM scope 01–21 + maturity row 21 PARTIAL — `docs/architecture/requirements-traceability.md`
  10. Design-review memo APPROVED — `production/qa/requirements-corpus-w0-design-review-2026-07-08.md`
  11. Task 10 verification battery PASS (21 req files; hub greps; related targets; docs-only status; no `.cs`)
  12. No commit performed in this closeout (user: no commits)
- **Code Review:** N/A (docs) — design-review memo is the review artifact
