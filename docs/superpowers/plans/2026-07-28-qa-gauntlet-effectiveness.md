# QA Gauntlet Effectiveness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement `docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md` — a canonical oracles-as-code gauntlet driver (`run-gauntlet.sh` + `evaluate_run.py` with golden anchors, token coverage, roving seeds), strict-key policy validation in the C# CLI, and a `/qa-gauntlet-calibrate` saboteur skill that measures oracle kill-rate against curated mutants.

**Architecture:** Hybrid split by trust requirement. One small C# addition (strict unknown-key rejection for `gauntlet.*`, hooked into the existing `gauntlet_oracle_eval` per-policy loop, unit-tested in xunit). Everything else is Python/bash in `tools/qa-gauntlet/` with pytest, following the existing `forge_scorecard.py` + `test_forge_scorecard.py` precedent. The saboteur applies committed `.patch` mutants inside disposable git worktrees, never commits from them, and reports a mutants×oracles kill matrix.

**Tech Stack:** .NET 8 (`~/.dotnet/dotnet`, xunit), Python 3 + pytest, bash, git worktrees, Graphite (`gt`).

## Global Constraints

- **Locked — never edit, never target with mutants:** `src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs`, `src/ProjectAegis.Delegation.Demo/Program.cs` batch internals, ReplayGolden fixtures, `src/ProjectAegis.Delegation.UnityAdapter/Baltic/DelegationBridge.cs`, Baltic v2 golden hash `17144800277401907079`, `.github/workflows/gauntlet-oracle.yml`.
- **No engine behavior changes:** nothing in `ProjectAegis.Sim` / `ProjectAegis.Delegation` is edited on the branch (mutant patches exist only as committed *files* under `tools/qa-gauntlet/mutants/`; they are applied only inside throwaway worktrees).
- **Graphite only:** branch created with `gt create`; no raw `git push`, no `gh pr create`.
- **GitNexus:** `impact({target, direction:"upstream", repo:"/home/username01/cmano-clone"})` before editing any existing C# symbol; `detect_changes({repo:"/home/username01/cmano-clone"})` before every commit — confirm only expected files changed.
- **Test count is monotonic:** baseline is **1912** (2026-07-28 runs); every C# task adds tests, none may be deleted.
- **dotnet is NOT on PATH in this environment.** Every shell step that needs it must use: `export PATH="$HOME/.dotnet:$PATH"` (scripts do their own resolution — that's part of the point).
- **All `dotnet test` / `dotnet build` / `dotnet run` commands run from the repo root** `/home/username01/cmano-clone`.
- **Legal `gauntlet.*` key whitelist (verified against source 2026-07-28):** `intent`, `oracle`, `catalogRefs`, `units`, `expect`, `expectCi`, `runId`, `tier`. Legacy warn-only: `emcon` (3 shipped policies carry it; see `BUG-gauntlet-emcon-dimension-not-exercised`; flips to error when the 2026-07-27 variability plan lands, which will also add `dimensionsClaimed` to the whitelist).
- Commit messages: `feat(cli): …` for C#, `qa(gauntlet): …` for tools/skills/data, per repo convention.

---

### Task 1: Branch setup

**Files:** none (branch only)

- [ ] **Step 1: Verify clean-enough state and create the Graphite branch**

```bash
cd /home/username01/cmano-clone
git status --short   # pre-existing WIP docs are expected; do NOT stage them in later tasks
gt create qa/gauntlet-effectiveness -m "qa(gauntlet): start effectiveness work (spec 2026-07-28)"
```

Expected: branch `qa/gauntlet-effectiveness` exists and is current. (If `gt create` refuses an empty commit, run it with `--all=false` after `git commit --allow-empty -m` equivalent: `git checkout -b qa/gauntlet-effectiveness && gt track`.)

- [ ] **Step 2: Commit the spec and this plan to the branch**

```bash
git add docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md docs/superpowers/plans/2026-07-28-qa-gauntlet-effectiveness.md
git commit -m "docs(qa): qa-gauntlet effectiveness spec + implementation plan"
```

---

### Task 2: C# strict-key validation — `GauntletPolicyStrictKeys`

**Files:**
- Create: `src/ProjectAegis.MissionEditor.Cli/GauntletPolicyStrictKeys.cs`
- Test: `src/ProjectAegis.MissionEditor.Cli.Tests/GauntletPolicyStrictKeysTests.cs`

**Interfaces:**
- Produces: `public static class GauntletPolicyStrictKeys` with
  `public static GauntletStrictKeyReport Check(string policyJson)` where
  `public sealed record GauntletStrictKeyReport(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)`.
  Consumed by Task 3 (`GauntletOracleEvalCommand`).

- [ ] **Step 1: GitNexus impact check (recording step, no edit yet)**

Run `impact({target: "GauntletOracleEvalCommand", direction: "upstream", repo: "/home/username01/cmano-clone"})`. Expected: LOW/MEDIUM (CLI command; callers are Program.cs dispatch + tests). Record the risk level for the commit message body. CRITICAL → stop and quarantine per skill rules (not expected).

- [ ] **Step 2: Write the failing tests**

Create `src/ProjectAegis.MissionEditor.Cli.Tests/GauntletPolicyStrictKeysTests.cs`:

```csharp
namespace ProjectAegis.MissionEditor.Cli.Tests;

using ProjectAegis.MissionEditor.Cli;
using Xunit;

public class GauntletPolicyStrictKeysTests
{
    private static string Policy(string gauntletBody) =>
        $$"""{ "friendlyRoe": "WeaponsFree", "id": "p1", "gauntlet": { {{gauntletBody}} } }""";

    private const string ValidCore =
        """
        "intent": "t", "oracle": "o", "runId": "r", "tier": 1,
        "catalogRefs": ["k-31-visby-2009"],
        "units": [{ "unitId": "u1", "platformId": "k-31-visby-2009", "domain": "surface", "side": "blue" }],
        "expect": { "side": "BLUE", "minKills": 1, "maxMissilesFired": 5, "minDenials": 2,
                    "maxDenials": 10, "minScore": 50.0, "maxScore": 90.0,
                    "requireNonEmptyFingerprint": true }
        """;

    [Fact]
    public void Valid_ladder_policy_has_no_errors_or_warnings()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore));
        Assert.Empty(report.Errors);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void Legacy_gauntlet_emcon_is_warning_not_error()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore + """, "emcon": "phased" """));
        Assert.Empty(report.Errors);
        var w = Assert.Single(report.Warnings);
        Assert.Contains("emcon", w);
        Assert.Contains("top-level", w); // suggests the real engine-bound block
    }

    [Fact]
    public void Unknown_gauntlet_key_is_error_listing_allowed_keys()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore + """, "emconPhases": [] """));
        var e = Assert.Single(report.Errors);
        Assert.Contains("emconPhases", e);
        Assert.Contains("expect", e); // allowed-keys listing present
    }

    [Fact]
    public void Unknown_expect_key_is_error()
    {
        var body = ValidCore.Replace("\"minScore\": 50.0", "\"minScore\": 50.0, \"minimumScore\": 1");
        var report = GauntletPolicyStrictKeys.Check(Policy(body));
        var e = Assert.Single(report.Errors);
        Assert.Contains("gauntlet.expect.minimumScore", e);
    }

    [Fact]
    public void Unknown_unit_key_is_error()
    {
        var body = ValidCore.Replace("\"side\": \"blue\"", "\"side\": \"blue\", \"emcon\": \"Off\"");
        var report = GauntletPolicyStrictKeys.Check(Policy(body));
        var e = Assert.Single(report.Errors);
        Assert.Contains("gauntlet.units[0].emcon", e);
    }

    [Fact]
    public void ExpectCi_block_is_allowed_and_checked()
    {
        var report = GauntletPolicyStrictKeys.Check(
            Policy(ValidCore + """, "expectCi": { "side": "BLUE", "bogusKey": 1 } """));
        var e = Assert.Single(report.Errors);
        Assert.Contains("gauntlet.expectCi.bogusKey", e);
    }

    [Fact]
    public void Missing_or_invalid_gauntlet_block_yields_no_report_entries()
    {
        // Absence is handled by the evaluator ("missing gauntlet.expect"), not strict keys.
        Assert.Empty(GauntletPolicyStrictKeys.Check("""{ "id": "p1" }""").Errors);
        Assert.Empty(GauntletPolicyStrictKeys.Check("not json").Errors);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests -v minimal --filter GauntletPolicyStrictKeysTests
```

Expected: build FAILS with "GauntletPolicyStrictKeys not found".

- [ ] **Step 4: Implement `GauntletPolicyStrictKeys`**

Create `src/ProjectAegis.MissionEditor.Cli/GauntletPolicyStrictKeys.cs`:

```csharp
namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;

/// <summary>
/// Strict unknown-key validation for the qa-gauntlet policy block.
/// Root-cause guard for BUG-gauntlet-emcon-dimension-not-exercised: unknown keys under
/// <c>gauntlet.*</c> were silently dropped by System.Text.Json, letting an entire ladder
/// dimension go inert unnoticed. Whitelist = union of ScenarioGauntletJsonDto properties
/// and keys consumed by GauntletOracleEvaluator (expect/expectCi) — see spec 2026-07-28.
/// </summary>
public static class GauntletPolicyStrictKeys
{
    private static readonly HashSet<string> GauntletKeys = new(StringComparer.Ordinal)
    { "intent", "oracle", "catalogRefs", "units", "expect", "expectCi", "runId", "tier" };

    private static readonly HashSet<string> ExpectKeys = new(StringComparer.Ordinal)
    {
        "side", "minKills", "maxMissilesFired", "minDenials", "maxDenials",
        "minScore", "maxScore", "requireNonEmptyFingerprint",
        "requireFingerprintSubstrings", "requireTrueLaunchedShooters",
    };

    private static readonly HashSet<string> UnitKeys = new(StringComparer.Ordinal)
    { "unitId", "platformId", "domain", "side" };

    // Grandfathered until the 2026-07-27 variability plan retrofits the three shipped
    // EMCON policies; then move "emcon" from warn to error and add "dimensionsClaimed"
    // to GauntletKeys (that plan owns both changes).
    private static readonly HashSet<string> LegacyWarnKeys = new(StringComparer.Ordinal)
    { "emcon" };

    public static GauntletStrictKeyReport Check(string policyJson)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(policyJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("gauntlet", out var gauntlet)
                || gauntlet.ValueKind != JsonValueKind.Object)
            {
                return new GauntletStrictKeyReport(errors, warnings);
            }

            foreach (var prop in gauntlet.EnumerateObject())
            {
                if (GauntletKeys.Contains(prop.Name))
                {
                    continue;
                }

                if (LegacyWarnKeys.Contains(prop.Name))
                {
                    warnings.Add(
                        $"gauntlet.{prop.Name}: legacy stand-in key ignored by the engine — real EMCON is the top-level \"emcon\" block (BUG-gauntlet-emcon-dimension-not-exercised)");
                    continue;
                }

                errors.Add(
                    $"gauntlet.{prop.Name}: unknown key (silently ignored by the engine). Allowed: {string.Join(", ", GauntletKeys.OrderBy(k => k, StringComparer.Ordinal))}");
            }

            CheckObjectKeys(gauntlet, "expect", ExpectKeys, errors);
            CheckObjectKeys(gauntlet, "expectCi", ExpectKeys, errors);

            if (gauntlet.TryGetProperty("units", out var units) && units.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var unit in units.EnumerateArray())
                {
                    if (unit.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in unit.EnumerateObject())
                        {
                            if (!UnitKeys.Contains(prop.Name))
                            {
                                errors.Add(
                                    $"gauntlet.units[{i}].{prop.Name}: unknown key. Allowed: {string.Join(", ", UnitKeys.OrderBy(k => k, StringComparer.Ordinal))}");
                            }
                        }
                    }

                    i++;
                }
            }
        }
        catch (JsonException)
        {
            // Invalid JSON is surfaced by the evaluator; strict keys stay silent.
        }

        return new GauntletStrictKeyReport(errors, warnings);
    }

    private static void CheckObjectKeys(
        JsonElement gauntlet, string blockName, HashSet<string> allowed, List<string> errors)
    {
        if (!gauntlet.TryGetProperty(blockName, out var block) || block.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var prop in block.EnumerateObject())
        {
            if (!allowed.Contains(prop.Name))
            {
                errors.Add(
                    $"gauntlet.{blockName}.{prop.Name}: unknown key. Allowed: {string.Join(", ", allowed.OrderBy(k => k, StringComparer.Ordinal))}");
            }
        }
    }
}

/// <summary>Strict-key findings: errors fail oracle eval; warnings are reported only.</summary>
public sealed record GauntletStrictKeyReport(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests -v minimal --filter GauntletPolicyStrictKeysTests
```

Expected: PASS, 7/7.

- [ ] **Step 6: detect_changes + commit**

Run `detect_changes({repo: "/home/username01/cmano-clone"})` — expected: only the two new files.

```bash
git add src/ProjectAegis.MissionEditor.Cli/GauntletPolicyStrictKeys.cs src/ProjectAegis.MissionEditor.Cli.Tests/GauntletPolicyStrictKeysTests.cs
git commit -m "feat(cli): strict unknown-key validation for gauntlet.* policy blocks

Root-cause guard for BUG-gauntlet-emcon-dimension-not-exercised.
Spec: docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md"
```

---

### Task 3: Wire strict keys into `gauntlet_oracle_eval`

**Files:**
- Modify: `src/ProjectAegis.MissionEditor.Cli/GauntletOracleEvalCommand.cs` (per-policy loop, lines ~84–136)
- Test: `src/ProjectAegis.MissionEditor.Cli.Tests/GauntletOracleEvalStrictKeysTests.cs`

**Interfaces:**
- Consumes: `GauntletPolicyStrictKeys.Check(string)` from Task 2.
- Produces: `gauntlet_oracle_eval` scenario entries gain a `warnings` array; strict-key errors append to `failures` and force `passed: false` / exit 1. Tasks 5 and 8 rely on this exit behavior.

- [ ] **Step 1: GitNexus impact on the exact symbol**

`impact({target: "Run", file_path: "src/ProjectAegis.MissionEditor.Cli/GauntletOracleEvalCommand.cs", direction: "upstream", repo: "/home/username01/cmano-clone"})`. Expected LOW/MEDIUM. Record.

- [ ] **Step 2: Write the failing test**

Create `src/ProjectAegis.MissionEditor.Cli.Tests/GauntletOracleEvalStrictKeysTests.cs`:

```csharp
namespace ProjectAegis.MissionEditor.Cli.Tests;

using ProjectAegis.MissionEditor.Cli;
using Xunit;

public class GauntletOracleEvalStrictKeysTests
{
    private const string Csv =
        "scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint\n"
        + "p-strict,42,BLUE,70,1,1,6,TOKEN_A TOKEN_B\n";

    private static string WriteTemp(string name, string content)
    {
        var dir = Directory.CreateTempSubdirectory("strictkeys").FullName;
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static string PolicyJson(string extraGauntletKey) =>
        $$"""
        {
          "id": "p-strict",
          "gauntlet": {
            "intent": "strict-key test", "tier": 1, "runId": "t"{{extraGauntletKey}},
            "expect": { "side": "BLUE", "minKills": 1, "maxScore": 90.0, "minScore": 50.0 }
          }
        }
        """;

    [Fact]
    public void Unknown_key_fails_eval_with_exit_1()
    {
        var policy = WriteTemp("p.policy.json", PolicyJson(", \"emconPhases\": []"));
        var csv = WriteTemp("r.csv", Csv);
        using var sw = new StringWriter();
        var exit = GauntletOracleEvalCommand.Run(policy, null, csv, null, sw);
        Assert.Equal(1, exit);
        Assert.Contains("emconPhases", sw.ToString());
    }

    [Fact]
    public void Legacy_emcon_warns_but_still_passes()
    {
        var policy = WriteTemp("p.policy.json", PolicyJson(", \"emcon\": \"phased\""));
        var csv = WriteTemp("r.csv", Csv);
        using var sw = new StringWriter();
        var exit = GauntletOracleEvalCommand.Run(policy, null, csv, null, sw);
        Assert.Equal(0, exit);
        Assert.Contains("warnings", sw.ToString());
        Assert.Contains("emcon", sw.ToString());
    }
}
```

- [ ] **Step 3: Run to verify failure**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests -v minimal --filter GauntletOracleEvalStrictKeysTests
```

Expected: `Unknown_key_fails_eval_with_exit_1` FAILS (exit is 0 today — key silently ignored); `Legacy_emcon_warns_but_still_passes` FAILS (no `warnings` field yet).

- [ ] **Step 4: Implement the wiring**

In `GauntletOracleEvalCommand.Run`, inside the `foreach (var path in policyPaths)` loop, after `policyJson` is read and `scenarioId` resolved (after line ~116), insert the strict-key check, and extend the anonymous result object:

```csharp
            var strict = GauntletPolicyStrictKeys.Check(policyJson);

            var filteredRows = GauntletOracleEvaluator.ParseCsvRows(resultsCsv)
                .Where(r => string.Equals(r.ScenarioId, scenarioId, StringComparison.Ordinal))
                .ToList();
            var filteredCsv = BuildCsv(filteredRows);
            var eval = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policyJson, filteredCsv, profile);

            var failures = eval.Failures.Concat(strict.Errors).ToArray();
            var passed = eval.Passed && strict.Errors.Count == 0;

            scenarioResults.Add(new
            {
                scenario = scenarioId,
                passed,
                failures,
                warnings = strict.Warnings,
                rows = filteredRows.Count,
            });

            if (!passed)
            {
                allPassed = false;
            }
