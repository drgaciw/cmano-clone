# Asset Specs — System: Command & Control UI

> **Source:** `design/gdd/command-and-control-ui.md`, `design/ux/c2-command-post.md`, `design/art/art-bible.md`  
> **Art Bible:** `design/art/art-bible.md` (lean B2)  
> **Generated:** 2026-06-25  
> **Status:** 11 assets specced (stub) / 0 approved / 0 in production / 0 done  
> **Review:** Stub for Phase B dashboard gap — expand via `/asset-spec system:command-and-control-ui --review full`

---

## ASSET-004 — APP-6 Frame Atlas

| Field | Value |
|-------|-------|
| Category | Sprite / 2D |
| Dimensions | 112×16 px strip (7× 16×16 frames) |
| Format | PNG alpha |
| Naming | `App6FrameAtlas.png` |
| Texture Res | Tier 1 — single atlas strip |

**Visual Description:**  
Seven affiliation frames: friendly square, hostile diamond, neutral, suspect, pending, unknown, friendly-destroyed (strikethrough). Shape is primary identity cue; fill minimal per NATO APP-6 cues.

**Art Bible Anchors:**
- §3 Shape Language: affiliation frames + semantic colors
- §4 Iconography: `map-app6-frame--{affiliation}` classes

**Generation Prompt:**  
Flat military symbology sprite sheet, 16×16 pixel icons, dark navy UI background sample, APP-6 style frames, no gradients, crisp 1px lines, transparent PNG — *deferred; use existing atlas path.*

**Status:** Needed

---

## ASSET-005 — C2 Top Bar Panel

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | 1920×48 px layout region |
| Format | UXML + USS |
| Naming | `C2TopBarPanel.{uxml,uss}` |

**Visual Description:**  
Matte `#0A101A` bar with clock, `1x 4x 8x` compression presets, SIDE/MODE labels, comms line (nominal green / degraded amber / denied red), Begin Execution CTA as sole warm accent in planning.

**Art Bible Anchors:** §2 Executing/Degraded modes; §3 `surface-topbar`, `comms-*`

**Status:** Needed

---

## ASSET-006 — Message Log Panel

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | Full width × 120px min (floating max 220px) |
| Format | UXML + USS |
| Naming | `MessageLogPanel.{uxml,uss}` |

**Visual Description:**  
10px monospace rows ~14px height; category tints for KILL, MAGAZINE, COMMS, CONTACT, MISSION; scroll beyond 12 visible rows; evidence-grade static styling.

**Art Bible Anchors:** §1 "every pixel serves the order log"; §3 message log categories

**Status:** Needed

---

## ASSET-007 — Left Drawer Panel

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | 240px width |
| Format | UXML + USS |
| Naming | `C2LeftDrawerPanel.{uxml,uss}` |

**Visual Description:**  
Tabbed OOB / missions / contacts; OOB rows 20px; planning-readonly modifier mutes tabs and borders (`#465260`).

**Status:** Needed

---

## ASSET-008 — Right Unit Detail Panel

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | 320px width |
| Format | UXML + USS |

**Visual Description:**  
Selection readout with 10px mono for numeric/catalog fields; sans 12px section headers; no avatar portrait.

**Status:** Needed

---

## ASSET-009 — Map Placeholder Canvas

| Field | Value |
|-------|-------|
| Category | UI / Environment |
| Dimensions | Flex center (1920×1080 baseline) |
| Format | UXML + USS + atlas reference |

**Visual Description:**  
`#101824` canvas on `#080E16` field; symbols use APP-6 atlas; planning mode 42% opacity + dim overlay.

**Status:** Needed

---

## ASSET-010 — Map Comms Degradation Modifiers

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | Per-symbol overlay |
| Format | USS modifiers |

**Visual Description:**  
Stale 55% opacity; frozen 35%; ghost duplicate at lag offset with italic `(lag N)` suffix — no particles.

**Art Bible Anchors:** §2 Degraded/Denied comms; §7 VFX N/A

**Status:** Needed

---

## ASSET-011 — Delegation Badge Overlay

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | Text badge readable at 16px on map |
| Format | UI Toolkit label |

**Visual Description:**  
Text-only `Human` / `Agent` / `Mixed` badges; icon atlas deferred post-v1.

**Status:** Needed

---

## ASSET-012 — Policy Denial + EMCON HUD

| Field | Value |
|-------|-------|
| Category | HUD |
| Dimensions | Inline tooltips + map emitter icon |
| Format | USS + bridge-driven state |

**Visual Description:**  
Weapon greyed at 45% opacity when denied; tooltip shows policy abort reason; EMCON emitter icon on unit per policy GDD P0.

**Source:** `design/gdd/policy-roe-emcon-wra.md` §Visual/Audio

**Status:** Needed

---

## ASSET-013 — Replay Scrubber / Hash Overlay

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | Bottom log adjunct or top bar region |
| Format | UXML + USS (read-only) |

**Visual Description:**  
Replay mode disables order affordances; optional golden hash display for QA evidence captures.

**Status:** Needed

---

## ASSET-014 — AegisTokens.uss

| Field | Value |
|-------|-------|
| Category | UI tokens |
| Dimensions | N/A |
| Format | USS |
| Naming | `Assets/UI/AegisTokens.uss` (proposed) |

**Visual Description:**  
Shared import of §3 palette + §6 spacing/border tokens for all C2 and Platform Editor panels.

**Status:** Needed

---

## ASSET-015 — Platform Catalog Panel

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | min-width 260px |
| Format | `PlatformCatalogPanel.{uxml,uss}` |

**Visual Description:**  
Read-only catalog browse; sectional COMMS/LINK lists; calm reference-library tone; Export/Diff text buttons 64px min-width.

**Status:** Needed

---

## ASSET-016 — Platform Import Staging Panel

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | min-width 280px |
| Format | `PlatformImportPanel.{uxml,uss}` |

**Visual Description:**  
Audit-queue tone; diff list is hero flex child; Approve disabled @ 45% until acknowledge checkbox set.

**Status:** Needed

---

## ASSET-017 — Staging Diff Row Styles

| Field | Value |
|-------|-------|
| Category | UI |
| Dimensions | ~16px mono rows |
| Format | USS classes `diff-{changed|added|removed|blocked}` |

**Visual Description:**  
Prefix token colors per art bible §3; entity keys in `text-data` mono.

**Status:** Needed

---

## ASSET-038 — Mission Planning RTwP Screen

| Field | Value |
|-------|-------|
| Category | UI Screen |
| Dimensions | Full 1920×1080 |
| Format | C2 hosts with planning modifiers |

**Visual Description:**  
Same shell as C2 Command Post with map dimmed, drawer read-only, Begin Execution CTA highlighted.

**Status:** Needed

---

## ASSET-042 — Evidence Capture Protocol

| Field | Value |
|-------|-------|
| Category | QA / Presentation |
| Dimensions | 1920×1080 exactly |
| Format | PNG |
| Naming | `{feature}-{context}-s{NN}-{descriptor}.png` |
| Location | `production/qa/evidence/` |

**Visual Description:**  
Game view captures at 100% UI scale; documents scene, UXML bindings, proxy test filter in README per sprint batch.

**Art Bible Anchors:** §8 Asset Standards — Evidence captures

**Status:** Needed
