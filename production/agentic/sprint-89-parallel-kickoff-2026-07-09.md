# Sprint 89 Parallel Kickoff — Invariant Floor Docs + UA Engage Hygiene

**Date:** 2026-07-09  
**Sprint:** S89  
**Authority:** [`roadmap-execute-plan-07092026.md`](../../docs/reports/roadmap-execute-plan-07092026.md) §3/§4 (S89), [`post-editor-hygiene-scope-boundary-2026-07-09.md`](../post-editor-hygiene-scope-boundary-2026-07-09.md), [`future-sprint-roadpmap-07092026.md`](../../docs/reports/future-sprint-roadpmap-07092026.md) §3/§6, [`qa-plan-sprint-89-post-editor-hygiene-2026-07-09.md`](../qa/qa-plan-sprint-89-post-editor-hygiene-2026-07-09.md), [`implementation-tracker-2026-07-04.md`](../../Game-Requirements/implementation-tracker-2026-07-04.md), AGENTS.md + CLAUDE.md

## Context

Post-editor immediate priorities **COMPLETE** (GitNexus fresh @ `223a5fe`; gates 1599/0f, 6/6, 20/20). User approved **S89–S92 roadmap** 2026-07-09. S89 is sprint 1: invariant floor honesty + UA engage hygiene.

**S89 focuses on (2 parallel cloud tracks + local closeout):**
- AGENTS/tracker floor bump to **≥1599 / 20/20** (S89-01)
- UA engage test triage/document (S89-02)
- Local closeout + smoke (S89-03)

## GitNexus status (pre-kickoff)

| Item | Value |
|------|-------|
| Indexed commit | `223a5fe` — ✅ fresh |
| Nodes / edges | 24,418 / 47,032 |
| ScenarioDocumentEditor | **233 CRITICAL** |
| CatalogWriteGate | **186 CRITICAL** |
| DelegationBridge | **145 CRITICAL** |
| PatrolCandidateEngagePolicy | **113 CRITICAL** |
| BalticReplayHarness | **54 CRITICAL** |

## Baseline @ kickoff (RUN+READ verified)

Evidence: [`production/qa/evidence/gates-post-editor-hygiene-2026-07-09.log`](../qa/evidence/gates-post-editor-hygiene-2026-07-09.log)

- Build: **0e/0w**
- Full sln: **1599/0f**
- ReplayGolden: **6/6**
- C2 proxy: **20/20**
- Hash: **18** paths
- UA engage filter: **3/3**
- ZERO DelegationBridge changes in hygiene program

## Dispatch Model

- Use `dispatching-parallel-agents` skill for S89-01 and S89-02.
- **Isolated worktrees:** `.worktrees/stack/sprint89/floor-docs`, `/ua-engage`
- **Mandatory per track (before any symbol edit):**
  - GitNexus: `node .gitnexus/run.cjs status` + `impact <symbol> --direction upstream --summary-only`
  - Cite **boundary** + execute-plan + qa-plan + sprint-89 plan in every artifact and commit
- TDD only if S89-02 requires code fix; otherwise docs-only + gate re-run
- Use `gt` for stack work (no raw `git push` or `gh pr create`)

## Track Assignments

### S89-01 Floor doc sync (Cloud, c-sharp-devops-engineer)

**Goal:** Update agent-facing docs to post-PE floors.

**Primary files:**
- `AGENTS.md` (Build & Test, Hard Invariants, Cloud Agent, Learned Workspace Facts)
- `CLAUDE.md` (if gate floors cited)
- `Game-Requirements/implementation-tracker-2026-07-04.md` (UA req 14 note if disposition changes)
- `.cursor/cloud-install.sh` references if stale

**Constraints:** Docs-only; no sim code; grep for stale `1232` / `18/18` after edit.

**Output:** PR stack `stack/sprint89/floor-docs` + changelog in closeout.

### S89-02 UA engage hygiene (Cloud, team-simulation)

**Goal:** Document or fix `BalticReplayHarnessPolicyEngageTests` status.

**Primary files:**
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessPolicyEngageTests.cs` (only if fix required)
- `production/qa/` triage note or update to tracker req 14

**Constraints:**
- GitNexus impact on `BalticReplayHarness` (CRITICAL 54) before any edit
- Do not alter replay golden hash
- Preserve 6/6 Replay + 20/20 C2
- No `DelegationBridge` edits

**Output:** Triage report + green tests or documented waive path.

### S89-03 Closeout (Local, producer)

**Goal:** Merge tracks, re-run gates, publish smoke closeout.

**Commands:** See qa-plan standing gates + `roadmap-execute-plan-07092026.md` §6 merge protocol.

**Output:** `production/qa/smoke-sprint-89-closeout-2026-07-*.md`, sprint-status update, optional GitNexus re-analyze.

## Wave Schedule

| Day | Action |
|-----|--------|
| 1 | Phase 0 baseline confirm; dispatch S89-01 + S89-02 |
| 2–3 | Track execution; mid-sprint gate spot-check |
| 4–5 | S89-03 closeout; user ack; prep S90 |

---
*Kickoff for S89. Cite boundary + execute-plan + qa-plan on every track.*
