# QA Gauntlet saboteur — oracle-sensitivity calibration

The **saboteur** is the QA Gauntlet's meta-test: it proves the oracles actually *catch
bugs* instead of merely proving "the sim didn't crash". It applies a curated catalog of
known-bad code patches (**mutants**) one at a time in a throwaway git worktree, rebuilds,
re-runs a fast oracle subset, and records whether each mutant was **caught** (an oracle
turned red) or **survived** (all oracles stayed green — a named blind spot).

Where [`qa-gauntlet.md`](qa-gauntlet.md) documents the fail-closed oracle *machinery*,
this doc is the runbook for the tool that keeps that machinery honest.

- Tool: [`tools/qa-gauntlet/saboteur.py`](../../tools/qa-gauntlet/saboteur.py)
- Catalog: [`tools/qa-gauntlet/mutants/catalog.yaml`](../../tools/qa-gauntlet/mutants/catalog.yaml) + `mutants/*.patch`
- Design spec: [`docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md`](../superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md)

> **Off-CI by design.** The saboteur rebuilds the solution per mutant in a worktree and is
> slow (`BUILD_TIMEOUT_S = 600`, `RUN_TIMEOUT_S = 900` each). It is a calibration tool run
> on demand, not a PR gate. It never commits anything from a worktree.

---

## Mental model

```
catalog.yaml (mutant list)
      │  for each selected mutant:
      ▼
git worktree add --detach .worktrees/saboteur-<id>   # isolated checkout of HEAD
      │
git apply mutants/<id>.patch                          # inject the known-bad change
      │
dotnet build …                                        # build failure ⇒ invalid-mutant
      │
run the oracle subset for this mutant's path          # classic OR swarm (see below)
      │
outcome = caught (an oracle fired) | survived (none fired)
      │
git worktree remove --force                           # nothing is ever committed
```

