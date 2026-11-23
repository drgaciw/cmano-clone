"""Strictly read-only live API and snapshot verifiers for Tier 4 acceptance."""

from __future__ import annotations

import json
import os
import re
import shutil
import subprocess
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Mapping, Sequence

from .contracts import MAX_BODY_BYTES, MAX_QUESTIONS, read_baltic_v2_hash, validate_jev_request

NOTION_API_VERSION = "2022-06-28"


def load_env_map(repo_root: Path) -> dict[str, str]:
    """Read env + .env keys into memory without ever printing or mutating os.environ."""
    env_map: dict[str, str] = {}
    env_file = repo_root / ".env"
    if env_file.is_file():
        for line in env_file.read_text(encoding="utf-8").splitlines():
            stripped = line.strip()
            if not stripped or stripped.startswith("#") or "=" not in stripped:
                continue
            k, _, v = stripped.partition("=")
            k = k.strip()
            if k:
                env_map[k] = v.strip().strip("'").strip('"')
    for k, v in os.environ.items():
        if v:
            env_map[k] = v
    return env_map


def parse_iso_utc(ts: str) -> datetime:
    cleaned = ts.strip()
    if cleaned.endswith("Z"):
        cleaned = cleaned[:-1] + "+00:00"
    dt = datetime.fromisoformat(cleaned)
    if dt.tzinfo is None:
        dt = dt.replace(tzinfo=timezone.utc)
    return dt


def assert_fresh_snapshot(
    snapshot_path: Path,
    *,
    min_timestamp_iso: str | None = None,
    max_age_hours: float = 24.0,
) -> dict[str, Any]:
    if not snapshot_path.is_file():
        raise AssertionError(f"Required live MCP snapshot missing: {snapshot_path}")
    data = json.loads(snapshot_path.read_text(encoding="utf-8"))
    captured_raw = data.get("captured_at")
    if not isinstance(captured_raw, str) or not captured_raw.strip():
        raise AssertionError(f"Snapshot missing captured_at: {snapshot_path}")
    captured_dt = parse_iso_utc(captured_raw)
    age_s = (datetime.now(timezone.utc) - captured_dt).total_seconds()
    if age_s > max_age_hours * 3600:
        raise AssertionError(f"Snapshot stale ({age_s / 3600:.1f}h > {max_age_hours}h): {snapshot_path}")
    if min_timestamp_iso:
        min_dt = parse_iso_utc(min_timestamp_iso)
        if captured_dt < min_dt:
            raise AssertionError(
                f"Snapshot captured_at ({captured_raw}) predates remediation completion ({min_timestamp_iso})"
            )
    return data


def post_live_openrouter_jev(
    api_key: str,
    payload: Mapping[str, Any],
    *,
    timeout_s: float = 25.0,
) -> dict[str, Any]:
    """Read-only POST to OpenRouter Jev decision endpoint (fallback systemone)."""
    res = validate_jev_request(payload)
    res.require_ok()
    assert len(payload.get("questions", {})) <= MAX_QUESTIONS
    raw = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    assert len(raw) <= MAX_BODY_BYTES

    endpoints = [
        ("https://openrouter.ai/api/alpha/decisions", payload.get("model") or "typesafe/jev-1.14"),
        ("https://openrouter.ai/api/v1/systemone", "typesafe/jev-1.13"),
    ]
    last_err: Exception | None = None
    for url, model in endpoints:
        body = {**payload, "model": model}
        req = urllib.request.Request(
            url,
            data=json.dumps(body, ensure_ascii=False).encode("utf-8"),
            headers={
                "Authorization": f"Bearer {api_key}",
                "Content-Type": "application/json",
                "Accept": "application/json",
            },
            method="POST",
        )
        try:
            with urllib.request.urlopen(req, timeout=timeout_s) as resp:
                return json.loads(resp.read().decode("utf-8"))
        except urllib.error.HTTPError as exc:
            last_err = exc
            if exc.code in (404, 405, 422):
                continue
            raise
    assert last_err is not None
    raise last_err


def query_linear_readonly_graphql(
    api_key: str,
    query: str,
    variables: Mapping[str, Any] | None = None,
    *,
    timeout_s: float = 20.0,
) -> dict[str, Any]:
    """Strictly read-only GraphQL query against Linear API."""
    if re.search(r"\bmutation\b", query, re.IGNORECASE):
        raise ValueError("Tier 4 Linear client strictly forbids GraphQL mutations")
    req = urllib.request.Request(
        "https://api.linear.app/graphql",
        data=json.dumps({"query": query, "variables": dict(variables or {})}).encode("utf-8"),
        headers={
            "Authorization": api_key,
            "Content-Type": "application/json",
            "Accept": "application/json",
        },
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=timeout_s) as resp:
        return json.loads(resp.read().decode("utf-8"))


def fetch_notion_page_readonly(
    api_key: str,
    page_id: str,
    *,
    min_interval_s: float = 0.5,
    timeout_s: float = 15.0,
) -> tuple[int, dict[str, Any] | None]:
    """Strictly read-only GET /v1/pages/{id} throttled <= 2 req/s with 429 backoff."""
    url = f"https://api.notion.com/v1/pages/{page_id}"
    for attempt in range(3):
        time.sleep(min_interval_s)
        req = urllib.request.Request(
            url,
            headers={
                "Authorization": f"Bearer {api_key}",
                "Notion-Version": NOTION_API_VERSION,
                "Accept": "application/json",
            },
            method="GET",
        )
        try:
            with urllib.request.urlopen(req, timeout=timeout_s) as resp:
                return resp.status, json.loads(resp.read().decode("utf-8"))
        except urllib.error.HTTPError as exc:
            if exc.code == 429 and attempt < 2:
                time.sleep(1.0 * (2**attempt))
                continue
            return exc.code, None
    return 429, None


