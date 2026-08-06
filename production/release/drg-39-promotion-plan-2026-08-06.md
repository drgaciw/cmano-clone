# DRG-39 — Promote store/launch drafts to final (agent plan)

**Linear:** DRG-39  
**Date:** 2026-08-06  
**Stage:** Release (prep only — no store submission)

---

## Split: agent-executable vs owner-only

### Agent-executable (docs promote)

| Draft | Final action |
|-------|----------------|
| `store/store-page-draft.md` | Publish `store/store-page.md` — strip sprint-prep noise; keep Steam sections; mark **Final for prep (not submitted)** |
| `store/asset-checklist.md` | Verify paths against current manifest; status → Final checklist |
| `store/platform-notes.md` | Confirm target platforms; Final |
| `launch/faq-draft.md` | Publish `launch/faq.md` — player-facing, drop internal GitNexus preambles |
| `launch/support-runbook-draft.md` | Publish `launch/support-runbook.md` — triage steps; contact placeholder until owner fills |
| `launch/patch-notes-template.md` | Already template; mark Final template |
| `launch/evidence-index.md` | Refresh links to current gates (S108/S109 closeouts, suite tip) |
| `release-checklist-v3.md` | Mark executable sections; do not claim Launch complete |
| i18n | Confirm **en-US P0 only** in checklist; no new locale without product decision |

### Owner-only (blocked for agents)

- Store account creation / platform agreement
- Store submission + build upload
- Payment / revenue setup
- Paid marketing

---

## Definition of Done (agent portion)

- [ ] Final files exist alongside or replace drafts with clear Status line
- [ ] No claim of store submission or Launch stage advance
- [ ] Evidence index points at real paths on `main`
- [ ] Linear DRG-39 comment lists remaining owner-only checklist

---

## Sequencing

Prefer after DRG-42 (#405) merge so governance citations are solid, and after DRG-50 lands if evidence-index cites current suite tip.

---

*Agent plan only — does not authorize Launch.*
