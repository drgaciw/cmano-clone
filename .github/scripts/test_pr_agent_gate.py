"""Unit tests for the pull-request agent gate matcher and glob overlap."""

from __future__ import annotations

import json
import unittest
import urllib.request
from pathlib import Path

import pr_agent_gate as gate

REPO_ROOT = Path(__file__).resolve().parents[2]

HARD_NO = (
    "src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs",
    "src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs",
    "src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs",
    "unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs",
    "unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs.meta",
    "unity/ProjectAegis/Assets/Scripts/Runtime/GlobeMapProductHost.cs",
    "unity/ProjectAegis/Assets/Scripts/Runtime/GlobeMapProductHost.cs.meta",
    "src/ProjectAegis.Delegation/Input/C2CommandIssuance.cs",
    "src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs",
    "src/ProjectAegis.Delegation/Projection/DatalinkPictureProjection.cs",
    "src/ProjectAegis.Delegation/Projection/TacticalOverlayProjection.cs",
    "tests/regression/replay-golden-*",
)

TEMPLATE = ".github/pull_request_template.md"


class DrgIdTests(unittest.TestCase):
    def test_matches_title_branch_or_body_case_insensitively(self) -> None:
        self.assertTrue(gate.has_drg_id("DRG-776: add gate", "feature", ""))
        self.assertTrue(gate.has_drg_id("add gate", "cursor/drg-776-pr-agent-gate", ""))
        self.assertTrue(gate.has_drg_id("add gate", "feature", "Closes drg-12\n"))
        self.assertTrue(gate.has_drg_id("Prefix DRG-1 suffix", "feature", ""))

    def test_cma_id_on_its_own_does_not_count(self) -> None:
        self.assertFalse(gate.has_drg_id("CMA-13 fix", "fix/cma-13-jsonl", "Closes CMA-13"))

    def test_rejects_missing_digits_and_embedded_word(self) -> None:
        self.assertFalse(gate.has_drg_id("DRG-", "feature", "DRG- next"))
        self.assertFalse(gate.has_drg_id("XDRG-12", "preDRG-3-branch", "notDRG-9"))

    def test_owner_is_not_skipped_and_still_needs_a_drg_id(self) -> None:
        self.assertFalse(gate.is_skipped_author("drgaciw"))
        result = gate.evaluate_drg_id("docs only", "docs/readme", "", "drgaciw")
        self.assertFalse(result.ok)
        self.assertIn("DRG-", result.message)

    def test_renovate_and_dependabot_skip_both_checks(self) -> None:
        for login in ("renovate[bot]", "dependabot[bot]", "Renovate", "Dependabot"):
            self.assertTrue(gate.is_skipped_author(login), login)
            drg = gate.evaluate_drg_id("chore(deps): bump", "renovate/hono", "", login)
            surface = gate.evaluate_surface(
                number=1,
                body="",
                author=login,
                files=["src/anywhere.cs"],
                list_open_pulls=lambda: (_ for _ in ()).throw(AssertionError("API used")),
                list_files=lambda _number: (_ for _ in ()).throw(AssertionError("API used")),
            )
            self.assertTrue(drg.ok and drg.skipped, login)
            self.assertTrue(surface.ok and surface.skipped, login)


class SurfaceParseTests(unittest.TestCase):
    def test_parses_template_line_after_closes(self) -> None:
        body = (
            "Closes DRG-776\n"
            "Surface: .github/workflows/pr-agent-gate.yml, "
            "`.github/scripts/pr_agent_gate.py`, "
            ".github/scripts/test_pr_agent_gate.py\n"
        )
        self.assertEqual(
            gate.parse_surface(body),
            [
                ".github/workflows/pr-agent-gate.yml",
                ".github/scripts/pr_agent_gate.py",
                ".github/scripts/test_pr_agent_gate.py",
            ],
        )

    def test_accepts_list_marker_and_whitespace_separated_paths(self) -> None:
        body = "- Surface: src/Foo.cs src/Bar.cs\n"
        self.assertEqual(gate.parse_surface(body), ["src/Foo.cs", "src/Bar.cs"])

    def test_ignores_a_mention_that_is_not_the_surface_line(self) -> None:
        body = "See Surface: not-a-declaration\nSurface: real/path.cs\n"
        self.assertEqual(gate.parse_surface(body), ["real/path.cs"])

    def test_missing_line_is_none_and_blank_line_is_empty(self) -> None:
        self.assertIsNone(gate.parse_surface("Closes DRG-1\n\nNo surface here.\n"))
        self.assertIsNone(gate.parse_surface(None))
        self.assertEqual(gate.parse_surface("Surface:\n"), [])

    def test_drg_pr_without_surface_fails_and_points_at_the_template(self) -> None:
        result = gate.evaluate_surface(
            number=10,
            body="Closes DRG-10\n",
            author="someone",
            files=[".github/workflows/pr-agent-gate.yml"],
            list_open_pulls=lambda: [],
            list_files=lambda _number: [],
        )
        self.assertFalse(result.ok)
        self.assertIn(TEMPLATE, result.message)
        self.assertIn("Surface:", result.message)


