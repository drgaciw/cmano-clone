#!/usr/bin/env python3
"""Pull-request gate for a DRG id and duplicate Surface reservations.

The workflow downloads this file and runs it. It does not import third-party
packages. Hard-no paths come from ``.cursor/rules/sprint-coordination.mdc``
section 2:

* ``DelegationBridge`` hotpath and ``DelegationBridgeHost`` (owner-only; the
  host ``.cs`` and ``.meta``), ``CatalogWriteGate``, ``SimulationSession``,
  ``MapPlaceholderPanelHost``, and ``GlobeMapProductHost`` (Unity host
  ``.meta`` files are part of those assets).
* CMD-31 / CMD-32 / CMD-34 seams, mapped to the implementation files named by
  the UI-maturity kickoff: ``C2CommandIssuance``, ``C2PlayerCommandBridge``,
  ``DatalinkPictureProjection``, and ``TacticalOverlayProjection``.
* ReplayGolden data under ``tests/regression/replay-golden-*``.

S122 / Launch / Phase N and the H4/H5/H6 ``implementation: gated`` rows name
programs, not paths. This checker does not invent globs for them.
"""

from __future__ import annotations

import email.utils
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request
from collections.abc import Callable, Mapping, Sequence
from dataclasses import dataclass
from datetime import timezone

DRG_ID = re.compile(r"\bDRG-\d+\b", re.IGNORECASE)
SURFACE_LINE = re.compile(
    r"^[ \t]*(?:>[ \t]*)?(?:[-*+][ \t]+)?Surface:[ \t]*(.*)$",
    re.IGNORECASE | re.MULTILINE,
)
TEMPLATE = ".github/pull_request_template.md"
API_VERSION = "2022-11-28"
USER_AGENT = "pr-agent-gate"
# One retry sleeps at most this long, so a rate-limit wait stays under two minutes.
MAX_RATE_LIMIT_WAIT_SECONDS = 60

