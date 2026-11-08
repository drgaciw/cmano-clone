# S118 / DRG-65 — `ew: moderate` research note (2026-08-12)

**Verdict:** **Won't-retune** until a 3-seed derive+control measurement exists.  
**Gate meaning:** documented in `tools/qa-gauntlet/README-stress-axes.md` § Unproven non-`off` levels.

## What is known

| Level | jamStrength | Evidence |
|-------|-------------|---------|
| extreme | 0.9 | Measured: Detected 30 → 25 across seeds 42/7/123 (per-seed 2/2/1) |
| moderate | 0.5 | **Not measured.** Single `basePd 1.0` trial; expected aggregate delta 0 |

## Why not retune in S118

Raising `jamStrength` (or adding detection trials) without a control sibling run would guess a proof threshold. DRG-63's production gate already hard-fails unproven non-config-only axes — claiming `ew: moderate` today would make the gate noisy for a real reason. Leave the catalog level as authored; do not put `ew: moderate` in evidence maps.

## To reopen

Derive `ew: moderate` vs control on a scenario with real detection targets, batch at tier ticks, compare aggregate `Detected` across seeds 42/7/123. If delta is 0, then retune (higher jamStrength or more trials) and re-measure.
