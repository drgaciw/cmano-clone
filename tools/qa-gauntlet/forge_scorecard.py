#!/usr/bin/env python3
"""Mechanical promotion scorecard for QA Gauntlet Forge.

Locked-eval analog: scores candidates using corpus coverage + oracle/batch
artifacts. Does not mutate policies or edit GauntletOracleEvaluator.

Usage:
  python3 tools/qa-gauntlet/forge-scorecard.py \\
    --run-dir production/qa/gauntlet/<RUN_ID> \\
    --tier N \\
    [--corpus production/qa/gauntlet/corpus] \\
    [--out <run-dir>/forge/scorecard.json]

Exit 0 always when scorecard written; promote decisions are encoded in JSON.
Exit 2 on usage / missing paths.
"""

from __future__ import annotations

import argparse
import json
import hashlib
import sys
from pathlib import Path
from collections import Counter
from typing import Any


TIER_TICKS = {1: 6, 2: 10, 3: 16, 4: 24, 5: 40}


def load_json(path: Path) -> Any:
    with path.open(encoding="utf-8") as f:
        return json.load(f)


def intent_hash(intent: str, cell_key: str) -> str:
    return hashlib.sha256(f"{intent}|{cell_key}".encode()).hexdigest()[:16]


def _infer_stress_axes(policy: dict[str, Any]) -> str:
    """Derives the stress-axis signature from policy content.

    Reads the policy itself rather than the scenario id, so a hand-authored
    scenario that applies pressure without the derived naming is still counted.
    """
    engage = policy.get("engage") or {}
    rounds = engage.get("defaultMagazineRounds")
    salvo = engage.get("salvoSize")
    if rounds is not None and rounds <= 1:
        weapons = "extreme"
    elif (rounds is not None and rounds <= 2) or (salvo is not None and salvo >= 2):
        weapons = "moderate"
    else:
        weapons = "off"

    jammers = policy.get("jammers") or []
    strength = max((j.get("jamStrength", 0) for j in jammers), default=0)
    if strength >= 0.8:
        ew = "extreme"
    elif strength > 0:
        ew = "moderate"
    else:
        ew = "off"

    logistics_block = policy.get("logistics") or {}
    burn = logistics_block.get("burnRateKgPerSecond", 0)
    if burn >= 14:
        logistics = "extreme"
    elif burn > 0:
        logistics = "moderate"
    else:
        logistics = "off"

    return f"ew:{ew}|logistics:{logistics}|weapons:{weapons}"


def infer_cell(policy: dict[str, Any], scenario_id: str) -> dict[str, Any]:
    g = policy.get("gauntlet") or {}
    intent = (g.get("intent") or "") + " " + scenario_id
    low = intent.lower()
    mission = "unknown"
    for m in (
        "patrol",
        "strike",
        "escort",
        "asw",
        "aaw",
        "theater",
        "cascade",
        "multi",
        "weighted",
        "id-roe",
        "event-chain",
        "emcon",
        "roe-change",
        "dynamic",
        "inject",
        "joint",
    ):
        if m in low or m.replace("-", "") in low.replace("-", ""):
            mission = m
            break
    if "roe-change" in scenario_id:
        mission = "roe-change"
    if "event-chain" in scenario_id or "event_chain" in scenario_id:
        mission = "event-chain"
    event = "none"
    if "cascade" in low:
        event = "cascade"
    elif "inject" in low or "random" in scenario_id:
        event = "inject"
    elif "event" in low:
        event = "event-chain"
    elif "roe-change" in scenario_id:
        event = "roe-change"
    elif "dynamic" in low:
        event = "dynamic-obj"
    emcon = "unrestricted"
    if "emcon" in low:
        emcon = "emcon-phases"
    elif "passive" in low:
        emcon = "passive"
    elif "contested" in low or "deception" in low:
        emcon = "contested-em"
    roe = f"{policy.get('friendlyRoe', '?')}/{policy.get('opposingRoe', '?')}"
    domains: set[str] = set()
    for u in g.get("units") or []:
        domains.add((u.get("domain") or "surface").lower())
    if not domains:
        domains = {"surface"}
    cell_key = f"{mission}|{','.join(sorted(domains))}|{roe}|{emcon}|{event}"
    return {
        "key": cell_key,
        "stressAxes": _infer_stress_axes(policy),
        "missionClass": mission,
        "domains": sorted(domains),
        "roePair": roe,
        "emconClass": emcon,
        "eventClass": event,
        "intentHash": intent_hash(g.get("intent") or "", cell_key),
    }


