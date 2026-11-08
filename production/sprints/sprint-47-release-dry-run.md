# Sprint 47 — Release Dry Run (B6 Prep)

**Dates:** ~TBD (~5–7 days)  
**Trunk:** `main` @ (post-S46)  
**Predecessor:** Sprint 46 — COMPLETE (B5 artifacts)  
**Stage:** Release Enablement (Track B)  
**Authority:** B6 prep — [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §5, §9.

> **OUT-OF-BOUNDARY — PLANNING ONLY.**

## Sprint Goal

Full release gate dry run: complete `dotnet test` + Play Mode smoke, draft gate-check, Buildkite/CI preflight, consolidated evidence, Go/No-Go checklist. Surface blockers before S48 verdict sprint.

## Capacity

- Total days: 7
- Buffer: 1 day
- **Effective dev-days**: **6**
- **Parallelism**: **4 tracks** (mostly verification)

## Tasks

### Must Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S47-01 | **Full test + Play Mode smoke** | c-sharp-devops-engineer | 1.5 | S46-06 | Full sln + PlayModeSmokeHarnessTests PASS |
| S47-02 | **Gate-check draft** | coordinator | 1.5 | S47-01 | `production/gate-checks/release-dry-run-*.md` |
| S47-03 | **CI/Buildkite preflight** | buildkite-ci-lead | 1.5 | S47-01 | Pipeline green; branch protection notes |
| S47-04 | **Evidence consolidation** | team-qa | 1.5 | S47-01 | Single release evidence bundle |
| S47-05 | **Go/No-Go checklist + closeout** | release-manager + coordinator | 1 | S47-02+ | Checklist with blockers enumerated |

## Definition of Done

- [ ] All tests + smoke PASS
- [ ] Gate-check draft complete (CONCERNS or PASS WITH NOTES documented)
- [ ] CI preflight signed
- [ ] Go/No-Go checklist ready for S48 human approval

## Parallel Execution Model

| Track | Stack prefix | Agent env | Focus |
|-------|--------------|-----------|-------|
| Test + smoke | `stack/sprint47/test-smoke` | Cloud | S47-01 |
| Gate-check draft | `stack/sprint47/gate-check` | Local | S47-02 |
| CI preflight | `stack/sprint47/ci-preflight` | Cloud | S47-03 |
| Evidence | `stack/sprint47/evidence` | Local | S47-04 |
| Closeout | `stack/sprint47/closeout` | Local | S47-05 |

---

*Planning only. Precedes S48 release gate.*
