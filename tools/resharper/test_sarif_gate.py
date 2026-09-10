import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sarif_gate


def result(rule="R1", path="src\\A.cs", line=10, fingerprint="fp", level="warning", message="problem"):
    value = {
        "ruleId": rule,
        "level": level,
        "message": {"text": message},
        "locations": [{"physicalLocation": {"artifactLocation": {"uri": path}, "region": {"startLine": line, "startColumn": 2}}}],
    }
    if fingerprint is not None:
        value["partialFingerprints"] = {"contextRegionHash/v1": fingerprint}
    return value


def sarif(*runs):
    return {"version": "2.1.0", "runs": list(runs)}


def run(*results, rules=None, invocation=None, properties=None):
    value = {"tool": {"driver": {"name": "InspectCode", "rules": rules or []}}, "results": list(results)}
    if invocation is not None:
        value["invocations"] = [invocation]
    if properties is not None:
        value["properties"] = properties
    return value


class SarifGateTests(unittest.TestCase):
    def write_json(self, directory, name, value):
        path = Path(directory, name)
        path.write_text(json.dumps(value), encoding="utf-8")
        return path

    def test_moved_line_matches_same_fingerprint(self):
        with tempfile.TemporaryDirectory() as directory:
            baseline = self.write_json(directory, "baseline.sarif", sarif(run(result(line=10))))
            current = self.write_json(directory, "current.sarif", sarif(run(result(line=400))))
            ledger_path = Path(directory, "ledger.json")
            sarif_gate.initialize_ledger(baseline, ledger_path)
            comparison = sarif_gate.check_report(current, ledger_path)
            self.assertTrue(comparison.passed)
            self.assertEqual(1, comparison.counts["open"])
            self.assertEqual(0, comparison.counts["new"])

    def test_equal_count_replacement_is_new_and_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            baseline = self.write_json(directory, "baseline.sarif", sarif(run(result(fingerprint="old"))))
            current = self.write_json(directory, "current.sarif", sarif(run(result(fingerprint="new"))))
            ledger_path = Path(directory, "ledger.json")
            sarif_gate.initialize_ledger(baseline, ledger_path)
            comparison = sarif_gate.check_report(current, ledger_path)
            self.assertFalse(comparison.passed)
            self.assertEqual(1, comparison.counts["new"])

    def test_repeated_fingerprint_is_a_multiset(self):
        with tempfile.TemporaryDirectory() as directory:
            baseline = self.write_json(directory, "baseline.sarif", sarif(run(result(), result())))
            current = self.write_json(directory, "current.sarif", sarif(run(result(), result(), result())))
            ledger_path = Path(directory, "ledger.json")
            ledger = sarif_gate.initialize_ledger(baseline, ledger_path)
            self.assertEqual(2, len(ledger["entries"]))
            comparison = sarif_gate.check_report(current, ledger_path)
            self.assertFalse(comparison.passed)
            self.assertEqual(1, comparison.counts["new"])

    def test_paths_are_repo_relative_and_separator_normalized(self):
        findings = sarif_gate.parse_sarif(sarif(run(result(path="file:///C:/repo/src/Foo.cs"))), repo_root=Path("C:/repo"))
        self.assertEqual("src/Foo.cs", findings[0]["path"])

    def test_all_runs_and_framework_tags_are_exported(self):
        report = sarif(
            run(result(rule="A"), properties={"targetFramework": "net8.0"}),
            run(result(rule="B"), properties={"framework": "netstandard2.1"}),
        )
        findings = sarif_gate.parse_sarif(report)
        self.assertEqual(["A", "B"], [item["ruleId"] for item in findings])
        self.assertEqual(["net8.0"], findings[0]["frameworkTags"])
        self.assertEqual(["netstandard2.1"], findings[1]["frameworkTags"])

    def test_jetbrains_framework_tags_are_retained(self):
        warning = result()
        warning['properties'] = {'tags': ['C#', '.NETStandard 2.1']}
        self.assertEqual(['.NETStandard 2.1'], sarif_gate.parse_sarif(sarif(run(warning)))[0]['frameworkTags'])

    def test_collision_with_distinct_framework_matches_its_own_disposition(self):
        with tempfile.TemporaryDirectory() as directory:
            first, second = result(), result()
            first['properties'] = {'tags': ['.NET 8.0']}
            second['properties'] = {'tags': ['.NETStandard 2.1']}
            baseline = self.write_json(directory, 'baseline.sarif', sarif(run(first, second)))
            current = self.write_json(directory, 'current.sarif', sarif(run(second)))
            ledger_path = Path(directory, 'ledger.json')
            ledger = sarif_gate.initialize_ledger(baseline, ledger_path)
            ledger['entries'][1].update(status='fixed', owner='team', rationale='removed', verification='tests', resolutionRevision='abc')
            ledger_path.write_text(json.dumps(ledger))
            self.assertFalse(sarif_gate.check_report(current, ledger_path).passed)

    def test_indistinguishable_collision_with_mixed_status_fails_closed(self):
        with tempfile.TemporaryDirectory() as directory:
            baseline = self.write_json(directory, 'baseline.sarif', sarif(run(result(), result())))
            current = self.write_json(directory, 'current.sarif', sarif(run(result())))
            ledger_path = Path(directory, 'ledger.json')
            ledger = sarif_gate.initialize_ledger(baseline, ledger_path)
            ledger['entries'][1].update(status='fixed', owner='team', rationale='removed', verification='tests', resolutionRevision='abc')
            ledger_path.write_text(json.dumps(ledger))
            comparison = sarif_gate.check_report(current, ledger_path)
            self.assertFalse(comparison.passed)
            self.assertEqual(1, comparison.counts['ambiguous'])

    def test_all_ledger_statuses_require_audit_schema(self):
        with tempfile.TemporaryDirectory() as directory:
            baseline = self.write_json(directory, 'baseline.sarif', sarif(run(result())))
            ledger_path = Path(directory, 'ledger.json')
            ledger = sarif_gate.initialize_ledger(baseline, ledger_path)
            for field in ('owner', 'rationale', 'verification', 'resolutionRevision', 'message', 'frameworkTags', 'occurrence'):
                broken = json.loads(json.dumps(ledger))
                del broken['entries'][0][field]
                ledger_path.write_text(json.dumps(broken))
                with self.subTest(field=field), self.assertRaises(sarif_gate.ReportError):
                    sarif_gate.check_report(baseline, ledger_path)

    def test_rule_default_warning_is_included_and_note_is_excluded(self):
        rules = [
            {"id": "WARN", "defaultConfiguration": {"level": "warning"}},
            {"id": "NOTE", "defaultConfiguration": {"level": "note"}},
        ]
        warning = result(rule="WARN"); warning.pop("level")
        note = result(rule="NOTE"); note.pop("level")
        findings = sarif_gate.parse_sarif(sarif(run(warning, note, rules=rules)))
        self.assertEqual(["WARN"], [item["ruleId"] for item in findings])

    def test_missing_malformed_and_failed_invocation_are_errors(self):
        with tempfile.TemporaryDirectory() as directory:
            missing = Path(directory, "missing.sarif")
            malformed = Path(directory, "bad.sarif"); malformed.write_text("", encoding="utf-8")
            failed = self.write_json(directory, "failed.sarif", sarif(run(invocation={"executionSuccessful": False, "exitCode": 1})))
            for path in (missing, malformed, failed):
                with self.subTest(path=path), self.assertRaises(sarif_gate.ReportError):
                    sarif_gate.load_report(path)

    def test_legitimate_empty_results_are_allowed(self):
        findings = sarif_gate.parse_sarif(sarif(run()))
        self.assertEqual([], findings)

    def test_export_defaults_are_auditable_and_never_accepted(self):
        with tempfile.TemporaryDirectory() as directory:
            baseline = self.write_json(directory, "baseline.sarif", sarif(run(result(message="hello"))))
            ledger_path = Path(directory, "ledger.json")
            entry = sarif_gate.initialize_ledger(baseline, ledger_path)["entries"][0]
            self.assertEqual("open", entry["status"])
            self.assertEqual("unassigned", entry["owner"])
            self.assertEqual("hello", entry["message"])
            self.assertEqual("", entry["rationale"])
            self.assertEqual("", entry["verification"])
            self.assertEqual("", entry["resolutionRevision"])
            self.assertEqual("", entry["reviewCondition"])

    def test_accepted_and_deferred_are_reported_separately(self):
        with tempfile.TemporaryDirectory() as directory:
            baseline = self.write_json(directory, "baseline.sarif", sarif(run(result(rule="A", fingerprint="a"), result(rule="D", fingerprint="d"))))
            current = self.write_json(directory, "current.sarif", sarif(run(result(rule="A", fingerprint="a"), result(rule="D", fingerprint="d"))))
            ledger_path = Path(directory, "ledger.json")
            ledger = sarif_gate.initialize_ledger(baseline, ledger_path)
            ledger["entries"][0].update(status="accepted-exception", owner="team", rationale="contract", verification="reviewed", reviewCondition="review yearly")
            ledger["entries"][1].update(status="deferred", owner="team", rationale="scheduled", reviewCondition="when integration ships")
            ledger_path.write_text(json.dumps(ledger), encoding="utf-8")
            comparison = sarif_gate.check_report(current, ledger_path)
            self.assertTrue(comparison.passed)
            self.assertEqual(1, comparison.counts["accepted-exception"])
            self.assertEqual(1, comparison.counts["deferred"])

    def test_accepted_exception_requires_review_condition(self):
        with tempfile.TemporaryDirectory() as directory:
            baseline = self.write_json(directory, "baseline.sarif", sarif(run(result())))
            current = self.write_json(directory, "current.sarif", sarif(run(result())))
            ledger_path = Path(directory, "ledger.json")
            ledger = sarif_gate.initialize_ledger(baseline, ledger_path)
            ledger["entries"][0].update(status="accepted-exception", owner="team", rationale="contract", verification="reviewed")
            ledger_path.write_text(json.dumps(ledger), encoding="utf-8")
            with self.assertRaises(sarif_gate.ReportError):
                sarif_gate.check_report(current, ledger_path)

    def test_fixed_finding_reintroduced_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            baseline = self.write_json(directory, "baseline.sarif", sarif(run(result())))
            current = self.write_json(directory, "current.sarif", sarif(run(result())))
            ledger_path = Path(directory, "ledger.json")
            ledger = sarif_gate.initialize_ledger(baseline, ledger_path)
            ledger["entries"][0].update(status="fixed", owner="team", rationale="removed", verification="tests", resolutionRevision="abc")
            ledger_path.write_text(json.dumps(ledger), encoding="utf-8")
            comparison = sarif_gate.check_report(current, ledger_path)
            self.assertFalse(comparison.passed)
            self.assertEqual(1, comparison.counts["reintroduced"])

    def test_location_fallback_distinguishes_findings_without_fingerprint(self):
        findings = sarif_gate.parse_sarif(sarif(run(result(line=1, fingerprint=None), result(line=2, fingerprint=None))))
        self.assertNotEqual(findings[0]["identity"], findings[1]["identity"])

    def test_cli_init_refuses_to_overwrite_and_check_writes_output(self):
        with tempfile.TemporaryDirectory() as directory:
            report = self.write_json(directory, "report.sarif", sarif(run()))
            ledger = Path(directory, "ledger.json")
            output = Path(directory, "comparison.json")
            script = Path(sarif_gate.__file__)
            first = subprocess.run([sys.executable, str(script), "init", "--report", str(report), "--ledger", str(ledger)], capture_output=True, text=True)
            second = subprocess.run([sys.executable, str(script), "init", "--report", str(report), "--ledger", str(ledger)], capture_output=True, text=True)
            checked = subprocess.run([sys.executable, str(script), "check", "--report", str(report), "--ledger", str(ledger), "--output", str(output)], capture_output=True, text=True)
            self.assertEqual(0, first.returncode)
            self.assertEqual(2, second.returncode)
            self.assertEqual(0, checked.returncode)
            self.assertTrue(output.exists())


if __name__ == "__main__":
    unittest.main()
