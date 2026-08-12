---
name: team-qa-gauntlet
description: >
  Orchestrate the QA Gauntlet pressure-test team without bloating a single skill.
  Routes to specialist skills: qa-gauntlet (ladder run), qa-gauntlet-forge (variance),
  qa-gauntlet-calibrate (saboteur), qa-gauntlet-stress (orthogonal axes), qa-gauntlet-remediation
  (Phase D TDD + UCA presentation gate). Use when the user runs /team-qa-gauntlet, or asks
  for "gauntlet team", "pressure-test team", or multi-agent gauntlet dispatch.
argument-hint: "[--run-id <id>] [--mode full|ladder|forge|calibrate|stress] [--tiers N]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, Task, AskUserQuestion
---

# Team QA Gauntlet — Pressure-Test Orchestrator

You are the **Gauntlet Team Lead**. Prefer **specialist skills** over pasting full
ladder/oracle/forge text into one file. Authority for detailed procedures remains
in the specialist `SKILL.md` files under `.claude/skills/`.

**Not** a substitute for `/team-qa` (human sprint QA package).

## Team composition

| Role / skill | Owns |
|--------------|------|
| **`/qa-gauntlet`** | Ladder Phase 0–E, batch driver, oracle gates, AAR handoff |
| **`/qa-gauntlet-forge`** | Variance, recipes, promote, Hindsight bank `qa-gauntlet-forge` |
| **`/qa-gauntlet-calibrate`** | Saboteur mutants, kill-rate matrix |
| **`/qa-gauntlet-stress`** | Orthogonal axes `weapons` / `ew` / `logistics`, proof gate |
| **`/qa-gauntlet-remediation`** | Phase D TDD; **UCA gate** on presentation Surfaces |

## Mode routing

| `--mode` | Dispatch |
|----------|----------|
| `full` (default) | `qa-gauntlet` with required forge hooks + stress when claimed |
| `ladder` | `qa-gauntlet` only |
| `forge` | `qa-gauntlet-forge` phases only |
| `calibrate` | `qa-gauntlet-calibrate` |
| `stress` | `qa-gauntlet-stress` plan → derive → proof gate |

## Parallel contract

Follow `production/agentic/qa-skills-parallel-task-contract-2026-07-23.md`:

- **File-disjoint** Tasks only; one-turn dispatch before waiting.
- Scenario generation (forge `a0`) may overlap tier N batch when write paths disjoint.
- Remediation: parallel only after GitNexus `impact` shows disjoint blast radii; serial merge to QA branch.

## Hard invariants (team-wide)

- Locked eval: `GauntletOracleEvaluator`, Demo batch internals, ReplayGolden fixtures,
  `.github/workflows/gauntlet-oracle.yml` — **never** edit for "learning."
- **`DelegationBridge.cs`** zero-touch; Baltic v2 hash `17144800277401907079` preserved.
- Catalog IDs from roster/DB only; no CatalogWriteGate mutations without EXTEND-ONLY path.
- Graphite for branch/PR; GitNexus `impact` before every symbol edit; `detect_changes()` before commit.
- Script-first hard gates — LLM never overrides `Passed=false` / `hardGatesPass=false`.

## Presentation defects → UCA

If a defect Surface is under `UnityAdapter` / presentation bridges / C2 chrome:

1. Invoke **`/qa-gauntlet-remediation`** (not ad-hoc fix).
2. That skill loads **unity-csharp-architect** + `pr-finish` (ADR-010/007/001).

See AGENTS.md Unity / C# architecture skill section.

## Success

- Specialist skills invoked by name; no duplicate full ladder prose in this file.
- AAR cites latest calibrate report when forge/ladder ran.
- Stress claimed ⇒ `qa-gauntlet-stress` proof gate evidence attached.

## See also

- `/qa-gauntlet`, `/qa-gauntlet-forge`, `/qa-gauntlet-calibrate`
- `/qa-gauntlet-stress`, `/qa-gauntlet-remediation`
- `/team-qa` — human sprint package
- `tools/qa-gauntlet/README-stress-axes.md`
