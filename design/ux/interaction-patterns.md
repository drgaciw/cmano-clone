# Interaction Pattern Library — C2 & Platform Editor

> **Status:** Committed — Sprint 35 (S35-03)  
> **Version:** 1.0  
> **Last Updated:** 2026-08-03  
> **Scope:** Implemented screens in Polish Phase 1 — no new gameplay systems  
> **Related:** [c2-command-post.md](c2-command-post.md), [c2-map-placeholder.md](c2-map-placeholder.md), [art-bible.md](../art/art-bible.md), [accessibility-requirements.md](../accessibility-requirements.md)  
> **Evidence:** `production/qa/c2-manual-signoff-2026-06-02.md` checks 1–18; `PlatformImportPanelTests` 10/10; human playtests `production/playtests/human/` @ 2026-06-19

---

## 1. Purpose

This library documents **interaction patterns already implemented** in code so no screen remains "designed in-code only." Patterns are normative for Polish UX work; deviations require story-level approval.

**Out of scope:** Delegation badges on map ([polish-scope-boundary-2026-06-19.md](../../production/polish-scope-boundary-2026-06-19.md)); globe pan/zoom (ADR-007 Phase B).

---

## 2. Pattern Index

| ID | Pattern | Primary surface | Sign-off anchor |
|----|---------|-----------------|-----------------|
| P-C2-01 | Drawer tab switch | C2 left drawer | Check 5–6 |
| P-C2-02 | Selection sync (map ↔ OOB ↔ detail) | C2 map + drawer + right panel | Checks 2–4 |
| P-C2-03 | OOB row click | C2 left drawer | Check 4 |
| P-C2-04 | Map symbol click | C2 map placeholder | Check 3 |
| P-C2-05 | COMMS degrade affordance | C2 map + top bar | Checks 9–11 |
| P-PE-01 | Import staging workflow | Platform Editor | Check 14 |
| P-PE-02 | Staging diff feedback | Platform Editor | Checks 14, 17, 18 |
| P-PE-03 | Validation error surfacing | Platform Editor | Check 14 |

---

## 3. C2 Left Drawer Tabs (P-C2-01)

