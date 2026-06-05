# Cursor Automations — Project Aegis Dashboard

This directory holds **version-controlled prompts** for Cursor Automations.
The schedule itself is configured in the [Cursor dashboard](https://cursor.com/automations), not in git.

## Daily Dashboard (09:00 + 21:00)

**Automation name:** `Project Aegis — Daily Dashboard`

### One-Time Setup

1. Open [cursor.com/automations/new](https://cursor.com/automations/new)
2. **Name:** `Project Aegis — Daily Dashboard`
3. **Repository:** attach this repo (`cmano-clone`), default branch
4. **Triggers** — add both:
   - Scheduled → `0 9 * * *` (morning)
   - Scheduled → `0 21 * * *` (evening)
5. **Timezone:** set in the UI so 09:00 and 21:00 match team local time
6. **Prompt:** paste contents of [project-dashboard-daily.prompt.md](./project-dashboard-daily.prompt.md)
7. **Tools:** enable **Open pull request** if dashboard commits should be reviewed
8. **MCP:** enable GitNexus if registered (CLI fallback works without MCP)
9. Save and activate

### Alternative: Chat Setup

In Cursor chat:

```
/create-automation Twice daily at 9am and 9pm, run the project-dashboard skill with GitNexus polling and update docs/reports/project-dashboard.md
```

Then attach this repo and paste the prompt from `project-dashboard-daily.prompt.md`.

### Manual Refresh

Run `/project-dashboard` or `/project-dashboard pm` anytime without waiting for the schedule.

### Outputs

| File | Purpose |
|------|---------|
| `docs/reports/project-dashboard.md` | Latest canonical dashboard |
| `docs/reports/dashboard-snapshots/YYYY-MM-DD-{am\|pm}.md` | Archived snapshots |
| `production/dashboard-state.yaml` | Delta baseline for next run |

### Fallback: GitHub Actions

If the team prefers repo-native scheduling, add `.github/workflows/project-dashboard-daily.yml`
with cron `0 9 * * *` and `0 21 * * *` (adjust timezone), `CURSOR_API_KEY` secret, and:

```yaml
- run: agent -p "$(cat .cursor/automations/project-dashboard-daily.prompt.md)" --force
```

Primary path remains Cursor Automations per project plan.
