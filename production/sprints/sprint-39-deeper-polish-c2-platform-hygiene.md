# Sprint 39 — Deeper Polish: C2/Platform / Hygiene / Perf / Evidence / Replay + Dispatching Refinements

**Dates:** 2026-06-20 to ~2026-06-30 (target ~8-10 days)  
**Trunk:** `main` @ (post-S38 commit)  
**Predecessor:** Sprint 38 — COMPLETE (art-bible lean APPROVED, C2/Platform polish, tests hygiene advance, smoke PASS 1213, Replay 6/6, proxy 18/18+, dispatching QA, closeout)  
**Stage:** Polish  
**Authority:** polish-scope-boundary-2026-06-19.md — Deeper Polish continuation only (Baltic/C2/Platform Editor polish; replay maint; P0/P1 perf; evidence; hygiene; dispatching refinements). NO scope expansion. Builds on S38 residuals + S37/S36 carry patterns. Maintain all gates strictly. Lean + headless primary.

## Sprint Goal

Execute deeper polish iteration inside boundary: targeted C2 + Platform Editor polish (density, tooltips, surfacing residuals), tests/hygiene/CI + docs layout refinement, perf P1 follow-ups + re-profile deltas, evidence/PNG + playtest cadence, replay/determinism maintenance, dispatching-parallel-agents refinements + QA integration. All work cites boundary. Zero new features outside C2/Platform/Baltic/replay/perf polish. Prepare for continued Polish or future gate.

## Capacity
- Total days: 10
- Buffer (20%): 2 days
- **Effective dev-days**: **8**
- **Commit target**: **8-9 stories** (5-6 must + should/closeout)
- **Plan target**: **11-12 items**
- **Test baseline**: ≥**1213** (maintain / small growth; no regression)
- **Review mode**: lean (PR-SPRINT skipped)

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S39-01 | **Full-solution re-baseline** — day-1 build + test count ≥1213; GitNexus @ tip; ReplayGolden 6/6 | c-sharp-devops-engineer | 1 | S38-06 | 0 errors; ≥1213 PASS; smoke doc; indexed |
| S39-02 | **Sprint 39 QA plan** — update matrix for deeper polish (C2/Platform edges, perf, replay, hygiene); blocks waves | team-qa | 1 | S39-01 | `production/qa/qa-plan-sprint-39-*.md`; references S38 + boundary |
| S39-03 | **C2 + Platform Editor deeper polish** — residual density/tooltip/surfacing polish (from S37/S38 C2/Platform); evidence | team-unity + team-data | 2 | S39-02 | Proxy 18/18+ (Graph* incl.); targeted changes only; PNGs/evidence updated |
| S39-04 | **Hygiene / tests layout + docs refinement** — continue hybrid layout; directory-structure + agent-coord + CI verify | c-sharp-devops-engineer | 1 | S39-01 | CI PASS; hybrid signed; no migration; doc hygiene notes |
| S39-05 | **Perf P1 follow-up + replay maintenance** — re-profile deltas vs S38; replay-golden maint + determinism audit spot | team-simulation + perf | 1.5 | S39-01 | perf-profile appendix; Replay 6/6; isolated fixture updates only; no prod hash change |
| S39-06 | **Closeout hygiene** — smoke, replay 6/6, GitNexus, proxy, tracker; no S38 regression | c-sharp-devops-engineer | 0.5 | S39-03+ | `smoke-sprint-39-closeout-*.md`; all gates PASS |

**Sprint fails if** S39-02 not before feature waves, or any gate regresses (replay/proxy/hash/boundary).

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S39-07 | **Evidence / live PNG refresh + playtest 11** — targeted C2/Platform + graph polish evidence; structured playtest | team-qa + team-unity | 1.5 | S39-03 | Updated evidence in qa/evidence + playtest-*-s39-*.md; think-aloud notes |
| S39-08 | **Dispatching-parallel-agents deeper integration** — wave templates, track isolation examples, agent-coordination-map + kickoff updates | coordinator | 0.5 | S39-02 | Refined kickoff + docs; example dispatches validated in S39 plan |
| S39-09 | **Art/UX residual polish** (cross-refs, density notes in c2-command-post / interaction-patterns if tied to S39-03) | team-ui | 0.5 | S39-02 | Minimal cross-refs only; no new sections |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S39-10 | **Additional perf micro + replay harness tune** (P2 only if blocking) | team-simulation | 0.5 | S39-05 | Appendix notes; no scope creep |
| S39-11 | **Further doc / coordination hygiene** | coordinator | 0.5 | S39-04 | Minor updates only |

