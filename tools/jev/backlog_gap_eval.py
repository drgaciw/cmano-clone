#!/usr/bin/env python3
"""Off-tick `typesafe ai jev` decision client and backlog gap rubric evaluator.

Primary endpoint: https://openrouter.ai/api/alpha/decisions (typesafe/jev-1.14)
Fallback endpoint: https://openrouter.ai/api/v1/systemone (typesafe/jev-1.13)

Pure Python stdlib-only CLI + importable module. Never imported by
ProjectAegis.Sim or ProjectAegis.Delegation core. Enforces script-first
deterministic pre-gates so Jev never overrides a failed mechanical check.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import threading
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Mapping, Protocol, Sequence

PRIMARY_URL = "https://openrouter.ai/api/alpha/decisions"
PRIMARY_MODEL = "typesafe/jev-1.14"
FALLBACK_URL = "https://openrouter.ai/api/v1/systemone"
FALLBACK_MODEL = "typesafe/jev-1.13"

DEFAULT_TIMEOUT_MS = 30_000
MAX_BODY_BYTES = 32 * 1024
MAX_QUESTIONS = 20
KEY_PATTERN = re.compile(r"^[a-z0-9_]{1,40}\Z")

RUBRIC_VERSION = "v1"
NOUL_PASS_FLOOR = 0.80
CHOICE_CONFIDENCE_FLOOR = 0.50
SCORE_COMPLETE_FLOOR = 1.50

ALLOWED_STATUSES = ("complete", "incomplete", "missing")
ALLOWED_ACTIONS = (
    "none",
    "create_linear",
    "update_linear",
    "create_notion",
    "update_notion",
    "both",
)

SENSITIVE_ENV_VARS = (
    "OPENROUTER_API_KEY",
    "TYPESAFE_API_KEY",
    "NOTION_API_KEY",
    "BUILDKITE_API_TOKEN",
    "OPENCODE_ZEN_API_KEY",
    "CLOUDFLARE_API_KEY",
    "CLOUDFLARE_API_TOKEN",
    "CLOUDFLARE_ACCESS_KEY_ID",
    "CLOUDFLARE_ACCESS_KEY_SECRET",
    "VERCEL_API_KEY",
    "AUTOJEV_API_KEY",
    "AI_GATEWAY_API_KEY",
    "VERCEL_OIDC_TOKEN",
    "LINEAR_API_KEY",
)

_DB3_PATTERN = re.compile(r"\.db3\b", re.IGNORECASE)
_CPQ_TEXT_PATTERN = re.compile(r"\b(OPEN-CRM-CPQ|P-DRG-6)\b", re.IGNORECASE)
_CMANO_REF_PATTERN = re.compile(r"docs/reference/cmano-db/", re.IGNORECASE)
_EXCLUDED_UAT_CPQ_PATTERN = re.compile(
    r"tests[/\\]uat[/\\](?:[^\s/\\]*cpq[^\s/\\]*|lead-to-fulfillment[^\s/\\]*)",
    re.IGNORECASE,
)
_SURFACE_LINE_PATTERN = re.compile(r"(?im)^\s*(?:[-*]\s*)?Surface\s*:")
_LINEAR_ID_PATTERN = re.compile(r"\bDRG-\d+\b")
_NOTION_LINK_PATTERN = re.compile(
    r"(https?://(?:www\.)?(?:app\.)?notion\.(?:so|com)/\S+|\bnotion://\S+)",
    re.IGNORECASE,
)

_REPO_ROOT = Path(__file__).resolve().parents[2]
_PARENT_ROOT = _REPO_ROOT.parent
DEFAULT_RUBRIC = _REPO_ROOT / "data" / "jev" / "backlog-gap-v1.questions.json"


class HttpTransport(Protocol):
    """Minimal HTTP POST protocol matching tools/jev/classify_uat.py."""

    def post(
        self,
        url: str,
        *,
        headers: Mapping[str, str],
        json_body: Mapping[str, Any],
        timeout: float,
    ) -> Any: ...


class UrllibTransport:
    """Minimal stdlib POST transport enforcing an overall request deadline."""

    def post(
        self,
        url: str,
        *,
        headers: Mapping[str, str],
        json_body: Mapping[str, Any],
        timeout: float,
    ) -> Any:
        """POST JSON and enforce an overall deadline across connect/send/read."""
        result: dict[str, Any] = {}

        def _worker() -> None:
            try:
                result["value"] = self._post_blocking(
                    url, headers=headers, json_body=json_body, timeout=timeout
                )
            except BaseException as exc:  # surfaced to caller below
                result["error"] = exc

        worker = threading.Thread(
            target=_worker, name="jev-backlog-gap-post", daemon=True
        )
        worker.start()
        worker.join(timeout)
        if worker.is_alive():
            raise TimeoutError(
                f"Jev decision request exceeded {timeout:.3f}s overall budget"
            )
        if "error" in result:
            raise result["error"]
        return result["value"]

    def _post_blocking(
        self,
        url: str,
        *,
        headers: Mapping[str, str],
        json_body: Mapping[str, Any],
        timeout: float,
    ) -> Any:
        data = json.dumps(json_body, ensure_ascii=False).encode("utf-8")
        req = urllib.request.Request(
            url,
            data=data,
            headers=dict(headers),
            method="POST",
        )
        try:
            with urllib.request.urlopen(req, timeout=timeout) as resp:
                body = resp.read().decode("utf-8")
                status = getattr(resp, "status", 200)

                class _Resp:
                    status_code = status
                    text = body

                    def json(self_inner: Any) -> Any:
                        return json.loads(body)

                return _Resp()
        except urllib.error.HTTPError as exc:
            body = exc.read().decode("utf-8", errors="replace")

            class _ErrResp:
                status_code = exc.code
                text = body

                def json(self_inner: Any) -> Any:
                    return json.loads(body)

            return _ErrResp()


def _utc_now_iso() -> str:
    return (
        datetime.now(timezone.utc)
        .replace(microsecond=0)
        .isoformat()
        .replace("+00:00", "Z")
    )


def load_dotenv_files(
    candidates: list[Path] | None = None,
) -> Path | None:
    """Load KEY=VALUE pairs from the first existing dotenv file.

    Never overwrites variables already present in os.environ.
    """
    search_paths = candidates or [
        _REPO_ROOT / ".env",
        _REPO_ROOT / ".env.local",
        _REPO_ROOT / "tools" / "jev" / ".env",
        _PARENT_ROOT / ".env",
    ]
    for path in search_paths:
        if not path.is_file():
            continue
        for line in path.read_text(encoding="utf-8").splitlines():
            stripped = line.strip()
            if not stripped or stripped.startswith("#") or "=" not in stripped:
                continue
            key, _, raw_val = stripped.partition("=")
            key = key.strip()
            if not key or key in os.environ:
                continue
            val = raw_val.strip()
            if len(val) >= 2 and (
                (val[0] == val[-1] == '"') or (val[0] == val[-1] == "'")
            ):
                val = val[1:-1]
            os.environ[key] = val
        return path
    return None


def resolve_openrouter_key() -> str | None:
    """Return stripped OPENROUTER_API_KEY from os.environ, or None if missing/blank."""
    value = os.environ.get("OPENROUTER_API_KEY")
    if value and value.strip():
        return value.strip()
    return None


def collect_secret_values(extra_secrets: list[str] | None = None) -> list[str]:
    """Collect non-empty sensitive credential values from env for redaction/hygiene."""
    secrets: list[str] = []
    for name in SENSITIVE_ENV_VARS:
        val = os.environ.get(name)
        if val and val.strip() and len(val.strip()) >= 4:
            secrets.append(val.strip())
    if extra_secrets:
        for val in extra_secrets:
            if val and val.strip() and len(val.strip()) >= 4:
                secrets.append(val.strip())
    # Deduplicate longest-first so partial substrings are redacted cleanly
    return sorted(set(secrets), key=len, reverse=True)


def redact_secrets(text: str, secrets: list[str] | None = None) -> str:
    """Scrub any known secret strings from logs or error text."""
    redacted = text
    for secret in secrets if secrets is not None else collect_secret_values():
        if secret in redacted:
            redacted = redacted.replace(secret, "[REDACTED]")
    return redacted


def _is_short_string(value: Any, max_len: int) -> bool:
    return isinstance(value, str) and 1 <= len(value) <= max_len


def validate_question(
    name: str,
    question: Any,
    *,
    require_escape_on_choice: bool = True,
) -> None:
    """Validate a single System One question against Jev 1.14 / jev-worker constraints."""
    if not isinstance(name, str) or not KEY_PATTERN.match(name):
        raise ValueError(
            f"Invalid question key {name!r}: must match ^[a-z0-9_]{{1,40}}$"
        )
    if not isinstance(question, Mapping):
        raise ValueError(f"Question {name!r} must be an object")

    instructions = question.get("instructions")
    if not _is_short_string(instructions, 600):
        raise ValueError(
            f"Question {name!r} instructions must be a non-empty string <= 600 chars"
        )

    q_type = question.get("type")
    if q_type == "choice":
        criteria = question.get("criteria")
        if not isinstance(criteria, Mapping):
            raise ValueError(f"Choice question {name!r} requires a criteria object")
        if not (2 <= len(criteria) <= 255):
            raise ValueError(
                f"Choice question {name!r} criteria requires 2..255 options"
            )
        for opt_key, opt_desc in criteria.items():
            if not isinstance(opt_key, str) or not KEY_PATTERN.match(opt_key):
                raise ValueError(
                    f"Choice {name!r} option key {opt_key!r} must match ^[a-z0-9_]{{1,40}}$"
                )
            if not _is_short_string(opt_desc, 400):
                raise ValueError(
                    f"Choice {name!r} option {opt_key!r} description must be 1..400 chars"
                )
        if require_escape_on_choice and not (
            "none" in criteria or "other" in criteria
        ):
            raise ValueError(
                f"Choice question {name!r} criteria must include an escape option ('none' or 'other')"
            )
    elif q_type == "score":
        criteria = question.get("criteria")
        if not isinstance(criteria, list):
            raise ValueError(f"Score question {name!r} requires a criteria array")
        if not (2 <= len(criteria) <= 10):
            raise ValueError(
                f"Score question {name!r} criteria requires 2..10 levels"
            )
        for idx, level in enumerate(criteria):
            if not _is_short_string(level, 400):
                raise ValueError(
                    f"Score {name!r} level[{idx}] must be a non-empty string <= 400 chars"
                )
    elif q_type == "noul":
        criteria = question.get("criteria")
        if criteria is not None:
            if not isinstance(criteria, Mapping) or len(criteria) == 0:
                raise ValueError(
                    f"Noul question {name!r} criteria, when present, must be a non-empty object"
                )
            for k, v in criteria.items():
                if not isinstance(k, str) or not KEY_PATTERN.match(k):
                    raise ValueError(
                        f"Noul {name!r} criteria key {k!r} must match ^[a-z0-9_]{{1,40}}$"
                    )
                if not _is_short_string(v, 400):
                    raise ValueError(
                        f"Noul {name!r} criteria value for {k!r} must be 1..400 chars"
                    )
    else:
        raise ValueError(
            f"Question {name!r} has invalid type {q_type!r}: must be choice, score, or noul"
        )


def validate_question_pack(
    pack: Mapping[str, Any],
    *,
    require_escape_on_choice: bool = True,
) -> dict[str, Any]:
    """Validate a pinned question pack JSON structure and return its questions map."""
    if not isinstance(pack, Mapping):
        raise ValueError("Question pack must be a JSON object")
    questions = pack.get("questions")
    if not isinstance(questions, Mapping) or not questions:
        raise ValueError("Question pack must contain a non-empty 'questions' object")
    if len(questions) > MAX_QUESTIONS:
        raise ValueError(
            f"Question pack has {len(questions)} questions; maximum is {MAX_QUESTIONS}"
        )
    for name, q in questions.items():
        validate_question(
            name, q, require_escape_on_choice=require_escape_on_choice
        )
    return dict(questions)


def validate_hygiene(
    state: Any,
    *,
    extra_secrets: list[str] | None = None,
) -> None:
    """Reject proprietary .db3 strings or literal env secret values in state."""
    if isinstance(state, str):
        serialized = state
    else:
        serialized = json.dumps(state, ensure_ascii=False)

    if _DB3_PATTERN.search(serialized):
        raise ValueError(
            "Hygiene violation: state must not contain proprietary .db3 references or extracts"
        )

    for secret in collect_secret_values(extra_secrets=extra_secrets):
        if secret in serialized:
            raise ValueError(
                "Hygiene violation: state contains a literal credential/secret value"
            )


def validate_request_body(
    body: Mapping[str, Any],
    *,
    require_escape_on_choice: bool = True,
    extra_secrets: list[str] | None = None,
) -> int:
    """Validate full Jev request body and return UTF-8 byte length."""
    if not isinstance(body, Mapping):
        raise ValueError("Request body must be a JSON object")

    state = body.get("state")
    if isinstance(state, str):
        if not state.strip():
            raise ValueError("Request 'state' string must be non-empty")
    elif isinstance(state, Mapping):
        if not state:
            raise ValueError("Request 'state' object must be non-empty")
    else:
        raise ValueError("Request 'state' must be a non-empty string or object")

    validate_hygiene(state, extra_secrets=extra_secrets)

    questions = body.get("questions")
    if not isinstance(questions, Mapping):
        raise ValueError("Request 'questions' must be an object")
    if not (1 <= len(questions) <= MAX_QUESTIONS):
        raise ValueError(
            f"Request 'questions' requires 1..{MAX_QUESTIONS} entries (got {len(questions)})"
        )
    for name, q in questions.items():
        validate_question(
            name, q, require_escape_on_choice=require_escape_on_choice
        )

    encoded = json.dumps(body, ensure_ascii=False).encode("utf-8")
    byte_len = len(encoded)
    if byte_len > MAX_BODY_BYTES:
        raise ValueError(
            f"Request body is {byte_len} UTF-8 bytes; maximum is {MAX_BODY_BYTES} bytes"
        )
    return byte_len


def load_questions(path: Path) -> dict[str, Any]:
    """Load and validate a pinned questions JSON file."""
    raw = json.loads(path.read_text(encoding="utf-8"))
    return validate_question_pack(raw)


def _post_json_once(
    *,
    url: str,
    model: str,
    state: str | Mapping[str, Any],
    questions: Mapping[str, Any],
    api_key: str,
    transport: HttpTransport,
    timeout_ms: int,
    extra_secrets: list[str] | None = None,
) -> tuple[dict[str, Any] | None, dict[str, Any]]:
    body = {
        "model": model,
        "state": state,
        "questions": dict(questions),
    }
    secrets = collect_secret_values(extra_secrets=[api_key, *(extra_secrets or [])])
    validate_request_body(body, extra_secrets=secrets)

    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
        "Accept": "application/json",
    }
    timeout_s = max(0.05, timeout_ms / 1000.0)
    try:
        resp = transport.post(
            url, headers=headers, json_body=body, timeout=timeout_s
        )
    except Exception as exc:  # noqa: BLE001 — degrade/fallback required
        err = redact_secrets(f"{type(exc).__name__}: {exc}", secrets)
        return None, {
            "url": url,
            "model": model,
            "status_code": None,
            "error": err,
        }

    status = getattr(resp, "status_code", None)
    if status is None or int(status) < 200 or int(status) >= 300:
        raw_text = str(getattr(resp, "text", "") or "")[:300]
        err = redact_secrets(f"http_{status}: {raw_text}", secrets)
        return None, {
            "url": url,
            "model": model,
            "status_code": status,
            "error": err,
        }

    try:
        payload = resp.json()
    except Exception as exc:  # noqa: BLE001
        err = redact_secrets(f"parse_error: {exc}", secrets)
        return None, {
            "url": url,
            "model": model,
            "status_code": int(status),
            "error": err,
        }

    if not isinstance(payload, Mapping) or not isinstance(payload.get("answers"), Mapping):
        return None, {
            "url": url,
            "model": model,
            "status_code": int(status),
            "error": "invalid_response: missing 'answers' object",
        }

    return dict(payload), {
        "url": url,
        "model": model,
        "status_code": int(status),
        "error": None,
    }


def decide_with_fallback(
    state: str | Mapping[str, Any],
    questions: Mapping[str, Any],
    *,
    api_key: str,
    transport: HttpTransport | None = None,
    timeout_ms: int = DEFAULT_TIMEOUT_MS,
    allow_fallback: bool = True,
    model_override: str | None = None,
) -> dict[str, Any]:
    """Execute a Jev decision call with primary alpha/decisions -> fallback v1/systemone.

    Raises ValueError on pre-flight schema/size/hygiene validation failure.
    Never raises on transport/HTTP/parse errors; degrades cleanly to status='no_advisory'.
    """
    primary_model = model_override or PRIMARY_MODEL
    primary_request = {
        "model": primary_model,
        "state": state,
        "questions": dict(questions),
    }
    secrets = collect_secret_values(extra_secrets=[api_key])
    # Pre-flight validation fails fast before any network call.
    validate_request_body(primary_request, extra_secrets=secrets)

    client = transport or UrllibTransport()
    requested_at = _utc_now_iso()
    attempts: list[dict[str, Any]] = []

    payload, primary_attempt = _post_json_once(
        url=PRIMARY_URL,
        model=primary_model,
        state=state,
        questions=questions,
        api_key=api_key,
        transport=client,
        timeout_ms=timeout_ms,
        extra_secrets=secrets,
    )
    attempts.append(primary_attempt)

    if payload is not None:
        return {
            "status": "ok",
            "route_used": "openrouter_alpha_decisions",
            "fallback_used": False,
            "requested_at_utc": requested_at,
            "request": primary_request,
            "response": {
                "id": payload.get("id"),
                "model": payload.get("model") or primary_model,
                "provider": payload.get("provider"),
                "answers": dict(payload["answers"]),
                "usage": payload.get("usage"),
            },
            "attempts": attempts,
        }

    if allow_fallback:
        print(
            f"[jev] primary failed ({primary_attempt['error']}); falling back to {FALLBACK_URL} ({FALLBACK_MODEL})",
            file=sys.stderr,
        )
        fallback_request = {
            "model": FALLBACK_MODEL,
            "state": state,
            "questions": dict(questions),
        }
        try:
            fb_payload, fb_attempt = _post_json_once(
                url=FALLBACK_URL,
                model=FALLBACK_MODEL,
                state=state,
                questions=questions,
                api_key=api_key,
                transport=client,
                timeout_ms=timeout_ms,
                extra_secrets=secrets,
            )
        except ValueError as exc:
            fb_payload = None
            fb_attempt = {
                "url": FALLBACK_URL,
                "model": FALLBACK_MODEL,
                "status_code": None,
                "error": redact_secrets(f"validation_error: {exc}", secrets),
            }
        attempts.append(fb_attempt)
        if fb_payload is not None:
            return {
                "status": "ok",
                "route_used": "openrouter_v1_systemone",
                "fallback_used": True,
                "requested_at_utc": requested_at,
                "request": fallback_request,
                "response": {
                    "id": fb_payload.get("id"),
                    "model": fb_payload.get("model") or FALLBACK_MODEL,
                    "provider": fb_payload.get("provider"),
                    "answers": dict(fb_payload["answers"]),
                    "usage": fb_payload.get("usage"),
                },
                "attempts": attempts,
            }

    last_error = attempts[-1]["error"] if attempts else "unknown_error"
    return {
        "status": "no_advisory",
        "route_used": None,
        "fallback_used": len(attempts) > 1,
        "requested_at_utc": requested_at,
        "request": primary_request,
        "response": None,
        "attempts": attempts,
        "error": redact_secrets(str(last_error), secrets),
    }


_LINEAR_XML_TAG_RE = re.compile(
    r"<(issue|pull-request)\b([^>]*?)(?:/>|>(.*?)</\1>)",
    re.IGNORECASE | re.DOTALL,
)
_XML_LABEL_ATTR_RE = re.compile(r'\blabel="([^"]+)"')
_NOTION_HTTP_MD_LINK_RE = re.compile(
    r"\[([^\]]+\.md)\]\(https?://[^)\s]+\.md\)", re.IGNORECASE
)
_NOTION_BARE_HTTP_MD_RE = re.compile(
    r"https?://([^\s)\]>\"']+\.md)\b", re.IGNORECASE
)
_NOTION_WRAPPER_SCHEMA_RE = re.compile(
    r"<parent-data-source-schema\b[^>]*>.*?</parent-data-source-schema>",
    re.IGNORECASE | re.DOTALL,
)
_NOTION_CONTENT_BLOCK_RE = re.compile(
    r"<content>(.*?)</content>", re.IGNORECASE | re.DOTALL
)
_RANGE_TOKEN_RE = re.compile(
    r"\b([A-Z][A-Z0-9-]*?)-(\d+(?:\.\d+)?)\.\.(?:([A-Z][A-Z0-9-]*?)-)?(\d+(?:\.\d+)?)\b"
)


def _strip_linear_xml_tags(text: str) -> str:
    """Replace verbose <issue ... label="DRG-NNN"/> / <pull-request .../> tags with [label]."""
    if not text or "<" not in text:
        return text

    def _repl(match: re.Match[str]) -> str:
        attrs = match.group(2) or ""
        lbl_m = _XML_LABEL_ATTR_RE.search(attrs)
        if lbl_m:
            return f"[{lbl_m.group(1)}]"
        inner = (match.group(3) or "").strip()
        return f"[{inner}]" if inner else ""

    stripped = _LINEAR_XML_TAG_RE.sub(_repl, text) if "<" in text else text
    return stripped.replace("\\[", "[").replace("\\]", "]")


def _clean_notion_markdown(text: str) -> str:
    """Strip raw notion-fetch wrappers, broken HTML fragments, and http://*.md auto-links."""
    raw = str(text or "").strip()
    if not raw:
        return ""
    if "<blank-page>" in raw and "<content>" not in raw:
        return ""
    content_m = _NOTION_CONTENT_BLOCK_RE.search(raw)
    if content_m:
        raw = content_m.group(1).strip()
    else:
        raw = _NOTION_WRAPPER_SCHEMA_RE.sub("", raw)
        if raw.startswith("Here is the result of") and "<page " in raw:
            raw = re.sub(r"</?(?:page|ancestor-path|ancestor-\d+-\w+|parent-data-source|properties|iconMetadata|blank-page)[^>]*>", " ", raw)
    raw = re.sub(r"<br\s*/?>", "\n", raw, flags=re.IGNORECASE)
    raw = re.sub(r"</?code>", "`", raw, flags=re.IGNORECASE)
    raw = re.sub(r"</?b>", "", raw, flags=re.IGNORECASE)
    raw = _NOTION_HTTP_MD_LINK_RE.sub(r"`\1`", raw)
    raw = _NOTION_BARE_HTTP_MD_RE.sub(r"`\1`", raw)
    return raw.strip()


