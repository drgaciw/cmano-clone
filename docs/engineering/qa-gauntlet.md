# QA Gauntlet — engineering runbook

The **QA Gauntlet** is an autonomous, escalating-complexity headless QA loop: it runs
catalog-grounded scenarios through the batch sim harness and gates each one with a
**fail-closed oracle** that checks numeric outcomes *and* order-log evidence. It is how
we get more than "the sim didn't crash" from a run — a tier is green only when the
observed score / kills / missiles / denials fall inside the policy's declared envelope
and the required evidence tokens appear in the fingerprint.

This doc is the developer reference for the *machinery* — the pipeline, the policy
`gauntlet.expect` schema, the CLI verb, the artifact layout, and the CI gate. The
end-to-end *orchestration* (scenario generation, TDD remediation, AAR) is agent-driven
and specified in [`.claude/skills/qa-gauntlet/SKILL.md`](../../.claude/skills/qa-gauntlet/SKILL.md);
this doc is what you read to run or debug the pieces by hand.

> **There is no `GauntletRunner` class.** The loop is a pipeline of existing pieces
> (the Demo batch harness + a `ProjectAegis.Data` evaluator + a Mission Editor CLI verb),
> stitched together by the skill. Everything below is a shipped, testable component.

---

## Pipeline

```
data/scenarios/gauntlet-*.policy.json      # scenario + engage config + gauntlet.expect oracle
        │
        ▼
ProjectAegis.Delegation.Demo  --batch      # BalticBatchRunner → BalticReplayHarness (headless sim)
        │  writes results.csv (LossesScoringCsvExporter schema)
        ▼
ProjectAegis.MissionEditor.Cli  gauntlet_oracle_eval   # GauntletOracleEvaluator (fail-closed)
        │  filters CSV rows to the policy id, checks bounds + fingerprint gates
        ▼
oracle-eval.json   { ok, allPassed, scenarios[] }      # exit 0 iff allPassed
```

A run is **green** only when the oracle returns `allPassed: true` (CLI exit `0`).
Completing all ticks without an exception is *necessary but not sufficient*.

### Components

| Component | File | Responsibility |
|-----------|------|----------------|
| Batch harness | [`BalticBatchRunner.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticBatchRunner.cs) · [`BalticReplayHarness.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs) | Runs each `(scenario, seed)` headless. Registers `gauntlet.units` (surface/air/sub), seeds magazines, emits `CATALOG_UNIT` / `MAGAZINE_SEED` tokens, assigns one engage agent per blue unit. |
| CSV export | [`LossesScoringCsvExporter.cs`](../../src/ProjectAegis.Delegation/Projection/LossesScoringCsvExporter.cs) | Defines the batch CSV schema; the `fingerprint` column is `DecisionLog.ComputeFingerprint()`. |
| Oracle evaluator | [`GauntletOracleEvaluator.cs`](../../src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs) | Fail-closed post-batch check: parses `gauntlet.expect`, parses CSV rows, validates bounds + fingerprint gates. |
| Expect schema | [`GauntletOracleExpect.cs`](../../src/ProjectAegis.Data/Catalog/GauntletOracleExpect.cs) | Record of the machine-checkable expects (null numeric = "no bound"). |
| Result | [`GauntletOracleEvaluationResult.cs`](../../src/ProjectAegis.Data/Catalog/GauntletOracleEvaluationResult.cs) | `(Passed, Failures[])`. |
| Roster validator | [`GauntletRosterValidator.cs`](../../src/ProjectAegis.Data/Catalog/GauntletRosterValidator.cs) | Oracle-0: `detection[]` observer/target IDs and `gauntlet.catalogRefs` must resolve against the tier `roster.json`. |
| CLI verb | [`GauntletOracleEvalCommand.cs`](../../src/ProjectAegis.MissionEditor.Cli/GauntletOracleEvalCommand.cs) | Wraps the evaluator as `gauntlet_oracle_eval`; exit 0 iff every scenario passed. |
| Defect re-test | [`tools/qa-gauntlet/retest-defect.sh`](../../tools/qa-gauntlet/retest-defect.sh) | Re-runs a closed registry defect (batch + oracle + prior-failure-string check). |
| CI gate | [`.github/workflows/gauntlet-oracle.yml`](../../.github/workflows/gauntlet-oracle.yml) | PR job: real Demo batch → oracle (must pass) → fail-closed strip smoke (must fail). |

