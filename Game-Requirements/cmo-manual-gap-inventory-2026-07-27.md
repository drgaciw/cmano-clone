# CMO Manual → Aegis Gap Inventory

**Date:** 2026-07-27  
**PDF source:** `/home/username01/Downloads/CMO_manual_EBOOK.pdf` (353 pages; text-extractable)  
**Also:** Matrix PDF URL in `cmo-manual-traceability.md`; HTML edition `docs/manual/`  
**Baseline matrix:** [cmo-manual-traceability.md](cmo-manual-traceability.md) (May 2026)  
**Existing vault packages (2026-07-27):** Aircraft Ship Parasite Operations (LOG), CMO Scenario Editor (SCE), Electronic Warfare (EW), Map and View System (MAP), CMO Interface Modules (CUI), Stealth Aircraft Detection (STD), Military Simulation Strategy Framework (MSF), Fleet Coordination (FLT)

## Purpose

Section-level coverage map from the CMO manual PDF against Project Aegis requirement docs, with **gap priority**. Not a page-by-page dump. Clean-room only — no proprietary DB/code/manual text wholesale.

## Legend

| Priority | Meaning |
|----------|---------|
| **P0** | High product-parity gap; author requirements this pass |
| **P1** | Important; inventory only or light cross-ref this pass |
| **Deferred** | Out of v1 / already deferred in hub |
| **Covered** | Existing req + vault note package adequate for this goal |

## Front matter & Ch 1–2

| Manual § | Title | Aegis | Coverage | Gap priority | Notes |
|----------|-------|-------|----------|--------------|-------|
| Pref / Hist | Tone, era | 01 | Partial | Deferred | Tone only |
| 1.1 | System Requirements | 01 NFR / ADR-018 | Partial+ | Covered | Platforms decided |
| 1.3 | Multitaskers | 20 | Draft | P1 | CMD-14 multitasker |
| 2.1 | Terms | 12 | Full | Covered | |
| 2.2 | Fundamentals | 02, 03 | Partial | P1 | |

## Chapter 3 — User Interface

| Manual § | Title | Aegis | Coverage | Gap priority | Notes |
|----------|-------|-------|----------|--------------|-------|
| 3.1 Globe | Map display | 20, MAP-* | Partial+ vault | Covered | MAP package |
| 3.2 Mouse | Selection | 20, MAP-10/11 | Partial | Covered | CUI/MAP |
| 3.3.1–2 Engage | Auto/manual | 14, CUI-10/11 | Partial | Covered | |
| 3.3.3 Plot | Course | 20, CUI-06/07 | Partial | Covered | |
| 3.3.4 Throttle/Alt | Flight regime | 20, 16, CUI-08 | Partial | Covered | |
| 3.3.5 Formation | Formations | 11, 20, FLT-*, SCE-17 | Partial | Covered | FLT package |
| 3.3.6 Magazines | Magazines | 16 LOG | Partial+ | Covered | |
| 3.3.7–8 Air/Boat ops | Parasite | 16 LOG-09–21 | Partial+ vault | Covered | Parasite note |
| 3.3.9 Mounts | Weapons | 14, 18 | Draft | P1 | |
| 3.3.10 Sensors | Sensors | 15, STD, EW | Partial+ vault | Covered | |
| 3.3.11 Damage | Systems | 18 | Draft | **P0** | Expand DOM damage/repair |
| 3.3.12–16 Doctrine tabs | ROE/EMCON/WRA/Withdraw | 13 ROE-* | Partial | P1 | LOG-20 linked |
| 3.3.17 Mission Editor | Missions | 11 Full | Full authoring | Covered | |

## Chapter 4 — Menus

| Manual § | Title | Aegis | Coverage | Gap priority | Notes |
|----------|-------|-------|----------|--------------|-------|
| 4.1.1 Attack options | Engage UI | 14, CUI | Partial | Covered | |
| 4.1.2 ASW actions | ASW | 18 | Draft | **P0** | Domain ASW package |
| 4.1.4 Group ops | Groups | 04, 20, FLT | Partial+ | Covered | |
| 4.1.5 Scenario Editor | ScenEdit | 11, SCE | Full/Partial+ | Covered | SCE vault |
| 4.3–4.4 Symbols / group view | Map | 20, MAP-12 | Partial | Covered | |
| 4.5 Right panel | Unit detail | 20, CUI-01–03 | Partial+ | Covered | |

## Chapter 5 — ScenEdit

| Manual § | Title | Aegis | Coverage | Gap priority | Notes |
|----------|-------|-------|----------|--------------|-------|
| 5.1–5.2 Walkthroughs | Workflows | 11 | Partial | P1 | |
| 5.3 Lua | Scripting | 11, ADR-014 | Partial (no Lua v1) | Deferred | Typed DSL v1 |
| 5.4.1 Times | Temporal | 11, SCE-09 | Partial+ | Covered | |
| 5.4.2 Database | dbRef | 06, SCE-01 | Partial+ | Covered | |
| 5.4.3 Campaigns | Campaigns | 01 | Deferred | Deferred | |
| 5.4.4–10 Sides/weather/features | Authoring | 11, SCE | Partial+ | Covered | |
| 5.5 Events TCA | Events | 11 AME-5 | Full/Partial+ | Covered | |
| 5.5.2 Special Actions | Special actions | 11, 13 | Partial | **P0** | SPA package |
| 5.6–5.8 SBR / import | Tools | 06, 11 | Partial | P1 | SCE-23 packs |

## Chapter 6 — Drop-down menus

