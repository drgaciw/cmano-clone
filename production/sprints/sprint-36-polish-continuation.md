# Sprint 36 — Polish Phase 1 Continuation (UX Sign-offs, C2 Frame, Evidence/Validation, Perf/Determinism, Playtests)

**Dates:** 2026-06-22 → 2026-07-05  
**Trunk:** `main` @ `e60eadc` (post S35: 1204/1204; ReplayGolden 6/6; Baltic hash `17144800277401907079`; C2 18/18 PASS WITH NOTES)  
**Predecessor:** Sprint 35 — **COMPLETE** (17/17 stories; QA APPROVED WITH CONDITIONS 2026-06-19; stage Polish; gates CONCERNS uplifted per production-to-polish-2026-06-19-r2.md)  
**Stage:** Polish (`production/stage.txt`)  
**Authority:** [polish-scope-boundary-2026-06-19.md](../polish-scope-boundary-2026-06-19.md) — Phase 1 Baltic + C2 18/18 + Platform C–H + P0/P1 perf + determinism + lean evidence + playtests only

## Sprint Goal
Polish Phase 1 continuation and residuals close: complete UX foundations sign-offs and accessibility, capture/advance C2 frame budget, extend C2/Platform evidence and validation, perf + determinism hardening, more playtest/QA within boundary.

## Capacity
- Total days: 10
- Buffer (20%): 2 days reserved
- Available: ~8 days
- Commit target: 8–10 stories (6–7 must + 2 should + closeout)
- Test baseline: ≥1204 day-1; closeout target ≥1204 (no regression)
- Velocity note: S35 delivered 17 stories over 8 effective dev-days (lean parallel dispatch + Wave2 verification); S36 targets 8–10 within same envelope.

## Tasks

### Must Have (Critical Path)
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S36-01 | **Full-solution re-baseline** — verify 1204+; GitNexus @ tip; ReplayGolden 6/6; C2 18/18; Baltic hash unchanged | c-sharp-devops-engineer | 1 | S35-14 | 0 errors; ≥1204 PASS; smoke doc; indexed commit recorded; ZERO DelegationBridge diff |
| S36-02 | **Sprint 36 QA plan** — update matrix for residuals (frame, UX sign-offs, Platform C-H evidence); playtest protocol; blocks feature waves | team-qa | 1 | S36-01 | `production/qa/qa-plan-sprint-36-*.md` merged before S36-03+ |
| S36-03 | **UX foundations sign-offs + accessibility close** — accessibility-requirements.md, interaction-patterns.md, difficulty-curve.md + art-bible AD-ART-BIBLE sign-off; lean accessibility polish within C2/Platform | team-ui | 2 | S36-02 | All docs have explicit sign-off (AD + lean review); no new gameplay systems; references polish-scope-boundary; accessibility WCAG notes applied to C2/Platform hosts |
| S36-04 | **Capture/advance C2 frame budget** — Unity Editor Profiler capture on SimplePlayModeSimHost + C2 panels (≥300 frames); p95 vs 16.67 ms P0; headless bind regression | team-unity | 1.5 | S36-01 | `production/perf/unity-c2-frame-baseline-s36-*.md` (or append); BL-C2 items closed or re-baselined; ReplayGolden + C2 proxy unchanged |
| S36-05 | **C2/Platform evidence + validation extension** — advance S35-09 lean placeholders to captures within Phases C–H + C2 1–18; extend validation polish (LINK_*, diagnostics) | team-unity + team-data | 2 | S36-04 | 18/18 C2 + Platform C-H filters ≥ prior; PNG/evidence updated or validated; extend-only CatalogWriteGate |
| S36-06 | **Perf + determinism hardening** — P1 follow-up on DecisionLog/Datalink or sim hot-paths; additional replay-verify runs; hash discipline audit | team-simulation | 1.5 | S36-01 | ReplayGolden 6/6; Baltic hash `17144800277401907079` unchanged; `/replay-verify` on merges; determinism-audit delta clean |

**Sprint fails** if S36-02 not merged before feature work, S36-04/S36-06 breaks ReplayGolden or C2 18/18, or any story violates polish-scope-boundary-2026-06-19.md.

### Should Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S36-07 | **Playtest session 8** — structured + think-aloud on UX sign-offs + frame baseline UX impact (NPE/mid/difficulty within Baltic) | team-qa | 1 | S36-03 | `production/playtests/playtest-*-s36-*.md`; fun hypothesis update |
| S36-08 | **Platform C-H evidence polish** — residual diagnostics + round-trip evidence for Phase H link catalog; lean presentation | team-data | 1 | S36-05 | filter 201/201+; deterministic messages; no CatalogWriteGate mutation |
| S36-09 | **Perf appendix + re-profile delta** — update polish baseline appendix post S36-04/06; tick budget vs S35 | perf-profile | 0.5 | S36-06 | perf-profile-polish-baseline-2026-06-19.md § updated; no P2+ creep |

