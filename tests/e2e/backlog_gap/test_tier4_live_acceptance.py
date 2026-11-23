"""Tier 4: Live real-world acceptance tests (6 tests, gated by @pytest.mark.live_backlog)."""

from __future__ import annotations

import json
import re
from pathlib import Path
from typing import Any, Mapping

import pytest

from harness.cli_adapter import (
    IMPL_EVIDENCE_DIR,
    IMPL_QUESTIONS_PACK,
    IMPL_RUBRIC_MD,
    assert_impl_deliverables_present,
)
from harness.contracts import (
    CHOICE_HUMAN_REVIEW_FLOOR,
    IN_SCOPE_GIT_FILES,
    LINEAR_PROTECTED_LABELS,
    NOTION_ONLY_PAGE_ID,
    NOTION_TASKS_COLLECTION,
    NOTION_TASKS_DB_ID,
    NOUL_PASS_THRESHOLD,
    SCORE_COMPLETE_THRESHOLD,
    build_independent_jev_state,
    is_cpq_linear_issue,
    is_excluded_path,
    parse_jev_primitives,
    scan_for_secrets_and_db3,
    select_stratified_evaluations,
    validate_cross_links,
    validate_gap_matrix,
    validate_linear_issue_payload,
    validate_markdown_parity,
    validate_notion_task_row,
    validate_question_pack,
    validate_remediation_log,
    validate_requirements_catalog,
    validate_rubric_markdown,
    validate_scope_decisions,
)
from harness.live_clients import (
    assert_fresh_snapshot,
    fetch_notion_page_readonly,
    load_env_map,
    post_live_openrouter_jev,
    query_linear_readonly_graphql,
    verify_dotnet_non_interference,
)

pytestmark = [pytest.mark.tier4, pytest.mark.live_backlog]


def _normalize_notion_id(raw: str) -> str:
    m = re.search(r"([0-9a-fA-F]{32})", raw.replace("-", ""))
    if m:
        return m.group(1).lower()
    return raw.replace("-", "").lower()


def _load_linear_issues_readonly(
    evidence_dir: Path,
    rem_log: Mapping[str, Any],
    env_map: Mapping[str, str],
) -> dict[str, dict[str, Any]]:
    """Shared read-only Linear loader: GraphQL when LINEAR_API_KEY is set, else fresh MCP snapshot."""
    linear_key = env_map.get("LINEAR_API_KEY", "").strip()
    issues_by_id: dict[str, dict[str, Any]] = {}

    if linear_key:
        query = """
        query AegisTeamIssues($after: String) {
          issues(first: 100, after: $after, filter: { team: { key: { eq: "DRG" } } }) {
            nodes {
              identifier
              title
              url
              description
              state { id name }
              project { id name }
              labels { nodes { name } }
              attachments { nodes { url title } }
            }
            pageInfo { hasNextPage endCursor }
          }
        }
        """
        after = None
        while True:
            data = query_linear_readonly_graphql(linear_key, query, {"after": after})
            conn = data["data"]["issues"]
            for node in conn["nodes"]:
                issues_by_id[node["identifier"]] = {
                    "id": node["identifier"],
                    "title": node["title"],
                    "url": node.get("url") or "",
                    "description": node.get("description") or "",
                    "state": (node.get("state") or {}).get("name"),
                    "state_id": (node.get("state") or {}).get("id"),
                    "project": (node.get("project") or {}).get("name") or "Unassigned",
                    "labels": [lbl["name"] for lbl in (node.get("labels") or {}).get("nodes", [])],
                    "attachments": [
                        {"url": att.get("url", ""), "title": att.get("title", "")}
                        for att in (node.get("attachments") or {}).get("nodes", [])
                    ],
                }
            if not conn["pageInfo"]["hasNextPage"]:
                break
            after = conn["pageInfo"]["endCursor"]
    else:
        snap = assert_fresh_snapshot(
            evidence_dir / "live-linear-snapshot.json",
            min_timestamp_iso=rem_log["completed_at"],
        )
        assert snap.get("source") == "linear-mcp"
        assert snap.get("team") == "DRG"
        for node in snap.get("issues", []):
            if node.get("id"):
                issues_by_id[str(node["id"])] = node

    return issues_by_id


