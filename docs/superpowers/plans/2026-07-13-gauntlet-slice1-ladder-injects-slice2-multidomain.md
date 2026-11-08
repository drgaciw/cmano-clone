# Gauntlet Slice 1 (Ladder Injects) + Slice 2 (Multidomain Default t3+) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close residual work so (1) ladder inject policies prove real mid-run `CommsStateChange`/`PolicyUpdate` deltas (not EventFired-only / no-op ROE), and (2) every tier≥3 gauntlet policy defaults to concurrent multi-domain engage via Visby→Sovremenny, Gripen→Steregushchiy, Gotland→Buyan — with batch oracle, CI fixtures, and a production QA note.

**Architecture:** No new sim architecture. Slice 1 is policy JSON + stronger harness tests on the existing `CommsTimelineSimulator` → `CommsStateChange` and contact-trigger → `ApplyRoeToUnits` paths. Slice 2 is scenario data (add Buyan red + retarget Gotland detection) on the existing multi-agent blues + `PreferredHostileByShooter` + `SwarmSalvoDeconfliction` path. Closure is Demo `--batch` × seeds 42,7,123 → `gauntlet_oracle_eval` → expand `.github/workflows/gauntlet-oracle.yml`.

**Tech Stack:** .NET 8, NUnit, BalticReplayHarness, ScenarioPolicy JSON under `data/scenarios/`, MissionEditor.Cli `gauntlet_oracle_eval`, GitHub Actions.

## Global Constraints

- Repo root: `cmano-clone/` (nested under workspace `cmano-clone/cmano-clone` when outer monorepo present).
- Catalog-DB combat only — no synthetic `u1` / `hostile-1` combat paths.
- TDD: failing/stronger tests first where assertions change; data-only tasks may green immediately after JSON edits.
- Determinism: fixed seeds `42,7,123`; prefer harness ticks matching existing tests (inject: 12; multi-domain: 10) and CI ticks `10` for batch gate.
- Do not mass-scrape CMO DB; do not delete historical `production/qa/gauntlet/*` runs (write new note instead).
- `ApplyRoeToUnits` skips log when ROE unchanged — never claim mid-run ROE change for WeaponsFree→WeaponsFree.
- Swarm deconfliction: one shooter per distinct red — Gotland must not share Visby's Sovremenny target.
- Oracle evaluator remains numeric (`gauntlet.expect`); inject/multi-domain semantics are unit-test gated (optional fingerprint post-checks in batch scripts only if needed).
- Commit only when user asks or task step explicitly says commit; prefer one logical commit per slice or a single dual-slice commit after green suite.

## File map

| File | Responsibility |
|------|----------------|
| `data/scenarios/gauntlet-t4-random-inject.policy.json` | Ladder inject + (post S2) Buyan triple |
| `data/scenarios/gauntlet-t5-cascade.policy.json` | Cascade comms + (post S2) Buyan triple |
| `data/scenarios/gauntlet-t5-roe-change.policy.json` | Tight→Free ROE + light comms + Buyan |
| `data/scenarios/gauntlet-t3-*.policy.json` (remaining) | Multidomain default roll-out |
| `data/scenarios/gauntlet-t4-*.policy.json` (remaining) | Multidomain default roll-out |
| `data/scenarios/gauntlet-t5-*.policy.json` (remaining) | Multidomain default roll-out |
| `data/scenarios/gauntlet-joint-orbat-smoke.policy.json` | Reference triple (already correct) |
| `data/scenarios/gauntlet-t3-emcon-phases.policy.json` | Reference triple (already correct) |
| `src/.../BalticReplayHarnessLadderInjectTests.cs` | Slice 1 hard gates |
| `src/.../BalticReplayHarnessLadderMultiDomainTests.cs` | Slice 2 hard gates (all t3+) |
| `src/.../BalticReplayHarnessGauntletTier35CatalogTests.cs` | `IsCatalogRed` must include Buyan |
| `.github/workflows/gauntlet-oracle.yml` | PR fixture expansion |
| `production/qa/gauntlet-ladder-injects-multidomain-2026-07-13.md` | Delivery note |

**Reference pairing (copy from joint-orbat-smoke):**

