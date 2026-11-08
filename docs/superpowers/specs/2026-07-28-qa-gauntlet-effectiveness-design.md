# QA Gauntlet Effectiveness — Design

**Date:** 2026-07-28
**Status:** Approved (user, 2026-07-28 20:37 CDT)
**Origin:** Brainstorm following runs `gauntlet-20260728-2000` / `gauntlet-20260728-2016`
**Related:** `docs/superpowers/plans/2026-07-27-gauntlet-variability.md` (owned separately — see Non-goals);
`production/qa/gauntlet/gauntlet-20260728-2000/bugs/BUG-gauntlet-emcon-dimension-not-exercised.md`

## Problem

The QA Gauntlet's effectiveness — probability of catching a real defect per run — is limited by
five failure modes, each observed directly in the 2026-07-28 runs:

| # | Failure mode | Evidence |
|---|---|---|
| a | Zero new information per run: 22 static scenarios × 3 static seeds × deterministic sim | Two same-SHA runs byte-identical; second run information-free by construction |
| b | Oracle sensitivity unproven: nothing verifies the oracles go red when the sim breaks; expect-recalibration only ever widens envelopes | The one defect found on 2026-07-28 was found by ad-hoc analysis outside the scripted oracles |
| c | Claimed coverage ≠ actual coverage: no vacuity detection; unknown policy keys silently dropped | EMCON dimension inert for months; `gauntlet.emcon` discarded by `System.Text.Json`; 0 `EMCON_OFF` tokens ladder-wide |
| d | Oracles live in prose: each orchestrator hand-rolls drivers, so rigor varies by session | 2026-07-20 run had no determinism-repeat and no cross-run diff; both were ad-hoc additions on 2026-07-28 |
| e | Envelopes are weak oracles: bounds like score 50–90 from 3 seeds; a 70→55 regression passes | `gauntlet-t1-patrol-a` expect: minScore 50, maxScore 90, while the sim is byte-deterministic and exact anchors are free |

## Solution overview

Two deliverables in one spec (user decision: A + B together; saboteur as a separate on-demand skill):

- **Package B — Oracles as code.** A canonical, shipped ladder driver plus a Python oracle
  aggregator that mechanically evaluates every oracle and emits one machine verdict per tier.
  Includes exact golden-fingerprint anchors, a token-coverage oracle, roving seeds, and a
  strict-key policy validation added to the C# CLI validator.
- **Package A — Saboteur calibration.** A new `/qa-gauntlet-calibrate` skill: apply curated
  known-fault patches in disposable git worktrees, run the anchor ladder, and report which
  oracles caught each mutant (kill-rate matrix). Directly measures P(detect | defect).

### File map

```
tools/qa-gauntlet/
├── run-gauntlet.sh          # canonical ladder driver (replaces per-run hand-rolled scripts)
├── evaluate_run.py          # oracle aggregator — all oracles as code, one JSON verdict
├── expected-tokens.json     # token-coverage oracle: expected fingerprint token types
├── test_evaluate_run.py     # pytest (mirrors forge_scorecard.py precedent)
├── saboteur.py              # mutation/calibration runner (worktree-isolated)
├── test_saboteur.py         # pytest
├── mutants/
│   ├── catalog.yaml         # mutant registry: target, patch, expected catching oracles, impact() record
│   └── NN-<slug>.patch      # 8 curated fault patches (see Package A)
└── goldens/
    ├── anchors.json         # per-(scenario, anchor-seed) fingerprint SHA-256
    └── README.md            # blessed-update runbook

src/ProjectAegis.MissionEditor.Cli/          # strict-key gauntlet.* validation (small addition
                                             # to the existing validate path; exact file per impact())
src/ProjectAegis.MissionEditor.Cli.Tests/    # unit tests for it (suite count grows)

.claude/skills/qa-gauntlet/SKILL.md          # updated: Phases B/C invoke the canonical driver
.claude/skills/qa-gauntlet-calibrate/SKILL.md # new on-demand skill
```

### Data flow

`/qa-gauntlet` → `run-gauntlet.sh` (per tier: stage policies → batch → repeat batch) →
`evaluate_run.py` (verdict JSON per tier + run-level report) → orchestrator triages/remediates only.

`/qa-gauntlet-calibrate` → `saboteur.py` (per mutant: worktree → `git apply` → build → anchor
subset → record fired oracles → remove worktree) → kill-rate matrix report.

## Package B — Oracles as code

### `run-gauntlet.sh`

- Parameters: `--run-id`, `--tiers`, `--seeds`, `--roving N`, per-tier ticks (defaults
  T1=6 T2=10 T3=16 T4=24 T5=40, plus `tier-extra` at 12).
- Resolves `dotnet`: `PATH` → `~/.dotnet/dotnet` → fail with a clear message
  (fixes the documented-commands-fail-verbatim defect).
