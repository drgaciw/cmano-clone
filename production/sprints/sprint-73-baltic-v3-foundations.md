# Sprint 73 — Baltic v3 Foundations

**Dates:** 2026-06-25 to ~2026-07-01 (est. 5–7 days)  
**Lead:** E9 Baltic v3 content expansion (S73 foundations)  
**Goal:** Publish `baltic-v3-scope-boundary-2026-06-25.md` (supersedes commercial-launch for S73+ only), playtest manifest v3, GitNexus re-index, closeout with full verification. Foundation for S74–S80 (scenarios v3, theater, catalog, C2, playtest).  
**Capacity:** ~5–7 days total, 20% buffer. Local coordinator owns boundary/closeout/human; cloud for manifest + re-index (cap 4 tracks).  
**Model:** Per `roadmap-execute-plan-062526.01.md` §3/§4/§5 (serial S73–S80; parallel tracks inside after boundary; GitNexus + verification-before; dispatching-parallel-agents + isolated worktrees). Stage remains **Release**. Cite `baltic-v3-scope-boundary-2026-06-25.md` + `future-sprint-roadpmap-062526.01.md` + execute-plan + design spec heavily on all artifacts. Prereqs: S72 complete (human ack "commercial launch prep complete").  

## Tasks / Tracks (from roadmap-execute-plan-062526.01.md §4 exact)

### Must Have (Critical Path per §3/§4)
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S73-01 | Scope boundary publish | producer (Local) | 1 | — | `production/baltic-v3-scope-boundary-2026-06-25.md` published (cites `future-sprint-roadpmap-062526.01.md` §3/§6/§7/§10 + `roadmap-execute-plan-062526.01.md` §3/§4 + commercial-launch-scope-boundary-2026-06-25.md (supersede for S73+); in/out scope per execute §4 + design; carry invariants 1232/6/6/18/18/hash 17144800277401907079/ZERO/ extend-only; GitNexus pre list/detect/impact exact 178/97/127/52 low risk; gates 0e/1232/0f/6/6/18/18/hash/ZERO RUN+READ; stage=Release) |
| S73-02 | Playtest manifest v3 | qa-lead (Cloud) | 1 | S73-01 boundary | `production/playtests/baltic-v3-scenario-manifest.yaml` (extends v2; defines v3 slots, difficulty bands, human template refs; cites boundary + execute §4 + design §2 isolation model; GitNexus pre) |
| S73-03 | GitNexus re-index | c-sharp-devops-engineer (Cloud) | 1 | Boundary | CLI analyze (if stale) + MCP list_repos canonical; detect low; impacts CRITICAL 178/97/127/52 exact; update index notes; verif gates; cite boundary + execute §5/§9 |
| S73-04 | Closeout | c-sharp-devops-engineer (Local) | 1–2 | All prior | gt submit/restack; re-run Phase 0 gates (build/test/replay/C2/hash/ZERO/GitNexus); smoke closeout doc; update sprint-status.yaml + qa/; all artifacts cite boundary + roadmaps + execute + design; human review |

### Should Have
- Sprint plan + kickoff artifacts created at dispatch

### Nice to Have
- (defer detailed content to S74+)

**Wave order (per §4):** S73-01 (boundary, day 1) → (W1 playtest-manifest ∥ W2 gitnexus-reindex) → W3 Closeout

**S73-01 Closeout Update (2026-06-25):** [To be filled at S73-04]. This S73-01 boundary + worktree baltic-v3-boundary prep complete. Prereqs S72 PASS + ack. Gates + GitNexus pre RUN+READ (see boundary). Ready for parallel dispatch S73-02/03.

## Baseline @ Start (verification-before, fresh RUN+READ 2026-06-25 per execute §5/§6 + boundary)

- Tests: **1232/0f** (Data 406 + Sim 279 + Del 247 + UA 252 + Cli 43 + Excel 5)  
- ReplayGolden: **6/6**  
- C2 proxy: **18/18**  
- Build: **0e/0w**  
- Hash: `17144800277401907079` preserved in goldens (8+ hits)  
- ZERO DelegationBridge (no edits to .cs; adapter only)  
- GitNexus: 20193 nodes / 37859 edges / 2487 files (list_repos canonical @ latest); detect unstaged 1/0 low (doc); impacts upstream summaryOnly: CatalogWriteGate=178, PatrolCandidateEngagePolicy=97, DelegationBridge=127, BalticReplayHarness=52 (**exact §5/§7**)  
- S69–S72 COMPLETE (s72-commercial-launch-prep-gate + smoke-sprint-72-closeout-2026-06-25.md + human ack)  
- Prior boundary superseded for S73+: commercial-launch-scope-boundary-2026-06-25.md (archive, do not delete; invariants carry)  
- S72 human ack: "commercial launch prep complete" (2026-06-25)  
- Design + isolation: v3 policies `baltic-v3-*` isolated from v2 frozen baseline

