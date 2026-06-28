# Visual Entity & Screen Inventory

> **Generated:** 2026-06-25  
> **Phase:** 0b (dashboard plan Phase B — asset-spec skill)  
> **Review status:** **User review pending** — defaults applied from docs; no blocking AskUserQuestion in cloud execution.  
> **Sources:** `design/art/art-bible.md`, `design/gdd/systems-index.md`, GDDs under `design/gdd/`, `design/ux/c2-command-post.md`, `design/ux/c2-map-placeholder.md`, `design/ux/interaction-patterns.md`, `design/ux/onboarding-baltic.md`, `production/release/store/asset-checklist.md` (S70)

---

## Scope note (lean v1)

Per art bible B2 scope: **C2 Command Post + Platform Editor UI only** for in-game production assets. World/character/VFX are **deferred post-Baltic slice**. Store capsule/screenshot/trailer items are **E7 prep placeholders** (S70) — listed for manifest completeness, not in-engine production.

---

## Entities

| # | Name | Type | Description | Source | Status |
|---|------|------|-------------|--------|--------|
| 1 | APP-6 Frame Atlas | Item / UI atlas | 7 affiliation frames (16×16) on 112×16 PNG strip; shape-primary symbology | art-bible.md §4, §8 | Needed |
| 2 | Map symbol overlays | UI / Map | Per-track affiliation + stale/ghost/frozen modifiers; selection ring | art-bible.md §2, §6; c2-map-placeholder.md | Needed |
| 3 | C2 Top Bar chrome | UI panel | 48px bar: clock, compression, comms state, Begin Execution CTA | art-bible.md §8; c2-command-post.md | Needed |
| 4 | Message Log panel | UI panel | 10px mono rows; category tints (KILL, MAGAZINE, COMMS, CONTACT, MISSION) | art-bible.md §3, §8; command-and-control-ui.md | Needed |
| 5 | Left drawer (OOB / missions / contacts) | UI panel | 240px tabbed drawer; planning-readonly modifier | c2-command-post.md §4; art-bible.md §6 | Needed |
| 6 | Right unit detail panel | UI panel | 320px selection readout; dense mono data | c2-command-post.md §4; art-bible.md §8 | Needed |
| 7 | Map placeholder canvas | Environment / UI | Theater board `#101824` on `#080E16`; planning dim overlay | c2-map-placeholder.md; art-bible.md §2 | Needed |
| 8 | Platform Catalog panel | UI panel | Read-only browse, COMMS/LINK sections, export/diff actions | art-bible.md §6, §8; interaction-patterns.md P-PE-* | Needed |
| 9 | Platform Import staging panel | UI panel | Diff list hero; propose/acknowledge/approve gate @ 45% disabled | art-bible.md §6, §8; ADR-011 | Needed |
| 10 | Delegation badge overlay | UI / Map | Text badges Human / Agent / Mixed on map (icon atlas deferred) | art-bible.md §4; agentic-infrastructure.md | Needed |
| 11 | Policy denial affordance | UI / HUD | Greyed weapon + tooltip; top-3 abort reasons | policy-roe-emcon-wra.md §Visual/Audio | Needed |
| 12 | EMCON emitter indicator | UI / Map | Emitter icon on unit (P0 policy visual) | policy-roe-emcon-wra.md §Visual/Audio | Needed |
| 13 | Sensor strip / contact filters | HUD | EMCON/track summary; stale/classification filters | sensor-detection-ew.md; c2-command-post.md | Needed |
| 14 | AegisTokens.uss shared theme | UI tokens | Canonical palette + spacing tokens imported across panel USS | art-bible.md §8 (proposed) | Needed |
| 15 | Baltic patrol theater backdrop | Environment | Naval Baltic patrol scenario framing for map + store captures (no 3D globe v1) | asset-checklist.md; baltic-v2 playtests | Needed |
| 16 | Comms degradation visual set | VFX (static) | Opacity steps 100%→55%→35%; ghost duplicate; italic lag suffix — no particles | art-bible.md §2, §7 N/A; cyber-comms-degradation.md | Needed |
| 17 | Replay scrubber / golden hash overlay | UI | Read-only replay mode; hash evidence display for QA captures | order-log-replay.md; asset-checklist screenshot #3 | Needed |
| 18 | Combat domains smoke overlay | UI / HUD | Domain visualization for hot-tick scenarios (band C stress) | combat-domains-damage.md; asset-checklist #4 | Needed |
| 19 | Main capsule art (Steam) | Marketing | 616×353 recommended; Baltic patrol silhouette + C2 overlay | asset-checklist.md S70 | Needed |
| 20 | Small capsule art (Steam) | Marketing | 231×87 crop or dedicated asset | asset-checklist.md S70 | Needed |
| 21 | Store logo / header variants | Marketing | Community + library header use | asset-checklist.md S70 | Needed |
| 22 | Press kit logo pack | Marketing | Logo + fact sheet linking release-checklist-v3 | asset-checklist.md S70 | Needed |

---

## UI Screens