def _load_notion_snapshot_pages(
    evidence_dir: Path,
    rem_log: Mapping[str, Any],
) -> dict[str, dict[str, Any]]:
    snapshot_path = evidence_dir / "live-notion-snapshot.json"
    snap_pages: dict[str, dict[str, Any]] = {}
    if snapshot_path.is_file():
        snap = assert_fresh_snapshot(snapshot_path, min_timestamp_iso=rem_log["completed_at"])
        for p in snap.get("pages", []):
            if p.get("id"):
                snap_pages[_normalize_notion_id(str(p["id"]))] = p
    return snap_pages


def _resolve_notion_row(
    page_ref: str,
    snap_pages_by_norm: Mapping[str, dict[str, Any]],
    notion_key: str,
    fallback_url: str = "",
) -> dict[str, Any] | None:
    norm_id = _normalize_notion_id(page_ref)
    row = snap_pages_by_norm.get(norm_id)
    if row is not None or not notion_key or len(norm_id) != 32:
        return row
    dashed = f"{norm_id[:8]}-{norm_id[8:12]}-{norm_id[12:16]}-{norm_id[16:20]}-{norm_id[20:]}"
    status_code, raw_page = fetch_notion_page_readonly(notion_key, dashed)
    if status_code == 200 and raw_page is not None:
        props = raw_page.get("properties", {})
        proj_rel = (props.get("Project") or {}).get("relation") or []
        status_obj = (props.get("Status") or {}).get("status") or {}
        note_rt = (props.get("Note") or {}).get("rich_text") or []
        return {
            "id": raw_page.get("id", dashed),
            "url": raw_page.get("url", fallback_url),
            "icon": raw_page.get("icon"),
            "project_id": proj_rel[0]["id"] if proj_rel else None,
            "status": status_obj.get("name"),
            "note": "".join(t.get("plain_text", "") for t in note_rt),
        }
    return None


def test_live_corpus_100pct_coverage_and_zero_cpq_contamination(repo_root: Path):
    """T4-01: Live requirements-catalog & final-gap-matrix cover 38/38 files + NotionOnly with 0 gaps."""
    assert_impl_deliverables_present(repo_root, include_final=True)

    catalog = json.loads((IMPL_EVIDENCE_DIR / "requirements-catalog.json").read_text(encoding="utf-8"))
    baseline = json.loads((IMPL_EVIDENCE_DIR / "baseline-gap-matrix.json").read_text(encoding="utf-8"))
    final = json.loads((IMPL_EVIDENCE_DIR / "final-gap-matrix.json").read_text(encoding="utf-8"))
    final_md = (IMPL_EVIDENCE_DIR / "final-gap-matrix.md").read_text(encoding="utf-8")
    scope_doc = json.loads((IMPL_EVIDENCE_DIR / "scope-decisions-jev.json").read_text(encoding="utf-8"))
    rubric_md = IMPL_RUBRIC_MD.read_text(encoding="utf-8")

    validate_scope_decisions(scope_doc).require_ok()
    validate_rubric_markdown(rubric_md).require_ok()
    validate_requirements_catalog(
        catalog, repo_root=repo_root, require_full_corpus=True, expected_in_scope_files=IN_SCOPE_GIT_FILES
    ).require_ok()

    # Notion-only Milsim Comparative Research must also be represented
    assert any(
        NOTION_ONLY_PAGE_ID in str(r.get("source_file", ""))
        or "Milsim Comparative Research" in str(r.get("source_file", ""))
        for r in catalog["requirements"]
    )
    for r in catalog["requirements"]:
        assert not is_excluded_path(r["source_file"])

    owner_held = [
        ev["req_id"]
        for ev in final["evaluations"]
        if any(lbl in json.dumps(ev) for lbl in LINEAR_PROTECTED_LABELS)
    ]
    validate_gap_matrix(
        final,
        is_final=True,
        baseline_matrix=baseline,
        catalog=catalog,
        expected_in_scope_files=IN_SCOPE_GIT_FILES,
        allowed_owner_hold_req_ids=owner_held,
    ).require_ok()
    validate_markdown_parity(final, final_md).require_ok()


