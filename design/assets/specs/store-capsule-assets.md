# Asset Specs — Store: Capsule & Marketing (S70)

> **Source:** `production/release/store/asset-checklist.md` (S70-02)  
> **Art Bible:** `design/art/art-bible.md` §1–3 (palette, posture)  
> **Generated:** 2026-06-25  
> **Refined:** 2026-07-09 (S91 asset spec production)  
> **Status:** 14 assets **Specced** (production-ready) / 0 approved / 0 in production / 0 done  
> **Sprint:** S91 — ASSET-003 umbrella + store/marketing pack (023–035)  
> **Scope:** E7 prep placeholders only — **no store upload** per [`commercial-launch-scope-boundary-2026-06-25.md`](../../../production/commercial-launch-scope-boundary-2026-06-25.md)

---

## ASSET-003 — Store Capsule & Header Pack (umbrella)

| Field | Value |
|-------|-------|
| Category | Marketing |
| Format | PNG (capsules), SVG/PNG (logos) |
| Naming prefix | `ProjectAegis_` |

**Visual Description:**  
Near-future NATO C2 workstation aesthetic: matte dark surfaces, blue/gray naval palette, Baltic patrol silhouette with subtle C2 overlay — no arcade chrome or consumer-game saturation.

**Art Bible Anchors:** §1 Visual Identity; §3 Primary + affiliation palette; §9 prohibitions

**Acceptance Criteria:**
- [ ] Capsules readable at Steam thumbnail scale (616×353, 231×87)
- [ ] Screenshot capture list maps to Baltic/C2 scenarios (ASSET-027…034)
- [ ] **No store submission** in S91 — specs only

**Dependencies:** ASSET-042 evidence protocol; `production/release/store/asset-checklist.md`

**Verification:** Manual art review against art bible §9 prohibitions; file naming audit

**Status:** Specced

---

## ASSET-023 — Main Capsule (Steam)

| Field | Value |
|-------|-------|
| Category | Marketing |
| Dimensions | 616×353 px (Steam recommended); high-res master ≥1920 wide |
| Format | PNG |
| Naming | `ProjectAegis_BalticMainCapsule_v1.png` |

**Visual Description:**  
Baltic patrol theater silhouette (surface/naval contacts implied), dark `#080E16` field, faint map grid or APP-6 hostile diamond accent, title treatment legible at thumbnail scale.

**Generation Prompt:**  
Military simulation game capsule art, dark navy command center aesthetic, Baltic sea theater, minimalist NATO C2 UI overlay, blue and gray palette #4A9EFF #080E16, no neon, no characters, flat matte, professional Steam store capsule 616x353 — negative: arcade HUD, explosions, sci-fi neon, text clutter

**Status:** Specced

---

## ASSET-024 — Small Capsule (Steam)

| Field | Value |
|-------|-------|
| Category | Marketing |
| Dimensions | 231×87 px |
| Format | PNG |
| Naming | `ProjectAegis_SmallCapsule.png` |

**Visual Description:**  
Dedicated crop or simplified mark readable at small size; retain affiliation blue + dark field contrast.

**Status:** Specced

---

## ASSET-025 — Logo / Header Variants

| Field | Value |
|-------|-------|
| Category | Marketing |
| Dimensions | Multiple (library header, community banner) |
| Format | PNG alpha + SVG source |

**Visual Description:**  
Wordmark + icon suitable for Steam library header and Discord/community avatar crops.

**Status:** Specced

---

## ASSET-026 — Press Kit Logo Pack

| Field | Value |
|-------|-------|
| Category | Marketing |
| Dimensions | Logo sheets + fact sheet PDF link |
| Format | ZIP / folder |

**Visual Description:**  
Logo on dark and light backgrounds; links to `production/release/release-checklist-v3.md` and `production/release/launch/evidence-index.md`.

**Status:** Specced

---

## Store screenshots (ASSET-027 — ASSET-034)

All captures: **1920×1080 PNG**, UI scale 100%, per art bible §8 evidence protocol.  
Target folder: `production/assets/screenshots/` (future) or `production/qa/evidence/`.

| Asset ID | Filename (proposed) | Scenario / subject |
|----------|---------------------|-------------------|
| ASSET-027 | `screenshot-01-c2-map-patrol.png` | C2 map, band B datalink contacts |
| ASSET-028 | `screenshot-02-policy-panel.png` | ROE/EMCON/WRA on mission-roe policy |
| ASSET-029 | `screenshot-03-order-log-replay.png` | Order log + replay scrubber + hash |
| ASSET-030 | `screenshot-04-combat-domains.png` | Combat domains hot-tick |
| ASSET-031 | `screenshot-05-sensor-ew-jammed.png` | Jammed / comms-challenged |
| ASSET-032 | `screenshot-06-catalog-panel.png` | Platform catalog / mission editor |
| ASSET-033 | `screenshot-07-band-c-intercept.png` | Band C intercept / spoof stress |
| ASSET-034 | `screenshot-08-replay-aar.png` | Full replay AAR — log vs world |

**Status (screenshots 027–034):** Specced — capture specs ready; binary production deferred until Editor host available (S70 checklist authority)

---

## ASSET-035 — Baltic Trailer Draft

| Field | Value |
|-------|-------|
| Category | Video |
| Dimensions | 1920×1080 master; 1–2 min |
| Format | MP4 (future) |
| Naming | `ProjectAegis_BalticTrailer_Draft.mp4` |

**Visual Description:**  
Vertical slice arc: Baltic patrol start → delegation decision → replay verify (hash stable). Script from S46 launch template + `baltic-v2-narrative-arc` policy beats.

**Status:** Specced (deferred — not produced in S70)

---

## Production checklist

- [ ] User review of capsule composition direction  
- [ ] Editor host PNG batch (B3 deferred 2026-06-25)  
- [ ] Art director sign-off against art bible §9 prohibitions  
- [ ] Link delivered assets in `design/assets/asset-manifest.md` (Status → Done)
