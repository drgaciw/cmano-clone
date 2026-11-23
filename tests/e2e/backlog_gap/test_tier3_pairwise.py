"""Tier 3: Cross-feature pairwise integration tests (9 tests across F1-F7, offline)."""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path

import pytest

from harness.cli_adapter import (
    IMPL_EVAL_SCRIPT,
    IMPL_EVIDENCE_DIR,
    IMPL_QUESTIONS_PACK,
    IMPL_RUBRIC_MD,
    NotionViewAdapter,
    assert_impl_deliverables_present,
    evaluate_requirements_batched,
    probe_impl_cli_help,
)
from harness.contracts import (
    IN_SCOPE_GIT_FILES,
    LINEAR_BACKLOG_STATE_ID,
    LINEAR_PROTECTED_LABELS,
    NOTION_FROZEN_MIRROR_PAGE_ID,
    NOTION_ONLY_PAGE_ID,
    NOTION_PROJECT_AEGIS_ID,
    NOTION_REQUIREMENTS_DB_ID,
    NOTION_TASKS_VIEW,
    build_independent_jev_state,
    evaluate_requirement_record,
    is_cpq_linear_issue,
    is_excluded_path,
    parse_jev_primitives,
    read_baltic_v2_hash,
    scan_for_secrets_and_db3,
    select_stratified_evaluations,
    validate_cross_links,
    validate_gap_matrix,
    validate_jev_request,
    validate_linear_issue_payload,
    validate_notion_task_row,
    validate_question_pack,
    validate_remediation_log,
    validate_requirements_catalog,
    validate_rubric_markdown,
    validate_scope_decisions,
)
from harness.live_clients import find_forbidden_diff_paths

pytestmark = pytest.mark.tier3


def test_f1_f2_scope_decisions_configure_rubric_pack(
    fixtures_dir: Path, load_fixture_json, require_impl_mode: bool
):
    """T3-01 (F1 x F2): M1 scope decisions align with question pack & rubric thresholds."""
    scope_doc = load_fixture_json("artifacts/scope-decisions-jev.json")
    pack = load_fixture_json("jev/valid-question-pack.json")
    rubric_text = (fixtures_dir / "artifacts" / "backlog-gap-rubric.md").read_text(encoding="utf-8")

    validate_scope_decisions(scope_doc).require_ok()
    validate_question_pack(pack).require_ok()
    validate_rubric_markdown(rubric_text).require_ok()

    # Verify Jev request constructed from pack obeys <=20 questions & <=32KB
    validate_jev_request(
        {
            "model": pack["model"],
            "state": {"scope_decisions": [d["ratified_choice"] for d in scope_doc["decisions"]]},
            "questions": pack["questions"],
        },
        require_escape=True,
    ).require_ok()

    # Script-first invariant holds even if Jev noul is 0.95
    ev = evaluate_requirement_record(
        req={"req_id": "FR-X", "source_file": IN_SCOPE_GIT_FILES[0]},
        linear_matches=[],
        notion_matches=[],
        jev_response={"id": "d-1", "answers": {"backlog_completeness": {"noul": 0.95}}},
        questions=pack["questions"],
    )
    assert ev["linear_status"] == "missing"
    assert ev["notion_status"] == "missing"

    if require_impl_mode:
        real_scope = json.loads(
            (IMPL_EVIDENCE_DIR / "scope-decisions-jev.json").read_text(encoding="utf-8")
        )
        real_pack = json.loads(IMPL_QUESTIONS_PACK.read_text(encoding="utf-8"))
        real_rubric = IMPL_RUBRIC_MD.read_text(encoding="utf-8")

        validate_scope_decisions(real_scope).require_ok()
        validate_question_pack(real_pack).require_ok()
        validate_rubric_markdown(real_rubric).require_ok()

        batched_answers = real_scope["decisions"][0]["response"]["answers"]
        ratified_summary = {
            q_key: ans["choice"] if "choice" in ans else ans.get("noul", ans.get("score"))
            for q_key, ans in batched_answers.items()
        }
        validate_jev_request(
            {
                "model": real_pack["model"],
                "state": {
                    "decision_ids": [d["response"]["id"] for d in real_scope["decisions"]],
                    "scope_decisions": ratified_summary,
                },
                "questions": real_pack["questions"],
            },
            require_escape=True,
        ).require_ok()

        help_proc = probe_impl_cli_help()
        assert help_proc is not None and help_proc.returncode == 0
        val_proc = subprocess.run(
            [
                sys.executable,
                str(IMPL_EVAL_SCRIPT),
                "validate",
                "--questions",
                str(IMPL_QUESTIONS_PACK),
            ],
            capture_output=True,
            text=True,
            timeout=15,
            check=False,
        )
        assert val_proc.returncode == 0, val_proc.stderr
        assert json.loads(val_proc.stdout).get("status") == "valid"

        real_ev = evaluate_requirement_record(
            req={"req_id": "FR-X", "source_file": IN_SCOPE_GIT_FILES[0]},
            linear_matches=[],
            notion_matches=[],
            jev_response={
                "id": "d-real-1",
                "answers": {"linear_presence_and_traceability": {"noul": 0.95}},
            },
            questions=real_pack["questions"],
        )
        assert real_ev["linear_status"] == "missing"
        assert real_ev["notion_status"] == "missing"


