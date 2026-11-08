# Sprint 40 — Deeper Polish: Catalog/Import Surfacing + Perf P1 Burn-Down + Replay Maint

**Dates:** ~2026-07-01 to ~2026-07-12 (target ~8–10 days)  
**Trunk:** `main` @ (post-S39 commit)  
**Predecessor:** Sprint 39 — COMPLETE (C2/Platform deeper polish, hygiene, perf P1, evidence, replay maint, dispatching refinements, closeout)  
**Stage:** Polish  
**Authority:** `production/polish-scope-boundary-2026-06-19.md` — Deeper Polish Horizon 2 only ([`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §3). Catalog/Import read-model surfacing (`Projection`-side), perf P1 burn-down, replay maintenance, evidence/playtest 12. NO scope expansion. ZERO DelegationBridge. Extend-only CatalogWriteGate. Immutable Baltic hash. Lean + headless primary.

## Sprint Goal

Execute Horizon 2 deeper polish: surface Catalog/Import provenance and quarantine outcomes in Editor read-models (`MountLoadoutQuarantineTriage`, `CatalogJsonImporter` projection paths — **single-agent Catalog cluster**), convert S39 perf P1 deltas into concrete fixes, replay/determinism maintenance (isolated fixtures only), evidence/PNG refresh + playtest session 12, hygiene/coordination closeout. All work cites boundary + roadmap §Horizon 2. Prepare for S41 Polish-exit pre-flight.

## Capacity

- Total days: 10
- Buffer (20%): 2 days
- **Effective dev-days**: **8**
- **Commit target**: **7–8 stories** (5 must + should/closeout)
- **Plan target**: **10–11 items**
- **Test baseline**: ≥**1213** (maintain / small growth; no regression)
- **Review mode**: lean (PR-SPRINT skipped)
- **Parallelism cap**: **4 tracks** (Catalog is HIGH blast-radius — one agent only)

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S40-01 | **Full-solution re-baseline** — day-1 build + test count ≥1213; GitNexus @ tip; ReplayGolden 6/6 | c-sharp-devops-engineer | 1 | S39-06 | 0 errors; ≥1213 PASS; smoke doc; indexed |
| S40-02 | **Sprint 40 QA plan** — matrix for Catalog/Import surfacing, perf P1, replay; blocks waves | team-qa | 1 | S40-01 | `production/qa/qa-plan-sprint-40-*.md`; cites boundary + roadmap §Horizon 2 |
| S40-03 | **Catalog/Import read-model surfacing** — quarantine/provenance legibility in Platform Editor projections; `impact()` mandatory | team-data (single agent) | 2.5 | S40-02 | Projection-side only; extend-only CatalogWriteGate; proxy 18/18+; GitNexus impact logged |
| S40-04 | **Perf P1 burn-down** — concrete fixes from S39 re-profile; appendix to perf baseline | team-simulation + perf | 1.5 | S40-01 | `production/perf/perf-profile-polish-baseline-2026-06-19.md` appendix; no prod hash change |
| S40-05 | **Replay/determinism maintenance** — golden fixture updates only; `/replay-verify` | team-simulation | 1 | S40-01 | Replay 6/6; isolated fixtures only |
| S40-06 | **Closeout hygiene** — smoke, replay 6/6, GitNexus, proxy, tracker; no S39 regression | c-sharp-devops-engineer | 0.5 | S40-03+ | `smoke-sprint-40-closeout-*.md`; all gates PASS |

**Sprint fails if** S40-02 not before feature waves, Catalog edits split across agents, or any gate regresses.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S40-07 | **Evidence / playtest 12** — targeted Catalog/Platform PNGs + structured playtest | team-qa + team-unity | 1.5 | S40-03 | `production/playtests/playtest-*-s40-*.md`; evidence in qa/evidence |
| S40-08 | **Hygiene / coordination** — sprint-status, coordination-map, kickoff validation | coordinator | 0.5 | S40-02 | Doc updates only; no cross-track conflicts |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S40-09 | **Import cohesion notes** — document `Import` module (67%) watch items; no refactor | coordinator | 0.5 | S40-03 | Notes in gap doc or ADR stub; read-only |

## Carryover from Sprint 39 / Roadmap §Horizon 2

| Item | Source | S40 placement |
|------|--------|---------------|
| Catalog/Import surfacing | Roadmap §Horizon 2; GitNexus Catalog hotspot | **S40-03** must |
| Perf P1 burn-down | S39-05 + roadmap §Horizon 2 | **S40-04** must |
| Replay maint | S39-05 pattern | **S40-05** must |
| Evidence / playtest cadence | S39-07 → playtest 12 | **S40-07** should |
| Hygiene / coordination | S39-04 pattern | **S40-08** should |

## Explicitly Out of Scope

Per `polish-scope-boundary-2026-06-19.md` and [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §4:
- Track B content completeness, full art bible, structural refactor, DOTS/ECS, launch artifacts.
- **ZERO touch** on `DelegationBridge.cs`
- **Extend-only** on `CatalogWriteGate`
- Production Baltic hash immutable
- No splitting Catalog symbol cluster across parallel agents

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S40-09 | S40-03 Catalog surfacing |
| 2 | S40-08 | S40-04 perf P1 |
| 3 | S40-07 | S40-05 replay |
| 4 | — | S40-06 closeout |

**Minimum shippable:** S40-01/02/03/04/05/06.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Catalog blast radius | High | CRITICAL | Single agent; mandatory `impact()`; projection-side only |
| Import cohesion creep into refactor | Med | High | Read-model surfacing only; defer B3 |
| Replay divergence | Med | CRITICAL | `/replay-verify`; isolated fixtures |
| Perf fixes touch sim hot path | Low | High | P1 items from baseline doc only |

## GitNexus / Hard Gates (Mandatory — every merge)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** Catalog/Platform projections, ReplayGolden 6/6, C2 proxy 18/18+
- `/replay-verify` mandatory on sim/delegation touches
- Production Baltic hash `17144800277401907079` **unchanged**
- Full sln test count ≥ prior baseline

## Definition of Done

- [ ] All Must Have tasks completed
- [ ] QA plan: `production/qa/qa-plan-sprint-40-*.md`
- [ ] Smoke PASS: `production/qa/smoke-sprint-40-closeout-*.md`
- [ ] C2 proxy 18/18+; ReplayGolden 6/6; tests ≥ baseline
- [ ] Catalog surfacing evidence captured (proxy + playtest if S40-07 ships)
- [ ] Perf P1 appendix updated
- [ ] **Boundary compliance** — every story cites `polish-scope-boundary-2026-06-19.md` + roadmap §Horizon 2

## Producer Feasibility Gate

**PR-SPRINT skipped — Lean mode.** Plan validated via dispatching-parallel-agents (prereq baseline + QA, then max 4 parallel tracks). Catalog track is **serial ownership** — do not split.

## Parallel Execution Model (dispatching-parallel-agents)

**Prerequisites (serial):** S40-01 + S40-02 **MUST** before feature dispatch.

**Tracks (max 4 parallel after prereqs):**

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| Catalog/Import surfacing | `stack/sprint40/catalog-import-projection` | **Local lead (1 agent only)** | S40-03 |
| Perf P1 burn-down | `stack/sprint40/perf-p1` | Cloud | S40-04 |
| Replay maint | `stack/sprint40/replay-maint` | Cloud | S40-05 |
| Evidence / playtest 12 | `stack/sprint40/evidence` | **Local** | S40-07 |
| Hygiene / coord | `stack/sprint40/hygiene` | Cloud | S40-08 |
| Closeout | `stack/sprint40/closeout` | Local | S40-06 |

**Wave plan:** see `production/agentic/sprint-40-parallel-kickoff-2026-06-20.md`.

## Related Artifacts

| Artifact | Path |
|----------|------|
| Roadmap | `docs/reports/future-sprint-roadpmap.md` §Horizon 2 |
| Parallel kickoff | `production/agentic/sprint-40-parallel-kickoff-2026-06-20.md` |
| Worktree manifest | `production/agentic/s39-s48-worktree-manifest.md` |
| Local/cloud routing | `production/agentic/local-cloud-agent-routing.md` |
| Boundary | `production/polish-scope-boundary-2026-06-19.md` |
| Perf baseline | `production/perf/perf-profile-polish-baseline-2026-06-19.md` |

## Next Steps

- `/qa-plan sprint 40`
- Bootstrap worktrees per manifest
- Parallel waves (Catalog single-owner, Perf, Replay, Evidence)
- `/smoke-check`; `/sprint-status`; `/retrospective`
- Prepare S41 plan during closeout

---

*Created per `/sprint-plan` + 10-sprint agent program. Horizon 2 in-boundary only. Cites polish-scope-boundary + future-sprint-roadpmap §3.*
