#!/usr/bin/env python3
"""Offline unit and fixture tests for tools/jev/backlog_gap_eval.py."""

from __future__ import annotations

import http.server
import json
import sys
import threading
import time
from pathlib import Path
from unittest.mock import MagicMock

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "jev"))

import backlog_gap_eval as bge  # noqa: E402

RUBRIC_PATH = ROOT / "data" / "jev" / "backlog-gap-v1.questions.json"


def _ok_jev_answers(
    *,
    noul_val: float = 0.92,
    score_val: float = 1.90,
    choice_val: str = "complete",
    choice_conf: float = 0.95,
    rem_choice: str = "none",
    rem_conf: float = 0.96,
) -> dict:
    return {
        "linear_presence_and_traceability": {"type": "noul", "noul": noul_val},
        "linear_ac_and_scope_completeness": {"type": "noul", "noul": noul_val},
        "linear_completeness_score": {
            "type": "score",
            "score": score_val,
            "legend": {"0": "missing", "1": "incomplete", "2": "complete"},
            "probabilities": {"0": 0.0, "1": 0.1, "2": 0.9},
            "confidence": 0.9,
        },
        "linear_classification": {
            "type": "choice",
            "choice": choice_val,
            "probabilities": {choice_val: 0.95, "other": 0.05},
            "confidence": choice_conf,
        },
        "notion_presence_and_traceability": {"type": "noul", "noul": noul_val},
        "notion_completeness_and_honesty": {"type": "noul", "noul": noul_val},
        "notion_completeness_score": {
            "type": "score",
            "score": score_val,
            "legend": {"0": "missing", "1": "incomplete", "2": "complete"},
            "probabilities": {"0": 0.0, "1": 0.1, "2": 0.9},
            "confidence": 0.9,
        },
        "notion_classification": {
            "type": "choice",
            "choice": choice_val,
            "probabilities": {choice_val: 0.95, "other": 0.05},
            "confidence": choice_conf,
        },
        "recommended_remediation": {
            "type": "choice",
            "choice": rem_choice,
            "probabilities": {rem_choice: 0.96, "other": 0.04},
            "confidence": rem_conf,
        },
    }


def _mock_transport(status_code: int = 200, payload: dict | None = None, text: str = "") -> MagicMock:
    resp = MagicMock()
    resp.status_code = status_code
    resp.text = text or (json.dumps(payload) if payload is not None else "")
    resp.json.return_value = payload
    transport = MagicMock()
    transport.post.return_value = resp
    return transport


def _sample_req(lifecycle: str = "active", **extra) -> dict:
    req = {
        "req_id": "KCX-01",
        "title": "Kill-chain inspection report",
        "source_file": "Game-Requirements/drafts/23-Kill-Chain-Explainability.md",
        "source_lines": "10-35",
        "domain_project": "cmano-clone",
        "lifecycle_state": lifecycle,
        "summary": "Emit structured kill-chain report.",
        "acceptance_criteria": ["CLI outputs deterministic JSON report"],
    }
    req.update(extra)
    return req


def _complete_linear_match(req_id: str = "KCX-01", source_file: str = "Game-Requirements/drafts/23-Kill-Chain-Explainability.md") -> dict:
    return {
        "id": "DRG-901",
        "title": f"[{req_id}] Kill-chain inspection report",
        "state": "Backlog",
        "project": "cmano-clone",
        "labels": [],
        "url": "https://linear.app/aegis/issue/DRG-901",
        "description": (
            f"Git source of truth: `{source_file}` ({req_id}).\n"
            "Draft — owner acceptance pending.\n"
            "Acceptance Criteria:\n- [ ] CLI outputs deterministic JSON report\n"
            "Notion: https://www.notion.so/task-901"
        ),
    }


def _complete_notion_match(req_id: str = "KCX-01", source_file: str = "Game-Requirements/drafts/23-Kill-Chain-Explainability.md") -> dict:
    return {
        "id": "page-901",
        "title": f"[{req_id}] Kill-chain inspection report",
        "surface": "tasks",
        "status": "Not started",
        "project": "Project Aegis",
        "has_icon": True,
        "url": "https://www.notion.so/task-901",
        "note": (
            f"Linear: https://linear.app/aegis/issue/DRG-901 (DRG-901)\n"
            f"Source: `{source_file}` ({req_id}), Draft — owner acceptance pending."
        ),
    }


# 1. Question pack validation tests
def test_pinned_question_pack_is_valid_and_within_limits():
    raw = json.loads(RUBRIC_PATH.read_text(encoding="utf-8"))
    questions = bge.validate_question_pack(raw)
    assert 1 <= len(questions) <= 20
    assert len(questions) == 9
    for qname, q in questions.items():
        assert bge.KEY_PATTERN.match(qname)
        assert 1 <= len(q["instructions"]) <= 600
        if q["type"] == "choice":
            assert 2 <= len(q["criteria"]) <= 255
            assert "other" in q["criteria"] or "none" in q["criteria"]
            for opt_k, opt_v in q["criteria"].items():
                assert bge.KEY_PATTERN.match(opt_k)
                assert 1 <= len(opt_v) <= 400
        elif q["type"] == "score":
            assert 2 <= len(q["criteria"]) <= 10
            assert all(1 <= len(c) <= 400 for c in q["criteria"])
        elif q["type"] == "noul":
            assert set(q["criteria"].keys()) == {"true", "false"}
            assert all(1 <= len(v) <= 400 for v in q["criteria"].values())
    byte_len = bge.validate_request_body(
        {"model": bge.PRIMARY_MODEL, "state": "sample factual state", "questions": questions}
    )
    assert byte_len <= bge.MAX_BODY_BYTES


# 2. Request validator & hygiene tests
def test_validate_request_rejects_over_20_questions():
    questions = {
        f"q_{i}": {"type": "noul", "instructions": "Valid question?"}
        for i in range(21)
    }
    with pytest.raises(ValueError, match="1..20"):
        bge.validate_request_body({"state": "facts", "questions": questions})


@pytest.mark.parametrize("bad_key", ["BadKey", "bad-key", "a" * 41, ""])
def test_validate_request_rejects_invalid_keys(bad_key):
    questions = {bad_key: {"type": "noul", "instructions": "Valid question?"}}
    with pytest.raises(ValueError, match="Invalid question key"):
        bge.validate_request_body({"state": "facts", "questions": questions})


def test_validate_request_rejects_long_instructions_and_bad_choice_score():
    with pytest.raises(ValueError, match="600 chars"):
        bge.validate_request_body(
            {"state": "ok", "questions": {"q1": {"type": "noul", "instructions": "x" * 601}}}
        )
    with pytest.raises(ValueError, match="2..255"):
        bge.validate_request_body(
            {
                "state": "ok",
                "questions": {
                    "q1": {
                        "type": "choice",
                        "instructions": "Pick one",
                        "criteria": {"only_one": "single"},
                    }
                },
            }
        )
    with pytest.raises(ValueError, match="escape option"):
        bge.validate_request_body(
            {
                "state": "ok",
                "questions": {
                    "q1": {
                        "type": "choice",
                        "instructions": "Pick one",
                        "criteria": {"alpha": "first", "beta": "second"},
                    }
                },
            }
        )
    with pytest.raises(ValueError, match="2..10"):
        bge.validate_request_body(
            {
                "state": "ok",
                "questions": {
                    "q1": {
                        "type": "score",
                        "instructions": "Score",
                        "criteria": [f"lvl {i}" for i in range(11)],
                    }
                },
            }
        )


