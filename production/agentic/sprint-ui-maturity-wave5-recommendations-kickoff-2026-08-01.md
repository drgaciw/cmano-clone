# UI Maturity Wave 5 — Recommendation Execution — 2026-08-01

**Branch base:** `stack/ui-maturity/wave4-cesium-deck-boat-lod`  
**Integration:** `stack/ui-maturity/wave5-tick-chrome-signoff`  
**Goal:** Implement post-Wave-4 recommendations without opening a sprawling new epic.

## Context

Stack [#382](https://github.com/drgaciw/cmano-clone/pull/382)–[#385](https://github.com/drgaciw/cmano-clone/pull/385) is feature-complete for UI maturity waves 1–4. Recommended next work:

1. Land stack (hygiene + single tip)  
2. Editor sign-off scaffolding  
3. **Wire FSMs into session tick**  
4. **CMD-22 / CMD-23 chrome**  
5. Basemap menu host wire  

## Lanes (surface-disjoint)

| Lane | Scope | Surface | Forbidden |
|------|-------|---------|-----------|
| **F FSM-Tick** | AirOps + BoatOps maps on `SimulationSession`; advance on `RunExecutingTick`; apply Launch/Abort/Boat orders from executed orders; bridge lifecycle feed | `SimulationSession.cs`, optional thin bridge Refresh*, tests | Top bar UXML, chrome hosts, Cesium |
| **T Time-22** | CMD-22 Zulu + local + remaining duration on top bar | `C2TopBar*`, top bar host/UXML additive, tests | Session.Tick body, boat FSM |
| **C Chrome-23** | CMD-23 collapse wire to MessageLog + LeftDrawer; pure prefs bag | Chrome collapse + hosts, tests | Order.cs, AirOps FSM |
| **H Host+Signoff** | C2 menu / layer checklist host; play-mode signoff checklist + scene builder Ensure hosts; stack land notes | Menu host, Editor builder additive, `production/qa/*`, `production/agentic/*land*` | SimulationSession tick core |

## Acceptance

1. Session tick advances AirOps/Boat maps deterministically; launch orders mutate map state.  
2. Top bar presents ZULU, LOCAL, REMAINING labels.  
3. Collapse toggles change presentation; prefs bag round-trips.  
4. Menu/layer host + QA checklist exist; stack merge runbook written.  
5. Full test suites green; no Tick rewrite of DelegationBridge; OrderKind append-only only if needed (prefer existing kinds).

## Merge order

F → T → C → H

---
*Kickoff Wave 5 recommendation execution.*
