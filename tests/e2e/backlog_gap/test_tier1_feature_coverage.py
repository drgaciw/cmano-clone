"""Tier 1: Feature coverage tests (35 tests: 5 per feature F1-F7, offline hermetic)."""

from __future__ import annotations

import json
import subprocess
from pathlib import Path

import pytest

from harness.cli_adapter import (
    IMPL_EVIDENCE_DIR,
    IMPL_QUESTIONS_PACK,
    IMPL_RUBRIC_MD,
    NotionViewAdapter,
    evaluate_requirements_batched,
    post_jev_json,
    probe_impl_cli_help,
    run_classify_uat_cli,
)
from harness.contracts import (
    IN_SCOPE_GIT_FILES,
    LINEAR_BACKLOG_STATE_ID,
    NOTION_FROZEN_MIRROR_PAGE_ID,
    NOTION_PROJECT_AEGIS_ID,
    NOTION_TASKS_COLLECTION,
    NOTION_TASKS_VIEW,
    derive_recommended_action,
    evaluate_requirement_record,
    is_cpq_linear_issue,
    parse_jev_primitives,
    read_baltic_v2_hash,
    scan_for_secrets_and_db3,
    validate_cross_links,
    validate_gap_matrix,
    validate_jev_request,
    validate_linear_issue_payload,
    validate_markdown_parity,
    validate_notion_task_row,
    validate_question_pack,
    validate_remediation_log,
    validate_requirements_catalog,
    validate_rubric_markdown,
    validate_scope_decisions,
)

pytestmark = pytest.mark.tier1


# ===========================================================================
# F1: typesafe ai jev Decision Client & Open-Scope Resolution (5 tests)
# ===========================================================================


def test_f1_jev_request_schema_valid_14_and_13(
    mock_jev_server, load_fixture_json, require_impl_mode
):
    """T1-F1-01: Valid jev-1.14 and jev-1.13 payloads succeed against mock endpoints."""
    req14 = load_fixture_json("jev/valid-jev14-request.json")
    req13 = load_fixture_json("jev/valid-jev13-request.json")

    validate_jev_request(req14).require_ok()
    validate_jev_request(req13).require_ok()

    resp14 = post_jev_json(mock_jev_server.decisions_url, req14)
    assert resp14["status_code"] == 200
    assert "answers" in resp14["body"]
    assert "usage" in resp14["body"]

    resp13 = post_jev_json(mock_jev_server.systemone_url, req13)
    assert resp13["status_code"] == 200
    assert "answers" in resp13["body"]

    if require_impl_mode:
        proc = probe_impl_cli_help()
        assert proc is not None and proc.returncode == 0


def test_f1_dotenv_and_provider_route_precedence(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]
):
    """T1-F1-02: Explicit env vars beat .env; provider precedence matches contract."""
    import sys

    repo_root = Path(__file__).resolve().parents[3]
    sys.path.insert(0, str(repo_root / "tools" / "jev"))
    import classify_uat

    evidence = tmp_path / "evidence.json"
    evidence.write_text(
        json.dumps(
            {
                "policy_id": "baltic-patrol-classify",
                "rows": [{"id": "UAT-1", "result": "pass", "observation": "Nominal"}],
            }
        ),
        encoding="utf-8",
    )
    rubric = repo_root / "data" / "jev" / "uat-v1.questions.json"
    for key in (
        "TYPESAFE_API_KEY",
        "OPENROUTER_API_KEY",
        "CLOUDFLARE_API_TOKEN",
        "CLOUDFLARE_ACCOUNT_ID",
        "AI_GATEWAY_API_KEY",
        "VERCEL_OIDC_TOKEN",
    ):
        monkeypatch.delenv(key, raising=False)

    # Isolated dir with no .env -> blocked exit 2
    isolated = tmp_path / "no_env"
    isolated.mkdir()
    monkeypatch.setattr(classify_uat, "_REPO_ROOT", isolated)
    monkeypatch.setattr(classify_uat, "_PARENT_ROOT", isolated)
    assert classify_uat.main(["--evidence", str(evidence), "--rubric", str(rubric)]) == 2
    assert json.loads(capsys.readouterr().out)["status"] == "blocked"

    # With .env providing OPENROUTER_API_KEY, explicit TYPESAFE_API_KEY still wins
    (tmp_path / ".env").write_text("OPENROUTER_API_KEY=dotenv-or-key\n", encoding="utf-8")
    monkeypatch.setattr(classify_uat, "_REPO_ROOT", tmp_path)
    monkeypatch.setenv("TYPESAFE_API_KEY", "explicit-ts-key")
    assert (
        classify_uat.main(
            ["--evidence", str(evidence), "--rubric", str(rubric), "--dry-request"]
        )
        == 0
    )
    dry_body = json.loads(capsys.readouterr().out)
    assert dry_body["model"] == "jev-1.13.0"


