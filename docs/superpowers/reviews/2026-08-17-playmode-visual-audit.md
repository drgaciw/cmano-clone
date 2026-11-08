# Play Mode Visual Audit — Baltic / Theater C2 (2026-08-17)

**Scene under review:** `unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity`  
**Checklist:** `unity/ProjectAegis/PLAYMODE-SMOKE.md`  
**Linear signoff:** [DRG-162](https://linear.app/drgamtd-workspace/issue/DRG-162/s121-play-human-play-mode-overlay-signoff) (owner-only visual — rings + datalink edges)  
**Review mode:** lean (`production/review-mode.txt`) — director gates skipped  
**Editor Game View this session:** **not run** (headless/cloud environment). Visual ACs marked **UNKNOWN** unless code/docs prove otherwise.

---

## 1. Executive verdict

Play Mode looks “visually dead” for CMANO-style movement, weapon tracks, live telemetry firehose, and attention UI because Unity is a **read-only presentation client** ([ADR-010](../../architecture/adr-010-headless-first-command-driven-ui.md)) over a **stub snapshot** (`SimplePlayModeSimHost`), not a live kinematic battlespace.

Three stacked causes:

1. **Stub host** — Editor ticks `SimplePlayModeSimHost` (`ActiveEngagementCount => 0`, fixed contacts, no Baltic engage loop). Full classify/engage/replay lives in `BalticReplayHarness` via `dotnet test`.
2. **Hash placement** — `MapPictureProjection` places ■/◆ with a deterministic hash “until sim publishes world coordinates”; `ISimWorldSnapshot` has **no lat/lon, course, or speed**.
3. **Thin projection** — `MessageLogProjection` surfaces a subset of order-log kinds; `PolicyUpdate` / `AgentDecision` fall through to `null`. Attention toast / auto-pause exist headless-only.

**DRG-162** (sensor/weapon rings + datalink edges) is **code-backed PASS** for the overlay path (`MapCanvasOverlayRenderer`); **owner Game View signoff remains UNKNOWN** until Editor is run. The four CMANO “aliveness” gaps are **not** DRG-162 scope.

---

## 2. Environment / how to reproduce

| Item | Value |
|------|--------|
| Project | `unity/ProjectAegis` — Unity **6000.3 LTS** |
| Scene | `Assets/Scenes/DelegationSmoke.unity` |
| Host stack | `DelegationBridgeHost` + `SimplePlayModeSimHost` + panel hosts (top/left/map/right/message log) |
| PanelSettings | Required (`Assets/UI/C2RuntimePanelSettings.asset`); empty sky = false negative |
| Smoke seed | `seedSmokeOrbatOnStart` on; wire each host’s `bridgeHost` |
| Typical policy | `baltic-patrol` (default); richer log/chrome: `baltic-patrol-comms`, `baltic-patrol-mission-roe` |
| Headless gate (no Editor) | `dotnet test … --filter PlayModeSmokeHarnessTests` |
| This audit evidence | Code + requirements + plan explore summaries + Linear DRG-162; **no Game View screenshots** |

Reproduce steps: follow `PLAYMODE-SMOKE.md` § Scene setup → Enter Play Mode → select friendly unit → observe map overlays / message log / top bar for ~30s–2 min.

---

## 3. DRG-162 checklist

Source AC from Linear DRG-162 (Todo, owner-only, assignee human). Status key: **PASS** = code/doc proves path exists; **FAIL** = code proves absence or contradiction; **UNKNOWN** = visual-only / Editor not run this session.

| AC | Status | Evidence |
|----|--------|----------|
| Unity 6000.3 + DelegationSmoke + `MapPlaceholderPanelHost` wired | **UNKNOWN** (scene exists in repo; Editor not opened) | `PLAYMODE-SMOKE.md`; scene path above |
| Selected unit: blue **sensor** ring + red **weapon** ring (circular on non-square canvas) | **PASS** (code) / **UNKNOWN** (pixels) | `MapCanvasOverlayRenderer` syncs `MapCanvasRingShape`; ASPECT called out in DRG-162; no Game View capture |
| Friendly pair: datalink edges (green / amber / grey) | **PASS** (code) / **UNKNOWN** (pixels) | Same renderer edge layer; DRG-160/163 contract tests |
| HUD counts `ENVELOPES` / `DATALINKS` still match | **UNKNOWN** | Requires live Game View / HUD bind observation |
| No console / bridge errors | **UNKNOWN** | Editor Console not available this session |
| Forbidden: DelegationBridge hotpath / CatalogWriteGate / etc. | **PASS** (process) | Audit is docs-only; no hotpath edits |

**DRG-162 overall (this session):** **code path ready; visual signoff deferred to owner.**

### Extended session checks (four gaps — not Linear AC)

| Check | Status | Notes |
|-------|--------|-------|
| PanelSettings wired (not empty sky) | **UNKNOWN** | Documented failure mode in `PLAYMODE-SMOKE.md` |
| Message log line count after 30s / 2 min (`baltic-patrol` vs `baltic-patrol-comms`) | **FAIL** (expected thin) / **UNKNOWN** (exact counts) | Stub + seed: few seeded lines; Baltic engage not on Editor tick |
| Time compression: display vs interactive | **FAIL** (interactive) | Top bar shows static `TIME: 1x` presentation label (`DelegationBridgeHost` / `C2TopBarPanelHost`); CMD-04 residual |
| Plot Course / Attack → map change | **FAIL** | `UnitOrderToolbarHost` emits `plot_course` order → log; symbols stay hash-placed |
| Toast / flash / auto-pause in UI | **FAIL** | No Unity toast host; `WatchAutoPauseGate` / `AttentionTierAlertProjection` headless-only |
| Combat particles / LineRenderer / VFX Graph under `unity/` | **FAIL** (absent; intentional) | Repo grep: no matches; art-bible §7 N/A |

---

## 4. Four-gap classification

| Gap | Play Mode today | Headless / code | Requirements | Classification |
|-----|-----------------|-----------------|--------------|----------------|
| **1. Platform movement** | Static hash-placed ■/◆; `plot_course` logs only | Swarm/waypoint kinematics not bound to C2 map; ORBAT lat/lon authoring-only | **CMD-06** Phase A placeholder shipped; **CMD-30.7** routes Open; ADR-007 Phase B = world-anchored when lat/lon published; **no** numbered “icons interpolate with course/speed” | **Planned** (layout upgrade) / **Missing from reqs** (animated kinematics) → draft **CMD-38** |
| **2. Weapon effects** | Range rings + datalink edges only (S121); no fire lines / missile tracks / flashes | `MvpEngagementResolver` → `DecisionLog` only | Art-bible **§7 VFX N/A**; **CMD-30.5** illumination/targeting vectors specified, not built | **Out of scope** (cinematic VFX) / **Planned** (2D engagement geometry) |
| **3. Game telemetry** | Message log host; default patrol often looks empty (seeded CONTACT/MAGAZINE-ish lines) | `MessageLogProjection` projects CONTACT / WEAPON_LAUNCH / KILL / MAGAZINE / PLAYER_ORDER / COMMS / FUEL / …; **`PolicyUpdate` / `AgentDecision` → null** | **CMD-05** / **RPL-14** partial/shipped; scrub **RPL-07/08** residual | **Implemented** as thin projection; looks dead because stub host + dropped kinds |
| **4. UI visual actions** | Top bar time ticks; static compression chrome; COMMS/doctrine/selection; no toast | `AttentionTierAlertProjection` + `WatchAutoPauseGate` headless | **CMD-04** compression display partial; CMO pop-up/pause **not** a v1 AC | **Partial** chrome / **Missing from reqs** (toast + clock interrupt) → draft **CMD-39** |

---

## 5. Root-cause stack

```text
[Headless truth]          [Play Mode today]
BalticReplayHarness  -.->  SimplePlayModeSimHost (stub snapshot)
MvpEngagementResolver      ActiveEngagementCount = 0
DecisionLog                Seeded ORBAT + few log rows
        |                           |
        v                           v
MessageLogProjection (thin)   MapPictureProjection.Place(hash)
        |                           |
        v                           v
(no Unity toast host)         Static ■/◆ + rings/edges chrome
```

| Layer | Mechanism | Citation |
|-------|-----------|----------|
| Snapshot | Time, contacts, EMCON, alive — **no kinematics** | `src/ProjectAegis.Delegation.UnityAdapter/Bridge/ISimWorldSnapshot.cs` |
| Map layout | Deterministic hash → normalized (x,y) | `src/ProjectAegis.Delegation/Projection/MapPictureProjection.cs` |
| Editor tick | Stub host; engagements never increment | `unity/ProjectAegis/Assets/Scripts/Runtime/SimplePlayModeSimHost.cs` |
| Overlays | Rings + edges only (UI Toolkit) | `unity/ProjectAegis/Assets/Scripts/Runtime/MapCanvasOverlayRenderer.cs` |
| Log filter | Switch omits PolicyUpdate / AgentDecision | `src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs` |
| Alerts | Diff → message line helper; no toast panel | `src/ProjectAegis.Delegation/Projection/AttentionTierAlertProjection.cs` |
| Auto-pause | Gate only; callers own clock | `src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs` |

GitNexus query for “map movement kinematics” returned **definitions** (`MapPictureProjection`, `MapSymbolPool`, Cesium billboard projection) and **no movement-on-map execution process** — consistent with hash placement only. Index freshness: treat as potentially stale before any later implementation `impact`.

---

## 6. Tracker summary

| Source | Finding |
|--------|---------|
| **Linear** | Next live visual item: **DRG-162** overlay signoff (rings + edges). Not movement, not weapon tracks, not live event firehose. H1 C2 Runtime Depth. M4/M5 historically closed as headless/placeholder. |
| **GitHub** | Open issues not driving this gap; recent Unity housekeeping PRs unrelated to kinematic picture. |
| **Notion** | Unity UI Maturity Plan stale vs S121 DRAW/CESIUM/ASPECT; CMANO Menu Operations already fed CMD-16…30; Map/Rendering stub. |
| **Requirements** | REQ-20 (CMD-04/05/06/30.x), REQ-14 engage, REQ-17 RPL-14/07/08, ADR-007/010, art-bible §7. |
| **Perplexity / CMO parity** | Target is **2D icons + course lines + weapon tracks + message log + pop-up/pause + time compression** — not cinematic VFX. |
| **GitNexus** | Confirms projection/host symbols; no map-kinematics process. |

---

## 7. Recommended next tracks (do not start until owner picks)

| Track | Product bar | Scope sketch | Risk |
|-------|-------------|--------------|------|
| **DRG-162 only** | Rings + edges + HUD counts signed in Editor | Owner Game View; no new reqs | Lowest |
| **A — Live telemetry** | “Log and chrome look alive” | Richer `ISimWorldSnapshot` / recorded log replay; project `PolicyUpdate`; bind attention alerts to toast host; interactive CMD-04 | Low; already required residual |
| **B — CMO 2D battle picture** | “Units sail; shots draw as tracks” | Snapshot lat/lon or course/speed; stop hash as live layout; lerp (presentation-only); CMD-30.7 polylines + CMD-30.5 vectors; **CMD-38** kinematics | Medium; needs REQ land + ADR-007 Phase B wire |
| **C — Cinematic VFX** | Particles / flashes | Explicit reopen art-bible §7 | Scope change — default **reject** |

Hard constraints for any later code: **DelegationBridge zero-touch**, CatalogWriteGate extend-only, replay golden `17144800277401907079`, GitNexus `impact` before symbol edits.

**Track-choice:** awaiting owner AskQuestion answer (not invented here).

---

## 8. Citations (primary paths)

| Path | Role |
|------|------|
| `src/ProjectAegis.Delegation.UnityAdapter/Bridge/ISimWorldSnapshot.cs` | Snapshot contract (no kinematics) |
| `src/ProjectAegis.Delegation/Projection/MapPictureProjection.cs` | Hash placement |
| `src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs` | Thin message log |
| `src/ProjectAegis.Delegation/Projection/AttentionTierAlertProjection.cs` | Headless attention alerts |
| `src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs` | Headless auto-pause gate |
| `unity/ProjectAegis/Assets/Scripts/Runtime/SimplePlayModeSimHost.cs` | Play Mode stub |
| `unity/ProjectAegis/Assets/Scripts/Runtime/MapCanvasOverlayRenderer.cs` | Rings/edges |
| `unity/ProjectAegis/Assets/Scripts/Runtime/UnitOrderToolbarHost.cs` | `plot_course` order chrome |
| `unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs` | Compression label bind |
| `unity/ProjectAegis/PLAYMODE-SMOKE.md` | Reproduce + PanelSettings |
| `Game-Requirements/requirements/20-Command-And-Control-UI.md` | CMD-04/05/06/30.x |
| `Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md` | RPL-07/08/14 |
| `docs/architecture/adr-007-c2-map-presentation.md` | Phase A hash / Phase B world-anchor |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | UI as client |
| `design/art/art-bible.md` §7 | Combat VFX N/A |
| Spec drafts (this wave) | `docs/superpowers/specs/2026-08-17-cmd-38-kinematic-map-picture-draft.md`, `…-cmd-39-attention-toast-clock-interrupt-draft.md` |

---

*Audit authored 2026-08-17 from code/docs + approved plan explore summaries. No Editor screenshots invented.*