| Blue | Domain | Red unitId |
|------|--------|------------|
| `k-31-visby-2009` | surface | `em-sovremenny-i-pr-956-sarych` |
| `jas-39c-gripen-2005` | air | `mpk-steregushchiy-pr-20380-2018` |
| `a-19-gotland-2022` | subsurface | `mrk-buyan-pr-21630-buyan-2007` |

**Buyan unit template:**

```json
{
  "unitId": "mrk-buyan-pr-21630-buyan-2007",
  "platformId": "mrk-buyan-pr-21630-buyan-2007",
  "domain": "surface",
  "side": "red"
}
```

**Gotland detection retarget:**

```json
{
  "observerId": "a-19-gotland-2022",
  "sensorId": "sonar-1",
  "targetId": "mrk-buyan-pr-21630-buyan-2007",
  "contactId": "c-gotland",
  "basePd": 1.0,
  "envMask": 1.0,
  "jamStrength": 0,
  "requiresActiveRadar": false
}
```

**Policies needing Buyan roll-out (missing today):**

- t3: `escort-strike`, `event-chain`, `id-roe`
- t4: `asymm-roe`, `multi-mission`, `random-inject`, `weighted`
- t5: `cascade`, `dynamic-obj`, `roe-change`, `theater`

**Already complete pairing:** `joint-orbat-smoke`, `t3-emcon-phases`, `multidomain-shooters`.

---

### Task 1: Slice 1 — Harden ladder inject tests (TDD red/green)

**Files:**
- Modify: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessLadderInjectTests.cs`
- May modify: `data/scenarios/gauntlet-t4-random-inject.policy.json`, `gauntlet-t5-cascade.policy.json`, `gauntlet-t5-roe-change.policy.json` (only if tests fail for hollow ROE)

**Interfaces:**
- Consumes: `BalticReplayHarness.Run`, `DecisionLog.CommsStateChanges`, `OrderLogEntryKind.PolicyUpdate`, `PolicyUpdateRecord`
- Produces: Stronger assertions used by CI confidence for ladder inject trio

- [ ] **Step 1: Strengthen tests (require real semantics)**

Replace OR-only logic with per-policy requirements:

```csharp
// All three: mid-run CommsStateChange Degraded/Denied (atTick >= 1)
// t5-roe-change additionally: PolicyUpdate field=roe with OldValue != NewValue and SimTick >= 1
// cascade: must observe Denied at some tick >= 4 (reason may include cascade_link_down)
// Do NOT accept tick-0 bind PolicyUpdate as mid-run inject
```

Concrete test outline:

```csharp
[TestCase("gauntlet-t4-random-inject")]
[TestCase("gauntlet-t5-cascade")]
[TestCase("gauntlet-t5-roe-change")]
public void Ladder_inject_emits_mid_run_comms_state_change(string scenarioId)
{
    ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
    var result = BalticReplayHarness.Run(42, scenarioId, ticks: 12, mvpEngagement: true);
    var deltas = result.DecisionLog.CommsStateChanges
        .Where(c => c.NewState is CommsState.Degraded or CommsState.Denied)
        .ToList();
    Assert.That(deltas, Is.Not.Empty, $"{scenarioId}: CommsStateChange required");
    Assert.That(deltas.Min(c => c.SimTick), Is.GreaterThanOrEqualTo(1ul));
    Assert.That(result.Fingerprint, Does.Contain("CommsStateChange"));
}

[Test]
public void Ladder_t5_roe_change_emits_mid_run_roe_policy_update_tight_to_free()
{
    var result = BalticReplayHarness.Run(42, "gauntlet-t5-roe-change", ticks: 12, mvpEngagement: true);
    var roeUpdates = result.DecisionLog.ChronologicalEntries()
        .Where(e => e.Kind == OrderLogEntryKind.PolicyUpdate
                    && e.Payload is PolicyUpdateRecord p
                    && string.Equals(p.Field, "roe", StringComparison.Ordinal)
                    && e.SimTick >= 1ul
                    && !string.Equals(p.OldValue, p.NewValue, StringComparison.Ordinal))
        .ToList();
    Assert.That(roeUpdates, Is.Not.Empty);
}

[Test]
public void Ladder_t5_cascade_hits_denied()
{
    var result = BalticReplayHarness.Run(42, "gauntlet-t5-cascade", ticks: 12, mvpEngagement: true);
    Assert.That(
        result.DecisionLog.CommsStateChanges.Any(c =>
            c.NewState == CommsState.Denied && c.SimTick >= 4ul),
        Is.True);
}
```

Keep `Ladder_inject_policies_declare_comms_timeline_or_mission_triggers`.

- [ ] **Step 2: Run tests (expect possible red on hollow ROE-only claims)**

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~BalticReplayHarnessLadderInjectTests" -c Release --nologo
```

