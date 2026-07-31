# BUG-gauntlet-emcon-dimension-not-exercised

| Field | Value |
|---|---|
| **Status** | **CLOSED** (2026-07-31) for engage-side gate observability |
| **Related** | mutant `06-emcon-engage-bypass` role flipped `expected-miss` → `defect` |

## Resolution

Added `gauntlet-t3-emcon-engage-block` with real top-level `emcon.units` Passive on
Visby and `requireFingerprintSubstrings: ["EMCON_OFF"]`. Healthy runs emit
`EMCON_OFF`; mutant opens the engage gate and diverges. `EMCON_OFF` promoted to
`requiredRunWide` in `tools/qa-gauntlet/expected-tokens.json`.

Legacy `gauntlet.emcon` prose on older scenarios may still warn until full
variability-plan retrofit; engage-side observability no longer depends on them.
