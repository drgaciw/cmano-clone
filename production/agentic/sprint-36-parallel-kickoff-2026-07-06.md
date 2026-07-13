# Sprint 36 Parallel Kickoff

**Date:** 2026-07-06 (post-S35 QA APPROVED WITH CONDITIONS + stage advance to Polish)  
**Trunk:** `main` @ `e60eadc` — **1204/1204**, ReplayGolden 6/6  
**Sprint plan:** `production/sprints/sprint-36-polish-phase-2-dependency-graph.md`  
**QA plan:** **MISSING** — **S36-02** blocks all feature waves

**Authority:** polish-scope-boundary-2026-06-19.md — Phase 2 continuation (dependency graph runtime + S35 carryover residuals)

## Sprint Goal
Polish Phase 2: land CatalogDependencyGraphIndex platform→link edges (S35-16 seed), close residual carryovers (Unity C2 frame budget remediation, live PNG re-capture, tests/unit/ hygiene or ADR, AD-ART-BIBLE), execute parallel domain tracks, preserve all determinism/replay/C2/proxy gates.

## Parallel Execution Model (dispatching-parallel-agents skill)
**Prerequisites (serial):** S36-01 and S36-02 **MUST complete before any feature dispatch**.

After prereqs:
- **Independent tracks run fully parallel** with no shared mutable state or cross-track file conflicts.
- **One sub-track (one focused agent) per domain** per the dispatching-parallel-agents skill.
- Data Track and Unity/C2 Track are explicitly parallelizable.
- Simulation and QA/DevOps/Hygiene tracks have minimal overlap and can advance concurrently where deps allow.
- Emphasis: parallel execution **after** S36-01 + S36-02 only.

## Wave plan

| Wave | Stories | Track | Est. | Notes |
|------|---------|-------|------|-------|
| Day-1 | S36-01 | DevOps baseline | 1d | **READY** — S35 complete @ `e60eadc`; GitNexus + ReplayGolden 6/6 |
| W0 | S36-02 | QA plan | 1d | **BLOCKS** S36-03 through S36-15; run `/qa-plan sprint` |
| W1 | S36-03, S36-04 | **Data** (independent sub-track) | 2.5d / 1d | **Parallel execution after prereqs**; CatalogDependencyGraphIndex platform→link + CLI/golden |
| W2 | S36-05, S36-06, S36-07 | **Unity** (parallel to Data) | 2d / 1.5d / 1.5d | C2 frame capture+remediation + Live Editor PNGs + Phase H link surfacing (read-only) |
| W3 | S36-08 | **Simulation** | 2d | P1 allocation follow-ups; `/replay-verify` mandatory |
| W4 | S36-09, S36-10, S36-11, S36-13 | **QA/DevOps/Hygiene** | 1d / 1d / 0.5d / 0.5d | Layout/ADR + Playtest 8 + ART-BIBLE + dispatching pattern refinement; parallel after S36-02 |
| Closeout | S36-14 | DevOps hygiene | 0.5d | Smoke + replay 6/6 after core waves |

**Carryover from S35:** Unity C2 frame, live PNGs, tests/unit/ layout, AD-ART-BIBLE sign-off, dep-graph runtime (S35-16).
**S36-09 status (QA/DevOps/Hygiene isolated):** COMPLETE — hybrid tests layout documented (directory-structure.md); devops specialist (c-sharp-devops-engineer) routed. (See sprint-36 + polish-boundary updates.)
**S36-10 status (QA/DevOps/Hygiene isolated):** COMPLETE — Playtest session 8 report in playtests/README.md (qa specialist / team-qa / qa-tester). Proxy validated gates.
**S36-11 status (QA/DevOps/Hygiene isolated):** COMPLETE — AD-ART-BIBLE sign-off note facilitated in design/art/art-bible.md (ui specialist / team-ui / ui-experience-lead). Lean verdict noted.
**S36-13 status (QA/DevOps/Hygiene isolated):** COMPLETE — Dispatching refinement documented (kickoff + coordination-map). Pattern locked for isolated tracks. (coordinator/devops)
**S36-14 status (QA/DevOps/Hygiene isolated):** IN PROGRESS (c-sharp-devops-engineer) — Closeout hygiene prep (smoke/replay 6/6 baseline hold; full close after waves). sprint-status.yaml mid-sim updated. See sprint-36.

## Track ownership (one sub-track per domain)