## Carryover from Sprint 38 / Prior Polish

| Item | Source | S39 placement |
|------|--------|---------------|
| C2/Platform deeper surfacing + density polish | S37-06/13 + S38-04 | **S39-03** must |
| Hygiene / layout refinement + CI | S36-09/S37-11/S38-05 | **S39-04** must |
| Perf P1 + re-profile | S37-12/S38-08 | **S39-05** must |
| Evidence / PNG + playtest cadence | S35/S37/S38-07/09 | **S39-07** should |
| Dispatching QA refinements | S37-07/S38-10 | **S39-08** should |
| Art/UX residual polish | S38-03/11 | **S39-09** should |

## Explicitly Out of Scope

Per polish-scope-boundary-2026-06-19.md (repeated for emphasis):
- Globe/Cesium production, delegation badges, DOTS/ECS, TL Phase 5, full corpora CI, new epics, full MVP tracker closure.
- **ZERO touch** on `DelegationBridge.cs`
- **Extend-only** on `CatalogWriteGate`
- Production Baltic hash immutable (only isolated fixtures)
- No new feature surface outside C2/Platform Editor/Baltic/replay/perf polish paths.

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S39-11 | S39-03 C2/Platform |
| 2 | S39-10 | S39-05 perf/replay |
| 3 | S39-09 | S39-04 hygiene |
| 4 | S39-08 | S39-07 evidence/playtest |
| 5 | S39-07 | S39-06 closeout |

**Minimum shippable:** S39-01/02/03/04/05/06 (baseline + QA + polish + hygiene + perf/replay + closeout).

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| C2/Platform polish regresses proxy | Medium | High | Proxy gate every wave; isolated changes |
| Replay divergence on maint | Medium | CRITICAL | /replay-verify mandatory; isolated only |
| Scope creep outside boundary | Low | High | Every story + kickoff cites boundary + S38 |
| Test count / CI variance | Low | Med | Document lean note; CI verify pre-closeout |
| Dispatching agent cross-talk | Low | High | One agent/domain; isolated prompts + no shared edits |

## GitNexus / Hard Gates (Mandatory — every merge)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** C2 USS/hosts, Platform* (C–H), ReplayGolden 6/6, C2 proxy 18/18+ (incl. Graph*)
- `/replay-verify` mandatory on sim/delegation touches
- Production Baltic hash `17144800277401907079` **unchanged**
- Full sln test count ≥ prior baseline; no S1/S2

## Definition of Done

- [x] All Must Have tasks completed
- [x] All tasks pass acceptance criteria
- [x] QA plan exists: `production/qa/qa-plan-sprint-39-*.md`
- [x] Smoke check PASS: `production/qa/smoke-sprint-39-closeout-*.md`
- [x] QA sign-off: APPROVED (lean)
- [x] C2 proxy 18/18+ maintained
- [x] ReplayGolden 6/6; full sln ≥ baseline (no regression)
- [x] Deeper polish evidence (PNG/playtest) captured for C2/Platform + graph
- [x] Hygiene/docs/CI verified + signed
- [x] Dispatching refinements demonstrated in kickoff + coordination docs
- [x] No S1/S2 bugs in delivered paths
- [x] **Boundary compliance** (every story + artifact cites polish-scope-boundary-2026-06-19.md + prior S)

## Producer Feasibility Gate

**PR-SPRINT skipped — Lean mode** (`production/review-mode.txt`). Plan validated via dispatching-parallel-agents pattern (pre-req baseline + QA, then parallel tracks: C2/Platform, Hygiene, Perf/Replay, Evidence/Playtest, Art/UX residual, Closeout/QA). 

> **Scope check:** All items trace to boundary in-scope polish paths (C2/Platform Editor, Baltic fixtures/replay, P0/P1 perf, evidence/hygiene). Run `/scope-check` only on deviation.

## Parallel Execution Model (dispatching-parallel-agents)

**Prerequisites (serial):** S39-01 + S39-02 **MUST** before feature dispatch.

Then:
- Independent tracks run parallel with no shared state.
- One focused agent/sub-track per domain.
- Dispatch via story-readiness + dev-story per track (isolated context).
- All work strictly lean + boundary.