| # | Screen Name | Description | Source | Status |
|---|-------------|-------------|--------|--------|
| 1 | Main Menu | Entry to scenario select; out of lean B2 art scope but listed for flow completeness | c2-command-post.md §3 navigation | Needed (deferred art) |
| 2 | Scenario Select | Baltic scenario picker; ties to onboarding-baltic | onboarding-baltic.md | Needed (deferred art) |
| 3 | Mission Planning (RTwP) | Pre-execution planning; map dimmed 42%; drawer read-only | c2-command-post.md §3–5; art-bible.md §2 Planning | Needed |
| 4 | C2 Command Post | Primary play screen — map-first 1920×1080 layout | c2-command-post.md; command-and-control-ui.md | Needed |
| 5 | Platform Editor | Catalog viewer + import staging hosts (`DelegationSmoke.unity`) | art-bible.md §6; interaction-patterns.md | Needed |
| 6 | AAR / Replay | Read-only UI; log + scrubber; no new orders | c2-command-post.md §3; order-log-replay.md | Needed |
| 7 | Onboarding Baltic | First-run Baltic patrol guidance overlay | onboarding-baltic.md | Needed (P1) |

---

## HUD Elements

| # | Element | Description | Source | Status |
|---|---------|-------------|--------|--------|
| 1 | Top bar clock + compression | `1x 4x 8x` text presets; sim time display | art-bible.md §4, §6 | Needed |
| 2 | Comms state line | nominal / degraded / denied chroma on bold label | art-bible.md §3 semantic comms | Needed |
| 3 | OOB row selection | Left bar + friendly blue wash | art-bible.md §3 `selected-row` | Needed |
| 4 | Map selection ring | `#FFC850` ring + 15% fill | art-bible.md §3 | Needed |
| 5 | Message log category rows | Tinted mono rows per event category | art-bible.md §3 | Needed |
| 6 | Staging diff prefix tokens | CellChanged / RowAdded / RowRemoved / blocked colors | art-bible.md §3 Platform Import | Needed |
| 7 | Score / priority numeric | Top bar right-aligned `#F0C040` | art-bible.md §3 | Needed |
| 8 | Begin Execution CTA | Warm accent `#285A3C` / `#50A064` — single planning accent | art-bible.md §2 Planning | Needed |

---

## Audio

| # | Name | Type | Description | Source | Status |
|---|------|------|-------------|--------|--------|
| 1 | Policy denial tone | SFX | Short deny tone on weapon greyed / ROE block | policy-roe-emcon-wra.md §Visual/Audio | Needed (deferred) |
| 2 | ROE change cue | SFX | Optional side panel flash companion (visual P1) | policy-roe-emcon-wra.md | Needed (deferred) |
| 3 | Ambient C2 workstation hum | Ambient | Low subtle loop — only if does not break determinism evidence captures | — | Not needed (v1) |

---

## Store capture placeholders (S70 — not in-engine)

| # | Name | Type | Description | Source | Status |
|---|------|------|-------------|--------|--------|
| 1 | Screenshot 01 — C2 map patrol | Capture | Main C2 map, band B datalink contacts | asset-checklist.md | Needed |
| 2 | Screenshot 02 — Delegation / policy panel | Capture | ROE/EMCON/WRA on mission-roe policy | asset-checklist.md | Needed |
| 3 | Screenshot 03 — Order log + replay scrubber | Capture | Golden hash overlay visible | asset-checklist.md | Needed |
| 4 | Screenshot 04 — Combat domains | Capture | Smoke / hot-tick from combat-domains scenario | asset-checklist.md | Needed |
| 5 | Screenshot 05 — Sensor/EW jammed | Capture | baltic-v2-jammed or comms-challenged | asset-checklist.md | Needed |
| 6 | Screenshot 06 — Mission editor / catalog | Capture | CatalogWriteGate extend-only note | asset-checklist.md | Needed |
| 7 | Screenshot 07 — Band C intercept/spoof | Capture | Difficulty band C stress imagery | asset-checklist.md | Needed |
| 8 | Screenshot 08 — Full replay AAR | Capture | Order log vs world state | asset-checklist.md | Needed |
| 9 | Baltic trailer draft | Video | 1–2 min vertical slice script placeholder | asset-checklist.md | Needed (deferred) |

---

## Deferred / out of scope (v1)

| Item | Reason |
|------|--------|
| Character avatars / portraits | art-bible.md §5 N/A |
| Combat particles / screen shake | art-bible.md §7 N/A |
| Full MIL-STD-2525D atlas beyond 7 frames | ADR-007 Phase C |
| Globe Phase B materials | art-bible.md open items |
| 3D unit meshes | Map-first theater command; APP-6 frames only |

---

## Next steps

1. **User review** — confirm add/remove items; mark deferred rows `Not needed` where appropriate.  
2. Run `/asset-spec system:command-and-control-ui` (or read `design/assets/specs/c2-ui-assets.md` stub) for C2 panel production.  
3. Run `/asset-spec` for Baltic patrol + store capsule stubs.  
4. Cross-link `design/assets/asset-manifest.md` for ASSET-ID tracking.
