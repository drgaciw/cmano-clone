"""Tier 2: Boundary & corner-case tests (43 test functions across F1-F7, offline hermetic)."""

from __future__ import annotations

import json
import sys
import urllib.error
import urllib.request
from pathlib import Path

import pytest

from harness.cli_adapter import (
    IMPL_EVIDENCE_DIR,
    IMPL_QUESTIONS_PACK,
    NotionViewAdapter,
    assert_impl_deliverables_present,
    post_jev_json,
)
from harness.contracts import (
    IN_SCOPE_GIT_FILES,
    MAX_BODY_BYTES,
    NOTION_FROZEN_MIRROR_PAGE_ID,
    NOTION_PROJECT_AEGIS_ID,
    NOTION_TASKS_VIEW,
    build_independent_jev_state,
    evaluate_requirement_record,
    is_cpq_linear_issue,
    is_excluded_path,
    parse_jev_primitives,
    scan_for_secrets_and_db3,
    validate_cross_links,
    validate_gap_matrix,
    validate_jev_request,
    validate_linear_issue_payload,
    validate_notion_task_row,
    validate_question_pack,
    validate_remediation_log,
    validate_requirements_catalog,
    validate_scope_decisions,
)

REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "tools" / "jev"))

from classify_uat import ApiRoute, UatEvidencePacket, UatRow, classify_or_degrade  # noqa: E402

pytestmark = pytest.mark.tier2


# ===========================================================================
# F1 Boundaries (7 tests)
# ===========================================================================


