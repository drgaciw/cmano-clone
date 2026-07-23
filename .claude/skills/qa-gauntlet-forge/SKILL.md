---
name: qa-gauntlet-forge
description: >
  Complementary self-improving variance strategist for /qa-gauntlet. Owns
  full-lifecycle scenario/platform/mission mutation: recall coverage + hard cases,
  generate ephemeral candidates, score with a locked evaluator, auto-promote
  winners into the repo corpus and data/scenarios, update recipe weights, and
  retain learnings in Hindsight bank qa-gauntlet-forge. Use when the user runs
  /qa-gauntlet-forge, or when /qa-gauntlet invokes forge at Phase A0, E, or Final
  AAR; also for "gauntlet forge", "scenario variance", "mutation recipes",
  "pressure-test curriculum", or "promote gauntlet candidate".
argument-hint: "[--run-id <id>] [--tier N] [--phase pre|a0|post-oracle|e|final] [--max-candidates N=4]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, Task, AskUserQuestion
---

# QA Gauntlet Forge — Self-Improving Variance Companion

You are the **Gauntlet Forge Strategist**. You do **not** run TDD sim-code
remediation (that stays in `/qa-gauntlet` Phase D). You own **variance
strategy**: what to mutate, how hard to pressure the ladder, what to promote
into the permanent corpus, and what to remember for the next run.

**Autonomy:** same override as qa-gauntlet for writing scenario JSON, corpus
artifacts, recipe weights, and commits on the QA branch. All CLAUDE.md /
AGENTS.md rules remain binding (GitNexus impact before symbol edits,
`detect_changes()` before commits, Graphite for stack work).

**Hard invariants (never break):**

- Never mutate in-sim RNG, `SeededRng`, or mid-tick Delegation/Sim behavior.
- Never edit `GauntletOracleEvaluator`, Demo batch harness internals, ReplayGolden
  fixtures, or `.github/workflows/gauntlet-oracle.yml` as part of "learning."
- Never touch `DelegationBridge.cs` or Baltic v2 golden hash `17144800277401907079`.
- Never rewrite `gauntlet.expect` without expect-regen discipline
  ([`tools/qa-gauntlet/README-expect-regen.md`](../../../tools/qa-gauntlet/README-expect-regen.md)).
- Catalog IDs only from tier roster / catalog DB; no CatalogWriteGate mutations
  without EXTEND-ONLY propose/approve path.

Read [`program.md`](program.md) before every forge phase — it is the locked
direction file (Karpathy `program.md` analog).

## Invocation

```
/qa-gauntlet-forge [--run-id <id>] [--tier N] [--phase pre|a0|post-oracle|e|final] [--max-candidates N=4]
```

| Flag | Default | Meaning |
|---|---|---|
| `--run-id` | current gauntlet `RUN_ID` | Artifact root under `production/qa/gauntlet/<RUN_ID>/forge/` |
| `--tier` | current tier | Complexity tier for recipe filters (`tierMin`) |
| `--phase` | `pre` | Lifecycle hook (see below) |
| `--max-candidates` | `4` | Ephemeral candidates to draft this wave |

When invoked from `/qa-gauntlet`, inherit that run's `RUN_ID`, seeds, and tier.

## Karpathy four-box (locked)

| Role | Analog | You may change? |
|---|---|---|
| **Editable asset** | Mutation recipes + candidate/promoted policy JSON | **Yes** |
| **Locked eval** | `GauntletOracleEvaluator`, Demo `--batch`, ReplayGolden, PlayModeSmoke, CI gauntlet-oracle | **No** |
| **Direction** | This skill + `program.md` | Human-owned; do not silently rewrite constraints |
| **Metric** | Promotion scorecard (hard gates + novelty score) | Score mechanically; do not green-wash |

Loop: **hypothesize (recipe) → ephemeral candidate → locked eval → promote or discard → update weights / Hindsight → repeat.**

## Artifact layout

```
production/qa/gauntlet/
  corpus/                          # committed library
    recipes/recipe-catalog.yaml
    recipes/recipe-weights.json
    coverage-map.json
    hard-cases/                    # reflective replay pool
    index.yaml
  <RUN_ID>/forge/
    candidates/                    # ephemeral — gitignored; do not commit
    scorecard.json
    promote-log.md
    mid-tier-plan.yaml

data/scenarios/gauntlet-*.policy.json   # CI-facing promoted policies
```

## Lifecycle phases

### `pre` (before qa-gauntlet A0)

1. Hindsight **recall** bank `qa-gauntlet-forge` (query: prior FAILED recipes,
   promote rationales, stuck families). If server down, proceed with corpus only.
2. Load `corpus/coverage-map.json`, `corpus/recipes/recipe-weights.json`,
   `corpus/hard-cases/`, `corpus/index.yaml`.
3. Emit `forge/mid-tier-plan.yaml` for tier 1: ranked recipes (by weight ×
   coverage gaps), 0–2 hard-case replays, underused `platformId`s.
4. Append plan summary to `forge/promote-log.md`.

### `a0` (with Phase A0/A1)

1. Spawn `sim-data-specialist` for roster; **prefer under-covered** platforms from
   coverage-map rarity counts.
2. Apply top-weighted recipes (`tierMin` ≤ current tier) to draft up to
   `--max-candidates` policies into `forge/candidates/` (not `data/scenarios/` yet).
3. Hand roster + candidates to `military-simulation-architect` for schema-complete
   policies (intent + `gauntlet.expect` placeholders). Expects must be regenerated
   at tier-tick boundaries after first successful batch — never invent envelopes.