# Canonical owner-only paths. A declared glob fails when it can match one of
# these, and a changed file fails when it matches one of these.
HARD_NO_PATHS = (
    "src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs",
    "unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs",
    "unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs.meta",
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

# A hard-no hit is waived only while this label, applied by an owner login, is on the PR.
SIGNOFF_LABEL = "owner-signoff"
OWNER_LOGINS = frozenset({"drgaciw"})

Fetch = Callable[[str], tuple[int, Mapping[str, str], str]]
PullList = Callable[[], Sequence[Mapping[str, object]]]
FileList = Callable[[int], Sequence[str]]


class RateLimited(Exception):
    """The GitHub REST API asked us to back off."""


class ApiError(Exception):
    """A GitHub REST call failed for a reason other than rate limiting."""

    def __init__(self, status: int, body: str) -> None:
        super().__init__(f"HTTP {status}")
        self.status = status
        self.body = body


@dataclass(frozen=True)
class CheckResult:
    """One gate result. ``warning`` means the check passed only by degrading."""

    ok: bool
    message: str
    skipped: bool = False
    warning: bool = False


def has_drg_id(title: str | None, branch: str | None, body: str | None) -> bool:
    """Return whether title, head branch, or body contains a DRG-<number> id."""
    haystack = "\n".join((title or "", branch or "", body or ""))
    return DRG_ID.search(haystack) is not None


def is_skipped_author(login: str | None) -> bool:
    """Return whether this author is Renovate or Dependabot.

    Owner logins, including ``drgaciw``, are not skipped.
    """
    name = (login or "").strip().lower()
    if name.endswith("[bot]"):
        name = name[: -len("[bot]")]
    return name in {"dependabot", "renovate"}


def parse_surface(body: str | None) -> list[str] | None:
    """Return Surface tokens, ``[]`` when the line is blank, or ``None`` when absent."""
    if not body:
        return None
    match = SURFACE_LINE.search(body)
    if match is None:
        return None
    return _split_surface(match.group(1))


def paths_overlap(left: str, right: str) -> bool:
    """Return whether two paths or globs can name a common file.

    ``*`` and ``?`` apply inside one path segment. ``**`` spans segments.
    A trailing slash means the directory and everything under it. ``*`` does
    not match ``/``.
    """
    left_segments = _pattern_segments(left)
    right_segments = _pattern_segments(right)
    return _segments_overlap(left_segments, right_segments)


def next_link(link_header: str | None) -> str | None:
    """Return the ``rel=next`` URL from a GitHub Link header."""
    if not link_header:
        return None
    for part in link_header.split(","):
        bits = [bit.strip() for bit in part.split(";") if bit.strip()]
        if not bits:
            continue
        url = bits[0]
        if url.startswith("<") and url.endswith(">"):
            url = url[1:-1]
        rels = []
        for bit in bits[1:]:
            if bit.lower().startswith("rel="):
                rels.append(bit.split("=", 1)[1].strip().strip('"').lower())
        if "next" in rels:
            return url
    return None


def is_rate_limited(status: int, headers: Mapping[str, str], body: str) -> bool:
    """Return whether this response is a primary or secondary rate limit."""
    if status == 429:
        return True
    if status != 403:
        return False
    if headers.get("x-ratelimit-remaining") == "0":
        return True
    lowered = (body or "").lower()
    return "rate limit" in lowered or "secondary rate" in lowered


def rate_limit_delay_seconds(
    headers: Mapping[str, str],
    now: float,
    max_wait: float = MAX_RATE_LIMIT_WAIT_SECONDS,
) -> float:
    """Return how long to wait before the single rate-limit retry.

    ``Retry-After`` wins over ``x-ratelimit-reset``. The wait is never negative
    and never longer than ``max_wait`` (60 seconds by default).
    """
    lowered = {str(key).lower(): str(value) for key, value in headers.items()}
    delay: float | None = None
    retry_after = lowered.get("retry-after")
    if retry_after is not None:
        text = retry_after.strip()
        try:
            delay = float(text)
        except ValueError:
            parsed = email.utils.parsedate_to_datetime(text)
            if parsed is not None:
                if parsed.tzinfo is None:
                    parsed = parsed.replace(tzinfo=timezone.utc)
                delay = parsed.timestamp() - now
    if delay is None:
        reset = lowered.get("x-ratelimit-reset")
        if reset is not None:
            try:
                delay = float(reset.strip()) - now
            except ValueError:
                delay = None
    if delay is None:
        delay = 1.0
    if delay < 0:
        delay = 0.0
    if delay > max_wait:
        delay = float(max_wait)
    return delay


def request_json_pages(
    fetch: Fetch,
    url: str,
    *,
    sleep: Callable[[float], None] = time.sleep,
    clock: Callable[[], float] = time.time,
    max_wait: float = MAX_RATE_LIMIT_WAIT_SECONDS,
) -> list[object]:
    """Follow Link headers and return the concatenated JSON lists.

    A rate-limited response is retried once after ``rate_limit_delay_seconds``.
    A second rate limit raises ``RateLimited``.
    """
    items: list[object] = []
    seen: set[str] = set()
    while url and url not in seen:
        seen.add(url)
        status, headers, body = fetch(url)
        lowered = {str(key).lower(): str(value) for key, value in headers.items()}
        if is_rate_limited(status, lowered, body):
            sleep(rate_limit_delay_seconds(lowered, clock(), max_wait))
            status, headers, body = fetch(url)
            lowered = {str(key).lower(): str(value) for key, value in headers.items()}
            if is_rate_limited(status, lowered, body):
                raise RateLimited((body or f"HTTP {status}")[:300])
        if status < 200 or status >= 300:
            raise ApiError(status, (body or "")[:500])
        try:
            payload = json.loads(body)
        except json.JSONDecodeError as exc:
            raise ApiError(status, f"invalid JSON: {exc}") from exc
        if not isinstance(payload, list):
            raise ApiError(status, "expected a JSON list")
        items.extend(payload)
        url = next_link(lowered.get("link")) or ""
    return items


def build_pulls_url(repo: str, api_base: str = "https://api.github.com") -> str:
    """Return the first page of open pull requests, drafts included."""
    return f"{api_base.rstrip('/')}/repos/{repo}/pulls?state=open&per_page=100"


def build_files_url(repo: str, number: int, api_base: str = "https://api.github.com") -> str:
    """Return the first page of files for one pull request."""
    return f"{api_base.rstrip('/')}/repos/{repo}/pulls/{number}/files?per_page=100"


def build_events_url(repo: str, number: int, api_base: str = "https://api.github.com") -> str:
    """Return the first page of issue events for one pull request."""
    return f"{api_base.rstrip('/')}/repos/{repo}/issues/{number}/events?per_page=100"


def owner_signoff_from_events(events: Sequence[object]) -> bool:
    """Return whether the latest label change for ``SIGNOFF_LABEL`` is an owner's ``labeled``."""
    active = False
    for event in events:
        if not isinstance(event, dict):
            continue
        label = event.get("label")
        if not isinstance(label, dict) or label.get("name") != SIGNOFF_LABEL:
            continue
        kind = event.get("event")
        actor = event.get("actor")
        login = actor.get("login") if isinstance(actor, dict) else None
        if kind == "labeled":
            active = isinstance(login, str) and login.lower() in OWNER_LOGINS
        elif kind == "unlabeled":
            active = False
    return active


def build_request(url: str, token: str) -> urllib.request.Request:
    """Build a read-only GitHub API request. The token is a header, not a log line."""
    return urllib.request.Request(
        url,
        headers={
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": API_VERSION,
            "User-Agent": USER_AGENT,
        },
    )


class GitHubApi:
    """Small REST client for open pull requests and their changed files."""

    def __init__(self, repo: str, token: str, fetch: Fetch | None = None, api_base: str = "https://api.github.com") -> None:
        self.repo = repo
        self.token = token
        self.api_base = api_base
        self._fetch = fetch or self._urlopen_fetch

    def list_open_pulls(self) -> list[dict[str, object]]:
        rows = request_json_pages(self._fetch, build_pulls_url(self.repo, self.api_base))
        pulls: list[dict[str, object]] = []
        for row in rows:
            if not isinstance(row, dict):
                continue
            number = row.get("number")
            if not isinstance(number, int):
                continue
            title = row.get("title")
            body = row.get("body")
            pulls.append(
                {
                    "number": number,
                    "title": title if isinstance(title, str) else "",
                    "body": body if isinstance(body, str) else "",
                    "draft": bool(row.get("draft")),
                }
            )
        return pulls

    def list_files(self, number: int) -> list[str]:
        rows = request_json_pages(self._fetch, build_files_url(self.repo, number, self.api_base))
        names: list[str] = []
        for row in rows:
            if not isinstance(row, dict):
                continue
            for key in ("filename", "previous_filename"):
                value = row.get(key)
                if isinstance(value, str) and value and value not in names:
                    names.append(value)
        return names

    def owner_signoff(self, number: int) -> bool:
        """Return whether an owner applied ``SIGNOFF_LABEL`` and it is still set."""
        return owner_signoff_from_events(
            request_json_pages(self._fetch, build_events_url(self.repo, number, self.api_base))
        )

    def _urlopen_fetch(self, url: str) -> tuple[int, Mapping[str, str], str]:
        request = build_request(url, self.token)
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                raw = response.read().decode("utf-8")
                headers = {key.lower(): value for key, value in response.headers.items()}
                return response.status, headers, raw
        except urllib.error.HTTPError as exc:
            raw = exc.read().decode("utf-8", "replace")
            headers = {key.lower(): value for key, value in exc.headers.items()} if exc.headers else {}
            return exc.code, headers, raw


def evaluate_drg_id(title: str | None, branch: str | None, body: str | None, author: str | None) -> CheckResult:
    """Check (a): a DRG id is present, unless the author is Renovate or Dependabot."""
    if is_skipped_author(author):
        return CheckResult(
            ok=True,
            skipped=True,
            message=f"Skipped DRG-id check for Renovate or Dependabot author `{author}`.",
        )
    if has_drg_id(title, branch, body):
        return CheckResult(ok=True, message="DRG-id check passed.")
    return CheckResult(
        ok=False,
        message=(
            "DRG-id check failed: the title, head branch, and body have no DRG-<number> id. "
            "A CMA- id on its own does not count."
        ),
    )


def evaluate_surface(
    *,
    number: int,
    body: str | None,
    author: str | None,
    files: Sequence[str],
    list_open_pulls: PullList,
    list_files: FileList,
    owner_signoff: Callable[[], bool] | None = None,
) -> CheckResult:
    """Check (b): Surface overlap, changed-file overlap, and hard-no paths.

    ``owner_signoff`` waives hard-no hits only; overlaps with other pull requests still fail.
    """
    if is_skipped_author(author):
        return CheckResult(
            ok=True,
            skipped=True,
            message=f"Skipped duplicate Surface check for Renovate or Dependabot author `{author}`.",
        )
    declared = parse_surface(body)
    if not declared:
        return CheckResult(ok=False, message=_missing_surface_message())

    hard_hits = _hard_no_hits([*declared, *files])
    if hard_hits and owner_signoff is not None:
        try:
            if owner_signoff():
                hard_hits = []
        except (RateLimited, ApiError):
            pass
    try:
        others = list(list_open_pulls())
    except RateLimited as exc:
        if hard_hits:
            return CheckResult(ok=False, message=_failure_message(hard_hits, [], rate_note=str(exc)))
        return _rate_limit_result(exc)
    except ApiError as exc:
        return _api_error_result(exc)

    overlaps: list[str] = []
    # Surface lines are already in the open-PR list. Compare those before any
    # per-PR file fetch so a rate limit cannot hide a collision we already have.
    comparable: list[tuple[int, str]] = []
    for other in others:
        other_number = other.get("number")
        if not isinstance(other_number, int) or other_number == number:
            continue
        their_body = other.get("body")
        their_surface = parse_surface(their_body if isinstance(their_body, str) else None) or []
        title = other.get("title")
        title_text = title.strip() if isinstance(title, str) else ""
        overlaps.extend(
            _pair_overlaps(
                our_surface=declared,
                our_files=files,
                their_number=other_number,
                their_title=title_text,
                their_surface=their_surface,
                their_files=(),
            )
        )
        comparable.append((other_number, title_text))

    for other_number, title_text in comparable:
        try:
            their_files = list(list_files(other_number))
        except RateLimited as exc:
            if hard_hits or overlaps:
                return CheckResult(
                    ok=False,
                    message=_failure_message(hard_hits, overlaps, rate_note=str(exc)),
                )
            return _rate_limit_result(exc)
        except ApiError as exc:
            return _api_error_result(exc)
        overlaps.extend(
            _pair_overlaps(
                our_surface=declared,
                our_files=files,
                their_number=other_number,
                their_title=title_text,
                their_surface=(),
                their_files=their_files,
            )
        )
    if hard_hits or overlaps:
        return CheckResult(ok=False, message=_failure_message(hard_hits, overlaps, rate_note=None))
    return CheckResult(
        ok=True,
        message="Duplicate Surface check passed. No overlap with another open pull request and no hard-no path.",
    )


def load_pull_request(env: Mapping[str, str]) -> dict[str, object]:
    """Read the pull request from the event file, then from explicit env vars."""
    event_path = env.get("GITHUB_EVENT_PATH")
    if event_path:
        with open(event_path, encoding="utf-8") as handle:
            event = json.load(handle)
        pull = event.get("pull_request") if isinstance(event, dict) else None
        if isinstance(pull, dict):
            head = pull.get("head") if isinstance(pull.get("head"), dict) else {}
            user = pull.get("user") if isinstance(pull.get("user"), dict) else {}
            return {
                "title": pull.get("title") if isinstance(pull.get("title"), str) else "",
                "body": pull.get("body") if isinstance(pull.get("body"), str) else "",
                "branch": head.get("ref") if isinstance(head.get("ref"), str) else "",
                "author": user.get("login") if isinstance(user.get("login"), str) else "",
                "number": pull.get("number") if isinstance(pull.get("number"), int) else None,
            }
    number_text = env.get("PR_NUMBER", "")
    number = int(number_text) if number_text.isdigit() else None
    return {
        "title": env.get("PR_TITLE", ""),
        "body": env.get("PR_BODY", ""),
        "branch": env.get("PR_HEAD", ""),
        "author": env.get("PR_AUTHOR", ""),
        "number": number,
    }


def emit(result: CheckResult, step_name: str) -> int:
    """Print the result, append the job summary, and return the process status."""
    text = result.message.strip() + "\n"
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(f"### {step_name}\n\n{text}\n")
    flat = " ".join(text.split())
    if result.warning:
        print(f"::warning title={step_name}::{flat}")
    elif not result.ok:
        print(f"::error title={step_name}::{flat}")
    sys.stdout.write(text)
    return 0 if result.ok else 1


def main(argv: Sequence[str] | None = None) -> int:
    """Run the ``drg-id`` or ``surface`` command."""
    args = list(sys.argv if argv is None else argv)
    if len(args) != 2 or args[1] not in {"drg-id", "surface"}:
        print("usage: pr_agent_gate.py drg-id|surface", file=sys.stderr)
        return 2
    pull = load_pull_request(os.environ)
    if args[1] == "drg-id":
        result = evaluate_drg_id(
            str(pull["title"]),
            str(pull["branch"]),
            str(pull["body"]),
            str(pull["author"]),
        )
        return emit(result, "DRG-id")
    number = pull["number"]
    if not isinstance(number, int):
        return emit(CheckResult(ok=False, message="Duplicate Surface check failed: pull request number is missing."), "Duplicate Surface")
    api = GitHubApi(
        os.environ.get("GITHUB_REPOSITORY", ""),
        os.environ.get("GITHUB_TOKEN", ""),
        api_base=os.environ.get("GITHUB_API_URL", "https://api.github.com"),
    )
    try:
        files = api.list_files(number)
        result = evaluate_surface(
            number=number,
            body=str(pull["body"]),
            author=str(pull["author"]),
            files=files,
            list_open_pulls=api.list_open_pulls,
            list_files=api.list_files,
            owner_signoff=lambda: api.owner_signoff(number),
        )
    except RateLimited as exc:
        result = _rate_limit_result(exc)
    except ApiError as exc:
        result = _api_error_result(exc)
    return emit(result, "Duplicate Surface")


def _split_surface(value: str) -> list[str]:
    tokens: list[str] = []
    for comma_part in value.split(","):
        for raw in comma_part.split():
            token = raw.strip().strip("`").strip("'").strip('"').strip()
            token = token.rstrip(".,;")
            if token:
                tokens.append(token)
    return tokens


def _pattern_segments(token: str) -> list[str]:
    text = token.strip().replace("\\", "/")
    while text.startswith("./"):
        text = text[2:]
    directory = text.endswith("/")
    text = text.strip("/")
    segments = [segment for segment in text.split("/") if segment not in ("", ".")]
    if directory:
        segments.append("**")
    return segments


def _segments_overlap(left: Sequence[str], right: Sequence[str]) -> bool:
    memo: dict[tuple[int, int], bool] = {}

    def rec(i: int, j: int) -> bool:
        key = (i, j)
        if key in memo:
            return memo[key]
        if i == len(left) and j == len(right):
            result = True
        elif i < len(left) and left[i] == "**":
            result = rec(i + 1, j) or (j < len(right) and rec(i, j + 1))
        elif j < len(right) and right[j] == "**":
            result = rec(i, j + 1) or (i < len(left) and rec(i + 1, j))
        elif i == len(left) or j == len(right):
            result = False
        else:
            result = _glob_segments_overlap(left[i], right[j]) and rec(i + 1, j + 1)
        memo[key] = result
        return result

    return rec(0, 0)


def _glob_segments_overlap(left: str, right: str) -> bool:
    memo: dict[tuple[int, int], bool] = {}

    def rec(i: int, j: int) -> bool:
        key = (i, j)
        if key in memo:
            return memo[key]
        if i == len(left) and j == len(right):
            result = True
        elif i < len(left) and left[i] == "*":
            result = rec(i + 1, j) or (j < len(right) and rec(i, j + 1))
        elif j < len(right) and right[j] == "*":
            result = rec(i, j + 1) or (i < len(left) and rec(i + 1, j))
        elif i == len(left) or j == len(right):
            result = False
        elif left[i] == "?" or right[j] == "?":
            result = rec(i + 1, j + 1)
        else:
            result = left[i] == right[j] and rec(i + 1, j + 1)
        memo[key] = result
        return result

    return rec(0, 0)


def _hard_no_hits(tokens: Sequence[str]) -> list[tuple[str, str]]:
    hits: list[tuple[str, str]] = []
    seen: set[tuple[str, str]] = set()
    for token in tokens:
        for blocked in HARD_NO_PATHS:
            pair = (token, blocked)
            if pair not in seen and paths_overlap(token, blocked):
                seen.add(pair)
                hits.append(pair)
    return hits


def _pair_overlaps(
    *,
    our_surface: Sequence[str],
    our_files: Sequence[str],
    their_number: int,
    their_title: str,
    their_surface: Sequence[str],
    their_files: Sequence[str],
) -> list[str]:
    label = f"#{their_number}"
    if their_title:
        label = f"{label} ({their_title.replace(chr(10), ' ')})"
    found: list[str] = []

    def add(kind: str, left: str, other_kind: str, right: str) -> None:
        if paths_overlap(left, right):
            found.append(f"{kind} `{left}` overlaps {label} {other_kind} `{right}`")

    for left in our_surface:
        for right in their_surface:
            add("surface", left, "surface", right)
        for right in their_files:
            add("surface", left, "changed file", right)
    for left in our_files:
        for right in their_surface:
            add("changed file", left, "surface", right)
        for right in their_files:
            add("changed file", left, "changed file", right)
    return found


def _missing_surface_message() -> str:
    return (
        "Duplicate Surface check failed: no Surface: line in the pull request body. "
        f"Add one on its own line after `Closes DRG-###`, using {TEMPLATE}."
    )


def _failure_message(hard_hits: Sequence[tuple[str, str]], overlaps: Sequence[str], rate_note: str | None) -> str:
    lines = ["Duplicate Surface check failed."]
    if hard_hits:
        lines.append(
            "Surface or changed files touch a hard-no owner-only surface from "
            ".cursor/rules/sprint-coordination.mdc §2. Owner sign-off is required before these paths can change:"
        )
        for token, blocked in hard_hits[:30]:
            lines.append(f"- `{token}` overlaps hard-no `{blocked}`")
        if len(hard_hits) > 30:
            lines.append(f"- and {len(hard_hits) - 30} more hard-no matches")
    if overlaps:
        lines.append("Overlaps another open pull request (drafts included):")
        for line in overlaps[:30]:
            lines.append(f"- {line}")
        if len(overlaps) > 30:
            lines.append(f"- and {len(overlaps) - 30} more overlaps")
    if rate_note:
        lines.append(f"GitHub API rate limit interrupted the rest of the overlap scan: {rate_note}")
    return "\n".join(lines)


def _rate_limit_result(exc: Exception) -> CheckResult:
    return CheckResult(
        ok=True,
        warning=True,
        message=(
            "RATE_LIMIT_WARN GitHub API rate limit after one retry; "
            "duplicate Surface check passed with a warning. "
            f"{exc}"
        ),
    )


def _api_error_result(exc: ApiError) -> CheckResult:
    detail = " ".join(exc.body.split())[:300]
    return CheckResult(
        ok=False,
        message=f"Duplicate Surface check failed: GitHub API error {exc.status}. {detail}",
    )


if __name__ == "__main__":
    raise SystemExit(main())
