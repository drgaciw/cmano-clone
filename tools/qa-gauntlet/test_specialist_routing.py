#!/usr/bin/env python3
"""DRG-199: coordinator can discover and route gauntlet specialist skills."""

from __future__ import annotations

from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[2]
SKILLS = ROOT / ".claude" / "skills"
COORD = SKILLS / "team-qa-gauntlet" / "SKILL.md"
ROUTING = Path(__file__).resolve().parent / "specialist-routing.yaml"

REQUIRED_HEADINGS = (
    "## Deterministic inputs",
    "## Evidence outputs",
    "## Entry / exit",
    "## Slice A/B/C coverage",
)

FORBIDDEN_LLM_OVERRIDE = (
    "LLM may override Passed=false",
    "LLM overrides hardGatesPass",
    "reinterpret evaluate_run.py",
)


def _load_routing() -> dict:
    data = yaml.safe_load(ROUTING.read_text(encoding="utf-8"))
    assert data["version"] == 1
    assert data["coordinator"] == "team-qa-gauntlet"
    return data


def test_routing_yaml_and_coordinator_discover_three_roles():
    data = _load_routing()
    coord = COORD.read_text(encoding="utf-8")
    assert COORD.is_file()
    hint_line = next(line for line in coord.splitlines() if line.startswith("argument-hint:"))
    for mode, spec in data["modes"].items():
        skill_dir = SKILLS / spec["skill"]
        skill_md = skill_dir / "SKILL.md"
        assert skill_dir.is_dir(), f"missing skill dir {skill_dir}"
        assert skill_md.is_file(), f"missing {skill_md}"
        assert f"--mode {mode}" in coord or f"`{mode}`" in coord
        assert spec["skill"] in coord
        assert mode in hint_line
        front = skill_md.read_text(encoding="utf-8")
        assert front.split("name:", 1)[1].splitlines()[0].strip() == spec["skill"]


def test_each_role_declares_io_entry_exit_and_slice_coverage():
    data = _load_routing()
    for spec in data["modes"].values():
        text = (SKILLS / spec["skill"] / "SKILL.md").read_text(encoding="utf-8")
        for heading in REQUIRED_HEADINGS:
            assert heading in text, f"{spec['skill']} missing {heading}"
        assert "Slice A" in text and "Slice B" in text and "Slice C" in text


def test_script_owned_pass_fail_and_presentation_remediation():
    data = _load_routing()
    coord = COORD.read_text(encoding="utf-8")
    assert "LLM never overrides" in coord
    assert data["presentation_defect_route"] in coord
    for spec in data["modes"].values():
        text = (SKILLS / spec["skill"] / "SKILL.md").read_text(encoding="utf-8")
        for phrase in FORBIDDEN_LLM_OVERRIDE:
            assert phrase not in text
        assert "/qa-gauntlet-remediation" in text
    for ticket in data["out_of_scope"]:
        if ticket.startswith("DRG-16"):
            combat = (SKILLS / "qa-gauntlet-combat-ui" / "SKILL.md").read_text(encoding="utf-8")
            assert "Do **not** implement" in combat or "not Combat UX Slice B" in combat


def test_does_not_claim_drg_200_or_201():
    data = _load_routing()
    assert "DRG-200" in data["out_of_scope"]
    assert "DRG-201" in data["out_of_scope"]
    coord = COORD.read_text(encoding="utf-8")
    assert "do not implement those tickets here" in coord
    for spec in data["modes"].values():
        text = (SKILLS / spec["skill"] / "SKILL.md").read_text(encoding="utf-8")
        assert "Does not replace DRG-200" in text or "do not implement" in text.lower()
