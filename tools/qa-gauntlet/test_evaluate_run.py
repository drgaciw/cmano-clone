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


def test_determinism_pass_ignores_row_order(tmp_path):
    from evaluate_run import oracle_determinism
    make_csv(tmp_path, [row_line(seed="42"), row_line(seed="7")])
    make_csv(tmp_path, [row_line(seed="7"), row_line(seed="42")], name="results-repeat.csv")
    assert oracle_determinism(tmp_path)["status"] == "pass"


def test_determinism_ignores_header_and_blank_noise(tmp_path):
    """Structured Row compare must not false-fail on blank/header line noise."""
    from evaluate_run import oracle_determinism
    a = tmp_path / "results.csv"
    b = tmp_path / "results-repeat.csv"
    a.write_text(HEADER + row_line(seed="42") + "\n" + row_line(seed="7"), encoding="utf-8")
    b.write_text(HEADER + row_line(seed="42") + row_line(seed="7"), encoding="utf-8")
    assert oracle_determinism(tmp_path)["status"] == "pass"


def test_determinism_fails_on_fingerprint_drift(tmp_path):
    from evaluate_run import oracle_determinism
    make_csv(tmp_path, [row_line(fp="A")])
    make_csv(tmp_path, [row_line(fp="B")], name="results-repeat.csv")
    o = oracle_determinism(tmp_path)
    assert o["status"] == "fail"
    assert (tmp_path / "determinism-diff.txt").exists()


def test_determinism_fails_when_repeat_missing(tmp_path):
    from evaluate_run import oracle_determinism
    make_csv(tmp_path, [row_line()])
    assert oracle_determinism(tmp_path)["status"] == "fail"


def test_filter_csv_by_seeds_preserves_fingerprint_commas(tmp_path):
    from evaluate_run import filter_csv_by_seeds, parse_results_csv
    src = make_csv(tmp_path, [
        row_line(seed="42", fp="A|1,2 B,C"),
        row_line(seed="99991", fp="ROVING,x"),
        row_line(seed="7", fp="D|3,4"),
    ])
    dst = tmp_path / "anchors.csv"
    kept = filter_csv_by_seeds(src, dst, {"42", "7", "123"})
    assert kept == 2
    rows = parse_results_csv(dst)
    assert [r.seed for r in rows] == ["42", "7"]
    assert rows[0].fingerprint == "A|1,2 B,C"
    assert rows[1].fingerprint == "D|3,4"


def test_filter_seeds_cli_subcommand(tmp_path):
    from evaluate_run import main
    src = make_csv(tmp_path, [row_line(seed="42"), row_line(seed="99"), row_line(seed="7")])
    dst = tmp_path / "out.csv"
    rc = main(["filter-seeds", "--in", str(src), "--out", str(dst), "--seeds", "42,7,123"])
    assert rc == 0
    text = dst.read_text(encoding="utf-8")
    assert text.startswith("scenarioId,")
    assert ",99," not in text
    assert text.count("\n") >= 3  # header + 2 rows + trailing newline


def test_victory_reads_oracle_eval_json(tmp_path):
    from evaluate_run import oracle_victory
    (tmp_path / "oracle-eval.json").write_text(json.dumps(
        {"ok": True, "allPassed": True,
         "scenarios": [{"scenario": "s1", "passed": True, "failures": [], "warnings": ["legacy emcon"]}]}))
    o = oracle_victory(tmp_path)
    assert o["status"] == "warn"
    assert any("legacy emcon" in e for e in o["evidence"])


def test_victory_fails_on_allpassed_false_or_missing(tmp_path):
    from evaluate_run import oracle_victory
    (tmp_path / "oracle-eval.json").write_text(json.dumps(
        {"ok": False, "allPassed": False,
         "scenarios": [{"scenario": "s1", "passed": False, "failures": ["score out of bounds"]}]}))
    assert oracle_victory(tmp_path)["status"] == "fail"
    assert oracle_victory(tmp_path / "nowhere")["status"] == "fail"


def _golden_file(tmp_path, fp="CATALOG_UNIT:x:surface T|1,extra", sid="s1", seed="42"):
    import hashlib
    g = {"version": 1, "blessedFrom": "test",
         "anchors": {f"{sid}|{seed}": hashlib.sha256(fp.encode()).hexdigest()}}
    p = tmp_path / "anchors.json"
    p.write_text(json.dumps(g))
    return p


def test_goldens_pass_on_matching_hash(tmp_path):
    from evaluate_run import oracle_goldens
    p = make_csv(tmp_path, [row_line()])
    g = _golden_file(tmp_path)
    assert oracle_goldens(parse_results_csv(p), g, ["42"])["status"] == "pass"


def test_goldens_fail_on_mismatch_and_missing_anchor(tmp_path):
    from evaluate_run import oracle_goldens
    p = make_csv(tmp_path, [row_line(fp="DIFFERENT"), row_line(seed="7")])
    g = _golden_file(tmp_path)
    o = oracle_goldens(parse_results_csv(p), g, ["42", "7"])
    assert o["status"] == "fail"
    assert any("mismatch" in e for e in o["evidence"])
    assert any("no golden" in e for e in o["evidence"])


