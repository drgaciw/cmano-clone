# QA Gauntlet effectiveness tooling — ladder driver, saboteur, stress axes & forge scorecard

> **Scope.** This page documents the Python tooling under
> [`tools/qa-gauntlet/`](../../tools/qa-gauntlet/) that drives and *measures the effectiveness of* the
> QA Gauntlet. It is the operational companion to [`qa-gauntlet.md`](qa-gauntlet.md) (the batch → oracle
> *loop* and the `gauntlet.expect` schema) and [`gauntlet-oracle-baseline.md`](gauntlet-oracle-baseline.md)
> (the pinned oracle baselines). Where `qa-gauntlet.md` explains *how a scenario is gated*, this page
> explains the surrounding harness:
>
> - the **ladder driver** (`run-gauntlet.sh` + `ladder.yaml` + `evaluate_run.py`) that runs the tiers and
>   aggregates verdicts,
> - the **saboteur** (`saboteur.py` + `mutants/`) that calibrates oracle sensitivity by measuring how many
>   deliberately-broken builds the gauntlet actually *catches*,
> - the **stress axes** (`stress_axes.py` / `apply_stress_axes.py` / `plan_stress_matrix.py` + the
>   `verify_stress_axes.py` proof gate) that layer orthogonal pressure on any tier and refuse to *claim*
>   pressure they cannot mechanically *prove*, and
> - the **forge scorecard** (`forge_scorecard.py`) that mechanically scores Forge promotion candidates.
>
> This is **QA/ops tooling**, not sim code — it drives the headless sim from the outside and does
> **not** edit `DelegationBridge`, CatalogWriteGate write paths, or the Baltic v2 replay goldens.
> Scoped exceptions: `evaluate_run.py bless` rewrites `tools/qa-gauntlet/goldens/anchors.json`;
> `apply_stress_axes.py` derives **ephemeral** stressed policies (not committed); the saboteur
> patches throwaway worktrees only. The end-to-end agent orchestration lives in the
> [`qa-gauntlet-*` skills](../../.claude/skills/); this page is what you read to run or debug the pieces by
> hand. Full mechanics for two sub-areas already have tool-local runbooks — this page links, not
> duplicates, [`README-stress-axes.md`](../../tools/qa-gauntlet/README-stress-axes.md) and
> [`README-expect-regen.md`](../../tools/qa-gauntlet/README-expect-regen.md).

Design spec: [`docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md`](../../docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md).

---

## Tool map

```
tools/qa-gauntlet/
  run-gauntlet.sh          # canonical ladder driver — "oracles as code"
  ladder.yaml              # tier → {ticks, scenarios} manifest + defaultAnchorSeeds
  evaluate_run.py          # oracle aggregator (tier | run | bless | filter-seeds | ladder)
  goldens/                 # anchors.json blessed baselines

  saboteur.py              # oracle-sensitivity calibration via curated mutants (kill rate)
  mutants/                 # catalog.yaml + NN-*.patch curated defects/controls

  stress_axes.py           # axis catalog loader + proof-mode validator
  apply_stress_axes.py     # pure derive: (base policy, axis selection) → derived policy
  plan_stress_matrix.py    # bounded pairwise (tier × axis) work list
  verify_stress_axes.py    # per-axis proof check  (aka gate_stress_proof.py / run-stress-proof-gate.sh)
  README-stress-axes.md    # stress-axis mechanics runbook (DRG-63/65)

  forge_scorecard.py       # mechanical Forge promotion scorecard (aka forge-scorecard.py)
  README-expect-regen.md   # gauntlet.expect regen discipline (S95-01)
  retest-defect.sh         # re-run a closed defect-registry entry

  test_*.py                # co-located pytest for every tool above
  requirements.txt         # pinned test/runtime deps
```

Everything is **deterministic and side-effect-scoped**: derivations are pure functions of their inputs,
the saboteur works only inside disposable git worktrees (never commits), and the gates communicate purely
through exit codes + JSON reports.

---

## Ladder driver — `run-gauntlet.sh` + `ladder.yaml`

`run-gauntlet.sh` is the canonical driver. It resolves `dotnet` (PATH → `~/.dotnet` → fail loud), runs the
requested tiers through the [Demo batch harness](qa-gauntlet.md), and aggregates verdicts via
`evaluate_run.py`:

```bash
tools/qa-gauntlet/run-gauntlet.sh --run-id <id> \
  [--tiers "1 2 3 4 5 extra"] [--seeds 42,7,123] [--roving 2] \
  [--out-root production/qa/gauntlet] [--stress-proof-evidence PATH]
```

[`ladder.yaml`](../../tools/qa-gauntlet/ladder.yaml) is the single source of truth for what each tier runs
— its `ticks` budget and its `scenarios` list — plus `defaultAnchorSeeds: [42, 7, 123]`. The tier tick
budgets are **T1=6, T2=10, T3=16, T4=24, T5=40** (mirrored in `evaluate_run.py` and the expect-regen
runbook). All three anchor seeds matter: some required evidence tokens (e.g. `NO_AMMO`) only occur at
seeds 7/123, so a seed-42-only subset would under-report coverage.

