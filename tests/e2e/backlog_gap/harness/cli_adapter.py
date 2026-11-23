"""Dual-phase CLI & evaluator adapter for opaque-box E2E tests."""

from __future__ import annotations

import json
import os
import subprocess
import sys
import time
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable, Mapping, Sequence

from .contracts import (
    MAX_BODY_BYTES,
    MAX_QUESTIONS,
    NOTION_REQUIREMENTS_VIEW,
    NOTION_TASKS_VIEW,
    evaluate_requirement_record,
    scan_for_secrets_and_db3,
    validate_jev_request,
)

REPO_ROOT = Path(__file__).resolve().parents[4]
IMPL_EVAL_SCRIPT = REPO_ROOT / "tools" / "jev" / "backlog_gap_eval.py"
IMPL_QUESTIONS_PACK = REPO_ROOT / "data" / "jev" / "backlog-gap-v1.questions.json"
IMPL_RUBRIC_MD = REPO_ROOT / "production" / "qa" / "backlog-gap-rubric.md"
IMPL_EVIDENCE_DIR = REPO_ROOT / "production" / "qa" / "evidence" / "backlog-gap-2026-11-22"


def is_require_impl() -> bool:
    val = os.environ.get("AEGIS_E2E_REQUIRE_IMPL", "").strip().lower()
    return val in ("1", "true", "yes")


def assert_impl_deliverables_present(
    repo_root: Path = REPO_ROOT, *, include_final: bool = False
) -> list[Path]:
    """Fail fast when --require-impl / AEGIS_E2E_REQUIRE_IMPL=1 is set and artifacts are missing."""
    required = [
        repo_root / "tools" / "jev" / "backlog_gap_eval.py",
        repo_root / "data" / "jev" / "backlog-gap-v1.questions.json",
        repo_root / "production" / "qa" / "backlog-gap-rubric.md",
        repo_root / "production" / "qa" / "evidence" / "backlog-gap-2026-11-22" / "scope-decisions-jev.json",
        repo_root / "production" / "qa" / "evidence" / "backlog-gap-2026-11-22" / "requirements-catalog.json",
        repo_root / "production" / "qa" / "evidence" / "backlog-gap-2026-11-22" / "baseline-gap-matrix.json",
        repo_root / "production" / "qa" / "evidence" / "backlog-gap-2026-11-22" / "remediation-log.json",
    ]
    if include_final:
        required.append(
            repo_root / "production" / "qa" / "evidence" / "backlog-gap-2026-11-22" / "final-gap-matrix.json"
        )
    missing = [str(p) for p in required if not p.is_file()]
    if missing:
        scope = "M1-M4" if include_final else "M1-M3"
        raise AssertionError(
            f"AEGIS_E2E_REQUIRE_IMPL=1 set, but required {scope} deliverables are missing:\n- "
            + "\n- ".join(missing)
        )
    return required


def probe_impl_cli_help(script_path: Path = IMPL_EVAL_SCRIPT) -> subprocess.CompletedProcess[str] | None:
    """Invoke tools/jev/backlog_gap_eval.py --help as an opaque-box subprocess if it exists."""
    if not script_path.is_file():
        return None
    return subprocess.run(
        [sys.executable, str(script_path), "--help"],
        capture_output=True,
        text=True,
        timeout=15,
        check=False,
    )


def run_classify_uat_cli(
    args: Sequence[str],
    *,
    cwd: Path | None = None,
    env_overrides: Mapping[str, str | None] | None = None,
) -> subprocess.CompletedProcess[str]:
    """Opaque-box subprocess invocation of existing tools/jev/classify_uat.py."""
    script = REPO_ROOT / "tools" / "jev" / "classify_uat.py"
    env = dict(os.environ)
    for key in (
        "TYPESAFE_API_KEY",
        "OPENROUTER_API_KEY",
        "CLOUDFLARE_API_TOKEN",
        "CLOUDFLARE_ACCOUNT_ID",
        "AI_GATEWAY_API_KEY",
        "VERCEL_OIDC_TOKEN",
    ):
        env.pop(key, None)
    if env_overrides:
        for k, v in env_overrides.items():
            if v is None:
                env.pop(k, None)
            else:
                env[k] = v
    return subprocess.run(
        [sys.executable, str(script), *args],
        cwd=str(cwd) if cwd else None,
        env=env,
        capture_output=True,
        text=True,
        timeout=15,
        check=False,
    )


