"""Independent specification-derived contract validators and reference evaluator."""

from __future__ import annotations

import json
import math
import re
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Any, Iterable, Mapping, Sequence

# ---------------------------------------------------------------------------
# Jev wire limits (tools/jev-worker/src/index.ts & tools/jev/classify_uat.py)
# ---------------------------------------------------------------------------
MAX_BODY_BYTES = 32 * 1024  # 32,768 UTF-8 bytes
MAX_QUESTIONS = 20
KEY_PATTERN = re.compile(r"^[a-z0-9_]{1,40}$")
MAX_INSTRUCTIONS_CHARS = 600
MAX_CRITERIA_TEXT_CHARS = 400
MIN_CHOICE_CRITERIA = 2
MAX_CHOICE_CRITERIA = 255
MIN_SCORE_CRITERIA = 2
MAX_SCORE_CRITERIA = 10

# ---------------------------------------------------------------------------
# Rubric thresholds (production/qa/uat-dod-rubric.md & data/jev/README.md)
# ---------------------------------------------------------------------------
NOUL_PASS_THRESHOLD = 0.80
CHOICE_HUMAN_REVIEW_FLOOR = 0.50
SCORE_COMPLETE_THRESHOLD = 1.50
VALID_STATUSES = ("complete", "incomplete", "missing")
VALID_LIFECYCLE_STATES = ("active", "draft", "deferred_phase_n", "shipped")
VALID_DOMAIN_PROJECTS = (
    "cmano-clone",
    "Combat Interaction UX",
    "Aegis Horizons H4-H6",
)
VALID_CATEGORIES = ("A", "B", "C", "NotionOnly")
VALID_RECOMMENDED_ACTIONS = (
    "none",
    "create_linear",
    "update_linear",
    "create_notion",
    "update_notion",
    "both",
)

# ---------------------------------------------------------------------------
# Corpus scope boundaries (38 Git files + 1 NotionOnly source; 17 excluded)
# ---------------------------------------------------------------------------
IN_SCOPE_GIT_FILES: tuple[str, ...] = (
    # Category A: 22 canonical requirements
    "Game-Requirements/requirements/01-Project-Overview.md",
    "Game-Requirements/requirements/02-Core-Gameplay-Loop.md",
    "Game-Requirements/requirements/03-Simulation-Modes.md",
    "Game-Requirements/requirements/04-Agent-Delegation.md",
    "Game-Requirements/requirements/05-Dynamic-Systems-Agent.md",
    "Game-Requirements/requirements/06-Database-Intelligence.md",
    "Game-Requirements/requirements/07-Agentic-Infrastructure.md",
    "Game-Requirements/requirements/08-Agentic-Architecture.md",
    "Game-Requirements/requirements/09-Near-Future-Technologies.md",
    "Game-Requirements/requirements/10-Speculative-Systems.md",
    "Game-Requirements/requirements/11-Agentic-Mission-Editor.md",
    "Game-Requirements/requirements/12-Terms-Glossary.md",
    "Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md",
    "Game-Requirements/requirements/14-Engagement-And-Fire-Control.md",
    "Game-Requirements/requirements/15-Sensor-Detection-And-EW.md",
    "Game-Requirements/requirements/16-Logistics-And-Magazines.md",
    "Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md",
    "Game-Requirements/requirements/18-Combat-Domains.md",
    "Game-Requirements/requirements/19-Cyber-And-Comms.md",
    "Game-Requirements/requirements/20-Command-And-Control-UI.md",
    "Game-Requirements/requirements/21-Platform-Editor.md",
    "Game-Requirements/requirements/22-Drone-Swarm-Platforms.md",
    # Category B: 7 drafts
    "Game-Requirements/drafts/2026-09-05-civilian-gameplay-symbology.md",
    "Game-Requirements/drafts/2026-09-05-military-tactical-symbology.md",
    "Game-Requirements/drafts/23-Kill-Chain-Explainability.md",
    "Game-Requirements/drafts/24-Human-On-The-Loop-Authority.md",
    "Game-Requirements/drafts/25-C2-Nodes-Mission-Command.md",
    "Game-Requirements/drafts/26-Verification-CI-Gauntlet.md",
    "Game-Requirements/drafts/27-Scenario-Library-Campaigns.md",
    # Category C: 2 docs/product + 2 design + 3 superpowers/specs + 1 docs/requirements + 1 docs/engineering
    "docs/product/prd-mvp-ui-2026-07-15.md",
    "docs/product/ui-requirements-brainstorm-2026-07-15.md",
    "design/accessibility-requirements.md",
    "design/difficulty-curve.md",
    "docs/superpowers/specs/2026-08-17-cmd-38-kinematic-map-picture-draft.md",
    "docs/superpowers/specs/2026-08-17-cmd-39-attention-toast-clock-interrupt-draft.md",
    "docs/superpowers/specs/2026-09-30-requirements-planning-reconciliation.md",
    "docs/requirements/impact-sim-c2-requirements-2026-05-29.md",
    "docs/engineering/sim-capability-gap-backlog.md",
)

NOTION_ONLY_PAGE_ID = "3f2f7cb4-e4df-8199-aad8-f1dd50c6831e"
NOTION_ONLY_TITLE = "Milsim Comparative Research & New Requirements — 2026-10-07"

EXCLUDED_PATH_PREFIXES: tuple[str, ...] = (
    "tests/uat/",
    "docs/reference/cmano-db/",
)
EXCLUDED_FILENAME_SUBSTRINGS: tuple[str, ...] = (
    "cpq",
    "lead-to-fulfillment",
    "pricing-lifecycle-management",
)

# ---------------------------------------------------------------------------
# Linear & Notion Governance Constants
# ---------------------------------------------------------------------------
LINEAR_TEAM_DRG_ID = "463fb01d-3daf-46c8-abbb-f5270282d76c"
LINEAR_BACKLOG_STATE_ID = "3fcf0557-d032-48c7-95fb-efd198c700d9"
LINEAR_EXCLUDED_PROJECT_CODES = {"P-DRG-6", "OPEN-CRM-CPQ"}
LINEAR_FORBIDDEN_LABELS = {"Open-CRM-CPQ", "renewals-amendments", "agent:cmano"}
LINEAR_PROTECTED_LABELS = {"hold:owner-go", "NEEDS_OWNER", "single-owner"}
LINEAR_FORBIDDEN_REMEDIATION_STATES = {"Done", "Canceled", "Cancelled", "Duplicate"}
LINEAR_URL_PREFIX = "https://linear.app/drgamtd-workspace/issue/DRG-"

NOTION_TASKS_DB_ID = "a722ffc6-7048-48d9-92db-8207b0ef5cae"
NOTION_TASKS_COLLECTION = "collection://d07263cd-13fa-4fc7-9e57-1a99151e2521"
NOTION_TASKS_VIEW = "view://c5b106a3-eb07-4138-8c72-09200f2cecba"
NOTION_REQUIREMENTS_DB_ID = "cbdc1d28-0571-47c8-a66b-ff9495946913"
NOTION_REQUIREMENTS_VIEW = "view://2b9b349c-b3a3-4bda-884d-2bb968e1f064"
NOTION_FROZEN_MIRROR_PAGE_ID = "3bef7cb4-e4df-81a7-bc47-eedf6573fa42"
NOTION_PROJECT_AEGIS_ID = "3d5f7cb4-e4df-8196-bbd0-fcc18493713a"
NOTION_VALID_TASK_STATUSES = {"Not started", "In progress", "Blocked", "Done", "Archived"}

# Secret & .db3 scanning patterns
SECRET_PATTERNS: tuple[tuple[str, re.Pattern[str]], ...] = (
    ("openrouter_key", re.compile(r"sk-or-v1-[A-Za-z0-9_-]{8,}")),
    ("notion_token", re.compile(r"\b(?:ntn_|secret_)[A-Za-z0-9_-]{8,}")),
    ("linear_token", re.compile(r"lin_api_[A-Za-z0-9_-]{8,}")),
    ("bearer_header", re.compile(r"Bearer\s+[A-Za-z0-9._-]{12,}")),
    ("db3_reference", re.compile(r"\.db3\b", re.IGNORECASE)),
)
DB3_FILENAME_PATTERN = re.compile(r"[A-Za-z0-9_./\\-]+\.db3\b", re.IGNORECASE)
_CODE_LITERAL_SECRET_NAMES = frozenset({"openrouter_key", "notion_token", "linear_token"})


@dataclass
class ValidationResult:
    ok: bool
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)

    def require_ok(self) -> None:
        if not self.ok:
            raise AssertionError("Contract validation failed:\n- " + "\n- ".join(self.errors))


def _is_iso8601(value: Any) -> bool:
    if not isinstance(value, str) or not value.strip():
        return False
    candidate = value.strip()
    if candidate.endswith("Z"):
        candidate = candidate[:-1] + "+00:00"
    try:
        datetime.fromisoformat(candidate)
        return True
    except ValueError:
        return False


def _is_short_string(value: Any, max_len: int) -> bool:
    return isinstance(value, str) and 0 < len(value) <= max_len


# ---------------------------------------------------------------------------
# 1. Jev Wire & Question Pack Validators
# ---------------------------------------------------------------------------