- Per tier: copy the tier's policies into `tier-N/` (so `gauntlet_oracle_eval --policy-dir`
  sees exactly that tier), run the Demo batch to `results.csv`, run the identical batch again
  to `results-repeat.csv`, then invoke `evaluate_run.py`.
- Artifact layout identical to runs `gauntlet-20260728-*` (`tier-N/results.csv`,
  `results-repeat.csv`, `run.log`, `run-repeat.log`, `verdict.json`), so cross-run tooling
  keeps working.

### `evaluate_run.py` — the oracle aggregator

One command, one machine verdict. Per tier:

| # | Oracle | Mechanism | Status vs today |
|---|---|---|---|
| 1 | Stability | Exception/error scan of run logs; row count == scenarios × seeds (incl. roving) | Scripted (was prose) |
| 2 | Determinism | Sorted diff `results.csv` vs `results-repeat.csv`; any delta = red + diff artifact | Scripted (was ad-hoc) |
| 3–4 | Victory / ROE | Shell out to existing `gauntlet_oracle_eval` CLI; locked `GauntletOracleEvaluator` stays authoritative | Unchanged |
| 5 | Token coverage | Fingerprint token-type histogram vs `data/glossary/abort_reason_manifest.json` + an expected-token list; a token expected somewhere ladder-wide but seen 0 times = red | New — catches vacuity generically (would have caught the EMCON hole) |
| 6 | Regression | Exact golden anchors: SHA-256 of each (scenario, anchor-seed) fingerprint vs `goldens/anchors.json`; any mismatch = red | New — strictly stronger than envelopes for anchors |
| 7 | Sanity + seed-sensitivity | Scores finite; fingerprints non-empty where required; N seeds → N distinct fingerprints per scenario | Scripted (was ad-hoc) |

- Output: `tier-N/verdict.json` — per oracle `{status, evidence}` — plus a run-level
  `verdict.json`. Non-zero exit on any red, making the skill's tier gate mechanical.
- Token-coverage expected-token list ships as a data file, `tools/qa-gauntlet/expected-tokens.json` (seeded
  from what the 2026-07-28 ladder actually emits, plus `EMCON_OFF` marked *expected-after* the
  variability plan's EMCON retrofits land — until then it reports as a **known-vacuous warning**,
  not a red, to avoid failing on the already-filed OPEN defect).

### Golden anchors

- Anchor seeds remain `42, 7, 123`. `goldens/anchors.json` stores SHA-256 of the full
  fingerprint per (scenario, anchor seed) — initially blessed from the byte-identical
  2026-07-28 runs.
- Blessed-update path: `evaluate_run.py --bless --run-dir <dir>` rewrites hashes from a run's
  CSVs; the runbook requires stating *why* behavior legitimately changed (same discipline as
  ReplayGolden / expect-regen). Never bless to silence an unexplained mismatch.
- Envelope `expect` bounds in the policies are untouched; they now primarily guard roving seeds.

### Roving seeds

- `--roving N` (default 2) derives N extra seeds deterministically from the run id
  (e.g. first 8 hex digits of SHA-256 of `"<run-id>:<k>"`), records them in the manifest, and
  appends them to every scenario's seed list.
- Roving rows are judged by oracles 1, 2, 3–4 (envelopes), and 7 — everything except
  oracle 6 goldens, which require a stored baseline. (Oracle 2 applies because the repeat
  batch covers all seeds, roving included.)
- Every run explores new trajectories at near-zero cost; any roving failure is reproducible
  because the seed derivation is recorded.

### Strict-key policy validation (C#)

- The MissionEditor CLI validate path gains a check: every key under `gauntlet.*` (and
  `gauntlet.expect.*`, `gauntlet.units[].*`) must be a declared DTO property. Unknown key →
  validation **error** naming the key and its nearest valid sibling
  (e.g. `gauntlet.emcon` → "unknown key; did you mean top-level `emcon`?").
