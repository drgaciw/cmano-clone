# Sprint 42 — Release Kickoff: Content Wave 1 + Art Bible Sections 1–4 (B1 + B2 Start)

**Dates:** ~TBD post scope gate (~10–12 days)  
**Trunk:** `main` @ (post-S41 + scope-expansion decision recorded)  
**Predecessor:** Sprint 41 — COMPLETE + **scope-expansion gate APPROVED**  
**Stage:** Release Enablement (Track B)  
**Authority:** **Requires scope-expansion decision** — [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §4, §9; new boundary doc post-gate (replaces or lifts `polish-scope-boundary-2026-06-19.md`). **DO NOT EXECUTE** until `production/gate-checks/scope-expansion-decision-*.md` is signed.

> **OUT-OF-BOUNDARY — PLANNING ONLY** until human scope gate is recorded.

## Sprint Goal

First Release Enablement sprint post-gate: deliver B1 content wave 1 (first committed tracker rows — Catalog/Platform/Scenario batch) and start B2 (art bible sections 1–4). Establish expanded QA baseline and gate matrix. Maintain determinism invariants unless scope ADR explicitly revises them.

## Capacity

- Total days: 12
- Buffer (20%): 2 days
- **Effective dev-days**: **10**
- **Commit target**: **8–9 stories**
- **Test baseline**: ≥ prior + monotonic growth expected
- **Parallelism**: **5 tracks** (B1 + B2 concurrent per roadmap)

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S42-01 | **Re-baseline + expanded gate matrix** — build/test; cite new boundary doc | c-sharp-devops-engineer | 1 | Scope gate | 0 errors; gate matrix doc updated |
| S42-02 | **Sprint 42 QA plan** — B1/B2 scope; blocks waves | team-qa | 1 | S42-01 | `production/qa/qa-plan-sprint-42-*.md` |
| S42-03 | **Content wave 1 — Catalog/Platform** — first committed tracker rows (per gate) | team-data (local lead) | 3 | S42-02, gate B1 list | Rows committed per scope decision; `impact()` mandatory |
| S42-04 | **Content wave 1 — Scenario/data** — scenario packages, policy JSON (replay-gated) | team-simulation | 2.5 | S42-02 | Replay 6/6; golden updates ADR if needed |
| S42-05 | **Art bible sections 1–4** — expand `design/art/art-bible.md` | art-director + team-ui | 2 | S42-02 | Sections 1–4 complete per B2 scope |
| S42-06 | **Closeout** — smoke, evidence, sprint-status | c-sharp-devops-engineer | 0.5 | S42-03+ | `smoke-sprint-42-closeout-*.md`; gates PASS |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S42-07 | **Baseline QA boundary cite** — link new scope doc in all artifacts | coordinator | 0.5 | S42-01 | All stories cite post-gate boundary |

## Explicitly Out of Scope (until gate)

- Entire sprint blocked without scope-expansion decision
- B3 refactor, B4 DOTS, B5 launch artifacts (later sprints)
- DelegationBridge touch without explicit ADR

## GitNexus / Hard Gates

- ReplayGolden 6/6; C2 proxy 18/18+ (expand matrix if new UI)
- Baltic hash immutable unless golden bump ADR
- CatalogWriteGate extend-only unless scope ADR revokes
- `impact()` before every symbol edit; `detect_changes()` before commit

## Definition of Done

- [ ] Scope gate decision on file
- [ ] Must Have complete; QA + smoke PASS
- [ ] B1 wave 1 rows committed per gate list
- [ ] Art bible sections 1–4 complete
- [ ] New boundary doc cited everywhere

## Parallel Execution Model

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| Content Catalog/Platform | `stack/sprint42/content-catalog-platform` | Local lead | S42-03 |
| Content Scenario | `stack/sprint42/content-scenario` | Cloud | S42-04 |
| Art bible 1–4 | `stack/sprint42/art-bible-1-4` | Cloud | S42-05 |
| Baseline + QA | `stack/sprint42/baseline-qa` | Cloud | S42-01, S42-02 |
| Closeout | `stack/sprint42/closeout` | Local | S42-06 |

## Related Artifacts

| Artifact | Path |
|----------|------|
| Roadmap | `docs/reports/future-sprint-roadpmap.md` §5, §9 |
| Kickoff | `production/agentic/sprint-42-parallel-kickoff-2026-06-20.md` |
| Scope template | `production/gate-checks/scope-expansion-decision-template-2026-06-20.md` |

---

*Planning artifact only. Requires scope-expansion decision before execution (roadmap §4).*