---

## The `gauntlet.expect` oracle

Every gauntlet policy carries a top-level `gauntlet` block. **`gauntlet.expect` is
required** — a missing block or missing `expect` is an automatic fail
(`"missing gauntlet.expect"`), as is an empty CSV or no matching rows (`"no results rows"`).

```jsonc
"gauntlet": {
  "intent": "human-readable description of the expected play",
  "tier": 3,
  "catalogRefs": [ "k-31-visby-2009", "..." ],   // validated by GauntletRosterValidator
  "units": [                                       // multi-domain ORBAT (optional)
    { "unitId": "jas-39c-gripen-2005", "platformId": "jas-39c-gripen-2005", "domain": "air", "side": "blue" }
  ],
  "expect": {
    "side": "BLUE",                 // row.side must match (case-insensitive); null = any
    "minKills": 1,                  // row.kills  >= min
    "maxMissilesFired": 11,         // row.missilesFired <= max
    "minDenials": 14,               // row.denials >= min
    "maxDenials": 18,               // row.denials <= max
    "minScore": -30,                // row.score  >= min
    "maxScore": 270,                // row.score  <= max
    "requireNonEmptyFingerprint": true,
    "requireFingerprintSubstrings": [ "CommsStateChange", "Degraded" ],
    "requireTrueLaunchedShooters": [ "jas-39c-gripen-2005", "a-19-gotland-2022" ]
  }
}
```

### Fail-closed semantics

Every rule below is evaluated **per CSV row** (one row per `scenario × seed`); any
violation adds a row-prefixed failure like `row[0] scenario=… seed=…: kills 0 < min 1`
and the tier is not green. All numeric fields are optional — **null means "no bound"**.

| Field | Rule | Notes |
|-------|------|-------|
| `side` | `row.side` must equal (case-insensitive) | omit for any side |
| `minKills` | `kills >= minKills` | |
| `maxMissilesFired` | `missilesFired <= maxMissilesFired` | ROE / ammo discipline |
| `minDenials` / `maxDenials` | `denials` within `[min, max]` | policy-denial band |
| `minScore` / `maxScore` | `score` within `[min, max]` | doubles |
| `requireNonEmptyFingerprint` | fingerprint column must be non-blank | **defaults `true`**; set `false` explicitly to opt out |
| `requireFingerprintSubstrings` | each string must appear in the fingerprint | evidence gate (e.g. an inject actually fired) |
| `requireTrueLaunchedShooters` | each unit id must appear as shooter on an `Engagement\|…\|True\|Launched` token | multi-domain concurrent-launch gate |

`requireNonEmptyFingerprint` is the important default: unless the policy sets it to
literal `false`, a blank fingerprint fails. This is what makes "the sim ran but did
nothing" fail closed rather than pass.

### Fingerprint evidence gates

The `fingerprint` column is the canonical order-log text (space-separated tokens). Two
gate types read it beyond the numeric bounds:

- **`requireFingerprintSubstrings`** — literal substrings that must be present. Used for
  *injects*: e.g. a ladder/theater inject policy requires `CommsStateChange`, `Degraded`,
  and its seeded reason string, so a decorative `EventFired` marker alone does not pass.
- **`requireTrueLaunchedShooters`** — unit IDs that must appear as the shooter on a
  `True|Launched` engagement token. The evaluator parses tokens shaped
  `Engagement|seq|t0|t1|shooter|engId|True|Launched` and collects `parts[4]` (the shooter).
  This is the **multi-domain** gate: air + subsurface catalog blues must *actually launch*,
  not merely be detected or registered.

---

## The batch harness CSV

`gauntlet_oracle_eval` reads the CSV produced by `Demo --batch`. The schema is fixed by
`LossesScoringCsvExporter.Header`:

```text
scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
```

- The evaluator maps columns **by header name** (order-tolerant) and coerces numerics
  with invariant culture; unparseable numerics become `0`.
- `fingerprint` is the **last** column and is allowed to contain commas — the CLI/evaluator
  join any cells past the 7th back into the fingerprint. It never contains a raw newline
  (the exporter replaces `\r\n`/`\n`/`\r` with spaces).
