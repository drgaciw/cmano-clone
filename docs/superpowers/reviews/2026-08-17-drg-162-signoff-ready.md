# DRG-162 — Play Mode overlay signoff-ready (2026-08-17)

**Linear:** [DRG-162](https://linear.app/drgamtd-workspace/issue/DRG-162/s121-play-human-play-mode-overlay-signoff) — S121 Wave D, owner-only visual  
**Checklist:** `unity/ProjectAegis/PLAYMODE-SMOKE.md` (new overlay section)  
**Scene:** `unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity`  
**Editor Game View this session:** **not run** (headless). Pixel ACs stay **UNKNOWN**.

---

## Verdict

| Layer | Status |
|-------|--------|
| Headless / code path (rings, edges, HUD bind, aspect math) | **PASS** |
| Catalog bind so datalink edges exist in Play Mode | **PASS** (fixed this session) |
| Owner Game View pixels / console | **UNKNOWN** |
| **DRG-162 overall** | **code signoff-ready; visual signoff deferred to owner** |

---

## GitNexus (pre-edit)

| Target | Result |
|--------|--------|
| `MapPlaceholderPanelHost` (class) | **LOW** — 0 upstream, 0 processes |
| `DelegationBridgeHost` (class) | **LOW** — 0 upstream (ambiguous extra hits; index noise) |
| `TacticalOverlayProjection` | **not found** — index stale |
| `MapCanvasOverlayGeometry` | **not found** — index stale |
| `MapCanvasOverlayRenderer` | **not found** — index stale |
| Query “map canvas overlay rings datalink” | definitions only (`MapSymbolPool`, Cesium billboards); no overlay execution process |
| `detect_changes({scope:"all"})` after edit | **LOW**, 0 affected processes |

Blast radius for the `Awake` catalog bind is presentation-only. **No HIGH/CRITICAL.** Refresh index (`node .gitnexus/run.cjs analyze`) before any later overlay symbol edit.

---

## Wiring verification

| Piece | Status | Evidence |
|-------|--------|----------|
| `DelegationSmoke.unity` + `MapPlaceholderPanelHost` | **PASS** | GameObject `MapPlaceholder`; `bridgeHost` → `DelegationBridgeHost`; UXML/USS GUIDs match `MapPlaceholderPanel.*`; `UIDocument.m_PanelSettings` set; `enableMvpEngagement: 1` |
| `MapPlaceholderPanelHost` → overlay path | **PASS** | `ApplyOverlayCounts` → `CatalogEnvelopeRangeResolver` → `TacticalOverlayProjection` → `DatalinkUnitPairFeed` → `MapPanelApplyState` → HUD labels → `ApplyCanvasOverlays` |
| `MapCanvasOverlayRenderer` | **PASS** | Inserts ring/edge layers under `map-canvas`; pixel layout via `LayoutRingPixels` / `LayoutEdgePixels`; `GeometryChangedEvent` relayout |
| `TacticalOverlayProjection` | **PASS** | Selected unit → Sensor + Weapon rings; empty when no selection |
| `MapCanvasOverlayGeometry` | **PASS** | Width-relative circular rings; aspect-correct edge angle/length |
| `DelegationBridgeHost` catalog | **PASS** (was **FAIL**) | `CatalogReader` was never assigned → `ApplyOverlayCounts` skipped `ProjectEdges` → `DATALINKS: 0` and no edges. `Awake` now binds session/factory/fixture catalog. |
| USS colors | **PASS** | Sensor `rgba(74,158,255)` blue; weapon `rgba(232,93,93)` red; edges green / amber / grey |
| HUD labels | **PASS** | UXML `envelope-ring-count` / `datalink-edge-count`; host binds `ENVELOPES: n` / `DATALINKS: n` |
| Smoke ORBAT pair | **PASS** (documented) | Seeder is `u1` + `hostile-1` (2 members). Mesh is adjacent OOB pair → **1** edge. Map picture labels all OOB as Friendly ■. Not a second-friendly seed (harness contract stays 2 members). |

---

## Fix (this session)

**Bug:** `DelegationBridgeHost.CatalogReader` was inject-only and never set in `Awake`. Envelope rings still appeared (40/20 nm fallbacks). Datalink edges did not (`if (catalog is not null)`).

**Change:** presentation-only bind in `Awake` (not `DelegationBridge.Tick`):

```csharp
CatalogReader ??= Bridge.Session?.CatalogReader
    ?? CatalogReaderFactory.TryCreateBalticPatrolReader()
    ?? InMemoryCatalogReader.BalticPatrolFixture();
```

`??=` keeps an injected catalog. Session catalog is used when MVP engage is on (DelegationSmoke default).

**Not changed:** `DelegationBridge` Tick/hotpath, `CatalogWriteGate`, CMD-32/34 types, kinematics, VFX, smoke ORBAT seeder.

---

## Tests (RUN+READ)

| Filter / fixture | Result |
|------------------|--------|
| `PlayModeSmokeHarnessTests` + overlay host contracts | **27/27** passed |
| `MapCanvasOverlayGeometryTests` + `TacticalOverlayProjectionTests` + `Drg162OverlaySignoffProjectionTests` | **16/16** passed |

Commands:

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmokeHarnessTests|MapCanvasOverlayRendererContractTests|MapPlaceholderPanelHostContractTests|DelegationBridgeHostCatalogBindContractTests"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "MapCanvasOverlayGeometryTests|TacticalOverlayProjectionTests|Drg162OverlaySignoffProjectionTests"
```

New: `Drg162OverlaySignoffProjectionTests` (HUD counts == drawn shape counts for smoke pair); `DelegationBridgeHostCatalogBindContractTests`.

---

## Linear AC

| AC | Status | Notes |
|----|--------|-------|
| Pull `main` @ `6e121cc` (DRAW/CESIUM/ASPECT/COMMS-CACHE) | **UNKNOWN** | Not re-pulled this session; overlay/ASPECT code is present |
| Unity 6000.3 + DelegationSmoke + `MapPlaceholderPanelHost` wired | **PASS** (scene) / **UNKNOWN** (Editor open) | Scene + builder verified |
| Selected unit: blue sensor + red weapon, circular on non-square canvas | **PASS** (code) / **UNKNOWN** (pixels) | USS + `LayoutRingPixels` |
| Friendly pair: datalink edges green / amber / grey | **PASS** (code) / **UNKNOWN** (pixels) | Catalog bind unblocks 1 edge; comms policy colors amber/grey |
| Wave 2 HUD `ENVELOPES` / `DATALINKS` match | **PASS** (headless) / **UNKNOWN** (Game View) | Expected `ENVELOPES: 2` / `DATALINKS: 1` |
| No console / bridge errors | **UNKNOWN** | Editor Console not available |
| Forbidden surfaces untouched | **PASS** | No Tick/hotpath, WriteGate, CMD-32/34 rebuild, VFX, kinematics |

---

## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md`  
**ADRs:** ADR-010, ADR-007, ADR-001 (presentation). Catalog read-only via existing `ICatalogReader` (ADR-006 read path; no SQLite open from UI). **Not** Git ADR-018.

**Verdict:** **PASS** (headless / architecture). Visual owner box remains **UNKNOWN**.

**Evidence:**
- Presentation reads: snapshot / `*Bridge` / catalog reader only — no sim mutation
- Command path: N/A (pure presentation bind; no new authority)
- Assemblies: existing Unity Runtime host; DelegationBridge hotpath untouched
- MB / DI: thin `Awake` bind; no Find*/Resources
- Editor: N/A
- Tests: filters above; Play Mode visual last-mile = owner
- Plugins: N/A (Unity Runtime `.cs` only; no plugin DLL refresh)

---

## Remaining owner Editor steps

1. Open `unity/ProjectAegis` in **Unity 6000.3 LTS**.
2. Open `Assets/Scenes/DelegationSmoke.unity`. Confirm every `UIDocument` has `C2RuntimePanelSettings` (empty sky = false negative).
3. Enter Play Mode (`baltic-patrol`). Confirm `u1` selected, blue+red circular rings, one green datalink, HUD `ENVELOPES: 2` / `DATALINKS: 1`.
4. Optional: `scenarioPolicyId` = `baltic-patrol-comms` for amber then grey edges.
5. Click the other ■ — rings follow; HUD counts hold; no console/bridge errors.
6. If pixels match, mark Linear DRG-162 **Done**. If not, file a visual bug with Game View shot — do not rebuild CMD-32/34.

---

## Files changed (this session)

| Path | Why |
|------|-----|
| `unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs` | Bind `CatalogReader` in `Awake` |
| `unity/ProjectAegis/PLAYMODE-SMOKE.md` | DRG-162 overlay signoff table |
| `src/ProjectAegis.Delegation.Tests/Projection/Drg162OverlaySignoffProjectionTests.cs` | Headless HUD/geometry match |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Map/DelegationBridgeHostCatalogBindContractTests.cs` | Source contract for bind |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Map/MapPlaceholderPanelHostContractTests.cs` | HUD + `CatalogReader` tokens |
| `docs/superpowers/reviews/2026-08-17-drg-162-signoff-ready.md` | This note |

---

*Headless evidence 2026-08-17. No Game View screenshots invented.*