def test_f1_scope_decisions_provenance_schema(load_fixture_json, require_impl_mode):
    """T1-F1-03: scope-decisions-jev.json records valid Jev provenance with 0 secrets."""
    doc = (
        json.loads((IMPL_EVIDENCE_DIR / "scope-decisions-jev.json").read_text(encoding="utf-8"))
        if require_impl_mode
        else load_fixture_json("artifacts/scope-decisions-jev.json")
    )
    validate_scope_decisions(doc).require_ok()


def test_f1_noul_choice_score_primitives_parsed(load_fixture_json):
    """T1-F1-04: noul, choice, and discrete rank score primitives are parsed accurately."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    resp = load_fixture_json("jev/mock-primitives-response.json")
    parsed = parse_jev_primitives(resp, pack["questions"])

    assert parsed["backlog_completeness"]["noul"] == pytest.approx(0.92)
    assert parsed["backlog_completeness"]["passes"] is True
    assert parsed["gap_classification"]["choice"] == "complete"
    assert parsed["gap_classification"]["needs_human_review"] is False
    # 1.49 on 3-level [0, 1, 2] discrete legend rounds to rank 1, not linear float
    assert parsed["remediation_priority"]["discrete_rank"] == 1


def test_f1_privacy_hygiene_blocks_secrets_and_db3(mock_jev_server, load_fixture_json):
    """T1-F1-05: Secret tokens or .db3 references are blocked before HTTP POST."""
    base = load_fixture_json("jev/valid-jev14-request.json")
    tainted_secret = {**base, "state": {"note": "leak sk-or-v1-secretcanary12345"}}
    tainted_db3 = {**base, "state": {"note": "data/catalog/cmo_v512.db3"}}

    with pytest.raises(ValueError, match="Blocked before HTTP POST"):
        post_jev_json(mock_jev_server.decisions_url, tainted_secret)
    with pytest.raises(ValueError, match="Blocked before HTTP POST"):
        post_jev_json(mock_jev_server.decisions_url, tainted_db3)

    assert len(mock_jev_server.requests) == 0


# ===========================================================================
# F2: Gap Analysis Rubric Specification & Question Pack (5 tests)
# ===========================================================================


def test_f2_rubric_markdown_has_required_sections(fixtures_dir: Path, require_impl_mode):
    """T1-F2-01: backlog-gap-rubric.md defines statuses, script-first priority, and thresholds."""
    rubric_path = IMPL_RUBRIC_MD if require_impl_mode else (fixtures_dir / "artifacts" / "backlog-gap-rubric.md")
    text = rubric_path.read_text(encoding="utf-8")
    validate_rubric_markdown(text).require_ok()


def test_f2_question_pack_valid_keys_and_escapes(load_fixture_json, require_impl_mode):
    """T1-F2-02: Question pack satisfies <=20 questions, key regex, escapes, and thresholds."""
    pack = (
        json.loads(IMPL_QUESTIONS_PACK.read_text(encoding="utf-8"))
        if require_impl_mode
        else load_fixture_json("jev/valid-question-pack.json")
    )
    validate_question_pack(pack).require_ok()


def test_f2_script_first_mechanical_gate_not_overridden(load_fixture_json):
    """T1-F2-03: Empty linear_matches stays 'missing' even when Jev returns noul=0.99."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    req = {
        "req_id": "FR-14",
        "source_file": "Game-Requirements/requirements/14-Engagement-And-Fire-Control.md",
        "lifecycle_state": "active",
    }
    optimistic_jev = {
        "id": "gen-dec-opt-1",
        "answers": {
            "backlog_completeness": {"noul": 0.99},
            "gap_classification": {"choice": "complete", "confidence": 0.98},
        },
    }
    ev = evaluate_requirement_record(
        req=req,
        linear_matches=[],
        notion_matches=[
            {
                "id": "n-1",
                "has_icon": True,
                "project_id": NOTION_PROJECT_AEGIS_ID,
                "status": "Not started",
                "note": "https://linear.app/drgamtd-workspace/issue/DRG-14",
            }
        ],
        jev_response=optimistic_jev,
        questions=pack["questions"],
    )
    assert ev["linear_status"] == "missing"
    assert ev["recommended_action"] in ("create_linear", "both")