- The CLI filters CSV rows to the policy's `id` (matched against `scenarioId`, ordinal)
  before evaluating, so one combined `results.csv` can back a whole tier's per-policy checks.

---

## The complexity ladder

Scenarios escalate across six dimensions (mission type, platform mix, victory conditions,
events, ROE, EMCON) — see the full matrix in the
[skill](../../.claude/skills/qa-gauntlet/SKILL.md). Each tier runs **every scenario × every
seed** (default seeds `42,7,123`) at the tier tick budget, and tier *N+1* may not start
until tier *N* is green.

| Tier | Focus | Ticks | Shipped policies (`data/scenarios/`) |
|------|-------|-------|--------------------------------------|
| 1 | Single patrol, 1–2 surface units | 6 | `gauntlet-t1-patrol-{a,b,c,d}` |
| 2 | Strike or escort + one timed event | 10 | `gauntlet-t2-{escort-a,escort-passive,strike-a,strike-event}` |
| 3 | Combined escort+strike, EMCON phases, ID-ROE, +submarines | 16 | `gauntlet-t3-{emcon-phases,escort-strike,event-chain,id-roe}` |
| 4 | Multi-mission, asymmetric ROE, **seeded random injects** | 24 | `gauntlet-t4-{asymm-roe,multi-mission,random-inject,weighted}` |
| 5 | Theater ops, cascading injects, dynamic objectives, mid-run ROE change | 40 | `gauntlet-t5-{cascade,dynamic-obj,roe-change,theater}` |
| extra | Phase-3 anchors outside the 4×5 grid | 12 | `gauntlet-joint-orbat-smoke`, `gauntlet-multidomain-shooters`, `gauntlet-theater-inject`, `gauntlet-theater-dynamic-victory` |

Tick budgets are **coupled to the expect envelope** — a policy's `expect` bounds are only
valid at the tick count they were regenerated against. The CI job runs a flat 10 ticks as a
cross-tier *smoke*, not as authority for the T3–T5 envelopes (see
[`gauntlet-expect-ci-discipline-2026-07-14.md`](../../production/qa/gauntlet-expect-ci-discipline-2026-07-14.md)).

### Concepts

- **Ladder inject** — a mid-run *real state delta* (comms `Degraded`/`Denied` via `comms[]`,
  or ROE change via `mission.triggers` / `PolicyUpdate`), not just an `EventFired` marker.
  Gated with `requireFingerprintSubstrings`. Canonical: `gauntlet-t4-random-inject`,
  `gauntlet-t5-cascade`, `gauntlet-t5-roe-change`.
- **Multi-domain** — surface + air + subsurface `gauntlet.units` that must engage.
  `BalticReplayHarness` maps each detection `observerId → targetId` (`PreferredHostileByShooter`)
  so concurrent launches at distinct reds survive salvo deconfliction; gated with
  `requireTrueLaunchedShooters`. Canonical: `gauntlet-multidomain-shooters`,
  `gauntlet-t3-emcon-phases`.
- **Theater inject** — Phase-3 "theater op" scenarios combining comms degradation/denial
  with mission-event choreography. Canonical: `gauntlet-theater-inject`,
  `gauntlet-theater-dynamic-victory`.
- **Fingerprint fail-closed** — the property that stripping evidence tokens from the CSV
  while keeping the expects makes the oracle fail (proven by the CI strip smoke, below).

---

## Commands

Run from repo root. (`-c Release` matches CI; omit for a quicker debug build.)

**Batch a tier** — one CSV, all scenarios × seeds at the tier tick budget:

```bash
dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t3-emcon-phases,gauntlet-t3-escort-strike,gauntlet-t3-event-chain,gauntlet-t3-id-roe \
  --seeds 42,7,123 --ticks 16 \
  --csv-out production/qa/gauntlet/<RUN_ID>/tier-3/results.csv
```

**Evaluate the oracle** — a whole directory of `*.policy.json`, or a single policy:

```bash
# Directory: every *.policy.json in the dir, each filtered to its own id
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy-dir production/qa/gauntlet/<RUN_ID>/tier-3 \
  --csv production/qa/gauntlet/<RUN_ID>/tier-3/results.csv \
  --out production/qa/gauntlet/<RUN_ID>/tier-3/oracle-eval.json

# Single policy
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy data/scenarios/gauntlet-t4-random-inject.policy.json \
  --csv /tmp/results.csv --out /tmp/oracle-eval.json
```

