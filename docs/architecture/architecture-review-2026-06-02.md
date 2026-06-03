# Architecture Review Report

**Date:** 2026-06-02  
**Mode:** `/architecture-review full`  
**Engine:** Unity 6.3 LTS (6000.3.14f1) + .NET 8 headless  
**GDDs reviewed:** 11 system GDDs under `design/gdd/`  
**ADRs reviewed:** 7 (`adr-001` … `adr-007`)

---

## Load summary

| Artifact | Count |
|----------|-------|
| System GDDs | 11 |
| ADRs | 7 |
| Engine reference | `docs/engine-reference/unity/VERSION.md`, `dots-ecs-notes.md` |
| TR registry (prior) | None — created this review |
| `docs/consistency-failures.md` | Not present |

---

## Requirements design-review blockers (C1–C5)

Source: [requirements-13-20-design-review-2026-05-29.md](../../Game-Requirements/reviews/requirements-13-20-design-review-2026-05-29.md)

| Blocker | Requirement | Evidence | Status |
|---------|-------------|----------|--------|
| **C1** | Order log + replay (doc 17) | `IOrderLog`, ADR-003, `ReplayGoldenTests` | **Closed** |
| **C2** | Combat outcomes + message log (doc 18) | `EngagementOutcomeRecord`, `MessageLogProjection` | **Closed** (MVP) |
| **C3** | ROE / policy (doc 13) | `IPolicyEvaluator`, scenario ROE JSON | **Closed** (headless MVP) |
| **C4** | EMCON (doc 20) | `EmconPolicyEvaluator`, scenario JSON | **Closed** (radar slice) |
| **C5** | Human-in-the-loop (doc 13) | `SimulationModeProfile`, `PlayerOrderRecord` | **Deferred** |

---

## Traceability summary

| Status | Count | % |
|--------|-------|---|
| Covered | 14 | 30% |
| Partial | 21 | 45% |
| Gap | 12 | 25% |
| **Total TRs** | **47** | 100% |

Full matrix: [architecture-traceability-index.md](architecture-traceability-index.md)  
Stable IDs: [tr-registry.yaml](tr-registry.yaml)

---

## Coverage gaps (no ADR or Proposed-only)

### Foundation / Core

| TR-ID | GDD | Requirement | Suggested ADR |
|-------|-----|-------------|---------------|
| TR-logistics-003 | logistics-magazines | Deterministic fuel burn | `/architecture-decision logistics-fuel-model` |
| TR-sensor-004 | sensor-detection-ew | Side picture / datalink | `/architecture-decision sensor-side-picture` |
| TR-editor-002 | agentic-mission-editor | Deterministic Validation Engine | `/architecture-decision mission-editor-validation-engine` |
| — | systems-index #4, #20 | Platform DB / DATA pipeline | **Accept ADR-006** |
| — | systems-index #9 | Mission Runtime | `/architecture-decision mission-runtime-contract` |

### Feature / extension

| TR-ID | GDD | Requirement | Suggested ADR |
|-------|-----|-------------|---------------|
| TR-combat-dom-001..003 | combat-domains-damage | Domain validators, damage order, BDA | `/architecture-decision combat-domain-validators` |
| TR-engage-003 | engagement-fire-control | Swarm slot order (P1) | Defer or engage ADR amendment |
| TR-agentic-002..003 | agentic-infrastructure | Hindsight / AAR agents (P1) | `/architecture-decision agentic-aar-infrastructure` |
| TR-editor-004..005 | agentic-mission-editor | editVersion, MCP export gate | Fold into mission-editor ADR |

---

## Cross-ADR conflicts

### Conflict: `requirements-traceability.md` vs ADR-005 file

**Type:** Integration / documentation  
**ADR-005 file:** DOTS/ECS world state — not engagement.  
**RTM error:** Maps combat TRs to ADR-005 as “engagement pipeline.”  
**Resolution:** Update RTM: engage → ADR-001 + ADR-004; ECS → ADR-005.

### Tension: Order log ownership (non-blocking)

ADR-003 keeps `DecisionLog` in Delegation; `architecture.md` notes future `Sim.Log`. Single writer until migration ADR.

### Unresolved: ADR-006 Proposed

Blocks accepted architecture for catalog writes and `dbSnapshotId` contracts. **Accept ADR-006** before platform-db epic expansion.

**No dependency cycles** among ADR-001..007.

---

## Recommended ADR implementation order

1. ADR-001 — Sim boundary *(Accepted)*  
2. ADR-004 — Tick pipeline *(Accepted)*  
3. ADR-003 — Order log *(Accepted)*  
4. ADR-002 — Policy evaluator *(Accepted)*  
5. **ADR-006 — Data layer** *(Proposed — accept next)*  
6. ADR-005 — DOTS/ECS *(Accepted)*  
7. ADR-007 — C2 map *(Accepted)*  
8. New: combat domains, editor validation engine, logistics fuel

---

## GDD revision flags

None — C2 GDD Phase A map placeholder aligns with ADR-007.

---

## Engine compatibility

| Check | Result |
|-------|--------|
| Version | Unity 6000.3.14f1 consistent |
| ADRs with Engine Compatibility | 1 / 7 (ADR-007) |
| Deprecated APIs in ADRs | None found |
| `breaking-changes.md` / `deprecated-apis.md` | Missing |
| Engine specialist | Skipped — `technical-preferences.md` not configured |

---

## Architecture document coverage

`docs/architecture/architecture.md` exists but is **stale**: missing ADR-007 in index; omits cyber, scoring, combat domains, editor, agentic infra systems.

---

## Verdict: **CONCERNS**

MVP deterministic spine is sound (C1–C4 closed). Full traceability shows 25% gaps, ADR-006 Proposed, RTM/ADR label drift, and incomplete engine sections on ADRs.

### Blocking issues for Pre-Production PASS

1. Accept or explicitly defer **ADR-006**  
2. Fix **requirements-traceability.md** ADR-005 mislabel  
3. Add ADRs for **combat-domains** and **mission-editor Validation Engine** (or defer GDDs from gate)

### Required ADRs (priority)

1. Accept **ADR-006**  
2. **Combat domain validators**  
3. **Mission editor Validation Engine**

---

## History

| Date | Verdict | Notes |
|------|---------|-------|
| 2026-06-02 AM | CONCERNS | MVP slice review (6 GDDs) |
| 2026-06-02 PM | CONCERNS | Full review — 47 TRs, 7 ADRs, traceability index + registry |