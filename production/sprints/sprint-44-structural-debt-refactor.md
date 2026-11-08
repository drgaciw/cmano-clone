# Sprint 44 — Structural Debt Refactor (B3)

**Dates:** ~TBD (~10–14 days)  
**Trunk:** `main` @ (post-S43)  
**Predecessor:** Sprint 43 — COMPLETE; **S41 ADR required**  
**Stage:** Release Enablement (Track B)  
**Authority:** B3 epic — [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §5, §9. Requires S41 structural-debt ADR + scope gate.

> **OUT-OF-BOUNDARY — PLANNING ONLY.**

## Sprint Goal

Execute B3: refactor `Decision` (60%) and `Telemetry` (67%) cohesion; audit `Osint` (68%). Use GitNexus `rename`/`impact`. Golden-replay 6/6 mandatory after each merge. Split by module — max 3 code tracks + replay gate.

## Capacity

- Total days: 14
- Buffer: 2 days
- **Effective dev-days**: **12**
- **Parallelism**: **3 code tracks max** + replay gate

## Tasks

### Must Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S44-01 | **Re-baseline + QA plan** | c-sharp-devops-engineer + team-qa | 1.5 | S43-06, S41 ADR | QA plan blocks waves |
| S44-02 | **Decision module refactor** | c-sharp-engineer (local lead) | 4 | S44-01 | Cohesion target per ADR; GitNexus rename |
| S44-03 | **Telemetry module refactor** | c-sharp-engineer | 3.5 | S44-01 | Parallel only if zero shared files with S44-02 |
| S44-04 | **Osint audit + targeted fixes** | team-data | 2 | S44-01 | Audit report; minimal surface |
| S44-05 | **Replay/regression gate track** | determinism-engineer | ongoing | All merges | 6/6 after each merge |
| S44-06 | **Closeout** | c-sharp-devops-engineer | 0.5 | S44-02+ | Smoke; B3 exit criteria |

## GitNexus / Hard Gates

- Mandatory `impact()` + `rename` (not find-replace)
- ReplayGolden 6/6 after **every** merge
- No cross-track edits on shared files

## Definition of Done

- [ ] S41 ADR recommendations addressed or explicitly deferred with ADR update
- [ ] Decision/Telemetry cohesion improved per targets
- [ ] Osint audit complete
- [ ] Replay 6/6; full test suite green

## Parallel Execution Model

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| Decision refactor | `stack/sprint44/decision-refactor` | Local lead | S44-02 |
| Telemetry refactor | `stack/sprint44/telemetry-refactor` | Local/Cloud | S44-03 |
| Osint audit | `stack/sprint44/osint-audit` | Cloud | S44-04 |
| Replay gate | `stack/sprint44/replay-gate` | Cloud | S44-05 |
| Closeout | `stack/sprint44/closeout` | Local | S44-06 |

---

*Planning only. Requires scope gate + S41 ADR + B1 scope locked.*