[`evaluate_run.py`](../../tools/qa-gauntlet/evaluate_run.py) is the **oracle aggregator — "all ladder
oracles as code"**. Modes:

| Mode | Does |
|------|------|
| `tier` | evaluate one tier dir (stability, determinism, victory/ROE via `oracle-eval.json`, goldens, sanity) → `tier-N/verdict.json` |
| `run` | aggregate tier verdicts + run-wide token coverage → `verdict.json` |
| `bless` | rewrite `goldens/anchors.json` from a green run's CSVs |
| `filter-seeds` | keep CSV rows whose seed is in the allow-list |
| `ladder` | print scenarios (csv) or ticks for a tier from `ladder.yaml` |

Exit is **0 iff no oracle failed** (warnings never fail). The per-scenario numeric envelope + evidence
tokens themselves are the `gauntlet.expect` schema documented in [`qa-gauntlet.md`](qa-gauntlet.md).

---

## Saboteur — does the gauntlet actually *catch* bugs?

A green ladder proves scenarios pass; it does **not** prove the oracle would notice a regression.
[`saboteur.py`](../../tools/qa-gauntlet/saboteur.py) closes that gap by applying curated **mutants**
(catalog patches in [`mutants/`](../../tools/qa-gauntlet/mutants/)) — each in a **disposable git
worktree** — building, and checking whether the gauntlet turns red.

- **Catalog `role` (required):** `control` | `expected-miss` | `defect`.
- **Kill rule (classic path):** caught = anchor-subset driver exit ≠ 0 **or** the `ReplayGolden` filter
  fails. **Swarm path:** caught = `dotnet test --filter FullyQualifiedName~Swarm` exit ≠ 0 (fires the
  `swarm_unit` oracle token). A build failure is an **invalid mutant**.
- **Kill rate** = `caught_defects / (caught_defects + survived_defects)` — `control` and `expected-miss`
  rows are excluded from both numerator and denominator, so the metric measures only real defects.
- **Path selection:** a mutant whose `expectedOracles` includes `swarm_unit` always uses the Swarm path;
  `--swarm-filter` (without `--mutants`) restricts the catalog to that family and forces the Swarm path.
- **Nothing is ever committed** from a worktree; exit contracts are role-driven (`exit_code_for`).

The curated defects are exactly the failure modes the oracle must never miss — e.g.
`01-pd-weakened`, `02-roe-tight-inverted`, `03-salvo-off-by-one`, `04-rng-seed-ignored`,
`05-contact-lifecycle-skip`, `06-emcon-engage-bypass`, `07-magazine-not-decremented`,
`08-bingo-gate-bypass`, `09-winchester-gate-bypass` — plus a `00-noop-comment` control that must
**survive** (a caught no-op would mean the harness is flapping).

```bash
python3 tools/qa-gauntlet/saboteur.py --out-dir /tmp/saboteur          # full catalog
python3 tools/qa-gauntlet/saboteur.py --swarm-filter --out-dir /tmp/sab # swarm-unit family only
```

---

## Stress axes — orthogonal pressure that must be *proven*

Tiers own mission/platform complexity; **stress axes** own pressure and layer onto *any* tier (tier-1 +
weapons-extreme is a valid, cheap config that tests something tier-5 does not). Full mechanics:
[`README-stress-axes.md`](../../tools/qa-gauntlet/README-stress-axes.md).

| Axis | Levels | Proof mode | Notes |
|------|--------|------------|-------|
| `weapons` | off / moderate / extreme | `differential-token` (`NO_AMMO`) | proven only on a **strict increase** over a control sibling |
| `ew` | off / moderate / extreme | `differential-aggregate` (`Detected`) | compare `Detected` summed across seeds |
| `logistics` | off / moderate / extreme | `config-only` | **not runtime-provable** (`FuelStateProjection` is UI-only, GAP-13) |
| `swarm_*` (S117) | — | `config-only` | always unproven; never hard-fails |

The design's load-bearing idea: **an axis that cannot be demonstrated at runtime is structurally
prevented from claiming it was.** [`stress_axes.py`](../../tools/qa-gauntlet/stress_axes.py) validates the
`PROOF_MODES` vocabulary and warns that a **presence** assertion (`fingerprint-token`) proves nothing for
a token the baseline already emits — `NO_AMMO` occurs **106×** in the unstressed tier-1 baseline, so only
a **differential** check against a control sibling isolates the axis's own contribution.

- [`apply_stress_axes.py`](../../tools/qa-gauntlet/apply_stress_axes.py) derives a policy from
  `(base policy, axis selection)` — **pure and deterministic**, with a fixed `AXIS_ORDER = (ew, logistics,
  weapons)` so the derived scenario **id** is stable regardless of caller dict order. It also resolves the
  EW jam target deterministically (existing jammer `targetId` → first `detection` `targetId` → `ValueError`)
  and **rejects** a scenario with no `detection` entries rather than silently deriving an inert EW axis.
