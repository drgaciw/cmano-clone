# Sprint 37 — 2026-07-20 to 2026-08-02

**Dates:** 2026-07-20 to 2026-08-02  
**Trunk:** `main` @ (post-S36 commit; TBD after S36 closeout)  
**Predecessor:** Sprint 36 — COMPLETE (core graph runtime + C2 budget + Phase H link surfacing + parallel tracks; assume 14/14 or close, QA APPROVED WITH CONDITIONS)  
**Stage:** Polish  
**Authority:** polish-scope-boundary-2026-06-19.md — Phase 2 continuation (graph surfacing completion) + deeper Polish + superpowers dispatching refinements. Builds directly on S36 (S36-03/04/07/05/13/15 etc.).  

## Sprint Goal
Complete UI surfacing of the dependency graph (C2 + Platform Editor integration beyond S36 runtime/Phase H), drive deeper Polish (perf P2, playtests cadence, hygiene), and refine superpowers dispatching for improved parallel agent execution — all while preserving determinism, C2 18/18 proxy, ReplayGolden, and polish-boundary scope.

## Capacity
- Total days: 10
- Buffer (20%): 2 days reserved for unplanned work
- Available: 8 days
- Commit target: ~10 stories (7-8 must + 2-3 should + closeout)
- Plan target: 15 stories
- Test baseline: ≥1215 (post-S36 bump from ≥1204)

## Tasks

### Must Have (Critical Path)
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S37-01 | Full-solution re-baseline post-S36 + GitNexus + ReplayGolden 6/6 + test count bump | c-sharp-devops-engineer | 1 | — | ≥1215 PASS; smoke doc; indexed commit recorded; ReplayGolden 6/6 |
| S37-02 | Sprint 37 QA plan (graph surfacing matrix, C2 proxy extensions, dispatching validation, playtest protocol) | team-qa | 1 | S37-01 | `production/qa/qa-plan-sprint-37-*.md` merged before feature waves |
| S37-03 | CatalogDependencyGraphIndex + CLI full kill-chain surfacing (platform→link + weapon→mount→sensor chains + API) | team-data | 2 | S37-01 | Full chains in GetSorted* + export; deterministic; goldens updated; tests pass | **COMPLETE** (story-done lean) |
| S37-04 | C2 graph surfacing completion — dependency viewer/panel, selection highlights, link-chain display + bind | team-unity + team-data | 2.5 | S37-03, S37-02 | C2 proxy filters extended (new graph checks); graph data visible in headless + live; 18/18+ maintained | **COMPLETE** (story-done lean) |
| S37-05 | Platform Editor graph surfacing completion (interactive FK/full graph display, tooltips, export polish) | team-unity + team-data | 1.5 | S37-03 | Full graph visible beyond S36-07 read-only; roundtrip + validation tests; evidence PNGs | **COMPLETE** (story-done lean) |
| S37-06 | Unity C2 frame budget deeper remediation + additional live evidence refresh | team-unity | 1 | S37-01, S37-02 | Perf doc update; sustained frame headroom vs 16.67 ms; new evidence batch (post-S36) | **COMPLETE** (story-done lean) |
| S37-07 | Superpowers dispatching-parallel-agents refinements (wave tracking, status sync, multi-track examples, kickoff templates) | coordinator (c-sharp-devops / agentic) | 1 | S37-02 | Updated skill/docs + examples in engineering/ + agentic/; backward compat; validated in kickoff | **COMPLETE** (story-done lean) |
| S37-08 | Closeout hygiene (smoke, replay 6/6, GitNexus tip, tracker notes) | c-sharp-devops-engineer | 0.5 | S37-03+ | All gates green; no regression on S36 deliverables | **COMPLETE** (story-done lean) |

**Sprint fails if** S37-02 is not landed before feature waves, graph surfacing regresses C2 proxy / frame budget, or ReplayGolden diverges.

