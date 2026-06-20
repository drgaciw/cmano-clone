# Sprint 39 Parallel Kickoff — Deeper Polish (C2/Platform / Hygiene / Perf / Evidence)

**Date:** 2026-06-20 (post-S38 COMPLETE + retro + gate)  
**Trunk:** `main` @ (post-S38) — **≥1213**, ReplayGolden 6/6, C2 18/18+  
**Sprint plan:** `production/sprints/sprint-39-deeper-polish-c2-platform-hygiene.md`  
**QA plan:** `production/qa/qa-plan-sprint-39-2026-06-20.md` (S39-02 complete)  
**Authority:** polish-scope-boundary-2026-06-19.md — **Deeper Polish only**. Lean (`production/review-mode.txt`). No globe/badges/DOTS/full MVP. ZERO DelegationBridge. Extend-only CatalogWriteGate. Immutable Baltic hash. Headless primary. C2 proxy + Replay 6/6 mandatory.

## Sprint Goal (recap)
Deeper polish iteration: targeted C2 + Platform Editor polish (density/tooltips/surfacing), tests/hygiene/CI/docs refinement, perf P1 + replay/determinism maintenance, evidence/PNG + playtest, dispatching-parallel-agents refinements. Maintain all gates. Prepare continuation in Polish.

## Parallel Execution Model (dispatching-parallel-agents skill)
**Prerequisites (serial, blocking):** 
- S39-01 (baseline) + S39-02 (QA plan) **MUST complete before ANY feature dispatch**.
- QA plan required pre-waves (per sprint-plan + qa-plan skill).

After prereqs:
- **Independent tracks run fully parallel** — no shared mutable state or cross-track file edits.
- **One sub-track (one focused agent) per domain**.
- Use `/story-readiness` + `/dev-story` (or equivalent lean) per track with isolated context + boundary cites.
- Dispatch order below. All work enforces lean + boundary.

**Emphasis:** Parallel speed on polish domains while preserving determinism, proxy, hash, and scope.

## Wave plan

| Wave | Stories | Track | Est. | Notes |
|------|---------|-------|------|-------|
| Day-1 | S39-01 | DevOps baseline | 1d | **READY** post-S38; blocks all |
| W0 | S39-02 | QA plan | 1d | **BLOCKS** feature waves |
| W1 | S39-03 | **C2/Platform** (deeper polish) | 2d | Density, tooltips, surfacing; evidence |
| W2 | S39-04 | **Hygiene** (layout + CI + docs) | 1d | Hybrid retained; directory + coordination updates |
| W3 | S39-05 | **Perf / Replay** | 1.5d | P1 deltas + replay maint (isolated fixtures) |
| W4 | S39-07 | **Evidence / Playtest** (parallel with above) | 1.5d | PNG refresh + playtest-11; think-aloud |
| W5 | S39-08 / S39-09 | **Dispatching + Art/UX residual** | 0.5d | Kickoff/docs refinements + minimal UX cross-refs |
| W6 | S39-06 | **Closeout** (DevOps) | 0.5d | Smoke + replay + proxy + GitNexus + status |

**Carryover from S38:** C2/Platform polish (S38-04) → S39-03; Hygiene (S38-05) → S39-04; Perf (S38-08) → S39-05; Evidence/playtest (S38-07/09) → S39-07; Dispatching (S38-10) → S39-08.

## Track ownership (one sub-track per domain)

| Track | Sub-track Owner | Stories | Stack prefix (suggested) |
|-------|-----------------|---------|--------------------------|
| **C2/Platform polish** | team-unity + team-data | S39-03 | `stack/sprint39/c2-platform-deeper` |
| **Hygiene / layout / CI / docs** | c-sharp-devops-engineer | S39-04 | `stack/sprint39/hygiene-ci` |
| **Perf / Replay / Determinism** | team-simulation | S39-05 | `stack/sprint39/perf-replay` |
| **Evidence / Playtest** | team-qa + team-unity | S39-07 | `stack/sprint39/evidence-playtest` |
| **Dispatching QA + Art/UX residual** | coordinator + team-ui | S39-08/09 | `stack/sprint39/dispatch-ux` |
| **Closeout (DevOps)** | c-sharp-devops-engineer | S39-06 | `stack/sprint39/closeout` |

**Dispatch rule:** One agent per sub-track/domain. **Isolated context only**. Every prompt must cite:
- polish-scope-boundary-2026-06-19.md
- S38 plan + retro
- Hard gates (replay/proxy/DelegationBridge ZERO / hash / extend-only)

## Hard gates (every merge + closeout)

- `dotnet test ProjectAegis.sln` — **≥ prior baseline (1213+)**
- `ReplayGoldenSuiteTests` — **6/6**
- **ZERO** touch `DelegationBridge.cs`
- `CatalogWriteGate` **extend-only**
- C2 headless proxy **18/18+** (incl. Graph* filters)
- Production Baltic hash `17144800277401907079` **unchanged**
- GitNexus tip recorded
- Boundary cite in every changed file / doc header (lean)

## Prerequisites (bash-style dispatch order)

```bash
# 1. Serial prereqs
# /sprint-plan update or confirm S39
# /qa-plan sprint 39   # blocks waves

# 2. Parallel dispatch (example agents)
# Use isolated prompts + story-readiness + dev-story per track.
# Example:
# spawn C2/Platform agent: "Implement S39-03 per sprint-39 plan + boundary. Isolated. ..."
# spawn Hygiene agent: ...
# spawn Perf/Replay agent: ...
# etc.

# After parallel: 
# /smoke-check
# /team-qa + sign-off
# /sprint-status
# /story-done per track
# Update session-state + sprint-status
# /retrospective
```

## Cut line / minimum

Must ship: baseline + QA plan + C2/Platform polish + hygiene + perf/replay + closeout (S39-01-06).

## Verification at end of session

- All tracks report COMPLETE via story-done equivalent.
- Smoke + gates green.
- sprint-status.yaml + session-state/active.md updated.
- Boundary + lean + superpowers compliance verified.

---

*Created per dispatching-parallel-agents + S39 plan (deeper polish only) + S38 retrospective. Enforces all boundary constraints.*