| Manual § | Title | Aegis | Coverage | Gap priority | Notes |
|----------|-------|-------|----------|--------------|-------|
| 6.3.1–2 Time | Compression | 03, MSF-08 | Partial+ | Covered | |
| 6.3.3 OOB | Order of battle | 20 | Draft | P1 | |
| 6.3.8–9 Doctrine | Side policy | 13 | Partial | P1 | |
| 6.3.10 Special Actions | SA | 11, 13 | Partial | **P0** | SPA |
| 6.3.11–14 Recorder/log/losses/score | AAR | 17 | Draft | P1 | |
| 6.4–6.5 Options / map settings | UI | 20, MAP | Partial+ vault | Covered | |
| 6.10 Help | Help | — | N/A | Deferred | |

## Chapter 7 — Missions

| Manual § | Title | Aegis | Coverage | Gap priority | Notes |
|----------|-------|-------|----------|--------------|-------|
| 7.1 Editor | Mission UI | 11 | Full | Covered | |
| 7.2.1–4 Ferry/Support/Patrol/Strike | Archetypes | 11 | Full authoring | Covered | |
| 7.2.5–6 Mining / mine-clear | Mines | 11 AME-3.6 | Deferred Phase 2.4 | **P1** | Light DOM mines under P0-A |
| 7.2.7 Cargo | Cargo | 11 | Deferred | Deferred | |
| 7.3 Reference points | RP | 11, MAP-21 | Full/Partial+ | Covered | |
| Marshalling patterns | Fleet coord | 11 FLT | Phase N vault | Covered | FLT note |

## Chapter 8 — Databases

| Manual § | Title | Aegis | Coverage | Gap priority | Notes |
|----------|-------|-------|----------|--------------|-------|
| 8.1–8.10 DB rebuild/templates | Catalog | 06, 21 | Partial | P1 | Platform editor Partial+ |

## Chapter 9 — Combat

| Manual § | Title | Aegis | Coverage | Gap priority | Notes |
|----------|-------|-------|----------|--------------|-------|
| 9.1 Sensors/Weapons | Sense/fire | 15, 14, STD | Partial+ | Covered | |
| 9.2.1 Air combat | Air domain | 18 | Draft | P1 | |
| 9.2.2 Naval combat | Surface | 18 | Draft | P1 | |
| 9.2.3 Submarine combat | Sub/ASW | 18 | Draft | **P0** | DOM ASW/sub |
| 9.2.4 Mine warfare | Mines | 18, 11 | Draft | **P0** | DOM mines |
| 9.2.5 Land combat | Land | 18 | Draft | **P0** | DOM land |
| 9.2.6 EW | EW | 15 EW-* | Partial+ vault | Covered | |
| 9.2.7 Damage/repairs | Damage | 18 | Draft | **P0** | DOM damage |
| 9.2.8 Weapon won't fire | Explain | 13, 14 ENG | Partial | **P0** | FireAbort catalog |
| 9.2.9 DLZ | Launch zone | 14 | Draft | **P0** | ENG DLZ |
| 9.2.10 Losses | Losses | 17 | Draft | P1 | |
| 9.3 Base construction | Air/naval bases | 18, SCE-18/19 | P1 draft | **P0** | DOM construction |

## Chapter 10 — Appendices

| Manual § | Title | Aegis | Coverage | Gap priority | Notes |
|----------|-------|-------|----------|--------------|-------|
| 10.1 Keyboard | Hotkeys | 20 CUI-16 | Draft | **P0** | KEY map package |
| 10.2 Custom overlays | Layers | 20 MAP-07 | P2 | Covered/P1 | MAP |
| 10.3 DB edit ScenEdit | DB | 06, 11 | Partial | P1 | |
| 10.4–10.6 Units/tactics | Reference | 06, genre | Reference | Deferred | |
| 10.7 Comms/Cyber | Comms | 19 | Draft | **P0** | CYB expand |
| 10.8 Tacview | Export | 17 | P2 | Deferred | |

## Ch 11–13 Glossary / history / credits

| Manual § | Aegis | Coverage | Gap |
|----------|-------|----------|-----|
| 11 Glossary | 12 | Full | Covered |
| 12–13 History/Credits | — | N/A | N/A |

## P0 gap set for this goal (author requirements)

| ID | Domain | Deliverable |
|----|--------|-------------|
| **P0-A** | Combat domains (ASW, mines, land, damage/repair, base construction) | Vault note + expand req **18** (`DOM-10+`) |
| **P0-B** | Engagement (DLZ, weapon-won't-fire catalog, BOL) | Vault note + expand req **14** (`ENG-10+`) |
| **P0-C** | Keyboard commands + Special Actions | Vault note + expand req **20** / cross-ref **11** (`KEY-*`, `SPA-*`) |
| **P0-D** | Comms disruption & cyber (manual §10.7) | Vault note + expand req **19** (`CYB-10+`) |

**Not re-authored this pass (already packaged in vault/git):** LOG, SCE, EW, MAP, CUI, STD, MSF, FLT.

## Parallel agent assignment

| Agent | Domain | Sections | Output |
|-------|--------|----------|--------|
| A | Combat domains | 4.1.2, 9.2.3–5, 9.2.7, 9.3 | Draft `DOM` requirements markdown |
| B | Engagement / DLZ | 9.2.8–9.2.9, 3.3.1–2, 4.1.1 | Draft `ENG` extensions markdown |
| C | Keyboard + Special Actions | 10.1, 5.5.2, 6.3.10 | Draft `KEY`/`SPA` markdown |
| D | Comms & cyber | 10.7 | Draft `CYB` extensions markdown |

Agents return text only — orchestrator merges to avoid write races.

## Maintenance

- Update [cmo-manual-traceability.md](cmo-manual-traceability.md) when P0 packages land.
- Owner: requirements-analyst / military-simulation-architect for combat rows.
