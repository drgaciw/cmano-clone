#!/usr/bin/env python3
"""Opaque-box CLI runner for Project Aegis backlog gap E2E verification suite."""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

SUITE_DIR = Path(__file__).resolve().parent
PYTEST_INI = SUITE_DIR / "pytest.ini"


def build_pytest_command(
    *,
    tier: str,
    live: bool,
    junit_xml: Path | None = None,
    extra_args: list[str] | None = None,
) -> list[str]:
    cmd = [
        sys.executable,
        "-m",
        "pytest",
        str(SUITE_DIR),
        "-c",
        str(PYTEST_INI),
    ]
    effective_tier = "all" if live else tier
    if effective_tier == "offline":
        cmd.extend(["-m", "not live_backlog"])
    elif effective_tier == "1":
        cmd.extend(["-m", "tier1"])
    elif effective_tier == "2":
        cmd.extend(["-m", "tier2"])
    elif effective_tier == "3":
        cmd.extend(["-m", "tier3"])
    elif effective_tier == "4":
        cmd.extend(["-m", "live_backlog"])
    elif effective_tier == "all":
        pass
    else:
        raise ValueError(f"Unsupported tier: {effective_tier}")

    if junit_xml is not None:
        cmd.append(f"--junitxml={junit_xml}")
    if extra_args:
        cmd.extend(extra_args)
    return cmd


def parse_junit_summary(xml_path: Path) -> dict[str, int]:
    tree = ET.parse(xml_path)
    root = tree.getroot()
    suites = [root] if root.tag == "testsuite" else list(root.findall("testsuite"))
    tests = sum(int(s.attrib.get("tests", 0)) for s in suites)
    failures = sum(int(s.attrib.get("failures", 0)) for s in suites)
    errors = sum(int(s.attrib.get("errors", 0)) for s in suites)
    skipped = sum(int(s.attrib.get("skipped", 0)) for s in suites)
    passed = max(0, tests - failures - errors - skipped)
    return {
        "tests": tests,
        "passed": passed,
        "failed": failures,
        "errors": errors,
        "skipped": skipped,
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Run Project Aegis 4-tier E2E verification suite."
    )
    parser.add_argument(
        "--tier",
        choices=("offline", "1", "2", "3", "4", "all"),
        default="offline",
        help="Tier selection (default: offline -> Tiers 1-3).",
    )
    parser.add_argument(
        "--live",
        action="store_true",
        help="Include Tier 4 live_backlog acceptance tests (equivalent to --tier all).",
    )
    parser.add_argument(
        "--require-impl",
        action="store_true",
        help="Set AEGIS_E2E_REQUIRE_IMPL=1 to require real M1-M3 deliverables.",
    )
    parser.add_argument(
        "--json-report",
        type=Path,
        default=None,
        help="Optional output path for structured JSON run summary.",
    )
    parser.add_argument(
        "--pytest-args",
        nargs=argparse.REMAINDER,
        default=[],
        help="Additional arguments passed straight to pytest.",
    )
    args = parser.parse_args(argv)

    env = dict(os.environ)
    if args.require_impl:
        env["AEGIS_E2E_REQUIRE_IMPL"] = "1"

    junit_xml: Path | None = None
    if args.json_report is not None:
        args.json_report.parent.mkdir(parents=True, exist_ok=True)
        junit_xml = args.json_report.with_suffix(".junit.xml")

    cmd = build_pytest_command(
        tier=args.tier,
        live=args.live,
        junit_xml=junit_xml,
        extra_args=list(args.pytest_args),
    )
    proc = subprocess.run(cmd, env=env, check=False)

    if args.json_report is not None and junit_xml is not None and junit_xml.is_file():
        counts = parse_junit_summary(junit_xml)
        junit_xml.unlink(missing_ok=True)
        report = {
            "tiers": "all" if args.live else args.tier,
            "impl_mode": "require_impl" if args.require_impl else "dual_phase",
            "exit_code": proc.returncode,
            **counts,
        }
        args.json_report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    return proc.returncode


if __name__ == "__main__":
    raise SystemExit(main())