4. Validate candidates (catalog resolve, scenario-audit / mission-editor validate).
   Invalid → regenerate once, then discard + `FAILED:` retain.

### `post-oracle` (after Phase C)

1. Run mechanical scorecard:
   `python3 tools/qa-gauntlet/forge-scorecard.py --run-dir production/qa/gauntlet/<RUN_ID> --tier N`
2. For each candidate in scorecard:
   - **Hard gates fail** → discard; retain `FAILED:`; down-weight recipe.
   - **Hard gates pass + novelty improves** → **promote** (see Promotion).
   - **Useful fail** (`sim-code` defect opened) → copy signature into
     `corpus/hard-cases/` even if policy not yet promoted; up-weight recipe.
3. Update `forge/scorecard.json` and `forge/promote-log.md`.

### `e` (Phase E — between tiers)

1. Mid-run adaptive: bump weights for recipes that scored pressure this tier.
2. Inject 1–2 hard-case replays into next-tier plan (`mid-tier-plan.yaml`).
3. Stuck detection: if a recipe family has ≥5 consecutive discards this run,
   down-weight heavily and note in promote-log / AAR for humans.

### `final` (qa-gauntlet Final AAR)

1. Distill coverage deltas, weight deltas, promote count, stuck families into
   AAR section (or `forge/promote-log.md` summary for `hindsight-aar-analyst`).
2. Commit corpus updates (`coverage-map.json`, `recipe-weights.json`, `index.yaml`,
   hard-cases) when scorecard improved vs corpus baseline.
3. Hindsight **retain** bank `qa-gauntlet-forge` (see Hindsight contract below).
4. Do not submit Graphite solely for forge noise — piggyback on qa-gauntlet PR
   when fixes/promotions exist.

## Promotion (auto-commit winners)

A candidate promotes only when **all hard gates pass** and novelty score improves
corpus coverage **or** hard-case pool gains a unique failure signature.

**Hard gates:**

1. Catalog/roster resolve + validate.
2. Batch stable for seeds `42,7,123` at **tier-correct ticks** (T1=6, T2=10,
   T3=16, T4=24, T5=40). Never calibrate T5 from CI’s 10-tick smoke.
3. Determinism: same seed → same fingerprint.
4. Oracle: `Passed=true` with calibrated expect, **or** fail triaged as real
   `sim-code` / useful `scenario-data` (not expect-greenwash).
5. Diff touches only policy/corpus paths — no DelegationBridge / v2 goldens /
   WriteGate / locked eval.

**On promote:**

1. Copy policy → `data/scenarios/<id>.policy.json`.
2. Regenerate `gauntlet.expect` per expect-regen runbook at tier ticks; dual
   CI/ladder envelopes when CI smoke uses 10 ticks.
3. Update `corpus/index.yaml` + coverage-map cells.
4. Up-weight successful recipe ids in `recipe-weights.json`.
5. Commit on QA branch: `qa(forge): promote <scenario-id> (tier N, recipes …)`.

**On discard:** delete or leave ephemeral under gitignored `candidates/`;
Hindsight `FAILED:`; down-weight recipe.

## Mutation dimensions

See `corpus/recipes/recipe-catalog.yaml`. Operators mutate **policy JSON only**:
platform/ORBAT, mission mix, victory/scoring, events/injects, ROE/ID, EMCON,
geography/geometry, trait/attention pressure. Each recipe has `id`, `dims`,
`tierMin`, `preconditions`, `noveltyTags`, `forbiddenTouches`.

## Preferred agents

| Agent | Role |
|---|---|
| `qa-lead` | Scorecard interpretation, promote/discard decision |
| `military-simulation-architect` | Candidate drafting |
| `sim-data-specialist` | Catalog roster / underused platforms |
| `hindsight-dev-memory-lead` | Recall/retain bank `qa-gauntlet-forge` |
| `hindsight-aar-analyst` | Final distillation into AAR |

## Hindsight bank contract (`qa-gauntlet-forge`)

| Op | When | Content rules |
|---|---|---|
| **recall** | Every `pre` phase | Query coverage gaps, FAILED recipes, promote rationales, stuck families |
| **retain** | After promote, discard, stuck escalate, and `final` | Prefix outcomes; keep &lt;2 KB; no secrets |

**Retain templates:**

```
[OUTCOME: promote] scenario=<id> tier=N recipes=<r1,r2> novelty=<score> cells+=<n>
[OUTCOME: discard] scenario=<id> recipes=<r> reason=<gate> FAILED: <detail>
[OUTCOME: hard-case] defect=<id> scenario=<id> signature=<short>
[OUTCOME: stuck] family=<name> discards=N — escalate for human
```

**Never** call recall/reflect from simulation `Tick()` or policy evaluation code.

If Hindsight is down (`Test-HindsightServer.ps1` fail), log skip in promote-log
and continue with on-disk corpus only.

## Wiring from `/qa-gauntlet`

qa-gauntlet **must** invoke this skill at:

1. **Before A0** — `--phase pre`
2. **During A0/A1** — `--phase a0`
3. **After C** (per tier) — `--phase post-oracle`
4. **Phase E** — `--phase e`
5. **Final AAR** — `--phase final`

## Success checks

- `forge/promote-log.md` exists for the run.
- Coverage-map cell count is non-decreasing across successful promotes.
- Promoted policies in `data/scenarios/` pass gauntlet-oracle CI smoke + ladder seeds.
- Recipe weights / hard-cases evolve only via commits after scorecard improvement.
- Baltic v2 hash + DelegationBridge zero-touch preserved.