**Screen:** [c2-command-post.md §4.1 Wireframe](c2-command-post.md#41-wireframe) — left drawer 240px  
**Host:** `C2LeftDrawerPanelHost`  
**Visual:** [art-bible.md §6 Layout hierarchy](art/art-bible.md#6-ui-visual-language) — Priority 4

### Behavior

| Input | Result | Feedback |
|-------|--------|----------|
| Click tab `OOB` / `MISSIONS` / `CONTACTS` | Activates tab; swaps ListView content | Instant tab highlight; no slide animation ([accessibility-requirements.md §4](../accessibility-requirements.md#4-reduced-motion)) |
| Keyboard focus on tab toolbar | Arrow keys move between tabs; Enter activates | Unity 6 default focus ring |
| Planning phase (pre–Begin Execution) | Drawer read-only styling | `c2-drawer-panel--planning-readonly` — border `#465260`, tabs 65% opacity ([art-bible.md §2 Planning](art/art-bible.md#planning-rtwp--before-begin-execution)) |

### Data binding

| Tab | Bridge | Update rate |
|-----|--------|-------------|
| OOB | `OobTreeBridge` | Per tick |
| Missions | `MissionListBridge` | Static list + log highlights |
| Contacts | `SensorC2Bridge` | Per tick |

### Acceptance

- Three tabs show live data in Play Mode with `baltic-patrol` / `baltic-patrol-classify` ([c2-command-post.md §10](c2-command-post.md#10-acceptance-criteria-sprint-45)).
- Tab switch does not clear unit selection unless selected entity absent from new tab.

---

## 4. Selection Sync — Map, OOB, Right Panel (P-C2-02)

**Screens:** [c2-command-post.md §5 States](c2-command-post.md#5-states--variants), [c2-map-placeholder.md §Interaction](c2-map-placeholder.md#interaction-mvp)  
**Projection:** `C2SelectionFlow` / `UnitDetailProjection`

### Single source of truth

```
Player click (map or OOB or log P1)
        ↓
Selection projection (unitId / contactId)
        ↓
┌───────┴───────┬───────────────┐
│ Map ring      │ OOB row bar   │ Right panel detail
│ (selected-ring)│ (selected-row)│ (UnitDetailProjection)
└───────────────┴───────────────┘
```

### Visual tokens

| Target | Token | Spec |
|--------|-------|------|
| Map symbol | `selected-ring` `#FFC850` | 1px border + 15% fill ([art-bible.md §3](art/art-bible.md#semantic--selection--focus)) |
| OOB row | `selected-row` | 3px left bar `#4A9EFF` + 25% background |
| Right panel | Populated lines | Unit id, alive/destroyed, magazine, EMCON, sensors summary |
| No selection | Placeholder copy | "Select a unit on map or OOB" ([c2-command-post.md §5](c2-command-post.md#5-states--variants)) |

### Edge cases

| Case | Behavior |
|------|----------|
| Destroyed friendly | Dim + strikethrough; detail shows destroyed state |
| Ghost symbol (comms lag) | **Not selectable** — `pickingMode: Ignore` ([c2-map-placeholder.md §Comms degradation](c2-map-placeholder.md#comms-degradation-p1--implemented)) |
| Stacked symbols | OOB row click is authoritative path when map pick ambiguous |

### Playtest signal

Human NPE session: OOB row click sync rated intuitive for grognard persona; live click-feel unobserved on lean host ([playtest-2026-06-19-npe-baltic-c2-thinkaloud.md](../../production/playtests/human/playtest-2026-06-19-npe-baltic-c2-thinkaloud.md)).

---

## 5. OOB Row Click (P-C2-03)

**Pattern:** Sub-pattern of P-C2-02 with drawer-specific affordances.

| Step | System response |
|------|-----------------|
| 1. Click OOB row | Row receives `selected-row` styling |
| 2. Selection projection updates | `unitId` published |
| 3. Map | Friendly `■` symbol receives selection ring (when visible) |
| 4. Right panel | `UnitDetailProjection` rebinds |
| 5. Contacts tab | If contact linked to unit, summary line may highlight (P1) |

**Keyboard:** Up/Down navigates rows; Enter triggers same chain as click.

**Tests:** `OobTreeBridgeTests`, `C2SelectionFlowTests`, PlayMode `Baltic_classify_selection_flow_syncs_map_oob_and_contact_summary`.

---

## 6. Map Placeholder Symbol Click (P-C2-04)

**Screen:** [c2-map-placeholder.md](c2-map-placeholder.md)  
**Host:** `MapPanelHost` / `MapPictureProjection`

| Input | Action |
|-------|--------|
| Click friendly `■` | Select unit → right panel + OOB highlight |
| Click hostile `◆` | Select contact → contact summary + CONTACTS tab context |
| Click ghost `◌` | No selection — stale hint only |
| Hover | Tooltip P2: id, lifecycle, last log category |

**Comms styling** (P-C2-05 overlap):

| COMMS state | Map behavior |
|-------------|--------------|
| Nominal | Full brightness all symbols |
| Degraded | Hostile `◆` at 55% opacity + ghost duplicate at lag offset |
| Denied | All symbols 35% opacity; no ghosts |

**Top bar:** `COMMS: NOMINAL | DEGRADED | DENIED` with semantic colors ([art-bible.md §3 comms tokens](../art/art-bible.md#semantic--comms--degraded-picture)).

**Reduced motion:** Selection ring appears instantly; no pan inertia on placeholder map.

---

## 7. Platform Import Staging (P-PE-01)

**Screen:** Platform Editor — `PlatformImportPanelHost`  
**Workflow:** **Propose → Acknowledge → Approve** (ADR-011)  
**Visual:** [art-bible.md §6 Platform Catalog vs Import](art/art-bible.md#platform-catalog-vs-import-panels)

### State machine

```
[Idle]
   │ user selects workbook + Propose
   ▼
[Staged] — diff list populated; status line shows row counts
   │ user reviews each change; toggles Acknowledge
   ▼
[Acknowledged] — all required rows acknowledged
   │ Approve enabled (opacity 100%)
   ▼
[Approved] — write routed to scenario DB; readback verified
```

### Control gating

| Control | Enabled when | Visual |
|---------|--------------|--------|
| `Propose` | Valid workbook path | Text button 72px min-width |
| `Approve` | All staged rows acknowledged **and** validation pass | Disabled @ **45% opacity** until gate cleared ([art-bible.md `diff-pending`](../art/art-bible.md#semantic--platform-import-staging-diff-target-uss--phase-e)) |
| Acknowledge checkbox | Per-row or global per implementation | Space toggles |

### Round-trip evidence

Baltic fixture: propose → acknowledge → approve → readback (`PlatformImportPanelTests` 10/10). Mid-game playtest: curator persona traced workflow without facilitator correction ([playtest-2026-06-19-midgame-delegation-catalog-thinkaloud.md](../../production/playtests/human/playtest-2026-06-19-midgame-delegation-catalog-thinkaloud.md)).

---

## 8. Staging Diff Feedback (P-PE-02)

**Purpose:** Player sees **exactly** what will change before Approve commits.

### Row format (monospace)

```
{SECTION} row={index}: {field}: '{old}' -> '{new}'
{SECTION} row={index}: removed
{SECTION} row={index}: added — {summary fields}
```

| SECTION prefix | Example | Color token |
|----------------|---------|-------------|
| `DAMAGE` | `DAMAGE row=0: MaxHp: '100' -> '85'` | `diff-changed` `#E6C850` |
| `COMMS` | `COMMS row=3: LinkId: 'LINK_NATO_50MS' -> 'LINK_SAT'` | `diff-changed` |
| `LINK` | `LINK row=2: DisplayName=…` (added) | `diff-added` `#64C88C` |
| (removed) | `COMMS row=3: removed` | `diff-removed` `#E85D5D` |

### Status line

| State | Status text example |
|-------|---------------------|
| Staged | `STAGING: 3 changes pending acknowledgement` |
| Blocked | `STAGING: blocked by validation errors` — `diff-blocked` `#E65A5A` |
| Approved | `STAGING: applied — readback OK` |

### Scanability rules

1. **Entity key first** — `COMMS row=` / `LINK row=` / `DAMAGE row=` always visible.
2. **Monospace owns truth** — numeric and string deltas in data font ([art-bible.md §4](../art/art-bible.md#4-typography--iconography)).
3. **No icon-only actions** — Export, Diff, Propose, Approve are text-labeled buttons.
4. Diff list `min-height: 100px` with scroll ([art-bible.md §6 List density](../art/art-bible.md#list-density)).

### Sim impact (player mental model)

| Diff type | Expected sim effect (after Approve) |
|-----------|-------------------------------------|
| `LINK` latency change | `DatalinkShareLagResolver` may change `shareLagTicks` (S34-07) |
| `COMMS` fitting change | Platform comms resolution + datalink display name |
| `DAMAGE` MaxHp change | Combat domain HP ledger inputs |

Playtest note: operator connected `LatencyMsNominal` → lag only via facilitator — **in-game attribution overlay deferred** ([fun-hypothesis-validation-2026-06-19.md](../../production/playtests/fun-hypothesis-validation-2026-06-19.md)).

---

## 9. Validation Error Surfacing (P-PE-03)

**Purpose:** Block Approve when workbook delta fails validation; surface errors in status line + diff list — never silent fail.

### Error channels

| Channel | Content |
|---------|---------|
| Status line | `STAGING: blocked by validation errors` (bold `diff-blocked` color) |
| Diff list | Blocked rows prefixed; validation message appended |
| Approve button | Remains `disabled` @ 45% opacity |

### Validation sources (implemented)

| Rule pack | Example failure |
|-----------|-----------------|
| `LINK_*` | Invalid LinkId FK, missing DisplayName |
| `KILL_CHAIN_*` | Broken kill-chain reference |
| Schema version | Workbook `SchemaVersion` mismatch |
| COMMS FK | LinkId not found in LinkCatalog |

### Player recovery loop

```
Propose → see blocked status
    → read error row in diff list
    → fix workbook externally OR cancel staging
    → re-Propose
    → acknowledge clean diff
    → Approve
```

**No auto-approve** in player-facing flow (headless CI may auto-accept per agentic-mission-editor GDD — not this panel).

---

## 9b. Unified Platform Editor Shell (P-PE-04)

**Screen:** Platform Catalog + Platform Import (same curator session)  
**Purpose:** Reduce cognitive load across damage / comms / link (and other) workbook domains without in-engine WYSIWYG editing (ADR-011).  
**Evidence driver:** [playtest-2026-06-19-midgame-delegation-catalog-thinkaloud.md](../../production/playtests/human/playtest-2026-06-19-midgame-delegation-catalog-thinkaloud.md) — “Unified Platform Editor UX spec”.

### Shell layout

| Pane | Role |
|------|------|
| **Catalog** (`PlatformCatalogViewerHost`) | Read-only browse; section bar Identity / Damage / Fits / Comms / Links / Graph; Export/Diff with in-panel status |
| **Import** (`PlatformImportPanelHost`) | Propose → section-filtered diff review → Acknowledge → Approve |

### Section filter (Import)

| Chip | Shows |
|------|-------|
| ALL | Every staging row |
| DAMAGE | MaxHp / withdraw / flags deltas |
| COMMS | Comms fitting deltas |
| LINK | LinkCatalog deltas |
| OTHER | Remaining sheets (Sensors, Mounts, …) |

Global Acknowledge still gates Approve (P-PE-01). Filters only change which rows are visible for review.

### Diff row styling

Apply USS classes from projection `DiffKind` (P-PE-02 colors): `platform-import-diff-row--added|changed|removed|blocked|info`. Status uses `platform-import-status--blocked` when validation blocks.

### Tokens

Panels consume `--aegis-*` aliases from [AegisTokens.uss](../../unity/ProjectAegis/Assets/UI/AegisTokens.uss) (PE-UX-W0). Diff hex values match art bible §3.

---

## 10. Related Patterns (reference only)

These screens have UX specs but are **not expanded** in S35-03 — see linked docs:

| Flow | Doc |
|------|-----|
| Begin Execution (Planning → Executing) | [c2-command-post.md §5–6](c2-command-post.md#5-states--variants); `C2TopBarBeginExecutionTests` |
| Doctrine ROE override | Check 15; `DoctrineOverrideCommandTests` |
| Message log row select | [c2-command-post.md §6](c2-command-post.md#6-interaction-map-mvp) P1 |
| Catalog viewer (read-only) | [art-bible.md §6 Catalog panel](../art/art-bible.md#platform-catalog-vs-import-panels) |

> S39-03 residual polish note (C2 + Platform Editor): tooltip/density improvements (PlatformCatalogViewerHost) + surfacing assertion; cross-ref [c2-command-post.md](c2-command-post.md). All per polish-scope-boundary-2026-06-19.md + sprint-39-deeper-polish-c2-platform-hygiene.md (extend-only; proxy maintained; evidence path ready). See also S38 carry.

---

## 11. Acceptance Criteria (S35-03)

1. Every implemented C2 interaction in checks 1–13 has a pattern ID in this library.
2. Platform import staging (check 14) documents propose → acknowledge → approve + diff + validation.
3. Cross-links to c2-command-post, c2-map-placeholder, and art-bible present.
4. No new gameplay systems introduced — patterns describe existing bridges/projections only.

---

## Document History

| Date | Change |
|------|--------|
| 2026-06-19 | Initial library — S35-03 gate r2 "interaction pattern library current" gap closed |
| 2026-07-23 | P-PE-04 unified Platform Editor shell (PE-UX productization) |