def test_f2_noul_threshold_080_classifies_complete(load_fixture_json):
    """T1-F2-04: Passing deterministic checks + noul>=0.80 yields complete/none."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    req = {
        "req_id": "FR-01",
        "source_file": "Game-Requirements/requirements/01-Project-Overview.md",
        "lifecycle_state": "active",
    }
    jev_resp = {
        "id": "gen-dec-pass-1",
        "answers": {
            "backlog_completeness": {"noul": 0.82},
            "gap_classification": {"choice": "complete", "confidence": 0.85},
        },
    }
    ev = evaluate_requirement_record(
        req=req,
        linear_matches=[{"id": "DRG-101", "source_path": req["source_file"], "acceptance_criteria": ["AC-1"]}],
        notion_matches=[
            {
                "id": "11111111-2222-3333-4444-555566667777",
                "has_icon": True,
                "project_id": NOTION_PROJECT_AEGIS_ID,
                "status": "Not started",
                "note": "https://linear.app/drgamtd-workspace/issue/DRG-101",
            }
        ],
        jev_response=jev_resp,
        questions=pack["questions"],
    )
    assert ev["linear_status"] == "complete"
    assert ev["notion_status"] == "complete"
    assert ev["recommended_action"] == "none"
    assert ev["needs_human_review"] is False


def test_f2_choice_low_confidence_flags_human_review(load_fixture_json):
    """T1-F2-05: choice confidence < 0.50 sets needs_human_review=True."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    req = {
        "req_id": "FR-03",
        "source_file": "Game-Requirements/requirements/03-Simulation-Modes.md",
        "lifecycle_state": "active",
    }
    jev_resp = {
        "id": "gen-dec-lowconf-1",
        "answers": {
            "backlog_completeness": {"noul": 0.85},
            "gap_classification": {"choice": "complete", "confidence": 0.42},
        },
    }
    ev = evaluate_requirement_record(
        req=req,
        linear_matches=[{"id": "DRG-103", "source_path": req["source_file"], "acceptance_criteria": ["AC-1"]}],
        notion_matches=[
            {
                "id": "n-103",
                "has_icon": True,
                "project_id": NOTION_PROJECT_AEGIS_ID,
                "status": "Not started",
                "note": "https://linear.app/drgamtd-workspace/issue/DRG-103",
            }
        ],
        jev_response=jev_resp,
        questions=pack["questions"],
    )
    assert ev["needs_human_review"] is True
    assert any("0.420 < 0.5" in g for g in ev["gaps"])


# ===========================================================================
# F3: Exhaustive Requirements Markdown Review & Extraction (5 tests)
# ===========================================================================


def test_f3_catalog_schema_and_required_fields(load_fixture_json):
    """T1-F3-01: Synthetic requirements-catalog.json satisfies Contract 2 schema."""
    catalog = load_fixture_json("artifacts/requirements-catalog.json")
    validate_requirements_catalog(catalog, require_full_corpus=False).require_ok()


def test_f3_in_scope_corpus_coverage_100_percent(tmp_path: Path):
    """T1-F3-02: All 38 in-scope Git requirement files are covered with >=1 requirement."""
    source_files = []
    requirements = []
    for idx, rel in enumerate(IN_SCOPE_GIT_FILES, start=1):
        md_file = tmp_path / rel
        md_file.parent.mkdir(parents=True, exist_ok=True)
        md_file.write_text(f"# Requirement {idx}\nLine 2\nLine 3\n", encoding="utf-8")
        cat = "A" if rel.startswith("Game-Requirements/requirements/") else (
            "B" if rel.startswith("Game-Requirements/drafts/") else "C"
        )
        source_files.append({"path": rel, "category": cat, "lines": 3, "in_scope": True})
        requirements.append(
            {
                "req_id": f"REQ-{idx:03d}",
                "title": f"Requirement {idx}",
                "source_file": rel,
                "source_lines": "1-3",
                "domain_project": "cmano-clone",
                "lifecycle_state": "active",
                "summary": f"Synthetic summary for {rel}",
                "acceptance_criteria": ["Testable acceptance criterion."],
            }
        )

    catalog = {
        "generated_at": "2026-11-22T23:00:00Z",
        "source_files": source_files,
        "requirements": requirements,
    }
    validate_requirements_catalog(
        catalog, repo_root=tmp_path, require_full_corpus=True
    ).require_ok()


