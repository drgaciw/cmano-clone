# BUG-oracle-blindspot-05-contact-lifecycle-skip

| Field | Value |
|---|---|
| **Found by** | `/qa-gauntlet-calibrate` first run |
| **Class** | oracle blind spot (ladder coverage) |
| **Severity** | Medium |
| **Status** | **CLOSED** (2026-07-31) — ladder exercises Classified/Identified |

## Resolution

- **2026-07-31 live (#374):** caught via Baltic ReplayGolden (UnityAdapter) alone.
- **2026-07-31 ladder close:** added `gauntlet-t3-logistics-contact-lifecycle` with
  `contactLifecycle.classifyAfterTicks: 1`, `identifyAfterTicks: 2` and
  `requireFingerprintSubstrings: ["Classified","Identified"]`. Mutant 05 skips
  Classified → substring/goldens fail on subset tier 3 without needing ReplayGolden.

## Residual note

Other ladder scenarios may still stay Detected-only; the new tier-3 pin is the
required run-wide proof for the transition site.