def test_f2_f3_rubric_evaluates_real_corpus_boundaries(repo_root: Path, require_impl_mode: bool):
    """T3-02 (F2 x F3): Real repo corpus includes all 38 in-scope files and excludes all 17."""
    missing_in_repo = [rel for rel in IN_SCOPE_GIT_FILES if not (repo_root / rel).is_file()]
    assert missing_in_repo == []

    uat_files = sorted(str(p.relative_to(repo_root)).replace("\\", "/") for p in (repo_root / "tests" / "uat").glob("*.md"))
    db_files = sorted(str(p.relative_to(repo_root)).replace("\\", "/") for p in (repo_root / "docs" / "reference" / "cmano-db").glob("*.md"))
    assert len(uat_files) == 9
    assert len(db_files) == 8
    for excluded in uat_files + db_files:
        assert is_excluded_path(excluded) is True

    # Build catalog covering all 38 real files and validate bounds
    source_files = []
    requirements = []
    for idx, rel in enumerate(IN_SCOPE_GIT_FILES, start=1):
        lines = len((repo_root / rel).read_text(encoding="utf-8").splitlines())
        cat = "A" if rel.startswith("Game-Requirements/requirements/") else (
            "B" if rel.startswith("Game-Requirements/drafts/") else "C"
        )
        source_files.append({"path": rel, "category": cat, "lines": lines, "in_scope": True})
        requirements.append(
            {
                "req_id": f"AEGIS-{idx:03d}",
                "title": f"Requirement from {Path(rel).name}",
                "source_file": rel,
                "source_lines": f"1-{min(5, lines)}",
                "domain_project": "cmano-clone",
                "lifecycle_state": "active",
                "summary": f"Extracted clause from {rel}",
                "acceptance_criteria": ["Verified on disk."],
            }
        )
    catalog = {
        "generated_at": "2026-11-22T23:00:00Z",
        "source_files": source_files,
        "requirements": requirements,
    }
    validate_requirements_catalog(catalog, repo_root=repo_root, require_full_corpus=True).require_ok()

    if require_impl_mode:
        real_rubric = IMPL_RUBRIC_MD.read_text(encoding="utf-8")
        validate_rubric_markdown(real_rubric).require_ok()

        real_catalog = json.loads(
            (IMPL_EVIDENCE_DIR / "requirements-catalog.json").read_text(encoding="utf-8")
        )
        validate_requirements_catalog(
            real_catalog,
            repo_root=repo_root,
            require_full_corpus=True,
            expected_in_scope_files=IN_SCOPE_GIT_FILES,
        ).require_ok()
        assert len(real_catalog["source_files"]) == 39
        assert any(sf.get("category") == "NotionOnly" for sf in real_catalog["source_files"])
        assert any(
            NOTION_ONLY_PAGE_ID in str(r.get("source_file", ""))
            or "Milsim Comparative Research" in str(r.get("source_file", ""))
            for r in real_catalog["requirements"]
        )
        assert all(
            not is_excluded_path(str(r.get("source_file", "")))
            for r in real_catalog["requirements"]
        )


