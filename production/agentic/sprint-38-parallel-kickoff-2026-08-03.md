# Sprint 38 Parallel Kickoff

**Date:** 2026-08-03 (post-S37 QA APPROVED + trunk advance)  
**Trunk:** `main` @ (post-S37 commit; TBD after S37 closeout) — **≥1215**, ReplayGolden 6/6  
**Sprint plan:** `production/sprints/sprint-38-polish-phase-3-art-bible-evidence-hygiene-wrap.md`  
**QA plan:** `production/qa/qa-plan-sprint-38-2026-08-03.md` (exists)  
**Authority:** polish-scope-boundary-2026-06-19.md — Phase 3 continuation (AD-ART-BIBLE sign-off, tests layout hygiene, live evidence, C2/Platform polish, perf, dispatching QA + closeout). Lean mode (`production/review-mode.txt` — PR-SPRINT skipped).  

## Sprint Goal
Polish Phase 3 wrap: complete AD-ART-BIBLE sign-off facilitation + UX polish, advance tests/unit + integration layout hygiene/CI, refresh live Editor PNG evidence, targeted C2/Platform additional polish, perf re-profile follow-up, dispatching QA integration + hygiene closeout. Maintain all determinism/replay/C2/proxy gates; prepare for gate-check or S39. Strictly inside polish-scope-boundary.

## Parallel Execution Model (dispatching-parallel-agents skill)
**Prerequisites (serial):** S38-01 and S38-02 **MUST complete before any feature dispatch**.

After prereqs:
- **Independent tracks run fully parallel** with no shared mutable state or cross-track file conflicts.
- **One sub-track (one focused agent) per domain** per the dispatching-parallel-agents skill.
- Art/UX (bible + polish), Hygiene (layout), C2/Polish, Perf/Evidence, QA/Process, Closeout are explicitly parallelizable after prereqs.
- Emphasis: parallel execution **after** S38-01 + S38-02 only. S38-10 refines dispatching QA.
- All work strictly enforces lean mode and polish-boundary compliance.

## Wave plan

| Wave | Stories | Track | Est. | Notes |
|------|---------|-------|------|-------|
| Day-1 | S38-01 | DevOps baseline | 1d | **READY** post-S37; blocks waves |
| W0 | S38-02 | QA plan | 1d | **BLOCKS** S38-03+ (exists) |
| W1 | S38-03 | **Art/UX** (bible sign-off + polish) | 1.5d | **Parallel after prereqs**; AD-ART-BIBLE sign-off + UX doc polish |
| W2 | S38-04, S38-07 | **C2/Polish** (parallel) | 2d / 1d | C2 + Platform Editor additional polish + Live Editor PNG refresh |
| W3 | S38-05 | **Hygiene** (layout + CI) | 1d | tests/unit + integration layout hygiene + CI verify |
| W4 | S38-06 | **Closeout** (DevOps) | 0.5d | Smoke + replay + GitNexus after waves |
| W5 | S38-08, S38-09 | **Perf/Playtest** (parallel) | 1d / 1d | Sim/Perf re-profile + Playtest session 10 |
| W6 | S38-10 | **QA/Process** (dispatching QA) | 0.5d | Dispatching QA integration + examples |
| W7 | S38-11 | **UX** (further polish) | 0.5d | Further UX/doc polish (nice) |

**Carryover from S37:** S37-14 (AD-ART-BIBLE) → S38-03; S37-11 layout → S38-05; S37-06/13 polish → S38-04/07; S37-12 perf → S38-08; S37-10 playtest → S38-09; S37-07 dispatching → S38-10.

**S38-02 status:** EXISTS (qa-plan generated). Blocks waves.

## Track ownership (one sub-track per domain)

| Track | Sub-track Owner | Stories | Stack prefix |
|-------|-----------------|---------|--------------|
| **Art/UX** | team-ui | S38-03, S38-11 | `stack/sprint38/art-ux-bible` |
| **C2/Polish** | team-unity (+ team-data) | S38-04, S38-07 | `stack/sprint38/c2-polish` |
| **Hygiene** | c-sharp-devops-engineer | S38-05 | `stack/sprint38/hygiene-layout` |
| **Closeout (DevOps)** | c-sharp-devops-engineer | S38-06 | `stack/sprint38/closeout` |
| **Perf/Playtest** | team-simulation + team-qa | S38-08, S38-09 | `stack/sprint38/perf-playtest` |
| **QA/Process** | coordinator + team-qa | S38-10 | `stack/sprint38/qa-process` |

**Dispatch rule:** One agent per sub-track/domain. Isolated context. S38-10 for dispatching QA.

## Hard gates (every merge)