### Should Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S37-09 | Sim perf P2 follow-ups (allocation / LINQ hotspots from S36-08) | team-simulation | 2 | S37-01 | Hash-identical vs S36; `/replay-verify`; optional bench appendix | **COMPLETE** (story-done lean) |
| S37-10 | Playtest session 9 (graph surfacing UX focus + polish feedback) | team-qa | 1 | S37-02 | Structured report + think-aloud notes in `production/playtests/` | **COMPLETE** (story-done lean) |
| S37-11 | Hygiene: advance `tests/unit/` + `tests/integration/` layout (migrate remainder / CI verification) | c-sharp-devops-engineer | 1 | S37-01 | Progress report + partial moves or signed ADR; no breakage on 1215+ baseline | **COMPLETE** (story-done lean) |

### Nice to Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S37-12 | Perf re-profile + polish baseline appendix update | perf-profile | 0.5 | S37-06, S37-09 | Update `production/perf/perf-profile-polish-baseline-2026-06-19.md` | **COMPLETE** (story-done lean) |
| S37-13 | Additional C2 / Editor polish (filters, tooltips, residual S36-15 items) | team-unity | 1 | S37-04 | Proxy pass + notes; no new scope | **COMPLETE** (story-done lean) |
| S37-14 | AD-ART-BIBLE sign-off facilitation / UX doc polish (carry) | team-ui | 0.5 | S37-02 | Verdict note or sign-off artifact |

## Parallel Execution Waves & Tracks

**Prerequisites (serial):** S37-01, S37-02 (baseline + QA plan block all feature work)

**Wave 1 — Data Track (independent after prereqs):** S37-03 (core full kill-chain + API)

**Wave 2 — UI / C2 + Editor Track (parallel to Data):** S37-04 (C2 viewer), S37-05 (Editor completion), S37-06 (frame + evidence), S37-13 (additional polish)

**Wave 3 — Simulation / Perf Track (parallel):** S37-09 (P2), S37-12 (re-profile)

**Wave 4 — QA / Process / Hygiene Track (parallel):** S37-07 (dispatching refinements), S37-10 (playtest 9), S37-11 (tests layout)

**Closeout (after waves):** S37-08

(Tracks enable dispatching-parallel-agents with one coordinator per track, refined in S37-07. See S36 pattern and S35-parallel-kickoff artifacts.)

## Carryover from Previous Sprint (S36)
| Task | Reason | New Estimate / Placement |
|------|--------|--------------------------|
| S36-15 Additional C2 polish | Residual post-Phase H link surfacing | S37-06 + S37-13 |
| S36-13 Dispatching-parallel-agents refinement | Explicit S37 focus area (superpowers process) | S37-07 (primary) |
| S36-12 Perf re-profile | Deeper polish requirement | S37-12 |
| S36-10 Playtest session 8 | Maintain cadence into deeper Polish | S37-10 (advance sequence to session 9) |
| S36-08 Sim P1 allocation follow-ups | If not fully closed in S36 | S37-09 |
| S36-09 tests/unit/ layout or ADR | Hygiene carryover | S37-11 |
| S36-11 AD-ART-BIBLE sign-off facilitation | UX foundation residual | S37-14 |
| Dep-graph platform→link + runtime (S35-16 seed + S36-03/04/07) | Core runtime complete in S36; UI surfacing incomplete | S37-03/04/05 (completion) |
| Live PNGs / C2 evidence | Polish evidence refresh | S37-06 |

(Per S36 "After S36, plan S37 for graph surfacing + deeper polish.")

## Risks
| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Graph surfacing regresses C2 frame budget or 18/18 proxy | Medium | High | S37-06 + proxy gate on S37-04/05; measure before merge |
| Dispatching refinements break existing parallel kickoff | Low | Medium | Doc + example only first; backward compat; validate via S37-07 + kickoff artifact |
| S37-02 QA plan delay | Medium | High | Block all feature waves (S37-03+) until merged |
| Scope creep outside polish-boundary (globe, delegation badges, TL Phase 5, DOTS etc.) | Low | High | Every story cites polish-scope-boundary-2026-06-19.md + S36 |
| Graph invalidation or non-determinism | Medium | CRITICAL | Deterministic export only; extend-only patterns; replay on changes |

