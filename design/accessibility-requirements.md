# Accessibility Requirements — Project Aegis (Standard Tier)

> **Status:** Committed — Sprint 35 (S35-03)  
> **Version:** 1.0  
> **Last Updated:** 2026-06-19  
> **Tier:** **Standard** (WCAG 2.1 AA targets for C2 Command Post + Platform Editor)  
> **Scope:** Polish Phase 1 — Baltic vertical slice player-facing surfaces only  
> **Out of scope:** Delegation badges UX, globe/Cesium, full input remapping implementation (stubs only)  
> **Related:** [art-bible.md](art/art-bible.md), [c2-command-post.md](ux/c2-command-post.md), [c2-map-placeholder.md](ux/c2-map-placeholder.md), [interaction-patterns.md](ux/interaction-patterns.md)

---

## 1. Tier Commitment

Project Aegis adopts the **Standard** accessibility tier for all Polish Phase 1 screens:

| Surface | Host / scene | Tier |
|---------|--------------|------|
| C2 Command Post (Play Mode) | `C2LeftDrawerPanelHost`, `MapPanelHost`, `UnitDetailPanelHost`, `MessageLogHost`, `C2TopBarHost` | Standard |
| Platform Editor (Editor Mode) | `PlatformCatalogViewerHost`, `PlatformImportPanelHost` | Standard |