def validate_jev_question(name: str, question: Any, *, require_escape: bool = False) -> list[str]:
    errors: list[str] = []
    if not KEY_PATTERN.match(name):
        errors.append(f"invalid question name: {name!r} (must match ^[a-z0-9_]{{1,40}}$)")
    if not isinstance(question, dict):
        return errors + [f"{name}: question must be an object"]

    if not _is_short_string(question.get("instructions"), MAX_INSTRUCTIONS_CHARS):
        errors.append(f"{name}: instructions required (1..{MAX_INSTRUCTIONS_CHARS} chars)")

    q_type = question.get("type")
    if q_type == "choice":
        criteria = question.get("criteria")
        if not isinstance(criteria, dict):
            errors.append(f"{name}: choice needs a criteria object")
        else:
            if not (MIN_CHOICE_CRITERIA <= len(criteria) <= MAX_CHOICE_CRITERIA):
                errors.append(
                    f"{name}: choice criteria needs {MIN_CHOICE_CRITERIA}..{MAX_CHOICE_CRITERIA} options"
                )
            for k, v in criteria.items():
                if not isinstance(k, str) or not KEY_PATTERN.match(k):
                    errors.append(f"{name}: choice criteria key {k!r} invalid")
                if not _is_short_string(v, MAX_CRITERIA_TEXT_CHARS):
                    errors.append(
                        f"{name}: choice criteria[{k!r}] must be non-empty <= {MAX_CRITERIA_TEXT_CHARS} chars"
                    )
            if require_escape and not any(esc in criteria for esc in ("none", "other")):
                errors.append(f"{name}: choice criteria must include 'none' or 'other' escape option")
    elif q_type == "score":
        criteria = question.get("criteria")
        if not isinstance(criteria, list):
            errors.append(f"{name}: score needs a criteria array")
        else:
            if not (MIN_SCORE_CRITERIA <= len(criteria) <= MAX_SCORE_CRITERIA):
                errors.append(
                    f"{name}: score criteria needs {MIN_SCORE_CRITERIA}..{MAX_SCORE_CRITERIA} levels"
                )
            for idx, item in enumerate(criteria):
                if not _is_short_string(item, MAX_CRITERIA_TEXT_CHARS):
                    errors.append(
                        f"{name}: score criteria[{idx}] must be non-empty <= {MAX_CRITERIA_TEXT_CHARS} chars"
                    )
    elif q_type == "noul":
        criteria = question.get("criteria")
        if criteria is not None:
            if not isinstance(criteria, dict):
                errors.append(f"{name}: noul criteria must be an object when present")
            else:
                for k, v in criteria.items():
                    if not _is_short_string(v, MAX_CRITERIA_TEXT_CHARS):
                        errors.append(
                            f"{name}: noul criteria[{k!r}] must be non-empty <= {MAX_CRITERIA_TEXT_CHARS} chars"
                        )
    else:
        errors.append(f"{name}: type must be choice, score or noul (got {q_type!r})")

    return errors


def validate_jev_request(payload: Any, *, require_escape: bool = False) -> ValidationResult:
    errors: list[str] = []
    if not isinstance(payload, dict):
        return ValidationResult(False, ["body must be a JSON object"])

    raw_bytes = len(json.dumps(payload, ensure_ascii=False).encode("utf-8"))
    if raw_bytes > MAX_BODY_BYTES:
        errors.append(f"payload_too_large: {raw_bytes} > {MAX_BODY_BYTES} UTF-8 bytes")

    state = payload.get("state")
    if not isinstance(state, (str, dict)):
        errors.append("state must be a string or object")

    questions = payload.get("questions")
    if not isinstance(questions, dict):
        errors.append("questions must be an object")
    else:
        if not (1 <= len(questions) <= MAX_QUESTIONS):
            errors.append(f"questions needs 1..{MAX_QUESTIONS} entries (got {len(questions)})")
        for q_name, q_body in questions.items():
            errors.extend(validate_jev_question(str(q_name), q_body, require_escape=require_escape))

    return ValidationResult(len(errors) == 0, errors)


def validate_question_pack(pack: Any) -> ValidationResult:
    errors: list[str] = []
    if not isinstance(pack, dict):
        return ValidationResult(False, ["question pack must be a JSON object"])
    questions = pack.get("questions")
    if not isinstance(questions, dict) or not (1 <= len(questions) <= MAX_QUESTIONS):
        errors.append(f"question pack must contain 1..{MAX_QUESTIONS} questions")
    else:
        for q_name, q_body in questions.items():
            errors.extend(validate_jev_question(str(q_name), q_body, require_escape=True))
    thresholds = pack.get("thresholds")
    if not isinstance(thresholds, dict):
        errors.append("question pack missing thresholds metadata")
    else:
        if thresholds.get("noul_pass_floor", NOUL_PASS_THRESHOLD) != NOUL_PASS_THRESHOLD:
            errors.append(f"noul_pass_floor must equal {NOUL_PASS_THRESHOLD}")
        if thresholds.get("choice_confidence_human_floor", CHOICE_HUMAN_REVIEW_FLOOR) != CHOICE_HUMAN_REVIEW_FLOOR:
            errors.append(f"choice_confidence_human_floor must equal {CHOICE_HUMAN_REVIEW_FLOOR}")
    return ValidationResult(len(errors) == 0, errors)


def parse_jev_primitives(response_body: Mapping[str, Any], questions: Mapping[str, Any]) -> dict[str, Any]:
    answers = response_body.get("answers")
    if not isinstance(answers, dict):
        raise ValueError("Jev response missing answers map")

    parsed: dict[str, Any] = {}
    for q_id, q_spec in questions.items():
        if q_id not in answers:
            continue
        ans = answers[q_id]
        q_type = q_spec.get("type")
        if q_type == "noul":
            prob = ans.get("noul") if isinstance(ans, dict) else ans
            if not isinstance(prob, (int, float)) or isinstance(prob, bool):
                raise ValueError(f"{q_id}: noul answer must be numeric probability")
            prob_f = float(prob)
            if not (0.0 <= prob_f <= 1.0):
                raise ValueError(f"{q_id}: noul probability out of [0, 1]: {prob_f}")
            parsed[q_id] = {"type": "noul", "noul": prob_f, "passes": prob_f >= NOUL_PASS_THRESHOLD}
        elif q_type == "choice":
            if not isinstance(ans, dict) or "choice" not in ans or "confidence" not in ans:
                raise ValueError(f"{q_id}: choice answer must include choice and confidence")
            criteria = q_spec.get("criteria")
            if isinstance(criteria, Mapping) and str(ans["choice"]) not in criteria:
                raise ValueError(f"{q_id}: choice {ans['choice']!r} not in criteria")
            raw_conf = ans["confidence"]
            if not isinstance(raw_conf, (int, float)) or isinstance(raw_conf, bool):
                raise ValueError(f"{q_id}: choice confidence must be numeric")
            conf = float(raw_conf)
            if not (0.0 <= conf <= 1.0):
                raise ValueError(f"{q_id}: choice confidence out of [0, 1]: {conf}")
            parsed[q_id] = {
                "type": "choice",
                "choice": str(ans["choice"]),
                "confidence": conf,
                "needs_human_review": conf < CHOICE_HUMAN_REVIEW_FLOOR,
                "probabilities": ans.get("probabilities") or {},
            }
        elif q_type == "score":
            if not isinstance(ans, dict) or "score" not in ans:
                raise ValueError(f"{q_id}: score answer must include score")
            raw_val = ans["score"]
            if not isinstance(raw_val, (int, float)) or isinstance(raw_val, bool):
                raise ValueError(f"{q_id}: score must be numeric")
            raw_score = float(raw_val)
            levels = q_spec.get("criteria") or []
            max_idx = max(len(levels) - 1, 0)
            if not math.isfinite(raw_score) or not (0.0 <= raw_score <= max_idx):
                raise ValueError(
                    f"{q_id}: score out of [0, {max_idx}]: {raw_score}"
                )
            discrete_rank = min(max(int(raw_score + 0.5), 0), max_idx)
            parsed[q_id] = {
                "type": "score",
                "score": raw_score,
                "discrete_rank": discrete_rank,
                "legend": ans.get("legend") or levels,
                "probabilities": ans.get("probabilities") or [],
            }
    return parsed


# ---------------------------------------------------------------------------
# 2. Rubric & Reference Evaluator (Script-First + Jev Thresholds)
# ---------------------------------------------------------------------------

def validate_rubric_markdown(text: str) -> ValidationResult:
    errors: list[str] = []
    lowered = text.lower()
    for term in ("complete", "incomplete", "missing", "0.80", "0.50"):
        if term not in lowered:
            errors.append(f"rubric markdown missing required term/threshold: {term!r}")
    if not any(kw in lowered for kw in ("script-first", "deterministic", "mechanical")):
        errors.append("rubric markdown must declare script-first / deterministic pre-gate priority")
    if "score" not in lowered or not any(kw in lowered for kw in ("rank", "threshold")):
        errors.append("rubric markdown must define score rank/threshold semantics")
    if not any(kw in lowered for kw in ("phase n", "deferred", "open scope", "scope decision")):
        errors.append("rubric markdown must document M1 open-scope / Phase N decisions")
    return ValidationResult(len(errors) == 0, errors)


