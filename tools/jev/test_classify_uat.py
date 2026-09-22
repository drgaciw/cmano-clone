#!/usr/bin/env python3
"""Fixture tests for tools/jev/classify_uat.py (request builder + degrade path)."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from unittest.mock import MagicMock

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "jev"))

from classify_uat import (  # noqa: E402
    BUDGET_MS,
    MODEL_ID,
    OPENROUTER_MODEL_ID,
    OPENROUTER_SYSTEMONE_URL,
    SYSTEMONE_URL,
    TYPESAFE_MODEL_ID,
    ApiRoute,
    UatEvidencePacket,
    UatRow,
    build_systemone_request,
    classify_or_degrade,
    load_evidence_packet,
    load_questions,
    parse_advisory,
    resolve_route,
)

FIXTURES = Path(__file__).resolve().parent / "fixtures"
RUBRIC = ROOT / "data" / "jev" / "uat-v1.questions.json"


@pytest.mark.parametrize("key,oidc,expected", [
    ("gateway-key", "oidc-token", "gateway-key"),
    ("", "oidc-token", "oidc-token"),
    (" ", " ", None),
])
def test_vercel_auth_and_explicit_routing(monkeypatch, key, oidc, expected):
    monkeypatch.setenv("AI_GATEWAY_API_KEY", key)
    monkeypatch.setenv("VERCEL_OIDC_TOKEN", oidc)
    monkeypatch.setenv("TYPESAFE_API_KEY", "other-provider-key")
    route = resolve_route("vercel")
    if expected is None:
        assert route is None
    else:
        assert route.api_key == expected
        assert route.model == "typesafe-ai/jev"
        assert route.url == "https://ai-gateway.vercel.sh/typesafe/v1/systemone"
    assert resolve_route().provider == "typesafe"


def test_vercel_auto_is_last_provider(monkeypatch):
    for name in ("TYPESAFE_API_KEY", "OPENROUTER_API_KEY", "CLOUDFLARE_API_TOKEN",
                 "CLOUDFLARE_ACCOUNT_ID", "VERCEL_OIDC_TOKEN"):
        monkeypatch.delenv(name, raising=False)
    monkeypatch.setenv("AI_GATEWAY_API_KEY", "gateway-key")
    assert resolve_route().provider == "vercel"
    monkeypatch.setenv("CLOUDFLARE_API_TOKEN", "cloudflare-key")
    monkeypatch.setenv("CLOUDFLARE_ACCOUNT_ID", "account")
    assert resolve_route().provider == "cloudflare"


@pytest.mark.parametrize("failure", [None, "timeout", "http", "json"])
def test_vercel_transport_preserves_typesafe_contract(monkeypatch, failure):
    monkeypatch.setenv("AI_GATEWAY_API_KEY", "gateway-key")
    route = resolve_route("vercel")
    packet = load_evidence_packet(FIXTURES / "sample-evidence.json")
    questions = load_questions(RUBRIC)
    recorded = json.loads((FIXTURES / "systemone-success.json").read_text())
    recorded["model"] = "typesafe-ai/jev"
    transport = MagicMock()
    transport.post.return_value.status_code = 200
    transport.post.return_value.json.return_value = recorded
    if failure == "timeout":
        transport.post.side_effect = TimeoutError("budget exceeded")
    elif failure == "http":
        transport.post.return_value.status_code = 429
        transport.post.return_value.text = "rate limited"
    elif failure == "json":
        transport.post.return_value.json.side_effect = ValueError("invalid JSON")
    result = classify_or_degrade(packet, questions, route=route, transport=transport)
    assert result["status"] == ("no_advisory" if failure else "ok")
    assert result["model"] == "typesafe-ai/jev"
    if not failure:
        assert result["answers"] == recorded["answers"]
        assert result["usage"] == recorded["usage"]
    transport.post.assert_called_once()
    call = transport.post.call_args
    assert call.args[0] == "https://ai-gateway.vercel.sh/typesafe/v1/systemone"
    assert call.kwargs["headers"]["Authorization"] == "Bearer gateway-key"
    assert call.kwargs["json_body"] == build_systemone_request(
        packet, questions, model="typesafe-ai/jev"
    )
    assert call.kwargs["timeout"] == 0.8


def test_vercel_cli_dry_run_and_missing_credentials(monkeypatch, capsys):
    import classify_uat

    monkeypatch.setattr(classify_uat, "load_dotenv_files", lambda: None)
    monkeypatch.delenv("AI_GATEWAY_API_KEY", raising=False)
    monkeypatch.delenv("VERCEL_OIDC_TOKEN", raising=False)
    monkeypatch.setenv("TYPESAFE_API_KEY", "unrelated-key")
    args = ["--provider", "vercel", "--evidence", str(FIXTURES / "sample-evidence.json")]
    assert classify_uat.main(args + ["--dry-request"]) == 0
    body = json.loads(capsys.readouterr().out)
    assert body["model"] == "typesafe-ai/jev"
    assert body["questions"]["needs_code_change"]["type"] == "noul"
    assert "unrelated-key" not in json.dumps(body)
    assert classify_uat.main(args) == 2
    assert json.loads(capsys.readouterr().out)["status"] == "blocked"


@pytest.mark.parametrize("wrapped", [False, True])
def test_cloudflare_request_and_response(monkeypatch, wrapped):
    monkeypatch.setenv("CLOUDFLARE_API_TOKEN", "cf-test-token")
    monkeypatch.setenv("CLOUDFLARE_ACCOUNT_ID", "test-account")
    monkeypatch.setenv("TYPESAFE_API_KEY", "ts-key")
    route = resolve_route("cloudflare")
    assert route.provider == "cloudflare"
    assert route.url == "https://api.cloudflare.com/client/v4/accounts/test-account/ai/run"
    recorded = json.loads((FIXTURES / "systemone-success.json").read_text())
    transport = MagicMock()
    transport.post.return_value.status_code = 200
    transport.post.return_value.json.return_value = (
        {"success": True, "result": recorded} if wrapped else recorded
    )
    questions = load_questions(RUBRIC)
    packet = load_evidence_packet(FIXTURES / "sample-evidence.json")
    result = classify_or_degrade(packet, questions, route=route, transport=transport)
    assert result["status"] == "ok"
    assert result["answers"] == recorded["answers"]
    assert result["usage"] == recorded["usage"]
    request = transport.post.call_args.kwargs
    assert request["headers"]["Authorization"] == "Bearer cf-test-token"
    assert request["timeout"] == 0.8
    expected = build_systemone_request(packet, questions)
    assert request["json_body"] == {
        "model": "typesafe/jev",
        "input": {"state": expected["state"], "questions": questions},
    }


@pytest.mark.parametrize("payload", [
    {"success": False, "result": {"answers": {}}},
    {"success": True, "result": None},
    {"success": True, "result": {}},
])
def test_cloudflare_invalid_response_degrades(payload):
    transport = MagicMock()
    transport.post.return_value.status_code = 200
    transport.post.return_value.json.return_value = payload
    result = classify_or_degrade(
        load_evidence_packet(FIXTURES / "sample-evidence.json"), load_questions(RUBRIC),
        route=ApiRoute("cloudflare", "https://api.cloudflare.com/test", "typesafe/jev", "test"),
        transport=transport,
    )
    assert result["status"] == "no_advisory"


def test_cloudflare_credentials_and_auto_precedence(monkeypatch):
    monkeypatch.delenv("TYPESAFE_API_KEY", raising=False)
    monkeypatch.delenv("OPENROUTER_API_KEY", raising=False)
    monkeypatch.setenv("CLOUDFLARE_API_TOKEN", "cf-token")
    monkeypatch.delenv("CLOUDFLARE_ACCOUNT_ID", raising=False)
    assert resolve_route("cloudflare") is None
    monkeypatch.setenv("CLOUDFLARE_ACCOUNT_ID", "account")
    assert resolve_route().provider == "cloudflare"
    monkeypatch.setenv("OPENROUTER_API_KEY", "or-key")
    assert resolve_route().provider == "openrouter"
    monkeypatch.setenv("TYPESAFE_API_KEY", "ts-key")
    assert resolve_route().provider == "typesafe"


def test_cloudflare_cli_dry_request_and_missing_credentials(monkeypatch, capsys):
    import classify_uat

    monkeypatch.setattr(classify_uat, "load_dotenv_files", lambda: None)
    monkeypatch.delenv("CLOUDFLARE_API_TOKEN", raising=False)
    monkeypatch.delenv("CLOUDFLARE_ACCOUNT_ID", raising=False)
    args = ["--provider", "cloudflare", "--evidence", str(FIXTURES / "sample-evidence.json")]
    assert classify_uat.main(args + ["--dry-request"]) == 0
    body = json.loads(capsys.readouterr().out)
    assert body["model"] == "typesafe/jev"
    assert set(body["input"]) == {"state", "questions"}
    assert classify_uat.main(args) == 2
    assert json.loads(capsys.readouterr().out)["status"] == "blocked"


def test_load_questions_includes_other_and_three_ids():
    questions = load_questions(RUBRIC)
    assert set(questions.keys()) == {"failure_class", "severity", "needs_code_change"}
    assert questions["failure_class"]["type"] == "choice"
    assert "other" in questions["failure_class"]["criteria"]
    assert "none" in questions["failure_class"]["criteria"]
    assert questions["severity"]["type"] == "score"
    assert questions["needs_code_change"]["type"] == "noul"


def test_build_systemone_request_shape():
    questions = load_questions(RUBRIC)
    packet = UatEvidencePacket(
        policy_id="baltic-patrol-classify",
        rows=[
            UatRow(
                id="UAT-TYPE",
                result="fail",
                observation="Labels too small in Game view.",
            )
        ],
    )
    body = build_systemone_request(packet, questions)
    assert body["model"] == MODEL_ID
    assert body["model"] == "jev-1.13.0"
    assert set(body["questions"].keys()) == {
        "failure_class",
        "severity",
        "needs_code_change",
    }
    state = body["state"]
    assert state["policy_id"] == "baltic-patrol-classify"
    assert len(state["rows"]) == 1
    row = state["rows"][0]
    assert row == {
        "id": "UAT-TYPE",
        "result": "fail",
        "observation": "Labels too small in Game view.",
        "policy_id": "baltic-patrol-classify",
    }
    # Hygiene: no screenshots, catalogs, or secrets in state.
    blob = json.dumps(body)
    assert "screenshot" not in blob.lower()
    assert ".db3" not in blob
    assert "api_key" not in blob.lower()


def test_load_evidence_packet_from_fixture():
    packet = load_evidence_packet(FIXTURES / "sample-evidence.json")
    assert packet.policy_id == "baltic-patrol-classify"
    assert [r.id for r in packet.rows] == ["UAT-START", "UAT-TYPE", "UAT-MISSION"]
    assert packet.rows[1].result == "fail"


def test_parse_advisory_from_recorded_http_body():
    recorded = json.loads((FIXTURES / "systemone-success.json").read_text(encoding="utf-8"))
    advisory = parse_advisory(recorded)
    assert advisory["status"] == "ok"
    assert advisory["model"] == "jev-1.13.0"
    assert advisory["question_ids"] == [
        "failure_class",
        "severity",
        "needs_code_change",
    ]
    assert advisory["usage"]["input_tokens"] == 412
    assert advisory["answers"]["failure_class"]["choice"] == "presentation"
    assert advisory["answers"]["needs_code_change"]["noul"] == 0.81


def test_classify_degrades_on_http_error():
    questions = load_questions(RUBRIC)
    packet = load_evidence_packet(FIXTURES / "sample-evidence.json")
    transport = MagicMock()
    transport.post.side_effect = TimeoutError("simulated budget exceeded")
    route = ApiRoute(
        provider="typesafe",
        url=SYSTEMONE_URL,
        model=MODEL_ID,
        api_key="test-key-not-real",
    )
    result = classify_or_degrade(
        packet,
        questions,
        route=route,
        transport=transport,
        timeout_ms=BUDGET_MS,
    )
    assert result["status"] == "no_advisory"
    assert result["model"] == MODEL_ID
    assert result["question_ids"] == [
        "failure_class",
        "severity",
        "needs_code_change",
    ]
    assert "error" in result
    transport.post.assert_called_once()
    call_kwargs = transport.post.call_args
    assert call_kwargs.args[0] == SYSTEMONE_URL or call_kwargs[0][0] == SYSTEMONE_URL


def test_classify_degrades_on_non_2xx():
    questions = load_questions(RUBRIC)
    packet = load_evidence_packet(FIXTURES / "sample-evidence.json")
    response = MagicMock()
    response.status_code = 529
    response.text = '{"error":"overloaded"}'
    response.json.side_effect = ValueError("not json answers")
    transport = MagicMock()
    transport.post.return_value = response
    route = ApiRoute(
        provider="typesafe",
        url=SYSTEMONE_URL,
        model=MODEL_ID,
        api_key="test-key-not-real",
    )
    result = classify_or_degrade(
        packet,
        questions,
        route=route,
        transport=transport,
        timeout_ms=BUDGET_MS,
    )
    assert result["status"] == "no_advisory"
    assert result["model"] == MODEL_ID


def test_classify_ok_with_recorded_body_via_transport():
    questions = load_questions(RUBRIC)
    packet = load_evidence_packet(FIXTURES / "sample-evidence.json")
    recorded = json.loads((FIXTURES / "systemone-success.json").read_text(encoding="utf-8"))
    response = MagicMock()
    response.status_code = 200
    response.json.return_value = recorded
    transport = MagicMock()
    transport.post.return_value = response
    route = ApiRoute(
        provider="typesafe",
        url=SYSTEMONE_URL,
        model=MODEL_ID,
        api_key="test-key-not-real",
    )
    result = classify_or_degrade(
        packet,
        questions,
        route=route,
        transport=transport,
        timeout_ms=BUDGET_MS,
    )
    assert result["status"] == "ok"
    assert result["usage"]["input_tokens"] == 412
    assert abs(transport.post.call_args.kwargs.get("timeout", 0) - (BUDGET_MS / 1000.0)) < 1e-9


def test_budget_constant_is_800ms():
    assert BUDGET_MS == 800


def test_resolve_route_openrouter_only(monkeypatch: pytest.MonkeyPatch):
    monkeypatch.delenv("TYPESAFE_API_KEY", raising=False)
    monkeypatch.setenv("OPENROUTER_API_KEY", "or-test-key")
    route = resolve_route()
    assert route is not None
    assert route.provider == "openrouter"
    assert route.url == OPENROUTER_SYSTEMONE_URL
    assert route.url == "https://openrouter.ai/api/v1/systemone"
    assert "chat/completions" not in route.url
    assert route.model == OPENROUTER_MODEL_ID
    assert route.model == "typesafe/jev-1.13"
    assert route.api_key == "or-test-key"


def test_resolve_route_typesafe_only(monkeypatch: pytest.MonkeyPatch):
    monkeypatch.delenv("OPENROUTER_API_KEY", raising=False)
    monkeypatch.setenv("TYPESAFE_API_KEY", "ts-test-key")
    route = resolve_route()
    assert route is not None
    assert route.provider == "typesafe"
    assert route.url == SYSTEMONE_URL
    assert route.url == "https://api.typesafe.ai/v1/systemone"
    assert route.model == TYPESAFE_MODEL_ID
    assert route.model == "jev-1.13.0"
    assert route.api_key == "ts-test-key"


def test_resolve_route_prefers_typesafe_when_both(monkeypatch: pytest.MonkeyPatch):
    monkeypatch.setenv("TYPESAFE_API_KEY", "ts-preferred")
    monkeypatch.setenv("OPENROUTER_API_KEY", "or-fallback")
    route = resolve_route()
    assert route is not None
    assert route.provider == "typesafe"
    assert route.api_key == "ts-preferred"
    assert route.model == "jev-1.13.0"


def test_classify_openrouter_posts_to_openrouter_systemone(
    monkeypatch: pytest.MonkeyPatch,
):
    monkeypatch.delenv("TYPESAFE_API_KEY", raising=False)
    monkeypatch.setenv("OPENROUTER_API_KEY", "or-live-key")
    questions = load_questions(RUBRIC)
    packet = load_evidence_packet(FIXTURES / "sample-evidence.json")
    recorded = json.loads((FIXTURES / "systemone-success.json").read_text(encoding="utf-8"))
    recorded = {**recorded, "model": "typesafe/jev-1.13"}
    response = MagicMock()
    response.status_code = 200
    response.json.return_value = recorded
    transport = MagicMock()
    transport.post.return_value = response

    route = resolve_route()
    assert route is not None
    result = classify_or_degrade(
        packet,
        questions,
        route=route,
        transport=transport,
        timeout_ms=BUDGET_MS,
    )
    assert result["status"] == "ok"
    call_args = transport.post.call_args
    assert call_args.args[0] == OPENROUTER_SYSTEMONE_URL
    headers = call_args.kwargs["headers"]
    assert headers["Authorization"] == "Bearer or-live-key"
    body = call_args.kwargs["json_body"]
    assert body["model"] == "typesafe/jev-1.13"
    assert "chat/completions" not in call_args.args[0]


def test_classify_typesafe_posts_to_typesafe_systemone(
    monkeypatch: pytest.MonkeyPatch,
):
    monkeypatch.delenv("OPENROUTER_API_KEY", raising=False)
    monkeypatch.setenv("TYPESAFE_API_KEY", "ts-live-key")
    questions = load_questions(RUBRIC)
    packet = load_evidence_packet(FIXTURES / "sample-evidence.json")
    recorded = json.loads((FIXTURES / "systemone-success.json").read_text(encoding="utf-8"))
    response = MagicMock()
    response.status_code = 200
    response.json.return_value = recorded
    transport = MagicMock()
    transport.post.return_value = response

    route = resolve_route()
    assert route is not None
    result = classify_or_degrade(
        packet,
        questions,
        route=route,
        transport=transport,
        timeout_ms=BUDGET_MS,
    )
    assert result["status"] == "ok"
    call_args = transport.post.call_args
    assert call_args.args[0] == SYSTEMONE_URL
    headers = call_args.kwargs["headers"]
    assert headers["Authorization"] == "Bearer ts-live-key"
    body = call_args.kwargs["json_body"]
    assert body["model"] == "jev-1.13.0"
