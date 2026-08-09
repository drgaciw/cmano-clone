# Waterline stale In Progress reconcile — 2026-08-09

**Lane:** W4  
**Scope:** Linear issues stuck In Progress since ~2026-07-24 with no recent movement  
**Method:** Read issue AC → search main for evidence (`production/determinism`, `production/qa`, ReplayGolden / DecisionLog paths) → `gh pr view` for PR mirrors → update Linear status with evidence notes  
**Constraint:** No feature work; docs/Linear bookkeeping only  
**HEAD reference:** `origin/main` @ reconcile time (post #438 swarm closeout)

## Verdict matrix

| Issue | Title | Prior Linear | Verdict | New Linear | Evidence (main / PR) |
|-------|-------|--------------|---------|------------|----------------------|
| **DRG-18** | [S36-01] Determinism Audit P1 Follow-up | In Progress | **Done** — AC met on main | Done | `production/determinism/determinism-audit-2026-06-19.md` (P1 findings = 0; hash immutable; GitNexus notes). Superseded re-audit `determinism-audit-2026-06-20.md` (S41-04). Gate still green: `replay-2026-07-27.md`. |
| **DRG-27** | [S36-05] Replay Golden + Harness Maintenance | In Progress | **Done** — AC met on main | Done | `production/determinism/replay-2026-06-19.md` (S36-05: 6/6 A=B=golden; DiagnoseDivergence). Code: `BalticReplayHarness.DiagnoseDivergence` (`BalticReplayHarness.cs:74`). Later: `replay-2026-07-27.md` 17/17 PASS. Goldens under `tests/regression/replay-golden-*.txt`. |
| **DRG-29** | [S36-10] DecisionLog Immutable Hash + P1 Polish | In Progress | **Done** — AC met on main | Done | S36-01 audit cleared DecisionLog paths. Hot path: `DecisionLog.ComputeFingerprint` = ordered `foreach` over `_chronological` (no OrderBy). Tests: `OrderLogReplayFingerprintSha256Tests`, `ReplayOrderLogFingerprintTests`, `OrderLogFingerprintTests`, `DecisionLogTests`. Later gates green (S41-04, 2026-07-27). |
| **DRG-30** | [S36-15] Datalink Merger GitNexus Planning + Re-audit | In Progress | **Done** — planning artifact on main | Done | `production/determinism/datalink-gitnexus-plan-2026-06-19.md` (GitNexus callers/impact, sort contracts, no code edits, ZERO DelegationBridge). Datalink goldens: `tests/regression/replay-golden-baltic-datalink-*.txt`. Related residual gap tracked as DRG-43 (not this story). |
| **DRG-32** | [PR #337] gauntlet denser catalog ORBATs | In Progress | **Done** — PR merged | Done | [PR #337](https://github.com/drgaciw/cmano-clone/pull/337) **MERGED** 2026-07-27. Main commit `ac87c756` (#337). |
| **DRG-34** | [PR #334] Linux vs Windows build gap | In Progress | **Done** — PR merged | Done | [PR #334](https://github.com/drgaciw/cmano-clone/pull/334) **MERGED** 2026-08-04. Artifacts: `docs/reports/linux-vs-windows-build-gap-analysis-2026-07-20.md` (+ `.html`). |
| **DRG-35** | [PR #324] S93 binary assets | In Progress | **Blocked** — open CONFLICTING PR | In Progress *(note)* | [PR #324](https://github.com/drgaciw/cmano-clone/pull/324) **OPEN**, `mergeable: CONFLICTING`. CI green ~2026-08-08 but cannot land. No Linear “Blocked” state → left In Progress with blocker note. Next: rebase/resolve or supersede (PR #340 restored some post-S93 governance). |

**Summary counts:** Done **6** / Blocked (In Progress) **1** / Backlog residual **0**.

## Per-issue detail

### DRG-18 — Done

- Story: `production/epics/sprint-36-perf-determinism/story-036-01-determinism-audit-p1.md` (frontmatter still `status: In Progress` — bookkeeping only).
- Primary evidence report closes AC explicitly (“AC status for S36-01”, no CRITICAL findings).
- Later S41-04 full-boundary audit reconfirmed DETERMINISTIC SAFE without reopening P1 remediation.

### DRG-27 — Done

- Story: `story-036-05-replay-golden-maintenance.md` (frontmatter stale In Progress).
- Harness polish shipped: `DiagnoseDivergence` on main; production 6/6 goldens documented bit-identical.
- No residual harness maintenance beyond ordinary CI `/replay-verify` discipline.

### DRG-29 — Done

- Story: `story-036-10-decisionlog-hash-polish.md` (frontmatter stale In Progress).
- No structural DecisionLog rewrite required after S35-10 + S36-01 clearance; immutability asserts and stable SHA tests already on main.
- Residual engineering: **none**. Optional hygiene: flip story frontmatter to Complete in a later docs pass (out of scope for this lane).

### DRG-30 — Done

- Story: `story-036-15-datalink-gitnexus-plan.md` (frontmatter stale In Progress).
- Planning-only AC fully satisfied by `datalink-gitnexus-plan-2026-06-19.md`.
- Product residual for datalink *features* (if any) lives under separate issues (e.g. DRG-43), not this planning ticket.

### DRG-32 — Done

- PR mirror issue; work landed via merge of #337 (denser catalog ORBATs + ladder expect TDD calibration).

### DRG-34 — Done

- PR mirror issue; work landed via merge of #334 (Linux vs Windows build gap analysis docs).

### DRG-35 — Blocked (In Progress)

- PR mirror issue still tracks live work.
- **Blocker:** #324 cannot merge (`CONFLICTING` with main).
- Related: #340 (`docs: restore four post-S93 governance artifacts to main`) already merged — may reduce remaining unique content of #324; owner should diff before large conflict resolution.

## Linear updates applied (2026-08-09)

| Issue | Action |
|-------|--------|
| DRG-18 | → **Done** + evidence note in description |
| DRG-27 | → **Done** + evidence note |
| DRG-29 | → **Done** + evidence note |
| DRG-30 | → **Done** + evidence note |
| DRG-32 | → **Done** + PR merge evidence |
| DRG-34 | → **Done** + PR merge evidence |
| DRG-35 | kept **In Progress** + CONFLICTING blocker note |

## Out of scope / follow-ups (not done this lane)

1. Flip epic story YAML frontmatter `status: In Progress` → `Complete` for S36-01/05/10/15 (repo bookkeeping; optional).
2. Resolve or close PR #324 / DRG-35 (human or dedicated land lane).
3. No ReplayGolden / DecisionLog / Datalink code changes.

## Commands used

```bash
gh pr view 337 --json state,mergedAt,url
gh pr view 334 --json state,mergedAt,url
gh pr view 324 --json state,mergeable,url,updatedAt
# Linear: linear___get_issue + linear___save_issue for DRG-18,27,29,30,32,34,35
```

## Sign-off

Lane W4 complete: waterline stale In Progress set reconciled with main evidence; 6 closed, 1 explicitly blocked by open conflicting PR.
