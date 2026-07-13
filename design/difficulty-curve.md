# Difficulty Curve — Baltic Scenario Bands

> **Status:** Committed — Sprint 35 (S35-03)  
> **Version:** 1.0  
> **Last Updated:** 2026-06-19  
> **Scope:** Polish Phase 1 Baltic vertical slice — player-facing difficulty communication, not harness tuning  
> **Baseline pin:** `data/scenarios/baltic-patrol.policy.json` — world hash `17144800277401907079` (unchanged)  
> **Validation:** [fun-hypothesis-validation-2026-06-19.md](../production/playtests/fun-hypothesis-validation-2026-06-19.md) — **VALIDATED WITH NOTES**  
> **Playtest corpus:** `production/playtests/` + `production/playtests/human/` @ 2026-06-19

---

## 1. Purpose

Map Baltic scenarios and isolated fixtures to **difficulty bands** so players, facilitators, and QA share vocabulary for NPE onboarding, mid-game systems depth, and comms stress. Engineering isolation (ReplayGolden, golden hashes) is the evidence layer; this doc is the **player-facing intent** layer called out as missing in playtests.

**Not in scope:** Balance retuning of `DatalinkShareLagResolver` formulas; new scenario families; delegation badge difficulty (Polish Phase 1 OUT).

---

## 2. Difficulty Band Model

Three bands cover the current Baltic slice. Numeric labels are for design/QA — not yet shown in-game (P1 tooltip pass).

| Band | Label | Target player | Cognitive load | Session length |
|------|-------|---------------|----------------|----------------|
| **Band A** | Introductory | NPE / first theater command | Low–medium | 10–15 min |
| **Band B** | Standard | Returning commander; mid-game systems | Medium | 20–30 min |
| **Band C** | Advanced | EW/comms curator; stress isolates | Medium–high | 15–25 min per fixture |

---

## 3. Scenario → Band Mapping

### Band A — Introductory (NPE)

| Scenario / path | Role | Difficulty signals | Playtest rating |
|-----------------|------|-------------------|-----------------|
| `baltic-patrol` (production) | Default first scenario — patrol scope, bounded contacts | Single-side picture; nominal comms; ReplayGolden stable | **Just Right (inferred)** for analytical audience |
| `baltic-patrol-classify` | NPE classify loop — map/OOB/contact sync tutorial surrogate | Hostile `◆` present; classification FSM; no comms deny in first 5 min | **Just Right (harness)**; human NPE **PASS WITH NOTES** |

**Recommended NPE path:** `baltic-patrol-classify` → production `baltic-patrol` short run → optional `baltic-patrol-comms` intro to degrade (facilitated).

**Gaps (from playtests):** No first-run tutorial; FUEL line non-obvious; COMMS legend missing ([npe-baltic-c2-thinkaloud](../../production/playtests/human/playtest-2026-06-19-npe-baltic-c2-thinkaloud.md)).

### Band B — Standard (Mid-game catalog & delegation context)

| Scenario / surface | Role | Difficulty signals | Playtest rating |
|--------------------|------|-------------------|-----------------|
| `baltic-patrol-mission-roe` | Doctrine / ROE overrides mid-fight | Policy inheritance panel; mission-scoped ROE | **Just Right** (proxy) |
| Platform Editor Phases C–H | Curator workflow — catalog browse + import staging | Three sections (damage, comms, link); propose→ack→approve gate | **Just Right** for power users; **PASS WITH NOTES** |
| `baltic-patrol-comms` | Comms degrade in C2 (not editor) | DEGRADED → DENIED; symbol opacity ladder; engage denial | **Standard+** — operator rated Standard+ when combined with Band A literacy |
| Begin Execution transition | Planning → Executing chrome | Score frozen until execution; time compression | Test-proven; medium menu depth on Attack |

**Mid-game session focus:** Catalog round-trip, doctrine panel, begin execution — checks 14–18 ([midgame-delegation-catalog-thinkaloud](../../production/playtests/human/playtest-2026-06-19-midgame-delegation-catalog-thinkaloud.md)).

**Pillar gap:** Agentic delegation / trust signals **not validated** in human session (check 13 miss) — backend exists; C2 UX deferred per [fun-hypothesis-validation-2026-06-19.md](../production/playtests/fun-hypothesis-validation-2026-06-19.md).

### Band C — Advanced (Comms degraded / denied & isolates)