def test_f3_excludes_cpq_and_cmano_db_reference(load_fixture_json):
    """T1-F3-03: Excluded tests/uat/* and docs/reference/cmano-db/* are rejected if present."""
    catalog = load_fixture_json("artifacts/requirements-catalog.json")
    tainted_cpq = json.loads(json.dumps(catalog))
    tainted_cpq["source_files"].append(
        {"path": "tests/uat/quote-lifecycle-management-cpq.md", "category": "C", "lines": 20, "in_scope": True}
    )
    res_cpq = validate_requirements_catalog(tainted_cpq, require_full_corpus=False)
    assert not res_cpq.ok

    tainted_db = json.loads(json.dumps(catalog))
    tainted_db["requirements"].append(
        {
            **tainted_db["requirements"][0],
            "req_id": "DB-BAD",
            "source_file": "docs/reference/cmano-db/ship.md",
        }
    )
    res_db = validate_requirements_catalog(tainted_db, require_full_corpus=False)
    assert not res_db.ok


def test_f3_source_line_citations_exist_and_nonempty(repo_root: Path):
    """T1-F3-04: Line-range citations resolve against real workspace markdown files."""
    first_file = IN_SCOPE_GIT_FILES[0]
    line_count = len((repo_root / first_file).read_text(encoding="utf-8").splitlines())
    catalog = {
        "generated_at": "2026-11-22T23:00:00Z",
        "source_files": [{"path": first_file, "category": "A", "lines": line_count, "in_scope": True}],
        "requirements": [
            {
                "req_id": "FR-01",
                "title": "Overview",
                "source_file": first_file,
                "source_lines": f"1-{min(10, line_count)}",
                "domain_project": "cmano-clone",
                "lifecycle_state": "active",
                "summary": "Overview check against real repo file.",
                "acceptance_criteria": ["Real file exists and line range is in bounds."],
            }
        ],
    }
    validate_requirements_catalog(catalog, repo_root=repo_root, require_full_corpus=False).require_ok()


def test_f3_lifecycle_state_and_domain_project_mapping(load_fixture_json):
    """T1-F3-05: Catalog preserves valid lifecycle_state and domain_project enums."""
    catalog = load_fixture_json("artifacts/requirements-catalog.json")
    states = {r["lifecycle_state"] for r in catalog["requirements"]}
    projects = {r["domain_project"] for r in catalog["requirements"]}
    assert {"active", "draft", "deferred_phase_n", "shipped"}.issubset(states)
    assert {"cmano-clone", "Combat Interaction UX", "Aegis Horizons H4-H6"}.issubset(projects)


# ===========================================================================
# F4: Baseline Live Backlog Inventory & Jev Rubric Evaluation (5 tests)
# ===========================================================================


