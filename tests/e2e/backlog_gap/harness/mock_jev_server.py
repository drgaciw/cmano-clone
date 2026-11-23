"""Ephemeral ThreadingHTTPServer simulating OpenRouter Jev-1.14 and System One Jev-1.13."""

from __future__ import annotations

import http.server
import itertools
import json
import threading
import time
from dataclasses import dataclass, field
from typing import Any, Callable

from .contracts import MAX_BODY_BYTES, validate_jev_request


@dataclass
class RecordedRequest:
    path: str
    headers: dict[str, str]
    raw_bytes: bytes
    body: dict[str, Any] | None


class MockJevServer:
    """Thread-safe local HTTP mock for Jev / System One with fault injection."""

    def __init__(self) -> None:
        self.requests: list[RecordedRequest] = []
        self._lock = threading.Lock()
        self._counter = itertools.count(1)
        self.mode: str = "ok"
        self.remaining_429s: int = 0
        self.slow_drip_delay_s: float = 0.05
        self.slow_drip_chunks: int = 30
        self.custom_answers_fn: Callable[[dict[str, Any]], dict[str, Any]] | None = None
        self._server: http.server.ThreadingHTTPServer | None = None
        self._thread: threading.Thread | None = None

    @property
    def port(self) -> int:
        assert self._server is not None
        return int(self._server.server_address[1])

    @property
    def base_url(self) -> str:
        return f"http://127.0.0.1:{self.port}"

    @property
    def decisions_url(self) -> str:
        return f"{self.base_url}/api/alpha/decisions"

    @property
    def systemone_url(self) -> str:
        return f"{self.base_url}/api/v1/systemone"

    def start(self) -> "MockJevServer":
        parent = self

        class _Handler(http.server.BaseHTTPRequestHandler):
            def do_POST(self) -> None:  # noqa: N802
                length = int(self.headers.get("Content-Length", "0"))
                raw = self.rfile.read(length)
                try:
                    parsed = json.loads(raw.decode("utf-8"))
                except Exception:
                    parsed = None

                with parent._lock:
                    parent.requests.append(
                        RecordedRequest(
                            path=self.path,
                            headers={k.lower(): v for k, v in self.headers.items()},
                            raw_bytes=raw,
                            body=parsed if isinstance(parsed, dict) else None,
                        )
                    )
                    mode = parent.mode
                    rem_429 = parent.remaining_429s
                    if rem_429 > 0:
                        parent.remaining_429s -= 1

                if rem_429 > 0:
                    self._send_json(429, {"error": {"code": "rate_limited"}})
                    return

                if mode == "http_502":
                    self._send_json(502, {"error": {"code": "bad_gateway"}})
                    return

                if mode == "malformed_json":
                    body = b"{not-valid-json"
                    self.send_response(200)
                    self.send_header("Content-Type", "application/json")
                    self.send_header("Content-Length", str(len(body)))
                    self.end_headers()
                    self.wfile.write(body)
                    return

                if mode == "missing_answers":
                    self._send_json(200, {"id": "gen-dec-missing", "model": "typesafe/jev-1.14"})
                    return

                if mode == "slow_drip":
                    self.send_response(200)
                    self.send_header("Content-Type", "application/json")
                    self.send_header("Content-Length", str(parent.slow_drip_chunks))
                    self.end_headers()
                    for _ in range(parent.slow_drip_chunks):
                        try:
                            self.wfile.write(b" ")
                            self.wfile.flush()
                        except OSError:
                            return
                        time.sleep(parent.slow_drip_delay_s)
                    return

                if len(raw) > MAX_BODY_BYTES:
                    self._send_json(
                        413,
                        {"error": {"code": "payload_too_large", "max_bytes": MAX_BODY_BYTES}},
                    )
                    return

                if not isinstance(parsed, dict):
                    self._send_json(400, {"error": {"code": "invalid_json"}})
                    return

                res = validate_jev_request(parsed)
                if not res.ok:
                    self._send_json(
                        400,
                        {"error": {"code": "invalid_request", "message": "; ".join(res.errors)}},
                    )
                    return

                seq = next(parent._counter)
                req_model = str(parsed.get("model") or (
                    "typesafe/jev-1.14" if "decisions" in self.path else "typesafe/jev-1.13"
                ))
                if parent.custom_answers_fn is not None:
                    answers = parent.custom_answers_fn(parsed)
                else:
                    answers = parent._default_answers(parsed, mode=mode)

                self._send_json(
                    200,
                    {
                        "id": f"gen-dec-mock-{seq:04d}",
                        "model": f"{req_model}-mock",
                        "provider": "TypeSafe",
                        "answers": answers,
                        "usage": {"input_tokens": 180, "output_tokens": 42, "cost": 0.0005},
                    },
                )

            def _send_json(self, status: int, obj: Any) -> None:
                data = json.dumps(obj).encode("utf-8")
                self.send_response(status)
                self.send_header("Content-Type", "application/json")
                self.send_header("Content-Length", str(len(data)))
                self.end_headers()
                self.wfile.write(data)

            def log_message(self, *args: Any) -> None:  # silence stderr in pytest
                pass

        self._server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), _Handler)
        self._server.daemon_threads = True
        self._thread = threading.Thread(target=self._server.serve_forever, daemon=True)
        self._thread.start()
        return self

    def stop(self) -> None:
        if self._server is not None:
            self._server.shutdown()
            self._server.server_close()
            self._server = None

    def reset(self) -> None:
        with self._lock:
            self.requests.clear()
            self.mode = "ok"
            self.remaining_429s = 0
            self.custom_answers_fn = None

    @staticmethod
    def _default_answers(parsed: dict[str, Any], *, mode: str) -> dict[str, Any]:
        questions = parsed.get("questions", {})
        answers: dict[str, Any] = {}
        for q_id, q_spec in questions.items():
            q_type = q_spec.get("type")
            if q_type == "noul":
                prob = 0.65 if mode == "low_noul" else 0.91
                answers[q_id] = {"noul": prob}
            elif q_type == "choice":
                criteria_keys = list((q_spec.get("criteria") or {}).keys())
                chosen = "complete" if "complete" in criteria_keys else criteria_keys[0]
                conf = 0.42 if mode == "low_choice_conf" else 0.88
                answers[q_id] = {
                    "choice": chosen,
                    "confidence": conf,
                    "probabilities": {k: (conf if k == chosen else 0.05) for k in criteria_keys},
                }
            elif q_type == "score":
                levels = q_spec.get("criteria") or ["low", "high"]
                answers[q_id] = {
                    "score": float(len(levels) - 1),
                    "legend": levels,
                    "probabilities": [0.1 for _ in levels[:-1]] + [0.8],
                }
        return answers
