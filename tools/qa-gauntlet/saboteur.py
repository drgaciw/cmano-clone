#!/usr/bin/env python3
"""QA Gauntlet saboteur — oracle-sensitivity calibration via curated mutants.

Applies each catalog patch in a disposable git worktree, builds, runs the anchor
ladder subset (tiers 1,3,5 x anchor seeds) + the ReplayGolden test filter, and
records which oracles fired. Nothing is ever committed from a worktree.

Kill rule: caught = subset driver exit != 0 OR ReplayGolden filter fails.
Build failure = invalid-mutant (fix or drop the patch; it proves nothing).
Control mutants (id prefix "00-") are behavior-neutral no-ops: they MUST survive;
a caught control is a false-positive pipeline bug and fails the run.

Spec: docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md
Subset note: all three anchor seeds are used (not just 42) because some required
tokens (e.g. NO_AMMO) only occur at seeds 7/123 — a seed-42-only subset would
red token_coverage at baseline and poison the kill measurement.
"""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILD_TIMEOUT_S = 600
RUN_TIMEOUT_S = 900
SUBSET_TIERS = "1 3 5"
CONTROL_PREFIX = "00-"
LOCKED = (
    "src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs",
    "src/ProjectAegis.Delegation.Demo/Program.cs",
    "src/ProjectAegis.Delegation.UnityAdapter/Baltic/DelegationBridge.cs",
)


def blocking_dirty_paths(porcelain: str) -> list[str]:
    """Dirty tracked paths that invalidate calibration (worktrees build from HEAD,
    so uncommitted changes here would NOT be calibrated). Docs/etc. don't block."""
    relevant = ("src/", "data/", "tools/qa-gauntlet/", "ProjectAegis.sln", "global.json")
    out: list[str] = []
    for line in porcelain.splitlines():
        path = line[3:].strip() if len(line) > 3 else ""
        if path.startswith(relevant):
            out.append(path)
    return out


def load_catalog(path: Path) -> list[dict]:
    import yaml
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


def exit_code_for(summary: dict, results: list[dict]) -> int:
    """0 iff no invalid mutants, no non-control survivors, and no caught controls."""
    if summary["invalid"] > 0:
        return 1
    for r in results:
        is_control = r["id"].startswith(CONTROL_PREFIX)
        if is_control and r["outcome"] == "caught":
            return 1  # false positive: a no-op turned an oracle red
        if not is_control and r["outcome"] == "survived":
            return 1  # oracle blind spot
    return 0


def render_report(summary: dict, results: list[dict]) -> str:
    lines = ["# Saboteur calibration report", "",
             f"**Kill rate: {summary['killRate']}** "
             f"(caught {summary['caught']}, survived {summary['survived']}, "
             f"invalid {summary['invalid']}; controls excluded from pass/fail by id prefix 00-)",
             "",
             "| Mutant | Outcome | Fired oracles | Expected |", "|---|---|---|---|"]
    for r in results:
        outcome = r["outcome"].upper() if r["outcome"] != "caught" else "caught"
        lines.append(f"| {r['id']} | {outcome} | {', '.join(r['firedOracles']) or '—'} "
                     f"| {', '.join(r['expectedOracles']) or '—'} |")
    lines.append("")
    lines.append("Every non-control SURVIVED row is a named oracle blind spot — file a bug per row.")
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
    if not run_dir.exists():
        return []
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
                          "--run-id", f"saboteur-{m['id']}", "--tiers", SUBSET_TIERS,
                          "--roving", "0",
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

    porcelain = subprocess.run(["git", "status", "--porcelain", "--untracked-files=no"],
                               cwd=ROOT, capture_output=True, text=True).stdout
    blocking = blocking_dirty_paths(porcelain)
    if blocking:
        print("saboteur: refusing to run — uncommitted changes in calibration-relevant paths "
              "(worktrees build from HEAD, so these would NOT be calibrated):", file=sys.stderr)
        for p in blocking:
            print(f"  {p}", file=sys.stderr)
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
    return exit_code_for(summary, results)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
