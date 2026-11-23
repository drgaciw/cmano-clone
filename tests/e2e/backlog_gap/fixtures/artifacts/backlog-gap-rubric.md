# Project Aegis Gap Analysis Rubric (Fixture v1)

## 1. Classification States
- **complete**: Mechanical pre-gates pass (matched Linear + Notion records with valid `Source:`, testable acceptance criteria, bidirectional links, and governance labels intact) **and** Jev `noul >= 0.80`.
- **incomplete**: Matched record exists in Linear or Notion, but lacks required `Source:` git path, acceptance criteria, cross-link, or fails Jev completeness (`noul < 0.80`).
- **missing**: No matching requirement record exists in the target backlog.

## 2. Script-First Priority & Jev Thresholds
- **Script-first / deterministic pre-gate**: Mechanical checks execute before Jev. A failed mechanical gate (`linear_matches == []` or `notion_matches == []` or missing required fields) is **never overridden** by a high Jev `noul` score.
- **`noul >= 0.80`**: Required pass floor for `complete`.
- **`choice` confidence `< 0.50`**: Flags `needs_human_review = true`; do not auto-decide.
- **`score` rank rule**: Discrete rank/threshold bands only; never treat interpolated scores as linear multipliers.

## 3. M1 Open Scope Decisions
- **Notion target surface**: `Tasks` DB only (`Project = Project Aegis`); `Frozen Git mirror` remains read-only.
- **Phase N & Draft items**: Deferred Phase N items (`DRG-47`, `SWARM-27..30`, `REQ-10b`) and `draft` items (`23..27`, `CMD-38/39`, `SYM-*`) are tracked in `Backlog` / `Not started` and never promoted to `Done`.
- **Exclusions**: `OPEN-CRM-CPQ` (`P-DRG-6`, `tests/uat/*.md`) and `docs/reference/cmano-db/*.md` are strictly excluded.
