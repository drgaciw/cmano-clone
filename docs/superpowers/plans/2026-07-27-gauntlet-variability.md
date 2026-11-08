# QA Gauntlet Variability Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved spec `docs/superpowers/specs/2026-07-27-gauntlet-variability-design.md`: extend the gauntlet tier matrix with six new dimension rows (combat domains, EMCON, EW, logistics, contact lifecycle, weapons boundary), raise the fixed scenario budget from 4 to 6 per tier, add 9 forge recipes, and replace every EMCON "stand-in" in the corpus with the real scenario-level `emcon` block — closing `BUG-t2-escort-passive-emcon-claim-unimplemented` at the root.

**Architecture:** Data-only. No `ProjectAegis.Sim`/`ProjectAegis.Delegation` engine code changes. Work is: (1) two skill markdown files, (2) forge corpus YAML/JSON, (3) new and modified scenario policy JSON under `data/scenarios/`, (4) one new Python verification script and one new corpus JSON lookup table (neither touches `tools/qa-gauntlet/forge_scorecard.py`). Every scenario's `gauntlet.expect` numeric bounds are derived from a real `dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch` CSV at the scenario's tier tick budget, per `tools/qa-gauntlet/README-expect-regen.md` — never invented.

**Tech Stack:** .NET 8 (Demo batch harness, MissionEditor CLI `gauntlet_oracle_eval` — both used read-only, not modified), Python 3 (new verification script), YAML/JSON (recipe catalog, corpus index), Markdown (skill files).

## Global Constraints

