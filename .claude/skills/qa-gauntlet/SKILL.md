---
name: qa-gauntlet
description: >
  Autonomous headless QA loop for Project Aegis: generate scenarios of escalating
  complexity (mission type, platform mix, victory conditions, events, ROE, EMCON)
  grounded in the platform catalog DB, run them through the batch sim harness, and
  remediate every defect via TDD. Runs a fixed 5-tier ladder unattended, commits
  fixes to a QA branch, and delivers a full AAR. Use when the user runs
  /qa-gauntlet, or asks for "QA gauntlet", "escalating complexity QA", "tiered
  scenario stress test", "autonomous sim QA loop", "batch sim defect remediation",
  or "gauntlet AAR".
---

# QA Gauntlet — Autonomous Escalating-Complexity Test Loop

You are the **QA Gauntlet Orchestrator**. Run the full loop unattended. This skill
operates under an explicit user-granted autonomy override of the Collaborative
Design Principle: you MAY write scenario files, tests, and fixes, and commit to the
QA branch without per-change approval. All other CLAUDE.md / AGENTS.md rules remain
binding — especially GitNexus impact analysis before every symbol edit,
`detect_changes()` before every commit, and Graphite (`gt`) for all branch work.

**Autonomy boundary:** if GitNexus impact returns CRITICAL on a symbol a fix must
touch, do NOT edit it. Quarantine the defect (see Phase D), continue the tier with
the remaining scenarios, and surface it prominently in the final report.

## Invocation

```
/qa-gauntlet [--tiers N=5] [--scenarios-per-tier N=4] [--seeds 42,7,123] [--max-fix-attempts 3] [--resume <run-id>]
```

| Flag | Default | Meaning |
|---|---|---|
| `--tiers` | `5` | Number of complexity tiers to run (1–5) |
| `--scenarios-per-tier` | `4` | Scenarios generated per tier |
| `--seeds` | `42,7,123` | Comma-separated seeds; every scenario × every seed |
| `--max-fix-attempts` | `3` | Max TDD remediation cycles per defect |
| `--resume` | _(none)_ | Continue an existing run from its last completed tier |

**Preferred tools for this skill:** Read, Glob, Grep, Write, Edit, Bash, Task, AskUserQuestion.

## Run identity & artifacts

- `RUN_ID = gauntlet-$(date +%Y%m%d-%H%M)`
- Artifact root: `production/qa/gauntlet/<RUN_ID>/`
  - `manifest.yaml` (tier plan + scenario registry), `tier-N/` (roster, scenario JSONs, CSVs, logs),
    `bugs/` (bug reports), `fixes.md` (TDD fix log), `AAR.md` (final report)
- Branch: `gt create -m "qa: gauntlet <RUN_ID>"` off trunk before any edit. All
  commits land here. Never push with raw git; `gt submit --stack --no-interactive`
  only at the very end, and only if at least one fix was committed.
- If `--resume <run-id>` was passed, read that manifest, find the last completed
  tier, and continue from the next phase instead of starting over.

## Phase 0 — Preflight gates (hard stop on failure)

1. `node .gitnexus/run.cjs analyze` if the GitNexus index is stale
   (check `gitnexus://repo/cmano-clone/context`).
2. Baseline: `dotnet test ProjectAegis.sln` must be fully green. Record the test
   count as the monotonic baseline — it may only grow during this run.
3. Replay determinism gate: run the `replay-verify` skill (golden replays must pass,
   e.g. 6/6). If red, the gauntlet is invalid — stop and report.
4. Smoke: run the `smoke-check` skill.
5. Catalog gate: confirm `assets/data/catalog/baltic_patrol.db` opens and its
   migrations are current (see `sqlite-schema-management` skill). Scenario
   generation is catalog-driven; a stale or broken catalog invalidates the run.
6. Write `manifest.yaml` with the tier plan below, resolved arguments, baseline
   numbers, and git SHA.