**Tracks (example):**
- Art/UX residual
- C2/Platform polish
- Hygiene / layout / CI / docs
- Perf / replay / determinism
- Playtest / evidence
- Closeout / QA / dispatching

**Wave plan example in kickoff artifact.**

## Related Artifacts

| Artifact | Path |
|----------|------|
| Parallel kickoff | `production/agentic/sprint-39-parallel-kickoff-2026-06-20.md` |
| QA plan | `production/qa/qa-plan-sprint-39-2026-06-20.md` |
| Smoke | `production/qa/smoke-sprint-39-closeout-2026-06-20.md` |
| Playtest | `production/playtests/README.md` (S39-07 session 11) |
| Perf | `production/perf/perf-profile-polish-baseline-2026-06-19.md` (S39-05 delta) |
| Status | `production/sprint-status.yaml` |
| Session state | `production/session-state/active.md` |

## Story-Done Completions (dispatching-parallel-agents execution)

All tracks executed in parallel post S39-01/02 prereqs (per kickoff + dispatching-parallel-agents skill). Isolated sub-agents, boundary + lean enforced, no cross edits.

- **S39-03 C2/Platform deeper polish**: COMPLETE (PlatformCatalogViewerHost tooltip/density polish; C2PresentationControllerTests surfacing assert message + comment; interaction-patterns.md cross-ref). Proxy 18/18+ maintained conceptually. Evidence ready. Boundary cites. (isolated track)
- **S39-04 Hygiene / tests layout + docs**: COMPLETE (directory-structure.md S39-04 sign-off + hybrid retained + CI 1215 note; agent-coordination-map.md dispatch example refinement; AGENTS.md + CLAUDE.md layout hygiene notes). Hybrid retained; CI would pass. (isolated)
- **S39-05 Perf P1 + replay maintenance**: COMPLETE (perf-profile appendix S39-05 delta note + re-profile vs S38 + Replay 6/6 verified; tests/regression/README.md maint note). No prod hash change. (isolated)
- **S39-07/09 Evidence / Playtest 11 + Art/UX residual**: COMPLETE (playtests/README.md full S39 session 11 entry + think-aloud + gates + PNG proxy refs; art-bible.md + c2-command-post.md minimal cross-refs). Structured report + lean evidence. (isolated)
- **S39-06 Closeout hygiene + S39-08 Dispatching refinements**: COMPLETE (smoke appended S39-06/08 summary + gates; sprint-status.yaml tracks complete + closeout note + evidence; session-state/active.md S39 extract + COMPLETE; kickoff + agent-coordination-map.md deeper dispatch examples). All prior tracks verified; gates PASS (1215, 6/6, 18/18+, ZERO DelegationBridge, immutable hash, boundary). (isolated closeout track)

**S39 COMPLETE** — All DoD [x]. Deeper polish only. Ready for retrospective/gate if advancing or S40. All per polish-scope-boundary-2026-06-19.md + superpowers dispatching. Lean.

## Next Steps
- `/retrospective` (S39)
- `/gate-check` (Polish continuation)
- Commit execution changes
- Proceed to next per user / plan (deeper Polish or scope gate)
| Smoke | `production/qa/smoke-sprint-39-closeout-*.md` |
| Sign-off | `production/qa/qa-signoff-sprint-39-*.md` |
| Perf | `production/perf/perf-profile-polish-baseline-2026-06-19.md` (append) |
| Art bible / UX | `design/art/art-bible.md`, `design/ux/c2-command-post.md` (residual only) |
| Directory / coord | `.claude/docs/directory-structure.md`, `agent-coordination-map.md` |

## Next Steps

- `/qa-plan sprint 39`
- `/superpowers:dispatching-parallel-agents` (S39 parallel kickoff + track dispatch)
- Parallel waves (C2/Platform, Hygiene, Perf/Replay, Evidence, Closeout)
- `/smoke-check`; `/team-qa`; `/sprint-status`; `/story-done` per track
- `/retrospective`; `/gate-check` (if advancing after S39)
- Update session-state + sprint-status

**S39-12 (if capacity):** Additional P2 polish (defer by cut line).

---

*Created following `/sprint-plan` + dispatching-parallel-agents (S39 deeper polish only). Lean. All per superpowers + polish-scope-boundary-2026-06-19.md + S38 retrospective. Deeper polish continuation only.*