Because worktrees are created **`--detach` from HEAD**, any uncommitted change in a
calibration-relevant path would silently *not* be measured. The tool therefore refuses to
run when such paths are dirty (see [Preconditions](#preconditions--guardrails)).

---

## Two execution paths

Each mutant auto-routes based on its `expectedOracles`; `--swarm-filter` forces the swarm
path and (without `--mutants`) restricts selection to the swarm family.

| Path | When | Build | Kill rule (`caught`) |
|------|------|-------|----------------------|
| **classic** | mutant does *not* declare `swarm_unit` | `dotnet build ProjectAegis.sln` | ladder subset driver exit ≠ 0 **OR** `ReplayGolden` filter fails |
| **swarm** | `expectedOracles` includes `swarm_unit`, or `--swarm-filter` | `dotnet build src/ProjectAegis.Sim.Tests` | `dotnet test --filter FullyQualifiedName~Swarm` exit ≠ 0 |

- Classic subset runs `run-gauntlet.sh --tiers "1 3 5" --roving 0` (all three anchor seeds
  — some required tokens such as `NO_AMMO` only appear at seeds 7/123, so a seed-42-only
  subset would red `token_coverage` at baseline and poison the measurement) plus the
  Baltic `ReplayGolden` filter in `ProjectAegis.Delegation.UnityAdapter.Tests`.
- A **build failure** on either path is recorded as `invalid-mutant` (never `survived`).

### Oracle tokens

The `firedOracles` column mixes real gauntlet oracles with two synthetic markers the
saboteur adds itself:

| Token | Source |
|-------|--------|
| `sanity`, `goldens`, `victory_roe`, `token_coverage` | real oracles read from each tier's `verdict.json` (see [`evaluate_run.py`](../../tools/qa-gauntlet/evaluate_run.py)) |
| `replay_golden` | synthetic — added when the classic-path `ReplayGolden` filter fails |
| `swarm_unit` | synthetic — added when the swarm-path `Sim.Tests` Swarm filter fails |

---

## Roles and the exit contract

Every catalog entry declares a `role`; the kill rate and the process exit code are
driven by it.

| Role | Meaning | Must be |
|------|---------|---------|
| `control` | a no-op change (e.g. comment-only) | **survived** — proves oracles don't flag noise |
| `defect` | a real, catchable regression | **caught** — a survivor is a blind spot |
| `expected-miss` | a real bug the oracles cannot yet catch | **survived** — tracked, not a hard fail |

**Kill rate** = `caught_defects / (caught_defects + survived_defects)`. Controls and
expected-misses are excluded from both numerator and denominator.

The run exits **non-zero** (calibration failure) on any of:

| Outcome ↓ / Role → | `control` | `expected-miss` | `defect` |
|--------------------|-----------|-----------------|----------|
| survived | OK | OK | **FAIL** (blind spot) |
| caught | **FAIL** (false positive) | **FAIL** (promote to `defect`) | OK |
| invalid-mutant | **FAIL** | **FAIL** | **FAIL** |

Any `invalid-mutant` (build failed) fails the whole run regardless of role.

---

## Mutant catalog schema

Each entry in `mutants/catalog.yaml` requires **all** of these keys (missing keys or an
unknown `role` raise a load error before any build runs):

```yaml
- id: "03-salvo-off-by-one"          # matches worktree name & out-dir subfolder
  patch: "03-salvo-off-by-one.patch" # applied with `git apply`, relative to catalog dir
  target: "src/ProjectAegis.Sim/Policy/PolicyEvaluator.cs"   # informational; must not be a locked-eval file
  description: "Salvo WRA comparison > become >= (off-by-one deny)"
  role: defect                       # control | expected-miss | defect
  expectedOracles: ["goldens", "victory_roe"]  # [] for controls; ["swarm_unit"] routes to swarm path
  impactRecorded: "CRITICAL (2026-07-28; …; worktree-only mutant)"  # one-time GitNexus impact() note
```

Load-time validation ([`load_catalog`](../../tools/qa-gauntlet/saboteur.py)) enforces:

- the `patch` file exists next to the catalog;
- `role` is one of `control | expected-miss | defect`;
- `target` never references a **locked-eval file** — mutating the evaluator itself would
  invalidate the measurement:
  - `src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs`
  - `src/ProjectAegis.Delegation.Demo/Program.cs`
  - `src/ProjectAegis.Delegation.UnityAdapter/Baltic/DelegationBridge.cs`

---

## Usage

```bash
pip install -r tools/qa-gauntlet/requirements.txt   # PyYAML

# Full catalog — each mutant auto-routes by expectedOracles
python3 tools/qa-gauntlet/saboteur.py

# Swarm family only, forced pure-Sim unit path (SWARM-* mutants 10/11/12/14/15/17)
python3 tools/qa-gauntlet/saboteur.py --swarm-filter

# Explicit subset (path still auto-routes unless --swarm-filter is also set)
python3 tools/qa-gauntlet/saboteur.py --mutants 02-roe-tight-inverted,03-salvo-off-by-one

# Keep worktrees for post-mortem of a survivor / invalid-mutant
python3 tools/qa-gauntlet/saboteur.py --mutants 05-contact-lifecycle-skip --keep-worktrees
```

| Flag | Default | Purpose |
|------|---------|---------|
| `--catalog` | `tools/qa-gauntlet/mutants/catalog.yaml` | mutant catalog to load |
| `--out-dir` | `production/qa/gauntlet/calibration-<today>` | report + per-mutant log root |
| `--mutants` | *(all)* | comma-separated ids to run (explicit selection wins) |
| `--swarm-filter` | off | force the swarm unit path; select swarm family when `--mutants` is unset |
| `--keep-worktrees` | off | do not remove `.worktrees/saboteur-<id>` after each run |

### Preconditions & guardrails

- **Clean tree in calibration paths.** Exits `2` if `git status` shows uncommitted tracked
  changes under `src/`, `data/`, `tools/qa-gauntlet/`, `ProjectAegis.sln`, or `global.json`
  — worktrees build from HEAD, so those edits would not be calibrated. Docs/other paths do
  not block. **Commit your change first, then calibrate it.**
- **`dotnet` on PATH** (or `~/.dotnet/dotnet`); exits `3` if missing.
- Never mutate a locked-eval file (rejected at catalog load).

---

## Artifacts & interpreting results

Written under `--out-dir`:

| File | Contents |
|------|----------|
| `report.json` | `{ summary, results[] }` — machine-readable kill rate + per-mutant outcomes |
| `report.md` | human table (Mutant · Role · Outcome · Fired oracles · Expected) + kill-rate header |
| `<id>/build.log`, `<id>/subset.log`, `<id>/replay.log`, `<id>/swarm.log` | per-mutant tool logs |

`summary` reports `caught`, `survived`, `invalid`, `caughtDefects`, `survivedDefects`, and
`killRate` (as `caught/denom`). The process prints `kill rate <x/y> — report: …/report.md`.

Reading the outcomes:

- **`defect` SURVIVED** → a named oracle blind spot. File one bug per survivor
  (e.g. [`production/qa/bugs/`](../../production/qa/bugs)) and tighten the oracle or add
  the missing pin scenario, then re-run to confirm the mutant is now caught.
- **`control` CAUGHT** → a false positive: a no-op turned an oracle red. Fix the oracle's
  over-sensitivity before trusting the suite.
- **`expected-miss` CAUGHT** → good news; promote its `role` to `defect` so future
  regressions of that class hard-fail.
- **`invalid-mutant`** → the patch no longer applies/compiles against HEAD (code drifted).
  Refresh the `.patch` so it targets current source.

---

## Adding a mutant

1. Write the known-bad change, capture it as a patch under `mutants/`
   (`git diff > tools/qa-gauntlet/mutants/NN-name.patch`), and revert your working tree.
2. Run GitNexus `impact()` on the target symbol and record the verdict in `impactRecorded`
   (per [AGENTS.md](../../AGENTS.md) — impact analysis before touching a symbol).
3. Add a catalog entry with the correct `role` and `expectedOracles`. Choose `defect` only
   if an oracle *should* catch it today; otherwise `expected-miss` (tracked) until a pin or
   oracle exists — see the logistics Bingo/Winchester pins in
   [`gauntlet-logistics-variables.md`](gauntlet-logistics-variables.md).
4. Do **not** target a locked-eval file. For a Swarm-controller mutant, set
   `expectedOracles: ["swarm_unit"]` so it auto-routes to the fast unit path.
5. Run `python3 tools/qa-gauntlet/saboteur.py --mutants NN-name` and confirm the outcome
   matches the intended role.

---

## See also

| Topic | Where |
|-------|-------|
| Oracle machinery, `gauntlet.expect`, ladder, CI gate | [`qa-gauntlet.md`](qa-gauntlet.md) |
| Stress axes (weapons / EW / logistics) + production proof gate | [`tools/qa-gauntlet/README-stress-axes.md`](../../tools/qa-gauntlet/README-stress-axes.md) |
| Logistics Bingo/Winchester pins that back mutants 08/09 | [`gauntlet-logistics-variables.md`](gauntlet-logistics-variables.md) |
| Effectiveness design (roles, kill rate rationale) | [`docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md`](../superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md) |
| Orchestration loop (phases, TDD remediation, AAR) | [`.claude/skills/qa-gauntlet/SKILL.md`](../../.claude/skills/qa-gauntlet/SKILL.md) |
