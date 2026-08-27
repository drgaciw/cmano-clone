# QA Skills — Parallel Task Contract + Phase-Static Model Tiers

**Date:** 2026-07-23  
**Authority:** Improve QA Skills plan; [`.claude/docs/coordination-rules.md`](../../.claude/docs/coordination-rules.md) Model Tier Assignment  
**Aliases only:** `haiku` / `sonnet` / `opus` — never version pins (e.g. `sonnet-4.5`) in skills. Canonical dated IDs live only in coordination-rules.

## Parallel Task contract

1. **Independent domain** = disjoint write paths and no shared mutable counters (BUG-NNN, RUN_ID).
2. **Self-contained Task prompt:** scope / goal / constraints / return summary.
3. **One-turn dispatch:** issue all independent Task calls in a single orchestrator turn before waiting.
4. **Gauntlet sim-code fixes** with disjoint GitNexus blast radii → parallel `.worktrees/`; **serial merge** onto QA branch + re-verify after each merge.
5. **team-qa Phase 4:** one Task per story path; assign `BUG-[NNN]` IDs only after collecting results.
6. Surface **BLOCKED** immediately; always produce a partial report.

## Phase→model: team-qa

| Phase | Work | Task model |
|-------|------|------------|
| 1 | Context / digests | `haiku` |
| 2–3 | Strategy + plan | `sonnet` (qa-lead) |
| 4 | Bulk test-case scaffolding | `haiku`, one Task per story, all in one turn |
| 5 | Manual QA digests | `haiku`; bug-report draft `sonnet` |
| 6 | Sign-off | `sonnet`; `opus` only if NOT APPROVED + multi-doc / S1 gate |

Orchestrator skill frontmatter stays `model: sonnet`.

## Phase→model: qa-gauntlet + qa-gauntlet-forge

| Phase | Model | Notes |
|-------|-------|-------|
| Preflight / forge `pre` / scorecard plumbing / expect CSV digest | `haiku` or **no LLM** (script) | Script-first |
| A0 roster digest | `haiku` | |
| A1 / forge `a0` draft | `sonnet` | architect |
| B batch + C oracle CLI | **tools only** | Then `haiku` to summarize exit codes |
| D TDD Red/Green | `sonnet` | `opus` if CRITICAL / quarantine synthesis |
| forge `post-oracle` promote judgment | `sonnet` after script scorecard | Never override hard gates |
| E / forge `e` | `haiku` | |
| Final AAR distill | `haiku` → `sonnet` prose | `opus` for stuck / multi-tier conflict |

## Script-first (gauntlet/forge)

Prefer `gauntlet_oracle_eval` and `python3 tools/qa-gauntlet/forge_scorecard.py` over LLM reinterpretation. Class `oracle` → expect-regen runbook only (no hand-edit envelopes).

## `/team-qa-gauntlet` specialist modes (DRG-199)

Coordinator only — runbooks live in the named skills. Do not dual-write Combat UX Slice B (DRG-165–170).

| `--mode` | Skill |
|----------|--------|
| `full` | ladder + forge hooks; **mission-thread** if T3+; **agentic-resilience** on Phase D/AAR; stress when claimed |
| `mission-thread` | `/qa-gauntlet-mission-thread` |
| `agentic-resilience` | `/qa-gauntlet-agentic-resilience` |
| `combat-ui` | `/qa-gauntlet-combat-ui` (not `/qa-gauntlet-ui`, not Slice B) |
| `ui` / `ui-smoke` | `/qa-gauntlet-ui` |
