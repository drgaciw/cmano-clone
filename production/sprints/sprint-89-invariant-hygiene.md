# Sprint 89 — Post-Editor Invariant + UA Hygiene

**Dates:** 2026-07-09 start (est. 3–5 days)  
**Lead:** c-sharp-devops-engineer / producer (local closeout)  
**Program:** S89–S92 Post-Editor Engineering Hygiene + Asset Spec Production — sprint 1 of 4  
**Authority:** [`future-sprint-roadpmap-07092026.md`](../../docs/reports/future-sprint-roadpmap-07092026.md) §3/§6, [`roadmap-execute-plan-07092026.md`](../../docs/reports/roadmap-execute-plan-07092026.md) §3/§4 (S89), [`post-editor-hygiene-scope-boundary-2026-07-09.md`](../post-editor-hygiene-scope-boundary-2026-07-09.md), [`qa-plan-sprint-89-post-editor-hygiene-2026-07-09.md`](../qa/qa-plan-sprint-89-post-editor-hygiene-2026-07-09.md), [`implementation-tracker-2026-07-04.md`](../../Game-Requirements/implementation-tracker-2026-07-04.md), AGENTS.md, this file.

**Roadmap approval:** User approved S89–S92 roadmap **2026-07-09**.

## Sprint Goal

Bump agent/tracker documentation to honest post–Platform Editor floors (**≥1599 / 20/20 / 6/6**), document UA engage-test hygiene status (fix/waive/exclude with evidence), and close S89 with full gate re-verification. Stage stays **Release**. No code changes to `DelegationBridge` or `CatalogWriteGate` write paths unless UA fix is strictly required and impact-approved.

## Capacity

| Dimension | Value |
|-----------|-------|
| Total days | 5 |
| Buffer (20%) | 1 day |
| Available | 4 days |
| Parallel tracks | 3 (2 cloud + 1 local closeout) |

## Tracks (parallel after baseline)

| Track | Stack prefix | Worktree | Env | Story | Owner |
|-------|--------------|----------|-----|-------|-------|
| Floor doc sync | `stack/sprint89/floor-docs` | `.worktrees/stack/sprint89/floor-docs` | **Cloud** | S89-01 | c-sharp-devops-engineer |
| UA engage hygiene | `stack/sprint89/ua-engage` | `.worktrees/stack/sprint89/ua-engage` | **Cloud** | S89-02 | team-simulation |
| Closeout | `stack/sprint89/closeout` | `.worktrees/stack/sprint89/closeout` | **Local** | S89-03 | producer |

**Wave order:** Phase 0 baseline (day 1) → (S89-01 ∥ S89-02) → S89-03 closeout

**Kickoff:** [`production/agentic/sprint-89-parallel-kickoff-2026-07-09.md`](../agentic/sprint-89-parallel-kickoff-2026-07-09.md)

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S89-01 | Floor doc sync — AGENTS.md, CLAUDE.md, implementation-tracker, Cloud/Cursor instructions | c-sharp-devops-engineer | 1.5 | Phase 0 | All cited docs show **≥1599** test floor, **20/20** C2, **6/6** Replay; remove stale **1232/18/18** where they gate current work; no contradictory UA exclusion text if engage filter is green |
| S89-02 | UA engage hygiene — triage/document `BalticReplayHarnessPolicyEngageTests` | team-simulation | 2 | Phase 0 | Triage report with RUN+READ evidence; if green (3/3): document residual history + gate policy; if regress: fix OR explicit waive with user ack path; hash + Replay preserved |
| S89-03 | S89 closeout — gates, smoke doc, sprint-status, gt submit | producer | 1.5 | S89-01, S89-02 | `smoke-sprint-89-closeout-2026-07-*.md`; gates log; sprint-status updated; all tracks merged via `gt` |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S89-04 | GitNexus freshness note in AGENTS banner | c-sharp-devops-engineer | 0.5 | S89-01 | Post-closeout analyze if stale; document `node .gitnexus/run.cjs analyze` cadence in agentic routing doc |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S89-05 | Dashboard addendum cross-link in project-dashboard | producer | 0.25 | S89-03 | `project-dashboard.md` links S89 closeout |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|--------------|
| UA engage test residuals (tracker req 14) | Historically 2 failures; currently 3/3 green at PE gate | Owned by S89-02 (document or fix) |
| AGENTS floor honesty (1232→1599) | Post-PE baseline not reflected in all agent docs | Owned by S89-01 |

