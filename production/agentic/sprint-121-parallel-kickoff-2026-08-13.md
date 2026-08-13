# S121 parallel kickoff — 2026-08-13

**Linear:** [DRG-156](https://linear.app/drgamtd-workspace/issue/DRG-156) · **Predecessor:** S120 (#486 landed) · **Epic:** DRG-149  
**Stage:** **Release** · **Not Launch**  
**Skill:** `dispatching-parallel-agents` · [`linear-parallel-dispatch-playbook.md`](linear-parallel-dispatch-playbook.md)  
**Plan:** [`production/sprints/sprint-121-c2-overlays.md`](../sprints/sprint-121-c2-overlays.md)

**Surfaces:** docs-only; **no C# lanes**

> **Do not rebuild CMD-32/34. Do not touch DelegationBridge hotpath or S120 issuance path.**

| Lane | Surface (allowed) | Does not write |
|------|-------------------|----------------|
| **A Inventory** | This kickoff + residual inventory in `production/sprints/sprint-121-c2-overlays.md` + QA plan stub | Any `src/**`, `unity/**` C#, tests, `DelegationBridge*`, issuance hosts |
| **B Hygiene** (optional, tiny) | Tracker/doc honesty under `production/**` or Linear/Hub wording only | Overlay projection types, `MapPlaceholderPanelHost`, PlayMode harness |

**Merge:** single docs PR if B fires; A may land as the open-sprint commit. No serial C# restack.

**Invariants:** ZERO DelegationBridge · CatalogWriteGate untouched · no new overlay pipeline · Baltic hash `17144800277401907079` · suite floor cite ≥1638/0f (no C# → no suite rerun required).

## File-disjoint from S120

S120 (DRG-155) audited **CMD-31 issuance** — `C2CommandIssuance`, `C2PlayerCommandBridge`, `UnitOrderToolbarHost`. **Landed #486.**

S121 audits **CMD-32/34 overlay projections** — `DatalinkPictureProjection`, `TacticalOverlayProjection`, `MapPlaceholderPanelHost.ApplyOverlayCounts`. **Already on trunk from S108** (#382/#390).

Shared read-only dependency: `DelegationBridgeHost` presentation feed (symbols, selection, catalog). **No shared write surfaces.** Overlay product dispatch after this inventory does **not** require S120 restack.

## Investigation summary (2026-08-13)

Verified on `main`:

- Projection layer for datalink edges and envelope rings exists with headless tests.
- Unity host projects overlays and surfaces **counts only**; default UXML has no overlay count labels (null-safe).
- **No** map canvas or Cesium geometry renderer for rings/edges on trunk.
- Datalink status is not fed from live comms state (catalog mesh defaults to Up).

**Do not co-dispatch overlay visual C# until owner accepts residual inventory and picks a product slice.**
