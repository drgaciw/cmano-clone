#!/usr/bin/env python3
"""Create and check an auditable warning/error SARIF disposition ledger."""

from __future__ import annotations

import argparse
from collections import Counter, defaultdict
from dataclasses import asdict, dataclass
import json
import os
from pathlib import Path, PurePosixPath
import re
import sys
from typing import Any, Iterable
from urllib.parse import unquote, urlparse


LEDGER_VERSION = 1
ALLOWED_STATUSES = {"open", "fixed", "accepted-exception", "deferred"}
GATED_LEVELS = {"warning", "error"}


class ReportError(ValueError):
    """Raised when SARIF or ledger input cannot be trusted."""


@dataclass(frozen=True)
class Comparison:
    """Result of comparing one SARIF report to a disposition ledger."""

    passed: bool
    counts: dict[str, int]
    new: list[dict[str, Any]]
    reintroduced: list[dict[str, Any]]
    ambiguous: list[dict[str, Any]]


def _require_dict(value: Any, description: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise ReportError(f"{description} must be a JSON object")
    return value


def _load_json(path: os.PathLike[str] | str, description: str) -> dict[str, Any]:
    source = Path(path)
    if not source.is_file():
        raise ReportError(f"{description} does not exist: {source}")
    try:
        if source.stat().st_size == 0:
            raise ReportError(f"{description} is empty: {source}")
        with source.open("r", encoding="utf-8-sig") as stream:
            return _require_dict(json.load(stream), description)
    except (OSError, json.JSONDecodeError) as error:
        raise ReportError(f"cannot read {description} {source}: {error}") from error


def load_report(path: os.PathLike[str] | str) -> dict[str, Any]:
    """Load SARIF and reject missing, malformed, or explicitly failed analysis."""
    report = _load_json(path, "SARIF report")
    _validate_sarif(report)
    return report


def _validate_sarif(report: dict[str, Any]) -> None:
    if report.get("version") not in {"2.1.0", "2.0.0"}:
        raise ReportError("SARIF report has no supported version")
    runs = report.get("runs")
    if not isinstance(runs, list) or not runs:
        raise ReportError("SARIF report must contain at least one run")
    for run_index, raw_run in enumerate(runs):
        run = _require_dict(raw_run, f"SARIF run {run_index}")
        if not isinstance(run.get("tool"), dict):
            raise ReportError(f"SARIF run {run_index} has no tool metadata")
        results = run.get("results")
        if not isinstance(results, list):
            raise ReportError(f"SARIF run {run_index} has no results array")
        invocations = run.get("invocations", [])
        if not isinstance(invocations, list):
            raise ReportError(f"SARIF run {run_index} invocations must be an array")
        for invocation in invocations:
            invocation = _require_dict(invocation, f"SARIF run {run_index} invocation")
            if invocation.get("executionSuccessful") is False:
                raise ReportError(f"analyzer invocation failed in SARIF run {run_index}")
            exit_code = invocation.get("exitCode")
            if exit_code is not None and (not isinstance(exit_code, int) or exit_code != 0):
                raise ReportError(f"analyzer invocation returned exit code {exit_code!r} in SARIF run {run_index}")


def _normalize_path(uri: str, repo_root: Path | None) -> str:
    decoded = unquote(uri)
    parsed = urlparse(decoded)
    if parsed.scheme.lower() == "file":
        decoded = parsed.path
        if parsed.netloc:
            decoded = f"//{parsed.netloc}{decoded}"
        if re.match(r"^/[A-Za-z]:/", decoded):
            decoded = decoded[1:]
    decoded = decoded.replace("\\", "/")
    if repo_root is not None:
        root = str(repo_root.resolve()).replace("\\", "/").rstrip("/")
        if decoded.lower().startswith(root.lower() + "/"):
            decoded = decoded[len(root) + 1 :]
        else:
            root_text = str(repo_root).replace("\\", "/").rstrip("/")
            if decoded.lower().startswith(root_text.lower() + "/"):
                decoded = decoded[len(root_text) + 1 :]
    while decoded.startswith("./"):
        decoded = decoded[2:]
    normalized = str(PurePosixPath(decoded))
    return normalized if normalized != "." else ""


def _rule_defaults(run: dict[str, Any]) -> tuple[dict[str, str], list[dict[str, Any]]]:
    driver = run.get("tool", {}).get("driver", {})
    rules = driver.get("rules", []) if isinstance(driver, dict) else []
    if not isinstance(rules, list):
        raise ReportError("SARIF driver rules must be an array")
    defaults: dict[str, str] = {}
    valid_rules: list[dict[str, Any]] = []
    for rule in rules:
        rule = _require_dict(rule, "SARIF rule")
        valid_rules.append(rule)
        rule_id = rule.get("id")
        configuration = rule.get("defaultConfiguration", {})
        if isinstance(rule_id, str) and isinstance(configuration, dict) and isinstance(configuration.get("level"), str):
            defaults[rule_id] = configuration["level"].lower()
    return defaults, valid_rules


def _framework_tags(run: dict[str, Any], finding: dict[str, Any]) -> list[str]:
    tags: set[str] = set()
    for properties in (run.get("properties", {}), finding.get("properties", {})):
        if not isinstance(properties, dict):
            continue
        raw_tags = properties.get('tags', [])
        if isinstance(raw_tags, list):
            tags.update(tag for tag in raw_tags if isinstance(tag, str) and re.match(r'^\.?NET', tag, re.IGNORECASE))
        for key in ("targetFramework", "targetFrameworks", "framework", "frameworks"):
            value = properties.get(key)
            if isinstance(value, str) and value:
                tags.add(value)
            elif isinstance(value, list):
                tags.update(item for item in value if isinstance(item, str) and item)
    return sorted(tags)


def _location(result: dict[str, Any], repo_root: Path | None) -> tuple[str, dict[str, int]]:
    locations = result.get("locations")
    if not isinstance(locations, list) or not locations:
        raise ReportError("warning/error result has no physical location")
    location = _require_dict(locations[0], "SARIF result location")
    physical = _require_dict(location.get("physicalLocation"), "SARIF physical location")
    artifact = _require_dict(physical.get("artifactLocation"), "SARIF artifact location")
    uri = artifact.get("uri")
    if not isinstance(uri, str) or not uri:
        raise ReportError("SARIF artifact location has no URI")
    raw_region = physical.get("region", {})
    region = _require_dict(raw_region, "SARIF region")
    clean_region = {
        key: value for key in ("startLine", "startColumn", "endLine", "endColumn")
        if isinstance((value := region.get(key)), int)
    }
    return _normalize_path(uri, repo_root), clean_region


def parse_sarif(report: dict[str, Any], repo_root: Path | None = None) -> list[dict[str, Any]]:
    """Return warning/error findings from every validated SARIF run."""
    _validate_sarif(report)
    findings: list[dict[str, Any]] = []
    for run_index, run in enumerate(report["runs"]):
        defaults, rules = _rule_defaults(run)
        for result_index, raw_result in enumerate(run["results"]):
            result = _require_dict(raw_result, f"SARIF result {run_index}:{result_index}")
            rule_id = result.get("ruleId")
            if not isinstance(rule_id, str) or not rule_id:
                rule_index = result.get("ruleIndex")
                if isinstance(rule_index, int) and 0 <= rule_index < len(rules):
                    rule_id = rules[rule_index].get("id")
            if not isinstance(rule_id, str) or not rule_id:
                raise ReportError(f"SARIF result {run_index}:{result_index} has no rule ID")
            level = result.get("level", defaults.get(rule_id, "warning"))
            if not isinstance(level, str):
                raise ReportError(f"SARIF result {run_index}:{result_index} has invalid severity")
            if level.lower() not in GATED_LEVELS:
                continue
            message_value = result.get("message", {})
            message_obj = _require_dict(message_value, "SARIF result message")
            message = message_obj.get("text", message_obj.get("markdown", ""))
            if not isinstance(message, str):
                raise ReportError("SARIF result message must be text")
            path, region = _location(result, repo_root)
            partial = result.get("partialFingerprints", {})
            partial = _require_dict(partial, "SARIF partialFingerprints")
            fingerprint = partial.get("contextRegionHash/v1")
            if fingerprint is not None and not isinstance(fingerprint, str):
                raise ReportError("contextRegionHash/v1 must be text")
            stable_part = {"contextRegionHash/v1": fingerprint} if fingerprint else {"location": region, "message": message}
            identity = json.dumps([rule_id, path, stable_part], sort_keys=True, separators=(",", ":"))
            findings.append({
                "identity": identity,
                "ruleId": rule_id,
                "path": path,
                "partialFingerprints": {"contextRegionHash/v1": fingerprint} if fingerprint else {},
                "location": region,
                "message": message,
                "frameworkTags": _framework_tags(run, result),
                "level": level.lower(),
            })
    return findings


def initialize_ledger(report_path: os.PathLike[str] | str, ledger_path: os.PathLike[str] | str, repo_root: Path | None = None) -> dict[str, Any]:
    """Explicitly create a ledger; refuse to replace an existing baseline."""
    destination = Path(ledger_path)
    if destination.exists():
        raise ReportError(f"ledger already exists; refusing automatic baseline replacement: {destination}")
    findings = parse_sarif(load_report(report_path), repo_root or Path.cwd())
    occurrences: dict[str, int] = defaultdict(int)
    entries = []
    for finding in findings:
        occurrences[finding["identity"]] += 1
        entry = dict(finding)
        entry["occurrence"] = occurrences[finding["identity"]]
        entry.update(owner="unassigned", status="open", rationale="", verification="", reviewCondition="", resolutionRevision="")
        entries.append(entry)
    ledger = {"schemaVersion": LEDGER_VERSION, "entries": entries}
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(ledger, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return ledger


def _load_ledger(path: os.PathLike[str] | str) -> dict[str, Any]:
    ledger = _load_json(path, "ledger")
    if ledger.get("schemaVersion") != LEDGER_VERSION or not isinstance(ledger.get("entries"), list):
        raise ReportError("unsupported or malformed ledger")
    for index, raw_entry in enumerate(ledger["entries"]):
        entry = _require_dict(raw_entry, f"ledger entry {index}")
        for field in ('identity', 'ruleId', 'path', 'message', 'owner', 'status', 'rationale', 'verification', 'reviewCondition', 'resolutionRevision'):
            if not isinstance(entry.get(field), str):
                raise ReportError(f'ledger entry {index} needs text field {field}')
        if (not isinstance(entry.get('frameworkTags'), list)
                or any(not isinstance(tag, str) for tag in entry['frameworkTags'])
                or type(entry.get('occurrence')) is not int or entry['occurrence'] < 1
                or not isinstance(entry.get('location'), dict)
                or not isinstance(entry.get('partialFingerprints'), dict)):
            raise ReportError(f'ledger entry {index} has invalid finding metadata')
        if not isinstance(entry.get("identity"), str) or entry.get("status") not in ALLOWED_STATUSES:
            raise ReportError(f"ledger entry {index} has invalid identity or status")
        if entry['status'] in {'fixed', 'deferred'}:
            if entry['owner'] in {'', 'unassigned'} or not entry['rationale']:
                raise ReportError(f"{entry['status']} entry {index} needs owner and rationale")
            if entry['status'] == 'fixed' and (not entry['verification'] or not entry['resolutionRevision']):
                raise ReportError(f'fixed entry {index} needs verification and resolutionRevision')
            if entry['status'] == 'deferred' and not entry['reviewCondition']:
                raise ReportError(f'deferred entry {index} needs reviewCondition')
        if entry["status"] == "accepted-exception":
            if (entry.get("owner") in {None, "", "unassigned"} or not entry.get("rationale")
                    or not entry.get("verification") or not entry.get("reviewCondition")):
                raise ReportError(
                    f"accepted-exception ledger entry {index} needs owner, rationale, verification, and reviewCondition"
                )
    return ledger


def _collision_key(finding: dict[str, Any]) -> tuple[str, str, tuple[str, ...]]:
    """Disambiguate matching context hashes without relying on moving line numbers."""
    return finding['identity'], finding['message'], tuple(sorted(finding['frameworkTags']))


def check_report(report_path: os.PathLike[str] | str, ledger_path: os.PathLike[str] | str, repo_root: Path | None = None) -> Comparison:
    """Compare stable finding identities, preserving duplicate multiplicity."""
    ledger = _load_ledger(ledger_path)
    current = parse_sarif(load_report(report_path), repo_root or Path.cwd())
    buckets: dict[tuple[str, str, tuple[str, ...]], list[dict[str, Any]]] = defaultdict(list)
    for entry in ledger["entries"]:
        buckets[_collision_key(entry)].append(entry)
    current_counts = Counter(_collision_key(finding) for finding in current)
    ambiguous = []
    for key, entries in buckets.items():
        dispositions = {(entry['status'], entry['owner'], entry['rationale'], entry['reviewCondition']) for entry in entries}
        if 0 < current_counts[key] < len(entries) and len(dispositions) > 1:
            ambiguous.append({'identity': key[0], 'message': key[1], 'frameworkTags': list(key[2]),
                              'baselineCount': len(entries), 'currentCount': current_counts[key],
                              'reason': 'Indistinguishable occurrences have different dispositions; manual review required.'})

    matched_ids: set[int] = set()
    new: list[dict[str, Any]] = []
    reintroduced: list[dict[str, Any]] = []
    present_counts = {status: 0 for status in ALLOWED_STATUSES}
    for finding in current:
        candidates = buckets.get(_collision_key(finding), [])
        match = next((entry for entry in candidates if id(entry) not in matched_ids), None)
        if match is None:
            new.append(finding)
            continue
        matched_ids.add(id(match))
        present_counts[match["status"]] += 1
        if match["status"] == "fixed":
            reintroduced.append(finding)

    fixed = sum(1 for entry in ledger["entries"] if id(entry) not in matched_ids)
    counts = {
        "original": len(ledger["entries"]),
        "fixed": fixed,
        "accepted-exception": present_counts["accepted-exception"],
        "deferred": present_counts["deferred"],
        "open": present_counts["open"],
        "new": len(new),
        "reintroduced": len(reintroduced),
        "ambiguous": len(ambiguous),
    }
    return Comparison(not new and not reintroduced and not ambiguous, counts, new, reintroduced, ambiguous)


def _write_comparison(path: os.PathLike[str] | str, comparison: Comparison) -> None:
    destination = Path(path)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(asdict(comparison), indent=2, sort_keys=True) + "\n", encoding="utf-8")


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    initialize = commands.add_parser("init", help="explicitly create a new open disposition ledger")
    initialize.add_argument("--report", required=True, type=Path)
    initialize.add_argument("--ledger", required=True, type=Path)
    check = commands.add_parser("check", help="compare a SARIF report to an existing ledger")
    check.add_argument("--report", required=True, type=Path)
    check.add_argument("--ledger", required=True, type=Path)
    check.add_argument("--output", type=Path)
    return parser


def main(argv: Iterable[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    try:
        if args.command == "init":
            ledger = initialize_ledger(args.report, args.ledger)
            print(f"initialized {args.ledger} with {len(ledger['entries'])} open findings")
            return 0
        comparison = check_report(args.report, args.ledger)
        if args.output:
            _write_comparison(args.output, comparison)
        print(json.dumps(comparison.counts, sort_keys=True))
        return 0 if comparison.passed else 1
    except ReportError as error:
        print(f"SARIF gate input error: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
