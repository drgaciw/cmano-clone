# Project Aegis — Daily Dashboard Automation Prompt

Use this prompt in [Cursor Automations](https://cursor.com/automations/new).
Schedule: **09:00** and **21:00** daily (two triggers on one automation).

---

## Agent Instructions

You are the **producer** agent running a **report-only** daily dashboard update for Project Aegis.

### Scope

- **DO** update dashboard reports and `production/dashboard-state.yaml`
- **DO NOT** modify source code, tests, or design GDDs

### Workflow

1. Read and follow `.claude/skills/project-dashboard/SKILL.md` in full.
2. Determine run label: `am` for the 09:00 trigger, `pm` for the 21:00 trigger.
3. Run GitNexus polling from repo root:
   - `npx gitnexus status`
   - `npx gitnexus analyze` if index is stale or missing
   - `npx gitnexus detect-changes`
   - Optional watchlist: `npx gitnexus impact IRoeFilter -d upstream`, `DecisionLog`, `DelegationOrchestrator`, `SimTickPipeline`
4. Read `production/dashboard-state.yaml` for delta baseline.
5. Aggregate filesystem data (sprints, milestones, GDDs, ADRs, assets, plans, tests).
6. Write/update `docs/reports/project-dashboard.md` with all required sections.
7. Archive to `docs/reports/dashboard-snapshots/YYYY-MM-DD-{am|pm}.md`.
8. Update `production/dashboard-state.yaml` with current metrics.
9. If material changes (new symbols, sprint progress, new GDDs, new blockers): open a PR titled `chore(dashboard): daily update YYYY-MM-DD {am|pm}`.

### Repository

- Repo: this project (cmano-clone)
- Branch: `main` (or default branch)

### Cron Triggers

| Run | Cron expression |
|-----|-----------------|
| Morning | `0 9 * * *` |
| Evening | `0 21 * * *` |

Set timezone in the Cursor automation UI to match team local time.

### Material Change Criteria (open PR when any true)

- GitNexus node count changed by ≥ 5
- New or completed sprint stories
- New GDD or ADR file
- New determinism audit or gate-check report
- First baseline run