`gauntlet_oracle_eval` always prints the JSON summary to stdout; `--out` also writes it.
It requires **exactly one** of `--policy` / `--policy-dir`, plus `--csv`. **Exit `0` iff
every scenario passed; `1` otherwise** (including arg/IO errors) — safe to use directly as a
gate.

**Re-test a closed defect** from the registry (batch + oracle + prior-failure-string check):

```bash
tools/qa-gauntlet/retest-defect.sh GAUNTLET-MD-001 --out-dir /tmp/gauntlet-retest
```

The script reads the defect's `scenarioId` / `seed` / `ticks` / `policyPath` from the
registry, re-runs it, and fails if the recorded `priorFailureMode` string is still present
in the CSV.

**Full autonomous loop** (agent-driven, writes scenarios/fixes to a QA branch):

```
/qa-gauntlet [--tiers 5] [--scenarios-per-tier 4] [--seeds 42,7,123] [--max-fix-attempts 3] [--resume <run-id>]
```

---

## Run artifacts

Each run writes to `production/qa/gauntlet/<RUN_ID>/` where `RUN_ID = gauntlet-YYYYMMDD-HHMM`:

```
manifest.yaml            # run plan: seeds, tiers, per-tier tick map, preflight PASS/FAIL, tier_plan, result summary
ladder-summary.json      # machine-readable pass matrix (per-tier allPassed + rows; plus `extra`)
AAR.md                   # after-action report (ladder table, defects, determinism/balance notes)
fixes.md                 # TDD fix log (empty when no defects)
preflight-*.log          # build / catalog / suite / replay preflight captures
tier-1/ … tier-5/        # per-tier: roster.json, *.policy.json, results.csv, run.log,
tier-extra/              #   oracle-eval.json (+ oracle-eval.stdout)
hindsight-retest/        # closed-defect re-test artifacts
```

- `results.csv` — the batch CSV (schema above). `rows` in `ladder-summary.json` =
  scenarios × seeds (e.g. 4 × 3 = 12).
- `oracle-eval.json` — `{ ok, allPassed, scenarios: [{ scenario, passed, failures[], rows }] }`.
- `roster.json` — the catalog `platformId`s the tier's scenarios are allowed to reference
  (oracle-0 input for `GauntletRosterValidator`).

### Defect registry

Closed defects and watched residuals live in
[`production/qa/gauntlet-defect-registry.json`](../../production/qa/gauntlet-defect-registry.json):

```jsonc
{
  "version": 1,
  "retestTool": "tools/qa-gauntlet/retest-defect.sh",
  "defects": [{
    "id": "GAUNTLET-MD-001", "status": "closed",
    "class": "sim-code",              // sim-code | scenario-data | oracle | flaky
    "scenarioId": "gauntlet-multidomain-shooters", "seed": 42, "ticks": 10,
    "policyPath": "data/scenarios/gauntlet-multidomain-shooters.policy.json",
    "priorFailureMode": "hostile-1",  // string the retest greps the CSV for; still-present ⇒ FAIL
    "fixSummary": "…", "closedUtc": "…"
  }],
  "residuals": [{ "id": "GAUNTLET-RES-…", "status": "watched", "class": "…", "notes": "…" }]
}
```

Residuals are **watched, not fake-closed** — e.g. expect-recalibration drift, T5
discriminative weakness, and the standing rule that any `BalticReplayHarness` touch is
CRITICAL blast radius (GitNexus impact first; keep the suite floor and zero
`DelegationBridge` hotpath edits).

---

## Expect regeneration discipline

`gauntlet.expect` bounds are derived from a **real** batch CSV at the correct tick budget —
never hand-guessed. Regenerate when the tier tick budget changes, the policy intent
changes, a legitimate sim fix moves the numbers, or triage classifies a fail as `oracle`
(wrong envelope). Do **not** loosen bounds to hide an unexplained regression. Full steps:
[`tools/qa-gauntlet/README-expect-regen.md`](../../tools/qa-gauntlet/README-expect-regen.md).

---

## CI gate