class GlobOverlapTests(unittest.TestCase):
    def test_literal_paths_overlap_only_when_equal(self) -> None:
        self.assertTrue(gate.paths_overlap("src/Foo.cs", "src/Foo.cs"))
        self.assertTrue(gate.paths_overlap("./src/Foo.cs", "src\\Foo.cs"))
        self.assertFalse(gate.paths_overlap("src/Foo.cs", "src/Foo.cs.meta"))
        self.assertFalse(gate.paths_overlap("src/Foo", "src/FooBar/Bar.cs"))

    def test_star_does_not_cross_directories(self) -> None:
        self.assertTrue(gate.paths_overlap("src/*.cs", "src/Foo.cs"))
        self.assertFalse(gate.paths_overlap("src/*.cs", "src/Nested/Foo.cs"))
        self.assertTrue(gate.paths_overlap("src/**/*.cs", "src/Nested/Foo.cs"))
        self.assertFalse(gate.paths_overlap("src/Foo*.cs", "src/Bar*.cs"))
        self.assertTrue(gate.paths_overlap("src/Foo*.cs", "src/FooBar.cs"))
        self.assertFalse(gate.paths_overlap("*.cs", "*.md"))
        self.assertTrue(gate.paths_overlap("*.cs", "*.cs"))

    def test_directory_prefix_and_double_star(self) -> None:
        self.assertTrue(gate.paths_overlap("src/Foo/", "src/Foo/Bar.cs"))
        self.assertFalse(gate.paths_overlap("src/Foo/", "src/FooBar/Bar.cs"))
        self.assertTrue(gate.paths_overlap("**", "src/Foo.cs"))
        self.assertFalse(gate.paths_overlap("src/**", "other/Foo.cs"))

    def test_question_mark_and_replay_golden_glob(self) -> None:
        self.assertTrue(gate.paths_overlap("src/A?.cs", "src/Ab.cs"))
        self.assertFalse(gate.paths_overlap("src/A?.cs", "src/Abc.cs"))
        golden = "tests/regression/replay-golden-baltic-engage-2026-06-02.txt"
        self.assertTrue(gate.paths_overlap("tests/regression/replay-golden-*", golden))
        self.assertFalse(gate.paths_overlap("tests/regression/replay-golden-*", "tests/regression/README.md"))


class HardNoTests(unittest.TestCase):
    def test_hard_no_list_matches_sprint_coordination_paths(self) -> None:
        self.assertEqual(gate.HARD_NO_PATHS, HARD_NO)

    def test_named_surfaces_and_globs_that_cover_them_fail(self) -> None:
        bridge = "src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs"
        result = gate.evaluate_surface(
            number=3,
            body="Closes DRG-3\nSurface: src/**/*.cs\n",
            author="someone",
            files=["README.md"],
            list_open_pulls=lambda: [],
            list_files=lambda _number: [],
        )
        self.assertFalse(result.ok)
        self.assertIn(bridge, result.message)
        self.assertIn("sprint-coordination.mdc", result.message)

    def test_changed_replay_golden_data_fails_and_test_files_do_not(self) -> None:
        golden = gate.evaluate_surface(
            number=4,
            body="Closes DRG-4\nSurface: README.md\n",
            author="someone",
            files=["tests/regression/replay-golden-baltic-engage-2026-06-02.txt"],
            list_open_pulls=lambda: [],
            list_files=lambda _number: [],
        )
        self.assertFalse(golden.ok)
        self.assertIn("replay-golden-baltic-engage-2026-06-02.txt", golden.message)

        tests = gate.evaluate_surface(
            number=5,
            body="Closes DRG-5\nSurface: src/ProjectAegis.Delegation.Tests/Input/C2CommandIssuanceTests.cs\n",
            author="someone",
            files=["src/ProjectAegis.Delegation.Tests/Input/C2CommandIssuanceTests.cs"],
            list_open_pulls=lambda: [],
            list_files=lambda _number: [],
        )
        self.assertTrue(tests.ok, tests.message)

    def test_this_gate_surface_is_not_a_hard_no(self) -> None:
        result = gate.evaluate_surface(
            number=6,
            body=(
                "Closes DRG-776\n"
                "Surface: .github/workflows/pr-agent-gate.yml, "
                ".github/scripts/pr_agent_gate.py, "
                ".github/scripts/test_pr_agent_gate.py\n"
            ),
            author="drgaciw",
            files=[
                ".github/workflows/pr-agent-gate.yml",
                ".github/scripts/pr_agent_gate.py",
                ".github/scripts/test_pr_agent_gate.py",
            ],
            list_open_pulls=lambda: [],
            list_files=lambda _number: [],
        )
        self.assertTrue(result.ok, result.message)