## Risks & Mitigations
- GitNexus staleness (index 3 commits behind at authoring): Use MCP list/impact/detect + CLI analyze; low for doc-only. Re-index S73-03.
- Parallel merge conflicts: Single owner per track/file (e.g. CatalogWriteGate in S77); gt restack on closeout only.
- Scope creep (v3 vs E7/multi/bridge): Enforce boundary in/out (E9 v3 content IN; E7 sub / multiplayer / DelegationBridge / hash w/o ADR / full tracker OUT). Every artifact cites boundary.
- Detect changes on docs: Expected low; re-run pre-commit.
- v3 goldens/hash drift: Isolated goldens only; production hash immutable w/o ADR.

## Definition of Done
- All must-have complete (4 tracks)
- Gates PASS (build 0e, test ≥1232 0f, replay 6/6, C2 18/18, hash preserved, ZERO bridge)
- GitNexus preflights + detect_changes clean (doc-only expected); impacts §5/§7 exact
- Artifacts: `baltic-v3-scope-boundary-2026-06-25.md`, `baltic-v3-scenario-manifest.yaml`, sprint-73-*.md + agentic kickoff, smoke-closeout, status updates, production/sprints/...
- All cite `baltic-v3-scope-boundary-2026-06-25.md` (target) + `future-sprint-roadpmap-062526.01.md` §3/6/7/10 + `roadmap-execute-plan-062526.01.md` §3/4/5/6/7/9 + design + AGENTS.md + commercial-launch-scope-boundary (invariants)
- S72 prereqs confirmed complete; stage remains Release (no stage.txt advance)
- Worktree isolation (baltic-v3-boundary + siblings); gt ready post closeout

## Closeout Prep Note (S73-01 boundary track contribution, per execute-plan §5/§9 + AGENTS.md)
S73-01 GitNexus + verification-before COMPLETE (search_tool first + use list_repos canonical /home/username01/projects/active/cmano-clone/cmano-clone : 20193/37859; detect_changes unstaged 1/0 low doc-only; impact CatalogWriteGate=178 CRITICAL / Patrol=97 / Bridge=127 exact / Baltic=52 exact; build 0e/0w; full test 1232/0f; C2 18/18; replay 6/6; hash 17144800277401907079 preserved (8 hits); ZERO hotpath=0). Boundary written in worktree stack/sprint73/baltic-v3-boundary. All RUN+READ outputs. Low risk docs-only. Updated: sprint-status.yaml (s73_status), this sprint-73-*.md , kickoff prep. Cite all authorities everywhere. Ready for S73-02/03 dispatch + S73-04 closeout gt + final verif. S73-01 COMPLETE. (Independent subagent: S73-01 Scope boundary subagent (Local, producer, per execute-plan §4 and design). No scope creep; stage remains Release.)

## Next (per execute-plan §5/§9 + roadmap)
- S73-01 boundary before artifact tracks
- Dispatch S73-02 playtest-manifest (cloud) ∥ S73-03 gitnexus-reindex (cloud)
- S73-04 closeout (local): re-verif, smoke, status, gt restack notes
- S73 full COMPLETE after gates + human review; ready S74 dispatch

**Cites (MANDATORY on all S73 artifacts):** `production/baltic-v3-scope-boundary-2026-06-25.md` ; `docs/reports/future-sprint-roadpmap-062526.01.md` §3/§6/§7/§10 ; `docs/reports/roadmap-execute-plan-062526.01.md` §3/§4/§5/§6/§7/§9 ; `docs/superpowers/specs/2026-06-25-baltic-v3-content-expansion-design.md` ; `production/commercial-launch-scope-boundary-2026-06-25.md` (supersede S73+ invariants) ; AGENTS.md ; S72 complete evidence (gate + smoke + ack) ; prior Baltic v2 boundary (for baseline pattern).

*Independent subagent: S73-01 boundary (local producer). GitNexus pre + verif-before executed. Docs-only. "S73-01 COMPLETE". 2026-06-25.*