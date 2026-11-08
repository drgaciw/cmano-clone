# UX Specification: Scenario Editor (Map + Mission Board)

> **Status**: Approved (visual mock) · Spec Draft for Unity host  
> **Author**: team-ui orchestrator (ux/ui expert decisions 2026-07-23)  
> **Last Updated**: 2026-07-23  
> **Screen / Flow Name**: `ScenarioEditorShell`  
> **Platform Target**: PC (Unity Edit Mode / UI Toolkit)  
> **Related GDDs**: `design/gdd/agentic-mission-editor.md`  
> **Related ADRs**: ADR-006, ADR-008, ADR-010; Phase 2 design `docs/superpowers/specs/2026-07-08-scenario-editor-phase2-design.md`  
> **Related UX Specs**: `c2-command-post.md`, `c2-map-placeholder.md`, Platform Editor shell (P-PE-04)  
> **Accessibility Tier**: Standard  
> **HTML review mock**: `docs/superpowers/reviews/scenario-editor-uiux-preview.html`

---

## 1. Purpose & Player Need

**What player need does this screen serve?**

Lets the theater designer author a scenario without losing the map as the primary mental model, while still managing missions as a filterable board when density demands it — and never silently shipping a broken scenario because live findings surface the same Validation Engine codes as CLI.

**The player goal**: Place/move ORBAT, draw zones, attach missions, and clear error findings before Play/Sample — with Map and Mission Board sharing one selection and one canonical file.

**The game goal**: Commit all mutations into `scenario.json` via `ScenarioDocumentEditor` / `ScenarioEditCommandBus`; keep `editorState` derived-only; gate Play/Sample on error-severity findings.

---

## 2. Player Context on Arrival

| Question | Answer |
|----------|--------|
| What was the player just doing? | Opening Edit Mode on a loaded scenario (e.g. Baltic Patrol v3) or creating a blank theater |
| Emotional state | Focused expert — wants speed and density, not onboarding wizards |
| Cognitive load | High — theater geography + force + missions + validity simultaneously |
| Information they already have | Scenario title, dbRef, rough force layout from prior session or fixture |
| Most likely trying to do | Fix a finding, place a unit, or attach a patrol/strike |
| Afraid of | Silent invalid geometry, unreachable strikes, losing work, Play with broken file |

**Emotional design target**: Authoritative workstation calm — expert density without modal thrash; findings are sharp but fixable in place.

---

## 3. Navigation Position

```
Unity Project Aegis (Edit Mode)
  └── Scenario Editor Shell
        ├── Map Authoring (P2.1)
        ├── Mission Board (P2.2)
        └── Events (P2.3 — disabled stub)
```

**Modal behavior**: Non-modal editor chrome (Edit Mode host). Findings are a dock, not a blocking dialog. Save allowed with errors; Play/Sample blocked.

**Reachability**

| Entry Point | Triggered By | Notes |
|-------------|--------------|-------|
| Edit Mode open | Designer loads scenario package | Primary |
| Finding jump | Click Live Findings row | Deep-link selection |
| Mission Board → Map | Show on Map / double-click row | Preserves selection |

---

## 4. Entry & Exit Points

| Trigger | Source | Transition | Data In | Notes |
|---------|--------|------------|---------|-------|
| Open Scenario Editor | Editor host | Instant (reduced-motion default) | Scenario path, editVersion | Load via session |
| Switch Map ↔ Missions | Shell tabs | Instant content swap | Selected entity id | Slots stay alive |
| Close / leave Edit | Host | Instant | Dirty confirm if unsaved | Host policy |

| Exit Action | Destination | Data Saved | Notes |
|-------------|-------------|------------|-------|
| Save | Same shell | Canonical scenario.json | Errors do not block save |
| Play / Sample | Play Mode / sample harness | Requires 0 error findings | AC-12 |
| Undo / Redo | Same shell | editVersion adjust | Bus undo stack |

---

## 5. Layout Specification