def resolve_dotnet_executable() -> Path:
    found = shutil.which("dotnet")
    if found:
        return Path(found)
    home_dotnet = Path.home() / ".dotnet" / "dotnet"
    if home_dotnet.is_file():
        return home_dotnet
    raise FileNotFoundError("dotnet executable not found in PATH or ~/.dotnet/dotnet")


def find_forbidden_diff_paths(
    changed_files: Sequence[str],
    *,
    include_all_src: bool = False,
) -> list[str]:
    """Identify modified paths violating Project Aegis hard invariants."""
    return [
        f
        for f in changed_files
        if (include_all_src and f.startswith("src/"))
        or f.startswith("tests/regression/")
        or "DelegationBridge.cs" in f
        or "CatalogWriteGate" in f
        or f == "ProjectAegis.sln"
        or f.endswith(".csproj")
    ]


def verify_dotnet_non_interference(repo_root: Path, *, run_tests: bool = True) -> dict[str, Any]:
    """Verify C# protected hard invariants are untouched, e2e dir has no .cs/.db3, and dotnet passes."""
    diff_proc = subprocess.run(
        ["git", "diff", "--name-only", "HEAD"],
        cwd=str(repo_root),
        capture_output=True,
        text=True,
        check=True,
    )
    changed_files = [line.strip() for line in diff_proc.stdout.splitlines() if line.strip()]
    forbidden_touches = find_forbidden_diff_paths(changed_files, include_all_src=False)
    if forbidden_touches:
        raise AssertionError(f"Forbidden protected C#/golden files modified: {forbidden_touches}")

    baltic_hash = read_baltic_v2_hash(repo_root)
    e2e_root = repo_root / "tests" / "e2e" / "backlog_gap"
    e2e_forbidden = [str(p) for p in e2e_root.rglob("*") if p.is_file() and p.suffix in (".cs", ".csproj", ".db3")]
    if e2e_forbidden:
        raise AssertionError(f"Forbidden .cs/.csproj/.db3 files in tests/e2e/backlog_gap/: {e2e_forbidden}")
    for p in e2e_root.rglob("*"):
        if p.is_file() and p.suffix in (".md", ".txt"):
            if baltic_hash in p.read_text(encoding="utf-8", errors="ignore"):
                raise AssertionError(f"Baltic v2 hash literal leaked into {p}")

    hash_found = False
    for folder in (repo_root / "tests" / "regression", repo_root / "data"):
        if not folder.exists():
            continue
        for p in folder.rglob("*"):
            if p.is_file() and p.suffix in (".txt", ".json", ".md", ".cs"):
                if baltic_hash in p.read_text(encoding="utf-8", errors="ignore"):
                    hash_found = True
                    break
        if hash_found:
            break
    if not hash_found:
        raise AssertionError("Baltic v2 golden hash missing from tests/regression/ and data/")

    dotnet_bin = resolve_dotnet_executable()
    test_summary = "skipped"
    if run_tests:
        floor = json.loads((repo_root / "production" / "test-floor.json").read_text(encoding="utf-8"))
        proc = subprocess.run(
            [str(dotnet_bin), "test", "ProjectAegis.sln", "-v", "minimal", "--no-restore"],
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            timeout=600,
            check=False,
        )
        if proc.returncode != 0:
            raise AssertionError(f"dotnet test failed:\n{proc.stdout}\n{proc.stderr}")
        total_passed = sum(int(m) for m in re.findall(r"Passed:\s*(\d+)", proc.stdout))
        total_failed = sum(int(m) for m in re.findall(r"Failed:\s*(\d+)", proc.stdout))
        min_pass = int(floor["solutionMinPass"])
        if total_failed != 0 or total_passed < min_pass:
            raise AssertionError(
                f"dotnet test floor violated: passed={total_passed} (min {min_pass}), failed={total_failed}"
            )

        unity_tests_proj = (
            "src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj"
        )
        targeted_floors = (
            ("ReplayGolden", int(floor["replayGoldenMin"])),
            ("PlayModeSmokeHarnessTests", int(floor["playModeSmokeMin"])),
        )
        targeted_counts: dict[str, int] = {}
        for filter_expr, floor_min in targeted_floors:
            sub_proc = subprocess.run(
                [
                    str(dotnet_bin),
                    "test",
                    unity_tests_proj,
                    "--no-build",
                    "--filter",
                    filter_expr,
                    "-v",
                    "minimal",
                ],
                cwd=str(repo_root),
                capture_output=True,
                text=True,
                timeout=180,
                check=False,
            )
            if sub_proc.returncode != 0:
                raise AssertionError(
                    f"dotnet test --filter {filter_expr} failed:\n{sub_proc.stdout}\n{sub_proc.stderr}"
                )
            sub_passed = sum(int(m) for m in re.findall(r"Passed:\s*(\d+)", sub_proc.stdout))
            sub_failed = sum(int(m) for m in re.findall(r"Failed:\s*(\d+)", sub_proc.stdout))
            if sub_failed != 0 or sub_passed < floor_min:
                raise AssertionError(
                    f"dotnet {filter_expr} floor violated: passed={sub_passed} (min {floor_min}), failed={sub_failed}"
                )
            targeted_counts[filter_expr] = sub_passed

        test_summary = (
            f"passed={total_passed}, failed={total_failed}, "
            f"ReplayGolden={targeted_counts['ReplayGolden']}, "
            f"PlayModeSmokeHarnessTests={targeted_counts['PlayModeSmokeHarnessTests']}"
        )

    return {"dotnet": str(dotnet_bin), "forbidden_touches": [], "test_summary": test_summary}
