# BUG-oracle-blindspot-05-contact-lifecycle-skip

| Field | Value |
|---|---|
| **Found by** | `/qa-gauntlet-calibrate` first run (`production/qa/gauntlet/calibration-2026-07-28/report.md`) |
| **Class** | oracle blind spot (ladder coverage gap) |
| **Severity** | Medium-High — an entire claimed subsystem is unexercised |
| **Status** | OPEN |

## What survived

Mutant `05-contact-lifecycle-skip` (`PdDetectionContactSimulator`: the
`Detected → Classified` transition jumps straight to `Identified`) survived the full
anchor ladder subset and ReplayGolden with byte-identical fingerprints.

## Root cause

**The contact-lifecycle state machine beyond `Detected` never executes in the ladder.**
Canonical-run fingerprint counts across all 22 scenarios × 5 seeds:

| State | Occurrences |
|---|---|
| Unknown | 306 |
| Detected | 164 |
| **Classified** | **0** |
| **Identified** | **0** |
| **Lost** | **0** |

No ladder policy configures classify/identify tick thresholds, so the mutated
transition site is dead code under the entire ladder. Note this refines the
2026-07-27 variability-plan finding #5 (which reported lifecycle states "confirmed
live" in run `gauntlet-20260727-1455`): only `Unknown`/`Detected` are live in the
shipped ladder.

## Fix direction

Add/retrofit a scenario with classify/identify thresholds inside its tick budget
(the variability plan's `gauntlet-t3-logistics-contact-lifecycle` covers this), then
add `Classified` (and ideally `Identified`) to `requiredRunWide` in
`tools/qa-gauntlet/expected-tokens.json` so the gap can never silently reopen.
Re-run `/qa-gauntlet-calibrate`; mutant 05 must flip to caught.
