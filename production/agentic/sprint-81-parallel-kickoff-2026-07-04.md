# Sprint 81 Parallel Kickoff — Scenario Editor Foundations

**Date:** 2026-07-04  
**Sprint:** S81  
**Authority:** `roadmap-execute-plan-07042026.md`, `scenario-editor-scope-boundary-2026-07-04.md`, `future-sprint-roadpmap-07042026.md`

## Context
S81 is the first sprint of the S81–S88 Scenario Editor program (E11 / req 11, headless first).

**S81-01 boundary** must be published before parallel code/doc tracks.

**Parallel tracks in this sprint (after boundary):**
- S81-02: Branch integration plan (local)
- S81-03: GitNexus re-index + impacts (cloud-style)
- Followed by S81-04 closeout (local coordinator)

## Dispatch Model (superpowers)
- Use `dispatching-parallel-agents` for independent tracks.
- Each agent gets isolated context, specific scope, clear goal, constraints, and required output.
- Work in isolated worktrees where possible: `.worktrees/stack/sprint81/<track-slug>`
- Always run GitNexus preflight (`impact --summary-only`) on ScenarioDocumentEditor, ScenarioValidationEngine, and any §5 CRITICAL symbols.
- Cite the S81-01 boundary + execute plan + roadmap in all artifacts.
- Follow TDD / verification-before-completion for any claims.

## Track Assignments (this sprint)

**S81-02 Branch Integration Plan (Docs)**
- Goal: Document the `fix-scenario-publish-cli-wiring` @ 17d426c stack, Graphite #237, commit-to-track mapping (A–D), submit steps, merge order, and Phase 0 re-verification.
- Constraints: Docs only. Flag the 2 UA failures for S86. No source changes.
- Output: `production/agentic/scenario-editor-branch-integration-plan-2026-07-04.md`
- Owner style: lead-programmer / local

**S81-03 GitNexus Re-index**
- Goal: Run full analyze + status + targeted impacts on CRITICALs and editor symbols. Record stats. Confirm exact counts from plan authoring.
- Commands: see execute-plan §8 Agent C.
- Output: Report in closeout + any appropriate sprint-status note.
- Owner style: c-sharp-devops-engineer / cloud

**S81-04 Closeout (after parallel)**
- Aggregate evidence.
- Drive gt submit / restack.
- Re-run full gates.
- Produce smoke closeout doc.
- Update status artifacts.

## Hard Gates for S81 Close
- Build 0 errors
- Tests ≥1232 floor (excl. known UA pair)
- ReplayGolden 6/6 + C2 proxy 18/18
- Hash `17144800277401907079` preserved
- ZERO DelegationBridge source edits
- GitNexus fresh + impacts match plan §5
- Boundary + roadmap cites present in artifacts
- Stage remains Release

## Communication
- All agents must surface questions early (NEEDS_CONTEXT or BLOCKED).
- Coordinator owns merge order and human gates.
- Do not dispatch S82 tracks until S81 boundary published + branch plan reviewed + user ack + gt submit.

## References
- `roadmap-execute-plan-07042026.md` (full per-sprint details)
- `scenario-editor-scope-boundary-2026-07-04.md`
- `production/agentic/local-cloud-agent-routing.md`
- Graphite workflow in `docs/engineering/graphite-github-substitute-plan.md`

**Kickoff complete.** Boundary published. Parallel agents can now execute S81-02 and S81-03.