def platform_ids(policy: dict[str, Any]) -> set[str]:
    g = policy.get("gauntlet") or {}
    ids: set[str] = set(g.get("catalogRefs") or [])
    for u in g.get("units") or []:
        pid = u.get("platformId") or u.get("id")
        if pid:
            ids.add(pid)
    return ids


def read_oracle_passed(tier_dir: Path) -> dict[str, bool]:
    """Map scenario id -> passed from oracle-eval.json if present."""
    path = tier_dir / "oracle-eval.json"
    if not path.is_file():
        return {}
    try:
        data = load_json(path)
    except json.JSONDecodeError:
        return {}
    out: dict[str, bool] = {}
    # Support common shapes: {results:[{scenarioId, passed}]} or {scenarios:{id:{passed}}}
    if isinstance(data.get("results"), list):
        for row in data["results"]:
            sid = row.get("scenarioId") or row.get("policyId") or row.get("id")
            if sid is None:
                continue
            passed = row.get("passed")
            if passed is None:
                passed = row.get("Passed")
            if passed is not None:
                out[str(sid).replace(".policy.json", "")] = bool(passed)
    scenarios = data.get("scenarios")
    if isinstance(scenarios, list):
        for row in scenarios:
            if not isinstance(row, dict):
                continue
            sid = row.get("scenario") or row.get("scenarioId") or row.get("policyId") or row.get("id")
            if sid is None:
                continue
            passed = row.get("passed", row.get("Passed"))
            if passed is not None:
                out[str(sid).replace(".policy.json", "")] = bool(passed)
    elif isinstance(scenarios, dict):
        for sid, row in scenarios.items():
            if isinstance(row, dict):
                passed = row.get("passed", row.get("Passed"))
                if passed is not None:
                    out[str(sid).replace(".policy.json", "")] = bool(passed)
    if "allPassed" in data and not out:
        # Single-policy eval files sometimes only have allPassed (no per-scenario rows)
        out["*"] = bool(data["allPassed"])
    return out



def rebuild_counts(
    cells: list[dict[str, Any]],
    *,
    policies_by_sid: dict[str, dict[str, Any]] | None = None,
    scenarios_dir: Path | None = None,
    underused_threshold: int = 2,
    underused_limit: int = 15,
) -> dict[str, Any]:
    """Rebuild coverage-map counts (+ underusedPlatformHint) from registered cells.

    Single-valued cell dims (missionClass, eventClass, roePair, emconClass) must
    each sum to len(cells). domain is multi-valued per cell. platformId counts
    are occurrence counts across policies (catalogRefs + unit platformIds).
    """
    mission: Counter[str] = Counter()
    event: Counter[str] = Counter()
    roe: Counter[str] = Counter()
    emcon: Counter[str] = Counter()
    domain: Counter[str] = Counter()
    platform: Counter[str] = Counter()

    if policies_by_sid is None and scenarios_dir is not None:
        policies_by_sid = {}
        for cell in cells:
            sid = cell.get("scenarioId")
            if not sid:
                continue
            p = scenarios_dir / f"{sid}.policy.json"
            if p.is_file():
                policies_by_sid[str(sid)] = load_json(p)

    policies_by_sid = policies_by_sid or {}

    for cell in cells:
        mission[str(cell.get("missionClass") or "unknown")] += 1
        event[str(cell.get("eventClass") or "none")] += 1
        roe[str(cell.get("roePair") or "?/?")] += 1
        emcon[str(cell.get("emconClass") or "unrestricted")] += 1
        for d in cell.get("domains") or []:
            domain[str(d)] += 1
        sid = cell.get("scenarioId")
        if sid and sid in policies_by_sid:
            for pid in platform_ids(policies_by_sid[str(sid)]):
                platform[pid] += 1

    counts = {
        "missionClass": dict(sorted(mission.items(), key=lambda kv: (-kv[1], kv[0]))),
        "eventClass": dict(sorted(event.items(), key=lambda kv: (-kv[1], kv[0]))),
        "roePair": dict(sorted(roe.items(), key=lambda kv: (-kv[1], kv[0]))),
        "emconClass": dict(sorted(emcon.items(), key=lambda kv: (-kv[1], kv[0]))),
        "domain": dict(sorted(domain.items(), key=lambda kv: (-kv[1], kv[0]))),
        "platformId": dict(sorted(platform.items(), key=lambda kv: (-kv[1], kv[0]))),
    }

    underused = sorted(
        [pid for pid, n in platform.items() if n <= underused_threshold],
        key=lambda pid: (platform[pid], pid),
    )[:underused_limit]

    return {"counts": counts, "underusedPlatformHint": underused}