def test_f1_boundary_payload_32768_vs_32769_bytes(mock_jev_server):
    """T2-F1-01: Exact 32,768 UTF-8 byte payload succeeds; 32,769 bytes is rejected."""
    base = {
        "model": "typesafe/jev-1.14",
        "state": {"note": ""},
        "questions": {
            "q1": {
                "type": "noul",
                "instructions": "Is the state valid?",
                "criteria": {"true": "Yes", "false": "No"},
            }
        },
    }
    overhead = len(json.dumps(base, ensure_ascii=False).encode("utf-8"))
    # Include multi-byte UTF-8 symbol '📌' (4 bytes in UTF-8)
    pad_exact = MAX_BODY_BYTES - overhead - 4
    exact_payload = {**base, "state": {"note": "📌" + ("a" * pad_exact)}}
    assert len(json.dumps(exact_payload, ensure_ascii=False).encode("utf-8")) == MAX_BODY_BYTES
    validate_jev_request(exact_payload).require_ok()
    resp_ok = post_jev_json(mock_jev_server.decisions_url, exact_payload)
    assert resp_ok["status_code"] == 200

    over_payload = {**base, "state": {"note": "📌" + ("a" * (pad_exact + 1))}}
    assert len(json.dumps(over_payload, ensure_ascii=False).encode("utf-8")) == MAX_BODY_BYTES + 1
    assert not validate_jev_request(over_payload).ok

    # Verify mock HTTP endpoint itself also enforces 413 on raw 32,769-byte POST
    req = urllib.request.Request(
        mock_jev_server.decisions_url,
        data=json.dumps(over_payload, ensure_ascii=False).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with pytest.raises(urllib.error.HTTPError) as exc_info:
        urllib.request.urlopen(req, timeout=2.0)
    assert exc_info.value.code == 413


def test_f1_boundary_questions_count_0_20_21(mock_jev_server):
    """T2-F1-02: 0 and 21 questions fail validation; exactly 20 questions passes."""
    q_tpl = {
        "type": "noul",
        "instructions": "Check item.",
        "criteria": {"true": "Ok", "false": "Gap"},
    }
    zero_q = {"model": "typesafe/jev-1.14", "state": "s", "questions": {}}
    twenty_q = {
        "model": "typesafe/jev-1.14",
        "state": "s",
        "questions": {f"q_{i:02d}": q_tpl for i in range(20)},
    }
    twenty_one_q = {
        "model": "typesafe/jev-1.14",
        "state": "s",
        "questions": {f"q_{i:02d}": q_tpl for i in range(21)},
    }
    assert not validate_jev_request(zero_q).ok
    validate_jev_request(twenty_q).require_ok()
    assert post_jev_json(mock_jev_server.decisions_url, twenty_q)["status_code"] == 200
    assert not validate_jev_request(twenty_one_q).ok


def test_f1_boundary_key_pattern_40_chars_and_hyphens():
    """T2-F1-03: 40-char [a-z0-9_] passes; 41-char, uppercase, hyphenated, and empty fail."""
    q_tpl = {"type": "noul", "instructions": "Valid.", "criteria": {"true": "Y", "false": "N"}}
    valid_40 = {"state": "s", "questions": {"a" * 40: q_tpl}}
    validate_jev_request(valid_40).require_ok()

    for bad_key in ("a" * 41, "Req1", "scenario-content", ""):
        bad = {"state": "s", "questions": {bad_key: q_tpl}}
        assert not validate_jev_request(bad).ok, f"Expected rejection for key {bad_key!r}"


def test_f1_boundary_instructions_600_and_criteria_bounds():
    """T2-F1-04: Exact bounds (600 chars, 2..255 choice, 2..10 score, 400 chars) enforced."""
    ok_choice_255 = {
        "state": "s",
        "questions": {
            "q_choice": {
                "type": "choice",
                "instructions": "x" * 600,
                "criteria": {f"opt_{i}": "valid" for i in range(255)},
            }
        },
    }
    validate_jev_request(ok_choice_255).require_ok()

    ok_score_400 = {
        "state": "s",
        "questions": {
            "q_score": {
                "type": "score",
                "instructions": "x" * 600,
                "criteria": ["z" * 400 for _ in range(10)],
            },
            "q_choice_len": {
                "type": "choice",
                "instructions": "Pick one.",
                "criteria": {"opt_a": "y" * 400, "opt_b": "y" * 400},
            },
        },
    }
    validate_jev_request(ok_score_400).require_ok()

    assert not validate_jev_request(
        {"state": "s", "questions": {"q": {"type": "noul", "instructions": "x" * 601}}}
    ).ok
    assert not validate_jev_request(
        {
            "state": "s",
            "questions": {"q": {"type": "choice", "instructions": "i", "criteria": {"only_one": "1"}}},
        }
    ).ok
    assert not validate_jev_request(
        {
            "state": "s",
            "questions": {
                "q": {
                    "type": "choice",
                    "instructions": "i",
                    "criteria": {f"k_{i}": "v" for i in range(256)},
                }
            },
        }
    ).ok
    assert not validate_jev_request(
        {
            "state": "s",
            "questions": {"q": {"type": "score", "instructions": "i", "criteria": ["only_one"]}},
        }
    ).ok
    assert not validate_jev_request(
        {
            "state": "s",
            "questions": {
                "q": {"type": "score", "instructions": "i", "criteria": [f"l{i}" for i in range(11)]}
            },
        }
    ).ok
    assert not validate_jev_request(
        {
            "state": "s",
            "questions": {
                "q": {"type": "score", "instructions": "i", "criteria": ["ok", "w" * 401]}
            },
        }
    ).ok
    assert not validate_jev_request(
        {
            "state": "s",
            "questions": {
                "q": {
                    "type": "choice",
                    "instructions": "i",
                    "criteria": {"opt_a": "ok", "opt_b": "y" * 401},
                }
            },
        }
    ).ok


@pytest.mark.parametrize("fault_mode", ["slow_drip", "http_502", "malformed_json", "missing_answers"])
def test_f1_fault_injection_timeout_http502_malformed_json(mock_jev_server, fault_mode: str):
    """T2-F1-05: Timeout, HTTP 502, malformed JSON, and missing answers degrade cleanly."""
    mock_jev_server.mode = fault_mode
    mock_jev_server.slow_drip_delay_s = 0.05
    mock_jev_server.slow_drip_chunks = 20
    packet = UatEvidencePacket("baltic-patrol", [UatRow("R1", "pass", "ok")])
    questions = {
        "q1": {
            "type": "noul",
            "instructions": "Check.",
            "criteria": {"true": "Y", "false": "N"},
        }
    }
    route = ApiRoute("openrouter", mock_jev_server.systemone_url, "typesafe/jev-1.13", "dummy-key")
    result = classify_or_degrade(packet, questions, route=route, timeout_ms=250)
    assert result["status"] == "no_advisory"
    assert result["answers"] is None


@pytest.mark.parametrize("bad_state", [[], None, 123, True])
def test_f1_state_type_rejects_array_or_null(bad_state):
    """T2-F1-06: Non-string, non-object state values are rejected."""
    payload = {
        "state": bad_state,
        "questions": {"q1": {"type": "noul", "instructions": "Check."}},
    }
    assert not validate_jev_request(payload).ok


def test_f1_scope_decisions_batched_format_valid_and_missing_answer_rejected(
    load_fixture_json, require_impl_mode: bool
):
    """T2-F1-07: Batched & synthetic decide formats reject missing answers, bad choice keys, bool confidence, or secrets."""
    valid_batched = {
        "generated_at": "2026-11-22T22:59:37Z",
        "purpose": "M1 open-scope calibration",
        "decisions": [
            {
                "status": "ok",
                "route_used": "openrouter_alpha_decisions",
                "request": {
                    "model": "typesafe/jev-1.14",
                    "state": "M1 calibration context",
                    "questions": {
                        "notion_target_surface": {
                            "type": "choice",
                            "instructions": "Which Notion surface?",
                            "criteria": {
                                "tasks_db_plus_wiki_drafts": "Tasks DB and wiki drafts.",
                                "other": "Escalate.",
                            },
                        },
                        "notion_frozen_mirror_respect": {
                            "type": "noul",
                            "instructions": "Keep Frozen Git mirror read-only?",
                            "criteria": {"true": "Read-only", "false": "Editable"},
                        },
                        "backlog_rigor_level": {
                            "type": "score",
                            "instructions": "Score rigor tier.",
                            "criteria": ["minimal", "balanced", "full"],
                        },
                    },
                },
                "response": {
                    "id": "gen-dec-batched-1",
                    "model": "typesafe/jev-1.14-20261014",
                    "answers": {
                        "notion_target_surface": {
                            "type": "choice",
                            "choice": "tasks_db_plus_wiki_drafts",
                            "confidence": 0.99,
                        },
                        "notion_frozen_mirror_respect": {
                            "type": "noul",
                            "noul": 0.94,
                        },
                        "backlog_rigor_level": {
                            "type": "score",
                            "score": 1.01,
                        },
                    },
                    "usage": {"input_tokens": 100, "output_tokens": 50},
                },
            }
        ],
    }
    validate_scope_decisions(valid_batched).require_ok()

    # Negative 1: missing answer for one question in request["questions"]
    missing_answer = json.loads(json.dumps(valid_batched))
    del missing_answer["decisions"][0]["response"]["answers"]["backlog_rigor_level"]
    res_miss_ans = validate_scope_decisions(missing_answer)
    assert not res_miss_ans.ok
    assert any("missing questions" in e for e in res_miss_ans.errors)

    # Negative 2: missing response id
    missing_id = json.loads(json.dumps(valid_batched))
    del missing_id["decisions"][0]["response"]["id"]
    res_miss_id = validate_scope_decisions(missing_id)
    assert not res_miss_id.ok
    assert any("response missing 'id'" in e for e in res_miss_id.errors)

    # Negative 3: non-ok status
    bad_status = json.loads(json.dumps(valid_batched))
    bad_status["decisions"][0]["status"] = "blocked"
    assert not validate_scope_decisions(bad_status).ok

    # Negative 4: secret leakage inside batched format
    tainted_secret = json.loads(json.dumps(valid_batched))
    tainted_secret["decisions"][0]["request"]["state"] = "leak sk-or-v1-canarytestsecret99"
    res_secret = validate_scope_decisions(tainted_secret)
    assert not res_secret.ok
    assert any("openrouter_key" in e for e in res_secret.errors)

    # Negative 5: invalid choice key not in criteria (batched format)
    bad_choice_batched = json.loads(json.dumps(valid_batched))
    bad_choice_batched["decisions"][0]["response"]["answers"]["notion_target_surface"]["choice"] = (
        "INVALID_CHOICE_KEY_NOT_IN_CRITERIA"
    )
    res_bad_choice_batched = validate_scope_decisions(bad_choice_batched)
    assert not res_bad_choice_batched.ok
    assert any("not in criteria" in e for e in res_bad_choice_batched.errors)

    # Negative 6: invalid choice key not in criteria (synthetic format)
    bad_choice_synth = load_fixture_json("artifacts/scope-decisions-jev.json")
    bad_choice_synth["decisions"][0]["jev_response"]["answers"]["notion_target"]["choice"] = (
        "INVALID_CHOICE_KEY_NOT_IN_CRITERIA"
    )
    res_bad_choice_synth = validate_scope_decisions(bad_choice_synth)
    assert not res_bad_choice_synth.ok
    assert any("not in criteria" in e for e in res_bad_choice_synth.errors)

    # Negative 7: boolean confidence/score rejected
    bool_conf_batched = json.loads(json.dumps(valid_batched))
    bool_conf_batched["decisions"][0]["response"]["answers"]["notion_target_surface"]["confidence"] = True
    res_bool_conf = validate_scope_decisions(bool_conf_batched)
    assert not res_bool_conf.ok
    assert any("confidence must be numeric" in e for e in res_bool_conf.errors)

    bool_score_batched = json.loads(json.dumps(valid_batched))
    bool_score_batched["decisions"][0]["response"]["answers"]["backlog_rigor_level"]["score"] = True
    res_bool_score = validate_scope_decisions(bool_score_batched)
    assert not res_bool_score.ok
    assert any("score must be numeric" in e for e in res_bool_score.errors)

    # Negative 8: non-finite and out-of-bounds score values rejected
    questions_3 = valid_batched["decisions"][0]["request"]["questions"]
    for bad_score in (float("inf"), float("-inf"), float("nan"), -0.5, 99.0):
        with pytest.raises(ValueError, match="score out of"):
            parse_jev_primitives(
                {"answers": {"backlog_rigor_level": {"score": bad_score}}},
                questions_3,
            )
    for oob_score in (-0.5, 99.0):
        oob_batched = json.loads(json.dumps(valid_batched))
        oob_batched["decisions"][0]["response"]["answers"]["backlog_rigor_level"]["score"] = oob_score
        res_oob = validate_scope_decisions(oob_batched)
        assert not res_oob.ok
        assert any("score out of" in e for e in res_oob.errors)

    if require_impl_mode:
        real_scope = json.loads(
            (IMPL_EVIDENCE_DIR / "scope-decisions-jev.json").read_text(encoding="utf-8")
        )
        validate_scope_decisions(real_scope).require_ok()


# ===========================================================================
# F2 Boundaries (5 tests)
# ===========================================================================


def test_f2_boundary_noul_0799_vs_0800(load_fixture_json):
    """T2-F2-01: noul=0.799 fails pass gate; noul=0.800 passes."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    req = {
        "req_id": "FR-02",
        "source_file": "Game-Requirements/requirements/02-Core-Gameplay-Loop.md",
        "lifecycle_state": "active",
    }
    lin = [{"id": "DRG-102", "source_path": req["source_file"], "acceptance_criteria": ["AC"]}]
    notn = [
        {
            "id": "n-102",
            "has_icon": True,
            "project_id": NOTION_PROJECT_AEGIS_ID,
            "status": "Not started",
            "note": "https://linear.app/drgamtd-workspace/issue/DRG-102",
        }
    ]
    ev_fail = evaluate_requirement_record(
        req=req,
        linear_matches=lin,
        notion_matches=notn,
        jev_response={"id": "d1", "answers": {"backlog_completeness": {"noul": 0.799}}},
        questions=pack["questions"],
    )
    assert ev_fail["linear_status"] == "incomplete"

    ev_pass = evaluate_requirement_record(
        req=req,
        linear_matches=lin,
        notion_matches=notn,
        jev_response={"id": "d2", "answers": {"backlog_completeness": {"noul": 0.800}}},
        questions=pack["questions"],
    )
    assert ev_pass["linear_status"] == "complete"
    assert ev_pass["notion_status"] == "complete"


def test_f2_boundary_choice_confidence_0499_vs_0500(load_fixture_json):
    """T2-F2-02: choice confidence 0.499 triggers human review; 0.500 does not."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    parsed_low = parse_jev_primitives(
        {"answers": {"gap_classification": {"choice": "complete", "confidence": 0.499}}},
        pack["questions"],
    )
    assert parsed_low["gap_classification"]["needs_human_review"] is True

    parsed_ok = parse_jev_primitives(
        {"answers": {"gap_classification": {"choice": "complete", "confidence": 0.500}}},
        pack["questions"],
    )
    assert parsed_ok["gap_classification"]["needs_human_review"] is False