def test_f3_f4_extracted_reqs_batch_eval_preserves_phase_n_and_draft(
    mock_jev_server, load_fixture_json, require_impl_mode: bool, repo_root: Path
):
    """T3-03 (F3 x F4): Batch evaluation preserves draft/deferred_phase_n metadata & balanced summary."""
    catalog = load_fixture_json("artifacts/requirements-catalog.json")
    reqs = catalog["requirements"]
    linear_by_req = {
        "FR-01": [{"id": "DRG-101", "source_path": reqs[0]["source_file"], "acceptance_criteria": ["AC"]}],
        "KCX-01": [],
        "UI-01": [{"id": "DRG-268", "source_path": reqs[2]["source_file"], "acceptance_criteria": ["AC"]}],
        "SWARM-27": [{"id": "DRG-112", "source_path": reqs[3]["source_file"], "acceptance_criteria": ["DRG-47"]}],
        "ENV-01": [{"id": "DRG-377", "source_path": reqs[4]["source_file"], "acceptance_criteria": ["AC"]}],
    }
    notion_by_req = {
        rid: [
            {
                "id": f"n-{rid}",
                "has_icon": True,
                "project_id": NOTION_PROJECT_AEGIS_ID,
                "status": "Not started",
                "note": f"https://linear.app/drgamtd-workspace/issue/DRG-101",
            }
        ]
        for rid in ("FR-01", "UI-01", "SWARM-27", "ENV-01")
    }

    evals = evaluate_requirements_batched(
        reqs,
        linear_by_req=linear_by_req,
        notion_by_req=notion_by_req,
        jev_url=mock_jev_server.decisions_url,
    )
    assert len(evals) == len(reqs)
    for rec in mock_jev_server.requests:
        assert len(rec.raw_bytes) <= 32768
        assert len(rec.body["questions"]) <= 20

    summary = {
        "total_requirements": len(evals),
        "source_files_covered": len({e["source_file"] for e in evals}),
        "linear": {
            "complete": sum(1 for e in evals if e["linear_status"] == "complete"),
            "incomplete": sum(1 for e in evals if e["linear_status"] == "incomplete"),
            "missing": sum(1 for e in evals if e["linear_status"] == "missing"),
        },
        "notion": {
            "complete": sum(1 for e in evals if e["notion_status"] == "complete"),
            "incomplete": sum(1 for e in evals if e["notion_status"] == "incomplete"),
            "missing": sum(1 for e in evals if e["notion_status"] == "missing"),
        },
    }
    matrix = {
        "evaluated_at": "2026-11-22T23:00:00Z",
        "rubric_version": "v1",
        "jev_model": "typesafe/jev-1.14",
        "summary": summary,
        "evaluations": evals,
    }
    validate_gap_matrix(matrix).require_ok()
    # Deferred SWARM-27 tracked in DRG-112 is complete without masquerading as shipped
    swarm_eval = next(e for e in evals if e["req_id"] == "SWARM-27")
    assert swarm_eval["linear_status"] == "complete"
    assert reqs[3]["lifecycle_state"] == "deferred_phase_n"

    if require_impl_mode:
        real_catalog = json.loads(
            (IMPL_EVIDENCE_DIR / "requirements-catalog.json").read_text(encoding="utf-8")
        )
        real_baseline = json.loads(
            (IMPL_EVIDENCE_DIR / "baseline-gap-matrix.json").read_text(encoding="utf-8")
        )
        validate_gap_matrix(
            real_baseline,
            is_final=False,
            catalog=real_catalog,
            expected_in_scope_files=IN_SCOPE_GIT_FILES,
        ).require_ok()

        target_states = ("active", "draft", "deferred_phase_n", "shipped")
        lifecycle_present = {r["lifecycle_state"] for r in real_catalog["requirements"]}
        assert set(target_states).issubset(lifecycle_present)

        sampled_reqs = [
            next(r for r in real_catalog["requirements"] if r["lifecycle_state"] == state)
            for state in target_states
        ]
        req_count_before = len(mock_jev_server.requests)
        real_evals = evaluate_requirements_batched(
            sampled_reqs,
            linear_by_req={r["req_id"]: [] for r in sampled_reqs},
            notion_by_req={r["req_id"]: [] for r in sampled_reqs},
            jev_url=mock_jev_server.decisions_url,
        )
        assert len(real_evals) == 4
        for rec in mock_jev_server.requests[req_count_before:]:
            assert len(rec.raw_bytes) <= 32768
            assert len(rec.body["questions"]) <= 20

        matches_path = IMPL_EVIDENCE_DIR / "backlog-matches.json"
        if matches_path.is_file():
            dry_proc = subprocess.run(
                [
                    sys.executable,
                    str(IMPL_EVAL_SCRIPT),
                    "eval",
                    "--catalog",
                    str(IMPL_EVIDENCE_DIR / "requirements-catalog.json"),
                    "--matches",
                    str(matches_path),
                    "--rubric",
                    str(IMPL_QUESTIONS_PACK),
                    "--repo-root",
                    str(repo_root),
                    "--dry-request",
                ],
                capture_output=True,
                text=True,
                timeout=30,
                check=True,
            )
            dry_payload = json.loads(dry_proc.stdout)
            dry_requests = dry_payload.get("dry_requests") or []
            assert dry_requests
            for item in dry_requests:
                validate_jev_request(item["request"], require_escape=True).require_ok()


