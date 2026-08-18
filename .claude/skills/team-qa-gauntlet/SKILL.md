---
name: team-qa-gauntlet
description: >
  Orchestrate the QA Gauntlet pressure-test team without bloating a single skill.
  Routes to specialist skills: qa-gauntlet (ladder run), qa-gauntlet-ui (game UI
  Smoke/Pressure), qa-gauntlet-forge (variance), qa-gauntlet-calibrate (saboteur),
  qa-gauntlet-stress (orthogonal axes), qa-gauntlet-remediation (Phase D TDD + UCA).
  Use when the user runs /team-qa-gauntlet, or asks for "gauntlet team",
  "pressure-test team", "gauntlet UI", or multi-agent gauntlet dispatch.
argument-hint: "[--run-id <id>] [--mode full|ladder|ui|ui-smoke|forge|calibrate|stress] [--tiers N]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, Task, AskUserQuestion
---

# Team QA Gauntlet — Pressure-Test Orchestrator

You are the **Gauntlet Team Lead**. Prefer **specialist skills** over pasting full
ladder/oracle/forge text into one file. Authority for detailed procedures remains
in the specialist `SKILL.md` files under `.claude/skills/`.

**Not** a substitute for `/team-qa` (human sprint QA package / **manual UAT**).

## Team composition

| Role / skill | Owns |
|--------------|------|
| **`/qa-gauntlet`** | Ladder Phase 0–E, batch driver, oracle gates, AAR handoff |
| **`/qa-gauntlet-ui`** | Game UI Smoke/Pressure: PlayMode+C2 filters, ReplayGolden, C2 signoffs ×5 |
| **`/qa-gauntlet-forge`** | Variance, recipes, promote, Hindsight bank `qa-gauntlet-forge` |
| **`/qa-gauntlet-calibrate`** | Saboteur mutants, kill-rate matrix |
| **`/qa-gauntlet-stress`** | Orthogonal axes `weapons` / `ew` / `logistics`, proof gate |
| **`/qa-gauntlet-remediation`** | Phase D TDD; **UCA gate** on presentation Surfaces |

## Mode routing

| `--mode` | Dispatch |
|----------|----------|
| `full` (default) | `qa-gauntlet` with required forge hooks + stress when claimed |
| `ladder` | `qa-gauntlet` only |
| `ui` / `ui-smoke` | **`qa-gauntlet-ui` only** (do not rewrite or run ladder prose) |
| `forge` | `qa-gauntlet-forge` phases only |
| `calibrate` | `qa-gauntlet-calibrate` |
| `stress` | `qa-gauntlet-stress` plan → derive → proof gate |

### `--mode ui` / `ui-smoke` (summary)

Hard package lives in **`/qa-gauntlet-ui`** — invoke that skill and follow it exactly:

1. `RUN_DIR=production/qa/gauntlet/gauntlet-<timestamp>-ui/`
2. Headless “118-style” UnityAdapter filter (PlayMode/C2/Presentation/Panel/…)
3. `ReplayGolden` filter
4. Unity Editor C2 Play Mode signoffs ×5
5. AAR + `manifest.yaml`
6. Any failure → **`/qa-gauntlet-remediation`** (+ UCA for presentation Surfaces)

**Manual UAT → `/team-qa`** (and `/smoke-check`). UI mode must not invent a second
human QA loop.

## Parallel contract

Follow `production/agentic/qa-skills-parallel-task-contract-2026-07-23.md`:

- **File-disjoint** Tasks only; one-turn dispatch before waiting.
- Scenario generation (forge `a0`) may overlap tier N batch when write paths disjoint.
- Remediation: parallel only after GitNexus `impact` shows disjoint blast radii; serial merge to QA branch.
- UI mode: headless `dotnet test` and Editor signoff are **serial on the same project**
  (Unity lock); do not parallel two Unity Editor instances on one `unity/ProjectAegis`.

## Hard invariants (team-wide)

- Locked eval: `GauntletOracleEvaluator`, Demo batch internals, ReplayGolden fixtures,
  `.github/workflows/gauntlet-oracle.yml` — **never** edit for "learning."
- **`DelegationBridge.cs`** zero-touch; Baltic v2 hash `17144800277401907079` preserved.
- Catalog IDs from roster/DB only; no CatalogWriteGate mutations without EXTEND-ONLY path.
- Graphite for branch/PR; GitNexus `impact` before every symbol edit; `detect_changes()` before commit.
- Script-first hard gates — LLM never overrides `Passed=false` / `hardGatesPass=false`.

## Presentation defects → UCA

If a defect Surface is under `UnityAdapter` / presentation bridges / C2 chrome
(including failures from `--mode ui`):

1. Invoke **`/qa-gauntlet-remediation`** (not ad-hoc fix).
2. That skill loads **unity-csharp-architect** + `pr-finish` (ADR-010/007/001).

See AGENTS.md Unity / C# architecture skill section.

## Success

- Specialist skills invoked by name; no duplicate full ladder prose in this file.
- `--mode ui` ⇒ AAR under `production/qa/gauntlet/<RUN_ID>/` per `/qa-gauntlet-ui`.
- AAR cites latest calibrate report when forge/ladder ran.
- Stress claimed ⇒ `qa-gauntlet-stress` proof gate evidence attached.
- UI track never claims manual UAT complete.

## See also

- `/qa-gauntlet`, `/qa-gauntlet-ui`, `/qa-gauntlet-forge`, `/qa-gauntlet-calibrate`
- `/qa-gauntlet-stress`, `/qa-gauntlet-remediation`
- `/team-qa` — human sprint package / **manual UAT**
- `/smoke-check` — sprint smoke hand-off
- `tools/qa-gauntlet/README-stress-axes.md`
- Reference UI run: `production/qa/gauntlet/gauntlet-20260817-1626-ui/`