def _clean_links(links: Any) -> list[str]:
    cleaned: list[str] = []
    seen: set[str] = set()
    for raw in list(links or []):
        s = str(raw or "").strip().rstrip(".,;:)>](")
        if not s or not s.startswith(("http://", "https://")):
            continue
        if _NOTION_BARE_HTTP_MD_RE.fullmatch(s):
            continue
        if s not in seen:
            seen.add(s)
            cleaned.append(s)
    cleaned.sort(key=lambda u: (0 if ("notion.so" in u or "linear.app" in u) else 1))
    return cleaned


def _parse_num_tuple(token: str) -> tuple[int, ...]:
    return tuple(int(part) for part in token.split("."))


def _expand_req_id_ranges(text: str) -> list[str]:
    """Expand compact requirement ID ranges like NFT-05..NFT-22 or DBI-1.1..1.5."""
    expanded: list[str] = []
    seen: set[str] = set()
    for m in _RANGE_TOKEN_RE.finditer(text or ""):
        prefix1, start_s, prefix2, end_s = m.group(1), m.group(2), m.group(3), m.group(4)
        if prefix2 and prefix2 != prefix1:
            continue
        if prefix1 == "DRG":
            continue
        if "." in start_s or "." in end_s:
            s_parts = _parse_num_tuple(start_s)
            e_parts = _parse_num_tuple(end_s)
            if len(s_parts) == 2 and len(e_parts) == 2 and s_parts[0] == e_parts[0]:
                major = s_parts[0]
                lo, hi = s_parts[1], e_parts[1]
                if 0 <= hi - lo <= 100:
                    for minor in range(lo, hi + 1):
                        rid = f"{prefix1}-{major}.{minor}"
                        if rid not in seen:
                            seen.add(rid)
                            expanded.append(rid)
            elif len(s_parts) == 2 and len(e_parts) == 1:
                major = s_parts[0]
                lo, hi = s_parts[1], e_parts[0]
                if 0 <= hi - lo <= 100:
                    for minor in range(lo, hi + 1):
                        rid = f"{prefix1}-{major}.{minor}"
                        if rid not in seen:
                            seen.add(rid)
                            expanded.append(rid)
        else:
            lo, hi = int(start_s), int(end_s)
            width = max(len(start_s), len(end_s)) if (start_s.startswith("0") or end_s.startswith("0")) else 0
            if 0 <= hi - lo <= 100:
                for num in range(lo, hi + 1):
                    num_str = f"{num:0{width}d}" if width else str(num)
                    rid = f"{prefix1}-{num_str}"
                    if rid not in seen:
                        seen.add(rid)
                        expanded.append(rid)
    return expanded