def validate_scope_decisions(doc: Any) -> ValidationResult:
    errors: list[str] = []
    if not isinstance(doc, dict):
        return ValidationResult(False, ["scope-decisions-jev.json must be an object"])
    decisions = doc.get("decisions")
    if not isinstance(decisions, list) or not decisions:
        return ValidationResult(False, ["scope-decisions-jev.json must contain non-empty decisions list"])

    seen_ids: set[str] = set()
    for idx, dec in enumerate(decisions):
        if not isinstance(dec, dict):
            errors.append(f"decisions[{idx}] must be an object")
            continue

        is_batched = ("request" in dec or "response" in dec) and (
            "jev_request" not in dec and "jev_response" not in dec and "decision_id" not in dec
        )
        if is_batched:
            if dec.get("status") != "ok":
                errors.append(
                    f"decisions[{idx}]: batched decision status must be 'ok' (got {dec.get('status')!r})"
                )
            req = dec.get("request")
            req_res = validate_jev_request(req)
            if not req_res.ok:
                errors.extend(f"decisions[{idx}]: request {e}" for e in req_res.errors)

            resp = dec.get("response")
            if not isinstance(resp, dict):
                errors.append(f"decisions[{idx}]: missing response")
            else:
                for key in ("id", "model", "answers", "usage"):
                    if (
                        key not in resp
                        or resp[key] is None
                        or (key in ("id", "model") and not str(resp[key]).strip())
                    ):
                        errors.append(f"decisions[{idx}]: response missing {key!r}")
                resp_id = resp.get("id")
                if isinstance(resp_id, str) and resp_id.strip():
                    if resp_id in seen_ids:
                        errors.append(f"duplicate response id: {resp_id}")
                    else:
                        seen_ids.add(resp_id)

                if isinstance(req, Mapping) and isinstance(req.get("questions"), Mapping):
                    try:
                        parsed = parse_jev_primitives(resp, req["questions"])
                    except ValueError as exc:
                        errors.append(f"decisions[{idx}]: invalid response answer: {exc}")
                    else:
                        missing_qs = sorted(set(req["questions"]) - set(parsed))
                        if missing_qs:
                            errors.append(
                                f"decisions[{idx}]: response answers missing questions: {missing_qs}"
                            )
            continue

        dec_id = dec.get("decision_id")
        if not isinstance(dec_id, str) or not dec_id:
            errors.append(f"decisions[{idx}] missing decision_id")
        elif dec_id in seen_ids:
            errors.append(f"duplicate decision_id: {dec_id}")
        else:
            seen_ids.add(dec_id)

        jev_req = dec.get("jev_request")
        req_res = validate_jev_request(jev_req)
        if not req_res.ok:
            errors.extend(f"{dec_id}: jev_request {e}" for e in req_res.errors)

        jev_resp = dec.get("jev_response")
        if not isinstance(jev_resp, dict):
            errors.append(f"{dec_id}: missing jev_response")
        else:
            for key in ("id", "model", "answers", "usage"):
                if key not in jev_resp:
                    errors.append(f"{dec_id}: jev_response missing {key!r}")
            if isinstance(jev_req, Mapping) and isinstance(jev_req.get("questions"), Mapping):
                try:
                    parsed = parse_jev_primitives(jev_resp, jev_req["questions"])
                except ValueError as exc:
                    errors.append(f"{dec_id}: invalid jev_response answer: {exc}")
                else:
                    missing_qs = sorted(set(jev_req["questions"]) - set(parsed))
                    if missing_qs:
                        errors.append(
                            f"{dec_id}: jev_response answers missing questions: {missing_qs}"
                        )
        if not dec.get("ratified_choice") and "noul" not in json.dumps(dec):
            errors.append(f"{dec_id}: missing ratified_choice or noul score")

    leak_res = scan_for_secrets_and_db3(json.dumps(doc))
    if not leak_res.ok:
        errors.extend(leak_res.errors)
    return ValidationResult(len(errors) == 0, errors)


def derive_recommended_action(linear_status: str, notion_status: str) -> str:
    l_gap = linear_status in ("missing", "incomplete")
    n_gap = notion_status in ("missing", "incomplete")
    if l_gap and n_gap:
        return "both"
    if linear_status == "missing":
        return "create_linear"
    if linear_status == "incomplete":
        return "update_linear"
    if notion_status == "missing":
        return "create_notion"
    if notion_status == "incomplete":
        return "update_notion"
    return "none"


def evaluate_requirement_record(
    *,
    req: Mapping[str, Any],
    linear_matches: Sequence[Mapping[str, Any]],
    notion_matches: Sequence[Mapping[str, Any]],
    jev_response: Mapping[str, Any] | None,
    questions: Mapping[str, Any] | None = None,
    phase_n_tracked_complete: bool = True,
) -> dict[str, Any]:
    """Reference opaque-box evaluator implementing script-first + Jev thresholds."""
    gaps: list[str] = []
    needs_human_review = False

    # Mechanical / deterministic pre-gates (never overridden by Jev)
    l_mech_ok = len(linear_matches) > 0
    if not l_mech_ok:
        linear_status = "missing"
        gaps.append("linear: no matching issue found")
    else:
        l_valid_fields = all(
            bool(m.get("source_path")) and bool(m.get("acceptance_criteria"))
            for m in linear_matches
        )
        linear_status = "complete" if l_valid_fields else "incomplete"
        if not l_valid_fields:
            gaps.append("linear: matched issue missing source_path or acceptance_criteria")

    n_mech_ok = len(notion_matches) > 0
    if not n_mech_ok:
        notion_status = "missing"
        gaps.append("notion: no matching record found")
    else:
        n_valid_fields = all(
            bool(m.get("has_icon"))
            and m.get("project_id") == NOTION_PROJECT_AEGIS_ID
            and m.get("status") in NOTION_VALID_TASK_STATUSES
            and LINEAR_URL_PREFIX in str(m.get("note", ""))
            for m in notion_matches
        )
        notion_status = "complete" if n_valid_fields else "incomplete"
        if not n_valid_fields:
            gaps.append("notion: matched record violates Tasks DB contract")

    # Deferred Phase N explicit tracking rule
    lifecycle = req.get("lifecycle_state")
    if lifecycle == "deferred_phase_n" and not phase_n_tracked_complete:
        if linear_status == "complete":
            linear_status = "incomplete"
        if notion_status == "complete":
            notion_status = "incomplete"
        gaps.append("phase_n: deferred item treated as incomplete under policy")

    # Jev evaluation (only refines/confirms passing mechanical checks or flags human review)
    jev_parsed: dict[str, Any] = {}
    if jev_response and questions:
        jev_parsed = parse_jev_primitives(jev_response, questions)
        for q_id, ans in jev_parsed.items():
            if ans["type"] == "noul" and not ans["passes"]:
                if linear_status == "complete":
                    linear_status = "incomplete"
                if notion_status == "complete":
                    notion_status = "incomplete"
                gaps.append(f"jev:{q_id} noul {ans['noul']:.3f} < {NOUL_PASS_THRESHOLD}")
            elif ans["type"] == "choice":
                if ans["needs_human_review"]:
                    needs_human_review = True
                    gaps.append(
                        f"jev:{q_id} choice confidence {ans['confidence']:.3f} < {CHOICE_HUMAN_REVIEW_FLOOR}"
                    )
                if ans["choice"] in ("incomplete", "missing"):
                    if linear_status == "complete":
                        linear_status = "incomplete"
                    if notion_status == "complete":
                        notion_status = "incomplete"
                    gaps.append(f"jev:{q_id} classified {ans['choice']}")

    return {
        "req_id": req["req_id"],
        "source_file": req["source_file"],
        "linear_matches": [m.get("id", "") for m in linear_matches],
        "notion_matches": [m.get("id", "") for m in notion_matches],
        "linear_status": linear_status,
        "notion_status": notion_status,
        "deterministic_checks": {
            "linear_matched": l_mech_ok,
            "notion_matched": n_mech_ok,
        },
        "jev_answers": jev_response.get("answers", {}) if jev_response else {},
        "jev_decision_id": jev_response.get("id") if jev_response else None,
        "needs_human_review": needs_human_review,
        "gaps": gaps,
        "recommended_action": derive_recommended_action(linear_status, notion_status),
    }


# ---------------------------------------------------------------------------
# 3. Requirements Corpus & Catalog Validators
# ---------------------------------------------------------------------------

def is_excluded_path(rel_path: str) -> bool:
    norm = rel_path.replace("\\", "/").lstrip("./")
    if any(norm.startswith(prefix) for prefix in EXCLUDED_PATH_PREFIXES):
        return True
    fname = norm.rsplit("/", 1)[-1].lower()
    return any(sub in fname for sub in EXCLUDED_FILENAME_SUBSTRINGS)