### 5.1 Wireframe (1920×1080)

```
┌─ Review / mode banner (HTML only) ─────────────────────────────────────────┐
├─ Top bar 48px: title · EDIT · dirty · editVersion · [Load][Save][Undo]… ──┤
│  Shell tabs: [Map] [Missions] [Events✗] · Play blocked · N errors         │
├──────────┬────────────────────────────────────────────┬───────────────────┤
│ Drawer   │  CENTER: Map SVG  -or-  Mission Board      │ Inspector 320px   │
│ 240px    │  tools / filters                            │ Selection fields  │
│ ORBAT /  │  {map glyphs + zones} / {mission table}     │ Attach mission    │
│ Zones    │                                            │ Doctrine chain    │
├──────────┴────────────────────────────────────────────┴───────────────────┤
│ Live Findings ~160px  [All|Errors|Warnings]  codes · jump-to-entity       │
└───────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Zone Definitions

| Zone | Size | Scroll | Notes |
|------|------|--------|-------|
| Top bar | 48px full width | No | Temporal + file authority |
| Shell tabs | under top bar | No | Map / Missions / Events(disabled) |
| Left drawer | 240px | Yes | Map mode: ORBAT/Zones; Mission mode may collapse to filters-only |
| Center | flex | Map no / Board yes | Primary mental model |
| Inspector | 320px | Yes | SelectionInspector |
| Findings | ~140–180px | Yes | LiveFindingsPresenter |

### 5.3 Component Inventory

| Component | Zone | Reuses |
|-----------|------|--------|
| ScenarioEditorShellHost | Shell | Pattern from PlatformEditorShellHost |
| MapAuthoringSurface (slot) | Center Map | Headless presenter exists |
| MissionBoardPresenter (slot) | Center Missions | Headless presenter exists |
| SelectionInspector | Right | SelectionInspectorModel |
| LiveFindings dock | Bottom | LiveFindingsPresenter |
| Drawer tabs ORBAT/Zones | Left | P-C2-01 |
| Play gate label | Shell | New — error count only |

**Primary focus on open**: First ORBAT row / last selected entity if session restores selection.

---

## 6. States & Variants

| State | Visual | Behavior |
|-------|--------|----------|
| Map mode | Map slot visible | Place/Move/Draw tools |
| Mission Board mode | Table visible | Filters + Add/Clone/Template |
| Dirty | Top bar `• unsaved` | Save enabled |
| Clean | `saved` muted | — |
| Findings errors > 0 | Play/Sample disabled; gate chip | Save still allowed |
| Invalid geometry | Red dashed zone + INVALID | Stays on map; editable |
| Events tab | Disabled 45% opacity | Tooltip P2.3 |
| Empty selection | Inspector placeholder copy | — |
| CONFLICT editVersion | Host error (not last-write-wins) | Re-fetch / retry |

---

## 7. Interaction Map

### 7.1 Navigation

| Input | Action |
|-------|--------|
| Click shell Map/Missions | Swap center; keep selection |
| Ctrl+Tab / ←→ on shell tabs | Cycle Map ↔ Missions |
| Click ORBAT / map glyph / board row | Selection sync (P-C2-02 extended) |
| Click finding | Select entity; mission findings prefer Missions mode |
| Esc | Clear tool arming / close inline Add strip (not quit Editor) |

### 7.2 Actions

| Action | Event / bus | Notes |
|--------|-------------|-------|
| Place/move unit | Bus ORBAT upsert/move | Gesture-end commit |
| Draw RP | Bus reference_point upsert | Invalid stays visible |
| Attach mission | Bus mission assign | Inspector dropdown |
| Board Clone / Template | MissionBoardPresenter | Refresh findings |
| Add Mission type strip | Bus add archetype | No wizard wall |
| Save | Session.Save | Allowed with errors |
| Play/Sample | EditModeController gate | Blocked on errors |

---

## 8. Data Requirements

| Data | Source | Update | Owner |
|------|--------|--------|-------|
| ORBAT / RP / missions | ScenarioDocumentEditor.ToDto() | After each bus commit | Data |
| Board rows | MissionBoardQuery / Presenter | Refresh after mutate | Data / UA Authoring |
| Findings | ScenarioValidationEngine via LiveFindingsPresenter | Debounce 200–400ms | Data |
| Selection | Shell + SelectionInspectorModel | Immediate | Projection (derived) |
| editorState camera/layers | Session.EditorState | Local | Derived-only — never validate |

UI never writes document fields directly.

---

## 9. Events Fired (host → bus)

| Player Action | Path | Notes |
|---------------|------|-------|
| Commit ORBAT/RP/mission | ScenarioEditCommandBus | editVersion optimistic lock |
| Clone / template | MissionBoardPresenter → Bus | |
| Save | ScenarioAuthoringSession.Save | |
| Request Play | EditModeController | Consults findings |

---

## 10. Transition & Animation

| Transition | Duration | Reduced motion |
|------------|----------|----------------|
| Shell mode swap | Instant | Instant |
| Selection highlight | Instant | Instant |
| Finding jump pulse | ≤200ms optional | Instant class only |
| No elastic/bounce | — | — |

---

## 11. Input Method Completeness

**Keyboard**
- [x] Shell tabs cycle (Ctrl+Tab / arrows)
- [x] Lists/tables activatable Enter/Space
- [x] Focus-visible rings
- [ ] Full gamepad (deferred — Standard tier PC editor; mouse+KB primary)

**Mouse**
- [x] Hover/select on rows, glyphs, findings
- [x] Double-click board → Map

**Touch**: N/A for Edit Mode v1.

---

## 12. Screen-Level Accessibility

| Element | Mitigation |
|---------|------------|
| Affiliation | Shape ■ ◆ ▲ + color |
| Findings severity | ERROR/WARN text + color |
| Play blocked | Text chip + disabled controls @ 45% |
| Contrast | art-bible tokens / Standard tier |
| Scale | C2AccessibilitySettings 100/125/150 |

**Focus order**: Shell tabs → drawer tabs → list/table → map tools (Map) → inspector fields → findings.

---

## 13. Localization

All player-facing chrome strings via loc keys in Unity host (HTML mock may hardcode for review). Tolerate 40% expansion on tab labels (`Missions`, `Findings`). Codes (`STRIKE_UNREACHABLE`) stay machine-stable English.

---

## 14. Acceptance Criteria

- [ ] Designer can switch Map ↔ Mission Board without losing selection
- [ ] Board filters match MissionBoardPresenter Type/Side/Status semantics
- [ ] Finding click selects entity; invalid zone remains visible
- [ ] Play/Sample disabled iff errorFindingCount > 0; Save always available when session open
- [ ] Events tab disabled with P2.3 affordance
- [ ] Projection tests: `ScenarioEditorShellProjectionTests` green
- [ ] No DelegationBridge hotpath edits; editorState not fed to validation
- [ ] HTML mock reviewed at `docs/superpowers/reviews/scenario-editor-uiux-preview.html`

---

## 15. Open Questions

| Question | Resolution |
|----------|------------|
| Map vs Board left-drawer collapse in Mission mode | **Decided**: Mission mode keeps a slim filter/context rail; full ORBAT drawer returns on Map |
| Add Mission UI | **Decided**: inline type strip, not modal wizard |
| Event graph | **Deferred P2.3** — disabled tab only |

---

## New interaction patterns (to add to library)

| ID | Pattern | Notes |
|----|---------|-------|
| P-SE-01 | Scenario shell Map \| Missions mode swap | Mirrors P-PE-04 |
| P-SE-02 | Live findings jump-to-entity | Extends P-PE-03 |
| P-SE-03 | Mission Board filter + inline add type strip | P2.2 |
| P-SE-04 | Play/Sample gate on error findings | AC-12 surface |