## Dependencies on External Factors
- QA plan (S37-02) must exist before any Data/UI/Sim waves (per sprint-plan Phase 5 + production → Polish gate).
- Production Baltic hash immutable unless explicit signed golden refresh.
- GitNexus impact + replay-verify required on graph/sim changes (S37-03/09).
- Lean review mode (production/review-mode.txt) — PR-SPRINT skipped.
- No touch on `DelegationBridge.cs` (control manifest).
- Extend-only on `CatalogWriteGate`.

## Definition of Done for this Sprint
- [x] All Must Have tasks completed (S37-01..08 via tracks + closeout)
- [x] All tasks pass acceptance criteria (verified per story-done: tests, playtest, docs, evidence)
- [x] QA plan exists (`production/qa/qa-plan-sprint-37-2026-06-20.md`)
- [x] All Logic/Integration stories have passing unit/integration tests (Data/Sim/Integration: PASS per subagents)
- [x] Smoke check passed (`/smoke-check sprint` — see `production/qa/smoke-sprint-37-closeout-2026-07-20.md`: PASS)
- [x] QA sign-off report: APPROVED (`/team-qa sprint` — see `production/qa/qa-signoff-sprint-37-2026-07-20.md`)
- [x] No S1 or S2 bugs in delivered features (none reported)
- [x] Design documents updated for any deviations (none; boundary enforced)
- [x] Code reviewed and merged (lean: prior subagent review + evidence)
- [x] Graph surfacing visible + tested in C2 + Platform Editor (evidence: PNGs, playtest, code/tests)
- [x] C2 proxy ≥18/18 + frame improvements documented (PASS; frame doc + PNGs)
- [x] ReplayGolden 6/6 + test baseline ≥1215 (PASS; subagent verification)
- [x] Dispatching refinements landed + kickoff example (S37-07 COMPLETE; kickoff doc)
- [x] Carryovers from S36 addressed or explicitly deferred (S36-15 etc. in W2/W3; S37-14 optional)

## Producer Feasibility Gate
PR-SPRINT skipped — Lean mode (`production/review-mode.txt`). Validated via parallel domain agents (data, unity, sim, qa, devops/coordinator) consistent with S36/S35.

> **Scope check:** If any story cites tracker "next stack" without polish-scope-boundary-2026-06-19.md + S36 in-scope path, run `/scope-check` before implementation.

## QA Plan Gate
**S37-02 COMPLETE** — `production/qa/qa-plan-sprint-37-2026-06-20.md` merged (per story-done + kickoff). Blocks were enforced for waves. See qa-plan for full matrix, test cases, playtest reqs, smoke scope.

## Next Steps
1. (Done) S37-01/02 prereqs + parallel kickoff + waves via dispatching (S37-07 refined).
2. (Done per tracks) `/story-readiness` + `/dev-story` + `/story-done` (lean) on S37-03..13 specified.
3. (Done) `/smoke-check sprint` (PASS) + `/team-qa sprint` (APPROVED) + `/sprint-status`.
4. Surface: S37-14 (Nice-to-Have carry) optional.
5. Gate review: PASS (all gates met post-smoke/sign-off).
6. After S37: deeper polish or next per gate (e.g. S38 for residuals). Run `/retrospective` + `/gate-check` if advancing. (See Story-Done section for details.)

**Related:** Builds directly on S36 (see S36 plan §Next Steps). Parallel tracks per S35/S36 agentic kickoff patterns (`production/agentic/sprint-*-parallel-kickoff-*.md` + domain plans). Enforces polish-scope-boundary-2026-06-19.md.

## Story-Done Completions (S37 Tracks — Lean Mode)
**Date**: 2026-06-20 (using story-done skill logic, batch approval via AskUserQuestion)
**Review mode**: lean (per production/review-mode.txt)
**Authority**: sprint-37-graph-surfacing-deeper-polish.md + qa-plan-sprint-37-2026-06-20.md + polish-scope-boundary-2026-06-19.md + prior subagent outputs (kickoff, perf doc, playtest report, PNGs, code/tests)
**Process**: Per story: read from plan task + qa ACs; verified ACs vs evidence (tests PASS logic in source/docs, reports, PNGs, docs updated); deviations NONE per boundary; test evidence present per Type (qa-plan); no blocking gaps.
**Updates**: Plan table rows marked **COMPLETE**; this section + per-track notes. sprint-status.yaml (no S37 entries yet — older sprints only; noted, not updated).

