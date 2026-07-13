# Gauntlet Tiers 1–2: Full Catalog DB-Driven Combat Path

**Date:** 2026-07-13  
**Goal:** Eliminate synthetic `u1` / `hostile-1` / `hostile-far` from tier-1 and tier-2 gauntlet combat (detection + engage).

## Platforms (already in `baltic_patrol.db`, cmo-db-derived imports)

| Role | platform_id | Nation | Domain | Sensors | Mag lines w/ range>0 | Max weapon range (m) |
|---|---|---|---|---|---|---|
| Blue primary shooter | `k-31-visby-2009` | Sweden | surface | 8 | 2 | 8300 |
| Blue escort secondary (t2-escort-a) | `k-22-gavle-ex-goteborg-class` | Sweden | surface | 6 | 4 | 148200 |
| Red near target | `em-sovremenny-i-pr-956-sarych` | Russia | surface | 6 | 3 | 120000 |
| Red far target | `mpk-steregushchiy-pr-20380-2018` | Russia | surface | 8 | 12 | 148200 |

No new cmo-db scrape was required — all four platforms were already combat-usable in the catalog DB (prior cmo-db import waves).

## Policy changes

Every `gauntlet-t1-*` and `gauntlet-t2-*` policy now has:

- `gauntlet.units` with real catalog blue (+ red) platform ids  
- `detection[].observerId` / `targetId` set to those catalog ids (no synthetic combat ids)  
- `catalogRefs` cleaned of `u1` / `hostile-1` / `hostile-far`  
- `gauntlet.expect` recalibrated after multi-unit catalog envelopes (see batch)

## Harness / side-map changes (skeptic remediation)

`BalticV3SideRegistry`:

- Scenario-scoped `RegisterScenarioSide(platformId, blue|red)` for gauntlet ORBAT units.  
- `IsBlueForceUnit` / `IsRedForceUnit` honor catalog ids (not only synthetic `u1`/`hostile-1`).  
- `GetDefaultBlueUnitId` / `GetDefaultRedUnitId` feed engage-victim fallbacks.  
- Cleared in `try/finally` around each `BalticReplayHarness.Run`.

`SimulationSession.ResolveEngageVictim`:

- Red shooters → `PrimaryBlueForceContactId` or catalog default **blue** (not always `u1`).  
- Blue shooters → `PrimaryHostileContactId` or catalog default **red**.

`HostileContactFilter`:

- Engageable only if registered **red** or legacy `hostile*` prefix.  
- Catalog **blue** slugs are never hostiles (kebab-case heuristic removed).

`BalticReplayHarness` HeadlessSnapshot:

- `PrimaryBlueForceContactId` falls back to registered catalog blue so red can lawfully engage blue without reverse detection trials.

`BalticReplayHarness` ORBAT:

- Prefers catalog red as `hostileBinding`; synthetic `hostile-1` only for blue-only ORBAT (joint smoke / ReplayGolden safety).

## Verification evidence (scratch)

- `catalog-id-proof.txt` — SQL combat-usable proof for all four platform ids  
- `orbat-harness-tests.log` — tier12 catalog + joint ORBAT tests green  
- `t1t2-results.csv` / `t1t2-batch.log` — 24 batch rows, `CATALOG_UNIT`/`MAGAZINE_SEED` for Visby, zero synthetic launches  
- `t1t2-oracle-eval.json` — C# `GauntletOracleEvaluator` all 8 scenarios Passed  
- `replay-golden.log` — ReplayGolden + gauntlet tests green  
- `full-suite.log` — full solution suite  

## Engagement direction (post side-fix)

Batch of 24 rows (t1 ticks=6, t2 ticks=10, seeds 42/7/123):

| Metric | Count |
|---|---|
| Blue-on-red successful launches | 36 |
| Red-on-blue successful launches | 26 |
| Red-on-red | **0** |
| Synthetic-id launches | **0** |

Tests: `Tier12_weapons_free_blue_launches_at_red_not_red_on_red` asserts fingerprint `Engagement|…|True|Launched` + next `EngagementOutcome` victim is opposite side.

## Expect recalibration rationale

After side-correct multi-unit combat, scores include mutual kills (e.g. patrol-b seed 42 score 200 / 2 kills). Bounds re-derived from observed deterministic envelopes with small slack; side BLUE; `requireNonEmptyFingerprint` true.
