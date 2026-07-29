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
