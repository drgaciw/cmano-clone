# Playtest Report — Difficulty & Scenario Curve (Baltic Fixtures)

## Session Info
- **Date**: 2026-06-19
- **Build**: `main` @ `d3db76dbca07237200f5e7ad69eb5d4fdcaa118f`
- **Duration**: ~20 min equivalent (ReplayGolden + isolated fixture audit)
- **Tester**: QA lean proxy synthesis
- **Platform**: Linux headless (`dotnet test` harness)
- **Input Method**: N/A (deterministic replay)
- **Session Type**: Targeted test — difficulty/scenario isolation across Baltic policy variants

## Test Focus
Difficulty and scenario curve signals across **production Baltic**, **isolated stress fixtures** (engage, readiness, spoof, datalink comms, catalog latency, combat domains), and **ReplayGolden 6/6** stability.

## First Impressions (First 5 minutes)
- **Understood the goal?** **Yes (proxy)** — each fixture encodes a bounded stress dimension with pinned golden hash.
- **Understood the controls?** N/A — headless replay, not interactive difficulty tuning UI.
- **Emotional response**: **Engaged (inferred)** for simulation-audience; frustration risk if players hit opaque readiness/engage blocks without UI explanation.
- **Notes**: Production world hash `17144800277401907079` unchanged; isolated fixtures excluded from default ReplayGolden catalog.

## Gameplay Flow

### What worked well
- **ReplayGolden 6/6** — default path stable across sprints.
- **Isolated fixtures** discipline: `baltic-patrol-datalink-comms`, `baltic-patrol-datalink-catalog-latency`, engage/readiness/spoof pins each have dedicated tests + golden files.
- **Combat domains** regression smoke (S33-09 path) — 115/115 filter PASS historical baseline.
- **Catalog share lag**: explicit `shareLagTicks: 0` on comms fixture preserves S33 golden; catalog-latency fixture derives 3-tick lag from NATO 50ms link.

### Pain points
- **No `design/difficulty-curve.md`** — Severity: Medium (cannot compare player experience to design intent).
- **Isolated fixtures are harness-only** — Severity: Low for dev; Medium for player-facing difficulty communication.

### Confusion points
- Players may not understand *why* sharing lags or comms degrades without in-game doctrine/tooltip surfacing (UI gap).
- Multiple overlapping datalink fixtures (comms gate vs catalog latency) — clear for engineers, unclear for players.

### Moments of delight
- Deterministic divergence between fixtures proves tunable difficulty knobs without corrupting production Baltic baseline.

## Bugs Encountered

| # | Description | Severity | Reproducible |
|---|-------------|----------|--------------|
| — | None filed | — | — |

## Feature-Specific Feedback

### Production Baltic (`baltic-patrol.policy.json`)
- **Understood purpose?** Yes (baseline)
- **Found engaging?** Unknown — needs human scenario playthrough
- **Suggestions**: Baseline playtest session with AAR review (order log).

### Datalink catalog-latency fixture (S34-07)
- **Understood purpose?** Yes (engineering)
- **Found engaging?** N/A headless
- **Suggestions**: Expose lag source (catalog vs policy override) in debug HUD for playtesters.

### Engage / readiness / spoof isolates
- **Understood purpose?** Partial — tests document gates; player messaging unvalidated
- **Found engaging?** Unknown
- **Suggestions**: `/quick-design` difficulty communication strings for `AIR_NOT_READY`, datalink stale, etc.

## Quantitative Data
- ReplayGolden suite: **6/6** PASS
- `Datalink|ShareLag` filter: **26/26** PASS
- S34-07 catalog-latency tests: **6** PASS; golden `12661701758887629394`
- Production Baltic world hash: `17144800277401907079` (pinned, unchanged)

## Overall Assessment
- **Would play again?** **Maybe** — depends on human scenario pacing (not measured here)
- **Difficulty**: **Unknown** for humans; **Just Right** for automated regression layering
- **Pacing**: **Good** for batch analysis; **Unknown** for real-time command pressure
- **Session length preference**: **Good** for replay analysis sessions

## Top 3 Priorities from this session
1. Author `design/difficulty-curve.md` (or `/quick-design`) mapping fixtures → player-facing difficulty tiers.
2. Human playthrough of **production Baltic** full loop with difficulty rating (Too Easy / Just Right / Too Hard).
3. Add player-facing explanations for comms degrade + catalog-derived lag (UX/tooltip pass in Polish).

## Action Routing
- **Design changes:** Difficulty communication — `/quick-design difficulty-curve`
- **Balance:** `/balance-check` on datalink lag formula if tuning catalog latencies
- **Bugs:** None
- **Polish:** Debug overlay for lag/comms source attribution

**Session mode:** Lean proxy synthesis — strong **engineering** difficulty isolation; **player difficulty curve** unvalidated.