def test_f2_choice_missing_none_or_other_escape_rejected(
    load_fixture_json, require_impl_mode: bool
):
    """T2-F2-03: Question pack omitting both 'none' and 'other' escape options is rejected."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    del pack["questions"]["gap_classification"]["criteria"]["none"]
    del pack["questions"]["gap_classification"]["criteria"]["other"]
    res = validate_question_pack(pack)
    assert not res.ok
    assert any("escape" in e for e in res.errors)

    if require_impl_mode:
        real_pack = json.loads(IMPL_QUESTIONS_PACK.read_text(encoding="utf-8"))
        validate_question_pack(real_pack).require_ok()


def test_f2_noul_does_not_apply_choice_confidence_floor(load_fixture_json):
    """T2-F2-04: noul probability has no separate confidence field and applies 0.80 floor."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    parsed = parse_jev_primitives(
        {"answers": {"backlog_completeness": {"noul": 0.45}}},
        pack["questions"],
    )
    assert parsed["backlog_completeness"]["noul"] == pytest.approx(0.45)
    assert parsed["backlog_completeness"]["passes"] is False


def test_f2_score_non_linear_interpolated_rank_handling(load_fixture_json):
    """T2-F2-05: Score 1.49 vs 1.50 maps cleanly to discrete rank thresholds (1 vs 2)."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    p_low = parse_jev_primitives(
        {"answers": {"remediation_priority": {"score": 1.49}}},
        pack["questions"],
    )
    p_high = parse_jev_primitives(
        {"answers": {"remediation_priority": {"score": 1.50}}},
        pack["questions"],
    )
    assert p_low["remediation_priority"]["discrete_rank"] == 1
    assert p_high["remediation_priority"]["discrete_rank"] == 2


# ===========================================================================
# F3 Boundaries (5 tests)
# ===========================================================================


def test_f3_cpq_and_non_requirement_filename_decoys():
    """T2-F3-01: pricing-lifecycle-management.md (no 'cpq' in name) & cmano-db are excluded."""
    for excluded in (
        "tests/uat/cpq-product-management-lifecycle.md",
        "tests/uat/pricing-lifecycle-management.md",
        "tests/uat/README.md",
        "docs/reference/cmano-db/ship.md",
    ):
        assert is_excluded_path(excluded) is True


def test_f3_unicode_and_multi_req_range_citations(tmp_path: Path):
    """T2-F3-02: UTF-8 symbols, clause ranges, and single-line (42-42) citations validate."""
    rel = "Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md"
    f = tmp_path / rel
    f.parent.mkdir(parents=True, exist_ok=True)
    f.write_text("\n".join([f"Line {i} — 📌 ⚔️" for i in range(1, 51)]), encoding="utf-8")
    catalog = {
        "generated_at": "2026-11-22T23:00:00Z",
        "source_files": [{"path": rel, "category": "A", "lines": 50, "in_scope": True}],
        "requirements": [
            {
                "req_id": "RPL-29..31",
                "title": "Replay determinism — UTF-8 📌",
                "source_file": rel,
                "source_lines": "42-42",
                "domain_project": "cmano-clone",
                "lifecycle_state": "active",
                "summary": "Multi-clause range citation with single-line anchor.",
                "acceptance_criteria": ["Replay preserves deterministic order log."],
            }
        ],
    }
    validate_requirements_catalog(catalog, repo_root=tmp_path, require_full_corpus=False).require_ok()


def test_f3_duplicate_req_id_across_files_detected(load_fixture_json):
    """T2-F3-03: Duplicate req_id entries without unique sub-IDs are rejected."""
    catalog = load_fixture_json("artifacts/requirements-catalog.json")
    dup = json.loads(json.dumps(catalog["requirements"][0]))
    dup["source_file"] = "Game-Requirements/drafts/23-Kill-Chain-Explainability.md"
    catalog["requirements"].append(dup)
    res = validate_requirements_catalog(catalog, require_full_corpus=False)
    assert not res.ok
    assert any("duplicate req_id" in e for e in res.errors)


def test_f3_empty_or_out_of_bounds_source_lines_rejected(repo_root: Path):
    """T2-F3-04: Out-of-bounds line numbers (>EOF) and empty AC arrays are rejected."""
    rel = "Game-Requirements/requirements/01-Project-Overview.md"
    bad_lines = {
        "generated_at": "2026-11-22T23:00:00Z",
        "source_files": [{"path": rel, "category": "A", "lines": 10, "in_scope": True}],
        "requirements": [
            {
                "req_id": "FR-BAD",
                "title": "Bad range",
                "source_file": rel,
                "source_lines": "99999-100010",
                "domain_project": "cmano-clone",
                "lifecycle_state": "active",
                "summary": "Beyond EOF.",
                "acceptance_criteria": [],
            }
        ],
    }
    res = validate_requirements_catalog(bad_lines, repo_root=repo_root, require_full_corpus=False)
    assert not res.ok
    assert any("acceptance_criteria" in e for e in res.errors)
    assert any("exceeds EOF" in e for e in res.errors)


def test_f3_notion_only_requirement_has_valid_virtual_source(load_fixture_json):
    """T2-F3-05: NotionOnly entry validates cleanly without requiring a local .md file."""
    catalog = load_fixture_json("artifacts/requirements-catalog.json")
    notion_reqs = [r for r in catalog["requirements"] if r["req_id"] == "ENV-01"]
    assert len(notion_reqs) == 1
    notion_only_catalog = {
        "generated_at": catalog["generated_at"],
        "source_files": [sf for sf in catalog["source_files"] if sf["category"] == "NotionOnly"],
        "requirements": notion_reqs,
    }
    validate_requirements_catalog(
        notion_only_catalog, repo_root=REPO_ROOT, require_full_corpus=False
    ).require_ok()


# ===========================================================================
# F4 Boundaries (6 tests)
# ===========================================================================


def test_f4_empty_requirements_or_zero_matches_edge():
    """T2-F4-01: Empty evaluations fail; 1 unmatched requirement evaluates to missing/missing."""
    empty_matrix = {
        "evaluated_at": "2026-11-22T23:00:00Z",
        "rubric_version": "v1",
        "jev_model": "typesafe/jev-1.14",
        "summary": {
            "total_requirements": 0,
            "source_files_covered": 0,
            "linear": {"complete": 0, "incomplete": 0, "missing": 0},
            "notion": {"complete": 0, "incomplete": 0, "missing": 0},
        },
        "evaluations": [],
    }
    assert not validate_gap_matrix(empty_matrix).ok

    ev = evaluate_requirement_record(
        req={"req_id": "FR-NEW", "source_file": "Game-Requirements/requirements/01-Project-Overview.md"},
        linear_matches=[],
        notion_matches=[],
        jev_response=None,
    )
    assert ev["linear_status"] == "missing"
    assert ev["notion_status"] == "missing"
    assert ev["recommended_action"] == "both"


def test_f4_summary_count_mismatch_rejected(load_fixture_json):
    """T2-F4-02: Matrix summary disagreement with evaluations[] fails validation."""
    matrix = load_fixture_json("artifacts/baseline-gap-matrix.json")
    matrix["summary"]["linear"]["missing"] += 1
    res = validate_gap_matrix(matrix)
    assert not res.ok
    assert any("summary.linear" in e for e in res.errors)


def test_f4_phase_n_deferred_handling_consistency(load_fixture_json):
    """T2-F4-03: Tracked Phase N item stays complete when tracked; absent Phase N is missing."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    req = {
        "req_id": "SWARM-27",
        "source_file": "Game-Requirements/requirements/22-Drone-Swarm-Platforms.md",
        "lifecycle_state": "deferred_phase_n",
    }
    tracked = evaluate_requirement_record(
        req=req,
        linear_matches=[{"id": "DRG-112", "source_path": req["source_file"], "acceptance_criteria": ["DRG-47"]}],
        notion_matches=[
            {
                "id": "n-112",
                "has_icon": True,
                "project_id": NOTION_PROJECT_AEGIS_ID,
                "status": "Not started",
                "note": "https://linear.app/drgamtd-workspace/issue/DRG-112",
            }
        ],
        jev_response={"id": "d-pn", "answers": {"backlog_completeness": {"noul": 0.88}}},
        questions=pack["questions"],
    )
    assert tracked["linear_status"] == "complete"

    absent = evaluate_requirement_record(
        req=req, linear_matches=[], notion_matches=[], jev_response=None
    )
    assert absent["linear_status"] == "missing"
    assert absent["notion_status"] == "missing"


