# Play Mode Sign-off Checklist — UI Maturity Wave 5 — 2026-08-01

**Audience:** Human Editor operator (sign-off authority)  
**Scene:** `Assets/Scenes/DelegationSmoke.unity`  
**Batch proxy (console-clean only):** `tools/unity/Invoke-C2PlayModeSignoffBatch.ps1`  
**Builder SoT:** `Project Aegis → Build DelegationSmoke Scene (comms QA)`  
**Ensure hosts:** `Project Aegis → Ensure UI Maturity Hosts (open scene)`  
**Stack land:** `production/agentic/stack-land-ui-maturity-prs-382-385-2026-08-01.md`  
**Kickoff:** `production/agentic/sprint-ui-maturity-wave5-recommendations-kickoff-2026-08-01.md`

## Preconditions

1. Stack tip includes Wave 1–4 UI maturity (PRs **#382 → #385**) **or** single tip branch `stack/ui-maturity/wave5-tick-chrome-signoff`.
2. `tools/copy-delegation-assemblies.sh` has been run after any pure-C# projection change.
3. No ion tokens in scenes / assets (CesiumSpike uses env / Inspector only).

## Setup (Editor)

| Step | Action | Expected |
|------|--------|----------|
| S1 | Menu **Project Aegis → Build DelegationSmoke Scene (comms QA)** | Scene saved; console has no hard errors |
| S2 | Menu **Project Aegis → Ensure UI Maturity Hosts (open scene)** | Adds any missing maturity hosts; logs `added N host(s)` |
| S3 | Hierarchy contains | `DelegationSmoke`, `C2TopBar`, `MapPlaceholder`, `UnitOrderToolbar`, `AirOps`, `BoatOps`, `MagazineLoadout`, `DeckHangar`, `ScenarioLibrary`, `C2Menu`, … |
| S4 | Enter Play Mode | No red console errors on start (check 1) |

Optional product globe:

| Step | Action | Expected |
|------|--------|----------|
| S5 | **Project Aegis → Build CesiumSpike Scene** | `CesiumSpike.unity`; `useGlobeMap=true`; **no** ion token serialized |
| S6 | Set personal ion token via env `CESIUM_ION_TOKEN` or Inspector only | Tile host reports presence honestly; never commit token |

## Human pass/fail table

Mark each row **Pass** / **Fail** / **N/A**. Failures block land only when marked **Gate**.

| # | Surface | Verify | Gate? | Pass | Tester | Notes |
|---|---------|--------|-------|------|--------|-------|
| 1 | Play start | Play Mode starts; no game/console **Error** / **Exception** for ~5–8 s | **Y** | ☐ | | Batch: `Invoke-C2PlayModeSignoffBatch.ps1` |
| 2 | Order toolbar | `UnitOrderToolbar` visible; buttons issue / refuse with status (select unit first) | **Y** | ☐ | | CMD-16 / CMD-31 |
| 3 | Air Ops launch | `AirOps` list binds; **Launch** enabled for ready row → status clears or refusal reason | **Y** | ☐ | | CMD-24 / LOG-08 |
| 4 | Air Ops abort | **Abort** on launching craft → status / phase update | **Y** | ☐ | | |
| 5 | Boat Ops | `BoatOps` header + list; Launch / Recover / Abort wired (or honest empty) | **Y** | ☐ | | LOG-09…11 |
| 6 | Magazine | `MagazineLoadout` shows weapon / remaining / capacity presentation | **Y** | ☐ | | Wave4 M |
| 7 | Deck / hangar | `DeckHangar` capacity bands bind (spots / ready) | **Y** | ☐ | | Wave4 M |
| 8 | Scenario library | `ScenarioLibrary` lists scenarios; campaigns section present when fixtures loaded | **Y** | ☐ | | CMD-27 / .12 |
| 9 | Layers HUD | `MapPlaceholder` shows `LAYERS: v/t` summary | **Y** | ☐ | | CMD-28.2 |
| 10 | C2 menu / layers | `C2Menu` lists View/Tools/Layers/Window; shortcuts inline; layer row click toggles map stack + summary | **Y** | ☐ | | Wave5 host |
| 11 | Top bar | `C2TopBar` visible; score / phase / COMMS; ZULU / LOCAL / REMAINING if Wave5 T landed | **Y** | ☐ | | CMD-22 when present |
| 12 | Chrome collapse | Collapse control hides/shows MessageLog and/or LeftDrawer presentation | **Y** | ☐ | | CMD-23 when present |
| 13 | CesiumSpike (optional) | Product host + tile gate; no token in scene YAML; package missing → honest status | N | ☐ | | ADR-007 B; not CI smoke |

## Verdict

| Field | Value |
|-------|-------|
| Build / tip SHA | |
| Date | |
| Tester | |
| Result | ☐ PASS · ☐ PASS WITH NOTES · ☐ FAIL |
| Blocking failures | |
| Notes | |

**Blockers:** list check numbers that failed with Gate = Y.

## Batch / proxy notes

- Headless Linux agents: check **1** via `C2PlayModeSignoffBatchRunner` only; checks **2–12** require human Editor or headless projection proxies already green in suite.
- Do **not** treat CesiumSpike as required for stack land of PRs 382–385.

---
*Wave 5 Host + Signoff lane — play-mode checklist.*
