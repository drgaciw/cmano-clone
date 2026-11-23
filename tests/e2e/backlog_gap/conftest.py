"""Pytest fixtures and configuration for Project Aegis backlog gap E2E suite."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from typing import Any, Iterator

import pytest

SUITE_ROOT = Path(__file__).resolve().parent
REPO_ROOT = SUITE_ROOT.parents[2]
FIXTURES_DIR = SUITE_ROOT / "fixtures"

if str(SUITE_ROOT) not in sys.path:
    sys.path.insert(0, str(SUITE_ROOT))

from harness.cli_adapter import (  # noqa: E402
    assert_impl_deliverables_present,
    is_require_impl,
)
from harness.mock_jev_server import MockJevServer  # noqa: E402


@pytest.fixture(scope="session")
def repo_root() -> Path:
    return REPO_ROOT


@pytest.fixture(scope="session")
def fixtures_dir() -> Path:
    return FIXTURES_DIR


@pytest.fixture(scope="session")
def require_impl_mode() -> bool:
    enabled = is_require_impl()
    if enabled:
        assert_impl_deliverables_present(REPO_ROOT)
    return enabled


@pytest.fixture()
def mock_jev_server() -> Iterator[MockJevServer]:
    srv = MockJevServer().start()
    try:
        yield srv
    finally:
        srv.stop()


@pytest.fixture()
def load_fixture_json():
    def _loader(rel_path: str) -> Any:
        return json.loads((FIXTURES_DIR / rel_path).read_text(encoding="utf-8"))

    return _loader