def test_f4_phase_n_untracked_policy_and_choice_missing(load_fixture_json):
    """T2-F4-06: phase_n_tracked_complete=False and choice='missing' downgrade complete records."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    phase_n_req = {
        "req_id": "SWARM-27",
        "source_file": "Game-Requirements/requirements/22-Drone-Swarm-Platforms.md",
        "lifecycle_state": "deferred_phase_n",
    }
    lin = [{"id": "DRG-112", "source_path": phase_n_req["source_file"], "acceptance_criteria": ["DRG-47"]}]
    notn = [
        {
            "id": "n-112",
            "has_icon": True,
            "project_id": NOTION_PROJECT_AEGIS_ID,
            "status": "Not started",
            "note": "https://linear.app/drgamtd-workspace/issue/DRG-112",
        }
    ]
    tracked_true = evaluate_requirement_record(
        req=phase_n_req,
        linear_matches=lin,
        notion_matches=notn,
        jev_response={"id": "d-pn-t", "answers": {"backlog_completeness": {"noul": 0.90}}},
        questions=pack["questions"],
        phase_n_tracked_complete=True,
    )
    assert tracked_true["linear_status"] == "complete"
    assert tracked_true["notion_status"] == "complete"

    tracked_false = evaluate_requirement_record(
        req=phase_n_req,
        linear_matches=lin,
        notion_matches=notn,
        jev_response={"id": "d-pn-f", "answers": {"backlog_completeness": {"noul": 0.90}}},
        questions=pack["questions"],
        phase_n_tracked_complete=False,
    )
    assert tracked_false["linear_status"] == "incomplete"
    assert tracked_false["notion_status"] == "incomplete"

    active_req = {**phase_n_req, "lifecycle_state": "active"}
    choice_missing = evaluate_requirement_record(
        req=active_req,
        linear_matches=lin,
        notion_matches=notn,
        jev_response={
            "id": "d-cm",
            "answers": {
                "backlog_completeness": {"noul": 0.90},
                "gap_classification": {"choice": "missing", "confidence": 0.90},
            },
        },
        questions=pack["questions"],
    )
    assert choice_missing["linear_status"] == "incomplete"
    assert choice_missing["notion_status"] == "incomplete"

    # Script-first priority: empty mechanical matches stay 'missing' even with choice='missing'
    unmatched_choice_missing = evaluate_requirement_record(
        req=active_req,
        linear_matches=[],
        notion_matches=[],
        jev_response={
            "id": "d-cm2",
            "answers": {"gap_classification": {"choice": "missing", "confidence": 0.90}},
        },
        questions=pack["questions"],
    )
    assert unmatched_choice_missing["linear_status"] == "missing"
    assert unmatched_choice_missing["notion_status"] == "missing"


def test_f4_fuzzy_title_collision_does_not_false_match_cpq(load_fixture_json):
    """T2-F4-04: P-DRG-6 / CPQ issue with colliding keywords is rejected from inventory."""
    dump = load_fixture_json("backlogs/synthetic-linear-dump.json")
    cpq_issue = next(i for i in dump["issues"] if i["id"] == "DRG-900")
    assert cpq_issue["title"] == "Quote / Proposal Approval"
    assert is_cpq_linear_issue(cpq_issue) is True


def test_f4_notion_quota_error_triggers_view_fallback():
    """T2-F4-05: usage_limit_reached on sql mode cleanly falls back to mode='view'."""
    def responder(mode: str, cursor: str | None):
        if mode == "sql":
            return {"error": "usage_limit_reached", "status_code": 429}
        return {"results": [{"id": "task-1"}], "has_more": False, "next_cursor": None}

    adapter = NotionViewAdapter(responder, min_interval_s=0.0, sleep_fn=lambda _: None)
    rows = adapter.query_with_fallback(view_url=NOTION_TASKS_VIEW, try_sql_first=True)
    assert rows == [{"id": "task-1"}]
    assert adapter.trace.modes_attempted == ["sql", "view"]


# ===========================================================================
# F5 Boundaries (6 tests)
# ===========================================================================


def test_f5_rejects_surface_line_in_new_backlog_description():
    """T2-F5-01: New Backlog issue with a 'Surface:' line violates Rubrics §2(c)."""
    bad = {
        "id": "DRG-820",
        "project": "P-DRG-1",
        "state": "Backlog",
        "labels": ["type:feature"],
        "description": "Surface: C2\nSource: `Game-Requirements/requirements/20-Command-And-Control-UI.md`",
    }
    res = validate_linear_issue_payload(bad, is_new_gap=True, repo_root=REPO_ROOT)
    assert not res.ok
    assert any("Surface:" in e for e in res.errors)


def test_f5_rejects_dropping_hold_owner_go_or_needs_owner():
    """T2-F5-02: Removing hold:owner-go, NEEDS_OWNER, or single-owner fails validation."""
    prior = {
        "id": "DRG-268",
        "project": "P-DRG-1",
        "state": "Backlog",
        "labels": ["hold:owner-go", "NEEDS_OWNER", "single-owner"],
    }
    updated = {
        "id": "DRG-268",
        "project": "P-DRG-1",
        "state": "Backlog",
        "labels": ["type:feature"],
        "description": "Source: `Game-Requirements/requirements/20-Command-And-Control-UI.md`",
    }
    res = validate_linear_issue_payload(updated, prior_issue=prior, repo_root=REPO_ROOT)
    assert not res.ok
    assert any("protected labels removed" in e for e in res.errors)


def test_f5_rejects_dead_git_source_path_in_issue():
    """T2-F5-03: Dead Git source path in Linear description fails disk resolution check."""
    bad = {
        "id": "DRG-821",
        "project": "P-DRG-1",
        "state": "Backlog",
        "labels": ["type:feature"],
        "description": "Git source of truth: `Game-Requirements/requirements/99-NonExistent.md`",
    }
    res = validate_linear_issue_payload(bad, is_new_gap=True, repo_root=REPO_ROOT)
    assert not res.ok
    assert any("does not exist on disk" in e for e in res.errors)


@pytest.mark.parametrize("bad_label", ["Open-CRM-CPQ", "CPQ Phase 1", "agent:cmano"])
def test_f5_rejects_forbidden_cpq_or_retired_labels(bad_label: str):
    """T2-F5-04: Forbidden CPQ or retired agent:cmano labels are rejected."""
    bad = {
        "id": "DRG-822",
        "project": "P-DRG-1",
        "state": "Backlog",
        "labels": [bad_label],
        "description": "Source: `Game-Requirements/requirements/01-Project-Overview.md`",
    }
    res = validate_linear_issue_payload(bad, is_new_gap=True, repo_root=REPO_ROOT)
    assert not res.ok


@pytest.mark.parametrize("bad_state", ["Done", "In Review", "Canceled"])
def test_f5_rejects_invalid_status_on_new_gap_issue(bad_state: str):
    """T2-F5-05: New gap issue cannot enter Linear in Done, In Review, or Canceled."""
    bad = {
        "id": "DRG-823",
        "project": "P-DRG-1",
        "state": bad_state,
        "labels": ["type:feature"],
        "description": "Source: `Game-Requirements/requirements/01-Project-Overview.md`",
    }
    res = validate_linear_issue_payload(bad, is_new_gap=True, repo_root=REPO_ROOT)
    assert not res.ok


@pytest.mark.parametrize(
    "excluded_src",
    [
        "tests/uat/pricing-lifecycle-management.md",
        "docs/reference/cmano-db/ship.md",
    ],
)
def test_f5_rejects_excluded_on_disk_source_path(excluded_src: str):
    """T2-F5-06: Linear payload citing excluded on-disk CPQ or cmano-db Source path is rejected."""
    assert (REPO_ROOT / excluded_src).is_file()
    bad = {
        "id": "DRG-999",
        "project": "P-DRG-1",
        "state": "Backlog",
        "labels": ["type:feature"],
        "description": f"Source: `{excluded_src}`\nAC: Must be rejected.",
    }
    res = validate_linear_issue_payload(bad, is_new_gap=True, repo_root=REPO_ROOT)
    assert not res.ok
    assert any("excluded Source path" in e for e in res.errors)


# ===========================================================================
# F6 Boundaries (7 tests)
# ===========================================================================


def test_f6_rejects_notion_task_missing_icon_or_project():
    """T2-F6-01: Notion Tasks row missing icon or linked to wrong project is rejected."""
    no_icon = {
        "id": "n-bad-1",
        "icon": None,
        "project_id": NOTION_PROJECT_AEGIS_ID,
        "status": "Not started",
        "note": "https://linear.app/drgamtd-workspace/issue/DRG-101",
    }
    wrong_proj = {
        "id": "n-bad-2",
        "icon": "🎯",
        "project_id": "open-crm-cpq-id",
        "status": "Not started",
        "note": "https://linear.app/drgamtd-workspace/issue/DRG-101",
    }
    assert not validate_notion_task_row(no_icon).ok
    assert not validate_notion_task_row(wrong_proj).ok


@pytest.mark.parametrize("bad_status", ["In Review", "Todo", "Backlog"])
def test_f6_rejects_linear_only_status_in_notion_tasks(bad_status: str):
    """T2-F6-02: Linear-only status names are rejected in Notion Tasks DB."""
    row = {
        "id": "n-bad-status",
        "icon": "🎯",
        "project_id": NOTION_PROJECT_AEGIS_ID,
        "status": bad_status,
        "note": "https://linear.app/drgamtd-workspace/issue/DRG-101",
    }
    assert not validate_notion_task_row(row).ok


def test_f6_rejects_one_way_orphan_link():
    """T2-F6-03: One-way Linear->Notion or Notion->Linear orphan link is caught."""
    linear = [{"id": "DRG-101", "description": "No notion url here"}]
    notion = [
        {
            "id": "11111111-2222-3333-4444-555566667777",
            "url": "https://www.notion.so/11111111222233334444555566667777",
            "note": "Linear: https://linear.app/drgamtd-workspace/issue/DRG-101",
        }
    ]
    res = validate_cross_links(linear, notion)
    assert not res.ok
    assert any("One-way link" in e for e in res.errors)


def test_f6_rejects_unapproved_frozen_mirror_mutation(load_fixture_json):
    """T2-F6-04: Mutating Frozen Git mirror without M1 authorization fails validation."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    rem_log = load_fixture_json("artifacts/remediation-log.json")
    rem_log["operations"].append(
        {
            "req_id": "FR-01",
            "target": "notion",
            "action": "update",
            "database_id": NOTION_FROZEN_MIRROR_PAGE_ID,
            "page_id": "mirror-row-1",
            "url": "https://www.notion.so/mirror1",
            "before_state": {},
            "after_state": {},
        }
    )
    res = validate_remediation_log(rem_log, baseline, allow_frozen_mirror_writes=False)
    assert not res.ok
    assert any("Frozen Git mirror" in e for e in res.errors)