def _record_covers_req_id(text: str, req_id: str, matched_ids: Sequence[str] | None = None) -> bool:
    if not req_id:
        return False
    req_upper = req_id.strip().upper()
    if matched_ids and any(str(x).strip().upper() == req_upper for x in matched_ids):
        return True
    if not text:
        return False
    if re.search(rf"(?<![A-Za-z0-9_.-]){re.escape(req_id)}(?![A-Za-z0-9_.-])", text, re.IGNORECASE):
        return True
    if req_upper in _expand_req_id_ranges(text):
        return True
    # Check multi-major dotted range like AME-1.2..AME-8.3
    req_m = re.match(r"^([A-Z][A-Z0-9-]*?)-(\d+)\.(\d+)$", req_upper)
    if req_m:
        r_pref, r_maj, r_min = req_m.group(1), int(req_m.group(2)), int(req_m.group(3))
        for m in _RANGE_TOKEN_RE.finditer(text.upper()):
            p1, s_s, p2, e_s = m.group(1), m.group(2), m.group(3), m.group(4)
            if p1 == r_pref and (not p2 or p2 == r_pref) and "." in s_s and "." in e_s:
                sp = _parse_num_tuple(s_s)
                ep = _parse_num_tuple(e_s)
                if len(sp) == 2 and len(ep) == 2 and sp <= (r_maj, r_min) <= ep:
                    return True
    return False