- **Base branch.** This plan's dependencies (`.claude/skills/qa-gauntlet-forge/SKILL.md`, `production/qa/gauntlet/corpus/**`, `production/qa/bugs/BUG-t2-escort-passive-emcon-claim-unimplemented.md`, `production/qa/bugs/BUG-forge-scorecard-filename-vs-policy-id.md`) exist only on branch `07-27-qa_gauntlet_tier_2_strike_escort_air_timed_event_tight_roe_one_side_passive_emcon` (tip `db34bcb1`) as of 2026-07-27, not on `main` or `feat/platform-editor-uiux-productization`. Before Task 1, `git checkout`/`gt` onto that branch (or its eventual merge into `main` — check first with `git log main -- production/qa/gauntlet/corpus/index.yaml`; if present, work from `main` instead). All paths below are relative to repo root either way.
- **Do not touch `tools/qa-gauntlet/forge_scorecard.py`.** `BUG-forge-scorecard-filename-vs-policy-id` is being fixed elsewhere. It blocks forge *auto-promotion* only — it does not block anything in this plan, which edits the fixed/named corpus directly. Sync (`gt sync`) before Task 3 in case that fix has landed; do not re-fix it here.
- **Tier tick budgets (unchanged):** T1=6, T2=10, T3=16, T4=24, T5=40.
- **Never invent `gauntlet.expect` numeric bounds.** Every scenario task below writes the *structural* expect fields (side, `requireNonEmptyFingerprint`, `requireFingerprintSubstrings`, `requireTrueLaunchedShooters`) up front, then has an explicit follow-up step that runs the real batch + `gauntlet_oracle_eval` and fills numeric fields (`minKills`, `maxMissilesFired`, `minDenials`, `maxDenials`, `minScore`, `maxScore`) from the observed CSV. This is not a placeholder — it is the project's own mandatory two-phase discipline (`tools/qa-gauntlet/README-expect-regen.md`).
- **Catalog IDs only.** Every new scenario reuses platform IDs already proven to resolve against the catalog DB (drawn from `gauntlet-t2-escort-passive`, `gauntlet-t3-emcon-phases`, `gauntlet-t5-roe-change`, and run `gauntlet-20260727-1455`'s tier-1 CSV). No invented platform IDs.
- **Locked eval — never edit:** `src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs`, `src/ProjectAegis.Delegation.Demo/Program.cs` batch internals, ReplayGolden fixtures, `src/ProjectAegis.Delegation.UnityAdapter/Baltic/DelegationBridge.cs`, Baltic v2 golden hash `17144800277401907079`, `.github/workflows/gauntlet-oracle.yml`.
- **Graphite only.** `gt create` / `gt modify` for branch work; no raw `git push` or `gh pr create` per `CLAUDE.md`.
- **GitNexus.** This plan touches zero C# symbols. `impact()` is not required before any step below. `detect_changes({scope: "compare", base_ref: "main"})` is still run once at the end (Task 12) to confirm the diff is data/docs-only.
- **Commit convention:** `qa(gauntlet): <summary>` per existing corpus history (e.g. `qa(gauntlet): fix <defect-id> — <symbol> (tier N)`).
- **Collaborative Design Principle applies normally** — this plan is authored outside a live `/qa-gauntlet` run, so the skill's autonomy override does not apply. Ask "May I write this to [filepath]?" before each Write/Edit when executing, per `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md`.

## Key engine findings that shape this plan (source-verified, 2026-07-27)

These came from a source sweep of `src/ProjectAegis.Sim`, `src/ProjectAegis.Delegation`, and `data/glossary/abort_reason_manifest.json`, done specifically to make the "dimension-coverage assertion" mechanical rather than aspirational:

1. **Only two abort-code families are actually wired to the fingerprint.** `data/glossary/abort_reason_manifest.json` declares `Doctrine`, `Engage`, `Logistics`, `Sensor`, `Cyber` families, but only `Doctrine` (`ProjectAegis.Sim.Policy.FireAbortReason`) and `Engage` (`ProjectAegis.Sim.Engage.EngagementAbortReason`, resolved via `EngagementAbortReasonCodes.ToLogCode`, `src/ProjectAegis.Sim/Engage/EngagementAbortReasonCodes.cs:21`) have a real C# enum with call sites. `Logistics` and `Sensor` codes (`STRIKE_UNREACHABLE_FUEL`, `SENSOR_EMCON_BLOCKED`, `DATALINK_STALE`, `TRACK_STALE`) are declared but **zero call sites exist** — they never reach a fingerprint. Do not write a dimension-coverage check that expects them.
2. **EMCON has two independent gates, only one of which logs an abort code.** Engage-side: `EngageContext.RadarEmconActive` false → `EngagementAbortReason.EmconOff` → fingerprint token `EMCON_OFF` (confirmed via `src/ProjectAegis.Delegation.Tests/Decision/EngagementOrderLogContractTests.cs:44` and `BalticReplayHarnessPolicyEngageTests.cs:42`). Sensor-side: `DeterministicDetectionLoop.RollTick` (`src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs:99`) and `ScenarioContactSimulator.Tick` (`src/ProjectAegis.Sim/Sensors/ScenarioContactSimulator.cs:47`) both silently `continue` (skip the trial) when `RequiresActiveRadar` and the unit is Passive — **no log entry at all**. Retrofit scenarios where the passive unit is also the shooter (proof via `EMCON_OFF`); use a differential control-sibling only where the passive unit is a pure observer.
3. **Jammer Pd-reduction has no discrete log token** — `DetectionProbability.ComputePd` folds `jamStrength` continuously into the Pd calculation; there is no "jammed" abort code. Proof requires a paired control-sibling scenario (`jamStrength: 0`) and a CSV diff, not a fingerprint substring.
4. **Logistics fuel state (`FuelStateProjection.ResolveState` → `BINGO`/`JOKER`/`NOMINAL`) is UI-only** (`src/ProjectAegis.Delegation/Projection/UnitDetailProjection.cs` is its only consumer) — it never reaches the order log or fingerprint, and nothing in `ProjectAegis.Sim`/`ProjectAegis.Delegation` gates engagement or movement on fuel state today. Logistics-fuel-pressure **cannot** be proven mechanically from the batch CSV under current engine capability. See Task 11 for how this plan handles that honestly.
5. **Contact lifecycle and datalink share-lag are both real and single-run provable.** `ContactChange` fingerprint entries print the literal `ContactLifecycleState` enum names (`Unknown`, `Detected`, `Classified`, `Identified`, `Lost` — `src/ProjectAegis.Sim/Sensors/ContactLifecycleState.cs`), confirmed live in `results.csv` from run `gauntlet-20260727-1455`. `DatalinkSidePictureMerger` (`src/ProjectAegis.Sim/Sensors/DatalinkSidePictureMerger.cs`) genuinely re-emits a shared `ContactChange` for a non-organic peer, timestamped `shareLagTicks` ticks after the organic observer's own entry — provable by diffing the two `ContactChange` tick numbers in one fingerprint, no control-sibling needed.
6. **Combat-domain proof is already established convention.** `.claude/skills/qa-gauntlet/SKILL.md`'s "Joint ORBAT" section already requires `CATALOG_UNIT:{platformId}:{domain}` fingerprint tokens for multi-domain claims. `gauntlet.units[].domain` is a free string consumed only for this tagging — Land/Facility/Mine domains reuse the exact same mechanism.
7. **`engage.mountOnline`, `engage.maxSalvo`, `engage.combatDomain` are real, already-typed fields** on `ScenarioPolicyJsonDto` (`src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonDto.cs:378-389`), just never yet used in a committed `data/scenarios/gauntlet-*` file.
8. **`gauntlet.emcon`/`gauntlet.runId`/`gauntlet.tier`/`gauntlet.expect` are already silently ignored by the engine's scenario loader** — `ScenarioGauntletJsonDto` (`src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonDto.cs:66-75`) only declares `Intent`, `Oracle`, `CatalogRefs`, `Units`. This proves it is safe to add one more QA-only metadata field, `gauntlet.dimensionsClaimed`, without any engine change or DTO edit.

## File map

| File | Responsibility |
|---|---|
| `tools/qa-gauntlet/verify_dimension_coverage.py` | New. Reads a scenario's `gauntlet.dimensionsClaimed` + `corpus/dimension-coverage-signals.json`, checks structural proof (substring presence/absence, tick-delta, field-literal) against a real batch CSV/oracle-eval output. |
| `production/qa/gauntlet/corpus/dimension-coverage-signals.json` | New. The dimension → proof-method lookup table from the findings above. |
| `.claude/skills/qa-gauntlet/SKILL.md` | Modify. Tier matrix gains 6 rows, `--scenarios-per-tier` default 4→6, dimension-coverage assertion gate wired into Phase A2/C, corrected EMCON wording, `BUG-forge-scorecard-filename-vs-policy-id` prerequisite callout. |
| `production/qa/gauntlet/corpus/recipes/recipe-catalog.yaml` | Modify. +9 recipes. |
| `production/qa/gauntlet/corpus/recipes/recipe-weights.json` | Modify. +9 initial weights. |
| `production/qa/gauntlet/corpus/coverage-map.json` | Modify. +13 cells (3 retrofits + 10 new scenarios). |
| `production/qa/gauntlet/corpus/index.yaml` | Modify. +13 `promoted` entries. |
| `data/scenarios/gauntlet-t2-escort-passive.policy.json` | Modify (retrofit). Real `emcon` block, closes the named bug. |
| `data/scenarios/gauntlet-t3-emcon-phases.policy.json` | Modify (retrofit). Real static mixed `emcon` block, corrected intent. |
| `data/scenarios/gauntlet-t5-roe-change.policy.json` | Modify (retrofit). Real static `emcon` block, corrected intent. |
| `data/scenarios/gauntlet-t1-patrol-pkkill-boundary.policy.json` | New (T1). |
| `data/scenarios/gauntlet-t1-patrol-roe-tight-tight.policy.json` | New (T1). |
| `data/scenarios/gauntlet-t2-strike-salvo-boundary.policy.json` | New (T2). |
| `data/scenarios/gauntlet-t2-escort-air-domain-pairing.policy.json` | New (T2). |
| `data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure.policy.json` | New (T3). |
| `data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure-control.policy.json` | New (T3). Differential-proof control sibling (jamStrength=0). |
| `data/scenarios/gauntlet-t3-logistics-contact-lifecycle.policy.json` | New (T3). |
| `data/scenarios/gauntlet-t4-facility-strike-datalink.policy.json` | New (T4). |
| `data/scenarios/gauntlet-t4-ew-spoof-mount-offline.policy.json` | New (T4). |
| `data/scenarios/gauntlet-t5-mine-hazard-theater.policy.json` | New (T5). |
| `data/scenarios/gauntlet-t5-cascading-ew-logistics.policy.json` | New (T5). |
| `data/scenarios/gauntlet-t5-cascading-ew-logistics-control.policy.json` | New (T5). Differential-proof control sibling. |
| `production/qa/bugs/BUG-t2-escort-passive-emcon-claim-unimplemented.md` | Modify. Status → Fixed, cross-ref commit. |
| `docs/engineering/sim-capability-gap-backlog.md` | Modify. +GAP-13 (logistics fuel state has no runtime/fingerprint signal). |

---

### Task 1: Dimension-coverage signal map + verification script

**Files:**
- Create: `production/qa/gauntlet/corpus/dimension-coverage-signals.json`
- Create: `tools/qa-gauntlet/verify_dimension_coverage.py`

**Interfaces:**
- Produces: CLI `python3 tools/qa-gauntlet/verify_dimension_coverage.py --policy <path> --csv <results.csv> [--control-csv <path>]` → exit 0 (all claimed dimensions proven) or exit 1 (prints which claimed dimension failed and why). Consumed by every scenario task's regen step (Tasks 4–9) and referenced from `.claude/skills/qa-gauntlet/SKILL.md` (Task 2).
- Consumes: a scenario's `gauntlet.dimensionsClaimed: string[]` field (new convention, this task defines it) and its `gauntlet.expect.requireFingerprintSubstrings`/`requireTrueLaunchedShooters`.

- [ ] **Step 1: Write the signal map**

```json
{
  "version": 1,
  "updated": "2026-07-27",
  "note": "Proof method per gauntlet.dimensionsClaimed entry. Source-verified against src/ProjectAegis.Sim and src/ProjectAegis.Delegation on 2026-07-27 — see docs/superpowers/plans/2026-07-27-gauntlet-variability.md 'Key engine findings'.",
  "signals": {
    "combat-domain": {
      "method": "fingerprint-substring-per-value",
      "template": "CATALOG_UNIT:{platformId}:{domain}",
      "note": "One required substring per non-surface domain claimed in gauntlet.units[].domain."
    },
    "weapons-boundary-pkkill-1": {
      "method": "field-literal-and-negative-substring",
      "field": "engage.pkKill",
      "equals": 1.0,
      "forbiddenOutcomeSuffix": "|Hit|",
      "note": "Every resolved hit must be a Kill; a bare Hit outcome for this scenario's shooters means pkKill=1.0 is not actually gating kill resolution."
    },
    "weapons-boundary-salvo-cap": {
      "method": "field-literal-and-forbidden-substring",
      "fieldsEqual": ["engage.salvoSize", "engage.maxSalvo"],
      "forbiddenSubstrings": ["WRA_SALVO"],
      "note": "salvoSize == maxSalvo must still launch (cap check is > not >=)."
    },
    "weapons-boundary-magazine-exhaustion": {
      "method": "fingerprint-substring",
      "substrings": ["NO_AMMO"]
    },
    "weapons-boundary-mount-offline": {
      "method": "fingerprint-substring",
      "substrings": ["MOUNT_OFFLINE"]
    },
    "weapons-boundary-never-kill": {
      "method": "field-literal-and-negative-substring",
      "fieldsEqual": ["engage.envelopeMinMeters", "engage.envelopeMaxMeters"],
      "field": "engage.pkKill",
      "equals": 0.0,
      "forbiddenOutcomeSuffix": "|Kill|"
    },
    "emcon-engage-block": {
      "method": "fingerprint-substring",
      "substrings": ["EMCON_OFF"],
      "note": "Passive unit must itself attempt to engage; if it is a pure observer, use emcon-sensor-block instead."
    },
    "emcon-sensor-block": {
      "method": "differential-control-sibling",
      "compare": "denials",
      "note": "No engine log token exists for detection-side EMCON gating (silent skip in DeterministicDetectionLoop/ScenarioContactSimulator). Requires a *-control sibling scenario with radar: Active and a CSV diff."
    },
    "ew-jammer-timing": {
      "method": "differential-control-sibling",
      "compare": "denials-or-kills",
      "note": "DetectionProbability.ComputePd folds jamStrength continuously; no discrete abort token exists. Requires a *-control sibling with jamStrength: 0."
    },
    "ew-spoof-inject": {
      "method": "fingerprint-substring",
      "substrings": ["CYBER_SPOOF_TRACK"]
    },
    "contact-lifecycle-timing": {
      "method": "fingerprint-substring-any",
      "substrings": ["Classified", "Identified"]
    },
    "datalink-degradation": {
      "method": "tick-delta-single-run",
      "note": "Find two ContactChange entries for the same contactId, different unitIds (organic vs shared peer); the peer's tick must equal the organic tick + shareLagTicks."
    },
    "logistics-fuel-pressure": {
      "method": "config-only-no-runtime-signal",
      "note": "FuelStateProjection.ResolveState is UI-only (src/ProjectAegis.Delegation/Projection/UnitDetailProjection.cs); no order-log/fingerprint emission and no engagement gating exists yet. Proof is limited to ScenarioLogisticsSettings constructor invariants holding at scenario load (bingo <= joker fraction, capacity/burn > 0). See docs/engineering/sim-capability-gap-backlog.md GAP-13."
    },
    "mine-hazard-zone": {
      "method": "fingerprint-substring",
      "substrings": ["MINE_ASPECT_BLOCK"]
    }
  }
}
```

- [ ] **Step 2: Write the verification script**

```python
#!/usr/bin/env python3
"""Verify a gauntlet scenario's gauntlet.dimensionsClaimed against real batch evidence.

Does NOT touch tools/qa-gauntlet/forge_scorecard.py or any locked-eval C#. Reads
a scenario policy JSON, its gauntlet.dimensionsClaimed list, the corpus signal
map, and a results.csv (plus an optional control-sibling CSV for differential
proofs), and fails closed if a claimed dimension has no evidence.

Usage:
  python3 tools/qa-gauntlet/verify_dimension_coverage.py \
    --policy data/scenarios/gauntlet-t2-escort-passive.policy.json \
    --csv /tmp/gauntlet-t2/results.csv \
    [--control-csv /tmp/gauntlet-t2/results-control.csv]

Exit 0: every claimed dimension proven. Exit 1: at least one is not proven
(prints which dimension and why).
"""
import argparse
import csv
import json
import sys
from pathlib import Path

SIGNAL_MAP_PATH = Path("production/qa/gauntlet/corpus/dimension-coverage-signals.json")


def load_json(path: str) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def read_rows(csv_path: str, scenario_id: str) -> list[dict]:
    rows = []
    with open(csv_path, "r", encoding="utf-8", newline="") as f:
        for row in csv.DictReader(f):
            if row.get("scenarioId") == scenario_id:
                rows.append(row)
    return rows


def get_field(policy: dict, dotted: str):
    node = policy
    for part in dotted.split("."):
        if not isinstance(node, dict) or part not in node:
            return None
        node = node[part]
    return node


def check_fingerprint_substring(rows: list[dict], substrings: list[str], mode: str) -> tuple[bool, str]:
    if not rows:
        return False, "no CSV rows for this scenario id"
    for row in rows:
        fp = row.get("fingerprint", "")
        present = [s for s in substrings if s in fp]
        if mode == "any" and present:
            continue
        if mode == "all" and len(present) == len(substrings):
            continue
        return False, f"seed {row.get('seed')}: missing required substrings {substrings} (mode={mode})"
    return True, "ok"


def check_forbidden_substring(rows: list[dict], forbidden: list[str]) -> tuple[bool, str]:
    for row in rows:
        fp = row.get("fingerprint", "")
        for s in forbidden:
            if s in fp:
                return False, f"seed {row.get('seed')}: forbidden substring '{s}' present"
    return True, "ok"


def check_field_literal(policy: dict, field: str, equals) -> tuple[bool, str]:
    val = get_field(policy, field)
    if val != equals:
        return False, f"{field} == {val!r}, expected {equals!r}"
    return True, "ok"


def check_fields_equal(policy: dict, fields: list[str]) -> tuple[bool, str]:
    vals = [get_field(policy, f) for f in fields]
    if len(set(vals)) != 1 or vals[0] is None:
        return False, f"fields {fields} not all equal / present: {vals}"
    return True, "ok"


def check_negative_outcome_suffix(rows: list[dict], shooters: list[str], forbidden_suffix: str) -> tuple[bool, str]:
    for row in rows:
        for token in row.get("fingerprint", "").split():
            if not token.startswith("EngagementOutcome|"):
                continue
            if token.endswith(forbidden_suffix):
                return False, f"seed {row.get('seed')}: forbidden outcome suffix '{forbidden_suffix}' in '{token}'"
    return True, "ok"


def check_tick_delta(rows: list[dict], expected_lag: int) -> tuple[bool, str]:
    for row in rows:
        contacts: dict[str, list[tuple[int, str]]] = {}
        for token in row.get("fingerprint", "").split():
            parts = token.split("|")
            if parts[0] != "ContactChange" or len(parts) < 6:
                continue
            tick = int(parts[1])
            unit_id = parts[3]
            contact_id = parts[4]
            contacts.setdefault(contact_id, []).append((tick, unit_id))
        found = False
        for contact_id, entries in contacts.items():
            entries.sort()
            if len(entries) < 2:
                continue
            organic_tick, organic_unit = entries[0]
            for tick, unit_id in entries[1:]:
                if unit_id != organic_unit and tick - organic_tick == expected_lag:
                    found = True
        if not found:
            return False, f"seed {row.get('seed')}: no shared ContactChange found at organic tick + {expected_lag}"
    return True, "ok"


def check_differential(rows: list[dict], control_rows: list[dict], compare: str) -> tuple[bool, str]:
    if not control_rows:
        return False, "no --control-csv rows supplied for a differential-control-sibling dimension"
    real_denials = sum(int(r.get("denials", 0)) for r in rows)
    control_denials = sum(int(r.get("denials", 0)) for r in control_rows)
    real_kills = sum(int(r.get("kills", 0)) for r in rows)
    control_kills = sum(int(r.get("kills", 0)) for r in control_rows)
    if compare == "denials" and real_denials <= control_denials:
        return False, f"real denials ({real_denials}) not greater than control ({control_denials})"
    if compare == "denials-or-kills" and real_denials <= control_denials and real_kills >= control_kills:
        return False, (
            f"neither denials increased ({real_denials} vs {control_denials}) "
            f"nor kills decreased ({real_kills} vs {control_kills})"
        )
    return True, "ok"


def verify(policy: dict, rows: list[dict], control_rows: list[dict], signals: dict) -> list[tuple[str, bool, str]]:
    results = []
    claimed = policy.get("gauntlet", {}).get("dimensionsClaimed", [])
    for dim in claimed:
        sig = signals["signals"].get(dim)
        if sig is None:
            results.append((dim, False, "unknown dimension key — not in dimension-coverage-signals.json"))
            continue
        method = sig["method"]
        if method == "fingerprint-substring":
            ok, why = check_fingerprint_substring(rows, sig["substrings"], "all")
        elif method == "fingerprint-substring-any":
            ok, why = check_fingerprint_substring(rows, sig["substrings"], "any")
        elif method == "fingerprint-substring-per-value":
            domains = [u.get("domain") for u in policy.get("gauntlet", {}).get("units", [])
                       if u.get("domain") not in (None, "surface")]
            needed = [sig["template"].format(platformId=u["platformId"], domain=u["domain"])
                      for u in policy.get("gauntlet", {}).get("units", []) if u.get("domain") in domains]
            ok, why = check_fingerprint_substring(rows, needed, "all") if needed else (True, "no non-surface domain units")
        elif method == "field-literal-and-negative-substring":
            ok, why = check_field_literal(policy, sig["field"], sig["equals"])
            if ok:
                shooters = [u["platformId"] for u in policy.get("gauntlet", {}).get("units", []) if u.get("side") == "blue"]
                ok, why = check_negative_outcome_suffix(rows, shooters, sig["forbiddenOutcomeSuffix"])
        elif method == "field-literal-and-forbidden-substring":
            ok, why = check_fields_equal(policy, sig["fieldsEqual"])
            if ok:
                ok, why = check_forbidden_substring(rows, sig["forbiddenSubstrings"])
        elif method == "tick-delta-single-run":
            lag = get_field(policy, "datalink.shareLagTicks")
            ok, why = check_tick_delta(rows, int(lag)) if lag is not None else (False, "datalink.shareLagTicks missing")
        elif method == "differential-control-sibling":
            ok, why = check_differential(rows, control_rows, sig["compare"])
        elif method == "config-only-no-runtime-signal":
            ok, why = True, "config-only dimension (see sim-capability-gap-backlog.md GAP-13) — schema validity checked at scenario load, not here"
        else:
            ok, why = False, f"unhandled method '{method}'"
        results.append((dim, ok, why))
    return results


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--policy", required=True)
    ap.add_argument("--csv", required=True)
    ap.add_argument("--control-csv", default=None)
    args = ap.parse_args()

    policy = load_json(args.policy)
    signals = load_json(str(SIGNAL_MAP_PATH))
    scenario_id = policy["id"]
    rows = read_rows(args.csv, scenario_id)
    control_rows = read_rows(args.control_csv, policy.get("gauntlet", {}).get("controlOf", scenario_id)) if args.control_csv else []

    results = verify(policy, rows, control_rows, signals)
    failed = [r for r in results if not r[1]]
    for dim, ok, why in results:
        print(f"{'PASS' if ok else 'FAIL'} {dim}: {why}")
    if failed:
        print(f"\n{len(failed)} of {len(results)} claimed dimension(s) not proven.")
        return 1
    print(f"\nAll {len(results)} claimed dimension(s) proven.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 3: Smoke-test the script against an existing scenario (no dimension claims yet — expect trivial pass)**

```bash
python3 -c "
import json
p = json.load(open('data/scenarios/gauntlet-t1-patrol-a.policy.json'))
p.setdefault('gauntlet', {})['dimensionsClaimed'] = []
open('/tmp/smoke-t1-patrol-a.json', 'w').write(json.dumps(p))
"
python3 tools/qa-gauntlet/verify_dimension_coverage.py \
  --policy /tmp/smoke-t1-patrol-a.json --csv /dev/null 2>&1 || true
```

Expected: `All 0 claimed dimension(s) proven.` (empty `dimensionsClaimed` always passes trivially; this only confirms the script loads its own signal map and parses a real policy file without crashing.)

- [ ] **Step 4: Commit**

```bash
git add tools/qa-gauntlet/verify_dimension_coverage.py production/qa/gauntlet/corpus/dimension-coverage-signals.json
git commit -m "qa(gauntlet): add dimension-coverage verification tool and signal map

Mechanical, source-grounded proof methods per claimed dimension (fingerprint
substring, field literal, tick-delta, or differential control-sibling).
Does not touch forge_scorecard.py."
```

---

### Task 2: Update `.claude/skills/qa-gauntlet/SKILL.md` — tier matrix, scenario budget, wiring

**Files:**
- Modify: `.claude/skills/qa-gauntlet/SKILL.md`

**Interfaces:**
- Consumes: `production/qa/gauntlet/corpus/dimension-coverage-signals.json` (Task 1), `verify_dimension_coverage.py` (Task 1).
- Produces: the matrix table every later task's scenarios must conform to.

- [ ] **Step 1: Replace `argument-hint` and the `--scenarios-per-tier` default**

Find:
```
argument-hint: "[--tiers N=5] [--scenarios-per-tier N=4] [--seeds 42,7,123] [--max-fix-attempts 3] [--resume <run-id>]"
```
Replace with:
```
argument-hint: "[--tiers N=5] [--scenarios-per-tier N=6] [--seeds 42,7,123] [--max-fix-attempts 3] [--resume <run-id>]"
```

Find the flags table row:
```
| `--scenarios-per-tier` | `4` | Scenarios generated per tier |
```
Replace with:
```
| `--scenarios-per-tier` | `6` | Scenarios generated per tier (raised from 4 — see docs/superpowers/specs/2026-07-27-gauntlet-variability-design.md; kept at the low end of the spec's 6–8 range to bound run-time growth, per that spec's own Risks section) |
```

Find:
```
Generate `--scenarios-per-tier` scenarios per tier (default 4). Every scenario is
```
Replace with:
```
Generate `--scenarios-per-tier` scenarios per tier (default 6). Every scenario is
```

- [ ] **Step 2: Replace the tier matrix table**

Find the existing table (starts `| Dim | Tier 1 | Tier 2 | Tier 3 | Tier 4 | Tier 5 |`) and replace the entire table with:

```markdown
| Dim | Tier 1 | Tier 2 | Tier 3 | Tier 4 | Tier 5 |
|---|---|---|---|---|---|
| **Mission type** | Single patrol | Strike OR escort | Escort + strike combined | ASW/AAW multi-mission | Multi-domain theater op (patrol+strike+escort+ASW concurrent) |
| **Platform mix** | 3 surface units/side (~6) | 3 surface + 1 air / side (~8) | 3 surface + 2 air + 1 sub / side (~12) | 3–4 surface + 2 air + 1–2 sub / side (~14) | 4 surface + 3 air + 2 sub blue vs dense red (~16); asymmetric joint mix |
| **Victory conditions** | Survive N ticks | Destroy designated target | Protect HVU + destroy target | Weighted multi-objective scoring | Conditional/dynamic objectives that change on trigger |
| **Events** | None | 1 scripted timed event | Timed event chain | Random injects (seeded) | Cascading adversarial injects (comms loss, sensor degradation, reinforcements) |
| **ROE** | Weapons free, both sides | Weapons tight one side | ID-required engagement criteria | Asymmetric per-side ROE + escalation rules | Mid-mission ROE changes via event |
| **Combat domains** | Surface | + Air (AAW, air→surface strike) | + Subsurface (ASW) | + Land / Facility (approximated via `engage.combatDomain` on a catalog surface unit — no literal facility platform exists in the catalog; see intent annotations) | + Mine; multiple domains concurrent |
| **EMCON** | Unrestricted emissions | Real `emcon` block, one unit Passive (engage-side gate, proven via `EMCON_OFF` fingerprint token — not the old basePd/envMask stand-in) | Static per-unit mixed EMCON posture across the ORBAT (**not** scheduled — `ScenarioEmconJsonDto` has no per-tick phase field and no runtime EMCON toggle exists; "timed phases" in the original spec wording is corrected here to what the engine can actually prove) | Static asymmetric EMCON: one side Active, one side Passive, differentiated by domain (**not** "dynamic … on detection" — no such runtime hook exists) | Contested EM: one side maintains Passive discipline throughout (**not** "deception emitters" — soft-kill/decoy modelling is ABSENT per `docs/engineering/sim-capability-gap-backlog.md` GAP-07); scored via `EMCON_OFF` gate frequency |
| **EW** | None | None | `jammers` with `activeFromTick`, proven via a differential control-sibling scenario (no discrete jam abort token exists — see `dimension-coverage-signals.json`) | + `spoofTracks` injects, proven via `CYBER_SPOOF_TRACK` fingerprint token | Cascading jam + spoof together |
| **Logistics** | None | None | `logistics` block present, thresholds internally consistent (`ScenarioLogisticsSettings` constructor invariants) | Joker/bingo fractions escalated | Bingo + magazine exhaustion combined |
| **Contact lifecycle** | None | None | `contactLifecycle` classify/identify gates, proven via `Classified`/`Identified` fingerprint literals | + `datalink` share-lag, proven via tick-delta between organic and shared `ContactChange` entries | + `datalink.organicOnly` harsher setting |
| **Weapons boundary** | `pkKill=1.0` exactly | `salvoSize==maxSalvo` exactly | Magazine exhaustion (`NO_AMMO` token) | `mountOnline=false` (`MOUNT_OFFLINE` token) | Zero-width envelope (`envelopeMin==envelopeMax`) and `pkKill=0.0` |

> **Logistics has no runtime/fingerprint signal today.** `FuelStateProjection` (fuel-state readout) is UI-only and nothing gates engagement on it. The Logistics row above is honest about this — the fixed scenarios exercise the block for schema/config correctness, not for a provable in-run behavioral effect. See `docs/engineering/sim-capability-gap-backlog.md` GAP-13 for the follow-up (an engine change, out of scope here).
```

- [ ] **Step 3: Add the dimension-coverage assertion gate to Phase A2 and Phase C**

Find:
```
Invalid scenario → send back to the architect with the validator output, max 2
regeneration attempts, then drop it and log why.
```
Insert immediately after it:
```
**Dimension-coverage assertion (required, A2 and C).** Every scenario's
`gauntlet.dimensionsClaimed` array must name only dimensions it demonstrably
exercises — this is the direct lesson of `BUG-t2-escort-passive-emcon-claim-unimplemented`.
After Phase B produces `results.csv` (and any `-control` sibling's CSV), run:

```bash
python3 tools/qa-gauntlet/verify_dimension_coverage.py \
  --policy production/qa/gauntlet/<RUN_ID>/tier-N/<scenario>.policy.json \
  --csv production/qa/gauntlet/<RUN_ID>/tier-N/results.csv \
  [--control-csv production/qa/gauntlet/<RUN_ID>/tier-N/results-control.csv]
```

Exit 1 → the scenario claims a dimension it does not prove. Either fix the
scenario (add the missing trigger/config) or remove the unproven entry from
`dimensionsClaimed` — do not weaken the check. Proof methods per dimension live
in `production/qa/gauntlet/corpus/dimension-coverage-signals.json`.
```

- [ ] **Step 4: Add the forge-scorecard prerequisite callout**

Find the "Forge post-oracle (required)" subsection under Phase C and insert immediately before it:
```
> **Prerequisite for the new EW/EMCON/datalink/mine forge recipes to pay off:**
> `BUG-forge-scorecard-filename-vs-policy-id` (`tools/qa-gauntlet/forge_scorecard.py`)
> currently blocks all forge promotion regardless of candidate quality. That fix
> is tracked and being made elsewhere — do not re-fix it as part of gauntlet
> variability work. Until it lands, forge candidates using the new recipes will
> still draft and score correctly; only the auto-promote step is affected.
```

- [ ] **Step 5: Verify the file is still valid Markdown / frontmatter-parseable**

```bash
python3 -c "
import re
text = open('.claude/skills/qa-gauntlet/SKILL.md').read()
assert text.startswith('---'), 'frontmatter missing'
assert '--scenarios-per-tier N=6' in text
assert 'verify_dimension_coverage.py' in text
assert 'BUG-forge-scorecard-filename-vs-policy-id' in text
print('SKILL.md structural checks OK')
"
```
Expected: `SKILL.md structural checks OK`

- [ ] **Step 6: Commit**

```bash
git add .claude/skills/qa-gauntlet/SKILL.md
git commit -m "qa(gauntlet): extend tier matrix with 6 new dimension rows, raise scenario budget to 6/tier

Adds combat domains, EMCON (corrected to what the engine can prove — no
scheduling/deception-emitter claims), EW, logistics, contact lifecycle, and
weapons-boundary rows. Wires the dimension-coverage assertion into Phase A2/C.
Notes BUG-forge-scorecard-filename-vs-policy-id as an external prerequisite,
not fixed here."
```

---

### Task 3: Add 9 forge recipes to `recipe-catalog.yaml` and `recipe-weights.json`

**Files:**
- Modify: `production/qa/gauntlet/corpus/recipes/recipe-catalog.yaml`
- Modify: `production/qa/gauntlet/corpus/recipes/recipe-weights.json`

**Interfaces:**
- Consumes: none (pure data).
- Produces: recipe `id`s referenced by future forge `a0` candidate drafting (out of scope here — this task only adds the catalog entries).

- [ ] **Step 1: Append 9 recipes to `recipe-catalog.yaml`**

Add to the end of the `recipes:` list (after `hard-case-replay`), preserving the existing 2-space indentation:

```yaml
  - id: emcon-real-block
    dims: [emcon]
    tierMin: 2
    preconditions: ["catalog-roster-available"]
    noveltyTags: [emcon-real-block, engage-side-gate]
    forbiddenTouches: [gauntlet.expect-without-regen]
    description: >
      Mutate per-unit emcon.units[].radar between Active/Passive using the real
      scenario-level emcon block (never the old basePd/envMask stand-in).
      Proven via EMCON_OFF fingerprint token when the passive unit is also a
      shooter; otherwise requires a *-control sibling per
      dimension-coverage-signals.json.

  - id: domain-pairing-shift
    dims: [platform, orbat, mission]
    tierMin: 2
    preconditions: ["tier-roster"]
    noveltyTags: [domain-pairing, orbat-mix]
    forbiddenTouches: [gauntlet.expect-without-regen]
    description: >
      Mutate engage.combatDomain plus which unit domains (surface/air/subsurface/
      land/facility/mine) participate, keeping gauntlet.units[].domain consistent
      with CATALOG_UNIT:{platformId}:{domain} fingerprint proof.

  - id: ew-jammer-timing
    dims: [ew]
    tierMin: 3
    preconditions: ["detection-array"]
    noveltyTags: [jammer, timed]
    forbiddenTouches: [gauntlet.expect-without-regen, sim-rng]
    description: >
      Mutate jammers[].jamStrength / activeFromTick / observer-target pairing.
      No discrete abort token exists for jam-driven Pd reduction — every
      candidate using this recipe must ship with a *-control sibling
      (jamStrength: 0) for the differential-control-sibling proof.

  - id: logistics-fuel-pressure
    dims: [logistics]
    tierMin: 3
    preconditions: []
    noveltyTags: [fuel-pressure, joker-bingo]
    forbiddenTouches: [gauntlet.expect-without-regen]
    description: >
      Mutate logistics burn rate / capacity / joker-bingo fractions. Config-only
      proof today (no runtime/fingerprint signal — see
      docs/engineering/sim-capability-gap-backlog.md GAP-13); ScenarioLogisticsSettings
      constructor invariants must still hold (bingo <= joker fraction, capacity/burn > 0).

  - id: contact-lifecycle-timing
    dims: [contactLifecycle]
    tierMin: 3
    preconditions: []
    noveltyTags: [classify, identify, stale]
    forbiddenTouches: [gauntlet.expect-without-regen]
    description: >
      Mutate contactLifecycle.classifyAfterTicks / identifyAfterTicks /
      staleThresholdTicks so Classified/Identified transitions land within the
      tier's tick budget. Proven via Classified/Identified fingerprint literals.

  - id: datalink-degradation
    dims: [datalink]
    tierMin: 4
    preconditions: ["multi-unit-blue"]
    noveltyTags: [share-lag, organic-only]
    forbiddenTouches: [gauntlet.expect-without-regen]
    description: >
      Mutate datalink.organicOnly / shareLagTicks across ≥2 same-side units.
      Proven via single-run tick-delta: the shared peer's ContactChange for a
      contact must land exactly shareLagTicks after the organic observer's own
      ContactChange for the same contactId.

  - id: mine-hazard-zone
    dims: [mineHazard]
    tierMin: 5
    preconditions: ["catalog-roster-available"]
    noveltyTags: [mine, transit-hazard]
    forbiddenTouches: [gauntlet.expect-without-regen, CatalogWriteGate]
    description: >
      Mutate mineHazard zone bounds / severity / mine placements and the
      transiting catalog platformId. Proven via MINE_ASPECT_BLOCK fingerprint
      token when an engagement inside a mine's trigger radius is attempted.

  - id: weapons-boundary-probe
    dims: [weapons]
    tierMin: 1
    preconditions: []
    noveltyTags: [boundary, wra-cap, pk-extreme]
    forbiddenTouches: [gauntlet.expect-without-regen]
    description: >
      Pin one of the per-tier boundary conditions from the revised tier matrix
      (pkKill=1.0, salvoSize==maxSalvo, mountOnline=false, envelopeMin==envelopeMax,
      pkKill=0.0) exactly, never an out-of-range invalid value.
```

- [ ] **Step 2: Add matching entries to `recipe-weights.json`**

Edit the `"weights"` object, inserting the 9 new keys before the closing brace (after `"hard-case-replay": 1.7192,` and before `"bootstrap-seed": 0.5`):

```json
    "emcon-real-block": 1.1,
    "domain-pairing-shift": 1.0,
    "ew-jammer-timing": 1.0,
    "logistics-fuel-pressure": 1.0,
    "contact-lifecycle-timing": 1.0,
    "datalink-degradation": 1.0,
    "mine-hazard-zone": 1.0,
    "weapons-boundary-probe": 1.1,
```

Full resulting `"weights"` object (for verification, not re-typing from scratch):

```json
  "weights": {
    "platform-swap-underused": 1.2,
    "orbat-asymmetric-ratio": 1.15,
    "mission-combo-escort-strike": 1.15,
    "mission-concurrent-asw-aaw": 1.0,
    "victory-weighted-multi": 1.0,
    "victory-trigger-conditional": 1.0,
    "event-timed-chain": 1.0,
    "event-seeded-random-inject": 1.0,
    "event-cascading-adversarial": 1.0,
    "roe-asymmetric-per-side": 1.1,
    "roe-mid-mission-change": 1.1,
    "emcon-timed-phases": 1.1,
    "emcon-contested-deception": 1.0,
    "geometry-detection-lane-shift": 1.0,
    "trait-attention-overload": 1.0,
    "hard-case-replay": 1.7192,
    "emcon-real-block": 1.1,
    "domain-pairing-shift": 1.0,
    "ew-jammer-timing": 1.0,
    "logistics-fuel-pressure": 1.0,
    "contact-lifecycle-timing": 1.0,
    "datalink-degradation": 1.0,
    "mine-hazard-zone": 1.0,
    "weapons-boundary-probe": 1.1,
    "bootstrap-seed": 0.5
  }
```

Also update the top-level `"updated"` field to `"2026-07-27"`.

- [ ] **Step 3: Validate both files parse**

```bash
python3 -c "import yaml; d = yaml.safe_load(open('production/qa/gauntlet/corpus/recipes/recipe-catalog.yaml')); ids = [r['id'] for r in d['recipes']]; assert len(ids) == len(set(ids)), 'duplicate recipe id'; print(len(ids), 'recipes, all unique ids')"
python3 -c "import json; d = json.load(open('production/qa/gauntlet/corpus/recipes/recipe-weights.json')); print(len(d['weights']), 'weight entries')"
```
Expected: `25 recipes, all unique ids` and `25 weight entries` (16 existing + 9 new).

- [ ] **Step 4: Commit**

```bash
git add production/qa/gauntlet/corpus/recipes/recipe-catalog.yaml production/qa/gauntlet/corpus/recipes/recipe-weights.json
git commit -m "qa(forge): add 9 recipes for combat-domain, EMCON, EW, logistics, contact-lifecycle, datalink, mine-hazard, and weapons-boundary variance

Per docs/superpowers/specs/2026-07-27-gauntlet-variability-design.md 'Forge
recipe additions' table. tierMin values match the spec exactly."
```

---

### Task 4: Retrofit the EMCON stand-in sweep (3 scenarios) — closes `BUG-t2-escort-passive-emcon-claim-unimplemented`

A full sweep of `data/scenarios/gauntlet-*.policy.json` for `"emcon": "<prose>"` (vocabulary-only, inside the `gauntlet` block) versus a real top-level `"emcon": {...}` block found exactly 3 offenders: `gauntlet-t2-escort-passive`, `gauntlet-t3-emcon-phases`, `gauntlet-t5-roe-change`. All three are fixed here — the bug report only names the first, but the same defect class applies to all three, so this task fixes all of them in one pass rather than reopening the sweep later.

**Files:**
- Modify: `data/scenarios/gauntlet-t2-escort-passive.policy.json`
- Modify: `data/scenarios/gauntlet-t3-emcon-phases.policy.json`
- Modify: `data/scenarios/gauntlet-t5-roe-change.policy.json`
- Modify: `production/qa/bugs/BUG-t2-escort-passive-emcon-claim-unimplemented.md`

**Interfaces:**
- Consumes: `verify_dimension_coverage.py` (Task 1).
- Produces: none consumed by later tasks (each retrofit is self-contained).

- [ ] **Step 1: Retrofit `gauntlet-t2-escort-passive.policy.json`**

Replace the file's full contents:

```json
{
  "friendlyRoe": "WeaponsTight",
  "opposingRoe": "WeaponsFree",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 5000,
    "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 4,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.2,
    "salvoSize": 1
  },
  "emcon": {
    "units": {
      "k-31-visby-2009": { "radar": "Passive" }
    }
  },
  "detection": [
    {
      "observerId": "k-31-visby-2009",
      "sensorId": "radar-1",
      "targetId": "em-sovremenny-i-pr-956-sarych",
      "contactId": "c1",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    },
    {
      "observerId": "k-31-visby-2009",
      "sensorId": "esm-1",
      "targetId": "em-sovremenny-i-pr-956-sarych",
      "contactId": "c1-esm",
      "basePd": 0.4,
      "envMask": 1.0,
      "jamStrength": 0,
      "requiresActiveRadar": false
    }
  ],
  "gauntlet": {
    "intent": "Escort under real EMCON discipline: k-31-visby-2009 runs radar Passive, blocking its own radar-1 detection AND engage attempts via the genuine emcon block (not the old basePd/envMask stand-in); an ESM sensor (requiresActiveRadar: false) keeps the escort minimally viable while Passive [catalog ORBAT: Visby vs Sovremenny]",
    "oracle": "k-31-visby-2009 aborts engage attempts with EMCON_OFF while Passive; ESM-only contact acquisition is degraded but non-zero.",
    "dimensionsClaimed": ["emcon-engage-block"],
    "catalogRefs": [
      "241-slazak-meko-100-mod-2019",
      "421-orkan-pr-660-2015",
      "624-pr-205p-tarantul-1992",
      "70-rauma-helsinki-ii-1990",
      "80-hamina-rauma-2-2006",
      "bpk-marshal-shaposhnikov-udaloy-i-pr-1155-fregat",
      "em-sovremenny-i-pr-956-sarych",
      "f-207-bremen-type-f122-1982",
      "f-35a-lightning-ii",
      "f-a-18c-hornet-f-18c",
      "hkp-14f-nh90-ttt",
      "jas-39a-gripen-1997",
      "jas-39b-gripen-1997",
      "jas-39c-gripen-2005",
      "k-31-visby-2009"
    ],
    "emcon": "engage-side-passive",
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 2,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["EMCON_OFF"]
    },
    "units": [
      {
        "unitId": "k-31-visby-2009",
        "platformId": "k-31-visby-2009",
        "domain": "surface",
        "side": "blue"
      },
      {
        "unitId": "em-sovremenny-i-pr-956-sarych",
        "platformId": "em-sovremenny-i-pr-956-sarych",
        "domain": "surface",
        "side": "red"
      }
    ]
  },
  "id": "gauntlet-t2-escort-passive"
}
```

Note: the numeric fields (`minScore`, `maxScore`, `maxMissilesFired`, `maxDenials`, `minKills`, `minDenials`) are deliberately omitted from `expect` here — Step 3 derives and adds them from a real batch run. Do not invent them.

- [ ] **Step 2: Retrofit `gauntlet-t3-emcon-phases.policy.json`**

Replace the file's `"gauntlet"` block's `"emcon"` field and add a top-level `"emcon"` block. Full replacement:

```json
{
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "WeaponsTight",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 5000,
    "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 4,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.2,
    "salvoSize": 1
  },
  "emcon": {
    "units": {
      "k-31-visby-2009": { "radar": "Passive" },
      "jas-39c-gripen-2005": { "radar": "Active" },
      "a-19-gotland-2022": { "radar": "Passive" }
    }
  },
  "detection": [
    {
      "observerId": "k-31-visby-2009",
      "sensorId": "radar-1",
      "targetId": "em-sovremenny-i-pr-956-sarych",
      "contactId": "c-surface",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    },
    {
      "observerId": "jas-39c-gripen-2005",
      "sensorId": "radar-1",
      "targetId": "mpk-steregushchiy-pr-20380-2018",
      "contactId": "c-air",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    },
    {
      "observerId": "a-19-gotland-2022",
      "sensorId": "sonar-1",
      "targetId": "mrk-buyan-pr-21630-buyan-2007",
      "contactId": "c-sub",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0,
      "requiresActiveRadar": false
    }
  ],
  "gauntlet": {
    "intent": "Static mixed EMCON posture across the ORBAT (corrected from the old vocabulary-only \"phased\" prose annotation, which had no top-level emcon block at all): Visby Passive (radar-gated, engage blocked via EMCON_OFF), Gripen Active (unaffected), Gotland Passive but sonar-based (requiresActiveRadar: false, unaffected by EMCON) — proves the real emcon block differentiates outcomes by sensor type, not just by label. No per-tick EMCON phase schedule exists in the engine (ScenarioEmconJsonDto has no schedule field); this is intentionally NOT a timed/dynamic claim.",
    "oracle": "Gripen and Gotland still True|Launched at their paired catalog reds; Visby's engage attempts abort EMCON_OFF; CATALOG_UNIT+MAGAZINE_SEED all domains still present.",
    "dimensionsClaimed": ["emcon-engage-block", "combat-domain"],
    "catalogRefs": [
      "241-slazak-meko-100-mod-2019",
      "421-orkan-pr-660-2015",
      "624-pr-205p-tarantul-1992",
      "70-rauma-helsinki-ii-1990",
      "80-hamina-rauma-2-2006",
      "a-17-södermanland-vastergotland-mod",
      "a-17-vastergotland-1999",
      "a-19-gotland-1996",
      "a-19-gotland-2022",
      "bpk-marshal-shaposhnikov-udaloy-i-pr-1155-fregat",
      "em-sovremenny-i-pr-956-sarych",
      "f-207-bremen-type-f122-1982",
      "f-35a-lightning-ii",
      "f-a-18c-hornet-f-18c",
      "hkp-14f-nh90-ttt",
      "jas-39a-gripen-1997",
      "jas-39b-gripen-1997",
      "jas-39c-gripen-2005",
      "k-31-visby-2009",
      "mpk-steregushchiy-pr-20380-2018",
      "mrk-buyan-pr-21630-buyan-2007"
    ],
    "emcon": "static-mixed-posture",
    "runId": "gauntlet-t3t5-catalog-red",
    "tier": 3,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["EMCON_OFF", "CATALOG_UNIT:jas-39c-gripen-2005:air", "CATALOG_UNIT:a-19-gotland-2022:subsurface"],
      "requireTrueLaunchedShooters": [
        "jas-39c-gripen-2005",
        "a-19-gotland-2022"
      ]
    },
    "units": [
      {
        "unitId": "k-31-visby-2009",
        "platformId": "k-31-visby-2009",
        "domain": "surface",
        "side": "blue"
      },
      {
        "unitId": "jas-39c-gripen-2005",
        "platformId": "jas-39c-gripen-2005",
        "domain": "air",
        "side": "blue"
      },
      {
        "unitId": "a-19-gotland-2022",
        "platformId": "a-19-gotland-2022",
        "domain": "subsurface",
        "side": "blue"
      },
      {
        "unitId": "em-sovremenny-i-pr-956-sarych",
        "platformId": "em-sovremenny-i-pr-956-sarych",
        "domain": "surface",
        "side": "red"
      },
      {
        "unitId": "mpk-steregushchiy-pr-20380-2018",
        "platformId": "mpk-steregushchiy-pr-20380-2018",
        "domain": "surface",
        "side": "red"
      },
      {
        "unitId": "mrk-buyan-pr-21630-buyan-2007",
        "platformId": "mrk-buyan-pr-21630-buyan-2007",
        "domain": "surface",
        "side": "red"
      }
    ]
  },
  "id": "gauntlet-t3-emcon-phases"
}
```

**Caution:** this scenario is cited elsewhere (`docs/superpowers/plans/2026-07-13-gauntlet-slice1-ladder-injects-slice2-multidomain.md`) as the canonical "reference triple (already correct)" for the Visby/Gripen/Gotland multi-domain pairing. Making Visby Passive changes its engagement behavior (it will no longer contribute a kill) — re-check any test or doc that assumed Visby fires successfully in this scenario before merging (Step 4 below runs the full suite specifically to catch this).

- [ ] **Step 3: Retrofit `gauntlet-t5-roe-change.policy.json`**

Find:
```json
    "emcon": "contested",
