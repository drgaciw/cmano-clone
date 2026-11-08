# UI Maturity Wave 2 Parallel Kickoff — 2026-08-01

**Branch base:** `stack/ui-maturity/cmd-31-37-parallel`  
**Integration branch:** `stack/ui-maturity/wave2-cmd-24-27-33-36`  
**Stage:** Release. Zero `DelegationBridge.Tick` rewrite. CatalogWriteGate untouched.

## Lanes (surface-disjoint)

| Lane | Scope | Surface (allowed) | Forbidden |
|------|-------|-------------------|-----------|
| **M Map+Doctrine** | CMD-33 doctrine map; catalog envelope ranges; unit-pair datalink feed | `Projection/DoctrineMap*`, `CatalogEnvelopeRangeResolver*`, `DatalinkUnitPair*`, additive `TacticalOverlay*`, `MapPlaceholderPanelHost` overlay wiring, tests | ScenarioLibrary*, AirOps*, SceneBuilder, LiveEdit*, Perf* |
| **L LiveEdit** | CMD-35 live editing contract | `Projection/LiveEdit*`, thin wrap over `ScenarioEditCommandBus` findings, optional UnityAdapter authoring presenter additive only, tests under Data/Delegation | MapPlaceholder, Order.cs, CatalogWriteGate |
| **P Perf** | CMD-36 panel bind perf bench | `UnityAdapter.Tests/.../C2Panel*Perf*`, `production/perf/*` report markdown, extend timing tests only | Hosts production code rewrite |
| **S ScenarioLib** | CMD-27 scenario library Phase 1 | `Data/Scenario/ScenarioLibrary*`, `Projection/ScenarioLibrary*`, Unity `ScenarioLibraryPanelHost` + UXML, tests | Map overlay, AirOps |
| **A AirOps** | CMD-24 Phase A | `Projection/AirOps*`, Unity `AirOpsPanelHost` + UXML, tests | Map, ScenarioLibrary |
| **E EditorScene** | Drop new hosts onto smoke scene builder | `unity/.../Editor/DelegationSmokeSceneBuilder.cs` only (+ optional scene YAML if safe) | Projection logic |

## Acceptance

1. **CMD-33:** Doctrine map overlay rows project per-unit ROE/source for map host; apply-state can count them.
2. **Envelope ranges:** Selected-unit weapon ring range derived from catalog `TryGetWeaponEnvelope` (meters→nm) with fallback defaults.
3. **Datalink feed:** Unit-pair edges project from friendly OOB pairs + catalog links (or scenario policy links); map host count > 0 when feed present.
4. **CMD-35:** Live-edit findings presentation from validation report; disabled actions state reason.
5. **CMD-36:** Headless panel-bind bench covers unit/contact/map/agent bind path under 100 ms budget.
6. **CMD-27:** Library lists scenarios from `data/scenarios` with pre-load feasibility (catalog/validation reason).
7. **CMD-24 Phase A:** Air ops rows show ReadyForLaunch / AIR_NOT_READY cause + aggregate ready count.
8. **Scene:** Scene builder creates UnitOrderToolbar, ContactDetail, AgentRoster (+ new AirOps/ScenarioLibrary if present).

## Merge order

M → L → P → S → A → E

---
*Kickoff UI Maturity Wave 2.*