Expected: comms tests pass for all three (comms already wired). ROE mid-run test passes for `t5-roe-change` only if contact ApplyRoe fires after Tight bind. If fail, proceed Step 3.

- [ ] **Step 3: Fix policies if needed**

For **t4-random-inject** and **t5-cascade**: remove no-op `mission.triggers` ROE WeaponsFree→WeaponsFree **or** set `friendlyRoe: WeaponsTight` if product wants ROE on those ladders. Prefer **remove hollow trigger** and document inject = comms-primary (matches skill “random inject / cascade” as comms adversarial). Keep trigger on **t5-roe-change** only.

Verify `t5-roe-change` still has:

```json
"friendlyRoe": "WeaponsTight",
"comms": [{ "atTick": 3, "newState": "Degraded", ... }],
"mission.triggers": [{ "roe": "WeaponsFree", "unitIds": ["k-31-visby-2009"], ... }]
```

- [ ] **Step 4: Re-run inject tests — all green**

Same command as Step 2. Expected: all Passed.

- [ ] **Step 5: Commit (optional mid-checkpoint)**

```bash
git add src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessLadderInjectTests.cs \
  data/scenarios/gauntlet-t4-random-inject.policy.json \
  data/scenarios/gauntlet-t5-cascade.policy.json \
  data/scenarios/gauntlet-t5-roe-change.policy.json
git commit -m "test(gauntlet): harden ladder inject mid-run comms and ROE assertions"
```

---

### Task 2: Slice 2 — Propagate multidomain Buyan triple to all t3–t5 policies

**Files:**
- Modify: every `data/scenarios/gauntlet-t{3,4,5}-*.policy.json` missing Buyan (list above)
- Do **not** change `gauntlet-t1-*` / `gauntlet-t2-*`
- Do **not** weaken already-correct joint/emcon/shooters

**Interfaces:**
- Consumes: `gauntlet.units`, `detection[]` schema already loaded by harness
- Produces: Distinct preferred hostiles for surface/air/sub on all tier≥3 ladder policies

- [ ] **Step 1: Scripted roll-out (deterministic edit)**

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
python3 <<'PY'
import json, glob, pathlib
BUYAN = {
  "unitId": "mrk-buyan-pr-21630-buyan-2007",
  "platformId": "mrk-buyan-pr-21630-buyan-2007",
  "domain": "surface",
  "side": "red",
}
TARGET = "mrk-buyan-pr-21630-buyan-2007"
for path in sorted(glob.glob("data/scenarios/gauntlet-t[345]-*.policy.json")):
    p = pathlib.Path(path)
    d = json.loads(p.read_text())
    units = d.setdefault("gauntlet", {}).setdefault("units", [])
    ids = {u.get("unitId") for u in units}
    if TARGET not in ids:
        # insert after steregushchiy if present else append
        units.append(BUYAN)
    for t in d.get("detection") or []:
        if t.get("observerId") == "a-19-gotland-2022":
            t["targetId"] = TARGET
    # catalogRefs
    refs = d.setdefault("gauntlet", {}).setdefault("catalogRefs", [])
    if TARGET not in refs:
        refs.append(TARGET)
    # intent note (non-breaking)
    intent = d["gauntlet"].get("intent") or ""
    if "Multi-domain engage default" not in intent and "Buyan" not in intent:
        d["gauntlet"]["intent"] = intent + " [multi-domain: Visby→Sov, Gripen→Ster, Gotland→Buyan]"
    p.write_text(json.dumps(d, indent=2) + "\n")
    print("updated", p.name)
PY
```

- [ ] **Step 2: Verify pairing**

```bash
python3 <<'PY'
import json,glob
need=[]
for path in sorted(glob.glob("data/scenarios/gauntlet-t[345]-*.policy.json")):
    d=json.load(open(path))
    reds={u['unitId'] for u in d['gauntlet']['units'] if str(u.get('side','')).lower()=='red'}
    got=next((t['targetId'] for t in d.get('detection',[]) if t.get('observerId')=='a-19-gotland-2022'), None)
    ok = 'mrk-buyan-pr-21630-buyan-2007' in reds and got=='mrk-buyan-pr-21630-buyan-2007'
    print(('OK' if ok else 'FAIL'), d.get('id', path), 'gotland→', got, 'reds', sorted(reds))
    if not ok: need.append(path)
