"""Exercise the real guard in disposable repositories without npm or MCP processes."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


GUARD = Path(__file__).with_name("gitnexus-safe-reindex.sh")
FAKE_CLI = r'''
import json, os, pathlib, subprocess, sys
tool = pathlib.Path(sys.argv[0]).name
args = sys.argv[1:]
with open(os.environ["CALL_LOG"], "a") as log:
    log.write(json.dumps([tool, *args]) + "\n")
if os.environ.get("FAIL_RUNNER") == tool:
    sys.exit(7)
if tool == "node":
    assert args.pop(0).endswith("/.gitnexus/run.cjs")
elif tool == "pnpm":
    args = args[args.index("dlx") + 2:]
elif tool == "npx":
    args = args[2:]  # --yes gitnexus
if args[0] == "analyze":
    assert "--force" in args and "--skip-agents-md" in args and "--skip-skills" in args
    sys.exit(0)
assert args[0] == "cypher"
if "--repo" not in args or args[args.index("--repo") + 1] != os.getcwd():
    print("Multiple repositories indexed; specify --repo", file=sys.stderr)
    sys.exit(8)
if os.environ.get("FAIL_QUERY"):
    print("database unavailable", file=sys.stderr)
    sys.exit(9)
query = args[1]
count = 2 if query == "MATCH (n) RETURN count(n) AS c" else 1 if query == "MATCH ()-[r]->() RETURN count(r) AS c" else 0
if os.environ.get("CORRUPT_INDEX") and "n:Method" in query:
    count = 1
print(json.dumps({"markdown": "| c |\n| --- |\n| " + str(count) + " |", "row_count": 1}))
'''


class SafeReindexTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="safe reindex ")
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.repo = root / "repo with spaces"
        self.repo.mkdir()
        self.bin = root / "bin"
        self.bin.mkdir()
        self.log = root / "calls.jsonl"
        for command in ("git", "dirname", "python3"):
            (self.bin / command).symlink_to(shutil.which(command))
        self.executable("pgrep", "#!/bin/sh\nexit 1\n")
        self.executable("pkill", "#!/bin/sh\nexit 99\n")
        self.env = {**os.environ, "PATH": str(self.bin), "CALL_LOG": str(self.log)}
        self.git("init", "-b", "main")
        (self.repo / "tools").mkdir()
        shutil.copy2(GUARD, self.repo / "tools" / GUARD.name)
        (self.repo / ".gitignore").write_text(".gitnexus/\n")
        self.git("add", ".")
        self.git("-c", "user.name=Guard Test", "-c", "user.email=test@example.invalid",
                 "-c", "commit.gpgsign=false", "commit", "-m", "fixture")
        self.index = self.repo / ".gitnexus"
        self.index.mkdir()
        (self.index / "meta.json").write_text(json.dumps({
            "lastCommit": self.git("rev-parse", "HEAD").stdout.strip(),
            "stats": {"nodes": 2, "edges": 1},
        }))

    def git(self, *args):
        return subprocess.run([str(self.bin / "git"), *args], cwd=self.repo,
                              capture_output=True, text=True, check=True)

    def executable(self, name, source):
        path = self.bin / name
        path.write_text(source)
        path.chmod(0o755)

    def cli(self, *tools):
        for tool in tools:
            self.executable(tool, f"#!{sys.executable}\n" + FAKE_CLI)

    def run_guard(self, *args, **env):
        result = subprocess.run(["/bin/bash", str(self.repo / "tools" / GUARD.name), *args],
                                env={**self.env, **env}, capture_output=True, text=True,
                                timeout=15)
        calls = [json.loads(line) for line in self.log.read_text().splitlines()] if self.log.exists() else []
        return result, calls

    def test_repository_runner_takes_precedence_for_analyze_and_queries(self):
        (self.index / "run.cjs").write_text("// runner fixture\n")
        self.cli("node", "gitnexus", "pnpm", "npx")
        result, calls = self.run_guard("--name", "review alias")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual({"node"}, {call[0] for call in calls})
        self.assertIn("review alias", calls[0])
        self.assertIn("--allow-duplicate-name", calls[0])
        self.assertEqual(11, len(calls))  # analyze + ten integrity queries

    def test_fresh_checkout_fallbacks(self):
        for available, expected in ((["gitnexus", "pnpm", "npx"], "gitnexus"),
                                    (["pnpm", "npx"], "pnpm"), (["npx"], "npx")):
            with self.subTest(expected=expected):
                for tool in ("gitnexus", "pnpm", "npx"):
                    (self.bin / tool).unlink(missing_ok=True)
                self.log.unlink(missing_ok=True)
                self.cli(*available)
                result, calls = self.run_guard("--skip-analyze", "--no-kill-mcp")
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertEqual({expected}, {call[0] for call in calls})
                self.assertEqual(10, len(calls))

    def test_selected_runner_failure_is_not_retried_through_npm(self):
        (self.index / "run.cjs").write_text("// runner fixture\n")
        self.cli("node", "npx")
        result, calls = self.run_guard(FAIL_RUNNER="node")
        self.assertEqual(2, result.returncode, result.stderr)
        self.assertEqual(["node"], [call[0] for call in calls])

    def test_corrupt_index_fails_closed(self):
        self.cli("gitnexus")
        result, _ = self.run_guard("--skip-analyze", CORRUPT_INDEX="1")
        self.assertEqual(3, result.returncode, result.stderr)
        self.assertIn("BAD INDEX", result.stderr)

    def test_query_failure_reports_bad_index_exit_code(self):
        self.cli("gitnexus")
        result, _ = self.run_guard("--skip-analyze", FAIL_QUERY="1")
        self.assertEqual(3, result.returncode, result.stderr)

    def test_missing_metadata_reports_bad_index_without_querying(self):
        (self.index / "meta.json").unlink()
        result, calls = self.run_guard("--skip-analyze")
        self.assertEqual(3, result.returncode, result.stderr)
        self.assertEqual([], calls)

    def test_dirty_checkout_is_refused_and_preserved(self):
        dirty = self.repo / "uncommitted.txt"
        dirty.write_text("preserve this\n")
        result, calls = self.run_guard()
        self.assertEqual(1, result.returncode)
        self.assertEqual([], calls)
        self.assertEqual("preserve this\n", dirty.read_text())

    def test_other_branch_is_refused_before_dispatch(self):
        self.git("checkout", "-b", "feature")
        result, calls = self.run_guard()
        self.assertEqual(1, result.returncode)
        self.assertEqual([], calls)


if __name__ == "__main__":
    unittest.main()
