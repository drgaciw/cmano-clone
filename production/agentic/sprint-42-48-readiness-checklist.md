# Track B Readiness Checklist — S42–S48 Pre-Flight

**Date:** 2026-06-20  
**Use:** Coordinator runs this checklist **before dispatching agents** for each Track B sprint (S42–S48).  
**Authority:** [`scope-expansion-decision-template-2026-06-20.md`](../gate-checks/scope-expansion-decision-template-2026-06-20.md)

---

## Global prerequisites (all S42–S48)

- [x] **Scope-expansion decision recorded** — [`production/gate-checks/scope-expansion-decision-2026-06-20.md`](../gate-checks/scope-expansion-decision-2026-06-20.md) **APPROVED** 2026-06-20 (user sign-off: "Approve Track B default")
- [ ] **S41 COMPLETE** — Polish-exit report + gap analysis exist
- [x] **New scope boundary doc** published — [`production/release-enablement-scope-boundary-2026-06-20.md`](../release-enablement-scope-boundary-2026-06-20.md) (supersedes polish-scope-boundary for Track B)
- [ ] **GitNexus index** @ HEAD (`node .gitnexus/run.cjs analyze` if stale)
- [ ] **Test baseline** documented in QA plan (floor ≥ S39 closeout: **1215**)
- [ ] **Worktrees** bootstrapped per [`s39-s48-worktree-manifest.md`](s39-s48-worktree-manifest.md)
- [ ] **Local/cloud routing** assigned per [`local-cloud-agent-routing.md`](local-cloud-agent-routing.md)

---

## S42 — Release kickoff (B1 wave 1 + B2 start)

- [ ] Sprint plan: `production/sprints/sprint-42-release-kickoff-content-art-bible-w1.md`
- [ ] Kickoff: `production/agentic/sprint-42-parallel-kickoff-2026-06-20.md`
- [ ] QA plan: `production/qa/qa-plan-sprint-42-*.md`
- [ ] Committed tracker rows for wave 1 listed in scope-expansion decision
- [ ] Art bible §1–4 scope approved in decision record
- [ ] File ownership matrix reviewed (Catalog single-agent rule)
- [ ] Expanded gate matrix documented in QA plan

**Dispatch when:** all global + S42 items checked.

---

## S43 — Content wave 2 + art bible complete (B1 + B2)

- [ ] S42 closeout smoke PASS
- [ ] Sprint plan + kickoff + QA plan for S43 exist
- [ ] Engage/features batch rows committed in scope decision
- [ ] B2 full 9-section + asset specs in sprint acceptance criteria
- [ ] B1 exit criteria met (tracker rows closed per decision)

**Dispatch when:** S42 COMPLETE + S43 checklist green.

---

## S44 — Structural debt (B3)

- [ ] S41 ADR for Decision/Telemetry exists (read-only spike output)
- [ ] Sprint plan + kickoff + QA plan for S44 exist
- [ ] Refactor scope approved in scope-expansion decision
- [ ] Decision and Telemetry tracks have **zero shared file overlap**
- [ ] `determinism-engineer` paired on replay-gate track
- [ ] Golden-replay 6/6 baseline captured pre-refactor

**Dispatch when:** S43 COMPLETE + S41 ADR on file.

---

## S45 — Performance scale-out (B4)

- [ ] B1 scope locked (S43 closeout)
- [ ] DOTS/ECS in/out of scope explicit in scope decision
- [ ] Sprint plan + kickoff + QA plan for S45 exist
- [ ] `determinism-engineer` assigned to every sim-touching track
- [ ] Perf budget doc updated or appendix planned

**Dispatch when:** S44 COMPLETE + B1 locked.

---

## S46 — Launch artifacts (B5)

- [ ] B1 + B2 COMPLETE (S43 closeout)
- [ ] Sprint plan + kickoff + QA plan for S46 exist
- [ ] `release-manager` + `localization-lead` tracks assigned
- [ ] Launch artifact list matches scope decision budget

**Dispatch when:** S43 COMPLETE + B1/B2 verified.

---

## S47 — Release dry run (B6 prep)

- [ ] S46 closeout smoke PASS
- [ ] Full solution test + Play Mode smoke green on trunk
- [ ] Gate-check draft prepared (`production/gate-checks/`)
- [ ] Buildkite preflight green (`buildkite-ci-lead`)
- [ ] Go/No-Go checklist drafted

**Dispatch when:** S46 COMPLETE + dry-run QA plan approved.

---

## S48 — Release gate (B6)

- [ ] S47 Go decision (human)
- [ ] All B1–B5 exit criteria met
- [ ] Consolidated evidence pack complete
- [ ] `/gate-check` Polish→Release run
- [ ] Human verdict on release recorded
- [ ] Stage advance + program retro

**Dispatch when:** S47 Go + human approves release gate execution.

---

## Verdict block (coordinator, per sprint)

| Sprint | Date | Checker | Ready to dispatch? | Notes |
|--------|------|---------|-------------------|-------|
| S42 | | | BLOCKED / READY | |
| S43 | | | BLOCKED / READY | |
| S44 | | | BLOCKED / READY | |
| S45 | | | BLOCKED / READY | |
| S46 | | | BLOCKED / READY | |
| S47 | | | BLOCKED / READY | |
| S48 | | | BLOCKED / READY | |

---

*Track B scope gate APPROVED 2026-06-20. S42 dispatch blocked until S41 closeout PASS.*
