# Sprint 121 — C2 overlays residual-scope (DRG-156)

**Dates:** 2026-08-13  
**Predecessor:** S120 residual-scope ([DRG-155](https://linear.app/drgamtd-workspace/issue/DRG-155); landed #486)  
**Linear:** [DRG-156](https://linear.app/drgamtd-workspace/issue/DRG-156)  
**Epic:** [DRG-149](https://linear.app/drgamtd-workspace/issue/DRG-149)  
**Stage:** **Release** · **Not Launch**  
**Kickoff:** [`production/agentic/sprint-121-parallel-kickoff-2026-08-13.md`](../agentic/sprint-121-parallel-kickoff-2026-08-13.md)  
**Doctrine:** ADR-010 / 007 / 001 · unity-csharp-architect pr-finish (cite only — no C# this sprint)

> **Do not rebuild CMD-32/34. Do not touch DelegationBridge hotpath or S120 issuance path.**

## Goal

Residual-scope **audit/docs only**. Confirm the existing **S108** overlay projection path is the product path for CMD-32 (datalink) and CMD-34 (sensor/engagement rings); list leftover gaps before any overlay **visual** dispatch. **No new overlay pipeline.** Core projection + count integration already landed in S108 UI Maturity (#382/#390; closeout [`sprint-ui-maturity-cmd31-37-closeout-2026-08-01.md`](../agentic/sprint-ui-maturity-cmd31-37-closeout-2026-08-01.md)).

## S120 / file-disjoint verdict

| Question | Answer |
|----------|--------|
| Blocked on S120 command path? | **No** — S120 landed on `main` (#486). Issuance (`C2CommandIssuance`, `UnitOrderToolbarHost`) is unrelated to overlay projection. |
| Share presenters with S120? | **No** — S120 touches issuance/toolbar hosts; S121 touches `MapPlaceholderPanelHost` + `Projection/*` overlay types. Both read `DelegationBridgeHost` presentation feed but **different files and methods**. |
| Safe to dispatch overlay product C# after this inventory? | **Yes, file-disjoint** — future overlay **visual** work can proceed without restacking on S120 issuance. Still require owner dispatch + pr-finish; do not rebuild projection layer. |

## Must Have

| ID | AC |
|----|-----|
| S121-01 | Publish residual-scope kickoff (`sprint-121-parallel-kickoff-2026-08-13.md`) — **docs-only surfaces; no C# lanes** |
| S121-02 | Residual inventory of the landed S108 overlay projection path + leftover gaps (this plan). Confirm no CMD-32/34 rebuild. |
| S121-03 | QA plan stub (`production/qa/qa-plan-sprint-121-c2-overlays-residual-2026-08-13.md`) — docs verification only |

## Should Have (optional tiny doc hygiene only)

| ID | AC |
|----|-----|
| S121-04 | Tracker/doc honesty: DRG-156 / Hub language says residual-scope, not “rebuild CMD-32/34”. Do not invent a new overlay API. |

## Landed path (confirm — do not rewrite)

S108 #382/#390 + UI Maturity closeout [`sprint-ui-maturity-cmd31-37-closeout-2026-08-01.md`](../agentic/sprint-ui-maturity-cmd31-37-closeout-2026-08-01.md):

```text
DelegationBridgeHost (presentation feed: symbols, selection, OOB, catalog)
  → MapPlaceholderPanelHost.ApplyOverlayCounts()
      → CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges (CMD-34 ranges)
      → TacticalOverlayProjection.ProjectSelectedUnitEnvelopes (CMD-21/34 rings)
      → DatalinkUnitPairFeed.ProjectEdges → DatalinkPictureProjection.Project (CMD-32 edges)
      → MapPanelApplyState.Apply (count-only presentation)
  → Optional HUD labels: ENVELOPES / DATALINKS counts (null-safe Q; UXML labels not in default panel)
```

**Important:** `MapPanelApplyState` and host comments explicitly state overlay lists are **count-only** — rings/edges are projected in headless code but **not drawn** on the map canvas or Cesium globe.

Pure projection types on trunk (do not re-implement):

| CMD | Type | Role |
|-----|------|------|
| CMD-32 | `DatalinkPictureProjection` | Deterministic datalink edge projection from tuples or catalog link ids |
| CMD-32 | `DatalinkUnitPairFeed` | Adjacent friendly unit-pair mesh from OOB + catalog links |
| CMD-21/34 | `TacticalOverlayProjection` | Selected-unit sensor + weapon envelope ring entries |
| CMD-21/34 | `CatalogEnvelopeRangeResolver` | Catalog weapon nm + default sensor/weapon fallbacks |
| — | `MapPanelApplyState` | Bind/apply with `EnvelopeRingCount` / `DatalinkEdgeCount` |
| — | `EnvelopeRingEntry`, `DatalinkEdgeEntry` | Immutable overlay DTOs |

Tests already on trunk:

| Assembly | Fixture | Covers |
|----------|---------|--------|
| `Delegation.Tests` | `Projection/DatalinkPictureProjectionTests` | tuple + catalog projection, status normalize, sort order |
| `Delegation.Tests` | `Projection/TacticalOverlayProjectionTests` | selected unit rings, domain labels, empty selection |
| `Delegation.Tests` | `Projection/MapPanelApplyStateTests` | overlay count apply, null/empty overlays |
| `Delegation.Tests` | `Projection/DatalinkUnitPairFeedTests` | mesh build, ProjectEdges, catalog integration |
| `Delegation.Tests` | `Projection/CatalogEnvelopeRangeResolverTests` | weapon nm resolve, defaults, unit ranges |
| `UnityAdapter.Tests` | `Bridge/C2PanelPerfBenchTests` | `TacticalOverlayProjection` + `MapPanelApplyState` in perf bind path |

## Residual inventory (leftovers — not a new pipeline)

| Item | Status | S121 disposition |
|------|--------|------------------|
| CMD-32 `DatalinkPictureProjection` + `DatalinkUnitPairFeed` | **Landed S108** | Confirm only. **Do not rebuild.** |
| CMD-34 `TacticalOverlayProjection` + `CatalogEnvelopeRangeResolver` | **Landed S108** | Confirm only. **Do not rebuild.** |
| `MapPlaceholderPanelHost.ApplyOverlayCounts` wiring | **Landed S108** | Confirm only; stores `LastEnvelopeRingCount` / `LastDatalinkEdgeCount` |
| Headless overlay projection tests | **Present** (table above) | No new test classes this sprint |
| **Visual** ring/edge rendering on map canvas or Cesium | **Not implemented** | Document gap; future product dispatch (out of residual-scope) |
| Default `MapPlaceholderPanel.uxml` overlay count labels | **Absent** — host uses null-safe `Q` for `envelope-ring-count` / `datalink-edge-count` | Document gap; optional UXML follow-up, not a rebuild trigger |
| Live datalink status (Up/Degraded/Down) from sim comms | **Not wired** — `DatalinkUnitPairFeed.ProjectEdges` uses `StatusUp` | Document gap; feed from snapshot/comms projection later |
| Per-platform sensor range from catalog mounts | **Partial** — weapon via catalog; sensor uses default nm unless extended | Document gap; Wave 2 closeout follow-up |
| Play Mode visual sign-off (rings/edges visible) | Checklist/human Editor box still open | Document gap; **do not** add C# / PlayMode harness work this sprint |
| PlayModeSmokeHarness named overlay cases | **None** (headless suite covers projection path) | Leave; not a rebuild trigger |
| CMD-33 `DoctrineMapOverlayProjection` on same host | **Landed S108** (adjacent; not DRG-156 AC) | Cite for honesty; **out** of S121 must-have |
| S120 issuance (`C2CommandIssuance`, toolbar) | **Landed S108; S120 inventory #486** | **Out** — file-disjoint |
| `DelegationBridge.cs` facade-on-hub P1 (S108 closeout) | Honesty item for later cleanup PR | **Out** — zero-touch hotpath |
| CMD-35 live edit · CMD-36 perf · basemap CMD-28.2 | Backlog (different CMDs) | **Out** |

## Forbidden rebuilds

| Forbidden | Reason |
|-----------|--------|
| Rebuild `DatalinkPictureProjection` | On trunk since S108; tests green |
| Rebuild `TacticalOverlayProjection` | On trunk since S108; tests green |
| Rewrite `MapPanelApplyState` count semantics | Working contract; count-only by design |
| Touch `DelegationBridge.Tick` / hotpath | Release invariant |
| Reopen S120 issuance path | File-disjoint; S120 residual complete |
| Invent new overlay projection API | Extend via additive callers only, after owner dispatch |

## Non-goals

Rebuild CMD-32/34 · rewrite overlay projection types · any C# under `src/` or `unity/**` this sprint · DelegationBridge Tick / hotpath · S120 issuance product work · visual map overlay implementation · Launch · Phase N · CatalogWriteGate · invent Approved · reopen S108–S119 product lands

## Invariants held

- Stage **Release**
- ZERO DelegationBridge / CatalogWriteGate / locked-eval
- ReplayGolden 6/6 · Baltic hash `17144800277401907079`
- Suite floor ≥1638/0f (cite last gate; **no C# → no suite rerun required**)

## Definition of Done

- [x] Kickoff published (S121-01)
- [x] Residual inventory written (S121-02)
- [x] QA plan stub published (S121-03)
- [ ] Optional S121-04 tracker wording (only if owner wants tiny hygiene)
- [ ] DRG-156 comment with inventory result; close or leave Todo with “residual-scope complete / no rebuild”
- [ ] Stage remains **Release**

---
*S121 opened 2026-08-13 as residual-scope only. Do not rebuild CMD-32/34. Overlay product C# is file-disjoint from S120. Stage **Release**. Not Launch.*
