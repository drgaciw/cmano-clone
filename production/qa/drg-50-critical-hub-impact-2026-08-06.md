# DRG-50 — Critical-hub impact analysis (DelegationBridge)

**Date:** 2026-08-06  
**Linear:** DRG-50  
**ADR:** ADR-020 Decision 1  
**Playbook:** `production/agentic/critical-hub-merge-playbook-2026-07-14.md`  
**Boundary:** `production/release-continuity-scope-boundary-2026-07-14.md` (ZERO hotpath is hard; this is a **separate, impact-analyzed exception** — not a waiver of the rule)

---

## Change surface

| Item | Detail |
|------|--------|
| File | `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` |
| Method | `EmitFuelTransitions` only |
| Field | `private double? _lastFuelSimTime` |
| Tests | `DelegationBridgeFuelDeltaTests` (new) |
| Tick() reorder | **None** |
| CatalogWriteGate / other hubs | **Untouched** |
| Baltic hash | **Untouched** (`17144800277401907079`) |

---

## Why the exception is authorized

1. **ADR-020 Decision 1** records the fuel cadence contract: drain must use elapsed sim time, not call count × 1.0.
2. Pre-fix behavior over-drains ~60× under `SimplePlayModeSimHost` (1/60 cadence) — product-broken Play Mode fuel, not a theoretical nit.
3. Change is **localized** to the fuel emission helper; no policy evaluation, order dispatch, or registry mutation order changes.
4. ReplayGolden 1.0 s/tick path preserved (first-tick and subsequent deltas of 1.0).

---

## Impact (playbook checklist)

| Check | Result |
|-------|--------|
| GitNexus / hub class | `DelegationBridge` is CRITICAL (playbook watchlist). Edit confined to fuel timeline drain argument. |
| Detect changes | 2 production files (bridge + tests); docs impact note only. |
| ReplayGolden | Expect 6/6; 1.0 s cadence regression test asserts JOKER ~93.75 s. |
| Play Mode smoke | Expect ≥20/20; sub-second cadence test asserts no early JOKER at 2.5 sim-s. |
| Suite floor | AGENTS.md ≥1638/0f monotonic — **reconcile** any scoped 1370 figure against full solution before main promotion. |
| Pre-existing fail | `UnityPluginEpicATypesTests` (stale Unity plugin DLL) — document waiver or fix before claiming 0 failed on main. |

---

## Merge sequence (runbook)

1. Land **PR #405** (boundary must resolve on `main` so this exception can cite it).
2. Merge this PR (**#407**) into `fix/drg-50-fuel-playmode-delta`.
3. Open promotion PR: `fix/drg-50-fuel-playmode-delta` → `main` with full-suite evidence attached.

---

## Refuse confirmation

- Not a drive-by Tick rewrite.
- Not a CatalogWriteGate change.
- Not a hash change.
- Stage remains **Release**.

*Attached to PR #407 per critical-hub merge playbook.*