def test_validate_request_rejects_oversized_body_and_hygiene_violations(monkeypatch):
    questions = bge.load_questions(RUBRIC_PATH)
    with pytest.raises(ValueError, match="32768"):
        bge.validate_request_body({"state": "x" * (33 * 1024), "questions": questions})

    with pytest.raises(ValueError, match="\\.db3"):
        bge.validate_request_body({"state": "Loaded cmo_secret.db3 table", "questions": questions})

    monkeypatch.setenv("OPENROUTER_API_KEY", "sk-or-very-secret-test-value")
    with pytest.raises(ValueError, match="secret"):
        bge.validate_request_body(
            {"state": "key=sk-or-very-secret-test-value", "questions": questions}
        )


# 3. Dotenv & credential resolution tests
def test_load_dotenv_does_not_overwrite_existing_env(tmp_path, monkeypatch):
    env_file = tmp_path / ".env"
    env_file.write_text(
        "OPENROUTER_API_KEY='from-dotenv'\nNOTION_API_KEY=\"notion-from-dotenv\"\n",
        encoding="utf-8",
    )
    monkeypatch.setenv("OPENROUTER_API_KEY", "from-process-env")
    monkeypatch.delenv("NOTION_API_KEY", raising=False)

    loaded = bge.load_dotenv_files([env_file])
    assert loaded == env_file
    assert bge.resolve_openrouter_key() == "from-process-env"
    assert bge.os.environ["NOTION_API_KEY"] == "notion-from-dotenv"


def test_missing_credentials_returns_exit_2_blocked(tmp_path, monkeypatch, capsys):
    req_file = tmp_path / "req.json"
    req_file.write_text(
        json.dumps(
            {
                "state": "factual state",
                "questions": {
                    "q1": {"type": "noul", "instructions": "Pass?"}
                },
            }
        ),
        encoding="utf-8",
    )
    monkeypatch.setattr(bge, "load_dotenv_files", lambda: None)
    monkeypatch.setenv("OPENROUTER_API_KEY", "   ")
    rc = bge.main(["decide", "--input", str(req_file)])
    assert rc == 2
    out = json.loads(capsys.readouterr().out)
    assert out["status"] == "blocked"


# 4. Primary -> Fallback routing tests
def test_decide_primary_success_preserves_provenance():
    questions = bge.load_questions(RUBRIC_PATH)
    recorded = {
        "id": "gen-dec-12345",
        "model": "typesafe/jev-1.14-20261014",
        "provider": "TypeSafe",
        "answers": _ok_jev_answers(),
        "usage": {"input_tokens": 500, "output_tokens": 80, "cost": 0.0001},
    }
    transport = _mock_transport(200, recorded)
    res = bge.decide_with_fallback(
        "factual state",
        questions,
        api_key="or-test-key",
        transport=transport,
    )
    assert res["status"] == "ok"
    assert res["route_used"] == "openrouter_alpha_decisions"
    assert res["fallback_used"] is False
    assert res["response"]["id"] == "gen-dec-12345"
    assert res["response"]["provider"] == "TypeSafe"
    assert res["response"]["usage"]["input_tokens"] == 500
    assert transport.post.call_args.args[0] == bge.PRIMARY_URL
    assert transport.post.call_args.kwargs["json_body"]["model"] == bge.PRIMARY_MODEL


def test_decide_falls_back_to_jev_1_13_on_primary_failure():
    questions = bge.load_questions(RUBRIC_PATH)
    fb_body = {
        "id": "gen-sys1-999",
        "model": "typesafe/jev-1.13",
        "provider": "TypeSafe",
        "answers": _ok_jev_answers(),
        "usage": {"input_tokens": 410, "output_tokens": 60},
    }
    err_resp = MagicMock()
    err_resp.status_code = 502
    err_resp.text = "bad gateway"
    ok_resp = MagicMock()
    ok_resp.status_code = 200
    ok_resp.json.return_value = fb_body

    transport = MagicMock()
    transport.post.side_effect = [err_resp, ok_resp]

    res = bge.decide_with_fallback(
        "factual state",
        questions,
        api_key="or-test-key",
        transport=transport,
    )
    assert res["status"] == "ok"
    assert res["route_used"] == "openrouter_v1_systemone"
    assert res["fallback_used"] is True
    assert res["request"]["model"] == bge.FALLBACK_MODEL
    assert res["response"]["id"] == "gen-sys1-999"
    assert len(res["attempts"]) == 2
    assert transport.post.call_args_list[0].args[0] == bge.PRIMARY_URL
    assert transport.post.call_args_list[1].args[0] == bge.FALLBACK_URL


def test_decide_no_fallback_and_both_failed_redact_secrets():
    questions = bge.load_questions(RUBRIC_PATH)
    secret = "sk-or-super-secret-xyz"
    transport = MagicMock()
    transport.post.side_effect = TimeoutError(f"failed with {secret}")

    res_no_fb = bge.decide_with_fallback(
        "factual state",
        questions,
        api_key=secret,
        transport=transport,
        allow_fallback=False,
    )
    assert res_no_fb["status"] == "no_advisory"
    assert len(res_no_fb["attempts"]) == 1
    assert secret not in json.dumps(res_no_fb)
    assert "[REDACTED]" in res_no_fb["error"]

    transport.post.reset_mock()
    transport.post.side_effect = [TimeoutError("t1"), ValueError("bad json")]
    res_both = bge.decide_with_fallback(
        "factual state",
        questions,
        api_key=secret,
        transport=transport,
        allow_fallback=True,
    )
    assert res_both["status"] == "no_advisory"
    assert len(res_both["attempts"]) == 2
    assert res_both["fallback_used"] is True


# 5. Transport deadline tests
def _serve(handler_body):
    class _H(http.server.BaseHTTPRequestHandler):
        def do_POST(self):  # noqa: N802
            self.rfile.read(int(self.headers.get("Content-Length", 0)))
            handler_body(self)

        def log_message(self, *args):
            pass

    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), _H)
    server.daemon_threads = True
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return server


def test_urllib_transport_enforces_overall_deadline_on_slow_drip():
    def slow_drip(h):
        h.send_response(200)
        h.send_header("Content-Length", "40")
        h.end_headers()
        for _ in range(40):
            try:
                h.wfile.write(b" ")
                h.wfile.flush()
            except OSError:
                return
            time.sleep(0.05)

    server = _serve(slow_drip)
    try:
        url = f"http://127.0.0.1:{server.server_address[1]}/"
        start = time.monotonic()
        with pytest.raises(TimeoutError):
            bge.UrllibTransport().post(url, headers={}, json_body={}, timeout=0.25)
        assert time.monotonic() - start < 0.6
    finally:
        server.shutdown()


# 6. Script-first deterministic + Jev evaluation tests
def test_empty_linear_matches_stays_missing_even_with_optimistic_jev():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(lifecycle="draft")
    transport = _mock_transport(
        200,
        {"id": "gen-1", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers(noul_val=0.99)},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[],
        notion_matches=[_complete_notion_match()],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=None,
    )
    assert ev["linear_status"] == "missing"
    assert ev["notion_status"] == "complete"
    assert ev["recommended_action"] == "create_linear"