def test_goldens_ignore_roving_rows(tmp_path):
    from evaluate_run import oracle_goldens
    p = make_csv(tmp_path, [row_line(), row_line(seed="99991", fp="ROVING")])
    g = _golden_file(tmp_path)
    assert oracle_goldens(parse_results_csv(p), g, ["42"])["status"] == "pass"


def test_bless_writes_all_anchor_hashes(tmp_path):
    from evaluate_run import bless
    run = tmp_path / "run"
    (run / "tier-1").mkdir(parents=True)
    make_csv(run / "tier-1", [row_line(), row_line(seed="7", fp="FP7")])
    out = tmp_path / "anchors.json"
    rc = bless(run, out, "run-x", ["tier-1"])
    assert rc == 0
    g = json.loads(out.read_text())
    assert g["blessedFrom"] == "run-x" and len(g["anchors"]) == 2


def test_bless_excludes_roving_rows(tmp_path):
    # Roving seeds are run-specific; blessing them would bloat anchors.json with
    # entries no future run can match.
    from evaluate_run import bless
    run = tmp_path / "run"
    (run / "tier-1").mkdir(parents=True)
    make_csv(run / "tier-1", [row_line(), row_line(seed="7", fp="FP7"),
                              row_line(seed="55555", fp="ROVING")])
    out = tmp_path / "anchors.json"
    assert bless(run, out, "run-x", ["tier-1"], anchor_seeds=["42", "7", "123"]) == 0
    g = json.loads(out.read_text())
    assert len(g["anchors"]) == 2
    assert not any("55555" in k for k in g["anchors"])


def test_bless_refuses_red_verdict(tmp_path):
    from evaluate_run import bless
    run = tmp_path / "run"
    (run / "tier-1").mkdir(parents=True)
    make_csv(run / "tier-1", [row_line()])
    (run / "tier-1" / "verdict.json").write_text(json.dumps(
        {"tier": "tier-1", "pass": False,
         "oracles": {"stability": {"status": "fail", "evidence": ["boom"]}}}))
    assert bless(run, tmp_path / "anchors.json", "run-x", ["tier-1"]) == 2


def test_bless_allows_goldens_only_red(tmp_path):
    # A red goldens oracle is exactly the state you re-bless FROM; it must not block.
    from evaluate_run import bless
    run = tmp_path / "run"
    (run / "tier-1").mkdir(parents=True)
    make_csv(run / "tier-1", [row_line()])
    (run / "tier-1" / "verdict.json").write_text(json.dumps(
        {"tier": "tier-1", "pass": False,
         "oracles": {"stability": {"status": "pass", "evidence": []},
                     "goldens": {"status": "fail", "evidence": ["mismatch"]}}}))
    assert bless(run, tmp_path / "anchors.json", "run-x", ["tier-1"]) == 0


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
    from evaluate_run import oracle_token_coverage
    rows = parse_results_csv(make_csv(tmp_path, [row_line(fp="CATALOG_UNIT:a:surface MAGAZINE_SEED:a:1:2")]))
    ok = oracle_token_coverage(rows, _expected(tmp_path, ["CATALOG_UNIT:", "MAGAZINE_SEED:"]), _manifest(tmp_path))
    assert ok["status"] == "pass"
    bad = oracle_token_coverage(rows, _expected(tmp_path, ["CATALOG_UNIT:", "ContactChange|"]), _manifest(tmp_path))
    assert bad["status"] == "fail"
    assert any("ContactChange|" in e and "0" in e for e in bad["evidence"])


def test_token_coverage_warn_list_never_fails(tmp_path):
    from evaluate_run import oracle_token_coverage
    rows = parse_results_csv(make_csv(tmp_path, [row_line(fp="CATALOG_UNIT:a:surface")]))
    o = oracle_token_coverage(rows, _expected(tmp_path, ["CATALOG_UNIT:"], warn=["EMCON_OFF"]),
                              _manifest(tmp_path))
    assert o["status"] == "warn"
    assert any("EMCON_OFF" in e for e in o["evidence"])


def test_roving_observe_warns_never_fails(tmp_path):
    from evaluate_run import oracle_roving_observe
    (tmp_path / "oracle-eval-roving.json").write_text(json.dumps(
        {"ok": False, "allPassed": False,
         "scenarios": [{"scenario": "s1", "passed": False,
                        "failures": ["seed=99948: kills 0 < min 1"]}]}))
    o = oracle_roving_observe(tmp_path, ["99948"])
    assert o["status"] == "warn"
    assert any("99948" in e for e in o["evidence"])


def test_roving_observe_pass_when_no_roving_or_absent(tmp_path):
    from evaluate_run import oracle_roving_observe
    assert oracle_roving_observe(tmp_path, [])["status"] == "pass"
    assert oracle_roving_observe(tmp_path, ["99948"])["status"] == "warn"  # file absent -> warn, not fail


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