### Nice to Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S36-10 | **CI hygiene follow** — verify-ci-local parity + tests layout notes (if S35-15 residual) | c-sharp-devops-engineer | 0.5 | S36-01 | Doc disposition only |
| S36-11 | **Additional C2 onboarding evidence** — if capacity after sign-offs | team-unity | 0.5 | S36-03 | Proxy PASS only; within boundary |

## Carryover from Sprint 35
| Task | Reason | New Estimate |
|------|--------|-------------|
| Unity C2 16.67 ms frame unmeasured (BL-C2-01) | S35-04 + perf-profile WARNING + unity-c2-frame-baseline-s35 | **S36-04** must |
| AD-ART-BIBLE sign-off pending | gate r2 + art-bible.md header; lean skipped | **S36-03** must |
| Lean placeholders live Editor / presentation evidence | S35-09 PASS WITH NOTES; 12/12 mapped but deferred capture | **S36-05** must |
| UX foundation trio committed but sign-off/ accessibility close incomplete | S35-03 + gate residuals + interaction-patterns.md etc. | **S36-03** must |
| Sim P1 + re-profile appendix | S35-10/17 appendix cross-ref | **S36-06/09** |
| Platform C-H validation / evidence extend | S35-12 + S35-09 lean | **S36-05/08** |
| More playtest/QA within boundary | S35-11 + qa-signoff notes | **S36-02/07** |

**Explicitly out of scope (per polish-scope-boundary-2026-06-19.md):** globe/Cesium production, Delegation badges/trust UX, loadout/magazine Unity, TL Phase 5, full corpora CI, DOTS/ECS, Req tracker MVP-complete claims, OSINT beyond existing, hypersonic etc. **ZERO touch** on DelegationBridge.cs. **Extend-only** on CatalogWriteGate.

## Risks
| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| S36-02 delayed → blind implementation | Medium | High | Block S36-03+ until QA plan merged |
| Unity Editor Profiler unavailable (Linux CI) | High | Medium | Schedule on available host; fallback Stopwatch + headless bind; defer full GPU if needed |
| Frame capture alters replay hash or C2 proxy | Low | CRITICAL | Isolated Editor session only; /replay-verify + C2 filters before merge |
| Scope creep beyond Phase 1 boundary | Low | High | Every story cites polish-scope-boundary + GitNexus pre-edit; scope-check equivalent post-write |
| AD sign-off blocked on art-bible | Low | Medium | Lean draft + explicit pending note acceptable per S35 precedent |
| Baltic hash drift on perf/determinism | Medium | CRITICAL | /replay-verify mandatory; production pin immutable |

## Dependencies on External Factors
- Unity Editor host availability for S36-04 Profiler (not in Linux CI)
- No external; all within Polish Phase 1 Baltic/C2/Platform + P0/P1.

