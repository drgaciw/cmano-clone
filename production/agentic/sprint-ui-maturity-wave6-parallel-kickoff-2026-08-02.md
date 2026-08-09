# UI Maturity Wave 6 — Parallel Plan Execution — 2026-08-02

**Base:** `stack/ui-maturity/wave5-tick-chrome-signoff` (PRs #382–#386 still open to main)  
**Integration:** `stack/ui-maturity/wave6-explain-axes-map-ground`

## Gate note

Stack #382–#386 is **not** yet on `main`. Wave 6 branches from the **wave5 tip** so product work continues without waiting on human merge. Land order remains: 382→383→384→385→386→**this tip**.

## Lanes

| Lane | CMD | Deliverable |
|------|-----|-------------|
| **E** | CMD-11 | `EngageExplainProjection` plain-language FireAbort/engage explain |
| **A** | CMD-19 + CMD-18 | `AxisControlProjection` + `DomainPresetProjection` |
| **M** | CMD-20, 28.4/28.5 | `MapScaleProjection`, measure, unit cycle |
| **G** | CMD-26 Phase A | `GroundOpsProjection` brigade+ leaf rows |
| **H** | Hygiene | This kickoff + playmode checklist delta |

## Invariants

- No `DelegationBridge.Tick` rewrite
- CatalogWriteGate untouched
- OrderKind unchanged
- No battalion TO&E invent

## Merge order

H → E → A → M → G (docs-first; pure projections have low conflict risk — single tip commit acceptable).
