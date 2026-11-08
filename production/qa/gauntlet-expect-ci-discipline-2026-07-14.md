# Gauntlet expect / CI discipline (S95-01)

**Date:** 2026-07-14  
**Story:** S95-01 — Expect regen / CI discipline  
**Authority:** [`sprint-95-gauntlet-productization.md`](../sprints/sprint-95-gauntlet-productization.md), [`qa-plan-sprint-95-gauntlet-productization-2026-07-14.md`](qa-plan-sprint-95-gauntlet-productization-2026-07-14.md), [`.claude/skills/qa-gauntlet/SKILL.md`](../../.claude/skills/qa-gauntlet/SKILL.md)  
**Stage:** **Release** (no Launch advance)  
**Operator runbook:** [`tools/qa-gauntlet/README-expect-regen.md`](../../tools/qa-gauntlet/README-expect-regen.md)

---

## 1. Problem — oracle expect recalibration drift vs tier tick budgets

`gauntlet.expect` envelopes (`minScore` / `maxScore`, `maxMissilesFired`, `minDenials` / `maxDenials`, kill floors, fingerprint substring gates) are **tick-budget–coupled**. Score, denial, and engagement counts scale with how long the batch harness runs.

**Failure mode observed on max-variance run [`gauntlet-20260713-1739`](gauntlet/gauntlet-20260713-1739/AAR.md):**

| Class | Detail |
|-------|--------|
| Symptom | First-pass `gauntlet_oracle_eval` red on 9 policies after a green stability batch |
| Root cause | Stale `gauntlet.expect` calibrated at mismatched or underspecified tick budgets (esp. **T3@16**, **T4@24**, **T5@40**) |
| Not a sim bug | Envelopes were too tight vs observed scores/missiles/denials at the **ladder** tick counts; fingerprint gates were still valid |
| Remediation | Recalibrate expects from observed batch CSV at the **same** tier tick budget; keep `requireFingerprintSubstrings` / `requireTrueLaunchedShooters` unless the scenario intent changes |
| Audit trail | `production/qa/gauntlet/gauntlet-20260713-1739/oracle-expect-recalibration.json` |

If expects are hand-tuned against a 10-tick CI smoke while the ladder runs T5 at 40 ticks, **oracle red/green becomes noise** — recalibration thrash on every max-variance or tier promotion run.

**Discipline goal:** generate/refresh `gauntlet.expect` only at **known tier-tick boundaries**, from a real Demo batch CSV, then re-run `gauntlet_oracle_eval` fail-closed before merge.

---

## 2. Rule — regenerate / refresh expects at tier-tick boundaries

### 2.1 Recommended ticks (ladder practice)

From qa-gauntlet Phase B and max-variance AAR `gauntlet-20260713-1739` (matrix: T1=6 … T5=40):

| Tier | Recommended ticks | Scenario class (ladder) | Notes |
|------|-------------------|-------------------------|-------|
| **T1** | **6** | Patrol / survive | Short stability + kill floor |
| **T2** | **10** | Escort / strike | Matches default CI fixture batch ticks |
| **T3** | **16** | Joint / EMCON phases / ID-ROE | Denial floors grow with EMCON phases |
| **T4** | **24** | Multi-mission / random inject | Inject mid-run tokens need longer horizon |
| **T5** | **40** | Cascade / theater / mid-run ROE change | Contested EM + cascade injects |

**CI fixture smoke** (`.github/workflows/gauntlet-oracle.yml`) uses **`--ticks 10`** as a **cross-tier smoke budget**, not as the ladder authority for T3–T5 expect envelopes. When refreshing a T3–T5 policy expect, always batch at the **recommended tier tick** above — do not re-baseline T5 expects from a 10-tick CSV.

**Extra / phase-3 anchors** (joint ORBAT, multi-domain shooters, theater inject/victory): use **12** ticks unless the policy documents otherwise (per max-variance “extra” row).

### 2.2 When to regen

Regenerate or refresh `gauntlet.expect` when **any** of:

1. **Tier tick budget changes** for that scenario (or ladder default changes).
2. **Scenario intent / units / injects / ROE / EMCON** change in the policy JSON.
3. **Harness behavior** that legitimately moves score/kills/missiles/denials (after a sim fix, not before).
4. **Oracle first-pass fail** classified `oracle` (wrong expectation) — not `sim-code` — after triage.
5. **Max-variance or full-ladder promotion** lands and envelopes were last calibrated under a different budget.