**Standard tier means:** WCAG 2.1 Level AA contrast and scaling targets; keyboard-operable primary flows; reduced-motion alternatives for non-essential animation; colorblind-safe affiliation and comms affordances per [art-bible.md §3 Colorblind safety](art/art-bible.md#colorblind-safety). Full AAA, screen-reader certification, and gamepad-complete navigation are **deferred** post-Baltic slice.

---

## 2. Contrast Ratios (WCAG AA)

All token pairs below use [art-bible.md §3 Color Palette](art/art-bible.md#3-color-palette) canonical hex values on the `surface-panel` / `surface-map` backgrounds.

### 2.1 Text on panels

| Foreground token | Background | Min ratio | WCAG AA (normal text) | WCAG AA (large text ≥18px / 14px bold) |
|------------------|------------|-----------|------------------------|----------------------------------------|
| `text-heading` `#C8D2DC` | `surface-panel` `#0C121C` | **11.2:1** | Pass | Pass |
| `text-body` `#B4BEC8` | `surface-panel` | **9.1:1** | Pass | Pass |
| `text-muted` `#8C9BAA` | `surface-panel` | **5.8:1** | Pass | Pass |
| `text-data` `#D2DCE6` | `surface-panel` | **12.1:1** | Pass | Pass |
| `text-topbar` `#BEC8D2` | `surface-topbar` `#0A101A` | **9.5:1** | Pass | Pass |

**Rule:** Do not place `text-muted` on `surface-map` at reduced opacity without re-checking ratio; use `text-body` minimum for evidence-critical rows (message log, staging diff).

### 2.2 Semantic / affiliation on map

| Pair | Min ratio vs `surface-map` `#101824` | Notes |
|------|--------------------------------------|-------|
| `affil-friendly` `#4A9EFF` + shape `■` | **5.1:1** (label) | Shape is primary cue — see §5 |
| `affil-hostile` `#E85D5D` + shape `◆` | **4.8:1** (label) | Shape is primary cue |
| `selected-ring` `#FFC850` | **8.9:1** (ring on map) | 1px ring + 15% fill per art bible |
| `comms-degraded` `#F0B450` (top bar) | **7.2:1** | Paired with text label `COMMS: DEGRADED` |
| `comms-denied` `#E65A5A` (top bar) | **4.9:1** | Paired with text label `COMMS: DENIED` |

### 2.3 Platform Import staging diff

| Change kind | Token | Hex | Contrast vs panel |
|-------------|-------|-----|-------------------|
| `CellChanged` | `diff-changed` | `#E6C850` | **7.8:1** |
| `RowAdded` | `diff-added` | `#64C88C` | **6.2:1** |
| `RowRemoved` | `diff-removed` | `#E85D5D` | **4.8:1** + text prefix `removed` |
| Blocked | `diff-blocked` | `#E65A5A` | **4.9:1** + status line text |

**Rule:** Staging diff rows always include **entity key prefix** (`COMMS row=`, `LINK row=`, `DAMAGE row=`) — never color-only diff lines. See [art-bible.md §3 Semantic — Platform Import staging diff](art/art-bible.md#semantic--platform-import-staging-diff-target-uss--phase-e).

### 2.4 Non-text UI (focus, selection)

| Element | Requirement |
|---------|-------------|
| Focus indicator | **3:1** contrast against adjacent colors (WCAG 2.1 1.4.11) |
| OOB selected row | `selected-row` — 3px left bar `#4A9EFF` + 25% background (not fill-only) |
| Disabled Approve button | 45% opacity + `disabled` pseudo-state class; label remains readable |

---

## 3. Text Scaling

| Surface | Baseline @ 1080p | Scaling rule |
|---------|------------------|--------------|
| Message log | 10px monospace | **P1:** player setting 100% / 125% / 150% scales log font; row height adjusts; max visible rows may shrink (scroll retained) |
| OOB / catalog lists | 12px sans | Scale with global UI scale setting; **floor 10px** for evidence captures |
| Top bar / chrome | 12px sans | Scale proportionally; no truncation of `COMMS:` state label |
| Map labels | 14px sans | Scale to 18px max before collision; ghost suffix `(lag N)` stays italic |
| Staging diff | 10px mono | Same scale curve as message log |

**Implementation stub (P1):** `C2AccessibilitySettings.ScalePercent` enum `{100, 125, 150}` applied to USS `font-size` custom properties on log, OOB, and diff hosts. Default **100%** until settings UI ships.

**Hard floor:** Never render evidence-bearing text below **10px** at 100% scale ([art-bible.md §4 Minimum legibility](art/art-bible.md#hierarchy-rules)).

---

## 4. Reduced Motion

Per [c2-command-post.md §9 Accessibility](ux/c2-command-post.md#9-accessibility-screen-level) and [c2-map-placeholder.md §Acceptance](ux/c2-map-placeholder.md#acceptance):

| Motion type | Default | Reduced-motion alternative |
|-------------|---------|----------------------------|
| Drawer tab switch | Instant (no slide) | Same — already compliant |
| Map selection ring | Instant outline | No pulse, no scale tween |
| Map pan / zoom | Deferred (globe Phase B) | No inertia when implemented |
| Comms ghost symbol | Static offset position | No drift animation; opacity step only |
| Top bar PAUSE state | Bold label, no pulse | No decorative freeze VFX |
| Staging diff list refresh | Instant rebind | No row highlight sweep |
| Platform Editor approve gate | Opacity step 45% → 100% | No button bounce |

**Implementation stub (P1):** Respect OS `prefers-reduced-motion` via USS class `.reduced-motion` on root UIDocument; when set, disable any future pan inertia and selection pulse tweens.

**Evidence rule:** Replay captures at tick *N* must match tick *N* visually — no frame-varying decorative motion on C2 or Platform Editor ([art-bible.md §1 Evidence-grade clarity](art/art-bible.md#supporting-principles)).

---

## 5. Colorblind-Safe Affiliation & State

All affiliation and comms state encoding follows [art-bible.md §3 Colorblind safety](art/art-bible.md#colorblind-safety):

| State | Primary cue | Secondary cue | Tertiary |
|-------|-------------|---------------|----------|
| Friendly | APP-6 square frame / `■` | `affil-friendly` blue | `unitId` text label |
| Hostile | APP-6 diamond / `◆` | `affil-hostile` red | `contactId` + lifecycle text |
| Neutral / suspect / pending / unknown | APP-6 frame shape per token | Hue per art bible | Label always present |
| Destroyed friendly | Strikethrough label | 50% opacity | `affil-friendly-dead` token |
| COMMS nominal → degraded → denied | Top bar **text** `COMMS: {state}` | Opacity ladder 100% → 55% → 35% on hostile tracks | Ghost duplicate `◌` at lag offset (degraded only) |
| List selection | 3px **left border bar** | Row background tint | Not hue-only |

**Never:** Encode affiliation, comms health, or staging change type by color alone.

---

## 6. Keyboard Focus & Navigation

### 6.1 Focus order (C2 — MVP)

Per [c2-command-post.md §6 Interaction Map](ux/c2-command-post.md#6-interaction-map-mvp):

```
Top bar → Left drawer (active tab ListView) → Message log → Right panel (read-only)
```

| Zone | Keyboard behavior | Status |
|------|-------------------|--------|
| Left drawer tabs | Arrow keys within tab toolbar; Enter activates tab | Implemented (Unity 6 ListView default) |
| OOB / contacts list | Up/Down row navigation; Enter selects row | Implemented |
| Message log | Up/Down scroll; Enter selects row (P1 → focus unit) | Partial — selection P1 |
| Map symbols | Not keyboard-pickable in placeholder | **Stub P2** — see §6.3 |
| Top bar (pause, compression) | Hotkeys `Space`, `1–4` | Implemented (doc 03) |
| Right panel | Read-only; no trap | N/A |

### 6.2 Focus order (Platform Editor)

```
Workbook path field → Action buttons (Export, Diff, Propose) → Diff list → Acknowledge checkbox → Approve
```

| Control | Keyboard | Status |
|---------|----------|--------|
| Propose / Approve | Focusable buttons; Approve `disabled` until acknowledge | Implemented |
| Staging diff list | Scroll via arrow keys when focused | Implemented |
| Acknowledge checkbox | Space toggles; gates Approve | Implemented |
| Catalog search / list | Tab into search → list → detail sections | Implemented |

### 6.3 Input remapping stubs (P2 — not Polish Phase 1)

Documented for future settings screen; **no implementation required for S35-03**.

| Action | Default binding | Remappable stub ID |
|--------|-----------------|-------------------|
| Pause / resume | `Space` | `input.pause` |
| Time compression 1x / 4x / 8x | `1` / `2` / `3` / `4` | `input.time_compression.{preset}` |
| Close modal / cancel intent | `Esc` | `input.cancel` |
| Center on primary hostile | `F` | `input.focus_primary_threat` |
| Cycle drawer tab | `Ctrl+1/2/3` | `input.drawer_tab.{oob\|missions\|contacts}` |

**Stub contract:** Remap table stored in player settings JSON; sim hotkeys read resolved binding at session start; order log records binding profile id on session start (determinism note for replay).

---

## 7. Screen-Specific Requirements

### C2 Command Post

Cross-reference [c2-command-post.md](ux/c2-command-post.md):

- Affiliation shape + color on map and OOB (§5 above).
- Message log: scalable font; ≥12 visible rows with scroll at 100% scale.
- Focus: drawer ListView keyboard navigable.
- Reduced motion: instant tab switch, no map pan inertia.
- COMMS degrade: opacity + ghost + top bar text — validated in `baltic-patrol-comms` ([c2-map-placeholder.md](ux/c2-map-placeholder.md)).

### Platform Editor

Cross-reference [art-bible.md §6 Platform Catalog vs Import panels](art/art-bible.md#platform-catalog-vs-import-panels):

- Text buttons only (`Export`, `Diff`, `Propose`, `Approve`) — no icon-only actions.
- Approve disabled at 45% opacity until acknowledge — must remain readable (§2.4).
- Staging diff monospace with prefix tokens — see [interaction-patterns.md §Platform Import](ux/interaction-patterns.md#platform-import-staging).

---

## 8. Verification Checklist (QA)

| Check | Pass condition |
|-------|----------------|
| Tier named | This doc declares **Standard** tier |
| Contrast table | All committed token pairs meet AA for their text size class |
| Scaling floor | No evidence text below 10px @ 100% |
| Reduced motion | No required motion for comprehension on C2 or import flows |
| Affiliation | Map symbol identifiable by shape alone in grayscale capture |
| Keyboard | OOB select + drawer tabs operable without pointer |
| Cross-links | art-bible + c2-command-post referenced |

---

## 9. Deferred (Post–Baltic Slice)

- WCAG AAA contrast on all semantic hues
- Full screen-reader labels for map symbology (ARIA-equivalent in UI Toolkit)
- Complete gamepad navigation (noted in [c2-command-post.md §6](ux/c2-command-post.md#6-interaction-map-mvp))
- Live input remapping UI
- Delegation badge readability at 16px ([art-bible.md §4 Iconography](art/art-bible.md#iconography)) — **out of Polish Phase 1 scope**

---

## Lean Review (2026-06-19)

**Reviewer:** team-ui (lean doc review per S35-03 / gate r2 residual #2)  
**Verdict:** **APPROVED WITH NOTES**

| Criterion | Result |
|-----------|--------|
| Tier committed (Standard) | Pass |
| C2-relevant requirements (contrast, scaling, motion, affiliation) | Pass |
| Platform Editor staging readability | Pass |
| Cross-links to art-bible + c2-command-post | Pass |
| Keyboard remapping | Stub only — acceptable per polish-scope-boundary |
| Delegation badges | Correctly out of scope |

**Notes (non-blocking):**

1. **P1 scaling setting** not yet wired in Unity — ratio table assumes committed USS tokens; verify on first Editor capture.
2. **Map keyboard picking** remains P2 stub; OOB row selection is the accessible path until globe Phase B.
3. **COMMS legend** called out in playtests — add HUD tooltip copy in a future UX polish story (not S35-03).
4. Closes gate r2 residual **#2** (accessibility tier verified) alongside `design/ux/interaction-patterns.md`.

**Gate trace:** `production/gate-checks/production-to-polish-2026-06-19-r2.md` — "Accessibility tier verified" gap closed by this document.