def validate_requirements_catalog(
    catalog: Any,
    *,
    repo_root: Path | None = None,
    require_full_corpus: bool = True,
    expected_in_scope_files: Sequence[str] | None = None,
) -> ValidationResult:
    errors: list[str] = []
    if not isinstance(catalog, dict):
        return ValidationResult(False, ["requirements-catalog.json must be a JSON object"])

    if not _is_iso8601(catalog.get("generated_at")):
        errors.append("generated_at must be valid ISO-8601 timestamp")

    source_files = catalog.get("source_files")
    if not isinstance(source_files, list) or not source_files:
        errors.append("source_files must be a non-empty list")
        source_files = []

    in_scope_paths: set[str] = set()
    for idx, sf in enumerate(source_files):
        if not isinstance(sf, dict):
            errors.append(f"source_files[{idx}] must be an object")
            continue
        p = str(sf.get("path", "")).replace("\\", "/")
        cat = sf.get("category")
        in_scope = sf.get("in_scope")
        if not p:
            errors.append(f"source_files[{idx}] missing path")
            continue
        if is_excluded_path(p) and in_scope is True:
            errors.append(f"excluded path marked in_scope=True: {p}")
        if cat not in VALID_CATEGORIES:
            errors.append(f"source_files[{idx}] invalid category {cat!r}")
        if in_scope is True:
            in_scope_paths.add(p)

    requirements = catalog.get("requirements")
    if not isinstance(requirements, list) or not requirements:
        errors.append("requirements must be a non-empty list")
        requirements = []

    seen_req_ids: set[str] = set()
    covered_sources: set[str] = set()
    line_range_re = re.compile(r"^(\d+)-(\d+)$")

    for idx, req in enumerate(requirements):
        if not isinstance(req, dict):
            errors.append(f"requirements[{idx}] must be an object")
            continue
        req_id = req.get("req_id")
        if not isinstance(req_id, str) or not req_id.strip():
            errors.append(f"requirements[{idx}] missing req_id")
        elif req_id in seen_req_ids:
            errors.append(f"duplicate req_id detected: {req_id}")
        else:
            seen_req_ids.add(req_id)

        for field_name in ("title", "source_file", "summary"):
            if not isinstance(req.get(field_name), str) or not req[field_name].strip():
                errors.append(f"{req_id or idx}: missing {field_name}")

        src = str(req.get("source_file", "")).replace("\\", "/")
        if is_excluded_path(src):
            errors.append(f"{req_id}: excluded source_file leaked into requirements: {src}")
        covered_sources.add(src)

        if req.get("domain_project") not in VALID_DOMAIN_PROJECTS:
            errors.append(f"{req_id}: invalid domain_project {req.get('domain_project')!r}")
        if req.get("lifecycle_state") not in VALID_LIFECYCLE_STATES:
            errors.append(f"{req_id}: invalid lifecycle_state {req.get('lifecycle_state')!r}")

        ac = req.get("acceptance_criteria")
        if not isinstance(ac, list) or not ac or not all(isinstance(x, str) and x.strip() for x in ac):
            errors.append(f"{req_id}: acceptance_criteria must be a non-empty list of strings")

        src_lines = str(req.get("source_lines", ""))
        m = line_range_re.match(src_lines)
        is_notion_only = src.startswith("notion://") or NOTION_ONLY_PAGE_ID in src or "Milsim Comparative Research" in src
        if not is_notion_only:
            if not m:
                errors.append(f"{req_id}: invalid source_lines format {src_lines!r} (expected 'start-end')")
            else:
                start, end = int(m.group(1)), int(m.group(2))
                if start < 1 or end < start:
                    errors.append(f"{req_id}: invalid source_lines bounds {start}-{end}")
                elif repo_root is not None:
                    full_path = repo_root / src
                    if not full_path.is_file():
                        errors.append(f"{req_id}: source_file does not exist on disk: {src}")
                    else:
                        total_lines = len(full_path.read_text(encoding="utf-8").splitlines())
                        if end > total_lines:
                            errors.append(
                                f"{req_id}: source_lines {start}-{end} exceeds EOF ({total_lines} lines) in {src}"
                            )

    if require_full_corpus:
        expected = set(expected_in_scope_files or IN_SCOPE_GIT_FILES)
        missing_from_sf = sorted(expected - in_scope_paths)
        if missing_from_sf:
            errors.append(f"in-scope files missing from source_files: {missing_from_sf}")
        uncovered = sorted(expected - covered_sources)
        if uncovered:
            errors.append(f"in-scope files with 0 extracted requirements: {uncovered}")

    return ValidationResult(len(errors) == 0, errors)


# ---------------------------------------------------------------------------
# 4. Gap Matrix & Final Acceptance Validators
# ---------------------------------------------------------------------------

def validate_gap_matrix(
    matrix: Any,
    *,
    is_final: bool = False,
    baseline_matrix: Mapping[str, Any] | None = None,
    catalog: Mapping[str, Any] | None = None,
    expected_req_ids: Iterable[str] | None = None,
    expected_in_scope_files: Sequence[str] | None = None,
    allowed_owner_hold_req_ids: Iterable[str] = (),
) -> ValidationResult:
    errors: list[str] = []
    if not isinstance(matrix, dict):
        return ValidationResult(False, ["gap matrix must be a JSON object"])

    if not _is_iso8601(matrix.get("evaluated_at")):
        errors.append("evaluated_at must be valid ISO-8601 timestamp")
    if not matrix.get("rubric_version"):
        errors.append("rubric_version missing")
    if not matrix.get("jev_model"):
        errors.append("jev_model missing")

    summary = matrix.get("summary")
    evaluations = matrix.get("evaluations")
    if not isinstance(summary, dict) or not isinstance(evaluations, list) or not evaluations:
        return ValidationResult(False, ["summary object and non-empty evaluations list required"])

    counts = {
        "linear": {"complete": 0, "incomplete": 0, "missing": 0},
        "notion": {"complete": 0, "incomplete": 0, "missing": 0},
    }
    sources_seen: set[str] = set()
    eval_req_ids: set[str] = set()
    baseline_dec_ids: set[str] = set()
    baseline_req_ids: set[str] = set()
    if baseline_matrix:
        for ev in baseline_matrix.get("evaluations", []):
            if isinstance(ev, dict):
                if ev.get("req_id"):
                    baseline_req_ids.add(str(ev["req_id"]))
                dec_id = ev.get("jev_decision_id") or (ev.get("jev_answers") or {}).get("decision_id")
                if dec_id:
                    baseline_dec_ids.add(str(dec_id))

    owner_held = set(allowed_owner_hold_req_ids)
    for idx, ev in enumerate(evaluations):
        if not isinstance(ev, dict):
            errors.append(f"evaluations[{idx}] must be an object")
            continue
        req_id = ev.get("req_id", f"idx:{idx}")
        if ev.get("req_id"):
            eval_req_ids.add(str(ev["req_id"]))
        src = str(ev.get("source_file", "")).replace("\\", "/")
        if is_excluded_path(src):
            errors.append(f"{req_id}: excluded path {src} leaked into gap matrix")
        sources_seen.add(src)

        l_st = ev.get("linear_status")
        n_st = ev.get("notion_status")
        if l_st not in VALID_STATUSES:
            errors.append(f"{req_id}: invalid linear_status {l_st!r}")
        else:
            counts["linear"][l_st] += 1
        if n_st not in VALID_STATUSES:
            errors.append(f"{req_id}: invalid notion_status {n_st!r}")
        else:
            counts["notion"][n_st] += 1

        rec_act = ev.get("recommended_action")
        if rec_act not in VALID_RECOMMENDED_ACTIONS:
            errors.append(f"{req_id}: invalid recommended_action {rec_act!r}")
        elif l_st in VALID_STATUSES and n_st in VALID_STATUSES:
            expected_act = derive_recommended_action(l_st, n_st)
            if rec_act != expected_act:
                errors.append(
                    f"{req_id}: recommended_action {rec_act!r} != derived {expected_act!r}"
                )

        if is_final:
            is_ev_owner_held = str(req_id) in owner_held
            is_shipped_verified = ev.get("sub_status") == "shipped_verified"
            l_matches = ev.get("linear_matches")
            n_matches = ev.get("notion_matches")
            if l_st in ("complete", "incomplete") and not is_shipped_verified and not is_ev_owner_held:
                if not isinstance(l_matches, list) or not any(bool(str(m).strip()) for m in l_matches):
                    errors.append(
                        f"{req_id}: final matrix linear_status={l_st!r} requires non-empty linear_matches"
                    )
            if n_st in ("complete", "incomplete") and not is_ev_owner_held:
                if not isinstance(n_matches, list) or not any(bool(str(m).strip()) for m in n_matches):
                    errors.append(
                        f"{req_id}: final matrix notion_status={n_st!r} requires non-empty notion_matches"
                    )
            if req_id not in owner_held:
                if l_st in ("missing", "incomplete"):
                    errors.append(f"{req_id}: final matrix still has linear_status={l_st!r}")
                if n_st in ("missing", "incomplete"):
                    errors.append(f"{req_id}: final matrix still has notion_status={n_st!r}")
            dec_id = ev.get("jev_decision_id") or (ev.get("jev_answers") or {}).get("decision_id")
            if not dec_id:
                errors.append(f"{req_id}: final matrix evaluation missing fresh jev_decision_id")
            elif str(dec_id) in baseline_dec_ids:
                errors.append(f"{req_id}: final matrix reused baseline decision ID {dec_id!r}")

    if is_final and baseline_req_ids:
        missing_base_reqs = sorted(baseline_req_ids - eval_req_ids)
        if missing_base_reqs:
            errors.append(f"final matrix missing baseline req_ids: {missing_base_reqs}")

    if catalog is not None:
        cat_reqs = {
            str(r["req_id"])
            for r in catalog.get("requirements", [])
            if isinstance(r, Mapping) and r.get("req_id")
        }
        missing_cat_reqs = sorted(cat_reqs - eval_req_ids)
        if missing_cat_reqs:
            errors.append(f"gap matrix missing catalog req_ids: {missing_cat_reqs}")
        cat_files = {
            str(sf["path"]).replace("\\", "/")
            for sf in catalog.get("source_files", [])
            if isinstance(sf, Mapping) and sf.get("in_scope") is True and sf.get("path")
        }
        missing_cat_files = sorted(cat_files - sources_seen)
        if missing_cat_files:
            errors.append(f"gap matrix missing catalog in-scope source_files: {missing_cat_files}")

    if expected_req_ids is not None:
        missing_exp_reqs = sorted({str(r) for r in expected_req_ids} - eval_req_ids)
        if missing_exp_reqs:
            errors.append(f"gap matrix missing expected req_ids: {missing_exp_reqs}")

    sfc = summary.get("source_files_covered")
    sfc_is_int = isinstance(sfc, int) and not isinstance(sfc, bool)
    if (is_final or catalog is not None) and not sfc_is_int:
        errors.append("summary.source_files_covered must be an integer when is_final=True or catalog is provided")

    if expected_in_scope_files is not None or (is_final and catalog is not None):
        target_files = set(expected_in_scope_files if expected_in_scope_files is not None else IN_SCOPE_GIT_FILES)
        missing_scope_files = sorted(target_files - sources_seen)
        if missing_scope_files:
            errors.append(f"gap matrix missing in-scope files: {missing_scope_files}")
        if sfc_is_int and sfc < len(target_files):
            errors.append(
                f"summary.source_files_covered ({sfc}) < required in-scope files ({len(target_files)})"
            )

    if summary.get("total_requirements") != len(evaluations):
        errors.append(
            f"summary.total_requirements ({summary.get('total_requirements')}) != len(evaluations) ({len(evaluations)})"
        )
    for side in ("linear", "notion"):
        side_sum = summary.get(side)
        if side_sum != counts[side]:
            errors.append(f"summary.{side} {side_sum} != computed counts {counts[side]}")

    if sfc_is_int and sfc < len(sources_seen):
        errors.append(
            f"summary.source_files_covered ({sfc}) < distinct sources ({len(sources_seen)})"
        )

    return ValidationResult(len(errors) == 0, errors)


