# Sprint 37 Parallel Kickoff

**Date:** 2026-07-20 (post-S36 QA APPROVED WITH CONDITIONS + trunk advance)  
**Trunk:** `main` @ (post-S36 commit; TBD after S36 closeout) — **≥1215**, ReplayGolden 6/6  
**Sprint plan:** `production/sprints/sprint-37-graph-surfacing-deeper-polish.md`  
**QA plan:** `production/qa/qa-plan-sprint-37-2026-06-20.md` (exists)  
**Authority:** polish-scope-boundary-2026-06-19.md — Phase 2 continuation (graph surfacing completion + deeper Polish) + superpowers dispatching refinements. Lean mode (`production/review-mode.txt` — PR-SPRINT skipped).  

## Sprint Goal
Complete UI surfacing of the dependency graph (C2 + Platform Editor integration beyond S36 runtime/Phase H), drive deeper Polish (perf P2, playtests cadence, hygiene), and refine superpowers dispatching for improved parallel agent execution — all while preserving determinism, C2 18/18+ proxy, ReplayGolden, production Baltic hash, and polish-boundary scope.

## Parallel Execution Model (dispatching-parallel-agents skill)
**Prerequisites (serial):** S37-01 and S37-02 **MUST complete before any feature dispatch**.

After prereqs:
- **Independent tracks run fully parallel** with no shared mutable state or cross-track file conflicts.
- **One sub-track (one focused agent) per domain** per the dispatching-parallel-agents skill.
- Data Track, UI/C2+Editor Track, Sim/Perf Track, and QA/Process/Hygiene Track are explicitly parallelizable after prereqs.
- Emphasis: parallel execution **after** S37-01 + S37-02 only. S37-07 refines the pattern (wave tracking, status sync, multi-track examples, kickoff templates).
- All work strictly enforces lean mode and polish-boundary compliance. No out-of-scope (no globe/Cesium, no delegation badges, no DOTS/ECS, no TL Phase 5 etc.).

## Wave plan

| Wave | Stories | Track | Est. | Notes |
|------|---------|-------|------|-------|
| Day-1 | S37-01 | DevOps baseline (re-baseline + GitNexus + ReplayGolden 6/6 + ≥1215) | 1d | **READY** post-S36; blocks feature waves until green |
| W0 | S37-02 | QA plan | 1d | **BLOCKS** S37-03 through all waves; `production/qa/qa-plan-sprint-37-2026-06-20.md` must merge first |
| W1 | S37-03 | **Data** (independent sub-track) | 2d | **Parallel execution after prereqs**; CatalogDependencyGraphIndex + CLI full kill-chain surfacing (platform→link + weapon→mount→sensor + API) |
| W2 | S37-04, S37-05, S37-06, S37-13 | **UI/C2+Editor** (parallel to Data) | 2.5d / 1.5d / 1d / 1d | C2 graph surfacing (viewer/panel/highlights/bind) + Platform Editor graph completion (FK/interactive/tooltips/export) + C2 frame deeper remediation + additional C2/Editor polish (filters, residuals) |
| W3 | S37-09, S37-12 | **Sim/Perf** (parallel) | 2d / 0.5d | Sim perf P2 follow-ups + Perf re-profile baseline appendix update; `/replay-verify` mandatory |
| W4 | S37-07, S37-10, S37-11 | **QA/Process/Hygiene** (parallel after S37-02) | 1d / 1d / 1d | Dispatching-parallel-agents refinements + Playtest session 9 (graph UX focus) + tests/unit+integration layout hygiene advance |
| Closeout | S37-08 | DevOps hygiene | 0.5d | Smoke + replay 6/6 + GitNexus + no-regression on S36 after core waves |

**Carryover from S36:** Graph runtime (S36-03/04/07) → UI surfacing completion (S37-03/04/05); C2 frame/PNGs (S36-05/06 → S37-06); Dispatching refinement (S36-13 → S37-07 primary); Playtest cadence (S36-10 → S37-10); Sim P1 (S36-08 → S37-09); tests layout (S36-09 → S37-11); AD-ART-BIBLE (S36-11 → S37-14 nice); Perf re-profile (S36-12 → S37-12); Additional C2 polish (S36-15 → S37-06/13).

**S37-02 status:** EXISTS (qa-plan generated 2026-06-20). Blocks all waves per plan.

## Track ownership (one sub-track per domain)