def test_f4_f5_gap_matrix_drives_compliant_linear_remediation_plan(
    repo_root: Path, load_fixture_json, require_impl_mode: bool
):
    """T3-04 (F4 x F5): Gap matrix drives compliant Backlog payloads & preserves protected labels."""
    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    dump = load_fixture_json("backlogs/synthetic-linear-dump.json")
    # Confirm P-DRG-6 excluded from inventory
    filtered = [i for i in dump["issues"] if not is_cpq_linear_issue(i)]
    assert all(i["project"] != "P-DRG-6" for i in filtered)

    planned_payloads = []
    for ev in baseline["evaluations"]:
        if ev["linear_status"] == "missing":
            payload = {
                "id": "DRG-NEW-1",
                "project": "P-DRG-1",
                "state": "Backlog",
                "state_id": LINEAR_BACKLOG_STATE_ID,
                "labels": ["type:feature"],
                "is_notion_only": ev["source_file"].startswith("notion://"),
                "description": (
                    f"Source: `{ev['source_file']}`\n"
                    f"Req IDs: {ev['req_id']}\n"
                    "AC: Verifiable requirement acceptance criterion."
                ),
            }
            validate_linear_issue_payload(payload, is_new_gap=True, repo_root=repo_root).require_ok()
            planned_payloads.append(payload)
        elif ev["linear_status"] == "incomplete":
            prior = {"id": ev["linear_matches"][0], "labels": ["hold:owner-go", "NEEDS_OWNER"]}
            patch = {
                "id": ev["linear_matches"][0],
                "project": "Combat Interaction UX",
                "state": "Backlog",
                "labels": ["hold:owner-go", "NEEDS_OWNER", "area:c2"],
                "description": f"Source: `{ev['source_file']}`\nReq IDs: {ev['req_id']}\nAC: Complete.",
            }
            validate_linear_issue_payload(
                patch, is_new_gap=False, prior_issue=prior, repo_root=repo_root
            ).require_ok()
            planned_payloads.append(patch)

    assert len(planned_payloads) == 2

    if require_impl_mode:
        real_baseline = json.loads(
            (IMPL_EVIDENCE_DIR / "baseline-gap-matrix.json").read_text(encoding="utf-8")
        )
        real_rem = json.loads(
            (IMPL_EVIDENCE_DIR / "remediation-log.json").read_text(encoding="utf-8")
        )
        validate_remediation_log(real_rem, real_baseline).require_ok()

        unique_linear_ops: dict[str, dict] = {}
        first_before_by_id: dict[str, dict | None] = {}
        for op in real_rem["operations"]:
            if op.get("target") != "linear":
                continue
            iid = str(op["issue_id"])
            if iid not in first_before_by_id:
                before = op.get("before_state")
                first_before_by_id[iid] = before if isinstance(before, dict) else None
            unique_linear_ops[iid] = op

        assert unique_linear_ops
        for iid, op in unique_linear_ops.items():
            after = dict(op["after_state"])
            after.setdefault("id", iid)
            assert not is_cpq_linear_issue(after)
            action = str(op.get("action", "")).lower()
            prior = first_before_by_id[iid] if action == "update" else None
            validate_linear_issue_payload(
                after,
                is_new_gap=(action == "create"),
                prior_issue=prior,
                repo_root=repo_root,
            ).require_ok()
            if str(after.get("state") or after.get("status") or "") == "Done":
                assert after.get("merged_to_main") is True

        shipped_verified_reqs = {
            str(ev["req_id"])
            for ev in real_baseline["evaluations"]
            if ev.get("sub_status") == "shipped_verified"
        }
        shipped_linear_creates = [
            op
            for op in real_rem["operations"]
            if op.get("target") == "linear"
            and str(op.get("action", "")).lower() == "create"
            and str(op.get("req_id", "")) in shipped_verified_reqs
        ]
        assert shipped_linear_creates == []