def validate_markdown_parity(matrix: Mapping[str, Any], markdown_text: str) -> ValidationResult:
    errors: list[str] = []
    summary = matrix.get("summary", {})
    total = str(summary.get("total_requirements", ""))
    if total and total not in markdown_text:
        errors.append(f"Markdown report missing total_requirements={total}")
    if str(matrix.get("rubric_version", "")) not in markdown_text:
        errors.append("Markdown report missing rubric_version")
    if str(matrix.get("jev_model", "")) not in markdown_text:
        errors.append("Markdown report missing jev_model")
    for side in ("linear", "notion"):
        side_counts = summary.get(side, {})
        for k, v in side_counts.items():
            if str(v) not in markdown_text:
                errors.append(f"Markdown report missing {side}.{k}={v}")
    return ValidationResult(len(errors) == 0, errors)


# ---------------------------------------------------------------------------
# 5. Linear & Notion Governance Validators
# ---------------------------------------------------------------------------

def is_cpq_linear_issue(issue: Mapping[str, Any]) -> bool:
    proj = str(issue.get("project") or issue.get("project_code") or "")
    if proj in LINEAR_EXCLUDED_PROJECT_CODES:
        return True
    labels = {str(lbl) for lbl in (issue.get("labels") or [])}
    if labels & {"Open-CRM-CPQ", "renewals-amendments"}:
        return True
    if any(lbl.startswith("CPQ Phase") for lbl in labels):
        return True
    return False


def validate_linear_issue_payload(
    issue: Mapping[str, Any],
    *,
    is_new_gap: bool = False,
    prior_issue: Mapping[str, Any] | None = None,
    repo_root: Path | None = None,
) -> ValidationResult:
    errors: list[str] = []
    if is_cpq_linear_issue(issue):
        errors.append(f"{issue.get('id', '?')}: OPEN-CRM-CPQ / P-DRG-6 issue must be excluded")

    labels = {str(lbl) for lbl in (issue.get("labels") or [])}
    bad_labels = labels & LINEAR_FORBIDDEN_LABELS
    if bad_labels or any(lbl.startswith("CPQ Phase") for lbl in labels):
        errors.append(f"{issue.get('id', '?')}: forbidden/retired labels present: {sorted(labels)}")

    state = str(issue.get("state") or issue.get("status") or "")
    state_id = str(issue.get("state_id") or "")
    if is_new_gap:
        if state != "Backlog" and state_id != LINEAR_BACKLOG_STATE_ID:
            errors.append(f"{issue.get('id', '?')}: new gap issue must enter as Backlog (got {state!r})")
    if state in LINEAR_FORBIDDEN_REMEDIATION_STATES and not issue.get("merged_to_main"):
        errors.append(f"{issue.get('id', '?')}: remediation cannot invent status {state!r}")

    desc = str(issue.get("description") or "")
    if is_new_gap and re.search(r"(?im)^\s*Surface\s*:", desc):
        errors.append(f"{issue.get('id', '?')}: new Backlog issue must NOT contain a 'Surface:' line")

    if len(desc.encode("utf-8")) > 16 * 1024:
        errors.append(f"{issue.get('id', '?')}: Linear issue must be pointer+state, not full spec prose dump")

    # Git source path validation
    src_matches = re.findall(r"(?im)(?:Git source of truth|Source)\s*:\s*`?([^\s`\n]+)`?", desc)
    if issue.get("source_path"):
        src_matches.append(str(issue["source_path"]))
    if not src_matches and not issue.get("is_notion_only"):
        errors.append(f"{issue.get('id', '?')}: missing Source: repo path citation")
    else:
        for rel in src_matches:
            if rel.startswith(("http://", "https://", "notion://")):
                continue
            if is_excluded_path(rel):
                errors.append(f"{issue.get('id', '?')}: cited excluded Source path: {rel}")
            elif repo_root is not None and not (repo_root / rel).exists():
                errors.append(f"{issue.get('id', '?')}: cited Source path does not exist on disk: {rel}")

    if prior_issue is not None:
        prior_labels = {str(lbl) for lbl in (prior_issue.get("labels") or [])}
        dropped_protected = (prior_labels & LINEAR_PROTECTED_LABELS) - labels
        if dropped_protected:
            errors.append(
                f"{issue.get('id', '?')}: protected labels removed: {sorted(dropped_protected)}"
            )

    return ValidationResult(len(errors) == 0, errors)


def validate_notion_task_row(row: Mapping[str, Any]) -> ValidationResult:
    errors: list[str] = []
    rid = row.get("id", "?")
    if not row.get("icon"):
        errors.append(f"{rid}: Notion Tasks row missing page icon")
    proj = row.get("project_id") or row.get("Project")
    if proj != NOTION_PROJECT_AEGIS_ID and proj != "Project Aegis":
        errors.append(f"{rid}: Notion Tasks row Project must be Project Aegis (got {proj!r})")
    status = row.get("status") or row.get("Status")
    if status not in NOTION_VALID_TASK_STATUSES:
        errors.append(f"{rid}: invalid Notion Status {status!r}")
    note = str(row.get("note") or row.get("Note") or "")
    if LINEAR_URL_PREFIX not in note:
        errors.append(f"{rid}: Notion Note missing Linear DRG-* URL")
    return ValidationResult(len(errors) == 0, errors)


def validate_cross_links(
    linear_issues: Sequence[Mapping[str, Any]],
    notion_rows: Sequence[Mapping[str, Any]],
    *,
    known_notion_rows: Sequence[Mapping[str, Any]] = (),
) -> ValidationResult:
    errors: list[str] = []
    linear_by_id = {str(i.get("id")): i for i in linear_issues if i.get("id")}
    notion_by_id: dict[str, Mapping[str, Any]] = {}
    for n in (*notion_rows, *known_notion_rows):
        if n.get("id"):
            raw_nid = str(n["id"]).lower()
            notion_by_id[raw_nid] = n
            notion_by_id[raw_nid.replace("-", "")] = n

    for n in notion_rows:
        note = str(n.get("note") or n.get("Note") or "")
        m = re.search(r"DRG-(\d+)", note)
        if not m:
            errors.append(f"Notion {n.get('id')}: missing DRG-* reference in Note")
            continue
        drg_id = f"DRG-{m.group(1)}"
        li = linear_by_id.get(drg_id)
        if not li:
            errors.append(f"Notion {n.get('id')} points to missing Linear {drg_id}")
            continue
        li_blob_lower = json.dumps(li).lower()
        nid = str(n.get("id", ""))
        nid_lower = nid.lower()
        nid_nodash = nid_lower.replace("-", "")
        nurl_lower = str(n.get("url", "")).lower()
        has_ref = (
            (bool(nid_lower) and nid_lower in li_blob_lower)
            or (len(nid_nodash) == 32 and nid_nodash in li_blob_lower.replace("-", ""))
            or (bool(nurl_lower) and nurl_lower in li_blob_lower)
        )
        if not has_ref:
            errors.append(f"One-way link: Notion {nid} -> {drg_id}, but {drg_id} lacks Notion link")

    for li in linear_issues:
        li_blob = json.dumps(li)
        for raw_nid in re.findall(r"notion\.so/[^\s\"']*?([0-9a-fA-F]{32})", li_blob):
            nid = raw_nid.lower()
            if nid not in notion_by_id:
                errors.append(f"One-way link: Linear {li.get('id')} references missing Notion {nid}")

    return ValidationResult(len(errors) == 0, errors)