assert not need, need
PY
```

Expected: all OK.

- [ ] **Step 3: Expand multi-domain tests**

Modify `BalticReplayHarnessLadderMultiDomainTests.cs` — add TestCases for every `gauntlet-t3-*`…`t5-*` id (or Theory with static list). Keep air+sub True|Launched + distinct catalog reds. Optionally assert exact victims:

```csharp
// preferred: air→steregushchiy, sub→buyan (surface optional in same test)
Assert.That(airVictim, Is.EqualTo("mpk-steregushchiy-pr-20380-2018"));
Assert.That(subVictim, Is.EqualTo("mrk-buyan-pr-21630-buyan-2007"));
```

Start with full ladder list as TestCase attributes (12 policies + joint-orbat-smoke). If full matrix is slow, gate full list behind seeds/ticks 10 and keep fixture count ≤ 13.

Also update `IsCatalogRed` (or equivalent) in `BalticReplayHarnessGauntletTier35CatalogTests.cs` to include `mrk-buyan-pr-21630-buyan-2007`.

- [ ] **Step 4: Run multi-domain tests**

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~LadderMultiDomain|FullyQualifiedName~MultiDomainShooter|FullyQualifiedName~Tier35Catalog" \
  -c Release --nologo
```

Expected: all Passed. If a policy fails (e.g. ROE Tight blocks launch), fix that policy’s ROE/engage envelope or quarantine with documented skip **only** after root-cause note — prefer WeaponsFree for multi-domain launch proof except `t3-id-roe` / `t5-roe-change` where Tight is intentional (ensure engage still eventually allowed after ID/trigger).

- [ ] **Step 5: Commit checkpoint (optional)**

```bash
git add data/scenarios/gauntlet-t*.policy.json \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessLadderMultiDomainTests.cs \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessGauntletTier35CatalogTests.cs
git commit -m "feat(gauntlet): default multi-domain Buyan triple for all t3+ policies"
```

---

### Task 3: Batch recalibrate expects + oracle gate

**Files:**
- Modify: `gauntlet.expect` blocks inside touched `data/scenarios/gauntlet-*.policy.json`
- Create: `production/qa/gauntlet/gauntlet-YYYYMMDD-HHMM/oracle-expect-recalibration.json` (or under run dir)
- Create/update CSV under scratch or `production/qa/gauntlet/<RUN_ID>/`

**Interfaces:**
- Consumes: Demo `--batch` CSV columns matching `GauntletOracleEvaluator`
- Produces: expects that pass seeds 42,7,123

- [ ] **Step 1: Batch Slice 1 + Slice 2 critical set**

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
RUN_ID="gauntlet-$(date -u +%Y%m%d-%H%M)"
mkdir -p "production/qa/gauntlet/$RUN_ID"
SCENARIOS="gauntlet-t4-random-inject,gauntlet-t5-cascade,gauntlet-t5-roe-change,gauntlet-joint-orbat-smoke,gauntlet-t3-emcon-phases,gauntlet-t3-escort-strike,gauntlet-t3-event-chain,gauntlet-t3-id-roe,gauntlet-t4-asymm-roe,gauntlet-t4-multi-mission,gauntlet-t4-weighted,gauntlet-t5-dynamic-obj,gauntlet-t5-theater,gauntlet-multidomain-shooters"
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios "$SCENARIOS" \
  --seeds 42,7,123 \
  --ticks 10 \
  --csv-out "production/qa/gauntlet/$RUN_ID/results.csv"