def test_f4_f6_notion_view_pagination_and_mirror_safety(
    load_fixture_json, require_impl_mode: bool
):
    """T3-05 (F4 x F6): Notion sql quota falls back to view://, throttles 429, and protects mirror."""
    tasks_fixture = load_fixture_json("backlogs/synthetic-notion-tasks.json")["pages"]
    aegis_rows = [r for r in tasks_fixture if r["project_id"] == NOTION_PROJECT_AEGIS_ID]
    calls = {"view_count": 0}
    sleeps: list[float] = []

    def fetch(mode: str, cursor: str | None):
        if mode == "sql":
            return {"error": "usage_limit_reached", "status_code": 429}
        calls["view_count"] += 1
        if calls["view_count"] == 1:
            return {"status_code": 429, "results": []}
        return {"status_code": 200, "results": aegis_rows, "has_more": False}

    adapter = NotionViewAdapter(fetch, min_interval_s=0.5, sleep_fn=sleeps.append)
    rows = adapter.query_with_fallback(view_url=NOTION_TASKS_VIEW, try_sql_first=True)
    assert len(rows) == 2
    assert adapter.trace.retries_on_429 == 1
    assert adapter.trace.view_urls_used == [NOTION_TASKS_VIEW]
    for row in rows:
        validate_notion_task_row(row).require_ok()

    baseline = load_fixture_json("artifacts/baseline-gap-matrix.json")
    rem_log = load_fixture_json("artifacts/remediation-log.json")
    assert all(
        op.get("database_id") != NOTION_FROZEN_MIRROR_PAGE_ID
        for op in rem_log["operations"]
        if op.get("target") == "notion"
    )
    validate_remediation_log(rem_log, baseline, allow_frozen_mirror_writes=False).require_ok()

    if require_impl_mode:
        real_baseline = json.loads(
            (IMPL_EVIDENCE_DIR / "baseline-gap-matrix.json").read_text(encoding="utf-8")
        )
        real_rem = json.loads(
            (IMPL_EVIDENCE_DIR / "remediation-log.json").read_text(encoding="utf-8")
        )
        notion_ops = [op for op in real_rem["operations"] if op.get("target") == "notion"]
        assert notion_ops
        for op in notion_ops:
            target_db = str(op.get("database_id") or op.get("parent_id") or "")
            assert target_db not in (NOTION_FROZEN_MIRROR_PAGE_ID, NOTION_REQUIREMENTS_DB_ID)
        validate_remediation_log(
            real_rem, real_baseline, allow_frozen_mirror_writes=False
        ).require_ok()

        tasks_by_id: dict[str, dict] = {}
        for op in notion_ops:
            if op.get("surface") != "tasks":
                continue
            row = dict(op["after_state"])
            row.setdefault("id", op["page_id"])
            row.setdefault("url", op.get("url", ""))
            tasks_by_id[str(row["id"])] = row
        assert tasks_by_id
        for row in tasks_by_id.values():
            validate_notion_task_row(row).require_ok()
            note_text = str(row.get("Note") or row.get("note") or "")
            assert len(note_text) <= 280


def test_f5_f6_bidirectional_linear_notion_crosslink_consistency(
    load_fixture_json, require_impl_mode: bool
):
    """T3-06 (F5 x F6): Post-remediation Linear & Notion entries cross-link 1:1 with valid statuses."""
    rem_log = load_fixture_json("artifacts/remediation-log.json")
    linear_issues = []
    notion_rows = []
    for op in rem_log["operations"]:
        if op["target"] == "linear":
            paired_notion = next(
                (n for n in rem_log["operations"] if n["target"] == "notion" and n["req_id"] == op["req_id"]),
                None,
            )
            linear_issues.append(
                {
                    "id": op["issue_id"],
                    "description": f"Notion: {paired_notion['url'] if paired_notion else ''}",
                }
            )
        elif op["target"] == "notion":
            assert op["after_state"]["status"] in {"Not started", "In progress", "Blocked", "Done", "Archived"}
            paired_linear = next(
                (l for l in rem_log["operations"] if l["target"] == "linear" and l["req_id"] == op["req_id"]),
                None,
            )
            note = op["after_state"].get("note") or (
                f"Linear: {paired_linear['url']}" if paired_linear else "Linear: https://linear.app/drgamtd-workspace/issue/DRG-268"
            )
            if op["req_id"] == "UI-01":
                linear_issues.append({"id": "DRG-268", "description": f"Notion: {op['url']}"})
            notion_rows.append(
                {
                    "id": op["page_id"],
                    "url": op["url"],
                    "icon": "🎯",
                    "project_id": NOTION_PROJECT_AEGIS_ID,
                    "status": op["after_state"]["status"],
                    "note": note,
                }
            )

    for nr in notion_rows:
        validate_notion_task_row(nr).require_ok()
    validate_cross_links(linear_issues, notion_rows).require_ok()

    if require_impl_mode:
        real_rem = json.loads(
            (IMPL_EVIDENCE_DIR / "remediation-log.json").read_text(encoding="utf-8")
        )
        lin_by_id: dict[str, dict] = {}
        tasks_by_id: dict[str, dict] = {}
        wiki_by_id: dict[str, dict] = {}
        for op in real_rem["operations"]:
            target = op.get("target")
            if target == "linear":
                after = dict(op["after_state"])
                after.setdefault("id", op["issue_id"])
                lin_by_id[str(after["id"])] = after
            elif target == "notion":
                row = dict(op["after_state"])
                row.setdefault("id", op["page_id"])
                row.setdefault("url", op.get("url", ""))
                if op.get("surface") == "tasks":
                    tasks_by_id[str(row["id"])] = row
                else:
                    wiki_by_id[str(row["id"])] = row

        tasks_rows = list(tasks_by_id.values())
        wiki_rows = list(wiki_by_id.values())
        inv_path = IMPL_EVIDENCE_DIR / "live-backlog-inventory.json"
        if inv_path.is_file():
            inv = json.loads(inv_path.read_text(encoding="utf-8"))
            inv_lin = {
                str(i["id"]): i
                for i in inv.get("linear", {}).get("issues", [])
                if isinstance(i, dict) and i.get("id")
            }
            for row in tasks_rows:
                note_text = str(row.get("Note") or row.get("note") or "")
                for drg_id in re.findall(r"DRG-\d+", note_text):
                    if drg_id not in lin_by_id and drg_id in inv_lin:
                        lin_by_id[drg_id] = inv_lin[drg_id]
            validate_cross_links(
                list(lin_by_id.values()),
                tasks_rows,
                known_notion_rows=wiki_rows,
            ).require_ok()
        else:
            scoped_tasks = [
                row
                for row in tasks_rows
                if any(
                    drg_id in lin_by_id
                    for drg_id in re.findall(
                        r"DRG-\d+", str(row.get("Note") or row.get("note") or "")
                    )
                )
            ]
            validate_cross_links(
                list(lin_by_id.values()),
                scoped_tasks,
                known_notion_rows=wiki_rows,
            ).require_ok()


