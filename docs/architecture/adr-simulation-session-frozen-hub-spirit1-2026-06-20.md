# ADR Addendum: `SimulationSession` as Frozen Hub (Spirit 1 / Vertical Slice MVP)

**Status:** Accepted (docs-only; no code change)  
**Date:** 2026-06-20  
**Authority:** [spirit1-vertical-slice-gap-analysis-2026-06-05.md](../../Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md) §4–§5 item 3 (Recommendation R3)  
**Related:** [vertical-slice-mvp.md](../../production/milestones/vertical-slice-mvp.md) Risk Register; ADR-004 (tick pipeline ordering)

## Context

The Baltic Vertical Slice MVP (Phase 1) routes the headless **plan → fight → replay** loop through `SimulationSession` in `ProjectAegis.Delegation.Orchestration`. The Spirit 1 gap analysis quantified this hub as **CRITICAL** blast radius: **68 impacted symbols**, 10 direct callers, 5 execution processes, and 6 modules (Baltic 43, Bridge 14). The milestone Risk Register already lists DecisionLog / Orchestrator as **HIGH blast radius** with mitigation **"Impact analysis before C1 edits"** — status **Open**.

Graph evidence (`impact(SimulationSession, upstream)`) matches the documented architecture: tick pipeline, order log, checkpoints, and C1 combat all converge on this orchestration surface.

## Decision

1. **Treat `SimulationSession` as a frozen hub** for the Vertical Slice MVP and through Release v1.0 unless a scoped story explicitly authorizes hub edits with completed GitNexus impact analysis.
2. **Mandatory impact-before-edit gate:** Before any change to `SimulationSession` or its public orchestration contract, run `gitnexus impact` (upstream) on `SimulationSession`, report blast radius to reviewers, and obtain explicit approval when risk is **HIGH** or **CRITICAL**.
3. **No hub refactor in Spirit 1 remediation:** This ADR records policy only. Product code is unchanged by this document.

## Consequences

- **Positive:** Preserves determinism and replay stability on the highest-fan-out symbol in the MVP graph; aligns milestone risk register with measured GitNexus data.
- **Negative:** Feature work that naturally touches the hub remains gated; teams must route changes through narrow seams when possible.
- **Operational:** Any PR touching `SimulationSession` must cite impact output in the description (per `AGENTS.md` / GitNexus rules).

## Future direction (optional; not scheduled)

To shrink CRITICAL blast radius post-MVP, consider extracting the C1 combat / engage step behind a **narrower interface** (e.g. `IEngagementTickStep` or equivalent) so Baltic harness, replay, and Unity adapter depend on a smaller contract than the full session type. **No symbol extraction is planned or authorized by this addendum** — it is a traceability placeholder for a future ADR if hub edits become unavoidable at scale.

## Evidence summary (Spirit 1 gap analysis)

| Metric | Value | Source |
|--------|------:|--------|
| Blast radius | CRITICAL | Gap analysis §4 |
| Impacted symbols | 68 | `impact(SimulationSession, upstream)` @ 2026-06-05 |
| Direct callers | 10 | Gap analysis §4 |
| Processes | 5 | Gap analysis §4 |
| Modules | 6 (Baltic 43, Bridge 14) | Gap analysis §4 |