def assert_counts_consistent(
    coverage: dict[str, Any],
    *,
    policies_by_sid: dict[str, dict[str, Any]] | None = None,
    scenarios_dir: Path | None = None,
) -> None:
    """Raise AssertionError if coverage counts drift from registered cells."""
    cells = coverage.get("cells") or []
    cell_count = int(coverage.get("cellCount") or 0)
    scenario_count = int(coverage.get("scenarioCount") or 0)
    if cell_count != len(cells):
        raise AssertionError(f"cellCount {cell_count} != len(cells) {len(cells)}")
    if scenario_count != len(cells):
        raise AssertionError(
            f"scenarioCount {scenario_count} != len(cells) {len(cells)}"
        )

    rebuilt = rebuild_counts(
        cells, policies_by_sid=policies_by_sid, scenarios_dir=scenarios_dir
    )
    stored = coverage.get("counts") or {}
    for dim in (
        "missionClass",
        "eventClass",
        "roePair",
        "emconClass",
        "domain",
        "platformId",
    ):
        if (stored.get(dim) or {}) != rebuilt["counts"][dim]:
            raise AssertionError(
                f"counts.{dim} stale vs rebuild from cells/policies"
            )
    for dim in ("missionClass", "eventClass", "roePair", "emconClass"):
        total = sum((stored.get(dim) or {}).values())
        if total != len(cells):
            raise AssertionError(
                f"counts.{dim} sum {total} != cellCount {len(cells)}"
            )
    # Multi-valued dims: each value is a scenario occurrence count, so no entry
    # may exceed scenarioCount.
    for dim in ("domain", "platformId"):
        for key, n in (stored.get(dim) or {}).items():
            if int(n) > len(cells):
                raise AssertionError(
                    f"counts.{dim}[{key}]={n} exceeds scenarioCount {len(cells)}"
                )
    if (coverage.get("underusedPlatformHint") or []) != rebuilt[
        "underusedPlatformHint"
    ]:
        raise AssertionError("underusedPlatformHint stale vs rebuild")