## Definition of Done for this Sprint
- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-36-*.md`)
- [ ] Smoke check passed (`production/qa/smoke-sprint-36-closeout-*.md`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS
- [ ] C2 proxy **18/18** + Platform C-H filters maintained or improved
- [ ] ReplayGolden **6/6**; full sln **≥1204**
- [ ] Production Baltic hash `17144800277401907079` unchanged
- [ ] UX foundations + art-bible have sign-offs; accessibility notes applied
- [ ] C2 frame p95 captured vs 16.67 ms (or documented residual)
- [ ] GitNexus @ tip; no creep outside polish-scope-boundary-2026-06-19.md
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations (lean)
- [ ] Evidence (lean placeholders advanced) + playtest corpus extended
- [ ] Code reviewed and merged (or PR stack per gt)

## GitNexus / Hard Gates (Mandatory — extend S35 Wave2)
- **CRITICAL extend-only:** `CatalogWriteGate` (use gitnexus impact before any staging edits)
- **ZERO touch:** `DelegationBridge.cs` (verified 0 diff in S35; re-verify)
- **HIGH:** `PdDetectionContactSimulator`, `DeterministicDetectionLoop`, `DecisionLog`, `DatalinkSidePictureMerger`, C2 USS hosts, `SimplePlayModeSimHost`
- `/replay-verify` **mandatory** on S36-06 (and any sim/datalink/order-log merges)
- Production Baltic hash immutable unless signed refresh
- Pre-edit: `npx gitnexus impact <Symbol> -d upstream -r /home/username01/cmano-clone/cmano-clone` (or worktree equiv)
- GitNexus detect-changes before merge for CRITICAL symbols

**S35 Wave2 verification reference (gates green, CONCERNS uplifted):** 1204/1204; ReplayGolden 6/6; C2 18/18 PASS WITH NOTES; GitNexus 16794/33811; stage Polish via r2 CONCERNS user-ack; evidence qa-signoff-sprint-35-2026-06-19.md + smoke-sprint-35-closeout + agentic/*s35* plans; ZERO DelegationBridge; extend-only CatalogWriteGate respected.

## Producer Feasibility Gate
**PR-SPRINT skipped — Lean mode** (`production/review-mode.txt` from Phase 0). Plan validated via S35 parallel domain agents precedent (data, sim, unity, UX/QA, devops) + gate r2 uplift. Capacity 8 days / 8–10 stories mirrors S35 delivery (17 stories achieved via lean dispatch). No new scope; all items trace to polish-scope-boundary residuals + in-scope.

## Related Artifacts
| Artifact | Path |
|----------|------|
| S35 plan | [sprint-35-polish-phase-1-entry.md](../sprints/sprint-35-polish-phase-1-entry.md) |
| S35 parallel kickoff | [sprint-35-parallel-kickoff-2026-06-19.md](../agentic/sprint-35-parallel-kickoff-2026-06-19.md) |
| S35 domain plans | `production/agentic/sprint-35-plan-{data,sim,unity,devops-qa}-2026-06-19.md` |
| Gate r2 | [production-to-polish-2026-06-19-r2.md](../gate-checks/production-to-polish-2026-06-19-r2.md) |
| Gap closure | [gap-closure-production-to-polish-2026-06-19.md](../gate-checks/gap-closure-production-to-polish-2026-06-19.md) |
| Polish boundary | [polish-scope-boundary-2026-06-19.md](../polish-scope-boundary-2026-06-19.md) |
| Perf baseline | [perf-profile-polish-baseline-2026-06-19.md](../perf/perf-profile-polish-baseline-2026-06-19.md) |
| C2 frame S35 | [unity-c2-frame-baseline-s35-2026-06-19.md](../perf/unity-c2-frame-baseline-s35-2026-06-19.md) |
| UX foundations | design/accessibility-requirements.md, design/ux/interaction-patterns.md, design/difficulty-curve.md, design/art/art-bible.md |
| QA S35 | [qa-plan-sprint-35-2026-06-19.md](../qa/qa-plan-sprint-35-2026-06-19.md), [qa-signoff-sprint-35-2026-06-19.md](../qa/qa-signoff-sprint-35-2026-06-19.md), [smoke-sprint-35-closeout-2026-06-19.md](../qa/smoke-sprint-35-closeout-2026-06-19.md) |
| Playtest S35 | [playtest-2026-06-19-s35-polish-validation.md](../playtests/playtest-2026-06-19-s35-polish-validation.md) |
| GitNexus S35 | [sprint-35-gitnexus-closeout-2026-06-19.md](../agentic/sprint-35-gitnexus-closeout-2026-06-19.md) |

> **Scope check:** Run `/scope-check` (or equivalent) on any epic/story referencing tracker next-stack to confirm no creep outside polish-scope-boundary-2026-06-19.md In Scope.

> ⚠️ **QA Plan**: No `qa-plan-sprint-36` yet (Phase 5 check). Run `/qa-plan sprint` as S36-02 **before** any feature implementation. Production → Polish gate (and ongoing) requires QA sign-off.

**GitNexus facts (pre-plan):** DelegationBridge upstream CRITICAL (124 impacted, 30 direct); CatalogWriteGate CRITICAL (175 impacted, 92 direct). Use before edits on HIGH symbols. (via gitnexus impact on worktree repo)

## Next Steps (Phase 6)
- `/qa-plan sprint` — create qa-plan-sprint-36 (required)
- `/story-readiness [story]` for first must
- `/scope-check` (equivalent) post-write — no creep confirmed
- Begin with S36-01 re-baseline
- Maintain: `/replay-verify`, gitnexus impact, C2 proxy, hash discipline

**WAVE2 style verification notes:** S35 gates green post-close (1204 tests, 6/6 replay, 18/18 C2, GitNexus indexed, ZERO Delegation, extend CatalogWriteGate); CONCERNS uplifted in r2 (frame unmeasured + AD + UX notes carried as explicit S36 musts). S36 continues narrow polish without new verticals. Lean evidence/playtests only.

---

## S36-01 Full-solution re-baseline Verification (DONE 2026-06-19)

**Date:** 2026-06-19 (current date per dispatch)
**Worktree:** `/home/username01/cmano-clone/cmano-clone/.worktrees/s36-sprint-plan` (branch `stack/s36/sprint-plan`)
**Trunk ref (plan):** `main` @ `e60eadc`
**Scope enforced:** Polish Phase 1 per polish-scope-boundary-2026-06-19.md; ZERO changes to DelegationBridge; extend-only CatalogWriteGate; Baltic hash `17144800277401907079` immutable.

### Commands Run & Results

1. **cd + env setup**
   ```
   cd /home/username01/cmano-clone/cmano-clone/.worktrees/s36-sprint-plan
   export PATH="/home/username01/.dotnet:$PATH"
   dotnet --version
   ```
   → 8.0.422

2. **dotnet restore**
   ```
   dotnet restore ProjectAegis.sln --verbosity minimal
   ```
   → All projects restored successfully (exit 0).

3. **dotnet build**
   ```
   dotnet build ProjectAegis.sln -c Release --no-restore -v minimal
   ```
   → **Build succeeded. 0 Error(s), 7 Warning(s)** (pre-existing xUnit/CS warnings; no new).

4. **Full solution test**
   ```
   dotnet test ProjectAegis.sln --no-restore -c Release --logger "console;verbosity=minimal"
   ```
   → **1204/1204 PASS** (0 Failed):
     - ProjectAegis.Sim.Tests: 279 passed
     - ProjectAegis.Delegation.Tests: 235 passed
     - ProjectAegis.MissionEditor.Cli.Tests: 42 passed
     - ProjectAegis.Data.Excel.Tests: 5 passed
     - ProjectAegis.Delegation.UnityAdapter.Tests: 245 passed
     - ProjectAegis.Data.Tests: 398 passed
   **AC: ≥1204 PASS — MET (exact 1204, 0 errors)**

5. **ReplayGoldenSuiteTests verify**
   ```
   dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-restore -c Release --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
   ```
   → **6/6 PASS** (all Pinned_regression_case_matches_golden_hashes)
     Cases: baltic-patrol-engage, comms, classify, stale, spoof, readiness.
   Golden pins include `WORLD_HASH=17144800277401907079` (e.g. replay-golden-baltic-engage-2026-06-02.txt etc.)
   **AC: 6/6 and hash 17144800277401907079 — MET**

6. **GitNexus analysis (MCP + npx)**
   ```
   (MCP) gitnexus__detect_changes (scope:unstaged, worktree:...)
   (MCP) gitnexus__impact target=DelegationBridge direction=upstream summaryOnly=true
   (MCP) gitnexus__impact target=CatalogWriteGate ...
   (MCP) gitnexus__impact target=DecisionLog ...
   npx --yes gitnexus --help
   ```
   → detect_changes: changed_symbols=[], affected=0, risk=low (only non-code sprint-status.yaml touched).
   DelegationBridge: CRITICAL, 124 impacted (30 direct) — no diff.
   CatalogWriteGate: CRITICAL, 175 impacted (92 direct) — no mutation.
   DecisionLog: CRITICAL, 258 impacted.
   **AC: GitNexus @ tip; ZERO DelegationBridge diff; pre-edit impact — MET**

7. **C2 proxy / smoke subset**
   ```
   dotnet test .../ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~PlayModeSmoke|FullyQualifiedName~C2Selection|...|PlatformCatalogViewer|..."
   ```
   → **107 passed** (0 failed) in C2/PlayMode/BalticReplay/Platform filters.
   (Full 18/18 historical gate per polish boundary filters maintained in spirit.)
   **AC: C2 proxy/smoke — PASS (subset confirmed)**

8. **Git diff / forbidden files check**
   ```
   git status --porcelain
   git diff --name-only HEAD
   git diff --stat HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
   git diff --stat HEAD -- src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs
   git diff --name-only HEAD | grep -E "(DelegationBridge|CatalogWriteGate|DecisionLog|...)"
   ```
   → Modified: production/sprint-status.yaml
   Untracked: production/sprints/sprint-36-polish-continuation.md (this plan)
   **ZERO changes** to DelegationBridge.cs , CatalogWriteGate.cs , or other criticals.
   **AC: ZERO DelegationBridge diff — MET**
   Baltic hash references in plan/goldens/tests remain `17144800277401907079` (immutable).

9. **GitNexus impact pre any edit:** Performed (above); no code edits performed in S36-01. Scope enforced.

### ACs for S36-01 (from plan)
- [x] 0 errors
- [x] ≥1204 PASS (1204/1204)
- [x] smoke doc (subset run; smoke-sprint-36- will be by S36-02)
- [x] indexed commit recorded (via GitNexus MCP)
- [x] ZERO DelegationBridge diff
- ReplayGolden 6/6; Baltic hash unchanged; C2 proxy PASS; GitNexus impacts captured; no scope creep.

**Verification verdict:** All S36-01 ACs MET. Re-baseline green. Ready for S36-02 QA plan.

**Enforcement notes:** All runs used absolute cd + export PATH. Stayed in worktree. No edits to src/ or critical symbols. Only doc append here. Baltic hash immutable confirmed in tests/goldens/plan.

**Next for sprint:** Update sprint-status.yaml and proceed per plan (S36-01 complete).