def test_f4_gap_matrix_schema_and_summary_counts(load_fixture_json):
    """T1-F4-01: baseline-gap-matrix.json satisfies Contract 3 and summary parity."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    validate_gap_matrix(baseline, is_final=False).require_ok()


def test_f4_linear_project_filter_excludes_p_drg_6(load_fixture_json):
    """T1-F4-02: Inventory retains Aegis + DRG-377 and excludes 100% of P-DRG-6/CPQ."""
    dump = load_fixture_json("backlogs/synthetic-linear-dump.json")
    retained = [i for i in dump["issues"] if not is_cpq_linear_issue(i)]
    retained_ids = {i["id"] for i in retained}
    assert retained_ids == {"DRG-101", "DRG-268", "DRG-377", "DRG-112"}
    assert "DRG-900" not in retained_ids


def test_f4_notion_view_pagination_and_cpq_exclusion():
    """T1-F4-03: Paginated mode='view' query merges pages and filters CPQ rows."""
    pages = {
        None: {
            "results": [{"id": f"aegis-{i}", "project_id": NOTION_PROJECT_AEGIS_ID} for i in range(100)],
            "has_more": True,
            "next_cursor": "cursor-2",
        },
        "cursor-2": {
            "results": [{"id": f"cpq-{i}", "project_id": "cpq-excluded"} for i in range(27)]
            + [{"id": "aegis-100", "project_id": NOTION_PROJECT_AEGIS_ID}],
            "has_more": False,
            "next_cursor": None,
        },
    }

    adapter = NotionViewAdapter(lambda mode, cursor: pages[cursor], min_interval_s=0.0, sleep_fn=lambda _: None)
    all_rows = adapter.query_with_fallback(view_url=NOTION_TASKS_VIEW, try_sql_first=False)
    aegis_rows = [r for r in all_rows if r["project_id"] == NOTION_PROJECT_AEGIS_ID]
    assert len(all_rows) == 128
    assert len(aegis_rows) == 101
    assert adapter.trace.view_urls_used == [NOTION_TASKS_VIEW]


def test_f4_batched_jev_eval_respects_20_question_and_32kb_caps(mock_jev_server):
    """T1-F4-04: Batch of 35 requirements chunks into <=20-question, <=32KB HTTP POSTs."""
    reqs = [
        {
            "req_id": f"REQ-{i:02d}",
            "source_file": IN_SCOPE_GIT_FILES[i % len(IN_SCOPE_GIT_FILES)],
            "lifecycle_state": "active",
            "summary": f"Requirement {i} description.",
        }
        for i in range(35)
    ]
    linear_by_req = {
        r["req_id"]: [{"id": f"DRG-{i}", "source_path": r["source_file"], "acceptance_criteria": ["AC"]}]
        for i, r in enumerate(reqs)
    }
    notion_by_req = {
        r["req_id"]: [
            {
                "id": f"n-{i}",
                "has_icon": True,
                "project_id": NOTION_PROJECT_AEGIS_ID,
                "status": "Not started",
                "note": f"https://linear.app/drgamtd-workspace/issue/DRG-{i}",
            }
        ]
        for i, r in enumerate(reqs)
    }

    evals = evaluate_requirements_batched(
        reqs,
        linear_by_req=linear_by_req,
        notion_by_req=notion_by_req,
        jev_url=mock_jev_server.decisions_url,
    )
    assert len(evals) == 35
    assert len(mock_jev_server.requests) == 2
    for recorded in mock_jev_server.requests:
        assert len(recorded.raw_bytes) <= 32768
        assert 1 <= len(recorded.body["questions"]) <= 20


def test_f4_recommended_action_derivation_matrix():
    """T1-F4-05: recommended_action derives deterministically from (linear, notion) pairs."""
    assert derive_recommended_action("complete", "missing") == "create_notion"
    assert derive_recommended_action("incomplete", "complete") == "update_linear"
    assert derive_recommended_action("missing", "missing") == "both"
    assert derive_recommended_action("complete", "complete") == "none"
    assert derive_recommended_action("complete", "incomplete") == "update_notion"
    assert derive_recommended_action("missing", "complete") == "create_linear"


# ===========================================================================
# F5: Linear Backlog Gap Remediation (5 tests)
# ===========================================================================


def test_f5_new_linear_issue_enters_backlog_without_surface(repo_root: Path):
    """T1-F5-01: New Linear gap issue enters Backlog with zero Surface: lines."""
    issue = {
        "id": "DRG-810",
        "project": "P-DRG-1",
        "state": "Backlog",
        "state_id": LINEAR_BACKLOG_STATE_ID,
        "labels": ["type:feature", "area:sim"],
        "description": (
            "Source: `Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md`\n"
            "Req IDs: ROE-01\n"
            "AC: Doctrine inheritance resolves deterministically.\n"
            "Notion: https://www.notion.so/11111111222233334444555566667777"
        ),
    }
    validate_linear_issue_payload(issue, is_new_gap=True, repo_root=repo_root).require_ok()


def test_f5_incomplete_linear_update_is_non_destructive(repo_root: Path):
    """T1-F5-02: Updating an incomplete issue preserves hold:owner-go/NEEDS_OWNER/single-owner."""
    prior = {
        "id": "DRG-999",
        "project": "P-DRG-1",
        "state": "Backlog",
        "labels": ["hold:owner-go", "NEEDS_OWNER", "single-owner"],
        "description": "Original summary text.",
    }
    updated = {
        "id": "DRG-999",
        "project": "P-DRG-1",
        "state": "Backlog",
        "labels": ["hold:owner-go", "NEEDS_OWNER", "single-owner", "area:c2"],
        "description": (
            "Original summary text.\n"
            "Source: `Game-Requirements/requirements/20-Command-And-Control-UI.md`\n"
            "AC: Panel renders without overlap.\n"
            "Notion: https://www.notion.so/22222222222233334444555566667777"
        ),
    }
    validate_linear_issue_payload(
        updated, is_new_gap=False, prior_issue=prior, repo_root=repo_root
    ).require_ok()


def test_f5_linear_pointer_not_prose_and_valid_git_path(repo_root: Path):
    """T1-F5-03: Concise pointer passes while huge spec dump fails pointer-not-prose rule."""
    concise = {
        "id": "DRG-811",
        "project": "P-DRG-1",
        "state": "Backlog",
        "labels": ["type:feature"],
        "description": "Source: `Game-Requirements/requirements/04-Agent-Delegation.md`\nAC: Pointer only.",
    }
    validate_linear_issue_payload(concise, is_new_gap=True, repo_root=repo_root).require_ok()

    prose_dump = {
        **concise,
        "description": concise["description"] + "\n" + ("Spec prose paragraph. " * 1200),
    }
    res = validate_linear_issue_payload(prose_dump, is_new_gap=True, repo_root=repo_root)
    assert not res.ok


def test_f5_never_invents_done_or_cancels_on_remediation(load_fixture_json):
    """T1-F5-04: Remediation log never transitions issues to Done/Canceled/Duplicate."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    rem_log = load_fixture_json("artifacts/remediation-log.json")
    validate_remediation_log(rem_log, baseline).require_ok()