def test_f1_f7_independent_re_eval_rejects_stale_or_unverified_pass(
    load_fixture_json, require_impl_mode: bool, repo_root: Path
):
    """T3-07 (F1 x F7): Final verifier fails closed on low-conf choice, mechanical miss, or missing file."""
    pack = load_fixture_json("jev/valid-question-pack.json")
    # (a) low-confidence choice (<0.50)
    parsed = parse_jev_primitives(
        {"answers": {"gap_classification": {"choice": "complete", "confidence": 0.41}}},
        pack["questions"],
    )
    assert parsed["gap_classification"]["needs_human_review"] is True

    # (b) failed mechanical pre-gate (notion_matches=[]) with optimistic Jev noul=0.95
    ev = evaluate_requirement_record(
        req={"req_id": "FR-10", "source_file": IN_SCOPE_GIT_FILES[9]},
        linear_matches=[{"id": "DRG-110", "source_path": IN_SCOPE_GIT_FILES[9], "acceptance_criteria": ["AC"]}],
        notion_matches=[],
        jev_response={"id": "d-opt", "answers": {"backlog_completeness": {"noul": 0.95}}},
        questions=pack["questions"],
    )
    assert ev["notion_status"] == "missing"

    # (c) final-gap-matrix claiming missing=0 while catalog omits an expected file
    catalog = load_fixture_json("artifacts/requirements-catalog.json")
    res = validate_requirements_catalog(
        catalog,
        require_full_corpus=True,
        expected_in_scope_files=IN_SCOPE_GIT_FILES,
    )
    assert not res.ok

    if require_impl_mode:
        real_pack = json.loads(IMPL_QUESTIONS_PACK.read_text(encoding="utf-8"))
        real_parsed = parse_jev_primitives(
            {"answers": {"linear_classification": {"choice": "complete", "confidence": 0.41}}},
            real_pack["questions"],
        )
        assert real_parsed["linear_classification"]["needs_human_review"] is True

        real_ev = evaluate_requirement_record(
            req={"req_id": "FR-10", "source_file": IN_SCOPE_GIT_FILES[9]},
            linear_matches=[
                {"id": "DRG-110", "source_path": IN_SCOPE_GIT_FILES[9], "acceptance_criteria": ["AC"]}
            ],
            notion_matches=[],
            jev_response={
                "id": "d-opt",
                "answers": {"linear_presence_and_traceability": {"noul": 0.95}},
            },
            questions=real_pack["questions"],
        )
        assert real_ev["notion_status"] == "missing"

        assert len(assert_impl_deliverables_present(repo_root, include_final=False)) == 7
        final_matrix = IMPL_EVIDENCE_DIR / "final-gap-matrix.json"
        if not final_matrix.exists():
            with pytest.raises(AssertionError, match="missing"):
                assert_impl_deliverables_present(repo_root, include_final=True)
        else:
            real_final = json.loads(final_matrix.read_text(encoding="utf-8"))
            real_baseline = json.loads(
                (IMPL_EVIDENCE_DIR / "baseline-gap-matrix.json").read_text(encoding="utf-8")
            )
            real_catalog = json.loads(
                (IMPL_EVIDENCE_DIR / "requirements-catalog.json").read_text(encoding="utf-8")
            )
            owner_held = [
                item["req_id"]
                for item in real_final.get("evaluations", [])
                if any(lbl in json.dumps(item) for lbl in LINEAR_PROTECTED_LABELS)
            ]
            validate_gap_matrix(
                real_final,
                is_final=True,
                baseline_matrix=real_baseline,
                catalog=real_catalog,
                expected_in_scope_files=IN_SCOPE_GIT_FILES,
                allowed_owner_hold_req_ids=owner_held,
            ).require_ok()