def _extract_targeted_slice(
    text: str,
    req_id: str = "",
    source_file: str = "",
    *,
    max_chars: int = 1800,
) -> str:
    """Extract header metadata + requirement-targeted section slice within max_chars."""
    raw = text.strip()
    multi_section = len(re.findall(r"(?m)^#{2,3}\s+", raw)) >= 3
    if not raw or (len(raw) <= max_chars and not (req_id and multi_section)):
        return raw

    lines = raw.splitlines()
    # Collect clean metadata header lines at top (stop before Acceptance Criteria / --- / ## / ###)
    meta_lines: list[str] = []
    header_lines: list[str] = []
    in_meta = True
    for ln in lines[:16]:
        st = ln.strip()
        if st == "---" or st.startswith("## ") or st.startswith("### "):
            break
        if in_meta and (
            st.lower().startswith("acceptance criteria")
            or st.startswith("- [")
            or st.startswith("* ")
        ):
            in_meta = False
        if in_meta:
            meta_lines.append(ln)
        header_lines.append(ln)
    meta_header = "\n".join(meta_lines).strip()
    header = "\n".join(header_lines).strip()

    target_slice = ""
    if req_id:
        esc = re.escape(req_id)
        heading_re = re.compile(
            rf"(?m)^#{{2,4}}\s*.*?\b{esc}\b",
            re.IGNORECASE,
        )
        table_re = re.compile(
            rf"(?m)^\|\s*(?:\*\*)?{esc}(?:\*\*)?\s*\|[^\n]*",
            re.IGNORECASE,
        )
        bullet_re = re.compile(
            rf"(?m)^[-*]\s*(?:\[[ xX]\]\s*)?(?:\*\*|\[|`)?{esc}\b",
            re.IGNORECASE,
        )
        m = heading_re.search(raw)
        if m:
            start = m.start()
            # Find next ### or ## heading after start
            next_h = re.search(r"(?m)^#{2,3}\s+", raw[start + 4 :])
            end = start + 4 + next_h.start() if next_h else len(raw)
            target_slice = raw[start:end].strip()
        else:
            tm = table_re.search(raw)
            if tm:
                row_text = tm.group(0).strip()
                hdr_m = re.search(r"(?m)^\|\s*Req ID\s*\|[^\n]*\n\|\s*---[^\n]*", raw, re.IGNORECASE)
                target_slice = f"{hdr_m.group(0).strip()}\n{row_text}" if hdr_m else row_text
            else:
                bm = bullet_re.search(raw)
                if bm:
                    start = bm.start()
                    next_h = re.search(r"(?m)^(?:#{2,3}\s+|---)", raw[start + 4 :])
                    end = start + 4 + next_h.start() if next_h else min(len(raw), start + max_chars)
                    target_slice = raw[start:end].strip()
                else:
                    # Fallback: search after header so "Req IDs:" line does not shadow the body section
                    search_offset = len(header) if len(header) < len(raw) else 0
                    idx = raw.lower().find(req_id.lower(), search_offset)
                    if idx < 0 and source_file:
                        basename = Path(source_file).name
                        if basename:
                            idx = raw.lower().find(basename.lower(), search_offset)
                    if idx >= 0:
                        win_start = max(search_offset, idx - 220)
                        win_end = min(len(raw), idx + max_chars - 300)
                        target_slice = raw[win_start:win_end].strip()

    if not target_slice:
        return _truncate(raw, max_chars)

    if target_slice in meta_header:
        return _truncate(meta_header, max_chars)

    chosen_header = meta_header or header
    header_budget = min(len(chosen_header), max(760, max_chars // 2))
    compact_header = _truncate(chosen_header, header_budget) if chosen_header else ""
    combined = f"{compact_header}\n\n[Targeted slice for {req_id or 'requirement'}]\n{target_slice}" if compact_header else target_slice
    return _truncate(combined, max_chars)


def _truncate(text: Any, limit: int) -> str:
    raw = str(text or "").strip()
    if len(raw) <= limit:
        return raw
    return raw[: max(0, limit - 3)] + "..."


def _summarize_linear_match(
    item: Mapping[str, Any],
    *,
    req_id: str = "",
    source_file: str = "",
) -> dict[str, Any]:
    raw_desc = _strip_linear_xml_tags(str(item.get("description") or item.get("body") or ""))
    raw_desc = _NOTION_HTTP_MD_LINK_RE.sub(r"`\1`", raw_desc)
    desc = _extract_targeted_slice(raw_desc, req_id, source_file, max_chars=3000)
    matched_ids = [str(x) for x in list(item.get("matched_req_ids") or []) if str(x).strip()]
    covers = _record_covers_req_id(
        f"{item.get('title') or ''}\n{raw_desc}", req_id, matched_ids
    )
    if covers and req_id:
        matched_ids = [req_id] + [x for x in matched_ids if x != req_id]
    summary: dict[str, Any] = {
        "id": str(item.get("id") or item.get("identifier") or ""),
        "title": _truncate(item.get("title") or "", 160),
        "state": str(item.get("state") or item.get("status") or ""),
        "project": str(item.get("project") or ""),
        "labels": list(item.get("labels") or []),
        "url": str(item.get("url") or ""),
        "source_refs": list(item.get("source_refs") or [])[:4],
        "links": _clean_links(item.get("links"))[:4],
        "has_ac": bool(item.get("has_ac")) or _text_has_ac(raw_desc) or _text_has_ac(desc),
        "description_excerpt": desc,
    }
    if req_id:
        summary["covers_target_req_id"] = covers
    if matched_ids:
        summary["matched_req_ids"] = matched_ids[:8]
    return summary


def _summarize_notion_match(
    item: Mapping[str, Any],
    *,
    req_id: str = "",
    source_file: str = "",
) -> dict[str, Any]:
    raw_note = _clean_notion_markdown(item.get("note") or item.get("description") or "")
    note = _truncate(raw_note, 500)
    raw_content = _clean_notion_markdown(item.get("content") or "")
    content_excerpt = _extract_targeted_slice(
        raw_content, req_id, source_file, max_chars=2800
    )
    matched_ids = [str(x) for x in list(item.get("matched_req_ids") or []) if str(x).strip()]
    covers = _record_covers_req_id(
        f"{item.get('title') or item.get('name') or ''}\n{raw_note}\n{raw_content}",
        req_id,
        matched_ids,
    )
    if covers and req_id:
        matched_ids = [req_id] + [x for x in matched_ids if x != req_id]
    raw_icon = item.get("icon")
    icon_str = str(raw_icon) if raw_icon is not None else ""
    summary: dict[str, Any] = {
        "id": str(item.get("id") or item.get("page_id") or ""),
        "title": _truncate(item.get("title") or item.get("name") or "", 160),
        "surface": str(item.get("surface") or item.get("database") or ""),
        "status": str(item.get("status") or item.get("state") or ""),
        "project": str(item.get("project") or ""),
        "icon": icon_str,
        "has_icon": bool(item.get("has_icon") or icon_str),
        "url": str(item.get("url") or ""),
        "links": _clean_links(item.get("links"))[:4],
        "note_excerpt": note,
        "content_excerpt": content_excerpt,
    }
    if req_id:
        summary["covers_target_req_id"] = covers
    if matched_ids:
        summary["matched_req_ids"] = matched_ids[:8]
    return summary


def build_compact_state(
    requirement: Mapping[str, Any],
    linear_matches: list[Mapping[str, Any]],
    notion_matches: list[Mapping[str, Any]],
    deterministic_checks: Mapping[str, Any],
    *,
    max_state_bytes: int = 16_000,
    questions: Mapping[str, Any] | None = None,
    model: str = PRIMARY_MODEL,
) -> str:
    """Build a bounded, secret-free factual state string for Jev evaluation."""
    def _serialized_state_bytes(candidate: str) -> int:
        return len(json.dumps(candidate, ensure_ascii=False).encode("utf-8"))

    def _fits_budget(candidate: str) -> bool:
        if _serialized_state_bytes(candidate) > max_state_bytes:
            return False
        if questions is not None:
            probe = {
                "model": model,
                "state": candidate,
                "questions": dict(questions),
            }
            probe_bytes = len(json.dumps(probe, ensure_ascii=False).encode("utf-8"))
            reserve = max(
                0,
                max(len(PRIMARY_MODEL), len(FALLBACK_MODEL)) - len(model),
            )
            if probe_bytes + reserve > MAX_BODY_BYTES:
                return False
        return True

    req_id = str(requirement.get("req_id") or "")
    source_file = str(requirement.get("source_file") or "")
    payload = {
        "requirement": {
            "req_id": req_id,
            "title": _truncate(requirement.get("title") or "", 200),
            "source_file": source_file,
            "source_lines": str(requirement.get("source_lines") or ""),
            "domain_project": str(requirement.get("domain_project") or ""),
            "lifecycle_state": str(requirement.get("lifecycle_state") or "active"),
            "shipped_evidence": _truncate(
                requirement.get("shipped_evidence") or "", 300
            ),
            "shipped_files": [
                str(p) for p in list(requirement.get("shipped_files") or [])[:6]
            ],
            "summary": _truncate(requirement.get("summary") or "", 500),
            "acceptance_criteria": [
                _truncate(c, 220)
                for c in list(requirement.get("acceptance_criteria") or [])[:12]
            ],
        },
        "deterministic_checks": dict(deterministic_checks),
        "linear_matches": [
            _summarize_linear_match(m, req_id=req_id, source_file=source_file)
            for m in linear_matches[:8]
        ],
        "notion_matches": [
            _summarize_notion_match(m, req_id=req_id, source_file=source_file)
            for m in notion_matches[:8]
        ],
    }
    text = json.dumps(payload, ensure_ascii=False)
    if _fits_budget(text):
        return text

    # Deterministically trim metadata/auxiliary lists first, then excerpts if still verbose.
    payload["requirement"]["shipped_files"] = payload["requirement"]["shipped_files"][:4]
    payload["requirement"]["summary"] = _truncate(payload["requirement"]["summary"], 250)
    payload["requirement"]["acceptance_criteria"] = payload["requirement"][
        "acceptance_criteria"
    ][:6]
    for m in payload["linear_matches"]:
        m["links"] = m.get("links", [])[:2]
        m["matched_req_ids"] = m.get("matched_req_ids", [])[:4]
        m["description_excerpt"] = _truncate(m["description_excerpt"], 950)
    for m in payload["notion_matches"]:
        m["links"] = m.get("links", [])[:2]
        m["matched_req_ids"] = m.get("matched_req_ids", [])[:4]
        m["note_excerpt"] = _truncate(m["note_excerpt"], 280)
        m["content_excerpt"] = _truncate(m.get("content_excerpt", ""), 800)
    text = json.dumps(payload, ensure_ascii=False)
    if _fits_budget(text):
        return text

    # Further structural compaction for dense 20-question packs or quote-heavy payloads.
    payload["linear_matches"] = payload["linear_matches"][:4]
    payload["notion_matches"] = payload["notion_matches"][:4]
    for m in payload["linear_matches"]:
        m["description_excerpt"] = _truncate(m["description_excerpt"], 120)
    for m in payload["notion_matches"]:
        m["note_excerpt"] = _truncate(m["note_excerpt"], 120)
        m["content_excerpt"] = _truncate(m.get("content_excerpt", ""), 120)
    payload["requirement"]["summary"] = _truncate(payload["requirement"]["summary"], 100)
    payload["requirement"]["acceptance_criteria"] = payload["requirement"][
        "acceptance_criteria"
    ][:3]
    text = json.dumps(payload, ensure_ascii=False)
    if _fits_budget(text):
        return text

    # Final exact UTF-8 byte cap (accounting for JSON escaping and questions envelope).
    lo, hi = 1, len(text)
    best = text[:1]
    while lo <= hi:
        mid = (lo + hi) // 2
        candidate = text[:mid]
        if _fits_budget(candidate):
            best = candidate
            lo = mid + 1
        else:
            hi = mid - 1
    return best


def _text_has_ac(text: str) -> bool:
    lowered = text.lower()
    return any(
        token in lowered
        for token in (
            "acceptance criteria",
            "acceptance:",
            "ac:",
            "- [ ]",
            "- [x]",
            "verify:",
            "verification:",
            "given ",
            "when ",
            "then ",
            "must ",
        )
    )


def _match_text_blob(match: Mapping[str, Any]) -> str:
    parts = [
        str(match.get("id") or match.get("identifier") or match.get("page_id") or ""),
        str(match.get("title") or match.get("name") or ""),
        str(match.get("description") or match.get("body") or ""),
        str(match.get("note") or match.get("content") or ""),
        str(match.get("url") or ""),
        " ".join(str(x) for x in (match.get("source_refs") or [])),
        " ".join(str(x) for x in (match.get("links") or [])),
    ]
    return "\n".join(parts)


def _is_cpq_or_excluded_match(match: Mapping[str, Any]) -> bool:
    project = str(match.get("project") or "")
    blob = _match_text_blob(match)
    if _CPQ_TEXT_PATTERN.search(project) or _CPQ_TEXT_PATTERN.search(blob):
        return True
    if _CMANO_REF_PATTERN.search(blob) or _EXCLUDED_UAT_CPQ_PATTERN.search(blob):
        return True
    return False


def _is_frozen_mirror_match(match: Mapping[str, Any]) -> bool:
    raw = str(match.get("surface") or match.get("database") or "").strip().lower()
    if not raw:
        return False
    if any(
        tok in raw
        for tok in (
            "frozen_git_mirror",
            "frozen git mirror",
            "pin_requirements",
            "requirements_db",
            "📌 requirements",
        )
    ):
        return True
    ascii_only = "".join(
        ch if ch.isascii() and (ch.isalnum() or ch in " _-/") else " "
        for ch in raw
    )
    normalized = " ".join(ascii_only.replace("_", " ").split())
    if "frozen git mirror" in normalized or "requirements db" in normalized:
        return True
    segments = [" ".join(seg.split()) for seg in normalized.split("/")]
    return any(seg == "requirements" for seg in segments)


def _is_tasks_db_match(match: Mapping[str, Any]) -> bool:
    if _is_frozen_mirror_match(match):
        return False
    raw = str(match.get("surface") or match.get("database") or "").strip().lower()
    if not raw:
        return True
    tasks_names = {"tasks", "tasks db", "notion tasks", "notion tasks db"}
    segments: list[str] = []
    for seg in raw.split("/"):
        ascii_seg = "".join(
            ch if ch.isascii() and (ch.isalnum() or ch in " _-") else " "
            for ch in seg
        )
        norm_seg = " ".join(ascii_seg.replace("_", " ").split())
        if norm_seg:
            segments.append(norm_seg)
    normalized = " / ".join(segments)
    return normalized in tasks_names or any(seg in tasks_names for seg in segments)


def _normalize_repo_path(path: Any) -> str:
    norm = str(path or "").strip().replace("\\", "/")
    while norm.startswith("./"):
        norm = norm[2:]
    return norm


def _is_shipped_verified(
    requirement: Mapping[str, Any],
    *,
    linear_matches: list[Mapping[str, Any]] | None = None,
    repo_root: Path | None = None,
) -> bool:
    if str(requirement.get("lifecycle_state") or "").lower() != "shipped":
        return False
    matches = list(linear_matches or [])
    for m in matches:
        state = str(m.get("state") or m.get("status") or "").strip().lower()
        if state == "done" and not bool(m.get("merged_to_main", True)):
            return False
    evidence = requirement.get("shipped_evidence")
    if isinstance(evidence, str) and evidence.strip():
        return True
    if isinstance(evidence, Mapping) and any(bool(v) for v in evidence.values()):
        return True
    evidence_files = requirement.get("shipped_files")
    if isinstance(evidence_files, list) and evidence_files:
        if repo_root is None:
            return True
        if any((repo_root / str(p)).exists() for p in evidence_files):
            return True
    for m in matches:
        state = str(m.get("state") or m.get("status") or "").strip().lower()
        if state == "done" and bool(m.get("merged_to_main", True)):
            return True
    return False


def evaluate_requirement_deterministic(
    requirement: Mapping[str, Any],
    linear_matches: list[Mapping[str, Any]],
    notion_matches: list[Mapping[str, Any]],
    *,
    repo_root: Path | None = _REPO_ROOT,
) -> dict[str, Any]:
    """Run script-first deterministic mechanical pre-gates for Linear and Notion."""
    source_file = str(requirement.get("source_file") or "").strip()
    req_id = str(requirement.get("req_id") or "").strip()
    lifecycle = str(requirement.get("lifecycle_state") or "active").strip().lower()
    source_basename = Path(source_file).name if source_file else ""

    # P_SRC: source file exists in repo or is whitelisted Notion-only research page.
    is_notion_only = (
        str(requirement.get("source_category") or "").lower() == "notiononly"
        or source_file.startswith("notion://")
        or "milsim comparative research" in source_file.lower()
    )
    if not source_file:
        p_src = False
    elif is_notion_only:
        p_src = True
    elif repo_root is not None:
        p_src = (repo_root / source_file).is_file()
    else:
        p_src = True

    # P_ISO: project isolation — reject OPEN-CRM-CPQ / P-DRG-6 / docs/reference/cmano-db/ / tests/uat/*cpq* / lead-to-fulfillment*
    req_is_excluded = bool(
        _CPQ_TEXT_PATTERN.search(str(requirement.get("domain_project") or ""))
        or _CPQ_TEXT_PATTERN.search(source_file)
        or _CMANO_REF_PATTERN.search(source_file)
        or _EXCLUDED_UAT_CPQ_PATTERN.search(source_file)
    )
    p_iso = (
        not req_is_excluded
        and not any(_is_cpq_or_excluded_match(m) for m in linear_matches)
        and not any(_is_cpq_or_excluded_match(m) for m in notion_matches)
    )

    shipped_verified = _is_shipped_verified(
        requirement, linear_matches=linear_matches, repo_root=repo_root
    )

    # Linear deterministic gates
    has_linear_match = len(linear_matches) > 0
    closed_states = ("done", "canceled", "cancelled", "duplicate")
    open_linear_matches = [
        m
        for m in linear_matches
        if str(m.get("state") or m.get("status") or "").strip().lower()
        not in closed_states
    ]
    contract_linear_matches = (
        open_linear_matches if open_linear_matches else linear_matches
    )

    linear_cites_source = False
    linear_cites_req_id = False
    linear_has_ac = False
    linear_no_surface_line = True
    linear_honest_state = True
    linear_has_notion_link = False

    for m in contract_linear_matches:
        blob = _match_text_blob(m)
        if source_file and (
            source_file in blob or (source_basename and source_basename in blob)
        ):
            linear_cites_source = True
        if req_id and _record_covers_req_id(blob, req_id, m.get("matched_req_ids")):
            linear_cites_req_id = True
        if bool(m.get("has_ac")) or _text_has_ac(blob):
            linear_has_ac = True
        if _NOTION_LINK_PATTERN.search(blob) or bool(m.get("notion_url")):
            linear_has_notion_link = True

    for m in linear_matches:
        state = str(m.get("state") or m.get("status") or "").strip().lower()
        labels = [str(x).lower() for x in (m.get("labels") or [])]

        # New requirements / Backlog items must NEVER have an agent-stamped Surface: line or surface/ label
        if state in ("backlog", "todo", "triage", ""):
            desc = str(m.get("description") or m.get("body") or "")
            if _SURFACE_LINE_PATTERN.search(desc) or any(
                lbl.startswith(("surface:", "surface/")) for lbl in labels
            ):
                linear_no_surface_line = False

        if lifecycle in ("active", "shipped"):
            if state == "done" and not bool(m.get("merged_to_main", True)):
                linear_honest_state = False

    # Lifecycle honesty checks for deferred_phase_n / draft evaluate over active/open
    # matches when present so historical Done prerequisite slices do not poison open Backlog.
    for m in contract_linear_matches:
        blob = _match_text_blob(m)
        state = str(m.get("state") or m.get("status") or "").strip().lower()
        if lifecycle == "deferred_phase_n":
            has_deferral_marker = any(
                tok in blob.lower()
                for tok in ("phase n", "deferred", "do not start", "2027-01-15")
            )
            if state in ("in progress", "in review", "done") or not has_deferral_marker:
                linear_honest_state = False
        elif lifecycle == "draft":
            has_draft_marker = any(
                tok in blob.lower()
                for tok in ("draft", "owner acceptance pending", "pending owner")
            )
            if state in ("done", "approved") or not has_draft_marker:
                linear_honest_state = False

    all_linear_historical_closed = all(
        str(m.get("state") or m.get("status") or "").strip().lower()
        in closed_states
        for m in linear_matches
    )
    shipped_verified_exempt = (
        shipped_verified
        and p_iso
        and p_src
        and linear_honest_state
        and (not has_linear_match or all_linear_historical_closed)
    )

    if shipped_verified_exempt:
        # M1 Jev-calibrated shipped exemption: verified code/test/gate evidence or
        # merged Done DRG-* ticket counts without requiring Backlog/AC/Notion backfill.
        p_lin_exists = True
        p_lin_contract = True
    else:
        p_lin_exists = has_linear_match
        p_lin_contract = (
            has_linear_match
            and (linear_cites_source or linear_cites_req_id)
            and (linear_has_ac or lifecycle == "deferred_phase_n")
            and linear_no_surface_line
            and linear_honest_state
            and linear_has_notion_link
        )

    # Notion deterministic gates (active surfaces only; Frozen Git mirror is read-only)
    active_notion_matches = [
        m for m in notion_matches if not _is_frozen_mirror_match(m)
    ]
    has_notion_match = len(active_notion_matches) > 0
    notion_only_frozen = len(notion_matches) > 0 and len(active_notion_matches) == 0
    notion_valid_project_and_icon = True
    notion_cites_source_or_id = False
    notion_has_linear_link = False
    notion_honest_state = True

    linear_states = {
        str(m.get("state") or m.get("status") or "").strip().lower()
        for m in linear_matches
    }

    for m in active_notion_matches:
        blob = _match_text_blob(m)
        status = str(m.get("status") or m.get("state") or "").strip().lower()
        if _is_tasks_db_match(m):
            project = str(m.get("project") or "").strip().lower()
            has_icon = bool(m.get("has_icon") or m.get("icon"))
            if project != "project aegis" or not has_icon:
                notion_valid_project_and_icon = False
            # Blocked must be Status value, not free text in Note while Status != Blocked
            note_text = str(m.get("note") or m.get("description") or "").lower()
            if "blocked:" in note_text and status not in ("blocked", "archived"):
                notion_valid_project_and_icon = False

        if (
            (source_file and (source_file in blob or (source_basename and source_basename in blob)))
            or (req_id and _record_covers_req_id(blob, req_id, m.get("matched_req_ids")))
        ):
            notion_cites_source_or_id = True

        if _LINEAR_ID_PATTERN.search(blob) or "linear.app/" in blob.lower() or bool(
            m.get("linear_url")
        ) or shipped_verified:
            notion_has_linear_link = True

    open_notion_matches = [
        m
        for m in active_notion_matches
        if str(m.get("status") or m.get("state") or "").strip().lower()
        not in ("done", "archived")
    ]
    honesty_notion_matches = (
        open_notion_matches if open_notion_matches else active_notion_matches
    )
    for m in honesty_notion_matches:
        blob = _match_text_blob(m)
        status = str(m.get("status") or m.get("state") or "").strip().lower()
        # H2: Notion goes to Done only after Linear is Done (never the reverse)
        if status == "done" and not shipped_verified:
            if not linear_states or any(s != "done" for s in linear_states):
                notion_honest_state = False
        if lifecycle == "deferred_phase_n":
            if status == "done" or not any(
                tok in blob.lower()
                for tok in ("phase n", "deferred", "do not start", "2027-01-15")
            ):
                notion_honest_state = False
        elif lifecycle == "draft":
            if status in ("done", "approved") or not any(
                tok in blob.lower()
                for tok in ("draft", "owner acceptance pending", "pending owner")
            ):
                notion_honest_state = False

    if not active_notion_matches:
        notion_valid_project_and_icon = False

    p_not_exists = has_notion_match
    p_not_contract = (
        has_notion_match
        and not notion_only_frozen
        and notion_valid_project_and_icon
        and notion_cites_source_or_id
        and notion_has_linear_link
        and notion_honest_state
    )

    return {
        "P_SRC": p_src,
        "P_ISO": p_iso,
        "P_LIN_EXISTS": p_lin_exists,
        "P_LIN_CONTRACT": p_lin_contract,
        "P_NOT_EXISTS": p_not_exists,
        "P_NOT_CONTRACT": p_not_contract,
        "shipped_verified_exempt": shipped_verified_exempt,
        "linear_cites_source": linear_cites_source,
        "linear_cites_req_id": linear_cites_req_id,
        "linear_has_ac": linear_has_ac,
        "linear_no_surface_line": linear_no_surface_line,
        "linear_honest_state": linear_honest_state,
        "linear_has_notion_link": linear_has_notion_link,
        "notion_only_frozen_mirror": notion_only_frozen,
        "notion_valid_project_and_icon": notion_valid_project_and_icon,
        "notion_cites_source_or_id": notion_cites_source_or_id,
        "notion_has_linear_link": notion_has_linear_link,
        "notion_honest_state": notion_honest_state,
    }


def _collect_deterministic_gaps(checks: Mapping[str, Any]) -> list[str]:
    gaps: list[str] = []
    if not checks.get("P_SRC"):
        gaps.append("deterministic: source_file missing or invalid")
    if not checks.get("P_ISO"):
        gaps.append("deterministic: project isolation violation (CPQ or reference DB)")
    if not checks.get("P_LIN_EXISTS"):
        gaps.append("linear: missing matching issue")
    elif not checks.get("P_LIN_CONTRACT"):
        if not (checks.get("linear_cites_source") or checks.get("linear_cites_req_id")):
            gaps.append("linear: missing source_file or req_id pointer")
        if not checks.get("linear_has_ac"):
            gaps.append("linear: missing testable acceptance criteria")
        if not checks.get("linear_no_surface_line"):
            gaps.append("linear: unauthorized Surface: line on Backlog issue")
        if not checks.get("linear_honest_state"):
            gaps.append("linear: lifecycle/state dishonesty (premature Done or missing Draft/Phase N marker)")
        if not checks.get("linear_has_notion_link"):
            gaps.append("linear: missing Notion cross-link")

    if checks.get("notion_only_frozen_mirror"):
        gaps.append("notion: matched only in Frozen Git mirror (read-only archive); missing on active surface")
    elif not checks.get("P_NOT_EXISTS"):
        gaps.append("notion: missing matching entry on active surface (Tasks DB / cmano-clone wiki)")
    elif not checks.get("P_NOT_CONTRACT"):
        if not checks.get("notion_valid_project_and_icon"):
            gaps.append("notion: missing Project=Project Aegis, page icon, or valid Blocked Status")
        if not checks.get("notion_cites_source_or_id"):
            gaps.append("notion: missing source_file or req_id citation")
        if not checks.get("notion_has_linear_link"):
            gaps.append("notion: missing Linear issue cross-link")
        if not checks.get("notion_honest_state"):
            gaps.append("notion: lifecycle/state dishonesty (premature Done before Linear or missing Draft/Phase N marker)")
    return gaps


def _evaluate_side_with_jev(
    *,
    side: str,
    exists: bool,
    contract_ok: bool,
    shipped_exempt: bool,
    isolation_ok: bool,
    source_ok: bool,
    jev_answers: Mapping[str, Any] | None,
) -> tuple[str, bool, list[str]]:
    """Apply script-first rules + Jev thresholds to determine side status."""
    side_gaps: list[str] = []
    human_review = False

    if not exists:
        return "missing", False, side_gaps
    if not isolation_ok or not source_ok:
        return "incomplete", False, side_gaps
    if not contract_ok:
        # Script-first invariant: Jev can NEVER override a failed mechanical gate.
        return "incomplete", False, side_gaps

    if shipped_exempt and side == "linear":
        if not jev_answers:
            return "complete", False, side_gaps
        score_val = (jev_answers.get("linear_completeness_score") or {}).get("score")
        choice_obj = jev_answers.get("linear_classification") or {}
        choice_val = choice_obj.get("choice")
        choice_conf = choice_obj.get("confidence")
        if score_val is None or float(score_val) < SCORE_COMPLETE_FLOOR:
            side_gaps.append(
                f"linear: Jev linear_completeness_score score={score_val} < {SCORE_COMPLETE_FLOOR}"
            )
        if (
            choice_val == "other"
            or choice_conf is None
            or float(choice_conf) < CHOICE_CONFIDENCE_FLOOR
        ):
            human_review = True
            side_gaps.append(
                f"linear: Jev linear_classification uncertain (choice={choice_val}, confidence={choice_conf})"
            )
        elif choice_val != "complete":
            side_gaps.append(
                f"linear: Jev linear_classification returned {choice_val!r}"
            )
        if side_gaps or human_review:
            return "incomplete", human_review, side_gaps
        return "complete", False, []

    if not jev_answers:
        # If Jev was skipped/unavailable when record exists, mark incomplete + needs_human_review.
        side_gaps.append(f"{side}: Jev advisory unavailable for semantic verification")
        return "incomplete", True, side_gaps

    if side == "linear":
        noul1_key = "linear_presence_and_traceability"
        noul2_key = "linear_ac_and_scope_completeness"
        score_key = "linear_completeness_score"
        choice_key = "linear_classification"
    else:
        noul1_key = "notion_presence_and_traceability"
        noul2_key = "notion_completeness_and_honesty"
        score_key = "notion_completeness_score"
        choice_key = "notion_classification"

    noul1 = (jev_answers.get(noul1_key) or {}).get("noul")
    noul2 = (jev_answers.get(noul2_key) or {}).get("noul")
    score_val = (jev_answers.get(score_key) or {}).get("score")
    choice_obj = jev_answers.get(choice_key) or {}
    choice_val = choice_obj.get("choice")
    choice_conf = choice_obj.get("confidence")

    if noul1 is None or float(noul1) < NOUL_PASS_FLOOR:
        side_gaps.append(f"{side}: Jev {noul1_key} noul={noul1} < {NOUL_PASS_FLOOR}")
    if noul2 is None or float(noul2) < NOUL_PASS_FLOOR:
        side_gaps.append(f"{side}: Jev {noul2_key} noul={noul2} < {NOUL_PASS_FLOOR}")
    if score_val is None or float(score_val) < SCORE_COMPLETE_FLOOR:
        side_gaps.append(f"{side}: Jev {score_key} score={score_val} < {SCORE_COMPLETE_FLOOR}")

    if (
        choice_val == "other"
        or choice_conf is None
        or float(choice_conf) < CHOICE_CONFIDENCE_FLOOR
    ):
        human_review = True
        side_gaps.append(
            f"{side}: Jev {choice_key} uncertain (choice={choice_val}, confidence={choice_conf})"
        )
    elif choice_val != "complete":
        side_gaps.append(f"{side}: Jev {choice_key} returned {choice_val!r}")

    remediation_obj = jev_answers.get("recommended_remediation") or {}
    rem_choice = remediation_obj.get("choice")
    rem_conf = remediation_obj.get("confidence")
    if rem_choice == "other" or (
        bool(remediation_obj)
        and (rem_conf is None or float(rem_conf) < CHOICE_CONFIDENCE_FLOOR)
    ):
        human_review = True
        side_gaps.append(
            f"{side}: Jev recommended_remediation uncertain (choice={rem_choice}, confidence={rem_conf})"
        )

    if side_gaps or human_review:
        return "incomplete", human_review, side_gaps
    return "complete", False, []


def derive_recommended_action(linear_status: str, notion_status: str) -> str:
    """Derive exact recommended_action enum matching PROJECT.md § Interface Contracts #3."""
    if linear_status == "complete" and notion_status == "complete":
        return "none"
    if linear_status == "missing" and notion_status == "complete":
        return "create_linear"
    if linear_status == "incomplete" and notion_status == "complete":
        return "update_linear"
    if linear_status == "complete" and notion_status == "missing":
        return "create_notion"
    if linear_status == "complete" and notion_status == "incomplete":
        return "update_notion"
    return "both"


def classify_requirement(
    requirement: Mapping[str, Any],
    linear_matches: list[Mapping[str, Any]],
    notion_matches: list[Mapping[str, Any]],
    *,
    questions: Mapping[str, Any],
    api_key: str | None = None,
    transport: HttpTransport | None = None,
    timeout_ms: int = DEFAULT_TIMEOUT_MS,
    allow_fallback: bool = True,
    skip_jev_on_missing: bool = True,
    repo_root: Path | None = _REPO_ROOT,
) -> dict[str, Any]:
    """Evaluate one requirement using script-first deterministic gates + Jev System One."""
    checks = evaluate_requirement_deterministic(
        requirement, linear_matches, notion_matches, repo_root=repo_root
    )
    gaps = _collect_deterministic_gaps(checks)

    active_notion_exists = bool(checks["P_NOT_EXISTS"])
    has_any_candidates = (
        len(linear_matches) > 0
        or active_notion_exists
        or bool(checks["shipped_verified_exempt"])
    )

    jev_result: dict[str, Any] | None = None
    jev_answers: dict[str, Any] = {}
    jev_model_used = PRIMARY_MODEL
    jev_decision_id: str | None = None

    should_query_jev = (not skip_jev_on_missing) or has_any_candidates
    if should_query_jev and api_key:
        state_str = build_compact_state(
            requirement,
            linear_matches,
            notion_matches,
            checks,
            questions=questions,
        )
        jev_result = decide_with_fallback(
            state_str,
            questions,
            api_key=api_key,
            transport=transport,
            timeout_ms=timeout_ms,
            allow_fallback=allow_fallback,
        )
        if jev_result.get("status") == "ok" and jev_result.get("response"):
            resp = jev_result["response"]
            jev_answers = dict(resp.get("answers") or {})
            jev_model_used = str(resp.get("model") or PRIMARY_MODEL)
            raw_decision_id = resp.get("id")
            jev_decision_id = (
                str(raw_decision_id) if raw_decision_id is not None else None
            )

    linear_status, lin_review, lin_jev_gaps = _evaluate_side_with_jev(
        side="linear",
        exists=bool(checks["P_LIN_EXISTS"]),
        contract_ok=bool(checks["P_LIN_CONTRACT"]),
        shipped_exempt=bool(checks["shipped_verified_exempt"]),
        isolation_ok=bool(checks["P_ISO"]),
        source_ok=bool(checks["P_SRC"]),
        jev_answers=jev_answers if jev_answers else None,
    )
    notion_status, not_review, not_jev_gaps = _evaluate_side_with_jev(
        side="notion",
        exists=bool(checks["P_NOT_EXISTS"]),
        contract_ok=bool(checks["P_NOT_CONTRACT"]),
        shipped_exempt=False,
        isolation_ok=bool(checks["P_ISO"]),
        source_ok=bool(checks["P_SRC"]),
        jev_answers=jev_answers if jev_answers else None,
    )

    gaps.extend(lin_jev_gaps)
    gaps.extend(not_jev_gaps)
    needs_human_review = lin_review or not_review

    action = derive_recommended_action(linear_status, notion_status)

    entry: dict[str, Any] = {
        "req_id": str(requirement.get("req_id") or ""),
        "source_file": str(requirement.get("source_file") or ""),
        "linear_matches": [
            str(m.get("id") or m.get("identifier") or m.get("url") or "")
            for m in linear_matches
        ],
        "notion_matches": [
            str(m.get("id") or m.get("page_id") or m.get("url") or "")
            for m in notion_matches
        ],
        "linear_status": linear_status,
        "notion_status": notion_status,
        "deterministic_checks": checks,
        "jev_decision_id": jev_decision_id,
        "jev_answers": jev_answers,
        "gaps": gaps,
        "recommended_action": action,
        "needs_human_review": needs_human_review,
        "_jev_model": jev_model_used,
    }
    if checks.get("shipped_verified_exempt"):
        entry["sub_status"] = "shipped_verified"
        ver_ev = requirement.get("verification_evidence")
        if ver_ev is None:
            ev_parts: list[str] = []
            raw_shipped_ev = requirement.get("shipped_evidence")
            if isinstance(raw_shipped_ev, str) and raw_shipped_ev.strip():
                ev_parts.append(_truncate(raw_shipped_ev, 300))
            elif isinstance(raw_shipped_ev, Mapping):
                ev_parts.append(
                    _truncate(json.dumps(dict(raw_shipped_ev), ensure_ascii=False), 300)
                )
            for sp in list(requirement.get("shipped_files") or [])[:6]:
                if str(sp).strip():
                    ev_parts.append(f"trunk_file:{sp}")
            for m in linear_matches:
                st = str(m.get("state") or m.get("status") or "").strip().lower()
                mid = str(m.get("id") or m.get("identifier") or "").strip()
                if st == "done" and bool(m.get("merged_to_main", True)) and mid:
                    ev_parts.append(f"merged_done:{mid}")
            if ev_parts:
                ver_ev = ev_parts
        if ver_ev is not None:
            entry["verification_evidence"] = ver_ev
    elif str(requirement.get("lifecycle_state") or "").lower() == "deferred_phase_n":
        entry["sub_status"] = "deferred_phase_n"
    return entry


def _extract_matches_for_req(
    matches_data: Any,
    req_id: str,
) -> tuple[list[Mapping[str, Any]], list[Mapping[str, Any]]]:
    if isinstance(matches_data, Mapping):
        if "by_req_id" in matches_data and isinstance(matches_data["by_req_id"], Mapping):
            item = matches_data["by_req_id"].get(req_id) or {}
            return list(item.get("linear") or []), list(item.get("notion") or [])
        if req_id in matches_data and isinstance(matches_data[req_id], Mapping):
            item = matches_data[req_id]
            return list(item.get("linear") or []), list(item.get("notion") or [])
        if "linear" in matches_data or "notion" in matches_data:
            lin = [
                m
                for m in (matches_data.get("linear") or [])
                if req_id in (m.get("matched_req_ids") or [m.get("req_id")])
            ]
            notn = [
                m
                for m in (matches_data.get("notion") or [])
                if req_id in (m.get("matched_req_ids") or [m.get("req_id")])
            ]
            return lin, notn
    return [], []


def evaluate_catalog(
    catalog: Mapping[str, Any],
    matches_data: Any,
    *,
    questions: Mapping[str, Any],
    api_key: str | None = None,
    transport: HttpTransport | None = None,
    timeout_ms: int = DEFAULT_TIMEOUT_MS,
    allow_fallback: bool = True,
    skip_jev_on_missing: bool = True,
    repo_root: Path | None = _REPO_ROOT,
) -> dict[str, Any]:
    """Evaluate an entire requirements-catalog.json and produce a gap matrix."""
    requirements = list(catalog.get("requirements") or [])
    evaluations: list[dict[str, Any]] = []
    linear_counts = {"complete": 0, "incomplete": 0, "missing": 0}
    notion_counts = {"complete": 0, "incomplete": 0, "missing": 0}
    observed_model = PRIMARY_MODEL
    covered_sources: set[str] = set()

    for req in requirements:
        req_id = str(req.get("req_id") or "")
        src = str(req.get("source_file") or "")
        if src:
            covered_sources.add(src)
        lin_matches, not_matches = _extract_matches_for_req(matches_data, req_id)
        ev = classify_requirement(
            req,
            lin_matches,
            not_matches,
            questions=questions,
            api_key=api_key,
            transport=transport,
            timeout_ms=timeout_ms,
            allow_fallback=allow_fallback,
            skip_jev_on_missing=skip_jev_on_missing,
            repo_root=repo_root,
        )
        model_used = ev.pop("_jev_model", PRIMARY_MODEL)
        if ev.get("jev_answers"):
            observed_model = model_used
        linear_counts[ev["linear_status"]] += 1
        notion_counts[ev["notion_status"]] += 1
        evaluations.append(ev)

    raw_source_files = list(catalog.get("source_files") or [])
    eval_sources = {
        norm
        for ev in evaluations
        if (norm := _normalize_repo_path(ev.get("source_file")))
    }
    in_scope_paths = {
        norm
        for sf in raw_source_files
        if isinstance(sf, Mapping)
        and sf.get("in_scope", True)
        and (norm := _normalize_repo_path(sf.get("path")))
    }
    out_of_scope_paths = {
        norm
        for sf in raw_source_files
        if isinstance(sf, Mapping)
        and not sf.get("in_scope", True)
        and (norm := _normalize_repo_path(sf.get("path")))
    }
    if in_scope_paths:
        source_files_covered = len(eval_sources & in_scope_paths)
    else:
        source_files_covered = len(eval_sources - out_of_scope_paths)

    matrix = {
        "evaluated_at": _utc_now_iso(),
        "rubric_version": RUBRIC_VERSION,
        "jev_model": observed_model,
        "summary": {
            "total_requirements": len(evaluations),
            "source_files_covered": source_files_covered,
            "linear": linear_counts,
            "notion": notion_counts,
        },
        "evaluations": evaluations,
    }
    validate_matrix(matrix)
    return matrix


def validate_matrix(matrix: Mapping[str, Any]) -> None:
    """Validate gap matrix JSON strictly against PROJECT.md § Interface Contracts #3."""
    required_top = (
        "evaluated_at",
        "rubric_version",
        "jev_model",
        "summary",
        "evaluations",
    )
    for key in required_top:
        if key not in matrix:
            raise ValueError(f"Matrix missing required top-level key: {key}")

    summary = matrix["summary"]
    evaluations = matrix["evaluations"]
    if not isinstance(evaluations, list):
        raise ValueError("Matrix 'evaluations' must be a list")
    if summary.get("total_requirements") != len(evaluations):
        raise ValueError(
            "Matrix summary.total_requirements does not match len(evaluations)"
        )

    lin_counts = {"complete": 0, "incomplete": 0, "missing": 0}
    not_counts = {"complete": 0, "incomplete": 0, "missing": 0}
    for idx, ev in enumerate(evaluations):
        for field in (
            "req_id",
            "source_file",
            "linear_matches",
            "notion_matches",
            "linear_status",
            "notion_status",
            "deterministic_checks",
            "jev_answers",
            "gaps",
            "recommended_action",
        ):
            if field not in ev:
                raise ValueError(f"Evaluation[{idx}] missing field {field!r}")
        if ev["linear_status"] not in ALLOWED_STATUSES:
            raise ValueError(f"Evaluation[{idx}] invalid linear_status {ev['linear_status']!r}")
        if ev["notion_status"] not in ALLOWED_STATUSES:
            raise ValueError(f"Evaluation[{idx}] invalid notion_status {ev['notion_status']!r}")
        if ev["recommended_action"] not in ALLOWED_ACTIONS:
            raise ValueError(
                f"Evaluation[{idx}] invalid recommended_action {ev['recommended_action']!r}"
            )
        lin_counts[ev["linear_status"]] += 1
        not_counts[ev["notion_status"]] += 1

    if summary.get("linear") != lin_counts:
        raise ValueError("Matrix summary.linear counts do not match evaluations")
    if summary.get("notion") != not_counts:
        raise ValueError("Matrix summary.notion counts do not match evaluations")


def render_markdown_matrix(matrix: Mapping[str, Any]) -> str:
    """Render a concise Markdown table summarizing a gap matrix."""
    s = matrix["summary"]
    lines = [
        f"# Backlog Gap Matrix ({matrix['rubric_version']})",
        "",
        f"- **Evaluated at:** `{matrix['evaluated_at']}`",
        f"- **Jev model:** `{matrix['jev_model']}`",
        f"- **Total requirements:** {s['total_requirements']} (source files covered: {s['source_files_covered']})",
        f"- **Linear:** complete={s['linear']['complete']}, incomplete={s['linear']['incomplete']}, missing={s['linear']['missing']}",
        f"- **Notion:** complete={s['notion']['complete']}, incomplete={s['notion']['incomplete']}, missing={s['notion']['missing']}",
        "",
        "| Req ID | Source File | Linear | Notion | Action | Human Review | Gaps |",
        "|--------|-------------|--------|--------|--------|--------------|------|",
    ]
    for ev in matrix["evaluations"]:
        gaps_text = "; ".join(ev.get("gaps") or []) or "-"
        review = "yes" if ev.get("needs_human_review") else "no"
        lines.append(
            f"| {ev['req_id']} | `{ev['source_file']}` | {ev['linear_status']} | "
            f"{ev['notion_status']} | `{ev['recommended_action']}` | {review} | {gaps_text} |"
        )
    lines.append("")
    return "\n".join(lines)


def _emit_json(payload: Mapping[str, Any], out: Path | None) -> None:
    text = json.dumps(payload, indent=2, ensure_ascii=False)
    print(text)
    if out is not None:
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(text + "\n", encoding="utf-8")


def _cmd_validate(args: argparse.Namespace) -> int:
    try:
        questions = load_questions(args.questions)
        if args.request is not None:
            req_raw = json.loads(args.request.read_text(encoding="utf-8"))
            byte_len = validate_request_body(req_raw)
            print(
                json.dumps(
                    {
                        "status": "valid",
                        "questions_count": len(questions),
                        "request_questions_count": len(req_raw.get("questions", {})),
                        "request_bytes": byte_len,
                        "max_bytes": MAX_BODY_BYTES,
                    },
                    indent=2,
                )
            )
        else:
            sample_req = {
                "model": PRIMARY_MODEL,
                "state": "validation_probe",
                "questions": questions,
            }
            byte_len = validate_request_body(sample_req)
            print(
                json.dumps(
                    {
                        "status": "valid",
                        "questions_count": len(questions),
                        "pack_request_bytes": byte_len,
                        "max_bytes": MAX_BODY_BYTES,
                    },
                    indent=2,
                )
            )
        return 0
    except Exception as exc:  # noqa: BLE001
        print(
            json.dumps({"status": "invalid", "error": redact_secrets(str(exc))}, indent=2),
            file=sys.stderr,
        )
        return 1


def _cmd_decide(args: argparse.Namespace) -> int:
    load_dotenv_files()
    raw_input = json.loads(args.input.read_text(encoding="utf-8"))
    if not isinstance(raw_input, Mapping):
        print(json.dumps({"status": "invalid", "error": "Input must be a JSON object"}), file=sys.stderr)
        return 1

    state = raw_input.get("state")
    questions = raw_input.get("questions")
    model_override = raw_input.get("model")

    request_body = {
        "model": model_override or PRIMARY_MODEL,
        "state": state,
        "questions": questions,
    }
    try:
        byte_len = validate_request_body(request_body)
    except ValueError as exc:
        print(json.dumps({"status": "invalid", "error": redact_secrets(str(exc))}, indent=2), file=sys.stderr)
        return 1

    if args.dry_request:
        print(
            f"[jev] dry-request valid: questions={len(questions)} bytes={byte_len}/{MAX_BODY_BYTES}",
            file=sys.stderr,
        )
        print(json.dumps(request_body, indent=2, ensure_ascii=False))
        return 0

    api_key = resolve_openrouter_key()
    if not api_key:
        blocked = {
            "status": "blocked",
            "model": request_body["model"],
            "question_ids": list(questions.keys()),
            "answers": None,
            "usage": None,
            "error": "Missing OPENROUTER_API_KEY in environment or .env",
        }
        _emit_json(blocked, args.out)
        return 2

    decision = decide_with_fallback(
        state,
        questions,
        api_key=api_key,
        timeout_ms=args.timeout_ms,
        allow_fallback=not args.no_fallback,
        model_override=model_override,
    )
    envelope = {
        "generated_at": _utc_now_iso(),
        "purpose": args.purpose,
        "decisions": [decision],
    }
    _emit_json(envelope, args.out)
    return 0 if decision.get("status") == "ok" else 1


def _cmd_eval(args: argparse.Namespace) -> int:
    load_dotenv_files()
    questions = load_questions(args.rubric)
    catalog = json.loads(args.catalog.read_text(encoding="utf-8"))
    matches_data = json.loads(args.matches.read_text(encoding="utf-8"))
    requirements = list(catalog.get("requirements") or [])

    if args.dry_request:
        sample_reqs = []
        for req in requirements[:3]:
            req_id = str(req.get("req_id") or "")
            lin_m, not_m = _extract_matches_for_req(matches_data, req_id)
            checks = evaluate_requirement_deterministic(
                req, lin_m, not_m, repo_root=args.repo_root
            )
            state_str = build_compact_state(
                req, lin_m, not_m, checks, questions=questions
            )
            body = {
                "model": PRIMARY_MODEL,
                "state": state_str,
                "questions": questions,
            }
            byte_len = validate_request_body(body)
            sample_reqs.append({"req_id": req_id, "bytes": byte_len, "request": body})
        print(json.dumps({"dry_requests": sample_reqs}, indent=2, ensure_ascii=False))
        return 0

    api_key = resolve_openrouter_key()

    def _req_needs_live_jev(req: Mapping[str, Any]) -> bool:
        if not args.skip_jev_on_missing:
            return True
        lin_m, not_m = _extract_matches_for_req(
            matches_data, str(req.get("req_id") or "")
        )
        active_not_m = [m for m in not_m if not _is_frozen_mirror_match(m)]
        return (
            bool(lin_m)
            or bool(active_not_m)
            or _is_shipped_verified(
                req, linear_matches=lin_m, repo_root=args.repo_root
            )
        )

    needs_live_jev = any(_req_needs_live_jev(r) for r in requirements)
    if needs_live_jev and not api_key:
        blocked = {
            "status": "blocked",
            "model": PRIMARY_MODEL,
            "error": "Missing OPENROUTER_API_KEY in environment or .env",
        }
        _emit_json(blocked, args.out)
        return 2

    matrix = evaluate_catalog(
        catalog,
        matches_data,
        questions=questions,
        api_key=api_key,
        timeout_ms=args.timeout_ms,
        allow_fallback=not args.no_fallback,
        skip_jev_on_missing=args.skip_jev_on_missing,
        repo_root=args.repo_root,
    )
    _emit_json(matrix, args.out)
    if args.markdown_out is not None:
        args.markdown_out.parent.mkdir(parents=True, exist_ok=True)
        args.markdown_out.write_text(
            render_markdown_matrix(matrix), encoding="utf-8"
        )
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Project Aegis typesafe ai jev decision client & backlog gap evaluator."
    )
    sub = parser.add_subparsers(dest="command", required=True)

    p_val = sub.add_parser(
        "validate", help="Validate a question pack and optional request JSON offline."
    )
    p_val.add_argument(
        "--questions",
        type=Path,
        default=DEFAULT_RUBRIC,
        help=f"Pinned question pack JSON (default: {DEFAULT_RUBRIC})",
    )
    p_val.add_argument(
        "--request",
        type=Path,
        default=None,
        help="Optional full request JSON ({state, questions}) to validate.",
    )

    p_dec = sub.add_parser(
        "decide",
        help="Execute a batched System One decision payload via OpenRouter Jev 1.14 (with 1.13 fallback).",
    )
    p_dec.add_argument(
        "--input",
        type=Path,
        required=True,
        help="JSON file containing {model?, state, questions}.",
    )
    p_dec.add_argument(
        "--out",
        type=Path,
        default=None,
        help="Optional path to write provenance JSON envelope.",
    )
    p_dec.add_argument(
        "--purpose",
        type=str,
        default="M1 open-scope and completeness calibration decisions via typesafe ai jev",
        help="Provenance purpose string.",
    )
    p_dec.add_argument(
        "--dry-request",
        action="store_true",
        help="Validate and print request JSON without making network calls.",
    )
    p_dec.add_argument(
        "--no-fallback",
        action="store_true",
        help="Disable fallback to /api/v1/systemone (typesafe/jev-1.13).",
    )
    p_dec.add_argument(
        "--timeout-ms",
        type=int,
        default=DEFAULT_TIMEOUT_MS,
        help=f"Overall request deadline in ms (default: {DEFAULT_TIMEOUT_MS}).",
    )

    p_eval = sub.add_parser(
        "eval",
        help="Evaluate requirements-catalog.json against Linear & Notion backlog matches.",
    )
    p_eval.add_argument("--catalog", type=Path, required=True, help="requirements-catalog.json path.")
    p_eval.add_argument("--matches", type=Path, required=True, help="Backlog matches JSON path.")
    p_eval.add_argument(
        "--rubric",
        type=Path,
        default=DEFAULT_RUBRIC,
        help=f"Pinned question pack JSON (default: {DEFAULT_RUBRIC})",
    )
    p_eval.add_argument("--out", type=Path, default=None, help="Output gap matrix JSON path.")
    p_eval.add_argument("--markdown-out", type=Path, default=None, help="Optional Markdown summary output path.")
    p_eval.add_argument("--dry-request", action="store_true", help="Print sample compact Jev requests without HTTP calls.")
    p_eval.add_argument(
        "--skip-jev-on-missing",
        dest="skip_jev_on_missing",
        action="store_true",
        default=True,
        help="Skip Jev calls when both Linear & Notion matches are empty (default).",
    )
    p_eval.add_argument(
        "--force-jev",
        dest="skip_jev_on_missing",
        action="store_false",
        help="Force Jev calls even when matches are empty.",
    )
    p_eval.add_argument("--no-fallback", action="store_true", help="Disable fallback to jev-1.13.")
    p_eval.add_argument(
        "--timeout-ms",
        type=int,
        default=DEFAULT_TIMEOUT_MS,
        help=f"Overall request deadline in ms (default: {DEFAULT_TIMEOUT_MS}).",
    )
    p_eval.add_argument(
        "--repo-root",
        type=Path,
        default=_REPO_ROOT,
        help="Repository root for checking source_file existence.",
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    if args.command == "validate":
        return _cmd_validate(args)
    if args.command == "decide":
        return _cmd_decide(args)
    if args.command == "eval":
        return _cmd_eval(args)
    parser.print_help()
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
