# Requirements Corpus Maturity — Wave 1 Implementation Plan

> **For agentic workers:** Use parallel track implementers (W1-a…d), then W1-e, then closeout. Docs-only. No commits without user instruction.

**Goal:** Locked Template A honesty for docs 02–08 + glossary 12; standard depth; one story five tracks.

**Architecture:** See approved design §5 Wave 1 and session plan. Preserve Resolved Design Decisions. Mapping tables + tracker footers + FR reverse-refs. Hub FR-04 residual fix.

**Tech Stack:** Markdown only.

**Story:** `production/epics/requirements-corpus-maturity/story-002-template-a-honesty.md`

## Tracks

| Track | Docs | Parallel? |
|-------|------|-----------|
| W1-a | 02, 03 | Yes |
| W1-b | 04, 05 | Yes |
| W1-c | 06 | Yes |
| W1-d | 07, 08 | Yes |
| W1-e | 12 | After a–d |
| Closeout | 01 FR-04, tracker, QA memo, story complete | Serial |

## Shared footer

```markdown
---
**Implementation grade:** <Partial|Partial+> — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row NN. Design Status remains **Locked**. Charter re-honesty: Wave 1 2026-07-08.
```

## Shared mapping columns

`Area | Path / type | Status (Shipped|Partial|Gap|Phase N) | Evidence`

## Exit verify

```bash
rg -n '## Implementation Mapping' Game-Requirements/requirements/0{2,3,4,5,6,7,8}-*.md
rg -n 'FR-0[1-7]|re-honesty: Wave 1' Game-Requirements/requirements/0{2,3,4,5,6,7,8}-*.md Game-Requirements/requirements/12-Terms-Glossary.md
rg -n 'FR-04' Game-Requirements/requirements/01-Project-Overview.md
git diff --name-only | rg '\.cs$|tests/regression|DelegationBridge' || true
```

Full per-track edit specs: approved Wave 1 plan (session) + design §5.