```
Replace with:
```json
    "emcon": "static-contested-red-passive",
```

Find:
```json
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "minKills": 1,
      "maxMissilesFired": 12,
      "maxDenials": 28,
      "minScore": 50,
      "maxScore": 170,
      "minDenials": 14,
      "requireFingerprintSubstrings": [
        "CommsStateChange",
        "Degraded"
      ]
    },
```
Replace with:
```json
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": [
        "CommsStateChange",
        "Degraded",
        "EMCON_OFF"
      ]
    },
```
(Numeric fields removed pending Step 4 re-derivation — the new red-Passive emcon block will change Red's engagement behavior, so the old bounds no longer apply.)

Find:
```json
  "id": "gauntlet-t5-roe-change",
```
Insert a new top-level `"emcon"` block immediately before it (after the closing `}` of the `"gauntlet"` block, i.e. as a sibling of `"gauntlet"`, `"commsDisplay"`, `"comms"`, `"mission"`):
```json
  "emcon": {
    "units": {
      "em-sovremenny-i-pr-956-sarych": { "radar": "Passive" }
    }
  },
```

Also add `"dimensionsClaimed": ["emcon-engage-block"]` to the `"gauntlet"` block (alongside `"intent"`/`"oracle"`), and update `"intent"` to append: `" Red (em-sovremenny-i-pr-956-sarych) additionally runs radar Passive throughout — static contested EMCON, corrected from the old vocabulary-only \"contested\" prose annotation (no deception-emitter modelling exists — see docs/engineering/sim-capability-gap-backlog.md GAP-07)."`

- [ ] **Step 4: Regenerate expect envelopes for all 3 retrofitted scenarios from real batch runs**

```bash
mkdir -p /tmp/gauntlet-emcon-retrofit

