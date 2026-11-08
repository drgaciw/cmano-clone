#!/usr/bin/env python3
"""Project-local Hindsight-compatible memory server.

This implements the small HTTP surface used by this repository's Hindsight
wrappers. It stores memories as JSONL under .hindsight-local by default and
uses deterministic keyword scoring for recall.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import time
import uuid
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlparse


TOKEN_RE = re.compile(r"[a-z0-9_./:-]+")


def tokenize(value: str) -> set[str]:
    return set(TOKEN_RE.findall(value.lower()))


def bank_path(data_dir: Path, bank_id: str) -> Path:
    safe = re.sub(r"[^A-Za-z0-9_.-]+", "_", bank_id).strip("._")
    if not safe:
        safe = "default"
    return data_dir / "banks" / f"{safe}.jsonl"


def load_records(data_dir: Path, bank_id: str) -> list[dict]:
    path = bank_path(data_dir, bank_id)
    if not path.exists():
        return []

    records: list[dict] = []
    with path.open("r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line:
                continue
            try:
                records.append(json.loads(line))
            except json.JSONDecodeError:
                continue
    return records


def append_records(data_dir: Path, bank_id: str, records: list[dict]) -> None:
    path = bank_path(data_dir, bank_id)
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as handle:
        for record in records:
            handle.write(json.dumps(record, sort_keys=True, separators=(",", ":")))
            handle.write("\n")


def list_banks(data_dir: Path) -> list[dict]:
    banks_dir = data_dir / "banks"
    if not banks_dir.exists():
        return []
    banks = []
    for path in sorted(banks_dir.glob("*.jsonl")):
        bank_id = path.stem
        count = sum(1 for _ in path.open("r", encoding="utf-8"))
        banks.append({"id": bank_id, "count": count})
    return banks


class HindsightHandler(BaseHTTPRequestHandler):
    server_version = "ProjectAegisHindsight/1.0"

    @property
    def data_dir(self) -> Path:
        return self.server.data_dir  # type: ignore[attr-defined]

    def log_message(self, fmt: str, *args: object) -> None:
        if self.server.quiet:  # type: ignore[attr-defined]
            return
        super().log_message(fmt, *args)

    def read_json(self) -> dict:
        length = int(self.headers.get("content-length", "0"))
        if length <= 0:
            return {}
        raw = self.rfile.read(length)
        return json.loads(raw.decode("utf-8"))

    def send_json(self, status: HTTPStatus, body: object) -> None:
        payload = json.dumps(body, indent=2).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def send_text(self, status: HTTPStatus, body: str) -> None:
        payload = body.encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "text/plain; charset=utf-8")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def do_GET(self) -> None:  # noqa: N802
        parsed = urlparse(self.path)
        if parsed.path == "/health":
            self.send_json(HTTPStatus.OK, {"ok": True})
            return
        if parsed.path == "/v1/default/banks":
            self.send_json(HTTPStatus.OK, {"banks": list_banks(self.data_dir)})
            return
        self.send_json(HTTPStatus.NOT_FOUND, {"error": "not_found"})

    def do_POST(self) -> None:  # noqa: N802
        parsed = urlparse(self.path)
        parts = [unquote(part) for part in parsed.path.strip("/").split("/")]

        if len(parts) >= 5 and parts[:3] == ["v1", "default", "banks"]:
            bank_id = parts[3]
            tail = parts[4:]
            if tail == ["memories", "retain"]:
                self.handle_retain(bank_id)
                return
            if tail == ["memories", "recall"]:
                self.handle_recall(bank_id)
                return
            if tail == ["reflect"]:
                self.handle_reflect(bank_id)
                return

        self.send_json(HTTPStatus.NOT_FOUND, {"error": "not_found"})

    def handle_retain(self, bank_id: str) -> None:
        body = self.read_json()
        items = body.get("items") or []
        if not isinstance(items, list):
            self.send_json(HTTPStatus.BAD_REQUEST, {"error": "items must be a list"})
            return

        now = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
        records = []
        for item in items:
            if isinstance(item, str):
                content = item
                context = ""
            elif isinstance(item, dict):
                content = str(item.get("content", ""))
                context = str(item.get("context", ""))
            else:
                continue

            if not content.strip():
                continue

            records.append(
                {
                    "id": str(uuid.uuid4()),
                    "bankId": bank_id,
                    "content": content,
                    "context": context,
                    "createdAt": now,
                }
            )

        append_records(self.data_dir, bank_id, records)
        self.send_json(
            HTTPStatus.OK,
            {"bankId": bank_id, "retained": len(records), "async": bool(body.get("async", False))},
        )

    def ranked_records(self, bank_id: str, query: str, limit: int = 8) -> list[dict]:
        query_tokens = tokenize(query)
        records = load_records(self.data_dir, bank_id)
        ranked = []
        for record in records:
            content = str(record.get("content", ""))
            content_tokens = tokenize(content)
            overlap = query_tokens & content_tokens
            phrase_bonus = 1 if query.lower() in content.lower() else 0
            score = len(overlap) + phrase_bonus
            if score > 0 or not query_tokens:
                ranked.append((score, record))

        ranked.sort(key=lambda item: (item[0], item[1].get("createdAt", "")), reverse=True)
        return [{"score": score, "record": record} for score, record in ranked[:limit]]

    def handle_recall(self, bank_id: str) -> None:
        body = self.read_json()
        query = str(body.get("query", ""))
        results = []
        for item in self.ranked_records(bank_id, query):
            record = item["record"]
            results.append(
                {
                    "text": record.get("content", ""),
                    "score": item["score"],
                    "metadata": {
                        "id": record.get("id"),
                        "bankId": bank_id,
                        "context": record.get("context", ""),
                        "createdAt": record.get("createdAt"),
                    },
                }
            )
        self.send_json(HTTPStatus.OK, {"bankId": bank_id, "query": query, "results": results})

    def handle_reflect(self, bank_id: str) -> None:
        body = self.read_json()
        query = str(body.get("query", ""))
        ranked = self.ranked_records(bank_id, query, limit=5)
        if not ranked:
            text = f"No retained memories matched: {query}"
        else:
            lines = [f"Reflection for {bank_id}: {query}"]
            for item in ranked:
                content = str(item["record"].get("content", "")).strip()
                lines.append(f"- {content}")
            text = "\n".join(lines)
        self.send_json(HTTPStatus.OK, {"bankId": bank_id, "query": query, "text": text})


def main() -> int:
    parser = argparse.ArgumentParser(description="Run the project-local Hindsight-compatible server.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8888)
    parser.add_argument("--data-dir", default=".hindsight-local")
    parser.add_argument("--quiet", action="store_true")
    args = parser.parse_args()

    data_dir = Path(args.data_dir).resolve()
    data_dir.mkdir(parents=True, exist_ok=True)

    server = ThreadingHTTPServer((args.host, args.port), HindsightHandler)
    server.data_dir = data_dir  # type: ignore[attr-defined]
    server.quiet = args.quiet  # type: ignore[attr-defined]
    print(f"Hindsight local server listening at http://{args.host}:{args.port}")
    print(f"Data dir: {data_dir}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        return 130
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
