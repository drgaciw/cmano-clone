# Sprint 36 — Polish Phase 2: Dependency Graph & Carryover Closure

**Dates:** 2026-07-06 to 2026-07-17  
**Trunk:** `main` @ e60eadc (post S35)  
**Predecessor:** Sprint 35 — COMPLETE (17/17, QA APPROVED WITH CONDITIONS)  
**Stage:** Polish  
**Authority:** polish-scope-boundary-2026-06-19.md — Phase 2 continuation (graph runtime + residuals)

## Sprint Goal
Polish Phase 2 kickoff: implement platform→link dependency graph runtime + surfacing (S35-16 seed), close residual Polish 0 carryovers (C2 frame budget, live evidence, test layout, art bible), and execute parallel polish tracks while preserving all gates.

## Capacity
- Total days: 10
- Buffer (20%): 2 days reserved
- Available: 8 days
- Commit target: 9 stories (7 must + 2 should + closeout)
- Plan target: 14 stories
- Test baseline: ≥1204

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S36-01 | Full-solution re-baseline post-S35 + GitNexus + ReplayGolden 6/6 | c-sharp-devops-engineer | 1 | — | ≥1204 PASS; smoke; indexed |
| S36-02 | Sprint 36 QA plan | team-qa | 1 | S36-01 | qa-plan-sprint-36-*.md |
| S36-03 | CatalogDependencyGraphIndex Platform→Link edges + tests + invalidation | team-data | 2.5 | S36-01 | GetSortedDependencyEdges includes platform→link; tests pass; hash unchanged |
| S36-04 | CLI extension + golden for link edges | team-data | 1 | S36-03 | link: entries; deterministic |
| S36-05 | Unity C2 frame budget capture + remediation | team-unity | 2 | S36-01, S36-02 | Frame measurement doc; proxy 18/18 |
| S36-06 | Live Editor PNG re-capture | team-unity | 1.5 | S36-05 | 12+ PNGs or lean notes |
| S36-07 | Platform Editor Phase H link surfacing (read-only) | team-unity + team-data | 1.5 | S36-03, S36-05 | Viewer shows FKs; tests pass |
| S36-14 | Closeout hygiene | c-sharp-devops-engineer | 0.5 | S36-03+ | Smoke; replay 6/6 |
|        | **IN PROGRESS (S36-14)**: Devops closeout hygiene (c-sharp-devops-engineer). Pre-close: baseline confirmed 1204; replay 6/6; GitNexus indexed. Full smoke + replay verification + sprint-status update to follow parallel waves. Isolated hygiene track. Evidence will land in production/qa/ + agentic closeout. Sprint-status.yaml updated for mid sim. | | | | |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S36-08 | Sim P1 allocation follow-ups | team-simulation | 2 | S36-01 | Hash identical; replay-verify |
| S36-09 | tests/unit/ layout or ADR | c-sharp-devops-engineer | 1 | S36-01 | Layout or signed ADR |
|        | **COMPLETE (S36-09)**: hybrid layout documented in .claude/docs/directory-structure.md (co-located *Tests/ + regression/ retained per .NET/.sln; flat tests/unit/ deferred as hygiene-only per polish-boundary). No new ADR required — "or layout" satisfied. Isolated devops track. | | | | |
| S36-10 | Playtest session 8 | team-qa | 1 | S36-02 | Report in playtests/ |
|        | **COMPLETE (S36-10)**: Report appended to production/playtests/README.md (session 8 proxy + template from playtest-report skill). Focus: frame/graph/dispatching. qa-lead + qa-tester routed. | | | | |
| S36-11 | AD-ART-BIBLE sign-off facilitation | team-ui | 0.5 | S36-02 | Verdict in art-bible.md |
|        | **COMPLETE (S36-11)**: Sign-off facilitation note added to design/art/art-bible.md header. ui specialist (team-ui / ui-experience-lead) routed; lean ACCEPTED WITH CONDITIONS. | | | | |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S36-12 | Perf re-profile | perf-profile | 0.5 | S36-05, S36-08 | Appendix update |
| S36-13 | Dispatching-parallel-agents refinement | coordinator | 0.5 | S36-02 | Pattern updates + kickoff example |
|        | **COMPLETE (S36-13)**: Refinement notes + isolation contract + stack prefix added to sprint-36-parallel-kickoff-2026-07-06.md and .claude/docs/agent-coordination-map.md. devops/coordinator specialist. | | | | |
| S36-15 | Additional C2 polish | team-unity | 1 | S36-07 | Filters pass |

## Parallel Execution Waves & Tracks

**Prerequisites (serial):** S36-01, S36-02

**Wave 1 — Data Track (independent):** S36-03, S36-04

**Wave 2 — Unity/C2 Track (parallel to Data):** S36-05, S36-06, S36-07

**Wave 3 — Simulation Track:** S36-08

**Wave 4 — QA/DevOps/Hygiene Track:** S36-09, S36-10, S36-11, S36-13

**Closeout:** S36-14

## Carryover from S35
Unity C2 frame, live PNGs, tests/unit/, AD-ART-BIBLE, dep-graph runtime (S35-16)

## Risks
Graph invalidation, frame budget miss, QA plan delay, scope creep (mitigated by polish-boundary)

## Definition of Done
All Must Have; QA plan; smoke; replay 6/6; proxy 18/18; graph landed.

## Producer Feasibility Gate
PR-SPRINT skipped — Lean mode. Validated via parallel domain agents.

## QA Plan Gate
No QA plan yet for S36. Run /qa-plan sprint before implementation.

## Next Steps
1. Commit S35 (already on main per git).
2. /qa-plan sprint.
3. Parallel kickoff with dispatching-parallel-agents (one agent per track).
4. /story-readiness + /dev-story on tracks.
5. /sprint-status.
6. After S36, plan S37 for graph surfacing + deeper polish.

**Related:** Builds on S35-16 plan doc. Parallel tracks enable dispatching-parallel-agents.
