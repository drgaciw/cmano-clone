# Live saboteur calibration — 2026-07-31 (UnityAdapter ReplayGolden)

## What ran

| Field | Value |
|-------|--------|
| Commit | `667aa1c` (`main` post-#372) |
| Tool | `python3 tools/qa-gauntlet/saboteur.py` (full catalog, 8 mutants) |
| ReplayGolden project | `src/ProjectAegis.Delegation.UnityAdapter.Tests` (`REPLAY_GOLDEN_PROJECT`) |
| Subset ladder | tiers `1 3 5` (per `saboteur.SUBSET_TIERS`) |
| Date (UTC) | 2026-07-31 |

## Kill rate

**5/6** = `caught_defects / (caught_defects + survived_defects)`

| Role accounting | |
|-----------------|--|
| Defects caught | 5 (01, 02, 04, **05**, 07) |
| Defects survived | 1 (**03-salvo-off-by-one**) |
| Control 00 | survived (correct) |
| Expected-miss 06 | survived (tracked, not fail) |
| Exit code | **1** (only for defect survivor 03) |

## Delta vs prior artifacts

| Artifact | Kill rate | Notes |
|----------|-----------|--------|
| `calibration-2026-07-28-postrebase` | measured outcomes (pre-role formula prose) | live |
| `calibration-2026-07-31-role-refresh` | **4/6** recompute | outcomes reused; 05 still survived |
| **this run** | **5/6** live | **05 caught via `replay_golden`** after #372 point UnityAdapter suite |

### Why 05 flipped

`05-contact-lifecycle-skip` previously reported as an oracle blind spot when saboteur invoked `ProjectAegis.Delegation.Tests --filter ReplayGolden` (wrong suite). With Baltic ReplayGolden in UnityAdapter.Tests, fired oracles = `replay_golden` → **caught**.

### Still open

- **03-salvo-off-by-one** — still survives (file remains: `BUG-oracle-blindspot-03-salvo-off-by-one`)
- **06-emcon-engage-bypass** — expected-miss until EMCON retrofit
- **05** — previously filed blind spot may be closed or reclassified as “caught by ReplayGolden only” (ladder goldens still did not fire)

## Not invented

All outcomes from this live worktree apply → build → subset gauntlet → ReplayGolden sequence. No hand-edited kill matrices.
