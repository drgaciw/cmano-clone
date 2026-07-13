---
id: S35-15
status: Complete
completed: 2026-06-19
type: Config
priority: nice-to-have
graphite_branch: stack/sprint35/ci-hygiene
estimate_days: 0.5
dependencies:
  - S35-01 green baseline
owner: c-sharp-devops-engineer
sprint: 35
req_trace: S34-12 carryover (6th deferral OK); gate r2 tests/unit layout
governing_adrs: N/A — doc-only CI hygiene
---

# Story 035-15 — CI/Local Gate Refresh + Test Layout Audit

> **Epic:** sprint-35-closeout-devops

## Summary

Doc-only refresh of `tools/verify-ci-local.ps1` / `tools/buildkite/dotnet-ci.sh` thresholds for **≥1193** baseline and closeout target. Audit studio `tests/unit/` + `tests/integration/` layout gap (gate r2 residual #7) with disposition — alias, migrate plan, or explicit deferral. **Non-blocking** for sprint closeout.

## Acceptance Criteria

- [x] `tools/verify-ci-local.ps1` policy pointer updated for Sprint 35 (day-1 **≥1204**, closeout **≥1204**)
- [x] Bash parity note in evidence if `pwsh` unavailable on Linux host
- [x] `tests/unit/` layout audit documented: current state (`src/*.Tests`), gap vs studio template, recommended disposition (**DEFER** — 6th deferral)
- [x] Evidence: `production/qa/sprint-35-ci-hygiene-2026-06-19.md`
- [x] 6th deferral explicitly OK if capacity constrained — note in carry-forward log
- [x] No breaking CI changes required for closeout

## QA Test Cases

```
Test: Local gate script runs green @ trunk
  Given: clean tree @ closeout or day-1 baseline
  When: pwsh -File tools/verify-ci-local.ps1 OR bash tools/buildkite/dotnet-ci.sh
  Then: ≥1193 tests PASS; ReplayGolden step documented PASS or skipped with rationale

Manual check: Test layout audit complete
  Setup: Read gate r2 residual #7 and polish-scope-boundary §Out of Scope
  Verify: Evidence states whether tests/unit/ will alias, migrate, or defer
  Pass condition: Unambiguous disposition; no false claim of studio layout complete
```

## Test Evidence Path

- `production/qa/sprint-35-ci-hygiene-YYYY-MM-DD.md`
- `tools/verify-ci-local.ps1`
- `tools/buildkite/dotnet-ci.sh`

## Out of Scope

- Migrating all tests to `tests/unit/` (audit + plan only unless trivial alias)
- Buildkite pipeline structural changes
- Blocking sprint closeout on this story