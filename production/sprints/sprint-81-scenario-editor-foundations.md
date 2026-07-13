# Sprint 81 — Scenario Editor Foundations

**Dates:** 2026-07-04 start (est. 5–7 days)  
**Lead:** E11 / producer + c-sharp-devops-engineer  
**Program:** S81–S88 Scenario Editor (req 11)  
**Authority:** `roadmap-execute-plan-07042026.md` §3/§4 (S81), `future-sprint-roadpmap-07042026.md`, `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md`, `implementation-tracker-2026-07-04.md`

## Sprint Goal
Publish scope boundary, produce branch integration plan for the in-flight `fix-scenario-publish-cli-wiring` stack, re-index GitNexus with editor symbols, and close the sprint with clean baseline verification. Enable safe merge of validation tracks A–D and set up for S82+ parallel work.

## Tracks (parallel after S81-01)

| Track | Owner | Worktree / Stack | Key Deliverable | Type |
|-------|-------|------------------|-----------------|------|
| S81-01 Scope boundary | producer | local | `production/scenario-editor-scope-boundary-2026-07-04.md` | Docs |
| S81-02 Branch integration plan | lead-programmer | local | `production/agentic/scenario-editor-branch-integration-plan-2026-07-04.md` | Docs |
| S81-03 GitNexus re-index | c-sharp-devops-engineer | cloud | Updated index + impact reports on CRITICALs + editor symbols | Tooling |
| S81-04 Closeout | c-sharp-devops-engineer | local | Closeout doc, gt submit plan, gate re-run | Coord |

**Wave order:** S81-01 (day 1) → (S81-02 ∥ S81-03) → S81-04

## Key Artifacts & Commands

**Mandatory baseline (Phase 0) before/after any merge:**
See `roadmap-execute-plan-07042026.md` §5 for the full script (GitNexus status/impacts, dotnet build, targeted tests, ReplayGolden 6/6, PlayMode 18/18, hash grep, smoke-ac6 when touched).

**In-flight branch:** `fix-scenario-publish-cli-wiring` @ `17d426c` (Graphite PR #237)

**Post-boundary submit (user):** See full exact block + verification-before + post-merge Phase 0 (incl. impacts on ScenarioDocumentEditor/ValidationEngine/Catalog/Delegation/Baltic + hash/smoke-ac6 + current state) in `production/agentic/scenario-editor-branch-integration-plan-2026-07-04.md` (Graphite/Stack Workflow section) + smoke closeout. Short form:

```bash
git checkout fix-scenario-publish-cli-wiring
# run verification-before (GitNexus + full gates per AGENTS + execute-plan §5)
gt submit --stack --no-interactive
# post-merge on main:
git checkout main; gt sync; gt restack
# re-run full Phase 0
```

Cites: scenario-editor-scope-boundary-2026-07-04.md + roadmap-execute-plan-07042026.md + graphite plan.

## Acceptance Criteria for Sprint
- [x] Boundary published with full cites, in/out scope, invariants, ownership matrix.
- [x] Branch integration plan with commit inventory, track A–D mapping, gt commands, risks (esp. 2 UA failures for S86).
- [x] GitNexus re-index + impacts: CatalogWriteGate 178, Patrol 97, DelegationBridge 127, Baltic 52 (CRITICAL); Editor 20 CRITICAL / 17 HIGH.
- [x] Baseline: build clean, tests 1308/2 (known UA only), Replay 6/6, C2 18/18, hash preserved, ZERO bridge edits.
- [x] Closeout doc + sprint artifacts produced.
- [ ] User ack + gt submit + restack (pending human action).

## Standing Rules (from boundary)
- Stage = Release throughout.
- Single owner per sprint for ScenarioDocumentEditor.
- GitNexus impact preflight on CRITICALs + hot symbols before code work.
- No edits to DelegationBridge, extend-only on CatalogWriteGate.
- Baltic corpora + hash frozen.

## Risks / Notes
- 2 pre-existing UA failures owned by S86.
- In-flight stack +9 ahead of remote at authoring time — re-submit after ack.
- Use worktrees for future tracks: `.worktrees/stack/sprint81/{slug}`

**Next after closeout:** User ack → S82 dispatch (validation tracks A+C).

---
*Created as part of S81 dispatch using superpowers patterns.*
