---
name: qa-gauntlet-mission-thread
description: >
  QA Gauntlet specialist for concurrent mission-thread honesty (T3 escort+strike,
  T4 ASW/AAW, T5 multi-domain theater). Checks claimed threads against real
  detection/engage lanes and timeline windows — not vocabulary-only intent.
  Use when /qa-gauntlet-mission-thread, /team-qa-gauntlet --mode mission-thread,
  or /qa-gauntlet A1/A2 on tiers ≥3.
argument-hint: "[--run-id <id>] [--tier N] [--policy-dir PATH]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Bash, Task
---

# QA Gauntlet Mission-Thread — Concurrent Thread Honesty

**Owns:** whether a scenario's **mission-thread claim** is exercised in policy JSON
(detection/engage lanes, staggered windows, concurrent ROE).  
**Does not own:** ladder oracles (`/qa-gauntlet`), forge recipes (`/qa-gauntlet-forge`),
orthogonal axes (`/qa-gauntlet-stress`), C2 Play Mode (`/qa-gauntlet-ui`), or Combat UX
Slice B (DRG-165–170).

Never write `src/`, `unity/`, catalog DB, or `DelegationBridge.cs`. Artifact notes stay
under `production/qa/gauntlet/<RUN_ID>/`. Ask before writing outside that tree.

## Deterministic inputs

| Input | Source |
|-------|--------|
| `--run-id`, `--tier` | Coordinator / `/qa-gauntlet` A1b |
| Policy dir | `production/qa/gauntlet/<RUN_ID>/tier-<N>/` |
| Roster | `tier-N/roster.json` |
| Ladder Mission-type row | `/qa-gauntlet` complexity matrix (T3+) |

## Evidence outputs

| Artifact | Meaning |
|----------|---------|
| `mission-thread-report.json` | `PASS` \| `FAIL` \| `BLOCKED` \| `skipped: below-T3` |
| Claimed vs evidenced thread counts | Distinct detection/engage lanes, not intent prose |

## Entry / exit

- **Enter:** `--mode mission-thread`, or `full`/`qa-gauntlet` A1b when Mission-type ≥ T3.
- **Exit PASS:** each claimed thread has lane/window evidence; IDs in roster.
- **Exit FAIL:** vocabulary-only intent → `/qa-gauntlet-remediation` `scenario-data`.
- **Exit BLOCKED:** missing roster/policies. Script oracles stay unused here (pre-batch).

## Slice A/B/C coverage

| Slice | Coverage |
|-------|----------|
| Slice A (Find/Fix/Track/Target) | **In scope** — concurrent kill-chain *lanes* in policy JSON |
| Slice B (DRG-165–170 Combat UX) | **Out** — do not implement chrome |
| Slice C | **Out** — later; do not invent |

Does not replace DRG-200 manifest or DRG-201 evidence ledger.

## Phase 1 — When to run

| Trigger | Action |
|---------|--------|
| `/team-qa-gauntlet --mode mission-thread` | This skill only |
| `/qa-gauntlet` A1/A2 and ladder **Mission type** row is T3+ | Required honesty gate |
| Intent mentions concurrent / combined / theater threads | This skill |

T1–T2 single-mission rows: **skip** (PASS with `skipped: below-T3`).

## Phase 2 — Honesty checklist

For each policy under `--policy-dir` (default `production/qa/gauntlet/<RUN_ID>/tier-<N>/`):

1. Parse `gauntlet.intent` for claimed threads (patrol / strike / escort / ASW / AAW).
2. Count distinct `detection[]` observer→target pairs and `gauntlet.units` domain mix.
3. Confirm timeline support: `mission.triggers`, `events`, or staggered `activeFromTick`
   / window notes — not prose alone.
4. Catalog IDs must already be in `tier-N/roster.json`. Unresolved ID → `scenario-data`.

| Claim | Minimum evidence | Verdict if missing |
|-------|------------------|--------------------|
| Two threads (T3) | ≥2 detection lanes **or** escort+strike units with distinct targets | **FAIL** vocabulary-only |
| ASW+AAW (T4) | surface/air **and** subsurface observers or targets | **FAIL** |
| Theater 3+ threads (T5) | ≥3 concurrent lanes / staggered windows | **FAIL** |

Write `mission-thread-report.json` (`PASS` \| `FAIL` \| `BLOCKED`) into the run dir.

## Phase 3 — Routing

| Result | Route |
|--------|--------|
| **FAIL** (vocabulary-only) | `/qa-gauntlet-remediation` class `scenario-data` (regenerate ≤2 via forge A) |
| **BLOCKED** (no roster / no policies) | Surface immediately; do not invent lanes |
| **PASS** | Return to `/qa-gauntlet` A2 / `/qa-gauntlet-forge` |

## Never

- Invent detection/engage rows to make intent true.
- Edit `data/scenarios/gauntlet-t2-escort-passive.policy.json` (other worker).
- Claim threads proven from `gauntlet.intent` text alone.

## See also

- `/team-qa-gauntlet` — `--mode mission-thread` and `full` T3+ hook
- `/qa-gauntlet` — ladder Mission-type row (authority for which tier claims threads)
- `/qa-gauntlet-forge` — candidate drafting; this skill is the honesty gate
- `/qa-gauntlet-remediation` — scenario-data regen