class DuplicateSurfaceTests(unittest.TestCase):
    def test_reports_the_other_pr_and_overlapping_paths(self) -> None:
        others = [
            {
                "number": 6,
                "title": "mine",
                "body": "Surface: .github/workflows/pr-agent-gate.yml\n",
            },
            {
                "number": 42,
                "title": "Other agent",
                "body": "Closes DRG-42\nSurface: .github/scripts/*.py, src/Unique.cs\n",
                "draft": True,
            },
        ]

        def list_files(number: int) -> list[str]:
            if number == 42:
                return ["src/Shared.cs", "README.md"]
            return []

        result = gate.evaluate_surface(
            number=6,
            body="Closes DRG-776\nSurface: .github/scripts/*.py\n",
            author="someone",
            files=["src/Shared.cs", ".github/workflows/pr-agent-gate.yml"],
            list_open_pulls=lambda: others,
            list_files=list_files,
        )
        self.assertFalse(result.ok)
        self.assertIn("#42", result.message)
        self.assertIn(".github/scripts/*.py", result.message)
        self.assertIn("src/Shared.cs", result.message)
        self.assertNotIn("#6", result.message)

    def test_surface_glob_overlaps_another_prs_changed_file(self) -> None:
        result = gate.evaluate_surface(
            number=7,
            body="Closes DRG-7\nSurface: src/ProjectAegis.Data/**\n",
            author="someone",
            files=["README.md"],
            list_open_pulls=lambda: [
                {"number": 8, "title": "data", "body": "Surface: docs/only.md\n"}
            ],
            list_files=lambda number: ["src/ProjectAegis.Data/WriteGate/README.md"]
            if number == 8
            else [],
        )
        self.assertFalse(result.ok)
        self.assertIn("#8", result.message)
        self.assertIn("src/ProjectAegis.Data/WriteGate/README.md", result.message)

    def test_no_overlap_passes(self) -> None:
        result = gate.evaluate_surface(
            number=9,
            body="Closes DRG-9\nSurface: docs/a.md\n",
            author="someone",
            files=["docs/a.md"],
            list_open_pulls=lambda: [
                {"number": 10, "title": "other", "body": "Surface: docs/b.md\n"}
            ],
            list_files=lambda _number: ["docs/b.md"],
        )
        self.assertTrue(result.ok, result.message)


