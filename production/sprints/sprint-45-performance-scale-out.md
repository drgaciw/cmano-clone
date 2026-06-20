# Sprint 45 — Performance Scale-Out (B4)

**Dates:** ~TBD (~10–14 days)  
**Trunk:** `main` @ (post-S44)  
**Predecessor:** Sprint 44 — COMPLETE; **B1 scope locked**  
**Stage:** Release Enablement (Track B)  
**Authority:** B4 epic — [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §5, §9. `determinism-engineer` paired on every sim-touching track.

> **OUT-OF-BOUNDARY — PLANNING ONLY.**

## Sprint Goal

Beyond P0/P1 polish perf: scale-out work on `Runtime`/`Sensors`/`Engage` hot paths (may include DOTS/ECS paths if scope ADR permits). Profiling budgets, determinism verification, replay gate. Coordinate with determinism-engineer on all sim changes.

## Capacity

- Total days: 14
- Buffer: 2 days
- **Effective dev-days**: **12**
- **Parallelism**: **5 tracks** (sim tracks require determinism pairing)

## Tasks

### Must Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S45-01 | **Re-baseline + QA plan** | c-sharp-devops-engineer + team-qa | 1.5 | S44-06 | Blocks waves |
| S45-02 | **Runtime/Sensors hot path** | unity-dots-specialist + determinism-engineer | 4 | S45-01 | Perf budgets met; replay 6/6 |
| S45-03 | **Engage scale** | gameplay-programmer + determinism-engineer | 3.5 | S45-01 | Scale targets per scope ADR |
| S45-04 | **Profiling + budgets doc** | performance-analyst | 2 | S45-01 | Updated perf profile; before/after |
| S45-05 | **Replay/determinism verification** | determinism-engineer | ongoing | All merges | 6/6; `/replay-verify` |
| S45-06 | **Closeout** | c-sharp-devops-engineer | 0.5 | S45-02+ | Smoke; B4 exit |

## Definition of Done

- [ ] Perf scale-out targets met per scope ADR
- [ ] Determinism-engineer sign-off on sim tracks
- [ ] GitNexus re-index @ HEAD (program milestone)
- [ ] Replay 6/6; tests monotonic

## Parallel Execution Model

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| Runtime/Sensors | `stack/sprint45/runtime-sensors` | Local lead | S45-02 |
| Engage scale | `stack/sprint45/engage-scale` | Cloud + determinism review | S45-03 |
| Perf profile | `stack/sprint45/perf-profile` | Cloud | S45-04 |
| Replay | `stack/sprint45/replay` | Cloud | S45-05 |
| Closeout | `stack/sprint45/closeout` | Local | S45-06 |

---

*Planning only. Requires B1 locked + scope gate.*