def score_candidate(
    policy_path: Path,
    coverage: dict[str, Any],
    index_hashes: set[str],
    oracle_map: dict[str, bool],
    tier: int,
    useful_fail_ids: set[str],
    index_scenario_ids: set[str] | None = None,
) -> dict[str, Any]:
    policy = load_json(policy_path)
    # Prefer the policy's own id (what gauntlet_oracle_eval keys oracle-eval.json
    # by) over the filename stem, which the qa-gauntlet-forge skill's candidate
    # naming convention (candidate-1.policy.json, ...) never matches.
    # See BUG-forge-scorecard-filename-vs-policy-id.
    sid = policy.get("id") or policy_path.name.replace(".policy.json", "")
    cell = infer_cell(policy, sid)
    known_keys = {c["key"] for c in coverage.get("cells") or []}
    new_cell = cell["key"] not in known_keys
    plat_counts = (coverage.get("counts") or {}).get("platformId") or {}
    plats = platform_ids(policy)
    rare_hits = [p for p in plats if int(plat_counts.get(p, 0)) <= 1]
    indexed_ids = index_scenario_ids or set()
    duplicate = cell["intentHash"] in index_hashes or sid in indexed_ids
    oracle_ok = oracle_map.get(sid)
    if oracle_ok is None and "*" in oracle_map:
        oracle_ok = oracle_map["*"]
    useful_fail = sid in useful_fail_ids

    # Distinguish "never evaluated" (no oracle entry at all) from "evaluated and
    # failed" (oracle_ok is False). Both still block promotion unless usefulFail
    # applies — this flag is a visible diagnostic signal only, it does not
    # change hard-gate semantics.
    oracle_lookup_missed = oracle_ok is None and not useful_fail
    if oracle_lookup_missed:
        print(
            f"warn: forge-scorecard: no oracle result for candidate '{sid}' "
            f"(path={policy_path}); treating as never-evaluated (hard gate blocks "
            "promotion) — distinct from an evaluated-and-failed oracle result",
            file=sys.stderr,
        )

    # Oracle must be known: Passed=true OR usefulFail (sim-code / scenario-data).
    # oracle_ok is None (never evaluated) must NOT promote — locked-eval contract.
    oracle_gate = (oracle_ok is True) or useful_fail
    hard = {
        "hasGauntletIntent": bool((policy.get("gauntlet") or {}).get("intent")),
        "hasGauntletExpect": isinstance((policy.get("gauntlet") or {}).get("expect"), dict),
        "tierTicksKnown": tier in TIER_TICKS,
        "notDuplicateIntent": not duplicate,
        "oracleKnown": oracle_ok is not None or useful_fail,
        "oraclePassedOrUsefulFail": oracle_gate,
    }
    hard_pass = all(
        [
            hard["hasGauntletIntent"],
            hard["hasGauntletExpect"],
            hard["tierTicksKnown"],
            hard["notDuplicateIntent"],
            hard["oracleKnown"],
            hard["oraclePassedOrUsefulFail"],
        ]
    )

    novelty = 0.0
    if new_cell:
        novelty += 3.0
    novelty += min(2.0, 0.5 * len(rare_hits))
    if useful_fail:
        novelty += 4.0
    if duplicate:
        novelty -= 5.0
    if oracle_ok is True and new_cell:
        novelty += 1.0

    promote = hard_pass and novelty > 0 and not duplicate

    return {
        "scenarioId": sid,
        "path": str(policy_path),
        "tier": tier,
        "recommendedTicks": TIER_TICKS.get(tier),
        "cell": cell,
        "newCoverageCell": new_cell,
        "rarePlatformHits": rare_hits,
        "duplicateIntent": duplicate,
        "oraclePassed": oracle_ok,
        "oracleLookupMissed": oracle_lookup_missed,
        "usefulFail": useful_fail,
        "hardGates": hard,
        "hardGatesPass": hard_pass,
        "noveltyScore": round(novelty, 3),
        "recommendPromote": promote,
        "recipesHint": (policy.get("gauntlet") or {}).get("forgeRecipes") or [],
    }


