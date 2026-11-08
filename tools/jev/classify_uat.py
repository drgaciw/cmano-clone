#!/usr/bin/env python3
"""Off-tick Jev 1.13 UAT classifier CLI (advisory only).

POSTs filtered UAT evidence to TypeSafe, OpenRouter, Cloudflare, or Vercel.
Never imported by ProjectAegis.Sim or Delegation core. On any error,
degrades to no_advisory.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import threading
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Mapping, Protocol

TYPESAFE_MODEL_ID = "jev-1.13.0"
OPENROUTER_MODEL_ID = "typesafe/jev-1.13"
CLOUDFLARE_MODEL_ID = "typesafe/jev"
VERCEL_MODEL_ID = "typesafe-ai/jev"
VERCEL_SYSTEMONE_URL = "https://ai-gateway.vercel.sh/typesafe/v1/systemone"
# Backward-compatible aliases (TypeSafe is the preferred / default pin).
MODEL_ID = TYPESAFE_MODEL_ID
TYPESAFE_SYSTEMONE_URL = "https://api.typesafe.ai/v1/systemone"
OPENROUTER_SYSTEMONE_URL = "https://openrouter.ai/api/v1/systemone"
SYSTEMONE_URL = TYPESAFE_SYSTEMONE_URL
BUDGET_MS = 800

_REPO_ROOT = Path(__file__).resolve().parents[2]
_PARENT_ROOT = _REPO_ROOT.parent
_DEFAULT_RUBRIC = _REPO_ROOT / "data" / "jev" / "uat-v1.questions.json"


@dataclass(frozen=True)
class UatRow:
    id: str
    result: str
    observation: str


@dataclass(frozen=True)
class UatEvidencePacket:
    policy_id: str
    rows: list[UatRow]


@dataclass(frozen=True)
class ApiRoute:
    """Resolved HTTP target for a System One classify call."""

    provider: str
    url: str
    model: str
    api_key: str


class HttpTransport(Protocol):
    def post(
        self,
        url: str,
        *,
        headers: Mapping[str, str],
        json_body: Mapping[str, Any],
        timeout: float,
    ) -> Any: ...


class UrllibTransport:
    """Minimal POST transport with a hard timeout (seconds)."""

    def post(
        self,
        url: str,
        *,
        headers: Mapping[str, str],
        json_body: Mapping[str, Any],
        timeout: float,
    ) -> Any:
        """POST and return a response object, bounded by an overall deadline.

        ``urlopen(timeout=...)`` only bounds individual socket operations, so a
        slow-drip ``resp.read()`` could exceed the budget. The whole request
        (connect, send, read) runs in a daemon worker joined with ``timeout``;
        if it has not finished, ``TimeoutError`` is raised and callers degrade.
        """
        result: dict[str, Any] = {}

        def _worker() -> None:
            try:
                result["value"] = self._post_blocking(
                    url, headers=headers, json_body=json_body, timeout=timeout
                )
            except BaseException as exc:  # surfaced to caller below
                result["error"] = exc

        worker = threading.Thread(target=_worker, name="jev-uat-post", daemon=True)
        worker.start()
        worker.join(timeout)
        if worker.is_alive():
            raise TimeoutError(f"System One request exceeded {timeout:.3f}s overall budget")
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
        data = json.dumps(json_body).encode("utf-8")
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


def load_questions(path: Path) -> dict[str, Any]:
    raw = json.loads(path.read_text(encoding="utf-8"))
    questions = raw.get("questions")
    if not isinstance(questions, dict) or not questions:
        raise ValueError(f"rubric missing questions map: {path}")
    return questions


def load_evidence_packet(path: Path) -> UatEvidencePacket:
    raw = json.loads(path.read_text(encoding="utf-8"))
    policy_id = str(raw["policy_id"])
    rows_raw = raw.get("rows")
    if not isinstance(rows_raw, list):
        raise ValueError("evidence.rows must be a list")
    rows: list[UatRow] = []
    for item in rows_raw:
        rows.append(
            UatRow(
                id=str(item["id"]),
                result=str(item["result"]).lower(),
                observation=str(item["observation"]),
            )
        )
    return UatEvidencePacket(policy_id=policy_id, rows=rows)


def build_systemone_request(
    packet: UatEvidencePacket,
    questions: Mapping[str, Any],
    *,
    model: str = TYPESAFE_MODEL_ID,
) -> dict[str, Any]:
    """Build the System One request body (TypeSafe or OpenRouter).

    State is only the filtered UAT row list (id, pass/fail, one-line observation,
    policy id). No screenshots, catalogs, coordinates, or secrets.
    """
    state_rows = [
        {
            "id": row.id,
            "result": row.result,
            "observation": row.observation,
            "policy_id": packet.policy_id,
        }
        for row in packet.rows
    ]
    return {
        "model": model,
        "state": {
            "policy_id": packet.policy_id,
            "rows": state_rows,
        },
        "questions": dict(questions),
    }


def parse_advisory(response_body: Mapping[str, Any]) -> dict[str, Any]:
    answers = response_body.get("answers")
    if not isinstance(answers, dict):
        raise ValueError("response missing answers map")
    usage = response_body.get("usage") or {}
    question_ids = list(answers.keys())
    return {
        "status": "ok",
        "model": str(response_body.get("model") or MODEL_ID),
        "question_ids": question_ids,
        "answers": dict(answers),
        "usage": {
            "input_tokens": usage.get("input_tokens"),
            "output_tokens": usage.get("output_tokens"),
        },
    }


def _no_advisory(question_ids: list[str], error: str, *, model: str = MODEL_ID) -> dict[str, Any]:
    return {
        "status": "no_advisory",
        "model": model,
        "question_ids": question_ids,
        "answers": None,
        "usage": None,
        "error": error,
    }


def classify_or_degrade(
    packet: UatEvidencePacket,
    questions: Mapping[str, Any],
    *,
    route: ApiRoute,
    transport: HttpTransport | None = None,
    timeout_ms: int = BUDGET_MS,
) -> dict[str, Any]:
    question_ids = list(questions.keys())
    body = build_systemone_request(packet, questions, model=route.model)
    if route.provider == "cloudflare":
        body = {"model": body.pop("model"), "input": body}
    client = transport or UrllibTransport()
    timeout_s = timeout_ms / 1000.0
    headers = {
        "Authorization": f"Bearer {route.api_key}",
        "Content-Type": "application/json",
        "Accept": "application/json",
    }
    try:
        resp = client.post(
            route.url,
            headers=headers,
            json_body=body,
            timeout=timeout_s,
        )
    except Exception as exc:  # noqa: BLE001 — degrade is the product requirement
        return _no_advisory(
            question_ids, f"{type(exc).__name__}: {exc}", model=route.model
        )

    status = getattr(resp, "status_code", None)
    if status is None or int(status) < 200 or int(status) >= 300:
        text = getattr(resp, "text", "") or ""
        return _no_advisory(
            question_ids, f"http_{status}: {text[:200]}", model=route.model
        )

    try:
        payload = resp.json()
        if route.provider == "cloudflare" and isinstance(payload, dict):
            if payload.get("success") is False:
                raise ValueError("Cloudflare reported an unsuccessful request")
            if "result" in payload:
                payload = payload["result"]
        return parse_advisory(payload)
    except Exception as exc:  # noqa: BLE001
        return _no_advisory(
            question_ids, f"parse_error: {exc}", model=route.model
        )


def _env_value(name: str) -> str | None:
    value = os.environ.get(name)
    if value and value.strip():
        return value.strip()
    return None


def resolve_route(provider: str = "auto") -> ApiRoute | None:
    """Resolve credentials in TypeSafe, OpenRouter, Cloudflare, Vercel order."""
    typesafe = _env_value("TYPESAFE_API_KEY")
    if typesafe and provider in ("auto", "typesafe"):
        return ApiRoute(
            provider="typesafe",
            url=TYPESAFE_SYSTEMONE_URL,
            model=TYPESAFE_MODEL_ID,
            api_key=typesafe,
        )
    openrouter = _env_value("OPENROUTER_API_KEY")
    if openrouter and provider in ("auto", "openrouter"):
        return ApiRoute(
            provider="openrouter",
            url=OPENROUTER_SYSTEMONE_URL,
            model=OPENROUTER_MODEL_ID,
            api_key=openrouter,
        )
    token = _env_value("CLOUDFLARE_API_TOKEN")
    account = _env_value("CLOUDFLARE_ACCOUNT_ID")
    if token and account and provider in ("auto", "cloudflare"):
        from urllib.parse import quote

        return ApiRoute(
            provider="cloudflare",
            url=f"https://api.cloudflare.com/client/v4/accounts/{quote(account, safe='')}/ai/run",
            model=CLOUDFLARE_MODEL_ID,
            api_key=token,
        )
    gateway_key = _env_value("AI_GATEWAY_API_KEY") or _env_value("VERCEL_OIDC_TOKEN")
    if gateway_key and provider in ("auto", "vercel"):
        return ApiRoute(
            provider="vercel",
            url=VERCEL_SYSTEMONE_URL,
            model=VERCEL_MODEL_ID,
            api_key=gateway_key,
        )
    return None


def resolve_api_key() -> str | None:
    """Legacy helper: returns the preferred key string, or None."""
    route = resolve_route()
    return route.api_key if route else None


def load_dotenv_files() -> Path | None:
    """Load KEY=VALUE pairs from the first existing .env candidate.

    Search order: repo `.env`, repo `.env.local`, `tools/jev/.env`, then
    parent-of-repo `.env` (e.g. `C:\\Users\\…\\cmano-clone\\.env`).
    Does not overwrite already-set process env vars. Returns the path loaded.
    """
    candidates = [
        _REPO_ROOT / ".env",
        _REPO_ROOT / ".env.local",
        _REPO_ROOT / "tools" / "jev" / ".env",
        _PARENT_ROOT / ".env",
    ]
    for path in candidates:
        if not path.is_file():
            continue
        for line in path.read_text(encoding="utf-8").splitlines():
            stripped = line.strip()
            if not stripped or stripped.startswith("#"):
                continue
            if "=" not in stripped:
                continue
            key, _, raw_val = stripped.partition("=")
            key = key.strip()
            if not key or key in os.environ:
                continue
            val = raw_val.strip().strip("'").strip('"')
            os.environ[key] = val
        return path
    return None


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Classify filtered UAT evidence with Jev 1.13 (advisory only)."
    )
    parser.add_argument(
        "--evidence",
        type=Path,
        required=True,
        help="JSON evidence packet: {policy_id, rows:[{id,result,observation}]}",
    )
    parser.add_argument(
        "--rubric",
        type=Path,
        default=_DEFAULT_RUBRIC,
        help=f"Pinned questions JSON (default: {_DEFAULT_RUBRIC})",
    )
    parser.add_argument(
        "--out",
        type=Path,
        default=None,
        help="Optional path to write advisory JSON (also printed to stdout).",
    )
    parser.add_argument(
        "--dry-request",
        action="store_true",
        help="Print the selected provider's request body without calling the API.",
    )
    parser.add_argument(
        "--provider", choices=("auto", "typesafe", "openrouter", "cloudflare", "vercel"),
        default="auto", help="Provider selection (default: auto).",
    )
    args = parser.parse_args(argv)

    load_dotenv_files()
    questions = load_questions(args.rubric)
    packet = load_evidence_packet(args.evidence)
    route = resolve_route(args.provider)
    request_model = route.model if route else {
        "cloudflare": CLOUDFLARE_MODEL_ID,
        "openrouter": OPENROUTER_MODEL_ID,
        "vercel": VERCEL_MODEL_ID,
    }.get(args.provider, TYPESAFE_MODEL_ID)
    request_body = build_systemone_request(packet, questions, model=request_model)
    if args.provider == "cloudflare" or (route and route.provider == "cloudflare"):
        request_body = {"model": request_body.pop("model"), "input": request_body}

    if args.dry_request:
        print(json.dumps(request_body, indent=2))
        return 0

    if not route:
        blocked = {
            "status": "blocked",
            "model": request_model,
            "question_ids": list(questions.keys()),
            "answers": None,
            "usage": None,
            "error": "Missing credentials for selected provider: TYPESAFE_API_KEY, "
            "OPENROUTER_API_KEY, CLOUDFLARE_API_TOKEN and CLOUDFLARE_ACCOUNT_ID, "
            "or AI_GATEWAY_API_KEY / VERCEL_OIDC_TOKEN",
        }
        _emit(blocked, args.out)
        return 2

    print(
        f"[jev] provider={route.provider} url={route.url} model={route.model}",
        file=sys.stderr,
    )
    result = classify_or_degrade(packet, questions, route=route)
    _log_summary(result)
    _emit(result, args.out)
    return 0 if result.get("status") == "ok" else 1


def _log_summary(result: Mapping[str, Any]) -> None:
    usage = result.get("usage") or {}
    print(
        f"[jev] model={result.get('model')} "
        f"status={result.get('status')} "
        f"question_ids={result.get('question_ids')} "
        f"input_tokens={usage.get('input_tokens')} "
        f"output_tokens={usage.get('output_tokens')}",
        file=sys.stderr,
    )


def _emit(payload: Mapping[str, Any], out: Path | None) -> None:
    text = json.dumps(payload, indent=2)
    print(text)
    if out is not None:
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(text + "\n", encoding="utf-8")


if __name__ == "__main__":
    raise SystemExit(main())