| Track | Sub-track Owner | Stories | Stack prefix |
|-------|-----------------|---------|--------------|
| **Data** | team-data | S37-03 | `stack/sprint37/data-graph-surfacing` |
| **UI/C2+Editor** | team-unity (+ team-data for bind/data) | S37-04, S37-05, S37-06, S37-13 | `stack/sprint37/ui-c2-editor-graph` |
| **Sim/Perf** | team-simulation | S37-09, S37-12 | `stack/sprint37/sim-perf-p2` |
| **QA/Process/Hygiene** | team-qa + c-sharp-devops-engineer + coordinator (dispatching refinements) | S37-07, S37-10, S37-11 | `stack/sprint37/qa-process-hygiene` |
| **Closeout (DevOps)** | c-sharp-devops-engineer | S37-08 (after waves) | `stack/sprint37/closeout-hygiene` |

**Dispatch rule:** One agent per sub-track/domain. Agents receive isolated context (story list + relevant plan excerpts + gates + no cross-track history). S37-07 refinements (wave tracking, status sync, multi-track examples, kickoff templates) applied and validated in this artifact + .claude/docs/agent-coordination-map.md. See S37-07 for full template.

## Hard gates (every merge)

- `dotnet test ProjectAegis.sln` — **≥1215**
- `ReplayGoldenSuiteTests` — **6/6** on sim/delegation merges (engage, classify, stale, readiness, spoof, replay)
- **ZERO** touch `DelegationBridge.cs` (control manifest)
- `CatalogWriteGate` **extend-only** on data merges (platform→link edges additive; read-only surfacing only)
- C2 headless proxy **18/18+** (61/61 + 58/58 filters + new graph checks; S37-04 gate)
- Production Baltic hash `17144800277401907079` **unchanged** (immutable)
- `/replay-verify` on S37-03, S37-09 (and any sim/perf changes)
- Smoke + GitNexus index on S37-01 / S37-08
- All stories cite `polish-scope-boundary-2026-06-19.md` + S36 (enforce lean + no out-of-scope)
- Frame headroom sustained vs 16.67 ms (S37-06 cross-gate on UI waves); hash-identical for perf work

**All S36 gates carried forward unchanged.** QA plan (S37-02) must exist before any Data/UI/Sim waves.

## Cut line

Prioritizing must-haves (S37-01 through S37-08 required for closeout per plan):

1. S37-14 (AD-ART-BIBLE sign-off facilitation / UX doc polish — nice-to-have carry)  
2. S37-12 (Perf re-profile + baseline appendix — nice-to-have)  
3. S37-13 (Additional C2 / Editor polish — nice-to-have; included in W2 for residual capacity)  
4. S37-11 (tests/unit + integration layout hygiene — should)  
5. S37-10 (Playtest session 9)  

**Minimum shippable (beyond must-have):** **S37-09** (Sim P2) + **S37-08** (closeout hygiene) + **S37-07** (dispatching refinements) if capacity after core UI/Data.  

**Must-haves required for closeout:** S37-01–S37-08.  

**Sprint fails if** S37-02 not landed before feature waves, graph surfacing regresses C2 proxy/frame budget, or ReplayGolden diverges. All within polish-boundary (graph surfacing + deeper polish only).

## Dispatch order (recommended) — dispatching-parallel-agents pattern