def validate_remediation_log(
    log: Any,
    baseline_matrix: Mapping[str, Any],
    *,
    allow_frozen_mirror_writes: bool = False,
) -> ValidationResult:
    errors: list[str] = []
    if not isinstance(log, dict):
        return ValidationResult(False, ["remediation-log.json must be a JSON object"])
    if not _is_iso8601(log.get("completed_at")):
        errors.append("completed_at must be a valid ISO-8601 timestamp")
    ops = log.get("operations")
    if not isinstance(ops, list):
        return ValidationResult(False, ["remediation-log.json missing operations list"])

    covered_reqs: set[str] = set()
    for idx, op in enumerate(ops):
        if not isinstance(op, dict):
            errors.append(f"operations[{idx}] must be an object")
            continue
        target = op.get("target")
        action = str(op.get("action", "")).lower()
        req_id = op.get("req_id")
        if req_id:
            covered_reqs.add(str(req_id))
        for field_name in ("before_state", "after_state", "url"):
            if field_name not in op:
                errors.append(f"operations[{idx}] ({req_id}) missing {field_name}")
        if not (op.get("issue_id") or op.get("page_id")):
            errors.append(f"operations[{idx}] ({req_id}) missing issue_id or page_id")

        if target == "linear":
            after = op.get("after_state") or {}
            if isinstance(after, Mapping) and str(after.get("state", "")) in LINEAR_FORBIDDEN_REMEDIATION_STATES:
                if not after.get("merged_to_main"):
                    errors.append(f"operations[{idx}] ({req_id}): invented forbidden Linear state {after.get('state')}")
        elif target == "notion":
            if action == "delete" or op.get("deleted"):
                errors.append(f"operations[{idx}] ({req_id}): Notion delete is strictly forbidden")
            db = str(op.get("database_id") or op.get("parent_id") or "")
            if db in (NOTION_FROZEN_MIRROR_PAGE_ID, NOTION_REQUIREMENTS_DB_ID) and not allow_frozen_mirror_writes:
                errors.append(f"operations[{idx}] ({req_id}): unauthorized mutation of Frozen Git mirror")

    for ev in baseline_matrix.get("evaluations", []):
        if ev.get("linear_status") in ("missing", "incomplete") or ev.get("notion_status") in ("missing", "incomplete"):
            rid = str(ev.get("req_id"))
            if rid not in covered_reqs:
                errors.append(f"baseline gap {rid} has no remediation-log entry")

    return ValidationResult(len(errors) == 0, errors)


# ---------------------------------------------------------------------------
# 6. Secret & .db3 Leakage Scanner + Baltic Hash Dynamic Reader
# ---------------------------------------------------------------------------

def scan_for_secrets_and_db3(
    text: str,
    *,
    extra_forbidden_values: Iterable[str] = (),
    mode: str = "evidence",
) -> ValidationResult:
    if mode not in ("evidence", "doc", "code", "test_code"):
        raise ValueError(f"Unsupported scan mode: {mode!r}")

    errors: list[str] = []
    if mode == "evidence":
        patterns = SECRET_PATTERNS
    elif mode == "doc":
        patterns = tuple(
            ("db3_reference", DB3_FILENAME_PATTERN) if name == "db3_reference" else (name, pat)
            for name, pat in SECRET_PATTERNS
        )
    elif mode == "code":
        patterns = tuple(
            (name, pat) for name, pat in SECRET_PATTERNS if name in _CODE_LITERAL_SECRET_NAMES
        )
    else:  # mode == "test_code"
        patterns = ()

    for name, pattern in patterns:
        if pattern.search(text):
            errors.append(f"forbidden {name} pattern detected")
    for idx, secret_val in enumerate(extra_forbidden_values):
        if secret_val and len(secret_val) >= 8 and secret_val in text:
            errors.append(f"raw secret value #{idx} leaked in artifact")
    return ValidationResult(len(errors) == 0, errors)


def read_baltic_v2_hash(repo_root: Path) -> str:
    floor_path = repo_root / "production" / "test-floor.json"
    data = json.loads(floor_path.read_text(encoding="utf-8"))
    val = str(data.get("balticV2Hash", "")).strip()
    if not val:
        raise ValueError("production/test-floor.json missing balticV2Hash")
    return val


def select_stratified_evaluations(
    evaluations: Sequence[Mapping[str, Any]],
    catalog: Mapping[str, Any],
    remediation_log: Mapping[str, Any] | None = None,
    *,
    max_samples: int = 10,
) -> list[Mapping[str, Any]]:
    """Deterministically select a stratified sample across category, project, lifecycle, and remediated items."""
    if not evaluations:
        return []
    req_by_id = {
        str(r["req_id"]): r
        for r in catalog.get("requirements", [])
        if isinstance(r, Mapping) and r.get("req_id")
    }
    sf_cat_by_path = {
        str(sf["path"]).replace("\\", "/"): str(sf.get("category", ""))
        for sf in catalog.get("source_files", [])
        if isinstance(sf, Mapping) and sf.get("path")
    }
    remediated_reqs = {
        str(op["req_id"])
        for op in (remediation_log or {}).get("operations", [])
        if isinstance(op, Mapping) and op.get("req_id")
    }

    selected: list[Mapping[str, Any]] = []
    seen_req_ids: set[str] = set()

    def _add(ev: Mapping[str, Any]) -> None:
        rid = str(ev.get("req_id", ""))
        if rid and rid not in seen_req_ids:
            seen_req_ids.add(rid)
            selected.append(ev)

    def _ev_meta(ev: Mapping[str, Any]) -> tuple[str, str, str, bool]:
        rid = str(ev.get("req_id", ""))
        req = req_by_id.get(rid, {})
        src = str(ev.get("source_file") or req.get("source_file") or "").replace("\\", "/")
        cat = sf_cat_by_path.get(src, "")
        if not cat:
            if src.startswith("notion://") or NOTION_ONLY_PAGE_ID in src:
                cat = "NotionOnly"
            elif src.startswith("Game-Requirements/requirements/"):
                cat = "A"
            elif src.startswith("Game-Requirements/drafts/"):
                cat = "B"
            else:
                cat = "C"
        proj = str(req.get("domain_project", ""))
        life = str(req.get("lifecycle_state", ""))
        return cat, proj, life, rid in remediated_reqs

    for cat in VALID_CATEGORIES:
        for ev in evaluations:
            if _ev_meta(ev)[0] == cat:
                _add(ev)
                break

    for proj in VALID_DOMAIN_PROJECTS:
        if any(_ev_meta(ev)[1] == proj for ev in selected):
            continue
        for ev in evaluations:
            if _ev_meta(ev)[1] == proj:
                _add(ev)
                break

    for life in VALID_LIFECYCLE_STATES:
        if any(_ev_meta(ev)[2] == life for ev in selected):
            continue
        for ev in evaluations:
            if _ev_meta(ev)[2] == life:
                _add(ev)
                break

    rem_added = 0
    for ev in evaluations:
        if len(selected) >= max_samples or rem_added >= 2:
            break
        if _ev_meta(ev)[3]:
            before = len(selected)
            _add(ev)
            if len(selected) > before:
                rem_added += 1

    for ev in evaluations:
        if len(selected) >= max_samples:
            break
        _add(ev)

    return selected[:max_samples]


_INDEP_XML_TAG_RE = re.compile(
    r"<(issue|pull-request)\b([^>]*?)(?:/>|>(.*?)</\1>)",
    re.IGNORECASE | re.DOTALL,
)
_INDEP_XML_LABEL_RE = re.compile(r'\blabel="([^"]+)"')
_INDEP_HTTP_MD_LINK_RE = re.compile(
    r"\[([^\]]+\.md)\]\(https?://[^)\s]+\.md\)", re.IGNORECASE
)
_INDEP_BARE_HTTP_MD_RE = re.compile(
    r"https?://([^\s)\]>\"']+\.md)\b", re.IGNORECASE
)
_INDEP_SCHEMA_BLOCK_RE = re.compile(
    r"<parent-data-source-schema\b[^>]*>.*?</parent-data-source-schema>",
    re.IGNORECASE | re.DOTALL,
)
_INDEP_CONTENT_BLOCK_RE = re.compile(
    r"<content>(.*?)</content>", re.IGNORECASE | re.DOTALL
)


def _indep_strip_xml(text: str) -> str:
    if not text or "<" not in text:
        return text

    def _repl(m: re.Match[str]) -> str:
        attrs = m.group(2) or ""
        lbl = _INDEP_XML_LABEL_RE.search(attrs)
        if lbl:
            return f"[{lbl.group(1)}]"
        inner = (m.group(3) or "").strip()
        return f"[{inner}]" if inner else ""

    stripped = _INDEP_XML_TAG_RE.sub(_repl, text) if "<" in text else text
    return stripped.replace("\\[", "[").replace("\\]", "]")


