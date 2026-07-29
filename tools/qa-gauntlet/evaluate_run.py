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
        verdict_path = run_dir / tier / "verdict.json"
        if verdict_path.exists():
            verdict = json.loads(verdict_path.read_text(encoding="utf-8"))
            if not verdict.get("pass", False):
                print(f"bless refused: {tier} verdict is red", file=sys.stderr)
                return 2
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
    run_p = sub.add_parser("run")
    run_p.add_argument("--run-dir", required=True, type=Path)
    run_p.add_argument("--tiers", required=True)
    run_p.add_argument("--expected-tokens", required=True, type=Path)
    run_p.add_argument("--manifest", type=Path,
                       default=Path("data/glossary/abort_reason_manifest.json"))
    run_p.add_argument("--anchor-seeds", default="42,7,123")
    run_p.add_argument("--out", type=Path)
    bless_p = sub.add_parser("bless")
    bless_p.add_argument("--run-dir", required=True, type=Path)
    bless_p.add_argument("--run-id", required=True)
    bless_p.add_argument("--goldens", required=True, type=Path)
    bless_p.add_argument("--tiers", default="tier-1,tier-2,tier-3,tier-4,tier-5,tier-extra")
    args = parser.parse_args(argv)

    if args.mode == "bless":
        return bless(args.run_dir, args.goldens, args.run_id,
                     [t for t in args.tiers.split(",") if t])

    if args.mode == "run":
        tier_names = [t for t in args.tiers.split(",") if t]
        all_rows: list[Row] = []
        tier_failures: list[str] = []
        for tier in tier_names:
            csv_path = args.run_dir / tier / "results.csv"
            if csv_path.exists():
                all_rows.extend(parse_results_csv(csv_path))
            else:
                tier_failures.append(f"{tier}: results.csv missing")
            verdict_path = args.run_dir / tier / "verdict.json"
            if not verdict_path.exists():
                tier_failures.append(f"{tier}: verdict.json missing")
            elif not json.loads(verdict_path.read_text(encoding="utf-8")).get("pass", False):
                tier_failures.append(f"{tier}: verdict red")
        oracles = [
            _oracle("tiers", tier_failures),
            oracle_token_coverage(all_rows, args.expected_tokens, args.manifest),
        ]
        out = args.out or (args.run_dir / "verdict.json")
        ok = write_verdict(out, "run", oracles)
        print(json.dumps({"run": str(args.run_dir), "pass": ok}))
        return 0 if ok else 1

    if args.mode == "tier":
        tier_dir = args.tier_dir
        scenarios = [s for s in args.scenarios.split(",") if s]
        seeds = [s for s in args.anchor_seeds.split(",") if s] + \
                [s for s in args.roving_seeds.split(",") if s]
        rows = parse_results_csv(tier_dir / "results.csv")
        anchor_seeds = [s for s in args.anchor_seeds.split(",") if s]
        oracles = [
            oracle_stability(tier_dir, rows, scenarios, seeds),
            oracle_determinism(tier_dir),
            oracle_victory(tier_dir),
            oracle_sanity(rows, seeds),
        ]
        if args.goldens:
            oracles.append(oracle_goldens(rows, args.goldens, anchor_seeds))
        out = args.out or (tier_dir / "verdict.json")
        ok = write_verdict(out, tier_dir.name, oracles)
        print(json.dumps({"tier": tier_dir.name, "pass": ok}))
        return 0 if ok else 1
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
