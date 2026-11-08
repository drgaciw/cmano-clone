# CMO Manual → Project Aegis Requirements Traceability

**Last Updated:** May 29, 2026  
**Manual source:** `docs/manual/index.html` (COMMAND: Modern Operations HTML edition; page bodies are scanned images)  
**PDF reference:** [CMO Manual (PDF)](https://www.matrixgames.com/amazon/PDF/CMO/CMO_manual_EBOOK.pdf)

## Purpose

Map every major manual section to Project Aegis requirement documents so gaps are visible, parity is explicit, and unique (non-CMO) requirements stay traceable to player-facing features.

## Legend

| Coverage | Meaning |
|----------|---------|
| **Full** | Dedicated requirement doc with P0 acceptance criteria |
| **Partial** | Mentioned in an existing doc; needs expansion |
| **Planned** | New doc drafted or listed in index |
| **Deferred** | Out of v1 scope per doc 01; interface only |
| **N/A** | Install/credits/update history — not simulated |

## Front Matter

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| — | Preface | — | N/A |
| — | Historical Introduction | 01 Project Overview | Partial (tone, era) |
| — | What is Command? | 01, 02 | Partial |

## Chapter 1 — Installation

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 1.1 | System Requirements | Tech-Stack.md | Partial (platform targets TBD) |
| 1.2 | Support | — | N/A |
| 1.3 | Notes for Multitaskers and Returning Players | **20** Command UI | Draft |

## Chapter 2 — Introduction

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 2.1 | Important Terms | **12** Terms Glossary | Full (vocabulary) |
| 2.2 | Fundamentals | 02 Core Gameplay Loop | Partial |
| 2.2.1 | Starting COMMAND | 02, 03 | Partial |

## Chapter 3 — User Interface

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 3.1 | Globe Display | **20** Command UI | Draft |
| 3.2 | Mouse Functions | **20** Command UI | Draft |
| 3.3.1 | Engage Target(s) - Auto | **14** Engagement | Draft |
| 3.3.2 | Engage Target(s) - Manual | **14** Engagement | Draft |
| 3.3.3 | Plot Course | **20** Command UI | Draft |
| 3.3.4 | Throttle and Altitude | **20**, **16** Logistics | Draft |
| 3.3.5 | Formation Editor | 11 Mission Editor, **20** | Partial |
| 3.3.6 | Magazines | **16** Logistics | Draft |
| 3.3.7 | Air Operations | **16** Logistics | Draft |
| 3.3.8 | Boat Operations | **16**, **18** Combat | Draft |
| 3.3.9 | Mounts and Weapons | **14**, **18** | Draft |
| 3.3.10 | Sensors | **15** Sensor/EW | Draft |
| 3.3.11 | Systems and Damage | **18** Combat | Draft |
| 3.3.12 | Doctrine | **13** Doctrine | Draft |
| 3.3.13 | General / Strategic | **13**, 04 Delegation | Draft |
| 3.3.14 | EMCON Tab | **13** Doctrine | Draft |
| 3.3.15 | WRA Tab | **13**, **14** | Draft |
| 3.3.16 | Withdraw/Redeploy Tab | **13**, 04 | Draft |
| 3.3.17 | Mission Editor | 11 Mission Editor | Full |

## Chapter 4 — Menus and Dialogs

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 4.1.1 | Attack Options | **14** Engagement | Draft |
| 4.1.2 | ASW-specific Actions | **18** Combat | Draft |
| 4.1.3 | Context Menu, Cont. | **20** Command UI | Draft |
| 4.1.4 | Group Operations | **20**, 04 | Draft |
| 4.1.5 | Scenario Editor | 11 Mission Editor | Full |
| 4.2 | Control Right Click on Map | **20** | Draft |
| 4.3 | Unit/Group/Weapon Symbols | **20** | Draft |
| 4.4 | Group Mode and Unit View Mode | **20** | Draft |
| 4.5.1–4.5.8 | Right Side Information Panel | **20**, **13** | Draft |

## Chapter 5 — ScenEdit

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 5.1–5.2 | Getting Started, Walkthroughs | 11 Mission Editor | Partial (workflows) |
| 5.3 | Lua Scripting | 11 (DSL + Lua shim P1) | Partial |
| 5.4.1 | Scenario Times + Duration | 11 | Full |
| 5.4.2 | Database | 06 DB Intelligence, 11 | Partial |
| 5.4.3 | Campaigns | — | Deferred (doc 01) |
| 5.4.4–5.4.10 | Sides, Briefing, Scoring, Weather, Merge | 11 | Partial–Full |
| 5.5 | Events (TCA) | 11 | Full |
| 5.6 | Scenario Batch-Rebuilder | 06, 11 | Partial |
| 5.7–5.8 | Unit Actions, Import/Export | 11, 06 | Partial |

## Chapter 6 — Drop-Down Menus

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 6.3.1 | Start/Stop | 03 Simulation Modes | Partial |
| 6.3.2 | Time Compression | 03 | Partial |
| 6.3.3 | Order of Battle | **20**, 11 | Draft |
| 6.3.4–6.3.5 | Database Viewer, Browse Platforms | 06 | Partial |
| 6.3.6–6.3.7 | Scenario Description, Side Briefing | 11 | Full |
| 6.3.8–6.3.9 | Side Doctrine/ROE/WRA/EMCON | **13** | Draft |
| 6.3.10 | Special Actions | 11, **13** | Partial |
| 6.3.11 | Recorder | **17** Replay | Draft |
| 6.3.12 | Message Log | **20**, **17** | Draft |
| 6.3.13 | Losses and Expenditures | **17**, **16** | Draft |
| 6.3.14 | Scoring | **17** (metrics), 11 | Draft |
| 6.4 | Game Options | **20** | Draft |
| 6.6–6.9 | Quick Jump, Orders, Contacts, Missions | **20** | Draft |

## Chapter 7 — Missions and Reference Points

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 7.1–7.1.2 | Mission Editor | 11 | Full |
| 7.2.1–7.2.7 | Mission types | 11 | Full (authoring); runtime in 11 + **18** |
| 7.3 | Reference Points | 11 | Full |

## Chapter 8 — Databases and Templates

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 8.1–8.10 | DB binding, rebuild, INI templates | 06 DB Intelligence | Partial |

## Chapter 9 — Combat

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 9.1.1–9.1.2 | Sensors and Weapons | **15**, **14** | Draft |
| 9.2.1–9.2.6 | Domain combat + EW | **18**, **15** | Draft |
| 9.2.7 | Damage and Repairs | **18** | Draft |
| 9.2.8 | My Weapon Won't Fire!!! | **13**, **14** | Draft |
| 9.2.9 | DLZ | **14** | Draft |
| 9.2.10 | Note On Losses | **17** | Draft |
| 9.3 | Construction/Destruction of bases | **18** | P1 (draft) |

## Chapter 10 — Appendices

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 10.1 | Keyboard Commands | **20** | P1 (draft) |
| 10.2 | Custom Overlays | **20** | P2 |
| 10.3 | DB Edit in Scenario Editor | 06, 11 | Partial |
| 10.4–10.6 | Common units/tactics | 06, genre docs | Reference |
| 10.7 | Comms Disruption & Cyber | **19** Cyber | Draft |
| 10.8 | Tacview | **17** | P2 export |

## Chapters 11–13

| Manual § | Title | Aegis doc(s) | Coverage |
|----------|-------|--------------|----------|
| 11 | Glossary | **12** Terms Glossary | Full |
| 12–13 | Update History, Credits | — | N/A |

## Requirement document registry

| ID | File | Status |
|----|------|--------|
| 01–10 | Core, agents, DB, tech, near-future | Approved / draft mix |
| 11 | Agentic Mission Editor | Revised (2026-07-01, implementation-aligned; AME-* IDs, ADR-008/013–017) |
| 12 | Terms Glossary | Active |
| 13 | Doctrine, ROE, EMCON, WRA | Draft |
| 14 | Engagement & Fire Control | Draft |
| 15 | Sensor, Detection & EW | Draft |
| 16 | Logistics & Magazines | Draft |
| 17 | Replay, AAR & Order Log | Draft |
| 18 | Combat Domains | Draft |
| 19 | Cyber & Comms | Draft |
| 20 | Command & Control UI | Draft |

## Unique Aegis themes (cross-cutting)

| Theme | Primary docs |
|-------|----------------|
| Agent delegation & doctrine contract | 04, **13**, 14 |
| Determinism & replay | 03, 08, **17** |
| NL / MCP authoring | 07, 08, 11 |
| Near-future / speculative | 09, 10 |
| Explainability (“why no fire”) | **13**, **14** |

## Maintenance

- Re-run this matrix when adding manual pages or requirement files.
- Owner: `requirements-analyst` (updates), `military-simulation-architect` (combat/sensor rows).