def _indep_clean_notion_text(text: Any) -> str:
    raw = str(text or "").strip()
    if not raw:
        return ""
    if "<blank-page>" in raw and "<content>" not in raw:
        return ""
    cm = _INDEP_CONTENT_BLOCK_RE.search(raw)
    if cm:
        raw = cm.group(1).strip()
    else:
        raw = _INDEP_SCHEMA_BLOCK_RE.sub("", raw)
        if raw.startswith("Here is the result of") and "<page " in raw:
            raw = re.sub(
                r"</?(?:page|ancestor-path|ancestor-\d+-\w+|parent-data-source|properties|iconMetadata|blank-page)[^>]*>",
                " ",
                raw,
            )
    raw = re.sub(r"<br\s*/?>", "\n", raw, flags=re.IGNORECASE)
    raw = re.sub(r"</?code>", "`", raw, flags=re.IGNORECASE)
    raw = re.sub(r"</?b>", "", raw, flags=re.IGNORECASE)
    raw = _INDEP_HTTP_MD_LINK_RE.sub(r"`\1`", raw)
    raw = _INDEP_BARE_HTTP_MD_RE.sub(r"`\1`", raw)
    return raw.strip()


def _indep_targeted_slice(text: str, req_id: str, source_file: str, max_chars: int) -> str:
    raw = text.strip()
    multi_section = len(re.findall(r"(?m)^#{2,3}\s+", raw)) >= 3
    if not raw or (len(raw) <= max_chars and not (req_id and multi_section)):
        return raw
    lines = raw.splitlines()
    meta_lines: list[str] = []
    header_lines: list[str] = []
    in_meta = True
    for ln in lines[:16]:
        st = ln.strip()
        if st == "---" or st.startswith("## ") or st.startswith("### "):
            break
        if in_meta and (
            st.lower().startswith("acceptance criteria")
            or st.startswith("- [")
            or st.startswith("* ")
        ):
            in_meta = False
        if in_meta:
            meta_lines.append(ln)
        header_lines.append(ln)
    meta_header = "\n".join(meta_lines).strip()
    header = "\n".join(header_lines).strip()

    target_slice = ""
    if req_id:
        esc = re.escape(req_id)
        heading_re = re.compile(
            rf"(?m)^#{{2,4}}\s*.*?\b{esc}\b",
            re.IGNORECASE,
        )
        table_re = re.compile(
            rf"(?m)^\|\s*(?:\*\*)?{esc}(?:\*\*)?\s*\|[^\n]*",
            re.IGNORECASE,
        )
        bullet_re = re.compile(
            rf"(?m)^[-*]\s*(?:\[[ xX]\]\s*)?(?:\*\*|\[|`)?{esc}\b",
            re.IGNORECASE,
        )
        m = heading_re.search(raw)
        if m:
            start = m.start()
            next_h = re.search(r"(?m)^#{2,3}\s+", raw[start + 4 :])
            end = start + 4 + next_h.start() if next_h else len(raw)
            target_slice = raw[start:end].strip()
        else:
            tm = table_re.search(raw)
            if tm:
                row_text = tm.group(0).strip()
                hdr_m = re.search(r"(?m)^\|\s*Req ID\s*\|[^\n]*\n\|\s*---[^\n]*", raw, re.IGNORECASE)
                target_slice = f"{hdr_m.group(0).strip()}\n{row_text}" if hdr_m else row_text
            else:
                bm = bullet_re.search(raw)
                if bm:
                    start = bm.start()
                    next_h = re.search(r"(?m)^(?:#{2,3}\s+|---)", raw[start + 4 :])
                    end = start + 4 + next_h.start() if next_h else min(len(raw), start + max_chars)
                    target_slice = raw[start:end].strip()
                else:
                    search_offset = len(header) if len(header) < len(raw) else 0
                    idx = raw.lower().find(req_id.lower(), search_offset)
                    if idx < 0 and source_file:
                        bn = source_file.rsplit("/", 1)[-1]
                        if bn:
                            idx = raw.lower().find(bn.lower(), search_offset)
                    if idx >= 0:
                        win_start = max(search_offset, idx - 200)
                        win_end = min(len(raw), idx + max_chars - 250)
                        target_slice = raw[win_start:win_end].strip()
    if not target_slice:
        return raw[: max(0, max_chars - 3)] + "..."
    if target_slice in meta_header:
        return meta_header[:max_chars]
    chosen_header = meta_header or header
    h_budget = min(len(chosen_header), max(760, max_chars // 2))
    compact_h = chosen_header[:h_budget] if chosen_header else ""
    combined = f"{compact_h}\n\n[Targeted slice for {req_id}]\n{target_slice}" if compact_h else target_slice
    if len(combined) <= max_chars:
        return combined
    return combined[: max(0, max_chars - 3)] + "..."


def _indep_clean_links(links: Any) -> list[str]:
    out: list[str] = []
    seen: set[str] = set()
    for x in list(links or []):
        s = str(x or "").strip().rstrip(".,;:)>](")
        if not s or not s.startswith(("http://", "https://")) or _INDEP_BARE_HTTP_MD_RE.fullmatch(s):
            continue
        if s not in seen:
            seen.add(s)
            out.append(s)
    out.sort(key=lambda u: (0 if ("notion.so" in u or "linear.app" in u) else 1))
    return out


def build_independent_jev_state(
    req: Mapping[str, Any],
    linear_records: Sequence[Mapping[str, Any]],
    notion_records: Sequence[Mapping[str, Any]],
    evaluation: Mapping[str, Any] | None = None,
) -> dict[str, Any]:
    """Build non-circular Jev state strictly from requirements catalog + independent Linear/Notion records."""
    req_id = str(req.get("req_id") or "")
    source_file = str(req.get("source_file") or "")
    req_state: dict[str, Any] = {
        "req_id": req.get("req_id"),
        "title": req.get("title"),
        "source_file": req.get("source_file"),
        "source_lines": req.get("source_lines"),
        "domain_project": req.get("domain_project"),
        "lifecycle_state": req.get("lifecycle_state"),
        "summary": str(req.get("summary", ""))[:500],
        "acceptance_criteria": [str(a)[:220] for a in (req.get("acceptance_criteria") or [])[:12]],
    }
    if req.get("shipped_evidence"):
        req_state["shipped_evidence"] = str(req["shipped_evidence"])[:300]
    if req.get("shipped_files"):
        req_state["shipped_files"] = [str(p) for p in list(req.get("shipped_files") or [])[:6]]
    if (evaluation and evaluation.get("sub_status") == "shipped_verified") or (
        req.get("sub_status") == "shipped_verified"
    ):
        req_state["sub_status"] = "shipped_verified"
        ver_ev = (
            evaluation.get("verification_evidence")
            if evaluation and evaluation.get("verification_evidence") is not None
            else req.get("verification_evidence")
        )
        if ver_ev is not None:
            req_state["verification_evidence"] = ver_ev

    lin_list: list[dict[str, Any]] = []
    source_basename = source_file.rsplit("/", 1)[-1] if source_file else ""
    lifecycle_l = str(req.get("lifecycle_state") or "active").strip().lower()
    closed_states = ("done", "canceled", "cancelled", "duplicate")
    open_lin = [
        li for li in linear_records
        if str(li.get("state") or li.get("status") or "").strip().lower() not in closed_states
    ]
    contract_lin = open_lin if open_lin else list(linear_records)

    for li in list(linear_records)[:8]:
        cleaned_desc = _indep_strip_xml(str(li.get("description") or ""))
        cleaned_desc = _INDEP_HTTP_MD_LINK_RE.sub(r"`\1`", cleaned_desc)
        sliced_desc = _indep_targeted_slice(cleaned_desc, req_id, source_file, 3000)
        lin_blob = f"{li.get('title') or ''}\n{cleaned_desc}"
        lin_has_ac = (
            bool(li.get("has_ac"))
            or any(
                tok in cleaned_desc.lower()
                for tok in ("acceptance criteria", "acceptance:", "ac:", "- [ ]", "- [x]", "must ")
            )
        )
        lin_rec: dict[str, Any] = {
            "id": li.get("id"),
            "title": li.get("title"),
            "state": li.get("state"),
            "project": li.get("project"),
            "labels": list(li.get("labels") or []),
            "url": li.get("url") or "",
            "source_refs": list(li.get("source_refs") or [])[:4],
            "links": _indep_clean_links(li.get("links"))[:4],
            "has_ac": lin_has_ac,
            "covers_target_req_id": bool(req_id and req_id.lower() in lin_blob.lower()),
            "description": sliced_desc,
        }
        if li.get("matched_req_ids"):
            m_ids = [str(x) for x in li["matched_req_ids"] if str(x).strip()]
            if req_id and req_id in m_ids:
                m_ids = [req_id] + [x for x in m_ids if x != req_id]
            lin_rec["matched_req_ids"] = m_ids[:8]
        lin_list.append(lin_rec)

    not_list: list[dict[str, Any]] = []
    for nr in list(notion_records)[:8]:
        raw_icon = nr.get("icon")
        icon_val = str(raw_icon) if raw_icon is not None else ""
        cleaned_note = _indep_clean_notion_text(nr.get("note") or nr.get("Note") or "")
        cleaned_content = _indep_clean_notion_text(nr.get("content") or "")
        content_excerpt = _indep_targeted_slice(cleaned_content, req_id, source_file, 2800)
        proj_id = nr.get("project_id") or nr.get("Project")
        proj_name = nr.get("project") or (
            "Project Aegis" if proj_id == NOTION_PROJECT_AEGIS_ID else (proj_id or "")
        )
        not_blob = f"{nr.get('title') or nr.get('name') or ''}\n{cleaned_note}\n{cleaned_content}"
        not_rec: dict[str, Any] = {
            "id": nr.get("id"),
            "title": nr.get("title") or nr.get("name") or "",
            "surface": nr.get("surface") or nr.get("database") or "",
            "status": nr.get("status") or nr.get("Status"),
            "project": proj_name,
            "project_id": proj_id,
            "icon": icon_val,
            "has_icon": bool(nr.get("has_icon") or icon_val),
            "url": nr.get("url") or "",
            "links": _indep_clean_links(nr.get("links"))[:4],
            "covers_target_req_id": bool(req_id and req_id.lower() in not_blob.lower()),
            "note": cleaned_note[:600],
            "content_excerpt": content_excerpt,
        }
        if nr.get("matched_req_ids"):
            n_ids = [str(x) for x in nr["matched_req_ids"] if str(x).strip()]
            if req_id and req_id in n_ids:
                n_ids = [req_id] + [x for x in n_ids if x != req_id]
            not_rec["matched_req_ids"] = n_ids[:8]
        not_list.append(not_rec)

    # Independently compute non-circular deterministic predicates from raw records
    p_src = bool(source_file)
    p_iso = (
        not is_excluded_path(source_file)
        and not any(is_cpq_linear_issue(li) for li in linear_records)
    )
    shipped_ver = (
        lifecycle_l == "shipped"
        and (
            req_state.get("sub_status") == "shipped_verified"
            or bool(req_state.get("shipped_evidence"))
            or any(
                str(li.get("state") or "").strip().lower() == "done" and bool(li.get("merged_to_main", True))
                for li in linear_records
            )
        )
    )
    lin_cites_src = any(
        (source_file and source_file in (str(li.get("description") or "")))
        or (source_basename and source_basename in (str(li.get("description") or "")))
        for li in contract_lin
    )
    lin_cites_id = any(
        req_id and req_id.lower() in f"{li.get('title') or ''}\n{li.get('description') or ''}".lower()
        for li in contract_lin
    )
    lin_has_ac_all = any(
        bool(li.get("has_ac"))
        or any(
            tok in str(li.get("description") or "").lower()
            for tok in ("acceptance criteria", "acceptance:", "ac:", "- [ ]", "- [x]", "must ")
        )
        for li in contract_lin
    )
    lin_no_surface = all(
        not (
            str(li.get("state") or "").strip().lower() in ("backlog", "todo", "triage", "")
            and (
                re.search(r"(?im)^\s*(?:[-*]\s*)?Surface\s*:", str(li.get("description") or ""))
                or any(str(lb).lower().startswith(("surface:", "surface/")) for lb in (li.get("labels") or []))
            )
        )
        for li in linear_records
    )
    lin_has_notion = any(
        "notion.so/" in str(li.get("description") or "").lower()
        or "notion.com/" in str(li.get("description") or "").lower()
        or bool(li.get("notion_url"))
        for li in contract_lin
    )
    lin_honest = True
    for li in contract_lin:
        st_l = str(li.get("state") or "").strip().lower()
        blob_l = f"{li.get('title') or ''}\n{li.get('description') or ''}".lower()
        if lifecycle_l == "deferred_phase_n" and (st_l in ("in progress", "in review", "done") or not any(t in blob_l for t in ("phase n", "deferred", "do not start"))):
            lin_honest = False
        if lifecycle_l == "draft" and (st_l in ("done", "approved") or not any(t in blob_l for t in ("draft", "owner acceptance pending", "pending owner"))):
            lin_honest = False
    all_lin_closed = all(str(li.get("state") or "").strip().lower() in closed_states for li in linear_records)
    shipped_exempt = shipped_ver and p_iso and p_src and lin_honest and (not linear_records or all_lin_closed)
    p_lin_exists = bool(linear_records) or shipped_exempt
    p_lin_contract = True if shipped_exempt else (
        bool(linear_records) and (lin_cites_src or lin_cites_id) and (lin_has_ac_all or lifecycle_l == "deferred_phase_n") and lin_no_surface and lin_honest and lin_has_notion
    )

    active_not = [
        nr for nr in notion_records
        if not any(t in str(nr.get("surface") or nr.get("database") or "").lower() for t in ("frozen", "requirements"))
        or "drafts" in str(nr.get("surface") or "").lower()
    ]
    not_valid_proj_icon = bool(active_not)
    not_cites = False
    not_has_lin = False
    for nr in active_not:
        surf_l = str(nr.get("surface") or nr.get("database") or "").lower()
        if not surf_l or "tasks" in surf_l:
            proj_ok = str(nr.get("project") or "").strip().lower() == "project aegis" or (nr.get("project_id") or nr.get("Project")) == NOTION_PROJECT_AEGIS_ID
            icon_ok = bool(nr.get("has_icon") or nr.get("icon"))
            if not proj_ok or not icon_ok:
                not_valid_proj_icon = False
        n_blob = f"{nr.get('title') or nr.get('name') or ''}\n{_indep_clean_notion_text(nr.get('note') or nr.get('Note') or '')}\n{_indep_clean_notion_text(nr.get('content') or '')}"
        if (source_file and (source_file in n_blob or (source_basename and source_basename in n_blob))) or (req_id and req_id.lower() in n_blob.lower()):
            not_cites = True
        if re.search(r"\bDRG-\d+\b", n_blob) or "linear.app/" in n_blob.lower() or shipped_ver:
            not_has_lin = True

    not_honest = True
    lin_states = {str(li.get("state") or "").strip().lower() for li in linear_records}
    open_not = [
        nr for nr in active_not
        if str(nr.get("status") or nr.get("Status") or "").strip().lower() not in ("done", "archived")
    ]
    for nr in (open_not if open_not else active_not):
        st_n = str(nr.get("status") or nr.get("Status") or "").strip().lower()
        n_blob_l = f"{nr.get('title') or nr.get('name') or ''}\n{_indep_clean_notion_text(nr.get('note') or nr.get('Note') or '')}\n{_indep_clean_notion_text(nr.get('content') or '')}".lower()
        if st_n == "done" and not shipped_ver and (not lin_states or any(s != "done" for s in lin_states)):
            not_honest = False
        if lifecycle_l == "deferred_phase_n" and (st_n == "done" or not any(t in n_blob_l for t in ("phase n", "deferred", "do not start"))):
            not_honest = False
        if lifecycle_l == "draft" and (st_n in ("done", "approved") or not any(t in n_blob_l for t in ("draft", "owner acceptance pending", "pending owner"))):
            not_honest = False

    notion_only_frozen = bool(notion_records) and not active_not
    indep_checks: dict[str, Any] = {
        "P_SRC": p_src,
        "P_ISO": p_iso,
        "P_LIN_EXISTS": p_lin_exists,
        "P_LIN_CONTRACT": p_lin_contract,
        "P_NOT_EXISTS": bool(active_not),
        "P_NOT_CONTRACT": bool(active_not) and not notion_only_frozen and not_valid_proj_icon and not_cites and not_has_lin and not_honest,
        "shipped_verified_exempt": shipped_exempt,
        "linear_cites_source": lin_cites_src,
        "linear_cites_req_id": lin_cites_id,
        "linear_has_ac": lin_has_ac_all,
        "linear_no_surface_line": lin_no_surface,
        "linear_honest_state": lin_honest,
        "linear_has_notion_link": lin_has_notion,
        "notion_only_frozen_mirror": notion_only_frozen,
        "notion_valid_project_and_icon": not_valid_proj_icon,
        "notion_cites_source_or_id": not_cites,
        "notion_has_linear_link": not_has_lin,
        "notion_honest_state": not_honest,
    }

    state_obj: dict[str, Any] = {
        "requirement": req_state,
        "deterministic_checks": indep_checks,
        "linear_records": lin_list,
        "notion_records": not_list,
    }

    # Keep total state safely within Jev 32 KB wire limit (leaving >= 12 KB for question pack)
    def _size() -> int:
        return len(json.dumps(state_obj, ensure_ascii=False).encode("utf-8"))

    if _size() > 20_000:
        for rec in lin_list:
            rec["description"] = rec["description"][:450]
        for rec in not_list:
            rec["note"] = rec["note"][:280]
            rec["content_excerpt"] = rec["content_excerpt"][:350]
    if _size() > 20_000:
        state_obj["linear_records"] = lin_list[:4]
        state_obj["notion_records"] = not_list[:4]
        for rec in state_obj["linear_records"]:
            rec["description"] = rec["description"][:160]
        for rec in state_obj["notion_records"]:
            rec["note"] = rec["note"][:160]
            rec["content_excerpt"] = rec["content_excerpt"][:160]

    return state_obj