# T2 (ticks=10)
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t2-escort-passive --seeds 42,7,123 --ticks 10 \
  --csv-out /tmp/gauntlet-emcon-retrofit/t2-escort-passive.csv

# T3 (ticks=16)
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t3-emcon-phases --seeds 42,7,123 --ticks 16 \
  --csv-out /tmp/gauntlet-emcon-retrofit/t3-emcon-phases.csv

# T5 (ticks=40)
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t5-roe-change --seeds 42,7,123 --ticks 40 \
  --csv-out /tmp/gauntlet-emcon-retrofit/t5-roe-change.csv
```

For each CSV: compute `minScore`/`maxScore`/`minKills`/`maxMissilesFired`/`minDenials`/`maxDenials` as the min/max of the corresponding column across the 3 seed rows for that `scenarioId` (per `tools/qa-gauntlet/README-expect-regen.md` — no headroom padding beyond what the existing corpus convention uses, i.e. use the observed min/max directly unless a seed produces exactly the boundary value, in which case widen by one unit in the safe direction only). Add these fields into each scenario's `gauntlet.expect` object.

Then run the oracle and the new dimension-coverage check for each:

```bash
for s in gauntlet-t2-escort-passive:t2-escort-passive gauntlet-t3-emcon-phases:t3-emcon-phases gauntlet-t5-roe-change:t5-roe-change; do
  id="${s%%:*}"; tag="${s##*:}"
  dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
    --policy "data/scenarios/${id}.policy.json" \
    --csv "/tmp/gauntlet-emcon-retrofit/${tag}.csv" \
    --out "/tmp/gauntlet-emcon-retrofit/${tag}-oracle-eval.json"
  python3 tools/qa-gauntlet/verify_dimension_coverage.py \
    --policy "data/scenarios/${id}.policy.json" \
    --csv "/tmp/gauntlet-emcon-retrofit/${tag}.csv"
done
```
Expected for each: `"allPassed": true` in the oracle-eval JSON and `All N claimed dimension(s) proven.` from the coverage script. If `gauntlet-t3-emcon-phases`'s dropped Visby kill moves its numeric bounds enough to fail, re-derive (do not hand-tighten) — this is expected per the plan's Risks section ("corpus expansion may surface latent defects").

- [ ] **Step 5: Full regression — confirm the 3 retrofits didn't break anything downstream**

```bash
dotnet test ProjectAegis.sln
```
Expected: PASS, count ≥ the pre-task baseline (this task changes no C#, so a drop would indicate the JSON edits broke a test that hardcodes expected values from these 3 files — investigate before proceeding, do not silence).

- [ ] **Step 6: Close the bug report**

Edit `production/qa/bugs/BUG-t2-escort-passive-emcon-claim-unimplemented.md`: change `**Status**: Open` to `**Status**: Fixed` and append a new line after the `## Suggested fix` section:

```markdown
## Resolution (2026-07-27)

Fixed by replacing the basePd/envMask stand-in with the real scenario-level
`emcon` block (`emcon.units.k-31-visby-2009.radar = "Passive"`), proven via the
`EMCON_OFF` engage-abort fingerprint token rather than a numeric Pd nerf. The
same defect class was found in two more corpus scenarios during the sweep this
fix invited (`gauntlet-t3-emcon-phases`, `gauntlet-t5-roe-change`) and fixed in
the same change. See `docs/superpowers/plans/2026-07-27-gauntlet-variability.md`
Task 4.
```

- [ ] **Step 7: Commit**

```bash
git add data/scenarios/gauntlet-t2-escort-passive.policy.json \
        data/scenarios/gauntlet-t3-emcon-phases.policy.json \
        data/scenarios/gauntlet-t5-roe-change.policy.json \
        production/qa/bugs/BUG-t2-escort-passive-emcon-claim-unimplemented.md
git commit -m "qa(gauntlet): replace EMCON stand-ins with the real emcon block (3 scenarios)

Closes BUG-t2-escort-passive-emcon-claim-unimplemented. Sweep found two more
vocabulary-only \"emcon\": \"<prose>\" instances (gauntlet-t3-emcon-phases,
gauntlet-t5-roe-change) and fixed them the same way. All three now prove
EMCON via the EMCON_OFF fingerprint token; gauntlet-t3-emcon-phases's tier
matrix wording is also corrected to drop the false \"timed phases\" claim
(no per-tick EMCON schedule exists in the engine)."
```

---

### Task 5: Tier 1 additions (2 scenarios, ticks=6)

**Files:**
- Create: `data/scenarios/gauntlet-t1-patrol-pkkill-boundary.policy.json`
- Create: `data/scenarios/gauntlet-t1-patrol-roe-tight-tight.policy.json`

- [ ] **Step 1: Write `gauntlet-t1-patrol-pkkill-boundary.policy.json`**

