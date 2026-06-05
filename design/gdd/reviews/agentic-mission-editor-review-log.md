# Review Log: Agentic Mission & Scenario Editor

## Review — 2026-05-30 — Verdict: MAJOR REVISION NEEDED (then revised)
Scope signal: L
Specialists: systems-designer, game-designer, qa-lead, ai-programmer, creative-director
Blocking items: 7 | Recommended: ~10
Summary: Strikingly convergent adversarial review. Root cross-cutting issue — aspirational "agent" vocabulary the v1 reality (validation-only) couldn't support, in tension with the determinism pillar. Blockers: (1) Validation Agent misnomer → deterministic engine; (2) §2 fantasy promised drafting staff, v1 ships a linter; (3) §4.1 fuel formula broken (dead RT param, one-way/round-trip ambiguity, no input validation); (4) determinism asserted but AC-2 world-state hash/fire_order undefined + no sim isolation spec; (5) 4 untestable ACs (AC-2/4/6/7); (6) contradictory concurrency spec (schemaVersion reuse + last-write-wins vs conflict-reject); (7) §3.3 invariant self-violated by editorState validation cache. Bones judged excellent — tightening pass, not redesign.

### Revision applied same day (decisions: Option A rescope; round-trip DB convention)
- Validation **Engine** (deterministic, no LLM); "agent" reserved for Phase 2/3.
- §2 split into v1 fantasy + Phase 2/3 north star.
- §4.1 rewritten on round-trip combat radius; RT removed; input validation added; worked example fixed.
- Determinism contract: world-state hash (SHA-256), fire_order array, same-tick semantics, scenario_simulate_sample isolation.
- Concurrency: metadata.editVersion + conflict-reject + recovery path.
- §3.3 enforcement: no cached validation; CI schema-lint (AC-9).
- Added: §3.8 map-interaction contract, unit-state triggers, tick-density warning, §4.3 variable table, system 20 dep, data-driven knobs path, TeleportUnit logged transform, AC-1..AC-12 rewritten testable, AC-3 all 6 rules.

Open (non-blocking): Platform DB owner to confirm combat_radius_nm stores round-trip; final WARN_THRESHOLD/DENSITY_THRESHOLD at perf budgeting; doc open Q3 (Phase 2/3 agent labeling).

Prior verdict resolved: First review.
Status: Awaiting fresh full re-review in a clean session.

## Review — 2026-06-01 — Verdict: NEEDS REVISION

**Scope:** Re-review after 2026-05-30 major revision.

**Summary:** Prior seven blockers resolved. Five integration/schema items remain (metadata.seed/editVersion in §3.2, replay-verify hash alignment, event debugger ↔ order log, validation error code registry, fire_order artifact shape).

**Blocking (5):** metadata schema; determinism/replay parity; EventFired/debugger contract; six v1 validation codes; fire_order output definition.

**Recommended:** v1 scope for P1 mission types / operationsTimeline[]; AC-5 sample-complete schema.

**Status:** Awaiting revision; no implementation sign-off until blockers closed.

## Review — 2026-06-01 (follow-up doc pass) — Verdict: APPROVED for v1 design gate

**Summary:** GDD updated with `metadata.seed`/`editVersion` in §3.2, six validation codes in §3.6, `fire_order` + replay-verify cross-refs, event debugger ↔ order-log projection in §3.5.

**Status:** Design gate **APPROVED** for v1 implementation planning; implementation of editor still gated on Platform DB `combat_radius_nm` sign-off.
