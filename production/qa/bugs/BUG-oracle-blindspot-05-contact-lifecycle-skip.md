# BUG-oracle-blindspot-05-contact-lifecycle-skip

| Field | Value |
|---|---|
| **Found by** | `/qa-gauntlet-calibrate` first run (`production/qa/gauntlet/calibration-2026-07-28/report.md`) |
| **Class** | oracle blind spot (ladder coverage gap) + prior wrong ReplayGolden target |
| **Severity** | Medium — ladder still does not exercise Classified+; saboteur now catches via ReplayGolden |
| **Status** | **MITIGATED** (2026-07-31 live) — mutant **caught** by Baltic ReplayGolden; ladder `goldens` still silent |

## What survived (historical)

Mutant `05-contact-lifecycle-skip` (`PdDetectionContactSimulator`: the
`Detected → Classified` transition jumps straight to `Identified`) survived the
subset ladder when saboteur pointed at the **wrong** ReplayGolden suite
(`ProjectAegis.Delegation.Tests`).

## 2026-07-31 re-measure (live)

After #372 (`REPLAY_GOLDEN_PROJECT` → `UnityAdapter.Tests`):

| Surface | Outcome |
|---------|---------|
| Subset ladder (tiers 1/3/5) goldens/victory | did **not** fire |
| Baltic ReplayGolden (`UnityAdapter.Tests`) | **fired → caught** |
| Kill rate impact | 4/6 → **5/6** (`calibration-2026-07-31-live-unity-replay`) |

So the mutant is no longer a full saboteur blind spot, but the **shipped ladder
still does not exercise Classified/Identified** (only Unknown/Detected in
canonical fingerprints). That residual gap remains product risk if ReplayGolden
is skipped.

## Root cause (ladder gap — still open)

**The contact-lifecycle state machine beyond `Detected` never executes in the ladder.**
Canonical-run fingerprint counts across all 22 scenarios × 5 seeds (historical):

| State | Occurrences |
|---|---|
| Unknown | 306 |
| Detected | 164 |
| **Classified** | **0** |
| **Identified** | **0** |
| **Lost** | **0** |

No ladder policy configures classify/identify tick thresholds, so the mutated
transition site is dead code under the ladder alone.

## Fix direction (residual)

Add/retrofit a scenario with classify/identify thresholds inside its tick budget
(the variability plan's `gauntlet-t3-logistics-contact-lifecycle` covers this), then
add `Classified` (and ideally `Identified`) to `requiredRunWide` in
`tools/qa-gauntlet/expected-tokens.json` so the gap can never silently reopen
even without ReplayGolden. Re-run calibrate; expected fired oracles should include
`goldens` / token_coverage, not only `replay_golden`.