def test_f5_f7_secret_redaction_and_bridge_isolation_across_pipeline(
    repo_root: Path, fixtures_dir: Path, mock_jev_server, require_impl_mode: bool
):
    """T3-08 (F5 x F7): Pipeline blocks canary secrets/.db3 and verifies zero C#/DelegationBridge edits."""
    canary_secret = "sk-or-v1-dummycanarysecret98765"
    tainted_artifact = json.dumps({"note": canary_secret, "db": "cmo.db3"})
    res = scan_for_secrets_and_db3(tainted_artifact, extra_forbidden_values=[canary_secret])
    assert not res.ok
    assert len(mock_jev_server.requests) == 0

    for artifact in (fixtures_dir / "artifacts").glob("*.json"):
        scan_for_secrets_and_db3(
            artifact.read_text(encoding="utf-8"),
            extra_forbidden_values=[canary_secret],
        ).require_ok()

    # Check live git diff on protected hard-invariant paths has 0 changes
    proc = subprocess.run(
        ["git", "diff", "--name-only", "HEAD"],
        cwd=str(repo_root),
        capture_output=True,
        text=True,
        check=True,
    )
    changed_files = [line.strip() for line in proc.stdout.splitlines() if line.strip()]
    assert find_forbidden_diff_paths(changed_files, include_all_src=False) == []

    # Synthetic diff-list check verifies unauthorized src/ edits are rejected when enforcing full isolation
    assert find_forbidden_diff_paths(
        ["src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs"],
        include_all_src=True,
    ) == ["src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs"]

    # Verify zero .cs/.csproj/.db3 files and zero Baltic v2 hash literals in E2E suite
    e2e_root = repo_root / "tests" / "e2e" / "backlog_gap"
    assert [p for p in e2e_root.rglob("*") if p.is_file() and p.suffix in (".cs", ".csproj", ".db3")] == []
    baltic_hash = read_baltic_v2_hash(repo_root)
    for p in e2e_root.rglob("*"):
        if p.is_file() and p.suffix in (".md", ".txt"):
            assert baltic_hash not in p.read_text(encoding="utf-8", errors="ignore")

    if require_impl_mode:
        scan_targets: list[tuple[Path, str]] = [
            (IMPL_RUBRIC_MD, "doc"),
            (IMPL_QUESTIONS_PACK, "evidence"),
            *((p, "evidence") for p in sorted(IMPL_EVIDENCE_DIR.rglob("*")) if p.is_file()),
            *(
                (p, "test_code" if p.name.startswith("test_") else "code")
                for p in sorted((repo_root / "tools" / "jev").glob("*.py"))
                if p.is_file()
            ),
        ]
        for target_path, scan_mode in scan_targets:
            scan_for_secrets_and_db3(
                target_path.read_text(encoding="utf-8", errors="ignore"),
                extra_forbidden_values=[canary_secret],
                mode=scan_mode,
            ).require_ok()
        assert find_forbidden_diff_paths(changed_files, include_all_src=True) == []


