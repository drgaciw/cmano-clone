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


def oracle_determinism(tier_dir: Path) -> dict:
    first, repeat = tier_dir / "results.csv", tier_dir / "results-repeat.csv"
    if not first.exists() or not repeat.exists():
        missing = first.name if not first.exists() else repeat.name
        return _oracle("determinism", [f"missing CSV for repeat diff: {missing}"])
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
            oracle_determinism(tier_dir),
            oracle_victory(tier_dir),
            oracle_sanity(rows, seeds),
        ]
        out = args.out or (tier_dir / "verdict.json")
        ok = write_verdict(out, tier_dir.name, oracles)
        print(json.dumps({"tier": tier_dir.name, "pass": ok}))
        return 0 if ok else 1
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