```json
{
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "WeaponsFree",
  "engage": {
    "rangeMeters": 40000,
    "envelopeMinMeters": 4000,
    "envelopeMaxMeters": 100000,
    "defaultMagazineRounds": 4,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.9,
    "pkIntercept": 0.0,
    "pkKill": 1.0,
    "salvoSize": 1
  },
  "detection": [
    {
      "observerId": "k-31-visby-2009",
      "sensorId": "radar-1",
      "targetId": "em-sovremenny-i-pr-956-sarych",
      "contactId": "c1",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    }
  ],
  "gauntlet": {
    "intent": "T1 weapons-boundary pin: pkKill=1.0 exactly (always-kill path) [catalog: Visby vs Sovremenny]",
    "oracle": "Every resolved hit is a Kill; no bare Hit outcome ever appears for this scenario's shooter.",
    "dimensionsClaimed": ["weapons-boundary-pkkill-1"],
    "catalogRefs": ["em-sovremenny-i-pr-956-sarych", "k-31-visby-2009"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 1,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true
    },
    "units": [
      { "unitId": "k-31-visby-2009", "platformId": "k-31-visby-2009", "domain": "surface", "side": "blue" },
      { "unitId": "em-sovremenny-i-pr-956-sarych", "platformId": "em-sovremenny-i-pr-956-sarych", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t1-patrol-pkkill-boundary"
}
```

- [ ] **Step 2: Write `gauntlet-t1-patrol-roe-tight-tight.policy.json`**

Coverage-map novelty fill: the existing T1 set (`patrol-a/b/c/d`) covers `WeaponsFree/WeaponsTight`, `WeaponsFree/WeaponsFree`, `WeaponsTight/WeaponsFree` roePairs but not `WeaponsTight/WeaponsTight` — this scenario fills that cell (a real forge novelty criterion, not arbitrary padding).

```json
{
  "friendlyRoe": "WeaponsTight",
  "opposingRoe": "WeaponsTight",
  "engage": {
    "rangeMeters": 40000,
    "envelopeMinMeters": 4000,
    "envelopeMaxMeters": 100000,
    "defaultMagazineRounds": 3,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.2,
    "salvoSize": 1
  },
  "detection": [
    {
      "observerId": "f-361-iver-huitfeldt-2012",
      "sensorId": "radar-1",
      "targetId": "421-orkan-pr-660-2015",
      "contactId": "c1",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    }
  ],
  "gauntlet": {
    "intent": "T1 coverage-map fill: WeaponsTight/WeaponsTight ROE pair, the one combination not yet represented in the existing patrol-a/b/c/d set [catalog: Iver Huitfeldt vs Orkan]",
    "oracle": "Stable single-patrol run under mutual WeaponsTight; engagement only on explicit ID/contact trigger, not free-fire.",
    "dimensionsClaimed": [],
    "catalogRefs": ["421-orkan-pr-660-2015", "f-361-iver-huitfeldt-2012"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 1,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true
    },
    "units": [
      { "unitId": "f-361-iver-huitfeldt-2012", "platformId": "f-361-iver-huitfeldt-2012", "domain": "surface", "side": "blue" },
      { "unitId": "421-orkan-pr-660-2015", "platformId": "421-orkan-pr-660-2015", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t1-patrol-roe-tight-tight"
}
```

- [ ] **Step 3: Validate both scenarios (catalog resolution, oracle 0)**

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t1-patrol-pkkill-boundary.policy.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t1-patrol-roe-tight-tight.policy.json
```
Expected: both PASS (every id resolves against the catalog DB).

- [ ] **Step 4: Batch + oracle + dimension-coverage at T1 ticks (6)**

```bash
mkdir -p /tmp/gauntlet-t1-new
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t1-patrol-pkkill-boundary,gauntlet-t1-patrol-roe-tight-tight \
  --seeds 42,7,123 --ticks 6 \
  --csv-out /tmp/gauntlet-t1-new/results.csv
```

Derive `minKills`/`maxMissilesFired`/`minDenials`/`maxDenials`/`minScore`/`maxScore` for each scenario from the CSV (min/max across the 3 seeds) and add them to each file's `gauntlet.expect`. Then:

```bash
for id in gauntlet-t1-patrol-pkkill-boundary gauntlet-t1-patrol-roe-tight-tight; do
  dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
    --policy "data/scenarios/${id}.policy.json" --csv /tmp/gauntlet-t1-new/results.csv \
    --out "/tmp/gauntlet-t1-new/${id}-oracle-eval.json"
  python3 tools/qa-gauntlet/verify_dimension_coverage.py \
    --policy "data/scenarios/${id}.policy.json" --csv /tmp/gauntlet-t1-new/results.csv
done
```
Expected: `"allPassed": true` for both, and `gauntlet-t1-patrol-pkkill-boundary` prints `PASS weapons-boundary-pkkill-1: ok`.

- [ ] **Step 5: Commit**

```bash
git add data/scenarios/gauntlet-t1-patrol-pkkill-boundary.policy.json data/scenarios/gauntlet-t1-patrol-roe-tight-tight.policy.json
git commit -m "qa(gauntlet): add 2 T1 scenarios — pkKill=1.0 boundary + ROE coverage fill

