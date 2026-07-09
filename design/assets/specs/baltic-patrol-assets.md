# Asset Specs — Scenario: Baltic Patrol

> **Source:** `production/release/store/asset-checklist.md`, Baltic v2/v3 playtest corpus, `design/ux/onboarding-baltic.md`, `design/gdd/cyber-comms-degradation.md`, `design/gdd/combat-domains-damage.md`  
> **Art Bible:** `design/art/art-bible.md` (lean B2 — map/UI only)  
> **Generated:** 2026-06-25  
> **Refined:** 2026-07-09 (S91 asset spec production)  
> **Status:** 7 assets **Specced** (production-ready) / 0 approved / 0 in production / 0 done  
> **Sprint:** S91 — ASSET-002 umbrella + Baltic theater children (018–022, 039)  
> **Authority:** [`post-editor-hygiene-scope-boundary-2026-07-09.md`](../../../production/post-editor-hygiene-scope-boundary-2026-07-09.md), Baltic v2/v3 corpora frozen (hash `17144800277401907079`)

---

## ASSET-002 — Baltic Patrol Theater Presentation (umbrella)

| Field | Value |
|-------|-------|
| Category | Environment / UI |
| Dimensions | 1920×1080 map region |
| Format | USS + scenario policy bindings |
| Naming | `baltic-patrol-*`, `baltic-v2-*`, `baltic-v3-*` policy family |

**Visual Description:**  
Theater-level Baltic patrol presents naval contacts on map placeholder with APP-6 symbology; comms degradation scenarios drive opacity/ghost modifiers per art bible §2 — no environmental 3D assets in v1.

**Art Bible Anchors:** §2 mood modes (Executing, Degraded, Denied); §6 map modifiers

**Acceptance Criteria:**
- [ ] Band B/C and comms-challenged captures match store checklist scenarios
- [ ] No Baltic v2 replay hash change without ADR
- [ ] ReplayGolden **6/6** preserved

**Dependencies:** ASSET-009 map canvas, ASSET-010 comms modifiers, `production/playtests/baltic-v2-scenario-manifest.yaml`

**Verification:** Replay golden suite; policy-specific PlayMode smoke where applicable

**Status:** Specced

---

## ASSET-018 — Baltic Patrol Map Framing

| Field | Value |
|-------|-------|
| Category | Environment |
| Dimensions | Full map canvas |
| Format | Map placeholder panel + policy seed |

**Visual Description:**  
Cool neutral-cool panel stack; map field `#080E16`; theater label in `text-muted`; contacts as hostile diamonds / friendly squares per scenario OOB. No decorative ocean VFX.

**Source:** `production/playtests/baltic-v2-scenario-manifest.yaml`, v3 manifest prep

**Status:** Specced

---

## ASSET-019 — Band B Datalink Contact Presentation

| Field | Value |
|-------|-------|
| Category | UI / Map |
| Dimensions | Per-contact symbols |
| Format | USS `map-symbol--*` + policy state |

**Visual Description:**  
Live contacts at full opacity with datalink-active presentation; suitable for store screenshot #1 (asset-checklist). Use `baltic-v2-patrol-band-b` or v3 equivalent policy for capture.

**Status:** Specced

---

## ASSET-020 — Comms-Challenged / Jammed Capture Set

| Field | Value |
|-------|-------|
| Category | UI / Map |
| Dimensions | Map + top bar comms line |
| Format | Scenario `baltic-v2-comms-challenged`, `baltic-v2-jammed`, v3 comms policies |

**Visual Description:**  
Degraded amber comms line; stale hostile symbols 55%; optional ghost duplicates with italic lag suffix. Store screenshot #5 target.

**Art Bible Anchors:** §2 Degraded comms; §3 `comms-degraded`, `ghost-track`

**Status:** Specced

---

## ASSET-021 — Combat Domains Hot-Tick Overlay

| Field | Value |
|-------|-------|
| Category | HUD |
| Dimensions | Map + log adjunct |
| Format | C2 projection from combat-domains scenario |

**Visual Description:**  
Static HUD surfacing domain engagement state during hot-tick (`baltic-patrol-combat-domains` family) — no arcade VFX; log categories carry KILL tint `#FFB4A0`.

**Source:** `design/gdd/combat-domains-damage.md`; asset-checklist screenshot #4

**Status:** Specced

---

## ASSET-022 — Band C Intercept / Spoof Stress Layout

| Field | Value |
|-------|-------|
| Category | UI / Map |
| Dimensions | Full C2 layout under high cognitive load |
| Format | `baltic-v2-intercept`, `baltic-v2-spoof`, v3 band C policies |

**Visual Description:**  
Dense contact picture with suspect/pending frames; message log scroll required; demonstrates information-density rules at band C. Store screenshot #7 + playtest thinkaloud reference.

**Source:** `design/difficulty-curve.md` band C; `production/qa/evidence/baltic-v2-playtest-index.md`

**Status:** Specced

---

## ASSET-039 — Onboarding Baltic Overlay

| Field | Value |
|-------|-------|
| Category | UI Screen |
| Dimensions | Overlay on C2 shell |
| Format | UXML overlay (P1) |

**Visual Description:**  
First-run guidance for Baltic patrol flow; restrained typography matching C2 chrome; dismissible without blocking sim determinism.

**Source:** `design/ux/onboarding-baltic.md`

**Status:** Specced (P1)

---

## Capture notes (B3 advisory)

Live Editor PNG re-capture **deferred** 2026-06-25 (no Unity Editor host in cloud VM). Headless PlayMode smoke **18/18** remains merge authority. When Editor host available, capture scenarios listed in `production/release/store/asset-checklist.md` at 1920×1080 into `production/assets/screenshots/` or `production/qa/evidence/`.