- `dotnet test ProjectAegis.sln` — **≥1215**
- `ReplayGoldenSuiteTests` — **6/6**
- **ZERO** touch `DelegationBridge.cs`
- `CatalogWriteGate` **extend-only**
- C2 headless proxy **18/18+** (incl. Graph*)
- Production Baltic hash `17144800277401907079` **unchanged**
- `/replay-verify` on relevant (S38-08 etc.)
- All stories cite `polish-scope-boundary-2026-06-19.md` + S37
- Lean mode, headless primary, evidence in qa/evidence/ + playtests/

## Cut line

Prioritizing must-haves (S38-01 through S38-06 + closeout):

1. S38-11 (further UX)  
2. S38-10 (dispatching QA)  
3. S38-09 (playtest 10)  
4. S38-08 (perf re-profile)  
5. S38-07 (live PNG)  

**Minimum shippable (beyond must-have):** S38-03 art-bible + S38-05 layout + S38-06 closeout.

## Dispatch order (recommended) — dispatching-parallel-agents pattern

```bash
# ============================================================
# PREREQUISITES (serial — BLOCK parallel dispatch)
# ============================================================
# /story-readiness S38-01
# /dev-story dispatch S38-01

# /story-readiness S38-02
# /dev-story dispatch S38-02   # /qa-plan sprint (exists; must be merged before waves)

# ============================================================
# PARALLEL EXECUTION (after S38-01 + S38-02 only)
# One sub-track / one agent per domain per dispatching-parallel-agents skill
# Isolated context only: stories + sprint-plan excerpts + full hard gates + polish-boundary + qa-plan
# Emphasis: S38-10 refines dispatching QA (team-qa + coordinator for QA/Process track)
# ============================================================

# Art/UX sub-track
# /story-readiness S38-03
# /dev-story dispatch S38-03
# /story-readiness S38-11 (nice)
# /dev-story dispatch S38-11

# C2/Polish sub-track (parallel)
# /story-readiness S38-04
# /dev-story dispatch S38-04
# /story-readiness S38-07
# /dev-story dispatch S38-07

# Hygiene sub-track
# /story-readiness S38-05
# /dev-story dispatch S38-05

# Perf/Playtest sub-track (parallel)
# /story-readiness S38-08
# /dev-story dispatch S38-08
# /story-readiness S38-09
# /dev-story dispatch S38-09

# QA/Process (dispatching QA) sub-track [S38-10 refinement track]
# /story-readiness S38-10
# /dev-story dispatch S38-10   # coordinator + team-qa; validates dispatching QA examples + kickoff integration
# Optional inside QA track (lean): /team-qa sprint   # for QA sign-off + dispatching validation evidence

# ============================================================
# CLOSEOUT (after parallel waves reach ready state; S38-06)
# ============================================================
# /story-readiness S38-06
# /dev-story dispatch S38-06
```

## S38-10 Dispatching QA integration (coordinator + team-qa)
**Refinement focus (carry from S37-07):** 
- Isolated QA/Process track for dispatching integration stories.
- Validate: kickoff examples function, coordination-map updated, backward compat holds, lean parallel tracks green.
- Use `/team-qa sprint` (lean) inside or post-dispatch for this track to exercise QA orchestration in dispatching context.
- Evidence: updated examples here + `.claude/docs/agent-coordination-map.md`; checklist per qa-plan-sprint-38.
- Track prefix: `stack/sprint38/qa-process` (if used).
- Gates still enforced independently: replay/C2-proxy/Baltic/hash/1215+.

**Post-dispatch notes (for S38-10 + coordinator):**
- Validate S38-10 outputs: examples present in this kickoff + coordination-map; QA integration patterns documented.
- Confirm no breakage to prior kickoffs (S37/S36 patterns additive).
- All tracks (incl. QA/Process) must satisfy smoke + gates before S38-06 closeout.
- This kickoff + coordination-map S38-10 section serve as canonical example of dispatching QA refinements.
- Lean + boundary: production/review-mode.txt, polish-scope-boundary-2026-06-19.md + S37 cited.

**Notes on dispatch:**
- Do **not** dispatch W1–W7 until prereqs return success + updated baseline recorded (≥1215, ReplayGolden 6/6, GitNexus tip, C2 18/18+).
- Agents for each sub-track receive ONLY their track's stories, relevant ACs, gate list (replay 6/6, C2 18/18+, Baltic hash immutable, ZERO DelegationBridge, extend-only CatalogWriteGate, polish-boundary), sprint-plan + qa-plan excerpts. No cross-track history.
- Use `/story-readiness` per story before `/dev-story` inside a dispatched agent.
- Enforce lean mode (no PR-SPRINT) and polish-boundary on every story. No out-of-scope.
- S38-10 (dispatching QA) validated via this artifact + coordination-map + (optional) /team-qa run.

**All S37 gates carried forward.** QA plan (S38-02) exists. Enforce lean + polish-boundary. Production Baltic hash immutable.

**Sprint 38 ready for parallel dispatch after prereqs.**