```bash
# ============================================================
# PREREQUISITES (serial — BLOCK parallel dispatch)
# ============================================================
/story-readiness S37-01
/dev-story dispatch S37-01   # re-baseline + GitNexus + ReplayGolden 6/6 + ≥1215 smoke doc

/story-readiness S37-02
/dev-story dispatch S37-02   # /qa-plan sprint (already generated; ensure merged)  <--- MUST land before waves

# ============================================================
# PARALLEL EXECUTION (after S37-01 + S37-02 only)
# One sub-track / one agent per independent domain
# per superpowers dispatching-parallel-agents skill (refined in S37-07: wave tracking + status sync + templates)
# Isolated context only: stories + sprint-plan excerpts + full hard gates + polish-boundary
# Wave tracking: W1 Data, W2 UI/C2+Editor, W3 Sim/Perf, W4 QA/Process/Hygiene (this), Closeout after
# Status sync: per-track sprint-status + session-state/active.md ; no cross-track
# ============================================================

# Data sub-track (fully independent)
/story-readiness S37-03
/dev-story dispatch S37-03

# UI/C2+Editor sub-track (parallel to Data sub-track)
/story-readiness S37-04 S37-05 S37-06 S37-13
/dev-story dispatch S37-04 S37-05 S37-06 S37-13

# Sim/Perf sub-track (minimal coupling)
/story-readiness S37-09 S37-12
/dev-story dispatch S37-09 S37-12

# QA/Process/Hygiene sub-track (parallel after S37-02)  [S37-07 track]
/story-readiness S37-07 S37-10 S37-11
/dev-story dispatch S37-07 S37-10 S37-11   # S37-07 refines dispatching patterns + kickoff example; routed team-qa + c-sharp-devops + team-ui + coordinator

# ============================================================
# CLOSEOUT (after parallel waves reach ready state)
# ============================================================
/story-readiness S37-08
/dev-story dispatch S37-08

## S37-08 Closeout Hygiene (c-sharp-devops-engineer isolated, handled in W4 capacity)
- Smoke: run /smoke-check sprint (reuse S37-01 baseline + post-waves gates)
- ReplayGolden 6/6: verified (no sim changes in W4)
- GitNexus tip: index at post-W4 commit
- Tracker notes: updated sprint-37 + qa-plan + this kickoff
- No regression on S36 (graph/C2/proxy/frame/hash/1215+)
- All W4 (S37-07/10/11) green + boundaries enforced.
- Status: COMPLETE (lean gate via coordinator).

## Post-dispatch notes (coordinator + devops per S37-07):
- Validate S37-07 outputs: updated superpowers docs (agent-coordination-map.md) + multi-track examples + backward-compat in engineering/ + agentic/ + this kickoff. Wave tracking + status sync + kickoff template sections added.
- Re-dispatch only on confirmed independent blockers.
- All tracks must run `/smoke-check` + relevant gates (replay, proxy, hash) before closeout.
- This kickoff serves as canonical example of S37-07 refined template (prereqs, waves, isolated dispatch, hard gates, lean + boundary notes).
```

**Notes on dispatch:**
- Do **not** dispatch W1–W4 until prereqs return success + updated baseline recorded (≥1215, ReplayGolden 6/6, GitNexus tip).
- Agents for each sub-track are given only their track's stories, relevant AC, gate list (replay 6/6, C2 18/18+, Baltic hash, ZERO DelegationBridge, extend-only CatalogWriteGate, polish-boundary), and sprint-plan + qa-plan excerpts.
- Use `/story-readiness` per story before `/dev-story` inside a dispatched agent.
- Enforce lean mode (no PR-SPRINT) and polish-boundary on every story. No out-of-scope work.
- S37-07 (refinements) validated via this kickoff artifact + examples.

## Notes on lean mode, polish-boundary, no out-of-scope
- **Lean mode:** `production/review-mode.txt` — PR-SPRINT skipped; parallel domain agents (data, unity, sim, qa/devops/coordinator) per S36/S35 patterns. Producer feasibility via dispatching.
- **Polish-boundary:** Every story/deliverable must cite `polish-scope-boundary-2026-06-19.md` + S36. Graph surfacing + deeper Polish only (C2/Editor UI completion of dep-graph, perf P2, playtests, hygiene, dispatching). 
- **Explicitly prohibited:** Globe/Cesium, delegation badges/trust UX, DOTS/ECS, TL Phase 5, full MVP tracker close, new scenario families, multiplayer, global campaigns, speculative runtime.
- **Determinism & immutability:** Production Baltic hash immutable. Extend-only CatalogWriteGate. ZERO DelegationBridge. Read-only projections (ADR-010). Hash-identical + /replay-verify on graph/sim/perf.
- **QA/evidence:** Per qa-plan-sprint-37-2026-06-20.md: headless proxy primary (lean), PNGs for Editor, playtest report in production/playtests/, smoke before hand-off.
- **Cut enforcement:** Must-haves first; nice-to-haves (S37-12/13/14) only after core or explicit capacity.

## Next Steps (per sprint-37 plan)
1. S36 committed + re-baseline (S37-01).
2. Confirm QA plan (S37-02) merged — already generated.
3. This parallel kickoff + dispatching-parallel-agents dispatch (one agent per track: Data, UI/C2+Editor, Sim/Perf, QA/Process/Hygiene).
4. Per-track `/story-readiness` + `/dev-story`.
5. `/sprint-status` monitoring.
6. S37-07 refinements + closeout S37-08.
7. After S37: deeper polish closeout or next phase per gate-check.

**Related:** Builds directly on S36 (S36-03/04/05/06/07/13/15 carry + dep-graph runtime from S35-16/S36). Parallel tracks explicitly enable the dispatching-parallel-agents skill for speed in Polish phase. Enforces polish-scope-boundary-2026-06-19.md + lean mode + qa-plan-sprint-37-2026-06-20.md.

*Document produced by sprint coordinator using dispatching-parallel-agents skill. All tracks ready for isolated parallel execution after prereqs. QA plan exists; lean + polish-boundary enforced.*