def test_live_jev_independent_re_evaluation_matches_final_matrix(repo_root: Path):
    """T4-02: Live read-only OpenRouter Jev re-evaluation on non-circular stratified sample."""
    assert_impl_deliverables_present(repo_root, include_final=True)
    env_map = load_env_map(repo_root)
    api_key = env_map.get("OPENROUTER_API_KEY", "").strip()
    assert api_key, "OPENROUTER_API_KEY must be set in env or .env for Tier 4 live Jev check"
    notion_key = env_map.get("NOTION_API_KEY", "").strip()

    pack = json.loads(IMPL_QUESTIONS_PACK.read_text(encoding="utf-8"))
    validate_question_pack(pack).require_ok()
    catalog = json.loads((IMPL_EVIDENCE_DIR / "requirements-catalog.json").read_text(encoding="utf-8"))
    baseline = json.loads((IMPL_EVIDENCE_DIR / "baseline-gap-matrix.json").read_text(encoding="utf-8"))
    rem_log = json.loads((IMPL_EVIDENCE_DIR / "remediation-log.json").read_text(encoding="utf-8"))
    validate_remediation_log(rem_log, baseline).require_ok()
    final = json.loads((IMPL_EVIDENCE_DIR / "final-gap-matrix.json").read_text(encoding="utf-8"))

    issues_by_id = _load_linear_issues_readonly(IMPL_EVIDENCE_DIR, rem_log, env_map)
    snap_pages = _load_notion_snapshot_pages(IMPL_EVIDENCE_DIR, rem_log)
    req_by_id = {r["req_id"]: r for r in catalog["requirements"]}

    sample = select_stratified_evaluations(final["evaluations"], catalog, rem_log, max_samples=10)
    assert sample, "final-gap-matrix.json has no evaluations"

    for ev in sample:
        req = req_by_id[ev["req_id"]]
        matched_linear = [
            issues_by_id[lid] for lid in ev.get("linear_matches", []) if lid in issues_by_id
        ]
        matched_notion = []
        for nid in ev.get("notion_matches", []):
            row = _resolve_notion_row(str(nid), snap_pages, notion_key)
            if row is not None:
                matched_notion.append(row)

        state_obj = build_independent_jev_state(req, matched_linear, matched_notion, evaluation=ev)
        assert "linear_status" not in state_obj and "notion_status" not in state_obj

        payload = {
            "model": "typesafe/jev-1.14",
            "state": state_obj,
            "questions": pack["questions"],
        }
        resp = post_live_openrouter_jev(api_key, payload)
        assert "answers" in resp and isinstance(resp["answers"], dict)

        parsed = parse_jev_primitives(resp, pack["questions"])
        assert set(pack["questions"]).issubset(parsed), (
            f"{ev['req_id']}: missing Jev answers for {sorted(set(pack['questions']) - set(parsed))}"
        )
        is_both_complete = (
            ev.get("linear_status") == "complete" and ev.get("notion_status") == "complete"
        )
        for q_id, q_spec in pack["questions"].items():
            ans = parsed[q_id]
            if ans["type"] == "noul":
                if is_both_complete:
                    assert ans["noul"] >= NOUL_PASS_THRESHOLD, (
                        f"{ev['req_id']}: Jev {q_id} noul={ans['noul']:.3f} < {NOUL_PASS_THRESHOLD}"
                    )
            elif ans["type"] == "choice":
                assert ans["choice"] in q_spec["criteria"], (
                    f"{ev['req_id']}: invalid choice {ans['choice']!r}"
                )
                assert ans["confidence"] >= CHOICE_HUMAN_REVIEW_FLOOR, (
                    f"{ev['req_id']}: Jev {q_id} confidence={ans['confidence']:.3f} < {CHOICE_HUMAN_REVIEW_FLOOR}"
                )
                if is_both_complete:
                    if q_id == "recommended_remediation":
                        assert ans["choice"] == "none", (
                            f"{ev['req_id']}: Jev discrepancy {q_id}={ans['choice']!r} vs complete/complete (expected 'none')"
                        )
                    else:
                        assert ans["choice"] not in ("missing", "incomplete", "other"), (
                            f"{ev['req_id']}: Jev discrepancy {q_id}={ans['choice']!r} vs complete/complete"
                        )
            elif ans["type"] == "score":
                levels = q_spec["criteria"]
                assert 0 <= ans["discrete_rank"] < len(levels)
                assert 0.0 <= ans["score"] <= max(len(levels) - 1, 0)
                if is_both_complete:
                    assert ans["score"] >= SCORE_COMPLETE_THRESHOLD, (
                        f"{ev['req_id']}: Jev {q_id} score={ans['score']:.3f} < {SCORE_COMPLETE_THRESHOLD}"
                    )


