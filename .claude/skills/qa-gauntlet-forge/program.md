# QA Gauntlet Forge — program.md (locked direction)

Human-owned constraints for the forge loop. Agents follow this file; do not
silently weaken it. Analogous to Karpathy autoresearch `program.md`.

## Optimize for

1. **Coverage growth** — new cells in mission × domain × ROE × EMCON × event class
   (and underused catalog `platformId`s).
2. **Useful pressure** — scenarios that exercise rare branches or surface real
   `sim-code` / `scenario-data` defects (not oracle green-wash).
3. **Reproducible curriculum** — promoted policies + recipe weights committed;
   ephemeral candidates never become CI truth without promotion gates.

## Single editable surface

You may modify:

- `production/qa/gauntlet/corpus/**` (recipes, weights, coverage-map, hard-cases, index)
- Ephemeral `production/qa/gauntlet/<RUN_ID>/forge/**` (except do not commit `candidates/`)
- Promoted `data/scenarios/gauntlet-*.policy.json` **only after** scorecard promote

You may **not** modify as part of forge learning:

- `GauntletOracleEvaluator` and related locked eval C#
- Demo batch harness scoring paths
- Replay golden fixtures / Baltic v2 hash
- `DelegationBridge.cs`
- `.github/workflows/gauntlet-oracle.yml`
- CatalogWriteGate write paths (unless separate EXTEND-ONLY story)

## Metric (scorecard)

Hard gates are binary (all must pass). Novelty score is the scalar keep/discard
signal. Run:

```bash
python3 tools/qa-gauntlet/forge_scorecard.py \
  --run-dir production/qa/gauntlet/<RUN_ID> \
  --tier <N>
```

Hyphenated wrapper `forge-scorecard.py` is equivalent.

Commit winners only when novelty improves corpus coverage or hard-cases gain a
unique failure signature **and** hard gates pass.

## Tick budgets (expect calibration)

| Tier | Ticks |
|------|-------|
| T1 | 6 |
| T2 | 10 |
| T3 | 16 |
| T4 | 24 |
| T5 | 40 |

CI fixture smoke uses `--ticks 10` — never use it as ladder authority for T3–T5
expect envelopes. Follow `tools/qa-gauntlet/README-expect-regen.md`.

## Experiment discipline

- One focused mutation family per candidate when possible (interpretable diffs).
- Prefer weighting under-covered dims over random churn.
- After 5 consecutive discards on a recipe family → stuck: down-weight + AAR note.
- Retain FAILED attempts to Hindsight bank `qa-gauntlet-forge`.

## Seeds

Default ladder seeds: `42,7,123`. Determinism gate: identical seed → identical
fingerprint.

## Out of scope

- Runtime adaptive mutation inside sim ticks
- Replacing `/qa-gauntlet` TDD remediator
- Launch / commercial scope