Do **not** regen expects to green-wash an unexplained sim regression. Classify first (`sim-code` vs `oracle` vs `scenario-data` vs `flaky`).

### 2.3 How to regen (contract)

1. Run Demo batch at the **tier tick** for that policy (table §2.1).
2. Derive envelope bounds from observed CSV rows across seeds (default seeds `42,7,123` for ladder/max-variance).
3. Preserve intent gates: `side`, `requireNonEmptyFingerprint`, `requireFingerprintSubstrings`, `requireTrueLaunchedShooters` unless the scenario design changes.
4. Write/update `gauntlet.expect` in the policy under `data/scenarios/` (shipped source of truth).
5. Re-run `gauntlet_oracle_eval` — must report `allPassed: true`.
6. Optionally archive old→new→observed in a run-dir audit JSON (pattern: `oracle-expect-recalibration.json`).

**Forbidden:** hand-edit score/missile/denial envelopes without a fresh batch CSV at the matching tick budget. See operator runbook.

---

## 3. CI contract — fail-closed `gauntlet_oracle_eval`

**Workflow:** [`.github/workflows/gauntlet-oracle.yml`](../../.github/workflows/gauntlet-oracle.yml)

| Step | Contract |
|------|----------|
| Build | `dotnet build ProjectAegis.sln -c Release` |
| Fixture set | Stage phase-3 + ladder sample policies into `artifacts/gauntlet-oracle/policies/` |
| Batch | `ProjectAegis.Delegation.Demo --batch` → real CSV (`--seeds 42`, `--ticks 10`) |
| **Positive gate** | `gauntlet_oracle_eval --policy-dir … --csv …` must exit **0** and `allPassed === true` |
| **Negative smoke (fail-closed)** | Strip inject / multi-domain evidence tokens from CSV while **keeping** expect gates on policies (`gauntlet-t4-random-inject`, `gauntlet-t3-emcon-phases`); eval must exit **non-zero** and `allPassed === false` |
| Artifacts | Upload `artifacts/gauntlet-oracle/` always |

### 3.1 What “fail-closed” means

- Missing or failing positive eval → **job fails** (no soft continue).
- Stripped-token path that accidentally still passes → **job fails** (gates are not decorative).
- Fingerprint / inject / `True|Launched` substring expects are part of the product gate, not advisory docs.

### 3.2 GHA billing gate — local dry-run required before merge

Org **GitHub Actions may be billing-gated** (permanent local-gate advisory elsewhere in the program). When GHA does not run:

- The workflow YAML remains the **product contract**.
- **Local dry-run that mirrors the job is required before merge** of any change that touches gauntlet policies, Demo batch path, or `gauntlet_oracle_eval` behavior.
- Do not treat “GHA skipped / billing-aborted” as product green.

---

## 4. Local dry-run steps

Run from repo root with .NET 8 on `PATH` (`export PATH="${HOME}/.dotnet:${PATH}"` if needed).

### 4.1 Positive path (mirror CI fixture set)

```bash
mkdir -p artifacts/gauntlet-oracle/policies
cp data/scenarios/gauntlet-multidomain-shooters.policy.json \
   data/scenarios/gauntlet-theater-inject.policy.json \
   data/scenarios/gauntlet-theater-dynamic-victory.policy.json \
   data/scenarios/gauntlet-t1-patrol-b.policy.json \
   data/scenarios/gauntlet-t4-random-inject.policy.json \
   data/scenarios/gauntlet-t5-cascade.policy.json \
   data/scenarios/gauntlet-t5-roe-change.policy.json \
   data/scenarios/gauntlet-joint-orbat-smoke.policy.json \
   data/scenarios/gauntlet-t3-emcon-phases.policy.json \
   artifacts/gauntlet-oracle/policies/

SCENARIOS="gauntlet-multidomain-shooters,gauntlet-theater-inject,gauntlet-theater-dynamic-victory,gauntlet-t1-patrol-b,gauntlet-t4-random-inject,gauntlet-t5-cascade,gauntlet-t5-roe-change,gauntlet-joint-orbat-smoke,gauntlet-t3-emcon-phases"

dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios "$SCENARIOS" \
  --seeds 42 \
  --ticks 10 \
  --csv-out artifacts/gauntlet-oracle/results.csv

dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- \
  gauntlet_oracle_eval \
  --policy-dir artifacts/gauntlet-oracle/policies \
  --csv artifacts/gauntlet-oracle/results.csv \
  --out artifacts/gauntlet-oracle/oracle-eval.json

python3 -c "import json; d=json.load(open('artifacts/gauntlet-oracle/oracle-eval.json')); assert d.get('allPassed') is True, d"
```