Raises T1 fixed set from 4 to 6 per the revised tier matrix."
```

---

### Task 6: Tier 2 additions (2 scenarios, ticks=10)

**Files:**
- Create: `data/scenarios/gauntlet-t2-strike-salvo-boundary.policy.json`
- Create: `data/scenarios/gauntlet-t2-escort-air-domain-pairing.policy.json`

- [ ] **Step 1: Write `gauntlet-t2-strike-salvo-boundary.policy.json`**

```json
{
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "WeaponsTight",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 5000,
    "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 6,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.3,
    "salvoSize": 2,
    "maxSalvo": 2
  },
  "detection": [
    {
      "observerId": "jas-39c-gripen-2005",
      "sensorId": "radar-1",
      "targetId": "mpk-steregushchiy-pr-20380-2018",
      "contactId": "c1",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    }
  ],
  "gauntlet": {
    "intent": "T2 weapons-boundary pin: salvoSize == maxSalvo exactly (WRA cap off-by-one probe), air-to-surface strike [catalog: Gripen vs Steregushchiy]",
    "oracle": "Launch at exactly the WRA cap still succeeds (cap check is > not >=); no WRA_SALVO abort for this shooter.",
    "dimensionsClaimed": ["weapons-boundary-salvo-cap", "combat-domain"],
    "catalogRefs": ["jas-39c-gripen-2005", "mpk-steregushchiy-pr-20380-2018"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 2,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["CATALOG_UNIT:jas-39c-gripen-2005:air"]
    },
    "units": [
      { "unitId": "jas-39c-gripen-2005", "platformId": "jas-39c-gripen-2005", "domain": "air", "side": "blue" },
      { "unitId": "mpk-steregushchiy-pr-20380-2018", "platformId": "mpk-steregushchiy-pr-20380-2018", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t2-strike-salvo-boundary"
}
```

- [ ] **Step 2: Write `gauntlet-t2-escort-air-domain-pairing.policy.json`**

```json
{
  "friendlyRoe": "WeaponsTight",
  "opposingRoe": "WeaponsFree",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 5000,
    "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 4,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.25,
    "salvoSize": 1
  },
  "detection": [
    {
      "observerId": "k-31-visby-2009",
      "sensorId": "radar-1",
      "targetId": "em-sovremenny-i-pr-956-sarych",
      "contactId": "c1",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    },
    {
      "observerId": "jas-39c-gripen-2005",
      "sensorId": "radar-1",
      "targetId": "em-sovremenny-i-pr-956-sarych",
      "contactId": "c2",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    }
  ],
  "gauntlet": {
    "intent": "T2 combat-domain novelty: surface escort and an air asset both engaging the same surface contact (escort + air-to-surface strike combined), a distinct ORBAT pairing from existing T2 set (escort-a, escort-passive, strike-a, strike-event)",
    "oracle": "Both Visby and Gripen True|Launched at Sovremenny; CATALOG_UNIT tokens present for both domains.",
    "dimensionsClaimed": ["combat-domain"],
    "catalogRefs": ["em-sovremenny-i-pr-956-sarych", "jas-39c-gripen-2005", "k-31-visby-2009"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 2,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["CATALOG_UNIT:jas-39c-gripen-2005:air"],
      "requireTrueLaunchedShooters": ["k-31-visby-2009", "jas-39c-gripen-2005"]
    },
    "units": [
      { "unitId": "k-31-visby-2009", "platformId": "k-31-visby-2009", "domain": "surface", "side": "blue" },
      { "unitId": "jas-39c-gripen-2005", "platformId": "jas-39c-gripen-2005", "domain": "air", "side": "blue" },
      { "unitId": "em-sovremenny-i-pr-956-sarych", "platformId": "em-sovremenny-i-pr-956-sarych", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t2-escort-air-domain-pairing"
}
```

- [ ] **Step 3: Validate, batch (ticks=10), oracle, dimension-coverage**

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t2-strike-salvo-boundary.policy.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t2-escort-air-domain-pairing.policy.json

mkdir -p /tmp/gauntlet-t2-new
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t2-strike-salvo-boundary,gauntlet-t2-escort-air-domain-pairing \
  --seeds 42,7,123 --ticks 10 \
  --csv-out /tmp/gauntlet-t2-new/results.csv
```

Derive and add numeric `expect` fields from the CSV as in Task 5 Step 4, then:

```bash
for id in gauntlet-t2-strike-salvo-boundary gauntlet-t2-escort-air-domain-pairing; do
  dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
    --policy "data/scenarios/${id}.policy.json" --csv /tmp/gauntlet-t2-new/results.csv \
    --out "/tmp/gauntlet-t2-new/${id}-oracle-eval.json"
  python3 tools/qa-gauntlet/verify_dimension_coverage.py \
    --policy "data/scenarios/${id}.policy.json" --csv /tmp/gauntlet-t2-new/results.csv
done
```
Expected: `"allPassed": true` for both; `gauntlet-t2-strike-salvo-boundary` prints `PASS weapons-boundary-salvo-cap: ok`.

- [ ] **Step 4: Commit**

```bash
git add data/scenarios/gauntlet-t2-strike-salvo-boundary.policy.json data/scenarios/gauntlet-t2-escort-air-domain-pairing.policy.json
git commit -m "qa(gauntlet): add 2 T2 scenarios — salvo==maxSalvo boundary + domain-pairing novelty

Raises T2 fixed set from 4 to 6 per the revised tier matrix."
```

---

### Task 7: Tier 3 additions (2 scenarios + 1 control sibling, ticks=16)

**Files:**
- Create: `data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure.policy.json`
- Create: `data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure-control.policy.json`
- Create: `data/scenarios/gauntlet-t3-logistics-contact-lifecycle.policy.json`

- [ ] **Step 1: Write `gauntlet-t3-ew-jammer-magazine-pressure.policy.json`**

```json
{
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "WeaponsTight",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 5000,
    "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 1,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.2,
    "salvoSize": 1
  },
  "jammers": [
    { "targetId": "em-sovremenny-i-pr-956-sarych", "jamStrength": 0.8, "activeFromTick": 2 }
  ],
  "detection": [
    {
      "observerId": "k-31-visby-2009",
      "sensorId": "radar-1",
      "targetId": "em-sovremenny-i-pr-956-sarych",
      "contactId": "c-surface",
      "basePd": 1.0,
      "envMask": 1.0,
      "eccmFactor": 0.6,
      "jamStrength": 0
    },
    {
      "observerId": "jas-39c-gripen-2005",
      "sensorId": "radar-1",
      "targetId": "mpk-steregushchiy-pr-20380-2018",
      "contactId": "c-air",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    },
    {
      "observerId": "a-19-gotland-2022",
      "sensorId": "sonar-1",
      "targetId": "mrk-buyan-pr-21630-buyan-2007",
      "contactId": "c-sub",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0,
      "requiresActiveRadar": false
    }
  ],
  "gauntlet": {
    "intent": "T3 EW + weapons-boundary pressure: red jams Visby's radar track on Sovremenny from tick 2 (partial ECCM 0.6), while Visby's 1-round magazine forces a NO_AMMO abort after its first shot; Gripen/Gotland preserve the existing multi-domain concurrency baseline unaffected by either pressure.",
    "oracle": "NO_AMMO appears for Visby after 1 launch; Gripen and Gotland still True|Launched. Jam effect on Visby's denials is proven differentially against the -control sibling (jamStrength: 0), not by a fingerprint token.",
    "dimensionsClaimed": ["ew-jammer-timing", "weapons-boundary-magazine-exhaustion", "combat-domain"],
    "controlOf": null,
    "catalogRefs": ["a-19-gotland-2022", "em-sovremenny-i-pr-956-sarych", "jas-39c-gripen-2005", "k-31-visby-2009", "mpk-steregushchiy-pr-20380-2018", "mrk-buyan-pr-21630-buyan-2007"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 3,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["NO_AMMO", "CATALOG_UNIT:jas-39c-gripen-2005:air", "CATALOG_UNIT:a-19-gotland-2022:subsurface"],
      "requireTrueLaunchedShooters": ["jas-39c-gripen-2005", "a-19-gotland-2022"]
    },
    "units": [
      { "unitId": "k-31-visby-2009", "platformId": "k-31-visby-2009", "domain": "surface", "side": "blue" },
      { "unitId": "jas-39c-gripen-2005", "platformId": "jas-39c-gripen-2005", "domain": "air", "side": "blue" },
      { "unitId": "a-19-gotland-2022", "platformId": "a-19-gotland-2022", "domain": "subsurface", "side": "blue" },
      { "unitId": "em-sovremenny-i-pr-956-sarych", "platformId": "em-sovremenny-i-pr-956-sarych", "domain": "surface", "side": "red" },
      { "unitId": "mpk-steregushchiy-pr-20380-2018", "platformId": "mpk-steregushchiy-pr-20380-2018", "domain": "surface", "side": "red" },
      { "unitId": "mrk-buyan-pr-21630-buyan-2007", "platformId": "mrk-buyan-pr-21630-buyan-2007", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t3-ew-jammer-magazine-pressure"
}
```

- [ ] **Step 2: Write the control sibling `gauntlet-t3-ew-jammer-magazine-pressure-control.policy.json`**

Identical to Step 1's file except: `"jammers": [{ "targetId": "em-sovremenny-i-pr-956-sarych", "jamStrength": 0.0, "activeFromTick": 2 }]`, `"id": "gauntlet-t3-ew-jammer-magazine-pressure-control"`, `"gauntlet.dimensionsClaimed": []`, `"gauntlet.intent": "Differential-proof control sibling for gauntlet-t3-ew-jammer-magazine-pressure — identical except jamStrength: 0.0. Not part of the fixed ladder; used only to prove the EW dimension differentially. Excluded from the CI gauntlet-oracle fixture list."`, and `"gauntlet.controlOf": "gauntlet-t3-ew-jammer-magazine-pressure"`. Remove `"gauntlet.expect.requireFingerprintSubstrings"`'s dependence on jam (keep `NO_AMMO` and the two `CATALOG_UNIT` entries — those aren't jam-dependent).

- [ ] **Step 3: Write `gauntlet-t3-logistics-contact-lifecycle.policy.json`**

```json
{
  "friendlyRoe": "WeaponsTight",
  "opposingRoe": "WeaponsFree",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 5000,
    "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 4,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.2,
    "salvoSize": 1
  },
  "detection": [
    {
      "observerId": "f-361-iver-huitfeldt-2012",
      "sensorId": "radar-1",
      "targetId": "70-rauma-helsinki-ii-1990",
      "contactId": "c1",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    },
    {
      "observerId": "jas-39c-gripen-2005",
      "sensorId": "radar-1",
      "targetId": "70-rauma-helsinki-ii-1990",
      "contactId": "c2",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    }
  ],
  "logistics": {
    "jokerSimSeconds": 90,
    "bingoSimSeconds": 180,
    "fuelCapacityKg": 10000,
    "burnRateKgPerSecond": 80,
    "jokerFuelFraction": 0.25,
    "bingoFuelFraction": 0.10
  },
  "contactLifecycle": {
    "staleThresholdTicks": 12,
    "classifyAfterTicks": 1,
    "identifyAfterTicks": 2
  },
  "gauntlet": {
    "intent": "T3 logistics + contact-lifecycle pressure: Iver Huitfeldt (surface) and Gripen (air, fuel-burning) track Rauma through classify (tick 1) -> identify (tick 2) gates under ID-required ROE. Logistics thresholds reuse the proven baltic-v3-patrol-comms values as a starting point — see Task 7 Step 4 for the derive-and-scale follow-up (logistics has no runtime/fingerprint signal today; see GAP-13).",
    "oracle": "Classified and Identified both appear in the fingerprint within the 16-tick budget; contactLifecycle thresholds hold their documented invariants.",
    "dimensionsClaimed": ["contact-lifecycle-timing", "logistics-fuel-pressure"],
    "catalogRefs": ["70-rauma-helsinki-ii-1990", "f-361-iver-huitfeldt-2012", "jas-39c-gripen-2005"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 3,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["Classified", "Identified"]
    },
    "units": [
      { "unitId": "f-361-iver-huitfeldt-2012", "platformId": "f-361-iver-huitfeldt-2012", "domain": "surface", "side": "blue" },
      { "unitId": "jas-39c-gripen-2005", "platformId": "jas-39c-gripen-2005", "domain": "air", "side": "blue" },
      { "unitId": "70-rauma-helsinki-ii-1990", "platformId": "70-rauma-helsinki-ii-1990", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t3-logistics-contact-lifecycle"
}
```

- [ ] **Step 4: Validate, batch (ticks=16, including the control sibling), oracle, dimension-coverage (with `--control-csv` for the jammer scenario)**

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure.policy.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure-control.policy.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t3-logistics-contact-lifecycle.policy.json

mkdir -p /tmp/gauntlet-t3-new
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t3-ew-jammer-magazine-pressure,gauntlet-t3-ew-jammer-magazine-pressure-control,gauntlet-t3-logistics-contact-lifecycle \
  --seeds 42,7,123 --ticks 16 \
  --csv-out /tmp/gauntlet-t3-new/results.csv
```

Derive and add numeric `expect` fields for the two non-control scenarios (the control sibling does not need its own committed `expect` bounds — it exists only to be diffed). Then:

```bash
dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure.policy.json \
  --csv /tmp/gauntlet-t3-new/results.csv --out /tmp/gauntlet-t3-new/jammer-oracle-eval.json
dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy data/scenarios/gauntlet-t3-logistics-contact-lifecycle.policy.json \
  --csv /tmp/gauntlet-t3-new/results.csv --out /tmp/gauntlet-t3-new/logistics-oracle-eval.json

python3 tools/qa-gauntlet/verify_dimension_coverage.py \
  --policy data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure.policy.json \
  --csv /tmp/gauntlet-t3-new/results.csv --control-csv /tmp/gauntlet-t3-new/results.csv
python3 tools/qa-gauntlet/verify_dimension_coverage.py \
  --policy data/scenarios/gauntlet-t3-logistics-contact-lifecycle.policy.json \
  --csv /tmp/gauntlet-t3-new/results.csv
```
(The jammer check's `--control-csv` points at the same combined CSV — the script filters rows by `scenarioId`/`controlOf` internally, so the control sibling's own rows are found within it.)

Expected: both oracle evals `"allPassed": true`; jammer scenario prints `PASS ew-jammer-timing: ok` (real denials > control denials) and `PASS weapons-boundary-magazine-exhaustion: ok`; logistics scenario prints `PASS contact-lifecycle-timing: ok` and `PASS logistics-fuel-pressure: ok` (config-only pass).

If the jammer scenario's differential check fails (denials not actually higher than control), increase `jamStrength` toward 1.0 and/or increase `eccmFactor`'s gap versus jamStrength until the CSVs genuinely diverge — do not weaken the check.

- [ ] **Step 5: Commit**

```bash
git add data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure.policy.json \
        data/scenarios/gauntlet-t3-ew-jammer-magazine-pressure-control.policy.json \
        data/scenarios/gauntlet-t3-logistics-contact-lifecycle.policy.json
git commit -m "qa(gauntlet): add 2 T3 scenarios (+1 control sibling) — EW/magazine pressure + logistics/contact-lifecycle

Raises T3 fixed set from 4 to 6 per the revised tier matrix. EW jam effect
proven via differential control-sibling CSV (no discrete jam abort token
exists in the engine)."
```

---

### Task 8: Tier 4 additions (2 scenarios, ticks=24)

**Files:**
- Create: `data/scenarios/gauntlet-t4-facility-strike-datalink.policy.json`
- Create: `data/scenarios/gauntlet-t4-ew-spoof-mount-offline.policy.json`

- [ ] **Step 1: Write `gauntlet-t4-facility-strike-datalink.policy.json`**

```json
{
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "WeaponsTight",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 5000,
    "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 4,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.2,
    "salvoSize": 1,
    "combatDomain": "Facility"
  },
  "emcon": {
    "units": {
      "f-450-elli-kortenaer-batch-ii-0": { "radar": "Active" },
      "421-orkan-pr-660-2015": { "radar": "Passive" }
    }
  },
  "detection": [
    {
      "observerId": "f-450-elli-kortenaer-batch-ii-0",
      "sensorId": "radar-1",
      "targetId": "421-orkan-pr-660-2015",
      "contactId": "c-facility",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    }
  ],
  "datalink": {
    "organicOnly": false,
    "shareLagTicks": 3,
    "unitSides": {
      "f-450-elli-kortenaer-batch-ii-0": "blue",
      "d-32-daring-type-45-batch-1": "blue"
    }
  },
  "contactLifecycle": {
    "staleThresholdTicks": 20,
    "classifyAfterTicks": 1,
    "identifyAfterTicks": 2
  },
  "gauntlet": {
    "intent": "T4 combat-domain + datalink: engage.combatDomain='Facility' applied to 421-orkan-pr-660-2015 as the nearest catalog analog for a land-attack/facility-strike engagement classification — the catalog has no literal 'facility' platform type, matching the precedent set by baltic-patrol-mine-transit-hazard.policy.json applying combatDomain='Mine' to an ordinary ship. Elli Kortenaer (organic observer, Active) shares its contact with Daring (non-organic peer) after a 3-tick datalink lag. Static asymmetric EMCON (blue Active, red Passive) differentiates by domain rather than claiming a runtime EMCON toggle.",
    "oracle": "Daring's ContactChange for c-facility lands exactly 3 ticks after Elli Kortenaer's own ContactChange for the same contact (tick-delta proof, single run, no control sibling needed).",
    "dimensionsClaimed": ["combat-domain", "datalink-degradation", "emcon-engage-block"],
    "catalogRefs": ["421-orkan-pr-660-2015", "d-32-daring-type-45-batch-1", "f-450-elli-kortenaer-batch-ii-0"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 4,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["CATALOG_UNIT:f-450-elli-kortenaer-batch-ii-0:facility"]
    },
    "units": [
      { "unitId": "f-450-elli-kortenaer-batch-ii-0", "platformId": "f-450-elli-kortenaer-batch-ii-0", "domain": "facility", "side": "blue" },
      { "unitId": "d-32-daring-type-45-batch-1", "platformId": "d-32-daring-type-45-batch-1", "domain": "surface", "side": "blue" },
      { "unitId": "421-orkan-pr-660-2015", "platformId": "421-orkan-pr-660-2015", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t4-facility-strike-datalink"
}
```

Note: `gauntlet.units[].domain` for the shooter is set to `"facility"` (a free string, per DTO — see Global Constraints/finding 6) specifically so the `CATALOG_UNIT:{platformId}:facility` proof token is emitted, even though `f-450-elli-kortenaer-batch-ii-0` is physically a surface frigate standing in for the facility-strike classification. This is the same approximation the scenario's own intent text discloses.

- [ ] **Step 2: Write `gauntlet-t4-ew-spoof-mount-offline.policy.json`**

```json
{
  "friendlyRoe": "WeaponsTight",
  "opposingRoe": "WeaponsFree",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 5000,
    "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 4,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.2,
    "salvoSize": 1,
    "mountOnline": false
  },
  "detection": [
    {
      "observerId": "f-341-absalon-2020",
      "sensorId": "radar-1",
      "targetId": "mrk-buyan-mod-pr-21631-buyan-m-2014",
      "contactId": "c1",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    }
  ],
  "spoofTracks": [
    { "atTick": 5, "contactId": "c1", "reason": "cyber-spoof" }
  ],
  "gauntlet": {
    "intent": "T4 EW + weapons-boundary: a cyber-spoofed track injected on the live contact at tick 5 (CYBER_SPOOF_TRACK), combined with mountOnline=false (MOUNT_OFFLINE boundary pin) so Absalon's weapon mount is offline for the whole run — two independent, both-present abort conditions on one shooter, asymmetric per-side ROE continuing the T4 row.",
    "oracle": "Both MOUNT_OFFLINE and CYBER_SPOOF_TRACK appear in the fingerprint for this scenario.",
    "dimensionsClaimed": ["ew-spoof-inject", "weapons-boundary-mount-offline"],
    "catalogRefs": ["f-341-absalon-2020", "mrk-buyan-mod-pr-21631-buyan-m-2014"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 4,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["MOUNT_OFFLINE", "CYBER_SPOOF_TRACK"]
    },
    "units": [
      { "unitId": "f-341-absalon-2020", "platformId": "f-341-absalon-2020", "domain": "surface", "side": "blue" },
      { "unitId": "mrk-buyan-mod-pr-21631-buyan-m-2014", "platformId": "mrk-buyan-mod-pr-21631-buyan-m-2014", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t4-ew-spoof-mount-offline"
}
```

- [ ] **Step 3: Validate, batch (ticks=24), oracle, dimension-coverage**

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t4-facility-strike-datalink.policy.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t4-ew-spoof-mount-offline.policy.json

mkdir -p /tmp/gauntlet-t4-new
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t4-facility-strike-datalink,gauntlet-t4-ew-spoof-mount-offline \
  --seeds 42,7,123 --ticks 24 \
  --csv-out /tmp/gauntlet-t4-new/results.csv
```

Derive and add numeric `expect` fields from the CSV, then:

```bash
for id in gauntlet-t4-facility-strike-datalink gauntlet-t4-ew-spoof-mount-offline; do
  dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
    --policy "data/scenarios/${id}.policy.json" --csv /tmp/gauntlet-t4-new/results.csv \
    --out "/tmp/gauntlet-t4-new/${id}-oracle-eval.json"
  python3 tools/qa-gauntlet/verify_dimension_coverage.py \
    --policy "data/scenarios/${id}.policy.json" --csv /tmp/gauntlet-t4-new/results.csv
done
```
Expected: both `"allPassed": true`; `gauntlet-t4-facility-strike-datalink` prints `PASS datalink-degradation: ok` (peer ContactChange lands 3 ticks after organic); `gauntlet-t4-ew-spoof-mount-offline` prints `PASS ew-spoof-inject: ok` and `PASS weapons-boundary-mount-offline: ok`.

If the datalink tick-delta check fails, confirm both `f-450-elli-kortenaer-batch-ii-0` and `d-32-daring-type-45-batch-1` are tagged `"blue"` in `datalink.unitSides` and that `organicOnly` is `false` (sharing must be enabled) before adjusting `shareLagTicks`.

- [ ] **Step 4: Commit**

```bash
git add data/scenarios/gauntlet-t4-facility-strike-datalink.policy.json data/scenarios/gauntlet-t4-ew-spoof-mount-offline.policy.json
git commit -m "qa(gauntlet): add 2 T4 scenarios — facility/datalink pairing + spoof/mount-offline boundary

Raises T4 fixed set from 4 to 6 per the revised tier matrix."
```

---

### Task 9: Tier 5 additions (2 scenarios + 1 control sibling, ticks=40)

**Files:**
- Create: `data/scenarios/gauntlet-t5-mine-hazard-theater.policy.json`
- Create: `data/scenarios/gauntlet-t5-cascading-ew-logistics.policy.json`
- Create: `data/scenarios/gauntlet-t5-cascading-ew-logistics-control.policy.json`

- [ ] **Step 1: Write `gauntlet-t5-mine-hazard-theater.policy.json`**

```json
{
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "WeaponsTight",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 45000,
    "envelopeMaxMeters": 45000,
    "defaultMagazineRounds": 4,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.2,
    "salvoSize": 1,
    "combatDomain": "Mine"
  },
  "detection": [
    {
      "observerId": "k-31-visby-2009",
      "sensorId": "radar-1",
      "targetId": "em-sovremenny-i-pr-956-sarych",
      "contactId": "c-surface",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    },
    {
      "observerId": "jas-39c-gripen-2005",
      "sensorId": "radar-1",
      "targetId": "mpk-steregushchiy-pr-20380-2018",
      "contactId": "c-air",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0
    },
    {
      "observerId": "a-19-gotland-2022",
      "sensorId": "sonar-1",
      "targetId": "mrk-buyan-pr-21630-buyan-2007",
      "contactId": "c-sub",
      "basePd": 1.0,
      "envMask": 1.0,
      "jamStrength": 0,
      "requiresActiveRadar": false
    }
  ],
  "catalogWithdraw": [
    { "platformId": "k-31-visby-2009", "currentHpPct": 100 }
  ],
  "mineHazard": {
    "zoneMinRangeMeters": 45000,
    "zoneMaxRangeMeters": 70000,
    "triggerRadiusMeters": 8000,
    "hazardSeverity": 1.0,
    "mines": [
      { "mineId": "mine-a", "rangeMeters": 52000, "lethality": 1.0 },
      { "mineId": "mine-b", "rangeMeters": 61000, "lethality": 1.0 }
    ],
    "transit": [
      { "platformId": "k-31-visby-2009", "rangesMeters": [40000, 52000, 61000, 75000] }
    ]
  },
  "gauntlet": {
    "intent": "T5 combat-domain (Mine) + multi-domain concurrent + weapons-boundary: Visby transits a mined lane (engage.combatDomain='Mine', same approximation precedent as baltic-patrol-mine-transit-hazard.policy.json) while the Visby/Gripen/Gotland surface+air+sub triple (canonical multi-domain reference pairing) engages concurrently. Visby's own lane additionally pins a zero-width envelope (envelopeMin==envelopeMax) as the T5 weapons-boundary probe.",
    "oracle": "MINE_ASPECT_BLOCK appears when Visby's engagement is attempted inside a mine's trigger radius; Gripen and Gotland still True|Launched, proving domains stay concurrent despite the mine hazard on Visby's lane.",
    "dimensionsClaimed": ["mine-hazard-zone", "combat-domain", "weapons-boundary-never-kill"],
    "catalogRefs": ["a-19-gotland-2022", "em-sovremenny-i-pr-956-sarych", "jas-39c-gripen-2005", "k-31-visby-2009", "mpk-steregushchiy-pr-20380-2018", "mrk-buyan-pr-21630-buyan-2007"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 5,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["MINE_ASPECT_BLOCK", "CATALOG_UNIT:jas-39c-gripen-2005:air", "CATALOG_UNIT:a-19-gotland-2022:subsurface"],
      "requireTrueLaunchedShooters": ["jas-39c-gripen-2005", "a-19-gotland-2022"]
    },
    "units": [
      { "unitId": "k-31-visby-2009", "platformId": "k-31-visby-2009", "domain": "mine", "side": "blue" },
      { "unitId": "jas-39c-gripen-2005", "platformId": "jas-39c-gripen-2005", "domain": "air", "side": "blue" },
      { "unitId": "a-19-gotland-2022", "platformId": "a-19-gotland-2022", "domain": "subsurface", "side": "blue" },
      { "unitId": "em-sovremenny-i-pr-956-sarych", "platformId": "em-sovremenny-i-pr-956-sarych", "domain": "surface", "side": "red" },
      { "unitId": "mpk-steregushchiy-pr-20380-2018", "platformId": "mpk-steregushchiy-pr-20380-2018", "domain": "surface", "side": "red" },
      { "unitId": "mrk-buyan-pr-21630-buyan-2007", "platformId": "mrk-buyan-pr-21630-buyan-2007", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t5-mine-hazard-theater"
}
```

Note `engage.envelopeMinMeters == engage.envelopeMaxMeters == 45000` (zero-width envelope, satisfies `weapons-boundary-never-kill`'s field-equality half — `pkKill` is left at `0.2`, not `0.0`, here because the never-kill claim is carried by the mine hazard's aspect-block instead; if regen shows a `|Kill|` outcome does occur for Visby, either remove `weapons-boundary-never-kill` from `dimensionsClaimed` for this scenario or lower `pkKill` to `0.0` to genuinely satisfy it — the differential-proof discipline in Task 1 will fail closed either way, so this is a real gate, not a formality.

- [ ] **Step 2: Write `gauntlet-t5-cascading-ew-logistics.policy.json`**

```json
{
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "WeaponsTight",
  "engage": {
    "rangeMeters": 45000,
    "envelopeMinMeters": 5000,
    "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 2,
    "hasFireControlTrack": true,
    "combatDomainsEnabled": true,
    "pkBase": 0.85,
    "pkIntercept": 0.0,
    "pkKill": 0.0,
    "salvoSize": 1
  },
  "jammers": [
    { "targetId": "421-orkan-pr-660-2015", "jamStrength": 0.9, "activeFromTick": 3 }
  ],
  "spoofTracks": [
    { "atTick": 10, "contactId": "c1", "reason": "cyber-spoof" }
  ],
  "detection": [
    {
      "observerId": "f-207-bremen-type-f122-1982",
      "sensorId": "radar-1",
      "targetId": "421-orkan-pr-660-2015",
      "contactId": "c1",
      "basePd": 1.0,
      "envMask": 1.0,
      "eccmFactor": 0.5,
      "jamStrength": 0
    }
  ],
  "logistics": {
    "jokerSimSeconds": 90,
    "bingoSimSeconds": 180,
    "fuelCapacityKg": 10000,
    "burnRateKgPerSecond": 80,
    "jokerFuelFraction": 0.25,
    "bingoFuelFraction": 0.10
  },
  "datalink": {
    "organicOnly": true,
    "shareLagTicks": 0,
    "unitSides": {
      "f-207-bremen-type-f122-1982": "blue"
    }
  },
  "gauntlet": {
    "intent": "T5 cascading EW (jam + spoof together) + logistics escalation + weapons-boundary never-kill (pkKill=0.0): Bremen's contact is jammed from tick 3 (partial ECCM) and its track spoofed at tick 10 — two independent EW pressures stacked, cascading per the T5 Events row's spirit. pkKill=0.0 means no engagement by this shooter can ever resolve as a Kill for the whole 40-tick run. organicOnly datalink (harsher than T4's shared setting) means no peer sharing occurs at all.",
    "oracle": "CYBER_SPOOF_TRACK present; no |Kill| outcome ever appears for Bremen. Jam effect proven differentially against the -control sibling (jamStrength: 0).",
    "dimensionsClaimed": ["ew-jammer-timing", "ew-spoof-inject", "weapons-boundary-never-kill", "logistics-fuel-pressure"],
    "controlOf": null,
    "catalogRefs": ["421-orkan-pr-660-2015", "f-207-bremen-type-f122-1982"],
    "runId": "gauntlet-matrix-expansion-2026-07-27",
    "tier": 5,
    "expect": {
      "side": "BLUE",
      "requireNonEmptyFingerprint": true,
      "requireFingerprintSubstrings": ["CYBER_SPOOF_TRACK"]
    },
    "units": [
      { "unitId": "f-207-bremen-type-f122-1982", "platformId": "f-207-bremen-type-f122-1982", "domain": "surface", "side": "blue" },
      { "unitId": "421-orkan-pr-660-2015", "platformId": "421-orkan-pr-660-2015", "domain": "surface", "side": "red" }
    ]
  },
  "id": "gauntlet-t5-cascading-ew-logistics"
}
```

- [ ] **Step 3: Write the control sibling `gauntlet-t5-cascading-ew-logistics-control.policy.json`**

Identical to Step 2's file except: `"jammers": [{ "targetId": "421-orkan-pr-660-2015", "jamStrength": 0.0, "activeFromTick": 3 }]`, `"id": "gauntlet-t5-cascading-ew-logistics-control"`, `"gauntlet.dimensionsClaimed": []`, `"gauntlet.intent": "Differential-proof control sibling for gauntlet-t5-cascading-ew-logistics — identical except jamStrength: 0.0. Not part of the fixed ladder."`, `"gauntlet.controlOf": "gauntlet-t5-cascading-ew-logistics"`.

- [ ] **Step 4: Validate, batch (ticks=40, including the control sibling), oracle, dimension-coverage**

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t5-mine-hazard-theater.policy.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t5-cascading-ew-logistics.policy.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate data/scenarios/gauntlet-t5-cascading-ew-logistics-control.policy.json

mkdir -p /tmp/gauntlet-t5-new
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t5-mine-hazard-theater,gauntlet-t5-cascading-ew-logistics,gauntlet-t5-cascading-ew-logistics-control \
  --seeds 42,7,123 --ticks 40 \
  --csv-out /tmp/gauntlet-t5-new/results.csv
```

Derive and add numeric `expect` fields for the two non-control scenarios, then:

```bash
dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy data/scenarios/gauntlet-t5-mine-hazard-theater.policy.json \
  --csv /tmp/gauntlet-t5-new/results.csv --out /tmp/gauntlet-t5-new/mine-oracle-eval.json
dotnet run -c Release --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy data/scenarios/gauntlet-t5-cascading-ew-logistics.policy.json \
  --csv /tmp/gauntlet-t5-new/results.csv --out /tmp/gauntlet-t5-new/cascade-oracle-eval.json

python3 tools/qa-gauntlet/verify_dimension_coverage.py \
  --policy data/scenarios/gauntlet-t5-mine-hazard-theater.policy.json \
  --csv /tmp/gauntlet-t5-new/results.csv
python3 tools/qa-gauntlet/verify_dimension_coverage.py \
  --policy data/scenarios/gauntlet-t5-cascading-ew-logistics.policy.json \
  --csv /tmp/gauntlet-t5-new/results.csv --control-csv /tmp/gauntlet-t5-new/results.csv
```
Expected: both oracle evals `"allPassed": true`; mine-hazard scenario prints `PASS mine-hazard-zone: ok`, `PASS combat-domain: ok`; cascading scenario prints `PASS ew-jammer-timing: ok`, `PASS ew-spoof-inject: ok`, `PASS weapons-boundary-never-kill: ok`, `PASS logistics-fuel-pressure: ok`.

- [ ] **Step 5: Commit**

```bash
git add data/scenarios/gauntlet-t5-mine-hazard-theater.policy.json \
        data/scenarios/gauntlet-t5-cascading-ew-logistics.policy.json \
        data/scenarios/gauntlet-t5-cascading-ew-logistics-control.policy.json
git commit -m "qa(gauntlet): add 2 T5 scenarios (+1 control sibling) — mine-hazard theater + cascading EW/logistics

Raises T5 fixed set from 4 to 6 per the revised tier matrix."
```

---

### Task 10: Register all 13 touched/added scenarios in the corpus (`coverage-map.json` + `index.yaml`)

**Files:**
- Modify: `production/qa/gauntlet/corpus/coverage-map.json`
- Modify: `production/qa/gauntlet/corpus/index.yaml`

**Interfaces:**
- Consumes: `intentHash` values — compute as the first 16 hex chars of `sha256(canonical-JSON of the scenario's "gauntlet" block)`, matching the existing corpus convention.

- [ ] **Step 1: Compute `intentHash` for each of the 13 touched scenarios**

```bash
python3 - <<'EOF'
import json, hashlib, glob

targets = [
    "gauntlet-t2-escort-passive", "gauntlet-t3-emcon-phases", "gauntlet-t5-roe-change",
    "gauntlet-t1-patrol-pkkill-boundary", "gauntlet-t1-patrol-roe-tight-tight",
    "gauntlet-t2-strike-salvo-boundary", "gauntlet-t2-escort-air-domain-pairing",
    "gauntlet-t3-ew-jammer-magazine-pressure", "gauntlet-t3-logistics-contact-lifecycle",
    "gauntlet-t4-facility-strike-datalink", "gauntlet-t4-ew-spoof-mount-offline",
    "gauntlet-t5-mine-hazard-theater", "gauntlet-t5-cascading-ew-logistics",
]
for t in targets:
    path = f"data/scenarios/{t}.policy.json"
    policy = json.load(open(path))
    canon = json.dumps(policy.get("gauntlet", {}), sort_keys=True, separators=(",", ":"))
    h = hashlib.sha256(canon.encode("utf-8")).hexdigest()[:16]
    print(f"{t}: {h}")
EOF
```

Record the printed hash for each scenario — used in Steps 2 and 3 below (shown as `<HASH:scenario-id>` placeholders that must be replaced with the actual printed value; this is a computed value, not an invented one).

- [ ] **Step 2: Add 13 cells to `coverage-map.json`**

Update `"generatedAt"` to `"2026-07-27"`, increment `"cellCount"` by 13 and `"scenarioCount"` by 10 (3 retrofits reuse existing cells with an updated hash; 10 new scenarios add new cells), and append to the `"cells"` array (one object per scenario, `missionClass`/`domains`/`roePair`/`emconClass`/`eventClass` describing each as done for existing entries; `catalogRefCount` = length of that scenario's `catalogRefs`):

```json
    {
      "key": "patrol|surface|WeaponsFree/WeaponsFree|unrestricted|none",
      "scenarioId": "gauntlet-t1-patrol-pkkill-boundary",
      "tier": 1,
      "missionClass": "patrol",
      "domains": ["surface"],
      "roePair": "WeaponsFree/WeaponsFree",
      "emconClass": "unrestricted",
      "eventClass": "none",
      "catalogRefCount": 2,
      "intentHash": "<HASH:gauntlet-t1-patrol-pkkill-boundary>"
    },
    {
      "key": "patrol|surface|WeaponsTight/WeaponsTight|unrestricted|none",
      "scenarioId": "gauntlet-t1-patrol-roe-tight-tight",
      "tier": 1,
      "missionClass": "patrol",
      "domains": ["surface"],
      "roePair": "WeaponsTight/WeaponsTight",
      "emconClass": "unrestricted",
      "eventClass": "none",
      "catalogRefCount": 2,
      "intentHash": "<HASH:gauntlet-t1-patrol-roe-tight-tight>"
    },
    {
      "key": "strike|air,surface|WeaponsFree/WeaponsTight|unrestricted|none",
      "scenarioId": "gauntlet-t2-strike-salvo-boundary",
      "tier": 2,
      "missionClass": "strike",
      "domains": ["air", "surface"],
      "roePair": "WeaponsFree/WeaponsTight",
      "emconClass": "unrestricted",
      "eventClass": "none",
      "catalogRefCount": 2,
      "intentHash": "<HASH:gauntlet-t2-strike-salvo-boundary>"
    },
    {
      "key": "escort|air,surface|WeaponsTight/WeaponsFree|unrestricted|none",
      "scenarioId": "gauntlet-t2-escort-air-domain-pairing",
      "tier": 2,
      "missionClass": "escort",
      "domains": ["air", "surface"],
      "roePair": "WeaponsTight/WeaponsFree",
      "emconClass": "unrestricted",
      "eventClass": "none",
      "catalogRefCount": 3,
      "intentHash": "<HASH:gauntlet-t2-escort-air-domain-pairing>"
    },
    {
      "key": "multi|air,subsurface,surface|WeaponsFree/WeaponsTight|unrestricted|none",
      "scenarioId": "gauntlet-t3-ew-jammer-magazine-pressure",
      "tier": 3,
      "missionClass": "multi",
      "domains": ["air", "subsurface", "surface"],
      "roePair": "WeaponsFree/WeaponsTight",
      "emconClass": "unrestricted",
      "eventClass": "none",
      "catalogRefCount": 6,
      "intentHash": "<HASH:gauntlet-t3-ew-jammer-magazine-pressure>"
    },
    {
      "key": "escort|air,surface|WeaponsTight/WeaponsFree|unrestricted|none",
      "scenarioId": "gauntlet-t3-logistics-contact-lifecycle",
      "tier": 3,
      "missionClass": "escort",
      "domains": ["air", "surface"],
      "roePair": "WeaponsTight/WeaponsFree",
      "emconClass": "unrestricted",
      "eventClass": "none",
      "catalogRefCount": 3,
      "intentHash": "<HASH:gauntlet-t3-logistics-contact-lifecycle>"
    },
    {
      "key": "strike|facility,surface|WeaponsFree/WeaponsTight|engage-side-passive|none",
      "scenarioId": "gauntlet-t4-facility-strike-datalink",
      "tier": 4,
      "missionClass": "strike",
      "domains": ["facility", "surface"],
      "roePair": "WeaponsFree/WeaponsTight",
      "emconClass": "engage-side-passive",
      "eventClass": "none",
      "catalogRefCount": 3,
      "intentHash": "<HASH:gauntlet-t4-facility-strike-datalink>"
    },
    {
      "key": "escort|surface|WeaponsTight/WeaponsFree|unrestricted|inject",
      "scenarioId": "gauntlet-t4-ew-spoof-mount-offline",
      "tier": 4,
      "missionClass": "escort",
      "domains": ["surface"],
      "roePair": "WeaponsTight/WeaponsFree",
      "emconClass": "unrestricted",
      "eventClass": "inject",
      "catalogRefCount": 2,
      "intentHash": "<HASH:gauntlet-t4-ew-spoof-mount-offline>"
    },
    {
      "key": "multi|air,mine,subsurface,surface|WeaponsFree/WeaponsTight|unrestricted|none",
      "scenarioId": "gauntlet-t5-mine-hazard-theater",
      "tier": 5,
      "missionClass": "multi",
      "domains": ["air", "mine", "subsurface", "surface"],
      "roePair": "WeaponsFree/WeaponsTight",
      "emconClass": "unrestricted",
      "eventClass": "none",
      "catalogRefCount": 6,
      "intentHash": "<HASH:gauntlet-t5-mine-hazard-theater>"
    },
    {
      "key": "strike|surface|WeaponsFree/WeaponsTight|unrestricted|inject",
      "scenarioId": "gauntlet-t5-cascading-ew-logistics",
      "tier": 5,
      "missionClass": "strike",
      "domains": ["surface"],
      "roePair": "WeaponsFree/WeaponsTight",
      "emconClass": "unrestricted",
      "eventClass": "inject",
      "catalogRefCount": 2,
      "intentHash": "<HASH:gauntlet-t5-cascading-ew-logistics>"
    }
```

For the 3 retrofitted scenarios, find their existing cell objects (by `scenarioId`) and update only `intentHash` (to the freshly computed value) and `emconClass` (`gauntlet-t2-escort-passive` → `"engage-side-passive"`; `gauntlet-t3-emcon-phases` → `"static-mixed-posture"`; `gauntlet-t5-roe-change` → keep existing class, it was never EMCON-keyed).

- [ ] **Step 2: Add matching `promoted` entries to `index.yaml`**

For each of the 10 new scenarios, append (matching the existing entry shape, `recipes: [bootstrap-seed]` retained as the provenance convention for hand-authored fixed corpus additions — same as the original 24):

```yaml
  - scenarioId: gauntlet-t1-patrol-pkkill-boundary
    path: data/scenarios/gauntlet-t1-patrol-pkkill-boundary.policy.json
    tier: 1
    cellKey: "patrol|surface|WeaponsFree/WeaponsFree|unrestricted|none"
    intentHash: <HASH:gauntlet-t1-patrol-pkkill-boundary>
    promotedAt: matrix-expansion-2026-07-27
    recipes: [weapons-boundary-probe]
    noveltyScore: null
```

(repeat the shape above for the remaining 9 new scenarios, using each one's `dimensionsClaimed` as the `recipes:` list entry where it maps 1:1 to a Task 3 recipe id — e.g. `gauntlet-t2-strike-salvo-boundary` → `recipes: [weapons-boundary-probe, domain-pairing-shift]`; `gauntlet-t3-ew-jammer-magazine-pressure` → `recipes: [ew-jammer-timing, weapons-boundary-probe]`; use `[bootstrap-seed]` only for `gauntlet-t1-patrol-roe-tight-tight`, which claims no new dimension).

For the 3 retrofitted scenarios, find their existing `promoted` entries (by `scenarioId`) and update `intentHash` to the freshly computed value; leave `recipes: [bootstrap-seed]` and `promotedAt` unchanged (they were not re-promoted, just corrected in place).

- [ ] **Step 3: Validate both files parse and cross-reference correctly**

```bash
python3 - <<'EOF'
import json, yaml

cov = json.load(open("production/qa/gauntlet/corpus/coverage-map.json"))
idx = yaml.safe_load(open("production/qa/gauntlet/corpus/index.yaml"))

cov_ids = {c["scenarioId"] for c in cov["cells"]}
idx_ids = {p["scenarioId"] for p in idx["promoted"]}

new_ids = {
    "gauntlet-t1-patrol-pkkill-boundary", "gauntlet-t1-patrol-roe-tight-tight",
    "gauntlet-t2-strike-salvo-boundary", "gauntlet-t2-escort-air-domain-pairing",
    "gauntlet-t3-ew-jammer-magazine-pressure", "gauntlet-t3-logistics-contact-lifecycle",
    "gauntlet-t4-facility-strike-datalink", "gauntlet-t4-ew-spoof-mount-offline",
    "gauntlet-t5-mine-hazard-theater", "gauntlet-t5-cascading-ew-logistics",
}
missing_cov = new_ids - cov_ids
missing_idx = new_ids - idx_ids
assert not missing_cov, f"missing from coverage-map.json: {missing_cov}"
assert not missing_idx, f"missing from index.yaml: {missing_idx}"
print(f"coverage-map.json cells: {len(cov['cells'])}; index.yaml promoted: {len(idx['promoted'])}")
print("All 10 new scenarios registered in both files.")
EOF
```
Expected: `All 10 new scenarios registered in both files.`

- [ ] **Step 4: Commit**

```bash
git add production/qa/gauntlet/corpus/coverage-map.json production/qa/gauntlet/corpus/index.yaml
git commit -m "qa(forge): register 10 new + 3 retrofitted gauntlet scenarios in the corpus index/coverage-map"
```

---

### Task 11: Capability-gap backlog addendum (GAP-13 — logistics fuel state has no runtime signal)

**Files:**
- Modify: `docs/engineering/sim-capability-gap-backlog.md`

- [ ] **Step 1: Add a new GAP entry**

Find the `## PARTIAL — modelled, but with notable gaps` section and insert a new subsection after `### GAP-12 — Damage modelling depth` (renumbering not required — GAP ids are stable references, this is purely additive):

```markdown
### GAP-13 — Logistics fuel state (joker/bingo) has no runtime or fingerprint signal

- **Status:** `ScenarioLogisticsSettings` (`src/ProjectAegis.Sim/Scenario/ScenarioLogisticsSettings.cs`) is real, validated config (bingo ≤ joker fraction, capacity/burn > 0 enforced by the constructor). `FuelStateProjection.ResolveState` (`src/ProjectAegis.Delegation/Projection/FuelStateProjection.cs`) genuinely computes `BINGO`/`JOKER`/`NOMINAL` from it.
- **The gap:** `FuelStateProjection` has exactly one consumer — `UnitDetailProjection` (a UI unit-detail panel readout). Nothing in `ProjectAegis.Sim` or `ProjectAegis.Delegation` gates engagement, movement, or any order-log entry on fuel state, and no abort code in `data/glossary/abort_reason_manifest.json`'s `Logistics` family (`STRIKE_UNREACHABLE_FUEL`, `FERRY_UNREACHABLE_FUEL`) has a call site — confirmed by source sweep, zero hits outside the manifest/glossary declaration.
- **Consequence:** a gauntlet scenario can carry a fully valid `logistics` block, run a real batch, and produce a fingerprint that is byte-for-byte identical regardless of whether fuel ever crosses bingo. The QA Gauntlet Variability Expansion (2026-07-27, `docs/superpowers/specs/2026-07-27-gauntlet-variability-design.md`) needed a "logistics-fuel-pressure" dimension and found this while trying to make it mechanically provable — the corpus now ships it as a config-only dimension (schema validity checked at scenario load, no in-run behavioral proof) rather than silently claiming coverage it can't demonstrate.
- **Suggested resolution:** wire `FuelStateProjection`'s band transitions into the order log (an `AppendFuelBandChange`-style entry analogous to `AppendFuelBurn` in `FuelBurnOrderLogTests`), so joker/bingo crossings become a real, fingerprint-checkable event. This is an engine change (touches `ProjectAegis.Delegation`'s order log and likely `DecisionLog.ComputeFingerprint`) — out of scope for the gauntlet variability plan, which is data-only by design. Track as its own story when logistics realism is prioritized.

## Suggested prioritisation

Ordered by *QA value per unit of engine work*, not by realism ambition:

1. **GAP-01 / GAP-02 (vocabulary-only)** — cheapest to resolve and highest risk of silent misuse. Either implement the behaviour or make the authoring layer honest. These actively mislead today.
2. **GAP-13 (logistics fuel state has no runtime signal)** — small, well-scoped engine slice (order-log entry for band transitions); unblocks a real, checkable "logistics-fuel-pressure" gauntlet dimension without touching the fuel math itself.
3. **GAP-11 (catalog EMCON data)** — content work, no engine change, unblocks genuine per-platform EMCON testing.
```

Note: this replaces the existing `## Suggested prioritisation` section's numbered list (renumbering the existing items 2→3, 3→4, etc., keeping their text unchanged) to slot GAP-13 in at position 2 — do not duplicate the section header.

- [ ] **Step 2: Commit**

```bash
git add docs/engineering/sim-capability-gap-backlog.md
git commit -m "docs(gauntlet): add GAP-13 — logistics fuel state has no runtime/fingerprint signal

Found while making the logistics-fuel-pressure gauntlet dimension mechanically
checkable (docs/superpowers/plans/2026-07-27-gauntlet-variability.md). Config-only
proof adopted in the corpus in the meantime; wiring a real fuel-band order-log
event is queued here as future engine work, out of scope for this plan."
```

---

### Task 12: Full corpus regression + close-out

**Files:** none (verification only).

- [ ] **Step 1: Full test suite**

```bash
dotnet test ProjectAegis.sln
```
Expected: PASS, test count ≥ the baseline recorded before Task 4 (no C# was touched anywhere in this plan, so any red test means a scenario JSON edit broke an assumption a test hardcodes — investigate the specific test, do not adjust it to match unless the old assumption was itself wrong).

- [ ] **Step 2: Replay determinism gate**

```bash
# Follow the replay-verify skill's standard invocation for the affected scenarios.
```
Run the `replay-verify` skill against `gauntlet-t2-escort-passive`, `gauntlet-t3-emcon-phases`, and `gauntlet-t5-roe-change` (the 3 retrofits) at minimum — same `(scenario, seed)` pair run twice must produce identical fingerprints.

- [ ] **Step 3: Whole-corpus batch smoke at each tier's tick budget**

```bash
mkdir -p /tmp/gauntlet-corpus-smoke
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t1-patrol-a,gauntlet-t1-patrol-b,gauntlet-t1-patrol-c,gauntlet-t1-patrol-d,gauntlet-t1-patrol-pkkill-boundary,gauntlet-t1-patrol-roe-tight-tight \
  --seeds 42,7,123 --ticks 6 --csv-out /tmp/gauntlet-corpus-smoke/t1.csv
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t2-escort-a,gauntlet-t2-escort-passive,gauntlet-t2-strike-a,gauntlet-t2-strike-event,gauntlet-t2-strike-salvo-boundary,gauntlet-t2-escort-air-domain-pairing \
  --seeds 42,7,123 --ticks 10 --csv-out /tmp/gauntlet-corpus-smoke/t2.csv
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t3-id-roe,gauntlet-t3-emcon-phases,gauntlet-t3-escort-strike,gauntlet-t3-event-chain,gauntlet-t3-ew-jammer-magazine-pressure,gauntlet-t3-logistics-contact-lifecycle \
  --seeds 42,7,123 --ticks 16 --csv-out /tmp/gauntlet-corpus-smoke/t3.csv
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t4-asymm-roe,gauntlet-t4-multi-mission,gauntlet-t4-random-inject,gauntlet-t4-weighted,gauntlet-t4-facility-strike-datalink,gauntlet-t4-ew-spoof-mount-offline \
  --seeds 42,7,123 --ticks 24 --csv-out /tmp/gauntlet-corpus-smoke/t4.csv
dotnet run -c Release --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios gauntlet-t5-cascade,gauntlet-t5-dynamic-obj,gauntlet-t5-roe-change,gauntlet-t5-theater,gauntlet-t5-mine-hazard-theater,gauntlet-t5-cascading-ew-logistics \
  --seeds 42,7,123 --ticks 40 --csv-out /tmp/gauntlet-corpus-smoke/t5.csv
```

For each tier CSV, run `gauntlet_oracle_eval` across all 6 policies in that tier's `--policy-dir` and confirm `"allPassed": true` corpus-wide (not just for this plan's own additions) — this is the "whole-corpus regression at correct per-tier budgets" gate the spec's Verification section requires.

- [ ] **Step 4: `detect_changes` scope check**

```
detect_changes({scope: "compare", base_ref: "main"})
```
Expected: only the files listed in this plan's File map are touched — no unexpected C#, no `tools/qa-gauntlet/forge_scorecard.py`, no locked-eval files.

- [ ] **Step 5: Final commit (if Step 4 required any cleanup) or close out**

If `detect_changes` is clean, no further commit is needed — Tasks 1–11 already committed everything. If it flags anything unexpected, fix and commit with:

```bash
git commit -m "qa(gauntlet): corpus regression cleanup post variability expansion"
```

---

## Self-review notes (writing-plans skill requirement)

**Spec coverage:**
- Tier matrix new rows → Task 2. ✓
- Scenario budget 4→6 → Task 2. ✓
- 9 forge recipes → Task 3. ✓
- EMCON stand-in replacement (real `emcon` block) → Task 4 (found and fixed 3 instances, not just the 1 named in the bug). ✓
- Dimension-coverage assertion, mechanically checkable → Task 1 (script + signal map), exercised in every scenario task's verification step. ✓
- Expect regeneration at correct tier ticks for all new/modified scenarios → every scenario task's regen step. ✓
- Prerequisite note on `BUG-forge-scorecard-filename-vs-policy-id` without fixing it → Global Constraints + Task 2 Step 4. ✓
- Tier 1/2 already-complete-scenario handling → Task 4 (retrofit only the one bug-named + 2 sibling EMCON-stand-in scenarios) + Tasks 5–6 (supplement with 2 new scenarios each, not a full retrofit of the green set).

**Placeholder scan:** no "TBD"/"implement later" strings. The only intentionally-deferred content is `gauntlet.expect` numeric bounds, which every task states must come from a real batch CSV — this is the project's own documented discipline, not an unfinished plan step.

**Type/name consistency:** `dimensionsClaimed` values are spelled identically everywhere they appear (Task 1's `dimension-coverage-signals.json` keys ↔ every scenario's `gauntlet.dimensionsClaimed` entries ↔ Task 10's `recipes:` mapping). `verify_dimension_coverage.py`'s `--policy`/`--csv`/`--control-csv` flags are used identically in every task's verification step.
