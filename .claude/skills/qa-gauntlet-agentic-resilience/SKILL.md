---
name: qa-gauntlet-agentic-resilience
description: >
  QA Gauntlet specialist for agentic-run resilience: GitNexus CRITICAL quarantine,
  max-fix-attempts, parallel-task BLOCKED, hard-gate never-LLM-override, stalled
  Task/worktree isolation. Distinct from weapons/ew/logistics stress axes.
  Use when /qa-gauntlet-agentic-resilience, /team-qa-gauntlet --mode agentic-resilience,
  or /qa-gauntlet Phase D / Final AAR needs quarantine contract.
argument-hint: "[--run-id <id>] [--tier N] [--defect-id <id>]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Bash, Task
---

# QA Gauntlet Agentic-Resilience — Quarantine & Hard-Gate Contract

**Owns:** how the gauntlet **agent** fails closed (CRITICAL impact, retries, BLOCKED,
no LLM override of script gates).  
**Does not own:** TDD Red/Green (`/qa-gauntlet-remediation`), saboteur kill-rate
(`/qa-gauntlet-calibrate`), stress axes (`/qa-gauntlet-stress`), or sim/UI code.

Never write `src/`, `unity/`, catalog DB, or `DelegationBridge.cs`. Notes only under
`production/qa/gauntlet/<RUN_ID>/`. Ask before writing outside that tree.

## Deterministic inputs

| Input | Source |
|-------|--------|
| GitNexus `impact` risk | MCP / `impact()` before any symbol edit |
| `Passed` / `hardGatesPass` | `evaluate_run.py`, `gauntlet_oracle_eval`, `forge_scorecard.py` |
| `--max-fix-attempts` | `/qa-gauntlet` flag |
| Parallel Task status | `qa-skills-parallel-task-contract-2026-07-23.md` |

## Evidence outputs

| Artifact | Meaning |
|----------|---------|
| `agentic-resilience.json` | `quarantine` \| `retry` \| `blocked` \| `continue` |
| AAR `QUARANTINED-CRITICAL` list | Never silent skip |

## Entry / exit

- **Enter:** `--mode agentic-resilience`, or `full` Phase D / Final AAR, or CRITICAL impact.
- **Exit PASS:** contract followed; script gate values unchanged.
- **Exit FAIL:** LLM flipped a red script gate, or BLOCKED hidden.
- **Exit BLOCKED:** CRITICAL quarantine — do not edit; hand humans the list.

## Slice A/B/C coverage

| Slice | Coverage |
|-------|----------|
| Slice A | **In scope** — fail-closed agent contract around Slice A gauntlet runs |
| Slice B | **Out** — no Combat UX implementation |
| Slice C | **Out** |

Does not replace DRG-200 / DRG-201. Presentation defects still `/qa-gauntlet-remediation` + UCA.

## Phase 1 — Detect

Before any symbol edit and whenever a Task stalls or a gate is red:

| Signal | Contract |
|--------|----------|
| GitNexus `impact` **CRITICAL** | **Quarantine** — do not edit; AAR prominent `QUARANTINED-CRITICAL` |
| `--max-fix-attempts` exhausted | Revert, quarantine scenario, continue remaining ladder |
| `Passed=false` / `hardGatesPass=false` | Script is authority — LLM must not flip to green |
| Independent Tasks | One-turn dispatch; **BLOCKED** surfaced immediately |
| Dirty `src/` / `data/` / `tools/qa-gauntlet/` during calibrate | Stop — saboteur worktrees build from HEAD |

Authority: `production/agentic/qa-skills-parallel-task-contract-2026-07-23.md`.

## Phase 2 — Apply

1. Record `agentic-resilience.json` in the run dir: signal, defect-id, action
   (`quarantine` \| `retry` \| `blocked` \| `continue`).
2. Quarantine list goes to `manifest.yaml` + Final AAR — never silent skip.
3. Retry only via `/qa-gauntlet-remediation` (TDD); this skill does not patch code.
4. Parallel sim-code fixes: disjoint GitNexus radii + `.worktrees/`; **serial merge**
   onto the QA branch.

Verdict: **PASS** (contract followed) \| **FAIL** (LLM overrode a gate / silent skip)
\| **BLOCKED** (CRITICAL / stalled Task).

## Phase 3 — Handoff

| Situation | Next |
|-----------|------|
| Need a code fix | `/qa-gauntlet-remediation` |
| Presentation Surface | Remediation **+ UCA** (still this contract for CRITICAL) |
| Oracle envelope | expect-regen runbook only |
| Calibrate dirty tree | `/qa-gauntlet-calibrate` after clean HEAD |

## Never

- Override `evaluate_run.py` / `gauntlet_oracle_eval` / `forge_scorecard.py` exits.
- Edit locked eval, `DelegationBridge.cs`, or catalog via CatalogWriteGate.
- Continue a tier while hiding BLOCKED Tasks.

## See also

- `/team-qa-gauntlet` — `--mode agentic-resilience` and `full` Phase D hook
- `/qa-gauntlet` — ladder; this skill is the fail-closed contract
- `/qa-gauntlet-remediation` — TDD owner
- `/qa-gauntlet-calibrate` — saboteur (separate)