def test_shipped_verified_exempts_missing_linear_ticket():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(
        req_id="FR-01",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
        lifecycle_state="shipped",
        shipped_evidence="DelegationOrchestrator.Tick verified in 4,800 green tests",
    )
    notion = _complete_notion_match(
        req_id="FR-01",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
    )
    transport = _mock_transport(
        200,
        {"id": "gen-ship", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers()},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[],
        notion_matches=[notion],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev["linear_status"] == "complete"
    assert ev["sub_status"] == "shipped_verified"
    assert ev["notion_status"] == "complete"
    assert ev["recommended_action"] == "none"


def test_deterministic_failures_never_overridden_by_optimistic_jev():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(lifecycle="draft")
    # Missing AC + unauthorized Surface: line on Backlog
    bad_linear = {
        "id": "DRG-902",
        "title": "KCX-01 Kill-chain report",
        "state": "Backlog",
        "project": "cmano-clone",
        "description": (
            "Git source of truth: Game-Requirements/drafts/23-Kill-Chain-Explainability.md KCX-01.\n"
            "Draft — owner acceptance pending.\n"
            "Surface: tools/foo.py\n"
            "Notion: https://www.notion.so/task-901"
        ),
    }
    # Notion only in Frozen Git mirror -> missing on active surface
    frozen_notion = {
        "id": "frozen-1",
        "title": "KCX-01",
        "surface": "frozen_git_mirror",
        "status": "Done",
        "note": "DRG-902 Game-Requirements/drafts/23-Kill-Chain-Explainability.md",
    }
    transport = _mock_transport(
        200,
        {"id": "gen-2", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers(noul_val=0.99)},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[bad_linear],
        notion_matches=[frozen_notion],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=None,
    )
    assert ev["deterministic_checks"]["P_LIN_CONTRACT"] is False
    assert ev["deterministic_checks"]["linear_no_surface_line"] is False
    assert ev["deterministic_checks"]["P_NOT_EXISTS"] is False
    assert ev["linear_status"] == "incomplete"
    assert ev["notion_status"] == "missing"
    assert ev["recommended_action"] == "both"


def test_cpq_isolation_blocks_complete():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(lifecycle="draft")
    cpq_linear = {**_complete_linear_match(), "project": "OPEN-CRM-CPQ"}
    transport = _mock_transport(
        200,
        {"id": "gen-iso", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers()},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[cpq_linear],
        notion_matches=[_complete_notion_match()],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=None,
    )
    assert ev["deterministic_checks"]["P_ISO"] is False
    assert ev["linear_status"] == "incomplete"
    assert ev["notion_status"] == "incomplete"


def test_low_choice_confidence_triggers_human_review_and_blocks_complete():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(lifecycle="draft")
    transport = _mock_transport(
        200,
        {
            "id": "gen-lowconf",
            "model": bge.PRIMARY_MODEL,
            "answers": _ok_jev_answers(choice_conf=0.42),
        },
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[_complete_linear_match()],
        notion_matches=[_complete_notion_match()],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=None,
    )
    assert ev["needs_human_review"] is True
    assert ev["linear_status"] == "incomplete"
    assert ev["notion_status"] == "incomplete"
    assert ev["recommended_action"] == "both"


def test_evaluate_catalog_matches_project_md_matrix_schema():
    questions = bge.load_questions(RUBRIC_PATH)
    catalog = {
        "generated_at": "2026-11-22T23:00:00Z",
        "source_files": [
            {"path": "Game-Requirements/drafts/23-Kill-Chain-Explainability.md", "category": "B", "lines": 100, "in_scope": True},
            {"path": "Game-Requirements/drafts/24-Holistic.md", "category": "B", "lines": 80, "in_scope": True},
        ],
        "requirements": [
            _sample_req(req_id="KCX-01", lifecycle="draft"),
            _sample_req(req_id="HOL-01", source_file="Game-Requirements/drafts/24-Holistic.md", lifecycle="draft"),
        ],
    }
    matches = {
        "by_req_id": {
            "KCX-01": {
                "linear": [_complete_linear_match()],
                "notion": [_complete_notion_match()],
            },
            "HOL-01": {"linear": [], "notion": []},
        }
    }
    transport = _mock_transport(
        200,
        {"id": "gen-mat", "model": "typesafe/jev-1.14-20261014", "answers": _ok_jev_answers()},
    )
    matrix = bge.evaluate_catalog(
        catalog,
        matches,
        questions=questions,
        api_key="or-key",
        transport=transport,
        skip_jev_on_missing=True,
        repo_root=None,
    )
    bge.validate_matrix(matrix)
    assert matrix["summary"] == {
        "total_requirements": 2,
        "source_files_covered": 2,
        "linear": {"complete": 1, "incomplete": 0, "missing": 1},
        "notion": {"complete": 1, "incomplete": 0, "missing": 1},
    }
    assert matrix["evaluations"][0]["recommended_action"] == "none"
    assert matrix["evaluations"][1]["recommended_action"] == "both"
    # Only KCX-01 called Jev; HOL-01 was skipped deterministically as missing
    assert transport.post.call_count == 1


# 7. CLI smoke tests
def test_cli_validate_decide_dry_and_eval_out(tmp_path, monkeypatch, capsys):
    assert bge.main(["validate", "--questions", str(RUBRIC_PATH)]) == 0
    val_out = json.loads(capsys.readouterr().out)
    assert val_out["status"] == "valid"

    req_file = tmp_path / "decide.json"
    req_file.write_text(
        json.dumps(
            {
                "state": "Clean factual state",
                "questions": {
                    "scope_pick": {
                        "type": "choice",
                        "instructions": "Choose target",
                        "criteria": {"tasks_db": "Tasks DB", "other": "Other"},
                    }
                },
            }
        ),
        encoding="utf-8",
    )
    assert bge.main(["decide", "--input", str(req_file), "--dry-request"]) == 0
    dry_out = json.loads(capsys.readouterr().out)
    assert dry_out["model"] == bge.PRIMARY_MODEL

    src_doc = tmp_path / "req.md"
    src_doc.write_text("# KCX-01\n", encoding="utf-8")
    cat_file = tmp_path / "catalog.json"
    mat_file = tmp_path / "matches.json"
    out_json = tmp_path / "matrix.json"
    out_md = tmp_path / "matrix.md"
    cat_file.write_text(
        json.dumps(
            {
                "source_files": [],
                "requirements": [_sample_req(lifecycle="draft", source_file="req.md")],
            }
        ),
        encoding="utf-8",
    )
    mat_file.write_text(json.dumps({"by_req_id": {}}), encoding="utf-8")

    monkeypatch.setattr(bge, "load_dotenv_files", lambda: None)
    monkeypatch.delenv("OPENROUTER_API_KEY", raising=False)
    assert (
        bge.main(
            [
                "eval",
                "--catalog",
                str(cat_file),
                "--matches",
                str(mat_file),
                "--repo-root",
                str(tmp_path),
                "--out",
                str(out_json),
                "--markdown-out",
                str(out_md),
            ]
        )
        == 0
    )
    assert out_json.is_file()
    assert out_md.is_file()
    written = json.loads(out_json.read_text(encoding="utf-8"))
    assert written["summary"]["linear"]["missing"] == 1
    assert written["summary"]["notion"]["missing"] == 1


def test_missing_source_file_blocks_complete_when_matched():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(source_file="nonexistent/missing.md", lifecycle="draft")
    transport = _mock_transport(
        200,
        {"id": "gen-src", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers()},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[_complete_linear_match(source_file="nonexistent/missing.md")],
        notion_matches=[_complete_notion_match(source_file="nonexistent/missing.md")],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev["deterministic_checks"]["P_SRC"] is False
    assert ev["linear_status"] == "incomplete"
    assert ev["notion_status"] == "incomplete"


# 8. Iteration 2 regression tests (Fixes #1-#6)
def test_shipped_verified_with_historical_done_linear_match_is_complete():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(
        req_id="FR-01",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
        lifecycle_state="shipped",
        shipped_evidence="Verified in 4,800 green tests",
    )
    historical_done = {
        "id": "DRG-100",
        "title": "Legacy overview implementation",
        "state": "Done",
        "project": "cmano-clone",
        "description": "Implemented 01-Project-Overview.md.",
    }
    notion = _complete_notion_match(
        req_id="FR-01",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
    )
    transport = _mock_transport(
        200,
        {"id": "gen-ship-done", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers()},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[historical_done],
        notion_matches=[notion],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev["deterministic_checks"]["shipped_verified_exempt"] is True
    assert ev["deterministic_checks"]["P_LIN_EXISTS"] is True
    assert ev["deterministic_checks"]["P_LIN_CONTRACT"] is True
    assert ev["linear_status"] == "complete"
    assert ev["sub_status"] == "shipped_verified"
    assert ev["notion_status"] == "complete"
    assert ev["recommended_action"] == "none"


def test_shipped_with_merged_done_linear_match_and_no_explicit_evidence_is_complete():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(
        req_id="FR-02",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
        lifecycle_state="shipped",
    )
    merged_done = {
        "id": "DRG-101",
        "title": "Merged feature",
        "state": "Done",
        "project": "cmano-clone",
        "merged_to_main": True,
        "description": "Completed prior to rubric v1.",
    }
    notion = _complete_notion_match(
        req_id="FR-02",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
    )
    transport = _mock_transport(
        200,
        {"id": "gen-ship-merged", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers()},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[merged_done],
        notion_matches=[notion],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev["deterministic_checks"]["shipped_verified_exempt"] is True
    assert ev["deterministic_checks"]["P_LIN_CONTRACT"] is True
    assert ev["linear_status"] == "complete"
    assert ev["sub_status"] == "shipped_verified"


def test_shipped_with_unmerged_done_linear_match_fails_contract():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(
        req_id="FR-03",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
        lifecycle_state="shipped",
        shipped_evidence="Local branch only",
    )
    unmerged_done = {
        "id": "DRG-102",
        "title": "Prematurely closed feature",
        "state": "Done",
        "project": "cmano-clone",
        "merged_to_main": False,
        "description": "01-Project-Overview.md FR-03 Acceptance Criteria: pending.",
    }
    notion = _complete_notion_match(
        req_id="FR-03",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
    )
    transport = _mock_transport(
        200,
        {"id": "gen-unmerged", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers()},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[unmerged_done],
        notion_matches=[notion],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev["deterministic_checks"]["shipped_verified_exempt"] is False
    assert ev["deterministic_checks"]["linear_honest_state"] is False
    assert ev["deterministic_checks"]["P_LIN_CONTRACT"] is False
    assert ev["linear_status"] == "incomplete"


def test_shipped_with_cpq_or_missing_source_cannot_use_shipped_exempt():
    req_missing_src = _sample_req(
        req_id="FR-04",
        source_file="nonexistent/missing.md",
        lifecycle_state="shipped",
        shipped_evidence="Verified",
    )
    checks_src = bge.evaluate_requirement_deterministic(
        req_missing_src, [], [_complete_notion_match()], repo_root=ROOT
    )
    assert checks_src["P_SRC"] is False
    assert checks_src["shipped_verified_exempt"] is False

    req_cpq = _sample_req(
        req_id="FR-05",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
        domain_project="OPEN-CRM-CPQ",
        lifecycle_state="shipped",
        shipped_evidence="Verified",
    )
    checks_cpq = bge.evaluate_requirement_deterministic(
        req_cpq, [], [_complete_notion_match()], repo_root=ROOT
    )
    assert checks_cpq["P_ISO"] is False
    assert checks_cpq["shipped_verified_exempt"] is False


def test_classify_requirement_preserves_jev_decision_id():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(lifecycle="draft")
    transport = _mock_transport(
        200,
        {
            "id": "gen-dec-provenance-42",
            "model": "typesafe/jev-1.14-20261014",
            "answers": _ok_jev_answers(),
        },
    )
    ev_with_jev = bge.classify_requirement(
        req,
        linear_matches=[_complete_linear_match()],
        notion_matches=[_complete_notion_match()],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev_with_jev["jev_decision_id"] == "gen-dec-provenance-42"

    ev_skipped = bge.classify_requirement(
        req,
        linear_matches=[],
        notion_matches=[],
        questions=questions,
        api_key="or-key",
        transport=transport,
        skip_jev_on_missing=True,
        repo_root=ROOT,
    )
    assert ev_skipped["jev_decision_id"] is None


def test_draft_marked_approved_on_notion_fails_contract():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(lifecycle="draft")
    approved_notion = {
        **_complete_notion_match(),
        "status": "Approved",
    }
    transport = _mock_transport(
        200,
        {"id": "gen-draft-approved", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers()},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[_complete_linear_match()],
        notion_matches=[approved_notion],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev["deterministic_checks"]["notion_honest_state"] is False
    assert ev["deterministic_checks"]["P_NOT_CONTRACT"] is False
    assert ev["notion_status"] == "incomplete"


@pytest.mark.parametrize(
    "surface",
    [
        "frozen_git_mirror",
        "frozen git mirror",
        "pin_requirements",
        "requirements_db",
        "📌 requirements",
        "🗄️ Frozen Git mirror",
        "📌 Requirements DB",
        "Requirements",
        "Frozen Git mirror / 📌 Requirements",
    ],
)
def test_frozen_mirror_variants_fail_active_notion_exists(surface):
    req = _sample_req(lifecycle="draft")
    frozen = {**_complete_notion_match(), "surface": surface}
    checks = bge.evaluate_requirement_deterministic(
        req, [_complete_linear_match()], [frozen], repo_root=ROOT
    )
    assert checks["P_NOT_EXISTS"] is False
    assert checks["notion_only_frozen_mirror"] is True


@pytest.mark.parametrize(
    "active_surface",
    [
        "tasks",
        "📝 Requirements drafts",
        "cmano-clone/Requirements drafts",
    ],
)
def test_active_notion_surfaces_not_rejected_as_frozen_mirror(active_surface):
    req = _sample_req(lifecycle="draft")
    active = {**_complete_notion_match(), "surface": active_surface}
    checks = bge.evaluate_requirement_deterministic(
        req, [_complete_linear_match()], [active], repo_root=ROOT
    )
    assert checks["P_NOT_EXISTS"] is True
    assert checks["notion_only_frozen_mirror"] is False


def test_evaluate_catalog_counts_only_covered_in_scope_source_files():
    questions = bge.load_questions(RUBRIC_PATH)
    catalog = {
        "source_files": [
            {"path": "f0.md", "category": "A", "lines": 10, "in_scope": True},
            {"path": "f1.md", "category": "A", "lines": 10, "in_scope": True},
            {"path": "f2.md", "category": "A", "lines": 10, "in_scope": True},
            {"path": "f3.md", "category": "A", "lines": 10, "in_scope": True},
            {"path": "f4.md", "category": "A", "lines": 10, "in_scope": True},
            {"path": "out.md", "category": "A", "lines": 10, "in_scope": False},
        ],
        "requirements": [
            _sample_req(req_id="R-0", source_file="f0.md", lifecycle="draft"),
        ],
    }
    matrix = bge.evaluate_catalog(
        catalog,
        {"by_req_id": {}},
        questions=questions,
        api_key=None,
        skip_jev_on_missing=True,
        repo_root=None,
    )
    assert matrix["summary"]["total_requirements"] == 1
    assert matrix["summary"]["source_files_covered"] == 1


@pytest.mark.parametrize(
    "excluded_path",
    [
        "tests/uat/cpq-smoke.md",
        "tests/uat/01-cpq-quote-flow.md",
        "tests/uat/lead-to-fulfillment-e2e.md",
    ],
)
def test_p_iso_rejects_tests_uat_cpq_and_lead_to_fulfillment_paths(excluded_path):
    req = _sample_req(source_file=excluded_path, lifecycle="draft")
    checks_req = bge.evaluate_requirement_deterministic(
        req, [_complete_linear_match()], [_complete_notion_match()], repo_root=None
    )
    assert checks_req["P_ISO"] is False

    clean_req = _sample_req(lifecycle="draft")
    bad_lin = {
        **_complete_linear_match(),
        "description": _complete_linear_match()["description"] + f"\nRef: {excluded_path}",
    }
    checks_match = bge.evaluate_requirement_deterministic(
        clean_req, [bad_lin], [_complete_notion_match()], repo_root=ROOT
    )
    assert checks_match["P_ISO"] is False


def test_missing_choice_confidence_and_uncertain_remediation_record_gaps_and_review():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(lifecycle="draft")

    none_conf_answers = _ok_jev_answers()
    none_conf_answers["linear_classification"].pop("confidence")
    transport = _mock_transport(
        200,
        {"id": "gen-none-conf", "model": bge.PRIMARY_MODEL, "answers": none_conf_answers},
    )
    ev_none = bge.classify_requirement(
        req,
        linear_matches=[_complete_linear_match()],
        notion_matches=[_complete_notion_match()],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev_none["needs_human_review"] is True
    assert ev_none["linear_status"] == "incomplete"
    assert any("linear_classification uncertain" in g for g in ev_none["gaps"])

    rem_other_answers = _ok_jev_answers(rem_choice="other", rem_conf=0.90)
    transport2 = _mock_transport(
        200,
        {"id": "gen-rem-other", "model": bge.PRIMARY_MODEL, "answers": rem_other_answers},
    )
    ev_rem = bge.classify_requirement(
        req,
        linear_matches=[_complete_linear_match()],
        notion_matches=[_complete_notion_match()],
        questions=questions,
        api_key="or-key",
        transport=transport2,
        repo_root=ROOT,
    )
    assert ev_rem["needs_human_review"] is True
    assert ev_rem["linear_status"] == "incomplete"
    assert ev_rem["notion_status"] == "incomplete"
    assert any("recommended_remediation uncertain" in g for g in ev_rem["gaps"])


def test_key_pattern_rejects_trailing_newline():
    with pytest.raises(ValueError, match="Invalid question key"):
        bge.validate_question("valid_key\n", {"type": "noul", "instructions": "Ok?"})
    with pytest.raises(ValueError, match="option key"):
        bge.validate_question(
            "valid_key",
            {
                "type": "choice",
                "instructions": "Pick",
                "criteria": {"ok\n": "Valid", "other": "Other"},
            },
        )


def test_decide_with_fallback_handles_oversized_fallback_model_cleanly():
    min_q = {"q1": {"type": "noul", "instructions": "Ok?"}}
    model_override = "a"
    overhead = len(
        json.dumps(
            {"model": model_override, "state": "", "questions": min_q},
            ensure_ascii=False,
        ).encode("utf-8")
    )
    exact_state = "x" * (bge.MAX_BODY_BYTES - overhead)
    err_resp = MagicMock()
    err_resp.status_code = 502
    err_resp.text = "bad gateway"
    transport = MagicMock()
    transport.post.return_value = err_resp

    res = bge.decide_with_fallback(
        exact_state,
        min_q,
        api_key="sk-test-key",
        transport=transport,
        allow_fallback=True,
        model_override=model_override,
    )
    assert res["status"] == "no_advisory"
    assert res["fallback_used"] is True
    assert len(res["attempts"]) == 2
    assert "validation_error:" in res["attempts"][1]["error"]
    assert transport.post.call_count == 1


def test_build_compact_state_respects_20_questions_and_quote_escaping():
    q20 = {
        f"q_{i}": {
            "type": "choice",
            "instructions": "I" * 580,
            "criteria": {
                "complete": "C" * 380,
                "incomplete": "N" * 380,
                "other": "O" * 100,
            },
        }
        for i in range(20)
    }
    quote_heavy_req = _sample_req(
        lifecycle="draft",
        summary='"' * 400 + "\\\\" * 200,
        acceptance_criteria=['"quoted" \\\\ item'] * 12,
    )
    quote_heavy_matches = [
        {
            **_complete_linear_match(),
            "description": ('"escaped" ' * 80),
        }
        for _ in range(8)
    ]
    checks = bge.evaluate_requirement_deterministic(
        quote_heavy_req, quote_heavy_matches, [_complete_notion_match()], repo_root=ROOT
    )
    state_str = bge.build_compact_state(
        quote_heavy_req,
        quote_heavy_matches,
        [_complete_notion_match()],
        checks,
        questions=q20,
    )
    byte_len = bge.validate_request_body(
        {"model": bge.PRIMARY_MODEL, "state": state_str, "questions": q20}
    )
    assert byte_len <= bge.MAX_BODY_BYTES


# 9. Iteration 3 regression tests (M1 Challenger 2 fixes)
@pytest.mark.parametrize(
    ("surface", "expected"),
    [
        ("Tasks", True),
        ("Tasks DB", True),
        ("Tasks / Project Aegis", True),
        ("Project Aegis / Tasks", True),
        ("Notion Tasks DB", True),
        ("tasks_db", True),
        ("notion_tasks", True),
        ("", True),
        ("📋 Tasks / Project Aegis", True),
        ("cmano-clone / 📝 Requirements drafts", False),
        ("cmano-clone/Requirements drafts", False),
        ("🗄️ Frozen Git mirror", False),
        ("Frozen Git mirror / Tasks", False),
        ("📌 Requirements DB", False),
        ("Requirements", False),
    ],
)
def test_is_tasks_db_match_variants_and_non_tasks_surfaces(surface, expected):
    assert bge._is_tasks_db_match({"surface": surface}) is expected


@pytest.mark.parametrize("surface", ["Tasks / Project Aegis", "Tasks DB"])
def test_tasks_db_surface_variants_enforce_project_icon_and_blocked_status(surface):
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(lifecycle="draft")
    bad_notion = {
        "id": "p1",
        "surface": surface,
        "status": "Not started",
        "project": "Wrong",
        "has_icon": False,
        "note": "DRG-100 KCX-01 Draft\nBlocked: owner",
    }
    checks = bge.evaluate_requirement_deterministic(
        req, [_complete_linear_match()], [bad_notion], repo_root=ROOT
    )
    assert bge._is_tasks_db_match(bad_notion) is True
    assert checks["notion_valid_project_and_icon"] is False
    assert checks["P_NOT_CONTRACT"] is False

    transport = _mock_transport(
        200,
        {"id": "gen-tasks-hygiene", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers()},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[_complete_linear_match()],
        notion_matches=[bad_notion],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev["notion_status"] == "incomplete"

    wiki_notion = {
        "id": "p2",
        "surface": "cmano-clone / 📝 Requirements drafts",
        "status": "Draft",
        "project": "",
        "has_icon": False,
        "note": "DRG-901 KCX-01 Game-Requirements/drafts/23-Kill-Chain-Explainability.md Draft",
    }
    wiki_checks = bge.evaluate_requirement_deterministic(
        req, [_complete_linear_match()], [wiki_notion], repo_root=ROOT
    )
    assert bge._is_tasks_db_match(wiki_notion) is False
    assert wiki_checks["notion_valid_project_and_icon"] is True
    assert wiki_checks["P_NOT_CONTRACT"] is True


def test_evaluate_catalog_normalizes_paths_and_handles_in_scope_false_only_manifest():
    questions = bge.load_questions(RUBRIC_PATH)
    norm_catalog = {
        "source_files": [
            {"path": "f0.md", "category": "A", "lines": 10, "in_scope": True},
            {"path": ".\\f1.md", "category": "A", "lines": 10, "in_scope": True},
        ],
        "requirements": [
            _sample_req(req_id="R-0", source_file="./f0.md", lifecycle="draft"),
            _sample_req(req_id="R-1", source_file="f1.md", lifecycle="draft"),
            _sample_req(req_id="R-2", source_file="./f1.md", lifecycle="draft"),
        ],
    }
    norm_matrix = bge.evaluate_catalog(
        norm_catalog,
        {"by_req_id": {}},
        questions=questions,
        api_key=None,
        skip_jev_on_missing=True,
        repo_root=None,
    )
    assert norm_matrix["summary"]["source_files_covered"] == 2

    out_only_catalog = {
        "source_files": [
            {"path": "./out.md", "category": "A", "lines": 10, "in_scope": False},
        ],
        "requirements": [
            _sample_req(req_id="R-10", source_file="f0.md", lifecycle="draft"),
            _sample_req(req_id="R-11", source_file="out.md", lifecycle="draft"),
        ],
    }
    out_only_matrix = bge.evaluate_catalog(
        out_only_catalog,
        {"by_req_id": {}},
        questions=questions,
        api_key=None,
        skip_jev_on_missing=True,
        repo_root=None,
    )
    assert out_only_matrix["summary"]["source_files_covered"] == 1


def test_open_backlog_linear_issue_cannot_piggyback_on_sibling_done_issue():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(
        req_id="FR-01",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
        lifecycle_state="shipped",
        shipped_evidence="Verified in CI",
    )
    done_sibling = {
        "id": "DRG-100",
        "title": "FR-01 initial implementation",
        "state": "Done",
        "project": "cmano-clone",
        "description": (
            "Source: Game-Requirements/requirements/01-Project-Overview.md FR-01\n"
            "Acceptance Criteria:\n- [x] Implemented\n"
            "Notion: https://www.notion.so/task-100"
        ),
    }
    ac_less_backlog = {
        "id": "DRG-200",
        "title": "FR-01 residual backlog follow-up",
        "state": "Backlog",
        "project": "cmano-clone",
        "description": "Residual work without AC or Notion link",
    }
    notion = _complete_notion_match(
        req_id="FR-01",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
    )
    transport = _mock_transport(
        200,
        {"id": "gen-piggyback", "model": bge.PRIMARY_MODEL, "answers": _ok_jev_answers()},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[done_sibling, ac_less_backlog],
        notion_matches=[notion],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev["deterministic_checks"]["shipped_verified_exempt"] is False
    assert ev["deterministic_checks"]["linear_cites_source"] is False
    assert ev["deterministic_checks"]["linear_cites_req_id"] is True
    assert ev["deterministic_checks"]["linear_has_ac"] is False
    assert ev["deterministic_checks"]["linear_has_notion_link"] is False
    assert ev["deterministic_checks"]["P_LIN_CONTRACT"] is False
    assert ev["linear_status"] == "incomplete"

    unlinked_backlog = {
        "id": "DRG-201",
        "title": "Residual follow-up",
        "state": "Todo",
        "project": "cmano-clone",
        "description": "Residual work missing source, req_id, AC, and Notion link",
    }
    checks_unlinked = bge.evaluate_requirement_deterministic(
        req, [done_sibling, unlinked_backlog], [notion], repo_root=ROOT
    )
    assert checks_unlinked["linear_cites_source"] is False
    assert checks_unlinked["linear_cites_req_id"] is False
    assert checks_unlinked["linear_has_ac"] is False
    assert checks_unlinked["linear_has_notion_link"] is False
    assert checks_unlinked["P_LIN_CONTRACT"] is False

    complete_backlog = _complete_linear_match(
        req_id="FR-01",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
    )
    checks_good = bge.evaluate_requirement_deterministic(
        req, [done_sibling, complete_backlog], [notion], repo_root=ROOT
    )
    assert checks_good["shipped_verified_exempt"] is False
    assert checks_good["P_LIN_CONTRACT"] is True


# 10. M3-A evaluator alignment regression tests
def test_shipped_verified_exempt_with_low_noul_and_uncertain_remediation_is_complete():
    questions = bge.load_questions(RUBRIC_PATH)
    shipped_path = "src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs"
    req = _sample_req(
        req_id="OV-SC-G1",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
        lifecycle="shipped",
        shipped_evidence="DelegationBridge.cs + PlayModeSmokeHarnessTests verified",
        shipped_files=[shipped_path],
    )
    notion = {
        "id": "wiki-ov-sc-g1",
        "surface": "cmano-clone / Shipped Baseline",
        "status": "Done",
        "note": "Source: Game-Requirements/requirements/01-Project-Overview.md OV-SC-G1 shipped_verified",
    }
    answers = _ok_jev_answers(noul_val=0.92, score_val=1.90, choice_val="complete", choice_conf=0.95)
    answers["linear_presence_and_traceability"] = {"type": "noul", "noul": 0.62}
    answers["linear_ac_and_scope_completeness"] = {"type": "noul", "noul": 0.14}
    answers["linear_completeness_score"]["score"] = 1.85
    answers["linear_classification"]["choice"] = "complete"
    answers["linear_classification"]["confidence"] = 0.89
    answers["recommended_remediation"] = {
        "type": "choice",
        "choice": "none",
        "probabilities": {"none": 0.29, "other": 0.71},
        "confidence": 0.29,
    }
    answers["notion_classification"]["confidence"] = 0.95
    # With Notion complete and recommended_remediation ignored on shipped_exempt Linear,
    # also test Notion separately with confident remediation=none so overall needs_human_review is False.
    answers_clean = dict(answers)
    answers_clean["recommended_remediation"] = {
        "type": "choice",
        "choice": "none",
        "probabilities": {"none": 0.96, "other": 0.04},
        "confidence": 0.96,
    }

    checks = bge.evaluate_requirement_deterministic(req, [], [notion], repo_root=ROOT)
    compact = json.loads(
        bge.build_compact_state(req, [], [notion], checks, questions=questions)
    )
    assert compact["requirement"]["shipped_files"] == [shipped_path]

    # Even with recommended_remediation confidence=0.29, Linear side is complete and not flagged for review.
    lin_status, lin_review, lin_gaps = bge._evaluate_side_with_jev(
        side="linear",
        exists=bool(checks["P_LIN_EXISTS"]),
        contract_ok=bool(checks["P_LIN_CONTRACT"]),
        shipped_exempt=bool(checks["shipped_verified_exempt"]),
        isolation_ok=bool(checks["P_ISO"]),
        source_ok=bool(checks["P_SRC"]),
        jev_answers=answers,
    )
    assert lin_status == "complete"
    assert lin_review is False
    assert lin_gaps == []

    transport = _mock_transport(
        200,
        {"id": "gen-ov-sc-g1", "model": bge.PRIMARY_MODEL, "answers": answers_clean},
    )
    ev = bge.classify_requirement(
        req,
        linear_matches=[],
        notion_matches=[notion],
        questions=questions,
        api_key="or-key",
        transport=transport,
        repo_root=ROOT,
    )
    assert ev["linear_status"] == "complete"
    assert ev["notion_status"] == "complete"
    assert ev["sub_status"] == "shipped_verified"
    assert ev["needs_human_review"] is False
    assert "DelegationBridge.cs + PlayModeSmokeHarnessTests verified" in ev["verification_evidence"]
    assert f"trunk_file:{shipped_path}" in ev["verification_evidence"]


def test_shipped_verified_exempt_still_fails_if_jev_classifies_incomplete_or_low_score():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(
        req_id="OV-SC-G2",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
        lifecycle="shipped",
        shipped_evidence="Verified in CI",
    )
    notion = {
        "id": "wiki-ov-sc-g2",
        "surface": "cmano-clone / Shipped Baseline",
        "status": "Done",
        "note": "Source: Game-Requirements/requirements/01-Project-Overview.md OV-SC-G2 shipped_verified",
    }

    bad_choice = _ok_jev_answers(choice_val="incomplete", choice_conf=0.88)
    ev_inc = bge.classify_requirement(
        req,
        linear_matches=[],
        notion_matches=[notion],
        questions=questions,
        api_key="or-key",
        transport=_mock_transport(
            200, {"id": "gen-inc", "model": bge.PRIMARY_MODEL, "answers": bad_choice}
        ),
        repo_root=ROOT,
    )
    assert ev_inc["linear_status"] == "incomplete"

    low_score = _ok_jev_answers(score_val=1.10)
    ev_low = bge.classify_requirement(
        req,
        linear_matches=[],
        notion_matches=[notion],
        questions=questions,
        api_key="or-key",
        transport=_mock_transport(
            200, {"id": "gen-low", "model": bge.PRIMARY_MODEL, "answers": low_score}
        ),
        repo_root=ROOT,
    )
    assert ev_low["linear_status"] == "incomplete"

    uncertain_choice = _ok_jev_answers(choice_val="other", choice_conf=0.40)
    ev_unc = bge.classify_requirement(
        req,
        linear_matches=[],
        notion_matches=[notion],
        questions=questions,
        api_key="or-key",
        transport=_mock_transport(
            200, {"id": "gen-unc", "model": bge.PRIMARY_MODEL, "answers": uncertain_choice}
        ),
        repo_root=ROOT,
    )
    assert ev_unc["linear_status"] == "incomplete"
    assert ev_unc["needs_human_review"] is True


def test_draft_with_historical_done_slice_and_compliant_open_backlog_passes():
    req = _sample_req(
        req_id="AME-6.11",
        source_file="Game-Requirements/drafts/23-Kill-Chain-Explainability.md",
        lifecycle="draft",
    )
    done_slice = {
        "id": "DRG-179",
        "state": "Done",
        "project": "cmano-clone",
        "description": "Source: Game-Requirements/drafts/23-Kill-Chain-Explainability.md AME-6.11",
    }
    open_backlog = _complete_linear_match(
        req_id="AME-6.11",
        source_file="Game-Requirements/drafts/23-Kill-Chain-Explainability.md",
    )
    tasks_done = {
        "id": "n-done",
        "surface": "Tasks / Project Aegis",
        "status": "Done",
        "project": "Project Aegis",
        "has_icon": True,
        "note": "DRG-179 AME-6.11 Game-Requirements/drafts/23-Kill-Chain-Explainability.md",
    }
    wiki_draft = {
        "id": "n-draft",
        "surface": "cmano-clone / Requirements drafts",
        "status": "Draft",
        "note": (
            "DRG-901 AME-6.11 Game-Requirements/drafts/23-Kill-Chain-Explainability.md "
            "Draft — owner acceptance pending."
        ),
    }

    checks_combined = bge.evaluate_requirement_deterministic(
        req, [done_slice, open_backlog], [tasks_done, wiki_draft], repo_root=ROOT
    )
    assert checks_combined["linear_honest_state"] is True
    assert checks_combined["P_LIN_CONTRACT"] is True
    assert checks_combined["notion_honest_state"] is True
    assert checks_combined["P_NOT_CONTRACT"] is True

    checks_done_only = bge.evaluate_requirement_deterministic(
        req, [done_slice], [tasks_done], repo_root=ROOT
    )
    assert checks_done_only["linear_honest_state"] is False
    assert checks_done_only["P_LIN_CONTRACT"] is False
    assert checks_done_only["notion_honest_state"] is False
    assert checks_done_only["P_NOT_CONTRACT"] is False


def test_deferred_phase_n_with_historical_done_and_compliant_open_backlog_passes():
    req = _sample_req(
        req_id="DBI-8.5",
        source_file="Game-Requirements/drafts/23-Kill-Chain-Explainability.md",
        lifecycle="deferred_phase_n",
    )
    done_slice = {
        "id": "DRG-180",
        "state": "Done",
        "project": "cmano-clone",
        "description": "Source: Game-Requirements/drafts/23-Kill-Chain-Explainability.md DBI-8.5",
    }
    open_backlog = {
        "id": "DRG-902",
        "state": "Backlog",
        "project": "cmano-clone",
        "description": (
            "Git source of truth: Game-Requirements/drafts/23-Kill-Chain-Explainability.md DBI-8.5\n"
            "[Phase N] Deferred — do not start before 2027-01-15.\n"
            "Notion: https://www.notion.so/task-902"
        ),
    }
    tasks_done = {
        "id": "n-done",
        "surface": "Tasks / Project Aegis",
        "status": "Done",
        "project": "Project Aegis",
        "has_icon": True,
        "note": "DRG-180 DBI-8.5",
    }
    tasks_open = {
        "id": "n-open",
        "surface": "Tasks / Project Aegis",
        "status": "Not started",
        "project": "Project Aegis",
        "has_icon": True,
        "note": (
            "Linear: DRG-902 DBI-8.5 Game-Requirements/drafts/23-Kill-Chain-Explainability.md "
            "[Phase N] Deferred — do not start"
        ),
    }

    checks_combined = bge.evaluate_requirement_deterministic(
        req, [done_slice, open_backlog], [tasks_done, tasks_open], repo_root=ROOT
    )
    assert checks_combined["linear_honest_state"] is True
    assert checks_combined["P_LIN_CONTRACT"] is True
    assert checks_combined["notion_honest_state"] is True
    assert checks_combined["P_NOT_CONTRACT"] is True

    checks_done_only = bge.evaluate_requirement_deterministic(
        req, [done_slice], [tasks_done], repo_root=ROOT
    )
    assert checks_done_only["linear_honest_state"] is False
    assert checks_done_only["P_LIN_CONTRACT"] is False
    assert checks_done_only["notion_honest_state"] is False
    assert checks_done_only["P_NOT_CONTRACT"] is False


def test_shipped_baseline_wiki_page_without_drg_passes_p_not_contract_and_h2():
    req = _sample_req(
        req_id="FR-01",
        source_file="Game-Requirements/requirements/01-Project-Overview.md",
        lifecycle="shipped",
        shipped_evidence="Verified in CI",
    )
    wiki_baseline = {
        "id": "wiki-baseline-1",
        "surface": "cmano-clone / Shipped Baseline",
        "status": "Done",
        "note": "Source: Game-Requirements/requirements/01-Project-Overview.md FR-01 shipped_verified",
    }
    checks_ok = bge.evaluate_requirement_deterministic(
        req, [], [wiki_baseline], repo_root=ROOT
    )
    assert checks_ok["P_NOT_EXISTS"] is True
    assert checks_ok["P_NOT_CONTRACT"] is True
    assert checks_ok["notion_honest_state"] is True
    assert checks_ok["notion_has_linear_link"] is True

    frozen_mirror = {
        "id": "wiki-frozen-1",
        "surface": "cmano-clone / Requirements",
        "status": "Done",
        "note": "Source: Game-Requirements/requirements/01-Project-Overview.md FR-01",
    }
    checks_frozen = bge.evaluate_requirement_deterministic(
        req, [], [frozen_mirror], repo_root=ROOT
    )
    assert checks_frozen["P_NOT_EXISTS"] is False
    assert checks_frozen["notion_only_frozen_mirror"] is True
    assert checks_frozen["P_NOT_CONTRACT"] is False


# 11. M4-T4 Iteration 2 state fidelity & surface/ label regression tests
def test_build_compact_state_targets_slice_strips_xml_forwards_icon_and_content():
    questions = bge.load_questions(RUBRIC_PATH)
    req = _sample_req(
        req_id="AGD-05",
        source_file="Game-Requirements/requirements/04-Agent-Delegation.md",
        lifecycle_state="active",
    )
    long_linear = {
        "id": "DRG-866",
        "title": "[AGD-05] Agent Assignment UI",
        "state": "Backlog",
        "project": "cmano-clone",
        "labels": ["type:feature"],
        "url": "https://linear.app/drgamtd-workspace/issue/DRG-866",
        "description": (
            'Source: `Game-Requirements/requirements/04-Agent-Delegation.md`\n'
            'Req IDs: AGD-01, AGD-05\n'
            'Related: <issue id="01143645" label="DRG-147" href="https://linear.app/drgamtd-workspace/issue/DRG-147"/> '
            '<pull-request id="p1" label="drgaciw/cmano-clone#704" href="https://linear.app/x"/>\n'
            "Notion: https://www.notion.so/421f7cb4e4df813ea814eac170d83536\n\n"
            + ("### AGD-01 Boilerplate\n" + ("Filler text. " * 80) + "\n\n")
            + "### AGD-05 Agent Assignment UI\n"
            + "Acceptance Criteria:\n- [ ] Assign Agent badge renders and edits at runtime.\n"
        ),
    }
    notion_page = {
        "id": "421f7cb4-e4df-813e-a814-eac170d83536",
        "title": "DRG-866 [AGD-05] Agent Assignment UI",
        "surface": "Tasks / Project Aegis",
        "status": "Not started",
        "project": "Project Aegis",
        "icon": "📋",
        "links": ["http://04-Agent-Delegation.md", "https://linear.app/drgamtd-workspace/issue/DRG-866"],
        "note": "Linear: https://linear.app/drgamtd-workspace/issue/DRG-866 | Source: [04-Agent-Delegation.md](http://04-Agent-Delegation.md) | Reqs: AGD-01..06",
        "content": (
            "## Requirement Summary\n"
            "### AGD-05\n"
            "Acceptance Criteria:\n- [ ] Assign Agent badge renders and edits at runtime.\n"
        ),
    }
    checks = bge.evaluate_requirement_deterministic(
        req, [long_linear], [notion_page], repo_root=ROOT
    )
    assert checks["P_LIN_CONTRACT"] is True
    assert checks["P_NOT_CONTRACT"] is True
    assert checks["notion_cites_source_or_id"] is True

    compact = json.loads(
        bge.build_compact_state(req, [long_linear], [notion_page], checks, questions=questions)
    )
    lin_ex = compact["linear_matches"][0]
    assert "<issue " not in lin_ex["description_excerpt"]
    assert "<pull-request " not in lin_ex["description_excerpt"]
    assert "[DRG-147]" in lin_ex["description_excerpt"]
    assert "### AGD-05" in lin_ex["description_excerpt"]
    assert "Assign Agent badge renders" in lin_ex["description_excerpt"]
    assert lin_ex["covers_target_req_id"] is True

    not_ex = compact["notion_matches"][0]
    assert not_ex["icon"] == "📋"
    assert not_ex["has_icon"] is True
    assert "http://04-Agent-Delegation.md" not in not_ex["note_excerpt"]
    assert "http://04-Agent-Delegation.md" not in not_ex["links"]
    assert "Assign Agent badge renders" in not_ex["content_excerpt"]
    assert not_ex["covers_target_req_id"] is True


def test_evaluate_requirement_deterministic_flags_surface_slash_on_backlog_todo_only():
    req = _sample_req(
        req_id="MODE-02",
        source_file="Game-Requirements/requirements/03-Simulation-Modes.md",
        lifecycle_state="active",
    )
    backlog_slash = {
        **_complete_linear_match(
            req_id="MODE-02",
            source_file="Game-Requirements/requirements/03-Simulation-Modes.md",
        ),
        "state": "Backlog",
        "labels": ["surface/c2-presentation"],
    }
    todo_slash = {**backlog_slash, "state": "Todo"}
    in_progress_slash = {**backlog_slash, "state": "In Progress"}
    notion = _complete_notion_match(
        req_id="MODE-02",
        source_file="Game-Requirements/requirements/03-Simulation-Modes.md",
    )

    checks_backlog = bge.evaluate_requirement_deterministic(
        req, [backlog_slash], [notion], repo_root=ROOT
    )
    assert checks_backlog["linear_no_surface_line"] is False
    assert checks_backlog["P_LIN_CONTRACT"] is False

    checks_todo = bge.evaluate_requirement_deterministic(
        req, [todo_slash], [notion], repo_root=ROOT
    )
    assert checks_todo["linear_no_surface_line"] is False
    assert checks_todo["P_LIN_CONTRACT"] is False

    checks_wip = bge.evaluate_requirement_deterministic(
        req, [in_progress_slash], [notion], repo_root=ROOT
    )
    assert checks_wip["linear_no_surface_line"] is True
    assert checks_wip["P_LIN_CONTRACT"] is True


def test_extract_targeted_slice_omits_other_req_ac_preamble():
    desc = (
        "Source: `Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md`\n"
        "Req IDs: RPL-22, RPL-24, RPL-25\n"
        "Lifecycle: Active\n"
        "Notion: https://www.notion.so/421f7cb4e4df81cfb265ff477fb909cb\n"
        "Acceptance Criteria:\n\n"
        "- [ ] `RPL-22`: " + ("First requirement filler text " * 60) + "\n"
        "- [ ] `RPL-24`: Cut-off preamble check.\n\n"
        "### RPL-24 — AAR suggested operational and policy improvements\n\n"
        "Recommend actionable mission timing adjustments.\n\n"
        "- [ ] [RPL-24] AAR outputs grounded suggestions referencing observed mission timing delays.\n"
    )
    sliced = bge._extract_targeted_slice(desc, "RPL-24", "17-Replay-AAR-And-Order-Log.md", max_chars=1600)
    assert "Req IDs: RPL-22, RPL-24, RPL-25" in sliced
    assert "[Targeted slice for RPL-24]" in sliced
    assert "### RPL-24" in sliced
    assert "AAR outputs grounded suggestions" in sliced
    assert "First requirement filler text" not in sliced


def test_extract_targeted_slice_preserves_long_req_ids_and_lifecycle_header():
    req_list = ", ".join(f"SPEC-{i:02d}" for i in range(3, 27))
    content = (
        "**Linear:** [DRG-117](https://linear.app/drgamtd-workspace/issue/DRG-117/phase-n-req-10) (Backlog)\n"
        "**Sources:** `Game-Requirements/requirements/10-Speculative-Systems.md`\n"
        f"**Req IDs:** {req_list}\n"
        "**Lifecycle:** [Phase N] Deferred — do not start\n\n"
        + ("### SPEC-03 — Filler section\n\nFiller body.\n\n" * 12)
        + "### SPEC-19 — [Phase N] Brain-Computer Interface (BCI) command\n\n"
        "Phase N BCI command deferred.\n\n"
        "- [ ] [Phase N / DRG-117] Deferred post-release: BCI command deferred to Phase N.\n"
    )
    sliced = bge._extract_targeted_slice(content, "SPEC-19", "10-Speculative-Systems.md", max_chars=1500)
    assert f"**Req IDs:** {req_list}" in sliced
    assert "**Lifecycle:** [Phase N] Deferred" in sliced
    assert "### SPEC-19" in sliced
    assert "BCI command deferred to Phase N" in sliced
