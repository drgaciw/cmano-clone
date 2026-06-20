# Art Bible: Project Aegis (Lean — C2 & Platform Editor; B2 Complete)

> **Status:** B2 Complete (lean v1 for Baltic slice) — S43-05  
> **Version:** 1.0 (B2)  
> **Last Updated:** 2026-06-20  
> **Owned By:** art-director / team-ui  
> **Scope:** C2 Command Post + Platform Editor UI only; world/character/VFX deferred post-Baltic slice (per release-enablement-scope-boundary-2026-06-20.md B2)  
> **Sources:** [game-concept.md](../gdd/game-concept.md), [c2-command-post.md](../ux/c2-command-post.md), [c2-map-placeholder.md](../ux/c2-map-placeholder.md), [interaction-patterns.md](../ux/interaction-patterns.md), [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md), [ADR-011](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md), Unity USS under `unity/ProjectAegis/Assets/UI/`  
> **Art Director Sign-Off (AD-ART-BIBLE):** FULL VERDICT — APPROVED lean v1 (S43); §1–9 complete per B2; §5/§7 N/A explicit for v1. Cites S42 closeout + S41 ack. (art-director + team-ui + csharpexpert)
>
> **S36-11 COMPLETE (QA/DevOps/Hygiene — team-ui / ui-experience-lead isolated):** Facilitation note added per story. Lean Polish: bible remains lean/draft for C2+Editor scope only. Verdict: ACCEPTED WITH CONDITIONS (carryover from gate-check #2; full sign-off deferred post-Baltic or when non-lean mode). No new sections authored (existing complete; no placeholders touched). UX/UI alignment cross-ref intact with interaction-patterns + c2-command-post. (ui specialist track)
> **S38-03 / S38-11 (Art/UX):** Sign-off + residual UX/doc polish complete. No new sections. Cross-refs verified. (lean; isolated track)
> **S39-07/09 (Evidence/Playtest + Art/UX residual):** Minimal playtest 11 entry (production/playtests/README.md) + lean cross-ref for C2/Platform polish (density/tooltips/surfacing S39-03). PNG/playtest 11 uses proxy + existing s37 PNGs (c2-graph-viewer-s37.png etc). No new sections. (isolated track; cites S39 plan + qa-plan + boundary)
> **S42-05 (B2 start):** §1–4 expanded + tokens context. COMPLETE per smoke-sprint-42-closeout-2026-06-20.md (cites release-enablement-scope-boundary + scope-expansion-decision-2026-06-20-S41-close.md "i provide the ack")
> **S43-05 (B2 complete):** §5–9 + asset specs expanded (lean v1). N/A for §5/§7. §8 specs + §9 sign-off + full verdict. Worktree stack/sprint43/art-bible-complete. Budget held. (this pass; parallel independent)

---

## 1. Visual Identity Statement

### One-line rule

**Every pixel serves the order log — if it cannot be cited in replay evidence, it does not belong on screen.**

### Supporting principles

| Principle | Pillar served | Design test |
|-----------|---------------|-------------|
| **Calm command authority** | Agentic command | When a panel could shout (flashing chrome, oversized icons), choose restrained typography, stable layout, and state expressed through log lines and badges — not motion. |
| **Information density without clutter** | Simulation fidelity | When two data points compete for the same row, prefer monospace tabular columns and category color over extra panels; if a row cannot be read at 10px at 1080p, split or scroll — never shrink below minimum. |
| **Evidence-grade clarity** | Determinism | When styling could vary by frame or RNG (particles, idle shimmer, decorative map VFX), choose static opacity, shape, and hex values so a 1920×1080 capture at tick *N* matches tick *N* on replay. |

### Near-future posture

UI reads as a **2030s NATO C2 workstation**: matte dark surfaces, APP-6/NATO symbology cues, and catalog tooling that feels like a curated intelligence product — not a consumer game HUD.

---

## 2. Mood & Atmosphere

Emotional targets are **mode-driven**, not biome-driven. C2 mood is carried by surface luminance, map opacity, and comms chroma — not environmental lighting.

### Planning (RTwP — before Begin Execution)

| Attribute | Target |
|-----------|--------|
| **Primary emotion** | Deliberate preparation — the fight is not yet live |
| **Lighting character** | Cool, low-contrast; map and drawer visually subordinate to planning chrome |
| **Descriptors** | muted, staged, read-only, anticipatory, orderly |
| **Energy** | Contemplative |
| **Visual carriers** | Map canvas at **42% opacity** + `map-planning-dim-overlay` (`rgba(4,8,14,0.35)`); left drawer `c2-drawer-panel--planning-readonly` (border `#465260`, panel opacity **0.88**, tabs **0.65**); **Begin Execution** CTA in top bar (`#285A3C` fill, `#50A064` border) is the single warm accent |

### Executing (sim ticking, comms nominal)

| Attribute | Target |
|-----------|--------|
| **Primary emotion** | Analytical stress under control — high load, clear hierarchy |
| **Lighting character** | Neutral-cool panels; map canvas at full brightness; top bar anchors temporal authority |
| **Descriptors** | live, dense, legible, accountable, clock-driven |
| **Energy** | Measured |
| **Visual carriers** | Full-opacity map (`#101824` canvas on `#080E16` field); live affiliation colors at 100%; message log categories tinted by event type; top bar comms **nominal** green `#64C88C` |

### Degraded comms (stale picture — Baltic `baltic-patrol-comms`)

| Attribute | Target |
|-----------|--------|
| **Primary emotion** | Distrust of the picture — "what I see may not be what they see" |
| **Lighting character** | Desaturated live tracks; duplicate ghost geometry; top bar comms shifts warm-warning |
| **Descriptors** | stale, lagged, duplicated, faded, italic |
| **Energy** | Uneasy but not panicked |
| **Visual carriers** | Hostile live symbol **55% opacity** (`map-symbol--stale`); **ghost** duplicate at lag offset (`map-symbol--ghost`, `#B47878`, italic label suffix `(lag N)`); top bar comms **degraded** amber `#F0B450`; friendly tracks remain full brightness (player still owns own C2) |

### Denied comms (frozen picture)

| Attribute | Target |
|-----------|--------|
| **Primary emotion** | Isolation — last known picture only |
| **Lighting character** | Flat, dim, frozen; no implied motion |
| **Descriptors** | frozen, dim, static, last-known, cut off |
| **Energy** | Stalled |
| **Visual carriers** | All map symbols **35% opacity** (`map-symbol--frozen`); top bar comms **denied** red `#E65A5A`; no ghost duplicates (nothing new to mislead) |

### Paused (tick frozen)

Same palette as executing, with top bar **PAUSE** state lit (bold label; no pulsing). Log remains scrollable; no decorative freeze-frame VFX.

---

## 3. Color Palette

All values are **canonical** — USS files and new work must reference these tokens, not ad-hoc RGB.

### Primary (surfaces & chrome)

| Token | Hex | Role |
|-------|-----|------|
| `surface-deep` | `#080E16` | Map field, deepest backdrop |
| `surface-map` | `#101824` | Map canvas, theater board |
| `surface-panel` | `#0C121C` @ 92% alpha | Side panels, log, Platform Editor shells |
| `surface-topbar` | `#0A101A` @ 95% alpha | C2 top bar |
| `border-subtle` | `#324150` | Top bar bottom edge, map frame |
| `border-panel` | `#384860` | Floating log border |
| `border-section` | `#506482` @ 60% alpha | Platform Catalog section dividers |
| `border-planning` | `#465260` | Planning-readonly drawer frame |

### Secondary (typography & chrome text)

| Token | Hex | Role |
|-------|-----|------|
| `text-heading` | `#C8D2DC` | Panel titles, theater label (bold) |
| `text-body` | `#B4BEC8` | Unit detail lines, catalog detail |
| `text-muted` | `#8C9BAA` | Theater labels, tertiary hints |
| `text-data` | `#D2DCE6` | Message log default row |
| `text-topbar` | `#BEC8D2` | Top bar clock, side, mode |

### Semantic — affiliation (map & OOB)

Shape is primary; color reinforces. Never encode affiliation by color alone.

| Token | Hex | Shape cue | Role |
|-------|-----|-----------|------|
| `affil-friendly` | `#4A9EFF` | Filled square `■` / APP-6 friendly frame | Blue force, own units |
| `affil-hostile` | `#E85D5D` | Diamond `◆` / APP-6 hostile frame | Threat contacts |
| `affil-neutral` | `#78C878` | APP-6 neutral frame | Non-combatant / neutral |
| `affil-suspect` | `#E6C850` | APP-6 suspect frame | Ambiguous identity |
| `affil-pending` | `#B4B4DC` | APP-6 pending frame | Unresolved classification |
| `affil-unknown` | `#8C9BAA` | APP-6 unknown frame | Insufficient ID |
| `affil-friendly-dead` | `#5A6E82` @ 50% opacity | Strikethrough label | Destroyed friendly |

### Semantic — selection & focus

| Token | Hex | Role |
|-------|-----|------|
| `selected-ring` | `#FFC850` | Map symbol selection ring (1px border + 15% fill) |
| `selected-row` | `#4A9EFF` @ 25% bg + 3px left bar | OOB / list row selection |
| `score-accent` | `#F0C040` | Top bar score / priority numeric (bold, right-aligned) |

### Semantic — comms & degraded picture

| Token | Hex | Role |
|-------|-----|------|
| `comms-nominal` | `#64C88C` | Top bar COMMS line |
| `comms-degraded` | `#F0B450` | Top bar COMMS + stale affordances |
| `comms-denied` | `#E65A5A` | Top bar COMMS + frozen map |
| `ghost-track` | `#B47878` | Lag ghost symbol (italic label) |

### Semantic — message log categories

| Category | Hex | USS class |
|----------|-----|-----------|
| KILL / engagement | `#FFB4A0` | `message-log-row--kill` |
| MAGAZINE / logistics | `#B4C8FF` | `message-log-row--magazine` |
| COMMS | `#C8A0FF` (bold) | `message-log-row--comms` |
| CONTACT | `#8CC8FF` | `message-log-row--contact` |
| MISSION | `#A0DCB4` | `message-log-row--mission` |

### Semantic — Platform Import staging diff (target USS — Phase E+)

Diff rows use **prefix token color**; entity key remains `text-data`.

| Change kind | Prefix color | Hex | Example line |
|-------------|--------------|-----|--------------|
| `CellChanged` | `diff-changed` | `#E6C850` | `SENSORS row=0: BasePd: '0.85' -> '0.48'` |
| `RowAdded` | `diff-added` | `#64C88C` | `LINK row=2: DisplayName=…` |
| `RowRemoved` | `diff-removed` | `#E85D5D` | `COMMS row=3: removed` |
| Blocked / validation | `diff-blocked` | `#E65A5A` | Status line: `STAGING: blocked by validation errors` |
| Acknowledge gate | `diff-pending` | `#8C9BAA` | Approve button **45% opacity** until acknowledged |

### Colorblind safety

- Affiliation: **APP-6 frame shape** + text label always present on map symbols.
- Comms degradation: **opacity step** (100% → 55% → 35%) + ghost duplicate + italic suffix — not hue shift alone.
- Staging diff: **text prefix** (`LINK`, `COMMS`, `DAMAGE`) + change-kind color — never color-only rows.
- Selection: **left border bar** on lists, not fill alone.

---

## 4. Typography & Iconography

### Type roles

| Role | Family | Size @ 1080p | Weight | Usage |
|------|--------|--------------|--------|-------|
| **Data / log / diff** | Monospace (Unity: `Roboto Mono` or project `Assets/Fonts/DataMono.ttf`) | 10px | Regular | Message log rows, staging diff list, workbook path, numeric columns (LatencyMs, magazine counts, timestamps) |
| **Chrome headings** | Sans (Unity default UI / `Inter`-class) | 12px | Bold (`-unity-font-style: bold`) | Panel titles, section headers (`platform-catalog-comms`, `platform-import-diff-title`) |
| **Top bar / status** | Sans | 12px | Regular; bold for comms state | `c2-topbar-item`, Begin Execution label |
| **Map labels** | Sans | 14px | Regular; italic for ghost | `map-symbol` unit/contact ids |
| **Sensor strip** | Sans | 11–13px | Bold labels, regular values | EMCON / track summary |

### Hierarchy rules

1. **Monospace owns truth** — any value that appears in Excel round-trip, order log, or replay diff uses monospace.
2. **Sans owns structure** — zone titles, tabs, buttons, and mode labels use sans bold sparingly (one bold element per panel header).
3. **Minimum legibility** — 10px floor for dense lists; 12px for primary chrome. Scalable log font is a P1 accessibility upgrade; do not go below 10px for evidence captures.
4. **No display faces** — no sci-fi decorative fonts in C2 or Platform Editor.

### Iconography

| Domain | Standard | Notes |
|--------|----------|-------|
| **Map affiliation** | APP-6 frame atlas (`App6FrameAtlas.png`, 16×16 sprites, 7 frames) | Classes: `map-app6-frame--{friendly\|hostile\|neutral\|suspect\|pending\|unknown\|friendly-destroyed}`; Phase C expands to full MIL-STD-2525D set per ADR-007 |
| **Delegation** | Text badge `Human` / `Agent` / `Mixed` (P0); icon atlas deferred | Badge sits on map overlay; must remain readable at 16px |
| **Platform Editor actions** | Text buttons (`Export`, `Diff`, `Propose`, `Approve`) | No icon-only actions in catalog/import flows — evidence captures must read without tooltips |
| **Time compression** | Text presets `1x 4x 8x` | No skeuomorphic transport controls |
| **NATO symbology cues** | Frame shape = identity; fill = none in MVP | Hostile diamond, friendly rectangle per `c2-map-placeholder.md` |

---

## 5. Character Design Direction

**Formal N/A for v1 (B2 per release-enablement-scope-boundary-2026-06-20.md).** 

Project Aegis v1 Baltic slice is map-first theater command (C2 workstation posture); no on-screen avatars, unit portraits, or character models. 

- Unit identity: APP-6 frame shape + `unitId` label + right-panel readout (per §3 affiliation, §4 iconography, c2-command-post.md).
- No production character assets or VFX for personnel in scope.
- Post-v1 (B5+): consider NATO crew/ops room elements only if story-driven; budget separate.
- Cross-ref: ux/c2-command-post.md (no avatar flows), interaction-patterns.md (APP-6 primary).

**AD-ART-BIBLE note (S43):** N/A accepted; lean v1 gate passed. (art-director + team-ui)

---

## 6. UI Visual Language

### Layout hierarchy (1920×1080 baseline)

```
Priority 1 — Map / globe (center flex, primary mental model)
Priority 2 — Top bar (48px) — time, compression, comms, Begin Execution
Priority 3 — Message log (120px min height; floating variant max 220px) — evidentiary trail
Priority 4 — Left drawer (240px) — OOB / missions / contacts
Priority 5 — Right panel (320px) — unit detail on selection
Priority 6 — Platform Editor panels (editor scene overlay) — catalog viewer, import staging
```

Panels are **multi-host UIDocuments** (ADR-010); shared tokens below keep visual unity across hosts.

### Spacing tokens

| Token | Value | Application |
|-------|-------|-------------|
| `space-xs` | 2px | Row padding vertical (`oob-row`, list item tight) |
| `space-sm` | 4px | Panel inner padding (drawer), button margins, title `margin-bottom` |
| `space-md` | 6px | Standard panel padding (catalog, import, OOB); section `margin-top` |
| `space-lg` | 8px | Unit detail padding; map canvas margin; log panel padding |
| `space-xl` | 16px | Top bar item horizontal rhythm |
| `border-1` | 1px | Dividers, map frame, selection ring |
| `border-accent-3` | 3px | OOB selected left bar |

### List density

| Surface | Row height target | Font | Max visible rows |
|---------|-------------------|------|------------------|
| Message log | ~14px / row | 10px mono | 12 (scroll beyond) |
| OOB tree | ~20px / row | 12px sans | Fill drawer |
| Platform catalog list | ~18px / row | 12px sans; mono for ids | `min-height: 120px` scroll |
| Staging diff list | ~16px / row | 10px mono | `min-height: 100px` scroll |
| Link / comms sub-lists | ~18px / row | 12px sans | `min-height: 48px` each section |

### Panel surface recipe

All C2 and Platform Editor panels share:

```css
background-color: rgba(12, 18, 28, 0.92);  /* surface-panel */
padding: 6px;                                 /* space-md — drawer uses 4px */
/* Section split */
border-top-width: 1px;
border-top-color: rgba(80, 100, 130, 0.6);   /* border-section */
```

### Platform Catalog vs Import panels

| Aspect | **Platform Catalog** (`PlatformCatalogPanel`) | **Platform Import** (`PlatformImportPanel`) |
|--------|--------------------------------------------------|---------------------------------------------|
| **Purpose** | Read-only browse, export, diff trigger | Write-gate staging review (propose → acknowledge → approve) |
| **Min width** | 260px | 280px |
| **Primary action** | `Export` / `Diff` (secondary buttons, 64px min-width) | `Propose` / `Approve` (72px min-width); **Approve disabled @ 45% opacity** until acknowledge |
| **Content zones** | Search → platform list → detail → **COMMS** section → **LINK** section (Phase H) | Workbook path → actions → status line → **diff list** → acknowledge checkbox |
| **Tone** | Reference library — calm, sectional dividers | Audit queue — status line carries gate state (`STAGING: N change(s)…`) |
| **Evidence capture** | `platform-catalog-link-s34-viewer-columns.png` (S34); S36-06 re-captured live editor PNGs or lean notes for Phase H LINK section FK surfacing + current viewer state (12+ or notes) | `platform-import-staging-s34-link-diff.png` |

Catalog sections stack vertically with identical section divider treatment; Import panel treats the diff list as the hero flex child (`flex-grow: 1`).

### C2-specific modifiers

| Class | When | Effect |
|-------|------|--------|
| `c2-drawer-panel--planning-readonly` | RTwP planning | Muted drawer; no engage affordances |
| `map-placeholder-panel--planning-dimmed` | RTwP planning | Map opacity 0.42 + overlay |
| `c2-topbar-item--comms-{nominal\|degraded\|denied}` | Comms projection | Semantic comms color on bold label |
| `oob-row--selected` | Unit selected | Friendly blue wash + left bar |
| `map-symbol--{affiliation}` | Per-track | Affiliation color |
| `map-symbol--{stale\|frozen\|ghost}` | Comms degradation | Opacity / italic rules per §2 |

### Interaction feedback

- **Instant** — tab switches, selection rings, row highlights (reduced-motion default).
- **No** elastic easing, bounce, or particle confirmation on button press.
- **Disabled** controls: **45% opacity** only — no desaturate-to-invisibility.

---

## 7. VFX & Particle Style

**Formal N/A for v1 (B2 per release-enablement-scope-boundary-2026-06-20.md).**

C2 Polish / Baltic v1 scope excludes combat particles, screen shake, ambient map VFX, and any non-deterministic decorative effects.

- Permitted motion only: UI scroll + time-compression clock advance (deterministic; evidence-capture safe per §1 "every pixel serves the order log").
- No particles / idle shaders / animated gradients on data panels (violates determinism + replay 6/6 gate).
- Cross-ref: ux/interaction-patterns.md (reduced-motion default), design/gdd/sensor-detection-ew.md (no VFX in degraded comms), art-bible §2 mode carriers (opacity/text only).
- Future (post B1): separate environment art bible for globe Phase B.

**AD-ART-BIBLE note (S43):** N/A accepted; lean v1 gate passed. (art-director + team-ui; csharpexpert det cross-check)

---

## 8. Asset Standards

### Evidence captures (QA / presentation protocol)

| Requirement | Standard |
|-------------|----------|
| Resolution | **1920×1080** exactly — `Game` view capture |
| Naming | `{feature}-{context}-s{NN}-{descriptor}.png` e.g. `platform-import-staging-s34-link-diff.png` |
| Location | `production/qa/evidence/` |
| README | Each sprint batch documents scene, UXML bindings, and proxy test filter (see `README-presentation-evidence-s34.md`) |
| Headless CI | Protocol placeholders acceptable on agent host; live Editor re-capture is optional polish |
| Merge authority | Adapter tests (`PlatformImport\|PlatformCatalogViewer\|…`) ≥48/48 PASS per ADR-010 lean mode |

### Lean v1 Asset Spec Sheets (B2 §8 complete; C2 + Platform Editor only)

**APP-6 Frame Atlas (Map symbols)**
- File: `unity/ProjectAegis/Assets/UI/MapPlaceholder/App6FrameAtlas.png`
- Size: 112×16 px strip (7 frames × 16×16)
- Format: PNG alpha
- Spec: 1px padding; frames = friendly(□), hostile(◇), neutral, suspect, pending, unknown, friendly-destroyed (strikethrough)
- Addressables key: `Map/App6FrameAtlas`
- Usage: map-symbol--{affil} per §3; shape primary (colorblind safe)
- Evidence: cesium-s26-*.png batches; no change post S26

**C2 Top Bar Panel**
- UXML/USS: `C2TopBar/C2TopBarPanel.{uxml,uss}`
- Tokens: surface-topbar, text-topbar, comms-{nominal|degraded|denied}
- Layout: 48px fixed; flex row; clock / compression / comms / Begin Execution CTA
- States: per §2 (planning dim, executing, degraded amber, denied red, PAUSE)
- Spec: 12px sans; no motion except clock; 100% deterministic capture

**Message Log Panel**
- UXML/USS: `MessageLog/MessageLogPanel.{uxml,uss}`
- Font: 10px monospace (data); category tint per §3 (KILL #FFB4A0 etc)
- Rows: ~14px; max 12 visible + scroll; left border accent on select
- Spec: static; evidence-grade; no particles

**Platform Catalog / Import Panels**
- Catalog: `PlatformCatalog/PlatformCatalogPanel.{uxml,uss}` — read-only; list ~18px; mono ids; COMMS/LINK sections
- Import: `PlatformImport/PlatformImportPanel.{uxml,uss}` — staging diff hero; approve @45% until ack; prefix colors per §3 diff-*
- Common: surface-panel bg; border-section dividers; 260-280px min

**Left Drawer / OOB / Right Panel**
- Drawer: `C2LeftDrawer/C2LeftDrawerPanel.{uxml,uss}`; planning-readonly modifier per §6
- OOB rows: 20px; 12px sans
- Detail: 320px; dense 10px mono data

**AegisTokens.uss (recommended shared)**
- Path (proposed): `unity/ProjectAegis/Assets/UI/AegisTokens.uss`
- Content: map §3 palette + space-*/border-* tokens; import into all panel USS
- Status: doc rec from S35+; implement in polish if budget; copy hex fallback per §3 until then
- csharpexpert: ensure no runtime cost; static USS

**Texture/Atlas Budget (lean v1)**
- APP-6 only production atlas (112px strip)
- UI icons (future 16/24px flat PNG; deferred)
- All evidence 1920x1080 PNG (no scale)

All specs lean for v1; production assets limited to atlas + UI Toolkit panels. No VFX/character. Cross-ref ux/* + c2 gdd.

### USS naming conventions

| Pattern | Example | Rule |
|---------|---------|------|
| `{feature}-panel` | `.platform-catalog-panel` | Root container per UXML host |
| `{feature}-{zone}` | `.platform-catalog-comms-list` | Named UXML element mirror |
| `{feature}-{control}` | `.platform-import-approve-button` | Buttons include role |
| `{scope}--{modifier}` | `.map-symbol--hostile`, `.c2-topbar-item--comms-degraded` | BEM modifier; double hyphen |
| `{scope}--{state}` | `.platform-import-approve-button:disabled` | Pseudo-state for gates |

**USS authoring rules (Unity 6.3):**

- Use `-unity-font-style: bold` — never bare `unity-font-style`.
- Prefer `rgb()` / `rgba()` matching §3 tokens.
- Background images: `project://database/Assets/...` paths for atlases.
- One USS file per panel host, co-located with UXML under `Assets/UI/{Feature}/`.
- Shared tokens file `Assets/UI/AegisTokens.uss` (recommended Sprint 35+) — until then, copy canonical hex from §3.

### UI Toolkit conventions

| Topic | Standard |
|-------|----------|
| **Host pattern** | `{Feature}PanelHost.cs` wires ListView `makeItem`/`bindItem`; no gameplay state in MonoBehaviour |
| **Element names** | kebab-case matching UXML `name=` (`platform-import-diff-list`) |
| **ListView** | Label-based rows for evidence legibility; virtualize only after Baltic sign-off |
| **Layout** | Flexbox; absolute positioning reserved for map symbols and floating log |
| **Multi-document** | Retain multi-host layout per Sprint 4; consolidate only with measured perf win |
| **Scene** | `DelegationSmoke.unity` for Platform Editor + C2 sign-off; `useGlobeMap=false` in CI default |

### Texture & atlas

| Asset | Size | Format | Notes |
|-------|------|--------|-------|
| APP-6 frame atlas | 112×16 px strip, 16×16 cells | PNG | Addressables key `Map/App6FrameAtlas` |
| UI icons (future) | 16×16 / 24×24 | PNG alpha | Flat silhouette; 1px padding |
| Evidence PNG | 1920×1080 | PNG | No UI scale ≠ 100% in capture |

### File locations

```
unity/ProjectAegis/Assets/UI/
  C2LeftDrawer/     C2LeftDrawerPanel.{uxml,uss}
  TopBar/           C2TopBarPanel.{uxml,uss}
  MapPlaceholder/   MapPlaceholderPanel.{uxml,uss} + App6FrameAtlas.png
  MessageLog/       MessageLogPanel.{uxml,uss}
  PlatformCatalog/  PlatformCatalogPanel.{uxml,uss}
  PlatformImport/   PlatformImportPanel.{uxml,uss}
  …
```

---

## 9. Style Prohibitions

Hard rules for C2 and Platform Editor — violations fail visual QA and gate-check #2.

| # | Prohibition | Rationale |
|---|-------------|-----------|
| 1 | **No arcade chrome** — neon rims, XP bars, combo counters, skeuomorphic radar sweeps, trophy popups | Breaks theater-command fantasy and Project Aegis pillars |
| 2 | **No illegible density** — sub-10px text, color-only rows, overlapping map labels, >12 log rows without scroll | Violates information-density principle; fails evidence review |
| 3 | **No non-deterministic decorative VFX in C2** — particles, random flicker, idle shader noise, animated gradients on data panels | Breaks determinism; replay capture must match frame-for-frame |
| 4 | **No color-only affiliation or comms state** | Accessibility + NATO symbology contract |
| 5 | **No UI-owned gameplay state** — panels do not mutate `DecisionLog` or sim except via bridge command API | ADR-010 / headless-first |
| 6 | **No icon-only destructive actions** — Approve, Reject, Engage must have text labels in MVP | Evidence captures must stand alone |
| 7 | **No consumer-game palette** — saturated purples, candy greens, or high-saturation backgrounds behind data tables | Keeps near-future military C2 tone |
| 8 | **No decorative type** — script, stencil, or "hacker terminal" faces for non-data text | Undermines calm command authority |

### AD-ART-BIBLE Full Sign-Off (B2 Complete — S43-05)

**Verdict:** APPROVED (lean v1)

- §1–4: S42 complete (S42-05; handed off from smoke-sprint-42-closeout-2026-06-20.md)
- §5: Formal N/A v1 (character deferred)
- §6: Retained + cross-ref ux/ (c2-command-post.md layout, interaction-patterns.md density)
- §7: Formal N/A v1 (VFX deferred; det rules enforced)
- §8: Full lean asset spec sheets (atlas, panels, evidence, tokens, budgets)
- §9: Complete prohibitions + this verdict
- Consistency: Verified against ux/c2-command-post.md, interaction-patterns.md, c2-map-placeholder.md + gdd/command-and-control-ui.md. Tokens match USS + §3.
- AegisTokens.uss: Recommended (spec added); fallback hex until implemented.
- Budget: Held (S42 3d + S43 2.5d = 5.5d total lean; per boundary 8d cap)
- Cites: release-enablement-scope-boundary-2026-06-20.md + scope-expansion-decision-2026-06-20-S41-close.md ("i provide the ack") + S42 closeout + S43-05 plan + Art-Bible-Complete.md manifest. GitNexus (docs low impact).

**Art Director + team-ui sign-off:** Lean B2 complete. Ready for B1 lock + S43 closeout. Post-Baltic: non-lean expansion + globe env bible.

*Verification-before-completion: cross-read ux/* + art-bible history + boundary before edit.*

---

## Traceability

| Artifact | Section coverage |
|----------|------------------|
| Req 20 C2 UI | §1–4, §6, §9 |
| Req 21 Platform Editor | §4, §6 (Catalog vs Import), §8 |
| ADR-007 map presentation | §4 iconography, §6 map modifiers |
| ADR-011 Excel round-trip | §6 staging diff, §8 evidence |
| `c2-command-post.md` layout | §6 hierarchy, spacing |
| USS implementation | §3 hex, §6 tokens, §8 naming |

---

## Open items (post-Baltic)

- Consolidate `AegisTokens.uss` shared import across all panel USS files
- Implement `diff-{added\|changed\|removed\|blocked}` row classes in `PlatformImportPanel.uss`
- Ship `DataMono.ttf` and bind in panel UXML theme
- Phase B globe materials — separate environment art bible section
- Full APP-6 icon atlas beyond affiliation frames (ADR-007 Phase C)