| Fixture | Role | Difficulty signals | Tier (operator informal) |
|---------|------|-------------------|--------------------------|
| `baltic-patrol-comms` | Player-facing comms deny | All symbols 35% at DENIED; policy denial in log; attack blocked | **Standard+** |
| `baltic-patrol-datalink-comms` | Harness isolate — comms gate, `shareLagTicks: 0` | Preserves production golden; stress datalink without catalog lag | **Standard** (engineering) |
| `baltic-patrol-datalink-catalog-latency` | Catalog-derived 3-tick share lag (S34-07) | `LatencyMsNominal` → lag; golden `12661701758887629394` | **Hard** |
| `baltic-patrol-readiness` | Engage gate — `AIR_NOT_READY` | Every engage logs denial; recovery = readiness workflow | **Hard** (communication) |
| `baltic-patrol-spoof` | EW spoof stress | Contact picture integrity challenged | **Hard** (EW-curious) |
| `baltic-patrol-jammed` | Jammed environment variant | Sensor/contact FSM stress | **Hard** |
| `baltic-patrol-combat-domains` | Multi-domain damage hot path | Combat domains enabled; bounded hot-tick | **Advanced** (backend confidence) |

**Rule:** Isolated fixtures must **not** alter production pin hash `17144800277401907079` ([polish-scope-boundary-2026-06-19.md](../production/polish-scope-boundary-2026-06-19.md)).

---

## 4. Telegraph → Fail → Recover Loops

Derived from facilitated playtests @ 2026-06-19. Each loop names what the player **should see**, what **failure feels like**, and how they **recover**.

### Loop 1 — Contact classification (Band A)

| Phase | Telegraph | Fail state | Recover |
|-------|-----------|------------|---------|
| See hostile | `◆` on map + CONTACT line in log | Player misses contact — no OOB sync | Facilitator: click OOB or map `■`/`◆` |
| Classify | CONTACTS tab + contact summary | Ambiguous identity confusion | Read CONTACT log category; check sensor strip |
| Engage (later) | Attack menu → Fire Single | Policy denial if ROE blocks | Read message log `POLICY_DENIAL` row |

**Playtest:** Checks 5–6, 13 PASS; onboarding copy gap non-blocking.

### Loop 2 — COMMS degrade / deny (Band A → C)

| Phase | Telegraph | Fail state | Recover |
|-------|-----------|------------|---------|
| Degrade | Top bar `COMMS: DEGRADED` amber; hostile 55% + ghost `◌` | Player trusts stale ◆ as live | Learn ghost = lag duplicate; friendly tracks stay bright |
| Deny | Top bar `COMMS: DENIED` red; all symbols 35% | Player issues engage — blocked | Read log policy denial; wait for nominal or accept isolation |
| Order block | `Comms_denied_appends_policy_denial_*` | Frustration without "why" | **P1:** HUD one-liner + legend ([difficulty-baltic-scenarios-thinkaloud](../../production/playtests/human/playtest-2026-06-19-difficulty-baltic-scenarios-thinkaloud.md)) |

**Playtest:** Operator predicted deny correctly; legend/tooltip top Polish request.

### Loop 3 — Catalog lag attribution (Band B → C)

| Phase | Telegraph | Fail state | Recover |
|-------|-----------|------------|---------|
| Edit link latency | `LINK row=` staging diff; `LatencyMsNominal` column in viewer | Player doesn't connect edit → sim lag | Approve → run `baltic-patrol-datalink-catalog-latency` fixture |
| In-fight lag | Side picture shares arrive late (3-tick) | Planning on stale shared picture | **P1:** debug overlay lag source (catalog vs policy) |
| Golden safety | Default Baltic hash unchanged | Accidental production corruption | Use isolated fixture only |

**Playtest:** Operator understood cause-effect **only with facilitator** — in-game surfacing absent.

### Loop 4 — Readiness / engage gate (Band C)

| Phase | Telegraph | Fail state | Recover |
|-------|-----------|------------|---------|
| Pre-engage | Unit detail readiness line (subtle) | `AIR_NOT_READY` denial loop | Player sees log denial but not fix path |
| Denial | Message log `POLICY_DENIAL` / readiness category | Repeated failed engage attempts | **P1:** quick-design strings for readiness recovery steps |
| Recovery | Readiness flips true in scenario timeline | — | Re-issue engage |

**Playtest:** "I'd see a denial log line but not know how to fix readiness" ([difficulty-baltic-scenarios](../../production/playtests/playtest-2026-06-19-difficulty-baltic-scenarios.md)).

