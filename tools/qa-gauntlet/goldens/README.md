# Golden anchors — blessed-update runbook

`anchors.json` stores SHA-256 of the full batch fingerprint per (scenarioId, anchor seed).
Anchor seeds: 42, 7, 123. Any mismatch is a red oracle — the strongest regression signal
the ladder has, because the sim is byte-deterministic.

## When a golden mismatch is legitimate

Only when a sim change *intentionally* alters behavior (new feature, approved balance
change, bug fix that moves outcomes). Same discipline as ReplayGolden and
`tools/qa-gauntlet/README-expect-regen.md`: **never bless to silence an unexplained diff.**

## How to re-bless

1. Confirm the diff is explained (link the PR/story/defect in the commit message).
2. Run a full green ladder (all non-golden oracles pass).
3. `python3 tools/qa-gauntlet/evaluate_run.py bless --run-dir production/qa/gauntlet/<RUN_ID> --run-id <RUN_ID> --goldens tools/qa-gauntlet/goldens/anchors.json`
4. Commit `anchors.json` with message `qa(gauntlet): re-bless goldens — <why>`.
