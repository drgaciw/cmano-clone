# Sprint 115 — Attention Queue + Auto-Pause Spine (PRD P0-6/P0-7 minimal)

**Dates:** 2026-08-11 → est. 4–6 days  
**Program:** Post–Release Product Progress (S110–S114 complete engineering; human ack optional parallel)  
**Stage:** **Release** · **Not Launch**  
**Predecessor:** S112 clock controls; S114 gate engineering COMPLETE  
**Authority:** `docs/product/prd-mvp-ui-2026-07-15.md` P0-6/P0-7 · `production/agentic/agentic-workflow-sprint-series-2026-08-09.md` · AGENTS.md  
**QA:** `production/qa/qa-plan-sprint-115-attention-autopause-2026-08-11.md`  
**Kickoff:** `production/agentic/sprint-115-parallel-kickoff-2026-08-11.md`

## Decision (producer / architecture)

After S114, the highest-leverage Release product step is **not** another asset wave or Phase N.  
S112 delivered player-controlled pause/accel; MVP still lacks **why** the sim paused and an **actionable attention queue**.  
S115 ships a **headless spine** only — projection + deterministic pause-class events + tests. Unity chrome is out of scope.

**Naming (PRD OQ-4):** Do **not** overload `Delegation/Attention/` (AI cognitive-load model). New types live under a distinct namespace, e.g. `ProjectAegis.Delegation.Watch` or `...Projection.WatchAttention*`.

## Goal

1. **Pause-class events** fire on first detection of hostile/unknown contact and own-side loss/battle-damage (classic rule), carrying stable event ID, priority, subject, trigger tick, and optional raid/formation grouping key.  
2. **Auto-pause** sets `SimClock`/`SimulationSession` pause with a visible reason code (headless string enum / reason record).  
3. **WatchAttentionQueueProjection** exposes an ordered, filterable queue (priority → tick → event ID); acknowledge/dismiss are presentation-only and restorable.  
4. **Precedence retained:** pause > forced-1x > player compression (S112). Resume path: clear unresolved pause-class cards **or** explicit override flag (test both).

## Tracks (surface-disjoint)

| Track | Story | Surface | Notes |
|-------|-------|---------|-------|
| A Event + pause wire | S115-01 | `src/ProjectAegis.Sim/**` (event bus / detection hooks minimal) + pure models if shared | Prefer pure records in Delegation if sim only emits facts |
| B Watch queue projection | S115-02 | `src/ProjectAegis.Delegation/Projection/` **new** files only (no Bridge hotpath) | Queue + reason snapshot |
| C Session resume + tests | S115-03 | `SimulationSession*` thin API + `*.Tests` | ClockControls extension; ReplayGolden 6/6 |

**Hub rule:** No uncontrolled `DelegationBridge` hotpath edits. CatalogWriteGate untouched. Baltic hash immutable.

## Must Have

| ID | AC |
|----|-----|
| S115-01 | Deterministic emission of ≥2 pause-class kinds: `HostileOrUnknownContact`, `OwnSideLossOrDamage` with stable IDs |
| S115-02 | `WatchAttentionQueueProjection` returns ordered cards; ack/dismiss presentation-only |
| S115-03 | Auto-pause invokes existing Pause path; HeadlessBatch still overrides pause for CI |
| S115-04 | Tests: projection + session/clock; ReplayGolden **6/6**; suite 0 failed on touched filters |
| S115-05 | Smoke + residual list; stage **Release** |

## Should Have

| ID | AC |
|----|-----|
| S115-06 | Grouping key on events (data only; grouping UI is P1) |
| S115-07 | Explicit resume-override API for unresolved cards |

## Non-goals

- Full Unity attention panel chrome / UXML  
- Weapons-release forced 1× (PRD P0-8) — residual  
- Authority handoff (P0-3), message-log author fields (P0-4)  
- Launch · Phase N · invent Approved assets · Addressables  

## Hard gates

| Gate | Criterion |
|------|-----------|
| Stage | Release |
| Bridge | ZERO hotpath unless unavoidable + playbook |
| Replay | 6/6 |
| Hash | `17144800277401907079` preserved |
| Suite | Touched filters green; full suite cite CI if docs-only residual |

## Definition of Done

- [ ] Must Have complete with smoke  
- [ ] Linear children Done (if opened)  
- [ ] Residual: P0-8, Unity panel, full priority taxonomy polish  