```

(This replaces the existing `filteredRows`…`if (!eval.Passed)` block; the earlier read/id-error paths are unchanged. Do not touch `GauntletOracleEvaluator` itself — it is locked.)

- [ ] **Step 5: Run the new tests, then the full CLI test project**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests -v minimal
```

Expected: all pass (113 pre-existing + 9 new).

- [ ] **Step 6: Prove the shipped ladder still passes oracle eval (legacy emcon = warn only)**

```bash
export PATH="$HOME/.dotnet:$PATH"
mkdir -p /tmp/claude-1000/-home-username01-cmano-clone/*/scratchpad/strict-ladder 2>/dev/null || true
S=$(ls -d /tmp/claude-1000/-home-username01-cmano-clone/*/scratchpad | head -1)/strict-ladder; mkdir -p "$S"
cp data/scenarios/gauntlet-t2-escort-passive.policy.json "$S/"
dotnet run --project src/ProjectAegis.Delegation.Demo --no-build -- --batch \
  --scenarios gauntlet-t2-escort-passive --seeds 42,7,123 --ticks 10 --csv-out "$S/r.csv"
dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- gauntlet_oracle_eval \
  --policy-dir "$S" --csv "$S/r.csv"; echo "EXIT=$?"
```

Expected: `EXIT=0`, output contains a `warnings` entry mentioning `emcon`.

- [ ] **Step 7: detect_changes + commit**

`detect_changes` — expected: `GauntletOracleEvalCommand.cs` + new test file only.

```bash
git add src/ProjectAegis.MissionEditor.Cli/GauntletOracleEvalCommand.cs src/ProjectAegis.MissionEditor.Cli.Tests/GauntletOracleEvalStrictKeysTests.cs
git commit -m "feat(cli): gauntlet_oracle_eval enforces strict gauntlet.* keys (errors fail, legacy emcon warns)"
```

---

### Task 4: `evaluate_run.py` core — CSV parsing, stability, sanity/seed-sensitivity

**Files:**
- Create: `tools/qa-gauntlet/evaluate_run.py`
- Test: `tools/qa-gauntlet/test_evaluate_run.py`

**Interfaces:**
- Produces (used by Tasks 5–9 and the driver):
  - `parse_results_csv(path: Path) -> list[Row]` — `Row = dataclasses` with fields `scenario_id: str, seed: str, side: str, score: str, kills: int, missiles: int, denials: int, fingerprint: str` (score kept as string for byte-stable hashing; numeric checks parse locally).
  - `oracle_stability(tier_dir: Path, rows, expected_scenarios: list[str], seeds: list[str]) -> Oracle`
  - `oracle_sanity(rows, seeds: list[str]) -> Oracle`
  - `Oracle = {"name": str, "status": "pass"|"fail"|"warn", "evidence": list[str]}`
  - CLI: `evaluate_run.py tier --tier-dir D --scenarios a,b --anchor-seeds 42,7,123 [--roving-seeds x,y] [--goldens F] [--expected-tokens F] [--out D/verdict.json]` → exit 0 iff no oracle failed.
  - `write_verdict(path, tier_name, oracles) -> bool` (returns overall pass)

- [ ] **Step 1: Write the failing tests**

Create `tools/qa-gauntlet/test_evaluate_run.py`:

```python
#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/evaluate_run.py (oracles-as-code, spec 2026-07-28)."""

from __future__ import annotations

import json
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from evaluate_run import (  # noqa: E402
    Row,
    oracle_sanity,
    oracle_stability,
    parse_results_csv,
    write_verdict,
)

HEADER = "scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint\n"


def row_line(sid="s1", seed="42", score="70", fp="CATALOG_UNIT:x:surface T|1,extra"):
    return f"{sid},{seed},BLUE,{score},1,2,6,{fp}\n"


def make_csv(tmp_path, lines, name="results.csv"):
    p = tmp_path / name
    p.write_text(HEADER + "".join(lines), encoding="utf-8")
    return p


def test_parse_preserves_fingerprint_with_commas(tmp_path):
    p = make_csv(tmp_path, [row_line(fp="A|1,2 B,C")])
    rows = parse_results_csv(p)
    assert len(rows) == 1
    assert rows[0].fingerprint == "A|1,2 B,C"
    assert rows[0].kills == 1 and rows[0].missiles == 2 and rows[0].denials == 6


def test_stability_passes_on_full_grid_and_clean_log(tmp_path):
    make_csv(tmp_path, [row_line(seed=s) for s in ("42", "7")])
    (tmp_path / "run.log").write_text("batch ok\n", encoding="utf-8")
    o = oracle_stability(tmp_path, parse_results_csv(tmp_path / "results.csv"), ["s1"], ["42", "7"])
    assert o["status"] == "pass"


def test_stability_fails_on_missing_row(tmp_path):
    make_csv(tmp_path, [row_line(seed="42")])
    (tmp_path / "run.log").write_text("ok\n", encoding="utf-8")
    o = oracle_stability(tmp_path, parse_results_csv(tmp_path / "results.csv"), ["s1"], ["42", "7"])
    assert o["status"] == "fail"
    assert any("s1" in e and "7" in e for e in o["evidence"])


def test_stability_fails_on_exception_in_log(tmp_path):
    make_csv(tmp_path, [row_line()])
    (tmp_path / "run.log").write_text("Unhandled exception: boom\n", encoding="utf-8")
    o = oracle_stability(tmp_path, parse_results_csv(tmp_path / "results.csv"), ["s1"], ["42"])
    assert o["status"] == "fail"


def test_sanity_fails_on_empty_fingerprint_and_nonfinite_score(tmp_path):
    p = make_csv(tmp_path, [row_line(fp=""), row_line(seed="7", score="NaN")])
    o = oracle_sanity(parse_results_csv(p), ["42", "7"])
    assert o["status"] == "fail"
    assert len(o["evidence"]) == 2


def test_sanity_fails_on_seed_insensitive_scenario(tmp_path):
    p = make_csv(tmp_path, [row_line(seed="42", fp="SAME"), row_line(seed="7", fp="SAME")])
    o = oracle_sanity(parse_results_csv(p), ["42", "7"])
    assert o["status"] == "fail"
    assert any("seed-insensitive" in e for e in o["evidence"])


def test_write_verdict_overall(tmp_path):
    ok = write_verdict(tmp_path / "verdict.json", "tier-1",
                       [{"name": "stability", "status": "pass", "evidence": []},
                        {"name": "sanity", "status": "warn", "evidence": ["w"]}])
    assert ok is True
    v = json.loads((tmp_path / "verdict.json").read_text())
    assert v["pass"] is True and v["tier"] == "tier-1"
    ok2 = write_verdict(tmp_path / "v2.json", "tier-1",
                        [{"name": "sanity", "status": "fail", "evidence": ["x"]}])
    assert ok2 is False
```

- [ ] **Step 2: Run to verify failure**

```bash
cd /home/username01/cmano-clone && python3 -m pytest tools/qa-gauntlet/test_evaluate_run.py -v
```

Expected: FAIL — `ModuleNotFoundError: evaluate_run`.

- [ ] **Step 3: Implement the core**

Create `tools/qa-gauntlet/evaluate_run.py`:

```python
#!/usr/bin/env python3
"""QA Gauntlet oracle aggregator — all ladder oracles as code.

Spec: docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md
Modes:
  tier  — evaluate one tier dir (stability, determinism, victory/ROE via
          oracle-eval.json, goldens, sanity) -> tier-N/verdict.json
  run   — aggregate tier verdicts + run-wide token coverage -> verdict.json
  bless — rewrite goldens/anchors.json from a green run's CSVs
Exit 0 iff no oracle failed (warnings never fail).
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import sys
from dataclasses import dataclass
from pathlib import Path

CSV_HEADER = "scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint"
ERROR_LOG_RE = re.compile(r"unhandled exception|fatal|stack trace", re.IGNORECASE)


@dataclass
class Row:
    scenario_id: str
    seed: str
    side: str
    score: str
    kills: int
    missiles: int
    denials: int
    fingerprint: str


def parse_results_csv(path: Path) -> list[Row]:
    rows: list[Row] = []
    lines = path.read_text(encoding="utf-8").splitlines()
    if not lines or not lines[0].startswith("scenarioId,"):
        raise ValueError(f"unexpected CSV header in {path}")
    for line in lines[1:]:
        if not line.strip():
            continue
        parts = line.split(",", 7)  # fingerprint is last and may contain commas
        if len(parts) != 8:
            raise ValueError(f"malformed CSV row in {path}: {line[:80]}")
        rows.append(Row(parts[0], parts[1], parts[2], parts[3],
                        int(parts[4]), int(parts[5]), int(parts[6]), parts[7]))
    return rows


def _oracle(name: str, failures: list[str], warnings: list[str] | None = None) -> dict:
    status = "fail" if failures else ("warn" if warnings else "pass")
    return {"name": name, "status": status, "evidence": failures + (warnings or [])}


def oracle_stability(tier_dir: Path, rows: list[Row],
                     expected_scenarios: list[str], seeds: list[str]) -> dict:
    failures: list[str] = []
    have = {(r.scenario_id, r.seed) for r in rows}
    for sid in expected_scenarios:
        for seed in seeds:
            if (sid, seed) not in have:
                failures.append(f"missing row: scenario={sid} seed={seed}")
    for log_name in ("run.log", "run-repeat.log"):
        log = tier_dir / log_name
        if log.exists():
            for i, line in enumerate(log.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
                if ERROR_LOG_RE.search(line):
                    failures.append(f"{log_name}:{i}: {line.strip()[:160]}")
    return _oracle("stability", failures)


def oracle_sanity(rows: list[Row], seeds: list[str]) -> dict:
    failures: list[str] = []
    for r in rows:
        try:
            if not math.isfinite(float(r.score)):
                failures.append(f"non-finite score: {r.scenario_id} seed={r.seed} score={r.score}")
        except ValueError:
            failures.append(f"non-numeric score: {r.scenario_id} seed={r.seed} score={r.score}")
        if not r.fingerprint.strip():
            failures.append(f"empty fingerprint: {r.scenario_id} seed={r.seed}")
    if len(seeds) > 1:
        by_scenario: dict[str, set[str]] = {}
        for r in rows:
            by_scenario.setdefault(r.scenario_id, set()).add(r.fingerprint)
        for sid, fps in sorted(by_scenario.items()):
            n_rows = sum(1 for r in rows if r.scenario_id == sid)
            if n_rows > 1 and len(fps) == 1:
                failures.append(f"seed-insensitive: {sid} produced 1 distinct fingerprint across {n_rows} seeds")
    return _oracle("sanity", failures)


def write_verdict(path: Path, tier: str, oracles: list[dict]) -> bool:
    overall = all(o["status"] != "fail" for o in oracles)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps({"tier": tier, "pass": overall,
                                "oracles": {o["name"]: {"status": o["status"], "evidence": o["evidence"]}
                                            for o in oracles}}, indent=2) + "\n",
                    encoding="utf-8")
    return overall


def main(argv: list[str]) -> int:  # extended in later tasks
    parser = argparse.ArgumentParser(prog="evaluate_run.py")
    sub = parser.add_subparsers(dest="mode", required=True)
    tier_p = sub.add_parser("tier")
    tier_p.add_argument("--tier-dir", required=True, type=Path)
    tier_p.add_argument("--scenarios", required=True)
    tier_p.add_argument("--anchor-seeds", default="42,7,123")
    tier_p.add_argument("--roving-seeds", default="")
    tier_p.add_argument("--goldens", type=Path)
    tier_p.add_argument("--out", type=Path)
    args = parser.parse_args(argv)

    if args.mode == "tier":
        tier_dir = args.tier_dir
        scenarios = [s for s in args.scenarios.split(",") if s]
        seeds = [s for s in args.anchor_seeds.split(",") if s] + \
                [s for s in args.roving_seeds.split(",") if s]
        rows = parse_results_csv(tier_dir / "results.csv")
        oracles = [
            oracle_stability(tier_dir, rows, scenarios, seeds),
            oracle_sanity(rows, seeds),
        ]
        out = args.out or (tier_dir / "verdict.json")
        ok = write_verdict(out, tier_dir.name, oracles)
        print(json.dumps({"tier": tier_dir.name, "pass": ok}))
        return 0 if ok else 1
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
```

- [ ] **Step 4: Run tests to verify pass**

```bash
cd /home/username01/cmano-clone && python3 -m pytest tools/qa-gauntlet/test_evaluate_run.py -v
```

Expected: PASS 7/7. Also confirm the pre-existing suite still passes: `python3 -m pytest tools/qa-gauntlet/ -q`.

- [ ] **Step 5: detect_changes + commit**

```bash
git add tools/qa-gauntlet/evaluate_run.py tools/qa-gauntlet/test_evaluate_run.py
git commit -m "qa(gauntlet): evaluate_run.py core — CSV parsing, stability + sanity oracles (TDD)"
```

---

### Task 5: Determinism + victory/ROE oracles in `evaluate_run.py`

**Files:**
- Modify: `tools/qa-gauntlet/evaluate_run.py`
- Modify: `tools/qa-gauntlet/test_evaluate_run.py`

**Interfaces:**
- Produces: `oracle_determinism(tier_dir: Path) -> Oracle` (byte-diff of sorted `results.csv` vs `results-repeat.csv`; missing repeat file = fail); `oracle_victory(tier_dir: Path) -> Oracle` (reads `tier_dir/oracle-eval.json` written by the CLI; `allPassed: false` or file missing = fail; scenario `warnings` surfaced as warn evidence). Both wired into `tier` mode.

- [ ] **Step 1: Add failing tests** (append to `test_evaluate_run.py`)