### 4.2 Tier-tick expect refresh (ladder authority)

```bash
# Example: refresh T5 expects at the ladder budget (not CI's 10 ticks)
TIER=5
TICKS=40
SCENARIOS="gauntlet-t5-cascade,gauntlet-t5-dynamic-obj,gauntlet-t5-roe-change,gauntlet-t5-theater"
OUT=artifacts/gauntlet-expect-regen/t${TIER}

mkdir -p "$OUT"
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios "$SCENARIOS" \
  --seeds 42,7,123 \
  --ticks "$TICKS" \
  --csv-out "$OUT/results.csv"

# After updating data/scenarios/*.policy.json gauntlet.expect from $OUT/results.csv:
dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- \
  gauntlet_oracle_eval \
  --policy-dir data/scenarios \
  --csv "$OUT/results.csv" \
  --out "$OUT/oracle-eval.json"
# Restrict policy-dir to only the refreshed policies if the full scenarios dir is too broad.
```

### 4.3 Fail-closed strip-token smoke (manual mirror)

Follow the Python strip logic in `.github/workflows/gauntlet-oracle.yml` (job step **Fail-closed smoke**): strip inject / `True|Launched` tokens from the CSV for `gauntlet-t4-random-inject` and `gauntlet-t3-emcon-phases`, keep expects, assert non-zero exit and `allPassed is False`.

### 4.4 Closed-defect re-test

```bash
tools/qa-gauntlet/retest-defect.sh <defect-id> --out-dir /tmp/gauntlet-retest
```

Registry: `production/qa/gauntlet-defect-registry.json` (hygiene owned by S95-02; do not casually rewrite closed IDs).

---

## 5. Suite floor and evidence cites

| Cite | Value |
|------|--------|
| Max-variance green family | Run **`gauntlet-20260713-1739`** — T1–T5 + extra allPassed after expect recalibration; 24 policies × seeds 42,7,123; AAR under `production/qa/gauntlet/gauntlet-20260713-1739/` |
| Post-land suite floor | **≥1638 pass / 0 failed** (post-gauntlet land gate; cite last-gate log e.g. `gates-gauntlet-land-post-2026-07-14.log` when not re-running full suite on docs-only) |
| Related CI productization | [`gauntlet-phase3-multidomain-theater-ci-2026-07-13.md`](gauntlet-phase3-multidomain-theater-ci-2026-07-13.md) |

If C# / harness / evaluator is touched: **RUN+READ** full suite ≥1638/0f before merge. Docs/fixtures-only: floor citation is acceptable per S95 hard gates.

---

## 6. Stage and non-goals

| Item | Rule |
|------|------|
| Stage | Remains **Release** — **no Launch** |
| S95-01 scope | Discipline docs + operator runbook; full regen of every expect file is optional, not required for story close |
| Out of scope | S94 asset rework; S96–S97; store/Launch; DelegationBridge hotpath; hand-weakening fail-closed CI |
| CRITICAL hubs | Impact before edits to `BalticReplayHarness` / oracle evaluator symbols |

---

## 7. Acceptance checklist (S95-01)

- [x] Problem (expect drift vs tier ticks) documented with max-variance cite `gauntlet-20260713-1739`
- [x] Rule: refresh `gauntlet.expect` at tier-tick boundaries; T1–T5 ticks **6 / 10 / 16 / 24 / 40**
- [x] CI contract: `gauntlet-oracle.yml` positive `gauntlet_oracle_eval` + strip-token negative smoke
- [x] Local dry-run steps published (batch + eval)
- [x] GHA billing-gated → local dry-run required before merge (explicit)
- [x] Stage **Release**; no Launch
- [x] Suite floor **≥1638** cited
- [x] Operator runbook at `tools/qa-gauntlet/README-expect-regen.md`

---

*S95-01 deliverable — 2026-07-14. Stage remains Release.*