class GitHubApiTests(unittest.TestCase):
    def test_next_link_and_pagination(self) -> None:
        self.assertEqual(
            gate.next_link('<https://example.test/p2>; rel="next", <https://example.test/p1>; rel="prev"'),
            "https://example.test/p2",
        )
        self.assertIsNone(gate.next_link(None))
        calls: list[str] = []

        def fetch(url: str) -> tuple[int, dict[str, str], str]:
            calls.append(url)
            if url.endswith("page=1"):
                headers = {"link": '<https://example.test/pulls?page=2>; rel="next"'}
                return 200, headers, json.dumps([{"number": 1}])
            return 200, {}, json.dumps([{"number": 2}])

        rows = gate.request_json_pages(fetch, "https://example.test/pulls?page=1")
        self.assertEqual([row["number"] for row in rows], [1, 2])
        self.assertEqual(len(calls), 2)

    def test_rate_limit_warns_and_permission_errors_fail(self) -> None:
        self.assertTrue(
            gate.is_rate_limited(403, {"x-ratelimit-remaining": "0"}, "API rate limit exceeded")
        )
        self.assertTrue(gate.is_rate_limited(429, {}, "secondary rate limit"))
        self.assertFalse(
            gate.is_rate_limited(403, {"x-ratelimit-remaining": "4000"}, "Resource not accessible by integration")
        )

        def list_open_pulls() -> list[dict[str, object]]:
            raise gate.RateLimited("primary rate limit")

        warned = gate.evaluate_surface(
            number=11,
            body="Closes DRG-11\nSurface: docs/a.md\n",
            author="someone",
            files=["docs/a.md"],
            list_open_pulls=list_open_pulls,
            list_files=lambda _number: [],
        )
        self.assertTrue(warned.ok)
        self.assertTrue(warned.warning)
        self.assertIn("rate limit", warned.message.lower())

        def list_files(_number: int) -> list[str]:
            raise gate.ApiError(403, "Resource not accessible by integration")

        failed = gate.evaluate_surface(
            number=12,
            body="Closes DRG-12\nSurface: docs/a.md\n",
            author="someone",
            files=["docs/a.md"],
            list_open_pulls=lambda: [{"number": 13, "title": "x", "body": "Surface: docs/b.md\n"}],
            list_files=list_files,
        )
        self.assertFalse(failed.ok)
        self.assertIn("403", failed.message)

    def test_client_lists_open_pulls_and_renames(self) -> None:
        pulls_url = gate.build_pulls_url("drgaciw/cmano-clone")
        files_url = gate.build_files_url("drgaciw/cmano-clone", 15)
        self.assertIn("state=open", pulls_url)
        self.assertIn("per_page=100", pulls_url)
        self.assertIn("/pulls/15/files", files_url)

        def fetch(url: str) -> tuple[int, dict[str, str], str]:
            if "/pulls?" in url or url.endswith("/pulls?state=open&per_page=100"):
                return 200, {}, json.dumps(
                    [{"number": 15, "title": "Draft", "body": "Surface: a.cs\n", "draft": True}]
                )
            return 200, {}, json.dumps(
                [{"filename": "new.cs", "previous_filename": "old.cs", "status": "renamed"}]
            )

        api = gate.GitHubApi("drgaciw/cmano-clone", "token", fetch=fetch)
        pulls = api.list_open_pulls()
        self.assertEqual(pulls[0]["number"], 15)
        self.assertTrue(pulls[0]["draft"])
        self.assertEqual(api.list_files(15), ["new.cs", "old.cs"])

        request = gate.build_request("https://api.github.com/repos/x/y/pulls", "secret-token")
        self.assertIsInstance(request, urllib.request.Request)
        self.assertEqual(request.get_header("Authorization"), "Bearer secret-token")
        self.assertEqual(request.get_header("User-agent"), "pr-agent-gate")
        self.assertNotIn("secret-token", request.get_header("User-agent"))


class WorkflowContractTests(unittest.TestCase):
    def test_workflow_is_pull_request_only_and_unprivileged(self) -> None:
        workflow = REPO_ROOT / ".github" / "workflows" / "pr-agent-gate.yml"
        text = workflow.read_text(encoding="utf-8")
        self.assertIn("pull_request:", text)
        self.assertNotIn("pull_request_target", text)
        self.assertNotIn("workflow_dispatch", text)
        self.assertNotIn("actions/checkout", text)
        self.assertNotIn("self-hosted", text)
        self.assertNotIn("secrets.", text)
        self.assertIn("ubuntu-latest", text)
        self.assertIn("contents: read", text)
        self.assertIn("pull-requests: read", text)
        self.assertIn("timeout-minutes: 5", text)
        self.assertIn("cancel-in-progress: true", text)
        self.assertIn("github.event.pull_request.number", text)
        self.assertIn("DRG-id", text)
        self.assertIn("Duplicate Surface", text)
        self.assertIn(".github/scripts/pr_agent_gate.py", text)
        self.assertIn("fail-fast: false", text)
        start = text.index("python3 - <<'PY'\n") + len("python3 - <<'PY'\n")
        end = text.index("\n          PY\n", start)
        lines = []
        for line in text[start:end].splitlines():
            if line.startswith("          "):
                lines.append(line[10:])
            else:
                lines.append(line)
        compile("\n".join(lines) + "\n", "pr-agent-gate.yml", "exec")


if __name__ == "__main__":
    unittest.main()
