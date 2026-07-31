#!/usr/bin/env python3
"""QA Gauntlet oracle aggregator — all ladder oracles as code.

Spec: docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md
Modes:
  tier  — evaluate one tier dir (stability, determinism, victory/ROE via
          oracle-eval.json, goldens, sanity) -> tier-N/verdict.json
  run   — aggregate tier verdicts + run-wide token coverage -> verdict.json
  bless — rewrite goldens/anchors.json from a green run's CSVs
  filter-seeds — keep CSV rows whose seed is in the allow-list
  ladder — print scenarios (csv) or ticks for a tier from ladder.yaml
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

import yaml

CSV_HEADER = "scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint"
DEFAULT_LADDER_PATH = Path(__file__).resolve().parent / "ladder.yaml"
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
    if not lines or lines[0] != CSV_HEADER:
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


def filter_csv_by_seeds(src: Path, dst: Path, seeds: set[str]) -> int:
    """Write header + rows whose seed column is in seeds. Return row count kept.

    Fingerprint may contain commas — split limit 7 (8 fields), matching parse_results_csv.
    """
    lines = src.read_text(encoding="utf-8").splitlines()
    if not lines or lines[0] != CSV_HEADER:
        raise ValueError(f"unexpected CSV header in {src}")
    keep = [CSV_HEADER]
    count = 0
    for line in lines[1:]:
        if not line.strip():
            continue
        parts = line.split(",", 7)
        if len(parts) != 8:
            raise ValueError(f"malformed CSV row in {src}: {line[:80]}")
        if parts[1] in seeds:
            keep.append(line)
            count += 1
    dst.write_text("\n".join(keep) + "\n", encoding="utf-8")
    return count


def load_ladder(path: Path | None = None) -> dict:
    ladder_path = path or DEFAULT_LADDER_PATH
    data = yaml.safe_load(ladder_path.read_text(encoding="utf-8"))
    if not isinstance(data, dict) or "tiers" not in data:
        raise ValueError(f"invalid ladder manifest: {ladder_path}")
    return data


def ladder_ticks(ladder: dict, tier_id: str) -> int:
    tiers = ladder["tiers"]
    if tier_id not in tiers:
        raise KeyError(tier_id)
    return int(tiers[tier_id]["ticks"])


def ladder_scenarios(ladder: dict, tier_id: str) -> list[str]:
    tiers = ladder["tiers"]
    if tier_id not in tiers:
        raise KeyError(tier_id)
    return list(tiers[tier_id]["scenarios"])


def _oracle(name: str, failures: list[str], warnings: list[str] | None = None) -> dict:
    status = "fail" if failures else ("warn" if warnings else "pass")
    return {"name": name, "status": status, "evidence": failures + (warnings or [])}


def oracle_stability(tier_dir: Path, rows: list[Row],
                     expected_scenarios: list[str], seeds: list[str]) -> dict:
    """Exact scenario×seed grid: missing, duplicate, and unexpected rows fail."""
    failures: list[str] = []
    expected = {(sid, seed) for sid in expected_scenarios for seed in seeds}
    counts: dict[tuple[str, str], int] = {}
    for r in rows:
        key = (r.scenario_id, r.seed)
        counts[key] = counts.get(key, 0) + 1
    for key in sorted(expected):
        n = counts.get(key, 0)
        if n == 0:
            failures.append(f"missing row: scenario={key[0]} seed={key[1]}")
        elif n > 1:
            failures.append(f"duplicate row: scenario={key[0]} seed={key[1]} count={n}")
    for key, n in sorted(counts.items()):
        if key not in expected:
            failures.append(f"unexpected row: scenario={key[0]} seed={key[1]} count={n}")
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


def _row_key(r: Row) -> tuple[str, str]:
    return (r.scenario_id, r.seed)


def oracle_determinism(tier_dir: Path) -> dict:
    first, repeat = tier_dir / "results.csv", tier_dir / "results-repeat.csv"
    if not first.exists() or not repeat.exists():
        missing = first.name if not first.exists() else repeat.name
        return _oracle("determinism", [f"missing CSV for repeat diff: {missing}"])
    # Structured compare keyed by (scenario_id, seed) — ignore header/blank noise.
    a_by = {_row_key(r): r for r in parse_results_csv(first)}
    b_by = {_row_key(r): r for r in parse_results_csv(repeat)}
    if a_by == b_by:
        return _oracle("determinism", [])
    diff_path = tier_dir / "determinism-diff.txt"
    only_a = sorted(k for k in a_by if k not in b_by or a_by[k] != b_by.get(k))[:20]
    only_b = sorted(k for k in b_by if k not in a_by or b_by[k] != a_by.get(k))[:20]

    def fmt(keys: list[tuple[str, str]], by: dict) -> list[str]:
        return [f"{sid}|{seed}: {by[(sid, seed)].fingerprint[:80]}"
                for sid, seed in keys if (sid, seed) in by]

    diff_path.write_text("--- results.csv only/diff\n" + "\n".join(fmt(only_a, a_by))
                         + "\n+++ results-repeat.csv only/diff\n" + "\n".join(fmt(only_b, b_by)) + "\n",
                         encoding="utf-8")
    return _oracle("determinism", [f"repeat batch diverged; see {diff_path.name} "
                                   f"({len(only_a)}+{len(only_b)} differing keys shown)"])


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


def bless(run_dir: Path, goldens_path: Path, run_id: str, tier_names: list[str],
          anchor_seeds: list[str] | None = None,
          require_run_verdict: bool = False) -> int:
    """Bless golden anchors from evaluated tier CSVs.

    Every requested tier must have a verdict.json. Non-golden oracle fails block.
    Missing verdicts refuse (do not bless unevaluated CSVs). When
    ``require_run_verdict`` is True, ``run_dir/verdict.json`` must exist and
    pass all non-golden oracles (blocks blessing when token_coverage is red).
    """
    anchor_seeds = anchor_seeds or ["42", "7", "123"]
    anchor_set = set(anchor_seeds)
    anchors: dict[str, str] = {}
    for tier in tier_names:
        verdict_path = run_dir / tier / "verdict.json"
        if not verdict_path.exists():
            print(f"bless refused: missing {verdict_path}", file=sys.stderr)
            return 2
        verdict = json.loads(verdict_path.read_text(encoding="utf-8"))
        # A red goldens oracle is the state you re-bless FROM; only non-golden
        # reds (stability, determinism, victory, sanity) block a bless.
        non_golden_red = [name for name, o in verdict.get("oracles", {}).items()
                          if name != "goldens" and o.get("status") == "fail"]
        if non_golden_red:
            print(f"bless refused: {tier} has non-golden red oracles: {non_golden_red}",
                  file=sys.stderr)
            return 2
        csv_path = run_dir / tier / "results.csv"
        if not csv_path.exists():
            print(f"bless: missing {csv_path}", file=sys.stderr)
            return 2
        for r in parse_results_csv(csv_path):
            if r.seed not in anchor_set:
                continue  # roving rows are run-specific; never golden material
            anchors[f"{r.scenario_id}|{r.seed}"] = hashlib.sha256(
                r.fingerprint.encode("utf-8")).hexdigest()
    if require_run_verdict:
        run_verdict_path = run_dir / "verdict.json"
        if not run_verdict_path.exists():
            print(f"bless refused: missing run verdict {run_verdict_path}", file=sys.stderr)
            return 2
        run_verdict = json.loads(run_verdict_path.read_text(encoding="utf-8"))
        run_red = [name for name, o in run_verdict.get("oracles", {}).items()
                   if name != "goldens" and o.get("status") == "fail"]
        if run_red:
            print(f"bless refused: run verdict has red oracles: {run_red}", file=sys.stderr)
            return 2
    goldens_path.parent.mkdir(parents=True, exist_ok=True)
    goldens_path.write_text(json.dumps(
        {"version": 1, "blessedFrom": run_id, "anchors": dict(sorted(anchors.items()))},
        indent=2) + "\n", encoding="utf-8")
    print(f"bless: wrote {len(anchors)} anchors from {run_id} -> {goldens_path}")
    return 0


def oracle_roving_observe(tier_dir: Path, roving_seeds: list[str]) -> dict:
    """Roving rows are exploration, not gate: envelope anomalies surface as warnings.

    Envelope bounds are calibrated on anchor seeds only (README-expect-regen), so
    enforcing them on arbitrary seeds would red every run; observing keeps the signal.
    """
    if not roving_seeds:
        return _oracle("roving_observe", [])
    path = tier_dir / "oracle-eval-roving.json"
    if not path.exists():
        return _oracle("roving_observe", [], [f"missing {path.name} (driver did not run roving eval)"])
    data = json.loads(path.read_text(encoding="utf-8"))
    warnings: list[str] = []
    for s in data.get("scenarios", []):
        for f in s.get("failures", []):
            warnings.append(f"roving {s.get('scenario')}: {f}")
    return _oracle("roving_observe", [], warnings)


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
    bless_p.add_argument("--anchor-seeds", default="42,7,123")
    bless_p.add_argument("--require-run-verdict", action="store_true",
                         help="require run-level verdict.json (token_coverage etc.)")
    filter_p = sub.add_parser("filter-seeds")
    filter_p.add_argument("--in", dest="src", required=True, type=Path)
    filter_p.add_argument("--out", dest="dst", required=True, type=Path)
    filter_p.add_argument("--seeds", required=True,
                          help="comma-separated seed allow-list (anchor seeds)")
    ladder_p = sub.add_parser("ladder")
    ladder_p.add_argument("--tier", required=True, help="tier id: 1..5 or extra")
    ladder_p.add_argument("--field", required=True, choices=("scenarios", "ticks"))
    ladder_p.add_argument("--ladder", type=Path, default=DEFAULT_LADDER_PATH)
    args = parser.parse_args(argv)

    if args.mode == "filter-seeds":
        seeds = {s for s in args.seeds.split(",") if s}
        n = filter_csv_by_seeds(args.src, args.dst, seeds)
        print(json.dumps({"kept": n, "out": str(args.dst)}))
        return 0

    if args.mode == "ladder":
        try:
            ladder = load_ladder(args.ladder)
            if args.field == "ticks":
                print(ladder_ticks(ladder, args.tier))
            else:
                print(",".join(ladder_scenarios(ladder, args.tier)))
        except KeyError:
            print(f"unknown tier: {args.tier}", file=sys.stderr)
            return 1
        return 0

    if args.mode == "bless":
        return bless(args.run_dir, args.goldens, args.run_id,
                     [t for t in args.tiers.split(",") if t],
                     anchor_seeds=[s for s in args.anchor_seeds.split(",") if s],
                     require_run_verdict=args.require_run_verdict)

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
        roving_seeds = [s for s in args.roving_seeds.split(",") if s]
        oracles = [
            oracle_stability(tier_dir, rows, scenarios, seeds),
            oracle_determinism(tier_dir),
            oracle_victory(tier_dir),
            oracle_roving_observe(tier_dir, roving_seeds),
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