def load_useful_fails(run_dir: Path) -> set[str]:
    bugs = run_dir / "bugs"
    ids: set[str] = set()
    if not bugs.is_dir():
        return ids
    for p in bugs.glob("*.md"):
        text = p.read_text(encoding="utf-8", errors="replace")
        if "sim-code" in text.lower() or "scenario-data" in text.lower():
            # Best-effort: look for scenario id lines
            for line in text.splitlines():
                if "scenario" in line.lower() and "gauntlet-" in line:
                    for token in line.replace("`", " ").replace(",", " ").split():
                        if token.startswith("gauntlet-"):
                            ids.add(token.strip(".,;:"))
    return ids


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--run-dir", type=Path, default=None)
    ap.add_argument("--tier", type=int, default=None)
    ap.add_argument(
        "--corpus",
        type=Path,
        default=Path("production/qa/gauntlet/corpus"),
    )
    ap.add_argument("--out", type=Path, default=None)
    ap.add_argument(
        "--candidates-dir",
        type=Path,
        default=None,
        help="Override candidates path (default: <run-dir>/forge/candidates)",
    )
    ap.add_argument(
        "--rebuild-counts",
        action="store_true",
        help="Rewrite coverage-map.json counts (+ underusedPlatformHint) from cells/policies and exit",
    )
    args = ap.parse_args()

    if args.rebuild_counts:
        coverage_path = args.corpus / "coverage-map.json"
        if not coverage_path.is_file():
            print(f"error: missing coverage map: {coverage_path}", file=sys.stderr)
            return 2
        coverage = load_json(coverage_path)
        scenarios_dir = Path("data/scenarios")
        rebuilt = rebuild_counts(
            coverage.get("cells") or [],
            scenarios_dir=scenarios_dir if scenarios_dir.is_dir() else None,
        )
        coverage["counts"] = rebuilt["counts"]
        coverage["underusedPlatformHint"] = rebuilt["underusedPlatformHint"]
        coverage["cellCount"] = len(coverage.get("cells") or [])
        coverage["scenarioCount"] = len(coverage.get("cells") or [])
        with coverage_path.open("w", encoding="utf-8") as f:
            json.dump(coverage, f, indent=2, ensure_ascii=False)
            f.write("\n")
        print(
            f"forge-scorecard: rebuilt counts for {coverage['cellCount']} cells -> {coverage_path}"
        )
        return 0

    if args.run_dir is None or args.tier is None:
        print("error: --run-dir and --tier required (unless --rebuild-counts)", file=sys.stderr)
        return 2
    run_dir = args.run_dir
    if not run_dir.is_dir():
        print(f"error: run-dir not found: {run_dir}", file=sys.stderr)
        return 2

    coverage_path = args.corpus / "coverage-map.json"
    if not coverage_path.is_file():
        print(f"error: missing coverage map: {coverage_path}", file=sys.stderr)
        return 2
    coverage = load_json(coverage_path)

    index_hashes: set[str] = set()
    index_scenario_ids: set[str] = set()
    index_path = args.corpus / "index.yaml"
    if index_path.is_file():
        for line in index_path.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line.startswith("intentHash:"):
                index_hashes.add(line.split(":", 1)[1].strip())
            if line.startswith("scenarioId:"):
                index_scenario_ids.add(line.split(":", 1)[1].strip())

    cand_dir = args.candidates_dir or (run_dir / "forge" / "candidates")
    tier_dir = run_dir / f"tier-{args.tier}"
    # Also score tier policy copies if candidates empty (post-hoc on ladder policies)
    policy_paths = sorted(cand_dir.glob("*.policy.json")) if cand_dir.is_dir() else []
    if not policy_paths and tier_dir.is_dir():
        policy_paths = sorted(tier_dir.glob("gauntlet-*.policy.json"))
        policy_paths += sorted((tier_dir / "policies").glob("gauntlet-*.policy.json")) if (tier_dir / "policies").is_dir() else []
        # dedupe by name
        seen: set[str] = set()
        uniq: list[Path] = []
        for p in policy_paths:
            if p.name not in seen:
                seen.add(p.name)
                uniq.append(p)
        policy_paths = uniq

    oracle_map = read_oracle_passed(tier_dir) if tier_dir.is_dir() else {}
    useful_fails = load_useful_fails(run_dir)

    results = [
        score_candidate(
            p,
            coverage,
            index_hashes,
            oracle_map,
            args.tier,
            useful_fails,
            index_scenario_ids=index_scenario_ids,
        )
        for p in policy_paths
    ]

    promote = [r for r in results if r["recommendPromote"]]
    discard = [r for r in results if not r["recommendPromote"]]

    out_obj = {
        "version": 1,
        "runDir": str(run_dir),
        "tier": args.tier,
        "recommendedTicks": TIER_TICKS.get(args.tier),
        "candidateCount": len(results),
        "promoteCount": len(promote),
        "discardCount": len(discard),
        "corpusCellCount": coverage.get("cellCount"),
        "results": results,
        "promoteIds": [r["scenarioId"] for r in promote],
        "discardIds": [r["scenarioId"] for r in discard],
    }

    out_path = args.out or (run_dir / "forge" / "scorecard.json")
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("w", encoding="utf-8") as f:
        json.dump(out_obj, f, indent=2)
        f.write("\n")

    print(
        f"forge-scorecard: tier={args.tier} candidates={len(results)} "
        f"promote={len(promote)} discard={len(discard)} -> {out_path}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