### Loop 5 — Import staging gate (Band B)

| Phase | Telegraph | Fail state | Recover |
|-------|-----------|------------|---------|
| Propose | Diff list populates; monospace deltas | User approves without reading | Acknowledge gate blocks Approve |
| Validation fail | `STAGING: blocked by validation errors` | Blocked with opaque FK error | Fix workbook → re-Propose |
| Approve | Readback matches workbook | Sim behavior surprise | Trace `LINK`/`COMMS`/`DAMAGE` prefix to effect table (§3) |

**Playtest:** Curator persona PASS; three parallel catalog sections potentially overwhelming.

### Loop 6 — Deterministic replay trust (all bands)

| Phase | Telegraph | Fail state | Recover |
|-------|-----------|------------|---------|
| Post-fight | ReplayGolden 6/6 available | Player disputes outcome | Re-run seeded replay; compare order log |
| AAR | Message log categories tinted | Missing log line | Scrub to `sequenceId` (P1) |

**Fun hypothesis:** Determinism pillar **Strong**; supports analytical player fantasy ([fun-hypothesis-validation-2026-06-19.md](../production/playtests/fun-hypothesis-validation-2026-06-19.md)).

---

## 5. Progression Recommendation

```
Band A (NPE)
  baltic-patrol-classify
       ↓
  baltic-patrol (production baseline)
       ↓
Band B (mid-game)
  Platform Editor tour (catalog viewer → import staging)
  baltic-patrol-mission-roe + Begin Execution practice
       ↓
Band C (optional mastery)
  baltic-patrol-comms (player-facing)
  baltic-patrol-datalink-catalog-latency (curator + sim)
  baltic-patrol-readiness / spoof / jammed (isolates)
```

**Mastery signal:** Player completes production Baltic loop with comms degrade understood **without facilitator** — not yet validated (Editor re-capture recommended).

---

## 6. Difficulty Communication Backlog (P1 Polish)

Prioritized from playtest action items; **not S35-03 implementation**:

| Priority | Item | Source |
|----------|------|--------|
| P0 | COMMS state legend (nominal / degraded / denied symbology) | NPE + difficulty think-aloud |
| P0 | HUD one-liner on order denial (`why` comms / ROE / readiness) | Difficulty think-aloud |
| P1 | Lag source attribution overlay (catalog vs policy) | Difficulty proxy + S34-07 |
| P1 | Readiness recovery copy (`AIR_NOT_READY` → steps) | Difficulty proxy |
| P2 | In-game difficulty band label on scenario select | This doc |

---

## 7. Quantitative Anchors (engineering)

| Metric | Value | Role in curve |
|--------|-------|---------------|
| ReplayGolden suite | **6/6 PASS** | Band A–C regression stability |
| Production world hash | `17144800277401907079` | Band A baseline unchanged |
| Catalog-latency golden | `12661701758887629394` | Band C isolate pin |
| `Datalink\|ShareLag` tests | **26/26 PASS** | Lag mechanics verified |
| Human sessions | **3/3 PASS WITH NOTES** | Bands inferred; not live Editor |

---

## 8. Cross-References

| Doc | Link |
|-----|------|
| Fun hypothesis validation | [fun-hypothesis-validation-2026-06-19.md](../production/playtests/fun-hypothesis-validation-2026-06-19.md) |
| NPE playtest | [playtest-2026-06-19-npe-baltic-c2.md](../production/playtests/playtest-2026-06-19-npe-baltic-c2.md) |
| Mid-game playtest | [playtest-2026-06-19-midgame-delegation-catalog.md](../production/playtests/playtest-2026-06-19-midgame-delegation-catalog.md) |
| Difficulty playtest | [playtest-2026-06-19-difficulty-baltic-scenarios.md](../production/playtests/playtest-2026-06-19-difficulty-baltic-scenarios.md) |
| C2 comms UX | [c2-map-placeholder.md](ux/c2-map-placeholder.md), [interaction-patterns.md](ux/interaction-patterns.md) |
| Polish scope | [polish-scope-boundary-2026-06-19.md](../production/polish-scope-boundary-2026-06-19.md) |

---

## 9. Acceptance (S35-03)

- [x] Baltic scenario bands defined (NPE, mid-game, comms degraded/denied)
- [x] Telegraph / fail / recover loops documented from playtest corpus
- [x] References fun-hypothesis-validation-2026-06-19.md
- [x] Closes gate r2 "Confusion loops / difficulty curve" **PARTIAL** → committed design doc