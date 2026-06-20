# Sprint 41 Parallel Kickoff — Polish Hardening + Release Pre-Flight

**Date:** 2026-06-20 (drafted during S39 closeout; execute post-S40 COMPLETE)  
**Trunk:** `main` @ (post-S40)  
**Sprint plan:** `production/sprints/sprint-41-polish-hardening-release-preflight.md`  
**QA plan:** `production/qa/qa-plan-sprint-41-*.md` (TBD)  
**Authority:** `production/polish-scope-boundary-2026-06-19.md` + [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §Horizon 3. **Read-only ADR — no production refactor code.**

## Sprint Goal (recap)

Final in-boundary Polish: structural-debt ADR, determinism audit, Polish-exit evidence pack, Track B gap analysis, scope-expansion decision packet. **Blocks S42 dispatch.**

## Parallel Execution Model

**Prerequisites (serial):** S41-01 + S41-02 before waves. **Parallelism cap: 4 tracks** (cloud-heavy).

## Wave plan

| Wave | Stories | Track | Est. | Agent env | Notes |
|------|---------|-------|------|-----------|-------|
| Day-1 | S41-01 | Baseline | 1d | Local or Cloud | Blocks all |
| W0 | S41-02 | QA plan | 1d | Cloud | Blocks waves |
| W1 | S41-03 | **ADR (read-only)** | 2d | Cloud | Decision/Telemetry/Osint |
| W2 | S41-04 | **Determinism audit** | 1.5d | Cloud | Re-index GitNexus |
| W3 | S41-05 | **Evidence pack** | 1.5d | Local + Cloud | Polish-exit index |
| W4 | S41-07 | **Gap analysis** | 1.5d | Cloud | B1–B6 enum; no impl |
| W5 | S41-06, S41-08 | **Closeout + scope packet** | 1d | **Local** | Gate template filled |

## Track ownership

| Track | Owner | Stories | Stack prefix | Agent env |
|-------|-------|---------|--------------|-----------|
| ADR | c-sharp-architect | S41-03 | `stack/sprint41/adr-decision-telemetry` | Cloud |
| Determinism | determinism-engineer | S41-04 | `stack/sprint41/determinism-audit` | Cloud |
| Evidence pack | team-qa | S41-05 | `stack/sprint41/evidence-pack` | Local + Cloud |
| Gap analysis | requirements-analyst | S41-07 | `stack/sprint41/gap-analysis` | Cloud |
| Closeout | coordinator | S41-06, S41-08 | `stack/sprint41/closeout` | **Local** |

## File ownership matrix

| File / path | Owner track | Notes |
|-------------|-------------|-------|
| `docs/adr/*` or `production/adr/*` | ADR | Read-only characterization |
| `production/qa/evidence/README-polish-exit-*` | Evidence | Consolidation index |
| `production/gate-checks/scope-expansion-decision-*` | Closeout | **Required before S42** |
| `src/**` production code | **None** | No refactor this sprint |

## Hard gates

- Same Polish gates (replay 6/6, proxy 18/18+, hash, DelegationBridge ZERO)
- **No S42 agent dispatch** until scope decision recorded

## Worktree bootstrap

See `production/agentic/s39-s48-worktree-manifest.md` §S41.

---

*Outputs feed scope-expansion gate (roadmap §4). Template: `production/gate-checks/scope-expansion-decision-template-2026-06-20.md`.*
