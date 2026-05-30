# Design Review: Requirements Bundle 13–20 (Simulation & C2)

**Date:** 2026-05-29  
**Reviewer:** design-review (lean — single session, no specialist spawn)  
**Scope:** `Game-Requirements/requirements/13` through `20`  
**GitNexus:** CLI analysis in `docs/requirements/impact-sim-c2-requirements-2026-05-29.md`

---

## Verdict: **CONCERNS** (approve to proceed with listed fixes)

Requirements are **coherent, cross-linked, and implementable at architecture level**. Not **FAIL** because gaps are known and scoped. Not **PASS** until P0 conflicts below are resolved in doc 08 / ADRs and `DecisionLog` / `IRoeFilter` alignment is explicit.

---

## Completeness (Requirements Standard)

| Criterion | 13 | 14 | 15 | 16 | 17 | 18 | 19 | 20 |
|-----------|----|----|----|----|----|----|----|-----|
| Purpose / Vision | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| CMO parity table | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Functional P0/P1 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| NFR (perf/determinism) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Acceptance criteria | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Traceability | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| MCP tools | ✓ | ✓ | ✓ | ✓ | ✓ | — | ✓ | P1 |
| **Formulas / numeric budgets** | — | — | — | — | — | — | — | — |
| **Explicit data schemas** | Partial | Partial | Partial | Partial | **Strong** | Partial | Partial | — |

**Note:** These are **requirements**, not GDDs. Missing formulas are acceptable here but must appear in system GDDs before implementation.

---

## Cross-Document Consistency

### Strengths

- Shared contracts: **Policy Snapshot** (13) → engage pipeline (14) → **FireAbortReason** (13/14)
- **Order log** (17) as single timeline referenced by UI (20), AAR (07), replay tests
- **ObservedState** / fog (15) ↔ agents (04) ↔ cyber (19) aligned
- Phasing (MVP vs P1) consistent with doc **01** scope (limited land, no campaigns v1)

### Issues to resolve (blocking for architecture)

| ID | Severity | Issue | Resolution |
|----|----------|-------|------------|
| C1 | **Blocker** | Doc 17 `OrderLog` vs code `DecisionLog` / `DecisionRecord` — different fields and scope | Add §“Implementation mapping” in doc 17; ADR for schema v1 |
| C2 | **Blocker** | Doc 13 full ROE/EMCON/WRA vs `IRoeFilter.Evaluate(Order)` + `PassthroughRoeFilter` | ADR: policy evaluator; deprecate passthrough in production |
| C3 | **Major** | Doc 14 engage pipeline vs `OrderKind.Engage` only — no shooter/target/weapon in `Order` record | Extend `Order` payload or separate `EngageIntent` type in ADR |
| C4 | **Major** | Doc 11 mission **runtime** vs editor-only data model — timeline execution not in 11 acceptance criteria | Add runtime AC to doc 11 or reference new “Mission Runtime” GDD |
| C5 | **Major** | Doc 15 detection tick vs no sim assembly in repo — undefined owner | `systems-index`: Sim Core owns tick; bridge reads snapshot |
| C6 | **Minor** | `FireAbortReason` vs `LogisticsAbortReason` (16) — two enums | Document shared `AbortReason` namespace or parent enum |
| C7 | **Minor** | Doc 20 engine UI stack “pending setup-engine” | Run `/setup-engine` before UI GDD |

### Non-blocking improvements

- Add **TR-ID** placeholders in each doc for architecture traceability matrix
- Doc 18 land combat P1 vs doc 01 “limited land” — add explicit “strategic facilities only” cross-ref (already partial)
- Doc 19 cyber `SpoofTracks` — clarify player-visible vs debug-only in 20

---

## Implementability (GitNexus-informed)

| Requirement area | Code today | Risk |
|------------------|------------|------|
| 13 Doctrine | `IRoeFilter`, `AutonomyGate`, `DefaultRiskClassifier` | Extend, HIGH touch |
| 14 Engagement | `OrderKind.Engage` | New module |
| 15 Sensors | `ObservedState`, `PerceivedStateFactory` | Extend snapshot builder |
| 16 Logistics | None | Greenfield |
| 17 Replay | `DecisionLog`, `ReplayGoldenTests` | Evolve, LOW–MED touch |
| 18 Combat | None | Greenfield |
| 19 Cyber | None | Greenfield P1 |
| 20 UI | Unity adapter only | Greenfield presentation |

**Determinism:** Existing `ReplayGoldenTests`, `SeededRng`, `DeterministicHash` support doc 17 gate — expand fingerprints to include policy denials and engagements when implemented.

---

## Dependency Graph (Requirements → GDD)

All systems in `design/gdd/systems-index.md` list requirement doc sources. **No GDD files exist yet** — expected at this stage.

| Dependency | Status |
|------------|--------|
| game-concept.md | **Created** (from doc 01) |
| systems-index.md | **Created** |
| Per-system GDDs | Not started — use `/design-system` |

---

## Verdict Actions Before `/create-architecture`

1. Patch doc **17** with `DecisionLog` migration subsection (owner: lead programmer).
2. Patch doc **13** with `IRoeFilter` implementation mapping (owner: simulation architect).
3. Approve **systems-index** design order (foundation → combat → presentation).
4. Run **`npx gitnexus analyze`** after first `ProjectAegis.Sim` commit.

---

## Sign-off

| Role | Verdict |
|------|---------|
| Design review | **CONCERNS** — proceed to architecture + GDDs with C1–C5 tracked |
| Requirements analyst | Impact **HIGH** — expected for greenfield sim |
| Next gate | `/create-architecture` or `/architecture-review` when ADRs exist |