| Track | Sub-track Owner | Stories | Stack prefix |
|-------|-----------------|---------|--------------|
| **Data** | team-data | S36-03, S36-04 | `stack/sprint36/data-dependency-graph` |
| **Unity** | team-unity | S36-05, S36-06, S36-07 | `stack/sprint36/unity-c2-frame-polish` |
| **Simulation** | team-simulation | S36-08 | `stack/sprint36/sim-p1-followups` |
| **QA/DevOps/Hygiene** | team-qa + c-sharp-devops-engineer + team-ui | S36-09, S36-10, S36-11, S36-13, S36-14 | `stack/sprint36/qa-devops-hygiene-closeout` |

**Dispatch rule:** One agent per sub-track/domain. Agents receive isolated context (story list + relevant plan excerpts + gates + no cross-track history).

## Hard gates (every merge)

- `dotnet test ProjectAegis.sln` — ≥**1204**
- `ReplayGoldenSuiteTests` — **6/6** on sim/delegation merges
- ZERO touch `DelegationBridge.cs`
- `CatalogWriteGate` extend-only on data merges (platform→link edges are additive)
- C2 headless proxy **18/18** (61/61 + 58/58 filters) — S36-05 gate
- Production Baltic hash `17144800277401907079` unchanged
- `/replay-verify` on S36-08 (and any sim P1 allocation changes)
- Smoke + GitNexus index on S36-01 / S36-14

**All S35 gates carried forward unchanged.**

## Cut line

1. S36-15 (Additional C2 polish — nice-to-have)  
2. S36-12 (Perf re-profile — nice-to-have)  
3. S36-13 (Dispatching-parallel-agents refinement — nice-to-have)  
4. S36-11 (AD-ART-BIBLE sign-off facilitation)  
5. S36-09 (tests/unit/ layout or ADR)  

**Minimum shippable (beyond must-have):** **S36-08** (Sim P1) + **S36-14** (closeout hygiene) + **S36-10** (playtest) if capacity after core.

**Must-haves required for closeout:** S36-01–S36-07 + S36-14.

## Dispatch order (recommended) — dispatching-parallel-agents pattern

```bash
# ============================================================
# PREREQUISITES (serial — BLOCK parallel dispatch)
# ============================================================
/dev-story dispatch S36-01
/dev-story dispatch S36-02    # /qa-plan sprint   <--- MUST land before waves

# ============================================================
# PARALLEL EXECUTION (after S36-01 + S36-02 only)
# One sub-track / one agent per independent domain
# per superpowers dispatching-parallel-agents skill
# ============================================================

# Data sub-track (fully independent)
/dev-story dispatch S36-03 S36-04

# Unity/C2 sub-track (parallel to Data sub-track)
/dev-story dispatch S36-05 S36-06 S36-07

# Simulation sub-track (minimal coupling)
/dev-story dispatch S36-08

# QA/DevOps/Hygiene sub-track (parallel after S36-02)
/dev-story dispatch S36-09 S36-10 S36-11
/dev-story dispatch S36-13   # nice-to-have refinement

## S36-13 Refinement (coordinator + devops)
Pattern updates per S36-13:
- Explicit isolation contract: "Agents receive isolated context (story list + relevant plan excerpts + gates + no cross-track history)."
- Stack prefix convention for hygiene track: `stack/sprint36/qa-devops-hygiene-closeout`
- Dispatch example locked above. See .claude/docs/agent-coordination-map.md for full pattern. (Nice-to-have complete.)

# ============================================================
# CLOSEOUT (after parallel waves reach ready state)
# ============================================================
/dev-story dispatch S36-14
```

**Notes on dispatch:**
- Do **not** dispatch W1–W4 until prereqs return success + updated baseline recorded.
- Agents for each sub-track are given only their track's stories, relevant AC, gate list, and sprint-plan excerpts.
- Use `/story-readiness` per story before `/dev-story` inside a dispatched agent.
- Re-dispatch only on confirmed independent blockers.

## Next Steps (per sprint-36 plan)
1. S35 already committed to main.
2. `/qa-plan sprint` (S36-02).
3. This parallel kickoff + dispatching-parallel-agents dispatch (one agent per track).
4. Per-track `/story-readiness` + `/dev-story`.
5. `/sprint-status` monitoring.
6. After S36: plan S37 for deeper graph surfacing + polish.

**Related:** Builds directly on S35-16 dependency-graph plan-only seed. Parallel tracks explicitly enable the dispatching-parallel-agents skill for speed in Polish phase.

*Document produced by sprint coordinator using dispatching-parallel-agents skill. All tracks ready for isolated parallel execution after prereqs.*