- Implementation: a `JsonDocument` key-walk against a whitelist derived from the DTO
  properties, applied in the CLI validate path only. (Not `JsonUnmappedMemberHandling.Disallow`
  on the DTOs — that would change deserialization behavior for every DTO consumer, including
  the engine's scenario loader.) No behavior change for valid policies.
- Unit tests in `ProjectAegis.MissionEditor.Cli.Tests` (suite count grows; monotonic rule held).
- GitNexus `impact()` required before the edit, per CLAUDE.md; the validate path is not on the
  locked-eval list.

## Package A — `/qa-gauntlet-calibrate` (saboteur)

### Mutant catalog

`mutants/catalog.yaml` + 8 curated patches spanning the oracle families:

| # | Fault (illustrative) | Expected catching oracle(s) |
|---|---|---|
| 1 | Pd math weakened (e.g. pkBase effect lowered) | 6 goldens; 3 envelopes |
| 2 | Doctrine ROE gate inverted | 3–4; 6 |
| 3 | Salvo deconfliction off-by-one | 3–4; 6 |
| 4 | RNG reseed dropped (determinism break) | 2; ReplayGolden |
| 5 | Contact-lifecycle state transition skipped | 5 token coverage; 6 |
| 6 | EMCON engage-gate bypass | 5 (once EMCON tokens expected); 6 |
| 7 | Score-weight constant nudged | 3 envelopes; 6 |
| 8 | Magazine decrement dropped | 3–4; 6 |

Rules: no mutant touches a locked-eval file; exact target files/symbols chosen at
catalog-authoring time with a one-time GitNexus `impact()` recorded per entry in
`catalog.yaml`; patches are committed but only ever *applied* inside disposable worktrees.

### `saboteur.py`

Per mutant: `git worktree add` (detached at HEAD, temp dir) → `git apply` patch → build →
run a fixed anchor subset (tier-1 + tier-3 + tier-5 anchor scenarios × seed 42; full
calibration ≤ ~15 min) → run `evaluate_run.py` + the `ReplayGolden` test filter → record
which oracles fired → `git worktree remove`. A `trap` cleans up worktrees on abort.
Nothing is ever committed from a worktree.

Preconditions (refuse to start): dirty working tree; baseline ladder not green at HEAD
(cannot calibrate oracles against a broken baseline).

### Output — kill-rate matrix

`production/qa/gauntlet/calibration-<date>/report.md` + `report.json`: mutants × oracles,
each cell caught/missed. Headline: kill rate (target 8/8). Any surviving mutant = a named
oracle blind spot → file a bug via the `bug-report` skill. The `/qa-gauntlet` AAR template
gains one line: "Last calibration: <date>, kill rate N/M".

### `/qa-gauntlet-calibrate` SKILL.md (new, small)

When to run: after oracle/expect/golden changes, after sim refactors, monthly. Invokes
`saboteur.py`, interprets the matrix, files bugs for survivors. Explicitly *not* part of
every `/qa-gauntlet` run (user decision: separate skill).

## Skill-file updates

- `/qa-gauntlet` SKILL.md: Phases B/C rewritten to invoke `run-gauntlet.sh` +
  `evaluate_run.py`; the prose seven-oracle list becomes a reference to `verdict.json`
  fields with triage guidance. Phase E tier gate keys off the aggregator's exit code.
  AAR template gains the calibration line.
- Autonomy, TDD remediation (Phase D), Graphite, GitNexus, and budget-guard rules unchanged.

## Error handling

- Driver and aggregator fail loud, leave all artifacts in place, and never mask a red.
- Saboteur always removes its worktrees (trap on EXIT/INT), never commits, and aborts the
  whole calibration if a build exceeds a per-mutant timeout.
- `--bless` refuses to run when the source run has any non-golden oracle red.

## Testing

- pytest for `evaluate_run.py` and `saboteur.py` (fixture CSVs, synthetic fingerprints,
  a no-op trivial mutant patch), following the existing `test_forge_scorecard.py` pattern.
- xunit for the strict-key validator (valid policy unchanged; `gauntlet.emcon` fails with
  the suggestion; unknown `expect` key fails).
- Acceptance evidence: one end-to-end `/qa-gauntlet-calibrate` dry run whose report shows a
  deliberately trivial mutant being caught.

## Rollout order

1. Strict-key validator (+ tests) — root-cause kill, independent.
2. `run-gauntlet.sh` + `evaluate_run.py` (+ tests) — next gauntlet run uses them.
3. Bless goldens from the byte-identical 2026-07-28 runs.
4. Roving seeds.
5. `saboteur.py` + mutant catalog (+ tests) + first calibration run.
6. Skill-file updates last (they document what now exists).

## Non-goals / constraints

- **No engine changes.** Making the sensor-side EMCON gate observable (silent `continue` in
  `DeterministicDetectionLoop.RollTick` / `ScenarioContactSimulator.Tick`) is flagged as
  valuable future engine work, out of scope here.
- **No overlap with the variability plan.** Corpus expansion, `dimensionsClaimed`,
  `verify_dimension_coverage.py`, and the EMCON scenario retrofits remain owned by
  `docs/superpowers/plans/2026-07-27-gauntlet-variability.md`. This design's token-coverage
  oracle is run-level and generic; the plan's dimension verification is per-scenario — they
  complement, and the expected-token list here gets updated when that plan lands.
- **Locked-eval files untouched:** `GauntletOracleEvaluator.cs`, Demo `Program.cs` batch
  internals, ReplayGolden fixtures, `DelegationBridge.cs`, Baltic golden hash, and
  `.github/workflows/gauntlet-oracle.yml` are neither edited nor targeted by mutants.
- **Repo rules apply:** Graphite for all branch work; GitNexus `impact()` before the C#
  edit and per catalog entry; `detect_changes()` before every commit; test count monotonic.