```python
from evaluate_run import oracle_determinism, oracle_victory  # noqa: E402


def test_determinism_pass_ignores_row_order(tmp_path):
    make_csv(tmp_path, [row_line(seed="42"), row_line(seed="7")])
    make_csv(tmp_path, [row_line(seed="7"), row_line(seed="42")], name="results-repeat.csv")
    assert oracle_determinism(tmp_path)["status"] == "pass"


def test_determinism_fails_on_fingerprint_drift(tmp_path):
    make_csv(tmp_path, [row_line(fp="A")])
    make_csv(tmp_path, [row_line(fp="B")], name="results-repeat.csv")
    o = oracle_determinism(tmp_path)
    assert o["status"] == "fail"
    assert (tmp_path / "determinism-diff.txt").exists()


def test_determinism_fails_when_repeat_missing(tmp_path):
    make_csv(tmp_path, [row_line()])
    assert oracle_determinism(tmp_path)["status"] == "fail"


def test_victory_reads_oracle_eval_json(tmp_path):
    (tmp_path / "oracle-eval.json").write_text(json.dumps(
        {"ok": True, "allPassed": True,
         "scenarios": [{"scenario": "s1", "passed": True, "failures": [], "warnings": ["legacy emcon"]}]}))
    o = oracle_victory(tmp_path)
    assert o["status"] == "warn"
    assert any("legacy emcon" in e for e in o["evidence"])


def test_victory_fails_on_allpassed_false_or_missing(tmp_path):
    (tmp_path / "oracle-eval.json").write_text(json.dumps(
        {"ok": False, "allPassed": False,
         "scenarios": [{"scenario": "s1", "passed": False, "failures": ["score out of bounds"]}]}))
    assert oracle_victory(tmp_path)["status"] == "fail"
    assert oracle_victory(tmp_path / "nowhere")["status"] == "fail"
```

- [ ] **Step 2: Run to verify the new tests fail** — `python3 -m pytest tools/qa-gauntlet/test_evaluate_run.py -v` → ImportError on the two new names.

- [ ] **Step 3: Implement** (add to `evaluate_run.py`; wire both into the `tier` mode oracle list between stability and sanity)

```python
def oracle_determinism(tier_dir: Path) -> dict:
    first, repeat = tier_dir / "results.csv", tier_dir / "results-repeat.csv"
    if not first.exists() or not repeat.exists():
        return _oracle("determinism", [f"missing CSV for repeat diff: {first.name if not first.exists() else repeat.name}"])
    a = sorted(first.read_text(encoding="utf-8").splitlines())
    b = sorted(repeat.read_text(encoding="utf-8").splitlines())
    if a == b:
        return _oracle("determinism", [])
    diff_path = tier_dir / "determinism-diff.txt"
    only_a = [l for l in a if l not in set(b)][:20]
    only_b = [l for l in b if l not in set(a)][:20]
    diff_path.write_text("--- results.csv only\n" + "\n".join(only_a)
                         + "\n+++ results-repeat.csv only\n" + "\n".join(only_b) + "\n",
                         encoding="utf-8")
    return _oracle("determinism", [f"repeat batch diverged; see {diff_path.name} "
                                   f"({len(only_a)}+{len(only_b)} differing lines shown)"])


def oracle_victory(tier_dir: Path) -> dict:
    path = tier_dir / "oracle-eval.json"
    if not path.exists():
        return _oracle("victory_roe", [f"missing {path.name} (run gauntlet_oracle_eval first)"])
    data = json.loads(path.read_text(encoding="utf-8"))
    failures: list[str] = []
    warnings: list[str] = []
    for s in data.get("scenarios", []):
        for f in s.get("failures", []):
            failures.append(f"{s.get('scenario')}: {f}")
        for w in s.get("warnings", []):
            warnings.append(f"{s.get('scenario')}: {w}")
    if not data.get("allPassed", False) and not failures:
        failures.append("allPassed=false")
    return _oracle("victory_roe", failures, warnings)
```

In `main` tier mode, the oracle list becomes:

```python
        oracles = [
            oracle_stability(tier_dir, rows, scenarios, seeds),
            oracle_determinism(tier_dir),
            oracle_victory(tier_dir),
            oracle_sanity(rows, seeds),
        ]
```

- [ ] **Step 4: Run all pytest** — `python3 -m pytest tools/qa-gauntlet/ -q` → all pass.

- [ ] **Step 5: detect_changes + commit**

```bash
git add tools/qa-gauntlet/evaluate_run.py tools/qa-gauntlet/test_evaluate_run.py
git commit -m "qa(gauntlet): determinism repeat-diff + victory/ROE oracles in evaluate_run.py"
```

---

### Task 6: Golden anchors — oracle + `bless` mode

**Files:**
- Modify: `tools/qa-gauntlet/evaluate_run.py`
- Modify: `tools/qa-gauntlet/test_evaluate_run.py`
- Create: `tools/qa-gauntlet/goldens/README.md`

**Interfaces:**
- Produces: `oracle_goldens(rows, goldens_path: Path, anchor_seeds: list[str]) -> Oracle`; `bless(run_dir: Path, goldens_path: Path, run_id: str, tier_names: list[str]) -> int`; CLI `bless --run-dir D --run-id ID --goldens F [--tiers tier-1,...]`. Goldens format:
  `{"version": 1, "blessedFrom": "<run-id>", "anchors": {"<scenarioId>|<seed>": "<sha256 hex of fingerprint>"}}`.
- Consumes: `Row`, `parse_results_csv` from Task 4.

- [ ] **Step 1: Add failing tests** (append)

```python
from evaluate_run import bless, oracle_goldens  # noqa: E402


def _golden_file(tmp_path, fp="CATALOG_UNIT:x:surface T|1,extra", sid="s1", seed="42"):
    import hashlib
    g = {"version": 1, "blessedFrom": "test",
         "anchors": {f"{sid}|{seed}": hashlib.sha256(fp.encode()).hexdigest()}}
    p = tmp_path / "anchors.json"
    p.write_text(json.dumps(g))
    return p


def test_goldens_pass_on_matching_hash(tmp_path):
    p = make_csv(tmp_path, [row_line()])
    g = _golden_file(tmp_path)
    assert oracle_goldens(parse_results_csv(p), g, ["42"])["status"] == "pass"


def test_goldens_fail_on_mismatch_and_missing_anchor(tmp_path):
    p = make_csv(tmp_path, [row_line(fp="DIFFERENT"), row_line(seed="7")])
    g = _golden_file(tmp_path)
    o = oracle_goldens(parse_results_csv(p), g, ["42", "7"])
    assert o["status"] == "fail"
    assert any("mismatch" in e for e in o["evidence"])
    assert any("no golden" in e for e in o["evidence"])


def test_goldens_ignore_roving_rows(tmp_path):
    p = make_csv(tmp_path, [row_line(), row_line(seed="99991", fp="ROVING")])
    g = _golden_file(tmp_path)
    assert oracle_goldens(parse_results_csv(p), g, ["42"])["status"] == "pass"


def test_bless_writes_all_anchor_hashes(tmp_path):
    run = tmp_path / "run"
    (run / "tier-1").mkdir(parents=True)
    make_csv(run / "tier-1", [row_line(), row_line(seed="7", fp="FP7")])
    out = tmp_path / "anchors.json"
    rc = bless(run, out, "run-x", ["tier-1"])
    assert rc == 0
    g = json.loads(out.read_text())
    assert g["blessedFrom"] == "run-x" and len(g["anchors"]) == 2
```

- [ ] **Step 2: Verify failure** — pytest → ImportError.

- [ ] **Step 3: Implement**

```python
def _load_goldens(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def oracle_goldens(rows: list[Row], goldens_path: Path, anchor_seeds: list[str]) -> dict:
    if not goldens_path or not goldens_path.exists():
        return _oracle("goldens", [f"goldens file missing: {goldens_path}"])
    anchors = _load_goldens(goldens_path).get("anchors", {})
    failures: list[str] = []
    anchor_set = set(anchor_seeds)
    for r in rows:
        if r.seed not in anchor_set:
            continue  # roving rows have no stored baseline
        key = f"{r.scenario_id}|{r.seed}"
        want = anchors.get(key)
        got = hashlib.sha256(r.fingerprint.encode("utf-8")).hexdigest()
        if want is None:
            failures.append(f"no golden for {key} (bless required after adding scenarios)")
        elif want != got:
            failures.append(f"golden mismatch {key}: expected {want[:12]}… got {got[:12]}… "
                            f"(legit change? re-bless per goldens/README.md)")
    return _oracle("goldens", failures)


def bless(run_dir: Path, goldens_path: Path, run_id: str, tier_names: list[str]) -> int:
    anchors: dict[str, str] = {}
    for tier in tier_names:
        csv_path = run_dir / tier / "results.csv"
        if not csv_path.exists():
            print(f"bless: missing {csv_path}", file=sys.stderr)
            return 2
        for r in parse_results_csv(csv_path):
            anchors[f"{r.scenario_id}|{r.seed}"] = hashlib.sha256(
                r.fingerprint.encode("utf-8")).hexdigest()
    goldens_path.parent.mkdir(parents=True, exist_ok=True)
    goldens_path.write_text(json.dumps(
        {"version": 1, "blessedFrom": run_id, "anchors": dict(sorted(anchors.items()))},
        indent=2) + "\n", encoding="utf-8")
    print(f"bless: wrote {len(anchors)} anchors from {run_id} -> {goldens_path}")
    return 0
```

Wire-up in `main`: `tier` mode appends `oracle_goldens(rows_anchor_only_input…)` when `--goldens` given — pass full `rows` plus anchor seed list (function filters). Add `bless` subparser: `--run-dir`, `--run-id`, `--goldens`, `--tiers` (default `tier-1,tier-2,tier-3,tier-4,tier-5,tier-extra`); dispatch `return bless(args.run_dir, args.goldens, args.run_id, args.tiers.split(","))`. **Bless guard:** before writing, if any `tier-N/verdict.json` exists with `"pass": false`, refuse with exit 2 and message `bless refused: <tier> verdict is red`.

Create `tools/qa-gauntlet/goldens/README.md`:

```markdown
# Golden anchors — blessed-update runbook

`anchors.json` stores SHA-256 of the full batch fingerprint per (scenarioId, anchor seed).
Anchor seeds: 42, 7, 123. Any mismatch is a red oracle — the strongest regression signal
the ladder has, because the sim is byte-deterministic.

## When a golden mismatch is legitimate

Only when a sim change *intentionally* alters behavior (new feature, approved balance
change, bug fix that moves outcomes). Same discipline as ReplayGolden and
`tools/qa-gauntlet/README-expect-regen.md`: **never bless to silence an unexplained diff.**

## How to re-bless

1. Confirm the diff is explained (link the PR/story/defect in the commit message).
2. Run a full green ladder (all non-golden oracles pass).
3. `python3 tools/qa-gauntlet/evaluate_run.py bless --run-dir production/qa/gauntlet/<RUN_ID> --run-id <RUN_ID> --goldens tools/qa-gauntlet/goldens/anchors.json`
4. Commit `anchors.json` with message `qa(gauntlet): re-bless goldens — <why>`.
```