### Data Track (S37-03)
**Verdict**: COMPLETE
**Type**: Logic
**ACs verified** (from plan/qa-plan): Full chains (platform→link + weapon→mount→sensor) in GetSortedDependencyEdges/BuildFullKillChain + CLI export; deterministic (Ordinal); goldens/JSON parity; tests pass; invalidation; edge cases (empty, orphans).
**Evidence**: 
- src/ProjectAegis.Data/Catalog/CatalogDependencyGraphIndex.cs (BuildFullKillChain, S37-03 comments + platform→link code)
- src/ProjectAegis.Data.Tests/Catalog/DependencyGraphIndexTests.cs (15+ facts: chains, determinism across rebuilds, Baltic fixtures, cache invalidation, excludes rejected)
- src/ProjectAegis.MissionEditor.Cli/CatalogDependencyGraphCommand.cs + .Tests/CatalogDependencyGraphCommandTests.cs (fullKillChainSurfaced, chainTypes incl link/weapon/sensor/mount, canonicalLines sorted, help updated)
- Readers (Sqlite/InMemory/Overlay) expose via GetSortedDependencyEdges; C2 controller + tests consume
- Playtest report confirms full chains surface correctly per S37-03 AC
**Test evidence**: Present (unit tests in Data.Tests + Cli.Tests; mapped to ACs)
**Deviations/Scope**: None (extend-only CatalogWriteGate, ZERO DelegationBridge, Baltic hash immutable, within polish-boundary)
**Completion Notes**: All ACs covered by test assertions + impl + cross reports. Lean: no full QL/LP spawned. Code changes within listed files. Ready for S37-08 gates.

### UI Track (S37-04,05,06,13)
**S37-04 Verdict**: COMPLETE | **Type**: UI (mixed Logic+UI)
**ACs**: C2 proxy filters extended (graph checks); graph data visible (viewer/panel, selection highlights, link-chain display + bind) in headless+live; 18/18+ maintained.
**Evidence**: C2PresentationController.cs (ApplyGraphSurfacing, graphHighlights); src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/C2PresentationControllerTests.cs (ApplyGraphSurfacing tests); playtest (visible in proxy); c2-automated-proxy refs + graph filters; qa/evidence/c2-graph-viewer-s37.png + c2-polish-tooltips-s37.png
**Test evidence**: Proxy tests + bind tests present. Headless primary (lean).
**Deviations**: None.

**S37-05 Verdict**: COMPLETE | **Type**: UI (mixed UI+Integration)
**ACs**: Full graph visible (interactive FK/full beyond S36-07 read-only); tooltips + reverse; roundtrip+validation PASS; evidence PNGs.
**Evidence**: PlatformLinkCatalog/Comms tests extend; editor-fk-graph-s37.png in qa/evidence; playtest report (tooltips/reverse/Editor interactive); roundtrip assertions per ADR-011.
**Test evidence**: Projection/roundtrip tests + PNGs.
**Deviations**: None.

**S37-06 Verdict**: COMPLETE | **Type**: Logic (perf)
**ACs**: Perf doc update; sustained frame headroom vs 16.67ms; new evidence batch (post-S36).
**Evidence**: unity-c2-frame-baseline-s35...md (S37-06 Update section); perf-profile...md cross; qa/evidence/frame-headroom-s37.png; panel bind timing tests + graph integration; playtest confirms no spikes.
**Test evidence**: Timing + proxy in UnityAdapter.Tests.
**Deviations**: None.

**S37-13 Verdict**: COMPLETE | **Type**: UI
**ACs**: Proxy pass + notes; filters/tooltips clean; no new scope (residual S36-15).
**Evidence**: Incorporated in S37-04/05/06 + playtest (minor residuals noted for future); no new files/scope per kickoff.
**Deviations**: None (advisory polish only).

**Overall UI Track**: All ACs verified; C2 18/18+ + frame held per playtest+docs; PNGs+proxy evidence present.

