# Forge Promote Log — gauntlet-20260727-1455

## Phase `pre` (tier 1) — 2026-07-27T14:55Z

- Hindsight recall: **SKIPPED** — bank server unreachable (`curl http://localhost:8888` → HTTP 000). Proceeding with on-disk corpus only per forge contract.
- Corpus loaded: `coverage-map.json` (20 cells / 24 scenarios), `recipes/recipe-weights.json` (17 recipes), `hard-cases/` (empty — no prior failure signatures yet), `index.yaml` (24 promoted policies).
- Tier 1 ranked recipes: `platform-swap-underused` (weight 1.2, tierMin 1) — the only live tier-1-eligible recipe (`bootstrap-seed` is provenance-only, not selectable for new candidates).
- No hard-case replays injected this tier (pool empty).
- Plan written: `forge/mid-tier-plan.yaml`.