def test_f6_handles_notion_429_retry_backoff():
    """T2-F6-05: Notion adapter backs off on HTTP 429 and completes without dropping rows."""
    calls = {"n": 0}
    sleeps: list[float] = []

    def flaky_view(mode: str, cursor: str | None):
        calls["n"] += 1
        if calls["n"] == 1:
            return {"status_code": 429, "results": []}
        return {"status_code": 200, "results": [{"id": "recovered-row"}], "has_more": False}

    adapter = NotionViewAdapter(flaky_view, min_interval_s=0.0, sleep_fn=sleeps.append)
    rows = adapter.query_with_fallback(try_sql_first=False)
    assert rows == [{"id": "recovered-row"}]
    assert adapter.trace.retries_on_429 == 1
    assert sleeps == [0.05]


def test_f6_cross_links_slugless_url_oneway_and_dashed_uuid_normalization():
    """T2-F6-06: Slugless notion.so/<32hex> one-way links fail; dashed UUID + slugless URL pair passes."""
    one_way_slugless = validate_cross_links(
        [
            {
                "id": "DRG-1",
                "description": "Notion: https://www.notion.so/deadbeefdeadbeefdeadbeefdeadbeef",
            }
        ],
        [],
    )
    assert not one_way_slugless.ok
    assert any("deadbeefdeadbeefdeadbeefdeadbeef" in e for e in one_way_slugless.errors)

    valid_dashed_vs_slugless = validate_cross_links(
        [
            {
                "id": "DRG-1",
                "description": "Notion: https://www.notion.so/11111111222233334444555566667777",
            }
        ],
        [
            {
                "id": "11111111-2222-3333-4444-555566667777",
                "url": "https://www.notion.so/Page-Slug-11111111222233334444555566667777",
                "note": "Linear: https://linear.app/drgamtd-workspace/issue/DRG-1",
            }
        ],
    )
    valid_dashed_vs_slugless.require_ok()