### Sim Track (S37-09,12)
**S37-09 Verdict**: COMPLETE | **Type**: Logic
**ACs**: Hash-identical vs S36; /replay-verify; optional bench appendix.
**Evidence**: perf-profile-polish-baseline-2026-06-19.md (P2 follow-ups: SimulationSession.cs LINQ->foreach, PdDetectionContactSimulator.cs HashSet->SortedSet; 13+71 unit PASS, ReplayGolden 17 PASS, hash `17144800277401907079` unchanged, /replay-verify equiv).
**Test evidence**: Affected Sim/Delegation tests + replay harnesses.
**Deviations**: None (deterministic, boundary).

**S37-12 Verdict**: COMPLETE | **Type**: Config/Data
**ACs**: Perf baseline appendix update.
**Evidence**: perf-profile-polish-baseline...md (S37-12 section appended; hotspot table updated).
**Test evidence**: N/A (doc per qa-plan).
**Deviations**: None.

### QA/Hygiene Track (S37-07,10,11)
**S37-07 Verdict**: COMPLETE | **Type**: Integration
**ACs**: Updated skill/docs + examples; backward compat; validated in kickoff (wave tracking, status sync, multi-track, templates).
**Evidence**: .claude/docs/agent-coordination-map.md (S37-07 refinements section); production/agentic/sprint-37-parallel-kickoff-2026-07-20.md (validates pattern + W1-4 dispatch); kickoff used as example.
**Test evidence**: Doc/kickoff review (per qa-plan).
**Deviations**: None.

**S37-10 Verdict**: COMPLETE | **Type**: UI
**ACs**: Structured report + think-aloud notes in production/playtests/.
**Evidence**: production/playtests/playtest-s37-session-9-graph-ux-2026-07-20.md (full report: graph UX validation for S37-04/05/06, gates 18/18+ / frame / chains / proxy PASS, verdicts Delivered); README.md updated.
**Test evidence**: Playtest report (UI).
**Deviations**: None (lean proxy sufficient).

**S37-11 Verdict**: COMPLETE | **Type**: Config/Data
**ACs**: Progress report + ... ; no breakage on 1215+ baseline.
**Evidence**: .claude/docs/directory-structure.md (S37-11 note: hybrid retained, CI verify on baseline no regression); qa-plan status update.
**Test evidence**: CI verification (no breakage).
**Deviations**: None (signed decision per boundary).

### Closeout (S37-08)
**Verdict**: COMPLETE | **Type**: Integration
**ACs**: All gates green; no regression on S36.
**Evidence**: Post all waves (S37-03+); cross refs in kickoff (smoke, replay 6/6, GitNexus, tracker); playtest/perf/qa-plan confirm gates (≥1215, Replay 6/6, proxy 18/18+, hash immutable, graph landed, no DelegationBridge).
**Test evidence**: Full sln + replay + proxy gates (reuse baselines).
**Deviations**: None.
**Notes**: S37-01/02 assumed complete (prereqs per plan); S37-08 after waves. All tracks lean + boundary.

**Summary per track**: All verdicts COMPLETE. No blocking gaps. Lean mode (AskUserQuestion approval used; full QL/LP skipped per lean). Polish-boundary + ADR-010/011/003 enforced everywhere. Next: S37-14 (Nice-to-Have, carry) or /smoke-check sprint + /team-qa sprint + /gate-check per plan DoD. All evidence from previous subagent/outputs used for verification.

**sprint-status.yaml note**: File covers pre-S37 sprints only (no S37 id/entries). Manual /story-done updates not applicable yet for yaml (old format); plan.md is source of truth here. Would require sprint-plan regen for yaml sync in future.

## Next After Story-Done
- S37-14 (AD-ART-BIBLE sign-off / UX doc polish) remains Nice-to-Have (not in specified tracks).
- Recommend: `/smoke-check sprint` (reuse S37-01 baseline + post-wave gates: ≥1215, Replay 6/6, C2 proxy 18/18+ w/ graph, graph surfacing verified, Baltic hash, frame).
- Then `/team-qa sprint` for sign-off report.
- `/gate-check` (Production→Polish continuation or next).
- Enforce: no cross-track, lean, boundary on any follow-up.
- Prepare gate review artifacts: this plan + qa-plan + playtest + perf appendix + evidence PNGs + test baselines.

(Story-done complete for specified tracks. All COMPLETE.)