## The complexity ladder

Generate `--scenarios-per-tier` scenarios per tier (default 4). Every scenario is
run with every seed in `--seeds` (default `42,7,123`). Each dimension escalates
per this matrix — combine dimensions within a tier, don't cherry-pick one:

| Dim | Tier 1 | Tier 2 | Tier 3 | Tier 4 | Tier 5 |
|---|---|---|---|---|---|
| **Mission type** | Single patrol | Strike OR escort | Escort + strike combined | ASW/AAW multi-mission | Multi-domain theater op (patrol+strike+escort+ASW concurrent) |
| **Platform mix** | 1–2 surface units/side | + fixed-wing air | + submarines | + UAV/drone elements | Joint mix incl. drone swarms, both sides asymmetric |
| **Victory conditions** | Survive N ticks | Destroy designated target | Protect HVU + destroy target | Weighted multi-objective scoring | Conditional/dynamic objectives that change on trigger |
| **Events** | None | 1 scripted timed event | Timed event chain | Random injects (seeded) | Cascading adversarial injects (comms loss, sensor degradation, reinforcements) |
| **ROE** | Weapons free, both sides | Weapons tight one side | ID-required engagement criteria | Asymmetric per-side ROE + escalation rules | Mid-mission ROE changes via event |
| **EMCON** | Unrestricted emissions | Passive-only one side | Timed EMCON phases | Dynamic EMCON change on detection | Contested EM: deception emitters + EMCON discipline scored |

Tier N+1 may not start until tier N is green (all scenarios pass all oracles,
after remediation) or explicitly quarantined.

## Per-tier loop

### Phase A — Scenario generation (parallelizable with tier N execution)

**A0 — Platform roster from the catalog.** Before scenario drafting, spawn
`sim-data-specialist` to assemble the tier's platform roster from real data:

- Query the catalog DB `assets/data/catalog/baltic_patrol.db` (schema per
  `src/ProjectAegis.Data/Catalog` and the `sqlite-schema-management` skill) for
  platform, sensor, and weapon IDs matching the tier's platform-mix row —
  including each platform's `CatalogEmcon` emissions profile and archetype bindings.
- For tiers 4–5 (UAV/drone/swarm, asymmetric near-future mixes), draw from
  `data/catalog/near_future_archetypes.json` and
  `data/catalog/speculative_platforms.json`; sensors from
  `data/catalog/sensors_baltic.json`.
- Cross-check plausibility (ranges, speeds, loadouts) against the offline
  reference export in `docs/reference/cmano-db/` where a real-world analog exists.
- Output: `tier-N/roster.json` — the only platform/sensor/weapon IDs the
  architect may reference.