def test_live_linear_remediation_and_governance_invariants(repo_root: Path):
    """T4-03: Live Linear GraphQL (if key set) or fresh MCP snapshot verifies all remediation invariants."""
    assert_impl_deliverables_present(repo_root, include_final=True)
    rem_log = json.loads((IMPL_EVIDENCE_DIR / "remediation-log.json").read_text(encoding="utf-8"))
    baseline = json.loads((IMPL_EVIDENCE_DIR / "baseline-gap-matrix.json").read_text(encoding="utf-8"))
    validate_remediation_log(rem_log, baseline).require_ok()

    env_map = load_env_map(repo_root)
    issues_by_id = _load_linear_issues_readonly(IMPL_EVIDENCE_DIR, rem_log, env_map)

    for op in rem_log["operations"]:
        if op.get("target") != "linear":
            continue
        drg_id = op["issue_id"]
        assert drg_id in issues_by_id, f"Remediated Linear issue {drg_id} missing from live state"
        issue = issues_by_id[drg_id]
        assert not is_cpq_linear_issue(issue)
        prior_issue = (
            op.get("before_state")
            if op.get("action") == "update" and isinstance(op.get("before_state"), Mapping)
            else None
        )
        validate_linear_issue_payload(
            issue,
            is_new_gap=(op.get("action") == "create"),
            prior_issue=prior_issue,
            repo_root=repo_root,
        ).require_ok()

    # Protected owner-hold labels preserved on known held issues
    for held_id in ("DRG-231", "DRG-236", "DRG-268", "DRG-269", "DRG-347", "DRG-747", "DRG-778"):
        if held_id in issues_by_id:
            lbls = set(issues_by_id[held_id].get("labels", []))
            assert lbls & LINEAR_PROTECTED_LABELS, f"{held_id} lost protected owner label"


