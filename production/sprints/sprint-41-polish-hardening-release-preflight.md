# Sprint 41 — Polish Hardening + Release-Readiness Pre-Flight (Polish Exit)

**Dates:** ~2026-07-13 to ~2026-07-24 (target ~8–10 days)  
**Trunk:** `main` @ (post-S40 commit)  
**Predecessor:** Sprint 40 — COMPLETE (Catalog/Import surfacing, perf P1, replay maint, evidence/playtest 12, closeout)  
**Stage:** Polish (final in-boundary horizon)  
**Authority:** `production/polish-scope-boundary-2026-06-19.md` — Horizon 3 only ([`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §3). Read-only structural-debt ADR, determinism audit, Polish-exit evidence pack, Track B gap analysis + scope-decision packet. **NO production code refactor.** NO launch artifacts. Prepares scope-expansion gate (roadmap §4).

## Sprint Goal

Complete final in-boundary Polish sprint: characterize `Decision` (60%) and `Telemetry` (67%) cohesion problems in a **read-only ADR** (tees up Track B B3), full determinism audit + GitNexus re-index, consolidate Polish-exit evidence pack (perf, replay, proxy, playtest corpus), enumerate Track B requirements in gap analysis (no implementation), produce scope-expansion decision packet for human gate before S42. All work cites boundary + roadmap §Horizon 3.

## Capacity

- Total days: 10
- Buffer (20%): 2 days
- **Effective dev-days**: **8**
- **Commit target**: **6–7 stories** (4 must + should/closeout)
- **Plan target**: **9–10 items**
- **Test baseline**: ≥**1213** (maintain; no regression)
- **Review mode**: lean (PR-SPRINT skipped)
- **Parallelism cap**: **4 tracks** (cloud-heavy; mostly read/docs)

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S41-01 | **Full-solution re-baseline** — build + test ≥1213; GitNexus @ tip; ReplayGolden 6/6 | c-sharp-devops-engineer | 1 | S40-06 | 0 errors; indexed; smoke doc |
| S41-02 | **Sprint 41 QA plan** — audit matrix, determinism sweep, evidence consolidation; blocks waves | team-qa | 1 | S41-01 | `production/qa/qa-plan-sprint-41-*.md` |
| S41-03 | **Structural-debt ADR (read-only)** — Decision/Telemetry/Osint characterization; **no production code** | c-sharp-architect | 2 | S41-02 | ADR in `docs/adr/` or `production/adr/`; GitNexus cohesion cites; input to B3 |
| S41-04 | **Determinism audit pass** — `/determinism-audit`, `/replay-verify`, GitNexus re-index @ HEAD | determinism-engineer + team-simulation | 1.5 | S41-01 | Audit report; Replay 6/6; `node .gitnexus/run.cjs analyze` recorded |
| S41-05 | **Polish-exit evidence pack** — single consolidated pack (perf, replay, proxy, playtests S35–S40) | team-qa | 1.5 | S41-02 | `production/qa/evidence/README-polish-exit-*.md` or equivalent index |
| S41-06 | **Closeout + scope-decision packet** — Polish-exit report + gap analysis for gate; smoke | coordinator + c-sharp-devops-engineer | 1 | S41-03+ | `production/gate-checks/` packet draft; `smoke-sprint-41-closeout-*.md`; **blocks S42 dispatch** |

**Sprint fails if** production refactor code lands, launch artifacts produced, or scope packet missing.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S41-07 | **Track B gap analysis** — enumerate B1–B6 requirements from tracker; no implementation | requirements-analyst | 1.5 | S41-02 | Gap doc cites tracker Partial rows; maps to roadmap §5 |
| S41-08 | **Release pre-flight checklist (analysis only)** — what B5/B6 require; no store pages | release-manager | 0.5 | S41-07 | Checklist stub in production/; read-only |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S41-09 | **Coordination / worktree manifest refresh** — S42–S48 planning refs | coordinator | 0.5 | S41-06 | Manifest + kickoff refs updated |

## Carryover from Sprint 40 / Roadmap §Horizon 3

| Item | Source | S41 placement |
|------|--------|---------------|
| Structural-debt spike (read-only) | Roadmap §Horizon 3; GitNexus Decision 60% | **S41-03** must |
| Determinism audit | Roadmap §Horizon 3 | **S41-04** must |
| Evidence consolidation | Roadmap §Horizon 3 | **S41-05** must |
| Release gap analysis | Roadmap §Horizon 3 | **S41-07** should |
| Scope-expansion packet | Roadmap §4 | **S41-06** must |

## Explicitly Out of Scope

Per boundary + roadmap §4 (Track B requires scope decision):
- **Any S42–S48 execution** — plans exist; dispatch forbidden until gate recorded
- Production refactor of Decision/Telemetry/Osint (defer to S44 / B3)
- Full art bible, content waves, launch artifacts, store pages, localization pipeline
- **ZERO touch** `DelegationBridge.cs`; extend-only `CatalogWriteGate`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S41-09 | S41-03 ADR |
| 2 | S41-08 | S41-04 determinism |
| 3 | S41-07 | S41-05 evidence pack |
| 4 | — | S41-06 closeout + scope packet |

**Minimum shippable:** S41-01/02/03/04/05/06 + scope packet.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| ADR triggers premature refactor | Med | High | ADR read-only; explicit out-of-scope |
| Scope packet incomplete | Med | CRITICAL | Use `scope-expansion-decision-template-2026-06-20.md` |
| GitNexus index stale at exit | Med | Med | Mandatory re-index in S41-04 |

## GitNexus / Hard Gates (Mandatory — every merge)

- **ZERO touch:** `DelegationBridge.cs`
- **Extend-only:** `CatalogWriteGate`
- ReplayGolden 6/6; C2 proxy 18/18+; Baltic hash unchanged
- Full sln ≥ baseline; `/replay-verify` on any sim touch
- Re-index GitNexus @ HEAD (S41-04)

## Definition of Done

- [x] All Must Have completed (S41-01 baseline PASS; S41-02 QA plan AC MET; S41-03 ADR read-only Accepted; S41-04 determinism PASS 0 issues; S41-05 polish-exit ADEQUATE; S41-06/08 closeout + scope packet assembled)
- [x] QA plan + smoke closeout PASS (smoke-sprint-41-closeout-2026-06-20.md + gate packet)
- [x] Parallel waves complete via dispatching-parallel-agents; closeout assembled (S41-06/S41-08 + S41-09 note)
- **S42 dispatch BLOCKED until human gate recorded** (per scope packet + sprint-status + all manifests)
- [ ] Structural-debt ADR published (read-only)
- [ ] Determinism audit + GitNexus re-index complete
- [ ] Polish-exit evidence pack indexed
- [ ] **Scope-expansion decision packet** ready for human gate (`production/gate-checks/`)
- [ ] **S42 dispatch explicitly blocked** until gate decision recorded
- [ ] Boundary compliance on all artifacts

## Producer Feasibility Gate

**PR-SPRINT skipped — Lean mode.** Cloud-heavy parallel tracks (ADR, audit, gap analysis). Local owns evidence consolidation + closeout + scope packet assembly.

## Parallel Execution Model (dispatching-parallel-agents)

**Prerequisites (serial):** S41-01 + S41-02 **MUST** before waves.

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| ADR (read-only) | `stack/sprint41/adr-decision-telemetry` | Cloud | S41-03 |
| Determinism audit | `stack/sprint41/determinism-audit` | Cloud | S41-04 |
| Evidence pack | `stack/sprint41/evidence-pack` | Local + Cloud | S41-05 |
| Gap analysis | `stack/sprint41/gap-analysis` | Cloud | S41-07 |
| Closeout + scope packet | `stack/sprint41/closeout` | **Local** | S41-06, S41-08 |

## Related Artifacts

| Artifact | Path |
|----------|------|
| Roadmap | `docs/reports/future-sprint-roadpmap.md` §3–§4 |
| Scope gate template | `production/gate-checks/scope-expansion-decision-template-2026-06-20.md` |
| Parallel kickoff | `production/agentic/sprint-41-parallel-kickoff-2026-06-20.md` |
| Boundary | `production/polish-scope-boundary-2026-06-19.md` |

## Next Steps

- `/qa-plan sprint 41`
- Execute parallel waves (cloud-heavy)
- Human scope-expansion decision using template
- **Do not dispatch S42** until gate recorded

---

*Created per `/sprint-plan` + 10-sprint agent program. Final in-boundary Polish horizon. Output feeds scope-expansion gate (roadmap §4).*