def test_f6_remediation_log_rejects_missing_completed_at(load_fixture_json):
    """T2-F6-07: remediation-log.json missing or non-ISO completed_at is rejected."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    rem_log = load_fixture_json("artifacts/remediation-log.json")
    del rem_log["completed_at"]
    res_missing = validate_remediation_log(rem_log, baseline)
    assert not res_missing.ok
    assert any("completed_at" in e for e in res_missing.errors)

    rem_log["completed_at"] = "not-a-timestamp"
    res_bad = validate_remediation_log(rem_log, baseline)
    assert not res_bad.ok
    assert any("completed_at" in e for e in res_bad.errors)


# ===========================================================================
# F7 Boundaries (7 tests)
# ===========================================================================


def test_f7_fails_if_any_single_requirement_remains_incomplete(load_fixture_json):
    """T2-F7-01: A single remaining incomplete item causes final-gap-matrix gate to fail."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    final = load_fixture_json("artifacts/final-gap-matrix.json")
    final["evaluations"][0]["notion_status"] = "incomplete"
    final["evaluations"][0]["recommended_action"] = "update_notion"
    final["summary"]["notion"]["complete"] -= 1
    final["summary"]["notion"]["incomplete"] += 1
    res = validate_gap_matrix(final, is_final=True, baseline_matrix=baseline)
    assert not res.ok