def test_live_notion_remediation_and_crosslinks(repo_root: Path):
    """T4-04: Live Notion REST and/or fresh MCP snapshot verifies Tasks DB & bidirectional links."""
    assert_impl_deliverables_present(repo_root, include_final=True)
    rem_log = json.loads((IMPL_EVIDENCE_DIR / "remediation-log.json").read_text(encoding="utf-8"))
    baseline = json.loads((IMPL_EVIDENCE_DIR / "baseline-gap-matrix.json").read_text(encoding="utf-8"))
    validate_remediation_log(rem_log, baseline).require_ok()
    final = json.loads((IMPL_EVIDENCE_DIR / "final-gap-matrix.json").read_text(encoding="utf-8"))

    env_map = load_env_map(repo_root)
    notion_key = env_map.get("NOTION_API_KEY", "").strip()
    issues_by_id = _load_linear_issues_readonly(IMPL_EVIDENCE_DIR, rem_log, env_map)
    snap_pages = _load_notion_snapshot_pages(IMPL_EVIDENCE_DIR, rem_log)

    verified_notion_rows: list[dict[str, Any]] = []
    verified_wiki_rows: list[dict[str, Any]] = []
    remediated_linear_ids: set[str] = set()
    remediated_req_ids: set[str] = set()

    for op in rem_log["operations"]:
        if op.get("req_id"):
            remediated_req_ids.add(str(op["req_id"]))
        if op.get("target") == "linear" and op.get("issue_id"):
            remediated_linear_ids.add(str(op["issue_id"]))
            continue
        if op.get("target") != "notion":
            continue
        page_id = op["page_id"]
        row = _resolve_notion_row(page_id, snap_pages, notion_key, fallback_url=str(op.get("url", "")))
        assert row is not None, (
            f"Notion page {page_id} could not be verified via live REST or fresh live-notion-snapshot.json"
        )
        is_tasks_db = (
            op.get("database_id") in (NOTION_TASKS_DB_ID, NOTION_TASKS_COLLECTION)
        ) or (
            op.get("database_id") is None and op.get("surface") != "wiki_drafts"
        )
        if is_tasks_db:
            validate_notion_task_row(row).require_ok()
            verified_notion_rows.append(row)
            note = str(row.get("note") or row.get("Note") or "")
            for m in re.findall(r"DRG-\d+", note):
                remediated_linear_ids.add(m)
        else:
            if row.get("icon") is not None:
                assert bool(row["icon"]), f"Notion wiki page {page_id} has invalid empty icon"
            verified_wiki_rows.append(row)

    all_known_notion_by_norm: dict[str, dict[str, Any]] = dict(snap_pages)
    for row in (*verified_notion_rows, *verified_wiki_rows):
        if row.get("id"):
            all_known_notion_by_norm[_normalize_notion_id(str(row["id"]))] = row

    # Include paired cross-linked issues from final-gap-matrix.json for remediated reqs or known Notion pages
    for ev in final.get("evaluations", []):
        ev_req = str(ev.get("req_id", ""))
        ev_n_matches = [_normalize_notion_id(str(n)) for n in ev.get("notion_matches", [])]
        if ev_req in remediated_req_ids or any(nid in all_known_notion_by_norm for nid in ev_n_matches):
            for lid in ev.get("linear_matches", []):
                if lid:
                    remediated_linear_ids.add(str(lid))

    scoped_linear_issues: list[dict[str, Any]] = []
    for lid in sorted(remediated_linear_ids):
        assert lid in issues_by_id, f"Cross-linked Linear issue {lid} missing from live Linear state"
        li = issues_by_id[lid]
        scoped_linear_issues.append(li)
        # Resolve any referenced Notion pages via REST if not in snapshot
        if notion_key:
            li_blob = json.dumps(li)
            for raw_hex in re.findall(r"notion\.so/[^\s\"']*?([0-9a-fA-F]{32})", li_blob):
                norm_hex = raw_hex.lower()
                if norm_hex not in all_known_notion_by_norm:
                    fetched = _resolve_notion_row(norm_hex, all_known_notion_by_norm, notion_key)
                    if fetched is not None:
                        all_known_notion_by_norm[norm_hex] = fetched

    if rem_log["operations"]:
        assert scoped_linear_issues or verified_notion_rows, (
            "remediation-log.json has operations but no Linear/Notion pairs resolved for cross-link check"
        )
    validate_cross_links(
        scoped_linear_issues,
        verified_notion_rows,
        known_notion_rows=list(all_known_notion_by_norm.values()),
    ).require_ok()


def test_live_dotnet_and_hard_invariants_unharmed(repo_root: Path):
    """T4-05: C# src/, DelegationBridge.cs, CatalogWriteGate, golden hash, and dotnet test pass."""
    result = verify_dotnet_non_interference(repo_root, run_tests=True)
    assert result["forbidden_touches"] == []


def test_live_zero_secret_and_db3_leakage_in_artifacts(repo_root: Path):
    """T4-06: Zero raw .env secrets, Bearer tokens, or .db3 extracts appear in M1-M4 artifacts."""
    assert_impl_deliverables_present(repo_root, include_final=True)
    env_map = load_env_map(repo_root)
    secret_values = [
        v
        for k, v in env_map.items()
        if any(tok in k.upper() for tok in ("KEY", "TOKEN", "SECRET")) and len(v) >= 8
    ]

    scan_targets: list[tuple[Path, str]] = [
        (IMPL_RUBRIC_MD, "doc"),
        (IMPL_QUESTIONS_PACK, "evidence"),
        *((p, "evidence") for p in sorted(IMPL_EVIDENCE_DIR.rglob("*"))),
        *(
            (p, "test_code" if p.name.startswith("test_") else "code")
            for p in sorted((repo_root / "tools" / "jev").glob("*.py"))
        ),
    ]
    for path, mode in scan_targets:
        if path.is_file():
            text = path.read_text(encoding="utf-8", errors="ignore")
            res = scan_for_secrets_and_db3(
                text, extra_forbidden_values=secret_values, mode=mode
            )
            if not res.ok:
                raise AssertionError(
                    f"Contract validation failed ({path}, mode={mode}):\n- "
                    + "\n- ".join(res.errors)
                )