- [ ] **Step 4: Run pytest** — all pass.
- [ ] **Step 5: detect_changes + commit**

```bash
git add tools/qa-gauntlet/evaluate_run.py tools/qa-gauntlet/test_evaluate_run.py tools/qa-gauntlet/goldens/README.md
git commit -m "qa(gauntlet): exact golden-anchor oracle + bless mode with red-run guard"
```

---

### Task 7: Token-coverage oracle (run mode) + `expected-tokens.json`

**Files:**
- Modify: `tools/qa-gauntlet/evaluate_run.py`
- Modify: `tools/qa-gauntlet/test_evaluate_run.py`
- Create: `tools/qa-gauntlet/expected-tokens.json`

**Interfaces:**
- Produces: `oracle_token_coverage(all_rows: list[Row], expected_path: Path, manifest_path: Path) -> Oracle`; CLI `run --run-dir D --tiers tier-1,... --expected-tokens F --manifest data/glossary/abort_reason_manifest.json --out D/verdict.json` — aggregates tier verdicts (any red tier = red run) + run-wide token coverage.
- `expected-tokens.json` format:
  `{"version": 1, "requiredRunWide": ["<substring>", …], "warnIfAbsent": [{"token": "<substring>", "reason": "…"}], "reportManifestCounts": true}`.

- [ ] **Step 1: Add failing tests** (append)

```python
from evaluate_run import oracle_token_coverage  # noqa: E402


def _expected(tmp_path, required, warn=()):
    p = tmp_path / "expected-tokens.json"
    p.write_text(json.dumps({
        "version": 1, "requiredRunWide": list(required),
        "warnIfAbsent": [{"token": t, "reason": "pending"} for t in warn],
        "reportManifestCounts": False}))
    return p


def _manifest(tmp_path):
    p = tmp_path / "abort_reason_manifest.json"
    p.write_text(json.dumps({"version": 1, "families": [
        {"name": "Doctrine", "enum": "X", "entries": [{"logCode": "EMCON_OFF", "member": "EmconOff"}]}]}))
    return p


def test_token_coverage_pass_and_vacuity_fail(tmp_path):
    rows = parse_results_csv(make_csv(tmp_path, [row_line(fp="CATALOG_UNIT:a:surface MAGAZINE_SEED:a:1:2")]))
    ok = oracle_token_coverage(rows, _expected(tmp_path, ["CATALOG_UNIT:", "MAGAZINE_SEED:"]), _manifest(tmp_path))
    assert ok["status"] == "pass"
    bad = oracle_token_coverage(rows, _expected(tmp_path, ["CATALOG_UNIT:", "ContactChange|"]), _manifest(tmp_path))
    assert bad["status"] == "fail"
    assert any("ContactChange|" in e and "0" in e for e in bad["evidence"])


def test_token_coverage_warn_list_never_fails(tmp_path):
    rows = parse_results_csv(make_csv(tmp_path, [row_line(fp="CATALOG_UNIT:a:surface")]))
    o = oracle_token_coverage(rows, _expected(tmp_path, ["CATALOG_UNIT:"], warn=["EMCON_OFF"]),
                              _manifest(tmp_path))
    assert o["status"] == "warn"
    assert any("EMCON_OFF" in e for e in o["evidence"])
```

- [ ] **Step 2: Verify failure** — ImportError.

- [ ] **Step 3: Implement**

```python
def oracle_token_coverage(all_rows: list[Row], expected_path: Path, manifest_path: Path) -> dict:
    if not expected_path.exists():
        return _oracle("token_coverage", [f"missing expected-tokens file: {expected_path}"])
    cfg = json.loads(expected_path.read_text(encoding="utf-8"))
    blob = "\n".join(r.fingerprint for r in all_rows)
    failures: list[str] = []
    warnings: list[str] = []
    for token in cfg.get("requiredRunWide", []):
        n = blob.count(token)
        if n == 0:
            failures.append(f"required token '{token}' seen 0 times run-wide (vacuous dimension?)")
    for item in cfg.get("warnIfAbsent", []):
        if blob.count(item["token"]) == 0:
            warnings.append(f"token '{item['token']}' absent (known: {item.get('reason', '')})")
    if cfg.get("reportManifestCounts") and manifest_path.exists():
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        for family in manifest.get("families", []):
            for entry in family.get("entries", []):
                code = entry["logCode"]
                warnings.append(f"manifest {family['name']}.{code}: {blob.count(code)} occurrence(s)")
    return _oracle("token_coverage", failures, warnings)
```

Add `run` mode to `main`: parse `--run-dir`, `--tiers`, `--expected-tokens`, `--manifest` (default `data/glossary/abort_reason_manifest.json`), `--anchor-seeds`, `--out`. Load every `tier/results.csv` row into one list; read each `tier/verdict.json` (missing or `"pass": false` → failure `f"{tier}: verdict red/missing"`); oracles = that aggregate check (`_oracle("tiers", failures)`) + `oracle_token_coverage(...)`; `write_verdict(out, "run", oracles)`.

- [ ] **Step 4: Seed `expected-tokens.json` from the real 2026-07-28 run (mechanical, not invented)**

```bash
R=production/qa/gauntlet/gauntlet-20260728-2016
cat $R/tier-*/results.csv | python3 - <<'PY'
import sys, json
blob = sys.stdin.read()
manifest = json.load(open("data/glossary/abort_reason_manifest.json"))
codes = [e["logCode"] for f in manifest["families"] for e in f["entries"]]
structural = ["CATALOG_UNIT:", "MAGAZINE_SEED:", "ContactChange|", "EventFired|"]
for t in structural + codes:
    print(f"{blob.count(t):8d}  {t}")
PY
```

Record the output in the commit body. Write `tools/qa-gauntlet/expected-tokens.json`: `requiredRunWide` = every token with count > 0 in that output; `warnIfAbsent` = `[{"token": "EMCON_OFF", "reason": "BUG-gauntlet-emcon-dimension-not-exercised — flip to required when the 2026-07-27 variability-plan EMCON retrofit lands"}]`; `reportManifestCounts: true`.

- [ ] **Step 5: Run pytest; verify run mode end-to-end against the real artifacts**

```bash
python3 -m pytest tools/qa-gauntlet/ -q
python3 tools/qa-gauntlet/evaluate_run.py run \
  --run-dir production/qa/gauntlet/gauntlet-20260728-2016 \
  --tiers tier-1,tier-2,tier-3,tier-4,tier-5,tier-extra \
  --expected-tokens tools/qa-gauntlet/expected-tokens.json \
  --out /dev/null; echo "EXIT=$?"
```

Expected: pytest green. Run mode: `tiers` oracle FAILS (old run dirs have no `verdict.json` — correct fail-closed behavior); token-coverage itself shows pass with the EMCON warn. (Exit 1 here is expected and proves fail-closed aggregation; the full green path is proven in Task 8.)

- [ ] **Step 6: detect_changes + commit**

```bash
git add tools/qa-gauntlet/evaluate_run.py tools/qa-gauntlet/test_evaluate_run.py tools/qa-gauntlet/expected-tokens.json
git commit -m "qa(gauntlet): run-wide token-coverage oracle + expected-tokens.json seeded from gauntlet-20260728-2016"
```

---

### Task 8: Canonical driver `run-gauntlet.sh` + roving seeds

**Files:**
- Create: `tools/qa-gauntlet/run-gauntlet.sh` (chmod +x)

**Interfaces:**
- Consumes: Demo batch harness, `gauntlet_oracle_eval` CLI (Task 3 behavior), `evaluate_run.py` tier/run modes (Tasks 4–7).
- Produces: run directory layout used by `/qa-gauntlet` and `saboteur.py` (Task 10): `production/qa/gauntlet/<RUN_ID>/tier-N/{results.csv,results-repeat.csv,run.log,run-repeat.log,oracle-eval.json,verdict.json}` + run-level `verdict.json` + `roving-seeds.txt`. Exit 0 iff every tier verdict and the run verdict pass.

- [ ] **Step 1: Write the driver**