```

- [ ] **Step 2: Spot-check fingerprints for injects**

```bash
# CSV or companion logs: ensure CommsStateChange appears for ladder inject trio
rg -n "CommsStateChange|PolicyUpdate" "production/qa/gauntlet/$RUN_ID/results.csv" || true
# If fingerprints are not in CSV columns, re-check via unit tests + single-run harness debug
```

- [ ] **Step 3: Oracle eval (may fail until recalibration)**

```bash
mkdir -p "production/qa/gauntlet/$RUN_ID/policies"
# copy only the scenarios in SCENARIOS
for id in ${SCENARIOS//,/ }; do
  cp "data/scenarios/${id}.policy.json" "production/qa/gauntlet/$RUN_ID/policies/"
done
dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy-dir "production/qa/gauntlet/$RUN_ID/policies" \
  --csv "production/qa/gauntlet/$RUN_ID/results.csv" \
  --out "production/qa/gauntlet/$RUN_ID/oracle-eval.json"
```

- [ ] **Step 4: Recalibrate failed expects**

For each failed scenario: compute min/max kills, missiles, denials, score across seeds; write new `gauntlet.expect` with small slack; log old→new in `oracle-expect-recalibration.json`. Re-run eval until `allPassed: true`.

Pattern:

```json
"expect": {
  "side": "BLUE",
  "requireNonEmptyFingerprint": true,
  "minKills": <obs_min>,
  "maxMissilesFired": <obs_max + slack>,
  "minDenials": <obs_min - slack or null>,
  "maxDenials": <obs_max + slack>,
  "minScore": <obs_min - slack>,
  "maxScore": <obs_max + slack>
}
```

- [ ] **Step 5: Confirm allPassed**

```bash
python3 -c "import json; d=json.load(open('production/qa/gauntlet/$RUN_ID/oracle-eval.json')); assert d.get('allPassed') is True, d; print('allPassed OK')"
```

---

### Task 4: Expand CI fixture set + local dry-run

**Files:**
- Modify: `.github/workflows/gauntlet-oracle.yml`

**Interfaces:**
- Consumes: Demo batch + `gauntlet_oracle_eval` (existing job steps)
- Produces: PR gate covering ladder inject + multidomain samples

- [ ] **Step 1: Expand staged policies**

In `gauntlet-oracle.yml` Stage step, add:

```yaml
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
```

Update `SCENARIOS=` comma list to match (ids only). Keep seed `42`, ticks `10`, fail-closed smoke unchanged.

- [ ] **Step 2: Local CI dry-run**

```bash
mkdir -p /tmp/gauntlet-oracle/policies
cp data/scenarios/gauntlet-multidomain-shooters.policy.json \
   data/scenarios/gauntlet-theater-inject.policy.json \
   data/scenarios/gauntlet-theater-dynamic-victory.policy.json \
   data/scenarios/gauntlet-t1-patrol-b.policy.json \
   data/scenarios/gauntlet-t4-random-inject.policy.json \
   data/scenarios/gauntlet-t5-cascade.policy.json \
   data/scenarios/gauntlet-t5-roe-change.policy.json \
   data/scenarios/gauntlet-joint-orbat-smoke.policy.json \
   data/scenarios/gauntlet-t3-emcon-phases.policy.json \
   /tmp/gauntlet-oracle/policies/
SCENARIOS="gauntlet-multidomain-shooters,gauntlet-theater-inject,gauntlet-theater-dynamic-victory,gauntlet-t1-patrol-b,gauntlet-t4-random-inject,gauntlet-t5-cascade,gauntlet-t5-roe-change,gauntlet-joint-orbat-smoke,gauntlet-t3-emcon-phases"
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios "$SCENARIOS" --seeds 42 --ticks 10 \
  --csv-out /tmp/gauntlet-oracle/results.csv
dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy-dir /tmp/gauntlet-oracle/policies \
  --csv /tmp/gauntlet-oracle/results.csv \
  --out /tmp/gauntlet-oracle/oracle-eval.json
python3 -c "import json; d=json.load(open('/tmp/gauntlet-oracle/oracle-eval.json')); assert d['allPassed'] is True, d"
```

Expected: allPassed true.

---

### Task 5: Suite verification + QA note + goal close

**Files:**
- Create: `production/qa/gauntlet-ladder-injects-multidomain-2026-07-13.md`
- Update: plan checkboxes; optional goal scratch if used

- [ ] **Step 1: Full relevant test filter**

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~LadderInject|FullyQualifiedName~LadderMultiDomain|FullyQualifiedName~MultiDomainShooter|FullyQualifiedName~TheaterPolicy|FullyQualifiedName~Tier35Catalog|FullyQualifiedName~GauntletOracle" \
  -c Release --nologo
```

Expected: Total tests N, Failed: 0.

- [ ] **Step 2: Write QA note** covering:

  - Slice 1: policies, real mid-run comms (and ROE for t5-roe-change), test names, batch seeds
  - Slice 2: Buyan default for all t3+, tests expanded, deconfliction note
  - Oracle allPassed path + CI policy list expansion
  - Explicit non-goals: synthetic cleanup, mass CMO scrape, full 5-tier tick matrix in GHA

- [ ] **Step 3: Commit**

```bash
git add data/scenarios/gauntlet-*.policy.json \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ \
  .github/workflows/gauntlet-oracle.yml \
  production/qa/gauntlet-ladder-injects-multidomain-2026-07-13.md \
  docs/superpowers/plans/2026-07-13-gauntlet-slice1-ladder-injects-slice2-multidomain.md
git status
# commit only when user approves message; suggested:
# feat(gauntlet): ladder inject proofs + multi-domain default for t3+
```

---

## Agent orchestration map (subagent-driven)

| Phase | Agent / skill | Parallel? | Scope |
|-------|---------------|-----------|-------|
| Task 1 | c-sharp-test-engineer / general-purpose | Sequential start | Ladder inject tests + policy fix |
| Task 2 | general-purpose (data) | **After** Task 1 if sharing t4/t5 inject policies; else can parallel with Task 1 if inject policies left to Task 1 for ROE cleanup only | Buyan roll-out + multi-domain tests |
| Task 3 | general-purpose (batch) | After Tasks 1–2 | Batch + expect recalibration |
| Task 4 | DevOps / general-purpose | After Task 3 expects stable | CI YAML + dry-run |
| Task 5 | se-technical-writer + verification-before-completion | After 1–4 | QA note + suite |

**Parallelism note:** Task 1 and Task 2 touch overlapping `gauntlet-t4-random-inject` / `t5-cascade` / `t5-roe-change` → **do not** parallel-edit those three. Safe parallel: Task 1 on inject tests + those three ROE fixes **while** Task 2 agent only edits the **other** 9 policies (exclude inject trio until merge). Then one agent applies Buyan to the inject trio.

**SDLC roles to invoke as needed:**

- `team-simulation` / `team-csharp` — if multi-domain launches fail on a ROE-tight policy
- `c-sharp-test-engineer` — test expansion
- `se-gitops-ci-specialist` / `github-actions-expert` — workflow only
- `qa-gauntlet` skill — acceptance language for note
- `verification-before-completion` — before any “done” claim
- `requesting-code-review` — after green suite, before merge PR

---

## Exit criteria (both goals)

### Slice 1 done when

- [x] Policies have real `comms[]` (already)
- [ ] Unit tests require mid-run `CommsStateChange` for t4-random-inject, t5-cascade, t5-roe-change
- [ ] t5-roe-change proves mid-run ROE `PolicyUpdate` with old≠new
- [ ] t5-cascade proves `Denied` at tick ≥ 4
- [ ] No hollow WeaponsFree→WeaponsFree “ROE inject” claim on t4/cascade
- [ ] Batch + oracle pass for the three (seeds 42,7,123 or CI seed 42)
- [ ] CI stages the three policies

### Slice 2 done when

- [ ] All `gauntlet-t3|t4|t5-*.policy.json` include Buyan red + Gotland→Buyan detection
- [ ] Multi-domain tests cover all t3+ ladder ids (air+sub True|Launched, distinct reds)
- [ ] `IsCatalogRed` includes Buyan
- [ ] Batch + oracle pass for expanded critical set
- [ ] CI includes at least `joint-orbat-smoke` + `t3-emcon-phases` (plus multidomain-shooters already)

### Dual goal closed when

- [ ] Tasks 1–5 complete, suite green, QA note written, local CI dry-run allPassed

---

## Out of scope

- ReplayGolden synthetic cleanup
- Mass CMO-DB scrape / new platform import
- Full 5-tier GHA matrix (ticks 6–40 × 3 seeds)
- Oracle schema v2 fingerprint token fields (unless trivial and requested)
- UAV/drone/swarm ORBAT (skill matrix aspirational)

---

## Self-review

1. **Spec coverage:** Slice 1 residual (tests, hollow ROE, batch, CI) and Slice 2 residual (Buyan default all t3+, tests, expects, CI, note) each map to tasks.
2. **No placeholders:** Commands, file paths, JSON templates, and test outlines are concrete.
3. **Type consistency:** Preferred hostile IDs and unitIds match joint-orbat-smoke reference.