**A1 — Drafting.** Spawn `military-simulation-architect` (Task tool) with the
tier row from the matrix, `tier-N/roster.json`, and the schema of an existing
scenario under `data/scenarios/` as reference. It must produce the scenario JSONs
into `production/qa/gauntlet/<RUN_ID>/tier-N/` — referencing only roster IDs,
with EMCON postures consistent with each platform's `CatalogEmcon` profile —
plus a one-line intent + expected-outcome oracle per scenario (e.g. "Blue wins by
HVU survival; Red fires ≤ X missiles under tight ROE").

**A2 — Validation.** Validate every generated scenario before running:

- **Catalog resolution (oracle 0):** every entity, sensor, and weapon ID in the
  scenario resolves against `tier-N/roster.json` (and therefore the catalog DB).
  Unresolved ID → `scenario-data` defect, regardless of whether the sim tolerates it.
- `pwsh tools/mission-editor/Invoke-ScenarioValidate.ps1 <file>` (or the
  mission-editor CLI equivalent via `dotnet run --project src/ProjectAegis.MissionEditor.Cli`)
- Run the `scenario-audit` skill on the batch.

Invalid scenario → send back to the architect with the validator output, max 2
regeneration attempts, then drop it and log why.

**Parallel development note:** while tier N executes (Phase B), spawn A0/A1 for
tier N+1 concurrently. Scenario generation is data-only and cannot conflict with
code fixes.

### Phase B — Execution

Run the batch harness per scenario × seed:

```bash
dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios <tier scenario ids> --seeds <seeds> --ticks <tier-appropriate> \
  --csv-out production/qa/gauntlet/<RUN_ID>/tier-N/results.csv
```

Capture stdout/stderr to `tier-N/run.log`. Tick budget scales with tier
(suggest 6 / 10 / 16 / 24 / 40 unless the scenario intent dictates otherwise).

### Phase C — Oracle evaluation (hard gate — no stability-only green)

**Required:** every scenario policy MUST include `gauntlet.intent` and machine-checkable
`gauntlet.expect` (fields: `side`, `minKills`, `maxMissilesFired`, `minDenials`,
`maxDenials`, `minScore`, `maxScore`, `requireNonEmptyFingerprint`). Missing expect
is an automatic tier fail.

**Required:** after batch, run the shipped evaluator
`ProjectAegis.Data.Catalog.GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policyJson, resultsCsv)`
via CLI (preferred) or equivalent harness wrapper, and write `tier-N/oracle-eval.json`:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy-dir production/qa/gauntlet/<RUN_ID>/tier-N \
  --csv production/qa/gauntlet/<RUN_ID>/tier-N/results.csv \
  --out production/qa/gauntlet/<RUN_ID>/tier-N/oracle-eval.json
```

A tier is **not** green on stability/fingerprint alone — if the evaluator returns
`Passed=false` (CLI exit 1), the tier fails and defects are opened.

**CI:** PR workflow `.github/workflows/gauntlet-oracle.yml` runs Demo batch + this CLI
(fail-closed). Local dry-run mirrors that job.

**Hindsight re-test:** closed defects live in `production/qa/gauntlet-defect-registry.json`.
Re-run a closed defect after a fix:

```bash
tools/qa-gauntlet/retest-defect.sh <defect-id> --out-dir <scratch>
```

**Multi-domain:** policies with surface+air+sub `gauntlet.units` assign engage agents to
every blue unit; detection observer→target pairs feed preferred victims so concurrent
domain launches are not collapsed by salvo deconfliction.

Spawn `qa-lead` to evaluate `results.csv` + `run.log` + evaluator output against:

1. **Stability** — zero unhandled exceptions, zero crashes, run completed all ticks.
2. **Determinism** — identical `fingerprint` for identical (scenario, seed) across a
   repeat run; different seeds may differ. Any mismatch → also run the
   `determinism-audit` skill and treat as a defect owned by `determinism-engineer`.
3. **Victory-condition correctness** — winner/score matches `gauntlet.expect` bounds
   (enforced by `GauntletOracleEvaluator`).
4. **ROE compliance** — no engagements violating ROE; denials/missiles vs expect
   (enforced where expect bounds apply).
5. **EMCON plausibility** — detection/denial patterns consistent with emissions
   posture *as defined by each platform's catalog `CatalogEmcon` profile*.
6. **Regression** — tier ≤ N-1 anchor scenarios (re-run 1 per prior tier) still pass;
   scores within tolerance of their previous CSVs.
7. **Sanity** — scores finite; fingerprint non-empty when required; CSV schema intact.

**Joint ORBAT (tier ≥3 when available):** policies SHOULD set `gauntlet.units` with
surface + air + subsurface catalog `platformId`s. The harness registers them and
emits `CATALOG_UNIT:{platformId}:{domain}` events (see `gauntlet-joint-orbat-smoke`).
Do not claim multi-domain play without those events in the run fingerprint/fire order.

Every failed oracle becomes a defect entry via the `bug-triage` skill, classified as:
`scenario-data` (bad generated JSON or catalog mismatch), `sim-code`, `oracle`
(our expectation was wrong), or `flaky` (route to `test-flakiness` skill).

### Phase D — TDD remediation (max `--max-fix-attempts` per defect, default 3)

For each `sim-code` defect, run this strict cycle:

1. **Red** — spawn `c-sharp-test-engineer` to write a minimal failing test in the
   owning test assembly (`ProjectAegis.Sim.Tests`, `ProjectAegis.Delegation.Tests`, …)
   that reproduces the defect deterministically (fixed seed). Confirm it fails.
2. **Impact** — `impact({target: <symbol>, direction: "upstream"})` on every symbol
   to be edited. CRITICAL → quarantine (skip fix, tag defect `QUARANTINED-CRITICAL`,
   write bug report via `bug-report` skill, continue). HIGH → proceed but flag in AAR.
3. **Green** — spawn `c-sharp-engineer` (or `determinism-engineer` for oracle-2
   defects) with the failing test, impact summary, and defect report. Minimal fix
   only; no drive-by refactoring.
4. **Verify** — full `dotnet test ProjectAegis.sln` green (count ≥ baseline),
   `replay-verify` still green, re-run the failing scenario × all seeds.
5. **Commit** — `detect_changes()` must show only expected symbols/flows, then
   `gt modify` / commit on the QA branch with message
   `qa(gauntlet): fix <defect-id> — <symbol> (tier N)`. Append entry to `fixes.md`.
6. Attempts exhausted → revert working tree to last commit, quarantine the defect
   and its scenario, continue the tier.

`scenario-data` defects → regenerate via Phase A path (counts against the
scenario's 2 regeneration attempts). `oracle` defects → qa-lead corrects the
expectation in the manifest with justification logged.

**Parallel fixes:** independent defects (disjoint GitNexus blast radii — verify
with `impact` before assigning) may be fixed by parallel subagents in separate
worktrees under `.worktrees/`; merge back through the QA branch sequentially,
re-running step 4 after each merge.

### Phase E — Tier gate

Re-run every previously-failed scenario × seed. Tier is green when all
non-quarantined scenarios pass all oracles. Record the tier summary in the
manifest (pass/fail matrix, defects found/fixed/quarantined, test-count delta).
Then retain the tier's learnings: use the hindsight skills
(`hindsight-retain`) via `hindsight-dev-memory-lead`, and
`balance-tuning-memory-agent` for any score/balance anomalies observed.

## Final phase — AAR & handoff

After tier 5 (or an unrecoverable halt):

1. Spawn `hindsight-aar-analyst` to write `AAR.md`: ladder results table, every
   defect (root cause, fix commit, or quarantine reason), determinism findings,
   balance/score trends across tiers, flaky-test notes, recommended follow-ups.
2. Spawn `qa-lead` for the sign-off section: baseline vs final test count,
   replay-golden status, regression anchors status.
3. `detect_changes({scope: "compare", base_ref: "main"})` — attach output to AAR.
4. `gt submit --stack --no-interactive` if fixes were committed; include AAR link
   in the PR body. Do NOT merge.
5. Print a concise terminal summary: tiers passed, scenarios run, defects
   found/fixed/quarantined, commits, AAR path. Explicitly list every
   `QUARANTINED-CRITICAL` item — these require human decisions.

## Hard rules recap

- Never edit a symbol without `impact` first; never ignore HIGH/CRITICAL silently.
- Never commit without `detect_changes()`.
- Never use raw `git push` / `gh pr create` — Graphite only.
- Test count is monotonic; a fix that deletes tests is invalid.
- Every fix starts from a failing test. No test, no fix.
- Scenarios may only reference platform/sensor/weapon IDs present in the tier
  roster drawn from the catalog DB.
- Budget guard: if a single tier exceeds 12 defects or the run exceeds its
  remediation budget, halt gracefully and jump to the Final phase.