```bash
#!/usr/bin/env bash
# Canonical QA Gauntlet ladder driver — oracles as code.
# Spec: docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md
# Usage: run-gauntlet.sh --run-id <id> [--tiers "1 2 3 4 5 extra"] [--seeds 42,7,123]
#                        [--roving 2] [--out-root production/qa/gauntlet]
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

# dotnet resolution: PATH, then ~/.dotnet, then fail loud.
if command -v dotnet >/dev/null 2>&1; then DOTNET=dotnet;
elif [ -x "$HOME/.dotnet/dotnet" ]; then DOTNET="$HOME/.dotnet/dotnet";
else echo "FATAL: dotnet not found on PATH or at ~/.dotnet/dotnet" >&2; exit 3; fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1

RUN_ID=""; TIERS="1 2 3 4 5 extra"; ANCHOR_SEEDS="42,7,123"; ROVING=2
OUT_ROOT="production/qa/gauntlet"
while [ $# -gt 0 ]; do case "$1" in
  --run-id) RUN_ID="$2"; shift 2;;
  --tiers) TIERS="$2"; shift 2;;
  --seeds) ANCHOR_SEEDS="$2"; shift 2;;
  --roving) ROVING="$2"; shift 2;;
  --out-root) OUT_ROOT="$2"; shift 2;;
  *) echo "unknown arg: $1" >&2; exit 3;;
esac; done
[ -n "$RUN_ID" ] || { echo "FATAL: --run-id required" >&2; exit 3; }

RUN_DIR="$OUT_ROOT/$RUN_ID"; mkdir -p "$RUN_DIR"
GOLDENS="tools/qa-gauntlet/goldens/anchors.json"
EXPECTED="tools/qa-gauntlet/expected-tokens.json"

# Deterministic roving seeds from run-id (recorded for reproducibility).
ROVING_SEEDS=""
if [ "$ROVING" -gt 0 ]; then
  ROVING_SEEDS=$(python3 - "$RUN_ID" "$ROVING" <<'PY'
import hashlib, sys
run_id, n = sys.argv[1], int(sys.argv[2])
print(",".join(str(int(hashlib.sha256(f"{run_id}:{k}".encode()).hexdigest()[:8], 16) % 90000 + 10000)
               for k in range(n)))
PY
)
  echo "$ROVING_SEEDS" > "$RUN_DIR/roving-seeds.txt"
fi
ALL_SEEDS="$ANCHOR_SEEDS${ROVING_SEEDS:+,$ROVING_SEEDS}"

scenarios_for() { case "$1" in
  1) echo "gauntlet-t1-patrol-a,gauntlet-t1-patrol-b,gauntlet-t1-patrol-c,gauntlet-t1-patrol-d";;
  2) echo "gauntlet-t2-escort-a,gauntlet-t2-escort-passive,gauntlet-t2-strike-a,gauntlet-t2-strike-event";;
  3) echo "gauntlet-t3-escort-strike,gauntlet-t3-emcon-phases,gauntlet-t3-id-roe,gauntlet-t3-event-chain";;
  4) echo "gauntlet-t4-multi-mission,gauntlet-t4-weighted,gauntlet-t4-asymm-roe,gauntlet-t4-random-inject";;
  5) echo "gauntlet-t5-cascade,gauntlet-t5-theater,gauntlet-t5-dynamic-obj,gauntlet-t5-roe-change";;
  extra) echo "gauntlet-joint-orbat-smoke,gauntlet-multidomain-shooters";;
  *) return 1;; esac; }
ticks_for() { case "$1" in 1) echo 6;; 2) echo 10;; 3) echo 16;; 4) echo 24;; 5) echo 40;; extra) echo 12;; esac; }

OVERALL=0
TIER_NAMES=""
for t in $TIERS; do
  TIER="tier-$t"; TDIR="$RUN_DIR/$TIER"; mkdir -p "$TDIR"
  TIER_NAMES="${TIER_NAMES:+$TIER_NAMES,}$TIER"
  SCEN=$(scenarios_for "$t") || { echo "FATAL: unknown tier $t" >&2; exit 3; }
  TICKS=$(ticks_for "$t")
  echo "=== $TIER ticks=$TICKS seeds=$ALL_SEEDS ==="
  IFS=',' read -ra IDS <<< "$SCEN"
  for id in "${IDS[@]}"; do cp "data/scenarios/$id.policy.json" "$TDIR/"; done

  "$DOTNET" run --project src/ProjectAegis.Delegation.Demo --no-build -- --batch \
    --scenarios "$SCEN" --seeds "$ALL_SEEDS" --ticks "$TICKS" \
    --csv-out "$TDIR/results.csv" > "$TDIR/run.log" 2>&1 \
    || { echo "BATCH_FAIL $TIER"; OVERALL=1; continue; }
  "$DOTNET" run --project src/ProjectAegis.Delegation.Demo --no-build -- --batch \
    --scenarios "$SCEN" --seeds "$ALL_SEEDS" --ticks "$TICKS" \
    --csv-out "$TDIR/results-repeat.csv" > "$TDIR/run-repeat.log" 2>&1 \
    || { echo "REPEAT_FAIL $TIER"; OVERALL=1; continue; }

  "$DOTNET" run --project src/ProjectAegis.MissionEditor.Cli --no-build -- gauntlet_oracle_eval \
    --policy-dir "$TDIR" --csv "$TDIR/results.csv" \
    --out "$TDIR/oracle-eval.json" > "$TDIR/oracle.log" 2>&1
  # exit code intentionally not consumed here — evaluate_run.py reads oracle-eval.json (fail-closed)

  python3 tools/qa-gauntlet/evaluate_run.py tier \
    --tier-dir "$TDIR" --scenarios "$SCEN" \
    --anchor-seeds "$ANCHOR_SEEDS" --roving-seeds "$ROVING_SEEDS" \
    --goldens "$GOLDENS" || OVERALL=1
done

python3 tools/qa-gauntlet/evaluate_run.py run \
  --run-dir "$RUN_DIR" --tiers "$TIER_NAMES" \
  --expected-tokens "$EXPECTED" --anchor-seeds "$ANCHOR_SEEDS" \
  --out "$RUN_DIR/verdict.json" || OVERALL=1

echo "RUN_VERDICT exit=$OVERALL run_dir=$RUN_DIR"
exit "$OVERALL"
```

`chmod +x tools/qa-gauntlet/run-gauntlet.sh`

