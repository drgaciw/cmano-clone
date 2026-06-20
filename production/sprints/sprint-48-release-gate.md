# Sprint 48 — Release Gate (B6)

**Dates:** ~TBD (~3–5 days)  
**Trunk:** `main` @ (post-S47 dry run)  
**Predecessor:** Sprint 47 — COMPLETE; Go/No-Go checklist green or explicitly waived  
**Stage:** Release Enablement → **Release** (stage advance if PASS)  
**Authority:** B6 — [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §5, §9. `/gate-check` Polish→Release.

> **OUT-OF-BOUNDARY — PLANNING ONLY.**

## Sprint Goal

Execute release gate: `/gate-check`, final retro, stage advance (`production/stage.txt`), optional tag. **Mostly serial — 1–2 agents max.** Human approval required on verdict.

## Capacity

- Total days: 5
- Buffer: 1 day
- **Effective dev-days**: **4**
- **Parallelism**: **Serial** (1–2 agents)

## Tasks

### Must Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S48-01 | **Final verification sweep** | c-sharp-devops-engineer | 1 | S47-05 | Full sln + Replay 6/6 + proxy + smoke |
| S48-02 | **`/gate-check` execution** | coordinator + technical-director | 1.5 | S48-01 | Verdict recorded in `production/gate-checks/` |
| S48-03 | **GitNexus re-index + detect_changes** | c-sharp-devops-engineer | 0.5 | S48-01 | Index @ HEAD; change scope verified |
| S48-04 | **Retro + stage advance + closeout** | producer + coordinator | 1 | S48-02 | `retro-sprint-48-*.md`; stage.txt updated if PASS |
| S48-05 | **Hindsight retain + program closeout** | coordinator | 0.5 | S48-04 | `[OUTCOME:]` in dev-cmano-clone bank |

## Hard Gates (Release)

All B1–B5 prerequisites + standing invariants (roadmap §6):
- ReplayGolden 6/6; C2 proxy 18/18+
- Full content + artifacts per scope decision
- Release checklist + store + i18n pipeline complete
- **Human sign-off** on gate verdict

## Definition of Done

- [ ] Gate-check verdict APPROVED (or documented CONCERNS with waiver)
- [ ] Stage advanced to Release if PASS
- [ ] Program retro complete
- [ ] sprint-status.yaml final
- [ ] 10-sprint program artifacts archived

## Parallel Execution Model

**Serial only.** Coordinator local; cloud optional for CI re-run.

| Step | Stack prefix | Agent env |
|------|--------------|-----------|
| Verification | `stack/sprint48/verification` | Cloud OK |
| Gate-check | `stack/sprint48/gate-check` | **Local** |
| Closeout | `stack/sprint48/closeout` | **Local** |

---

*Planning only. Final sprint of 10-sprint program. Human approval on verdict mandatory.*