- [`plan_stress_matrix.py`](../../tools/qa-gauntlet/plan_stress_matrix.py) builds a bounded **pairwise
  (2-way)** work list: the full 5-tier × three-3-level-axis cross-product is 135 scenarios; pairwise
  covering guarantees every factor-level *pair* appears at least once (where most interaction defects live)
  at ~1/10 the cost. **Truncation is always reported, never silent.**

### Production proof gate (DRG-63/65)

[`verify_stress_axes.py`](../../tools/qa-gauntlet/verify_stress_axes.py) (aliases
`gate_stress_proof.py` / `run-stress-proof-gate.sh`) consumes an **evidence JSON** (`axis_id` → stressed
vs control fingerprints/aggregates) and verifies every catalog axis:

| Result | Exit |
|--------|------|
| all **non-config-only** axes proven | `0` |
| any non-config-only axis unproven / missing evidence | `1` |
| bad path / malformed JSON | `2` |

**Config-only axes (`logistics`, `swarm_*`) are always unproven and must not hard-fail** — they land in
`config_only_unproven`, and the gate still passes when `weapons`/`ew` are proven. The gate is **opt-in**
after a ladder run (`STRESS_PROOF_EVIDENCE=… run-gauntlet.sh` or `--stress-proof-evidence`); the default
ladder is unchanged when unset. Report keys: `pass`, `results[]`, `proven`, `unproven`, `hard_failures`,
`config_only_unproven`.

---

## Forge scorecard — mechanical promotion scoring

[`forge_scorecard.py`](../../tools/qa-gauntlet/forge_scorecard.py) (alias `forge-scorecard.py`) is the
**mechanical analog of the locked oracle eval** for `/qa-gauntlet-forge` promotion candidates: it scores a
run using corpus coverage + oracle/batch artifacts and **does not mutate policies or edit the oracle
evaluator**. It exits `0` whenever a scorecard is written (promote decisions are encoded in the JSON) and
`2` on usage/missing-path errors.

```bash
python3 tools/qa-gauntlet/forge_scorecard.py --run-dir production/qa/gauntlet/<RUN_ID> --tier <N>
python3 tools/qa-gauntlet/forge_scorecard.py --rebuild-counts   # refresh corpus rarity counts (DRG-60)
```

After a promote, `gauntlet.expect` **must** be regenerated at the tier tick from a real batch CSV — never
hand-edited. That discipline (when to regen, the batch → envelope → `gauntlet_oracle_eval` loop,
`retest-defect.sh`) is the [`README-expect-regen.md`](../../tools/qa-gauntlet/README-expect-regen.md)
runbook.

---

## Invariants

| Invariant | Enforced by |
|-----------|-------------|
| Derivations are pure/deterministic (stable derived scenario ids) | `apply_stress_axes.py` fixed `AXIS_ORDER` |
| An axis can't *claim* pressure it can't *prove* | `stress_axes.py` proof-mode vocabulary + `verify_stress_axes.py` |
| Config-only axes (`logistics`, `swarm_*`) never hard-fail the proof gate | `verify_stress_axes.py` → `config_only_unproven` |
| Matrix truncation is reported, never silent | `plan_stress_matrix.py` |
| Saboteur never commits; runs in disposable worktrees | `saboteur.py` |
| Forge scorecard never mutates policies or the oracle | `forge_scorecard.py` |
| `gauntlet.expect` is regenerated from real CSVs, never hand-edited | `README-expect-regen.md` |

---

## Tests

Every tool has a co-located `pytest` module in [`tools/qa-gauntlet/`](../../tools/qa-gauntlet/):
`test_saboteur.py`, `test_stress_axes.py`, `test_apply_stress_axes.py`, `test_plan_stress_matrix.py`,
`test_verify_stress_axes.py`, `test_gate_stress_proof.py`, `test_forge_scorecard.py`, `test_ladder.py`,
`test_t3_tier_tags.py`, `test_evaluate_run.py`.

```bash
python3 -m pip install -r tools/qa-gauntlet/requirements.txt   # first run only
python3 -m pytest tools/qa-gauntlet -q
```

---

## Related docs

| Doc | Relationship |
|-----|--------------|
| [qa-gauntlet.md](qa-gauntlet.md) | The batch → fail-closed-oracle *loop* and the `gauntlet.expect` schema this tooling drives. |
| [gauntlet-oracle-baseline.md](gauntlet-oracle-baseline.md) | The pinned oracle baselines the aggregator checks against. |
| [gauntlet-logistics-variables.md](gauntlet-logistics-variables.md) | The logistics-axis variables reference (a specific slice of the stress axes above). |
| [`README-stress-axes.md`](../../tools/qa-gauntlet/README-stress-axes.md) · [`README-expect-regen.md`](../../tools/qa-gauntlet/README-expect-regen.md) | Tool-local runbooks for stress-axis mechanics and expect regen. |
| [`.claude/skills/qa-gauntlet/`](../../.claude/skills/qa-gauntlet/SKILL.md) + `qa-gauntlet-forge` / `-stress` / `-remediation` | The agent orchestration that composes this tooling. |