[`.github/workflows/gauntlet-oracle.yml`](../../.github/workflows/gauntlet-oracle.yml) runs on
every PR:

1. Release build, then `Demo --batch` over a 9-policy fixture set at `--seeds 42 --ticks 10`.
2. `gauntlet_oracle_eval --policy-dir … --csv …` — asserts `allPassed: true`.
3. **Fail-closed strip smoke** — rewrites the CSV to remove inject `CommsStateChange` tokens
   and flip `True|Launched` → `False|NO_AMMO`, keeps the expects, and asserts the oracle now
   returns `allPassed: false` (non-zero exit). This proves the fingerprint gates actually bite.

> Org GitHub Actions may be billing-gated. When the remote job can't run, the local oracle
> dry-run (commands above) is the proof of record — the workflow is a product gate only when
> Actions execute.

---

## Test filters

Gauntlet behavior is pinned by xUnit tests; run the relevant slice while iterating:

| Slice | Filter (`--filter FullyQualifiedName~…`) |
|-------|------------------------------------------|
| Oracle evaluator | `GauntletOracleEvaluatorTests` (`ProjectAegis.Data.Tests`) |
| Roster validator (oracle-0) | `GauntletRosterValidatorTests` (`ProjectAegis.Data.Tests`) |
| CLI verb contract | `GauntletOracleEvalCommandTests` (`ProjectAegis.MissionEditor.Cli.Tests`) |
| Tier catalog units | `GauntletTier12Catalog`, `GauntletTier35Catalog` |
| Joint ORBAT | `GauntletOrbat` |
| Ladder inject / multi-domain | `LadderInject`, `LadderMultiDomain`, `MultiDomainShooter` |
| Negative (strip ⇒ fail) | `LadderNegativeGate` |
| Theater phase-3 | `TheaterPolicy` |

---

## Common pitfalls

| Symptom | Cause / fix |
|---------|-------------|
| Oracle fails with `"missing gauntlet.expect"` | Policy has no `gauntlet` block or no `expect` object. Both are mandatory. |
| Oracle fails with `"no results rows"` | The CSV had no row whose `scenarioId` equals the policy `id`. Check the policy `id` matches the `--scenarios` id and that the batch actually ran it. |
| `row[…]: empty fingerprint` on a passive scenario | `requireNonEmptyFingerprint` defaults `true`. If the scenario is intentionally inert, set it to literal `false`. |
| Numbers pass locally but fail in CI (or vice-versa) | Tick mismatch. CI uses 10 ticks; the expect envelope is only valid at the tier tick budget it was regenerated at. |
| `requireTrueLaunchedShooters` fails though the unit fired | The shooter id must match the fingerprint token's `parts[4]` exactly (ordinal). Confirm the `gauntlet.units[].unitId`/`platformId` equals the launching entity's id. |
| CSV parses wrong columns | The evaluator maps by header name, so keep the header line; a fingerprint with embedded commas is fine (tail-joined) but a stray newline is not. |
| Editing `BalticReplayHarness` for a fix | CRITICAL blast radius — run GitNexus `impact` first, keep the suite floor, and never touch the `DelegationBridge` hotpath. |

---

## See also

| Topic | Where |
|-------|-------|
| Orchestration loop (phases, TDD remediation, AAR) | [`.claude/skills/qa-gauntlet/SKILL.md`](../../.claude/skills/qa-gauntlet/SKILL.md) |
| Expect regen operator runbook | [`tools/qa-gauntlet/README-expect-regen.md`](../../tools/qa-gauntlet/README-expect-regen.md) |
| Expect / tick discipline + CI contract | [`production/qa/gauntlet-expect-ci-discipline-2026-07-14.md`](../../production/qa/gauntlet-expect-ci-discipline-2026-07-14.md) |
| Batch harness + fingerprint / determinism | [`docs/engineering/determinism-and-replay.md`](determinism-and-replay.md) · [`src/ProjectAegis.Delegation.Demo/README.md`](../../src/ProjectAegis.Delegation.Demo/README.md) |
| Mission Editor CLI (`gauntlet_oracle_eval` verb) | [`mission-editor-cli.md`](mission-editor-cli.md) |
| Scenario / policy authoring | [`scenario-policy-authoring.md`](scenario-policy-authoring.md) |
