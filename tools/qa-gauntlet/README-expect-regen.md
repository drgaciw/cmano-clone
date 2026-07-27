# Gauntlet expect regen — operator runbook

Short runbook for refreshing `gauntlet.expect` on shipped policies. Full discipline: [`production/qa/gauntlet-expect-ci-discipline-2026-07-14.md`](../../production/qa/gauntlet-expect-ci-discipline-2026-07-14.md).

## When to regen

Regenerate expects when:

- Tier **tick budget** changes (ladder defaults: T1=**6**, T2=**10**, T3=**16**, T4=**24**, T5=**40**)
- Policy **intent** changes (units, injects, ROE, EMCON, victory)
- Sim fix **legitimately** moves score / kills / missiles / denials
- Oracle triage classifies a fail as **`oracle`** (wrong envelope), not sim-code
- Max-variance / full ladder promotion needs re-baseline

Do **not** regen to hide an unexplained sim regression.

## Do not hand-edit envelopes

1. Run a **real** Demo batch CSV at the **matching tier tick** (not a guessed envelope).
2. Derive min/max bounds from observed rows (prefer seeds `42,7,123` for ladder).
3. Keep fingerprint / multi-domain gates unless scenario design changes.
4. Re-run `gauntlet_oracle_eval` until `allPassed: true`.

**Never** tighten or loosen `minScore` / `maxScore` / missile / denial fields without re-running the batch CSV at the correct tick budget.

## Commands

```bash
# Batch (example T3)
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t3-emcon-phases,gauntlet-t3-escort-strike \
  --seeds 42,7,123 --ticks 16 \
  --csv-out /tmp/gauntlet-t3/results.csv

# Oracle (policy path = refreshed .policy.json)
dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- \
  gauntlet_oracle_eval \
  --policy data/scenarios/gauntlet-t3-emcon-phases.policy.json \
  --csv /tmp/gauntlet-t3/results.csv \
  --out /tmp/gauntlet-t3/oracle-eval.json
```

CI mirror (10-tick fixture smoke + fail-closed strip): see [`.github/workflows/gauntlet-oracle.yml`](../../.github/workflows/gauntlet-oracle.yml). **GHA may be billing-gated — run the local dry-run before merge.**

## Closed defects

Re-test closed registry entries after a fix:

```bash
tools/qa-gauntlet/retest-defect.sh <defect-id> --out-dir /tmp/gauntlet-retest
```

Registry: `production/qa/gauntlet-defect-registry.json`.

## Forge promote → expect regen

When `/qa-gauntlet-forge` **promotes** an ephemeral candidate into
`data/scenarios/`:

1. Score with the mechanical helper (does not replace locked oracle eval):

```bash
python3 tools/qa-gauntlet/forge_scorecard.py \
  --run-dir production/qa/gauntlet/<RUN_ID> \
  --tier <N>
```

(Hyphenated alias `forge-scorecard.py` also works.)

2. Copy the winner to `data/scenarios/<id>.policy.json`.
3. **Immediately** regen `gauntlet.expect` at the **tier tick** above (batch CSV →
   envelopes → `gauntlet_oracle_eval`). Do not invent envelopes.
4. If the policy is also CI-smoke’d at 10 ticks, add dual CI/ladder envelopes
   when ladder ticks ≠ 10 (T3–T5 pattern).
5. Update `production/qa/gauntlet/corpus/index.yaml` + coverage-map; commit on
   the QA branch with `qa(forge): promote …`.

Corpus + skill: [`.claude/skills/qa-gauntlet-forge/SKILL.md`](../../.claude/skills/qa-gauntlet-forge/SKILL.md),
[`production/qa/gauntlet/corpus/`](../../production/qa/gauntlet/corpus/).

## Stage

Program stage remains **Release**. Expect regen is QA discipline, not Launch work.

---

*S95-01 companion — 2026-07-14; forge promote path — 2026-07-23*