def test_f7_fails_if_final_matrix_reuses_baseline_decision_ids(load_fixture_json):
    """T2-F7-02: Reusing a baseline Jev decision ID in final-gap-matrix fails independence check."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    final = load_fixture_json("artifacts/final-gap-matrix.json")
    final["evaluations"][0]["jev_decision_id"] = baseline["evaluations"][0]["jev_decision_id"]
    res = validate_gap_matrix(final, is_final=True, baseline_matrix=baseline)
    assert not res.ok
    assert any("reused baseline decision ID" in e for e in res.errors)


def test_f7_fails_on_stale_or_missing_source_file_in_final_audit(load_fixture_json):
    """T2-F7-03: Omitting any expected in-scope file causes 100% corpus coverage gate to fail."""
    catalog = load_fixture_json("artifacts/requirements-catalog.json")
    res = validate_requirements_catalog(
        catalog,
        require_full_corpus=True,
        expected_in_scope_files=[
            "Game-Requirements/requirements/01-Project-Overview.md",
            "Game-Requirements/requirements/02-Core-Gameplay-Loop.md",
        ],
    )
    assert not res.ok
    assert any("02-Core-Gameplay-Loop.md" in e for e in res.errors)


def test_f7_secret_scanner_catches_embedded_openrouter_or_notion_key():
    """T2-F7-04: Secret scanner catches OpenRouter, Notion, and Linear API key patterns across modes."""
    assert not scan_for_secrets_and_db3("key=sk-or-v1-canarytestsecret99").ok
    assert not scan_for_secrets_and_db3("token=ntn_canarytestsecret99").ok
    assert not scan_for_secrets_and_db3("token=secret_canarytestsecret99").ok
    assert not scan_for_secrets_and_db3("Authorization: Bearer canarytoken1234567890").ok

    # Leading \b on notion_token prevents false-positives on snake_case identifiers
    for scan_mode in ("evidence", "doc", "code", "test_code"):
        scan_for_secrets_and_db3("test_zero_secret_and_db3_leakage", mode=scan_mode).require_ok()

    # mode="doc": allows bare `.db3` rule prose, rejects actual <name>.db3 filenames/paths
    scan_for_secrets_and_db3(
        "Never include `.db3` extracts or credentials in Jev state.", mode="doc"
    ).require_ok()
    assert not scan_for_secrets_and_db3("Loaded cmo_secret.db3 in rubric", mode="doc").ok
    assert not scan_for_secrets_and_db3("Path data/cmo.db3 leaked", mode="doc").ok

    # mode="code": allows _DB3_PATTERN / Bearer {tok}, rejects concrete tokens and real .env secrets
    scan_for_secrets_and_db3(
        '_DB3_PATTERN = re.compile(r"\\.db3\\b")\nheaders = {"Authorization": f"Bearer {api_key}"}\n'
        'hdr = "Bearer dummy_placeholder_1234"',
        mode="code",
    ).require_ok()
    assert not scan_for_secrets_and_db3("KEY = 'sk-or-v1-canarytestsecret99'", mode="code").ok
    assert not scan_for_secrets_and_db3("TOK = 'ntn_canarytestsecret99'", mode="code").ok
    assert not scan_for_secrets_and_db3(
        "VAL = 'actual-env-secret-value'",
        extra_forbidden_values=["actual-env-secret-value"],
        mode="code",
    ).ok

    # mode="test_code": allows dummy unit-test fixtures, rejects real .env secrets
    scan_for_secrets_and_db3(
        'state="Loaded cmo_secret.db3 table"\nauth="Bearer canarytoken1234567890"\n'
        'key="sk-or-v1-canarytestsecret99"',
        extra_forbidden_values=["real-dotenv-secret-xyz"],
        mode="test_code",
    ).require_ok()
    assert not scan_for_secrets_and_db3(
        'key="real-dotenv-secret-xyz"',
        extra_forbidden_values=["real-dotenv-secret-xyz"],
        mode="test_code",
    ).ok

    with pytest.raises(ValueError, match="Unsupported scan mode"):
        scan_for_secrets_and_db3("clean", mode="invalid_mode")


def test_f7_require_impl_flag_fails_when_deliverables_missing(
    tmp_path: Path, require_impl_mode: bool, repo_root: Path
):
    """T2-F7-05: AEGIS_E2E_REQUIRE_IMPL check fails fast if deliverables are absent and passes when present."""
    with pytest.raises(AssertionError, match="missing"):
        assert_impl_deliverables_present(tmp_path)
    with pytest.raises(AssertionError, match="missing"):
        assert_impl_deliverables_present(tmp_path, include_final=True)

    if require_impl_mode:
        present = assert_impl_deliverables_present(repo_root, include_final=False)
        assert len(present) == 7
        final_matrix = (
            repo_root
            / "production"
            / "qa"
            / "evidence"
            / "backlog-gap-2026-11-22"
            / "final-gap-matrix.json"
        )
        if not final_matrix.exists():
            with pytest.raises(AssertionError, match="missing"):
                assert_impl_deliverables_present(repo_root, include_final=True)
        else:
            assert len(assert_impl_deliverables_present(repo_root, include_final=True)) == 8


def test_f7_final_matrix_rejects_truncated_catalog_or_baseline_coverage(load_fixture_json):
    """T2-F7-06: validate_gap_matrix(is_final=True) rejects truncated final matrices."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    final = load_fixture_json("artifacts/final-gap-matrix.json")
    catalog = load_fixture_json("artifacts/requirements-catalog.json")

    # Truncate final matrix to first item only
    truncated = json.loads(json.dumps(final))
    truncated["evaluations"] = truncated["evaluations"][:1]
    truncated["summary"]["total_requirements"] = 1
    truncated["summary"]["source_files_covered"] = 1
    truncated["summary"]["linear"] = {"complete": 1, "incomplete": 0, "missing": 0}
    truncated["summary"]["notion"] = {"complete": 1, "incomplete": 0, "missing": 0}

    res_base = validate_gap_matrix(truncated, is_final=True, baseline_matrix=baseline)
    assert not res_base.ok
    assert any("missing baseline req_ids" in e for e in res_base.errors)

    res_cat = validate_gap_matrix(
        final,
        is_final=True,
        baseline_matrix=baseline,
        catalog=catalog,
        expected_in_scope_files=IN_SCOPE_GIT_FILES,
    )
    assert not res_cat.ok
    assert any("missing catalog req_ids" in e or "missing in-scope files" in e for e in res_cat.errors)


