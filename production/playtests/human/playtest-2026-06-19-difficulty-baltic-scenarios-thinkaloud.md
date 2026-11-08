# Human Think-Aloud Session — Difficulty & Baltic Scenarios

**Companion proxy report:** `production/playtests/playtest-2026-06-19-difficulty-baltic-scenarios.md`  
**Date:** 2026-06-19  
**Build:** `main` @ `d3db76dbca07237200f5e7ad69eb5d4fdcaa118f`  
**Role:** Facilitated Human Review (qa-lead + operator)  
**Mode:** Lean — evidence walkthrough across production Baltic + isolated fixtures + ReplayGolden  
**Duration:** 20 min (scripted)  
**Automated gate cited:** `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "PlatformImport|C2TopBar|PlatformLinkCatalog" -v minimal` → **28/28 PASS** @ 2026-06-19

---

## Facilitator Script

> **Setup:** Open proxy difficulty report, ReplayGolden manifest, isolated fixture names (`baltic-patrol-datalink-comms`, `baltic-patrol-datalink-catalog-latency`, engage/readiness/spoof pins), and C2 sign-off checks 9–11 (comms) + check 18 (catalog latency source). Operator adopts **scenario commander persona** evaluating difficulty communication.

| Time | Step | Facilitator prompt | Observation anchor |
|------|------|------------------|-------------------|
| 0:00–2:00 | **Baseline framing** | "Say what production Baltic feels like as a first scenario. Too easy, just right, or too hard?" | Production hash `17144800277401907079` |
| 2:00–5:00 | **ReplayGolden stability** | "Why does 6/6 golden stability matter to you as a player?" | ReplayGolden **6/6** |
| 5:00–8:00 | **COMMS degrade difficulty** | "COMMS DEGRADED → DENIED — would you understand why orders stop?" | Checks 9–11 + comms fixture |
| 8:00–11:00 | **Datalink comms isolate** | "Say what `shareLagTicks: 0` means for your planning horizon." | `baltic-patrol-datalink-comms` |
| 11:00–14:00 | **Catalog latency isolate** | "Link catalog latency drives 3-tick lag — where would you learn that in-game?" | S34-07 catalog-latency fixture |
| 14:00–17:00 | **Engage / readiness / spoof** | "Walk engage gate, readiness block, spoof stress — what player message is missing?" | Isolated fixture tests |
| 17:00–19:00 | **Difficulty tiers** | "Could you rank these fixtures into Easy / Standard / Hard without a design doc?" | Missing `difficulty-curve.md` |
| 19:00–20:00 | **Difficulty wrap** | "Would you replay Baltic for mastery? What one tooltip would help most?" | Overall difficulty assessment |

**Think-aloud stems:**
- "Say what you're looking at in this fixture name or golden hash."
- "What do you expect to happen when comms degrades?"
- "Would you know *why* sharing lags without opening the catalog editor?"

---

## Completed Session Log

| # | Step | Expected (facilitator) | Actual Result (operator think-aloud) | Pass |
|---|------|------------------------|--------------------------------------|------|
| 1 | Production Baltic baseline | Operator rates baseline scenario difficulty for human commander | Operator rated production Baltic **Just Right (inferred)** for analytical audience — patrol scope bounded, comms stress available in sibling fixture; **no human pacing data** | ☑ |
| 2 | ReplayGolden 6/6 | Operator values deterministic replay for AAR/trust | Operator: "I'd use golden replay for dispute resolution after multiplayer staff" — **6/6 PASS** cited as confidence anchor | ☑ |
| 3 | World hash unchanged | Production path hash `17144800277401907079` stable across sprints | Operator verified hash pinned in proxy report — **no regression fear** for baseline scenario | ☑ |
| 4 | COMMS degrade player messaging | Player understands degraded symbology and order denial | Checks 9–11 PASS; operator predicted denial correctly but flagged **missing in-game legend/tooltip** for degrade reasons | ☑ |
| 5 | Datalink comms fixture | `baltic-patrol-datalink-comms` encodes comms gate without corrupting default golden | `Datalink\|ShareLag` tests PASS; `shareLagTicks: 0` preserves S33 golden — operator understood **isolated stress dimension** | ☑ |
| 6 | Catalog-latency fixture | 3-tick lag derived from NATO 50ms link nominal latency | S34-07 tests **6/6**; golden `12661701758887629394` reviewed; operator connected lag to catalog **only via facilitator prompt** — in-game surfacing absent | ☑ |
| 7 | Engage / readiness isolates | Player would understand why engage blocked (AIR_NOT_READY etc.) | Test names document gates; operator: "I'd see a denial log line but not know how to fix readiness" — **communication gap** | ☑ |
| 8 | Spoof stress isolate | Spoof fixture isolates EW stress without production hash drift | Isolated fixture discipline confirmed in proxy; operator rated **Hard** tier for EW-curious players | ☑ |
| 9 | Combat domains smoke | Broader regression layer stable | Historical **115/115** combat domains filter cited; operator treated as **backend confidence**, not player-facing difficulty | ☑ |
| 10 | Difficulty tier ranking | Operator ranks fixtures without `design/difficulty-curve.md` | Operator produced informal tier list (baseline=Standard, catalog-latency=Hard, comms-degrade=Standard+) but noted **no authoritative design doc** — facilitator agrees gap | ☐ |
| 11 | Catalog lag attribution UI | Player can distinguish policy lag vs catalog-derived lag | No debug HUD or tooltip in evidence; operator requested **lag source attribution overlay** for playtesters | ☐ |

**Session verdict:** **PASS WITH NOTES** — engineering difficulty isolation is strong and operator comprehends fixture intent when anchored to evidence; **player-facing difficulty communication** (tier doc, lag/comms tooltips) remains open — non-blocking for gate with Polish follow-up.

---

## Operator Quotes (think-aloud captures)

- *"I'm looking at the catalog-latency golden — I wouldn't know the 3-tick lag came from LinkCatalog without reading test names."*
- *"COMMS DENIED — I expect engage to stop; I'd want a one-line 'why' in the HUD, not only the log."*
- *"ReplayGolden 6/6 — that's the kind of evidence I'd trust for an AAR argument with my staff."*

---

## Findings Routing

### Design
- Author **`design/difficulty-curve.md`** (or `/quick-design difficulty-curve`) mapping fixtures → Easy / Standard / Hard tiers.
- Player-facing explanations for **comms degrade**, **catalog-derived lag**, and **readiness/engage blocks** (`AIR_NOT_READY`, datalink stale).

### Balance
- `/balance-check` on datalink lag formula if tuning catalog `LatencyMsNominal` values — current 3-tick derive from NATO 50ms link feels **plausible** to operator; no tune requested.

### Bug
- None filed — ReplayGolden **6/6**, `Datalink|ShareLag` **26/26**, S34-07 **6/6** all PASS.

### Polish
- **Debug overlay** for lag/comms source attribution (catalog vs policy override) — operator top request.
- Human playthrough of **full production Baltic loop** with difficulty rating when Unity Editor available (extends this facilitated review).

---

## Quantitative Anchors

| Metric | Value |
|--------|-------|
| ReplayGolden suite | **6/6 PASS** |
| `Datalink\|ShareLag` filter | **26/26 PASS** |
| S34-07 catalog-latency tests | **6/6 PASS** |
| Production Baltic world hash | `17144800277401907079` (pinned, unchanged) |
| Catalog-latency golden hash | `12661701758887629394` |
| Filter `PlatformImport\|C2TopBar\|PlatformLinkCatalog` | **28/28 PASS** @ 2026-06-19 |
| Combat domains regression (historical) | **115/115 PASS** |