def test_f1_f4_f6_f7_stratified_non_circular_state_and_scoped_crosslinks(
    load_fixture_json, require_impl_mode: bool
):
    """T3-09 (F1 x F4 x F6 x F7): Stratified non-circular Jev state & known_notion_rows scoping."""
    catalog = load_fixture_json("artifacts/requirements-catalog.json")
    final = load_fixture_json("artifacts/final-gap-matrix.json")
    rem_log = load_fixture_json("artifacts/remediation-log.json")
    linear_dump = load_fixture_json("backlogs/synthetic-linear-dump.json")["issues"]
    notion_dump = load_fixture_json("backlogs/synthetic-notion-tasks.json")["pages"]

    sample = select_stratified_evaluations(final["evaluations"], catalog, rem_log, max_samples=10)
    sampled_ids = {ev["req_id"] for ev in sample}
    # All 4 categories in final-gap-matrix (A, B, C, NotionOnly) are covered deterministically
    assert sampled_ids == {"FR-01", "KCX-01", "UI-01", "ENV-01"}

    req_by_id = {r["req_id"]: r for r in catalog["requirements"]}
    state = build_independent_jev_state(req_by_id["FR-01"], [linear_dump[0]], [notion_dump[0]])
    assert "linear_status" not in state and "notion_status" not in state
    assert state["requirement"]["req_id"] == "FR-01"
    assert state["requirement"]["acceptance_criteria"]
    assert state["linear_records"][0]["id"] == "DRG-101"
    assert state["notion_records"][0]["id"] == "11111111-2222-3333-4444-555566667777"

    # Scoped cross-link check: pre-existing Notion page in known_notion_rows allows DRG-101->1111 link
    # without forcing DRG-* checks on unrelated historical Notion rows
    unrelated_historical_notion = {"id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", "note": "Historical note"}
    remediated_notion = {
        "id": "33333333-2222-3333-4444-555566667777",
        "url": "https://www.notion.so/33333333222233334444555566667777",
        "note": "Linear: https://linear.app/drgamtd-workspace/issue/DRG-501",
    }
    remediated_linear = {
        "id": "DRG-501",
        "description": "Notion: https://www.notion.so/33333333222233334444555566667777",
    }
    validate_cross_links(
        [remediated_linear, linear_dump[0]],
        [remediated_notion],
        known_notion_rows=[notion_dump[0], unrelated_historical_notion],
    ).require_ok()

    if require_impl_mode:
        real_catalog = json.loads(
            (IMPL_EVIDENCE_DIR / "requirements-catalog.json").read_text(encoding="utf-8")
        )
        real_rem = json.loads(
            (IMPL_EVIDENCE_DIR / "remediation-log.json").read_text(encoding="utf-8")
        )
        real_pack = json.loads(IMPL_QUESTIONS_PACK.read_text(encoding="utf-8"))
        final_matrix = IMPL_EVIDENCE_DIR / "final-gap-matrix.json"
        matrix_path = (
            final_matrix
            if final_matrix.exists()
            else (IMPL_EVIDENCE_DIR / "baseline-gap-matrix.json")
        )
        real_matrix = json.loads(matrix_path.read_text(encoding="utf-8"))

        real_sample = select_stratified_evaluations(
            real_matrix["evaluations"], real_catalog, real_rem, max_samples=10
        )
        assert len(real_sample) >= 4

        sf_cat = {
            str(sf["path"]).replace("\\", "/"): sf.get("category")
            for sf in real_catalog["source_files"]
        }
        real_req_by_id = {r["req_id"]: r for r in real_catalog["requirements"]}
        sampled_cats = set()
        for ev in real_sample:
            req = real_req_by_id[ev["req_id"]]
            src = str(req.get("source_file", "")).replace("\\", "/")
            cat = sf_cat.get(src)
            if not cat and (src.startswith("notion://") or NOTION_ONLY_PAGE_ID in src):
                cat = "NotionOnly"
            sampled_cats.add(cat)
        assert {"A", "B", "C", "NotionOnly"}.issubset(sampled_cats)

        lin_after_by_id: dict[str, dict] = {}
        not_after_by_id: dict[str, dict] = {}
        for op in real_rem["operations"]:
            if op.get("target") == "linear" and op.get("issue_id"):
                rec = dict(op["after_state"])
                rec.setdefault("id", op["issue_id"])
                lin_after_by_id[str(op["issue_id"])] = rec
            elif op.get("target") == "notion" and op.get("page_id"):
                rec = dict(op["after_state"])
                rec.setdefault("id", op["page_id"])
                not_after_by_id[str(op["page_id"])] = rec

        for ev in real_sample:
            req = real_req_by_id[ev["req_id"]]
            matched_lin = [
                lin_after_by_id[lid]
                for lid in ev.get("linear_matches", [])
                if lid in lin_after_by_id
            ]
            matched_not = [
                not_after_by_id[nid]
                for nid in ev.get("notion_matches", [])
                if nid in not_after_by_id
            ]
            ind_state = build_independent_jev_state(req, matched_lin, matched_not, evaluation=ev)
            assert "linear_status" not in ind_state and "notion_status" not in ind_state
            validate_jev_request(
                {
                    "model": real_pack["model"],
                    "state": ind_state,
                    "questions": real_pack["questions"],
                },
                require_escape=True,
            ).require_ok()
            if final_matrix.exists() and ev.get("sub_status") == "shipped_verified":
                assert ind_state.get("requirement", {}).get("verification_evidence") is not None