## Hard Gates (S89)

| Gate | Command / check | Pass criterion |
|------|-----------------|---------------|
| Build | `dotnet build ProjectAegis.sln` | 0 errors |
| Tests | `dotnet test ProjectAegis.sln -v minimal` | **≥1599 / 0 failed** |
| Replay | `--filter ReplayGoldenSuiteTests` | **6/6** |
| C2 | `--filter PlayModeSmokeHarnessTests` | **≥20/20** |
| Hash | `rg -l '17144800277401907079' tests/ data/` | 18 paths |
| Bridge | No `DelegationBridge.cs` hotpath edits | **ZERO** |
| Catalog | `CatalogWriteGate` | **extend-only** |
| GitNexus | `node .gitnexus/run.cjs status` | fresh @ HEAD or re-analyze post-merge |

**GitNexus watchlist (pre-edit):** ScenarioDocumentEditor **233**, CatalogWriteGate **186**, DelegationBridge **145**, PatrolCandidateEngagePolicy **113**, BalticReplayHarness **54** — all CRITICAL.

**Phase 0 evidence (pre-S89):** [`production/qa/evidence/gates-post-editor-hygiene-2026-07-09.log`](../qa/evidence/gates-post-editor-hygiene-2026-07-09.log)

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| UA engage tests regress during S89 | Low | Medium | Run engage filter on every track close; no harness changes without Baltic impact preflight |
| Doc-only PR touches wrong symbols | Low | Low | GitNexus detect_changes on staged files; docs-only stacks |
| Stale agent docs mislead cloud agents | Medium | High | S89-01 is must-have; cite boundary in every commit |
| Accidental Bridge/Catalog edit | Low | Critical | ZERO bridge policy; extend-only catalog; impact pre mandatory |

## Dependencies on External Factors

- User roadmap approval — **received 2026-07-09**
- Graphite `gt` stack workflow for parallel tracks
- No Unity Editor required (headless gates only)

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [x] QA plan exists — [`production/qa/qa-plan-sprint-89-post-editor-hygiene-2026-07-09.md`](../qa/qa-plan-sprint-89-post-editor-hygiene-2026-07-09.md)
- [ ] Standing gates RUN+READ at closeout (≥1599/0f, 6/6, 20/20, hash, ZERO bridge)
- [ ] Smoke closeout doc published
- [ ] sprint-status.yaml updated (S89 COMPLETE block)
- [ ] No S1 or S2 bugs introduced
- [ ] Stage remains **Release**
- [ ] Ready for S90 dispatch (`/sprint-plan new` not required — plan exists in roadmap)

## Standing Rules (from boundary)

- Stage = **Release** throughout S89–S92.
- GitNexus impact preflight on §5 CRITICALs before any symbol edit.
- No edits to `DelegationBridge` hotpath; extend-only on `CatalogWriteGate`.
- Baltic corpora + hash frozen (`17144800277401907079`).
- Docs-first in S89; code changes only for UA fix if strictly necessary.

## Next

S89-01 + S89-02 parallel dispatch → S89-03 closeout → user ack → S90 agent/skill P0 sync per roadmap.

---
*Created via `/sprint-plan new` after S89–S92 roadmap approval (2026-07-09). Review mode: lean (PR-SPRINT skipped). Graphite-first. Superpowers. All gates RUN+READ.*
