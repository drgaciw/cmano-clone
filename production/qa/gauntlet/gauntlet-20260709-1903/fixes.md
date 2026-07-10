# Gauntlet TDD / defect fix log — `gauntlet-20260709-1903`

## FIX-SD-001 — Tier 4–5 rosters omitted Baltic harness seed platforms

- **Commit:** `c60faf5` — `qa(gauntlet): run gauntlet-20260709-1903 ladder + roster oracle-0 fix`
- **Classification:** `scenario-data` (not sim-code)
- **Tier found:** Oracle-0 catalog resolution after ladder batch (tiers 4–5 FAIL; tiers 1–3 PASS)
- **Symptom:** Multi-domain tier rosters listed Visby/Gripen/Gotland/etc. but omitted `u1`, `hostile-1`, `hostile-far`. Gauntlet policies still detect/engage via harness seeds → oracle-0 unresolved observer/target IDs.
- **Red (TDD-style invariant):**
  - `GauntletRosterValidatorTests.Missing_seed_platforms_in_roster_fails_resolution` — pure unit fixture reproduces the gap.
  - Live assertion: `Live_gauntlet_1903_all_tier_policies_resolve_against_rosters` would fail pre-fix.
- **Green fix:**
  - Merge seed platforms + sensors + magazines into `tier-4/roster.json` and `tier-5/roster.json`.
  - Append seed IDs to policy `gauntlet.catalogRefs` and sync `data/scenarios/gauntlet-t4-*.policy.json` / `gauntlet-t5-*.policy.json`.
  - Ship `GauntletRosterValidator` (`ProjectAegis.Data/Catalog/GauntletRosterValidator.cs`) so oracle-0 is reusable code, not a one-off script.
- **Verify:** 3/3 `GauntletRosterValidatorTests` green; re-eval oracle-eval.json all tiers PASS; batch CSVs already green (stability unchanged).
- **Impact:** Data-only roster/policy + new pure validator; no sim engage path edits. No CRITICAL symbols.
- **Parallelism:** Single scenario-data domain (sequential). No independent sim-code cluster to parallelize.

## Sim-code defects

None found during preflight or ladder batch (stability/determinism/sanity all PASS; no unhandled exceptions).
