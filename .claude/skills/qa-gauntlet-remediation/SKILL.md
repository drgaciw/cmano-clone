---
name: qa-gauntlet-remediation
description: >
  Phase D TDD remediation for QA Gauntlet defects. Classifies sim-code vs scenario-data
  vs oracle vs flaky; enforces GitNexus impact, failing tests first, and unity-csharp-architect
  pr-finish when Surface is presentation (UnityAdapter bridges / C2). Use when /qa-gauntlet-remediation,
  or when /qa-gauntlet Phase D / team-qa-gauntlet dispatches a defect fix.
argument-hint: "[--defect-id <id>] [--surface <path>] [--max-fix-attempts 3]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, Task
---

# QA Gauntlet Remediation — Phase D + UCA Gate

Owns **fixes only**. Ladder execution and forge promote stay in `/qa-gauntlet` and
`/qa-gauntlet-forge`.

## Classification

| Class | Route |
|-------|--------|
| `sim-code` | TDD Red → impact → Green → verify → commit |
| `scenario-data` | Regenerate via Phase A / forge (≤2 attempts) |
| `oracle` | Expect-regen runbook only — no hand envelopes |
| `flaky` | `/test-flakiness` |
| CRITICAL impact | Quarantine; do not edit; AAR prominent |

## Presentation Surface → unity-csharp-architect (required)

If Surface matches any of:

- `src/ProjectAegis.Delegation.UnityAdapter/Bridge/**`
- `**/Presentation/**`, C2 chrome, MonoBehaviour bind, snapshot/projection façade

Then **before** claiming fixed:

1. Load `production/agentic/skills/unity-csharp-architect/SKILL.md`
2. Run `checklists/pr-finish.md` — paste PASS/FAIL/BLOCKED into commit/PR notes
3. Cite **ADR-010 §2–3**, **ADR-007**, **ADR-001** (never Git ADR-018 for presentation)
4. Prefer headless tests; **ZERO-touch** `DelegationBridge.cs`
5. Null guards + docs at MapPictureBridge / OobTreeBridge quality bar when touching public façades

Sim/data-only defects do **not** require UCA.

## TDD cycle (`sim-code`)

1. **Red** — minimal xUnit in co-located `*.Tests` (not top-level `tests/`); fixed seed; confirm RED.
2. **Impact** — GitNexus `impact` upstream on every symbol; CRITICAL → quarantine.
3. **Green** — minimal fix only (`c-sharp-engineer` / `determinism-engineer`).
4. **Verify** — full `dotnet test` ≥ baseline; ReplayGolden green; re-run failing scenario × seeds.
5. **Commit** — `detect_changes()`; Graphite message `qa(gauntlet): fix <id> — <symbol> (tier N)`.
6. Attempts exhausted → revert, quarantine scenario, continue ladder.

## Parallel fixes

Contract: `production/agentic/qa-skills-parallel-task-contract-2026-07-23.md`.
Disjoint impact only; worktrees under `.worktrees/`; **serial merge** + re-verify.

## Never

- Edit locked eval or DelegationBridge for convenience.
- Fix without failing test (`sim-code`).
- Skip UCA/pr-finish on presentation Surfaces.

## See also

- `/qa-gauntlet-ui` — proactive UI Smoke/Pressure gates; failures dispatch here.
- `/qa-gauntlet-combat-ui` — combat-presentation gates; failures dispatch here.
- `/qa-gauntlet-agentic-resilience` — CRITICAL quarantine / never-LLM-override.
- `/team-qa-gauntlet --mode ui` — team entry for the UI track.
- `/team-qa` — manual UAT (not remediation).