def test_f5_deduplication_links_existing_epic_before_create(load_fixture_json):
    """T1-F5-05: Requirements already matched to DRG-377 or DRG-112 select update/link, not duplicate create."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    req = {
        "req_id": "SWARM-27",
        "source_file": "Game-Requirements/requirements/22-Drone-Swarm-Platforms.md",
        "lifecycle_state": "deferred_phase_n",
    }
    ev = evaluate_requirement_record(
        req=req,
        linear_matches=[{"id": "DRG-112", "source_path": req["source_file"], "acceptance_criteria": []}],
        notion_matches=[],
        jev_response={"id": "gen-dec-1", "answers": {"backlog_completeness": {"noul": 0.70}}},
        questions=pack["questions"],
    )
    assert ev["linear_status"] == "incomplete"
    assert ev["recommended_action"] == "both"
    assert ev["linear_matches"] == ["DRG-112"]


# ===========================================================================
# F6: Notion Backlog Gap Remediation & Cross-Linking (5 tests)
# ===========================================================================


def test_f6_notion_task_row_has_icon_project_status_and_note(load_fixture_json):
    """T1-F6-01: Notion Tasks rows have icon, Project Aegis relation, valid Status, and Linear URL."""
    tasks = load_fixture_json("backlogs/synthetic-notion-tasks.json")["pages"]
    aegis_rows = [r for r in tasks if r["project_id"] == NOTION_PROJECT_AEGIS_ID]
    assert len(aegis_rows) == 2
    for row in aegis_rows:
        validate_notion_task_row(row).require_ok()


def test_f6_bidirectional_linear_notion_cross_linking(load_fixture_json):
    """T1-F6-02: Paired Linear issues and Notion Tasks pages cross-link bidirectionally."""
    linear = [
        i
        for i in load_fixture_json("backlogs/synthetic-linear-dump.json")["issues"]
        if i["id"] in {"DRG-101", "DRG-268"}
    ]
    notion = [
        n
        for n in load_fixture_json("backlogs/synthetic-notion-tasks.json")["pages"]
        if n["project_id"] == NOTION_PROJECT_AEGIS_ID
    ]
    validate_cross_links(linear, notion).require_ok()


def test_f6_frozen_git_mirror_protection_rule(load_fixture_json):
    """T1-F6-03: Remediation obeys Frozen Git mirror protection policy from M1 decisions."""
    scope_doc = load_fixture_json("artifacts/scope-decisions-jev.json")
    allow_mirror = any(d.get("allow_frozen_mirror_writes") for d in scope_doc["decisions"])
    assert allow_mirror is False
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    rem_log = load_fixture_json("artifacts/remediation-log.json")
    for op in rem_log["operations"]:
        if op.get("target") == "notion":
            assert op.get("database_id") != NOTION_FROZEN_MIRROR_PAGE_ID
    validate_remediation_log(rem_log, baseline, allow_frozen_mirror_writes=allow_mirror).require_ok()


def test_f6_notion_rate_limit_pacing_and_view_mode():
    """T1-F6-04: Notion view fallback and pacing track <=2 req/s without sql calls succeeding."""
    sleep_calls: list[float] = []

    def fake_fetch(mode: str, cursor: str | None):
        if mode == "sql":
            return {"error": "usage_limit_reached", "status_code": 429}
        return {"results": [{"id": "row-1"}], "has_more": False, "next_cursor": None, "status_code": 200}

    adapter = NotionViewAdapter(fake_fetch, min_interval_s=0.5, sleep_fn=sleep_calls.append)
    rows = adapter.query_with_fallback(view_url=NOTION_TASKS_VIEW, try_sql_first=True)
    assert len(rows) == 1
    assert adapter.trace.modes_attempted == ["sql", "view"]
    assert adapter.trace.view_urls_used == [NOTION_TASKS_VIEW]
    assert any(s >= 0.49 for s in sleep_calls)


def test_f6_remediation_log_traceability(load_fixture_json):
    """T1-F6-05: Every baseline gap maps 1:1 to a recorded Linear/Notion operation."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    rem_log = load_fixture_json("artifacts/remediation-log.json")
    validate_remediation_log(rem_log, baseline).require_ok()