def post_jev_json(
    url: str,
    payload: Mapping[str, Any],
    *,
    api_key: str = "test-mock-key",
    timeout_s: float = 1.5,
) -> dict[str, Any]:
    """Validate privacy & wire constraints before posting to a Jev endpoint."""
    raw_text = json.dumps(payload, ensure_ascii=False)
    leak_res = scan_for_secrets_and_db3(raw_text)
    if not leak_res.ok:
        raise ValueError("Blocked before HTTP POST: " + "; ".join(leak_res.errors))

    wire_res = validate_jev_request(payload)
    if not wire_res.ok:
        raise ValueError("Blocked invalid Jev request: " + "; ".join(wire_res.errors))

    req = urllib.request.Request(
        url,
        data=raw_text.encode("utf-8"),
        headers={
            "Authorization": f"Bearer {api_key}",
            "Content-Type": "application/json",
            "Accept": "application/json",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=timeout_s) as resp:
            return {"status_code": resp.status, "body": json.loads(resp.read().decode("utf-8"))}
    except urllib.error.HTTPError as exc:
        body_text = exc.read().decode("utf-8", errors="replace")
        try:
            body_json = json.loads(body_text)
        except ValueError:
            body_json = {"raw": body_text}
        return {"status_code": exc.code, "body": body_json}
    except Exception as exc:  # noqa: BLE001 — degrade contract
        return {"status_code": None, "status": "no_advisory", "error": f"{type(exc).__name__}: {exc}"}


def evaluate_requirements_batched(
    requirements: Sequence[Mapping[str, Any]],
    *,
    linear_by_req: Mapping[str, Sequence[Mapping[str, Any]]],
    notion_by_req: Mapping[str, Sequence[Mapping[str, Any]]],
    jev_url: str,
    model: str = "typesafe/jev-1.14",
    batch_size: int = MAX_QUESTIONS,
) -> list[dict[str, Any]]:
    """Chunk requirements so every Jev HTTP POST stays <= 20 questions and <= 32 KB."""
    if not (1 <= batch_size <= MAX_QUESTIONS):
        raise ValueError(f"batch_size must be in 1..{MAX_QUESTIONS}")

    results: list[dict[str, Any]] = []
    idx = 0
    while idx < len(requirements):
        chunk = list(requirements[idx : idx + batch_size])
        # Shrink chunk dynamically if serialized payload exceeds 32 KB
        while chunk:
            questions: dict[str, Any] = {}
            state_items: list[dict[str, Any]] = []
            for pos, req in enumerate(chunk):
                q_key = f"req_{pos:02d}"
                questions[q_key] = {
                    "type": "noul",
                    "instructions": f"Is requirement {req['req_id']} adequately tracked?",
                    "criteria": {"true": "Adequately tracked", "false": "Gap exists"},
                }
                state_items.append(
                    {
                        "req_id": req["req_id"],
                        "source_file": req["source_file"],
                        "summary": str(req.get("summary", ""))[:240],
                    }
                )
            payload = {"model": model, "state": {"items": state_items}, "questions": questions}
            byte_len = len(json.dumps(payload, ensure_ascii=False).encode("utf-8"))
            if byte_len <= MAX_BODY_BYTES or len(chunk) == 1:
                break
            chunk = chunk[: max(1, len(chunk) // 2)]

        resp = post_jev_json(jev_url, payload)
        resp_body = resp.get("body") if resp.get("status_code") == 200 else None
        answers = (resp_body or {}).get("answers") or {}
        dec_id = (resp_body or {}).get("id")

        for pos, req in enumerate(chunk):
            q_key = f"req_{pos:02d}"
            single_q = {q_key: questions[q_key]}
            single_resp = (
                {"id": dec_id, "answers": {q_key: answers[q_key]}}
                if q_key in answers
                else None
            )
            ev = evaluate_requirement_record(
                req=req,
                linear_matches=linear_by_req.get(req["req_id"], ()),
                notion_matches=notion_by_req.get(req["req_id"], ()),
                jev_response=single_resp,
                questions=single_q,
            )
            results.append(ev)

        idx += len(chunk)
    return results


@dataclass
class NotionQueryTrace:
    modes_attempted: list[str]
    view_urls_used: list[str]
    request_timestamps: list[float]
    retries_on_429: int


class NotionViewAdapter:
    """Simulated/contract Notion client enforcing mode='view' fallback, <=2 req/s, and 429 retry."""

    def __init__(
        self,
        fetch_fn: Callable[[str, str | None], Mapping[str, Any]],
        *,
        min_interval_s: float = 0.5,
        sleep_fn: Callable[[float], None] = time.sleep,
    ) -> None:
        self._fetch_fn = fetch_fn
        self._min_interval_s = min_interval_s
        self._sleep_fn = sleep_fn
        self.trace = NotionQueryTrace([], [], [], 0)
        self._last_req_ts: float | None = None

    def _throttle(self) -> None:
        now = time.monotonic()
        if self._last_req_ts is not None:
            elapsed = now - self._last_req_ts
            if elapsed < self._min_interval_s:
                self._sleep_fn(self._min_interval_s - elapsed)
        self._last_req_ts = time.monotonic()
        self.trace.request_timestamps.append(self._last_req_ts)

    def query_with_fallback(
        self,
        *,
        view_url: str = NOTION_TASKS_VIEW,
        try_sql_first: bool = True,
        max_429_retries: int = 3,
    ) -> list[dict[str, Any]]:
        rows: list[dict[str, Any]] = []
        mode = "sql" if try_sql_first else "view"
        cursor: str | None = None

        while True:
            attempt = 0
            while True:
                self._throttle()
                self.trace.modes_attempted.append(mode)
                if mode == "view" and view_url not in self.trace.view_urls_used:
                    self.trace.view_urls_used.append(view_url)
                resp = self._fetch_fn(mode, cursor)
                err = resp.get("error")
                status = resp.get("status_code", 200)
                if err == "usage_limit_reached" and mode == "sql":
                    mode = "view"
                    cursor = None
                    continue
                if status == 429:
                    attempt += 1
                    self.trace.retries_on_429 += 1
                    if attempt > max_429_retries:
                        raise RuntimeError("Notion 429 retry budget exhausted")
                    self._sleep_fn(0.05 * (2 ** (attempt - 1)))
                    continue
                break

            rows.extend(resp.get("results", []))
            cursor = resp.get("next_cursor")
            if not resp.get("has_more") or not cursor:
                break

        return rows