- [ ] **Step 2: Build once, then smoke the driver on tier 1 only (goldens don't exist yet — expect a controlled red)**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build ProjectAegis.sln -v minimal
tools/qa-gauntlet/run-gauntlet.sh --run-id smoke-driver-t1 --tiers "1" --roving 0 \
  --out-root /tmp/claude-1000/-home-username01-cmano-clone/*/scratchpad 2>/dev/null \
  || tools/qa-gauntlet/run-gauntlet.sh --run-id smoke-driver-t1 --tiers "1" --roving 0 --out-root /tmp/gauntlet-smoke
```

(Use the scratchpad path if it expands; otherwise the /tmp fallback.) Expected: batch + repeat run; `verdict.json` exists; **exit 1 with the only red being `goldens: goldens file missing`** — proves fail-closed. All other oracles pass.

- [ ] **Step 3: detect_changes + commit**

```bash
git add tools/qa-gauntlet/run-gauntlet.sh
git commit -m "qa(gauntlet): canonical ladder driver — batch, repeat, oracle eval, verdicts, roving seeds"
```

---

### Task 9: Bless goldens from the 2026-07-28 run + first fully-green canonical run

**Files:**
- Create: `tools/qa-gauntlet/goldens/anchors.json` (generated, then committed)

- [ ] **Step 1: Bless from the byte-identical verified run**

```bash
python3 tools/qa-gauntlet/evaluate_run.py bless \
  --run-dir production/qa/gauntlet/gauntlet-20260728-2016 --run-id gauntlet-20260728-2016 \
  --goldens tools/qa-gauntlet/goldens/anchors.json
python3 -c "import json; a=json.load(open('tools/qa-gauntlet/goldens/anchors.json'))['anchors']; print(len(a))"
```

Expected: 66 anchors (22 scenarios × 3 seeds). (The bless red-verdict guard doesn't trip: that run has no verdict.json files, and absence only blocks `run`-mode aggregation, not bless — bless checks only verdicts that exist.)

- [ ] **Step 2: Full canonical run — everything green end-to-end**

```bash
export PATH="$HOME/.dotnet:$PATH"
tools/qa-gauntlet/run-gauntlet.sh --run-id "gauntlet-canonical-$(date +%Y%m%d-%H%M)" --roving 2
echo "EXIT=$?"
```

Expected: `EXIT=0`; every `tier-N/verdict.json` green (goldens now match — same SHA as the blessed run); run `verdict.json` green with the EMCON warn present; roving seeds recorded and their rows judged by envelopes only.

- [ ] **Step 3: detect_changes + commit**

```bash
git add tools/qa-gauntlet/goldens/anchors.json
git commit -m "qa(gauntlet): bless golden anchors from gauntlet-20260728-2016 (66 anchors, byte-identical x2 verified)"
```

---

### Task 10: `saboteur.py` — worktree mutant runner

**Files:**
- Create: `tools/qa-gauntlet/saboteur.py`
- Test: `tools/qa-gauntlet/test_saboteur.py`
- Create: `tools/qa-gauntlet/mutants/catalog.yaml` (schema + one no-op calibration fixture entry)
- Create: `tools/qa-gauntlet/mutants/00-noop-comment.patch`

**Interfaces:**
- Produces: `load_catalog(path: Path) -> list[dict]`; `summarize(results: list[dict]) -> dict` (kill-rate + per-oracle counts); `render_report(summary, results) -> str` (markdown matrix); CLI `saboteur.py --catalog tools/qa-gauntlet/mutants/catalog.yaml --out-dir production/qa/gauntlet/calibration-<date> [--mutants id1,id2] [--keep-worktrees]`.
- Catalog entry schema (YAML):
  ```yaml
  mutants:
    - id: "01-pd-halved"
      patch: "01-pd-halved.patch"
      target: "src/ProjectAegis.Sim/Sensors/DetectionProbability.cs"
      description: "Pd computation halved"
      expectedOracles: ["goldens", "victory_roe"]
      impactRecorded: "LOW (2026-07-28, upstream callers: detection loop, tests)"
  ```
- Caught rule: a mutant is **caught** if the anchor ladder subset (tiers 1,3,5; anchor seed 42 only; roving 0) exits non-zero OR the `ReplayGolden` test filter fails. Build failure = `invalid-mutant` (neither caught nor survived; must be fixed or removed).

- [ ] **Step 1: Write failing tests for the pure functions**

Create `tools/qa-gauntlet/test_saboteur.py`:

```python
#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/saboteur.py pure logic (no worktrees in unit tests)."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from saboteur import load_catalog, render_report, summarize  # noqa: E402


def test_load_catalog_parses_yaml(tmp_path):
    (tmp_path / "m.patch").write_text("--- a\n+++ b\n")
    (tmp_path / "catalog.yaml").write_text(
        "mutants:\n"
        "  - id: \"01-x\"\n"
        "    patch: \"m.patch\"\n"
        "    target: \"src/Foo.cs\"\n"
        "    description: \"d\"\n"
        "    expectedOracles: [\"goldens\"]\n"
        "    impactRecorded: \"LOW\"\n")
    cat = load_catalog(tmp_path / "catalog.yaml")
    assert cat[0]["id"] == "01-x" and (tmp_path / cat[0]["patch"]).exists()


def test_load_catalog_rejects_missing_patch(tmp_path):
    (tmp_path / "catalog.yaml").write_text(
        "mutants:\n  - id: \"01-x\"\n    patch: \"absent.patch\"\n    target: \"t\"\n"
        "    description: \"d\"\n    expectedOracles: []\n    impactRecorded: \"LOW\"\n")
    try:
        load_catalog(tmp_path / "catalog.yaml")
        assert False, "expected ValueError"
    except ValueError as e:
        assert "absent.patch" in str(e)


def test_summarize_and_report():
    results = [
        {"id": "01-x", "outcome": "caught", "firedOracles": ["goldens"], "expectedOracles": ["goldens"]},
        {"id": "02-y", "outcome": "survived", "firedOracles": [], "expectedOracles": ["victory_roe"]},
        {"id": "03-z", "outcome": "invalid-mutant", "firedOracles": [], "expectedOracles": []},
    ]
    s = summarize(results)
    assert s["caught"] == 1 and s["survived"] == 1 and s["invalid"] == 1
    assert s["killRate"] == "1/2"  # invalid mutants excluded from denominator
    md = render_report(s, results)
    assert "02-y" in md and "SURVIVED" in md and "1/2" in md
```

- [ ] **Step 2: Verify failure** — ImportError.

- [ ] **Step 3: Implement `saboteur.py`**

```python
#!/usr/bin/env python3
"""QA Gauntlet saboteur — oracle-sensitivity calibration via curated mutants.

Applies each catalog patch in a disposable git worktree, builds, runs the anchor
ladder subset (tiers 1,3,5 x seed 42) + the ReplayGolden test filter, and records
which oracles fired. Nothing is ever committed from a worktree.

Kill rule: caught = subset driver exit != 0 OR ReplayGolden filter fails.
Build failure = invalid-mutant (fix or drop the patch; it proves nothing).
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILD_TIMEOUT_S = 600
RUN_TIMEOUT_S = 900
LOCKED = (
    "src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs",
    "src/ProjectAegis.Delegation.Demo/Program.cs",
    "src/ProjectAegis.Delegation.UnityAdapter/Baltic/DelegationBridge.cs",
)


def load_catalog(path: Path) -> list[dict]:
    import yaml  # PyYAML present in repo tooling env; fail loud otherwise
    data = yaml.safe_load(path.read_text(encoding="utf-8"))
    mutants = data.get("mutants", [])
    for m in mutants:
        for key in ("id", "patch", "target", "description", "expectedOracles", "impactRecorded"):
            if key not in m:
                raise ValueError(f"catalog entry missing '{key}': {m}")
        if any(lock in m["target"] for lock in LOCKED):
            raise ValueError(f"mutant {m['id']} targets a locked-eval file: {m['target']}")
        if not (path.parent / m["patch"]).exists():
            raise ValueError(f"mutant {m['id']}: patch not found: {m['patch']}")
    return mutants


def summarize(results: list[dict]) -> dict:
    caught = sum(1 for r in results if r["outcome"] == "caught")
    survived = sum(1 for r in results if r["outcome"] == "survived")
    invalid = sum(1 for r in results if r["outcome"] == "invalid-mutant")
    valid = caught + survived
    return {"caught": caught, "survived": survived, "invalid": invalid,
            "killRate": f"{caught}/{valid}"}


def render_report(summary: dict, results: list[dict]) -> str:
    lines = ["# Saboteur calibration report", "",
             f"**Kill rate: {summary['killRate']}** "
             f"(caught {summary['caught']}, survived {summary['survived']}, "
             f"invalid {summary['invalid']})", "",
             "| Mutant | Outcome | Fired oracles | Expected |", "|---|---|---|---|"]
    for r in results:
        outcome = r["outcome"].upper() if r["outcome"] != "caught" else "caught"
        lines.append(f"| {r['id']} | {outcome} | {', '.join(r['firedOracles']) or '—'} "
                     f"| {', '.join(r['expectedOracles']) or '—'} |")
    lines.append("")
    lines.append("Every SURVIVED row is a named oracle blind spot — file a bug per row.")
    return "\n".join(lines) + "\n"


def _run(cmd: list[str], cwd: Path, timeout: int, log: Path) -> int:
    with log.open("w", encoding="utf-8") as fh:
        try:
            return subprocess.run(cmd, cwd=cwd, stdout=fh, stderr=subprocess.STDOUT,
                                  timeout=timeout).returncode
        except subprocess.TimeoutExpired:
            fh.write(f"\nTIMEOUT after {timeout}s\n")
            return 124


def _fired_oracles(run_dir: Path) -> list[str]:
    fired: set[str] = set()
    for verdict in run_dir.rglob("verdict.json"):
        data = json.loads(verdict.read_text(encoding="utf-8"))
        for name, o in data.get("oracles", {}).items():
            if o.get("status") == "fail":
                fired.add(name)
    return sorted(fired)


def run_mutant(m: dict, catalog_dir: Path, out_dir: Path, dotnet: str, keep: bool) -> dict:
    wt = ROOT / ".worktrees" / f"saboteur-{m['id']}"
    mdir = out_dir / m["id"]
    mdir.mkdir(parents=True, exist_ok=True)
    result = {"id": m["id"], "expectedOracles": m["expectedOracles"],
              "firedOracles": [], "outcome": "invalid-mutant"}
    try:
        subprocess.run(["git", "worktree", "add", "--detach", str(wt)],
                       cwd=ROOT, check=True, capture_output=True)
        subprocess.run(["git", "apply", str((catalog_dir / m["patch"]).resolve())],
                       cwd=wt, check=True, capture_output=True)
        if _run([dotnet, "build", "ProjectAegis.sln", "-v", "minimal"],
                wt, BUILD_TIMEOUT_S, mdir / "build.log") != 0:
            return result  # invalid-mutant
        subset_rc = _run(["bash", "tools/qa-gauntlet/run-gauntlet.sh",
                          "--run-id", f"saboteur-{m['id']}", "--tiers", "1 3 5",
                          "--seeds", "42", "--roving", "0",
                          "--out-root", "production/qa/gauntlet/saboteur-tmp"],
                         wt, RUN_TIMEOUT_S, mdir / "subset.log")
        replay_rc = _run([dotnet, "test", "src/ProjectAegis.Delegation.Tests",
                          "-v", "minimal", "--filter", "ReplayGolden"],
                         wt, RUN_TIMEOUT_S, mdir / "replay.log")
        result["firedOracles"] = _fired_oracles(
            wt / "production/qa/gauntlet/saboteur-tmp" / f"saboteur-{m['id']}")
        if replay_rc != 0:
            result["firedOracles"] = sorted(set(result["firedOracles"]) | {"replay_golden"})
        result["outcome"] = "caught" if (subset_rc != 0 or replay_rc != 0) else "survived"
        return result
    except subprocess.CalledProcessError as ex:
        (mdir / "error.log").write_text(str(ex.stderr or ex), encoding="utf-8")
        return result
    finally:
        if not keep:
            subprocess.run(["git", "worktree", "remove", "--force", str(wt)],
                           cwd=ROOT, capture_output=True)


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(prog="saboteur.py")
    parser.add_argument("--catalog", type=Path,
                        default=ROOT / "tools/qa-gauntlet/mutants/catalog.yaml")
    parser.add_argument("--out-dir", type=Path,
                        default=ROOT / f"production/qa/gauntlet/calibration-{date.today().isoformat()}")
    parser.add_argument("--mutants", default="")
    parser.add_argument("--keep-worktrees", action="store_true")
    args = parser.parse_args(argv)

    if subprocess.run(["git", "status", "--porcelain", "--untracked-files=no"],
                      cwd=ROOT, capture_output=True, text=True).stdout.strip():
        print("saboteur: refusing to run with a dirty tracked working tree", file=sys.stderr)
        return 2
    dotnet = shutil.which("dotnet") or str(Path.home() / ".dotnet/dotnet")
    if not Path(dotnet).exists():
        print("saboteur: dotnet not found", file=sys.stderr)
        return 3

    mutants = load_catalog(args.catalog)
    if args.mutants:
        wanted = set(args.mutants.split(","))
        mutants = [m for m in mutants if m["id"] in wanted]
    args.out_dir.mkdir(parents=True, exist_ok=True)
    results = [run_mutant(m, args.catalog.parent, args.out_dir, dotnet, args.keep_worktrees)
               for m in mutants]
    summary = summarize(results)
    (args.out_dir / "report.json").write_text(
        json.dumps({"summary": summary, "results": results}, indent=2) + "\n", encoding="utf-8")
    (args.out_dir / "report.md").write_text(render_report(summary, results), encoding="utf-8")
    print(f"kill rate {summary['killRate']} — report: {args.out_dir}/report.md")
    return 0 if summary["survived"] == 0 and summary["invalid"] == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
```

Create `tools/qa-gauntlet/mutants/00-noop-comment.patch` — a comment-only change used to prove the pipeline reports SURVIVED for a behavior-neutral mutant (generate it for a stable non-locked file):

```bash
cd /home/username01/cmano-clone
git worktree add --detach .worktrees/gen-noop
cd .worktrees/gen-noop
printf '\n// saboteur no-op calibration marker\n' >> src/ProjectAegis.Sim/Sensors/ContactLifecycleState.cs
git diff > ../../tools/qa-gauntlet/mutants/00-noop-comment.patch
cd ../.. && git worktree remove --force .worktrees/gen-noop
```

Create `tools/qa-gauntlet/mutants/catalog.yaml`:

```yaml
# Saboteur mutant catalog — oracle-sensitivity calibration.
# Rules: never target locked-eval files; every entry records a one-time GitNexus
# impact() result; patches are committed but only ever applied in throwaway worktrees.
mutants:
  - id: "00-noop-comment"
    patch: "00-noop-comment.patch"
    target: "src/ProjectAegis.Sim/Sensors/ContactLifecycleState.cs"
    description: "Comment-only no-op — pipeline fixture; MUST survive (proves we don't flag noise)"
    expectedOracles: []
    impactRecorded: "n/a (comment only)"
```

- [ ] **Step 4: Run pytest** — `python3 -m pytest tools/qa-gauntlet/ -q` → green. (If PyYAML is missing: `pip3 install --user pyyaml` and note it in the commit body.)

- [ ] **Step 5: Integration check with the no-op mutant only**

```bash
python3 tools/qa-gauntlet/saboteur.py --mutants 00-noop-comment \
  --out-dir /tmp/saboteur-noop-check
```

Expected: builds, runs subset, exit 1 with `kill rate 0/1` and `00-noop-comment | SURVIVED` in the report — correct: a no-op *should* survive (it validates the pipeline doesn't false-positive; it stays in the catalog as the control and is excluded from the headline by its `expectedOracles: []` marker — later reporting treats `00-*` as control, see Task 12).

- [ ] **Step 6: detect_changes + commit**

```bash
git add tools/qa-gauntlet/saboteur.py tools/qa-gauntlet/test_saboteur.py tools/qa-gauntlet/mutants/
git commit -m "qa(gauntlet): saboteur worktree mutant runner + no-op control mutant (TDD)"
```

---

### Task 11: Real mutant catalog (7 behavior mutants)

**Files:**
- Create: `tools/qa-gauntlet/mutants/01-pd-weakened.patch` … `07-magazine-not-decremented.patch`
- Modify: `tools/qa-gauntlet/mutants/catalog.yaml`

**Procedure per mutant (same 5 steps each; repeat for all seven):**

1. Locate the exact line with the given grep.
2. Generate the patch in a temp worktree (never edit the main tree):
   ```bash
   git worktree add --detach .worktrees/gen-mut && cd .worktrees/gen-mut
   # apply the one-line semantic change described below with your editor
   git diff > ../../tools/qa-gauntlet/mutants/<NN-slug>.patch
   cd ../.. && git worktree remove --force .worktrees/gen-mut
   ```
3. Run GitNexus `impact({target: "<symbol>", direction: "upstream", repo: "/home/username01/cmano-clone"})`; record risk in the catalog entry. (These are catalog-authoring records; the mutants are never applied to the real tree.)
4. Add the catalog.yaml entry (id, patch, target, description, expectedOracles, impactRecorded).
5. Verify it is CAUGHT: `python3 tools/qa-gauntlet/saboteur.py --mutants <id> --out-dir /tmp/mut-<id>` → report shows caught. If SURVIVED, that is a real finding — keep the mutant, note it, continue (Task 12 files the bug).

**The seven mutants** (semantic change + locator; targets verified non-locked):

| id | Locate via | Semantic change | expectedOracles |
|---|---|---|---|
| `01-pd-weakened` | `grep -rn "ComputePd" src/ProjectAegis.Sim/Sensors/DetectionProbability.cs` | multiply the final computed Pd by `0.5` before return | goldens, victory_roe |
| `02-roe-tight-inverted` | `grep -rn "WeaponsTight" src/ProjectAegis.Sim/Policy/*.cs` (the FireAbortReason decision site, not the enum) | invert the weapons-tight branch condition (`if (x)` → `if (!x)`) | victory_roe, goldens |
| `03-salvo-off-by-one` | `grep -rn "WraSalvo\|salvoSize\|SalvoSize" src/ProjectAegis.Sim --include=*.cs -l` then the comparison site | change the salvo-limit comparison `<` → `<=` (or equivalent off-by-one) | victory_roe, goldens |
| `04-rng-reseed-dropped` | `grep -rn "seed" src/ProjectAegis.Sim/**/[Rr]andom*.cs src/ProjectAegis.Sim/**/[Dd]eterministic*.cs -l` | make one RNG construction ignore the passed seed (use a fixed constant) | determinism, replay_golden, goldens |
| `05-contact-lifecycle-skip` | `grep -rn "Classified" src/ProjectAegis.Sim/Sensors/ContactLifecycleState.cs src/ProjectAegis.Sim/Sensors/*.cs` (the transition site) | skip the `Detected → Classified` transition (jump straight to `Identified`) | token_coverage, goldens |
| `06-emcon-engage-bypass` | `grep -rn "EmconOff\|RadarEmconActive" src/ProjectAegis.Sim/Engage/*.cs` | force the engage-side EMCON gate open (`RadarEmconActive` treated as always true) | goldens *(token_coverage only after EMCON retrofit lands — record as expected-miss for now)* |
| `07-magazine-not-decremented` | `grep -rn "MagazineRounds\|magazine" src/ProjectAegis.Sim --include=*.cs -l` (the decrement site; NOT DelegationBridge) | remove/neutralize the rounds decrement on launch | victory_roe (maxMissilesFired), goldens |

- [ ] **Step 1–7:** one checkbox per mutant: generate patch → impact → catalog entry → verify caught (or record survivor).
- [ ] **Step 8: detect_changes + commit**

```bash
git add tools/qa-gauntlet/mutants/
git commit -m "qa(gauntlet): 7-mutant saboteur catalog with per-mutant impact records and caught-verification"
```

---

### Task 12: First full calibration run + report

**Files:**
- Create (generated): `production/qa/gauntlet/calibration-<date>/report.md`, `report.json`

- [ ] **Step 1: Run the full catalog**

```bash
python3 tools/qa-gauntlet/saboteur.py
echo "EXIT=$?"
```

Expected: `00-noop-comment` SURVIVED (control — correct); mutants 01–07 caught. Exit 1 solely due to the control survivor is acceptable **iff** that's the only survivor — adjust `main()` to exclude ids starting `00-` from the survived count used for the exit code (one-line change + one pytest case asserting a `00-` survivor doesn't fail the run; commit with the same TDD cycle as Task 10).

- [ ] **Step 2: For any real survivor:** file a bug via the `bug-report` skill at `production/qa/bugs/BUG-oracle-blindspot-<mutant-id>.md` quoting the report row. Do not delete the mutant.

- [ ] **Step 3: detect_changes + commit**

```bash
git add production/qa/gauntlet/calibration-*/ tools/qa-gauntlet/saboteur.py tools/qa-gauntlet/test_saboteur.py
git commit -m "qa(gauntlet): first saboteur calibration — kill rate <N>/7 (control excluded)"
```

---

### Task 13: Skill-file updates

**Files:**
- Modify: `.claude/skills/qa-gauntlet/SKILL.md`
- Create: `.claude/skills/qa-gauntlet-calibrate/SKILL.md`

- [ ] **Step 1: Update `/qa-gauntlet` SKILL.md**

Replace the **Phase B — Execution** body with:

```markdown
### Phase B — Execution (canonical driver)

Run the shipped driver — do NOT hand-roll batch loops or oracle checks:

```bash
tools/qa-gauntlet/run-gauntlet.sh --run-id <RUN_ID> [--tiers "1 2 3 4 5 extra"] \
  [--seeds 42,7,123] [--roving 2]
```

The driver resolves dotnet itself (PATH, then ~/.dotnet/dotnet), runs each tier's batch
plus an identical repeat batch, runs `gauntlet_oracle_eval` (which enforces strict
`gauntlet.*` keys — unknown keys fail, legacy `emcon` warns), and invokes
`tools/qa-gauntlet/evaluate_run.py` for the tier and run verdicts. Roving seeds are
derived from the run id and recorded in `roving-seeds.txt`.
```

Replace the numbered seven-oracle prose list in **Phase C** with:

```markdown
Oracles are code: read `tier-N/verdict.json` and the run-level `verdict.json`.
Fields: `stability`, `determinism`, `victory_roe`, `goldens`, `sanity` (per tier);
`tiers`, `token_coverage` (run level). Any `"status": "fail"` fails the tier — triage
each failure into `scenario-data` / `sim-code` / `oracle` / `flaky` exactly as before.
Golden mismatches (`goldens`) are `sim-code` unless a deliberate behavior change is
documented — then re-bless per `tools/qa-gauntlet/goldens/README.md`. `token_coverage`
reds mean a claimed dimension produced zero observable effect — treat as `scenario-data`
against the claiming scenario. Phase E's tier gate = driver exit code 0.
```

Append to the **Final phase** AAR item 1: `Include: "Last oracle calibration: <date>, kill rate <N/M> (production/qa/gauntlet/calibration-<date>/report.md)".`

- [ ] **Step 2: Create `/qa-gauntlet-calibrate` SKILL.md**

```markdown
---
name: qa-gauntlet-calibrate
description: Measure QA Gauntlet oracle sensitivity by running the saboteur mutant catalog — deliberately broken sim builds in throwaway worktrees must turn the ladder red. Use after oracle/expect/golden changes, after sim refactors, or monthly; or when the user asks to "calibrate the gauntlet", "run the saboteur", "check oracle kill rate".
---

# QA Gauntlet Calibrate — Oracle Sensitivity (Saboteur)

Measures P(detect | defect): applies each curated mutant patch in a disposable git
worktree, builds, runs the anchor ladder subset (tiers 1/3/5 × seed 42) plus the
ReplayGolden filter, and reports which oracles fired.

## Run

```bash
python3 tools/qa-gauntlet/saboteur.py            # full catalog
python3 tools/qa-gauntlet/saboteur.py --mutants 01-pd-weakened   # one mutant
```

Preconditions (tool enforces): clean tracked working tree; dotnet resolvable.
Baseline ladder must be green at HEAD — run `tools/qa-gauntlet/run-gauntlet.sh` first
if in doubt; calibrating oracles against a broken baseline is meaningless.

## Read the report

`production/qa/gauntlet/calibration-<date>/report.md`:
- `00-noop-comment` must SURVIVE (control — proves no false positives).
- Every other SURVIVED row is a named oracle blind spot: file
  `production/qa/bugs/BUG-oracle-blindspot-<mutant-id>.md` via the bug-report skill.
- `INVALID-MUTANT` (build failure) proves nothing: fix or remove the patch.

## Rules

- Mutants never touch locked-eval files (`saboteur.py` refuses).
- Never commit from a saboteur worktree; the tool removes worktrees on exit.
- Adding a mutant: follow the procedure in
  `docs/superpowers/plans/2026-07-28-qa-gauntlet-effectiveness.md` Task 11
  (temp-worktree patch generation + GitNexus impact record + caught-verification).
- Cite the latest kill rate in every `/qa-gauntlet` AAR.
```

- [ ] **Step 3: detect_changes + commit**

```bash
git add .claude/skills/qa-gauntlet/SKILL.md .claude/skills/qa-gauntlet-calibrate/SKILL.md
git commit -m "qa(gauntlet): skills call the canonical driver; add /qa-gauntlet-calibrate"
```

---

### Task 14: Final verification

- [ ] **Step 1: Full test suite (monotonic check)**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test ProjectAegis.sln -v minimal
python3 -m pytest tools/qa-gauntlet/ -q
```

Expected: dotnet total ≥ **1921** (1912 baseline + 9 new xunit), 0 failures; pytest green.

- [ ] **Step 2: Replay + smoke gates**

```bash
dotnet test ProjectAegis.sln -v minimal --no-build --filter "PlayModeSmokeHarnessTests|ReplayGolden"
```

Expected: 4/4 + 38/38.

- [ ] **Step 3: One final canonical ladder run** — `tools/qa-gauntlet/run-gauntlet.sh --run-id gauntlet-final-check --roving 2` → exit 0.

- [ ] **Step 4: Compare against main**

`detect_changes({scope: "compare", base_ref: "main", repo: "/home/username01/cmano-clone"})` — confirm changed C# symbols are only `GauntletPolicyStrictKeys` + `GauntletOracleEvalCommand.Run` + tests; everything else is tools/skills/docs/data.

- [ ] **Step 5: Submit the stack (do NOT merge)**

```bash
gt submit --stack --no-interactive
```

Include in the PR body: spec + plan links, kill-rate report path, canonical run verdict path.

---

## Self-review notes (done at authoring)

- **Spec coverage:** driver (T8), evaluate_run oracles 1–7 (T4–T7), goldens+bless (T6, T9), roving (T8), strict keys (T2–T3), saboteur+catalog+calibration (T10–T12), skill updates (T13), rollout order preserved (T2→T14). Spec's "expected-token list seeded from real run" honored in T7 Step 4.
- **Deviation from spec (recon-driven, intentional):** whitelist is the union of DTO + evaluator keys (`expect`/`expectCi` are evaluator-consumed but not on the DTO); `gauntlet.emcon` is warn-not-error until the variability plan lands (3 shipped policies carry it; a hard error would red the ladder on the already-filed OPEN defect). Both recorded in Task 2 code comments.
- **Type consistency:** `Oracle` dict shape (`name/status/evidence`) used identically in T4–T8 and read by `_fired_oracles` in T10; `GauntletStrictKeyReport(Errors, Warnings)` consistent T2→T3; goldens key `"<scenarioId>|<seed>"` consistent T6/T9; driver artifact names consumed by evaluate_run/saboteur match (`results.csv`, `results-repeat.csv`, `oracle-eval.json`, `verdict.json`).