# ===========================================================================
# F7: Final E2E Acceptance & Independent Live Re-Verification (5 tests)
# ===========================================================================


def test_f7_final_gap_matrix_zero_missing_or_incomplete(load_fixture_json):
    """T1-F7-01: final-gap-matrix.json reports 0 missing and 0 incomplete on both backlogs."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    final = load_fixture_json("artifacts/final-gap-matrix.json")
    validate_gap_matrix(final, is_final=True, baseline_matrix=baseline).require_ok()


def test_f7_markdown_report_parity_with_final_json(fixtures_dir: Path, load_fixture_json):
    """T1-F7-02: final-gap-matrix.md counts, rubric version, and model match JSON exactly."""
    final = load_fixture_json("artifacts/final-gap-matrix.json")
    md_text = (fixtures_dir / "artifacts" / "final-gap-matrix.md").read_text(encoding="utf-8")
    validate_markdown_parity(final, md_text).require_ok()


def test_f7_independent_re_eval_uses_fresh_jev_provenance(load_fixture_json):
    """T1-F7-03: Every final evaluation carries a fresh decision ID distinct from baseline."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    final = load_fixture_json("artifacts/final-gap-matrix.json")
    base_ids = {ev["jev_decision_id"] for ev in baseline["evaluations"]}
    final_ids = {ev["jev_decision_id"] for ev in final["evaluations"]}
    assert final_ids
    assert base_ids.isdisjoint(final_ids)


def test_f7_zero_secret_or_db3_leakage_across_evidence(fixtures_dir: Path):
    """T1-F7-04: Zero API keys, Bearer tokens, or .db3 extracts appear in evidence fixtures."""
    for p in (fixtures_dir / "artifacts").rglob("*"):
        if p.is_file():
            scan_for_secrets_and_db3(p.read_text(encoding="utf-8")).require_ok()


def test_f7_csharp_and_golden_hash_invariants_unmodified(repo_root: Path):
    """T1-F7-05: Baltic v2 hash is read dynamically from test-floor.json and E2E dir has 0 .cs/.db3."""
    baltic_hash = read_baltic_v2_hash(repo_root)
    assert len(baltic_hash) == 20 and baltic_hash.isdigit()

    e2e_root = repo_root / "tests" / "e2e" / "backlog_gap"
    forbidden = [p for p in e2e_root.rglob("*") if p.suffix in (".cs", ".csproj", ".db3")]
    assert forbidden == []
    for p in e2e_root.rglob("*"):
        if p.is_file() and p.suffix in (".md", ".txt"):
            assert baltic_hash not in p.read_text(encoding="utf-8", errors="ignore")
