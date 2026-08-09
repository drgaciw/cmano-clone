# Thermo-Nuclear Remediation Plan — PR #368 Gauntlet Effectiveness

> **For agentic workers:** REQUIRED SUB-SKILL: Use `dispatching-parallel-agents` for Wave 1 (three independent domains in one turn), then a single integrator for Wave 2. Prefer `subagent-driven-development` / `executing-plans` per task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **Status (2026-07-31):** Wave 1 Agents A/B/C MERGED on `cursor/tncq-pr368-remediation-9b58` (PR #370). Gates: build 0/0; pytest 38/38; Gauntlet Data 30 + Cli 7; PlayModeSmoke 21/21; ReplayGolden filter green; hash preserved; ZERO DelegationBridge touches. Full suite 1941 passed; 1 env-only Unity plugin DLL miss cleared locally via gitignored copy (not committed).
>
> **Calibration refresh (2026-07-31):** role-aware summary at `production/qa/gauntlet/calibration-2026-07-31-role-refresh/` — kill rate **4/6** (recompute). **Live re-run post-#372:** `calibration-2026-07-31-live-unity-replay/` — kill rate **5/6** (05 caught via UnityAdapter ReplayGolden; 03 still survives).

**Goal:** Clear the thermo-nuclear REQUEST CHANGES bar on [PR #368](https://github.com/drgaciw/cmano-clone/pull/368) (`qa/gauntlet-effectiveness`) without changing ladder oracle semantics, Baltic v2 replay hash, or DelegationBridge.

**Architecture:** Three independent ownership fixes land in parallel: (A) make `gauntlet.*` schema canonical in `ProjectAegis.Data` and derive strict-key validation from it; (B) make saboteur kill-rate / exit contracts catalog-driven; (C) data-drive the ladder driver and collapse the third CSV parser. A short integrator wave merges, re-verifies, and aligns skill/report prose.

**Tech Stack:** C# (.NET 8) in `ProjectAegis.Data` + `MissionEditor.Cli`; Python 3 + PyYAML + pytest under `tools/qa-gauntlet/`; bash driver; xUnit co-located tests.

**Base branch for implementation:** `qa/gauntlet-effectiveness` (PR #368 tip). Do **not** implement against bare `main` — the code under review lives on that branch.

**Source review:** Cloud agent thermo-nuclear audit (2026-07-31) — REQUEST CHANGES; blockers = schema ownership + saboteur contracts; majors = eval seam, ladder manifest, CSV duplication.

---

## Global constraints

- **Locked-eval (forge four-box) still holds for mutants:** saboteur patches must never target `GauntletOracleEvaluator.cs`, Demo batch harness, ReplayGolden fixtures, `DelegationBridge.cs`, or Baltic v2 hash `17144800277401907079`.
- **Allowed Data edit:** extending `GauntletOracleEvaluator.EvaluateFromPolicyAndCsv` (or a sibling called from it) to fail-closed on unknown `gauntlet.*` keys is intentional oracle hardening, not a mutant surface. Expect-bound semantics must stay bit-identical for valid policies.
- **CatalogWriteGate / DelegationBridge:** zero-touch.
- **No proprietary CMO `.db3`.**
- **TDD:** RED → GREEN for every behavioral change.
- **GitNexus:** before editing any C# symbol, run `impact({target, direction: "upstream"})` and report blast radius. Before commit, `detect_changes()`.
- **Do not edit** attached `.cursor/plans/` files; this file under `docs/superpowers/plans/` is the working plan.
- **Python style:** top-level imports only (no inline `import yaml`).
- **File-size rule:** do not push any implementation file past 1000 lines; extract instead.

---

## Parallel dispatch map

```text
Wave 0 (serial, coordinator) — already this plan
        │
        ▼
Wave 1 (PARALLEL — dispatch all three in one turn)
   ┌────┴────┬──────────────┐
   ▼         ▼              ▼
 Agent A   Agent B       Agent C
 Schema    Saboteur      Ladder + CSV
 (1+3)     contracts(2)  + hygiene(4+5+6)
   │         │              │
   └────┬────┴──────────────┘
        ▼
Wave 2 (serial integrator)
  merge conflicts, full gates, skill/report sync, PR update
```

| Agent | Findings | Touches | Must NOT touch |
|-------|----------|---------|----------------|
| **A — Schema** | 1, 3 | `ScenarioPolicyJsonDto.cs`, new Data strict-keys type, `GauntletOracleEvaluator.cs` (wire-in only), Cli thin adapter/delete, Cli/Data tests | `tools/qa-gauntlet/**`, mutant patches |
| **B — Saboteur** | 2 (+ part of 6: yaml import) | `saboteur.py`, `mutants/catalog.yaml`, `test_saboteur.py`, `.claude/skills/qa-gauntlet-calibrate/SKILL.md` | C#, `run-gauntlet.sh`, `evaluate_run.py` |
| **C — Ladder/CSV** | 4, 5, 6 (determinism + CSV_HEADER) | `ladder.yaml` (new), `run-gauntlet.sh`, `evaluate_run.py`, `test_evaluate_run.py` | C#, `saboteur.py`, catalog.yaml |

**Conflict rule:** Agents A/B/C edit disjoint paths. If an agent needs a shared file, stop and escalate to the integrator — do not race-edit.

---

## Wave 1 — Agent A: Canonical `gauntlet.*` schema + strict keys in Data

**Problem:** Cli hardcodes a third whitelist that already drifted from reality (`dimensionsClaimed` "later" comment vs already listed). `ScenarioGauntletJsonDto` only has four properties; expect/QA metadata live as ad-hoc `JsonDocument` walks. Strict keys are bolted into `GauntletOracleEvalCommand`, so other evaluator callers skip the guard.

**Code judo:** One schema in Data; derive allowed keys from DTO property names (System.Text.Json camelCase); call strict check inside `EvaluateFromPolicyAndCsv` before expect eval; Cli becomes a thin pass-through (or deletes its copy).

### Task A1: Extend DTOs to match real policy surface

**Files:**
- Modify: `src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonDto.cs`
- Possibly add: `src/ProjectAegis.Data/Catalog/GauntletOracleExpectDto.cs` (or nest under ScenarioPolicy) if binder shape must mirror `GauntletOracleExpect`
- Test: extend/add Data tests for deserialize round-trip of a realistic gauntlet block

**DTO surface to own (camelCase JSON):**

| Block | Properties |
|-------|------------|
| `gauntlet` | `intent`, `oracle`, `catalogRefs`, `units`, `expect`, `expectCi`, `runId`, `tier`, `expectProvenance`, `expectCiProvenance`, `dimensionsClaimed`, `forge` |
| `gauntlet.expect` / `expectCi` | align with `GauntletOracleExpect` record fields |
| `gauntlet.units[]` | `unitId`, `platformId`, `domain`, `side` |

QA metadata values (`expectProvenance`, `forge`, …) may stay loosely typed (`JsonElement` / `object` / dedicated small DTOs) — the point is **key ownership**, not deep schema for provenance blobs.

- [ ] **A1.1** Write failing deserialize test: policy JSON with the full whitelist must bind onto `ScenarioGauntletJsonDto` without unknown-property loss for the listed keys.
- [ ] **A1.2** Extend `ScenarioGauntletJsonDto` (+ expect/unit DTOs) until the test is green.
- [ ] **A1.3** GitNexus `impact` on `ScenarioGauntletJsonDto` before edit; report callers.

### Task A2: Move strict-key checker into Data; derive keys

**Files:**
- Create: `src/ProjectAegis.Data/Catalog/GauntletPolicyStrictKeys.cs` (or `Scenario/Policy/`)
- Create/move tests into `src/ProjectAegis.Data.Tests/...`
- Delete (Wave 2 or end of A): Cli `GauntletPolicyStrictKeys.cs` after rewire

**Interfaces:**
```csharp
public sealed record GauntletStrictKeyReport(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public static class GauntletPolicyStrictKeys
{
    public static GauntletStrictKeyReport Check(string policyJson);
}
```

**Derivation rule (preferred):** build `HashSet`s once from `typeof(ScenarioGauntletJsonDto).GetProperties()` → camelCase names (same convention STJ uses without attributes). Same for expect DTO and unit DTO. Do **not** hand-maintain parallel string lists except:

```csharp
// Time-boxed grandfather only — remove when variability EMCON retrofit deletes gauntlet.emcon
private static readonly HashSet<string> LegacyWarnKeys = new(StringComparer.Ordinal) { "emcon" };
```

Fix the stale comment: never claim `dimensionsClaimed` is "later."

- [ ] **A2.1** RED: port Cli tests to Data.Tests; add test that a newly added DTO property becomes allowed without editing a string list (reflection/derivation proof).
- [ ] **A2.2** GREEN: implement derived whitelist + legacy warn for `emcon`.
- [ ] **A2.3** Unknown keys still error with allowed-key listing.

### Task A3: Wire into `GauntletOracleEvaluator` seam

**Files:**
- Modify: `src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs`
- Modify: `src/ProjectAegis.MissionEditor.Cli/GauntletOracleEvalCommand.cs` (remove bolted concat; surface warnings from evaluator result if needed)
- Modify: `GauntletOracleEvaluationResult` if warnings must travel (prefer extend result over anonymous JSON merge)

**Behavior:**
1. Parse policy JSON.
2. `GauntletPolicyStrictKeys.Check` — on any Errors, fail closed (concatenate with expect failures or return immediately).
3. Existing `TryParseExpect` / `Evaluate` unchanged for valid policies.
4. Warnings (`gauntlet.emcon`) appear in command output `warnings` field.

- [ ] **A3.1** GitNexus `impact(GauntletOracleEvaluator)` / `EvaluateFromPolicyAndCsv` — expect HIGH; proceed only because this is fail-closed hardening with tests proving valid policies still pass.
- [ ] **A3.2** RED: existing evaluator tests stay green; new test unknown key fails via `EvaluateFromPolicyAndCsv` alone (no Cli).
- [ ] **A3.3** GREEN: wire check; strip Cli duplicate call; leave Cli warnings plumbing if result carries them.
- [ ] **A3.4** Delete Cli `GauntletPolicyStrictKeys.cs` + retarget Cli tests to Data type **or** leave a one-line obsolete wrapper — prefer delete.

**Done when:**
- No hand-maintained `GauntletKeys` string encyclopedia in Cli.
- `EvaluateFromPolicyAndCsv` rejects unknown `gauntlet.*` keys.
- Valid ladder policies (incl. QA metadata keys) still pass.
- Stale "later add dimensionsClaimed" comment gone.

---

## Wave 1 — Agent B: Saboteur catalog roles + kill-rate / exit contracts

**Problem:** Skill/catalog treat `06-emcon-engage-bypass` as a documented expected miss, but `exit_code_for` fails every non-control survivor. `summarize` includes the `00-*` control in the kill-rate denominator (`4/8` vs claimed `4/7`). `expectedOracles` is display-only.

**Code judo:** Catalog declares `role`; code has one exit/kill-rate path driven by role. Delete prefix-magic as the sole contract (prefix may remain a convenience default for `control`).

### Task B1: Catalog `role` field

**Files:**
- Modify: `tools/qa-gauntlet/mutants/catalog.yaml`
- Modify: `tools/qa-gauntlet/saboteur.py` (`load_catalog`)
- Modify: `tools/qa-gauntlet/test_saboteur.py`

**Schema addition per mutant:**
```yaml
role: control | expected-miss | defect   # required
```

Mapping for current catalog:
| id | role |
|----|------|
| `00-noop-comment` | `control` |
| `01`…`05`, `07` | `defect` |
| `06-emcon-engage-bypass` | `expected-miss` |

`load_catalog` rejects missing/unknown `role`. Optional: default `control` if id startswith `00-` only during migration — prefer explicit required field.

- [ ] **B1.1** RED: tests for required `role`, reject unknown role, reject locked targets (existing).
- [ ] **B1.2** GREEN: update catalog + loader.

### Task B2: Kill-rate and exit_code from roles

**Files:**
- Modify: `saboteur.py` — `summarize`, `exit_code_for`, `render_report`
- Modify: `test_saboteur.py`
- Modify: `.claude/skills/qa-gauntlet-calibrate/SKILL.md`

**Contracts:**
| Outcome | `control` | `expected-miss` | `defect` |
|---------|-----------|-----------------|----------|
| survived | OK (exit 0) | OK (exit 0) | FAIL |
| caught | FAIL (false positive) | FAIL* or WARN — **choose FAIL** so "fixed EMCON" flips role to `defect` later | OK |
| invalid-mutant | FAIL | FAIL | FAIL |

\*When EMCON retrofit lands, flip `06` → `role: defect` and expect catch — do not special-case id strings in Python.

**Kill rate:**
```
killRate = caught_defects / (caught_defects + survived_defects)
```
Exclude `control` and `expected-miss` from numerator and denominator. Headline for today's catalog with 4 caught defects + 2 survived defects (03, 05) = `4/6` until those blind spots are fixed — **do not** claim `4/7` unless the math matches. Update skill + any commit prose references to the formula, not a frozen wrong fraction.

Also: move `import yaml` to module top (finding 6).

- [ ] **B2.1** RED: rewrite `test_summarize_*` / `test_exit_code_*` for role matrix (control survive OK; expected-miss survive OK; defect survive FAIL; control caught FAIL).
- [ ] **B2.2** GREEN: implement; top-level `yaml` import.
- [ ] **B2.3** Update calibrate SKILL: document `role`, kill-rate formula, exit 0 meaning.
- [ ] **B2.4** Do **not** re-run full saboteur in unit tests; optionally note integrator may refresh `calibration-*/report.json` in Wave 2.

**Done when:** With current catalog (06 = expected-miss, 03/05 still blind), `exit_code_for` returns **1** only because of defect survivors 03/05 — not because of 06. After 03/05 are fixed out-of-band, calibrate can exit 0 with 06 still surviving. Kill-rate denominator excludes control + expected-miss.

---

## Wave 1 — Agent C: Ladder manifest + single Python CSV path

**Problem:** Scenario IDs and tick budgets live in bash `case` statements while the rest of `tools/qa-gauntlet/` is YAML-driven. Anchor filtering is a third CSV parser inline in the shell. Determinism compares sorted raw lines; `CSV_HEADER` is dead.

**Code judo:** One `ladder.yaml`; driver reads it; anchor filter is a function in `evaluate_run.py` (or tiny `gauntlet_csv.py` imported by both).

### Task C1: `ladder.yaml` manifest

**Files:**
- Create: `tools/qa-gauntlet/ladder.yaml` (or `production/qa/gauntlet/corpus/ladder.yaml` — prefer **tools/qa-gauntlet/ladder.yaml** next to the driver)
- Modify: `tools/qa-gauntlet/run-gauntlet.sh`
- Create: `tools/qa-gauntlet/test_ladder.py` (load + schema validate)

**Shape:**
```yaml
version: 1
defaultAnchorSeeds: ["42", "7", "123"]
tiers:
  "1":
    ticks: 6
    scenarios: [gauntlet-t1-patrol-a, gauntlet-t1-patrol-b, ...]
  "2": { ticks: 10, scenarios: [...] }
  # ... 3,4,5,extra
```

Values must match today's `scenarios_for` / `ticks_for` exactly (behavior-preserving).

- [ ] **C1.1** RED: test manifest exists, all tiers present, tick budgets 6/10/16/24/40/12, scenario counts match current driver.
- [ ] **C1.2** GREEN: write YAML; shell loads via `python3 -c` / small helper printing scenarios/ticks for a tier id — **delete** `scenarios_for` / `ticks_for` case functions.

### Task C2: Collapse bash CSV filter into `evaluate_run.py`

**Files:**
- Modify: `tools/qa-gauntlet/evaluate_run.py`
- Modify: `tools/qa-gauntlet/run-gauntlet.sh`
- Modify: `tools/qa-gauntlet/test_evaluate_run.py`

**Add:**
```python
def filter_csv_by_seeds(src: Path, dst: Path, seeds: set[str]) -> int:
    """Write header + rows whose seed column is in seeds. Return row count kept."""
```

Shell replaces the hermit-crab heredoc with:
```bash
python3 tools/qa-gauntlet/evaluate_run.py filter-seeds \
  --in "$TDIR/results.csv" --out "$TDIR/results-anchors.csv" \
  --seeds "$ANCHOR_SEEDS"
```
(or a `if __name__` subcommand / `python3 -c 'from evaluate_run import …'`). Prefer a real subcommand next to `tier`/`run`/`bless`.

Keep C# `ParseCsvRows` as victory/ROE authority — dual stack stays; only the third parser dies.

- [ ] **C2.1** RED: filter-seeds preserves fingerprint commas; drops non-anchor seeds.
- [ ] **C2.2** GREEN: implement + rewire shell.

### Task C3: Determinism + dead code hygiene

**Files:**
- Modify: `evaluate_run.py` — `oracle_determinism`
- Modify: `test_evaluate_run.py`

- [ ] **C3.1** Compare parsed `Row` lists keyed by `(scenario_id, seed)` (or sorted by that key), not raw sorted lines. Header/blank noise must not false-fail.
- [ ] **C3.2** Delete unused `CSV_HEADER` or use it as the sole header equality check in `parse_results_csv`.
- [ ] **C3.3** Existing determinism tests updated; order-independence still passes.

**Done when:** No scenario/tick knowledge in bash case arms; no inline CSV parse in `run-gauntlet.sh`; determinism uses structured rows.

---

## Wave 2 — Integrator (serial, after A+B+C)

Dispatch **one** agent only after Wave 1 returns.

### Task I1: Merge + conflict audit

- [ ] Confirm Agents A/B/C only touched their path sets.
- [ ] Resolve any accidental overlap.
- [ ] `gitnexus_detect_changes` / `detect_changes({scope: "compare", base_ref: "main"})` on the integration branch — affected symbols should be Data gauntlet types + Cli command + Python tools only.

### Task I2: Verification gates (RUN+READ)

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal   # ≥1638, 0 failures
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Data.Tests --filter Gauntlet
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests --filter Gauntlet
python3 -m pytest tools/qa-gauntlet/test_evaluate_run.py tools/qa-gauntlet/test_saboteur.py tools/qa-gauntlet/test_ladder.py -q
grep -r "17144800277401907079" tests/ data/
# Confirm ZERO new DelegationBridge hotpath edits
```

### Task I3: Prose + calibration artifact sync

- [ ] `.claude/skills/qa-gauntlet/SKILL.md` — point at `ladder.yaml`; mention Data-owned strict keys.
- [ ] Calibrate skill kill-rate line matches `summarize` formula.
- [x] If refreshing calibration JSON/MD: rerun saboteur **or** hand-edit summary fields only when a full rerun is impossible — never invent kill matrices. Prefer rerun when CI time allows. **Done:** `calibration-2026-07-31-role-refresh` (role-aware recompute of measured postrebase outcomes; full live re-run still optional).
- [ ] Update PR #368 description with remediation checklist status.

### Task I4: Thermo-nuclear re-check (approval bar)

Re-score against the original bar:

| Bar item | Must be true |
|----------|--------------|
| No third schema in Cli | ✅ |
| Calibrate exit/kill-rate one source of truth | ✅ |
| Strict keys at Data eval seam | ✅ |
| Ladder contract data-driven | ✅ |
| No third CSV parser | ✅ |
| No 1k-line breach | ✅ |
| No new spaghetti branches in unrelated flows | ✅ |

---

## Out of scope (do not pull into this remediation)

- Fixing oracle blind spots **03-salvo-off-by-one** and **05-contact-lifecycle-skip** (separate sim/oracle work; already have BUG docs). This plan only makes their survival a *clean* calibrate fail, not confused with 06.
- EMCON variability retrofit that turns `06` into a real catch (flip `role: defect` when that lands).
- Collapsing C# victory/ROE evaluator into Python (approved dual stack).
- Parallelizing saboteur worktrees (nice-to-have orchestration; not a merge blocker).
- Rewriting the 1781-line plan doc from PR #368.

---

## Agent prompt templates (Wave 1 — paste into Task calls)

### Agent A prompt

```markdown
You are Agent A on branch qa/gauntlet-effectiveness (PR #368 remediation).
Implement Wave 1 Agent A from docs/superpowers/plans/2026-07-31-tncq-pr368-gauntlet-effectiveness-remediation.md
(findings 1+3: canonical gauntlet schema + strict keys in ProjectAegis.Data).

Constraints: TDD; GitNexus impact before symbol edits; do not touch tools/qa-gauntlet/**;
do not edit DelegationBridge; expect-bound semantics for valid policies must stay identical.
Delete Cli GauntletPolicyStrictKeys after Data owns it.

Return: files changed, impact summaries, test commands+results, residual risks.
```

### Agent B prompt

```markdown
You are Agent B on branch qa/gauntlet-effectiveness (PR #368 remediation).
Implement Wave 1 Agent B from docs/superpowers/plans/2026-07-31-tncq-pr368-gauntlet-effectiveness-remediation.md
(finding 2: saboteur role/kill-rate/exit contracts; top-level yaml import).

Constraints: TDD; touch only saboteur.py, mutants/catalog.yaml, test_saboteur.py,
and qa-gauntlet-calibrate SKILL. Do not touch C# or run-gauntlet.sh / evaluate_run.py.
Do not re-run full saboteur calibration unless asked.

Return: role matrix table, kill-rate formula, test results, skill edits.
```

### Agent C prompt

```markdown
You are Agent C on branch qa/gauntlet-effectiveness (PR #368 remediation).
Implement Wave 1 Agent C from docs/superpowers/plans/2026-07-31-tncq-pr368-gauntlet-effectiveness-remediation.md
(findings 4+5+6: ladder.yaml, filter-seeds subcommand, structured determinism, dead CSV_HEADER).

Constraints: TDD; behavior-preserving vs current scenarios_for/ticks_for; touch only
tools/qa-gauntlet/ladder.yaml (new), run-gauntlet.sh, evaluate_run.py, test_evaluate_run.py,
test_ladder.py. Do not touch C# or saboteur.py.

Return: ladder.yaml path, deleted case-functions confirmation, pytest results.
```

### Integrator prompt

```markdown
You are the Wave 2 integrator. Agents A/B/C have finished on qa/gauntlet-effectiveness.
Follow Wave 2 in docs/superpowers/plans/2026-07-31-tncq-pr368-gauntlet-effectiveness-remediation.md:
merge/audit paths, run full verification gates, sync skills/reports, update PR #368.
Do not start new features. Report gate outputs and residual FOLLOW-UPs only.
```

---

## Success criteria (merge-ready)

1. Thermo-nuclear blockers 1–2 and majors 3–5 addressed with tests.
2. Full solution test floor held (≥1638 / 0 failures); PlayModeSmoke ≥20/20; ReplayGolden hash preserved.
3. `pytest` for evaluate_run + saboteur + ladder green.
4. PR #368 description lists remediation complete; calibrate skill math matches code.
5. No file pushed across 1000 implementation lines.