def test_f7_final_matrix_rejects_empty_matches_and_omitted_source_files_covered(load_fixture_json):
    """T2-F7-07: Empty linear/notion matches & omitted source_files_covered fail; shipped_verified exempts linear_matches."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    final = load_fixture_json("artifacts/final-gap-matrix.json")

    # (a) Omitted summary.source_files_covered fails when is_final=True
    no_sfc = json.loads(json.dumps(final))
    del no_sfc["summary"]["source_files_covered"]
    res_no_sfc = validate_gap_matrix(no_sfc, is_final=True, baseline_matrix=baseline)
    assert not res_no_sfc.ok
    assert any("source_files_covered" in e for e in res_no_sfc.errors)

    # (b) Non-shipped, non-held complete item with empty linear_matches fails
    empty_lin = json.loads(json.dumps(final))
    empty_lin["evaluations"][0]["linear_matches"] = []
    res_empty_lin = validate_gap_matrix(empty_lin, is_final=True, baseline_matrix=baseline)
    assert not res_empty_lin.ok
    assert any("requires non-empty linear_matches" in e for e in res_empty_lin.errors)

    # (c) Non-held complete item with empty notion_matches fails
    empty_not = json.loads(json.dumps(final))
    empty_not["evaluations"][0]["notion_matches"] = []
    res_empty_not = validate_gap_matrix(empty_not, is_final=True, baseline_matrix=baseline)
    assert not res_empty_not.ok
    assert any("requires non-empty notion_matches" in e for e in res_empty_not.errors)

    # (d) sub_status == "shipped_verified" exempts empty linear_matches while preserving non-circular state
    shipped_ok = json.loads(json.dumps(final))
    shipped_ok["evaluations"][0]["linear_matches"] = []
    shipped_ok["evaluations"][0]["sub_status"] = "shipped_verified"
    shipped_ok["evaluations"][0]["verification_evidence"] = ["tests/ProjectAegis.Sim.Tests green"]
    validate_gap_matrix(shipped_ok, is_final=True, baseline_matrix=baseline).require_ok()

    jev_state = build_independent_jev_state(
        {
            "req_id": "FR-01",
            "title": "Overview",
            "source_file": "Game-Requirements/requirements/01-Project-Overview.md",
            "source_lines": "1-10",
            "lifecycle_state": "shipped",
            "summary": "Shipped feature.",
            "acceptance_criteria": ["Verified by suite."],
        },
        [],
        [],
        evaluation=shipped_ok["evaluations"][0],
    )
    assert jev_state["requirement"]["sub_status"] == "shipped_verified"
    assert jev_state["requirement"]["verification_evidence"] == ["tests/ProjectAegis.Sim.Tests green"]
    assert "linear_status" not in jev_state and "notion_status" not in jev_state

    # (e) Self-declared owner_held=True with empty matches fails when allowed_owner_hold_req_ids=()
    spoofed_held = json.loads(json.dumps(final))
    spoofed_held["evaluations"][0]["linear_matches"] = []
    spoofed_held["evaluations"][0]["notion_matches"] = []
    spoofed_held["evaluations"][0]["owner_held"] = True
    res_spoof = validate_gap_matrix(
        spoofed_held,
        is_final=True,
        baseline_matrix=baseline,
        allowed_owner_hold_req_ids=(),
    )
    assert not res_spoof.ok
    assert any("requires non-empty linear_matches" in e for e in res_spoof.errors)
    assert any("requires non-empty notion_matches" in e for e in res_spoof.errors)
