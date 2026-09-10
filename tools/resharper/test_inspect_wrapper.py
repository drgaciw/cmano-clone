"""Exercise wrapper failure propagation without invoking the expensive analyzer."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


class InspectWrapperTests(unittest.TestCase):
    def test_analyzer_failure_malformed_new_and_clean_reports(self):
        wrapper = Path(__file__).with_name('inspect.ps1').resolve()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            dotnet = root/'dotnet.ps1'
            dotnet.write_text("if ($args -contains '--version') { '8.0.400' }; exit 0\n")
            analyzer = root/'inspectcode.ps1'
            analyzer.write_text("""if ($args -contains '--version') { 'JetBrains Inspect Code 2026.2.1'; exit 0 }
$destination = ($args | Where-Object { $_ -like '--output=*' }) -replace '^--output=', ''
Copy-Item -LiteralPath $env:RESHARPER_TEST_REPORT -Destination $destination
exit ([int]$env:RESHARPER_TEST_EXIT)
""")
            ledger = root/'ledger.json'
            ledger.write_text(json.dumps({'schemaVersion': 1, 'entries': []}))
            report = root/'fixture.sarif'
            output = root/'output.sarif'
            valid = {'version':'2.1.0', 'runs':[{'tool':{'driver':{'name':'InspectCode'}}, 'results':[]}]}
            warning = {'ruleId':'NEW', 'level':'warning', 'message':{'text':'new warning'},
                       'locations':[{'physicalLocation':{'artifactLocation':{'uri':'src/Test.cs'},
                                    'region':{'startLine':1, 'startColumn':1}}}]}
            jb = root/'jb.ps1'
            jb.write_text("if ($args[0] -ne 'inspectcode') { throw 'Expected complete inspectcode subcommand' }\n" + analyzer.read_text())
            for scenario in ('analyzer-failure', 'malformed', 'new-warning', 'clean', 'jb-clean'):
                with self.subTest(scenario=scenario):
                    fixture = json.loads(json.dumps(valid))
                    if scenario == 'new-warning':
                        fixture['runs'][0]['results'].append(warning)
                    report.write_text('not json' if scenario == 'malformed' else json.dumps(fixture))
                    output.write_text(json.dumps(valid))  # stale clean report must never mask failure
                    env = dict(os.environ, RESHARPER_TEST_REPORT=str(report),
                               RESHARPER_TEST_EXIT='4' if scenario == 'analyzer-failure' else '0')
                    command = [shutil.which('pwsh') or 'pwsh', '-NoProfile', '-File', str(wrapper),
                               '-DotnetPath', str(dotnet), '-ToolPath', str(jb if scenario == 'jb-clean' else analyzer),
                               '-OutputPath', str(output), '-LedgerPath', str(ledger), '-Check']
                    completed = subprocess.run(command, env=env, capture_output=True, text=True, timeout=30)
                    if scenario in ('clean', 'jb-clean'):
                        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
                    else:
                        self.assertNotEqual(0, completed.returncode, completed.stdout + completed.stderr)
                    if scenario == 'analyzer-failure':
                        self.assertIn('InspectCode failed with exit code 4', completed.stderr)


if __name__ == '__main__':
    unittest.main()
